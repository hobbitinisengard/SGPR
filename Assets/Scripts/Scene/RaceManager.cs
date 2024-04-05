using System;
using UnityEngine;
using PathCreation;
using System.Collections;
using Newtonsoft.Json;
using System.IO;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using Unity.Services.Lobbies.Models;
using Unity.Netcode;
using Unity.Services.Lobbies;

namespace RVP
{
	public enum PartOfDay { Day, Night };
	[DisallowMultipleComponent]

	public class RaceManager : MonoBehaviour
	{
		AudioSource musicPlayer;
		public GameObject Sun;
		public ViewSwitcher viewSwitcher;
		public PathCreator[] racingPaths;
		public InputActionReference submitInput;
		public InputActionReference cancelInput;
		//public GameObject Sun;
		[Tooltip("Reload the scene with the 'Restart' button in the input manager")]
		public bool quickRestart = true;

		[Tooltip("Mask for what the wheels collide with")]
		public LayerMask wheelCastMask;
		public static LayerMask wheelCastMaskStatic;

		[Tooltip("Mask for objects which vehicles check against if they are rolled over")]
		public LayerMask groundMask;
		public static LayerMask groundMaskStatic;

		[Tooltip("Mask for objects that cause damage to vehicles")]
		public LayerMask damageMask;
		public static LayerMask damageMaskStatic;

		public static int ignoreWheelCastLayer;

		[Tooltip("Frictionless physic material")]
		public PhysicMaterial frictionlessMat;
		public static PhysicMaterial frictionlessMatStatic;

		public static Vector3 worldUpDir; // Global up direction, opposite of normalized gravity direction

		[Tooltip("Maximum segments per tire mark")]
		public int tireMarkLength;
		public static int tireMarkLengthStatic;

		[Tooltip("Gap between tire mark segments")]
		public float tireMarkGap;
		public static float tireMarkGapStatic;

		[Tooltip("Tire mark height above ground")]
		public float tireMarkHeight;
		public static float tireMarkHeightStatic;

		[Tooltip("Lifetime of tire marks")]
		public float tireFadeTime;
		public static float tireFadeTimeStatic;

		public PartOfDay pod = PartOfDay.Day;
		public NetworkManager networkManager;
		public SGP_HUD hud;
		public EditorPanel editorPanel;
		public CameraControl cam;
		public CountDownSeq countDownSeq;
		public ResultsSeq resultsSeq;
		public PauseMenuButton sfxButton;
		public PauseMenuButton musicButton;
		public GameObject DemoSGPLogo;

		VehicleParent leader;
		[NonSerialized]
		public VehicleParent playerCar;
		public event Action ClosingRace;

		bool firstDriverFinished = false;

		public static RaceManager I 
		{
			get; private set;
		}
		public enum InfoMessage
		{
			TAKES_THE_LEAD, NO_ENERGY, SPLIT_TIME,
		}
		public int ActiveCarsInKnockout
		{
			get
			{
				int activeCars = 0;
				foreach (var car in F.I.s_cars)
				{
					if (car.raceBox.enabled)
						activeCars++;
				}
				return activeCars;
			}
		}

		public void BackToEditor()
		{
			ClosingRace?.Invoke();
			hud.Disconnect();
			hud.gameObject.SetActive(false);
			cam.Disconnect();
			cam.enabled = false;
			if (F.I.gameMode == MultiMode.Singleplayer || (MultiPlayerSelector.I && MultiPlayerSelector.I.server.AmHost))
				for (int i = 0; i < F.I.s_cars.Count; ++i)
					Destroy(F.I.s_cars[i].gameObject);

			F.I.s_cars.Clear();

			editorPanel.gameObject.SetActive(true);
			F.I.raceStartDate = DateTime.MinValue;
		}
		public void BackToMenu(bool applyScoring)
		{
			//if (F.I.s_cars.Count < 2)
			//	applyScoring = false;

			//foreach (var c in F.I.s_cars)
			//	Destroy(c.gameObject);
			//F.I.s_cars.Clear();

			if(F.I.gameMode == MultiMode.Multiplayer)
			{
				F.I.actionHappening = ActionHappening.InLobby;
				MultiPlayerSelector.I.server.lobby.Data[ServerConnection.k_actionHappening] = new DataObject(DataObject.VisibilityOptions.Public, F.I.actionHappening.ToString());
				if(applyScoring)
				{
					Debug.Assert(F.I.s_cars.Count > 0);
					ResultsView.resultData = new ResultsView.PersistentResult[F.I.s_cars.Count];
					for (int i = 0; i < F.I.s_cars.Count; ++i)
					{
						ResultsView.resultData[i] = new ResultsView.PersistentResult()
						{
							drift = F.I.s_cars[i].raceBox.drift,
							lap = F.I.s_cars[i].raceBox.bestLapTime,
							stunt = F.I.s_cars[i].raceBox.Aero,
							name = F.I.s_cars[i].transform.name,
						};
					}
				}
			}
			viewSwitcher.PlayDimmerToMenu(applyScoring);
		}
		public void RestartButton()
		{
			switch (F.I.gameMode)
			{
				case MultiMode.Singleplayer:
					StartRace();
					break;
				case MultiMode.Multiplayer:
					if(Voting.I != null)
						Voting.I.VoteForRestart();					
					break;
				default:
					break;
			}
		}
		public void VoteForEndButton()
		{
			if(Voting.I != null)
				Voting.I.VoteForEnd();
		}

