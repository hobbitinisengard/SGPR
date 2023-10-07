using UnityEngine;
using System.Collections;
using PathCreation;
using static UnityEngine.GraphicsBuffer;

namespace RVP
{
	[RequireComponent(typeof(VehicleParent))]
	[DisallowMultipleComponent]
	[AddComponentMenu("RVP/AI/Follow AI", 0)]

	// Class for following AI
	public class FollowAI : MonoBehaviour
	{
		public PathCreator pathCreator;
		Transform tr;
		Rigidbody rb;
		VehicleParent vp;
		VehicleAssist va;
		Vector3 targetPos;
		Vector3 dirToTarget;
		bool close;

		[Tooltip("Percentage of maximum speed to drive at")]
		[Range(0, 1)]
		public float speed = 1;
		float prevSpeed;
		public float targetVelocity = -1;
		float speedLimit = 1;
		float brakeTime;

		float lookDot; // Dot product of forward direction and dirToTarget
		float steerDot; // Dot product of right direction and dirToTarget

		float stoppedTime;
		float reverseTime;

		[Tooltip("Time limit in seconds which the vehicle is stuck before attempting to reverse")]
		public float stopTimeReverse = 1;

		[Tooltip("Duration in seconds the vehicle will reverse after getting stuck")]
		public float reverseAttemptTime = 1;

		[Tooltip("How many times the vehicle will attempt reversing before resetting, -1 = no reset")]
		public int resetReverseCount = 1;
		int reverseAttempts;

		[Tooltip("Seconds a vehicle will be rolled over before resetting, -1 = no reset")]
		public float rollResetTime = 3;
		float rolledOverTime;

		void Start()
		{
			tr = transform;
			rb = GetComponent<Rigidbody>();
			vp = GetComponent<VehicleParent>();
			va = GetComponent<VehicleAssist>();
		}

		void FixedUpdate()
		{
			if (!pathCreator)
				return;
			float dist = pathCreator.path.GetClosestDistanceAlongPath(transform.position);
			targetPos = pathCreator.path.GetPointAtDistance(dist + 1, EndOfPathInstruction.Loop);
			dirToTarget = pathCreator.path.GetDirectionAtDistance(dist + 1, EndOfPathInstruction.Loop);

			lookDot = Vector3.Dot(vp.forwardDir, dirToTarget);
			steerDot = Vector3.Dot(vp.rightDir, dirToTarget);

			// Attempt to reverse if vehicle is stuck
			stoppedTime = Mathf.Abs(vp.localVelocity.z) < 1 &&
				vp.groundedWheels > 0 ? stoppedTime + Time.fixedDeltaTime : 0;

			if (stoppedTime > stopTimeReverse && reverseTime == 0)
			{
				reverseTime = reverseAttemptTime;
				reverseAttempts++;
			}

			// Reset if reversed too many times
			if (reverseAttempts > resetReverseCount && resetReverseCount >= 0)
			{
				StartCoroutine(ReverseReset());
			}

			reverseTime = Mathf.Max(0, reverseTime - Time.fixedDeltaTime);

			if (targetVelocity > 0)
			{
				speedLimit = Mathf.Clamp01(targetVelocity - vp.localVelocity.z);
			}
			else
			{
				speedLimit = 1;
			}

			// Set accel input
			if (!close && (lookDot > 0 || vp.localVelocity.z < 5) && vp.groundedWheels > 0 && reverseTime == 0)
			{
				vp.SetAccel(speed * speedLimit);
			}
			else
			{
				vp.SetAccel(0);
			}

			// Set brake input
			if (reverseTime == 0 && brakeTime == 0 && !(close && vp.localVelocity.z > 0.1f))
			{
				if (lookDot < 0.5f && lookDot > 0 && vp.localVelocity.z > 10)
				{
					vp.SetBrake(0.5f - lookDot);
				}
				else
				{
					vp.SetBrake(0);
				}
			}
			else
			{
				if (reverseTime > 0)
				{
					vp.SetBrake(1);
				}
				else
				{
					if (brakeTime > 0)
					{
						vp.SetBrake(brakeTime * 0.2f);
					}
					else
					{
						vp.SetBrake(1 - Mathf.Clamp01(Vector3.Distance(tr.position, target.position) / Mathf.Max(0.01f, followDistance)));
					}
				}
			}

			// Set steer input
			if (reverseTime == 0)
			{
				vp.SetSteer(Mathf.Abs(Mathf.Pow(steerDot, (tr.position - target.position).sqrMagnitude > 20 ? 1 : 2)) * Mathf.Sign(steerDot));
			}
			else
			{
				vp.SetSteer(-Mathf.Sign(steerDot) * (close ? 0 : 1));
			}

			// Set ebrake input
			if ((close && vp.localVelocity.z <= 0.1f) || (lookDot <= 0 && vp.velMag > 20))
			{
				vp.SetEbrake(1);
			}
			else
			{
				vp.SetEbrake(0);
			}
			rolledOverTime = va.rolledOver ? rolledOverTime + Time.fixedDeltaTime : 0;

			// Reset if stuck rolled over
			if (rolledOverTime > rollResetTime && rollResetTime >= 0)
			{
				StartCoroutine(ResetRotation());
			}

		}

		
	
		IEnumerator ReverseReset()
		{
			reverseAttempts = 0;
			reverseTime = 0;
			yield return new WaitForFixedUpdate();
			tr.position = targetPoint;
			tr.rotation = Quaternion.LookRotation(targetIsWaypoint ? (targetWaypoint.nextPoint.transform.position - targetPoint).normalized : Vector3.forward, RaceManager.worldUpDir);
			rb.velocity = Vector3.zero;
			rb.angularVelocity = Vector3.zero;
		}
		IEnumerator ResetRotation()
		{
			yield return new WaitForFixedUpdate();
			tr.eulerAngles = new Vector3(0, transform.eulerAngles.y, 0);
			tr.Translate(Vector3.up, Space.World);
			rb.velocity = Vector3.zero;
			rb.angularVelocity = Vector3.zero;
		}
	}
	
}
