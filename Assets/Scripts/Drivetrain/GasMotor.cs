using UnityEngine;
using System.Collections;

namespace RVP
{
    [RequireComponent(typeof(DriveForce))]
    [DisallowMultipleComponent]
    [AddComponentMenu("RVP/Drivetrain/Gas Motor", 0)]

    // Motor subclass for internal combustion engines
    public class GasMotor : Motor
    {
        public struct GearChangeData
        {
            public float TimeForDownshift;
            public float TimeForUpshift;
            public float BeginSequenceTime;
        }
        GearChangeData gearchangedata;
        readonly static double[][] torqueCurveData =
        {
            new double[]{ 0.275000, 0.287999, 0.300998, 0.313997, 0.326995, 0.325296, 0.323596, 0.321897, 0.320198, 0.318499, 0.316799, 0.315100, 0.313401, 0.316715, 0.320029, 0.323343, 0.326656, 0.329970, 0.333284, 0.336598, 0.339912, 0.339618, 0.339324, 0.339030, 0.338735, 0.338441, 0.338147, 0.337853, 0.337559, 0.336923, 0.336287, 0.335651, 0.335015, 0.334379, 0.333743, 0.333107, 0.332471, 0.336969, 0.341466, 0.345964, 0.350462, 0.354960, 0.359457, 0.363955, 0.368453, 0.373236, 0.378018, 0.382801, 0.387584, 0.392366, 0.397149, 0.401932, 0.406714, 0.424715, 0.442716, 0.460717, 0.478718, 0.496719, 0.514720, 0.532722, 0.550723, 0.567148, 0.583574, 0.600000, 0.616426, 0.631375, 0.646324, 0.661274, 0.676223, 0.715355, 0.754488, 0.793620, 0.832753, 0.871885, 0.911017, 0.950150, 0.989282, 0.981504, 0.973726, 0.965948, 0.958169, 0.950391, 0.942613, 0.934835, 0.927056, 0.925685, 0.924314, 0.922943, 0.921571, 0.910641, 0.899710, 0.888780, 0.877849, 0.862007, 0.846165, 0.830324, 0.814482, 0.800050, 0.785619, 0.771188, 0.756756, 0.742325, 0.727893, 0.713462, 0.699030, 0.701476, 0.703922, 0.706368, 0.708814, 0.711260, 0.713705, 0.716151, 0.718597, 0.693616, 0.668634, 0.643652, 0.618670, 0.593689, 0.568707, 0.543725, 0.510687, 0.477650, 0.444612, 0.411574, 0.414778, 0.417983, 0.421187, 0.424392 },
            new double[]{ 0.300000, 0.310314, 0.320629, 0.330943, 0.341257, 0.351571, 0.361885, 0.372200, 0.382514, 0.401972, 0.421430, 0.440888, 0.460346, 0.479805, 0.499263, 0.518721, 0.538179, 0.537597, 0.537015, 0.536433, 0.535851, 0.535269, 0.534687, 0.534105, 0.533523, 0.534316, 0.535109, 0.535901, 0.536694, 0.537680, 0.538666, 0.539652, 0.540638, 0.541625, 0.542611, 0.543597, 0.544583, 0.545819, 0.547055, 0.548291, 0.549527, 0.550763, 0.551999, 0.553235, 0.554471, 0.561971, 0.569470, 0.576969, 0.584468, 0.591967, 0.599467, 0.606966, 0.614465, 0.617010, 0.619555, 0.622100, 0.624645, 0.638510, 0.652374, 0.666238, 0.680102, 0.693966, 0.707830, 0.721694, 0.735559, 0.767757, 0.799955, 0.832154, 0.864352, 0.896550, 0.928749, 0.960947, 0.993145, 0.984845, 0.976544, 0.968243, 0.959942, 0.951641, 0.943341, 0.935040, 0.926739, 0.920117, 0.913494, 0.906872, 0.900250, 0.893627, 0.887005, 0.880382, 0.873760, 0.872076, 0.870393, 0.868709, 0.867026, 0.859062, 0.851098, 0.843134, 0.835171, 0.826192, 0.817214, 0.808236, 0.799258, 0.790280, 0.781302, 0.772323, 0.763345, 0.761728, 0.760112, 0.758495, 0.756878, 0.755261, 0.753645, 0.752028, 0.750411, 0.737251, 0.724091, 0.710931, 0.697771, 0.684611, 0.671452, 0.658292, 0.629054, 0.599817, 0.570579, 0.541342, 0.512104, 0.482867, 0.453630, 0.424392 },
            new double[]{ 0.400000, 0.421506, 0.443012, 0.464518, 0.486024, 0.493641, 0.501258, 0.508875, 0.516491, 0.524108, 0.531725, 0.539341, 0.546958, 0.551264, 0.555571, 0.559877, 0.564183, 0.568489, 0.572796, 0.577102, 0.581408, 0.585555, 0.589701, 0.593848, 0.597995, 0.600323, 0.602651, 0.604980, 0.607308, 0.609686, 0.612063, 0.614441, 0.616818, 0.619196, 0.621573, 0.623950, 0.626328, 0.635962, 0.645597, 0.655231, 0.664866, 0.674501, 0.684135, 0.693770, 0.703404, 0.726353, 0.749301, 0.772249, 0.795198, 0.812378, 0.829559, 0.846740, 0.863921, 0.881102, 0.898282, 0.915463, 0.932644, 0.939795, 0.946946, 0.954097, 0.961248, 0.966092, 0.970936, 0.975780, 0.980624, 0.985468, 0.990312, 0.995156, 1.000000, 0.999947, 0.999895, 0.999842, 0.999789, 0.999737, 0.999684, 0.999632, 0.999579, 0.994755, 0.989932, 0.985108, 0.980285, 0.975461, 0.970638, 0.965815, 0.960991, 0.958595, 0.956036, 0.953312, 0.950423, 0.947367, 0.944142, 0.940747, 0.937179, 0.933437, 0.929518, 0.925421, 0.921143, 0.916681, 0.912033, 0.907196, 0.902166, 0.896942, 0.891518, 0.885892, 0.880059, 0.874016, 0.867758, 0.861281, 0.854578, 0.847646, 0.840479, 0.833069, 0.825411, 0.817499, 0.809323, 0.800877, 0.792152, 0.783137, 0.773824, 0.764202, 0.754257, 0.743979, 0.733351, 0.722360, 0.710988, 0.699217, 0.687025, 0.674392 },
            new double[]{ 0.500000, 0.525430, 0.550860, 0.576290, 0.601720, 0.637426, 0.673133, 0.708839, 0.744545, 0.780252, 0.815958, 0.851665, 0.887371, 0.894142, 0.900914, 0.907685, 0.914456, 0.921228, 0.927999, 0.934770, 0.941541, 0.951529, 0.961516, 0.971504, 0.981491, 0.978624, 0.975756, 0.972889, 0.970021, 0.967154, 0.964287, 0.961419, 0.958552, 0.960034, 0.961517, 0.962999, 0.964481, 0.968921, 0.973361, 0.977801, 0.982241, 0.986681, 0.991120, 0.995560, 1.000000, 1.000000, 1.000000, 1.000000, 1.000000, 1.000000, 1.000000, 1.000000, 1.000000, 1.000000, 1.000000, 1.000000, 1.000000, 1.000000, 1.000000, 1.000000, 1.000000, 1.000000, 1.000000, 1.000000, 1.000000, 1.000000, 1.000000, 1.000000, 1.000000, 1.000000, 1.000000, 1.000000, 1.000000, 1.000000, 1.000000, 1.000000, 1.000000, 1.000000, 1.000000, 1.000000, 1.000000, 1.000000, 1.000000, 1.000000, 1.000000, 1.000000, 1.000000, 1.000000, 1.000000, 0.999518, 0.999036, 0.998554, 0.998071, 0.998312, 0.998554, 0.998795, 0.999036, 0.994755, 0.990474, 0.986193, 0.981912, 0.977632, 0.973351, 0.969070, 0.964789, 0.961173, 0.957558, 0.953942, 0.950327, 0.946711, 0.943096, 0.939480, 0.928164, 0.916847, 0.905531, 0.894214, 0.882898, 0.871581, 0.860264, 0.848948, 0.827128, 0.805309, 0.783489, 0.761670, 0.739850, 0.718031, 0.696211, 0.674392 }
        };
        [Header("Performance")]

