using System;
using UnityEngine;

namespace RVP
{
	[DisallowMultipleComponent]
	[AddComponentMenu("RVP/Drivetrain/Transmission/Gearbox Transmission", 0)]

	// Transmission subclass for gearboxes
	public class GearboxTransmission : Transmission
	{
		public AudioSource audioShift;
		Gear[] gears;
		public Gear[] Gears
		{
			get => gears;
			set
			{
				maxRPM = -1;
				autoCalculateRpmRanges = true;
				gears = value;
			}
		}

		public int startGear;
		[System.NonSerialized]
		public int currentGear;

		public bool skipNeutral;

		[Tooltip("Calculate the RPM ranges of the gears in play mode.  This will overwrite the current values")]
		public bool autoCalculateRpmRanges = false;

		[Tooltip("Number of physics steps a shift should last")]
		public float shiftDelay { get; private set; }
		public float shiftTime { get; private set; }
		public float shiftDelaySeconds = 0.5f;
		public float d_feedback;
		public float d_rpm;
		public float actualFeedbackRPM;
		Gear upperGear; // Next gear above current
		public enum DriveType { FWD, RWD, AWD }
		DriveType drive;
		public DriveType Drive
		{
			get => drive;
			set
			{
				drive = value;
				switch (drive)
				{
					case DriveType.FWD:
						outputDrives = new DriveForce[] { vp.wheels[0].suspensionParent.targetDrive, vp.wheels[1].suspensionParent.targetDrive };
						break;
					case DriveType.RWD:
						outputDrives = new DriveForce[] { vp.wheels[2].suspensionParent.targetDrive, vp.wheels[3].suspensionParent.targetDrive };
						break;
					case DriveType.AWD:
						outputDrives = new DriveForce[] { vp.wheels[0].suspensionParent.targetDrive, vp.wheels[1].suspensionParent.targetDrive,
																 vp.wheels[2].suspensionParent.targetDrive, vp.wheels[3].suspensionParent.targetDrive };
						break;
					default:
						break;
				}
			}
		}

		//public Drive Drive
		//[Tooltip("Multiplier for comparisons in automatic shifting calculations, should be 2 in most cases")]
		//float shiftThreshold = 2;
		public int selectedGear { get; private set; }
		public bool IsShifting()
		{
			return shiftTime > 0;
		}
		public override void Start()
		{
			base.Start();
			shiftDelay = shiftDelaySeconds;
			gears = new Gear[]
			{
				new Gear(-3.21f),
				new Gear(0),
				new Gear(3.21f),
				new Gear(2.52f),
				new Gear(2.0f),
				new Gear(1.5f),
				new Gear(1.2f)
			};
			currentGear = Mathf.Clamp(startGear, 0, gears.Length - 1);
			selectedGear = currentGear;
			shiftTime = 0;
		}
		void Update()
		{
			// Check for manual shift button presses
			if (!automatic)
			{
				if (vp.upshiftPressed && currentGear < gears.Length - 1)
				{
					Shift(1);
				}

				if (vp.downshiftPressed && currentGear > 0)
				{
					Shift(-1);
				}
			}
		}
		void FixedUpdate()
		{
			health = Mathf.Clamp01(health);
			shiftTime = Mathf.Max(0, shiftTime - Time.timeScale * Time.fixedDeltaTime);
			d_feedback = targetDrive.feedbackRPM;
			d_rpm = targetDrive.rpm;
			if (shiftTime == 0 || currentGear < 2)
			{
				if (shiftTime == 0)
					currentGear = selectedGear;

				float curOutputRatio = gears[currentGear].ratio;
				actualFeedbackRPM = targetDrive.feedbackRPM / (curOutputRatio == 0 ? 1 : Mathf.Abs(curOutputRatio));

				int upGearOffset = 1;

				//while (/*(skipNeutral || automatic) && 
				//                gears[Mathf.Clamp(currentGear + upGearOffset, 0, gears.Length - 1)].ratio == 0
				//                && */currentGear + upGearOffset != 0 && currentGear + upGearOffset < gears.Length - 1)
				//{
				//	upGearOffset++;
				//}

				//while ((skipNeutral || automatic) &&
				//		  gears[Mathf.Clamp(currentGear - downGearOffset, 0, gears.Length - 1)].ratio == 0
				//		  && currentGear - downGearOffset != 0 && currentGear - downGearOffset > 0)
				//{
				//	downGearOffset++;
				//}


				upperGear = gears[Mathf.Min(gears.Length - 1, currentGear + upGearOffset)];

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
				if (automatic && vp.countdownTimer < shiftDelay && vp.reallyGroundedWheels >= 2)
				{
					if (selectedGear == currentGear)
					{
						if (currentGear < gears.Length - 1)
						{
							if (!(vp.brakeInput > 0 && vp.brakeIsReverse && upperGear.ratio == 0)
							&& !(vp.localVelocity.z < 0 && vp.accelInput == 0))
							{
								if ((actualFeedbackRPM > 0.9f * gears[currentGear].maxRPM && vp.velMag > upperGear.minSpeed)
									 || (vp.localVelocity.z < 3 && vp.localVelocity.z > -3 && vp.accelInput > 0 && currentGear < 2))
								{
									Shift(1);
								}
							}
						}
						if (currentGear > 0)
						{
							if ((vp.velMag < gears[currentGear].minSpeed)
								|| (vp.localVelocity.z < 5 && vp.brakeIsReverse && vp.brakeInput > 0))
							{
								int downGearOffset = 1;
								while (
									((skipNeutral && currentGear - downGearOffset >= 0) || currentGear - downGearOffset > 0)
									&& (vp.velMag < gears[currentGear - downGearOffset].minSpeed))
								{
									downGearOffset++;
								}
								Shift(-downGearOffset);
							}
						}
					}
				}
				curOutputRatio = gears[currentGear].ratio;
				// Set RPMs and torque of output
				newDrive.curve = targetDrive.curve;

				newDrive.rpm = (automatic && skidSteerDrive ?
						Mathf.Abs(targetDrive.rpm) * Mathf.Sign(vp.accelInput - (vp.brakeIsReverse ? vp.brakeInput * (1 - vp.burnout) : 0))
						: targetDrive.rpm) / (curOutputRatio == 0 ? 1 : curOutputRatio);
				newDrive.torque = Mathf.Abs(curOutputRatio) * targetDrive.torque;

				SetOutputDrives(curOutputRatio);
			}
			else
			{ // switch gear with clutch-like action
			  // 0 = completed, 1 = just began
				float sequenceComplt = shiftTime / shiftDelay;
				// perform shift sequence
				if (sequenceComplt > 0.5f)
				{
					SetOutputDrives(gears[currentGear].ratio, Mathf.InverseLerp(1, 0.5f, sequenceComplt));//Mathf.Abs(-shiftDelay + shiftTime) / shiftDelay); // 0 -> 1
				}
				else
				{
					SetOutputDrives(gears[selectedGear].ratio, 2 * sequenceComplt); // | 1 -> 0
				}
			}
		}
		///// <summary>
		///// speed in m/s
		///// </summary>
		///// <param name="spd"></param>
		///// <returns></returns>
		//float RPM4Speed(float spd)
		//{
		//	return spd * 30 * 3.6f / (Mathf.PI * vp.wheels[2].tireRadius);
		//}
		// Shift gears by the number entered
		public void Shift(int dir)
		{
			Debug.Log(dir);
			shiftTime = shiftDelaySeconds;
			selectedGear += dir;
			if (audioShift)
				audioShift.Play();
			//while ((skipNeutral || automatic) && gears[Mathf.Clamp(currentGear, 0, gears.Length - 1)].ratio == 0
			//    && selectedGear != 0 && selectedGear != gears.Length - 1) {
			//    selectedGear += dir;
			//}

			selectedGear = Mathf.Clamp(selectedGear, 0, gears.Length - 1);
		}

