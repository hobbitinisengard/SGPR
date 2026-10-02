#if UNITY_EDITOR
using UnityEngine;
using System.Collections;
using UnityEditor;

namespace RVP
{
    [CustomEditor(typeof(GroundSurfaceInstance))]
    [CanEditMultipleObjects]

    public class GroundSurfaceInstanceEditor : Editor
    {
        public override void OnInspectorGUI() {
            GroundSurfaceMaster surfaceMaster = FindFirstObjectByType<GroundSurfaceMaster>();
            serializedObject.Update();
            SerializedProperty surfaceType = serializedObject.FindProperty("surfaceType");
            if (surfaceMaster && surfaceMaster.surfaceTypes != null && surfaceMaster.surfaceTypes.Length > 0)
            {
                string[] surfaceNames = new string[surfaceMaster.surfaceTypes.Length];
                for (int i = 0; i < surfaceNames.Length; i++)
                    surfaceNames[i] = surfaceMaster.surfaceTypes[i].name;
                surfaceType.intValue = EditorGUILayout.Popup("Surface Type", surfaceType.intValue, surfaceNames);
            }
            else
                EditorGUILayout.PropertyField(surfaceType);

            EditorGUILayout.PropertyField(serializedObject.FindProperty("sourceGrip"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("sourceFlags"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("sourceRolling"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("sourceRoughness"));
            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif
