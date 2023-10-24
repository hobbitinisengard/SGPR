using UnityEngine;
using System.Collections;
using PathCreation;
using System.Linq;
using System.Globalization;
using System;

namespace RVP
{
	[RequireComponent(typeof(VehicleParent))]
	[DisallowMultipleComponent]
	[AddComponentMenu("RVP/AI/Follow AI", 0)]

	// Class for following AI
	public class FollowAI : MonoBehaviour
	{
		[NonSerialized]
		public PathCreator trackPathCreator;
		PathCreator pitsPathCreator;
		/// <summary>
		/// CPU takes control in pits
		/// </summary>
		public bool isCPU = false;
		/// <summary>
		/// CPU drives
		/// </summary>
		public bool selfDriving = false;
		Transform tr;
		Rigidbody rb;
		VehicleParent vp;
		VehicleAssist va;
		Vector4 tPos;
		private Vector4 tPos2;
		Vector3 pathDir;
		public float forwardTargetDot;
		public float tSpeed;
		public float lookAhead = 15;
		public float radius = 30;
		private Vector4 tPos0;
		public float stoppedTime;
		public float reverseTime;
		public float brakeTime;
		public float progress;
		public float pitsProgress;
		public float dist = 0;
		public float speedLimit = 999;
		public float speedLimitDist = 0;
		public float hardCornerDot = 0.7f;
		public float slowingCoeff = 1;
		float maxPhysicalSteerAngle = 5;
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
		public float rolledOverTime;
		private bool dumbBool;
		public AnimationCurve lookAheadMultCurve = new AnimationCurve();
		public AnimationCurve tSpeedCurve = new AnimationCurve();
		public AnimationCurve tSpeedExpCurve = new AnimationCurve();
		private bool searchForPits;
		float inPitsTime;
		public float driven;
		public float outOfTrackTime;
		public float outOfTrackRequiredTime = 2;
		public void SetCPU(bool val)
		{
			isCPU = val;
			if (isCPU)
			{
				GetComponent<VehicleParent>().steeringControl.unfiltered = true;
				GetComponent<BasicInput>().enabled = false;
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
			else
			{
				GetComponent<BasicInput>().enabled = true;
				GetComponent<VehicleParent>().steeringControl.unfiltered = false;
				if (isCPU)
				{
					for (int i = 0; i < 4; ++i)
					{
						vp.wheels[i].sidewaysFriction /= tyreMult;
						vp.wheels[i].forwardFriction /= tyreMult;
					}
				}
			}
		}
		private void Start()
		{
			if (!trackPathCreator)
			{
				trackPathCreator = GameObject.Find("RacingPath").GetComponent<PathCreator>();
			}
			tr = transform;
			rb = GetComponent<Rigidbody>();
			vp = GetComponent<VehicleParent>();
			va = GetComponent<VehicleAssist>();

			maxPhysicalSteerAngle = vp.steeringControl.steeredWheels[0].steerRangeMax;
			SetCPU(isCPU);
		}
		float GetDist(Collider[] racingPathHits)
		{
			float dist = 0;
			string closestLen = null;
			float min = 3 * radius;
			foreach (var hit in racingPathHits)
			{
				dist = Vector3.Distance(transform.position, hit.transform.position);
				if (dist < min)
				{
					min = dist;
					closestLen = hit.transform.name;
				}
			}
			if (closestLen != null)
				dist = float.Parse(closestLen, CultureInfo.InvariantCulture.NumberFormat);
			else
			{
				//Debug.LogError("OverlapSphere failed");
			}
			return dist;
		}
		void SetOutOfPits()
		{
			pitsPathCreator = null;
			pitsProgress = 0;
			searchForPits = false;
			if (!isCPU)
				selfDriving = false;
		}
		void FixedUpdate()
		{
			if (!trackPathCreator)
				return;

			rolledOverTime = va.rolledOver ? rolledOverTime + Time.fixedDeltaTime : 0;

			// Reset if stuck rolled over
			if (rolledOverTime > rollResetTime && rollResetTime >= 0)
			{
				StartCoroutine(ResetOnTrack());
			}
			Collider[] hits = Physics.OverlapSphere(transform.position, radius, 1 << Info.racingLineLayer);
			Collider[] pitsPathHits;
			float pitsDist = 0;
			if (pitsPathCreator)
			{
				if (Time.time - inPitsTime > 8)
				{
					SetOutOfPits();
					StartCoroutine(ResetOnTrack());
				}
				pitsPathHits = Physics.OverlapSphere(transform.position, radius, 1 << Info.pitsLineLayer);
				pitsDist = GetDist(pitsPathHits);

				if (pitsDist < pitsProgress)
					pitsDist = pitsProgress;
				pitsProgress = pitsDist;

				if (pitsDist + lookAhead > pitsPathCreator.path.length)
				{
					SetOutOfPits();
				}
			}
			else
			{
				if (searchForPits)
				{
					pitsPathHits = Physics.OverlapSphere(transform.position, radius, 1 << Info.pitsLineLayer);

					if (pitsPathHits.Length > 0)
					{
						pitsPathCreator = hits[0].transform.parent.GetComponent<PathCreator>();
						pitsDist = 0;
						pitsProgress = 0;
					}
				}
				var racingPathHits = Physics.OverlapSphere(transform.position, radius, 1 << Info.racingLineLayer);
				dist = GetDist(racingPathHits);

				if (progress / trackPathCreator.path.length < 0.9f && dist < progress)
				{
					dist = progress;
				}

				if (dist < progress + 2 * radius && progress != 0)
				{
					progress = dist;
					outOfTrackTime = 0;
				}
				else
				{
					outOfTrackTime += Time.fixedDeltaTime;
				}
			}
			
			bool onRoad = Physics.SphereCast(tr.position + Vector3.up, radius, Vector3.down, out var _, Mathf.Infinity, 1 << Info.racingLineLayer);

			// Reset if out of track (driving not on track
			if(onRoad)
			outOfTrackTime = onRoad ? outOfTrackTime + Time.fixedDeltaTime : 0;

			if (outOfTrackTime > outOfTrackRequiredTime)
			{
				StartCoroutine(ResetOnTrack());
			}
			

			if (vp.battery < 0.2f)
			{
				searchForPits = true;
			}

			if (selfDriving)
			{
				if (pitsPathCreator)
				{
					tPos0 = pitsPathCreator.path.GetPointAtDistance(pitsDist, EndOfPathInstruction.Stop);
					tPos = pitsPathCreator.path.GetPointAtDistance(pitsDist + lookAhead, EndOfPathInstruction.Stop);
					tPos2 = pitsPathCreator.path.GetPointAtDistance(pitsDist + lookAhead, EndOfPathInstruction.Stop);
				}
				else
				{
					tPos0 = trackPathCreator.path.GetPointAtDistance(dist);
					tPos = trackPathCreator.path.GetPointAtDistance(dist + lookAhead);
					tPos2 = trackPathCreator.path.GetPointAtDistance(dist + 30);
				}

				tPos0.y = transform.position.y;
				tPos.y = transform.position.y;
				tPos2.y = transform.position.y;
				Debug.DrawLine((Vector3)tPos, (Vector3)tPos + 100 * Vector3.up, Color.magenta);
				Debug.DrawLine((Vector3)tPos2, (Vector3)tPos2 + 100 * Vector3.up, Color.red);

				Vector3 targetDir = ((Vector3)tPos - transform.position).normalized;

				if (vp.batteryLoadingSnd.isPlaying)
					tSpeed = 22f;
				else
				{
					if (pitsPathCreator)
					{
						tSpeed = 150;
						speedLimit = 1024;
						speedLimitDist = -1;
					}
					else
					{
						float aheadSpeed = tSpeedExpCurve.Evaluate(Mathf.Abs(tPos2.w));
						if (aheadSpeed < speedLimit)
						{
							speedLimit = aheadSpeed;
							speedLimitDist = (dist + lookAheadMultCurve.Evaluate(vp.velMag) * lookAhead) % trackPathCreator.path.length;
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
					}
				}

				// Attempt to reverse if vehicle is stuck
				stoppedTime = (Mathf.Abs(vp.localVelocity.z) < 1
				&& vp.groundedWheels > 0) ? stoppedTime + Time.fixedDeltaTime : 0;

				if (!dumbBool && stoppedTime > 0)
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
					StartCoroutine(ResetOnTrack());
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

				float steerAngle = Vector3.SignedAngle(tr.forward, targetDir, tr.up);
				// Set steer input
				if (reverseTime == 0)
				{
					vp.SetSteer(Mathf.Sign(steerAngle) * Mathf.InverseLerp(0, maxPhysicalSteerAngle, Mathf.Abs(steerAngle)));
				}
				else
				{
					vp.SetSteer(-Mathf.Sign(steerAngle) * Mathf.InverseLerp(0, maxPhysicalSteerAngle, Mathf.Abs(steerAngle)));
				}
			}
		}
		public IEnumerator ResetOnTrack()
		{
			vp.engine.transmission.ShiftToGear(1);
			pitsProgress = 0;
			reverseAttempts = 0;
			outOfTrackTime = 0;
			reverseTime = 0;
			stoppedTime = 0;
			yield return new WaitForFixedUpdate();
			tr.position = tPos;
			tr.rotation = Quaternion.LookRotation(pathDir, RaceManager.worldUpDir);
			rb.velocity = Vector3.zero;
			rb.angularVelocity = Vector3.zero;
		}
		//IEnumerator ResetRotation()
		//{
		//	stoppedTime = 0;
		//	yield return new WaitForFixedUpdate();
		//	tr.position = tPos;
		//	tr.eulerAngles = new Vector3(0, transform.eulerAngles.y, 0);
		//	tr.Translate(Vector3.up, Space.World);
		//	rb.velocity = Vector3.zero;
		//	rb.angularVelocity = Vector3.zero;
		//}
		public void DriveThruPits(in PathCreator pitsPathCreator)
		{
			inPitsTime = Time.time;
			this.pitsPathCreator = pitsPathCreator;
			selfDriving = true;
		}
	}

}
