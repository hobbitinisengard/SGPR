#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace RVP
{
	[CustomEditor(typeof(PropertyToggleSetter))]
	public class PropertyToggleSetterEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			EditorGUILayout.HelpBox(
				"Vehicle handling is controlled by the original physics configuration.",
				MessageType.Info);
		}
	}
}
#endif
