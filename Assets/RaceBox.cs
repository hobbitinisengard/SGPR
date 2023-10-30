using RVP;
using System;
using UnityEngine;
using System.Linq;
using static PtsAnim;

public class StuntRotInfo
{
	public int axis;
	public int rotation;
	public override string ToString()
	{
		return (axis > 1 ? 'Z' : (axis == 0 ? 'X' : 'Y')) + rotation.ToString();
	}
}
public enum StuntSeqStatus { None, Ongoing, Ended };

public class RaceBox : MonoBehaviour
{
	public float progress;
	public int curStuntpointIdx;
	public VehicleParent vp { get; private set; }
	SGP_Evo evoModule;
	public float distance { get; private set; }
	public float aero { get; private set; }
	private DateTime lapStartTime;
	public TimeSpan bestLapTime { get; private set; }
	/// <summary>
	/// set only after the race
	/// </summary>
	public TimeSpan raceTime { get; private set; }
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
	//Stack<StuntRotInfo> rots;
	Stunt[] stunts;
	private bool stuntsAvailableForFrontend = false;
	public float lastTimeInAir;
	[SerializeField]
	float stableLandingTimer = -1;
	private float prevVel;
	private float jumpTimer;
	private PtsAnimInfo stuntPai;
	private PtsAnimInfo prevStuntPai;

	private PtsAnimInfo jumpPai;
	public Vector3 elecTunnelCameraPos = -Vector3.one;
	private DateTime raceStartTime;
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
		lapStartTime = DateTime.MinValue;
		if (Info.tracks[Info.s_trackName].records[0].secondsOrPts > 0)
			bestLapTime = new TimeSpan(0, 0, (int)Info.tracks[Info.s_trackName].records[0].secondsOrPts);
		else
			bestLapTime = TimeSpan.MaxValue;
		raceTime = TimeSpan.MinValue;

