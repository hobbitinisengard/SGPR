using UnityEngine;
using System.Collections;
using System;
using Unity.Netcode;
using Unity.Collections;
using Unity.Profiling;

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
		static readonly ProfilerMarker OriginalPhysicsStepMarker =
			new ProfilerMarker("RVP.VehicleParent.OriginalPhysicsStep");
		//public Transform roadColParent;
		public Renderer antennaFlag;
		[NonSerialized]
		public Antenna antenna;
		public MeshRenderer[] springRenderers;
		public AudioSource honkerAudio;
		public SampleText sampleText;
		[NonSerialized]
		public BasicInput basicInput;
		[NonSerialized]
		public CarConfig carConfig;
		[NonSerialized]
		public OriginalVehiclePhysics originalVehiclePhysics;
		[NonSerialized]
		public OriginalVehicleCarSetup originalPartsSetup;
		readonly float[] prefabSuspensionTravel = new float[4];
		bool wasKinematicBeforePhysicsSetup;
		public float PrefabSuspensionTravel(int wheelIndex)
		{
			return wheelIndex >= 0 && wheelIndex < prefabSuspensionTravel.Length
				? prefabSuspensionTravel[wheelIndex] : 0;
		}
		public Vector3 WorldAngularVelocity => originalVehiclePhysics != null
			? originalVehiclePhysics.SourceAngularVelocity : Vector3.zero;
		public float SourceEnergy => originalVehiclePhysics != null ? originalVehiclePhysics.Energy : 0;
		public float SourceEnergyPercent => originalVehiclePhysics != null ? originalVehiclePhysics.EnergyPercent : 0;
		public float SourceEnergyThreshold => originalVehiclePhysics != null ? originalVehiclePhysics.EnergyThreshold : 0;
		public void UseOriginalPhysics(OriginalVehiclePhysicsConfig config)
		{
			if (!OriginalVehiclePhysics.IsUsable(config))
				throw new InvalidOperationException($"Missing or invalid original vehicle physics data: {config?.sourceConfig ?? carConfig?.name}");
			originalVehiclePhysics = new OriginalVehiclePhysics(this, config);
		}
		public void RefreshOriginalPhysicsParameters()
		{
			originalVehiclePhysics?.RefreshParameters();
		}
		/// <summary>
		/// from 0 ti 19
		/// </summary>
		public int carNumber;
		[NonSerialized]
		public Ghost ghost;
		public GameObject bodyObj;
		[System.NonSerialized]
		public Rigidbody rb;
		[System.NonSerialized]
		public Transform tr;
		[System.NonSerialized]
		public Transform norm; // Normal orientation object

		/// <summary>
		/// This can't be set to random.
		/// </summary>
		NetworkVariable<Livery> _sponsor = new(Livery.Random);

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
		public Vector3 worldCOM { get; private set; }

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
					{
						if (name == F.I.playerData.playerName)
						{
							// pick random from unlocked liveries
							var pickedLivery = F.I.unlockedLiveries.GetRandom();
							if (pickedLivery == null || pickedLivery == Livery.Random)
								value = F.I.cars[carNumber].defaultLivery;
							else
								value = pickedLivery.Value;
						}
						else
						{
							value = F.RandomLivery();
						}
					}
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

		Material rearLightsLighter;
		Material rearLightsDarker;
		public GasMotor engine;
		public Transform batteryLoadingParticleSystemParent;


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
		public bool reversing => originalVehiclePhysics != null && originalVehiclePhysics.CurrentGear == 0;
		[Tooltip("convention for placing wheels is FL, FR, RL, RR")]
		public Wheel[] wheels;
		[System.NonSerialized]
		public int groundedWheels; // Number of wheels grounded
		public int reallyGroundedWheels; // Number of really grounded wheels (cars can steer in air)
		[System.NonSerialized]
		public Vector3 wheelNormalAverage; // Average normal of the wheel contact points
		Vector3 wheelContactsVelocity; // Average velocity of wheel contact points

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

		[Header("Steering wheel")]
		public SteeringControl steeringControl;
		/// <summary>
		/// touching anything
		/// </summary>
		public bool colliding;
		private Coroutine colCo;
		public bool crashing; // serious impact
		[NonSerialized]
		public GameObject customCam;
		public bool Owner { get { return IsOwner || F.I.gameMode != GameMode.Multiplayer; } }
		[NonSerialized]
		public int lastRoundScore;
		[Rpc(SendTo.SpecifiedInParams)]
		public void RelinquishRpc(RpcParams ps)
		{
			NetworkObject.RemoveOwnership();
			Destroy(gameObject);
		}
		[Rpc(SendTo.SpecifiedInParams)]
		public void RequestRaceboxValuesRpc(RpcParams ps)
		{
			SynchRaceboxValuesRpc(raceBox.enabled, ServerC.I.PlayerMe.ScoreGet(), raceBox.curLap, followAI.dist, followAI.progress, raceBox.Aero, raceBox.Drift,
				(float)raceBox.bestLapTime.TotalSeconds, (float)raceBox.raceTime.TotalSeconds,
				RpcTarget.Single(ps.Receive.SenderClientId, RpcTargetUse.Temp));
		}
		[Rpc(SendTo.SpecifiedInParams)]
		public void SynchRaceboxValuesRpc(bool enabled, int lastRoundScore, int curLap, float dist, float progress, float aero, float drift,
			float bestLapSecs, float raceTimeSecs, RpcParams ps)
		{
			this.lastRoundScore = lastRoundScore;
			raceBox.UpdateValues(enabled, curLap, dist, progress, aero, drift, bestLapSecs, raceTimeSecs);
			ResultsView.Add(this);
		}
		public FollowAI followAI { get; private set; }
		public RaceBox raceBox { get; private set; }

		int roadSurfaceType;
		[NonSerialized]
		public float tyresOffroad;

		public CatchupStatus catchupStatus { get; private set; }

		private float lastCrashingTime;

		public void SetCatchup(CatchupStatus newStatus)
		{
			catchupStatus = newStatus;
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
			newMat.mainTexture = ImgPathToTexture2D(F.I.documentsSGPRpath + "textures/" + matName + ".jpg");

			rearLightsLighter.mainTexture = newMat.mainTexture;
			rearLightsLighter.SetTexture("_EmissionMap", newMat.mainTexture);
			rearLightsLighter.SetColor("_EmissionColor", Color.red);
			//rearLightsBrakeMaterial.color = new(1, 18/255f, 0);
			rearLightsDarker.mainTexture = newMat.mainTexture;
			// set emission texture
			rearLightsDarker.SetTexture("_EmissionMap", newMat.mainTexture);
			rearLightsDarker.SetColor("_EmissionColor", Color.gray);

			// assign to body
			mr.material = newMat;
			// assign to wheels
			foreach (var w in wheels)
			{
				var wmr = w.transform.GetChild(0).GetComponent<MeshRenderer>();
				var mats = wmr.materials;
				// by convention first material is liverable, second is tyre texture
				for (int i = 0; i < mats.Length; i++)
				{
					if (mats[i].name.Contains("grid"))
					{
						mats[i] = newMat;
					}
				}
				wmr.materials = mats;

			}
			ApplyOriginalTyreMaterial();
			// assign to antennas
			var anchor = bodyObj.transform.GetChild(0);
			for (int i = 0; i < anchor.childCount; i++)
			{
				if (anchor.GetChild(i).TryGetComponent<MeshRenderer>(out var amr))
				{
					var mats = amr.materials;
					for (int j = 0; j < mats.Length; j++)
					{
						if (mats[j].name.Contains("grid"))
						{
							mats[j] = newMat;
						}
					}
					amr.materials = mats;
				}
			}

			// assign to flag
			if (antennaFlag != null)
				antennaFlag.material = newMat;

			RaceManager.I.hud.AddToProgressBar(this);
			sampleText.textMesh.color = F.ReadColor(sponsor);
		}
		public void ApplyOriginalPartPresentation()
		{
			OriginalVehiclePartSlot engineSlot = originalPartsSetup?.GetSlot(OriginalVehiclePartType.Engine);
			if (engine && engineSlot != null)
			{
				var part = F.I.originalVehiclePartCatalog.GetPart(engineSlot.SelectedPartId);
				engine.SetPartAudio(part != null && part.IsUserPart ? part.presentationIndex : engineSlot.selectedIndex);
			}
			OriginalVehiclePartSlot hornSlot = originalPartsSetup?.GetSlot(OriginalVehiclePartType.Horn);
			if (hornSlot != null)
			{
				var part = F.I.originalVehiclePartCatalog.GetPart(hornSlot.SelectedPartId);
				SetHonkerAudio(part != null && part.IsUserPart ? part.presentationIndex : hornSlot.selectedIndex);
			}
			ApplyOriginalTyreMaterial();
		}
		void ApplyOriginalTyreMaterial()
		{
			OriginalVehicleCarSetup setup = originalPartsSetup ?? carConfig?.originalParts ??
				F.I.cars[carNumber].config?.originalParts ?? F.I.cars[carNumber].defaultOriginalParts;
			OriginalVehiclePartSlot slot = setup?.GetSlot(OriginalVehiclePartType.Tyres);
			if (slot == null || slot.selectedIndex <= 0 || wheels == null) return;
			OriginalVehiclePartDefinition part = F.I.originalVehiclePartCatalog.GetPresentationPart(
				F.I.originalVehiclePartCatalog.GetPart(slot.SelectedPartId));
			if (part == null) return;
			int tread = Mathf.RoundToInt(part.GetParameter("Tread", -99999));
			if (tread <= 0) return;
			Material tyreMaterial = Resources.Load<Material>("materials/carModels/Materials/cars_misc_tyre" + tread);
			if (!tyreMaterial) return;
			foreach (Wheel wheel in wheels)
			{
				if (!wheel) continue;
				foreach (MeshRenderer renderer in wheel.GetComponentsInChildren<MeshRenderer>(true))
				{
					Material[] materials = renderer.sharedMaterials;
					bool changed = false;
					for (int i = 0; i < materials.Length; i++)
						if (materials[i] && materials[i].name.IndexOf("tyre", StringComparison.OrdinalIgnoreCase) >= 0 &&
							materials[i] != tyreMaterial)
						{
							materials[i] = tyreMaterial;
							changed = true;
						}
					if (changed) renderer.sharedMaterials = materials;
				}
			}
		}
		private Texture2D ImgPathToTexture2D(string imgPath)
		{
			byte[] pngBytes = System.IO.File.ReadAllBytes(imgPath);
			Texture2D tex = new Texture2D(1024, 1024);
			tex.LoadImage(pngBytes);
			return tex;
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
			sampleText.gameObject.SetActive(!F.I.s_spectator && F.I.gameMode == GameMode.Multiplayer && RaceManager.I.playerCar != this);
		}

		private void NetworkManager_OnTransportFailure()
		{
			RaceManager.I.ExitButton();
		}

		public void PlayBatteryLoadingFXs(bool status)
		{
			if (status)
			{
				batteryLoadingParticleSystemParent.GetComponent<ParticleSystem>().Play();
				batteryLoadingParticleSystemParent.GetChild(0).GetComponent<ParticleSystem>().Play();
				batteryLoadingSnd.Play();
			}
			else
			{
				batteryLoadingSnd.Stop();
				batteryLoadingParticleSystemParent.GetComponent<ParticleSystem>().Stop();
				batteryLoadingParticleSystemParent.GetChild(0).GetComponent<ParticleSystem>().Stop();
			}
		}
		private void Awake()
		{
			if (wheels != null)
				for (int i = 0; i < prefabSuspensionTravel.Length && i < wheels.Length; i++)
					if (wheels[i])
					{
						Suspension suspension = wheels[i].transform.parent.GetComponent<Suspension>();
						if (suspension)
							prefabSuspensionTravel[i] = suspension.suspensionDistance;
					}
			ghost = GetComponent<Ghost>();
			followAI = GetComponent<FollowAI>();
			raceBox = GetComponent<RaceBox>();
			tr = transform;
			rb = GetComponent<Rigidbody>();
			wasKinematicBeforePhysicsSetup = rb.isKinematic;
			// Keep the car stationary until its original physics config has loaded.
			rb.isKinematic = true;
			rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
			//for (int i = 0; i < roadColParent.childCount; i++)
			//	roadColParent.GetChild(i).GetComponent<CapsuleCollider>().hasModifiableContacts = true;

			F.I.s_cars.Add(this);

			rearLightsLighter = new Material(Resources.Load<Material>($"materials/rearlights/lighter/cars_car{carNumber + 1}_b{carNumber + 1}grid1l"));
			rearLightsDarker = new Material(Resources.Load<Material>($"materials/rearlights/darker/cars_car{carNumber + 1}_b{carNumber + 1}grid1d"));
		}
		public override void OnNetworkSpawn()
		{
			base.OnNetworkSpawn();
			Initialize();
		}

		private void Start()
		{
			if (F.I.gameMode != GameMode.Multiplayer)
				Initialize();
		}

		void Initialize()
		{

			Color c = F.RandomColor();
			foreach (var s in springRenderers)
				s.material.color = c;


			// Create normal orientation object
			GameObject normTemp = new(tr.name + "'s Normal");
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

			bool isLocalPlayer = Owner && string.Equals(name,
				F.I.playerData.playerName, StringComparison.Ordinal);
			bool isCpuCar = !isLocalPlayer && name != null && name.Length > 2 &&
				char.IsDigit(name[2]);
			followAI.SetCPU(isCpuCar);

			yield return new WaitForSeconds(.5f); // wait for all the components to load

			carConfig = Championships.Active && isCpuCar ? Championships.OpponentConfig(carNumber) :
				new CarConfig(F.I.cars[carNumber].config);
			carConfig.Apply(this);
			rb.isKinematic = wasKinematicBeforePhysicsSetup;

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
			else if (F.I.gameMode == GameMode.Multiplayer)
			{
				ServerC.I.ReadySet(PlayerState.InRace);
				ServerC.I.UpdatePlayerData();
			}
			ResultsView.Add(this);
			lightsInput = F.I.s_timeOfDay == TimeOfDay.Night;
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

			if (wheels[2].curSurfaceType != roadSurfaceType)
			{
				roadSurfaceType = wheels[2].curSurfaceType;
				roadNoiseSnd.clip = GroundSurfaceMaster.surfaceTypesStatic[roadSurfaceType].roadNoise;
			}
			roadNoiseSnd.gameObject.SetActive((!F.I.gamePaused && reallyGroundedWheels > 0));
			roadNoiseSnd.volume = Mathf.InverseLerp(0, 80,
				(GroundSurfaceMaster.surfaceTypesStatic[roadSurfaceType].alwaysScrape ? 10 : 1) * velMag);// (1 + 80 * 2 / 3f * Mathf.Log10(volume)); 

			if (brakeInput > 0 && !reversing)
			{
				// brake lights
				foreach (var l in rearLights)
				{
					l.SetActive(true);
					l.GetComponent<MeshRenderer>().sharedMaterial = rearLightsLighter;
					//l.transform.GetChild(0).GetComponent<Light>().range = 10;
				}
			}
			else if (brakeInput == 0)
			{
				// no brake lights
				foreach (var l in rearLights)
				{
					l.SetActive(lightsInput);
					l.GetComponent<MeshRenderer>().sharedMaterial = rearLightsDarker;
					//l.transform.GetChild(0).GetComponent<Light>().range = 2;
				}
			}

			// Norm orientation visualizing
			// Debug.DrawRay(norm.position, norm.forward, Color.blue);
			// Debug.DrawRay(norm.position, norm.up, Color.green);
			// Debug.DrawRay(norm.position, norm.right, Color.red);
		}
		public void FixedUpdate()
		{
			GetGroundedWheels();

			prevVel = localVelocity;
			localVelocity = tr.InverseTransformDirection(rb.linearVelocity - wheelContactsVelocity);
			acceleration = localVelocity - prevVel;

			localAngularVel = tr.InverseTransformDirection(WorldAngularVelocity);

			velMag = rb.linearVelocity.magnitude;

			sqrVelMag = rb.linearVelocity.sqrMagnitude;
			forwardDir = tr.forward;
			rightDir = tr.right;
			upDir = tr.up;
			forwardDot = Vector3.Dot(forwardDir, RaceManager.worldUpDir);
			rightDot = Vector3.Dot(rightDir, RaceManager.worldUpDir);
			upDot = Vector3.Dot(upDir, RaceManager.worldUpDir);
			worldCOM = rb.worldCenterOfMass;
			norm.transform.SetPositionAndRotation(tr.position, Quaternion.LookRotation(reallyGroundedWheels == 0 ? upDir : wheelNormalAverage, forwardDir));
			if (originalVehiclePhysics != null)
			{
				using (OriginalPhysicsStepMarker.Auto())
					originalVehiclePhysics.Step();
			}
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
		/// Zero disables the horn; installed horns use hornloop01 through hornloop06.
		/// </summary>
		public void SetHonkerAudio(int type)
		{
			if (!honkerAudio) return;
			AudioClip clip = type <= 0 ? null : Resources.Load<AudioClip>("sfx/hornloop" + Mathf.Clamp(type, 1, 6).ToString("D2"));
			if (honkerAudio.clip == clip) return;
			bool resume = honkerAudio.isPlaying;
			honkerAudio.Stop();
			honkerAudio.clip = clip;
			if (resume && clip) honkerAudio.Play();
		}
		// Set accel input
		public void SetAccel(float f)
		{
			f = Mathf.Clamp(f, -1, 1);

			if (Owner)
				accelInput = f;
		}

		// Set brake input
		public void SetBrake(float f)
		{
			brakeInput = Mathf.Clamp01(f);
		}
		public void SetSteer(float f)
		{
			steerInput = Mathf.Clamp(f, -1, 1);
		}
		// Set ebrake input
		public void SetEbrake(float f)
		{
			ebrakeInput = Mathf.Clamp01(f);
		}
		public void SetBoost(bool b)
		{
			SetBoost(b ? 1 : 0);
		}
		public void SetBoost(int b)
		{
			boostButton = Mathf.Clamp(b, 0, 1);
		}
		public void SetSGPShift(int b)
		{
			SGPshiftbutton = b;
			originalVehiclePhysics?.SetStuntButton(b);
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

			if (originalVehiclePhysics != null)
			{
				// Source contact grace counters define the original grounded class;
				// wheel visuals can update a fixed step before or after this method.
				groundedWheels = originalVehiclePhysics.GroundedWheelCount;
				reallyGroundedWheels = originalVehiclePhysics.ReallyGroundedWheelCount;
				wheelContactsVelocity = Vector3.zero;
			}
		}

		public void PlaySparks(Collision c)
		{
			sparks.transform.position = c.GetContact(0).point;
			sparks.transform.rotation = Quaternion.LookRotation(c.relativeVelocity.normalized, c.GetContact(0).normal);
			sparks.Play();
		}
		public void StopSparks()
		{
			sparks.Stop();
		}
		// Check for crashes and play collision sounds
		void OnCollisionEnter(Collision col)
		{
			originalVehiclePhysics?.ResolveBodyContact(col, true);
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

						//if (sparks && playCrashSparks)
						//{
						//	PlaySparks(col);
						//}
					}
				}
			}
		}
		// Continuous collision checking
		void OnCollisionStay(Collision col)
		{
			originalVehiclePhysics?.ResolveBodyContact(col, false);
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
			SGP_HUD.I?.RemoveFromProgressBar(this);
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

		internal void ResetOnTrack()
		{
			if (CountDownSeq.Countdown > 0)
				return;
			StartCoroutine(followAI.ResetOnTrack());
		}
		public void RefuelSourceEnergy()
		{
			if (!batteryLoadingSnd.isPlaying)
			{
				batteryLoadingSnd.clip = F.I.audioClips["elec" + Mathf.RoundToInt(3 * UnityEngine.Random.value)];
				batteryLoadingSnd.Play();
			}
			if (originalVehiclePhysics != null)
				originalVehiclePhysics.TransferSourceTunnelEnergy(originalVehiclePhysics.SourceRefuelRate * Time.deltaTime);
		}

		public void AddSourceStuntEnergyAward()
		{
			originalVehiclePhysics?.AddSourceStuntEnergyAward();
		}
		public void ApplySourceRespawnEnergyCost()
		{
			originalVehiclePhysics?.ApplySourceRespawnEnergyCost();
		}
		public void KnockoutMe()
		{
			if (F.I.gameMode == GameMode.Multiplayer)
				KnockoutMeRpc();
			else
				KnockoutMeInternal();
		}
		void KnockoutMeInternal()
		{
			if (F.I.s_raceType == RaceType.Survival) raceBox.MarkSurvivalEliminated();
			SetAccel(0);
			SetBrake(0);
			SetSteer(0);
			raceBox.enabled = false;
		}
		[Rpc(SendTo.Everyone)]
		void KnockoutMeRpc()
		{
			KnockoutMeInternal();
			SGP_HUD.I.infoText.AddMessage(new(tr.name + " " + F.I.LocStr("ELIMINATED!"), BottomInfoType.ELIMINATED));
		}

	}
}
