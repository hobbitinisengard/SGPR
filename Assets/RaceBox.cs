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
    public SGP_HUD hud;
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
    Stack<StuntRotInfo> rots;
    string stuntsInStone;
    
    void AddStuntSequence(StuntRotInfo rot)
    {//example of stuntRotInfo: X+270°
        if (rots.Count == 0)
        { // first stunt rotation
            rots.Push(rot);
        }
        else if (rot.axis == rots.Peek().axis && rot.rotation * rots.Peek().rotation >= 0)
        {// last rotation was on the same axis and in the same direction
            //stuntLastRot.rotation += rot.rotation;
        }
        else
        { // new stunting move rotation
            //stuntsInStone += stuntLastRot.ToString();
        }
        
        //stuntRots.text = stuntsInStone + stuntLastRot.ToString();
    }
    void SetPossibleStunt()
    {

    }
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
        rots = new Stack<StuntRotInfo>(32);
    }
    //public bool isStuntingThisStunt(in Vector3 w, in Vector3 lA, in Vector3 A, in Stunt stunt)
    //{
    //bool w_lA_test = Stunt.GetRelationship(w, A) == stunt.w_and_Angular;
    //bool gY_lA_test = Stunt.GetRelationship(Vector3.up, A) == stunt.globalY_and_Angular;
    //bool lA_car_test = Stunt.GetRelationship(lA, stunt.rotationAxis) == VectorRelationship.Parallel;
    //            return w_lA_test && gY_lA_test && lA_car_test;
        
    //}
    void DetectStunts()
    {
        if (vp.groundedWheels == 0)
        {
            Vector3 w = vp.rb.velocity;
            w.y = 0;
            w = w.normalized;
            Vector3 lA = vp.localAngularVel.normalized;
            Vector3 A = vp.rb.angularVelocity.normalized;
;            // Check to see if vehicle is performing a stunt and add it to the stunts list
            foreach (Stunt stuntExample in StuntManager.stuntsStatic)
            {
                bool w_lA_test = Stunt.GetRelationship(w, A) == stunt.w_and_Angular;
                bool gY_lA_test = Stunt.GetRelationship(Vector3.up, A) == stunt.globalY_and_Angular;
                bool lA_car_test = Stunt.GetRelationship(lA, stunt.rotationAxis) == VectorRelationship.Parallel;

                if (w_lA_test && gY_lA_test && lA_car_test)
                {
                    bool stuntExists = false;

                    foreach (Stunt checkStunt in stunts)
                    {
                        if (stuntExample.name == checkStunt.name)
                        {
                            stuntExists = true;
                            break;
                        }
                    }

                    if (!stuntExists)
                    {
                        stunts.Add(new Stunt(stuntExample));
                    }
                }
            }

            // Check the progress of stunts and compile the flip string listing all stunts
            foreach (Stunt curStunt2 in stunts)
            {
                if (Vector3.Dot(vp.localAngularVel.normalized, curStunt2.rotationAxis) >= curStunt2.precision)
                {
                    curStunt2.progress += rb.angularVelocity.magnitude * Time.fixedDeltaTime;
                }

                if (curStunt2.progress * Mathf.Rad2Deg >= curStunt2.angleThreshold)
                {
                    bool stuntDoneExists = false;

                    foreach (Stunt curDoneStunt in doneStunts)
                    {
                        if (curDoneStunt == curStunt2)
                        {
                            stuntDoneExists = true;
                            break;
                        }
                    }

                    if (!stuntDoneExists)
                    {
                        doneStunts.Add(curStunt2);
                    }
                }
            }

            string stuntCount = "";
            flipString = "";

            foreach (Stunt curDoneStunt2 in doneStunts)
            {
                stuntCount = curDoneStunt2.progress * Mathf.Rad2Deg >= curDoneStunt2.angleThreshold * 2 ?
                    " x" + Mathf.FloorToInt((curDoneStunt2.progress * Mathf.Rad2Deg) / curDoneStunt2.angleThreshold).ToString() : "";
                flipString = string.IsNullOrEmpty(flipString) ? curDoneStunt2.name + stuntCount : flipString + " + " + curDoneStunt2.name + stuntCount;
            }
        }
        else
        {
            // Add stunt points to the score
            foreach (Stunt curStunt in stunts)
            {
                score += curStunt.progress * Mathf.Rad2Deg * curStunt.scoreRate * Mathf.FloorToInt((curStunt.progress * Mathf.Rad2Deg) / curStunt.angleThreshold) * curStunt.multiplier;

                // Add boost to the engine
                if (engine)
                {
                    engine.battery += curStunt.progress * Mathf.Rad2Deg * curStunt.boostAdd * curStunt.multiplier * 0.01f;
                }
            }

            stunts.Clear();
            doneStunts.Clear();
            flipString = "";
        }
    }
    private void Update()
    {
        // Detect flips
        if (detectEvo && !vp.crashing)
        {
            DetectStunts();
        }
        else
        {
            stunts.Clear();
            flipString = "";
        }
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
