using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RVP
{
	[RequireComponent(typeof(Camera))]
	[RequireComponent(typeof(AudioListener))]
	[DisallowMultipleComponent]
	[AddComponentMenu("RVP/Camera/Camera Control", 0)]

	// Class for controlling the camera
	public class CameraControl : MonoBehaviour
	{
		public InputActionReference moveRef;
		public InputActionReference changeCamRef;

		readonly Vector2[] camerasLH = new Vector2[] { new(8.5f, 3.5f), new(4.5f, 2), new(11, 4) };
		int curCameraLH = 0;
		public enum Mode { Follow, Replay };
		Mode _mode;
		public Mode mode
		{
			get { return _mode; }
			set
			{
				switch (value)
				{
					case Mode.Follow:
						cam.fieldOfView = 54;
						if(vp)
							tr.position = vp.tr.position;
						_mode = value;
						break;
					case Mode.Replay:
						if(vp.followAI.replayCams.Count > 0)
						{
							cam.fieldOfView = 24;
							_mode = value;
						}
						break;
				}
			}
		}
		Transform tr;
		Camera cam;
		VehicleParent vp;
		Rigidbody targetBody;

		public float height;
		public float targetCamCarDistance;

		public float xInput;
		public float yInput;
		float smoothYRot;
		public Vector3 d_vel_norm;
		public float d_dot;
		Transform lookObj;
		Vector3 forwardLook;
		Vector3 upLook;
		Vector3 targetForward;
		Vector3 targetUp;

		[Tooltip("Mask for which objects will be checked in between the camera and target vehicle")]
		public LayerMask castMask;

		float maxEffectiveRollTurnAngle = 30;
		float rollCoeff = 8f;
		float catchUpCamSpeed = 10f;


		// distance between target-camera-position and car
		[Header("Debug")]
		public float distRequiredToCamAtCarLookRotation = 100;
		public float camOffsetDistance = -1;
		public float carOffsetDistance;
		public float scaledTurnAngle;
		public float effectiveTurnAngle;
		public float smoothedRollAngle;
		public float rollAngleDeg;
		public float pitchAngle;

		public Vector3 pitchVec;
		public Vector3 Inverse_vec;
		private Vector3 dampOffset;
		private Vector3 velocity = Vector3.zero;
		private Vector3 fastVelocity = Vector3.zero;
		public bool slowCamera = false;
		private float smoothDampRspnvns = 10f;
		public float smoothTime = 1f;
		private Vector3 newTrPos;
		private float camStoppedSmoothTime = 4f;
		private float camFollowSmoothTime = 1f;
		private float smoothTimeSpeed = 2.5f;
		public int maxPitch = 10;
		public float cHeight = 2;
		public float smoothRotCoeff = 0.01f;
		public int curReplayCamIdx;
		public float replayCamAgility = 1;
		float forwardLookCoeff = 10;
		float upLookCoeff = 1f;
		Vector3 forward;
		public float xyInputCamSpeedCoeff = 5;
		void Awake()
		{
			tr = transform;
			cam = GetComponent<Camera>();
			cam.depthTextureMode |= DepthTextureMode.Depth;
			changeCamRef.action.performed += ChangeFollowCamera;
		}

		private void ChangeFollowCamera(InputAction.CallbackContext obj)
		{
			if (mode == Mode.Follow && !F.I.chat.texting)
			{
				curCameraLH++;
				curCameraLH %= camerasLH.Length;
				height = camerasLH[curCameraLH].y + vp.cameraHeightChange;
				targetCamCarDistance = camerasLH[curCameraLH].x + vp.cameraDistanceChange;
			}
		}

		private void OnEnable()
		{
			StartCoroutine(AllowChangingTarget());
		}
		IEnumerator AllowChangingTarget()
		{
			yield return new WaitForSeconds(1);
			moveRef.action.performed += SwitchTarget;
		}
		private void OnDisable()
		{
			moveRef.action.performed -= SwitchTarget;
		}

		public void Disconnect()
		{
			if (lookObj)
				Destroy(lookObj.gameObject);
		}
		public void Connect(VehicleParent car, Mode mode = Mode.Follow)
		{
			if (!lookObj)
			{// lookObj is an object used to help position and rotate the camera
				GameObject lookTemp = new GameObject("Camera Looker");
				lookObj = lookTemp.transform;
			}
			vp = car;
			targetCamCarDistance += vp.cameraDistanceChange;
			height += vp.cameraHeightChange;
			forwardLook = -vp.tr.up;
			upLook = vp.tr.forward;
			targetBody = vp.tr.GetComponent<Rigidbody>();
			tr.SetPositionAndRotation(vp.tr.position, Quaternion.LookRotation(forwardLook, upLook));
			dampOffset = vp.tr.position;
			if (this.mode == Mode.Replay && vp.followAI.replayCams?.Count == 0)
				this.mode = Mode.Follow;
			// Set the audio listener update mode to fixed, because the camera moves in FixedUpdate
			// This is necessary for doppler effects to sound correct
			GetComponent<AudioListener>().velocityUpdateMode = AudioVelocityUpdateMode.Fixed;
			enabled = true;
			this.mode = mode;
		}
		float WrapAround180Degs(float degs)
		{
			if (degs > 180)
				degs -= 360;
			return degs;
		}
		void FixedUpdate()
		{
			if (vp && targetBody && vp.tr.gameObject.activeSelf)
			{
				if (mode == Mode.Follow)
				{
					FollowCam();
				}
				else
				{
					ReplayCam();
				}
			}
		}
		private void SwitchTarget(InputAction.CallbackContext context)
		{
			if (vp && RaceManager.I.playerCar?.raceBox.enabled == false && !F.I.chat.texting)
			{
				Vector2 move = moveRef.action.ReadValue<Vector2>();
				if(Mathf.Abs(move.x) > 0.5f)
				{
					int index = F.I.s_cars.FindIndex(c => c.transform.name == vp.name);

					if (F.I.s_cars[index] != vp)
					{
						Connect(F.I.s_cars[F.Wraparound(index + (move.x > 0 ? 1 : -1), 0, F.I.s_cars.Count - 1) % F.I.s_cars.Count], Mode.Replay);

						RaceManager.I.hud.infoText.AddMessage(new Message(vp.transform.name, BottomInfoType.NEW_CAMERA_TARGET));
					}
					
				}
			}
		}
	
		void ReplayCam()
		{
			tr.position = vp.followAI.currentCam.cam.transform.position;
			Vector3 camTarget = vp.tr.position - replayCamAgility * Time.fixedDeltaTime * vp.rb.velocity;
			tr.rotation = Quaternion.LookRotation(camTarget - tr.position);
		}
		void FollowCam()
		{
			pitchAngle = WrapAround180Degs(vp.tr.localEulerAngles.x);
			
			bool pitchLocked = false;
			if (vp.reallyGroundedWheels == 4)
			{
				if (pitchAngle < -maxPitch)
				{
					pitchLocked = true;
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
				Vector2 forward = Vector2.up;
				Vector2 vel = vp.localVelocity.Flat();
				effectiveTurnAngle = Vector2.SignedAngle(forward, vel);

				effectiveTurnAngle = Mathf.Clamp(effectiveTurnAngle, -maxEffectiveRollTurnAngle, maxEffectiveRollTurnAngle);
				scaledTurnAngle = effectiveTurnAngle / maxEffectiveRollTurnAngle;
			}
			else
			{
				effectiveTurnAngle = 0;
				scaledTurnAngle = 0;
			}
			smoothedRollAngle = Mathf.Lerp(smoothedRollAngle, scaledTurnAngle, rollCoeff * Time.fixedDeltaTime);
			rollAngleDeg = smoothedRollAngle * Mathf.Lerp(0, 30, vp.velMag);
			Vector3 rollUp = Quaternion.AngleAxis(-rollAngleDeg, vp.tr.forward) * Vector3.up;
			
			//Debug.DrawRay(vp.tr.position + Vector3.up * 3, targetUp, Color.white);
			//camera look-position
			// cos(45d) = 0.7f cos(0d) = 1 cos(90deg) = 0
			if (vp.rb.velocity.magnitude < 1)
				forward = vp.tr.forward;
			else
				forward = (vp.reallyGroundedWheels > 0)
				 ? vp.tr.forward : vp.rb.velocity.normalized;

			if(!F.I.chat.texting)
			{
				xInput = vp.basicInput.lookAxisInput.action.ReadValue<float>();
				yInput = -vp.basicInput.lookBackInput.action.ReadValue<float>();
			}

			smoothYRot = Mathf.Lerp(smoothYRot, smoothRotCoeff * vp.rb.angularVelocity.y, Time.fixedDeltaTime);
			forward = Quaternion.AngleAxis(xInput * 90 + yInput * 180, vp.tr.up) * forward;
			forward = Quaternion.AngleAxis(Time.fixedDeltaTime * smoothYRot * Mathf.Rad2Deg, vp.tr.up) * forward;
			lookObj.position = vp.tr.position - forward * targetCamCarDistance + Vector3.up * height;
			//--------------

			targetForward = vp.tr.position + cHeight * Vector3.up - lookObj.position;
			forwardLook = Vector3.Lerp(forwardLook, targetForward, forwardLookCoeff * Time.fixedDeltaTime);
			if (vp.reallyGroundedWheels > 0 && Physics.Raycast(vp.tr.position + vp.tr.up, -targetUp, out RaycastHit hit, Mathf.Infinity, castMask))
			{
				float dot = Vector3.Dot(targetUp, hit.normal);
				// 0.9848 = cos(15d)
				upLook = Vector3.Lerp(
					 upLook, (dot < 0.9848077 && pitchLocked) ? targetUp : hit.normal, upLookCoeff * Time.fixedDeltaTime);
				Debug.DrawRay(vp.tr.position + Vector3.up * 3, targetUp, Color.red);
				Debug.DrawRay(vp.tr.position + Vector3.up * 3, hit.normal, Color.blue);

			}
			lookObj.rotation = Quaternion.LookRotation(forwardLook, upLook + rollUp);
			//-------------

			// this.tr chases lookObj
			if (vp.ghostComponent.justResetted)
			{
				dampOffset = lookObj.position;
				fastVelocity = Vector3.zero;
			}
			else
			{
				dampOffset = Vector3.SmoothDamp(dampOffset, lookObj.position, ref fastVelocity,
				 camFollowSmoothTime, catchUpCamSpeed, Time.fixedDeltaTime * smoothDampRspnvns);
			}

			camOffsetDistance = Vector3.Distance(tr.position, dampOffset);
			carOffsetDistance = Vector3.Distance(dampOffset, vp.tr.position);
			if (vp.reallyGroundedWheels == 0) // when car airborne
			{
				if (camOffsetDistance < 2)
				{
					slowCamera = true;
				}
				else
				{
					if (camOffsetDistance > .5f * carOffsetDistance)
						slowCamera = false;
				}
			}
			else
			{
				slowCamera = false;
			}
			if(vp.customCam)
			{
				lookObj.position = vp.customCam.transform.position;
			}

			bool badpos = Physics.Linecast(vp.tr.position + cHeight * Vector3.up, lookObj.position, out hit, castMask);
			Vector3 target;
			if (badpos && !vp.customCam)
			{ //Check if there is an object between the camera and target vehicle and move the camera in front of it
				target = hit.point + (vp.tr.position + cHeight * Vector3.up - newTrPos).normalized * (cam.nearClipPlane + 1);
			}
			else
				target = lookObj.position;

			newTrPos = Vector3.SmoothDamp(tr.position, target, ref velocity, smoothTime, catchUpCamSpeed, 
				(yInput == 0 && xInput == 0) ? (Time.fixedDeltaTime * smoothDampRspnvns) 
					: (xyInputCamSpeedCoeff * Time.fixedDeltaTime * smoothDampRspnvns));
			
			smoothTime = Mathf.Lerp(smoothTime, slowCamera ? camStoppedSmoothTime : camFollowSmoothTime
				, (slowCamera ? 1 : 2) * Time.fixedDeltaTime * smoothTimeSpeed);

			Quaternion rotation;
			if (!vp.customCam)
			{
				if (slowCamera)
				{ // cam lets car go ahead
					Quaternion cameraStoppedRotation = Quaternion.LookRotation(vp.tr.position - tr.position, rollUp);
					rotation = Quaternion.Lerp(tr.rotation, cameraStoppedRotation, 2 * Time.fixedDeltaTime);
				}
				else
				{
					if (camOffsetDistance > carOffsetDistance)
					{
						Quaternion cameraStoppedRotation = Quaternion.LookRotation(vp.tr.position + 2*Vector3.up - tr.position, rollUp);
						rotation = Quaternion.Lerp(tr.rotation, cameraStoppedRotation, 6 * Time.fixedDeltaTime);
					}
					else
					{// camera right behind car
						rotation = Quaternion.Lerp(tr.rotation, lookObj.rotation,
						 (vp.reallyGroundedWheels > 1 ? 12f : 3f) * Time.fixedDeltaTime);
					}
				}
			}
			else
				rotation = Quaternion.Lerp(tr.rotation, Quaternion.LookRotation(vp.tr.position - tr.position), 3 * Time.fixedDeltaTime);

			tr.SetPositionAndRotation(newTrPos, rotation);
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