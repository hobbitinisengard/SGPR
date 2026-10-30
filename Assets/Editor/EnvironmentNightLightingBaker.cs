using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

// Baking is explicit: never start expensive GI work on script import or Play Mode.
public static class EnvironmentNightLightingBaker
{
    const string Prefabs = "Assets/Resources/envirs/";
    const string DataFolder = Prefabs + "Lighting";
    const string SceneFolder = "Assets/EnvironmentLightingBakeScenes";
    static readonly string[] Countries = { "ENG", "FRA", "GER", "ITA", "JAP", "MEX", "SPN", "USA" };
    static Queue<string> pending;
    static SceneSetup[] oldScenes;
    static GameObject current;
    static string country;

    [MenuItem("Tools/Environment lighting/Prepare night prefabs")]
    public static void PrepareAll()
    {
        foreach (string name in Countries)
        {
            string path = Prefabs + name + ".prefab";
            PrepareImportedUVs(AssetDatabase.LoadAssetAtPath<GameObject>(path));
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                Prepare(root);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        AssetDatabase.SaveAssets();
        Debug.Log("Night environment prefabs prepared. Use Tools/Environment lighting/Bake all night environments to generate lightmaps.");
    }
    static void Prepare(GameObject root)
    {
        foreach (var light in root.GetComponentsInChildren<Light>(true))
            light.lightmapBakeType = LightmapBakeType.Baked;
        foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>(true))
        {
            var filter = renderer.GetComponent<MeshFilter>();
            if (!filter || !filter.sharedMesh) continue;
            // Animated decoration must stay dynamic.
            if (renderer.GetComponentInParent<Animator>() || renderer.GetComponentInParent<Animation>()) continue;
            GameObjectUtility.SetStaticEditorFlags(renderer.gameObject,
                GameObjectUtility.GetStaticEditorFlags(renderer.gameObject) | StaticEditorFlags.ContributeGI);
            renderer.receiveGI = ReceiveGI.Lightmaps;
            renderer.scaleInLightmap = .25f;
        }
        foreach (var terrain in root.GetComponentsInChildren<Terrain>(true))
            GameObjectUtility.SetStaticEditorFlags(terrain.gameObject,
                GameObjectUtility.GetStaticEditorFlags(terrain.gameObject) | StaticEditorFlags.ContributeGI);
    }
    static void PrepareImportedUVs(GameObject prefab)
    {
        var paths = new HashSet<string>();
        foreach (var filter in prefab.GetComponentsInChildren<MeshFilter>(true))
            if (filter.sharedMesh) paths.Add(AssetDatabase.GetAssetPath(filter.sharedMesh));
        // Reimport before opening prefab contents: reimports can invalidate model instances.
        foreach (string path in paths)
            if (AssetImporter.GetAtPath(path) is ModelImporter importer && !importer.generateSecondaryUV)
            {
                importer.generateSecondaryUV = true;
                importer.SaveAndReimport();
            }
    }
    [MenuItem("Tools/Environment lighting/Bake all night environments")]
    public static void BakeAll()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || Lightmapping.isRunning || pending != null)
        {
            Debug.LogWarning("Stop Play Mode or the current lighting bake first.");
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        PrepareAll();
        Directory.CreateDirectory(DataFolder);
        Directory.CreateDirectory(SceneFolder);
        AssetDatabase.Refresh();
        oldScenes = EditorSceneManager.GetSceneManagerSetup();
        pending = new Queue<string>(Countries);
        Lightmapping.bakeCompleted += Completed;
        Lightmapping.bakeCancelled += Cancelled;
        Next();
    }
    static void Next()
    {
        if (pending == null) return;
        try
        {
            if (pending.Count == 0) { Finish(); return; }
            country = pending.Dequeue();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            current = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + country + ".prefab"));
            var lights = current.transform.Find("Lights");
            if (lights) lights.gameObject.SetActive(true);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color32(100, 98, 86, 255);
            var settings = new LightingSettings
            {
                bakedGI = true,
                realtimeGI = false,
                directionalityMode = LightmapsMode.NonDirectional,
                lightmapResolution = 2,
                lightmapMaxSize = 2048,
                directSampleCount = 32,
                indirectSampleCount = 64
            };
            string settingsPath = SceneFolder + "/" + country + "_Night.lighting";
            var existing = AssetDatabase.LoadAssetAtPath<LightingSettings>(settingsPath);
            if (existing)
            {
                EditorUtility.CopySerialized(settings, existing);
                UnityEngine.Object.DestroyImmediate(settings);
                settings = existing;
            }
            else AssetDatabase.CreateAsset(settings, settingsPath);
            Lightmapping.lightingSettings = settings;
            EditorSceneManager.SaveScene(scene, SceneFolder + "/" + country + "_Night.unity");
            Debug.Log("Baking night environment: " + country);
            if (!Lightmapping.BakeAsync()) throw new InvalidOperationException("Unity could not start the lightmap bake.");
        }
        catch (Exception exception) { Debug.LogException(exception); Finish(); }
    }
    static string ObjectPath(Transform target)
    {
        var parts = new List<string>();
        while (target != current.transform)
        {
            parts.Add(target.GetSiblingIndex().ToString());
            target = target.parent;
        }
        parts.Reverse();
        return string.Join("/", parts);
    }
    static void Completed()
    {
        if (pending == null || !current) return;
        try
        {
            var maps = LightmapSettings.lightmaps;
            if (maps.Length == 0) throw new InvalidOperationException("Bake produced no lightmaps for " + country);
            string path = DataFolder + "/" + country + "_Night.asset";
            var data = AssetDatabase.LoadAssetAtPath<EnvironmentNightLightingData>(path);
            if (!data)
            {
                data = ScriptableObject.CreateInstance<EnvironmentNightLightingData>();
                AssetDatabase.CreateAsset(data, path);
            }
            data.lightmaps = Array.ConvertAll(maps, map => map.lightmapColor);
            var surfaces = new List<EnvironmentNightLightingData.Surface>();
            foreach (var renderer in current.GetComponentsInChildren<MeshRenderer>(true))
                if (renderer.lightmapIndex >= 0 && renderer.lightmapIndex < maps.Length)
                    surfaces.Add(new EnvironmentNightLightingData.Surface { path = ObjectPath(renderer.transform),
                        lightmap = renderer.lightmapIndex, scaleOffset = renderer.lightmapScaleOffset });
            foreach (var terrain in current.GetComponentsInChildren<Terrain>(true))
                if (terrain.lightmapIndex >= 0 && terrain.lightmapIndex < maps.Length)
                    surfaces.Add(new EnvironmentNightLightingData.Surface { path = ObjectPath(terrain.transform), terrain = true,
                        lightmap = terrain.lightmapIndex, scaleOffset = terrain.lightmapScaleOffset });
            data.surfaces = surfaces.ToArray();
            EditorUtility.SetDirty(data);
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            // Leave Unity's bake callback before loading the next scene.
            EditorApplication.delayCall += Next;
        }
        catch (Exception exception) { Debug.LogException(exception); Finish(); }
    }
    static void Cancelled()
    {
        Debug.LogWarning("Night environment bake cancelled; remaining environments have not been baked.");
        Finish();
    }
    static void Finish()
    {
        Lightmapping.bakeCompleted -= Completed;
        Lightmapping.bakeCancelled -= Cancelled;
        EditorApplication.delayCall -= Next;
        pending = null;
        current = null;
        if (oldScenes != null) EditorSceneManager.RestoreSceneManagerSetup(oldScenes);
        oldScenes = null;
        Debug.Log("Night environment baking finished. Generated data is in Resources/envirs/Lighting.");
    }
}