        [Tooltip("X-axis = RPM in thousands, y-axis = torque.  The rightmost key represents the maximum RPM")]
        public AnimationCurve torqueCurve = AnimationCurve.EaseInOut(0, 0, 8, 1);

        [Range(0, 0.99f)]
        [Tooltip("How quickly the engine adjusts its RPMs")]
        public float inertia = 0.3f;

        [Tooltip("Can the engine turn backwards?")]
        public bool canReverse;
        DriveForce targetDrive;
        [System.NonSerialized]
        public float maxRPM;

        float limit2RPM = 22.5f; // in 1000s
        float limitRPM = 22.35f; // in 1000s
        float minRPM = 0.45f; // in 1000s
        float maxTorque = 0.28F; // in 100s

        public DriveForce[] outputDrives;

        [Tooltip("Exponent for torque output on each wheel")]
        public float driveDividePower = 3;
        public float actualAccel;

        [Header("Transmission")]
        public GearboxTransmission transmission;
        bool rpmTooHigh = false;
        float curr_engine_rpm = 0;
        public ParticleSystem engineSmoke;
        public float batteryCutOffLevel = 0.1f;
        public float boostEval;
        public float maxBoost = 0.2f;
        AnimationCurve GenerateTorqueCurve(int curve_number, float max_rpm, float torque_mult)
        {
            curve_number = Mathf.Clamp(curve_number, 0, torqueCurveData.Length);
            Keyframe[] keys = new Keyframe[torqueCurveData[curve_number].Length];
            for (int i = 0; i < keys.Length; i++)
            {
                keys[i].time = max_rpm * (i+1) / keys.Length;
                keys[i].value = torque_mult * (float)torqueCurveData[curve_number][i];
            }
            return new AnimationCurve(keys);
        }
        public override void Start() 
        {
            base.Start();
            targetDrive = GetComponent<DriveForce>();
            torqueCurve = GenerateTorqueCurve(1, limit2RPM, maxTorque);
            GetMaxRPM();
        }

