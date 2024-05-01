using UnityEngine;
using System.Collections;
using PathCreation;
using System.Collections.Generic;
using System;
using UnityEngine.UIElements;
using Unity.Netcode;
using static UnityEditor.PlayerSettings;
using UnityEditor.PackageManager.Requests;

namespace RVP
{
	[RequireComponent(typeof(VehicleParent))]
	[DisallowMultipleComponent]
	[AddComponentMenu("RVP/AI/Follow AI", 0)]

	// Class for following AI
	public class FollowAI : MonoBehaviour
	{
		public class FollowTarget
		{
			public Vector3 pos;
			/// <summary>
			/// distance
			/// </summary>
			public float dist;
		}
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
		int racingLineLayerNumber;
		public List<ReplayCam> replayCams { get; private set; }
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
		float radius = 30;
		private Vector4 tPos0;
		public int curWaypointIdx;
		public float stoppedTime;
		public float reverseTime;
		public float brakeTime;
		public int progress = 0;
		public float pitsProgress = 0;
		public int dist = 0;
		public float speedLimit = 999;
		public float speedLimitDist = 0;
		public float hardCornerDot = 0.7f;
		public float slowingCoeff = 1;
		float maxPhysicalSteerAngle = 5;
		private int universalPathLayer;
		const float reqDist = 10;
		// CPU settings
		//float tyreMult = 1;
		//float lowSpeed = 30;
		FollowTarget target = new();
		public CpuLevel cpuLevel;
		public bool SGPshifting;
		[Tooltip("Time limit in seconds which the vehicle is stuck before attempting to reverse")]
		public float stopTimeReverse = 5;

		[Tooltip("Duration in seconds the vehicle will reverse after getting stuck")]
		public float reverseAttemptTime = 6;

		[Tooltip("How many times the vehicle will attempt reversing before resetting, -1 = no reset")]
		int resetReverseCount = 0;
		int reverseAttempts;

		[Tooltip("Seconds a vehicle will be rolled over before resetting, -1 = no reset")]
		public float rollResetTime = 3;
		public float rolledOverTime;
		private bool dumbBool;
		public AnimationCurve lookAheadMultCurve = new();
		public AnimationCurve lookAheadSteerCurve = new();
		public AnimationCurve tSpeedExpCurve = new();
		public bool searchForPits;
		float inPitsTime;
		public float outOfTrackTime;
		public float outOfTrackRequiredTime = 1;

		float steerAngle;
		int curStuntpointIdx;
		int curReplayPointIdx = 0;
		public bool aiStuntingProc;
		public float cpuSmoothCoeff = 5;
		public float cpuFastCoeff = 50;
		private bool revvingCo;
		public float targetSteer;
		public bool overRoad { get; private set; }
		public float raceEndedLapProgressPercent;
		public float maxSteerPerFrame = 5;