		// Shift straight to the gear specified
		public void ShiftToGear(int gear)
		{
			if (health > 0)
			{
				shiftTime = shiftDelaySeconds;
				selectedGear = Mathf.Clamp(gear, 0, gears.Length - 1);
			}
		}

		// Caculate ideal RPM ranges for each gear (works most of the time)
		public void CalculateRpmRanges()
		{
			bool cantCalc = false;
			//if (!Application.isPlaying)
			//{ }
			GasMotor engine = transform.GetTopmostParentComponent<VehicleParent>().GetComponentInChildren<GasMotor>();

			if (!engine)
			{
				Debug.LogError("There is no <GasMotor> in the vehicle to get RPM info from.", this);
				cantCalc = true;
			}
			else
			{
				maxRPM = engine.limit2kRPM * 1000;
			}


			if (!cantCalc)
			{
				float prevGearRatio;
				float nextGearRatio;

				for (int i = 0; i < gears.Length; i++)
				{
					prevGearRatio = gears[Mathf.Max(i - 1, 0)].ratio;
					nextGearRatio = gears[Mathf.Min(i + 1, gears.Length - 1)].ratio;

					if (gears[i].ratio < 0)
					{
						gears[i].minRPM = maxRPM / gears[i].ratio;

						if (nextGearRatio == 0)
						{
							gears[i].maxRPM = 0;
						}
						else
						{
							gears[i].maxRPM = maxRPM / nextGearRatio + (maxRPM / nextGearRatio - gears[i].minRPM) * 0.5f;
						}
					}
					else if (gears[i].ratio > 0)
					{
						gears[i].maxRPM = maxRPM / gears[i].ratio;

						if (prevGearRatio == 0)
						{
							gears[i].minRPM = 0;
						}
						else
						{
							gears[i].minRPM = maxRPM / prevGearRatio - (gears[i].maxRPM - maxRPM / prevGearRatio) * 0.5f;
						}
						// I have no idea why cofficients '0.45f' and '3.6f' are working. 
						gears[i].minSpeed = 0.45f * gears[i - 1].maxRPM / 60 * 2 * Mathf.PI * vp.wheels[2].tireRadius / 3.6f;
					}
					else
					{
						gears[i].minRPM = 0;
						gears[i].maxRPM = 0;
					}
					gears[i].minRPM *= 0.6f; // why? (it works though)
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
