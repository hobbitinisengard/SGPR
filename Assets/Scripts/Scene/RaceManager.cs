using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;


namespace RVP
{
	public enum PartOfDay { Day, Night };
	[DisallowMultipleComponent]

	// Global controller class
	public class RaceManager : MonoBehaviour
	{
		AudioSource musicPlayer;
		
		public GameObject Sun;
		public GameObject nightTimeLights;
		[Tooltip("Reload the scene with the 'Restart' button in the input manager")]
		public bool quickRestart = true;
		float initialFixedTime;

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
		public List<VehicleParent> cars = new List<VehicleParent>();
		bool initialized = false;
		int position = 1;
		public float trackDistance = 1000;
		public SGP_HUD hud;
		public EditorPanel editorPanel;
		public CameraControl cam;
		public enum InfoMessage
		{
			TAKES_THE_LEAD, NO_ENERGY, SPLIT_TIME,
		}
		public bool Initialized()
		{
			return initialized;
		}
		public void Initialize(VehicleParent[] cars)
		{
			this.cars.AddRange(cars);
			initialized = true;
		}
		public void StartFreeRoam(Vector3 position, Quaternion rotation)
		{
			position.y += 1;
			editorPanel.gameObject.SetActive(false);
			var carModel = Resources.Load<GameObject>(Info.carModelsPath + "car01");
			var newCar = Instantiate(carModel, position, rotation).GetComponent<VehicleParent>();
			cars.Add(newCar);
			cam.enabled = true;
			cam.Connect(newCar);
			hud.gameObject.SetActive(true);
			hud.Connect(newCar);
		}
		public void BackToEditor()
		{
			hud.Disconnect();
			hud.gameObject.SetActive(false);
			cam.Disconnect();
			cam.enabled = false;
			foreach (var c in cars)
				Destroy(c.gameObject);
			cars.Clear();
			editorPanel.gameObject.SetActive(true);
		}
		public void RestartButton()
		{

		}
		public void ExitButton()
		{
			if (Info.s_inEditor)
				BackToEditor();
			else
				BackToMenu();
		}
		public void BackToMenu()
		{

		}
		public int Position(VehicleParent vp)
		{
			//// DEBUG
			//if (Input.GetKeyDown(KeyCode.K))
			//    position++;
			//if (Input.GetKeyDown(KeyCode.M))
			//    position--;
			position = Mathf.Clamp(position, 1, 10);
			return position;
		}
		//Color HDRColor(float r, float g, float b, int intensity = 0)
		//{
		//	float factor = Mathf.Pow(2, intensity);
		//	return new Color(r * factor, g * factor, b * factor);
		//}
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
				nightTimeLights.SetActive(true);
				RenderSettings.ambientLight = new Color32(52, 52, 52, 1);
			}
		}
		private void Awake()
		{
			musicPlayer = GetComponent<AudioSource>();
			musicPlayer.clip = Resources.Load<AudioClip>("music/JAP");

			Info.PopulateSFXData();
			Info.PopulateCarsData();
			Info.PopulateTrackData();
		}
		void Start()
		{
			worldUpDir = Physics.gravity.sqrMagnitude == 0 ? Vector3.up : -Physics.gravity.normalized;
			initialFixedTime = Time.fixedDeltaTime;
			// Set static variables
			wheelCastMaskStatic = wheelCastMask;
			groundMaskStatic = groundMask;
			damageMaskStatic = damageMask;
			ignoreWheelCastLayer = LayerMask.NameToLayer("Ignore Wheel Cast");
			frictionlessMatStatic = frictionlessMat;
			tireMarkLengthStatic = Mathf.Max(tireMarkLength, 2);
			tireMarkGapStatic = tireMarkGap;
			tireMarkHeightStatic = tireMarkHeight;
			tireFadeTimeStatic = tireFadeTime;

			if(Info.s_inEditor)
			{
				BackToEditor();
			}
			else
			{
				// START RACE

			}
		}

		void Update()
		{
			// DEBUG
			if (Input.GetKeyDown(KeyCode.K))
				hud.AddMessage(new Message("CP1 IS RECHARGING!" + 5 * UnityEngine.Random.value, BottomInfoType.PIT_IN));
			if (Input.GetKeyDown(KeyCode.M))
				hud.AddMessage(new Message("CP2 TAKES THE LEAD!" + 5 * UnityEngine.Random.value, BottomInfoType.NEW_LEADER));
			// Quickly restart scene with a button press
			if (quickRestart)
			{
				if (Input.GetButtonDown("Restart"))
				{
					SceneManager.LoadScene(SceneManager.GetActiveScene().name);
					Time.timeScale = 1;
					Time.fixedDeltaTime = initialFixedTime;
				}
			}
			if (Input.GetKeyDown(KeyCode.N))
			{
				SetPartOfDay(PartOfDay.Night);
			}
		}
	}
}