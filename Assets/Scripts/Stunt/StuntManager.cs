using UnityEngine;
using System.Collections;
using System;
using System.CodeDom.Compiler;

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

        public virtual string OverlayName()
        {
            return name;
        }
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
        public string halfOverlayName { get; private set; }
        public string overlayName { get; private set; }

        //public RotationStunt(RotationStunt oldStunt) { // copy ctor
        //    name = oldStunt.name;
        //    rotationAxis = oldStunt.rotationAxis;
        //    score = oldStunt.score;
        //    angleThreshold = oldStunt.angleThreshold;
        //    doneTimes = oldStunt.doneTimes;
        //}

        public static VectorRelationship GetRelationship(Vector3 norm_a, Vector3 norm_b)
        {
            if(Parallel(norm_a, norm_b)) 
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
        
        public bool CarAlignmentConditionFulfilled(in VehicleParent vp, in Vector3 w)
        {
            switch (endStuntReqParallelAlignment)
            {
                case EndStuntReqParallelAlignment.None:
                    return true;
                case EndStuntReqParallelAlignment.Forward_w:
                    return Parallel(vp.forwardDir, w);
                case EndStuntReqParallelAlignment.Up_gY:
                    return vp.upDot >= 0.9f; // same as: Parallel(vp.upDir, Vector3.up);
            }
            Debug.LogError("Shouldn't come here");
            return false;
        }
        public void WriteOverlayName(in Vector3 w, bool natural, in Vector3 normCarFacingDir)
        {
            overlayName = natural ? "NATURAL " : "";
            if (Vector3.Dot(w, normCarFacingDir) < -0.9f)
                overlayName += "REVERSE ";
            if (rotationAxis.x != 0)
            {
                if (positiveProgress > 0)
                    overlayName += "BACK ";
                else
                    overlayName += "FRONT ";
            }
            else if (rotationAxis.y != 0)
            {
                if (positiveProgress > 0)
                    overlayName += "LEFT ";
                else
                    overlayName += "RIGHT ";
            }
            else if (rotationAxis.z != 0)
            {
                if (positiveProgress > 0)
                    overlayName += "RIGHT ";
                else
                    overlayName += "LEFT ";
            }
            overlayName += name;
        }
        public void WriteHalfOverlayName(in Vector3 w, bool natural, in Vector3 normCarFacingDir)
        {
            if (Vector3.Dot(w, normCarFacingDir) < -0.9f)
                halfOverlayName += "REVERSE ";
            if (lastWriteWasPositive)
                halfOverlayName += halfFirstNegativeName; // first .x/.y/.z lA < 0
            else
                halfOverlayName += halfFirstPositiveName; // first .x/.y/.z lA > 0

            overlayName = null;// halfoverlay is being written
        }
        public override string OverlayName()
        {
            if (overlayName == null) // is null when halfoverlay was written
                return halfOverlayName;
            else
                return overlayName;
        }
        public override string PostfixText(int localDoneTimes)
        {
            if (overlayName == null)// is null when halfoverlay was written
                return " x" + localDoneTimes.ToString();
            else
                return localDoneTimes.ToString();
        }
        
        public void AddProgress(float degrees, bool isPositive)
        {
            lastWriteWasPositive = isPositive;
            if (isPositive)
                positiveProgress += degrees;
            else
                negativeProgress += degrees;
        }
    }
}