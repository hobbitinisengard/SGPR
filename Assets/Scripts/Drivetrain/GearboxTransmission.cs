using UnityEngine;
using UnityEngine.Experimental;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;

namespace RVP
{
	[DisallowMultipleComponent]
	[AddComponentMenu("RVP/Drivetrain/Transmission/Gearbox Transmission", 0)]

	// Transmission subclass for gearboxes
	public class GearboxTransmission : Transmission
	{
		public AudioSource audioShift;
		public Gear[] gears { get; private set; }
		public int startGear;
		[System.NonSerialized]
		public int currentGear;
		int firstGear;

		public bool skipNeutral;

		[Tooltip("Calculate the RPM ranges of the gears in play mode.  This will overwrite the current values")]
		public bool autoCalculateRpmRanges = false;

		[Tooltip("Number of physics steps a shift should last")]
		public float shiftDelay { get; private set; }
		public float shiftTime { get; private set; }
		public float shiftDelaySeconds = 0.5f;
		private float wheelsNotGroundedTime;
        public float d_feedback;
        public float d_rpm;
		public float actualFeedbackRPM;
        Gear upperGear; // Next gear above current

		//[Tooltip("Multiplier for comparisons in automatic shifting calculations, should be 2 in most cases")]
		//float shiftThreshold = 2;
		public int selectedGear { get; private set; }
		public bool IsShifting()
		{
			return shiftTime>0;
		}
		public override void Start() {
			base.Start();
			shiftDelay = shiftDelaySeconds / Time.fixedDeltaTime;
			gears = new Gear[]
			{
				new Gear(-3.21f),
				new Gear(0),
				new Gear(3.21f),
				new Gear(2.68f),
				new Gear(2.12f),
				new Gear(1.76f),
				new Gear(1.52f),
				new Gear(1.38f),
				new Gear(1.25f),
				new Gear(1.15f)
			};
			currentGear = Mathf.Clamp(startGear, 0, gears.Length - 1);
			selectedGear = currentGear;
			// Get gear number 1 (first one above neutral)
			GetFirstGear();
			wheelsNotGroundedTime = Time.time;
			shiftTime = 0;
        }

		void Update() {
			if (vp.groundedWheels == 0)
				wheelsNotGroundedTime = Time.time;
			
			// Check for manual shift button presses
			if (!automatic) {
				if (vp.upshiftPressed && currentGear < gears.Length - 1) {
					Shift(1);
				}

				if (vp.downshiftPressed && currentGear > 0) {
					Shift(-1);
				}
			}
		}
		void FixedUpdate() {
			health = Mathf.Clamp01(health);
			shiftTime = Mathf.Max(0, shiftTime - Time.timeScale * TimeMaster.inverseFixedTimeFactor);
			d_feedback = targetDrive.feedbackRPM;
			d_rpm = targetDrive.rpm;
			if (shiftTime == 0)
			{
                currentGear = selectedGear;
                float curOutputRatio = gears[currentGear].ratio;
                actualFeedbackRPM = targetDrive.feedbackRPM / (curOutputRatio == 0 ? 1 : Mathf.Abs(curOutputRatio));

                int upGearOffset = 3;
				int downGearOffset = 3;

				while (/*(skipNeutral || automatic) && 
                    gears[Mathf.Clamp(currentGear + upGearOffset, 0, gears.Length - 1)].ratio == 0
                    && */currentGear + upGearOffset != 0 && currentGear + upGearOffset < gears.Length - 1)
				{
					upGearOffset++;
				}

				while (/*(skipNeutral || automatic) && 
                    gears[Mathf.Clamp(currentGear - downGearOffset, 0, gears.Length - 1)].ratio == 0 
                    && */ currentGear - downGearOffset != 0 && currentGear - downGearOffset > 0)
				{
					downGearOffset++;
				}

				upperGear = gears[Mathf.Min(gears.Length - 1, selectedGear + upGearOffset)];
                //Gear lowerGear = gears[Mathf.Max(0, selectedGear - downGearOffset)];

                // Perform RPM calculations
                if (maxRPM == -1)
                {
                    maxRPM = targetDrive.curve.keys[targetDrive.curve.length - 1].time;

                    if (autoCalculateRpmRanges)
                    {
                        CalculateRpmRanges();
                    }
                }

                
                
                if (automatic)
                {
                    if (!skidSteerDrive && vp.burnout == 0)
                    {
                        // Perform automatic shifting
                        //upshiftDifference = gears[selectedGear].maxRPM - upperGear.minRPM;
                        //downshiftDifference = lowerGear.maxRPM - gears[selectedGear].minRPM;

                        //if (Mathf.Abs(vp.localVelocity.z) > 1 || vp.accelInput > 0 || (vp.brakeInput > 0 && vp.brakeIsReverse)) 
                        //{
                        if (Time.time - wheelsNotGroundedTime > 1 && selectedGear < gears.Length - 1)
                        {
                            if (!(vp.brakeInput > 0 && vp.brakeIsReverse && upperGear.ratio >= 0)
                            && !(vp.localVelocity.z < 0 && vp.accelInput == 0))
                            {
                                if ((actualFeedbackRPM > 0.9f * gears[selectedGear].maxRPM && !vp.AnyWheelsPowerSliding())
                                    || (vp.localVelocity.z < 1 && vp.localVelocity.z > -1 && vp.accelInput > 0 && currentGear == 1))
                                {
                                    Shift(1);
                                }
                            }
                        }
                        if (selectedGear > 0)
                        {
                            if ((vp.groundedWheels != 0 && vp.localVelocity.z < gears[selectedGear].minSpeed
								&& vp.localVelocity.z > 0 && !vp.AnyWheelsPowerSliding())
                                || (vp.velMag < 1 && vp.brakeInput > 0 && vp.brakeIsReverse))
                            {
                                Shift(-1);
                            }
                        }
                        //}
                    }
                    else if (currentGear != firstGear)
                    {
                        // Shift into first gear if skid steering or burning out
                        ShiftToGear(firstGear);
                    }
                }

				// N gear
				if (selectedGear != currentGear)
				{
					newDrive.rpm = 0;
					newDrive.torque = 0;
                }
                else
				{
                    curOutputRatio = gears[currentGear].ratio;
                    // Set RPMs and torque of output
                    newDrive.curve = targetDrive.curve;

                    newDrive.rpm = (automatic && skidSteerDrive ?
                        Mathf.Abs(targetDrive.rpm) * Mathf.Sign(vp.accelInput - (vp.brakeIsReverse ? vp.brakeInput * (1 - vp.burnout) : 0))
                        : targetDrive.rpm) / (curOutputRatio == 0 ? 1 : curOutputRatio);
                    newDrive.torque = Mathf.Abs(curOutputRatio) * targetDrive.torque;

                    SetOutputDrives(curOutputRatio);
                }
            }
			else
			{
				float sequenceComplt = ShiftSeqProgress();
                // perform shift sequence
                if (sequenceComplt > 0.5f)
                {
					SetOutputDrives(gears[currentGear].ratio, Mathf.Abs(-shiftDelay+shiftTime)/shiftDelay); // 1 -> 0.5 | 0 -> 1
                }
                else
                {
					SetOutputDrives(gears[selectedGear].ratio, 2*sequenceComplt); // | 1 -> 0
                }
            }
        }
		/// <summary>
		/// 0 = completed, 1 = just began
		/// </summary>
		/// <returns></returns>
		public float ShiftSeqProgress()
		{
			return shiftTime / shiftDelay;
        }
		// Shift gears by the number entered
		public void Shift(int dir) {
			if (health > 0) {
				shiftTime = shiftDelay;
				selectedGear += dir;
				if(audioShift)
					audioShift.Play();
				//while ((skipNeutral || automatic) && gears[Mathf.Clamp(currentGear, 0, gears.Length - 1)].ratio == 0
				//    && selectedGear != 0 && selectedGear != gears.Length - 1) {
				//    selectedGear += dir;
				//}

				selectedGear = Mathf.Clamp(selectedGear, 0, gears.Length - 1);
			}
		}

