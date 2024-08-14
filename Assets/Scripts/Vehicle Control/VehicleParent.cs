using UnityEngine;
using System.Collections;
using System;
using Unity.Netcode;
using Unity.Collections;
using System.Linq;
using UnityEngine.InputSystem.HID;

namespace RVP
{
	//public struct StatePayload : INetworkSerializable
	//{
	//	public int tick;
	//	public DateTime timestamp;
	//	public Vector3 position;
	//	public Quaternion rotation;
	//	public Vector3 velocity;
	//	public Vector3 angularVelocity;

	//	public StatePayload(int tick, VehicleParent vp) : this()
	//	{
	//		this.tick = tick;
	//		timestamp = DateTime.UtcNow;
	//		position = vp.tr.position;
	//		rotation = vp.tr.rotation;
	//		velocity = vp.rb.velocity;
	//		angularVelocity = vp.rb.angularVelocity;
	//	}

	//	public void NetworkSerialize<T>(BufferSerializer<T> s) where T : IReaderWriter
	//	{
	//		s.SerializeValue(ref tick);
	//		s.SerializeValue(ref position);
	//		s.SerializeValue(ref rotation);
	//		s.SerializeValue(ref velocity);
	//		s.SerializeValue(ref angularVelocity);
	//		s.SerializeValue(ref timestamp);
	//	}
	//}
	//public struct InputPayload : INetworkSerializable
	//{
	//	public int tick;
	//	byte honkBoostShift;
	//	ushort accel;
	//	ushort steer;
	//	ushort brake;
	//	ushort roll;
	//	public void ApplyToCar(VehicleParent vp)
	//	{
	//		vp.SetAccel(Mathf.HalfToFloat(accel));
	//		vp.SetSteer(Mathf.HalfToFloat(steer));
	//		vp.SetBrake(Mathf.HalfToFloat(brake));
	//		vp.SetRoll(Mathf.HalfToFloat(roll));
	//		vp.SetHonkerInput(honkBoostShift & 0x01);
	//		vp.SetBoost((honkBoostShift >> 1) & 0x01);
	//		vp.SetRoll((honkBoostShift >> 2) & 0x01);
	//	}
	//	public InputPayload(VehicleParent vp, int tick)
	//	{
	//		accel = Mathf.FloatToHalf(vp.accelInput);
	//		steer = Mathf.FloatToHalf(vp.steerInput);
	//		brake = Mathf.FloatToHalf(vp.brakeInput);
	//		roll = Mathf.FloatToHalf(vp.rollInput);
	//		honkBoostShift = (byte)(vp.honkInput | vp.boostButton << 1 | vp.SGPshiftbutton << 2);
	//		this.tick = tick;

	//	}
	//	public void NetworkSerialize<T>(BufferSerializer<T> s) where T : IReaderWriter
	//	{
	//		if(s.IsWriter)
	//		{
	//			s.SerializeValue(ref honkBoostShift);
	//			s.SerializeValue(ref accel);
	//			s.SerializeValue(ref steer);
	//			s.SerializeValue(ref brake);
	//			s.SerializeValue(ref roll);

	//			s.SerializeValue(ref tick);
	//		}
	//	}
	//}

	public enum CatchupStatus { NoCatchup, Speeding, Slowing };

	[RequireComponent(typeof(Rigidbody))]
	[DisallowMultipleComponent]
	[AddComponentMenu("RVP/Vehicle Controllers/Vehicle Parent", 0)]

	// Vehicle root class
	public class VehicleParent : NetworkBehaviour
	{
		public Renderer antennaFlag;
		public MeshRenderer[] springRenderers;
		public AudioSource honkerAudio;
		public SampleText sampleText;
		[NonSerialized]
		public BasicInput basicInput;
		[NonSerialized]
		public CarConfig carConfig;
		/// <summary>
		/// from 1 to 20
		/// </summary>
		public int carNumber;
		[NonSerialized]
		public Ghost ghost;
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

		/// <summary>
		/// This can't be set to random.
		/// </summary>
		NetworkVariable<Livery> _sponsor = new();

