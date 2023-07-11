using UnityEngine;
using System;
using UnityEngine.UIElements.Experimental;

namespace RVP
{
    [DisallowMultipleComponent]
    [AddComponentMenu("RVP/Vehicle Controllers/Steering Control", 2)]

    // Class for steering vehicles
    public class SteeringControl : MonoBehaviour
    {
        Transform tr;
        VehicleParent vp;
        private float frontSidewaysCoeff;

        // unused
        public bool limitSteer = true;
        [Tooltip("First wheel should be FL")]
        public Suspension[] steeredWheels;

        [Tooltip("Shape of curve need to be setup in inspector. It's , x-axis = speed, y-axis = multiplier")]
        public AnimationCurve steerLimitCurve = AnimationCurve.Linear(0,1,30,0.1f);
        static readonly float speedOfMaxComebackSteeringSpeed = 30;
        AnimationCurve comebackSteerLimitCurve = AnimationCurve.Linear(0, 0, speedOfMaxComebackSteeringSpeed, 1f);
        [Header("Experimental")]
        public float steerLimitCurveCoeff = 1f;
        [Range(-1,1)]
        public float steerAngle = 0;
        float holdDuration = 0;
        [Range(0, 1)]
        public float steerLimit;
        public float steerStepScale = 0.5f;
        //[Tooltip("Less is more responsive. Must be positive")]
        //public float steeringResponsiveness = 8;
        //[Tooltip("Initial steering angle available instantly")]
        //public float steeringOffset = 0.05f;
        [Header("Visual")]

        public bool rotate;
        //float steerRot;

        public float maxDegreesRotation;
        AnimationCurve keyboardInputCurve;
        float secsForMaxSteeringSpeed = 1.5f;

        public float d_angleSteer;

