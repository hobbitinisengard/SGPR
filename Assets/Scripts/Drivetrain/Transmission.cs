using UnityEngine;
using System.Collections;

namespace RVP
{
    [RequireComponent(typeof(DriveForce))]

    // Class for transmissions
    public abstract class Transmission : MonoBehaviour
    {
        [Range(0, 1)]
        public float strength = 1;
        [System.NonSerialized]
        public float health = 1;
        protected VehicleParent vp;
        /// <summary>
        /// targetDrive is this transmission's drive
        /// </summary>
        protected DriveForce targetDrive;
        protected DriveForce newDrive;
        public bool automatic;

        [Tooltip("Apply special drive to wheels for skid steering")]
        public bool skidSteerDrive;

        public DriveForce[] outputDrives;

        [Tooltip("Exponent for torque output on each wheel")]
        public float driveDividePower = 3.89f;

        [System.NonSerialized]
        public float maxRPM = -1;

        

        public virtual void Start() {
            vp = transform.GetTopmostParentComponent<VehicleParent>();
            targetDrive = GetComponent<DriveForce>();
            newDrive = gameObject.AddComponent<DriveForce>();
        }

        protected void SetOutputDrives(float ratio, float clutch = 0) {
            // Distribute drive to wheels
            if (outputDrives.Length > 0) {
                if (ratio == 0) // N gear
                {
                    targetDrive.feedbackRPM = targetDrive.rpm;
                    return;
                }
                   
                int enabledDrives = 0;

                // Check for which outputs are enabled
                foreach (DriveForce curOutput in outputDrives)
                {
                    if (curOutput.active)
                    {
                        enabledDrives++;
                    }
                }

                float torqueFactor = Mathf.Pow(1f / enabledDrives, driveDividePower);
                float tempRPM = 0;

                foreach (DriveForce curOutput in outputDrives)
                {
                    if (curOutput.active)
                    {
                        tempRPM += skidSteerDrive ? Mathf.Abs(curOutput.feedbackRPM) : curOutput.feedbackRPM;
                        // curOutput represent wheels's forces
                        curOutput.SetDrive(newDrive, torqueFactor);
                    }
                }
                
                if (clutch > 0)
                {
                    float targetRpm = vp.velMag * 30 * 3.6f / (Mathf.PI * vp.wheels[2].tireRadius);
                    targetDrive.rpm = Mathf.Lerp(targetDrive.feedbackRPM, targetRpm, (1-clutch));
                }
                targetDrive.feedbackRPM = Mathf.Lerp((tempRPM / enabledDrives) * ratio, targetDrive.rpm, clutch);
            }
        }

        public void ResetMaxRPM() {
            maxRPM = -1; // Setting this to -1 triggers derived classes to recalculate things
        }
    }
}
