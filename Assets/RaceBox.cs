using RVP;
using System;
using UnityEngine;
using static PtsAnim;
using System.Collections.Generic;
using System.Collections;

public class StuntRotInfo
{
	public int axis;
	public int rotation;
	public override string ToString()
	{
		return (axis > 1 ? 'Z' : (axis == 0 ? 'X' : 'Y')) + rotation.ToString();
	}
}
public class StuntsData : IEnumerable<Stunt>
{
    public enum ExtraName
    {
        Trikstart,Wheelie, Stoppie, Looper, Grind, SideGrind
    };
   public bool availableForFrontend;
	public Flip[] flipData; // Meteor X1 | Backflip 360
	public Stunt[] driftData; // Slide x1 123 & Powerslide x1 456
	public Stunt[] extraData;
	public StuntsData(Flip[] flipData)
	{
		// deep copy
		this.flipData = new Flip[flipData.Length];
		for (int i = 0; i < flipData.Length; ++i)
			this.flipData[i] = new Flip(flipData[i]);

		driftData = new Stunt[]
		{
			new Stunt("SLIDE", 350),
			new Stunt("POWERSLIDE", 600),
		};
		
		for (int i = 0; i < driftData.Length; ++i)
			driftData[i] = new Stunt(driftData[i]);

		extraData = new Stunt[]
		{
			new Stunt("TRICKSTART", 250),
			new Stunt("WHEELIE", 550),
			new Stunt("STOPPIE", 550),
			new Stunt("SUPER LOOPER", 1750),
			new Stunt("GRIND", 2000),
			new Stunt("SIDE GRIND", 400),
		};
	}

	IEnumerator<Stunt> IEnumerable<Stunt>.GetEnumerator()
	{
		foreach (var item in flipData)
		{
			yield return item;
		}
		foreach (var item in driftData)
		{
			yield return item;
		}
		foreach (var item in extraData)
		{
			yield return item;
		}
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		throw new NotImplementedException();
	}
}

public enum StuntSeqStatus { None, Ongoing, Ended };

public class RaceBox : MonoBehaviour
{
	[NonSerialized]
	public RaceManager raceManager;
	public VehicleParent vp { get; private set; }
	public SGP_Evo evoModule { get; private set; }
	public float distance { get; private set; }
	private float aero;
	public float Aero
	{
		get { return 10 * aero; }
		private set { aero = value; }
	}
	float lapTimer;
	public TimeSpan? CurLaptime
	{
		get
		{
			if (lapTimer == 0 || curLap == 0)
				return null;
			return TimeSpan.FromSeconds(lapTimer);
		}
	}
	public TimeSpan bestLapTime { get; private set; }
	/// <summary>
	/// set only after the race
	/// </summary>
	public TimeSpan raceTime { get; private set; }
	public bool Finished
	{
		get { return curLap > Info.s_laps; }
	}
	public int curLap { get; private set; }

	public float w_A_dot;

	public int starLevel;
	public float driftPts;
	public float grantedComboTime;
	float maxAeroMeterVelocity = 0.05f;
	float minAeroMeterVelocity = 0;
	public float aeroMeterVelocity = 0;
	float aeroMeterResponsiveness = 1f;
	bool prevGroundedWheels0 = false;
	Vector3 w;
	StuntsData stuntsData;
	public Stunt[] stunts { get; private set; }
	public float lastTimeInAir;
	[SerializeField]
	float stableLandingTimer = -1;
	private float prevVel;
	private float jumpTimer;
	private PtsAnimInfo stuntPai;
	private PtsAnimInfo prevStuntPai;
	private PtsAnimInfo jumpPai;
	public Vector3 elecTunnelCameraPos = -Vector3.one;
	public float carStoppedTime;
	public float stoppieTimer;
	public float wheelieTimer;
	float lastLooperTime;
	private float sideGrindTimer;
	public float d_positiveProgress;

	public PtsAnimInfo JumpPai
	{
		get
		{
			if (jumpPai == null)
			{
				return null;
			}
			else
			{
				var local_pai = jumpPai;
				jumpPai = null;
				return local_pai;
			}
		}
		private set
		{
			jumpPai = value;
		}
	}

