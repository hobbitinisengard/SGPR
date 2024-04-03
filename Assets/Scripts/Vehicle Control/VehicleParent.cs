using UnityEngine;
using System.Collections;
using System;
using Unity.Netcode;
using UnityEngine.InputSystem.HID;
using System.Linq;
using Unity.Netcode.Components;

namespace RVP
{
	public enum CatchupStatus { NoCatchup, Speeding, Slowing };

	[RequireComponent(typeof(Rigidbody))]
	[DisallowMultipleComponent]
	[AddComponentMenu("RVP/Vehicle Controllers/Vehicle Parent", 0)]

	// Vehicle root class
	public class VehicleParent : NetworkBehaviour
	{
		public AudioSource honkerAudio;
		public SampleText sampleText;
		[NonSerialized]
		public BasicInput basicInput;
		CarConfig carCfg;
		public CarConfig carConfig
		{
			get
			{
				if (carCfg == null)
					return Info.cars[carNumber - 1].config;
				else
					return carCfg;
			}
			set
			{
				carCfg = value;
				carCfg.Apply(this);
			}
		}
		/// <summary>
		/// from 1 to 20
		/// </summary>
		public int carNumber;
		[NonSerialized]
		public Ghost ghostComponent;
		public GameObject bodyObj;
		static double[] digitalBrakeInputEnv = { 0.050000, 0.055749, 0.061497, 0.067246, 0.072994, 0.078743, 0.084491, 0.090240, 0.095988, 0.100108, 0.104227, 0.108347, 0.112467, 0.116586, 0.120706, 0.124826, 0.128945, 0.131461, 0.133977, 0.136492, 0.139008, 0.141523, 0.144039, 0.146555, 0.149070, 0.150732, 0.152394, 0.154057, 0.155719, 0.157381, 0.159043, 0.160705, 0.162367, 0.164702, 0.167037, 0.169372, 0.171707, 0.174042, 0.176377, 0.178712, 0.181047, 0.184175, 0.187302, 0.190430, 0.193557, 0.198389, 0.203220, 0.208051, 0.212883, 0.217714, 0.222546, 0.227377, 0.232208, 0.234335, 0.236462, 0.238589, 0.240716, 0.242843, 0.244970, 0.247097, 0.249224, 0.251272, 0.253320, 0.255368, 0.257416, 0.263848, 0.270281, 0.276713, 0.283146, 0.289578, 0.296011, 0.302443, 0.308876, 0.313165, 0.317455, 0.321745, 0.326034, 0.330324, 0.334614, 0.338904, 0.343193, 0.349227, 0.355260, 0.361294, 0.367327, 0.373361, 0.379394, 0.385428, 0.391461, 0.400442, 0.409424, 0.418405, 0.427386, 0.436368, 0.445349, 0.454330, 0.463311, 0.472721, 0.482131, 0.491541, 0.500951, 0.510361, 0.519771, 0.529180, 0.538590, 0.560293, 0.581995, 0.603697, 0.625400, 0.647102, 0.668805, 0.690507, 0.712209, 0.735046, 0.757883, 0.780720, 0.803557, 0.826394, 0.849231, 0.872068, 0.888060, 0.904051, 0.920043, 0.936034, 0.952026, 0.968017, 0.984009, 1.000000 };
		static AnimationCurve brakeCurve;
		[System.NonSerialized]
		public Rigidbody rb;
		[System.NonSerialized]
		public float originalDrag;
		[System.NonSerialized]
		public float originalMass;
		[System.NonSerialized]
		public Transform tr;
		[System.NonSerialized]
		public Transform norm; // Normal orientation object

		public float accelInput;
		[System.NonSerialized]
		public bool honkInput;
		[System.NonSerialized]
		public float brakeInput;
		[Range(-1, 1)]
		public float steerInput;
		[System.NonSerialized]
		public float ebrakeInput;
		[System.NonSerialized]
		public bool boostButton;
		[System.NonSerialized]
		public bool SGPshiftbutton;
		[System.NonSerialized]
		public bool SGPlockbutton;
		[System.NonSerialized]
		public bool lightsInput;
		[System.NonSerialized]
		public bool upshiftPressed;
		[System.NonSerialized]
		public bool downshiftPressed;
		[System.NonSerialized]
		public float upshiftHold;
		[System.NonSerialized]
		public float downshiftHold;
		[System.NonSerialized]
		public float pitchInput;
		[System.NonSerialized]
		public float yawInput;
		[System.NonSerialized]
		public float rollInput;

