using UnityEngine;
using System.Collections;
using PathCreation;
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
		[Serializable]
		public class FollowTarget
		{
			[NonSerialized]
			public Vector3 pos;
			public float dist;
			
		}
		const int steepestAllowedAngleOnRespawnDegs = 75;
		List<int> stuntPoints;
		int racingLineLayerNumber;
		public List<ReplayCam> replayCams { get; private set; }
		[NonSerialized]
		public PathCreator trackPathCreator;
		PathCreator pitsPathCreator;
		/// <summary>
		/// CPU takes control in pits
		/// </summary>
		public bool IsCPU { get; private set; }
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
		const float radius = 30;
		private Vector4 tPos0;
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

		float reqDist;
		const float steerMinDist = 10;
		// CPU settings
		//float tyreMult = 1;
		//float lowSpeed = 30;
		public FollowTarget target = new();
		[Tooltip("Time limit in seconds which the vehicle is stuck before attempting to reverse")]
		public float stopTimeReverse = 5;

		[Tooltip("Duration in seconds the vehicle will reverse after getting stuck")]
		public float reverseAttemptTime = 6;

		[Tooltip("How many times the vehicle will attempt reversing before resetting, -1 = no reset")]
		const int resetReverseCount = 0;
		int reverseAttempts;

		[Tooltip("Seconds a vehicle will be rolled over before resetting, -1 = no reset")]
		public float rollResetTime = 1;
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
		private bool revvingCo;
		public float targetSteer;
		[Range(0,1)]
		public float maxDiff = 0.01f;
		public bool overRoad { get; private set; }

		public bool Pitting { get { return pitsPathCreator != null; } }
		public ReplayCam currentCam { get { return replayCams[curReplayPointIdx]; } }

		float lapProgressPercent;
		float pitsDist;
		Vector3 distPoint;
		Vector3 progressPoint;
		float distLastTime;
		float LapProgressPercentTime;
		private float lastOutOfTrackTime;

		public float LapProgressPercent
		{
			get
			{
				if (Time.time - LapProgressPercentTime > .2f) // for better performance
				{
					LapProgressPercentTime = Time.time;
					int universalPathProgress = GetDist(1 << F.I.racingLineLayer);
					if (universalPathProgress > progress + 2 * radius || universalPathProgress < progress - 2 * radius)
						universalPathProgress = progress;

					if (universalPathProgress == 1 && lapProgressPercent >= 0.9f) // when driving directly pSast startline
					{
						lapProgressPercent = 1;
					}
					else
						lapProgressPercent = universalPathProgress / F.I.universalPath.path.length;
				}

				return lapProgressPercent;
			}
		}
		private bool NextStuntpointIn(float distanceOffset)
		{
			return stuntPoints != null && stuntPoints.Count > 0 && stuntPoints[curStuntpointIdx] > progress && stuntPoints[curStuntpointIdx] - progress < distanceOffset;
		}
		public void ResetCurCameraIdx()
		{
			curReplayPointIdx = 0;
		}
		public void NextLap()
		{
			progress = 1;
			dist = 1;
			lapProgressPercent = 0;
			target.dist = progress;
			target.pos = trackPathCreator.path.GetPointAtDistance(target.dist);
			curStuntpointIdx = 0;
			ResetCurCameraIdx();
			speedLimitDist = -1;
		}
		public void SetCPU(bool enabled)
		{
			if (!vp.Owner)
				return;
			selfDriving = enabled;
			IsCPU = enabled;
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
			ResetCurCameraIdx();
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
		}
		int GetDist(int layer)
		{
			float dist = 0;
			string closestLen = null;
			float min = 3 * radius;
			
			var racingPathHits = Physics.CapsuleCastAll(transform.position + Vector3.up, 
				transform.position + .5f * Vector3.up, radius, Vector3.down, Mathf.Infinity, layer);

			

			foreach (var hit in racingPathHits)
			{
				dist = Vector3.Distance(transform.position, hit.transform.position);
				if (dist < min && !Physics.Linecast(transform.position + Vector3.up,
					hit.transform.position + 3 * Vector3.up, 1 | 1 << F.I.roadLayer | 1 << F.I.terrainLayer))
				{
					min = dist;
					distPoint = hit.point;
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
				{
					var newDist = GetDist(1 << racingLineLayerNumber) + 40;
					if (newDist < progress + 300)
						progress = newDist;
				}
				pitsProgress = 0;
				pitsPathCreator = null;
				searchForPits = false;
				selfDriving = IsCPU;
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
			revvingCo = false;
		}
		public void PitsTrigger()
		{
			if (searchForPits)
			{
				Collider[] pitsPathHits;
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
		}
		void FixedUpdate()
		{
			if (!trackPathCreator)
				return;

			if (CountDownSeq.Countdown > 0)
			{
				vp.ebrakeInput = 1;

				if (IsCPU)
				{
					if (!revvingCo)
						StartCoroutine(RevvingCoroutine());
				}
				return;
			}
			vp.ebrakeInput = 0;

			overRoad = Physics.Raycast(tr.position + Vector3.up, Vector3.down, out var _, Mathf.Infinity, 1 << F.I.roadLayer);
			bool surface = Physics.Raycast(tr.position + Vector3.up, Vector3.down, out var hit, 3, RaceManager.I.wheelCastMask);
			if(surface)
				rolledOverTime = Mathf.Clamp((Vector3.Dot(hit.normal,vp.tr.up) < 0.5f) ? rolledOverTime + Time.fixedDeltaTime
				: rolledOverTime - Time.fixedDeltaTime, 0, rollResetTime);

			if (rolledOverTime >= rollResetTime)
			{
				StartCoroutine(ResetOnTrack());
			}
			

			if (!pitsPathCreator)
			{
				if ((!overRoad) // out of track
					 || (vp.velMag > 10 && vp.groundedWheels > 2 && Vector3.Dot(vp.forwardDir, trackPathCreator.path.GetDirectionAtDistance(dist)) < -0.5f
					&& Vector3.Dot(vp.rb.velocity.normalized, trackPathCreator.path.GetDirectionAtDistance(dist)) < -0.5f)) // wrong way drive
				{
					outOfTrackTime += Time.fixedDeltaTime;
					lastOutOfTrackTime = Time.time;
				}
					
				if (Time.time - lastOutOfTrackTime > 1)
				{
					outOfTrackTime = 0;
				}

				if (outOfTrackTime > outOfTrackRequiredTime
					|| vp.tr.position.y < -250) // out of bounds
				{
					StartCoroutine(ResetOnTrack());
				}
			}

			
			pitsDist = 0;
			if (pitsPathCreator)
			{
				if (Time.time - inPitsTime > 10)
				{
					OutOfPits();
					StartCoroutine(ResetOnTrack());
					return;
				}
				if (Time.time - LapProgressPercentTime > .2f) // for better performance
				{
					LapProgressPercentTime = Time.time;
					pitsDist = GetDist(1 << F.I.pitsLineLayer); // pitsDist
				}

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
				if (Time.time - distLastTime > .2f) // for better performance
				{
					dist = GetDist(1 << racingLineLayerNumber);
					distLastTime = Time.time;
				}

				if(dist != 1 && (dist < progress || dist > progress + 2 * radius))
					outOfTrackTime += Time.fixedDeltaTime;

				if (dist < progress)
					dist = progress;

				if (dist < progress + 2 * radius 
					|| (pitsPathCreator && pitsProgress >= pitsPathCreator.path.length) 
					|| (Mathf.Abs(progressPoint.y - distPoint.y) > 30 && Vector2.Distance(progressPoint.Flat(), distPoint.Flat()) < 2 * radius))
				{
					progressPoint = distPoint;

					progress = dist;
					pitsProgress = 0;
				}
				else if (progress == 1)
				{
					dist = progress;
					outOfTrackTime += 0.1f * Time.fixedDeltaTime;
				}
			}

			if(trackPathCreator)
			{
				while (replayCams.Count > 0 && replayCams[curReplayPointIdx].dist < vp.followAI.progress && curReplayPointIdx < replayCams.Count - 1)
				{
					++curReplayPointIdx;
				}
			}

			if (selfDriving && vp.Owner)
			{
				if (vp.BatteryPercent < 0.2f && vp.raceBox.curLap < F.I.s_laps)
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


				if (!vp.raceBox.evoModule.stunting)
				{
					reqDist = lookAheadSteerCurve.Evaluate(vp.velMag) * steerMinDist;

					float dTargetCar = F.FlatDistance(target.pos, vp.tr.position);
					if (Mathf.Abs(target.dist - dist) > 2 * reqDist || target.dist < dist)
					{ // reset target
						target.dist = dist + reqDist;
						target.pos = trackPathCreator.path.GetPointAtDistance(target.dist);
					}
					Vector2 targetPosOrientation = trackPathCreator.path.GetDirectionAtDistance(target.dist).Flat().normalized;

					if (dTargetCar < .75f * reqDist
						|| (vp.reallyGroundedWheels > 3 && Mathf.Abs(Vector2.Dot(targetPosOrientation, vp.tr.forward.Flat().normalized)) < .5f))
					{ // car catching up OR angle between racing line and car exceedes 60 degs
						target.dist += 2 * vp.velMag * Time.fixedDeltaTime;
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
					else if((aiStuntingProc || NextStuntpointIn(30)) 
						&& overRoad)
					{
						targetDir = ((Vector3)trackPathCreator.path.GetPointAtDistance(dist + 90) - vp.tr.position).Flat().normalized;
						Debug.DrawRay(vp.tr.position+Vector3.up * 3, targetDir, Color.yellow);
					}
					else
					{
						targetDir = F.Flat(target.pos - vp.tr.position);
					}

					var newTargetSteer = Vector2.SignedAngle(targetDir, (vp.rb.velocity.normalized.Flat()+tr.forward.Flat())/2f);

					newTargetSteer = F.Sign(newTargetSteer) * Mathf.InverseLerp(0, maxPhysicalSteerAngle, Mathf.Abs(newTargetSteer));

					newTargetSteer *= (reverseTime == 0) ? 1 : -1;
					vp.SetSteer(newTargetSteer);

					vp.SetBoost(steerAngle < 2 && vp.BatteryPercent > 0.5f && vp.reallyGroundedWheels > 2 && vp.velMag < 30);
				}

				if (vp.reallyGroundedWheels > 0)
				{
					vp.SetAccel((vp.velMag < tSpeed && reverseTime == 0) ? 1 : 0);

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

			foreach (var w in vp.wheels)
				w.gameObject.GetComponent<TireMarkCreate>().EndMark();

			vp.raceBox.ResetOnTrack();
			vp.engine.transmission.ShiftToGear(2);
			vp.ResetOnTrackBatteryPenalty();
			rolledOverTime = 0;
			pitsProgress = 0;
			reverseAttempts = 0;
			outOfTrackTime = 0;
			reverseTime = 0;
			stoppedTime = 0;
			
			progress = Mathf.Clamp(progress + 5, 0, (int)(trackPathCreator.path.length-5));

			Vector3 resetPos = trackPathCreator.path.GetPointAtDistance(progress);
			RaycastHit h;
			Vector3 resetDir = trackPathCreator.path.GetDirectionAtDistance(progress);
			while (
			(!Physics.Raycast(resetPos + 5 * Vector3.up, Vector3.down, out h, 15, 1 << F.I.roadLayer)
			|| Vector3.Dot(h.normal, Vector3.up) < -0.5f // while not hit road or hit culled face (backface raycasts are on)
			|| Vector3.SignedAngle(resetDir, Vector3.up, Vector3.Cross(resetDir, Vector3.up)) < steepestAllowedAngleOnRespawnDegs
			|| h.transform.parent.name == "loop") && progress < (trackPathCreator.path.length - 15))
			{
				progress += 10;
				resetPos = trackPathCreator.path.GetPointAtDistance(progress);
				resetDir = trackPathCreator.path.GetDirectionAtDistance(progress);
			}
			dist = progress;

			vp.ghost.StartGhostResetting();
			rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
			rb.isKinematic = true;
			tr.position = h.point + Vector3.up + resetDir;
			yield return new WaitForFixedUpdate();
			//rb.angularVelocity = Vector3.zero;
			//rb.velocity = Vector3.zero;
			tr.rotation = Quaternion.LookRotation(trackPathCreator.path.GetDirectionAtDistance(progress));

			target.dist = dist + reqDist;
			target.pos = trackPathCreator.path.GetPointAtDistance(target.dist);

			OutOfPits(resetProgress: false);
			rb.isKinematic = false;
			rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
			vp.resetOnTrackTime = Time.time;
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