		public void ExitButton()
		{
			if (resultsSeq.gameObject.activeSelf)
				return;

			if (F.I.gameMode == MultiMode.Multiplayer && MultiPlayerSelector.I.server.AmHost)
				Voting.I.VoteForEnd(); // host's decision is immediate

			musicPlayer.Stop();
			countDownSeq.gameObject.SetActive(false);

			if (F.I.s_inEditor)
				BackToEditor();
			else
				BackToMenu(applyScoring:false);
		}

		public float RaceProgress(VehicleParent vp)
		{
			return F.I.s_raceType switch
			{
				RaceType.Stunt => vp.raceBox.Aero,
				RaceType.Drift => vp.raceBox.drift,
				_ => vp.raceBox.curLap + vp.followAI.ProgressPercent,
			};
		}
		public int Position(VehicleParent vp)
		{
			if(F.I.s_cars.Count > 0)
			{
				F.I.s_cars.Sort((carA, carB) => RaceProgress(carB).CompareTo(RaceProgress(carA)));
				if (leader != F.I.s_cars[0])
				{
					if(leader != null)
						hud.infoText.AddMessage(new(F.I.s_cars[0].name + " TAKES THE LEAD!", BottomInfoType.NEW_LEADER));

					leader = F.I.s_cars[0];
				}
				for (int i = 0; i < F.I.s_cars.Count; ++i)
				{
					if (F.I.s_cars[i] == vp)
						return i + 1;
				}
			}
			return 1;
		}
		
