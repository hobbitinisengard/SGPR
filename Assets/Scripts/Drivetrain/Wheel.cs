using System;
using UnityEngine;

namespace RVP
{
	[ExecuteInEditMode]
	[DisallowMultipleComponent]
	[AddComponentMenu("RVP/Drivetrain/Wheel", 1)]
	public class Wheel : MonoBehaviour
	{
		[NonSerialized] public int curSurfaceType;
		[NonSerialized] public Transform tr;
		[NonSerialized] public bool sliding;
		[NonSerialized] public float originalGripUsage;
		[NonSerialized] public VehicleParent vp;
		[NonSerialized] public Suspension susParent;
		[NonSerialized] public Transform rim;

		Transform tire;
		Rigidbody rb;
		MeshFilter[] visualWheelMeshes;
		bool originalTireRadiusChecked;
		float airTime;
		float initialTirePressure;

		[Header("Original tyre telemetry")]
		[NonSerialized] public float slipThres = 0.5f;
		public float forwardSlip;
		[NonSerialized] public float sidewaysSlip;

		[Header("Size")]
		public float tireRadius;
		public float rimRadius;
		public float tireWidth;
		public float rimWidth;
		[NonSerialized] public float actualRadius;
		[NonSerialized] public float travelDist;

		[Header("Tire")]
		[Range(0, 1)] public float tirePressure = 1;
		public bool popped;
		public bool canPop;
		public float deformAmount;
		public float rimGlow;

		[Header("Audio")]
		public AudioSource impactSnd;
		public AudioClip[] tireHitClips;
		public AudioClip rimHitClip;
		public AudioClip tireAirClip;
		public AudioClip tirePopClip;

		[Header("Damage")]
		public float detachForce = Mathf.Infinity;
		[NonSerialized] public float damage;
		public float mass = 0.05f;
		[NonSerialized] public bool canDetach;
		[NonSerialized] public bool connected = true;
		public Mesh tireMeshLoose;
		public Mesh rimMeshLoose;
		public PhysicsMaterial detachedTireMaterial;
		public PhysicsMaterial detachedRimMaterial;
		public ParticleSystem airGreenParticleSystem;

		[NonSerialized] public float currentRPM;
		[NonSerialized] public float rawRPM;
		[NonSerialized] public WheelContact contactPoint = new();
		[NonSerialized] public bool grounded;
		public bool groundedReally { get; private set; }
		[NonSerialized] public Vector3 contactVelocity;

		GameObject detachedWheel;
		GameObject detachedTire;
		MeshCollider detachedCol;
		Rigidbody detachedBody;
		MeshFilter detachFilter;
		MeshFilter detachTireFilter;
		Material rimMat;
		Material tireMat;

		public bool isFront => name.Length > 5 && name[5] == 'F';
		public bool isLeft => name.Length > 6 && name[6] == 'L';

		void Awake()
		{
			tr = transform;
			susParent = tr.parent ? tr.parent.GetComponent<Suspension>() : null;
		}

		void Start()
		{
			rb = tr.GetTopmostParentComponent<Rigidbody>();
			vp = tr.GetTopmostParentComponent<VehicleParent>();
			if (!susParent)
				susParent = tr.parent ? tr.parent.GetComponent<Suspension>() : null;
			travelDist = susParent ? susParent.targetCompression : 1;
			initialTirePressure = tirePressure;
			canDetach = detachForce < Mathf.Infinity && Application.isPlaying;

			if (tr.childCount == 0)
				return;

			rim = tr.GetChild(0);
			if (rimGlow > 0 && Application.isPlaying)
			{
				MeshRenderer rimRenderer = rim.GetComponent<MeshRenderer>();
				if (rimRenderer)
				{
					rimMat = new Material(rimRenderer.sharedMaterial);
					rimMat.EnableKeyword("_EMISSION");
					rimRenderer.sharedMaterial = rimMat;
				}
			}

			if (canDetach)
			{
				detachedWheel = new GameObject(vp.transform.name + "'s Detached Wheel");
				detachedWheel.layer = LayerMask.NameToLayer("Detachable Part");
				detachFilter = detachedWheel.AddComponent<MeshFilter>();
				detachFilter.sharedMesh = rim.GetComponent<MeshFilter>().sharedMesh;
				MeshRenderer detachedRimRenderer = detachedWheel.AddComponent<MeshRenderer>();
				detachedRimRenderer.sharedMaterial = rim.GetComponent<MeshRenderer>().sharedMaterial;
				detachedCol = detachedWheel.AddComponent<MeshCollider>();
				detachedCol.convex = true;
				detachedBody = detachedWheel.AddComponent<Rigidbody>();
				detachedBody.mass = mass;
			}

			if (rim.childCount > 0)
			{
				tire = rim.GetChild(0);
				if (deformAmount > 0 && Application.isPlaying)
				{
					MeshRenderer tireRenderer = tire.GetComponent<MeshRenderer>();
					if (tireRenderer)
					{
						tireMat = new Material(tireRenderer.sharedMaterial);
						tireRenderer.sharedMaterial = tireMat;
					}
				}

				if (canDetach)
				{
					detachedTire = new GameObject("Detached Tire");
					detachedTire.transform.SetParent(detachedWheel.transform, false);
					detachTireFilter = detachedTire.AddComponent<MeshFilter>();
					detachTireFilter.sharedMesh = tire.GetComponent<MeshFilter>().sharedMesh;
					MeshRenderer detachedTireRenderer = detachedTire.AddComponent<MeshRenderer>();
					detachedTireRenderer.sharedMaterial = tireMat
						? tireMat : tire.GetComponent<MeshRenderer>().sharedMaterial;
				}
			}

			visualWheelMeshes = rim.GetComponentsInChildren<MeshFilter>(true);
			if (Application.isPlaying && vp && vp.originalVehiclePhysics != null)
			{
				MatchTireRadiusToVisualMesh();
				originalTireRadiusChecked = true;
			}

			if (canDetach)
				detachedWheel.SetActive(false);
		}