		public GameObject[] frontLights;
		public GameObject[] rearLights;
		public Material rearLightsBrakeMaterial;
		public Material rearLightsOnMaterial;

		[Tooltip("Accel axis is used for brake input")]
		public bool accelAxisIsBrake;

		[Tooltip("Brake input will act as reverse input")]
		public bool brakeIsReverse;

		[Tooltip("Automatically hold ebrake if it's pressed while parked")]
		public bool holdEbrakePark;

		public float burnoutThreshold = 0.9f;
		[System.NonSerialized]
		public float burnout;
		public float burnoutSpin = 5;
		[Range(0, 0.9f)]
		public float burnoutSmoothness = 0.5f;
		public GasMotor engine;
		public ParticleSystem[] batteryLoadingParticleSystems;

		public float energyRemaining = 1000;
		public float batteryCapacity = 1000;
		public float BatteryPercent
		{
			get { return energyRemaining / batteryCapacity; }
		}
		public float batteryChargingSpeed = 200;
		public float lowBatteryLevel = 0.2f;

		public float batteryStuntIncreasePercent = 0.1f;

		bool stopUpshift;
		bool stopDownShift;

		[System.NonSerialized]
		public Vector3 localVelocity; // Local space velocity
		[System.NonSerialized]
		public Vector3 localAngularVel; // Local space angular velocity
		[System.NonSerialized]
		public Vector3 forwardDir; // Forward direction
		[System.NonSerialized]
		public Vector3 rightDir; // Right direction
		[System.NonSerialized]
		public Vector3 upDir; // Up direction
		[System.NonSerialized]
		public float forwardDot; // Dot product between forwardDir and GlobalControl.worldUpDir
		[System.NonSerialized]
		public float rightDot; // Dot product between rightDir and GlobalControl.worldUpDir
		[System.NonSerialized]
		public float upDot; // Dot product between upDir and GlobalControl.worldUpDir
		[System.NonSerialized]
		public float velMag; // Velocity magnitude
		[System.NonSerialized]
		public float sqrVelMag; // Velocity squared magnitude

		[System.NonSerialized]
		public bool reversing;
		[Tooltip("convention for placing wheels is FL, FR, RL, RR")]
		public Wheel[] wheels;
		public HoverWheel[] hoverWheels;
		public WheelCheckGroup[] wheelGroups;
		bool wheelLoopDone = false;
		public bool hover;
		[System.NonSerialized]
		public int groundedWheels; // Number of wheels grounded
		public int reallyGroundedWheels; // Number of really grounded wheels (cars can steer in air)
		[System.NonSerialized]
		public Vector3 wheelNormalAverage; // Average normal of the wheel contact points
		Vector3 wheelContactsVelocity; // Average velocity of wheel contact points

		[Tooltip("Lower center of mass by suspension height")]
		public bool suspensionCenterOfMass;
		public Transform centerOfMassObj;

		public ForceMode wheelForceMode = ForceMode.Acceleration;
		public ForceMode suspensionForceMode = ForceMode.Acceleration;

		[Tooltip("Tow vehicle to instantiate")]
		public GameObject towVehicle;
		GameObject newTow;
		[System.NonSerialized]
		public VehicleParent inputInherit; // Vehicle which to inherit input from

		[Header("Crashing")]

		public bool canCrash = true;
		public AudioSource roadNoiseSnd;
		public AudioSource crashSnd;
		public AudioSource scrapeSnd;
		public AudioSource batteryLoadingSnd;
		public AudioClip[] crashClips;
		[System.NonSerialized]
		public bool playCrashSounds = true;
		public ParticleSystem sparks;
		[System.NonSerialized]
		public bool playCrashSparks = true;

		[Header("Camera")]

		public float cameraDistanceChange;
		public float cameraHeightChange;

