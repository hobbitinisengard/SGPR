#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class FindMissingScripts : EditorWindow
{
	[MenuItem("Tools/Find And Select Missing Scripts")]
	public static void FindAll()
	{
		List<GameObject> objectsWithMissing = new List<GameObject>();

		foreach (GameObject go in Resources.FindObjectsOfTypeAll<GameObject>())
		{
			foreach (Component c in go.GetComponents<Component>())
			{
				if (c == null)
				{
					Debug.LogWarning($"Missing script on: {go.name} " +
							$"| HideFlags: {go.hideFlags} " +
							$"| Scene: {go.scene.name}", go);
					objectsWithMissing.Add(go);
				}
			}
		}

		// Select all offending objects so they appear in the Inspector
		if (objectsWithMissing.Count > 0)
		{
			Selection.objects = objectsWithMissing.ToArray();
			Debug.Log($"Selected {objectsWithMissing.Count} object(s) with missing scripts.");
		}
	}
	[MenuItem("Tools/Find Missing Scripts")]
	public static void FindAll2()
	{
		int missingCount = 0;

		// Search scene objects
		foreach (GameObject go in Resources.FindObjectsOfTypeAll<GameObject>())
		{
			foreach (Component c in go.GetComponents<Component>())
			{
				if (c == null)
				{
					Debug.LogWarning($"Missing script on: {go.name} " +
							$"(Scene: {go.scene.name})", go);
					missingCount++;
				}
			}
		}

		// Search prefabs in project
		string[] prefabPaths = AssetDatabase.FindAssets("t:Prefab");
		foreach (string guid in prefabPaths)
		{
			string path = AssetDatabase.GUIDToAssetPath(guid);
			GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
			if (prefab == null) continue;

			foreach (Component c in prefab.GetComponentsInChildren<Component>(true))
			{
				if (c == null)
				{
					Debug.LogWarning($"Missing script on prefab: {prefab.name} " +
							$"at path: {path}");
					missingCount++;
				}
			}
		}

		Debug.Log($"Search complete. Found {missingCount} missing script(s).");
	}
}
#endif