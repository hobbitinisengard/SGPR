using System;
using UnityEngine;

namespace RVP
{
	[RequireComponent(typeof(DriveForce))]
	[ExecuteInEditMode]
	[DisallowMultipleComponent]
	[AddComponentMenu("RVP/Drivetrain/Wheel", 1)]

	// Class for the wheel
	public class Wheel : MonoBehaviour
	{
		[NonSerialized]
		public int curSurfaceType = 0;
		[NonSerialized]
		public Transform tr;
		[System.NonSerialized]
		public bool sliding;
		Rigidbody rb;
		[System.NonSerialized]
		public VehicleParent vp;
		[System.NonSerialized]
		public Suspension suspensionParent;
		[System.NonSerialized]
		public Transform rim;
		Transform tire;
		Vector3 localVel;

		[Tooltip("Generate a sphere collider to represent the wheel for side collisions")]
		public bool generateHardCollider = false;
		SphereCollider sphereCol; // Hard collider
		Transform sphereColTr; // Hard collider transform

		[Header("Rotation")]

		[Tooltip("Bias for feedback RPM lerp between target RPM and raw RPM")]
		[Range(0, 1)]
		public float feedbackRpmBias;

		[Tooltip("Curve for setting final RPM of wheel based on driving torque/brake force, x-axis = torque/brake force, y-axis = lerp between raw RPM and target RPM")]
		public AnimationCurve rpmBiasCurve = AnimationCurve.Linear(0, 0, 1, 1);

		[Tooltip("As the RPM of the wheel approaches this value, the RPM bias curve is interpolated with the default linear curve")]
		public float rpmBiasCurveLimit = Mathf.Infinity;

		[Range(0, 10)]
		public float axleFriction;

		[Header("Friction")]
		float frictionSmoothness = 0.5f;
		/// <summary>
		/// To set basic frictions use SetInitFrictions()
		/// </summary>
		public float forwardFriction = 1;
		/// <summary>
		/// To set basic frictions use SetInitFrictions()
		/// </summary>
		public float sidewaysFriction = 1;
		public float initForwardFriction { get; private set; }
		public float initSidewaysFriction { get; private set; }
		public void SetInitFrictions(float forward, float sideways, float initfrictionStretch)
		{
			initForwardFriction = forward;
			forwardFriction = forward;
			initSidewaysFriction = sideways;
			sidewaysFriction = sideways;
			initFrictionStretch = initfrictionStretch;
			forwardCurveStretch = initfrictionStretch;
			sidewaysCurveStretch = initfrictionStretch;
		}

		public float forwardRimFriction = 0.5f;
		public float sidewaysRimFriction = 0.5f;
		public float forwardCurveStretch = 1;
		public float sidewaysCurveStretch = 1;

		public float initFrictionStretch  { get; private set; }
		Vector3 frictionForce = Vector3.zero;
		//static double[] SGPFrictionData = {1.000000, 0.997070, 0.994141, 0.991211, 0.988281, 0.985645, 0.983008, 0.980371,0.977734, 0.975098, 0.972461, 0.969824, 0.967188, 0.957227, 0.947266, 0.937305,0.927344, 0.925488, 0.923633, 0.921777, 0.919922, 0.918067, 0.916211, 0.914356,0.912500, 0.907422, 0.902344, 0.897266, 0.892188, 0.887109, 0.882031, 0.876953,0.871875, 0.860513, 0.849152, 0.837790, 0.826428, 0.818370, 0.810312, 0.802254,0.794196, 0.786138, 0.778079, 0.770021, 0.761963, 0.750097, 0.738231, 0.726366,0.714500, 0.702634, 0.690768, 0.678902, 0.667036, 0.657031, 0.647025, 0.637020,0.627014, 0.617009, 0.607003, 0.596997, 0.586992, 0.580758, 0.574525, 0.568292,0.562058, 0.555825, 0.549591, 0.543358, 0.537125, 0.531591, 0.526058, 0.520525,0.514992, 0.509459, 0.503925, 0.498392, 0.492859, 0.489663, 0.486468, 0.483272,0.480077, 0.476881, 0.473685, 0.470490, 0.467294, 0.462449, 0.457604, 0.452759,0.447914, 0.443069, 0.438224, 0.433379, 0.428534, 0.427097, 0.425659, 0.424222,0.422784, 0.419936, 0.417088, 0.414240, 0.411392, 0.408544, 0.405696, 0.402848,0.400000, 0.396875, 0.393750, 0.390625, 0.387500, 0.384375, 0.381250, 0.378125,0.375000, 0.371524, 0.368048, 0.364572, 0.361096, 0.357620, 0.354144, 0.350668,0.347192, 0.347593, 0.347994, 0.348395, 0.348796, 0.349198, 0.349599, 0.350000};
		// better
		//static double[] SGPFrictionData = { 1, 0.99707, 0.994141, 0.991211, 0.988281, 0.985645, 0.983008, 0.980371, 0.979164257, 0.974241466, 0.969177728, 0.963976612, 0.958641683, 0.953176512, 0.947584665, 0.94186971, 0.936035216, 0.930084751, 0.924021882, 0.917850177, 0.911573204, 0.905194531, 0.898717726, 0.892146358, 0.885483993, 0.878734201, 0.871900548, 0.864986602, 0.857995933, 0.850932107, 0.843798693, 0.836599258, 0.829337371, 0.822016599, 0.81464051, 0.807212672, 0.799736654, 0.792216023, 0.784654347, 0.777055194, 0.769422132, 0.761758728, 0.754068552, 0.74635517, 0.738622151, 0.730873062, 0.723111472, 0.715340949, 0.70756506, 0.699787373, 0.692011456, 0.684240878, 0.676479206, 0.668730008, 0.660996852, 0.653283306, 0.645592938, 0.637929316, 0.630296007, 0.62269658, 0.615134603, 0.607613643, 0.600137269, 0.592709049, 0.58533255, 0.57801134, 0.570748987, 0.563549059, 0.556415125, 0.549350752, 0.542359508, 0.53544496, 0.528610678, 0.521860228, 0.515197179, 0.508625099, 0.502147556, 0.495768117, 0.48949035, 0.483317824, 0.477254106, 0.471302765, 0.465467367, 0.459751482, 0.454158678, 0.448692521, 0.44335658, 0.438154423, 0.433089618, 0.428165732, 0.423386335, 0.418754993, 0.414275274, 0.409950747, 0.40578498, 0.40178154, 0.397943996, 0.394275914, 0.390780864, 0.387462413, 0.384324129, 0.38136958, 0.378602334, 0.376025959, 0.373644023, 0.371460094, 0.369477739, 0.367700527, 0.366132025, 0.364775802, 0.363635426, 0.361, 0.359, 0.357, 0.357, 0.356, 0.354, 0.352, 0.35, 0.349, 0.347192, 0.347593, 0.347994, 0.348395, 0.348796, 0.349198, 0.349599, 0.35 };
		[Tooltip("X-axis = slip, y-axis = friction")]
		static AnimationCurve forwardFrictionCurve;

