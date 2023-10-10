using UnityEngine;
using System.Collections;
using PathCreation;
using UnityEngine.UIElements.Experimental;
using System.Linq;
using System.Globalization;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ToolTip;
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
		Vector4 tPos;
		private Vector4 tPos2;
		Vector3 pathDir;
		public float forwardTargetDot;
		public float RightTargetDot; 
		public float tSpeed;
		public float lookAhead = 15;
		private Vector4 tPos0;
		public float stoppedTime;
		public float reverseTime;
		public float brakeTime;
		public float progress;
		public float dist = 0;
		public float speedLimit = 999;
		public float speedLimitDist = 0;
		public float hardCornerDot = 0.7f;
		public float slowingCoeff = 1;

		// CPU settings
		float tyreMult = 1;
		float lowSpeed = 30;
		public int cpuLevel;
		public bool SGPshifting;
		[Tooltip("Time limit in seconds which the vehicle is stuck before attempting to reverse")]
		public float stopTimeReverse = 5;

		[Tooltip("Duration in seconds the vehicle will reverse after getting stuck")]
		public float reverseAttemptTime = 6;

		[Tooltip("How many times the vehicle will attempt reversing before resetting, -1 = no reset")]
		public int resetReverseCount = 1;
		int reverseAttempts;

		[Tooltip("Seconds a vehicle will be rolled over before resetting, -1 = no reset")]
		public float rollResetTime = 3;
		float rolledOverTime;
		private bool dumbBool;
		public AnimationCurve lookAheadMultCurve = new AnimationCurve();
		public AnimationCurve tSpeedCurve = new AnimationCurve();
		public AnimationCurve tSpeedExpCurve = new AnimationCurve();

		private void OnEnable()
		{
			tr = transform;
			rb = GetComponent<Rigidbody>();
			vp = GetComponent<VehicleParent>();
			va = GetComponent<VehicleAssist>();
			GetComponent<BasicInput>().enabled = false;
			GetComponent<VehicleParent>().steeringControl.CPUmode = true;

			if (cpuLevel == 3)
			{
				lowSpeed = 42;
				tyreMult = 1.5f;
			}
			if (cpuLevel == 2)
			{
				lowSpeed = 38;
				tyreMult = 1.5f;
			}
			if (cpuLevel == 1)
			{
				lowSpeed = 35;
				tyreMult = 1.2f;
			}
			if (cpuLevel == 0)
			{
				lowSpeed = 30;
				tyreMult = 1;
			}
			for (int i = 0; i < 4; ++i)
			{
				vp.wheels[i].sidewaysFriction *= tyreMult;
				vp.wheels[i].forwardFriction *= tyreMult;
			}
			var keys = tSpeedExpCurve.keys;
			keys[keys.Count() - 1].value = lowSpeed;
			tSpeedExpCurve.keys = keys;
		}
		private void OnDisable()
		{
			GetComponent<BasicInput>().enabled = true;
			GetComponent<VehicleParent>().steeringControl.CPUmode = false;
			for (int i = 0; i < 4; ++i)
			{
				vp.wheels[i].sidewaysFriction /= tyreMult;
				vp.wheels[i].forwardFriction /= tyreMult;
			}
		}

		void FixedUpdate()
		{
			if (!pathCreator)
				return;

			float radius = 30;
			var hits = Physics.OverlapSphere(transform.position, radius, 1 << Info.racingLineLayer);
			string closestLen = null;
			float min = 3*radius;
			foreach(var hit in hits)
			{
				float dist = Vector3.Distance(transform.position, hit.transform.position);
				if (dist < min)
				{
					min = dist;
					closestLen = hit.transform.name;
				}
			}
			if(closestLen != null)
				dist = float.Parse(closestLen, CultureInfo.InvariantCulture.NumberFormat);
			else
			{
				Debug.LogError("OverlapSphere failed");
			}
			if (progress/pathCreator.path.length < 0.9f && dist < progress)
				dist = progress;
			progress = dist;
			//lookAhead =  lookAheadMultCurve.Evaluate(vp.velMag);
			tPos0 = pathCreator.path.GetPointAtDistance(dist);
			tPos = pathCreator.path.GetPointAtDistance(dist + lookAhead);
			tPos2 = pathCreator.path.GetPointAtDistance(dist +  30);

			tPos0.y = transform.position.y;
			tPos.y = transform.position.y;
			tPos2.y = transform.position.y;

			Debug.DrawLine((Vector3)tPos, (Vector3)tPos + 100*Vector3.up, Color.magenta);
			Debug.DrawLine((Vector3)tPos2, (Vector3)tPos2 + 100*Vector3.up, Color.red);
			
			// targetDir for steering control
			Vector3 targetDir = ((Vector3)tPos - transform.position).normalized;
			RightTargetDot = Vector3.Dot(vp.rightDir, targetDir);

			// targetDir2 for speed control
			//Vector3 targetDir2 = (targetPos2 - transform.position).normalized;
			//tspeed = tSpeedCurve.Evaluate(Vector3.Dot(vp.rightDir, targetDir2)); 
			float aheadSpeed = tSpeedExpCurve.Evaluate(Mathf.Abs(tPos2.w));
			if (aheadSpeed < speedLimit)
			{
				speedLimit = aheadSpeed;
				speedLimitDist = (dist + lookAheadMultCurve.Evaluate(vp.velMag) * lookAhead) % pathCreator.path.length;
			}
			
			if (dist > speedLimitDist)
			{
				tSpeed = tSpeedExpCurve.Evaluate(Mathf.Abs(tPos0.w));
				speedLimit = 1024;
				speedLimitDist = -1;
			}
			else
			{
				//var pos = pathCreator.path.GetPointAtDistance(speedLimitDist);
				//Debug.DrawLine((Vector3)pos, (Vector3)pos + 100 * Vector3.up, Color.blue);
				tSpeed = speedLimit;

			}

			// Attempt to reverse if vehicle is stuck
			stoppedTime = (Mathf.Abs(vp.localVelocity.z) < 1 
			&& vp.groundedWheels > 0) ? stoppedTime + Time.fixedDeltaTime : 0;

			if(!dumbBool && stoppedTime > 0)
			{
				dumbBool = true;
				vp.SetAccel(0);
			}
			if (stoppedTime > stopTimeReverse && reverseTime == 0)
			{
				dumbBool = false;
				reverseTime = reverseAttemptTime;
				reverseAttempts++;
			}

			// Reset if reversed too many times
			if (reverseAttempts > resetReverseCount && resetReverseCount >= 0)
			{
				StartCoroutine(ReverseReset());
			}

			reverseTime = Mathf.Max(0, reverseTime - Time.fixedDeltaTime);

			
			// Set accel input
			if (vp.velMag < tSpeed && reverseTime == 0)
			{
				vp.SetAccel(1);
			}
			else
			{
				vp.SetAccel(0f);
			}

			// Set brake input
			if (reverseTime == 0 && brakeTime == 0)
			{
				if (vp.velMag > tSpeed)
				{
					vp.SetBrake(1);
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
						vp.SetBrake(1 - Mathf.Clamp01(Vector3.Distance(tr.position, tPos)));
					}
				}
			}
			
			// Set steer input
			if (reverseTime == 0)
			{
				vp.SetSteer(Mathf.Abs(Mathf.Pow(RightTargetDot, (tr.position - (Vector3)tPos).sqrMagnitude > 20 ? 1 : 2))
					* Mathf.Sign(RightTargetDot));
			}
			else
			{
				vp.SetSteer(-Mathf.Sign(RightTargetDot));
			}
			
			//if (reverseTime == 0)
			//{
			//	vp.SetSteer(Mathf.InverseLerp(0, physicalMaxSteeringAngle, Mathf.Abs(RightTargetDot))
			//		* Mathf.Sign(RightTargetDot));
			//}
			//else
			//{
			//	vp.SetSteer(-Mathf.InverseLerp(0, physicalMaxSteeringAngle, Mathf.Abs(RightTargetDot))
			//		* Mathf.Sign(RightTargetDot));
			//}

			rolledOverTime = va.rolledOver ? rolledOverTime + Time.fixedDeltaTime : 0;

			// Reset if stuck rolled over
			if (rolledOverTime > rollResetTime && rollResetTime >= 0)
			{
				StartCoroutine(ResetRotation());
			}
		}
		public IEnumerator ReverseReset()
		{
			reverseAttempts = 0;
			reverseTime = 0;
			stoppedTime = 0;
			yield return new WaitForFixedUpdate();
			tr.position = tPos;
			tr.rotation = Quaternion.LookRotation(pathDir, RaceManager.worldUpDir);
			rb.velocity = Vector3.zero;
			rb.angularVelocity = Vector3.zero;
		}
		IEnumerator ResetRotation()
		{
			stoppedTime = 0;
			yield return new WaitForFixedUpdate();
			tr.position = tPos;
			tr.eulerAngles = new Vector3(0, transform.eulerAngles.y, 0);
			tr.Translate(Vector3.up, Space.World);
			rb.velocity = Vector3.zero;
			rb.angularVelocity = Vector3.zero;
		}
	}
	
}
