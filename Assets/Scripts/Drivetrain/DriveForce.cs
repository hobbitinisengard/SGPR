using UnityEngine;

namespace RVP
{
	[AddComponentMenu("RVP/Drivetrain/Drive Force", 3)]

	// The class for RPMs and torque sent through the drivetrain
	public class DriveForce : MonoBehaviour
	{
		[System.NonSerialized]
		public float rpm;
		[System.NonSerialized]
		public float feedbackRPM;
		[System.NonSerialized]
		public float torque;
		[System.NonSerialized]
		public bool active = true;
		[System.NonSerialized]
		public AnimationCurve curve; // Torque curve
		public void SetDrive(DriveForce from)
		{
			rpm = from.rpm;
			torque = from.torque;
			curve = from.curve;
			active = from.active;
		}

		// Same as previous, but with torqueFactor multiplier for torque
		public void SetDrive(DriveForce from, float torqueFactor)
		{
			rpm = from.rpm;
			torque = from.torque * torqueFactor;
			curve = from.curve;
			active = from.active;
		}
	}
}