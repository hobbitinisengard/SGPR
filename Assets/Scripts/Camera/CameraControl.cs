using UnityEngine;

namespace RVP
{
	[RequireComponent(typeof(Camera))]
	[RequireComponent(typeof(AudioListener))]
	[DisallowMultipleComponent]
	[AddComponentMenu("RVP/Camera/Camera Control", 0)]

	// Class for controlling the camera
	public class CameraControl : MonoBehaviour
	{
		Transform tr;
		Camera cam;
		VehicleParent vp;
		public Transform target; // The target vehicle
		Rigidbody targetBody;

		public float height;
		public float targetCamCarDistance;

		public float xInput;
		public float yInput;

		Vector3 lookDir;
		float smoothYRot;
		Transform lookObj;
		Vector3 forwardLook;
		Vector3 upLook;
		Vector3 targetForward;
		Vector3 targetUp;
		[Tooltip("Should the camera stay flat? (Local y-axis always points up)")]
		public bool stayFlat = false;

		[Tooltip("Mask for which objects will be checked in between the camera and target vehicle")]
		public LayerMask castMask;

		[Header("Experimental")]
		public float maxEffectiveRollTurnAngle = 5;
		float rollCoeff = 4f;
		float catchUpCamSpeed = 10f;


		// distance between target-camera-position and car
		[Header("Debug")]
		public float velocityY;
		public float camOffsetDistance = -1;
		public float offsetCarDistance;
		public float scaledTurnAngle;
		public float EffectiveTurnAngle;
		public float smoothedRollAngle;
		public float rollAngleDeg;
		public float pitchAngle;

		public Vector3 pitchVec;
		public Vector3 Inverse_vec;
		Vector3 camOffset;
		private Vector3 dampOffset;
		private Vector3 velocity = Vector3.zero;
		private Vector3 fastVelocity = Vector3.zero;
		public bool cameraStopped = false;
		private float smoothDampRspnvns = 10f;
		public float smoothTime = 1f;
		private Vector3 newTrPos;
		private float camStoppedSmoothTime = 4f;
		private float camFollowSmoothTime = 1f;
		private float smoothTimeSpeed = 2.5f;

