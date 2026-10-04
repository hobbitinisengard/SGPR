using UnityEngine;

namespace RVP
{
	[DisallowMultipleComponent]
	[AddComponentMenu("RVP/Drivetrain/Transmission/Gearbox Transmission", 0)]
	public class GearboxTransmission : Transmission
	{
		public AudioSource audioShift;
		public int currentGear { get; private set; } = 1;
		public int selectedGear { get; private set; } = 1;

		public override void Start()
		{
			vp = transform.GetTopmostParentComponent<VehicleParent>();
			OriginalVehiclePhysicsConfig config = vp && vp.carConfig != null
				? vp.carConfig.originalPhysics : null;
			if (config != null)
				SetOriginalGear(1, config);
		}

		public void SetOriginalGear(int gear, OriginalVehiclePhysicsConfig config)
		{
			if (config == null)
				return;
			int lastGear = config.ratios != null && config.ratios.Length > 0
				? Mathf.Min(config.gearCount, config.ratios.Length - 1) : 1;
			currentGear = selectedGear = Mathf.Clamp(gear, 0, lastGear);
		}
	}
}