		[Tooltip("X-axis = slip, y-axis = friction")]
		static AnimationCurve sidewaysFrictionCurve;
		[Tooltip("How much the tire must slip before marks are created")]
		[NonSerialized]
		public float slipThreshold = 0.5f;
		[System.NonSerialized]
		public float forwardSlip;
		//[System.NonSerialized]
		public float sidewaysSlip;
		public enum SlipDependenceMode { dependent, forward, sideways, independent };
		public SlipDependenceMode slipDependence = SlipDependenceMode.sideways;
		[Range(0, 2)]
		public float forwardSlipDependence = 2;
		[Range(0, 2)]
		public float sidewaysSlipDependence = 2;

		[Tooltip("Adjusts how much friction the wheel has based on the normal of the ground surface. X-axis = normal dot product, y-axis = friction multiplier")]
		public AnimationCurve normalFrictionCurve = AnimationCurve.Linear(0, 1, 1, 1);

		[Tooltip("How much the suspension compression affects the wheel friction")]
		[Range(0, 1)]
		public float compressionFrictionFactor = 0.5f;

		[Header("Size")]

		public float tireRadius;
		public float rimRadius;
		public float tireWidth;
		public float rimWidth;

		[System.NonSerialized]
		public float setTireWidth;
		[System.NonSerialized]
		public float tireWidthPrev;
		[System.NonSerialized]
		public float setTireRadius;
		[System.NonSerialized]
		public float tireRadiusPrev;

		[System.NonSerialized]
		public float setRimWidth;
		[System.NonSerialized]
		public float rimWidthPrev;
		[System.NonSerialized]
		public float setRimRadius;
		[System.NonSerialized]
		public float rimRadiusPrev;

		[System.NonSerialized]
		public float actualRadius;

		[Header("Tire")]

		[Range(0, 1)]
		public float tirePressure = 1;
		[System.NonSerialized]
		public float setTirePressure;
		[System.NonSerialized]
		public float tirePressurePrev;
		float initialTirePressure;
		public bool popped;
		[System.NonSerialized]
		public bool setPopped;
		[System.NonSerialized]
		public bool poppedPrev;
		public bool canPop;

		[Tooltip("Requires deform shader")]
		public float deformAmount;
		Material rimMat;
		Material tireMat;
		float airLeakTime = -1;

		[Range(0, 1)]
		public float rimGlow;
		float glowAmount;
		Color glowColor;

		[System.NonSerialized]
		public bool updatedSize;
		[System.NonSerialized]
		public bool updatedPopped;

		float currentRPM;
		[System.NonSerialized]
		public DriveForce targetDrive;
		///<summary> RPM based purely on velocity</summary>
		[System.NonSerialized]
		public float rawRPM; 
		[System.NonSerialized]
		public WheelContact contactPoint = new WheelContact();
		[System.NonSerialized]
		public bool getContact = true; // Should the wheel try to get contact info?
		[System.NonSerialized]
		public bool grounded;
		public bool groundedReally { get; private set; }
		float airTime;
		[System.NonSerialized]
		public float travelDist;
		Vector3 upDir; // Up direction
		float circumference;

		[System.NonSerialized]
		public Vector3 contactVelocity; // Velocity of contact point
		float actualEbrake;
		float actualTargetRPM;
		float actualTorque;

		[System.NonSerialized]
		public Vector3 forceApplicationPoint; // Point at which friction forces are applied

		[Tooltip("Apply friction forces at ground point")]
		public bool applyForceAtGroundContact;

		[Header("Audio")]

		public AudioSource impactSnd;
		public AudioClip[] tireHitClips;
		public AudioClip rimHitClip;
		public AudioClip tireAirClip;
		public AudioClip tirePopClip;

		[Header("Damage")]

