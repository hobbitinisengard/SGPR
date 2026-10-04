#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace RVP
{
	[CustomEditor(typeof(GearboxTransmission))]
	public class GearboxTransmissionEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			EditorGUILayout.HelpBox(
				"Gear ratios and shifts are controlled by the vehicle's original physics carcfg.",
				MessageType.Info);
		}
	}
}
#endif
