using UnityEngine;

namespace RVP
{
	[RequireComponent(typeof(DriveForce))]
	[DisallowMultipleComponent]
	[AddComponentMenu("RVP/Drivetrain/Transmission/Continuous Transmission", 1)]
	public class ContinuousTransmission : Transmission
	{
		[Range(0, 1)] public float targetRatio;
		public float minRatio;
		public float maxRatio;
		[System.NonSerialized] public float currentRatio;
		public bool canReverse;
		public bool brakeIsReverse;
		[System.NonSerialized] public bool reversing;
		public float manualShiftRate = 0.5f;
		public bool automatic;
		public DriveForce[] outputDrives;
		public float driveDividePower = 3.89f;
		[System.NonSerialized] public float maxRPM = -1;

		DriveForce targetDrive;
		DriveForce newDrive;

		public override void Start()
		{
			base.Start();
			targetDrive = GetComponent<DriveForce>();
			newDrive = gameObject.AddComponent<DriveForce>();
		}

		void FixedUpdate()
		{
			health = Mathf.Clamp01(health);
			if (maxRPM == -1)
				maxRPM = targetDrive.curve.keys[targetDrive.curve.length - 1].time * 1000;

			if (health > 0)
			{
				if (automatic && vp.groundedWheels > 0)
					targetRatio = Mathf.Clamp01(
						Mathf.Abs(targetDrive.feedbackRPM) / Mathf.Max(0.01f, maxRPM * Mathf.Abs(currentRatio)));
				else if (!automatic)
					targetRatio = Mathf.Clamp01(targetRatio +
						(vp.upshiftHold - vp.downshiftHold) * manualShiftRate * Time.deltaTime);
			}

			reversing = canReverse && vp.localVelocity.z < 1 &&
				(vp.accelInput < 0 || (brakeIsReverse && vp.brakeInput > 0));
			currentRatio = Mathf.Lerp(minRatio, maxRatio, targetRatio) * (reversing ? -1 : 1);
			newDrive.curve = targetDrive.curve;
			newDrive.rpm = targetDrive.rpm / currentRatio;
			newDrive.torque = Mathf.Abs(currentRatio) * targetDrive.torque;
			SetOutputDrives(currentRatio);
		}

		void SetOutputDrives(float ratio)
		{
			newDrive.active = ratio != 0;
			if (outputDrives == null || outputDrives.Length == 0)
				return;
			if (ratio == 0)
			{
				targetDrive.feedbackRPM = targetDrive.rpm;
				return;
			}

			float torqueFactor = Mathf.Pow(1f / outputDrives.Length, driveDividePower);
			float wheelsRpm = 0;
			foreach (DriveForce output in outputDrives)
			{
				wheelsRpm += output.feedbackRPM;
				output.SetDrive(newDrive, torqueFactor);
			}
			targetDrive.feedbackRPM = Mathf.Lerp(targetDrive.feedbackRPM,
				wheelsRpm / outputDrives.Length * ratio, Time.fixedDeltaTime * 20);
		}

		public void ResetMaxRPM() => maxRPM = -1;
	}
}
