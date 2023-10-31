﻿using UnityEngine;
using System.Collections;
using PathCreation;
using System.Linq;
using System.Globalization;
using System;
using System.Collections.Generic;

namespace RVP
{
	[RequireComponent(typeof(VehicleParent))]
	[DisallowMultipleComponent]
	[AddComponentMenu("RVP/AI/Follow AI", 0)]

	// Class for following AI
	public class FollowAI : MonoBehaviour
	{
		List<int> stuntPoints;
		public List<ReplayCamStruct> replayCams { get; private set; }
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
		Vector4 tPos;
		private Vector4 tPos2;
		public float forwardTargetDot;
		public float tSpeed;
		public float lookAheadBase = 15;
		public float radius = 30;
		private Vector4 tPos0;
		public int curWaypointIdx;
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
		public AnimationCurve lookAheadSteerCurve = new AnimationCurve();
		public AnimationCurve tSpeedExpCurve = new AnimationCurve();
		public bool searchForPits;
		float inPitsTime;
		public float outOfTrackTime;
		public float outOfTrackRequiredTime = 2;
		private Coroutine ghostCo;

		private float steerAngle;
		public int curStuntpointIdx;
		public int curReplayPointIdx;
		public bool aiStuntingProc;

		public float ProgressPercent()
		{
			return progress / trackPathCreator.path.length;
		}
		public void SetCPU(bool val)
		{
			isCPU = val;
			selfDriving = val;
			if (isCPU)
			{
				GetComponent<VehicleParent>().steeringControl.unfiltered = true;
				GetComponent<BasicInput>().enabled = false;
				if (cpuLevel == 3)
				{
					lowSpeed = 30;
					tyreMult = 1.5f;
				}
				if (cpuLevel == 2)
				{
					lowSpeed = 30;
					tyreMult = 1.5f;
				}
				if (cpuLevel == 1)
				{
					lowSpeed = 30;
					tyreMult = 1.5f;
				}
				if (cpuLevel == 0)
				{
					lowSpeed = 35;
					tyreMult = 1.5f;
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
		public void AssignPath(in PathCreator path, ref List<int> stuntpointsContainer, ref List<ReplayCamStruct> replayCams)
		{
			this.stuntPoints = stuntpointsContainer;
			this.replayCams = replayCams;
			trackPathCreator = path;
			this.enabled = true;
		}
		private void Awake()
		{
			tr = transform;
			rb = GetComponent<Rigidbody>();
			vp = GetComponent<VehicleParent>();

			maxPhysicalSteerAngle = vp.steeringControl.steeredWheels[0].steerRangeMax;

			var racingPathHits = Physics.OverlapSphere(transform.position, radius, 1 << Info.racingLineLayer);
			dist = GetDist(racingPathHits);
			progress = dist;
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
		void OutOfPits()
		{
			speedLimit = 1024;
			speedLimitDist = -1;
			pitsPathCreator = null;
			pitsProgress = 0;
			searchForPits = false;
			if (!isCPU)
				selfDriving = false;
		}
		void FixedUpdate()
		{
			if (!trackPathCreator)
			{
				Debug.LogError("No path assigned");
				Debug.Break();
				return;
			}
			if(vp.countdownTimer>0 && isCPU)
			{
				vp.SetAccel(UnityEngine.Random.value > 0.5 ? 1 : 0);
				return;
			}
			//if(Input.GetKeyDown(KeyCode.Alpha1))
			//{
			//	StartCoroutine(ResetOnTrack());
			//}
			rolledOverTime = vp.crashing ? rolledOverTime + Time.fixedDeltaTime : 0;

			// Reset if stuck rolled over
			if (rolledOverTime > rollResetTime && rollResetTime >= 0)
			{
				StartCoroutine(ResetOnTrack());
			}
			bool onRoad = Physics.Raycast(tr.position + Vector3.up, Vector3.down, out var _, Mathf.Infinity, 1 << Info.roadLayer);
			if (onRoad)
			{
				if (vp.groundedWheels == 0)
					outOfTrackTime = 0;
			}
			else
				outOfTrackTime += Time.fixedDeltaTime;

			if (vp.groundedWheels == 4 && vp.velMag > 10 && Vector3.Dot(vp.forwardDir, trackPathCreator.path.GetDirectionAtDistance(dist)) < -0.5f)
			{ // wrong way driving
				outOfTrackTime += Time.fixedDeltaTime;
			}
			if (outOfTrackTime > outOfTrackRequiredTime)
			{
				StartCoroutine(ResetOnTrack());
			}

			Collider[] pitsPathHits;
			float pitsDist = 0;
			if (pitsPathCreator)
			{
				if (Time.time - inPitsTime > 30)
				{
					OutOfPits();
					StartCoroutine(ResetOnTrack());
				}
				pitsPathHits = Physics.OverlapSphere(transform.position, radius, 1 << Info.pitsLineLayer);
				pitsDist = GetDist(pitsPathHits); // pitsDist

				if (pitsDist < pitsProgress)
					pitsDist = pitsProgress;
				pitsProgress = pitsDist;			//pitsProgress

				if (pitsDist + lookAheadBase > pitsPathCreator.path.length)
				{
					OutOfPits();
				}
			}
			else
			{
				if (searchForPits)
				{
					pitsPathHits = Physics.OverlapSphere(transform.position, radius, 1 << Info.pitsLineLayer);

					if (pitsPathHits.Length > 0)
					{
						pitsPathCreator = pitsPathHits[0].transform.parent.GetComponent<PathCreator>();
						pitsDist = 0;
						pitsProgress = 0;
						searchForPits = false;
						inPitsTime = Time.time;
					}
				}
				var racingPathHits = Physics.OverlapSphere(transform.position, radius, 1 << Info.racingLineLayer);

				dist = GetDist(racingPathHits); 

				if (dist < progress)
				{
					dist = progress;
				}
				if (dist < progress + 2 * radius)
				{
					progress = dist;
				}
				else
				{
					outOfTrackTime += Time.fixedDeltaTime;
				}
				
			}
			if (selfDriving)
			{
				if (vp.battery < 0.2f)
				{
					searchForPits = true;
				}
				if (pitsPathCreator)
				{
					tPos0 = pitsPathCreator.path.GetPointAtDistance(pitsDist, EndOfPathInstruction.Stop);
					tPos = pitsPathCreator.path.GetPointAtDistance(pitsDist + 15, EndOfPathInstruction.Stop);
					tPos2 = pitsPathCreator.path.GetPointAtDistance(pitsDist + 30, EndOfPathInstruction.Stop);
				}
				else
				{
					tPos0 = trackPathCreator.path.GetPointAtDistance(dist);
					if (stuntPoints[curStuntpointIdx] < progress)
					{
						if (curStuntpointIdx < stuntPoints.Count - 1)
							++curStuntpointIdx;
					}
					if (replayCams[curReplayPointIdx].dist < vp.followAI.progress)
					{
						if (curReplayPointIdx < replayCams.Count - 1)
							++curReplayPointIdx;
					}
					//if (waypointsContainer[curWaypointIdx] < progress + lookAhead)
					//{
					//	if (curWaypointIdx == waypointsContainer.Count - 1)
					//	{
					//		tPos = trackPathCreator.path.GetPointAtDistance(0);
					//	}
					//	else
					//		++curWaypointIdx;
					//	tPos = trackPathCreator.path.GetPointAtDistance(waypointsContainer[curWaypointIdx]);
					//}
					tPos = trackPathCreator.path.GetPointAtDistance(dist +  lookAheadBase * lookAheadSteerCurve.Evaluate(vp.velMag));
					tPos2 = trackPathCreator.path.GetPointAtDistance(dist + lookAheadBase * lookAheadMultCurve.Evaluate(vp.velMag));
				}

				tPos0.y = transform.position.y;
				tPos.y = transform.position.y;
				tPos2.y = transform.position.y;
				Debug.DrawLine((Vector3)tPos, (Vector3)tPos + 100 * Vector3.up, Color.magenta);
				Debug.DrawLine((Vector3)tPos2, (Vector3)tPos2 + 100 * Vector3.up, Color.red);


				if (pitsPathCreator)
				{
					if(pitsProgress > 25)
						tSpeed = 22f;
					if (pitsProgress > 225)
						tSpeed = 80;
				}
				else
				{
					{
						float aheadSpeed = tSpeedExpCurve.Evaluate(Mathf.Abs(tPos2.w));
						if (aheadSpeed < speedLimit)
						{
							speedLimit = aheadSpeed;
							speedLimitDist = (dist + lookAheadBase * lookAheadMultCurve.Evaluate(vp.velMag));
						}

						if (dist > speedLimitDist)
						{
							tSpeed = tSpeedExpCurve.Evaluate(Mathf.Abs(tPos0.w));
							speedLimit = 999;
							speedLimitDist = -1;
						}
						else
						{
							var pos = trackPathCreator.path.GetPointAtDistance(speedLimitDist);
							Debug.DrawLine((Vector3)pos, (Vector3)pos + 100 * Vector3.up, Color.blue);
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


				if(vp.groundedWheels > 0)
				{
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
					if (stuntPoints[curStuntpointIdx] - progress > 0 && stuntPoints[curStuntpointIdx] - progress < 15)
					{
						if (!aiStuntingProc)
							StartCoroutine(AIStuntingProc());
						Vector3 forward = trackPathCreator.path.GetDirectionAtDistance(progress);
						steerAngle = Vector3.SignedAngle(F.Vec3Flatten(tr.forward), F.Vec3Flatten(forward), Vector3.up);
					}
					else
					{
						Vector3 targetDir = ((Vector3)tPos - transform.position).normalized;
						steerAngle = Vector3.SignedAngle(F.Vec3Flatten(tr.forward), F.Vec3Flatten(targetDir), Vector3.up);
					}
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
		}
		public IEnumerator AIStuntingProc()
		{
			aiStuntingProc = true;
			float waitTimer = 1;
			while(waitTimer > 0)
			{
				float stuntTimer = .5f;
				vp.SetSGPShift(true);
				if (vp.groundedWheels == 0)
				{
					vp.SetRoll(0);
					vp.SetSteer(0);
					vp.SetAccel(0);
					vp.SetBrake(0);
					waitTimer = 0;
					int type = Mathf.RoundToInt(3*UnityEngine.Random.value);
					int val = (type > 1) ? 1 : (UnityEngine.Random.value > 0.5f) ? 1 : -1;
					while (stuntTimer > 0)
					{
						switch(type)
						{
							case 0:
								vp.SetRoll(val); // -1 or 1
								break;
							case 1:
								vp.SetSteer(val); // -1 or 1
								break;
							case 2:
								vp.SetAccel(val); // 1
								break;
							case 3:
								vp.SetBrake(val); // 1
								break;
							default:
								break;
						}
						stuntTimer -= Time.fixedDeltaTime;
						yield return new WaitForFixedUpdate();
					}
				}
				waitTimer -= Time.fixedDeltaTime;
				yield return new WaitForFixedUpdate();
			}
			vp.SetSGPShift(false);
			aiStuntingProc = false;
		}
		public IEnumerator ResetOnTrack()
		{
			if (progress == 0)
				yield break;
			GetComponent<RaceBox>().ResetOnTrack();
			vp.engine.transmission.ShiftToGear(1);
			rolledOverTime = 0;
			pitsProgress = 0;
			reverseAttempts = 0;
			outOfTrackTime = 0;
			reverseTime = 0;
			stoppedTime = 0;
			float resetDist = progress;
			Vector3 resetPos = trackPathCreator.path.GetPointAtDistance(resetDist);
			while(!Physics.Raycast(resetPos + Vector3.up, Vector3.down, out var _, Mathf.Infinity,  1 << Info.roadLayer))
			{
				resetDist += 30;
				resetPos = trackPathCreator.path.GetPointAtDistance(resetDist);
			}
			tr.position = resetPos + Vector3.up;
			tr.rotation = Quaternion.LookRotation(trackPathCreator.path.GetDirectionAtDistance(progress), Vector3.up);
			if (ghostCo != null)
				StopCoroutine(ghostCo);
			ghostCo = StartCoroutine(GetComponent<Ghost>().ResetSeq());
			yield return new WaitForFixedUpdate();
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