		public bool Pitting { get { return pitsPathCreator != null; } }
		public ReplayCam currentCam { get { return replayCams[curReplayPointIdx]; } }
		public float LapProgressPercent
		{
			get
			{
				//if (vp.raceBox.enabled)
				//{
				int universalPathProgress = GetDist(1 << universalPathLayer);
				if (universalPathProgress > progress + 2 * radius || universalPathProgress < progress - 2 * radius)
					universalPathProgress = progress;
				if (progress == 1) // when driving directly past startline
					return 0;
				return universalPathProgress / F.I.universalPath.path.length;
				//}
				//else
				//	return raceEndedLapProgressPercent;
			}
		}
		private bool NextStuntpointIn(float distanceOffset)
		{
			return stuntPoints != null && stuntPoints.Count > 0 && stuntPoints[curStuntpointIdx] > progress && stuntPoints[curStuntpointIdx] - progress < distanceOffset;
		}
		public void NextLap()
		{
			progress = 1;
			target.dist = progress;
			target.pos = trackPathCreator.path.GetPointAtDistance(target.dist);
			curWaypointIdx = 0;
			curStuntpointIdx = 0;
			curReplayPointIdx = 0;
			speedLimitDist = -1;
		}
		public void SetCPU(bool val)
		{
			if (!vp.Owner)
				return;
			if (val == isCPU)
				return;
			cpuLevel = F.I.s_cpuLevel;
			isCPU = val;
			selfDriving = val;

			//if (isCPU)
			//{
			//	vp.basicInput.enabled = false;
			//	switch (cpuLevel)
			//	{
			//		case CpuLevel.Easy:
			//			lowSpeed = UnityEngine.Random.value * 2 + 28; // 30-32
			//			tyreMult = .9f;
			//			break;
			//		case CpuLevel.Medium:
			//			lowSpeed = UnityEngine.Random.value * 2 + 30; // 30-32
			//			tyreMult = 1.2f;
			//			break;
			//		case CpuLevel.Hard:
			//			lowSpeed = UnityEngine.Random.value * 2 + 36; // 36-38
			//			tyreMult = 1.2f;
			//			break;
			//		case CpuLevel.Elite:
			//			lowSpeed = UnityEngine.Random.value * 2 + 38; // 38-40
			//			tyreMult = 1.2f;
			//			break;
			//	}
			//	for (int i = 0; i < 4; ++i)
			//	{
			//		vp.wheels[i].sidewaysFriction = tyreMult * vp.wheels[i].initSidewaysFriction;
			//		vp.wheels[i].forwardFriction = tyreMult * vp.wheels[i].initForwardFriction;
			//	}
			//	var keys = tSpeedExpCurve.keys;
			//	keys[keys.Count() - 1].value = lowSpeed;
			//	tSpeedExpCurve.keys = keys;
			//}
			//else
			//{
			//	vp.basicInput.enabled = true;
			//	for (int i = 0; i < 4; ++i)
			//	{
			//		vp.wheels[i].sidewaysFriction = vp.wheels[i].initSidewaysFriction;
			//		vp.wheels[i].forwardFriction = vp.wheels[i].initForwardFriction;
			//	}
			//}
		}
		private void Awake()
		{
			tr = transform;
			rb = GetComponent<Rigidbody>();
			vp = GetComponent<VehicleParent>();
			racingLineLayerNumber = F.I.racingLineLayer;
			stuntPoints = F.I.stuntpointsContainer;
			replayCams = F.I.replayCams;
			trackPathCreator = F.I.universalPath;
			universalPathLayer = F.I.racingLineLayer;
			curReplayPointIdx = replayCams.Count - 1;
			enabled = true;
		}
		private void Start()
		{
			StartCoroutine(Prepare());
		}
		IEnumerator Prepare()
		{
			yield return null; // wait till other components initialize

			maxPhysicalSteerAngle = vp.steeringControl.steeredWheels[0].steerRangeMax;
			dist = GetDist(1 << racingLineLayerNumber);

			if (progress == 0) // progress could be synched earlier so set progress when it's not been set
			{
				progress = dist;
			}
			target.dist = dist;
			target.pos = trackPathCreator.path.GetPointAtDistance(dist);
			SetCPU(isCPU);
		}
		int GetDist(int layer)
		{
			float dist = 0;
			string closestLen = null;
			float min = 3 * radius;
			if (Physics.Raycast(transform.position + Vector3.up, Vector3.down, out RaycastHit h, Mathf.Infinity,
				1 | 1 << F.I.roadLayer | 1 << F.I.terrainLayer))
			{
				var racingPathHits = Physics.OverlapSphere(h.point, radius, layer);
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
				{
					try
					{
						dist = int.Parse(closestLen);
					}
					catch
					{
					}
				}
			}
			else
			{
				if (Physics.SphereCast(transform.position + Vector3.up, radius, Vector3.down, out h, Mathf.Infinity, layer))
				{
					dist = int.Parse(h.transform.name);
				}
				else
				{
					outOfTrackTime += Time.fixedDeltaTime;
				}
			}
			return (int)dist;
		}
		void OutOfPits(bool resetProgress = true)
		{
			if (pitsPathCreator)
			{
				RaceManager.I.hud.infoText.AddMessage(new Message(vp.name + " RETURNS ON TRACK!", BottomInfoType.PIT_OUT));
				speedLimit = 1024;
				speedLimitDist = -1;
				if (resetProgress)
					progress = GetDist(1 << racingLineLayerNumber) + 40;
				if (pitsPathCreator)
					pitsProgress = pitsPathCreator.path.length;
				pitsPathCreator = null;
				searchForPits = false;
				selfDriving = isCPU;
				vp.basicInput.enabled = vp.Owner;
			}
		}
		IEnumerator RevvingCoroutine()
		{
			revvingCo = true;
			float targetRev = 0;
			bool revHigher = true;

			while (CountDownSeq.Countdown > 0)
			{
				if (CountDownSeq.Countdown < 0.5f)
					vp.SetAccel(1);
				else
				{
					if ((revHigher && vp.engine.targetPitch > targetRev) || (!revHigher && vp.engine.targetPitch < targetRev))
					{
						revHigher = !revHigher;
						targetRev = 0.1f + 0.4f * UnityEngine.Random.value + (revHigher ? 0.4f : 0);
					}
					vp.SetAccel(revHigher ? 1 : 0);
				}
				yield return null;
			}
			vp.SetEbrake(0);
			revvingCo = false;
		}

