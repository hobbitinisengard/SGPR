using UnityEngine;

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

		public virtual void Start()
		{
			vp = transform.GetTopmostParentComponent<VehicleParent>();
			targetDrive = GetComponent<DriveForce>();
			newDrive = gameObject.AddComponent<DriveForce>();
		}

		protected void SetOutputDrives(float ratio, float clutch = 0)
		{
			newDrive.active = ratio != 0 && clutch == 0;
			// Distribute drive to wheels
			if (outputDrives.Length > 0)
			{
				if (ratio == 0) // N gear
				{
					targetDrive.feedbackRPM = targetDrive.rpm;
					return;
				}
				int enabledDrives = 0;

				foreach (DriveForce curOutput in outputDrives)
				{
					//if (curOutput.active)
					//{
						enabledDrives++;
					//}
				}

				float torqueFactor = Mathf.Pow(1f / enabledDrives, driveDividePower);
				float tempRPM = 0;

				foreach (DriveForce curOutput in outputDrives)
				{
					//if (curOutput.active)
					//{
						tempRPM += curOutput.feedbackRPM;
						// curOutput represent wheels's forces
						curOutput.SetDrive(newDrive, torqueFactor);
					//}
				}

				if (clutch > 0)
				{
					targetDrive.rpm = Mathf.Lerp(vp.wheels[2].rawRPM, targetDrive.feedbackRPM, clutch);
					targetDrive.torque *= (1-clutch);
				}

				float wheelsRPM = (tempRPM / enabledDrives) * ratio;
				float toRPM = Mathf.Lerp(wheelsRPM, targetDrive.rpm, clutch);
				targetDrive.feedbackRPM = Mathf.Lerp(targetDrive.feedbackRPM, toRPM, Time.fixedDeltaTime * 100);
			}
		}

		public void ResetMaxRPM()
		{
			maxRPM = -1; // Setting this to -1 triggers derived classes to recalculate things
		}
	}
}
