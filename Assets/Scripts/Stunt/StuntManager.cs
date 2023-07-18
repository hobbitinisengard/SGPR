﻿using System;
using UnityEngine;

namespace RVP
{
    [DisallowMultipleComponent]
    [AddComponentMenu("RVP/Stunt/Stunt Manager", 0)]

    // Class for managing stunts
    public class StuntManager : MonoBehaviour
    {      
        public static float stuntPrecisionStatic;
        public float driftScoreRate;
        public static float driftScoreRateStatic;

        [Tooltip("Maximum time gap between connected drifts")]
        public float driftConnectDelay;
        public static float driftConnectDelayStatic;

        public float driftBoostAdd;
        public static float driftBoostAddStatic;

        public float jumpScoreRate;
        public static float jumpScoreRateStatic;

        public float jumpBoostAdd;
        public static float jumpBoostAddStatic;

        public RotationStunt[] allPossibleFlips;

        void Start() {
            // Set static variables
            driftScoreRateStatic = driftScoreRate;
            driftConnectDelayStatic = driftConnectDelay;
            driftBoostAddStatic = driftBoostAdd;
            jumpScoreRateStatic = jumpScoreRate;
            jumpBoostAddStatic = jumpBoostAdd;
        }
    }
    public enum VectorRelationship { Perpendicular, Parallel, None};
    public enum CarAlignment { None, Up_positive, Up_negative};
    enum EndStuntReqParallelAlignment { Forward_w, Up_gY, None };

    public class Stunt
    {
        //public static readonly string[] doneTimesPrefixes = { "", "Double ", "Triple ", "Super ", "Master " };
        public string name;
        public float score;
        [System.NonSerialized]
        public float doneTimes = 0;
        [System.NonSerialized]
        public bool updateOverlay = false;
        [System.NonSerialized]
        public float positiveProgress;
        public string overlayName { get; protected set; }
        public static bool Parallel(Vector3 norm_a, Vector3 norm_b)
        {
            return Mathf.Abs(Vector3.Dot(norm_a, norm_b)) >= 0.9f;// angle is 0 deg +- 25deg
        }
        public static bool Perpendicular(Vector3 norm_a, Vector3 norm_b)
        {
            return Mathf.Abs(Vector3.Dot(norm_a, norm_b)) < 0.4f; // angle is 90 deg +- 23deg
        }
        public virtual string PostfixText(int localDoneTimes)
        {
            return localDoneTimes.ToString();
        }
    }

    [System.Serializable]
    public class RotationStunt : Stunt
    {
        public bool allowHalfs = false;
        public CarAlignment req_carAlignment = CarAlignment.None;
        public VectorRelationship req_w_and_Angular_relation;
        public VectorRelationship req_globalY_and_Angular_relation;
        [Tooltip(" Local rotation axis of the stunting car. Can be: Vector3.up, front, right")]
        public Vector3 rotationAxis;
        [SerializeField]
        private EndStuntReqParallelAlignment endStuntReqParallelAlignment = EndStuntReqParallelAlignment.None;
        public float angleThreshold;
        [System.NonSerialized]
        public float negativeProgress;
        public string halfFirstPositiveName;
        public string halfFirstNegativeName;
        bool lastWriteWasPositive;
        bool isHalfRotation;
        public bool canBeReverse { get; private set; }
        [NonSerialized]
        public Vector3 w;
        //public Vector3 w;

