using RVP;
using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SocialPlatforms.Impl;

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
    SGP_HUD hud;
    VehicleParent vp;
    SGP_Evo evoModule;
    public float distance { get; private set; }
    public float aero { get; private set; }
    public DateTime lapStartTime;
    public TimeSpan bestLapTime { get; private set; }

    /// <summary>
    /// number of laps done already
    /// </summary>
    public int curNoLap { get; private set; }
    /// <summary>
    /// All number of laps of this race
    /// </summary>
    public int NoLaps { get; private set; }

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
    [NonSerialized]
    public List<Stunt> stunts;
    
    void Start()
    {
        vp = transform.GetComponent<VehicleParent>();
        evoModule = transform.GetComponent<SGP_Evo>();
        lapStartTime = DateTime.MinValue;
        bestLapTime = TimeSpan.MaxValue;
        curNoLap = 0;
        NoLaps = 18;
        starLevel = 0;
        comboStartTime = DateTime.MinValue;
        //rots = new Stack<StuntRotInfo>(32);
        stunts = new List<Stunt>();
        var globalcontrol = GameObject.Find("GlobalControl");
        stunts.AddRange(globalcontrol.transform.GetComponent<StuntManager>().allPossibleFlips);
        hud = globalcontrol.transform.GetComponent<SGP_HUD>();
    }
    void DetectStunts()
    {
        //if (vp.groundedWheels == 0)
        //{
        //    if (!prevGroundedWheels0)
        //    {
        //        w = vp.rb.velocity; w.y = 0; w = w.normalized;
        //        prevGroundedWheels0 = true;
        //    }
        //    Vector3 lA = vp.localAngularVel.normalized;
        //    Vector3 A = vp.rb.angularVelocity.normalized;
        //    foreach (Flip stunt in stunts)
        //    {
        //        bool w_A_test = Flip.GetRelationship(w, A) == stunt.req_w_and_Angular_relation;
        //        bool gY_A_test = Flip.GetRelationship(Vector3.up, A) == stunt.req_globalY_and_Angular_relation;
        //        bool lA_car_test = Flip.GetRelationship(lA, stunt.rotationAxis) == VectorRelationship.Parallel;

        //        if (w_A_test && gY_A_test && lA_car_test)
        //        {
        //            stunt.progress += lA.magnitude * Time.fixedDeltaTime;

        //            if (stunt.progress * Mathf.Rad2Deg >= stunt.angleThreshold && stunt.CarAlignmentConditionFulfilled(vp, w))
        //            {
        //                stunt.doneTimes++;
        //                stunt.updateOverlay = true;
        //                stunt.progress = 0;
        //            }
        //        }
        //    }
            //hud.UpdateStuntList(stunts);
        //}
        //else
        //{
        //    prevGroundedWheels0 = false;
        //    // Add stunt points to the score
        //    foreach (Flip curStunt in stunts)
        //    {
        //        score += curStunt.progress * Mathf.Rad2Deg * curStunt.scoreRate * Mathf.FloorToInt((curStunt.progress * Mathf.Rad2Deg) / curStunt.angleThreshold) * curStunt.doneTimes;

        //        // Add boost to the engine
        //        if (engine)
        //        {
        //            engine.battery += curStunt.progress * Mathf.Rad2Deg * curStunt.boostAdd * curStunt.doneTimes * 0.01f;
        //        }
        //    }

        //    stunts.Clear();
        //    doneStunts.Clear();
        //    flipString = "";
        //}

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
        curNoLap++;
    }
    public bool ComboActive()
    {
        bool active = (DateTime.Now - comboStartTime).TotalSeconds < grantedComboTime;
        if (!active)
            starLevel = 0;
        return active;
    }
    
}