		[System.NonSerialized]
		public float accelInput;
		[System.NonSerialized]
		public int honkInput;
		[System.NonSerialized]
		public float brakeInput;
		[System.NonSerialized]
		[Range(-1, 1)]
		public float steerInput;
		[System.NonSerialized]
		public float ebrakeInput;
		[System.NonSerialized]
		public int boostButton;
		[System.NonSerialized]
		public int SGPshiftbutton;
		[System.NonSerialized]
		public float rollInput;
		[NonSerialized]
		public float resetOnTrackTime = 0;

		//NetworkVariable<float> _accelInput = new(writePerm: NetworkVariableWritePermission.Owner);
		//public float accelInput { get { return _accelInput.Value; } set { _accelInput.Value = value; } }
		//[System.NonSerialized]
		//NetworkVariable<int> _honkInput = new(writePerm: NetworkVariableWritePermission.Owner);
		//public int honkInput { get { return _honkInput.Value; } set { _honkInput.Value = value; } }
		//[System.NonSerialized]
		//NetworkVariable<float> _brakeInput = new(writePerm: NetworkVariableWritePermission.Owner);
		//public float brakeInput { get { return _brakeInput.Value; } set { _brakeInput.Value = value; } }
		//[Range(-1, 1)]
		//NetworkVariable<float> _steerInput = new(writePerm: NetworkVariableWritePermission.Owner);
		//public float steerInput { get { return _steerInput.Value; } set { _steerInput.Value = value; } }
		//[System.NonSerialized]
		//public float ebrakeInput;
		//[System.NonSerialized]
		//NetworkVariable<int> _boostButton = new(writePerm: NetworkVariableWritePermission.Owner);
		//public int boostButton { get { return _boostButton.Value; } set { _boostButton.Value = value; } }
		//[System.NonSerialized]
		//NetworkVariable<int> _SGPshiftbutton = new(writePerm: NetworkVariableWritePermission.Owner);
		//public int SGPshiftbutton { get { return _SGPshiftbutton.Value; } set { _SGPshiftbutton.Value = value; } }

		//[System.NonSerialized]
		//NetworkVariable<int> _rollButton = new(writePerm: NetworkVariableWritePermission.Owner);
		//public int rollInput { get { return _boostButton.Value; } set { _boostButton.Value = value; } }

		public Livery sponsor
		{
			get
			{
				return _sponsor.Value;
			}
			set
			{
				if (ServerC.I.AmHost)
				{
					if (value == Livery.Random)
						value = F.RandomLivery();
					_sponsor.Value = value;
				}
			}
		}

		NetworkVariable<FixedString32Bytes> _name = new(); // SERVER
		public new string name
		{
			get
			{
				return _name.Value.ToString();
			}
			set
			{
				if (ServerC.I.AmHost)
				{
					base.name = value;
					_name.Value = value;
				}
			}
		}
		public static bool InFreeroam
		{
			get { return F.I.s_inEditor && F.I.s_cpuRivals == 0; }
		}
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
		Vector3 prevVel;
		[System.NonSerialized]
		public float sqrVelMag; // Velocity squared magnitude
		public Vector3 acceleration { get; private set; }
		[System.NonSerialized]
		public bool reversing;
		[Tooltip("convention for placing wheels is FL, FR, RL, RR")]
		public Wheel[] wheels;
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

		public ForceMode wheelForceMode = ForceMode.Acceleration;
		public ForceMode suspensionForceMode = ForceMode.Acceleration;

		[Tooltip("Tow vehicle to instantiate")]
		public GameObject towVehicle;
		[System.NonSerialized]
		public VehicleParent inputInherit; // Vehicle which to inherit input from