		[Header("Steering wheel")]
		public SteeringControl steeringControl;
		private float brakeStart;
		/// <summary>
		/// touching anything
		/// </summary>
		public bool colliding;
		public bool crashing; // serious impact
		//[Tooltip("Sideways friction when vehicle is in air. 0=no steering in air")]
		//public float inAirFriction = 0.25f;
		[NonSerialized]
		public GameObject customCam;
		private float lastNoBatteryMessage;

		private NetworkVariable<Livery> _sponsor = new();
		/// <summary>
		/// allowed values: 0-6
		/// </summary>
		public Livery sponsor
		{
			get { return _sponsor.Value; }
			set
			{
				string matName = null;
				try 
				{
					_sponsor.Value = value;
					var mr = bodyObj.GetComponent<MeshRenderer>();
					matName = mr.sharedMaterial.name;
					matName = matName[..^1] + ((int)value).ToString();
					Material newMat = Resources.Load<Material>("materials/" + matName);
					newMat.name = matName;
					mr.material = newMat;
				}
				catch
				{
					Debug.LogError(matName);
				}
			}
		}

		public float countdownTimer;

		public FollowAI followAI { get; private set; }
		public RaceBox raceBox { get; private set; }

		float catchupGripMult = 1.1f;
		/// <summary>
		/// used for roadNoise
		/// </summary>
		int roadSurfaceType;
		[NonSerialized]
		public float tyresOffroad;

		public CatchupStatus catchupStatus { get; private set; }
		private void SponsorValueChanged(Livery previousValue, Livery newValue)
		{
			sponsor = newValue;
		}

		public void SetBattery(float capacity, float chargingSpeed, float lowBatPercent, float evoBountyPercent)
		{
			energyRemaining = capacity;
			batteryCapacity = capacity;
			batteryChargingSpeed = chargingSpeed;
			lowBatteryLevel = lowBatPercent;
			batteryStuntIncreasePercent = evoBountyPercent;
		}

		public void SetCatchup(CatchupStatus newStatus)
		{
			if (Info.s_catchup)
			{
				switch (newStatus)
				{
					case CatchupStatus.NoCatchup:
						for (int i = 0; i < 4; ++i)
						{
							if (catchupStatus == CatchupStatus.Speeding)
							{
								wheels[i].sidewaysFriction /= catchupGripMult;
								wheels[i].forwardFriction /= catchupGripMult;
							}
							if (catchupStatus == CatchupStatus.Slowing)
							{
								wheels[i].sidewaysFriction *= catchupGripMult;
								wheels[i].forwardFriction *= catchupGripMult;
							}
						}
						break;
					case CatchupStatus.Speeding:
						for (int i = 0; i < 4; ++i)
						{
							if (catchupStatus == CatchupStatus.NoCatchup)
							{
								wheels[i].sidewaysFriction *= catchupGripMult;
								wheels[i].forwardFriction *= catchupGripMult;
							}
							if (catchupStatus == CatchupStatus.Slowing)
							{
								wheels[i].sidewaysFriction *= catchupGripMult * catchupGripMult;
								wheels[i].forwardFriction *= catchupGripMult * catchupGripMult;
							}
						}
						break;
					case CatchupStatus.Slowing:
						for (int i = 0; i < 4; ++i)
						{
							if (catchupStatus == CatchupStatus.NoCatchup)
							{
								wheels[i].sidewaysFriction /= catchupGripMult;
								wheels[i].forwardFriction /= catchupGripMult;
							}
							if (catchupStatus == CatchupStatus.Speeding)
							{
								wheels[i].sidewaysFriction /= catchupGripMult * catchupGripMult;
								wheels[i].forwardFriction /= catchupGripMult * catchupGripMult;
							}
						}
						break;
				}
				catchupStatus = newStatus;
			}
		}