	void Awake()
	{
		vp = GetComponent<VehicleParent>();
		evoModule = GetComponent<SGP_Evo>();

		//rots = new Stack<StuntRotInfo>(32);
		var globalcontrol = GameObject.Find("Canvas");
		var stuntTable = globalcontrol.transform.GetComponent<StuntManager>().allPossibleFlips;
		stuntsData = new StuntsData(stuntTable);
	}
	private void OnEnable()
	{
		lapTimer = 0;
		bestLapTime = TimeSpan.FromSeconds(36000);//TimeSpan.FromSeconds(Info.tracks[Info.s_trackName].records[0].secondsOrPts);
		raceTime = TimeSpan.MinValue;
		curLap = 0;
		starLevel = 0;
		stuntPai = new PtsAnimInfo(0, PtsAnimType.Evo, -1);
	}
	public string Result(Info.RecordType recordType)
	{
		switch (recordType)
		{
			case Info.RecordType.BestLap:
				if (bestLapTime == TimeSpan.MaxValue)
					return "-";
				if (bestLapTime.Hours > 0)
					return bestLapTime.ToString(@"hh\:mm\:ss\.ff");
				return bestLapTime.ToString(@"mm\:ss\.ff");
			case Info.RecordType.RaceTime:
				if (raceTime == TimeSpan.MinValue)
					return "-";
				if (raceTime.Hours > 0)
					return raceTime.ToString(@"hh\:mm\:ss\.ff");
				return raceTime.ToString(@"mm\:ss\.ff");
			case Info.RecordType.StuntScore:
				return ((int)(aero * 10)).ToString();
			case Info.RecordType.DriftScore:
				return driftPts.ToString();
			default:
				return "-";
		}
	}


	private void FixedUpdate()
	{
		if (!Info.gamePaused && curLap > 0)
			lapTimer += Time.fixedDeltaTime;
		if (vp.reallyGroundedWheels == 0)
			lastTimeInAir = Time.time;

		if (!Finished)
		{
			StuntDetector();
			JumpDetector();
			DriftDetector();
		}
	}
	public bool GetStuntSeq(ref StuntsData outStuntsData)
	{
		if (stuntsData.availableForFrontend)
		{
			stuntsData.availableForFrontend = false;
			outStuntsData = stuntsData;
			return true;
		}
		else
			return false;
	}
	