		curLap = 0;
		starLevel = 0;
		//rots = new Stack<StuntRotInfo>(32);
		var globalcontrol = GameObject.Find("Canvas");
		stunts = globalcontrol.transform.GetComponent<StuntManager>().allPossibleFlips.ToArray();
		stuntPai = new PtsAnimInfo(0, PtsAnimType.Evo, -1);
	}
	public string Result(Info.RecordType recordType)
	{
		switch (recordType)
		{
			case Info.RecordType.BestLap:
				if (bestLapTime == TimeSpan.MaxValue)
					return "-";
				if(bestLapTime.Hours > 0)
					return bestLapTime.ToString(@"hh\:mm\:ss\.ff");
				return bestLapTime.ToString(@"mm\:ss\.ff");
			case Info.RecordType.RaceTime:
				if (raceTime == TimeSpan.MinValue)
					return "-";
				if (raceTime.Hours > 0)
					return raceTime.ToString(@"hh\:mm\:ss\.ff");
				return raceTime.ToString(@"mm\:ss\.ff");
			case Info.RecordType.StuntScore:
				return ((int)(aero*10)).ToString();
			case Info.RecordType.DriftScore:
				return driftPts.ToString();
			default:
				return "-";
		}
	}
	public bool Finished()
	{
		return curLap > Info.s_laps;
	}
	private void OnEnable()
	{
		raceStartTime = DateTime.Now;
	}
	private void FixedUpdate()
	{
		if (vp.groundedWheels == 0)
			lastTimeInAir = Time.time;

		if(!Finished())
		{
			StuntDetector();
			JumpDetector();
		}
	}
	public bool GetStuntsSeq(ref Stunt[] stunts)
	{
		if (stuntsAvailableForFrontend)
		{
			stuntsAvailableForFrontend = false;
			stunts = this.stunts;
			return true;
		}
		else
			return false;
	}

	void StuntDetector()
	{
		if (vp.groundedWheels == 0)
		{
			if (!prevGroundedWheels0)
			{
				stableLandingTimer = 2;
				w = vp.rb.velocity;
				//w.y = 0; 
				w = w.normalized;

				prevGroundedWheels0 = true;
				foreach (RotationStunt stunt in stunts.Cast<RotationStunt>())
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
				foreach (RotationStunt stunt in stunts)
				{
					//w_A_dot = Mathf.Abs(Vector3.Dot(w, normA));
					bool w_A_test = stunt.req_w_and_Angular_relation == VectorRelationship.None ||
						RotationStunt.GetRelationship(w, normA) == stunt.req_w_and_Angular_relation;
					if (!w_A_test)
					{
						//if (i == 1)
						//	Debug.Log("w_A_test");
						continue;

					}
					bool gY_A_test = stunt.req_globalY_and_Angular_relation == VectorRelationship.None ||
						RotationStunt.GetRelationship(Vector3.up, normA) == stunt.req_globalY_and_Angular_relation;
					if (!gY_A_test)
					{
						//if(i==1)
						//	Debug.Log("gY_A_test");
						continue;

					}
					var relationship = RotationStunt.GetRelationship(normlA, stunt.rotationAxis);
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
						stuntsAvailableForFrontend = true;
						stunt.ResetProgress();
						stunt.updateOverlay = true;

						bool reverse = stunt.IsReverse(vp) && stunt.canBeReverse;
						stunt.WriteHalfOverlayName(reverse, !evoModule.IsStunting());
						stuntPai.level++;
						stuntPai.score += (int)((starLevel + 1) * 1.5f * stunt.score * (reverse ? 2 : 1) * (!evoModule.IsStunting() ? 2 : 1));
					}
					else if ((stunt.positiveProgress * Mathf.Rad2Deg >= stunt.angleThreshold
						|| stunt.negativeProgress * Mathf.Rad2Deg >= stunt.angleThreshold)
						&& stunt.CarAlignmentConditionFulfilled(vp))
					{ // done full rotation
						stuntsAvailableForFrontend = true;
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
				foreach (RotationStunt stunt in stunts)
				{
					stunt.positiveProgress = 0;
					stunt.negativeProgress = 0;
				}
				prevGroundedWheels0 = false;
				stableLandingTimer = .5f;
			}
			if (stableLandingTimer > 0 && vp.groundedWheels == 4)
				stableLandingTimer -= Time.fixedDeltaTime;

			if (stableLandingTimer != -1 && vp.velMag < 14)
				DeclineStunt();

			if (stableLandingTimer != -1 && stableLandingTimer <= 0)
				AcceptStunt();
			
			if (grantedComboTime > 0)
			{
				if(vp.groundedWheels > 0 && stableLandingTimer == -1)
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
		stableLandingTimer = -1;
		prevStuntPai = null;
		StuntPaiReset();
	}
	void AcceptStunt()
	{
		stableLandingTimer = -1;

		if (stuntPai.score > 0)
		{
			SetGrantedComboTime(5 + 0.5f * starLevel);
			vp.battery = Mathf.Clamp01(vp.battery + vp.engine.batteryStuntIncrease);
			aero += stuntPai.score / 10f;
			Debug.Log("Accept");
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
			(vp.groundedWheels == 0 && !vp.colliding) ? maxAeroMeterVelocity : minAeroMeterVelocity
			, Time.deltaTime * aeroMeterResponsiveness);
			aero += aeroMeterVelocity;

			// jump detector
			if (vp.groundedWheels == 0)
			{
				if (!vp.colliding)
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
	void SetGrantedComboTime(float seconds)
	{
		Debug.Log(seconds);
		grantedComboTime = seconds;
		starLevel = Mathf.Clamp(++starLevel, 0, 10);
	}
	public TimeSpan? CurLapTime()
	{
		if (lapStartTime == DateTime.MinValue)
			return null;
		return DateTime.Now - lapStartTime;
	}
	public void NextLap()
	{
		if (curLap == 0 || vp.followAI.ProgressPercent() > 0.8f)
		{
			vp.followAI.progress = 0.1f;
			vp.followAI.curWaypointIdx = 0;
			vp.followAI.curStuntpointIdx = 0;
			vp.followAI.curReplayPointIdx = 0;
			var curlaptime = CurLapTime();
			lapStartTime = DateTime.Now;
			curLap++;
			if (curLap > Info.s_laps)
			{ // race finished
				GetComponent<Ghost>().SetHittable(false);
				raceTime = DateTime.Now - raceStartTime;
				vp.followAI.SetCPU(true);
			}
			else if (curlaptime!=null)
			{
				var recordTable = Info.tracks[Info.s_trackName].records;

				if (curlaptime.Value.TotalSeconds < recordTable[0].secondsOrPts)
					recordTable[0].secondsOrPts = (float)curlaptime.Value.TotalSeconds;
				if (bestLapTime < curlaptime)
					bestLapTime = curlaptime.Value;
			}
			
		}
	}
}