		public void SetBatteryLoading(bool status)
		{
			foreach (var ps in batteryLoadingParticleSystems)
			{
				if (status)
				{
					batteryLoadingSnd.Play();
					ps.Play();
				}
				else
				{
					batteryLoadingSnd.Stop();
					ps.Stop();
				}
			}
		}
		AnimationCurve GenerateBrakeCurve()
		{
			//double[] dydx = { 22.225, 1.808226, 1.808099, 1.808226, 1.808099, 1.808226, 1.808226, 1.808099, 1.808226, 0.535686, 0.535686, 0.535686, 0.535686, 0.535686,0.535686, 0.535559, 0.535686, 0.662432, 0.662305, 0.662432, 0.662305,0.662432, 0.662305, 0.662432, 0.662305, 0.407543, 0.407543, 0.407543,0.407543, 0.407543, 0.407543, 0.407543, 0.407416, 0.102362, 0.102362,0.102235, 0.102362, 0.102362, 0.102235, 0.102362, 0.102362, 0.545719,0.545719, 0.545592, 0.545719, 0.127127, 0.127127, 0.127127, 0.127127,0.127127, 0.127127, 0.127127, 0.127127, 0.617093, 0.616966, 0.616966,0.617093, 0.616966, 0.617093, 0.616966, 0.616966, 0.719328, 0.719328,0.719455, 0.719328, 0.719328, 0.719328, 0.719328, 0.719328, 0.930148,0.930148, 0.930148, 0.930275, 0.930148, 0.930148, 0.930148, 0.930148,1.364361, 1.364234, 1.364234, 1.364234, 1.364234, 1.364361, 1.364234,1.364234, 1.382903, 1.382776, 1.382903, 1.382903, 1.382776, 1.382903,1.382903, 1.382776, 1.885188, 1.885188, 1.885061, 1.885188, 1.885188,1.885188, 1.885061, 1.885188, 1.488313, 1.488313, 1.488186, 1.488313,1.488313, 1.488313, 1.488186, 1.488313, 0.595376, 0.595249, 0.595376,0.595249, 0.595376, 0.595249, 0.595376, 0.595249, 0.396875, 0.396875,0.396875, 0.396875, 0 , 0 , 0 , 0, 0,0};
			Keyframe[] keys = new Keyframe[digitalBrakeInputEnv.Length];
			for (int i = 0; i < keys.Length; i++)
			{
				keys[i].time = (float)i / keys.Length;
				keys[i].value = (float)digitalBrakeInputEnv[i];
			}
			return new AnimationCurve(keys);
		}
		private void Awake()
		{
			ghostComponent = GetComponent<Ghost>();
			followAI = GetComponent<FollowAI>();
			raceBox = GetComponent<RaceBox>();
			tr = transform;
			rb = GetComponent<Rigidbody>();
			originalDrag = rb.drag;
			originalMass = rb.mass;
			Info.s_cars.Add(this);
		}
		void Start()
		{
			if(Info.gameMode == MultiMode.Multiplayer)
			{
				gameObject.AddComponent<NetworkRigidbody>();
			}
			brakeCurve ??= GenerateBrakeCurve();

			// Create normal orientation object
			GameObject normTemp = new GameObject(tr.name + "'s Normal Orientation");
			norm = normTemp.transform;

			SetCenterOfMass();

			// Instantiate tow vehicle
			if (towVehicle)
			{
				newTow = Instantiate(towVehicle, Vector3.zero, tr.rotation);
				newTow.SetActive(false);
				newTow.transform.position = tr.TransformPoint(newTow.GetComponent<Joint>().connectedAnchor - newTow.GetComponent<Joint>().anchor);
				newTow.GetComponent<Joint>().connectedBody = rb;
				newTow.SetActive(true);
				newTow.GetComponent<VehicleParent>().inputInherit = this;
			}

			followAI.SetCPU(transform.name.Contains("CP"));
			sampleText.gameObject.SetActive(followAI.isCPU || transform.name != Info.playerData.playerName);
			if (Info.s_spectator)
			{
				if (UnityEngine.Random.value > 0.5f)
					Info.raceManager.cam.Connect(this, CameraControl.Mode.Replay);
			}
			else
			{
				if(transform.name == Info.playerData.playerName)
				{
					Info.raceManager.playerCar = this;
					Info.raceManager.cam.Connect(this);
					Info.raceManager.hud.Connect(this);
					//newCar.followAI.SetCPU(true); // CPU drives player's car
				}
			}
			Info.raceManager.cam.enabled = true;

			sampleText.gameObject.SetActive(Info.s_spectator);
			Info.raceManager.DemoSGPLogo.SetActive(Info.s_spectator);
			Info.raceManager.hud.gameObject.SetActive(!Info.s_spectator);

			StartCoroutine(ApplySetup());
			
			_sponsor.OnValueChanged += SponsorValueChanged;

			if (Info.s_isNight)
				SetLights();

			StartCoroutine(CountdownTimer(Info.countdownSeconds));
		}
		IEnumerator ApplySetup()
		{
			yield return null;
			Info.cars[carNumber - 1].config.Apply(this);
		}

