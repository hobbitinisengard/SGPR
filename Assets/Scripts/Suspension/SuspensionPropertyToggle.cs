using UnityEngine;

namespace RVP
{
	// Retained for Unity event bindings on existing assets. The original vehicle
	// model does not switch its drivetrain or steering through remake presets.
	[DisallowMultipleComponent]
	[AddComponentMenu("RVP/Suspension/Suspension Property", 2)]
	public class SuspensionPropertyToggle : MonoBehaviour
	{
		public void ToggleProperty(int index) { }
		public void SetProperty(int index, bool value) { }
	}
}
