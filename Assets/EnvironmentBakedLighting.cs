using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class EnvironmentBakedLighting : MonoBehaviour
{
    EnvironmentNightLightingData data;
    LightmapData[] previousMaps;
    LightmapsMode previousMode;
    static EnvironmentBakedLighting active;
    bool missingDataReported;
    readonly List<Binding> bindings = new();
    struct Binding
    {
        public Renderer renderer;
        public Terrain terrain;
        public int oldIndex;
        public Vector4 oldOffset;
        public EnvironmentNightLightingData.Surface surface;
    }

    public static void Attach(GameObject environment, string country)
    {
        var lighting = environment.AddComponent<EnvironmentBakedLighting>();
        lighting.data = Resources.Load<EnvironmentNightLightingData>("envirs/Lighting/" + country + "_Night");
        // Baked light components must never fall back to realtime after loading.
        foreach (var light in environment.GetComponentsInChildren<Light>(true))
            light.lightmapBakeType = LightmapBakeType.Baked;
    }
    public void SetNight(bool night)
    {
        if (!night) { Restore(); return; }
        if (active == this) return;
        if (!data || data.lightmaps.Length == 0)
        {
            if (!missingDataReported)
            {
                Debug.LogWarning("Night lightmaps are missing for " + name +
                    ". Run Tools/Environment lighting/Bake all night environments in Unity.", this);
                missingDataReported = true;
            }
            return;
        }
        if (active) active.Restore();
        previousMaps = LightmapSettings.lightmaps;
        previousMode = LightmapSettings.lightmapsMode;
        var maps = new LightmapData[previousMaps.Length + data.lightmaps.Length];
        previousMaps.CopyTo(maps, 0);
        for (int i = 0; i < data.lightmaps.Length; i++)
            maps[previousMaps.Length + i] = new LightmapData { lightmapColor = data.lightmaps[i] };
        LightmapSettings.lightmapsMode = LightmapsMode.NonDirectional;
        LightmapSettings.lightmaps = maps;
        active = this;
        foreach (var surface in data.surfaces)
        {
            if (surface.lightmap < 0 || surface.lightmap >= data.lightmaps.Length) continue;
            Transform target = Resolve(transform, surface.path);
            if (!target) continue;
            var binding = new Binding { surface = surface };
            int index = previousMaps.Length + surface.lightmap;
            if (surface.terrain)
            {
                binding.terrain = target.GetComponent<Terrain>();
                if (!binding.terrain) continue;
                binding.oldIndex = binding.terrain.lightmapIndex;
                binding.oldOffset = binding.terrain.lightmapScaleOffset;
                binding.terrain.lightmapIndex = index;
                binding.terrain.lightmapScaleOffset = surface.scaleOffset;
            }
            else
            {
                binding.renderer = target.GetComponent<MeshRenderer>();
                if (!binding.renderer) continue;
                binding.oldIndex = binding.renderer.lightmapIndex;
                binding.oldOffset = binding.renderer.lightmapScaleOffset;
                binding.renderer.lightmapIndex = index;
                binding.renderer.lightmapScaleOffset = surface.scaleOffset;
            }
            bindings.Add(binding);
        }
    }
    void Restore()
    {
        foreach (var binding in bindings)
        {
            if (binding.renderer)
            {
                binding.renderer.lightmapIndex = binding.oldIndex;
                binding.renderer.lightmapScaleOffset = binding.oldOffset;
            }
            if (binding.terrain)
            {
                binding.terrain.lightmapIndex = binding.oldIndex;
                binding.terrain.lightmapScaleOffset = binding.oldOffset;
            }
        }
        bindings.Clear();
        if (active != this) return;
        LightmapSettings.lightmaps = previousMaps;
        LightmapSettings.lightmapsMode = previousMode;
        previousMaps = null;
        active = null;
    }
    static Transform Resolve(Transform root, string path)
    {
        if (string.IsNullOrEmpty(path)) return root;
        foreach (string segment in path.Split('/'))
        {
            if (!int.TryParse(segment, out int index) || index < 0 || index >= root.childCount) return null;
            root = root.GetChild(index);
        }
        return root;
    }
    void OnDisable() => Restore();
}
