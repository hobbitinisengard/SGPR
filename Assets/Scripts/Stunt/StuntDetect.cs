using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace RVP
{
    [RequireComponent(typeof(VehicleParent))]
    [DisallowMultipleComponent]
    [AddComponentMenu("RVP/Stunt/Stunt Detector", 1)]

    // Class for detecting stunts
    public class StuntDetect : MonoBehaviour
    {
        Transform tr;
        Rigidbody rb;
        VehicleParent vp;

        [System.NonSerialized]
        public float score;
        List<RotationStunt> stunts = new List<RotationStunt>();
        List<RotationStunt> doneStunts = new List<RotationStunt>();
        bool drifting;
        float driftDist;
        float driftScore;
        float endDriftTime; // Time during which drifting counts even if the vehicle is not actually drifting
        float jumpDist;
        float jumpTime;
        Vector3 jumpStart;

        public bool detectDrift = true;
        public bool detectJump = true;
        public bool detectFlips = true;

        string driftString; // String indicating drift distance
        string jumpString; // String indicating jump distance
        string flipString; // String indicating flips
        [System.NonSerialized]
        public string stuntString; // String containing all stunts

        public Motor engine;

        public float lastGrindTime = 0;
        public float grindTimer = 0;
        void Start() {
            tr = transform;
            rb = GetComponent<Rigidbody>();
            vp = GetComponent<VehicleParent>();
        }

        private void OnTriggerEnter(Collider other)
        {
            if(vp.groundedWheels == 0)
            {
                lastGrindTime = Time.time;
                grindTimer += Time.fixedDeltaTime;
            }
        }
        void FixedUpdate() {
            // Detect grinds
            if(grindTimer > 0 && Time.time - lastGrindTime > 1)
            { // grind ended
                grindTimer = 0;
            }

            // Detect drifts
            if (detectDrift && !vp.crashing) {
                DetectDrift();
            }
            else {
                drifting = false;
                driftDist = 0;
                driftScore = 0;
                driftString = "";
            }

            // Detect jumps
            //if (detectJump && !vp.crashing) {
            //    DetectJump();
            //}
            //else {
            //    jumpTime = 0;
            //    jumpDist = 0;
            //    jumpString = "";
            //}

            

            // Combine strings into final stunt string
            stuntString = vp.crashing ? "Crashed" : driftString + jumpString + (string.IsNullOrEmpty(flipString) || string.IsNullOrEmpty(jumpString) ? "" : " + ") + flipString;
        }

        // Logic for detecting and tracking drift
        void DetectDrift() {
            endDriftTime = vp.groundedWheels > 0 ? (Mathf.Abs(vp.localVelocity.x) > 5 ? StuntManager.driftConnectDelayStatic : Mathf.Max(0, endDriftTime - Time.timeScale * TimeMaster.inverseFixedTimeFactor)) : 0;
            drifting = endDriftTime > 0;

            if (drifting) {
                driftScore += (StuntManager.driftScoreRateStatic * Mathf.Abs(vp.localVelocity.x)) * Time.timeScale * TimeMaster.inverseFixedTimeFactor;
                driftDist += vp.velMag * Time.fixedDeltaTime;
                driftString = "Drift: " + driftDist.ToString("n0") + " m";

                if (engine) {
                    vp.battery += (StuntManager.driftBoostAddStatic * Mathf.Abs(vp.localVelocity.x)) * Time.timeScale * 0.0002f * TimeMaster.inverseFixedTimeFactor;
                }
            }
            else {
                score += driftScore;
                driftDist = 0;
                driftScore = 0;
                driftString = "";
            }
        }

        // Logic for detecting and tracking jumps
        //void DetectJump() {
        //    if (vp.groundedWheels == 0) {
        //        jumpDist = Vector3.Distance(jumpStart, tr.position);
        //        jumpTime += Time.fixedDeltaTime;
        //        jumpString = "Jump: " + jumpDist.ToString("n0") + " m";

        //        if (engine) {
        //            vp.battery += StuntManager.jumpBoostAddStatic * Time.timeScale * 0.01f * TimeMaster.inverseFixedTimeFactor;
        //        }
        //    }
        //    else {
        //        score += (jumpDist + jumpTime) * StuntManager.jumpScoreRateStatic;

        //        if (engine) {
        //            vp.battery += (jumpDist + jumpTime) * StuntManager.jumpBoostAddStatic * Time.timeScale * 0.01f * TimeMaster.inverseFixedTimeFactor;
        //        }

        //        jumpStart = tr.position;
        //        jumpDist = 0;
        //        jumpTime = 0;
        //        jumpString = "";
        //    }
        //}

        // Logic for detecting and tracking flips
        
    }
}