		[Header("Crashing")]
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
		private Coroutine colCo;
		public bool crashing; // serious impact
		[NonSerialized]
		public GameObject customCam;
		private float lastNoBatteryMessage;
		public bool Owner { get { return IsOwner || F.I.gameMode == MultiMode.Singleplayer; } }
		[NonSerialized]
		public int lastRoundScore;
		[Rpc(SendTo.SpecifiedInParams)]
		public void RelinquishRpc(RpcParams ps)
		{
			NetworkObject.RemoveOwnership();
			Destroy(gameObject);
		}
		[Rpc(SendTo.SpecifiedInParams)]
		void RequestRaceboxValuesRpc(RpcParams ps)
		{
			SynchRaceboxValuesRpc(raceBox.enabled, ServerC.I.PlayerMe.ScoreGet(), raceBox.curLap, followAI.dist, followAI.progress, raceBox.Aero, raceBox.Drift,
				(float)raceBox.bestLapTime.TotalSeconds, (float)raceBox.raceTime.TotalSeconds,
				RpcTarget.Single(ps.Receive.SenderClientId, RpcTargetUse.Temp));
		}
		[Rpc(SendTo.SpecifiedInParams)]
		public void SynchRaceboxValuesRpc(bool enabled, int lastRoundScore, int curLap, int dist, int progress, float aero, float drift,
			float bestLapSecs, float raceTimeSecs, RpcParams ps)
		{
			this.lastRoundScore = lastRoundScore;
			raceBox.UpdateValues(enabled, curLap, dist, progress, aero, drift, bestLapSecs, raceTimeSecs);
			ResultsView.Add(this);
		}
		public FollowAI followAI { get; private set; }
		public RaceBox raceBox { get; private set; }

		float catchupGripMult = 1.1f;
		int roadSurfaceType;
		[NonSerialized]
		public float tyresOffroad;
		private float timeWhenInAir;

		public CatchupStatus catchupStatus { get; private set; }
		public float AngularDrag { get { return rb.angularDrag; } }

		bool collisionDetectionChangerActive;
		private float lastCrashingTime;

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
			if (F.I.catchup)
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
		void OnSponsorChanged()
		{
			var mr = bodyObj.GetComponent<MeshRenderer>();
			string matName = mr.sharedMaterial.name;
			if (matName.Contains("Variant"))
				matName = matName[..matName.IndexOf(' ')];
			matName = matName[..^1] + ((int)sponsor).ToString();
			Material newMat = Resources.Load<Material>("materials/" + matName);
			newMat.name = matName;
			mr.material = newMat;
			if (antennaFlag != null)
				antennaFlag.material = newMat;
			RaceManager.I.hud.AddToProgressBar(this);
			sampleText.textMesh.color = F.ReadColor(sponsor);
		}
		void OnNameChanged()
		{
			base.name = name;
			sampleText.textMesh.text = name;
			if (name == F.I.playerData.playerName && Owner)
			{
				RaceManager.I.playerCar = this;

				RaceManager.I.cam.Connect(this);
				RaceManager.I.hud.Connect(this);

				NetworkManager.OnTransportFailure += NetworkManager_OnTransportFailure;
				//newCar.followAI.SetCPU(true); // CPU drives player's car
			}
			sampleText.gameObject.SetActive(!F.I.s_spectator && F.I.gameMode == MultiMode.Multiplayer && RaceManager.I.playerCar != this);
		}

		private void NetworkManager_OnTransportFailure()
		{
			RaceManager.I.ExitButton();
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
			ghost = GetComponent<Ghost>();
			followAI = GetComponent<FollowAI>();
			raceBox = GetComponent<RaceBox>();
			tr = transform;
			rb = GetComponent<Rigidbody>();
			originalDrag = rb.drag;
			originalMass = rb.mass;
			F.I.s_cars.Add(this);
		}
		public override void OnNetworkSpawn()
		{
			base.OnNetworkSpawn();
			Initialize();
		}

		private void Start()
		{
			if (F.I.gameMode == MultiMode.Singleplayer)
				Initialize();
		}

		void Initialize()
		{

			Color c = F.RandomColor();
			foreach (var s in springRenderers)
				s.material.color = c;

			brakeCurve ??= GenerateBrakeCurve();

			// Create normal orientation object
			GameObject normTemp = new (tr.name + "'s Normal");
			norm = normTemp.transform;


			if (F.I.s_spectator)
			{
				if (UnityEngine.Random.value > 0.5f)
					RaceManager.I.cam.Connect(this, CameraControl.Mode.Replay);
			}

			

			StartCoroutine(ApplySetup());
		}

