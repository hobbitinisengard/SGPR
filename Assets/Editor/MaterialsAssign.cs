using UnityEngine;
using UnityEditor;
using System.IO;

public class AssignMaterials : EditorWindow
{
	[MenuItem("Tools/Assign Materials")]
	static void ShowWindow()
	{
		EditorWindow.GetWindow(typeof(AssignMaterials));
	}

	void OnGUI()
	{
		GUILayout.Label("Assign Materials", EditorStyles.boldLabel);

		if (GUILayout.Button("Assign Materials"))
		{
			AssignMaterialsToModels();
		}
	}

	void AssignMaterialsToModels()
	{
		// Directory path where your .blend models are located
		string modelsDirectory = "Assets/Resources/materialize";

		// Get all .blend model files in the specified directory
		string[] blendModelFiles = Directory.GetFiles(modelsDirectory, "*.blend", SearchOption.AllDirectories);

		foreach (string blendModelFile in blendModelFiles)
		{
			// Load the model into a GameObject
			GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(blendModelFile);

			if (model != null)
			{
				// Iterate through all the materials in the model
				Renderer renderer = model.GetComponent<Renderer>();
				foreach (Material originalMaterial in renderer.sharedMaterials)
				{
					// Material name to search for or create
					string materialName = originalMaterial.name;

					// Try to find an existing material with the same name
					Material newMaterial = AssetDatabase.LoadAssetAtPath<Material>(
						"Assets/Resources/materialize/" + materialName + ".mat");

					// If the material doesn't exist, create a new one
					if (newMaterial != null)
					{
						renderer.sharedMaterial = newMaterial;
					}
				}
			}
		}

		Debug.Log("Material assignment completed.");
	}
}