		public float detachForce = Mathf.Infinity;
		[System.NonSerialized]
		public float damage;
		public float mass = 0.05f;
		[System.NonSerialized]
		public bool canDetach;
		[System.NonSerialized]
		public bool connected = true;

		public Mesh tireMeshLoose; // Tire mesh for detached wheel collider
		public Mesh rimMeshLoose; // Rim mesh for detached wheel collider
		GameObject detachedWheel;
		GameObject detachedTire;
		MeshCollider detachedCol;
		Rigidbody detachedBody;
		MeshFilter detachFilter;
		MeshFilter detachTireFilter;
		public PhysicMaterial detachedTireMaterial;
		public PhysicMaterial detachedRimMaterial;
		public ParticleSystem airGreenParticleSystem;



		//AnimationCurve GenerateFrictionCurve(bool moreGrip = false)
		//{
		//    Keyframe[] keys = new Keyframe[SGPFrictionData.Length];
		//    for (int i = 0; i < keys.Length; i++)
		//    {
		//        if (moreGrip)
		//            keys[i].time = 0.5f + 0.5f * (i + 1) / keys.Length;
		//        else
		//            keys[i].time = (float)(i + 1) / keys.Length;

		//        keys[i].value = (float)SGPFrictionData[i];
		//    }
		//    return new AnimationCurve(keys);
		//}
		public void SetColliderLayer(int layer)
		{
			gameObject.layer = layer;
			if (sphereColTr)
				sphereColTr.gameObject.layer = layer;
		}
		void Start()
		{
			tr = transform;
			rb = tr.GetTopmostParentComponent<Rigidbody>();
			vp = tr.GetTopmostParentComponent<VehicleParent>();
			forwardFrictionCurve ??= new AnimationCurve(new Keyframe[] { new(0, .35f), new(1, 1) });
			sidewaysFrictionCurve ??= new AnimationCurve(new Keyframe[] { new(0, 0f), new(0.1f, 1), new(1, 0.9f) });
			suspensionParent = tr.parent.GetComponent<Suspension>();
			travelDist = suspensionParent.targetCompression;
			canDetach = detachForce < Mathf.Infinity && Application.isPlaying;
			initialTirePressure = tirePressure;

			if (tr.childCount > 0)
			{
				// Get rim
				rim = tr.GetChild(0);

				// Set up rim glow material
				if (rimGlow > 0 && Application.isPlaying)
				{
					rimMat = new Material(rim.GetComponent<MeshRenderer>().sharedMaterial);
					rimMat.EnableKeyword("_EMISSION");
					rim.GetComponent<MeshRenderer>().sharedMaterial = rimMat;
				}

				// Create detached wheel
				if (canDetach)
				{
					detachedWheel = new GameObject(vp.transform.name + "'s Detached Wheel");
					detachedWheel.layer = LayerMask.NameToLayer("Detachable Part");
					detachFilter = detachedWheel.AddComponent<MeshFilter>();
					detachFilter.sharedMesh = rim.GetComponent<MeshFilter>().sharedMesh;
					MeshRenderer detachRend = detachedWheel.AddComponent<MeshRenderer>();
					detachRend.sharedMaterial = rim.GetComponent<MeshRenderer>().sharedMaterial;
					detachedCol = detachedWheel.AddComponent<MeshCollider>();
					detachedCol.convex = true;
					detachedBody = detachedWheel.AddComponent<Rigidbody>();
					detachedBody.mass = mass;
				}

				// Get tire
				if (rim.childCount > 0)
				{
					tire = rim.GetChild(0);
					if (deformAmount > 0 && Application.isPlaying)
					{
						tireMat = new Material(tire.GetComponent<MeshRenderer>().sharedMaterial);
						tire.GetComponent<MeshRenderer>().sharedMaterial = tireMat;
					}

					// Create detached tire
					if (canDetach)
					{
						detachedTire = new GameObject("Detached Tire");
						detachedTire.transform.parent = detachedWheel.transform;
						detachedTire.transform.localPosition = Vector3.zero;
						detachedTire.transform.localRotation = Quaternion.identity;
						detachTireFilter = detachedTire.AddComponent<MeshFilter>();
						detachTireFilter.sharedMesh = tire.GetComponent<MeshFilter>().sharedMesh;
						MeshRenderer detachTireRend = detachedTire.AddComponent<MeshRenderer>();
						detachTireRend.sharedMaterial = tireMat ? tireMat : tire.GetComponent<MeshRenderer>().sharedMaterial;
					}
				}

				if (Application.isPlaying)
				{
					// Generate hard collider
					if (generateHardCollider)
					{
						GameObject sphereColNew = new ("Rim Collider")
						{
							layer = RaceManager.ignoreWheelCastLayer
						};
						sphereColTr = sphereColNew.transform;
						sphereCol = sphereColNew.AddComponent<SphereCollider>();
						sphereColTr.parent = tr;
						sphereColTr.localPosition = Vector3.zero;
						sphereColTr.localRotation = Quaternion.identity;
						sphereCol.radius = rimRadius;//Mathf.Min(rimWidth * 0.5f, rimRadius * 0.5f);
						sphereCol.sharedMaterial = RaceManager.frictionlessMatStatic;
					}

					if (canDetach)
					{
						detachedWheel.SetActive(false);
					}
				}
			}

			targetDrive = GetComponent<DriveForce>();
			currentRPM = 0;

			if (F.I.s_raceType == RaceType.Drift)
				slipDependence = SlipDependenceMode.independent;
		}

