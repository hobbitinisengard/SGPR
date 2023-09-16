using UnityEngine;
using UnityEngine.SceneManagement;


namespace RVP
{
	public enum PartOfDay { Day, Night };
	[DisallowMultipleComponent]
	[AddComponentMenu("RVP/Scene Controllers/Global Control", 0)]

	// Global controller class
	public class GlobalControl : MonoBehaviour
	{
		public AudioSource musicPlayer;
		
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
		Color HDRColor(float r, float g, float b, int intensity = 0)
		{
			float factor = Mathf.Pow(2, intensity);
			return new Color(r * factor, g * factor, b * factor);
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

			//audioSource.Play();
		}

		void Update()
		{
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

		void FixedUpdate()
		{
			// Set global up direction
			worldUpDir = Physics.gravity.sqrMagnitude == 0 ? Vector3.up : -Physics.gravity.normalized;
		}
	}
}