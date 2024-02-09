using System;
using UnityEngine;
using PathCreation;
using System.Collections;
using Newtonsoft.Json;
using System.IO;

namespace RVP
{
	public delegate void Notify();
	public enum PartOfDay { Day, Night };
	[DisallowMultipleComponent]

	// Global controller class
	public class RaceManager : MonoBehaviour
	{
		AudioSource musicPlayer;
		public GameObject Sun;
		public ViewSwitcher viewSwitcher;
		public PathCreator[] racingPaths;
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
		public SGP_HUD hud;
		public EditorPanel editorPanel;
		public CameraControl cam;
		public CountDownSeq countDownSeq;
		public ResultsSeq resultsSeq;
		public PauseMenuButton sfxButton;
		public PauseMenuButton musicButton;
		public GameObject DemoSGPLogo;
		VehicleParent leader;
		VehicleParent playerCar;
		public event Notify ClosingRace;
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
			foreach (var c in Info.s_cars)
				Destroy(c.gameObject);
			Info.s_cars.Clear();
			editorPanel.gameObject.SetActive(true);
			Info.raceStartDate = DateTime.MinValue;
		}
		public void BackToMenu()
		{
			//foreach (var c in Info.s_cars)
			//	Destroy(c.gameObject);
			//Info.s_cars.Clear();
			viewSwitcher.PlayDimmerToMenu();
		}
		public void RestartButton()
		{
			StartRace();
		}
		public void ExitButton()
		{
			if (resultsSeq.gameObject.activeSelf)
				return;
			musicPlayer.Stop();
			countDownSeq.gameObject.SetActive(false);

			if (Info.s_inEditor)
				BackToEditor();
			else
				BackToMenu();
		}