		void FixedUpdate()
		{
			Work(Time.fixedDeltaTime);
		}
		public void Work(float deltaTime)
		{
			upDir = tr.up;
			actualRadius = popped ? rimRadius : Mathf.Lerp(rimRadius, tireRadius, tirePressure);
			circumference = Mathf.PI * actualRadius * 2;
			localVel = rb.GetPointVelocity(forceApplicationPoint);

			// Get proper inputs
			actualEbrake = suspensionParent.ebrakeEnabled ? suspensionParent.ebrakeForce : 0;
			actualTargetRPM = targetDrive.rpm * (suspensionParent.driveInverted ? -1 : 1);
			actualTorque = suspensionParent.driveEnabled ? Mathf.Lerp(targetDrive.torque, Mathf.Abs(vp.accelInput), vp.burnout) : 0;

			if (getContact)
			{
				GetWheelContact(deltaTime);
			}
			else if (grounded)
			{
				contactPoint.point += localVel * deltaTime;
			}

			airTime = grounded ? 0 : airTime + deltaTime;
			forceApplicationPoint = applyForceAtGroundContact ? contactPoint.point : tr.position;

			if (connected)
			{
				GetRawRPM();
				ApplyDrive();
			}
			else
			{
				rawRPM = 0;
				currentRPM = 0;
				targetDrive.feedbackRPM = 0;
			}

			// Get travel distance
			travelDist = (suspensionParent.compression < travelDist || grounded) ? suspensionParent.compression
				: Mathf.Lerp(travelDist, suspensionParent.compression, suspensionParent.extendSpeed * deltaTime);

			PositionWheel();

			if (connected)
			{
				// Update hard collider size upon changed radius or width
				if (generateHardCollider)
				{
					setRimWidth = rimWidth;
					setRimRadius = rimRadius;
					setTireWidth = tireWidth;
					setTireRadius = tireRadius;
					setTirePressure = tirePressure;

					if (rimWidthPrev != setRimWidth || rimRadiusPrev != setRimRadius)
					{
						sphereCol.radius = Mathf.Min(rimWidth * 0.5f, rimRadius * 0.5f);
						updatedSize = true;
					}
					else if (tireWidthPrev != setTireWidth || tireRadiusPrev != setTireRadius || tirePressurePrev != setTirePressure)
					{
						updatedSize = true;
					}
					else
					{
						updatedSize = false;
					}

					rimWidthPrev = setRimWidth;
					rimRadiusPrev = setRimRadius;
					tireWidthPrev = setTireWidth;
					tireRadiusPrev = setTireRadius;
					tirePressurePrev = setTirePressure;
				}

				GetSlip();
				ApplyFriction();
				/*
			// Burnout spinning
			//if (vp.burnout > 0 && targetDrive.rpm != 0 && actualEbrake * vp.ebrakeInput == 0 && connected && grounded)
			//{
			//	rb.AddForceAtPosition(suspensionParent.forwardDir * -suspensionParent.flippedSideFactor *
			//		 (vp.steerInput * vp.burnoutSpin * currentRPM * Mathf.Min(0.1f, targetDrive.torque) * 0.001f)
			//		 * vp.burnout * (popped ? 0.5f : 1) * contactPoint.surfaceFriction, suspensionParent.tr.position
			//		 , vp.wheelForceMode);
			//}

			// Popping logic
			//setPopped = popped;

			//if (poppedPrev != setPopped)
			//{
			//	if (tire)
			//	{
			//		tire.gameObject.SetActive(!popped);
			//	}

			//	updatedPopped = true;
			//}
			//else
			//{
			//	updatedPopped = false;
			//}

			//poppedPrev = setPopped;

			// Air leak logic
			//if (airLeakTime >= 0)
			//{
			//	tirePressure = Mathf.Clamp01(tirePressure - Time.fixedDeltaTime * 0.5f);

			//	if (grounded)
			//	{
			//		airLeakTime += Mathf.Max(Mathf.Abs(currentRPM) * 0.001f, localVel.magnitude * 0.1f) * Time.timeScale * TimeMaster.inverseFixedTimeFactor;

			//		if (airLeakTime > 1000 && tirePressure == 0)
			//		{
			//			popped = true;
			//			airLeakTime = -1;

			//			if (impactSnd && tirePopClip)
			//			{
			//				impactSnd.PlayOneShot(tirePopClip);
			//				impactSnd.pitch = 1;
			//			}
			//		}
			//	}
			//}
			*/

			}
		}
		void LateUpdate()
		{
			RotateWheel();

			if (!Application.isPlaying)
			{
				PositionWheel();
			}
			else
			{
				if (airGreenParticleSystem != null)
				{
					if (!grounded && vp.rb.velocity.y < 0 && !vp.colliding)
					{
						if (!airGreenParticleSystem.isPlaying)
						{
							airGreenParticleSystem.Play();
						}
					}
				}
				//// Update tire and rim materials
				//if (deformAmount > 0 && tireMat && connected)
				//{
				//	if (tireMat.HasProperty("_DeformNormal"))
				//	{
				//		// Deform tire (requires deform shader)
				//		Vector3 deformNormal = grounded ? contactPoint.normal * Mathf.Max(-suspensionParent.penetration * (1 - suspensionParent.compression) * 10, 1 - tirePressure) * deformAmount : Vector3.zero;
				//		tireMat.SetVector("_DeformNormal", new Vector4(deformNormal.x, deformNormal.y, deformNormal.z, 0));
				//	}
				//}

				//if (rimMat)
				//{
				//	if (rimMat.HasProperty("_EmissionColor"))
				//	{
				//		// Make the rim glow
				//		float targetGlow = connected && GroundSurfaceMaster.surfaceTypesStatic[contactPoint.surfaceType].leaveSparks ? Mathf.Abs(F.MaxAbs(forwardSlip, sidewaysSlip)) : 0;
				//		glowAmount = popped ? Mathf.Lerp(glowAmount, targetGlow, (targetGlow > glowAmount ? 2 : 0.2f) * Time.deltaTime) : 0;
				//		glowColor = new Color(glowAmount, glowAmount * 0.5f, 0);
				//		rimMat.SetColor("_EmissionColor", popped ? Color.Lerp(Color.black, glowColor, glowAmount * rimGlow) : Color.black);
				//	}
				//}
			}
		}

