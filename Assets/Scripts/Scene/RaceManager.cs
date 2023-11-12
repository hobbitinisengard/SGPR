using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using PathCreation;
using System.Collections;
using Newtonsoft.Json;
using System.IO;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace RVP
{
	public enum PartOfDay { Day, Night };
	[DisallowMultipleComponent]

	// Global controller class
	public class RaceManager : MonoBehaviour
	{
		AudioSource musicPlayer;
		public PathCreator racingPath;
		public GameObject Sun;
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
		[NonSerialized]
		bool initialized = false;
		public float trackDistance = 1000;
		public SGP_HUD hud;
		public EditorPanel editorPanel;
		public CameraControl cam;
		public CountDownSeq countDownSeq;
		public ResultsSeq resultsSeq;
		public PauseMenuButton sfxButton;
		public PauseMenuButton musicButton;

		public enum InfoMessage
		{
			TAKES_THE_LEAD, NO_ENERGY, SPLIT_TIME,
		}


		public void BackToEditor()
		{
			hud.Disconnect();
			hud.gameObject.SetActive(false);
			cam.Disconnect();
			cam.enabled = false;
			foreach (var c in Info.s_cars)
				Destroy(c.gameObject);
			Info.s_cars.Clear();
			editorPanel.gameObject.SetActive(true);
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
		public void BackToMenu()
		{
		}
		public float RaceProgress(VehicleParent vp)
		{
			return vp.raceBox.curLap + vp.followAI.ProgressPercent();
		}
		public int Position(VehicleParent vp)
		{
			Info.s_cars.Sort((carA, carB) => RaceProgress(carB).CompareTo(RaceProgress(carA)));
			for (int i = 0; i < Info.s_cars.Count; ++i)
			{
				if (Info.s_cars[i] == vp)
					return i + 1;
			}
			return 1;
		}
		//Color HDRColor(float r, float g, float b, int intensity = 0)
		//{
		//	float factor = Mathf.Pow(2, intensity);
		//	return new Color(r * factor, g * factor, b * factor);
		//}
		void SetPartOfDay()
		{
			SetPartOfDay(Info.s_isNight ? PartOfDay.Night : PartOfDay.Day);
		}
		void SetPartOfDay(PartOfDay pod)
		{
			if (pod == PartOfDay.Day)
			{
				//Sun.SetActive(true);
				RenderSettings.ambientLight = new Color32(208, 208, 208, 1);
			}
			else if (pod == PartOfDay.Night)
			{
				//Sun.SetActive(false);
				RenderSettings.ambientLight = new Color32(52, 52, 52, 1);
			}
		}
		public bool Initialized()
		{
			return initialized;
		}
		public void Initialize(VehicleParent[] cars)
		{
			Info.s_cars.AddRange(cars);
			initialized = true;
		}
		void Start()
		{

			// Read PlayerSettings (sfx, music)
			string path = Path.Combine(Application.streamingAssetsPath, "playerSettings.json");
			PlayerSettingsData settingsData;
			if (!File.Exists(path))
			{
				settingsData = new PlayerSettingsData();
				string serializedSettings = JsonConvert.SerializeObject(settingsData);
				File.WriteAllText(path, serializedSettings);
			}
			else
			{
				string playerSettings = File.ReadAllText(path);
				settingsData = JsonConvert.DeserializeObject<PlayerSettingsData>(playerSettings);
			}
			sfxButton.soundLevel = settingsData.sfxVol;
			musicButton.soundLevel = settingsData.musicVol;


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
			Info.PopulateCarsData();
			Info.PopulateTrackData();
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
		public void StartFreeRoam(Vector3 position, Quaternion rotation)
		{
			position.y += 1;
			editorPanel.gameObject.SetActive(false);
			var carModel = Resources.Load<GameObject>(Info.carModelsPath + "car01");
			var newCar = Instantiate(carModel, position, rotation).GetComponent<VehicleParent>();
			Info.s_cars.Add(newCar);
			cam.Connect(newCar);
			hud.Connect(newCar);
			// do not assign path in freeroam as track might not be finished
			//newCar.followAI.AssignPath(racingPath, ref editorPanel.stuntpointsContainer, ref editorPanel.replayCamsContainer);
		}
		public void StartRace()
		{
			musicPlayer.Stop();
			int startlines = 0;
			for (int i = 0; i < editorPanel.placedTilesContainer.transform.childCount; ++i)
			{
				if (editorPanel.placedTilesContainer.transform.GetChild(i).name == "startline")
					startlines++;
			}
			if (startlines != 1)
				return;

			editorPanel.gameObject.SetActive(false);
			Debug.Log("Play: " + Info.tracks[Info.s_trackName].envir.ToString());
			musicPlayer.clip = Resources.Load<AudioClip>("music/" + Info.tracks[Info.s_trackName].envir.ToString());
			musicPlayer.PlayDelayed(5);
			Info.raceStartDate = DateTime.Now;
			Info.raceStartDate.AddSeconds(5);
			float countDownSeconds = 5;
			Info.s_cars.Clear();
			int dist = -7;
			for (int i = 0; i < Info.s_rivals + 1; ++i)
			{
				Vector3 startPos = racingPath.path.GetPointAtDistance(dist);
				Vector3 dirVec = racingPath.path.GetDirectionAtDistance(dist);
				Vector3 rotDirVec = Quaternion.AngleAxis(90, Vector3.up) * dirVec;
				Vector3 leftSide = startPos;
				Vector3 rightSide = startPos;
				for (int j = 2; j < 28; j += 2)//track width is around 30
				{
					if (Physics.Raycast(startPos + 5 * Vector3.up + rotDirVec * j, Vector3.down, out var hit, Mathf.Infinity, 1 << Info.roadLayer))
					{
						rightSide = hit.point;
					}
					else
						break;
				}
				for (int j = 2; j < 28; j += 2)
				{
					if (Physics.Raycast(startPos + 5 * Vector3.up - rotDirVec * j, Vector3.down, out var hit, Mathf.Infinity, 1 << Info.roadLayer))
					{
						leftSide = hit.point;
					}
					else
						break;
				}

				startPos = Vector3.Lerp(leftSide, rightSide, (i % 2 == 0) ? 0.286f : .714f);
				//Debug.DrawRay(startPos, Vector3.up);
				var carModel = Resources.Load<GameObject>(Info.carModelsPath + "car01");
				var position = new Vector3(startPos.x, startPos.y + 3, startPos.z);
				var rotation = Quaternion.LookRotation(dirVec);
				var newCar = Instantiate(carModel, position, rotation).GetComponent<VehicleParent>();
				newCar.SetSponsor(i % Info.Liveries);
				if (Info.s_isNight)
					newCar.SetLights();
				StartCoroutine(newCar.CountdownTimer(countDownSeconds - newCar.engine.transmission.shiftDelaySeconds));
				Info.s_cars.Add(newCar);
				if (i == Info.s_rivals)
				{ // last car is the player
					newCar.name = Info.s_playerName;
					cam.enabled = true;
					cam.Connect(newCar);
					hud.Connect(newCar);
					newCar.AddWheelGroup();
					//newCar.followAI.SetCPU(true);
				}
				else
				{
					newCar.name = "CP" + (i + 1).ToString();
					newCar.AddWheelGroup();
					newCar.followAI.SetCPU(true);
				}
				newCar.followAI.AssignPath(racingPath, ref editorPanel.stuntpointsContainer, ref editorPanel.replayCamsContainer);
				dist -= 10;
			}
			Debug.DebugBreak();
			countDownSeq.CountdownSeconds = countDownSeconds;
			countDownSeq.gameObject.SetActive(true);
		}
		void Update()
		{
			// DEBUG
			if (Input.GetKeyDown(KeyCode.K))
				hud.AddMessage(new Message("CP1 IS RECHARGING!" + 5 * UnityEngine.Random.value, BottomInfoType.PIT_IN));
			if (Input.GetKeyDown(KeyCode.M))
				hud.AddMessage(new Message("CP2 TAKES THE LEAD!" + 5 * UnityEngine.Random.value, BottomInfoType.NEW_LEADER));
			// Quickly restart scene with a button press
			//if (quickRestart)
			//{
			//	if (Input.GetButtonDown("Restart"))
			//	{
			//		SceneManager.LoadScene(SceneManager.GetActiveScene().name);
			//		Time.timeScale = 1;
			//		Time.fixedDeltaTime = initialFixedTime;
			//	}
			//}
		}

		public void PlayFinishSeq()
		{
			StartCoroutine(FinishSeq());
		}
		IEnumerator FinishSeq()
		{
			musicPlayer.Stop();
			resultsSeq.gameObject.SetActive(true);
			cam.SetMode(CameraControl.Mode.Replay);

			while (resultsSeq.gameObject.activeSelf)
			{
				yield return null;
			}
			if(Info.tracks.ContainsKey(Info.s_trackName))
			{
				//lap
				if((float)hud.vp.raceBox.bestLapTime.TotalSeconds < Info.tracks[Info.s_trackName].records[0].secondsOrPts)
					Info.tracks[Info.s_trackName].records[0].secondsOrPts = (float)hud.vp.raceBox.bestLapTime.TotalSeconds;
				//race
				if ((float)hud.vp.raceBox.raceTime.TotalSeconds > Info.tracks[Info.s_trackName].records[1].secondsOrPts)
					Info.tracks[Info.s_trackName].records[1].secondsOrPts = (float)hud.vp.raceBox.raceTime.TotalSeconds;
				//stunt
				if ((float)hud.vp.raceBox.aero/Info.s_laps > Info.tracks[Info.s_trackName].records[2].secondsOrPts)
					Info.tracks[Info.s_trackName].records[2].secondsOrPts = (float)hud.vp.raceBox.aero / Info.s_laps;
				//drift
				if (hud.vp.raceBox.driftPts > Info.tracks[Info.s_trackName].records[3].secondsOrPts)
					Info.tracks[Info.s_trackName].records[3].secondsOrPts = hud.vp.raceBox.driftPts;
			}
			countDownSeq.gameObject.SetActive(false);
			if (Info.s_inEditor)
			{
				BackToEditor();
			}
			else
			{
				BackToMenu();
			}
		}
	}
}