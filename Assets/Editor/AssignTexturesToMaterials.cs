using UnityEngine;
using UnityEditor;
using System.IO;

public class AssignTexturesToMaterials : EditorWindow
{
	[MenuItem("Tools/Assign Textures to Materials")]
	public static void ShowWindow()
	{
		GetWindow<AssignTexturesToMaterials>("Assign Textures to Materials");
	}

	private void OnGUI()
	{
		if (GUILayout.Button("Assign Textures to Selected Materials"))
		{
			AssignTextures();
		}
	}

	private static void AssignTextures()
	{
		string textureDirectory = Path.Combine(Application.streamingAssetsPath, "textures");

		if (!Directory.Exists(textureDirectory))
		{
			Debug.LogError($"Texture directory not found: {textureDirectory}");
			return;
		}

		Object[] selectedObjects = Selection.objects;

		foreach (Object obj in selectedObjects)
		{
			if (obj is Material material)
			{
				string materialName = material.name;
				string[] texturePaths = Directory.GetFiles(textureDirectory, materialName + ".*", SearchOption.TopDirectoryOnly);

				if (texturePaths.Length > 0)
				{
					string texturePath = texturePaths[0]; // Take the first matching texture
					string assetPath = "Assets" + texturePath.Substring(Application.dataPath.Length);

					Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);

					if (texture != null)
					{
						Undo.RecordObject(material, "Assign Texture");
						material.SetTexture("_MainTex", texture);
						Debug.Log($"Assigned texture {texture.name} to material {material.name}");
					}
					else
					{
						Debug.LogWarning($"Failed to load texture at path: {assetPath}");
					}
				}
				else
				{
					Debug.LogWarning($"No texture found for material: {materialName}");
				}
			}
			else
			{
				Debug.LogWarning($"Selected object is not a material: {obj.name}");
			}
		}

		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();
	}
}