		// Use raycasting to find the current contact point for the wheel
		void GetWheelContact(float deltaTime)
		{
			float castDist = Mathf.Max(suspensionParent.suspensionDistance * Mathf.Max(0.001f, suspensionParent.targetCompression) + actualRadius, 0.001f);
			//RaycastHit[] wheelHits = Physics.RaycastAll(transform.position, suspensionParent.springDirection, castDist, RaceManager.wheelCastMaskStatic);
			bool validHit = Physics.Raycast(transform.position, suspensionParent.springDirection, out RaycastHit hit, castDist, RaceManager.wheelCastMaskStatic);
			//bool validHit = Physics.BoxCast(transform.position, new Vector3(.05f,.1f,.05f), suspensionParent.springDirection, out RaycastHit hit, 
			//	 vp.tr.rotation, castDist, RaceManager.wheelCastMaskStatic);
			//bool validHit = false;
			//float hitDist = Mathf.Infinity;

			//if (connected)
			//{
			//	// Loop through raycast hits to find closest one
			//	for (int i = 0; i < wheelHits.Length; i++)
			//	{
			//		if (!wheelHits[i].transform.IsChildOf(vp.tr) && wheelHits[i].distance < hitDist)
			//		{
			//			hitIndex = i;
			//			hitDist = wheelHits[i].distance;
			//			validHit = true;
			//		}
			//	}
			//}
			//else
			//{
			//	validHit = false;
			//}

			// Set contact point variables
			if (validHit)
			{
				//hit = wheelHits[hitIndex];

				if (!grounded && impactSnd && ((tireHitClips.Length > 0 && !popped) || (rimHitClip && popped)))
				{
					impactSnd.PlayOneShot(popped ? rimHitClip : tireHitClips[Mathf.RoundToInt(UnityEngine.Random.Range(0, tireHitClips.Length - 1))], Mathf.Clamp01(airTime * airTime));
					impactSnd.pitch = Mathf.Clamp(airTime * 0.2f + 0.8f, 0.8f, 1);
				}

				grounded = true;
				groundedReally = true;
				contactPoint.distance = hit.distance - actualRadius;
				contactPoint.point = hit.point + localVel * deltaTime;
				contactPoint.grounded = true;
				contactPoint.normal = hit.normal;
				contactPoint.relativeVelocity = tr.InverseTransformDirection(localVel);
				contactPoint.col = hit.collider;

				if (hit.collider.attachedRigidbody)
				{
					contactVelocity = hit.collider.attachedRigidbody.GetPointVelocity(contactPoint.point);
					contactPoint.relativeVelocity -= tr.InverseTransformDirection(contactVelocity);
				}
				else
				{
					contactVelocity = Vector3.zero;
				}
				GroundSurfaceInstance curSurface = hit.collider.GetComponent<GroundSurfaceInstance>();
				TerrainSurface curTerrain = hit.collider.GetComponent<TerrainSurface>();

				if (curSurface)
				{
					if (curSurface.friction == 1)
						contactPoint.surfaceFriction = curSurface.friction;
					else
						contactPoint.surfaceFriction = Mathf.Lerp(curSurface.friction, Mathf.Max(.9f, curSurface.friction), vp.tyresOffroad);

					contactPoint.surfaceType = curSurface.surfaceType;
				}
				else if (curTerrain)
				{
					contactPoint.surfaceType = curTerrain.GetDominantSurfaceTypeAtPoint(contactPoint.point);
					contactPoint.surfaceFriction = curTerrain.GetFriction(contactPoint.surfaceType);
				}
				else
				{
					contactPoint.surfaceFriction = hit.collider.sharedMaterial != null ? hit.collider.sharedMaterial.dynamicFriction * 2 : 1.0f;
					contactPoint.surfaceType = 0;
				}

				//if (canPop && contactPoint.col.CompareTag("Pop Tire") && airLeakTime == -1 && !popped)
				//{
				//	Deflate();
				//}
			}
			else
			{
				//if (vp.followAI.isCPU && !vp.raceBox.evoModule.stunting && !vp.crashing 
				//	&& Vector3.Dot(vp.tr.up,Vector3.up) > 0)
				//{ // steering in air
				//	grounded = true;
				//	groundedReally = false;
				//	contactPoint.distance = suspensionParent.suspensionDistance;
				//	contactPoint.point = suspensionParent.transform.position - suspensionParent.upDir * suspensionParent.suspensionDistance;
				//	contactPoint.grounded = true;
				//	contactPoint.normal = Vector3.up;
				//	contactPoint.relativeVelocity = tr.InverseTransformDirection(localVel);
				//	contactPoint.col = null;
				//	contactVelocity = Vector3.zero;
				//	contactPoint.surfaceFriction = GroundSurfaceMaster.AirSteeringFriction;
				//	contactPoint.surfaceType = GroundSurfaceMaster.AirSteeringSurfaceType;
				//}
				//else
				{
					grounded = false;
					groundedReally = false;
					contactPoint.distance = suspensionParent.suspensionDistance;
					contactPoint.point = Vector3.zero;
					contactPoint.grounded = false;
					contactPoint.normal = upDir;
					contactPoint.relativeVelocity = Vector3.zero;
					contactPoint.col = null;
					contactVelocity = Vector3.zero;
					contactPoint.surfaceFriction = 0;
					contactPoint.surfaceType = 0;
				}
			}
			curSurfaceType = contactPoint.surfaceType;
		}