		void Awake()
		{
			tr = transform;
			cam = GetComponent<Camera>();
			cam.depthTextureMode |= DepthTextureMode.Depth;
		}
		public void Disconnect()
		{
			target = null;
			if(lookObj)
				Destroy(lookObj.gameObject);
		}
		public void Connect(VehicleParent car)
		{
			target = car.transform;
			// lookObj is an object used to help position and rotate the camera
			if (!lookObj)
			{
				GameObject lookTemp = new GameObject("Camera Looker");
				lookObj = lookTemp.transform;
			}

			// Set variables based on target vehicle's properties
			if (target)
			{
				vp = car;
				targetCamCarDistance += vp.cameraDistanceChange;
				height += vp.cameraHeightChange;
				forwardLook = target.forward;
				upLook = Vector3.up;//target.up;
				targetBody = target.GetComponent<Rigidbody>();
				tr.position = target.position - target.forward * targetCamCarDistance + target.up * height;
				tr.rotation = Quaternion.LookRotation(target.position - tr.position);
			}

			// Set the audio listener update mode to fixed, because the camera moves in FixedUpdate
			// This is necessary for doppler effects to sound correct
			GetComponent<AudioListener>().velocityUpdateMode = AudioVelocityUpdateMode.Fixed;
		}
		float WrapAround180Degs(float degs)
		{
			if (degs > 180)
				degs -= 360;
			return degs;
		}
		void FixedUpdate()
		{
			if (target && targetBody && target.gameObject.activeSelf)
			{
				const int maxPitch = 10;
				pitchAngle = WrapAround180Degs(vp.tr.localEulerAngles.x);
				bool pitchLocked = pitchAngle < -maxPitch;
				if (vp.groundedWheels > 0)
				{
					if (pitchLocked)
					{
						Quaternion qRotation = Quaternion.AngleAxis(-pitchAngle - maxPitch, vp.tr.right);
						targetForward = qRotation * vp.tr.forward;
						targetUp = qRotation * vp.tr.up;
					}
					else
					{
						targetForward = vp.tr.forward;
						targetUp = vp.tr.up;
					}
				}
				else
				{
					targetUp = vp.tr.up;
					targetForward = targetBody.velocity;
				}

				lookDir = Vector3.Lerp(
					 lookDir, (xInput == 0 && yInput == 0 ? Vector3.forward : new Vector3(xInput, 0, yInput).normalized), 0.1f * TimeMaster.inverseFixedTimeFactor);
				//lookDir = -Vector3.forward;

				smoothYRot = Mathf.Lerp(smoothYRot, targetBody.angularVelocity.y, 0.01f * Time.fixedDeltaTime);


				if (xInput == 0 && yInput == 0)
					targetForward = Quaternion.AngleAxis(9, vp.tr.right) * targetForward;
				else
					targetForward = Quaternion.AngleAxis(-18, vp.tr.right) * targetForward;


				// ROLL: camera rolls proportional to car's effective angle and speed
				if (vp.localVelocity.z > 5f)
				{
					Vector3 locVel = vp.localVelocity;
					locVel.y = 0;
					EffectiveTurnAngle = Vector3.SignedAngle(Vector3.forward, locVel, Vector3.up);
					EffectiveTurnAngle = Mathf.Clamp(EffectiveTurnAngle, -maxEffectiveRollTurnAngle, maxEffectiveRollTurnAngle);
					scaledTurnAngle = EffectiveTurnAngle / maxEffectiveRollTurnAngle;
				}
				else
				{
					EffectiveTurnAngle = 0;
					scaledTurnAngle = 0;
				}
				smoothedRollAngle = Mathf.Lerp(smoothedRollAngle, scaledTurnAngle, rollCoeff * Time.fixedDeltaTime);
				rollAngleDeg = smoothedRollAngle * 15f * Mathf.Lerp(0, 15, targetBody.velocity.magnitude) / 15f;
				Vector3 rollUp = Quaternion.AngleAxis(-rollAngleDeg, vp.tr.forward) * Vector3.up;
				// Calculate rotation and position variables
				forwardLook = Vector3.Lerp(forwardLook, targetForward, 0.1f * TimeMaster.inverseFixedTimeFactor);
				lookObj.rotation = Quaternion.LookRotation(forwardLook, upLook + rollUp);

				if (xInput != 0 || yInput != 0)
				{
					Quaternion lookRot = Quaternion.AngleAxis(-xInput * 90 + yInput * 180, vp.tr.up);
					lookObj.rotation *= lookRot;
				}
				lookObj.position = target.position;

				if (Physics.Raycast(target.position, -targetUp, out RaycastHit hit, 1, castMask))
				{
					float dot = Vector3.Dot(targetUp, hit.normal);
					// 0.9848 = cos(15d)
					upLook = Vector3.Lerp(
						 upLook, (dot < 0.9848077 && pitchLocked ? targetUp : hit.normal), 0.02f * TimeMaster.inverseFixedTimeFactor);
				}

				Vector3 lookDirActual = (lookDir - 0.4f * Mathf.Abs(smoothYRot) * new Vector3(Mathf.Sin(smoothYRot), 0, Mathf.Cos(smoothYRot))).normalized;

				//if(xInput != 0 || yInput != 0)
				//    camOffset = -lookDirActual * 5 - lookDirActual + Vector3.up * height;
				//else
				camOffset = -lookDirActual * targetCamCarDistance - lookDirActual + Vector3.up * height;

				camOffset = lookObj.TransformPoint(Quaternion.AngleAxis(-xInput * 90 + yInput * 180, vp.tr.up) * camOffset);

				// now camOffset takes into account distance that increases with car's speed
				dampOffset = Vector3.SmoothDamp(dampOffset, camOffset, ref fastVelocity,
					 camFollowSmoothTime, catchUpCamSpeed, Time.fixedDeltaTime * smoothDampRspnvns);

				offsetCarDistance = Vector3.Distance(dampOffset, target.position);
				camOffsetDistance = Vector3.Distance(tr.position, dampOffset);
				velocityY = vp.rb.velocity.y;
				if (vp.groundedWheels == 0) // when car airborne
				{
					if (camOffsetDistance < 2)
					{
						cameraStopped = true;
					}
					else if (camOffsetDistance > .5f * offsetCarDistance)
					{
						cameraStopped = false;
					}
				}
				else
				{
					cameraStopped = false;
				}

				if (Physics.Linecast(target.position, camOffset, out hit, castMask))
				{ //Check if there is an object between the camera and target vehicle and move the camera in front of it
					camOffset = hit.point + (target.position - camOffset).normalized * (cam.nearClipPlane + 0.1f);
				}
				//Vector3 forwardDir = lookObj.TransformDirection(lookDirActual);

				smoothTime = Mathf.Lerp(smoothTime, cameraStopped ? camStoppedSmoothTime : camFollowSmoothTime
					, (cameraStopped ? 1 : 2) * Time.fixedDeltaTime * smoothTimeSpeed);
				if (yInput == 0 && xInput == 0)
					newTrPos =
								Vector3.SmoothDamp(tr.position, camOffset, ref velocity,
								smoothTime, catchUpCamSpeed, Time.fixedDeltaTime * smoothDampRspnvns);
				else
					newTrPos =
						  Vector3.SmoothDamp(tr.position, camOffset, ref velocity,
						  0, float.MaxValue, Time.fixedDeltaTime * smoothDampRspnvns);

				Quaternion rotation;
				if (cameraStopped)
				{
					Quaternion cameraStoppedRotation = Quaternion.LookRotation(target.position - tr.position, rollUp);
					rotation = Quaternion.Lerp(tr.rotation, cameraStoppedRotation,
						 2 * Time.fixedDeltaTime);
				}
				else
				{
					//Vector3 euler = tr.rotation.eulerAngles;
					//Quaternion rollRotation = Quaternion.Euler(euler.x, euler.y, rollAngleDeg);
					rotation = Quaternion.Lerp(tr.rotation, lookObj.rotation,
						 (vp.groundedWheels > 1 ? 12f : 3f) * Time.fixedDeltaTime);
				}

				tr.SetPositionAndRotation(newTrPos, rotation);
			}
		}
		// function for setting the rotation input of the camera
		public void SetInput(float x, float y)
		{
			xInput = x;
			yInput = y;
		}

		// Destroy lookObj
		void OnDestroy()
		{
			if (lookObj)
			{
				Destroy(lookObj.gameObject);
			}
		}

	}
}