		void Update()
		{
			if (Physics.Raycast(tr.position, rb.velocity, 200, 1 << Info.aeroTunnel))
			{ // aerodynamic tunnel
				rb.drag = 0;
			}
			else
				rb.drag = originalDrag;
			// Shift single frame pressing logic
			if (stopUpshift)
			{
				upshiftPressed = false;
				stopUpshift = false;
			}

			if (stopDownShift)
			{
				downshiftPressed = false;
				stopDownShift = false;
			}

			if (upshiftPressed)
			{
				stopUpshift = true;
			}

			if (downshiftPressed)
			{
				stopDownShift = true;
			}

			if (inputInherit)
			{
				InheritInputOneShot();
			}

			if (wheels[2].curSurfaceType != roadSurfaceType)
			{
				roadSurfaceType = wheels[2].curSurfaceType;
				roadNoiseSnd.clip = GroundSurfaceMaster.surfaceTypesStatic[roadSurfaceType].roadNoise;
			}
			float volume = Mathf.InverseLerp(0, 80, velMag);

			roadNoiseSnd.volume = (Info.gamePaused || reallyGroundedWheels == 0) ? 0 : volume;// (1 + 80 * 2 / 3f * Mathf.Log10(volume)); 

			if (brakeInput > 0 && !reversing)
			{
				// brake lights
				foreach (var l in rearLights)
				{
					l.SetActive(true);
					l.GetComponent<MeshRenderer>().sharedMaterials =
						 new Material[] { l.GetComponent<MeshRenderer>().sharedMaterials[0], rearLightsBrakeMaterial };
					l.transform.GetChild(0).GetComponent<Light>().range = 10;
				}
			}
			else if (brakeInput == 0)
			{
				// no brake lights
				foreach (var l in rearLights)
				{
					l.SetActive(lightsInput);
					l.GetComponent<MeshRenderer>().sharedMaterials =
						 new Material[] { l.GetComponent<MeshRenderer>().sharedMaterials[0], rearLightsOnMaterial };
					l.transform.GetChild(0).GetComponent<Light>().range = 2;
				}
			}
			// Norm orientation visualizing
			// Debug.DrawRay(norm.position, norm.forward, Color.blue);
			// Debug.DrawRay(norm.position, norm.up, Color.green);
			// Debug.DrawRay(norm.position, norm.right, Color.red);
		}