		void FixedUpdate()
		{
			if (!vp || vp.originalVehiclePhysics == null || !susParent)
				return;

			if (!originalTireRadiusChecked)
			{
				MatchTireRadiusToVisualMesh();
				originalTireRadiusChecked = true;
			}

			actualRadius = popped ? rimRadius : Mathf.Lerp(rimRadius, tireRadius, tirePressure);
			vp.originalVehiclePhysics.UpdateWheelContact(this);
			airTime = grounded ? 0 : airTime + Time.fixedDeltaTime;
			rawRPM = connected ? currentRPM : 0;
			if (!connected)
				currentRPM = 0;

			travelDist = vp.originalVehiclePhysics.GetWheelTravelDistance(this);
			PositionWheel();

			if (connected)
			{
				int wheelIndex = Array.IndexOf(vp.wheels, this);
				vp.originalVehiclePhysics.GetWheelSlip(wheelIndex, out forwardSlip, out sidewaysSlip);
				originalGripUsage = vp.originalVehiclePhysics.GetWheelGripUsage(wheelIndex);
			}
		}

		void LateUpdate()
		{
			RotateWheel();
			if (!Application.isPlaying)
			{
				PositionWheel();
				return;
			}

			if (airGreenParticleSystem)
			{
				bool shouldPlay = !grounded && vp && vp.rb.linearVelocity.y < 0 && !vp.colliding;
				if (shouldPlay && !airGreenParticleSystem.isPlaying)
					airGreenParticleSystem.Play();
				else if (!shouldPlay && airGreenParticleSystem.isPlaying)
					airGreenParticleSystem.Stop();
			}
		}

		internal void SetOriginalContact(bool sourceGrounded, bool sourceGroundedReally,
			Vector3 point, Vector3 normal, Collider collider, Vector3 relativeVelocity,
			float distance, int surfaceType)
		{
			if (!groundedReally && sourceGroundedReally && impactSnd &&
				((tireHitClips != null && tireHitClips.Length > 0 && !popped) || (rimHitClip && popped)))
			{
				AudioClip hitClip = popped ? rimHitClip : tireHitClips[
					UnityEngine.Random.Range(0, tireHitClips.Length)];
				if (hitClip)
					impactSnd.PlayOneShot(hitClip, Mathf.Clamp01(airTime * airTime));
				impactSnd.pitch = Mathf.Clamp(airTime * 0.2f + 0.8f, 0.8f, 1);
			}

			grounded = sourceGrounded;
			groundedReally = sourceGroundedReally;
			contactPoint.grounded = sourceGrounded;
			contactPoint.point = point;
			contactPoint.normal = normal;
			contactPoint.col = collider;
			contactPoint.relativeVelocity = relativeVelocity;
			contactPoint.distance = distance;
			contactPoint.surfaceType = surfaceType;
			contactVelocity = Vector3.zero;
			curSurfaceType = surfaceType;
		}

		void PositionWheel()
		{
			if (!susParent || !rim)
				return;

			rim.position = susParent.maxCompressPoint +
				susParent.springDirection * susParent.suspensionDistance *
					(Application.isPlaying ? travelDist : susParent.targetCompression) +
				susParent.upDir * Mathf.Pow(Mathf.Max(
					Mathf.Abs(Mathf.Sin(susParent.sideAngle * Mathf.Deg2Rad)),
					Mathf.Abs(Mathf.Sin(susParent.casterAngle * Mathf.Deg2Rad))), 2) * actualRadius +
				susParent.pivotOffset * susParent.tr.TransformDirection(
					Mathf.Sin(tr.localEulerAngles.y * Mathf.Deg2Rad), 0,
					Mathf.Cos(tr.localEulerAngles.y * Mathf.Deg2Rad)) -
				susParent.pivotOffset * (Application.isPlaying
					? susParent.forwardDir : susParent.tr.forward);
		}