		public void KnockOutLastCar()
		{
			F.I.s_cars.Sort((carA, carB) => RaceProgress(carB).CompareTo(RaceProgress(carA)));
			VehicleParent eliminatedCar = null;
			for (int i=F.I.s_cars.Count-1; i>=0; --i)
			{
				if (F.I.s_cars[i].raceBox.enabled)
				{
					eliminatedCar = F.I.s_cars[i];
					break;
				}
			}
			Debug.Assert(eliminatedCar != null);

			eliminatedCar.KnockoutMeRpc();

			hud.infoText.AddMessage(new(eliminatedCar.transform.name + " IS ELIMINATED!", BottomInfoType.ELIMINATED));
		}
		//Color HDRColor(float r, float g, float b, int intensity = 0)
		//{
		//	float factor = Mathf.Pow(2, intensity);
		//	return new Color(r * factor, g * factor, b * factor);
		//}
		public void SetPartOfDay()
		{
			SetPartOfDay(F.I.s_isNight ? PartOfDay.Night : PartOfDay.Day);
		}
		void SetPartOfDay(PartOfDay pod)
		{
			if (pod == PartOfDay.Day)
			{
				Sun.SetActive(true);
				RenderSettings.ambientLight = new Color32(208, 208, 208, 1);
			}
			else if (pod == PartOfDay.Night)
			{
				Sun.SetActive(false);
				RenderSettings.ambientLight = new Color32(52, 52, 52, 1);
			}
		}
		void Awake()
		{
			I = this;
			worldUpDir = Physics.gravity.sqrMagnitude == 0 ? Vector3.up : -Physics.gravity.normalized;
			wheelCastMaskStatic = wheelCastMask;
			groundMaskStatic = groundMask;
			damageMaskStatic = damageMask;
			ignoreWheelCastLayer = LayerMask.NameToLayer("Ignore Wheel Cast");
			frictionlessMatStatic = frictionlessMat;
			tireMarkLengthStatic = Mathf.Max(tireMarkLength, 2);
			tireMarkGapStatic = tireMarkGap;
			tireMarkHeightStatic = tireMarkHeight;
			tireFadeTimeStatic = tireFadeTime;

			musicPlayer = GetComponent<AudioSource>();
		}
		private void OnDisable()
		{
			playerCar = null;
			DemoSGPLogo.SetActive(false);
		}
		private void OnEnable()
		{
			F.I.raceStartDate = DateTime.MinValue;
			StartCoroutine(editorPanel.LoadTrack());
			
			SetPartOfDay();
			if (F.I.s_inEditor)
			{
				BackToEditor();
			}
			else
			{
				StartRace();
			}
		}
		IEnumerator StartFreeroamCo(Vector3 position, Quaternion rotation)
		{
			position.y += 1;
			editorPanel.gameObject.SetActive(false);
			F.I.s_raceType = RaceType.Race;
			var carModel = Resources.Load<GameObject>(F.I.carPrefabsPath + F.I.s_playerCarName);
			var newCar = Instantiate(carModel, position, rotation).GetComponent<VehicleParent>();
			F.I.s_cars.Add(newCar);
			cam.Connect(newCar);
			hud.Connect(newCar);
			newCar.followAI.enabled = true;
			newCar.raceBox.enabled = false;
			yield return null;
			F.I.cars[newCar.carNumber - 1].config.Apply(newCar);
		}
		public void StartFreeRoam(Vector3 position, Quaternion rotation)
		{
			StartCoroutine(StartFreeroamCo(position, rotation));
		}
		void SetPitsLayer(int layer)
		{
			Transform t = editorPanel.placedTilesContainer.transform;
			int tilesCount = t.childCount;
			for (int i=0; i< tilesCount; ++i)
			{
				if (t.GetChild(i).name == "pits")
					t.GetChild(i).GetChild(0).gameObject.layer = layer;
			}
		}
		public void StartRace()
		{
			StartCoroutine(StartRaceCoroutine());
		}
		IEnumerator StartRaceCoroutine()
		{
			for(int i=0; i<F.I.s_cars.Count; ++i)
				if (F.I.s_cars[i] != null)
					Destroy(F.I.s_cars[i].gameObject);

			F.I.s_cars.Clear();

			musicPlayer.Stop();
			int startlines = 0;
			while (editorPanel.loadingTrack)
			{
				yield return null;
			}

			editorPanel.pathFollower.SetActive(false);
			for (int i = 0; i < editorPanel.placedTilesContainer.transform.childCount; ++i)
			{
				if (editorPanel.placedTilesContainer.transform.GetChild(i).name == "startline")
					startlines++;
			}
			if (startlines != 1)
			{
				editorPanel.DisplayMessageFor("Exactly 1 startline needed", 3);
				yield break;
			}
			editorPanel.gameObject.SetActive(false);

			if (F.I.tracks[F.I.s_trackName].envir == Envir.SPN && F.I.s_isNight)
			{
				musicPlayer.clip = Resources.Load<AudioClip>("music/SPN2");
				musicPlayer.Play();
			}
			else
			{
				musicPlayer.clip = Resources.Load<AudioClip>("music/" + F.I.tracks[F.I.s_trackName].envir.ToString());
				musicPlayer.PlayDelayed(5);
			}
			F.I.raceStartDate = DateTime.Now;
			F.I.raceStartDate.AddSeconds(5);
			
			int initialRandomLivery = UnityEngine.Random.Range(0, F.I.Liveries);

			SetPitsLayer(0);

			List<int> preferredCars = new();
			for(int i=0; i< F.I.cars.Length; ++i)
			{
				if(F.I.tracks[F.I.s_trackName].preferredCarClass == CarGroup.Wild 
					|| F.I.tracks[F.I.s_trackName].preferredCarClass == CarGroup.Team)
				{
					if (F.I.cars[i].category == CarGroup.Wild || F.I.cars[i].category == CarGroup.Team)
						preferredCars.Add(i + 1);
				}
				else
				{
					if (F.I.cars[i].category == CarGroup.Aero || F.I.cars[i].category == CarGroup.Speed)
						preferredCars.Add(i + 1);
				}
			}
			
			CarPlacement[] carPlacements = new CarPlacement[0];
			switch (F.I.gameMode)
			{
				case MultiMode.Singleplayer:
					carPlacements = new CarPlacement[F.I.s_cpuRivals + 1];
					for (int i = 0; i < F.I.s_cpuRivals; ++i)
						carPlacements[i] = CarPlacement.CPU(i, preferredCars);
					carPlacements[^1] = F.I.s_spectator ? CarPlacement.CPU(F.I.s_cpuRivals, preferredCars) : CarPlacement.LocalPlayer();
					break;
				case MultiMode.Multiplayer: // only server has the authority 
					var server = MultiPlayerSelector.I.server;
					if (server.AmHost)
					{
						Player[] sortedPlayers = new Player[server.lobby.Players.Count];
						for (int i = 0; i < sortedPlayers.Length; ++i)
							sortedPlayers[i] = server.lobby.Players[i];

						Array.Sort(sortedPlayers, (Player a, Player b) => { return a.ScoreGet().CompareTo(b.ScoreGet()); });

						carPlacements = new CarPlacement[F.I.s_cpuRivals + F.I.ActivePlayers.Count];
						for (int i = 0; i < carPlacements.Length; ++i)
						{
							int pIndex = i - F.I.s_cpuRivals; // CPU cars are always starting first, then online players
							carPlacements[i] = pIndex < 0 ? CarPlacement.CPU(i, preferredCars) : CarPlacement.OnlinePlayer(i, sortedPlayers[pIndex]);
						}
					}
					break;
				default:
					break;
			}
			
			foreach (var cp in carPlacements)
			{
				int dist = -20 - 10 * cp.position;
				Vector3 startPos = racingPaths[0].path.GetPointAtDistance(dist);
				Vector3 dirVec = racingPaths[0].path.GetDirectionAtDistance(dist);
				Vector3 rotDirVec = Quaternion.AngleAxis(90, Vector3.up) * dirVec;
				Vector3 leftSide = startPos;
				Vector3 rightSide = startPos;

				for (int j = 0; j < 28; j += 2)//track width is around 30
				{
					if (Physics.Raycast(startPos + 5 * Vector3.up + rotDirVec * j, Vector3.down, out var hit, 10, 1 << F.I.roadLayer))
					{
						rightSide = hit.point;
					}
					else
						break;
				}
				for (int j = 2; j < 28; j += 2)
				{
					if (Physics.Raycast(startPos + 5 * Vector3.up - rotDirVec * j, Vector3.down, out var hit, 10, 1 << F.I.roadLayer))
					{
						leftSide = hit.point;
					}
					else
						break;
				}

				startPos = Vector3.Lerp(leftSide, rightSide, (cp.position % 2 == 0) ? .286f : .714f);
				var carModel = Resources.Load<GameObject>(F.I.carPrefabsPath + cp.carName);
				var position = new Vector3(startPos.x, startPos.y + 3, startPos.z);
				var rotation = Quaternion.LookRotation(dirVec);
				VehicleParent newCar;
				if (F.I.gameMode == MultiMode.Multiplayer)
					newCar = NetworkObject.InstantiateAndSpawn(carModel, networkManager, cp.PlayerId, position: position, rotation: rotation).GetComponent<VehicleParent>();
				else
					newCar = Instantiate(carModel, position, rotation).GetComponent<VehicleParent>();

				newCar.sponsor = cp.sponsor;
				newCar.name = cp.name;
			} // ---carPlacements

			SetPitsLayer(F.I.roadLayer);

			countDownSeq.CountdownSeconds = F.I.countdownSeconds;
			countDownSeq.gameObject.SetActive(!F.I.s_spectator);
			
			if (F.I.s_spectator)
				StartCoroutine(SpectatorLoop());
		}
		public void SpawnCarsForLateClients(List<LobbyPlayerJoined> newPlayers)
		{
			foreach(var lpj in newPlayers)
			{
				var p = lpj.Player;
				var carModel = Resources.Load<GameObject>(F.I.carPrefabsPath + p.carNameGet());
				var position = F.I.s_cars[^1].tr.position + Vector3.up * 3;
				var rotation = F.I.s_cars[^1].tr.rotation;
				ulong Id = F.I.ActivePlayers.Find(ap => ap.playerLobbyId == p.Id).playerRelayId;
				var newCar = NetworkObject.InstantiateAndSpawn(carModel, networkManager, Id, position: position, rotation: rotation).GetComponent<VehicleParent>();
				newCar.sponsor = p.SponsorGet();
				newCar.name = p.NameGet();

			}
		}