		public float RaceProgress(VehicleParent vp)
		{
			return Info.s_raceType switch
			{
				Info.RaceType.Stunt => vp.raceBox.Aero,
				Info.RaceType.Drift => vp.raceBox.drift,
				_ => vp.raceBox.curLap + vp.followAI.ProgressPercent,
			};
		}
		public int Position(VehicleParent vp)
		{
			Info.s_cars.Sort((carA, carB) => RaceProgress(carB).CompareTo(RaceProgress(carA)));
			if (leader != Info.s_cars[0])
			{
				leader = Info.s_cars[0];
				hud.AddMessage(new(leader.name + " TAKES THE LEAD!", BottomInfoType.NEW_LEADER));
			}
			for (int i = 0; i < Info.s_cars.Count; ++i)
			{
				if (Info.s_cars[i] == vp)
					return i + 1;
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

			eliminatedCar.raceBox.enabled = false;
			eliminatedCar.followAI.SetCPU(false);
			eliminatedCar.followAI.selfDriving = false;
			eliminatedCar.followAI.GetComponent<BasicInput>().enabled = false;
			eliminatedCar.SetAccel(0);
			eliminatedCar.SetBrake(0);
			eliminatedCar.raceBox.SetRacetime();

			hud.AddMessage(new(eliminatedCar.transform.name + " IS ELIMINATED!", BottomInfoType.ELIMINATED));
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
			Info.s_raceType = Info.RaceType.Race;
			var carModel = Resources.Load<GameObject>(Info.carPrefabsPath + Info.s_playerCarName);
			var newCar = Instantiate(carModel, position, rotation).GetComponent<VehicleParent>();
			Info.s_cars.Add(newCar);
			cam.Connect(newCar);
			hud.Connect(newCar);
			newCar.followAI.enabled = true;
			newCar.raceBox.raceManager = this;
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
			foreach (var c in Info.s_cars)
				Destroy(c.gameObject);
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
			musicPlayer.clip = Resources.Load<AudioClip>("music/" + Info.tracks[Info.s_trackName].envir.ToString());
			musicPlayer.PlayDelayed(5);
			Info.raceStartDate = DateTime.Now;
			Info.raceStartDate.AddSeconds(5);
			float countDownSeconds = 5;
			int dist = -20;
			int initialRandomLivery = UnityEngine.Random.Range(0, Info.Liveries);

			SetPitsLayer(0);
			for (int i = 0; i < Info.s_rivals + 1; ++i)
			{
				Vector3 startPos = racingPaths[0].path.GetPointAtDistance(dist);
				Vector3 dirVec = racingPaths[0].path.GetDirectionAtDistance(dist);
				Vector3 rotDirVec = Quaternion.AngleAxis(90, Vector3.up) * dirVec;
				Vector3 leftSide = startPos;
				Vector3 rightSide = startPos;

				for (int j = 0; j < 28; j += 2)//track width is around 30
				{
					if (Physics.Raycast(startPos + 5 * Vector3.up + rotDirVec * j, Vector3.down, out var hit,
						10, 1 << Info.roadLayer))
					{
						rightSide = hit.point;
					}
					else
						break;
				}
				for (int j = 2; j < 28; j += 2)
				{
					if (Physics.Raycast(startPos + 5 * Vector3.up - rotDirVec * j, Vector3.down, out var hit,
						10, 1 << Info.roadLayer))
					{
						leftSide = hit.point;
					}
					else
						break;
				}

				startPos = Vector3.Lerp(leftSide, rightSide, (i % 2 == 0) ? .286f : .714f);
				//Debug.DrawRay(startPos, Vector3.up);
				string carName = (i == Info.s_rivals) ? Info.s_playerCarName : "car" + UnityEngine.Random.Range(1, Info.cars.Length + 1).ToString("D2");
				//string carName = "car06";
				var carModel = Resources.Load<GameObject>(Info.carPrefabsPath + carName);
				var position = new Vector3(startPos.x, startPos.y + 3, startPos.z);
				var rotation = Quaternion.LookRotation(dirVec);
				var newCar = Instantiate(carModel, position, rotation).GetComponent<VehicleParent>();
				newCar.SetSponsor((initialRandomLivery + i) % Info.Liveries);
				Info.s_cars.Add(newCar);
				if (Info.s_isNight)
					newCar.SetLights();

				StartCoroutine(newCar.CountdownTimer(countDownSeconds));

				
				int racingLineNumber = 0;// i % racingPaths.Length;
				newCar.followAI.AssignPath(racingPaths[racingLineNumber], racingPaths[0], ref editorPanel.stuntpointsContainer,
					ref editorPanel.replayCamsContainer, Info.racingLineLayers[racingLineNumber]);

				if (i == Info.s_rivals)
				{ // last car is the player
					cam.enabled = true;

					if (Info.s_spectator)
					{
						newCar.name = "CP" + (i + 1).ToString();

						cam.Connect(newCar, CameraControl.Mode.Replay);
						DemoSGPLogo.SetActive(true);
					}
					else
					{
						playerCar = newCar;
						newCar.name = Info.s_playerName;
						cam.Connect(newCar);
						hud.Connect(newCar);
						DemoSGPLogo.SetActive(false);
						//newCar.followAI.SetCPU(true); // CPU drives player's car
					}
				}
				else
				{
					newCar.name = "CP" + (i + 1).ToString();
					newCar.followAI.SetCPU(true);
				}
				newCar.raceBox.raceManager = this;
				dist -= 10;
			}
			SetPitsLayer(Info.roadLayer);
			leader = Info.s_cars[0];
			countDownSeq.CountdownSeconds = countDownSeconds;
			countDownSeq.gameObject.SetActive(!Info.s_spectator);
			hud.gameObject.SetActive(!Info.s_spectator);
			if (Info.s_spectator)
				StartCoroutine(SpectatorLoop());

			yield return null;

			foreach (var car in Info.s_cars)
			{
				Info.cars[car.carNumber - 1].config.Apply(car);
				if (car.name.Contains("CP"))
					car.followAI.SetCPU(true);
			}
		}
		
		IEnumerator SpectatorLoop()
		{
			while (true)
			{
				if (Info.s_spectator)
				{
					if ((DateTime.Now - Info.raceStartDate).TotalSeconds > 60 || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Escape))
					{
						BackToMenu();
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
			musicPlayer.Stop();
			resultsSeq.gameObject.SetActive(true);
			cam.mode = CameraControl.Mode.Replay;

			if (playerCar != null)
			{
				if (Info.s_inEditor)
				{ // lap, race, stunt, drift
					if (editorPanel.records == null)
						editorPanel.records = TrackHeader.Record.RecordTemplate();
					//lap
					if ((float)playerCar.raceBox.bestLapTime.TotalSeconds < editorPanel.records[0].secondsOrPts)
					{
						Info.tracks[Info.s_trackName].records[0].playerName = Info.s_playerName;
						Info.tracks[Info.s_trackName].records[0].requiredSecondsOrPts = (float)playerCar.raceBox.bestLapTime.TotalSeconds;
					}
					//race
					if ((float)playerCar.raceBox.raceTime.TotalSeconds > editorPanel.records[1].secondsOrPts)
					{
						Info.tracks[Info.s_trackName].records[1].playerName = Info.s_playerName;
						Info.tracks[Info.s_trackName].records[1].requiredSecondsOrPts = (float)playerCar.raceBox.raceTime.TotalSeconds;
					}
					//stunt
					if (playerCar.raceBox.Aero > editorPanel.records[2].secondsOrPts)
					{
						Info.tracks[Info.s_trackName].records[2].playerName = Info.s_playerName;
						Info.tracks[Info.s_trackName].records[2].requiredSecondsOrPts = playerCar.raceBox.Aero;
					}
					//drift
					if (playerCar.raceBox.drift > editorPanel.records[3].secondsOrPts)
					{
						Info.tracks[Info.s_trackName].records[3].playerName = Info.s_playerName;
						Info.tracks[Info.s_trackName].records[3].requiredSecondsOrPts = playerCar.raceBox.drift;
					}
				}
				else
				{
					//lap
					if ((float)playerCar.raceBox.bestLapTime.TotalSeconds < Info.tracks[Info.s_trackName].records[0].secondsOrPts)
					{
						Info.tracks[Info.s_trackName].records[0].playerName = Info.s_playerName;
						Info.tracks[Info.s_trackName].records[0].secondsOrPts = (float)playerCar.raceBox.bestLapTime.TotalSeconds;
					}
					//race
					if ((float)playerCar.raceBox.raceTime.TotalSeconds > Info.tracks[Info.s_trackName].records[1].secondsOrPts)
					{
						Info.tracks[Info.s_trackName].records[1].playerName = Info.s_playerName;
						Info.tracks[Info.s_trackName].records[1].secondsOrPts = (float)playerCar.raceBox.raceTime.TotalSeconds;
					}
					//stunt
					if (playerCar.raceBox.Aero > Info.tracks[Info.s_trackName].records[2].secondsOrPts)
					{
						Info.tracks[Info.s_trackName].records[2].secondsOrPts = playerCar.raceBox.Aero;
						Info.tracks[Info.s_trackName].records[2].playerName = Info.s_playerName;
					}
					//drift
					if (playerCar.raceBox.drift > Info.tracks[Info.s_trackName].records[3].secondsOrPts)
					{
						Info.tracks[Info.s_trackName].records[3].playerName = Info.s_playerName;
						Info.tracks[Info.s_trackName].records[3].secondsOrPts = playerCar.raceBox.drift;
					}
					// immediately set track header
					if (Info.tracks.ContainsKey(Info.s_trackName))
					{
						var trackJson = JsonConvert.SerializeObject(Info.tracks[Info.s_trackName]);
						var path = Path.Combine(Info.tracksPath, Info.s_trackName + ".track");
						File.WriteAllText(path, trackJson);
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
				BackToMenu();
			}
		}

		
	}
}