		// Calculate what the RPM of the wheel would be based purely on its velocity
		void GetRawRPM()
		{
			if (grounded)
			{
				rawRPM = (contactPoint.relativeVelocity.x / circumference) * (Mathf.PI * 100) * -suspensionParent.flippedSideFactor;
			}
			else
			{
				rawRPM = Mathf.Lerp(rawRPM, actualTargetRPM, (actualTorque + suspensionParent.brakeForce * vp.brakeInput + actualEbrake * vp.ebrakeInput) * Time.timeScale);
			}
		}

		// Calculate the current slip amount
		void GetSlip()
		{
			if (grounded)
			{
				sidewaysSlip = (contactPoint.relativeVelocity.z) / sidewaysCurveStretch;
				if (groundedReally)
					forwardSlip = (rawRPM - currentRPM) / forwardCurveStretch;
				else
					forwardSlip = 0;
			}
			else
			{
				sidewaysSlip = 0;
				forwardSlip = 0;
			}
		}

		// Apply actual forces to rigidbody based on wheel simulation
		void ApplyFriction()
		{
			if (grounded)
			{
				float forwardSlipFactor = (int)slipDependence == 0 || (int)slipDependence == 1 ? forwardSlip - sidewaysSlip : forwardSlip;
				float sidewaysSlipFactor = (int)slipDependence == 0 || (int)slipDependence == 2 ? sidewaysSlip - forwardSlip : sidewaysSlip;
				float forwardSlipDependenceFactor = Mathf.Clamp01(forwardSlipDependence - Mathf.Clamp01(Mathf.Abs(sidewaysSlip)));
				float sidewaysSlipDependenceFactor = Mathf.Clamp01(sidewaysSlipDependence - Mathf.Clamp01(Mathf.Abs(forwardSlip)));

				float targetForceX = forwardFrictionCurve.Evaluate(Mathf.Abs(forwardSlipFactor))
					* -System.Math.Sign(forwardSlip) * (popped ? forwardRimFriction : forwardFriction)
					* forwardSlipDependenceFactor * -suspensionParent.flippedSideFactor;
				float targetForceZ = sidewaysFrictionCurve.Evaluate(Mathf.Abs(sidewaysSlipFactor))
					* -System.Math.Sign(sidewaysSlip) * (popped ? sidewaysRimFriction : sidewaysFriction)
					* sidewaysSlipDependenceFactor * normalFrictionCurve.Evaluate(Mathf.Clamp01(Vector3.Dot(contactPoint.normal, RaceManager.worldUpDir))) *
					 (vp.burnout > 0 && Mathf.Abs(targetDrive.rpm) != 0 && actualEbrake * vp.ebrakeInput == 0 && grounded ? (1 - vp.burnout) * (1 - Mathf.Abs(vp.accelInput)) : 1);

				Vector3 targetForce = tr.TransformDirection(targetForceX, 0, targetForceZ);
				float wheelFriction = contactPoint.surfaceFriction;

				float targetForceMultiplier = ((1 - compressionFrictionFactor) + (1 - suspensionParent.compression) * compressionFrictionFactor
					 * Mathf.Clamp01(Mathf.Abs(suspensionParent.tr.InverseTransformDirection(localVel).z) * 2)) * wheelFriction;
				frictionForce = Vector3.Lerp(frictionForce, targetForce * targetForceMultiplier, 1 - frictionSmoothness);
				rb.AddForceAtPosition(frictionForce, forceApplicationPoint, vp.wheelForceMode);

				// If resting on a rigidbody, apply opposing force to it
				if (contactPoint.col && contactPoint.col.attachedRigidbody)
				{
					contactPoint.col.attachedRigidbody.AddForceAtPosition(-frictionForce, contactPoint.point, vp.wheelForceMode);
				}
			}
		}

