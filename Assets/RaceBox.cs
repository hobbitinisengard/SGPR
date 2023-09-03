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
public class RaceBox : MonoBehaviour
{
	VehicleParent vp;
	SGP_Evo evoModule;
	public float distance { get; private set; }
	public float aero { get; private set; }
	public DateTime lapStartTime;
	public TimeSpan bestLapTime { get; private set; }

	/// <summary>
	/// number of laps done already
	/// </summary>
	public int curLaps { get; private set; }
	/// <summary>
	/// All number of laps of this race
	/// </summary>
	public int LapsCount { get; private set; }
	public float w_A_dot;

	public int starLevel;

	public float grantedComboTime { get; private set; }
	float maxAeroMeterVelocity = 0.05f;
	float minAeroMeterVelocity = 0;
	public float aeroMeterVelocity = 0;
	float aeroMeterResponsiveness = 1f;
	bool prevGroundedWheels0 = false;
	Vector3 w;
	//Stack<StuntRotInfo> rots;
	Stunt[] stunts;
	private bool stuntsAvailableForFrontend = false;
	float stableLandingTimer = -1;
	private float prevVel;
	private float jumpTimer;
	public PtsAnimInfo stuntPai { get; private set; }
	//public PtsAnimInfo stuntPai { get; private set; }

	private PtsAnimInfo jumpPai;
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
		set
		{
			jumpPai = value;
		}
	}
	public PtsAnimInfo prevStuntPai{ get; private set; }

	void Start()
	{
		vp = transform.GetComponent<VehicleParent>();
		evoModule = transform.GetComponent<SGP_Evo>();
		lapStartTime = DateTime.MinValue;
		bestLapTime = TimeSpan.MaxValue;
		curLaps = 0;
		LapsCount = 18;
		starLevel = 0;
		//rots = new Stack<StuntRotInfo>(32);
		var globalcontrol = GameObject.Find("GlobalControl");
		stunts = globalcontrol.transform.GetComponent<StuntManager>().allPossibleFlips.ToArray();
		stuntPai = new PtsAnimInfo(0, PtsAnimType.Evo, -1);
	}
	public Stunt[] GetStuntsSeq()
	{
		if (stuntsAvailableForFrontend)
		{
			stuntsAvailableForFrontend = false;
			return stunts;
		}
		else
			return null;
	}

	void StuntDetector()
	{
		if (vp.groundedWheels == 0)
		{
			if (!prevGroundedWheels0)
			{
				stableLandingTimer = 1;
				w = vp.rb.velocity;
				//w.y = 0; 
				w = w.normalized;

				prevGroundedWheels0 = true;
				foreach (RotationStunt stunt in stunts)
				{
					stunt.positiveProgress = 0;
					stunt.negativeProgress = 0;
					stunt.w = w;
				}
			}

			prevGroundedWheels0 = true;

			if (stableLandingTimer == 1)
			{
				Vector3 normA = vp.rb.angularVelocity.normalized;
				Vector3 lA = vp.transform.InverseTransformDirection(vp.rb.angularVelocity);
				Vector3 normlA = lA.normalized;
				//Debug.DrawRay(vp.transform.position, lA, Color.red, 10);
				Vector3 foundMoves = Vector3.zero;
				foreach (RotationStunt stunt in stunts)
				{
					//w_A_dot = Mathf.Abs(Vector3.Dot(w, normA));
					bool w_A_test = stunt.req_w_and_Angular_relation == VectorRelationship.None ||
						RotationStunt.GetRelationship(w, normA) == stunt.req_w_and_Angular_relation;
					if (!w_A_test)
						continue;
					bool gY_A_test = stunt.req_globalY_and_Angular_relation == VectorRelationship.None ||
						RotationStunt.GetRelationship(Vector3.up, normA) == stunt.req_globalY_and_Angular_relation;
					if (!gY_A_test)
						continue;
					var relationship = RotationStunt.GetRelationship(normlA, stunt.rotationAxis);
					bool lA_car_test = relationship == VectorRelationship.None || relationship == VectorRelationship.Parallel;
					if (!lA_car_test)
						continue;
					bool Y_ok = stunt.StuntingCarAlignmentConditionFulfilled(vp);
					if (!Y_ok)
						continue;

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
				stableLandingTimer = 1;
			}

			
			if (stableLandingTimer > 0 && vp.groundedWheels == 4)
			{
				stableLandingTimer -= Time.fixedDeltaTime;
			}

			if (stableLandingTimer != -1 && stableLandingTimer <= 0)
			{
				stableLandingTimer = -1;
				
				if(stuntPai.score > 0)
				{
					SetGrantedComboTime(5 + 0.5f * starLevel);
					vp.engine.battery = Mathf.Clamp01(vp.engine.battery + vp.engine.batteryStuntIncrease);
					aero += stuntPai.score / 10f;
					prevStuntPai = new PtsAnimInfo(stuntPai);
					StuntPaiReset();
				}
			}
			
			if (grantedComboTime > 0)
			{
				grantedComboTime -= Time.fixedDeltaTime;
			}
			else
			{// end combo
				starLevel = 0;
			}
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
	/// <summary>
	/// returns true if Seq ended. withSuccess is false when vehicle landing failed
	/// </summary>
	public bool StuntSeqEnded(out PtsAnimInfo pai)
	{
		if (prevStuntPai == null)
		{
			pai = null;
			return false;
		}
		else
		{
			pai = prevStuntPai;
			prevStuntPai = null;
			return true;
		}
		//return stableLandingTimer == -1 && prevStuntPai != null;
	}
	private void FixedUpdate()
	{
		StuntDetector();
		JumpDetector();
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
					bool traf = Physics.Raycast(vp.tr.position, Vector3.down, out var hit, float.MaxValue, 1 << 0);
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
	public PtsAnimInfo GetPAI()
	{
		if (jumpPai == null)
		{
			return jumpPai;
		}
		else
		{
			var local_pai = jumpPai;
			jumpPai = null;
			return local_pai;
		}
	}
	void SetGrantedComboTime(float seconds)
	{
		Debug.Log(seconds);
		grantedComboTime = seconds;
		starLevel = Mathf.Clamp(++starLevel, 0, 10);
	}
	public TimeSpan CurLapTime()
	{
		if (lapStartTime == DateTime.MinValue)
			return TimeSpan.MinValue;
		return DateTime.Now - lapStartTime;
	}
	public void NextLap()
	{
		var curlaptime = CurLapTime();
		if (curlaptime < bestLapTime && curlaptime != TimeSpan.MinValue)
			bestLapTime = CurLapTime();

		lapStartTime = DateTime.Now;
		curLaps++;
	}

}
