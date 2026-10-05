using UnityEngine;
using System.Collections;
using PathCreation;
using System.Collections.Generic;
using System;
using Unity.Profiling;

namespace RVP
{
	[RequireComponent(typeof(VehicleParent))]
	[DisallowMultipleComponent]
	[AddComponentMenu("RVP/AI/Follow AI", 0)]

	// Class for following AI
	public class FollowAI : MonoBehaviour
	{
		List<int> stuntPoints;
		public List<ReplayCam> replayCams { get; private set; }
		public PathCreator trackPathCreator { get; private set; }
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

		public float progress = 0;
		public float pitsProgress = 0;

		public float dist = 0;
		public float speedLimit = 999;
		public float speedLimitDist = 0;
		public float hardCornerDot = 0.7f;
		public float slowingCoeff = 1;
		float maxPhysicalSteerAngle = 5;

		const float steerMinDist = 10;

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
		public float lowSpeedTime;
		float steerAngle;
		int curStuntpointIdx;
		int curReplayPointIdx = 0;
		private bool revvingCo;
		bool resetRoutineActive;
		public float targetSteer;
		[SerializeField] bool debugDrawAIPath;
		readonly RaycastHit[] groundSurfaceRayHits = new RaycastHit[32];
		struct GroundSurfaceCacheEntry
		{
			public Collider collider;
			public bool isGroundSurface;
		}
		static readonly Dictionary<int, GroundSurfaceCacheEntry> groundSurfaceCache = new();
		static readonly ProfilerMarker PathProjectionMarker =
			new ProfilerMarker("RVP.FollowAI.PathProjection");
		static readonly ProfilerMarker GroundSurfaceFallbackMarker =
			new ProfilerMarker("RVP.FollowAI.GroundSurfaceFallback");
		[Range(0, 1)]
		public float maxDiff = 0.01f;
		public bool overRoad { get; private set; }

		public bool Pitting { get { return pitsPathCreator != null; } }
		public PathCreator PitsPathCreator { get { return pitsPathCreator; } }
		public ReplayCam currentCam { get { return replayCams[curReplayPointIdx]; } }

		public float univProgress;
		Vector3 distPoint;
		Vector3 progressPoint;
		private float lastOutOfTrackTime;
		internal bool looping;