		void FixedUpdate()
		{
			if (!trackPathCreator)
				return;

			if (CountDownSeq.Countdown > 0)
			{
				vp.ebrakeInput = 1;
				if (isCPU)
				{
					if (!revvingCo)
						StartCoroutine(RevvingCoroutine());
				}
				return;
			}

			rolledOverTime = Mathf.Clamp((vp.reallyGroundedWheels < 3 && vp.crashing) ? rolledOverTime + Time.fixedDeltaTime
				: rolledOverTime - Time.fixedDeltaTime, 0, rollResetTime);

			if (rolledOverTime >= rollResetTime)
			{
				StartCoroutine(ResetOnTrack());
			}
			overRoad = Physics.Raycast(tr.position + Vector3.up, Vector3.down, out var _, Mathf.Infinity, 1 << F.I.roadLayer);

			if (!pitsPathCreator)
			{
				if ((vp.reallyGroundedWheels == 4 && !overRoad) || // out of track
					(overRoad && vp.velMag > 10 && vp.groundedWheels > 0 && Vector3.Dot(vp.forwardDir, trackPathCreator.path.GetDirectionAtDistance(dist)) < -0.5f)
					&& Vector3.Dot(vp.rb.velocity.normalized, trackPathCreator.path.GetDirectionAtDistance(dist)) < -0.5f) // wrong way drive
					outOfTrackTime += Time.fixedDeltaTime;

				if (outOfTrackTime > outOfTrackRequiredTime
					|| vp.tr.position.y < -250) // out of bounds
				{
					StartCoroutine(ResetOnTrack());
				}
			}

			Collider[] pitsPathHits;
			float pitsDist = 0;
			if (pitsPathCreator)
			{
				if (Time.time - inPitsTime > 10)
				{
					OutOfPits();
					StartCoroutine(ResetOnTrack());
					return;
				}
				pitsDist = GetDist(1 << F.I.pitsLineLayer); // pitsDist

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
					pitsPathHits = Physics.OverlapSphere(transform.position, radius, 1 << F.I.pitsLineLayer);

					if (pitsPathHits.Length > 0)
					{
						pitsPathCreator = pitsPathHits[0].transform.parent.GetComponent<PathCreator>();
						pitsDist = 0;
						pitsProgress = 0;
						searchForPits = false;
						inPitsTime = Time.time;
					}
				}

				dist = GetDist(1 << racingLineLayerNumber);

				if (dist < progress)
					dist = progress;

				if (dist < progress + 2 * radius || (pitsPathCreator && pitsProgress == pitsPathCreator.path.length))
				{
					progress = dist;
					pitsProgress = 0;
				}
				else if (progress == 1)
				{
					dist = progress;
					outOfTrackTime += 0.1f * Time.fixedDeltaTime;
				}
				else
				{
					outOfTrackTime += Time.fixedDeltaTime;
				}
			}

			if (selfDriving && vp.Owner)
			{
				if (vp.BatteryPercent < 0.2f)
				{
					searchForPits = true;
				}
				if (pitsPathCreator)
				{
					tPos0 = pitsPathCreator.path.GetPointAtDistance(pitsDist, EndOfPathInstruction.Stop);
					tPos = pitsPathCreator.path.GetPointAtDistance(pitsDist + 15, EndOfPathInstruction.Stop);
					tPos2 = pitsPathCreator.path.GetPointAtDistance(pitsDist + 30, EndOfPathInstruction.Stop);
				}
				else if (trackPathCreator)
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
					if (pitsProgress > 0)
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
				if (reverseAttempts > resetReverseCount && resetReverseCount >= 0 && trackPathCreator)
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
							vp.SetBrake(Mathf.InverseLerp(0, 20, vp.velMag - tSpeed));
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
					if (NextStuntpointIn(15))
					{
						if (!aiStuntingProc)
							StartCoroutine(AIStuntingProc());
					}
				}

