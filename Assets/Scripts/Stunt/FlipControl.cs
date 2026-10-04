using UnityEngine;

namespace RVP
{
	[RequireComponent(typeof(VehicleParent))]
	[DisallowMultipleComponent]
	[AddComponentMenu("RVP/Stunt/Flip Control", 2)]
	public sealed class FlipControl : MonoBehaviour
	{
		// Original airborne stunts are processed by OriginalVehiclePhysics.
	}
}