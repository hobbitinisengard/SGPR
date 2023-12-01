using UnityEngine;
using System.Collections;
using PathCreation;
using System.Linq;
using System.Globalization;
using System.Collections.Generic;
using System;

namespace RVP
{

	[RequireComponent(typeof(VehicleParent))]
	[DisallowMultipleComponent]
	[AddComponentMenu("RVP/AI/Follow AI", 0)]

	// Class for following AI
	public class FollowAI : MonoBehaviour
	{
		//struct PointAtDistanceJob : IJobParallelFor
		//{
		//	public NativeArray<float> dists;
		//	public NativeArray<Vector4> results;
		//	public Action<float, EndOfPathInstruction, Vector4> processFunction;
		//	public void Execute(int index)
		//	{
		//		// You can perform the same operations as an external method here
		//		results[index] = processFunction(dists[index], EndOfPathInstruction.Loop);
		//	}

		//	private Vector3 PerformProcessing(Vector3 input)
		//	{
		//		// Your processing logic here
		//		return input * 2;
		//	}
		//}
		List<int> stuntPoints;
		List<int> waypointsContainer;
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
		public float cpuSmoothCoeff = 10;
		public float cpuFastCoeff = 50;
		private bool revvingCo;

		public float ProgressPercent()
		{
			return progress / trackPathCreator.path.length;
		}
		public void SetCPU(bool val)
		{
			cpuLevel = Info.s_cpuLevel;
			if (val == isCPU)
				return;
			isCPU = val;
			selfDriving = val;
			if (isCPU)
			{
				GetComponent<BasicInput>().enabled = false;
				if (cpuLevel > 75) // elite
				{
					lowSpeed = UnityEngine.Random.value * 2 + 38; // 38-40
					tyreMult = 2f;
				}
				else if (cpuLevel > 50) //hard
				{
					lowSpeed = UnityEngine.Random.value * 2 + 36; // 36-38
					tyreMult = 1.5f;
				}
				else if (cpuLevel > 25) // medium
				{
					lowSpeed = UnityEngine.Random.value * 2 + 30; // 30-32
					tyreMult = 1.2f;
				}
				else if (cpuLevel > 0) // easy
				{
					lowSpeed = UnityEngine.Random.value * 2 + 28; // 28-30
					tyreMult = 1f;
				}
				else // beginner
				{
					lowSpeed = UnityEngine.Random.value * 3 + 25; // 25-28
					tyreMult = 1f;
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
				for (int i = 0; i < 4; ++i)
				{
					vp.wheels[i].sidewaysFriction /= tyreMult;
					vp.wheels[i].forwardFriction /= tyreMult;
				}
			}
		}
		public void AssignPath(in PathCreator path, ref List<int> stuntpointsContainer, ref List<ReplayCamStruct> replayCams, ref List<int> waypointsContainer)
		{
			this.waypointsContainer = waypointsContainer;
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
		}
		private void OnEnable()
		{
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
			if (pitsProgress > 0)
			{
				vp.raceBox.raceManager.hud.AddMessage(new Message(vp.name + " RETURNS ON TRACK!", BottomInfoType.PIT_OUT));
				speedLimit = 1024;
				speedLimitDist = -1;
				pitsProgress = pitsPathCreator.path.length;
				pitsPathCreator = null;
				searchForPits = false;
				if (!isCPU)
				{
					selfDriving = false;
					GetComponent<BasicInput>().enabled = true;
				}
			}
		}
		IEnumerator RevvingCoroutine()
		{
			revvingCo = true;
			float targetRev =0;
			bool revHigher = true;
			while(vp.countdownTimer > 0)
			{
				if (vp.countdownTimer < 0.5f)
					vp.SetAccel(1);
				else
				{
					if ((revHigher && vp.engine.targetPitch > targetRev) || (!revHigher && vp.engine.targetPitch < targetRev))
					{
						revHigher = !revHigher;
						targetRev = 0.1f +  0.4f * UnityEngine.Random.value + (revHigher ? 0.4f : 0);
					}
					vp.SetAccel(revHigher ? 1 : 0);
				}
				yield return null;
			}
			revvingCo = false;
		}
		void FixedUpdate()
		{
			if (!trackPathCreator)
			{
				Debug.LogError("No path assigned");
				Debug.Break();
				return;
			}
			if (vp.countdownTimer > 0)
			{
				if(isCPU)
				{
					if (!revvingCo)
						StartCoroutine(RevvingCoroutine());
				}
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
				if (vp.reallyGroundedWheels == 0)
					outOfTrackTime = 0;
			}
			else
				outOfTrackTime += Time.fixedDeltaTime;

			if (vp.reallyGroundedWheels == 4 && vp.velMag > 10 && Vector3.Dot(vp.forwardDir, trackPathCreator.path.GetDirectionAtDistance(dist)) < -0.5f)
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
				pitsProgress = pitsDist;         //pitsProgress

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

				if (dist < progress + 2 * radius || (pitsPathCreator && pitsProgress == pitsPathCreator.path.length))
				{
					progress = dist;
					pitsProgress = 0;
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
					if (stuntPoints.Count > 0 && stuntPoints[curStuntpointIdx] < progress)
					{
						if (curStuntpointIdx < stuntPoints.Count - 1)
							++curStuntpointIdx;
					}
					if (replayCams.Count > 0 && replayCams[curReplayPointIdx].dist < vp.followAI.progress)
					{
						if (curReplayPointIdx < replayCams.Count - 1)
							++curReplayPointIdx;
					}
					//if (waypointsContainer[curWaypointIdx] < progress + lookAheadBase)
					//{
					//	if (curWaypointIdx == waypointsContainer.Count - 1)
					//	{
					//		tPos = trackPathCreator.path.GetPointAtDistance(5);
					//	}
					//	else
					//		++curWaypointIdx;
					//	tPos = trackPathCreator.path.GetPointAtDistance(waypointsContainer[curWaypointIdx]);
					//}
					Debug.DrawLine((Vector3)tPos, (Vector3)tPos + 100 * Vector3.up, Color.blue);
					tPos0 = trackPathCreator.path.GetPointAtDistance(dist);
					tPos = trackPathCreator.path.GetPointAtDistance(dist + lookAheadBase * lookAheadSteerCurve.Evaluate(vp.velMag));
					tPos2 = trackPathCreator.path.GetPointAtDistance(dist + lookAheadBase * lookAheadMultCurve.Evaluate(vp.velMag));
				}

				tPos0.y = transform.position.y;
				tPos.y = transform.position.y;
				tPos2.y = transform.position.y;
				//Debug.DrawLine((Vector3)tPos, (Vector3)tPos + 100 * Vector3.up, Color.magenta);
				//Debug.DrawLine((Vector3)tPos2, (Vector3)tPos2 + 100 * Vector3.up, Color.red);


				if (pitsPathCreator)
				{
					if (pitsProgress > 25)
						tSpeed = 22f;
					if (pitsProgress > 225)
						tSpeed = 80;
				}
				else
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
						//var pos = trackPathCreator.path.GetPointAtDistance(speedLimitDist);
						//Debug.DrawLine((Vector3)pos, (Vector3)pos + 100 * Vector3.up, Color.blue);
						tSpeed = speedLimit;
					}
				}