				if (!vp.raceBox.evoModule.stunting)
				{
					float dTargetCar = Vector3.Distance(target.pos, vp.tr.position);
					if (Mathf.Abs(target.dist - dist) > 2 * reqDist || target.dist < dist)
					{ // reset target
						target.dist = dist + reqDist;
						target.pos = trackPathCreator.path.GetPointAtDistance(target.dist);
					}
					Vector2 targetPosOrientation = trackPathCreator.path.GetDirectionAtDistance(target.dist).Flat().normalized;

					if (dTargetCar < .75f * reqDist 
						|| (vp.reallyGroundedWheels > 3 && Mathf.Abs(Vector2.Dot(targetPosOrientation, vp.tr.forward.Flat().normalized)) < .5f))
					{ // car catching up OR angle between racing line and car exceedes 60 degs
						target.dist += 8 * vp.velMag * Time.fixedDeltaTime;
						target.pos = trackPathCreator.path.GetPointAtDistance(target.dist);
					}
					if (dTargetCar < reqDist) // go on
					{
						target.dist += vp.velMag * Time.fixedDeltaTime;
						target.pos = trackPathCreator.path.GetPointAtDistance(target.dist);
					}
					
					Debug.DrawRay(target.pos, Vector3.up * 3, Color.yellow);
					Vector2 targetDir;
					if (pitsPathCreator)
						targetDir = ((Vector3)tPos - vp.tr.position).Flat();
					else if((aiStuntingProc || NextStuntpointIn(30)) && Physics.Raycast(vp.tr.position + 3 * Vector3.up, Vector3.down, out RaycastHit hit, Mathf.Infinity, 1 << F.I.roadLayer))
					{
						targetDir = ((Vector3)trackPathCreator.path.GetPointAtDistance(dist + 90) - vp.tr.position).Flat().normalized;
						Debug.DrawRay(vp.tr.position+Vector3.up * 3, targetDir, Color.yellow);
					}
					else
					{
						
						targetDir = F.Flat(target.pos - vp.tr.position);
					}
						

					targetSteer = Vector2.SignedAngle(targetDir, tr.forward.Flat());

					//if (Mathf.Abs(targetSteer) > maxSteerPerFrame) // max steer 
					//	targetSteer = Mathf.Sign(targetSteer) * maxSteerPerFrame;
					//Debug.DrawRay(vp.tr.position + 3*Vector3.up, targetDir, Color.red);

					targetSteer = F.Sign(targetSteer) * Mathf.InverseLerp(0, maxPhysicalSteerAngle, Mathf.Abs(targetSteer));

					targetSteer = Mathf.Lerp(vp.steerInput, targetSteer, Pitting ? 1 : cpuSmoothCoeff * Time.fixedDeltaTime);

					if (vp.reallyGroundedWheels > 2)
						vp.SetSteer(((reverseTime == 0) ? 1 : -1) * targetSteer);

					vp.SetBoost(steerAngle < 2 && vp.BatteryPercent > 0.5f);
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
				vp.SetSGPShift(1);
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
			vp.SetSGPShift(0);
			aiStuntingProc = false;
		}
		public IEnumerator ResetOnTrack()
		{
			if (!vp.Owner)
				yield break;

			vp.customCam = null;

			vp.raceBox.ResetOnTrack();
			vp.engine.transmission.ShiftToGear(2);
			vp.ResetOnTrackBatteryPenalty();
			rolledOverTime = 0;
			pitsProgress = 0;
			reverseAttempts = 0;
			outOfTrackTime = 0;
			reverseTime = 0;
			stoppedTime = 0;

			Vector3 resetPos = trackPathCreator.path.GetPointAtDistance(progress);
			RaycastHit h;
			while (!Physics.Raycast(resetPos + 5 * Vector3.up, Vector3.down, out h, Mathf.Infinity, 1 << F.I.roadLayer)
				|| Vector3.Dot(h.normal, Vector3.up) < -0.5f // while not hit road or hit culled face (backface raycasts are on)
				|| Mathf.Abs(Vector3.Dot(h.normal, Vector3.up)) < .64f  // slope too big
				|| h.transform.parent.name == "loop")
			{
				progress += 10;
				resetPos = trackPathCreator.path.GetPointAtDistance(progress);
			}
			dist = progress;

			vp.ghost.StartGhostResetting();
			rb.isKinematic = true;
			tr.position = h.point + Vector3.up;
			yield return new WaitForFixedUpdate();
			//rb.angularVelocity = Vector3.zero;
			//rb.velocity = Vector3.zero;
			tr.rotation = Quaternion.LookRotation(trackPathCreator.path.GetDirectionAtDistance(progress));

			target.dist = dist + reqDist;
			target.pos = trackPathCreator.path.GetPointAtDistance(target.dist);

			OutOfPits(resetProgress: false);
			rb.isKinematic = false;
		}

		public void DriveThruPits(in PathCreator pitsPathCreator)
		{
			inPitsTime = Time.time;
			this.pitsPathCreator = pitsPathCreator;
			selfDriving = true;
			vp.basicInput.enabled = false;
		}
	}

}