		void MatchTireRadiusToVisualMesh()
		{
			if (!rim || visualWheelMeshes == null || visualWheelMeshes.Length == 0)
				return;

			Vector3 axle = rim.forward.normalized;
			float meshRadius = 0;
			bool foundMesh = false;
			foreach (MeshFilter meshFilter in visualWheelMeshes)
			{
				if (!meshFilter || !meshFilter.sharedMesh)
					continue;

				MeshRenderer meshRenderer = meshFilter.GetComponent<MeshRenderer>();
				if (meshRenderer && !meshRenderer.enabled)
					continue;

				Mesh mesh = meshFilter.sharedMesh;
				Transform meshTransform = meshFilter.transform;
				if (mesh.isReadable)
				{
					foreach (Vector3 vertex in mesh.vertices)
						IncludeWheelRadius(meshTransform.TransformPoint(vertex), axle,
							ref meshRadius, ref foundMesh);
				}
				else
				{
					Bounds bounds = mesh.bounds;
					for (int x = -1; x <= 1; x += 2)
					for (int y = -1; y <= 1; y += 2)
					for (int z = -1; z <= 1; z += 2)
					{
						Vector3 corner = bounds.center + Vector3.Scale(bounds.extents,
							new Vector3(x, y, z));
						IncludeWheelRadius(meshTransform.TransformPoint(corner), axle,
							ref meshRadius, ref foundMesh);
					}
				}
			}

			if (!foundMesh)
			{
				//Debug.LogWarning($"[Wheel] Could not measure visible tire radius: wheel={name}, " +
				//	$"configured={tireRadius:F3}, meshCount={visualWheelMeshes.Length}", this);
				return;
			}

			float previousRadius = tireRadius;
			if (meshRadius <= previousRadius)
			{
				//Debug.Log($"[Wheel] Visible tire radius check: wheel={name}, " +
				//	$"configured={previousRadius:F3}, measured={meshRadius:F3}; no correction needed", this);
				return;
			}

			tireRadius = meshRadius + 0.005f;
			//Debug.Log($"[Wheel] Corrected tireRadius from visible mesh: wheel={name}, " +
			//	$"configured={previousRadius:F3}, meshRadius={meshRadius:F3}, tireRadius={tireRadius:F3}", this);
		}

		void IncludeWheelRadius(Vector3 worldVertex, Vector3 axle, ref float maxRadius, ref bool found)
		{
			Vector3 fromWheelCenter = worldVertex - rim.position;
			Vector3 radialOffset = fromWheelCenter - Vector3.Dot(fromWheelCenter, axle) * axle;
			maxRadius = Mathf.Max(maxRadius, radialOffset.magnitude);
			found = true;
		}

		void RotateWheel()
		{
			if (tr && susParent)
			{
				tr.localEulerAngles = new Vector3(
					susParent.camberAngle + susParent.casterAngle *
						susParent.steerAngle * susParent.flippedSideFactor,
					-susParent.toeAngle * susParent.flippedSideFactor +
						susParent.steerDegrees,
					0);
			}

			if (Application.isPlaying && rim && susParent)
			{
				rim.Rotate(Vector3.forward,
					currentRPM / 60 * 360 * susParent.flippedSideFactor * Time.deltaTime);
				if (damage > 0)
				{
					rim.localEulerAngles = new Vector3(
						Mathf.Sin(-rim.localEulerAngles.z * Mathf.Deg2Rad) * Mathf.Clamp(damage, 0, 10),
						Mathf.Cos(-rim.localEulerAngles.z * Mathf.Deg2Rad) * Mathf.Clamp(damage, 0, 10),
						rim.localEulerAngles.z);
				}
				else if (rim.localEulerAngles.x != 0 || rim.localEulerAngles.y != 0)
					rim.localEulerAngles = new Vector3(0, 0, rim.localEulerAngles.z);
			}
		}

		public void SetColliderLayer(int layer) => gameObject.layer = layer;

		public void Deflate()
		{
			if (popped)
				return;
			popped = true;
			tirePressure = 0;
			if (impactSnd && tireAirClip)
				impactSnd.PlayOneShot(tireAirClip);
		}

		public void FixTire()
		{
			popped = false;
			tirePressure = initialTirePressure;
		}