	IEnumerator AddExtraStuntCo(StuntsData.ExtraName name)
	{
		var stunt = stuntsData.extraData[(int)name];
		stunt.updateOverlay = true;
		stunt.doneTimes++;
		stuntsData.availableForFrontend = true;
		
		stuntPai.level++;
		stuntPai.score += (int)((starLevel + 1) * 1.5f * stunt.score);
			
		AcceptExtraStunt();
		yield return null;
		stunt.doneTimes = 0;
	}
	public void AddLooper()
	{
		if(Time.time - lastLooperTime > 1)
		{
			StartCoroutine(AddExtraStuntCo(StuntsData.ExtraName.Looper));
			lastLooperTime = Time.time;
		}
	}
	void ProgressDrift(StuntsData.ExtraName name, float addProgress, bool addCombo = false)
	{
		var stunt = stuntsData.extraData[(int)name];
		if (addProgress > 0)
		{
			stunt.updateOverlay = true;
			stuntsData.availableForFrontend = true;

			if (addCombo)
			{
				stunt.doneTimes++;
				stunt.positiveProgress = 0;
				Debug.Log("doneTimes++");
			}
			else
			{
				stunt.positiveProgress += addProgress;
			}
		}
		else if(stunt.positiveProgress > 0)
		{
			stunt.positiveProgress += addProgress;
			if (stunt.positiveProgress < 0)
			{
				if (stunt.doneTimes > 0)
				{
					stuntPai.level++;
					stuntPai.score += (int)((starLevel + 1) * 1.5f * stunt.score);
					AcceptExtraStunt();
				}
				else
				{
					stunt.positiveProgress = 0;
					stunt.doneTimes = 0;
					stunt.updateOverlay = false;
				}
			}
		}
		d_positiveProgress = stunt.positiveProgress;
	}
	void DriftDetector()
	{
		//if (Info.s_raceType != Info.RaceType.Drift)
		//{
		//	drifting = false;
		//	driftDist = 0;
		//	driftScore = 0;
		//	driftString = "";
		//	return;
		//}
		//endDriftTime = vp.groundedWheels > 0 ? (Mathf.Abs(vp.localVelocity.x) > 5 ? StuntManager.driftConnectDelayStatic : Mathf.Max(0, endDriftTime - Time.timeScale * TimeMaster.inverseFixedTimeFactor)) : 0;
		//drifting = endDriftTime > 0;

		//if (drifting)
		//{
		//	driftScore += (StuntManager.driftScoreRateStatic * Mathf.Abs(vp.localVelocity.x)) * Time.timeScale * TimeMaster.inverseFixedTimeFactor;
		//	driftDist += vp.velMag * Time.fixedDeltaTime;
		//	driftString = "Drift: " + driftDist.ToString("n0") + " m";

		//	if (engine)
		//	{
		//		//vp.batteryRemaining += (StuntManager.driftBoostAddStatic * Mathf.Abs(vp.localVelocity.x)) * Time.timeScale * 0.0002f * TimeMaster.inverseFixedTimeFactor;
		//	}
		//}
		//else
		//{
		//	score += driftScore;
		//	driftDist = 0;
		//	driftScore = 0;
		//	driftString = "";
		//}
		// side grind detector -> change into drifts
		// 2s - gives side grind score (400)
		if (vp.colliding && vp.reallyGroundedWheels > 2)
		{
			ProgressDrift(StuntsData.ExtraName.SideGrind, 200 * Time.fixedDeltaTime);
			if (sideGrindTimer > 2)
			{
				ProgressDrift(StuntsData.ExtraName.SideGrind, 200 * Time.fixedDeltaTime);
			}
		}
		else
		{
			ProgressDrift(StuntsData.ExtraName.SideGrind, -200 * Time.fixedDeltaTime);
		}
	}
	void StuntDetector()
	{
		// grind detector
		//if(vp.reallyGroundedWheels < 2 && )

		// side grind detector
		// 2s - gives side grind score (400)
		//if (vp.colliding && vp.reallyGroundedWheels > 2)
		//{
		//	sideGrindTimer += Time.fixedDeltaTime;
		//	if (sideGrindTimer > 2)
		//	{
		//		StartCoroutine(AddExtraStuntCo(StuntsData.ExtraName.SideGrind));
		//		sideGrindTimer = 0;
		//	}
		//}
		//else
		//{
		//	sideGrindTimer = 0;
		//}

		// trickstart/wheelie/stoppie detection
		if (vp.velMag < .5f)
			carStoppedTime = Time.time;

		if (vp.reallyGroundedWheels == 2)
		{
			if (vp.wheels[0].groundedReally && vp.wheels[1].groundedReally)
				stoppieTimer += Time.fixedDeltaTime;
			else if (vp.wheels[2].groundedReally && vp.wheels[3].groundedReally)
				wheelieTimer += Time.fixedDeltaTime;

			if (wheelieTimer > .6f)
			{
				if (Time.time - carStoppedTime < 2)
				{
					StartCoroutine(AddExtraStuntCo(StuntsData.ExtraName.Trikstart));
					wheelieTimer = -99;
				}
				else
				{
					StartCoroutine(AddExtraStuntCo(StuntsData.ExtraName.Wheelie));
					wheelieTimer = -99;
				}
			}
			else if (stoppieTimer > .6f)
			{
				StartCoroutine(AddExtraStuntCo(StuntsData.ExtraName.Stoppie));
				stoppieTimer = -99;
			}
		}
		else
		{
			wheelieTimer = 0;
			stoppieTimer = 0;
		}

		// stunt detection
		if (vp.reallyGroundedWheels == 0)
		{
			if (!prevGroundedWheels0)
			{
				stableLandingTimer = 2;
				w = vp.rb.velocity;
				//w.y = 0; 
				w = w.normalized;

				prevGroundedWheels0 = true;
				foreach (Flip stunt in stuntsData.flipData)
				{
					stunt.positiveProgress = 0;
					stunt.negativeProgress = 0;
					stunt.w = w;
				}
			}

			prevGroundedWheels0 = true;

			if (stableLandingTimer == 2)
			{
				Vector3 normA = vp.rb.angularVelocity.normalized;
				Vector3 lA = vp.transform.InverseTransformDirection(vp.rb.angularVelocity);
				Vector3 normlA = lA.normalized;
				//Debug.DrawRay(vp.transform.position, lA, Color.red, 10);
				Vector3 foundMoves = Vector3.zero;
				int i = 0;
				foreach (Flip stunt in stuntsData.flipData)
				{
					//w_A_dot = Mathf.Abs(Vector3.Dot(w, normA));
					bool w_A_test = stunt.req_w_and_Angular_relation == VectorRelationship.None ||
						Flip.GetRelationship(w, normA) == stunt.req_w_and_Angular_relation;
					if (!w_A_test)
					{
						//if (i == 1)
						//	Debug.Log("w_A_test");
						continue;

					}
					bool gY_A_test = stunt.req_globalY_and_Angular_relation == VectorRelationship.None ||
						Flip.GetRelationship(Vector3.up, normA) == stunt.req_globalY_and_Angular_relation;
					if (!gY_A_test)
					{
						//if(i==1)
						//	Debug.Log("gY_A_test");
						continue;

					}
					var relationship = Flip.GetRelationship(normlA, stunt.rotationAxis);
					bool lA_car_test = relationship == VectorRelationship.None || relationship == VectorRelationship.Parallel;
					if (!lA_car_test)
					{
						//if (i == 1)
						//	Debug.Log("lA_car_test");
						continue;

					}
					bool Y_ok = stunt.StuntingCarAlignmentConditionFulfilled(vp);
					if (!Y_ok)
					{
						//if (i == 1)
						//	Debug.Log("Y_ok");
						continue;
					}

					i++;
					if (foundMoves.x == 0 && stunt.rotationAxis.x != 0 && lA.x != 0)
					{
						//Debug2Mono.DrawText(vp.transform.position, lA.x.ToString(),5,Color.blue);
						//Debug.DrawRay(vp.transform.position, normA, Color.red, 5);
						//Debug.Log(lA.x);
						stunt.AddProgress(lA.x * Time.fixedDeltaTime, vp);
						foundMoves.x = 1;
						//stunt.PrintProgress();
					}
					if (foundMoves.y == 0 && stunt.rotationAxis.y != 0 && lA.y != 0)
					{
						stunt.AddProgress(lA.y * Time.fixedDeltaTime, vp);
						foundMoves.y = 1;
					}
					if (foundMoves.z == 0 && stunt.rotationAxis.z != 0 && lA.z != 0)
					{
						stunt.AddProgress(lA.z * Time.fixedDeltaTime, vp);
						foundMoves.z = 1;
					}

					if (stunt.allowHalfs && stunt.positiveProgress * Mathf.Rad2Deg >= stunt.angleThreshold / 2f
						&& stunt.negativeProgress * Mathf.Rad2Deg >= stunt.angleThreshold / 2f
						&& stunt.CarAlignmentConditionFulfilled(vp))
					{ // done half rotation two-directions
						stuntsData.availableForFrontend = true;
						stunt.ResetProgress();
						stunt.updateOverlay = true;

						bool reverse = stunt.IsReverse(vp) && stunt.canBeReverse;
						stunt.WriteHalfOverlayName(reverse);
						stuntPai.level++;
						stuntPai.score += (int)((starLevel + 1) * 1.5f * stunt.score * (reverse ? 2 : 1) * (!evoModule.IsStunting() ? 2 : 1));
					}
					else if ((stunt.positiveProgress * Mathf.Rad2Deg >= stunt.angleThreshold
						|| stunt.negativeProgress * Mathf.Rad2Deg >= stunt.angleThreshold)
						&& stunt.CarAlignmentConditionFulfilled(vp))
					{ // done full rotation
						stuntsData.availableForFrontend = true;
						stunt.doneTimes++;
						stunt.updateOverlay = true;
						bool reverse = stunt.IsReverse(vp) && stunt.canBeReverse;
						stunt.WriteOverlayName(reverse, !evoModule.IsStunting());
						stunt.ResetProgress();
						stuntPai.level++;
						stuntPai.score += (int)((starLevel + 1) * stunt.score * (reverse ? 2 : 1) * (!evoModule.IsStunting() ? 2 : 1));
					}
				}
			}
		}
		else
		{
			if (prevGroundedWheels0) // first frame on road after landing
			{
				foreach (Flip stunt in stuntsData.flipData)
				{
					stunt.positiveProgress = 0;
					stunt.negativeProgress = 0;
					stunt.doneTimes = 0;
				}
				prevGroundedWheels0 = false;
				stableLandingTimer = .5f;
			}
			if (stableLandingTimer > 0 && vp.reallyGroundedWheels == 4)
				stableLandingTimer -= Time.fixedDeltaTime;

			if (stableLandingTimer != -1 && vp.velMag < 14)
				DeclineStunt();

			if (stableLandingTimer != -1 && stableLandingTimer <= 0)
				AcceptStunt();

			if (grantedComboTime > 0)
			{
				if (vp.reallyGroundedWheels > 0 && stableLandingTimer == -1)
					grantedComboTime -= Time.fixedDeltaTime;
			}
			else
			{// end combo
				starLevel = 0;
			}
		}
	}
	void DeclineStunt()
	{
		//Debug.Log(vp.tr.name + " Decline");
		stableLandingTimer = -1;
		prevStuntPai = null;
		StuntPaiReset();
	}
	void AcceptExtraStunt()
	{
		if (stuntPai.score > 0 && stableLandingTimer == -1)
		{
			aero += stuntPai.score / 10f;
			prevStuntPai = new PtsAnimInfo(stuntPai);
			vp.ChargeBatteryByStunt();
		}
		StuntPaiReset();
	}
	void AcceptStunt()
	{
		stableLandingTimer = -1;
		if (stuntPai.score > 0)
		{
			SetGrantedComboTime(5 + 0.5f * starLevel);
			vp.ChargeBatteryByStunt();

			aero += stuntPai.score / 10f;
			//Debug.Log(vp.tr.name + " Accept");
			prevStuntPai = new PtsAnimInfo(stuntPai);
			StuntPaiReset();
		}
	}
	void SetGrantedComboTime(float seconds)
	{
		grantedComboTime = seconds;
		starLevel = Mathf.Clamp(++starLevel, 0, 10);
	}
	void StuntPaiReset()
	{
		stuntPai.score = 0;
		stuntPai.level = -1;
	}
	public void ResetOnTrack()
	{
		StuntPaiReset();
		starLevel = 0;
		stableLandingTimer = -1;
		jumpTimer = 0;
	}
	public StuntSeqStatus StuntSeqEnded(out PtsAnimInfo pai)
	{
		pai = prevStuntPai;
		prevStuntPai = null;

		if (pai == null)
		{
			if (stableLandingTimer > -1)
				return StuntSeqStatus.Ongoing;
			return StuntSeqStatus.None;
		}
		else
			return StuntSeqStatus.Ended;
	}

