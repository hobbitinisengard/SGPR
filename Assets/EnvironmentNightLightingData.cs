using System;
using UnityEngine;

// Referenced lightmaps are included in builds through Resources, even when the
// environment is instantiated dynamically rather than loaded as a baked scene.
public sealed class EnvironmentNightLightingData : ScriptableObject
{
    [Serializable]
    public struct Surface
    {
        public string path; // Sibling indices, so duplicate object names are safe.
        public bool terrain;
        public int lightmap;
        public Vector4 scaleOffset;
    }
    public Texture2D[] lightmaps = Array.Empty<Texture2D>();
    public Surface[] surfaces = Array.Empty<Surface>();
}