		// Do torque and RPM calculations/simulation
		void ApplyDrive()
		{
			float brakeForce = 0;
			float brakeCheckValue = suspensionParent.skidSteerBrake ? vp.localAngularVel.y : vp.localVelocity.z;

			// Set brake force
			if (vp.brakeIsReverse)
			{
				if (brakeCheckValue > 0)
				{
					brakeForce = suspensionParent.brakeForce * vp.brakeInput;
				}
				else if (brakeCheckValue <= 0)
				{
					brakeForce = suspensionParent.brakeForce * Mathf.Clamp01(vp.accelInput);
				}
			}
			else
			{
				brakeForce = suspensionParent.brakeForce * vp.brakeInput;
			}

			brakeForce += axleFriction * 0.1f * (Mathf.Approximately(actualTorque, 0) ? 1 : 0);
			if (targetDrive.rpm != 0)
			{
				brakeForce *= (1 - vp.burnout);
			}

			// Set final RPM
			if (!suspensionParent.jammed && connected)
			{
				bool validTorque = (!(Mathf.Approximately(actualTorque, 0) && Mathf.Abs(actualTargetRPM) < 0.01f)
					&& !Mathf.Approximately(actualTargetRPM, 0)) || brakeForce + actualEbrake * vp.ebrakeInput > 0;

				currentRPM = Mathf.Lerp(rawRPM,
						Mathf.Lerp(Mathf.Lerp(rawRPM, actualTargetRPM, validTorque ? EvaluateTorque(actualTorque) : actualTorque),
					 0, Mathf.Max(brakeForce, actualEbrake * vp.ebrakeInput)),
				validTorque ? EvaluateTorque(actualTorque + brakeForce + actualEbrake * vp.ebrakeInput)
				: actualTorque + brakeForce + actualEbrake * vp.ebrakeInput);

				targetDrive.feedbackRPM = Mathf.Lerp(currentRPM, rawRPM, feedbackRpmBias);
			}
			else
			{
				currentRPM = 0;
				targetDrive.feedbackRPM = 0;
			}
		}

		// Extra method for evaluating torque to make the ApplyDrive method more readable
		float EvaluateTorque(float t)
		{
			float torque = Mathf.Lerp(rpmBiasCurve.Evaluate(t), t, rawRPM / (rpmBiasCurveLimit * Mathf.Sign(actualTargetRPM)));
			return torque;
		}

		// Visual wheel positioning
		void PositionWheel()
		{
			if (suspensionParent)
			{
				rim.position = suspensionParent.maxCompressPoint + suspensionParent.springDirection * suspensionParent.suspensionDistance * (Application.isPlaying ? travelDist : suspensionParent.targetCompression) +
					 suspensionParent.upDir * Mathf.Pow(Mathf.Max(Mathf.Abs(Mathf.Sin(suspensionParent.sideAngle * Mathf.Deg2Rad)), Mathf.Abs(Mathf.Sin(suspensionParent.casterAngle * Mathf.Deg2Rad))), 2) * actualRadius +
					 suspensionParent.pivotOffset * suspensionParent.tr.TransformDirection(Mathf.Sin(tr.localEulerAngles.y * Mathf.Deg2Rad), 0, Mathf.Cos(tr.localEulerAngles.y * Mathf.Deg2Rad))
					 - suspensionParent.pivotOffset * (Application.isPlaying ? suspensionParent.forwardDir : suspensionParent.tr.forward);
			}

			if (Application.isPlaying && generateHardCollider && connected)
			{
				sphereColTr.position = rim.position;
			}
		}

		// Visual wheel rotation
		void RotateWheel()
		{
			if (tr && suspensionParent)
			{
				float ackermannVal = Mathf.Sign(suspensionParent.steerAngle) == suspensionParent.flippedSideFactor ? 1 + suspensionParent.ackermannFactor : 1 - suspensionParent.ackermannFactor;
				
					tr.localEulerAngles = new Vector3(
				 suspensionParent.camberAngle + suspensionParent.casterAngle * suspensionParent.steerAngle * suspensionParent.flippedSideFactor,
				 -suspensionParent.toeAngle * suspensionParent.flippedSideFactor + suspensionParent.steerDegrees * ackermannVal,
				 0);
			}

			if (Application.isPlaying)
			{
				rim.Rotate(Vector3.forward, currentRPM * suspensionParent.flippedSideFactor * Time.deltaTime);

				if (damage > 0)
				{
					rim.localEulerAngles = new Vector3(
						 Mathf.Sin(-rim.localEulerAngles.z * Mathf.Deg2Rad) * Mathf.Clamp(damage, 0, 10),
						 Mathf.Cos(-rim.localEulerAngles.z * Mathf.Deg2Rad) * Mathf.Clamp(damage, 0, 10),
						 rim.localEulerAngles.z);
				}
				else if (rim.localEulerAngles.x != 0 || rim.localEulerAngles.y != 0)
				{
					rim.localEulerAngles = new Vector3(0, 0, rim.localEulerAngles.z);
				}
			}
		}

		// Begin deflating the tire/leaking air
		public void Deflate()
		{
			airLeakTime = 0;

			if (impactSnd && tireAirClip)
			{
				impactSnd.PlayOneShot(tireAirClip);
				impactSnd.pitch = 1;
			}
		}

		public void FixTire()
		{
			popped = false;
			tirePressure = initialTirePressure;
			airLeakTime = -1;
		}

		// Detach the wheel from the vehicle
		public void Detach()
		{
			if (connected && canDetach)
			{
				connected = false;
				detachedWheel.SetActive(true);
				detachedWheel.transform.position = rim.position;
				detachedWheel.transform.rotation = rim.rotation;
				detachedCol.sharedMaterial = popped ? detachedRimMaterial : detachedTireMaterial;

				if (tire)
				{
					detachedTire.SetActive(!popped);
					detachedCol.sharedMesh = airLeakTime >= 0 || popped ? (rimMeshLoose ? rimMeshLoose : detachFilter.sharedMesh) : (tireMeshLoose ? tireMeshLoose : detachTireFilter.sharedMesh);
				}
				else
				{
					detachedCol.sharedMesh = rimMeshLoose ? rimMeshLoose : detachFilter.sharedMesh;
				}

				rb.mass -= mass;
				detachedBody.velocity = rb.GetPointVelocity(rim.position);
				detachedBody.angularVelocity = rb.angularVelocity;

				rim.gameObject.SetActive(false);

				if (sphereColTr)
				{
					sphereColTr.gameObject.SetActive(false);
				}
			}
		}

