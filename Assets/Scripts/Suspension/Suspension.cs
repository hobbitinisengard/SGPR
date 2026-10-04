using UnityEngine;
using System;
using System.Collections.Generic;

namespace RVP
{
	[ExecuteInEditMode]
	[DisallowMultipleComponent]
	[AddComponentMenu("RVP/Suspension/Suspension", 0)]
	public class Suspension : MonoBehaviour
	{
		[NonSerialized] public Transform tr;
		VehicleParent vehicle;

		[NonSerialized] public bool flippedSide;
		[NonSerialized] public float flippedSideFactor;
		[NonSerialized] public Quaternion initialRotation;

		public Wheel wheel;

		[Header("Steering")]
		[Range(-180, 180)] public float steerRangeMin;
		[Range(-180, 180)] public float steerRangeMax;
		[Range(-1, 1)] public float steerAngle;
		[NonSerialized] public float steerDegrees;

		[Header("Wheel alignment")]
		public AnimationCurve camberCurve = AnimationCurve.Linear(0, 0, 1, 0);
		[Range(-89.999f, 89.999f)] public float camberOffset;
		[NonSerialized] public float camberAngle;
		public bool solidAxleCamber;
		public Suspension oppositeWheel;
		[Range(-89.999f, 89.999f)] public float sideAngle;
		[Range(-89.999f, 89.999f)] public float casterAngle;
		[Range(-89.999f, 89.999f)] public float toeAngle;
		public float pivotOffset;
		[NonSerialized] public List<SuspensionPart> movingParts = new();

		[Header("Original suspension presentation")]
		public float suspensionDistance;
		[NonSerialized] public float compression;
		[Range(0, 1)] public float targetCompression = 1;
		[NonSerialized] public Vector3 maxCompressPoint;
		[NonSerialized] public Vector3 springDirection;
		[NonSerialized] public Vector3 upDir;
		[NonSerialized] public Vector3 forwardDir;

		[Header("Damage")]
		public Vector3 damagePivot;
		[Range(0, 1)] public float detachedCompression = 0.5f;
		public float jamForce = Mathf.Infinity;
		[NonSerialized] public bool jammed;

		void Awake()
		{
			tr = transform;
			vehicle = tr.GetTopmostParentComponent<VehicleParent>();
		}

		void Start()
		{
			if (!vehicle)
				vehicle = tr.GetTopmostParentComponent<VehicleParent>();
			flippedSide = Vector3.Dot(tr.forward, vehicle.transform.right) < 0;
			flippedSideFactor = flippedSide ? -1 : 1;
			initialRotation = tr.localRotation;
			if (Application.isPlaying)
			{
				steerRangeMax = Mathf.Max(steerRangeMin, steerRangeMax);
			}
			GetCamber();
			GetSpringVectors();
		}

		void FixedUpdate()
		{
			upDir = tr.up;
			forwardDir = tr.forward;
			GetCamber();
			GetSpringVectors();

			if (!wheel || !wheel.connected)
			{
				compression = detachedCompression;
				return;
			}

			compression = vehicle && vehicle.originalVehiclePhysics != null
				? vehicle.originalVehiclePhysics.GetWheelTravelDistance(wheel)
				: targetCompression;
		}

		void Update()
		{
			GetCamber();
			if (!Application.isPlaying)
				GetSpringVectors();
			steerDegrees = Mathf.Abs(steerAngle) * (steerAngle > 0 ? steerRangeMax : steerRangeMin);
		}

		void GetSpringVectors()
		{
			if (!Application.isPlaying)
			{
				tr = transform;
				if (vehicle)
				{
					flippedSide = Vector3.Dot(tr.forward, vehicle.transform.right) < 0;
					flippedSideFactor = flippedSide ? -1 : 1;
				}
			}

			maxCompressPoint = tr.position;
			float casterDir = -Mathf.Sin(casterAngle * Mathf.Deg2Rad) * flippedSideFactor;
			float sideDir = -Mathf.Sin(sideAngle * Mathf.Deg2Rad);
			springDirection = tr.TransformDirection(casterDir,
				Mathf.Max(Mathf.Abs(casterDir), Mathf.Abs(sideDir)) - 1, sideDir).normalized;
		}

		void GetCamber()
		{
			if (solidAxleCamber && oppositeWheel && wheel && wheel.connected &&
				oppositeWheel.wheel && oppositeWheel.wheel.rim && wheel.rim)
			{
				Vector3 axleDir = tr.InverseTransformDirection(
					(oppositeWheel.wheel.rim.position - wheel.rim.position).normalized);
				camberAngle = Mathf.Atan2(axleDir.z, axleDir.y) * Mathf.Rad2Deg + 90 + camberOffset;
			}
			else
			{
				float travel = Application.isPlaying && wheel && wheel.connected
					? wheel.travelDist : targetCompression;
				camberAngle = camberCurve.Evaluate(travel) + camberOffset;
			}
		}

		void OnDrawGizmosSelected()
		{
			if (!tr)
				tr = transform;

			if (wheel && wheel.rim)
			{
				Vector3 wheelPoint = wheel.rim.position;
				float camberSin = -Mathf.Sin(camberAngle * Mathf.Deg2Rad);
				float steerSin = Mathf.Sin(Mathf.Lerp(steerRangeMin, steerRangeMax,
					(steerAngle + 1) * 0.5f) * Mathf.Deg2Rad);
				float minSteerSin = Mathf.Sin(steerRangeMin * Mathf.Deg2Rad);
				float maxSteerSin = Mathf.Sin(steerRangeMax * Mathf.Deg2Rad);
				Gizmos.color = Color.magenta;
				Gizmos.DrawWireSphere(wheelPoint, 0.05f);
				Gizmos.DrawLine(wheelPoint, wheelPoint + tr.TransformDirection(minSteerSin,
					camberSin * (1 - Mathf.Abs(minSteerSin)),
					Mathf.Cos(steerRangeMin * Mathf.Deg2Rad) * (1 - Mathf.Abs(camberSin))).normalized);
				Gizmos.DrawLine(wheelPoint, wheelPoint + tr.TransformDirection(maxSteerSin,
					camberSin * (1 - Mathf.Abs(maxSteerSin)),
					Mathf.Cos(steerRangeMax * Mathf.Deg2Rad) * (1 - Mathf.Abs(camberSin))).normalized);
				Gizmos.DrawLine(wheelPoint + tr.TransformDirection(minSteerSin,
					camberSin * (1 - Mathf.Abs(minSteerSin)),
					Mathf.Cos(steerRangeMin * Mathf.Deg2Rad) * (1 - Mathf.Abs(camberSin))).normalized * 0.9f,
					wheelPoint + tr.TransformDirection(maxSteerSin,
					camberSin * (1 - Mathf.Abs(maxSteerSin)),
					Mathf.Cos(steerRangeMax * Mathf.Deg2Rad) * (1 - Mathf.Abs(camberSin))).normalized);
				Gizmos.DrawLine(wheelPoint, wheelPoint + tr.TransformDirection(steerSin,
					camberSin * (1 - Mathf.Abs(steerSin)),
					Mathf.Cos(steerRangeMin * Mathf.Deg2Rad) * (1 - Mathf.Abs(camberSin))).normalized);
			}

			Gizmos.color = Color.red;
			Gizmos.DrawWireSphere(tr.TransformPoint(damagePivot), 0.05f);
		}
	}
}
