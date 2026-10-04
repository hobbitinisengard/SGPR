using UnityEngine;

namespace RVP
{
	// Kept as an inert component so existing scenes and Unity event targets remain valid.
	[AddComponentMenu("RVP/Suspension/Suspension Property Setter", 3)]
	public class PropertyToggleSetter : MonoBehaviour
	{
		public int currentPreset;

		public void ChangePreset(int preset)
		{
			currentPreset = Mathf.Max(0, preset);
		}
	}
}
