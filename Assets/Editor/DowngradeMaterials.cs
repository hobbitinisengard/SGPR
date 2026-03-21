using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class DowngradeMaterials : EditorWindow
{
	[MenuItem("Tools/HDRP to Built-in/Lit --> Standard")]
	static void DowngradeLitMaterials()
	{
		Undo.RegisterCompleteObjectUndo(Selection.objects, "UndoDowngrade");

		Object[] selectedObjects = Selection.objects;

		List<Material> hdMaterials = new List<Material>();

		for (int i = 0; i < selectedObjects.Length; i++)
		{
			if (selectedObjects[i] is Material)
			{
				hdMaterials.Add(selectedObjects[i] as Material);
			}
		}

		for (int i = 0; i < hdMaterials.Count; i++)
		{
			//Get render type Opaque = 0, Transparent = 1
			float surfaceType = hdMaterials[i].GetFloat("_SurfaceType");

			//Get Normal map
			Texture normalMap = hdMaterials[i].GetTexture("_NormalMap");

			//Get Mask Map - Metallic(R) - Ambient Occlusion(G) - Detail Mask(B) - Smoothness(A)
			Texture maskMap = hdMaterials[i].GetTexture("_MaskMap");

			//Get Emission Map
			Texture emissionMap = hdMaterials[i].GetTexture("_EmissiveColorMap");

			//Get Detail Map
			Texture detailMap = hdMaterials[i].GetTexture("_DetailMap");

			//Get main texture tiling and offset
			Vector2 first_UV_Tiling = hdMaterials[i].GetTextureScale("_BaseColorMap");
			Vector2 first_UV_OffSet = hdMaterials[i].GetTextureOffset("_BaseColorMap");

			//Get detail texture tiling and offset
			Vector2 second_UV_Tiling = hdMaterials[i].GetTextureScale("_DetailMap");
			Vector2 second_UV_OffSet = hdMaterials[i].GetTextureOffset("_DetailMap");


			hdMaterials[i].shader = Shader.Find("Standard");


			//Set render type
			if (surfaceType == 1)
			{
				//Opaque = 0, Cutout = 1, Fade = 2, Transparent = 3
				hdMaterials[i].SetFloat("_Mode", 3.0f);
			}

			//Set Normal map
			hdMaterials[i].SetTexture("_BumpMap", normalMap);

			//Set Metallic map
			hdMaterials[i].SetTexture("_MetallicGlossMap", maskMap);

			//Set Occlusion map
			hdMaterials[i].SetTexture("_OcclusionMap", maskMap);

			if (emissionMap != null)
			{
				//Set Occlusion map
				hdMaterials[i].SetTexture("_EmissionMap", emissionMap);

				hdMaterials[i].globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;

				hdMaterials[i].EnableKeyword("_EMISSION");
			}


			//Set Detail Map
			hdMaterials[i].SetTexture("_DetailAlbedoMap", detailMap);

			//Set main texture tiling and offset
			hdMaterials[i].SetTextureScale("_MainTex", first_UV_Tiling);
			hdMaterials[i].SetTextureOffset("_MainTex", first_UV_OffSet);

			//Set detail texture tiling and offset
			hdMaterials[i].SetTextureScale("_DetailAlbedoMap", second_UV_Tiling);
			hdMaterials[i].SetTextureOffset("_DetailAlbedoMap", second_UV_OffSet);
		}
	}
}