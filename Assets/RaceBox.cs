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
    private void OnDrawGizmos()
    {
        
    }
    void DetectStunts()
    {
        if (vp.groundedWheels == 0)
        {
            if (!prevGroundedWheels0)
            {
                w = vp.rb.velocity; 
                w.y = 0; 
                w = w.normalized;
                Debug.DrawRay(vp.transform.position, w, Color.red, 10);
                prevGroundedWheels0 = true;
            }
            Vector3 A = vp.rb.angularVelocity;
            Vector3 normA = A.normalized;
            Vector3 normlA = vp.transform.InverseTransformDirection(A).normalized;
            //Debug.DrawRay(vp.transform.position, normA, Color.blue, 5);
            //Debug2Mono.DrawText(vp.transform.position, Mathf.Abs(Vector3.Dot(Vector3.forward, normlA)).ToString(),5,Color.blue);
            foreach (RotationStunt stunt in stunts)
            {
                //w_A_dot = Mathf.Abs(Vector3.Dot(w, normA));
                bool w_A_test = RotationStunt.GetRelationship(w, normA) == stunt.req_w_and_Angular_relation;
                bool gY_A_test = RotationStunt.GetRelationship(Vector3.up, normA) == stunt.req_globalY_and_Angular_relation;
                bool lA_car_test = RotationStunt.GetRelationship(normlA, stunt.rotationAxis) == VectorRelationship.Parallel;

                if (w_A_test && gY_A_test && lA_car_test)
                {
                    if (stunt.rotationAxis.x != 0)
                    {
                        Debug.Log("progressing");
                        stunt.AddProgress(A.magnitude * Time.fixedDeltaTime, A.x > 0);
                    }
                    else if (stunt.rotationAxis.y != 0)
                    {
                        stunt.AddProgress(A.magnitude * Time.fixedDeltaTime, A.y > 0);
                    }
                    else if (stunt.rotationAxis.z != 0)
                    {
                        stunt.AddProgress(A.magnitude * Time.fixedDeltaTime, A.z > 0);
                    }

                    if(stunt.positiveProgress * Mathf.Rad2Deg >= 135 && stunt.negativeProgress * Mathf.Rad2Deg >= 135 
                        && stunt.CarAlignmentConditionFulfilled(vp, w))
                    { // done half rotation two-directions
                        stuntsAvailableForFrontend = true;
                        stunt.positiveProgress = 0;
                        stunt.negativeProgress = 0;
                        stunt.updateOverlay = true;
                        stunt.WriteHalfOverlayName(w, !evoModule.IsStunting(), vp.forwardDir);
                        pointsFromThisEvoSeq += (starLevel + 1) * 1.5f * stunt.score * (stunt.overlayName.Contains("REV") ? 2 : 1)
                            * (!evoModule.IsStunting() ? 2 : 1);
                    }
                    else if (stunt.positiveProgress * Mathf.Rad2Deg >= stunt.angleThreshold && stunt.CarAlignmentConditionFulfilled(vp, w))
                    { // done full rotation
                        stuntsAvailableForFrontend = true;
                        stunt.doneTimes++;
                        stunt.updateOverlay = true;
                        stunt.WriteOverlayName(w, !evoModule.IsStunting(), vp.forwardDir);
                        stunt.positiveProgress = 0;
                        stunt.negativeProgress = 0;
                        pointsFromThisEvoSeq += (starLevel + 1) * stunt.score * (stunt.overlayName.Contains("REV") ? 2 : 1)
                            * (!evoModule.IsStunting() ? 2 : 1);
                    }
                }
            }
            prevGroundedWheels0 = true;
        }
        else
        {
            prevGroundedWheels0 = false;
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
    private void Update()
    {

        DetectStunts();
        // Detect flips
        //if (detectEvo && !vp.crashing)
        //{
        //    DetectStunts();
        //}
        //else
        //{
        //    stunts.Clear();
        //    flipString = "";
        //}
        // ... i wysy³a informacjê do SGP_HUD
        // ... przez AddStunt() ? 


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
