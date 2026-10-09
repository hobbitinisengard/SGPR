using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RVP
{
	[RequireComponent(typeof(Camera), typeof(AudioListener))]
	[DisallowMultipleComponent]
	[DefaultExecutionOrder(100)]
	[AddComponentMenu("RVP/Camera/Camera Control", 0)]
	public class CameraControl : MonoBehaviour
	{
		public InputActionReference moveRef;
		public InputActionReference changeCamRef;
		public LayerMask castMask;
		public bool isSkateCam;
		public float xInput;
		public float yInput;
		public float height;
		public float targetCamCarDistance;
		public enum Mode { Follow, Replay }
		Mode _mode;
		public Mode mode
		{
			get => _mode;
			set
			{
				if (value == Mode.Replay && (!vp || !vp.followAI || vp.followAI.replayCams.Count == 0))
					value = Mode.Follow;
				_mode = value;
				if (!cam || !vp) return;
				if (value == Mode.Follow) ResetFollowCamera();
				else cam.fieldOfView = 14;
			}
		}

		// Original presets 2/3/1: keep distance, elevation and response paired.
		static readonly float[] Distances = { 10, 15, 6 };
		static readonly float[] Elevations = { 18, 20, 13 };
		static readonly float[] ApproachScales = { 0.125f, 0.125f, 0.5f };
		const float SourceHz = OriginalVehiclePhysics.SourceTicksPerSecond;
		const float CollisionRadius = 1.25f; // camera_frame.cpp: query.radius = 125
		const float BaseLens = 2.09f; // 0x4005c28f
		readonly RaycastHit[] sweepHits = new RaycastHit[32];
		int preset;
		int collisionMask;
		int collisionBypassTicks;
		int settleTicks;
		float tickAccumulator;
		float twist;
		float lens = BaseLens;
		float contactHeight;
		Vector3 cameraPosition;
		Vector3 cameraTarget;
		Vector3 stabilizedUp;
		Vector3 cameraVelocity; // metres per source tick, not metres per second
		Vector3 drift;
		int accelerationFrame = -1;
		Vector3 cameraSide;
		Vector3 customCameraVelocity;
		bool followInitialized;
		bool wasCustomCamera;
		bool wasResetting;
		Transform tr;
		Camera cam;
		VehicleParent vp;
		Rigidbody targetBody;
		GameObject waterObj;
		float waterObjHeight;
		Coroutine switchTargetRoutine;

		void Awake()
		{
			tr = transform;
			cam = GetComponent<Camera>();
			cam.depthTextureMode |= DepthTextureMode.Depth;
			collisionMask = castMask.value & ~LayerMask.GetMask("InvisibleLevel", "Vehicles", "vehicles", "CarCarCollision");
			if (changeCamRef) changeCamRef.action.performed += ChangeFollowCamera;
		}
		void OnEnable()
		{
			switchTargetRoutine = StartCoroutine(AllowChangingTarget());
		}
		IEnumerator AllowChangingTarget()
		{
			waterObj = GameObject.Find("Water");
			if (waterObj) waterObjHeight = waterObj.transform.position.y + 2;
			yield return new WaitForSeconds(1);
			if (moveRef) moveRef.action.performed += SwitchTarget;
			switchTargetRoutine = null;
		}
		void OnDisable()
		{
			if (switchTargetRoutine != null) StopCoroutine(switchTargetRoutine);
			switchTargetRoutine = null;
			if (moveRef) moveRef.action.performed -= SwitchTarget;
		}
		void OnDestroy()
		{
			if (changeCamRef) changeCamRef.action.performed -= ChangeFollowCamera;
		}
		public void Disconnect()
		{
			vp = null;
			targetBody = null;
			followInitialized = false;
			enabled = false;
		}
		public void Connect(VehicleParent car = null, Mode mode = Mode.Follow)
		{
			if (!car) { Disconnect(); return; }
			vp = car;
			targetBody = car.rb ? car.rb : car.GetComponent<Rigidbody>();
			if (vp.followAI) vp.followAI.ResetCurCameraIdx();
			GetComponent<AudioListener>().velocityUpdateMode = AudioVelocityUpdateMode.Fixed;
			enabled = true;
			this.mode = mode;
		}
		void ChangeFollowCamera(InputAction.CallbackContext context)
		{
			if (!enabled || mode != Mode.Follow || F.I.chat.texting) return;
			preset = (preset + 1) % Distances.Length;
			UpdateLH();
		}
		public void UpdateLH()
		{
			targetCamCarDistance = Distances[preset];
			height = Mathf.Sin(Elevations[preset] * Mathf.Deg2Rad) * targetCamCarDistance;
			if (vp && mode == Mode.Follow) ResetFollowCamera();
		}
		Vector3 GetCameraTargetPosition()
		{
			return vp.customCam && vp.bodyObj ? vp.bodyObj.transform.position : vp.tr.position;
		}
		void ResetFollowCamera()
		{
			targetCamCarDistance = Distances[preset];
			height = Mathf.Sin(Elevations[preset] * Mathf.Deg2Rad) * targetCamCarDistance;
			stabilizedUp = vp.tr.up;
			cameraVelocity = drift = customCameraVelocity = Vector3.zero;
			accelerationFrame = -1;
			twist = 0;
			lens = BaseLens;
			settleTicks = 0;
			collisionBypassTicks = 300; // camera_select_mode
			tickAccumulator = 0;
			contactHeight = vp.tr.position.y;
			UpdateContactHeight();
			cameraTarget = vp.tr.position + Vector3.up * 1.5f;
			cameraPosition = DesiredCameraPosition(0);
			followInitialized = true;
			wasCustomCamera = false;
			PublishFollowCamera();
		}
		void Update()
		{
			if (waterObj) waterObj.SetActive(tr.position.y > waterObjHeight);
		}
		void FixedUpdate()
		{
			if (!vp || !targetBody) return;
			if (mode == Mode.Replay) { ReplayCam(); return; }
			bool resetting = vp.ghost && vp.ghost.justResetted;
			if (!followInitialized || (resetting && !wasResetting) || (wasCustomCamera && !vp.customCam))
				ResetFollowCamera();
			wasResetting = resetting;
			wasCustomCamera = vp.customCam;
			if (vp.customCam)
			{
				// Keep the Unity-authored tunnel/pit cameras independent of chase state.
				float t = 1 - Mathf.Exp(-3 * Time.fixedDeltaTime);
				Vector3 position = Vector3.SmoothDamp(tr.position, vp.customCam.transform.position,
					ref customCameraVelocity, 0.5f, 15, Time.fixedDeltaTime * 10);
				Vector3 aim = GetCameraTargetPosition() - position;
				Quaternion rotation = aim.sqrMagnitude > 0.000001f ? Quaternion.LookRotation(aim) : tr.rotation;
				tr.SetPositionAndRotation(position, Quaternion.Slerp(tr.rotation, rotation, t));
				return;
			}
			if (!F.I.chat.texting)
			{
				xInput = F.I.lookAxisInput.action.ReadValue<float>();
				yInput = -F.I.lookBackInput.action.ReadValue<float>();
			}
			tickAccumulator += Time.fixedDeltaTime * SourceHz;
			while (tickAccumulator >= 0.99999f)
			{
				tickAccumulator = Mathf.Max(0, tickAccumulator - 1);
				FollowCameraTick();
			}
			PublishFollowCamera();
		}
		Vector3 DesiredCameraPosition(float speedLift)
		{
			// camera_chase_tick: stabilized basis, orbit, gravity clearance,
			// body-height correction, then normalize to the preset's orbit radius.
			Vector3 gravity = Physics.gravity.sqrMagnitude > 0.000001f ? Physics.gravity.normalized : Vector3.down;
			Vector3 forward = vp.tr.forward - stabilizedUp * Vector3.Dot(stabilizedUp, vp.tr.forward);
			if (forward.sqrMagnitude < 0.000001f) forward = Vector3.ProjectOnPlane(tr.forward, gravity);
			if (forward.sqrMagnitude < 0.000001f) forward = Vector3.forward;
			forward.Normalize();
			Vector3 right = Vector3.Cross(forward, stabilizedUp).normalized;
			Vector3 orbit = Quaternion.AngleAxis(xInput * 90 + yInput * 180, Vector3.up) *
				(Quaternion.AngleAxis(Elevations[preset], Vector3.right) * Vector3.forward * targetCamCarDistance);
			Vector3 vertical = stabilizedUp * orbit.y;
			if (Vector3.Dot(gravity, stabilizedUp) >= 0) vertical = -vertical;
			Vector3 relative = right * orbit.x + vertical + forward * orbit.z;
			float gravityHeight = Vector3.Dot(gravity, relative);
			if (gravityHeight < 1) relative += gravity * (1 - gravityHeight);
			float bodyHeight = vp.originalVehiclePhysics != null ? vp.originalVehiclePhysics.CameraBodyHeight : 1.15f;
			relative -= Mathf.Max(0, bodyHeight - 1.15f) * vp.tr.up;
			relative.y += speedLift;
			return vp.tr.position - relative.normalized * targetCamCarDistance;
		}
		void UpdateContactHeight()
		{
			// Original chase uses stunt.launch_height, not the road surface height.
			// Keep the takeoff reference throughout the flight, including above valleys.
			if (vp.originalVehiclePhysics != null)
				contactHeight = vp.originalVehiclePhysics.CameraLaunchHeight;
			else if (vp.reallyGroundedWheels > 0)
				contactHeight = vp.tr.position.y;
		}
		void FollowCameraTick()
		{
			// 442815 -> 4023F0: camera_begin_outer_frame clears acceleration,
			// while preserving velocity. Multiple simulation ticks within the same
			// rendered frame share acceleration, as in the original outer scheduler.
			if (accelerationFrame != Time.frameCount)
			{
				drift = Vector3.zero;
				accelerationFrame = Time.frameCount;
			}
			Vector3 desiredPosition = Vector3.zero;
			// Source velocities use centimetres per 60 Hz tick (1 source unit = .6 m/s).
			float fraction = Mathf.Clamp01((targetBody.linearVelocity.magnitude / 0.6f - 83.333336f) * 0.024f);
			lens = BaseLens - fraction * 0.5f;
			twist = (twist - Vector3.Dot(cameraVelocity * 100, cameraSide) * 0.25f) * 0.5f;
			cameraTarget += (vp.tr.position + Vector3.up * 1.5f - cameraTarget) / 3;
			stabilizedUp += (vp.tr.up - stabilizedUp) * 0.1f;
			UpdateContactHeight();
			OriginalVehiclePhysics physics = vp.originalVehiclePhysics;
			float counter = physics != null ? physics.CameraContactCounter : vp.groundedWheels;
			int contactClass = physics != null ? physics.CameraContactClass : (vp.reallyGroundedWheels > 0 ? 0 : 2);
			Vector3 gravity = Physics.gravity.sqrMagnitude > 0.000001f ? Physics.gravity.normalized : Vector3.down;
			if (Vector3.Dot(gravity, stabilizedUp) >= 0) drift -= gravity * 0.02f;
			bool coasting = false;
			if ((cameraPosition - vp.tr.position).sqrMagnitude < 900)
			{
				if (counter <= 0 && Mathf.Abs(vp.tr.position.y - contactHeight) > 0.5f)
				{
					drift *= 0.9f;
					drift -= gravity * 0.02f;
					if (settleTicks < 30) settleTicks += 2;
					coasting = true;
				}
				else if (contactClass == 2)
				{
					if (settleTicks < 30) settleTicks++;
					coasting = counter < 4;
					if (coasting) drift *= 0.9f;
				}
				else if (contactClass == 1)
				{
					if (settleTicks < 30) settleTicks++;
					coasting = counter >= 2;
					if (coasting) drift *= 0.9f;
				}
			}
			if (!coasting)
			{
				drift = Vector3.zero;
				desiredPosition = DesiredCameraPosition(fraction * 0.75f);
				Vector3 motion = desiredPosition - cameraPosition;
				float speed = motion.magnitude * ApproachScales[preset];
				float limit = 2.56f;
				if (settleTicks > 0)
				{
					limit = Mathf.Max(0, (30 - settleTicks) * (8.533333f * 0.01f));
					settleTicks--;
				}
				cameraVelocity = motion.normalized * Mathf.Min(speed, limit);
			}
			if ((cameraPosition - vp.tr.position).sqrMagnitude > 625) collisionBypassTicks = 600;
			if (collisionBypassTicks > 0)
			{
				cameraPosition += cameraVelocity;
				collisionBypassTicks--;
			}
			else MoveWithTrackCollision();
			// camera_integrate_velocity runs AFTER camera_move.
			cameraVelocity = (cameraVelocity + drift) * 0.95f;
			for (int axis = 0; axis < 3; axis++)
				if (Mathf.Abs(cameraVelocity[axis]) < 0.001f) cameraVelocity[axis] = 0;
		}
		void MoveWithTrackCollision()
		{
			Vector3 remaining = cameraVelocity;
			for (int attempt = 0; attempt < 33 && remaining.sqrMagnitude > 0.00000001f; attempt++)
			{
				float distance = remaining.magnitude;
				int count = Physics.SphereCastNonAlloc(cameraPosition, CollisionRadius, remaining / distance,
					sweepHits, distance, collisionMask, QueryTriggerInteraction.Ignore);
				int nearest = -1;
				float nearestDistance = distance;
				for (int i = 0; i < count; i++)
				{
					if (sweepHits[i].rigidbody || sweepHits[i].distance > nearestDistance) continue;
					nearest = i;
					nearestDistance = sweepHits[i].distance;
				}
				if (nearest < 0) { cameraPosition += remaining; return; }
				RaycastHit hit = sweepHits[nearest];
				float travel = Mathf.Max(0, hit.distance - 0.001f);
				cameraPosition += remaining / distance * travel;
				remaining *= 1 - travel / distance;
				float inward = Vector3.Dot(remaining, hit.normal);
				if (inward < 0) remaining -= hit.normal * inward * 1.01f;
			}
		}
		Quaternion FollowViewRotation(Vector3 position, Vector3 target, float roll)
		{
			Vector3 direction = target - position;
			return direction.sqrMagnitude > 0.000001f
				? Quaternion.LookRotation(direction, Vector3.up) * Quaternion.AngleAxis(roll, Vector3.forward) : tr.rotation;
		}
		void PublishFollowCamera()
		{
			Quaternion rotation = FollowViewRotation(cameraPosition, cameraTarget, twist);
			cameraSide = rotation * Vector3.right;
			cam.fieldOfView = 2 * Mathf.Atan(1 / lens) * Mathf.Rad2Deg;
			tr.SetPositionAndRotation(cameraPosition, rotation);
		}
		void ReplayCam()
		{
			if (!vp.followAI || !vp.followAI.currentCam.cam) return;
			tr.position = vp.followAI.currentCam.cam.transform.position;
			Vector3 target = GetCameraTargetPosition() - Time.fixedDeltaTime * targetBody.linearVelocity;
			if ((target - tr.position).sqrMagnitude > 0.000001f)
				tr.rotation = Quaternion.LookRotation(target - tr.position);
		}
		void SwitchTarget(InputAction.CallbackContext context)
		{
			if (F.I.gameMode != GameMode.Multiplayer || RaceManager.I.playerCar == null ||
				RaceManager.I.playerCar.raceBox.enabled || F.I.chat.texting || !vp) return;
			float move = moveRef.action.ReadValue<Vector2>().x;
			if (Mathf.Abs(move) <= 0.5f || F.I.s_cars.Count == 0) return;
			int index = F.I.s_cars.FindIndex(c => c.transform.name == vp.name);
			index = F.Wraparound(index + (move > 0 ? 1 : -1), 0, F.I.s_cars.Count - 1);
			if (F.I.s_cars[index] == vp) return;
			Connect(F.I.s_cars[index], Mode.Replay);
			RaceManager.I.hud.infoText.AddMessage(new Message(vp.transform.name, BottomInfoType.NEW_CAMERA_TARGET));
		}
		public void SetInput(float x, float y)
		{
			xInput = x;
			yInput = y;
		}
	}
}
