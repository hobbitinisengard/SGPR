using UnityEngine;

namespace RVP
{
	[RequireComponent(typeof(VehicleParent))]
	[DisallowMultipleComponent]
	[AddComponentMenu("RVP/Vehicle Controllers/Vehicle Assist", 1)]
	public sealed class VehicleAssist : MonoBehaviour
	{
		// Source vehicle dynamics, rail control, and airborne rotation are handled
		// by OriginalVehiclePhysics.
	}
}