	private void JumpDetector()
	{
		if (Time.timeScale != 0)
		{
			// aero meter movements
			aeroMeterVelocity = Mathf.Lerp(aeroMeterVelocity,
			(vp.reallyGroundedWheels == 0 && !vp.colliding) ? maxAeroMeterVelocity : minAeroMeterVelocity
			, Time.deltaTime * aeroMeterResponsiveness);
			aero += aeroMeterVelocity;

			// jump detector
			if (vp.reallyGroundedWheels == 0)
			{
				if (!vp.crashing)
				{
					bool traf = Physics.Raycast(vp.tr.position, Vector3.down, out var hit, float.MaxValue, 1 << Info.roadLayer);
					if (!traf || Vector3.Distance(vp.tr.position, hit.point) < 4)
						return;
					if (vp.rb.velocity.y > 0 && vp.velMag > 13)
					{
						jumpTimer += Time.deltaTime;
					}
					else if (vp.rb.velocity.y < 0 && prevVel >= 0)
					{ // jump pts
						int level = -1;
						int score = 0;
						if (jumpTimer >= 2f)
						{
							level = 4;
							score = 2000;
						}
						else if (jumpTimer >= 1.5f)
						{
							level = 3;
							score = 1000;
						}
						else if (jumpTimer >= 1)
						{
							level = 2;
							score = 500;
						}
						else if (jumpTimer >= 0.75f)
						{
							level = 1;
							score = 125;
						}
						else if (jumpTimer >= 0.5f)
						{
							level = 0;
							score = 100;
						}

						if (score > 0)
						{
							jumpPai = new PtsAnimInfo(score, PtsAnimType.Jump, level);
							aero += score / 10f;
						}
					}
				}
				prevVel = vp.rb.velocity.y;
			}
			else
			{
				jumpTimer = 0;
			}
		}
	}
	