		// Shift straight to the gear specified
		public void ShiftToGear(int gear) {
			if (health > 0) {
				shiftTime = shiftDelay;
				selectedGear = Mathf.Clamp(gear, 0, gears.Length - 1);
			}
		}

		// Caculate ideal RPM ranges for each gear (works most of the time)
		public void CalculateRpmRanges() {
			bool cantCalc = false;
			if (!Application.isPlaying) {
				GasMotor engine = transform.GetTopmostParentComponent<VehicleParent>().GetComponentInChildren<GasMotor>();

				if (engine) {
					maxRPM = engine.torqueCurve.keys[engine.torqueCurve.length - 1].time;
				}
				else {
					Debug.LogError("There is no <GasMotor> in the vehicle to get RPM info from.", this);
					cantCalc = true;
				}
			}

			if (!cantCalc) {
				float prevGearRatio;
				float nextGearRatio;
				float actualMaxRPM = maxRPM * 1000;

				for (int i = 0; i < gears.Length; i++) {
					prevGearRatio = gears[Mathf.Max(i - 1, 0)].ratio;
					nextGearRatio = gears[Mathf.Min(i + 1, gears.Length - 1)].ratio;

					if (gears[i].ratio < 0) {
						gears[i].minRPM = actualMaxRPM / gears[i].ratio;

						if (nextGearRatio == 0) {
							gears[i].maxRPM = 0;
						}
						else {
							gears[i].maxRPM = actualMaxRPM / nextGearRatio + (actualMaxRPM / nextGearRatio - gears[i].minRPM) * 0.5f;
						}
					}
					else if (gears[i].ratio > 0) {
						gears[i].maxRPM = actualMaxRPM / gears[i].ratio;

						if (prevGearRatio == 0) {
							gears[i].minRPM = 0;
						}
						else {
							gears[i].minRPM = actualMaxRPM / prevGearRatio - (gears[i].maxRPM - actualMaxRPM / prevGearRatio) * 0.5f;
						}
					}
					else {
						gears[i].minRPM = 0;
						gears[i].maxRPM = 0;
					}
                    gears[i].minRPM *= 0.75f;
                    gears[i].minSpeed = 0.8f*gears[i].minRPM/60 * 2 * Mathf.PI * vp.wheels[2].tireRadius / 3.6f;
					//gears[i].maxRPM *= 0.55f;
				}
			}
		}

		// Returns the first gear (first gear above neutral)
		public void GetFirstGear() {
			for (int i = 0; i < gears.Length; i++) {
				if (gears[i].ratio == 0) {
					firstGear = i + 1;
					return;
				}
			}
		}
	}

	// Gear class
	[System.Serializable]
	public class Gear
	{
		public float ratio;
		public float minRPM;
		public float maxRPM;
		public float minSpeed;

        public Gear(float ratio)
		{
			this.ratio = ratio;
		}
	}
}