		IEnumerator SpectatorLoop()
		{
			while (true)
			{
				if (F.I.s_spectator)
				{
					if ((DateTime.Now - F.I.raceStartDate).TotalSeconds > 60 
						|| (submitInput.action.ReadValue<float>()==1) || (cancelInput.action.ReadValue<float>()== 1))
					{
						BackToMenu(applyScoring: false);
						yield break;
					}
				}
				yield return null;
			}
		}

		public void PlayFinishSeq()
		{
			StartCoroutine(FinishSeq());
		}
		IEnumerator FinishSeq()
		{
			if (!firstDriverFinished && Voting.I != null)
				Voting.I.CountdownTillForceEveryoneToResults();

			firstDriverFinished = true;

			musicPlayer.Stop();
			resultsSeq.gameObject.SetActive(true);
			cam.mode = CameraControl.Mode.Replay;

			if (playerCar != null)
			{
				if (F.I.s_inEditor)
				{ // lap, race, stunt, drift
					if (editorPanel.records == null)
						editorPanel.records = new();
					//lap
					if ((float)playerCar.raceBox.bestLapTime.TotalSeconds < editorPanel.records.lap.secondsOrPts)
					{
						F.I.tracks[F.I.s_trackName].records.lap.playerName = F.I.playerData.playerName;
						F.I.tracks[F.I.s_trackName].records.lap.requiredSecondsOrPts = (float)playerCar.raceBox.bestLapTime.TotalSeconds;
					}
					//race
					if ((float)playerCar.raceBox.raceTime.TotalSeconds > editorPanel.records.race.secondsOrPts)
					{
						F.I.tracks[F.I.s_trackName].records.race.playerName = F.I.playerData.playerName;
						F.I.tracks[F.I.s_trackName].records.race.requiredSecondsOrPts = (float)playerCar.raceBox.raceTime.TotalSeconds;
					}
					//stunt
					if (playerCar.raceBox.Aero > editorPanel.records.stunt.secondsOrPts)
					{
						F.I.tracks[F.I.s_trackName].records.stunt.playerName = F.I.playerData.playerName;
						F.I.tracks[F.I.s_trackName].records.stunt.requiredSecondsOrPts = playerCar.raceBox.Aero;
					}
					//drift
					if (playerCar.raceBox.drift > editorPanel.records.drift.secondsOrPts)
					{
						F.I.tracks[F.I.s_trackName].records.drift.playerName = F.I.playerData.playerName;
						F.I.tracks[F.I.s_trackName].records.drift.requiredSecondsOrPts = playerCar.raceBox.drift;
					}
				}
				else
				{
					//lap
					if ((float)playerCar.raceBox.bestLapTime.TotalSeconds < F.I.tracks[F.I.s_trackName].records.lap.secondsOrPts)
					{
						F.I.tracks[F.I.s_trackName].records.lap.playerName = F.I.playerData.playerName;
						F.I.tracks[F.I.s_trackName].records.lap.secondsOrPts = (float)playerCar.raceBox.bestLapTime.TotalSeconds;
					}
					//race
					if ((float)playerCar.raceBox.raceTime.TotalSeconds > F.I.tracks[F.I.s_trackName].records.race.secondsOrPts)
					{
						F.I.tracks[F.I.s_trackName].records.race.playerName = F.I.playerData.playerName;
						F.I.tracks[F.I.s_trackName].records.race.secondsOrPts = (float)playerCar.raceBox.raceTime.TotalSeconds;
					}
					//stunt
					if (playerCar.raceBox.Aero > F.I.tracks[F.I.s_trackName].records.stunt.secondsOrPts)
					{
						F.I.tracks[F.I.s_trackName].records.stunt.secondsOrPts = playerCar.raceBox.Aero;
						F.I.tracks[F.I.s_trackName].records.stunt.playerName = F.I.playerData.playerName;
					}
					//drift
					if (playerCar.raceBox.drift > F.I.tracks[F.I.s_trackName].records.drift.secondsOrPts)
					{
						F.I.tracks[F.I.s_trackName].records.drift.playerName = F.I.playerData.playerName;
						F.I.tracks[F.I.s_trackName].records.drift.secondsOrPts = playerCar.raceBox.drift;
					}
					// immediately set track header
					if (F.I.tracks.ContainsKey(F.I.s_trackName))
					{
						var json = JsonConvert.SerializeObject(F.I.tracks[F.I.s_trackName]);
						var path = Path.Combine(F.I.tracksPath, F.I.s_trackName + ".track");
						File.WriteAllTextAsync(path, json);

						json = JsonConvert.SerializeObject(F.I.tracks[F.I.s_trackName].records);
						path = Path.Combine(F.I.tracksPath, F.I.s_trackName + ".rec");
						File.WriteAllTextAsync(path, json);
					}
				}
			}

			while (resultsSeq.gameObject.activeSelf)
			{
				yield return null;
			}

			countDownSeq.gameObject.SetActive(false);
			if (F.I.s_inEditor)
			{
				Debug.Log("back to editor");
				BackToEditor();
			}
			else
			{
				Debug.Log("back to menu");
				BackToMenu(applyScoring: true);
			}
		}
		public void TimeForRaceEnded()
		{
			if(playerCar != null)
				playerCar.raceBox.enabled = false;
		}
	}
}