				// Attempt to reverse if vehicle is stuck
				stoppedTime = (Mathf.Abs(vp.localVelocity.z) < 1
				&& vp.reallyGroundedWheels > 0) ? stoppedTime + Time.fixedDeltaTime : 0;

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


				if (vp.reallyGroundedWheels > 0)
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
					if (stuntPoints.Count > 0 && stuntPoints[curStuntpointIdx] - progress > 0 && stuntPoints[curStuntpointIdx] - progress < 15)
					{
						if (!aiStuntingProc)
							StartCoroutine(AIStuntingProc());
					}
				}
				//else
				//{
				//	vp.SetAccel(0);
				//	vp.SetBrake(0);
				//}
				if(!vp.raceBox.evoModule.stunting)
				{
					//Vector3 curTrackpathDir = trackPathCreator.path.GetDirectionAtDistance(progress);
					//curTrackpathDir.y = 0;
					Vector3 toTPosDir = ((Vector3)tPos - transform.position).normalized;
					//Vector3 targetDir = F.FlatDistance(tPos0, vp.tr.position) < 2 ? curTrackpathDir : toTPosDir;
					//Vector3 targetDir = Vector3.Lerp(curTrackpathDir,toTPosDir, F.FlatDistance(tPos0, vp.tr.position)/);
					Vector3 targetDir = toTPosDir;
					Debug.DrawRay(transform.position + Vector3.up * 2, targetDir);
					steerAngle = Vector3.SignedAngle(F.Vec3Flatten(tr.forward), F.Vec3Flatten(targetDir), Vector3.up);
					if(!aiStuntingProc)
						vp.SetSGPShift(steerAngle > 30);
					float targetSteer = Mathf.Sign(steerAngle) * Mathf.InverseLerp(0, maxPhysicalSteerAngle, Mathf.Abs(steerAngle));
					if (reverseTime == 0)
					{
						vp.SetSteer(targetSteer);
					}
					else
					{
						vp.SetSteer(-targetSteer);
					}
					vp.SetBoost(steerAngle < 2 && vp.battery > 0.5f);
				}
			}
		}
		public IEnumerator AIStuntingProc()
		{
			aiStuntingProc = true;
			float waitTimer = 1;
			while (waitTimer > 0)
			{
				float stuntTimer = .5f;
				vp.SetSGPShift(true);
				if (vp.reallyGroundedWheels == 0)
				{
					vp.SetRoll(0);
					vp.SetSteer(0);
					vp.SetAccel(0);
					vp.SetBrake(0);
					waitTimer = 0;
					int type = Mathf.RoundToInt(3 * UnityEngine.Random.value);
					int val = (type > 1) ? 1 : (UnityEngine.Random.value > 0.5f) ? 1 : -1;
					while (stuntTimer > 0)
					{
						switch (type)
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
			OutOfPits();
			GetComponent<RaceBox>().ResetOnTrack();
			vp.engine.transmission.ShiftToGear(2);
			rolledOverTime = 0;
			pitsProgress = 0;
			reverseAttempts = 0;
			outOfTrackTime = 0;
			reverseTime = 0;
			stoppedTime = 0;
			float resetDist = progress;
			Vector3 resetPos = trackPathCreator.path.GetPointAtDistance(resetDist);
			while (!Physics.Raycast(resetPos + Vector3.up, Vector3.down, out var h, Mathf.Infinity, 1 << Info.roadLayer)
				|| Vector3.Dot(h.normal, Vector3.up) < -0.5f) // while not hit road or hit culled face (backface raycasts are on)
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
			GetComponent<BasicInput>().enabled = false;
		}
	}

}
