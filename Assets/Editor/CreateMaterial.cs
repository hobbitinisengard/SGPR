using System.IO;
using UnityEditor;
using UnityEngine;
 
public static class CreateMaterialFromTexture
{
 
    [MenuItem("Assets/Create Material")]
    public static void CreateDiffuseMaterial()
    {
        var selectedAsset = Selection.GetFiltered(typeof(Object), SelectionMode.DeepAssets);
 
        var cnt = selectedAsset.Length * 1.0f;
        var idx = 0f;
        foreach (Object obj in selectedAsset)
        {
            idx++;
            EditorUtility.DisplayProgressBar("Create material", "Create material for: " + obj.name, idx / cnt);
 
            if (obj is Texture2D)
            {
                CreateMAtFromTx(obj as Texture2D, Shader.Find("Standard"));
            }
        }
        EditorUtility.ClearProgressBar();
    }
 
    private static void CreateMAtFromTx(Texture2D tx2D, Shader shader)
    {
        var path = AssetDatabase.GetAssetPath(tx2D);
        if (File.Exists(path))
        {
            path = Path.GetDirectoryName(path);
        }
 
        var mat = new Material(shader) {mainTexture = tx2D};
        AssetDatabase.CreateAsset(mat, Path.Combine(path, string.Format("{0}.mat", tx2D.name)));
    }
}