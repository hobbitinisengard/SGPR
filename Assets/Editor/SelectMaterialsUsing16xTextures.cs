using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class SelectMaterialsUsing16xTextures
{
    const string TextureFolder = "Assets/Unver/textures/16x/out";

    [MenuItem("Tools/Materials/Select materials using 16x out textures")]
    public static void SelectMaterials()
    {
        if (!AssetDatabase.IsValidFolder(TextureFolder))
        {
            Debug.LogError("Texture folder not found: " + TextureFolder);
            return;
        }

        var matches = new List<UnityEngine.Object>();
        string folderPrefix = TextureFolder + "/";
        string[] materialGuids = AssetDatabase.FindAssets("t:Material", new[] { "Assets" });

        try
        {
            for (int i = 0; i < materialGuids.Length; i++)
            {
                if (i % 64 == 0)
                    EditorUtility.DisplayProgressBar("Finding materials", TextureFolder, i / (float)materialGuids.Length);

                // Include embedded materials, for example those imported with models.
                string materialPath = AssetDatabase.GUIDToAssetPath(materialGuids[i]);
                foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(materialPath))
                {
                    if (!(asset is Material material) || !material.shader) continue;

                    foreach (string property in material.GetTexturePropertyNames())
                    {
                        Texture texture = material.GetTexture(property);
                        if (!texture) continue;

                        string texturePath = AssetDatabase.GetAssetPath(texture);
                        if (!texturePath.StartsWith(folderPrefix, StringComparison.OrdinalIgnoreCase)) continue;

                        matches.Add(material);
                        break;
                    }
                }
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        Selection.objects = matches.ToArray();
        Debug.Log($"Selected {matches.Count} materials using textures from {TextureFolder} (including subfolders).");
    }
}
