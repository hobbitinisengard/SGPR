using System;
using UnityEngine;
using PathCreation;
using System.Collections;
using Newtonsoft.Json;
using System.IO;
using System.Collections.Generic;
using Unity.Services.Lobbies.Models;
using Unity.Netcode;

namespace RVP
{
	public enum PartOfDay { Day, Night };
	[DisallowMultipleComponent]

	public class RaceManager : MonoBehaviour
	{ 
		AudioSource musicPlayer;
		public Light Sun;
		public ViewSwitcher viewSwitcher;
		public PathCreator[] racingPaths;
		public PathCreator universalPath;
		[Tooltip("Mask for what the wheels collide with")]
		public LayerMask wheelCastMask;

		//[Tooltip("Mask for what the wheels get surface info from")]
		//public LayerMask surfaceWheelCastMask;
		//public static LayerMask surfaceWheelCastMaskStatic;


		[Tooltip("Mask for objects which vehicles check against if they are rolled over")]
		public LayerMask groundMask;

		[Tooltip("Mask for objects that cause damage to vehicles")]
		public LayerMask damageMask;

		public static int ignoreWheelCastLayer;

		[Tooltip("Frictionless physic material")]
		public PhysicMaterial frictionlessMat;

		public static Vector3 worldUpDir; // Global up direction, opposite of normalized gravity direction

		[Tooltip("Maximum segments per tire mark")]
		public int tireMarkLength;

		[Tooltip("Gap between tire mark segments")]
		public float tireMarkGap;

		[Tooltip("Tire mark height above ground")]
		public float tireMarkHeight;

		[Tooltip("Lifetime of tire marks")]
		public float tireFadeTime;

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

		public static RaceManager I;
		public enum InfoMessage
		{
			TAKES_THE_LEAD, NO_ENERGY, SPLIT_TIME,
		}

		public void RemoveCars()
		{
			hud.Disconnect();
			hud.gameObject.SetActive(false);
			cam.Disconnect();
			
			if(ServerC.I.AmHost)
			{
				for (int i = 0; i < F.I.s_cars.Count;i++)
				{
					if (F.I.s_cars[i].Owner) // cars remove themselves from I.s_cars array on destroy
						Destroy(F.I.s_cars[i].gameObject);
				}
			}
			else
			{
				playerCar?.RelinquishRpc(playerCar.RpcTarget.Server);
			}
		}
		public void BackToMenu(bool applyScoring)
		{
			Time.timeScale = 0;
			viewSwitcher.PlayDimmerToMenu(applyScoring);
		}
		public void RestartButton()
		{ 
			switch (F.I.gameMode)
			{
				case MultiMode.Singleplayer:
					RemoveCars();
					StartRace();
					break;
				case MultiMode.Multiplayer:
					if (Voting.I != null)
						Voting.I.VoteForRestart();
					break;
				default:
					break;
			}
		}
		public void VoteForEndButton()
		{
			if (Voting.I != null && ResultsView.FinishedPlayers == 0)
				Voting.I.VoteForEnd();
		}

		public void ExitButton()
		{
			if (F.I.s_laps == 0) // when backing from editor
				F.I.s_laps = 3;

			if (resultsSeq.gameObject.activeSelf)
				return;

			if (F.I.gameMode == MultiMode.Multiplayer && ServerC.I.AmHost)
				if(ResultsView.FinishedPlayers == 0)
					Voting.I.EndForEveryone(); // host's decision is immediate
				else
					return;

			musicPlayer.Stop();
			countDownSeq.gameObject.SetActive(false);

			
			if (F.I.s_inEditor)
			{
				RemoveCars();
				editorPanel.gameObject.SetActive(true);
			}
			else
			{
				BackToMenu(applyScoring: false);
				if (F.I.gameMode == MultiMode.Multiplayer && !ServerC.I.AmHost)
					ServerC.I.DisconnectFromLobby();
				
			}
		}