	public void NextLap()
	{
		if (curLap == 0 || vp.followAI.ProgressPercent() > 0.9f || vp.followAI.pitsProgress > 0)
		{
			vp.followAI.progress = 1;
			vp.followAI.curWaypointIdx = 0;
			vp.followAI.curStuntpointIdx = 0;
			vp.followAI.curReplayPointIdx = 0;
			vp.followAI.speedLimitDist = -1;
			var curlaptime = CurLaptime;
			if (curLap < Info.s_laps + 1)
				curLap++;
			lapTimer = 0;
			if (curlaptime.HasValue)
			{
				if (bestLapTime > curlaptime)
					bestLapTime = curlaptime.Value;

				if (!vp.followAI.isCPU && bestLapTime < vp.raceBox.raceManager.hud.bestLapTime)
				{
					vp.raceBox.raceManager.hud.bestLapTime = bestLapTime;
					raceManager.hud.lapRecordSeq.gameObject.SetActive(true);
				}
			}
			if (curLap == Info.s_laps + 1)
			{ // race finished
				int curPos = raceManager.Position(vp);
				if (curPos == 1)
				{
					raceManager.hud.AddMessage(new(vp.tr.name + " WINS THE RACE!", BottomInfoType.CAR_WINS));
				}
				// in racemode after the end of a race, cars still run around the track, ghosts overtake each other. Don't let it change results
				curLap += 100 * (Info.s_rivals + 1 - curPos);
				vp.ghostComponent.SetHittable(false);
				raceTime = DateTime.Now - Info.raceStartDate;
				vp.followAI.SetCPU(true);
			}
		}
	}

	
}
