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
		public Vector3 d_vel_norm;
		public float d_dot;
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
		public float camOffsetDistance = -1;
		public float offsetCarDistance;
		public float scaledTurnAngle;
		public float EffectiveTurnAngle;
		public float smoothedRollAngle;
		public float rollAngleDeg;
		public float pitchAngle;

		public Vector3 pitchVec;
		public Vector3 Inverse_vec;
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
		public int maxPitch = 10;
		public float cHeight = 2;
		public float smoothRotCoeff = 0.01f;

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
				forwardLook = -target.up;
				upLook = target.forward;
				targetBody = target.GetComponent<Rigidbody>();
				tr.position = target.position;
				tr.rotation = Quaternion.LookRotation(forwardLook,upLook);
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
				pitchAngle = WrapAround180Degs(vp.tr.localEulerAngles.x);
				bool pitchLocked = pitchAngle < -maxPitch;
				if (vp.groundedWheels == 4)
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
				//-----------

				//camera look-position
				// cos(45d) = 0.7f cos(0d) = 1 cos(90deg) = 0
				//d_vel_norm = vp.rb.velocity.normalized;
				//d_dot = Vector3.Dot(vp.tr.forward, vp.rb.velocity.normalized);
				Vector3 forward;
				if (vp.rb.velocity.magnitude < 1)
					forward = vp.tr.forward;
				else
					forward = (vp.groundedWheels > 0)
					 ? vp.tr.forward : vp.rb.velocity.normalized;
				
				smoothYRot = Mathf.Lerp(smoothYRot, smoothRotCoeff * vp.rb.angularVelocity.y, 0.01f * Time.fixedDeltaTime);
				forward = Quaternion.AngleAxis(Time.fixedDeltaTime * smoothYRot * Mathf.Rad2Deg, vp.tr.up) * forward;//Mathf.Abs(smoothYRot) * new Vector3(Mathf.Sin(smoothYRot), 0, Mathf.Cos(smoothYRot)).normalized;
				forward = Quaternion.AngleAxis(xInput * 90 + yInput * 180, vp.tr.up) * forward;
				lookObj.position = target.position - forward * targetCamCarDistance + Vector3.up * height;
				//--------------
				targetForward = target.position + cHeight * Vector3.up - lookObj.position; //Quaternion.AngleAxis(9, vp.tr.right) * forward; // targetForward
																													//camera look-rotation
				forwardLook = Vector3.Lerp(forwardLook, targetForward, 0.1f * TimeMaster.inverseFixedTimeFactor);
				if (Physics.Raycast(target.position, -targetUp, out RaycastHit hit, 1, castMask))
				{
					float dot = Vector3.Dot(targetUp, hit.normal);
					// 0.9848 = cos(15d)
					upLook = Vector3.Lerp(
						 upLook, (dot < 0.9848077 && pitchLocked ? targetUp : hit.normal), 0.02f * TimeMaster.inverseFixedTimeFactor);
				}
				lookObj.rotation = Quaternion.LookRotation(forwardLook, upLook + rollUp);
				//-------------

				// this.tr chases lookObj
				dampOffset = Vector3.SmoothDamp(dampOffset, lookObj.position, ref fastVelocity,
					 camFollowSmoothTime, catchUpCamSpeed, Time.fixedDeltaTime * smoothDampRspnvns);

				if (vp.groundedWheels == 0) // when car airborne
				{
					offsetCarDistance = Vector3.Distance(dampOffset, target.position);
					camOffsetDistance = Vector3.Distance(tr.position, dampOffset);
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

				Quaternion rotation;
				if (vp.customCam)
				{
					rotation = Quaternion.Lerp(tr.rotation, Quaternion.LookRotation(target.position - vp.customCam.transform.position), 3*Time.fixedDeltaTime);
					newTrPos = vp.customCam.transform.position;
				}
				else
				{
					bool badpos = Physics.Linecast(target.position + cHeight * Vector3.up, lookObj.position, out hit, castMask);
					if (badpos)
					{ //Check if there is an object between the camera and target vehicle and move the camera in front of it
						lookObj.position = hit.point + (target.position - lookObj.position).normalized * (cam.nearClipPlane + 0.1f);
					}

					smoothTime = Mathf.Lerp(smoothTime, cameraStopped ? camStoppedSmoothTime : camFollowSmoothTime
						, (cameraStopped ? 1 : 2) * Time.fixedDeltaTime * smoothTimeSpeed);

					if (yInput == 0 && xInput == 0)
						newTrPos =
									Vector3.SmoothDamp(tr.position, lookObj.position, ref velocity,
									smoothTime, catchUpCamSpeed, Time.fixedDeltaTime * smoothDampRspnvns);
					else
						newTrPos = Vector3.Lerp(tr.position, lookObj.position, 0.5f);

					if (cameraStopped)
					{
						Quaternion cameraStoppedRotation = Quaternion.LookRotation(target.position - tr.position, rollUp);
						rotation = Quaternion.Lerp(tr.rotation, cameraStoppedRotation,
							 2 * Time.fixedDeltaTime);
					}
					else
					{
						rotation = Quaternion.Lerp(tr.rotation, lookObj.rotation,
							 (vp.groundedWheels > 1 ? 12f : 3f) * Time.fixedDeltaTime);
					}
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