		public float LiveProgress(VehicleParent vp)
		{
			return F.I.s_raceType switch
			{
				RaceType.Stunt => vp.raceBox.Aero,
				RaceType.Drift => vp.raceBox.Drift,
				RaceType.TimeTrial => -(float)vp.raceBox.bestLapTime.TotalMilliseconds, // trick to compare in a descending order
				_ => vp.raceBox.RaceProgressLaps,
			};
		}
		public int Position(VehicleParent vp)
		{
			if (F.I.s_cars.Count > 0)
			{
				F.I.s_cars.Sort((carA, carB) => LiveProgress(carB).CompareTo(LiveProgress(carA)));
				if (leader != F.I.s_cars[0])
				{
					if (leader == null)
					{
						leader = F.I.s_cars[0];
					}
					else
					{
						leader = F.I.s_cars[0];
						switch (F.I.s_raceType)
						{
							case RaceType.Race:
								hud.infoText.AddMessage(new(leader.name + " TAKES THE LEAD!", BottomInfoType.NEW_LEADER));
								break;
							case RaceType.Knockout:
								hud.infoText.AddMessage(new(leader.name + " TAKES THE LEAD!", BottomInfoType.NEW_LEADER));
								break;
							case RaceType.Stunt:
								hud.infoText.AddMessage(new(leader.name + " TAKES THE LEAD! AEROMILES:" + leader.raceBox.Aero, BottomInfoType.NEW_LEADER));
								break;
							case RaceType.Drift:
								hud.infoText.AddMessage(new(leader.name + " TAKES THE LEAD! DRIFTSCORE:" + leader.raceBox.Drift, BottomInfoType.NEW_LEADER));
								break;
							case RaceType.TimeTrial:
								hud.infoText.AddMessage(new(leader.name + " TAKES THE LEAD! LAP:" + leader.raceBox.bestLapTime.ToLaptimeStr(), BottomInfoType.NEW_LEADER));
								break;
							default:
								break;
						}
					}
				}
				for (int i = 0; i < F.I.s_cars.Count; ++i)
				{
					if (F.I.s_cars[i] == vp)
						return Mathf.Clamp(i, 0,9);
				}
			}
			return 0;
		}

