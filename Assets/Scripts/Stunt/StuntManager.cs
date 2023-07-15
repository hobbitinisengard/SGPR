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

        public Stunt[] stunts;
        public static Stunt[] stuntsStatic;

        void Start() {
            // Set static variables
            driftScoreRateStatic = driftScoreRate;
            driftConnectDelayStatic = driftConnectDelay;
            driftBoostAddStatic = driftBoostAdd;
            jumpScoreRateStatic = jumpScoreRate;
            jumpBoostAddStatic = jumpBoostAdd;
            stuntsStatic = stunts;
        }
    }
    public enum VectorRelationship { Perpendicular, Parallel, None};
    // Stunt class
    [System.Serializable]
    public class Stunt
    {
        public string name;
        public VectorRelationship w_and_Angular;
        public VectorRelationship globalY_and_Angular;
        public Vector3 rotationAxis; // Local rotation axis of the stunt
        public float scoreRate;
        public float multiplier = 1; // Multiplier for when the stunt is performed more than once in the same jump
        public float angleThreshold;
        [System.NonSerialized]
        public float progress; // How much rotation has happened during the stunt in radians?

        // Use this to duplicate a stunt
        public Stunt(Stunt oldStunt) {
            name = oldStunt.name;
            rotationAxis = oldStunt.rotationAxis;
            scoreRate = oldStunt.scoreRate;
            angleThreshold = oldStunt.angleThreshold;
            multiplier = oldStunt.multiplier;
        }

        public static VectorRelationship GetRelationship(Vector3 norm_a, Vector3 norm_b)
        {
            if(Vector3.Dot(norm_a, norm_b) >= 0.9f) // angle is 0 deg +- 25deg
            {
                return VectorRelationship.Parallel;
            }
            if (Vector3.Dot(norm_a, norm_b) < 0.4f) // angle is 90 deg +- 23deg
            {
                return VectorRelationship.Perpendicular;
            }
            else
                return VectorRelationship.None;
        }
    }
}