		void FixedUpdate()
		{
			if (inputInherit)
			{
				InheritInput();
			}

			if (wheelLoopDone && wheelGroups.Length > 0)
			{
				wheelLoopDone = false;
				StartCoroutine(WheelCheckLoop());
			}

			GetGroundedWheels();

			localVelocity = tr.InverseTransformDirection(rb.velocity - wheelContactsVelocity);
			localAngularVel = tr.InverseTransformDirection(rb.angularVelocity);
			velMag = rb.velocity.magnitude;
			sqrVelMag = rb.velocity.sqrMagnitude;
			forwardDir = tr.forward;
			rightDir = tr.right;
			upDir = tr.up;
			forwardDot = Vector3.Dot(forwardDir, RaceManager.worldUpDir);
			rightDot = Vector3.Dot(rightDir, RaceManager.worldUpDir);
			upDot = Vector3.Dot(upDir, RaceManager.worldUpDir);
			norm.transform.position = tr.position;
			norm.transform.rotation = Quaternion.LookRotation(reallyGroundedWheels == 0 ? upDir : wheelNormalAverage, forwardDir);

			// Check if performing a burnout
			if (reallyGroundedWheels > 0 && !hover && !accelAxisIsBrake && burnoutThreshold >= 0 && accelInput > burnoutThreshold && brakeInput > burnoutThreshold)
			{
				burnout = Mathf.Lerp(burnout, ((5 - Mathf.Min(5, Mathf.Abs(localVelocity.z))) / 5) * Mathf.Abs(accelInput), Time.fixedDeltaTime * (1 - burnoutSmoothness) * 10);
			}
			else if (burnout > 0.01f)
			{
				burnout = Mathf.Lerp(burnout, 0, Time.fixedDeltaTime * (1 - burnoutSmoothness) * 10);
			}
			else
			{
				burnout = 0;
			}

			if (engine)
			{
				burnout *= engine.health;
			}

			// Check if reversing
			if (brakeIsReverse && brakeInput > 0 && localVelocity.z < 1 && burnout == 0)
				reversing = true;
			else if (localVelocity.z >= 0 || burnout > 0)
				reversing = false;
		}
		public void SetHonkerInput(bool f)
		{
			honkInput = f;
			if (honkInput)
			{
				if (!honkerAudio.isPlaying)
					honkerAudio.Play();
			}
			else
				honkerAudio.Stop();
		}
		/// <summary>
		/// Type is clamped to <1;6>
		/// </summary>
		public void SetHonkerAudio(int type)
		{
			type = Mathf.Clamp(type, 1, 6);
			honkerAudio.clip = Info.audioClips["hornloop0" + type.ToString()];
		}
		// Set accel input
		public void SetAccel(float f)
		{
			if (!raceBox.enabled)
				energyRemaining = batteryCapacity;
			else if (BatteryPercent == 0 && Time.time - lastNoBatteryMessage > 60)
			{
				Info.raceManager.hud.infoText.AddMessage(new Message(name + " IS OUT OF BATTERY!", BottomInfoType.NO_BATT));
				lastNoBatteryMessage = Time.time;
			}
			f = Mathf.Clamp(f, -1, (BatteryPercent == 0) ? 0.75f : 1);
			accelInput = f;
			if (energyRemaining > 0)
				energyRemaining -= accelInput * engine.fuelConsumption * Time.deltaTime;
		}

		// Set brake input
		public void SetBrake(float f)
		{
			if (followAI.selfDriving)
			{
				brakeInput = f;
			}
			else
			{
				if (f == 0)
				{
					brakeStart = 0;
					brakeInput = 0;
				}
				else
				{
					if (brakeIsReverse && reversing)
						brakeInput = 1;
					else
					{
						if (brakeStart == 0)
						{
							brakeStart = Time.time;
						}
						brakeInput = Mathf.Lerp(brakeInput, 1, Time.fixedDeltaTime * brakeCurve.Evaluate(Time.time - brakeStart));
					}
				}
			}
		}
		public void SetSteer(float f)
		{
			steerInput = Mathf.Clamp(f, -1, 1);
		}

		// Set ebrake input
		public void SetEbrake(float f)
		{
			if ((f > 0 || ebrakeInput > 0) && holdEbrakePark && velMag < 1 && accelInput == 0 && (brakeInput == 0 || !brakeIsReverse))
			{
				ebrakeInput = 1;
			}
			else
			{
				ebrakeInput = Mathf.Clamp01(f);
			}
		}

		public void SetBoost(bool b)
		{
			if (b && BatteryPercent > lowBatteryLevel)
			{
				energyRemaining -= Time.deltaTime * engine.jetConsumption;
			}
			else
				b = false;

			boostButton = b;
		}
		public void SetSGPShift(bool b)
		{
			SGPshiftbutton = b;
		}
		public void SetLights()
		{
			lightsInput = !lightsInput;
			foreach (var l in frontLights)
				l.SetActive(lightsInput);
			foreach (var l in rearLights)
				l.SetActive(lightsInput);
		}
		// turned off
		public void SetPitch(float f)
		{
			pitchInput = 0;// = Mathf.Clamp(f, -1, 1);
		}

		// Set yaw rotate input
		public void SetYaw(float f)
		{
			yawInput = Mathf.Clamp(f, -1, 1);
		}

		// Set roll rotate input
		public void SetRoll(float f)
		{
			rollInput = Mathf.Clamp(f, -1, 1);
		}

		// Do upshift input
		public void PressUpshift()
		{
			upshiftPressed = true;
		}

