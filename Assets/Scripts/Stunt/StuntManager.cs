using UnityEngine;
using System.Collections;

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

        public Flip[] allPossibleFlips;

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
        public float progress;
    }

    [System.Serializable]
    public class Flip : Stunt
    {
        public VectorRelationship req_w_and_Angular_relation;
        public VectorRelationship req_globalY_and_Angular_relation;
        public Vector3 rotationAxis; // Local rotation axis of the stunt
        [SerializeField]
        private EndStuntReqParallelAlignment endStuntReqParallelAlignment = EndStuntReqParallelAlignment.None;
        public float angleThreshold;

        public Flip(Flip oldStunt) { // copy ctor
            name = oldStunt.name;
            rotationAxis = oldStunt.rotationAxis;
            score = oldStunt.score;
            angleThreshold = oldStunt.angleThreshold;
            doneTimes = oldStunt.doneTimes;
        }

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
        public static bool Parallel(Vector3 norm_a, Vector3 norm_b)
        {
            return Vector3.Dot(norm_a, norm_b) >= 0.9f;// angle is 0 deg +- 25deg
        }
        public static bool Perpendicular(Vector3 norm_a, Vector3 norm_b)
        {
            return Vector3.Dot(norm_a, norm_b) < 0.4f; // angle is 90 deg +- 23deg
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
    }
}