using RVP;
using System;
using UnityEngine;
using UnityEditor;
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

    public float grantedComboTime;
    public DateTime comboStartTime;
    float maxAeroMeterVelocity = 0.05f;
    float minAeroMeterVelocity = 0;
    public float aeroMeterVelocity = 0;
    float aeroMeterResponsiveness = 1f;
    bool prevGroundedWheels0 = false;
    Vector3 w;
    //Stack<StuntRotInfo> rots;
    Stunt[] stunts;
    float pointsFromThisEvoSeq = 0;
    int starLevelAdd = 0;
    private bool stuntsAvailableForFrontend;
    public bool duringStuntSeqTimer;
    float endStuntSeqTime;
    float stableLandingTimer = -1;
    void Start()
    {
        vp = transform.GetComponent<VehicleParent>();
        evoModule = transform.GetComponent<SGP_Evo>();
        lapStartTime = DateTime.MinValue;
        bestLapTime = TimeSpan.MaxValue;
        curLaps = 0;
        LapsCount = 18;
        starLevel = 0;
        comboStartTime = DateTime.MinValue;
        //rots = new Stack<StuntRotInfo>(32);
        var globalcontrol = GameObject.Find("GlobalControl");
        stunts = globalcontrol.transform.GetComponent<StuntManager>().allPossibleFlips.ToArray();
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
    /// <summary>
    /// returns true if Seq ended. withSuccess is false when vehicle landing failed
    /// </summary>
    public bool StuntSeqEnded(out bool withSuccess)
    {
        withSuccess = true;

        if (stableLandingTimer == -1)
            return false;
        
        bool ended = stableLandingTimer > 1;
        if (ended)
            stableLandingTimer = -1;
        return ended;
    }
    void StuntDetector()
    {
        if (vp.groundedWheels == 0)
        {
            if (!prevGroundedWheels0)
            {
                foreach (RotationStunt stunt in stunts)
                {
                    stunt.positiveProgress = 0;
                    stunt.negativeProgress = 0;
                }
                stableLandingTimer = 0;
                w = vp.rb.velocity; 
                w.y = 0; 
                w = w.normalized;
                
                prevGroundedWheels0 = true;
            }
            Vector3 normA = vp.rb.angularVelocity.normalized;
            Vector3 lA = vp.transform.InverseTransformDirection(vp.rb.angularVelocity);
            Vector3 normlA = lA.normalized;
            //Debug.DrawRay(vp.transform.position, lA, Color.red, 10);
            Vector3 foundMoves = Vector3.zero;
            foreach (RotationStunt stunt in stunts)
            {
                stunt.w = w;
                //w_A_dot = Mathf.Abs(Vector3.Dot(w, normA));
                bool w_A_test = stunt.req_w_and_Angular_relation == VectorRelationship.None || 
                    RotationStunt.GetRelationship(w, normA) == stunt.req_w_and_Angular_relation;

                bool gY_A_test = stunt.req_globalY_and_Angular_relation == VectorRelationship.None ||
                    RotationStunt.GetRelationship(Vector3.up, normA) == stunt.req_globalY_and_Angular_relation;

                var relationship = RotationStunt.GetRelationship(normlA, stunt.rotationAxis);
                bool lA_car_test = relationship == VectorRelationship.None || relationship == VectorRelationship.Parallel;

                bool Y_ok = stunt.StuntingCarAlignmentConditionFulfilled(vp);

                if (Y_ok && w_A_test && gY_A_test && lA_car_test)
                {
                    if (foundMoves.x == 0 && stunt.rotationAxis.x != 0 && lA.x != 0)
                    {
                        //Debug2Mono.DrawText(vp.transform.position, lA.x.ToString(),5,Color.blue);
                        //Debug.DrawRay(vp.transform.position, normA, Color.red, 5);
                        //Debug.Log(lA.x);
                        stunt.AddProgress(lA.x*Time.fixedDeltaTime, vp);
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
                
                    if(stunt.allowHalfs && stunt.positiveProgress * Mathf.Rad2Deg >= stunt.angleThreshold/2f 
                        && stunt.negativeProgress * Mathf.Rad2Deg >= stunt.angleThreshold/2f
                        && stunt.CarAlignmentConditionFulfilled(vp))
                    { // done half rotation two-directions
                        stuntsAvailableForFrontend = true;
                        stunt.ResetProgress();
                        stunt.updateOverlay = true;

                        bool reverse = stunt.IsReverse(vp);
                        stunt.WriteHalfOverlayName(reverse, !evoModule.IsStunting());
                        pointsFromThisEvoSeq += (starLevel + 1) * 1.5f * stunt.score * (reverse ? 2 : 1) * (!evoModule.IsStunting() ? 2 : 1);
                    }
                    else if ((stunt.positiveProgress * Mathf.Rad2Deg >= stunt.angleThreshold 
                        || stunt.negativeProgress * Mathf.Rad2Deg >= stunt.angleThreshold )
                        && stunt.CarAlignmentConditionFulfilled(vp))
                    { // done full rotation
                        stuntsAvailableForFrontend = true;
                        stunt.doneTimes++;
                        stunt.updateOverlay = true;
                        bool reverse = stunt.IsReverse(vp);
                        stunt.WriteOverlayName(reverse, !evoModule.IsStunting());
                        stunt.ResetProgress();
                        pointsFromThisEvoSeq += (starLevel + 1) * stunt.score * (reverse ? 2 : 1) * (!evoModule.IsStunting() ? 2 : 1);
                    }
                }
            }
            prevGroundedWheels0 = true;
        }
        else
        {
            if (stableLandingTimer >= 0 && vp.groundedWheels == 4)
            {
                stableLandingTimer += Time.fixedDeltaTime;
            }
            if(prevGroundedWheels0)
            {
                endStuntSeqTime = Time.time;
                foreach (RotationStunt stunt in stunts)
                {
                    stunt.positiveProgress = 0;
                    stunt.negativeProgress = 0;
                }
            }
            prevGroundedWheels0 = false;
            if(Time.time - endStuntSeqTime > 5 + starLevel*0.5f)
            { // end combo

            }
            //// Add stunt points to the score
            //foreach (Flip curStunt in stunts)
            //{
            //    score += curStunt.progress * Mathf.Rad2Deg * curStunt.scoreRate * Mathf.FloorToInt((curStunt.progress * Mathf.Rad2Deg) / curStunt.angleThreshold) * curStunt.doneTimes;

            //    // Add boost to the engine
            //    if (engine)
            //    {
            //        engine.battery += curStunt.progress * Mathf.Rad2Deg * curStunt.boostAdd * curStunt.doneTimes * 0.01f;
            //    }
            //}

            //stunts.Clear();
            //doneStunts.Clear();
            //flipString = "";
        }

    }
    private void FixedUpdate()
    {
        StuntDetector();
    }
    private void Update()
    {
        if (Time.timeScale != 0)
        {
            aeroMeterVelocity = Mathf.Lerp(aeroMeterVelocity,
            (vp.groundedWheels == 0 && !vp.colliding) ? maxAeroMeterVelocity : minAeroMeterVelocity
            , Time.deltaTime * aeroMeterResponsiveness);
            //aeroMeterVelocity *= Mathf.Clamp01(vp.rb.velocity.magnitude / 46);
            aero += aeroMeterVelocity;
        }
    }
    public void AddAeroPts(int pts)
    {
        aero += pts;
    }
    public void SetGrantedComboTime(float seconds)
    {
        grantedComboTime = seconds;
        comboStartTime = DateTime.Now;
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
    public bool ComboActive()
    {
        bool active = (DateTime.Now - comboStartTime).TotalSeconds < grantedComboTime;
        if (!active)
            starLevel = 0;
        return active;
    }
    
}
