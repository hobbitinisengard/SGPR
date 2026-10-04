using UnityEngine;

namespace RVP
{
	[RequireComponent(typeof(VehicleParent))]
	[DisallowMultipleComponent]
	[AddComponentMenu("RVP/Vehicle Controllers/Vehicle Balance", 4)]
	public sealed class VehicleBalance : MonoBehaviour
	{
		// Source vehicle dynamics already reproduce the original balance response.
	}
}