        //public RotationStunt(RotationStunt oldStunt) { // copy ctor
        //    name = oldStunt.name;
        //    rotationAxis = oldStunt.rotationAxis;
        //    score = oldStunt.score;
        //    angleThreshold = oldStunt.angleThreshold;
        //    doneTimes = oldStunt.doneTimes;
        //}
        public bool StuntingCarAlignmentConditionFulfilled(in VehicleParent vp)
        {
            switch (req_carAlignment)
            {
                case CarAlignment.None:
                    return true;
                case CarAlignment.Up_positive:
                    return vp.upDir.y > 0;
                case CarAlignment.Up_negative:
                    return vp.upDir.y < 0;
            }
            Debug.LogError("Shouldn't come here");
            return true;
        }
        public static VectorRelationship GetRelationship(Vector3 norm_a, Vector3 norm_b)
        {
            if (Parallel(norm_a, norm_b))
            {
                return VectorRelationship.Parallel;
            }
            if (Perpendicular(norm_a, norm_b))
            {
                return VectorRelationship.Perpendicular;
            }
            else
                return VectorRelationship.None;
        }

        public bool CarAlignmentConditionFulfilled(in VehicleParent vp)
        {
            switch (endStuntReqParallelAlignment)
            {
                case EndStuntReqParallelAlignment.None:
                    return true;
                case EndStuntReqParallelAlignment.Forward_w:
                    return Parallel(vp.forwardDir, vp.rb.velocity);
                case EndStuntReqParallelAlignment.Up_gY:
                    return vp.upDot >= 0.9f; // same as: Parallel(vp.upDir, Vector3.up);
            }
            Debug.LogError("Shouldn't come here");
            return false;
        }
        public void WriteHalfOverlayName(bool reverse, bool natural)
        {
            isHalfRotation = true;
            overlayName = (reverse ? "REVERSE " : "");
            if (lastWriteWasPositive)
                overlayName += halfFirstNegativeName; // first .x/.y/.z lA < 0
            else
                overlayName += halfFirstPositiveName; // first .x/.y/.z lA > 0
        }
        public void WriteOverlayName(bool reverse, bool natural)
        {
            isHalfRotation = false;
            overlayName = (natural ? "NATURAL " : "");
            if (reverse)
                overlayName += "REVERSE ";
            if (rotationAxis.x != 0)
            {
                if (lastWriteWasPositive)
                    overlayName += "FRONT ";
                else
                    overlayName += "BACK ";
            }
            else if (rotationAxis.y != 0)
            {
                if (lastWriteWasPositive)
                    overlayName += "RIGHT ";
                else
                    overlayName += "LEFT ";
            }
            else if (rotationAxis.z != 0)
            {
                if (lastWriteWasPositive)
                    overlayName += "LEFT ";
                else
                    overlayName += "RIGHT ";
            }
            overlayName += name;
        }
        

        public override string PostfixText(int localDoneTimes)
        {
            if (isHalfRotation)// is null when halfoverlay was written
                return " x" + localDoneTimes.ToString();
            else
                return (360 * localDoneTimes).ToString();
        }
        public void ResetProgress()
        {
            positiveProgress = 0;
            negativeProgress = 0;
            canBeReverse = false;
        }
        public bool IsReverse(in VehicleParent vp)
        {
            float dot = Vector2.Dot(Flat(w), Flat(vp.forwardDir));
            Debug.Log(dot);
            return /*canBeReverse &&*/ dot < -0.9f;
        }
        public void AddProgress(float radians, in VehicleParent vp)
        {
            if (!canBeReverse && (positiveProgress <= 2 || negativeProgress <= 2))
            {
                canBeReverse = Vector2.Dot(Flat(w/*vp.rb.velocity.normalized*/), Flat(vp.forwardDir)) < -0.9f;
                //Debug.Log(Vector3.Dot(vp.rb.velocity.normalized, vp.forwardDir));
            }
            lastWriteWasPositive = radians > 0;
            //Debug.Log(lastWriteWasPositive);
            if (lastWriteWasPositive)
                positiveProgress += radians;
            else
                negativeProgress -= radians;
        }

        public void PrintProgress()
        {
            Debug.Log("+++"+positiveProgress.ToString() + " ---" + negativeProgress.ToString());
        }
        Vector2 Flat(Vector3 v)
        {
            return new Vector2(v.x, v.z);
        }
    }
}