        public override void FixedUpdate() {
            base.FixedUpdate();

            // Calculate proper input
            actualAccel = Mathf.Lerp(vp.brakeIsReverse && vp.reversing && vp.accelInput <= 0 ? vp.brakeInput : vp.accelInput, Mathf.Max(vp.accelInput, vp.burnout), vp.burnout);
            float accelGet = canReverse ? actualAccel : Mathf.Clamp01(actualAccel);
            actualInput = inputCurve.Evaluate(Mathf.Abs(accelGet)) * Mathf.Sign(accelGet);

            targetDrive.curve = torqueCurve;

            if (ignition) {
                if (boosting && vp.accelInput>0)
                    boostEval = maxBoost * boostPowerCurve.Evaluate(Time.time - boostActivatedTime);
                else
                    boostEval = 0;
                
                float targetRPM;
                if (rpmTooHigh || actualInput == 0 || (transmission.IsShifting() && transmission.selectedGear > 2))
                    targetRPM = Mathf.Max(minRPM * 1000, targetDrive.feedbackRPM-2000);
                else
                    targetRPM = actualInput * maxRPM * 1000;
               
                if(!transmission.IsShifting())// || transmission.ShiftSeqProgress() > 0.5f)
                    targetDrive.rpm = Mathf.Lerp(targetDrive.rpm, targetRPM, Mathf.Clamp01((inertia+boostEval)) * 10 * Time.fixedDeltaTime);
                
                curr_engine_rpm = targetDrive.feedbackRPM / 1000f;

                if(engineSmoke != null)
                {
                    if (curr_engine_rpm <= 0.3f * maxRPM)
                    {
                        if (!engineSmoke.isPlaying)
                        {
                            engineSmoke.Play();
                            var main = engineSmoke.main;
                            main.loop = true;
                        }
                            
                    }
                    else
                        engineSmoke.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                }
                
                if(curr_engine_rpm < maxRPM)
                {
                    if(rpmTooHigh)
                    {
                        if (curr_engine_rpm < limitRPM)
                        {
                            rpmTooHigh = false;
                        }
                    }
                    else if (curr_engine_rpm >= 0.99f*limit2RPM && transmission.currentGear != transmission.gears.Length - 1)
                    {
                        rpmTooHigh = true;
                        targetDrive.torque = 0;
                    }
                    else
                    {
                        if (targetDrive.feedbackRPM > targetDrive.rpm)
                        {
                            targetDrive.torque = 0;
                        }
                        else
                        {
                            targetDrive.torque = (boosting ? boostEval : 1) * torqueCurve.Evaluate(curr_engine_rpm) *
                            Mathf.Lerp(targetDrive.torque,
                            Mathf.Abs(Mathf.Sign(actualInput)),
                            (inertia + boostEval) * Time.timeScale * health);
                            targetDrive.torque = Mathf.Clamp(targetDrive.torque, 0, float.MaxValue);
                        }
                    }
                }
                // Send RPM and torque through drivetrain
                if (outputDrives.Length > 0) {
                    float torqueFactor = Mathf.Pow(1f / outputDrives.Length, driveDividePower);
                    float tempRPM = 0;

                    foreach (DriveForce curOutput in outputDrives) {
                        tempRPM += curOutput.feedbackRPM;
                        curOutput.SetDrive(targetDrive, torqueFactor);
                    }

                    targetDrive.feedbackRPM = tempRPM / outputDrives.Length;
                }
            }
            else {
                // If turned off
                targetDrive.rpm = 0;
                targetDrive.torque = 0;
                //targetDrive.feedbackRPM = 0;

                if (outputDrives.Length > 0) {
                    foreach (DriveForce curOutput in outputDrives) {
                        curOutput.SetDrive(targetDrive);
                    }
                }
            }
        }
        public override void Update() {
            // Set audio pitch
            if (snd && ignition) {
                airPitch = vp.groundedWheels > 0 || actualAccel != 0 ? 1 : Mathf.Lerp(airPitch, 0, 0.5f * Time.deltaTime);
                
                targetPitch = Mathf.Abs((targetDrive.feedbackRPM * 0.001f) / limit2RPM);
            }

            base.Update();
        }

        // Calculates the max RPM and propagates its effects
        public void GetMaxRPM() {
            maxRPM = torqueCurve.keys[torqueCurve.length - 1].time;

            if (outputDrives.Length > 0) {
                foreach (DriveForce curOutput in outputDrives) {
                    curOutput.curve = targetDrive.curve;

                    if (curOutput.GetComponent<Transmission>()) {
                        curOutput.GetComponent<Transmission>().ResetMaxRPM();
                    }
                }
            }
        }
    }
}