		// Do downshift input
		public void PressDownshift()
		{
			downshiftPressed = true;
		}

		// Set held upshift input
		public void SetUpshift(float f)
		{
			upshiftHold = f;
		}

		// Set held downshift input
		public void SetDownshift(float f)
		{
			downshiftHold = f;
		}

		// Copy input from other vehicle
		void InheritInput()
		{
			accelInput = inputInherit.accelInput;
			brakeInput = inputInherit.brakeInput;
			steerInput = inputInherit.steerInput;
			ebrakeInput = inputInherit.ebrakeInput;
			pitchInput = inputInherit.pitchInput;
			yawInput = inputInherit.yawInput;
			rollInput = inputInherit.rollInput;
		}

		// Copy single-frame input from other vehicle
		void InheritInputOneShot()
		{
			upshiftPressed = inputInherit.upshiftPressed;
			downshiftPressed = inputInherit.downshiftPressed;
		}

		// Change the center of mass of the vehicle
		public void SetCenterOfMass()
		{
			float susAverage = 0;

			// Get average suspension height
			if (suspensionCenterOfMass)
			{
				if (hover)
				{
					for (int i = 0; i < hoverWheels.Length; i++)
					{
						susAverage = (i == 0) ? hoverWheels[i].hoverDistance : (susAverage + hoverWheels[i].hoverDistance) * 0.5f;
					}
				}
				else
				{
					for (int i = 0; i < wheels.Length; i++)
					{
						float newSusDist = wheels[i].transform.parent.GetComponent<Suspension>().suspensionDistance;
						susAverage = (i == 0) ? newSusDist : (susAverage + newSusDist) * 0.5f;
					}
				}
			}
			rb.centerOfMass = centerOfMassObj.localPosition + new Vector3(0, susAverage, 0);
		}

		// Get the number of grounded wheels and the normals and velocities of surfaces they're sitting on
		void GetGroundedWheels()
		{
			groundedWheels = 0;
			reallyGroundedWheels = 0;
			wheelContactsVelocity = Vector3.zero;

			if (hover)
			{
				for (int i = 0; i < hoverWheels.Length; i++)
				{

					if (hoverWheels[i].grounded)
					{
						wheelNormalAverage = i == 0 ? hoverWheels[i].contactPoint.normal : (wheelNormalAverage + hoverWheels[i].contactPoint.normal).normalized;
						groundedWheels++;
					}
				}
			}
			else
			{
				for (int i = 0; i < wheels.Length; i++)
				{
					if (wheels[i].grounded)
					{
						wheelContactsVelocity = (i == 0) ? wheels[i].contactVelocity : (wheelContactsVelocity + wheels[i].contactVelocity) * 0.5f;
						wheelNormalAverage = (i == 0) ? wheels[i].contactPoint.normal : (wheelNormalAverage + wheels[i].contactPoint.normal).normalized;
						groundedWheels++;
					}
					if (wheels[i].groundedReally)
					{
						reallyGroundedWheels++;
					}
				}
			}
		}

		// Check for crashes and play collision sounds
		void OnCollisionEnter(Collision col)
		{
			if (col.contacts.Length > 0)
			{
				foreach (ContactPoint curCol in col.contacts)
				{
					if (curCol.thisCollider.gameObject.layer != RaceManager.ignoreWheelCastLayer)
					{
						if (Mathf.Abs(Vector3.Dot(curCol.normal, col.relativeVelocity.normalized)) > 0.1f && col.relativeVelocity.magnitude > 10)
						{
							crashSnd.PlayOneShot(crashClips[UnityEngine.Random.Range(0, crashClips.Length)],
								Mathf.Clamp01(col.relativeVelocity.magnitude * 0.1f));
							crashing = canCrash;
						}

						if (sparks && playCrashSparks)
						{
							// play sparks
							sparks.transform.position = curCol.point;
							sparks.transform.rotation = Quaternion.LookRotation(col.relativeVelocity.normalized, curCol.normal);
							sparks.Play();
						}
					}
				}
			}
		}
		// Continuous collision checking
		void OnCollisionStay(Collision col)
		{
			bool nowCrashing = false;
			if (col.contacts.Length > 0)
			{
				foreach (ContactPoint curCol in col.contacts)
				{
					if (!curCol.thisCollider.CompareTag("Underside")
						&& curCol.thisCollider.gameObject.layer != RaceManager.ignoreWheelCastLayer)
					{
						nowCrashing = canCrash;

						//if (reallyGroundedWheels >= 2)
						{
							if (!scrapeSnd.isPlaying)
								scrapeSnd.Play();
							// play sparks
							sparks.transform.position = curCol.point;
							sparks.transform.rotation = Quaternion.LookRotation(col.relativeVelocity.normalized, curCol.normal);
							if (!sparks.isPlaying)
								sparks.Play();
						}
					}
				}
			}
			crashing = nowCrashing;
			if (!(colliding || crashing))
				scrapeSnd.Stop();
		}