		IEnumerator ApplySetup()
		{
			while (sponsor == Livery.Random)
			{ // sending sponsor info may come from the server after a while
				yield return null;
			}
			
			OnNameChanged();
			OnSponsorChanged();

			followAI.SetCPU(char.IsDigit(name[2]));

			yield return new WaitForSeconds(.5f); // wait for all the components to load

			carConfig = new CarConfig(F.I.cars[carNumber - 1].config);
			carConfig.Apply(this);

			if (F.I.s_raceType == RaceType.TimeTrial)
				ghost.SetGhostPermanently();

			if (!Owner)
			{
				//rb.isKinematic = true;
				basicInput.enabled = false;
				if (Online.I.raceAlreadyStarted.Value)
				{// latecomer's request to synch progress
				 //Debug.Log("RequestRaceboxValuesRpc");
					RequestRaceboxValuesRpc(RpcTarget.Owner);
				}
			}
			else if (F.I.gameMode == MultiMode.Multiplayer)
			{
				ServerC.I.ReadySet(PlayerState.InRace);
				ServerC.I.UpdatePlayerData();
			}
			ResultsView.Add(this);
			engine.ignition = true;
			lightsInput = F.I.s_isNight;
			foreach (var l in frontLights)
				l.SetActive(lightsInput);
			foreach (var l in rearLights)
				l.SetActive(lightsInput);
		}


