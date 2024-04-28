using RVP;
using System;
using UnityEngine;
using static PtsAnim;
using System.Collections.Generic;
using System.Collections;
using Unity.Netcode;
using System.Linq;

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
		Trikstart, Wheelie, Handstand, Looper, Grind, Slide, Powerslide,
		SidewinderLeft,
		SidewinderRight
	}
	public bool availableForFrontend;
	public Flip[] flipData; // Meteor X1 | Backflip 360
	public Drift driftData; // Slide x1 123 & Powerslide x1 456
	public Stunt[] extraData;
	public StuntsData(Flip[] flipData)
	{
		// deep copy
		this.flipData = new Flip[flipData.Length];
		for (int i = 0; i < flipData.Length; ++i)
			this.flipData[i] = new Flip(flipData[i]);

		driftData = new Drift("Slide", 0);
		extraData = new Stunt[] // = ExtraName.Length
		{
			new Stunt("TRICKSTART", 550),
			new Stunt("WHEELIE", 350),
			new Stunt("HANDSTAND", 350),
			new Stunt("SUPER LOOPER", 1750),
			new Stunt("GRIND", 2000),
			new Stunt("SLIDE", 350),
			new Stunt("POWERSLIDE", 600),
			new Stunt("SIDEWINDER LEFT", 550),
			new Stunt("SIDEWINDER RIGHT", 550),
		};
	}

	IEnumerator<Stunt> IEnumerable<Stunt>.GetEnumerator()
	{
		foreach (var item in flipData)
		{
			yield return item;
		}

		yield return driftData;

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
	public VehicleParent vp { get; private set; }
	public SGP_Evo evoModule { get; private set; }
	public float distance { get; private set; }
	private float aero;
	public float Aero
	{
		get { return 10 * aero; }
		set { aero = value; }
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

	public int curLap;

	public float w_A_dot;

	public int starLevel;
	public float drift;
	public float grantedComboTime;
	
	public float topMeterSpeed = 0;
	float aeroMeterResponsiveness = 1f;
	bool prevGroundedWheels0;
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
	public float handstandTimer;
	public float wheelieTimer;
	float lastLooperTime;
	//public float d_positiveProgress;
	public float d_effectiveTurnAngle;
	private float prevSmoothedDriftAngle;
	private float smoothedDriftAngle;
	public float driftingTime;
	public float driftingTimer;
	private float sidewinderLeftTimer;
	private float sidewinderRightTimer;

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
		lapTimer = 3600 * 24; // 24 hours
		bestLapTime = TimeSpan.MaxValue;//TimeSpan.FromSeconds(F.I.tracks[F.I.s_trackName].records[0].secondsOrPts);
		raceTime = TimeSpan.MinValue;
		curLap = 0;
		starLevel = 0;
		stuntPai = new PtsAnimInfo(0, PtsAnimType.Evo, -1);
	}
	
	public string Result(RecordType recordType)
	{
		switch (recordType)
		{
			case RecordType.BestLap:
				return bestLapTime.ToLaptimeStr();
			case RecordType.RaceTime:
				return raceTime.ToLaptimeStr();
			case RecordType.StuntScore:
				return ((int)(aero * 10)).ToString();
			case RecordType.DriftScore:
				return drift.ToString("F0");
			default:
				return "-";
		}
	}

	private void FixedUpdate()
	{
		if(F.I.s_laps > 0)
		{
			if (!F.I.gamePaused && curLap > 0)
				lapTimer += Time.fixedDeltaTime;
			if (vp.reallyGroundedWheels == 0)
				lastTimeInAir = Time.time;

			JumpDetector();
			StuntDetector();
			DriftDetector();

			if (F.I.s_raceType != RaceType.Drift)
				FlipDetector();
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
		stableLandingTimer = -1;
		AcceptExtraStunt();
		yield return null;
		//stunt.doneTimes = 0;
	}
	public void AddLooper()
	{
		if (Time.time - lastLooperTime > 1)
		{
			StartCoroutine(AddExtraStuntCo(StuntsData.ExtraName.Looper));
			lastLooperTime = Time.time;
		}
	}
	void ProgressDrift(float addProgress, bool addCombo = false)
	{
		var drift = stuntsData.driftData;
		string overlayName;
		if (driftingTimer <= 2)
			overlayName = "Slide";
		else if (driftingTimer <= 4)
			overlayName = "Powerslide";
		else if (driftingTimer <= 6)
			overlayName = "Superslide";
		else if (driftingTimer <= 8)
			overlayName = "Megaslide";
		else
			overlayName = "Masterslide";

		drift.overlayName = overlayName;

		if (addProgress > 0 || addCombo)
		{
			drift.updateOverlay = true;
			stuntsData.availableForFrontend = true;
		}

		if (!addCombo)
		{
			drift.positiveProgress += addProgress;
			//Debug.Log(drift.positiveProgress + " " + drift.doneTimes);
		}
		else
		{
			drift.doneTimes++;
		}
	}
	void ResetDrift()
	{
		driftingTimer = 0;
		driftingTime = 0;
		var drift = stuntsData.driftData;
		if (starLevel > 0)
		{
			stuntPai.level = 0;
			stuntPai.score = (int)(drift.positiveProgress);
			grantedComboTime = -1;
			AcceptStunt();
		}
		
		drift.positiveProgress = 0;
		drift.doneTimes = 0;
		drift.updateOverlay = false;
		
		driftingTime = 0;
		starLevel = 0;
	}

	void DriftDetector()
	{
		// slide detector
		if (vp.localVelocity.z > 5f)
		{
			Vector3 locVel = vp.localVelocity;
			locVel.y = 0;
			d_effectiveTurnAngle = Vector3.SignedAngle(Vector3.forward, locVel, Vector3.up);
		}
		else
		{
			d_effectiveTurnAngle = 0;
		}
		prevSmoothedDriftAngle = smoothedDriftAngle;
		smoothedDriftAngle = Mathf.Lerp(smoothedDriftAngle, d_effectiveTurnAngle, 10 * Time.fixedDeltaTime);


		if (F.I.s_raceType == RaceType.Drift)
		{
			if (vp.crashing)
			{
				//Debug.LogError("crashed");
				ResetDrift();
			}


			float addDriftPoints = 0;
			if (vp.reallyGroundedWheels >= 3 && Mathf.Abs(smoothedDriftAngle) > 5 && vp.velMag > 30)
			{ // drifting
				float comboMult = (int)(9 / 8f * Mathf.Clamp(driftingTimer, 0, 8));
				addDriftPoints = Time.fixedDeltaTime * vp.velMag * Mathf.InverseLerp(0, 60, Mathf.Abs(smoothedDriftAngle));
				grantedComboTime = 3;
				driftingTimer += Time.fixedDeltaTime;
				ProgressDrift((1 + comboMult) * addDriftPoints);
			}
			else
				if(driftingTimer > 0)
					driftingTimer-=Time.fixedDeltaTime;

			if (vp.velMag < 5)
				ResetDrift();

			topMeterSpeed = Mathf.Lerp(topMeterSpeed,
				(vp.reallyGroundedWheels >= 3) ? addDriftPoints : 0,
				Time.fixedDeltaTime * aeroMeterResponsiveness);
			drift += topMeterSpeed;

			if (prevSmoothedDriftAngle * smoothedDriftAngle <= 0 && vp.reallyGroundedWheels == 4 && driftingTimer > 1)
			{ // switching directions
				grantedComboTime = 3;
				starLevel = Mathf.Clamp(++starLevel, 0, 10);
				ProgressDrift(0, true);
				vp.ChargeBatteryByStunt();
				driftingTimer = 0;
			}
			if (grantedComboTime > 0 && vp.reallyGroundedWheels > 0)
				grantedComboTime = Mathf.Clamp(grantedComboTime-Time.fixedDeltaTime,0,3);

			if (grantedComboTime == 0)
			{ // no drifting for long

				var drift = stuntsData.driftData;
				if (drift.doneTimes > 0)
				{
					stuntPai.level = (int)Mathf.Clamp((drift.doneTimes-1)/2f,0,4);
					stuntPai.score = (int)(drift.positiveProgress * drift.doneTimes);
					grantedComboTime = -1;
					AcceptStunt();
				}

				drift.positiveProgress = 0;
				drift.doneTimes = 0;
				drift.updateOverlay = false;
				
				driftingTimer = 0;
				starLevel = 0;
			}
		}
		else
		{
			if (Mathf.Abs(smoothedDriftAngle) > 5 && vp.velMag > 30 && !vp.colliding)
			{
				driftingTime = Time.time;
				driftingTimer += Time.fixedDeltaTime;
			}
			if (driftingTimer > 0 && Time.time - driftingTime > .3f)
			{
				if (driftingTimer > 3)
				{
					StartCoroutine(AddExtraStuntCo(StuntsData.ExtraName.Powerslide));
					drift += stuntsData.extraData[(int)StuntsData.ExtraName.Powerslide].score;
				}
				else if (driftingTimer > 2)
				{
					StartCoroutine(AddExtraStuntCo(StuntsData.ExtraName.Slide));
					drift += stuntsData.extraData[(int)StuntsData.ExtraName.Slide].score;
				}
				driftingTime = 0;
				driftingTimer = 0;
			}
		}
	}
	void StuntDetector()
	{
		// trickstart/wheelie/stoppie detection
		if (vp.velMag < .5f)
			carStoppedTime = Time.time;

		if (vp.reallyGroundedWheels == 2)
		{
			if(vp.wheels[0].groundedReally)
				if(vp.wheels[1].groundedReally)
					handstandTimer += Time.fixedDeltaTime;
				else if(vp.wheels[2].groundedReally)
					sidewinderLeftTimer += Time.fixedDeltaTime;

			if (vp.wheels[2].groundedReally)
				if (vp.wheels[3].groundedReally)
					wheelieTimer += Time.fixedDeltaTime;
				else if (vp.wheels[1].groundedReally)
					sidewinderRightTimer += Time.fixedDeltaTime;

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
			else if (handstandTimer > 1)
			{
				StartCoroutine(AddExtraStuntCo(StuntsData.ExtraName.Handstand));
				handstandTimer = -99;
			}
			else if(sidewinderLeftTimer > 1)
			{
				StartCoroutine(AddExtraStuntCo(StuntsData.ExtraName.SidewinderLeft));
				sidewinderLeftTimer = -99;
			}
			else if (sidewinderRightTimer > 1)
			{
				StartCoroutine(AddExtraStuntCo(StuntsData.ExtraName.SidewinderRight));
				sidewinderRightTimer = -99;
			}
		}
		else
		{
			wheelieTimer = 0;
			handstandTimer = 0;
			sidewinderRightTimer = 0;
			sidewinderLeftTimer = 0;
		}
	}
	void FlipDetector() 
	{ 
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
					stunt.isReverse = false;
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

						stunt.WriteHalfOverlayName();
						stuntPai.level++;
						stuntPai.score += (int)((starLevel + 1) * 1.5f * stunt.score * (stunt.isReverse ? 2 : 1) * (!evoModule.IsStunting ? 2 : 1));
					}
					else if ((stunt.positiveProgress * Mathf.Rad2Deg >= stunt.angleThreshold
						|| stunt.negativeProgress * Mathf.Rad2Deg >= stunt.angleThreshold)
						&& stunt.CarAlignmentConditionFulfilled(vp))
					{ // done full rotation
						stuntsData.availableForFrontend = true;
						stunt.doneTimes++;
						stunt.updateOverlay = true;
						stunt.WriteOverlayName(!evoModule.IsStunting);
						stunt.ResetProgress();
						stuntPai.level++;
						stuntPai.score += (int)((starLevel + 1) * stunt.score * (stunt.isReverse ? 2 : 1) * (!evoModule.IsStunting ? 2 : 1));
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
			if (stableLandingTimer > 0 && vp.reallyGroundedWheels == 4 && vp.followAI.overRoad)
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
			vp.ChargeBatteryByStunt();
			if (F.I.s_raceType == RaceType.Drift)
				drift += stuntPai.score;
			else
			{
				grantedComboTime = 5 + 0.5f * starLevel;
				starLevel = Mathf.Clamp(++starLevel, 0, 10);
				aero += stuntPai.score / 10f;
			}
			//Debug.Log(vp.tr.name + " Accept");
			prevStuntPai = new PtsAnimInfo(stuntPai);
			StuntPaiReset();
		}

	}
	void StuntPaiReset()
	{
		stuntPai.score = 0;
		stuntPai.level = -1;
	}
	public void ResetOnTrack()
	{
		if(F.I.s_laps > 0)
		{
			DeclineStunt();
			evoModule.Reset();
			grantedComboTime = 0;
			starLevel = 0;
			stableLandingTimer = -1;
			jumpTimer = 0;
			if (F.I.s_raceType == RaceType.Drift)
				ResetDrift();
		}
	}
	public StuntSeqStatus StuntSeqEnded(out PtsAnimInfo pai)
	{
		pai = prevStuntPai;
		prevStuntPai = null;

		if(F.I.s_raceType == RaceType.Drift)
		{
			if (pai == null)
			{
				if (grantedComboTime > 0)
					return StuntSeqStatus.Ongoing;
				return StuntSeqStatus.None;
			}
			else
				return StuntSeqStatus.Ended;
		}


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
			float maxAeroMeterVelocity = 0.05f;
			float minAeroMeterVelocity = 0;
			topMeterSpeed = Mathf.Lerp(topMeterSpeed,
			(vp.reallyGroundedWheels == 0 && !vp.colliding) ? maxAeroMeterVelocity : minAeroMeterVelocity,
			Time.deltaTime * aeroMeterResponsiveness);
			aero += topMeterSpeed;

			// jump detector
			if (vp.reallyGroundedWheels == 0)
			{
				if (!vp.crashing)
				{
					bool traf = Physics.Raycast(vp.tr.position, Vector3.down, out var hit, float.MaxValue, 1 << F.I.roadLayer);
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
		if(F.I.s_laps > 0)
		{
			if (curLap == 0 || vp.followAI.LapProgressPercent > 0.9f || vp.followAI.pitsProgress > 0)
			{
				vp.followAI.NextLap();

				if (F.I.s_raceType == RaceType.TimeTrial)
					vp.energyRemaining = vp.batteryCapacity;

				var curlaptime = CurLaptime;
				lapTimer = 0;
				if (vp.Owner && curlaptime.HasValue)
				{
					if (bestLapTime > curlaptime)
					{
						bestLapTime = curlaptime.Value;
					}

					if (!vp.followAI.isCPU && bestLapTime < RaceManager.I.hud.bestLapTime)
					{
						RaceManager.I.hud.bestLapTime = bestLapTime;
						RaceManager.I.hud.lapRecordSeq.gameObject.SetActive(true);
					}
				}
				if (curLap <= F.I.s_laps)
					curLap++;

				if (F.I.s_raceType == RaceType.Knockout && curLap > 1 && RaceManager.I.Position(vp) + 1 == RaceManager.I.ActiveCarsInKnockout)
				{ // last car is knocked-out
					RaceManager.I.KnockOutLastCar();
				}

				if (curLap == F.I.s_laps + 1) // race finished
				{
					int curPos = RaceManager.I.Position(vp);
					if (!F.I.s_inEditor && curPos == 1)
					{
						RaceManager.I.hud.infoText.AddMessage(new(vp.tr.name + " WINS THE RACE!", BottomInfoType.CAR_WINS));
						OnlineCommunication.I.CountdownTillForceEveryoneToResults();
					}
					// in racemode after the end of a race, cars still run around the track, ghosts overtake each other. Don't let it change results
					curLap += 100 * (F.I.s_cars.Count+1 - curPos);

					if (vp.Owner)
						vp.ghost.SetGhostPermanently();

					if (F.I.s_raceType == RaceType.Drift)
					{
						var drift = stuntsData.driftData;
						if (drift.doneTimes > 0)
						{
							stuntPai.level = (int)Mathf.Clamp((drift.doneTimes - 1) / 2f, 0, 4);
							stuntPai.score = (int)(drift.positiveProgress * drift.doneTimes);
							grantedComboTime = -1;
							AcceptStunt();
						}
					}

					if (F.I.gameMode == MultiMode.Multiplayer && vp.Owner && vp == RaceManager.I.playerCar)
					{
						RaceManager.I.hud.endraceTimer.gameObject.SetActive(false);
					}
					if (enabled)
					{
						enabled = false;
					}
				}
			}
		}
	}
	/// <summary>
	/// disabling raceBox means the race has ended for this car
	/// Car has driven past finish or has been eliminated.
	/// </summary>
	private void OnDisable()
	{
		//vp.followAI.raceEndedLapProgressPercent = (curLap-1) + vp.followAI.LapProgressPercent;
		vp.sampleText.gameObject.SetActive(false);
		raceTime = DateTime.Now - F.I.raceStartDate;
		
		if (F.I.gameMode == MultiMode.Multiplayer && vp.Owner && vp == RaceManager.I.playerCar)
		{
			vp.SynchRaceboxValuesRpc(curLap, vp.followAI.dist, vp.followAI.progress, aero, drift, (float)bestLapTime.TotalMilliseconds / 1000f,
				(float)raceTime.TotalMilliseconds / 1000f, vp.RpcTarget.Everyone);
		}
		vp.followAI.SetCPU(true);
	}

	public void UpdateValues(int curLap, int dist, int progress, float aero, float drift, float bestLapSecs, float raceTimeSecs)
	{
		this.aero = aero;
		this.drift = drift;
		this.curLap = curLap;
		vp.followAI.dist = dist;
		vp.followAI.progress = progress;
		try
		{
			bestLapTime = TimeSpan.FromMilliseconds(bestLapSecs * 1000);
			if(raceTimeSecs > 0)
				raceTime = TimeSpan.FromMilliseconds(raceTimeSecs * 1000);
		}
		catch
		{
		}
	}
}