		public override void OnDestroy()
		{
			//if(Info.gameMode == MultiMode.Multiplayer) // when player suddenly disconnects
			//	Info.s_cars.Remove(Info.s_cars.First(c => c.transform.name == transform.name));

			if (norm)
			{
				Destroy(norm.gameObject);
			}

			if (sparks)
			{
				Destroy(sparks.gameObject);
			}
			base.OnDestroy();
		}

		// Loop through all wheel groups to check for wheel contacts
		IEnumerator WheelCheckLoop()
		{
			for (int i = 0; i < wheelGroups.Length; i++)
			{
				wheelGroups[i].Activate();
				wheelGroups[i == 0 ? wheelGroups.Length - 1 : i - 1].Deactivate();
				yield return new WaitForFixedUpdate();
			}
			wheelLoopDone = true;
		}

		internal void ResetOnTrack()
		{
			StartCoroutine(followAI.ResetOnTrack());
		}

		public IEnumerator CountdownTimer(float v)
		{
			countdownTimer = v;
			GetComponent<SGP_Bouncer>().enabled = false;
			while (countdownTimer > 0)
			{
				SetEbrake(1);
				countdownTimer -= Time.deltaTime;
				yield return null;
			}
			SetEbrake(0);
		}



		public void AddWheelGroup()
		{
			WheelCheckGroup wcg = new WheelCheckGroup
			{
				wheels = wheels
			};
			wheelGroups = new WheelCheckGroup[] { wcg };
		}

		public void ChargeBattery()
		{
			if (!batteryLoadingSnd.isPlaying)
			{
				batteryLoadingSnd.clip = Info.audioClips["elec" + Mathf.RoundToInt(3 * UnityEngine.Random.value)];
				batteryLoadingSnd.Play();
			}
			energyRemaining = Mathf.Clamp(energyRemaining + batteryChargingSpeed * Time.deltaTime, 0, batteryCapacity);
		}

		public void ChargeBatteryByStunt()
		{
			energyRemaining = Mathf.Clamp(energyRemaining + batteryCapacity * batteryStuntIncreasePercent, 0, batteryCapacity);
		}
		public void ResetOnTrackBatteryPenalty()
		{
			energyRemaining = Mathf.Clamp(energyRemaining - batteryCapacity * 0.5f * batteryStuntIncreasePercent, 0, batteryCapacity);
		}
		[Rpc(SendTo.Everyone)]
		public void KnockoutMeRpc()
		{
			raceBox.enabled = false;
			followAI.SetCPU(false);
			followAI.selfDriving = false;
			basicInput.enabled = false;
			SetAccel(0);
			SetBrake(0);
			SetSteer(0);
			raceBox.SetRacetime();
		}
	}

	// Class for groups of wheels to check each FixedUpdate
	[Serializable]
	public class WheelCheckGroup
	{
		public Wheel[] wheels;
		public HoverWheel[] hoverWheels;

		public void Activate()
		{
			foreach (Wheel curWheel in wheels)
			{
				curWheel.getContact = true;
			}

			foreach (HoverWheel curHover in hoverWheels)
			{
				curHover.getContact = true;
			}
		}

		public void Deactivate()
		{
			foreach (Wheel curWheel in wheels)
			{
				curWheel.getContact = false;
			}

			foreach (HoverWheel curHover in hoverWheels)
			{
				curHover.getContact = false;
			}
		}
	}
}