		public float LapProgressPercent
		{
			get
			{
				return univProgress / RaceManager.I.racingPaths[1].path.length;
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
			univProgress = 1;
			progress = 1;
			dist = 1;
			trackPathCreator = RaceManager.I.racingPaths.GetRandom();
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
			stuntPoints = F.I.stuntpointsContainer;
			replayCams = F.I.replayCams;
			trackPathCreator = RaceManager.I.racingPaths.GetRandom();
			ResetCurCameraIdx();
			enabled = true;
		}
		private void Start()
		{
			dist = trackPathCreator.path.GetClosestDistanceAlongPath(tr.position);
			progress = dist;
			univProgress = dist;
			StartCoroutine(Prepare());
		}
		IEnumerator Prepare()
		{
			while (vp && vp.originalVehiclePhysics == null)
				yield return null;

			if (vp && vp.originalVehiclePhysics != null)
				maxPhysicalSteerAngle = vp.originalVehiclePhysics.SteeringLimitDegrees;

			if (!vp.IsOwner && progress == 0) // progress could be synched earlier so set progress when it's not been set
			{
				progress = dist;
				univProgress = dist;
			}
		}
		float GetDist(PathCreator p)
		{
			using (PathProjectionMarker.Auto())
			{
				VertexPath path = p.path;
				return path.GetClosestTimeOnPath(tr.position) * path.length;
			}
		}
		static bool IsGroundSurfaceCollider(Collider collider)
		{
			int instanceId = collider.GetInstanceID();
			if (groundSurfaceCache.TryGetValue(instanceId, out GroundSurfaceCacheEntry cached) &&
				ReferenceEquals(cached.collider, collider))
				return cached.isGroundSurface;

			bool isGroundSurface = collider.GetComponent<GroundSurfaceInstance>() != null ||
				collider.GetComponent<TerrainSurface>() != null;
			if (groundSurfaceCache.Count >= 4096)
				groundSurfaceCache.Clear();
			groundSurfaceCache[instanceId] = new GroundSurfaceCacheEntry
			{
				collider = collider,
				isGroundSurface = isGroundSurface
			};
			return isGroundSurface;
		}
		bool HasGroundSurfaceBelow(out Vector3 surfaceNormal, out float surfaceDistance)
		{
			using (GroundSurfaceFallbackMarker.Auto())
			{
				surfaceNormal = Vector3.zero;
				surfaceDistance = Mathf.Infinity;
				int hitCount = Physics.RaycastNonAlloc(tr.position + Vector3.up, Vector3.down,
					groundSurfaceRayHits, Mathf.Infinity, RaceManager.I.wheelCastMask,
					QueryTriggerInteraction.Ignore);
				for (int i = 0; i < hitCount; i++)
				{
					Collider collider = groundSurfaceRayHits[i].collider;
					RaycastHit hit = groundSurfaceRayHits[i];
					if (collider && hit.distance < surfaceDistance && IsGroundSurfaceCollider(collider))
					{
						surfaceNormal = hit.normal;
						surfaceDistance = hit.distance;
					}
				}
				return surfaceDistance < Mathf.Infinity;
			}
		}
		float GetDist(PathCreator p, float progress)
		{
			using (PathProjectionMarker.Auto())
			{
				VertexPath path = p.path;
				return path.GetClosestPointOnPath(tr.position, Mathf.Max(0, progress - 100), progress + 200);
			}
		}
		//int GetDist(int layer)
		//{
		//	float dist = 0;
		//	string closestLen = null;
		//	float min = 3 * radius;

		//	var racingPathHits = Physics.CapsuleCastAll(tr.position + Vector3.up,
		//		tr.position + .5f * Vector3.up, radius, Vector3.down, Mathf.Infinity, layer);



		//	foreach (var hit in racingPathHits)
		//	{
		//		dist = Vector3.Distance(tr.position, hit.tr.position);
		//		if (dist < min && !Physics.Linecast(tr.position + Vector3.up,
		//			hit.tr.position + 3 * Vector3.up, 1 | 1 << F.I.roadLayer | 1 << F.I.terrainLayer))
		//		{
		//			min = dist;
		//			distPoint = hit.point;
		//			closestLen = hit.transform.name;
		//		}
		//	}
		//	if (closestLen != null)
		//	{
		//		try
		//		{
		//			dist = int.Parse(closestLen);
		//		}
		//		catch
		//		{
		//		}
		//	}
		//	return (int)dist;
		//}
		void OutOfPits(bool resetProgress = true)
		{
			if (pitsPathCreator)
			{
				RaceManager.I.hud.infoText.AddMessage(new Message(vp.name + " " + F.I.LocStr("RETURNS ON TRACK!"), BottomInfoType.PIT_OUT));
				speedLimit = 1024;
				speedLimitDist = -1;
				if (resetProgress)
				{
					var newDist = GetDist(trackPathCreator,progress) + 40;
					if (newDist < progress + 300)
					{
						progress = newDist;
						univProgress = GetDist(RaceManager.I.racingPaths[1]);
					}
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
				pitsPathHits = Physics.OverlapSphere(tr.position, radius, 1 << F.I.pitsLineLayer);

				if (pitsPathHits.Length > 0)
				{
					pitsPathCreator = pitsPathHits[0].transform.parent.GetComponent<PathCreator>();
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

			Vector3 originalSurfaceNormal = Vector3.zero;
			bool hasOriginalSurfaceNormal = vp.originalVehiclePhysics != null &&
				vp.originalVehiclePhysics.TryGetGroundSurfaceContactNormal(out originalSurfaceNormal);
			bool hasSourceTrackContact = vp.originalVehiclePhysics != null &&
				vp.originalVehiclePhysics.HasGroundSurfaceContactThisTick;
			Vector3 belowSurfaceNormal = Vector3.zero;
			float belowSurfaceDistance = Mathf.Infinity;
			bool hasMarkedSurfaceBelow = (!hasSourceTrackContact || !hasOriginalSurfaceNormal) &&
				HasGroundSurfaceBelow(out belowSurfaceNormal, out belowSurfaceDistance);
			bool originalTrackSurfaceContact = vp.originalVehiclePhysics != null &&
				(hasSourceTrackContact || hasMarkedSurfaceBelow);
			overRoad = originalTrackSurfaceContact;
			if (hasOriginalSurfaceNormal)
				rolledOverTime = Mathf.Clamp(Vector3.Dot(originalSurfaceNormal, vp.tr.up) < 0.5f
					? rolledOverTime + Time.fixedDeltaTime : rolledOverTime - Time.fixedDeltaTime, 0, rollResetTime);
			else if (hasMarkedSurfaceBelow && belowSurfaceDistance <= 4)
				rolledOverTime = Mathf.Clamp(Vector3.Dot(belowSurfaceNormal, vp.tr.up) < 0.5f
					? rolledOverTime + Time.fixedDeltaTime : rolledOverTime - Time.fixedDeltaTime, 0, rollResetTime);

			if (rolledOverTime >= rollResetTime)
			{
				StartCoroutine(ResetOnTrack("rollover timer"));
			}


			if (!pitsPathCreator)
			{


				bool wrongWay = vp.velMag > 10 && vp.groundedWheels > 2 &&
					Vector3.Dot(vp.forwardDir, trackPathCreator.path.GetDirectionAtDistance(dist)) < -0.5f &&
					Vector3.Dot(vp.rb.linearVelocity.normalized,
						trackPathCreator.path.GetDirectionAtDistance(dist)) < -0.5f;
				if (!overRoad || wrongWay)
				{
					outOfTrackTime += Time.fixedDeltaTime;
					lastOutOfTrackTime = Time.time;
				}
				else if (originalTrackSurfaceContact)
				{
					// A source wheel probe or the track-layer query found an authored
					// surface, so discard accumulated false off-track time.
					outOfTrackTime = 0;
					lastOutOfTrackTime = Time.time;
				}

				if (Time.time - lastOutOfTrackTime > 1)
				{
					outOfTrackTime = 0;
				}


				if (outOfTrackTime > outOfTrackRequiredTime || vp.tr.position.y < -250) // out of bounds
				{
					StartCoroutine(ResetOnTrack(vp.tr.position.y < -250
						? "below world bound" : "out-of-track timer"));
				}
			}

			if (pitsPathCreator)
			{
				if (Time.time - inPitsTime > 13)
				{
					OutOfPits();
					StartCoroutine(ResetOnTrack("pit timeout"));
					return;
				}

				pitsProgress = GetDist(pitsPathCreator,pitsProgress);

				if (pitsProgress + lookAheadBase > pitsPathCreator.path.length)
				{
					OutOfPits();
				}
			}
			else
			{
				float newUnivProgress = GetDist(RaceManager.I.racingPaths[1], univProgress);
				if (newUnivProgress - univProgress > -200 && newUnivProgress - univProgress < 200 
				&& newUnivProgress > univProgress)
				{
					univProgress = newUnivProgress;
				}

				dist = GetDist(trackPathCreator, progress);


				if (!originalTrackSurfaceContact && dist != 1 &&
					(dist < progress || dist > progress + 2 * radius))
					outOfTrackTime += Time.fixedDeltaTime;


				if (dist < progress)
					dist = progress;

				if (dist <= progress + 2 * radius
					|| (pitsPathCreator && pitsProgress >= pitsPathCreator.path.length)
					|| (Mathf.Abs(progressPoint.y - distPoint.y) > 30 && Vector2.Distance(progressPoint.Flat(), distPoint.Flat()) <= 2 * radius))
				{
					progressPoint = distPoint;

					progress = dist;
				}
				else if (progress == 1)
				{
					dist = progress;
					outOfTrackTime += 0.1f * Time.fixedDeltaTime;
				}
			}

			if (trackPathCreator)
			{
				while (replayCams.Count > 0 && replayCams[curReplayPointIdx].dist < vp.followAI.progress && curReplayPointIdx < replayCams.Count - 1)
				{
					++curReplayPointIdx;
				}
			}
			//UpdateFollowTarget();

			if (vp.Owner)
			{
				if (selfDriving)
				{
					//Vector3 targetPos;
					if (vp.velMag < 5)
					{
						lowSpeedTime += Time.fixedDeltaTime;
					}
					else if (lowSpeedTime > 0)
					{
						lowSpeedTime -= Time.fixedDeltaTime;
					}

					if (lowSpeedTime > 3)
					{
						StartCoroutine(ResetOnTrack("AI low-speed timeout"));
					}
					if (vp.SourceEnergyPercent < 0.2f && vp.raceBox.curLap < F.I.s_laps)
					{
						searchForPits = true;
					}
					if (pitsPathCreator)
					{
					 // targetPos = trackPathCreator.path.GetPointAtDistance(progress + reqDist);
						tPos0 = pitsPathCreator.path.GetPointAtDistance(pitsProgress, EndOfPathInstruction.Stop);
						tPos = pitsPathCreator.path.GetPointAtDistance(pitsProgress + 15, EndOfPathInstruction.Stop);
						tPos2 = pitsPathCreator.path.GetPointAtDistance(pitsProgress + 30, EndOfPathInstruction.Stop);
					}
					else if (trackPathCreator)
					{
						//targetPos = trackPathCreator.path.GetPointAtDistance(progress + reqDist);
						if (stuntPoints.Count > 0 && stuntPoints[curStuntpointIdx] < progress)
						{
							if (curStuntpointIdx < stuntPoints.Count - 1)
								++curStuntpointIdx;
						}

						tPos0 = trackPathCreator.path.GetPointAtDistance(dist);
						tPos = trackPathCreator.path.GetPointAtDistance(dist + lookAheadBase * lookAheadSteerCurve.Evaluate(vp.velMag));
						tPos2 = trackPathCreator.path.GetPointAtDistance(dist + lookAheadBase * lookAheadMultCurve.Evaluate(vp.velMag));
					}

					tPos0.y = tr.position.y;
					tPos.y = tr.position.y;
					tPos2.y = tr.position.y;
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
						StartCoroutine(ResetOnTrack("AI reverse-attempt limit"));
					}

					reverseTime = Mathf.Max(0, reverseTime - Time.fixedDeltaTime);


					if (!vp.raceBox.evoModule.stunting)
					{
						//UpdateFollowTarget();

						if (debugDrawAIPath)
							Debug.DrawLine(tr.position,
								RaceManager.I.racingPaths[1].path.GetPointAtDistance(univProgress), Color.yellow);
						//Debug.DrawRay(targetPos, Vector3.up * 3, Color.yellow);
						Vector2 targetDir;
						if (pitsPathCreator)
							targetDir = ((Vector3)tPos - vp.tr.position).Flat();
						else if (NextStuntpointIn(30) && overRoad)
						{
							targetDir = ((Vector3)trackPathCreator.path.GetPointAtDistance(dist + 90) - vp.tr.position).Flat().normalized;
							if (debugDrawAIPath)
								Debug.DrawRay(vp.tr.position + Vector3.up * 3, targetDir, Color.yellow);
						}
						else
						{
							targetDir = F.Flat((Vector3)tPos - vp.tr.position);
						}

						if (looping || vp.reallyGroundedWheels <= 2)
							vp.SetSteer(0);
						else
						{
							var newTargetSteer = Vector2.SignedAngle(targetDir, (vp.rb.linearVelocity.normalized.Flat() + tr.forward.Flat()) / 2f);
							newTargetSteer = F.Sign(newTargetSteer) * Mathf.InverseLerp(0, maxPhysicalSteerAngle, Mathf.Abs(newTargetSteer));
							newTargetSteer *= (reverseTime == 0) ? 1 : -1;
							vp.SetSteer(newTargetSteer);
						}

						vp.SetBoost(steerAngle < 2 && vp.SourceEnergyPercent > 0.5f && vp.reallyGroundedWheels > 2 && vp.velMag < 30);
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
					}
				}
			}
		}
		public IEnumerator ResetOnTrack(string resetCause = "external request")
		{
			if (!vp.Owner || F.I.s_inEditor || resetRoutineActive)
				yield break;
			resetRoutineActive = true;
			//Debug.LogWarning($"[FollowAI] Reset on track: cause={resetCause}, overRoad={overRoad}, " +
			//	$"outOfTrackTime={outOfTrackTime:F2}, rolledOverTime={rolledOverTime:F2}, " +
			//	$"lowSpeedTime={lowSpeedTime:F2}, position={tr.position:F2}, " +
			//	$"sourceSurfaceContact={vp.originalVehiclePhysics != null && vp.originalVehiclePhysics.HasGroundSurfaceContactThisTick}", vp);

			vp.customCam = null;

			foreach (var w in vp.wheels)
				w.gameObject.GetComponent<TireMarkCreate>().EndMark();


			vp.raceBox.ResetOnTrack();
			vp.originalVehiclePhysics.ResetToNeutral();
			vp.ApplySourceRespawnEnergyCost();
			rolledOverTime = 0;
			pitsProgress = 0;
			reverseAttempts = 0;
			outOfTrackTime = 0;
			lowSpeedTime = 0;
			reverseTime = 0;
			stoppedTime = 0;
			vp.raceBox.wheelieTimer = 0;
			vp.raceBox.handstandTimer = 0;
			vp.raceBox.sidewinderRightTimer = 0;
			vp.raceBox.sidewinderLeftTimer = 0;

			progress = Mathf.Clamp(progress + 5, 0, (int)(trackPathCreator.path.length - 5));

			Vector3 resetPos = trackPathCreator.path.GetPointAtDistance(progress);
			RaycastHit h;
			Vector3 resetDir = trackPathCreator.path.GetDirectionAtDistance(progress);
			while (
				 !Physics.Raycast(resetPos + 5 * Vector3.up, Vector3.down, out h, 15, 1 << F.I.roadLayer)
			|| Vector3.Dot(h.normal, Vector3.up) < -0.5f // while not hit road or hit culled face (backface raycasts are on)
			|| Vector3.SignedAngle(resetDir, Vector3.up, Vector3.Cross(resetDir, Vector3.up)) < 75
			|| Physics.CheckSphere(resetPos, 1, 1 << F.I.vehicleTriggerLayer)
			&& progress < (trackPathCreator.path.length - 15))
			{
				progress += 10;
				resetPos = trackPathCreator.path.GetPointAtDistance(progress);
				resetDir = trackPathCreator.path.GetDirectionAtDistance(progress);
			}

			if (progress >= trackPathCreator.path.length)
			{
				univProgress = RaceManager.I.racingPaths[1].path.length - 5;
				progress = (int)(trackPathCreator.path.length - 5);
			}
			dist = progress;
				
			vp.ghost.StartGhostResetting();
			rb.isKinematic = true;
			tr.position = resetPos + Vector3.up + resetDir;
			yield return new WaitForFixedUpdate();

			float newUnivProgress = GetDist(RaceManager.I.racingPaths[1]);
			if (newUnivProgress > univProgress)
			{
				univProgress = newUnivProgress;
			}

			vp.antenna?.Reset();
			//rb.angularVelocity = Vector3.zero;
			//rb.velocity = Vector3.zero;
			Vector3 resetForward = Vector3.ProjectOnPlane(resetDir, h.normal);
			if (resetForward.sqrMagnitude < 0.0001f)
				resetForward = Vector3.ProjectOnPlane(Vector3.forward, h.normal);
			tr.rotation = Quaternion.LookRotation(resetForward.normalized, h.normal);
			vp.originalVehiclePhysics.ResetSourceProbeHistory();

			OutOfPits(resetProgress: false);
			rb.isKinematic = false;
			vp.resetOnTrackTime = Time.time;
			resetRoutineActive = false;


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
