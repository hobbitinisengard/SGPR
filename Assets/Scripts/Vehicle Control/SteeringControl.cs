using UnityEngine;

namespace RVP
{
	[DisallowMultipleComponent]
	[AddComponentMenu("RVP/Vehicle Controllers/Steering Control", 2)]
	public class SteeringControl : MonoBehaviour
	{
		public AudioSource servoAudio;
		[Tooltip("Front suspension pivots, with FL first")]
		public Suspension[] steeredWheels;

		VehicleParent vehicle;
		Transform steeringWheel;

		void Start()
		{
			vehicle = transform.GetTopmostParentComponent<VehicleParent>();
			if (transform.childCount > 0)
				steeringWheel = transform.GetChild(0);
		}

		void FixedUpdate()
		{
			if (!vehicle || vehicle.originalVehiclePhysics == null)
				return;

			if (servoAudio)
			{
				servoAudio.volume = Mathf.Abs(vehicle.steerInput);
				servoAudio.pitch = 1;
			}

			if (steeringWheel)
				steeringWheel.localRotation = Quaternion.Lerp(steeringWheel.localRotation,
					Quaternion.Euler(0, 0, -120 * vehicle.steerInput), 5 * Time.fixedDeltaTime);

			if (steeredWheels == null)
				return;

			foreach (Suspension suspension in steeredWheels)
			{
				if (!suspension)
					continue;

				float steeringDegrees = vehicle.originalVehiclePhysics.SteeringDegreesFor(suspension);
				float range = steeringDegrees >= 0 ? suspension.steerRangeMax : -suspension.steerRangeMin;
				suspension.steerAngle = Mathf.Clamp(steeringDegrees / Mathf.Max(0.001f, range), -1, 1);
			}
		}
	}
}
