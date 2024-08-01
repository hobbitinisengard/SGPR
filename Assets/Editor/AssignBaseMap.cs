using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering.HighDefinition;
using System.IO;

public class AssignBaseMap : Editor
{
	[MenuItem("Tools/Assign Base Map to HDRP Materials")]
	public static void AssignBaseMapToHDRPMaterials()
	{
		// Find all materials in the project
		string[] guids = AssetDatabase.FindAssets("t:Material");
		foreach (string guid in guids)
		{
			string assetPath = AssetDatabase.GUIDToAssetPath(guid);
			Material material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);

			// Check if the material is using HDRP shader
			if (material.shader.name.Contains("HDRP"))
			{
				// Check if the material has a base map
				if (!material.HasProperty("_BaseColorMap") || material.GetTexture("_BaseColorMap") == null)
				{
					string materialName = material.name;
					if(materialName.Contains('.'))
						materialName = materialName[..materialName.IndexOf('.')];
					string texturePath = FindTexturePath(materialName);

					if (!string.IsNullOrEmpty(texturePath))
					{
						Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
						if (texture != null)
						{
							material.SetTexture("_BaseColorMap", texture);
							EditorUtility.SetDirty(material);
							Debug.Log($"Assigned texture {texture.name} to material {material.name}");
						}
					}
				}
			}
		}

		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();
	}

	private static string FindTexturePath(string textureName)
	{
		// Find all textures in the project
		string[] textureGuids = AssetDatabase.FindAssets("t:Texture2D");
		foreach (string textureGuid in textureGuids)
		{
			string texturePath = AssetDatabase.GUIDToAssetPath(textureGuid);
			string textureFileName = Path.GetFileNameWithoutExtension(texturePath);

			if (textureFileName.Equals(textureName, System.StringComparison.OrdinalIgnoreCase))
			{
				return texturePath;
			}
		}

		Debug.LogWarning($"No texture found with the name {textureName}");
		return null;
	}
}