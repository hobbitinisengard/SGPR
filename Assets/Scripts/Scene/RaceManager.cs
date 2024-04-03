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
using Kart;
using Unity.Netcode.Components;

namespace RVP
{
	public enum PartOfDay { Day, Night };
	[DisallowMultipleComponent]

	// Global controller class
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
		[NonSerialized]
		public Voting voting;
		public event Action ClosingRace;

		bool firstDriverFinished = false;
		public enum InfoMessage
		{
			TAKES_THE_LEAD, NO_ENERGY, SPLIT_TIME,
		}
		public int ActiveCarsInKnockout
		{
			get
			{
				int activeCars = 0;
				foreach (var car in Info.s_cars)
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
			if (Info.gameMode == MultiMode.Singleplayer || (Info.mpSelector && Info.mpSelector.server.AmHost))
				for (int i = 0; i < Info.s_cars.Count; ++i)
					Destroy(Info.s_cars[i].gameObject);

			Info.s_cars.Clear();

			editorPanel.gameObject.SetActive(true);
			Info.raceStartDate = DateTime.MinValue;
		}
		public void BackToMenu(bool applyScoring)
		{
			//foreach (var c in Info.s_cars)
			//	Destroy(c.gameObject);
			//Info.s_cars.Clear();

			if(applyScoring && Info.gameMode == MultiMode.Multiplayer)
			{
				var rd = new ResultsView.PersistentResult[Info.s_cars.Count];
				for(int i=0; i<Info.s_cars.Count; ++i)
				{
					rd[i] = new ResultsView.PersistentResult()
					{
						drift = Info.s_cars[i].raceBox.drift,
						lap = Info.s_cars[i].raceBox.bestLapTime,
						stunt = Info.s_cars[i].raceBox.Aero,
						name = Info.s_cars[i].transform.name,
					};
				}
				ResultsView.resultData = rd;
			}
			viewSwitcher.PlayDimmerToMenu(applyScoring);
		}
		public void RestartButton()
		{
			switch (Info.gameMode)
			{
				case MultiMode.Singleplayer:
					StartRace();
					break;
				case MultiMode.Multiplayer:
					if(voting != null)
						voting.VoteForRestart();					
					break;
				default:
					break;
			}
		}
		public void VoteForEndButton()
		{ // vote for end button is only visible in multiplayer game
			if(voting != null)
				voting.VoteForEnd();
		}

		public void ExitButton()
		{
			if (resultsSeq.gameObject.activeSelf)
				return;

			if (Info.gameMode == MultiMode.Multiplayer && Info.mpSelector.server.AmHost)
				voting.VoteForEnd(); // send signal to everyone that race ended

			musicPlayer.Stop();
			countDownSeq.gameObject.SetActive(false);

			if (Info.s_inEditor)
				BackToEditor();
			else
				BackToMenu(applyScoring:false);
		}

		public float RaceProgress(VehicleParent vp)
		{
			return Info.s_raceType switch
			{
				RaceType.Stunt => vp.raceBox.Aero,
				RaceType.Drift => vp.raceBox.drift,
				_ => vp.raceBox.curLap + vp.followAI.ProgressPercent,
			};
		}
		public int Position(VehicleParent vp)
		{
			if(Info.s_cars.Count > 0)
			{
				Info.s_cars.Sort((carA, carB) => RaceProgress(carB).CompareTo(RaceProgress(carA)));
				if (leader != Info.s_cars[0])
				{
					if(leader != null)
						hud.infoText.AddMessage(new(Info.s_cars[0].name + " TAKES THE LEAD!", BottomInfoType.NEW_LEADER));

					leader = Info.s_cars[0];
				}
				for (int i = 0; i < Info.s_cars.Count; ++i)
				{
					if (Info.s_cars[i] == vp)
						return i + 1;
				}
			}
			return 1;
		}
		
		public void KnockOutLastCar()
		{
			Info.s_cars.Sort((carA, carB) => RaceProgress(carB).CompareTo(RaceProgress(carA)));
			VehicleParent eliminatedCar = null;
			for (int i=Info.s_cars.Count-1; i>=0; --i)
			{
				if (Info.s_cars[i].raceBox.enabled)
				{
					eliminatedCar = Info.s_cars[i];
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
			SetPartOfDay(Info.s_isNight ? PartOfDay.Night : PartOfDay.Day);
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
			// Set static variables
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

			Info.PopulateSFXData();
			Info.ReloadCarsData();
			Info.PopulateTrackData();
		}
		private void OnDisable()
		{
			playerCar = null;
			DemoSGPLogo.SetActive(false);
		}
		private void OnEnable()
		{
			Info.raceStartDate = DateTime.MinValue;
			StartCoroutine(editorPanel.LoadTrack());
			
			SetPartOfDay();
			if (Info.s_inEditor)
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
			Info.s_raceType = RaceType.Race;
			var carModel = Resources.Load<GameObject>(Info.carPrefabsPath + Info.s_playerCarName);
			var newCar = Instantiate(carModel, position, rotation).GetComponent<VehicleParent>();
			Info.s_cars.Add(newCar);
			cam.Connect(newCar);
			hud.Connect(newCar);
			newCar.followAI.enabled = true;
			newCar.raceBox.enabled = false;
			yield return null;
			Info.cars[newCar.carNumber - 1].config.Apply(newCar);
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
			for(int i=0; i<Info.s_cars.Count; ++i)
				if (Info.s_cars[i] != null)
					Destroy(Info.s_cars[i].gameObject);

			Info.s_cars.Clear();

			musicPlayer.Stop();
			int startlines = 0;
			while (editorPanel.loadingTrack)
			{
				yield return null;
			}
			yield return null;
			Debug.Log("editor loaded Track. StartRace()");
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

			if (Info.tracks[Info.s_trackName].envir == Envir.SPN && Info.s_isNight)
			{
				musicPlayer.clip = Resources.Load<AudioClip>("music/SPN2");
				musicPlayer.Play();
			}
			else
			{
				musicPlayer.clip = Resources.Load<AudioClip>("music/" + Info.tracks[Info.s_trackName].envir.ToString());
				musicPlayer.PlayDelayed(5);
			}
			Info.raceStartDate = DateTime.Now;
			Info.raceStartDate.AddSeconds(5);
			
			int initialRandomLivery = UnityEngine.Random.Range(0, Info.Liveries);

			SetPitsLayer(0);

			List<int> preferredCars = new();
			for(int i=0; i< Info.cars.Length; ++i)
			{
				if(Info.tracks[Info.s_trackName].preferredCarClass == CarGroup.Wild 
					|| Info.tracks[Info.s_trackName].preferredCarClass == CarGroup.Team)
				{
					if (Info.cars[i].category == CarGroup.Wild || Info.cars[i].category == CarGroup.Team)
						preferredCars.Add(i + 1);
				}
				else
				{
					if (Info.cars[i].category == CarGroup.Aero || Info.cars[i].category == CarGroup.Speed)
						preferredCars.Add(i + 1);
				}
			}
			
			CarPlacement[] carPlacements = null;
			switch (Info.gameMode)
			{
				case MultiMode.Singleplayer:
					carPlacements = new CarPlacement[Info.s_cpuRivals + 1];
					for (int i = 0; i < Info.s_cpuRivals; ++i)
						carPlacements[i] = CarPlacement.CPU(i, preferredCars);
					carPlacements[^1] = Info.s_spectator ? CarPlacement.CPU(Info.s_cpuRivals, preferredCars) : CarPlacement.LocalPlayer();
					break;
				case MultiMode.Multiplayer: // only server has the authority 
					var server = Info.mpSelector.server;
					if (server.AmHost)
					{
						Player[] sortedPlayers = new Player[server.lobby.Players.Count];
						for (int i = 0; i < sortedPlayers.Length; ++i)
							sortedPlayers[i] = server.lobby.Players[i];

						Array.Sort(sortedPlayers, (Player a, Player b) => { return a.ScoreGet().CompareTo(b.ScoreGet()); });

						carPlacements = new CarPlacement[Info.s_cpuRivals + Info.ActivePlayers.Count];
						for (int i = 0; i < carPlacements.Length; ++i)
						{
							int pIndex = i - Info.s_cpuRivals; // CPU cars are always starting first, then online players
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
					if (Physics.Raycast(startPos + 5 * Vector3.up + rotDirVec * j, Vector3.down, out var hit, 10, 1 << Info.roadLayer))
					{
						rightSide = hit.point;
					}
					else
						break;
				}
				for (int j = 2; j < 28; j += 2)
				{
					if (Physics.Raycast(startPos + 5 * Vector3.up - rotDirVec * j, Vector3.down, out var hit, 10, 1 << Info.roadLayer))
					{
						leftSide = hit.point;
					}
					else
						break;
				}

				startPos = Vector3.Lerp(leftSide, rightSide, (cp.position % 2 == 0) ? .286f : .714f);
				//Debug.DrawRay(startPos, Vector3.up);
				//string carName = "car06";
				var carModel = Resources.Load<GameObject>(Info.carPrefabsPath + cp.carName);
				var position = new Vector3(startPos.x, startPos.y + 3, startPos.z);
				var rotation = Quaternion.LookRotation(dirVec);
				VehicleParent newCar;
				if (Info.gameMode == MultiMode.Multiplayer)
					newCar = NetworkObject.InstantiateAndSpawn(carModel, networkManager, cp.PlayerId, position: position, rotation: rotation).GetComponent<VehicleParent>();
				else
					newCar = Instantiate(carModel, position, rotation).GetComponent<VehicleParent>();

				newCar.sponsor = cp.sponsor;
				newCar.name = cp.name;
			} // ---carPlacements

			SetPitsLayer(Info.roadLayer);

			countDownSeq.CountdownSeconds = Info.countdownSeconds;
			countDownSeq.gameObject.SetActive(!Info.s_spectator);
			
			if (Info.s_spectator)
				StartCoroutine(SpectatorLoop());
		}
		
		IEnumerator SpectatorLoop()
		{
			while (true)
			{
				if (Info.s_spectator)
				{
					if ((DateTime.Now - Info.raceStartDate).TotalSeconds > 60 
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
			if (!firstDriverFinished && voting != null)
				voting.CountdownTillForceEveryoneToResults();

			firstDriverFinished = true;

			musicPlayer.Stop();
			resultsSeq.gameObject.SetActive(true);
			cam.mode = CameraControl.Mode.Replay;

			if (playerCar != null)
			{
				if (Info.s_inEditor)
				{ // lap, race, stunt, drift
					if (editorPanel.records == null)
						editorPanel.records = new();
					//lap
					if ((float)playerCar.raceBox.bestLapTime.TotalSeconds < editorPanel.records.lap.secondsOrPts)
					{
						Info.tracks[Info.s_trackName].records.lap.playerName = Info.playerData.playerName;
						Info.tracks[Info.s_trackName].records.lap.requiredSecondsOrPts = (float)playerCar.raceBox.bestLapTime.TotalSeconds;
					}
					//race
					if ((float)playerCar.raceBox.raceTime.TotalSeconds > editorPanel.records.race.secondsOrPts)
					{
						Info.tracks[Info.s_trackName].records.race.playerName = Info.playerData.playerName;
						Info.tracks[Info.s_trackName].records.race.requiredSecondsOrPts = (float)playerCar.raceBox.raceTime.TotalSeconds;
					}
					//stunt
					if (playerCar.raceBox.Aero > editorPanel.records.stunt.secondsOrPts)
					{
						Info.tracks[Info.s_trackName].records.stunt.playerName = Info.playerData.playerName;
						Info.tracks[Info.s_trackName].records.stunt.requiredSecondsOrPts = playerCar.raceBox.Aero;
					}
					//drift
					if (playerCar.raceBox.drift > editorPanel.records.drift.secondsOrPts)
					{
						Info.tracks[Info.s_trackName].records.drift.playerName = Info.playerData.playerName;
						Info.tracks[Info.s_trackName].records.drift.requiredSecondsOrPts = playerCar.raceBox.drift;
					}
				}
				else
				{
					//lap
					if ((float)playerCar.raceBox.bestLapTime.TotalSeconds < Info.tracks[Info.s_trackName].records.lap.secondsOrPts)
					{
						Info.tracks[Info.s_trackName].records.lap.playerName = Info.playerData.playerName;
						Info.tracks[Info.s_trackName].records.lap.secondsOrPts = (float)playerCar.raceBox.bestLapTime.TotalSeconds;
					}
					//race
					if ((float)playerCar.raceBox.raceTime.TotalSeconds > Info.tracks[Info.s_trackName].records.race.secondsOrPts)
					{
						Info.tracks[Info.s_trackName].records.race.playerName = Info.playerData.playerName;
						Info.tracks[Info.s_trackName].records.race.secondsOrPts = (float)playerCar.raceBox.raceTime.TotalSeconds;
					}
					//stunt
					if (playerCar.raceBox.Aero > Info.tracks[Info.s_trackName].records.stunt.secondsOrPts)
					{
						Info.tracks[Info.s_trackName].records.stunt.secondsOrPts = playerCar.raceBox.Aero;
						Info.tracks[Info.s_trackName].records.stunt.playerName = Info.playerData.playerName;
					}
					//drift
					if (playerCar.raceBox.drift > Info.tracks[Info.s_trackName].records.drift.secondsOrPts)
					{
						Info.tracks[Info.s_trackName].records.drift.playerName = Info.playerData.playerName;
						Info.tracks[Info.s_trackName].records.drift.secondsOrPts = playerCar.raceBox.drift;
					}
					// immediately set track header
					if (Info.tracks.ContainsKey(Info.s_trackName))
					{
						var json = JsonConvert.SerializeObject(Info.tracks[Info.s_trackName]);
						var path = Path.Combine(Info.tracksPath, Info.s_trackName + ".track");
						File.WriteAllTextAsync(path, json);

						json = JsonConvert.SerializeObject(Info.tracks[Info.s_trackName].records);
						path = Path.Combine(Info.tracksPath, Info.s_trackName + ".rec");
						File.WriteAllTextAsync(path, json);
					}
				}
			}

			while (resultsSeq.gameObject.activeSelf)
			{
				yield return null;
			}

			countDownSeq.gameObject.SetActive(false);
			if (Info.s_inEditor)
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