		public void KnockoutCarsBehind(VehicleParent survivorCar)
		{
			F.I.s_cars.Sort((carA, carB) => LiveProgress(carB).CompareTo(LiveProgress(carA)));
			
			for (int i = F.I.s_cars.FindIndex(c => c == survivorCar)+1; i < F.I.s_cars.Count; ++i)
			{
				if (F.I.s_cars[i].raceBox.enabled)
				{
					var eliminatedCar = F.I.s_cars[i];
					eliminatedCar.KnockoutMe();
				}
			}
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
				Sun.intensity = 130000;
				RenderSettings.ambientLight = new Color32(208, 208, 208, 1);
			}
			else if (pod == PartOfDay.Night)
			{
				Sun.intensity = 1200;
				RenderSettings.ambientLight = new Color32(52, 52, 52, 1);
			}
		}
		void Awake()
		{
			I = this;
			worldUpDir = Physics.gravity.sqrMagnitude == 0 ? Vector3.up : -Physics.gravity.normalized;
			ignoreWheelCastLayer = LayerMask.NameToLayer("Ignore Wheel Cast");

			musicPlayer = GetComponent<AudioSource>();
		}
		private void OnDisable()
		{
			playerCar = null;
			F.I.raceStartDate = DateTime.MinValue;
			DemoSGPLogo.SetActive(false);
		}
		private void OnEnable()
		{
			StartCoroutine(editorPanel.LoadTrack());
			SetPartOfDay();

			F.I.chat.SetVisibility(false);

			if (F.I.s_inEditor)
			{
				RemoveCars();
			}
			else
			{
				StartRace();
			}
		}
		void SetPitsLayer(int layer)
		{
			Transform t = editorPanel.placedTilesContainer.transform;
			int tilesCount = t.childCount;
			for (int i = 0; i < tilesCount; ++i)
			{
				if (t.GetChild(i).name == "pits")
					t.GetChild(i).GetChild(0).gameObject.layer = layer;
			}
		}
		public void StartFreeRoam(Vector3 position, Quaternion rotation)
		{
			position.y += 1;
			editorPanel.gameObject.SetActive(false);
			F.I.s_raceType = RaceType.Race;
			F.I.s_laps = 0;
			var carModel = Resources.Load<GameObject>(F.I.carPrefabsPath + F.I.s_playerCarName);
			var newCar = Instantiate(carModel, position, rotation).GetComponent<VehicleParent>();
			newCar.followAI.enabled = false;
			newCar.raceBox.enabled = true;
			newCar.sponsor = F.RandomLivery();
			newCar.name = F.I.playerData.playerName;
		}
		public void StartRace()
		{
			ResultsView.Clear();
			F.I.raceStartDate = DateTime.UtcNow.AddSeconds(5);
			var euler = Sun.transform.rotation.eulerAngles;
			Sun.transform.rotation = Quaternion.Euler(euler.x, UnityEngine.Random.Range(-180, 180), euler.z);
			
			StartCoroutine(StartRaceCoroutine());
		}
		IEnumerator StartRaceCoroutine()
		{
			musicPlayer.Stop();
			int startlines = 0;
			while (editorPanel.loadingTrack)
			{
				yield return null;
			}

			hud.pauseMenu.gameObject.SetActive(false);
			editorPanel.pathFollower.SetActive(false);
			for (int i = 0; i < editorPanel.placedTilesContainer.transform.childCount; ++i)
			{
				if (editorPanel.placedTilesContainer.transform.GetChild(i).name.Contains("startline"))
					startlines++;
			}
			if (startlines != 1)
			{
				editorPanel.DisplayMessageFor("Exactly 1 startline needed", 3);
				yield break;
			}
			editorPanel.gameObject.SetActive(false);

			SetPitsLayer(0);

			if ((F.I.tracks[F.I.s_trackName].envir == Envir.SPN
						|| F.I.tracks[F.I.s_trackName].envir == Envir.ENG
						|| F.I.tracks[F.I.s_trackName].envir == Envir.FRA) && F.I.s_isNight)
			{
				musicPlayer.clip = Resources.Load<AudioClip>($"music/{F.I.tracks[F.I.s_trackName].envir}2");
				musicPlayer.Play();
			}
			else
			{
				musicPlayer.clip = Resources.Load<AudioClip>($"music/{F.I.tracks[F.I.s_trackName].envir}");
				musicPlayer.PlayDelayed(5);
			}

			List<int> preferredCars = new();
			for (int i = 0; i < F.I.cars.Length; ++i)
			{
				if (F.I.tracks[F.I.s_trackName].preferredCarClass == CarGroup.Wild
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

			CarPlacement[] carPlacements;
			if (ServerC.I.AmHost)
			{
				carPlacements = new CarPlacement[F.I.s_cpuRivals + 1];
				for (int i = 0; i < F.I.s_cpuRivals; ++i)
					carPlacements[i] = CarPlacement.CPU(i, preferredCars);

				if(F.I.s_spectator)
				{
					carPlacements[^1] = CarPlacement.CPU(F.I.s_cpuRivals, preferredCars);
				}
				else
				{
					if(F.I.gameMode == MultiMode.Singleplayer)
						carPlacements[^1] = CarPlacement.LocalPlayer();
					else
						carPlacements[^1] = CarPlacement.OnlinePlayer(ServerC.I.LeaderboardPos + F.I.s_cpuRivals, ServerC.I.PlayerMe);
				}
			}
			else
			{
				carPlacements = new CarPlacement[] { CarPlacement.OnlinePlayer(ServerC.I.LeaderboardPos + F.I.s_cpuRivals, ServerC.I.PlayerMe) };
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
				var position = new Vector3(startPos.x, startPos.y + 3, startPos.z);
				var rotation = Quaternion.LookRotation(dirVec);

				if (F.I.gameMode == MultiMode.Multiplayer && !ServerC.I.AmHost)
					Online.I.GibCar(position, rotation);
				else
				{
					VehicleParent newCar;
					var carModel = Resources.Load<GameObject>(F.I.carPrefabsPath + cp.carName);
					
					if (F.I.gameMode == MultiMode.Singleplayer)
						newCar = Instantiate(carModel, position, rotation).GetComponent<VehicleParent>();
					else
						newCar = NetworkObject.InstantiateAndSpawn(carModel, networkManager, networkManager.LocalClientId, position: position, rotation: rotation).GetComponent<VehicleParent>();
					newCar.sponsor = cp.sponsor;
					newCar.name = cp.name;
				}
			} // ---carPlacements
			SetPitsLayer(F.I.roadLayer);

			DemoSGPLogo.SetActive(F.I.s_spectator);
			hud.gameObject.SetActive(!F.I.s_spectator);
			countDownSeq.gameObject.SetActive(!F.I.s_spectator);

			if (F.I.s_spectator)
				StartCoroutine(SpectatorLoop());
		}
		public void SpawnCarForPlayer(ulong relayId, string lobbyId, Vector3? position, Quaternion? rotation)
		{
			int index = ServerC.I.lobby.Players.FindIndex(p => p.Id == lobbyId);
			Player p = ServerC.I.lobby.Players[index];
			var carModel = Resources.Load<GameObject>(F.I.carPrefabsPath + p.carNameGet());
			if(position == null)
				position = F.I.s_cars[^1].tr.position + Vector3.up * 3;
			if(rotation == null)
				rotation = F.I.s_cars[^1].tr.rotation;

			if (F.I.s_cars.Count == F.I.maxCarsInRace)
			{
				for (int i = F.I.s_cars.Count - 1; i >= 0; i--)
				{
					if (F.I.s_cars[i].followAI.IsCPU)
					{
						Debug.Log("Removed CPU car: " + F.I.s_cars[i].name);
						Destroy(F.I.s_cars[i].gameObject);
						break;
					}
				}
			}

			int curLap = F.I.s_cars[^1].raceBox.curLap - 1;
			if (curLap < 0)
				curLap = 0;
			var newCar = NetworkObject.InstantiateAndSpawn(carModel, networkManager, relayId, position:position.Value, rotation:rotation.Value).GetComponent<VehicleParent>();
			newCar.sponsor = p.SponsorGet();
			newCar.name = p.NameGet();
			newCar.SetCurLapRpc(curLap, newCar.RpcTarget.Owner);
		}

		IEnumerator SpectatorLoop()
		{
			while (F.I.s_spectator)
			{
				if ((DateTime.UtcNow - F.I.raceStartDate).TotalSeconds > 120
					|| (F.I.enterRef.action.ReadValue<float>() == 1) || (F.I.escRef.action.ReadValue<float>() == 1))
				{
					BackToMenu(applyScoring: false);
					yield break;
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
			if(ServerC.I.AmHost)
				Online.I.raceAlreadyStarted.Value = false;
			
			musicPlayer.Stop();
			if(F.I.gameMode == MultiMode.Multiplayer)
				yield return new WaitForSeconds(1);
			resultsSeq.gameObject.SetActive(true);
			cam.mode = CameraControl.Mode.Replay;
			foreach (var c in F.I.s_cars)
				c.sampleText.gameObject.SetActive(false);
			do
			{
				yield return null;
			} while (resultsSeq.gameObject.activeSelf);

			if (F.I.tracks.ContainsKey(F.I.s_trackName))
			{ 
				var json = JsonConvert.SerializeObject(F.I.tracks[F.I.s_trackName].records);
				var path = Path.Combine(F.I.tracksPath, F.I.s_trackName + ".rec");
				File.WriteAllTextAsync(path, json);
			}

			countDownSeq.gameObject.SetActive(false);
			if (F.I.s_inEditor)
			{
				Debug.Log("back to editor");
				editorPanel.gameObject.SetActive(true);
				RemoveCars();
			}
			else
			{
				Debug.Log("back to menu");
				BackToMenu(applyScoring: true);
			}
		}
		public void TimeForRaceEnded()
		{
			for(int i=0; i< F.I.s_cars.Count; ++i)
			{
				F.I.s_cars[i].raceBox.enabled = false;
			}
		}
	}
}