		public void Detach()
		{
			if (!connected || !canDetach || !detachedWheel || !rb)
				return;

			connected = false;
			detachedWheel.SetActive(true);
			detachedWheel.transform.SetPositionAndRotation(rim.position, rim.rotation);
			if (detachedCol)
				detachedCol.sharedMaterial = popped ? detachedRimMaterial : detachedTireMaterial;

			if (detachedTire)
			{
				detachedTire.SetActive(!popped);
				detachedCol.sharedMesh = popped
					? (rimMeshLoose ? rimMeshLoose : detachFilter.sharedMesh)
					: (tireMeshLoose ? tireMeshLoose : detachTireFilter.sharedMesh);
			}
			else if (detachedCol)
				detachedCol.sharedMesh = rimMeshLoose ? rimMeshLoose : detachFilter.sharedMesh;

			rb.mass -= mass;
			detachedBody.linearVelocity = rb.GetPointVelocity(rim.position);
			detachedBody.angularVelocity = rb.angularVelocity;
			rim.gameObject.SetActive(false);
		}

		public void Reattach()
		{
			if (connected || !detachedWheel || !rb)
				return;

			connected = true;
			detachedWheel.SetActive(false);
			rb.mass += mass;
			rim.gameObject.SetActive(true);
		}

		public void GetWheelDimensions(float radiusMargin, float widthMargin)
		{
			if (transform.childCount == 0)
			{
				Debug.LogError("No rim or tire meshes found for getting wheel dimensions.", this);
				return;
			}

			Transform rimTransform = transform.GetChild(0);
			MeshFilter rimFilter = rimTransform.GetComponent<MeshFilter>();
			Transform tireTransform = rimTransform.childCount > 0 ? rimTransform.GetChild(0) : null;
			MeshFilter tireFilter = tireTransform ? tireTransform.GetComponent<MeshFilter>() : null;
			Mesh mesh = tireFilter ? tireFilter.sharedMesh : rimFilter ? rimFilter.sharedMesh : null;
			Transform meshTransform = tireFilter ? tireTransform : rimTransform;
			if (!mesh || !meshTransform)
			{
				Debug.LogError("No rim or tire meshes found for getting wheel dimensions.", this);
				return;
			}

			GetMeshDimensions(mesh, meshTransform, out float maxRadius, out float maxWidth);
			tireRadius = maxRadius + radiusMargin;
			tireWidth = maxWidth + widthMargin;
			if (tireFilter && rimFilter)
			{
				GetMeshDimensions(rimFilter.sharedMesh, rimTransform, out maxRadius, out maxWidth);
				rimRadius = maxRadius + radiusMargin;
				rimWidth = maxWidth + widthMargin;
			}
			else
			{
				rimRadius = maxRadius * 0.5f + radiusMargin;
				rimWidth = maxWidth * 0.5f + widthMargin;
			}
		}

		static void GetMeshDimensions(Mesh mesh, Transform meshTransform,
			out float maxRadius, out float maxWidth)
		{
			maxRadius = 0;
			maxWidth = 0;
			foreach (Vector3 vertex in mesh.vertices)
			{
				Vector3 radialOffset = meshTransform.TransformVector(new Vector3(vertex.x, vertex.y, 0));
				Vector3 widthOffset = meshTransform.TransformVector(new Vector3(0, 0, vertex.z));
				maxRadius = Mathf.Max(maxRadius, radialOffset.magnitude);
				maxWidth = Mathf.Max(maxWidth, widthOffset.magnitude);
			}
		}

		void OnDrawGizmosSelected()
		{
			tr = transform;
			if (tr.childCount > 0)
			{
				rim = tr.GetChild(0);
				if (rim.childCount > 0)
					tire = rim.GetChild(0);
			}

			if (!rim)
				return;

			float visibleRadius = Mathf.Lerp(rimRadius, tireRadius, tirePressure);
			Gizmos.color = Color.white;
			GizmosExtra.DrawWireCylinder(rim.position, rim.forward, visibleRadius, tireWidth * 2);
			Gizmos.color = Color.cyan;
			GizmosExtra.DrawWireCylinder(rim.position, rim.forward, rimRadius, rimWidth * 2);
		}

		void OnDestroy()
		{
			if (Application.isPlaying && detachedWheel)
				Destroy(detachedWheel);
		}
	}

	public class WheelContact
	{
		public bool grounded;
		public Collider col;
		public Vector3 point;
		public Vector3 normal;
		public Vector3 relativeVelocity;
		public float distance;
		public float surfaceFriction;
		public float sourceGrip = 1;
		public int sourceFlags = 1;
		public float sourceRolling = 1;
		public float sourceRoughness;
		public int surfaceType;
	}
}