		// Automatically sets wheel dimensions based on rim/tire meshes
		public void GetWheelDimensions(float radiusMargin, float widthMargin)
		{
			Mesh rimMesh = null;
			Mesh tireMesh = null;
			Mesh checker;
			Transform scaler = transform;

			if (transform.childCount > 0)
			{
				if (transform.GetChild(0).GetComponent<MeshFilter>())
				{
					rimMesh = transform.GetChild(0).GetComponent<MeshFilter>().sharedMesh;
					scaler = transform.GetChild(0);
				}

				if (transform.GetChild(0).childCount > 0)
				{
					if (transform.GetChild(0).GetChild(0).GetComponent<MeshFilter>())
					{
						tireMesh = transform.GetChild(0).GetChild(0).GetComponent<MeshFilter>().sharedMesh;
					}
				}

				checker = tireMesh ? tireMesh : rimMesh;

				if (checker)
				{
					float maxWidth = 0;
					float maxRadius = 0;

					foreach (Vector3 curVert in checker.vertices)
					{
						if (new Vector2(curVert.x * scaler.localScale.x, curVert.y * scaler.localScale.y).magnitude > maxRadius)
						{
							maxRadius = new Vector2(curVert.x * scaler.localScale.x, curVert.y * scaler.localScale.y).magnitude;
						}

						if (Mathf.Abs(curVert.z * scaler.localScale.z) > maxWidth)
						{
							maxWidth = Mathf.Abs(curVert.z * scaler.localScale.z);
						}
					}

					tireRadius = maxRadius + radiusMargin;
					tireWidth = maxWidth + widthMargin;

					if (tireMesh && rimMesh)
					{
						maxWidth = 0;
						maxRadius = 0;

						foreach (Vector3 curVert in rimMesh.vertices)
						{
							if (new Vector2(curVert.x * scaler.localScale.x, curVert.y * scaler.localScale.y).magnitude > maxRadius)
							{
								maxRadius = new Vector2(curVert.x * scaler.localScale.x, curVert.y * scaler.localScale.y).magnitude;
							}

							if (Mathf.Abs(curVert.z * scaler.localScale.z) > maxWidth)
							{
								maxWidth = Mathf.Abs(curVert.z * scaler.localScale.z);
							}
						}

						rimRadius = maxRadius + radiusMargin;
						rimWidth = maxWidth + widthMargin;
					}
					else
					{
						rimRadius = maxRadius * 0.5f + radiusMargin;
						rimWidth = maxWidth * 0.5f + widthMargin;
					}
				}
				else
				{
					Debug.LogError("No rim or tire meshes found for getting wheel dimensions.", this);
				}
			}
		}

		// Attach the wheel back onto its vehicle if detached
		public void Reattach()
		{
			if (!connected)
			{
				connected = true;
				detachedWheel.SetActive(false);
				rb.mass += mass;
				rim.gameObject.SetActive(true);

				if (sphereColTr)
				{
					sphereColTr.gameObject.SetActive(true);
				}
			}
		}

		// visualize wheel
		void OnDrawGizmosSelected()
		{
			tr = transform;

			if (tr.childCount > 0)
			{
				// Rim is the first child of this object
				rim = tr.GetChild(0);

				// Tire mesh should be first child of rim
				if (rim.childCount > 0)
				{
					tire = rim.GetChild(0);
				}
			}

			float tireActualRadius = Mathf.Lerp(rimRadius, tireRadius, tirePressure);

			if (tirePressure < 1 && tirePressure > 0)
			{
				Gizmos.color = new Color(1, 1, 0, popped ? 0.5f : 1);
				GizmosExtra.DrawWireCylinder(rim.position, rim.forward, tireActualRadius, tireWidth * 2);
			}

			Gizmos.color = Color.white;
			GizmosExtra.DrawWireCylinder(rim.position, rim.forward, tireRadius, tireWidth * 2);

			Gizmos.color = tirePressure == 0 || popped ? Color.green : Color.cyan;
			GizmosExtra.DrawWireCylinder(rim.position, rim.forward, rimRadius, rimWidth * 2);

			Gizmos.color = new Color(1, 1, 1, tirePressure < 1 ? 0.5f : 1);
			GizmosExtra.DrawWireCylinder(rim.position, rim.forward, tireRadius, tireWidth * 2);

			Gizmos.color = tirePressure == 0 || popped ? Color.green : Color.cyan;
			GizmosExtra.DrawWireCylinder(rim.position, rim.forward, rimRadius, rimWidth * 2);
		}

		// Destroy detached wheel
		void OnDestroy()
		{
			if (Application.isPlaying)
			{
				if (detachedWheel)
				{
					Destroy(detachedWheel);
				}
			}
		}
	}


	// Contact point class
	public class WheelContact
	{
		public bool grounded; // Is the contact point grounded?
		public Collider col; // The collider of the contact point
		public Vector3 point; // The position of the contact point
		public Vector3 normal; // The normal of the contact point
		public Vector3 relativeVelocity; // Relative velocity between the wheel and the contact point object
		public float distance; // Distance from the suspension to the contact point minus the wheel radius
		public float surfaceFriction; // Friction of the contact surface
		public int surfaceType; // The surface type identified by the surface types array of GroundSurfaceMaster
	}
}