        AnimationCurve Generate_digitalSteeringInputCurve()
        { 
            double[] digitalSteeringInputEnv = { 0.175000, 0.189238, 0.203475, 0.217713, 0.231950, 0.246188, 0.260426, 0.274663, 0.288901, 0.293119, 0.297337, 0.301555, 0.305773, 0.309991, 0.314209, 0.318426, 0.322644, 0.327860, 0.333075, 0.338291, 0.343506, 0.348722, 0.353937, 0.359153, 0.364368, 0.367577, 0.370786, 0.373995, 0.377204, 0.380413, 0.383622, 0.386831, 0.390039, 0.390845, 0.391651, 0.392456, 0.393262, 0.394068, 0.394873, 0.395679, 0.396485, 0.400782, 0.405079, 0.409375, 0.413672, 0.414673, 0.415674, 0.416675, 0.417676, 0.418677, 0.419678, 0.420679, 0.421680, 0.426539, 0.431397, 0.436255, 0.441114, 0.445972, 0.450831, 0.455689, 0.460547, 0.466211, 0.471875, 0.477540, 0.483204, 0.488868, 0.494532, 0.500196, 0.505860, 0.513184, 0.520508, 0.527832, 0.535157, 0.542481, 0.549805, 0.557129, 0.564453, 0.575196, 0.585938, 0.596680, 0.607422, 0.618164, 0.628907, 0.639649, 0.650391, 0.661280, 0.672168, 0.683057, 0.693946, 0.704834, 0.715723, 0.726612, 0.737500, 0.752344, 0.767188, 0.782031, 0.796875, 0.811719, 0.826563, 0.841406, 0.856250, 0.867969, 0.879688, 0.891406, 0.903125, 0.914844, 0.926563, 0.938281, 0.950000, 0.954688, 0.959375, 0.964063, 0.968750, 0.973438, 0.978125, 0.982813, 0.987500, 0.990625, 0.993750, 0.996875, 1.000000, 1.000000, 1.000000, 1.000000, 1.000000, 1.000000, 1.000000, 1.000000 };
            Keyframe[] keys = new Keyframe[digitalSteeringInputEnv.Length];
            for (int i = 0; i < keys.Length; i++)
            {   // i + 1 ??? TODO
                keys[i].time = secsForMaxSteeringSpeed * (i) / keys.Length;
                keys[i].value = (float)digitalSteeringInputEnv[i];
            }
            return new AnimationCurve(keys);
        }
        AnimationCurve Generate_inputVelocityDataCurve()
        {
            //double[] dydx = { 22.225, 1.808226, 1.808099, 1.808226, 1.808099, 1.808226, 1.808226, 1.808099, 1.808226, 0.535686, 0.535686, 0.535686, 0.535686, 0.535686,0.535686, 0.535559, 0.535686, 0.662432, 0.662305, 0.662432, 0.662305,0.662432, 0.662305, 0.662432, 0.662305, 0.407543, 0.407543, 0.407543,0.407543, 0.407543, 0.407543, 0.407543, 0.407416, 0.102362, 0.102362,0.102235, 0.102362, 0.102362, 0.102235, 0.102362, 0.102362, 0.545719,0.545719, 0.545592, 0.545719, 0.127127, 0.127127, 0.127127, 0.127127,0.127127, 0.127127, 0.127127, 0.127127, 0.617093, 0.616966, 0.616966,0.617093, 0.616966, 0.617093, 0.616966, 0.616966, 0.719328, 0.719328,0.719455, 0.719328, 0.719328, 0.719328, 0.719328, 0.719328, 0.930148,0.930148, 0.930148, 0.930275, 0.930148, 0.930148, 0.930148, 0.930148,1.364361, 1.364234, 1.364234, 1.364234, 1.364234, 1.364361, 1.364234,1.364234, 1.382903, 1.382776, 1.382903, 1.382903, 1.382776, 1.382903,1.382903, 1.382776, 1.885188, 1.885188, 1.885061, 1.885188, 1.885188,1.885188, 1.885061, 1.885188, 1.488313, 1.488313, 1.488186, 1.488313,1.488313, 1.488313, 1.488186, 1.488313, 0.595376, 0.595249, 0.595376,0.595249, 0.595376, 0.595249, 0.595376, 0.595249, 0.396875, 0.396875,0.396875, 0.396875, 0 , 0 , 0 , 0, 0,0};
            //double[] dydx = { 22.225, 1.808226, 1.808099, 1.808226, 1.808099, 1.808226, 1.808226, 1.808099, 1.808226, 0.535686, 0.535686, 0.535686, 0.535686, 0.535686, 0.535686, 0.535559, 0.535686, 0.662432, 0.662305, 0.662432, 0.662305, 0.662432, 0.662305, 0.662432, 0.662305, 0.407543, 0.407543, 0.407543, 0.407543, 0.407543, 0.407543, 0.407543, 0.407416, 0.102362, 0.102362, 0.102235, 0.102362, 0.102362, 0.102235, 0.102362, 0.102362, 0.545719, 0.545719, 0.545592, 0.545719, 0.127127, 0.127127, 0.127127, 0.127127, 0.127127, 0.127127, 0.127127, 0.127127, 0.617093, 0.616966, 0.616966, 0.617093, 0.616966, 0.617093, 0.616966, 0.616966, 0.719328, 0.719328, 0.719455, 0.719328, 0.719328, 0.719328, 0.719328, 0.719328, 0.930148, 0.930148, 0.930148, 0.930275, 0.930148, 0.930148, 0.930148, 0.930148, 1.364361, 1.364234, 1.364234, 1.364234, 1.364234, 1.364361, 1.364234, 1.364234, 1.382903, 1.382776, 1.382903, 1.382903, 1.382776, 1.382903, 1.382903, 1.382776, 1.885188, 1.885188, 1.885061, 1.885188, 1.885188, 1.885188, 1.885061, 1.885188, 1.488313, 1.488313, 1.488186, 1.488313, 1.488313, 1.488313, 1.488186, 1.488313, 0.595376, 0.595249, 0.595376, 0.595249, 0.595376, 0.595249, 0.595376, 0.595249, 0.396875, 0.396875, 0.396875, 0.396875, 0.396875, 5,5,5,5,5 };
            double[] dydx = { 1.8, 1.808226, 1.808099, 1.808226, 1.808099, 1.808226, 1.808226, 1.808099, 1.808226, 0.535686, 0.535686, 0.535686, 0.535686, 0.535686, 0.535686, 0.535559, 0.535686, 0.662432, 0.662305, 0.662432, 0.662305, 0.662432, 0.662305, 0.662432, 0.662305, 0.407543, 0.407543, 0.407543, 0.407543, 0.407543, 0.407543, 0.407543, 0.407416, 0.102362, 0.102362, 0.102235, 0.102362, 0.102362, 0.102235, 0.102362, 0.102362, 0.545719, 0.545719, 0.545592, 0.545719, 0.127127, 0.127127, 0.127127, 0.127127, 0.127127, 0.127127, 0.127127, 0.127127, 0.617093, 0.616966, 0.616966, 0.617093, 0.616966, 0.617093, 0.616966, 0.616966, 0.719328, 0.719328, 0.719455, 0.719328, 0.719328, 0.719328, 0.719328, 0.719328, 0.930148, 0.930148, 0.930148, 0.930275, 0.930148, 0.930148, 0.930148, 0.930148, 1.364361, 1.364234, 1.364234, 1.364234, 1.364234, 1.364361, 1.364234, 1.364234, 1.382903, 1.382776, 1.382903, 1.382903, 1.382776, 1.382903, 1.382903, 1.382776, 1.885188, 1.885188, 1.885061, 1.885188, 1.885188, 1.885188, 1.885061, 1.885188, 1.488313, 1.488313, 1.488186, 1.488313, 1.488313, 1.488313, 1.488186, 1.488313, 1.488313, 1.488313, 1.488313, 1.488313, 1.488313, 1.488313, 1.488313, 1.488313, 1.488313, 1.488313, 1.488313, 1.488313, 1.488313, 1.488313, 1.488313, 1.488313, 1.488313, 1.488313, 1.488313, 1.488313 };
            Keyframe[] keys = new Keyframe[dydx.Length];
            for (int i = 0; i < keys.Length; i++)
            {   // i + 1 ??? TODO
                keys[i].time = secsForMaxSteeringSpeed * i / keys.Length;
                keys[i].value = (float)dydx[i];
            }
            return new AnimationCurve(keys);
        }
        void Start()
        {

            keyboardInputCurve = Generate_digitalSteeringInputCurve();
            tr = transform;
            vp = tr.GetTopmostParentComponent<VehicleParent>();
            frontSidewaysCoeff = vp.wheels[0].sidewaysFriction;
        }
        void FixedUpdate()
        {
            float rawSteer = 0;
            if (vp.steerInput == 0)
            {
                if (holdDuration > 0)
                    holdDuration -= 5 * Time.fixedDeltaTime;
            }
            else
            {
                if (holdDuration < secsForMaxSteeringSpeed)
                {
                    //holdDurationRaw = Mathf.Clamp()
                    if (holdDuration < 0)
                        holdDuration = Time.fixedDeltaTime;
                    else
                        holdDuration += Time.fixedDeltaTime;
                }
                
                rawSteer = keyboardInputCurve.Evaluate(holdDuration);
            }
            steerLimit = steerLimitCurve.Evaluate(vp.localVelocity.z);
            // Set steer angles in wheels
            foreach (Suspension curSus in steeredWheels)
            {

                if (Mathf.Abs(curSus.steerAngle) < 0.001f && vp.steerInput == 0)
                { // important for high speed straight drive
                    curSus.steerAngle = 0;
                    continue;
                }

                if(vp.SGPshiftbutton)
                {
                    steerLimit *= 1.5f;
                    curSus.wheel.sidewaysFriction = 1.5f * frontSidewaysCoeff;
                }
                else
                {
                    curSus.wheel.sidewaysFriction = frontSidewaysCoeff;
                }
                //if (steerAngle * vp.steerInput < 0)
                //    curSus.steerAngle /= 1.1f; // for fast direction change 

                float targetSteerAngle;
                if (curSus.wheel.sliding)
                    targetSteerAngle = vp.steerInput * curSus.steerAngle;
                else
                    targetSteerAngle = vp.steerInput * steerLimit * rawSteer;

                if (Mathf.Abs(targetSteerAngle) > steerLimit)
                    targetSteerAngle = Mathf.Sign(targetSteerAngle) * steerLimit;

                float step;
                if (vp.steerInput == 0)
                    step = 12 * Time.fixedDeltaTime * comebackSteerLimitCurve.Evaluate(Mathf.Abs(Mathf.Clamp(vp.localVelocity.z, -speedOfMaxComebackSteeringSpeed, speedOfMaxComebackSteeringSpeed)));
                else
                    step = 10 * Time.fixedDeltaTime; //* Easing.InCubic(holdDuration/1f);
                curSus.steerAngle = Mathf.Lerp(curSus.steerAngle, targetSteerAngle, step);
            }
            steerAngle = steeredWheels[0].steerAngle;
        }
    }
}