		[Rpc(SendTo.SpecifiedInParams)]
		public void SetCurLapRpc(int curLap, RpcParams ps)
		{
			raceBox.curLap = curLap;
		}
		void Update()
		{
			// we need continous collisions for fast flying cars
			// but still we need discrete collisions for driving (car physics requirement)
			//if (reallyGroundedWheels == 0)
			//{
			//	rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
			//}
			//else
			//{
			//	if (rb.collisionDetectionMode == CollisionDetectionMode.Continuous && !collisionDetectionChangerActive)
			//		rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
			//}

			if (Physics.OverlapBox(tr.position, Vector3.one, Quaternion.identity, 1 << F.I.aeroTunnel).Length > 0)
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
			roadNoiseSnd.gameObject.SetActive((!F.I.gamePaused && reallyGroundedWheels > 0));
			roadNoiseSnd.volume = Mathf.InverseLerp(0, 80, velMag);// (1 + 80 * 2 / 3f * Mathf.Log10(volume)); 

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
		public void FixedUpdate()
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

			prevVel = localVelocity;
			localVelocity = tr.InverseTransformDirection(rb.velocity - wheelContactsVelocity);
			acceleration = localVelocity - prevVel;

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

			if (brakeIsReverse && brakeInput > 0 && localVelocity.z < 1)
				reversing = true;
			else if (localVelocity.z >= 0 || burnout > 0)
				reversing = false;
		}
		public void SetHonkerInput(int f)
		{
			honkInput = f;
			if (honkInput == 1)
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
			honkerAudio.clip = F.I.audioClips["hornloop0" + type.ToString()];
		}
		// Set accel input
		public void SetAccel(float f)
		{
			if (InFreeroam || !raceBox.enabled || F.I.s_raceType == RaceType.TimeTrial)
				energyRemaining = batteryCapacity;
			else if (BatteryPercent <= 0 && Time.time - lastNoBatteryMessage > 60)
			{
				RaceManager.I.hud.infoText.AddMessage(new Message(name + " IS OUT OF BATTERY!", BottomInfoType.NO_BATT));
				lastNoBatteryMessage = Time.time;
			}
			f = Mathf.Clamp(f, -1, (BatteryPercent <= 0) ? 0.67f : 1);

			if (Owner)
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
			SetBoost(b ? 1 : 0);
		}
		public void SetBoost(int b)
		{
			if (b == 1 && BatteryPercent > lowBatteryLevel)
			{
				energyRemaining -= Time.deltaTime * engine.jetConsumption;
			}
			else
				b = 0;

			boostButton = b;
		}
		public void SetSGPShift(int b)
		{
			SGPshiftbutton = b;
		}
		public void Switchlights()
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
			rollInput = (int)Mathf.Clamp(f, -1, 1);
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
		// Get the number of grounded wheels and the normals and velocities of surfaces they're sitting on
		void GetGroundedWheels()
		{
			groundedWheels = 0;
			reallyGroundedWheels = 0;
			wheelContactsVelocity = Vector3.zero;

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
		
		// Check for crashes and play collision sounds
		void OnCollisionEnter(Collision col)
		{
			raceBox.evoModule.Reset();

			if (col.contacts.Length > 0)
			{
				foreach (ContactPoint curCol in col.contacts)
				{
					if (curCol.thisCollider.gameObject.layer != RaceManager.ignoreWheelCastLayer)
					{
						if (Mathf.Abs(Vector3.Dot(curCol.normal, col.relativeVelocity.normalized)) > 0.1f 
							&& col.relativeVelocity.magnitude > 10)
						{
							crashSnd.PlayOneShot(crashClips[UnityEngine.Random.Range(0, crashClips.Length)],
								Mathf.Clamp01(col.relativeVelocity.magnitude * 0.1f));
							crashing = true;
							if (crashing)
								lastCrashingTime = Time.time;
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
						nowCrashing = true;

						if (!scrapeSnd.isPlaying)
							scrapeSnd.Play();
						//play sparks
						sparks.transform.position = curCol.point;
						sparks.transform.rotation = Quaternion.LookRotation(col.relativeVelocity.normalized, curCol.normal);
						if (!sparks.isPlaying)
							sparks.Play();
					}
				}
			}
			crashing = nowCrashing;
			if (crashing)
				lastCrashingTime = Time.time;
			if (!(colliding || crashing))
				scrapeSnd.Stop();
		}
		//IEnumerator Bla()
		//{
		//	yield return new WaitForSeconds(0.1f);
		//}
		void OnCollisionExit(Collision col)
		{
			//colCo = StartCoroutine(Bla());
			crashing = false;
			scrapeSnd.Stop();
		}
		public override void OnDestroy()
		{
			F.I.s_cars.Remove(this);
			SGP_HUD.I.RemoveFromProgressBar(this);
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
			if (CountDownSeq.Countdown > 0)
				return;
			StartCoroutine(followAI.ResetOnTrack());
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
				batteryLoadingSnd.clip = F.I.audioClips["elec" + Mathf.RoundToInt(3 * UnityEngine.Random.value)];
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
		public void KnockoutMe()
		{
			if (F.I.gameMode == MultiMode.Multiplayer)
				KnockoutMeRpc();
			else
				KnockoutMeInternal();
		}
		void KnockoutMeInternal()
		{
			followAI.selfDriving = false;
			SetAccel(0);
			SetBrake(0);
			SetSteer(0);
			raceBox.enabled = false;
		}
		[Rpc(SendTo.Everyone)]
		void KnockoutMeRpc()
		{
			KnockoutMeInternal();
			SGP_HUD.I.infoText.AddMessage(new(tr.name + " ELIMINATED!", BottomInfoType.ELIMINATED));
		}

		public void SetChassis(float mass, float drag, float angularDrag)
		{
			originalMass = mass;
			rb.mass = mass;
			originalDrag = drag;
			rb.drag = drag;
			rb.angularDrag = angularDrag;
		}
	}

	// Class for groups of wheels to check each FixedUpdate
	[Serializable]
	public class WheelCheckGroup
	{
		public Wheel[] wheels;

		public void Activate()
		{
			foreach (Wheel curWheel in wheels)
			{
				curWheel.getContact = true;
			}
		}

		public void Deactivate()
		{
			foreach (Wheel curWheel in wheels)
			{
				curWheel.getContact = false;
			}
		}
	}
}