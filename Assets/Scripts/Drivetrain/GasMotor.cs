using UnityEngine;

namespace RVP
{
	[RequireComponent(typeof(DriveForce))]
	[DisallowMultipleComponent]
	[AddComponentMenu("RVP/Drivetrain/Gas Motor", 0)]
	public class GasMotor : Motor
	{
		public GearboxTransmission transmission;
		DriveForce sourceEngineDrive;

		public override void Start()
		{
			base.Start();
			sourceEngineDrive = GetComponent<DriveForce>();
			if (!transmission)
				transmission = GetComponent<GearboxTransmission>();
		}

		protected override void FixedUpdate()
		{
			OriginalVehiclePhysicsConfig config = vp && vp.carConfig != null
				? vp.carConfig.originalPhysics : null;
			float rpmLimit = vp && vp.originalVehiclePhysics != null
				? vp.originalVehiclePhysics.EngineRpmLimit
				: config != null ? Mathf.Max(1, config.rpmLimit) : 0;
			targetPitch = sourceEngineDrive && rpmLimit > 0
				? Mathf.Clamp01(sourceEngineDrive.rpm / rpmLimit)
				: 0;

			bool wasBoosting = boosting;
			boosting = vp && vp.originalVehiclePhysics != null && vp.originalVehiclePhysics.TurboActive;
			if (boosting && !wasBoosting)
				boostActivatedTime = Time.time;

			health = Mathf.Clamp01(health);
			UpdateMotorAudio();
			UpdateBoostPresentation();
		}
	}
}
