// Crest Ocean System for HDRP

// Copyright 2020 Wave Harmonic Ltd

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR;

namespace Crest
{
    using SettingsType = SimSettingsShadow;

    /// <summary>
    /// Stores shadowing data to use during ocean shading. Shadowing is persistent and supports sampling across
    /// many frames and jittered sampling for (very) soft shadows.
    /// </summary>
    public class LodDataMgrShadow : LodDataMgr
    {
        public override string SimName { get { return "Shadow"; } }
        public override RenderTextureFormat TextureFormat { get { return RenderTextureFormat.RG16; } }
        protected override bool NeedToReadWriteTextureData { get { return true; } }

        public static bool s_processData = true;

        Light _mainLight;
        Camera _cameraMain;

        // SRP version needs access to this externally, hence public get
        public CommandBuffer BufCopyShadowMap { get; private set; }

        RenderTexture _sources;
        PropertyWrapperMaterial[] _renderMaterial;

        readonly int sp_CenterPos = Shader.PropertyToID("_CenterPos");
        readonly int sp_Scale = Shader.PropertyToID("_Scale");
        readonly int sp_JitterDiameters_CurrentFrameWeights = Shader.PropertyToID("_JitterDiameters_CurrentFrameWeights");
        readonly int sp_MainCameraProjectionMatrix = Shader.PropertyToID("_MainCameraProjectionMatrix");
        readonly int sp_SimDeltaTime = Shader.PropertyToID("_SimDeltaTime");
        readonly int sp_LD_SliceIndex_Source = Shader.PropertyToID("_LD_SliceIndex_Source");
        readonly int sp_cascadeDataSrc = Shader.PropertyToID("_CascadeDataSrc");

        SettingsType _defaultSettings;
        public SettingsType Settings
        {
            get
            {
                if (_ocean._simSettingsShadow != null) return _ocean._simSettingsShadow;

                if (_defaultSettings == null)
                {
                    _defaultSettings = ScriptableObject.CreateInstance<SettingsType>();
                    _defaultSettings.name = SimName + " Auto-generated Settings";
                }
                return _defaultSettings;
            }
        }

        public LodDataMgrShadow(OceanRenderer ocean) : base(ocean)
        {
            Start();
        }

        public override void Start()
        {
            base.Start();

            {
                _renderMaterial = new PropertyWrapperMaterial[OceanRenderer.Instance.CurrentLodCount];
                var shader = Shader.Find("Hidden/Crest/Simulation/Update Shadow");
                for (int i = 0; i < _renderMaterial.Length; i++)
                {
                    _renderMaterial[i] = new PropertyWrapperMaterial(shader);
                }
            }

            // Enable sample shadows custom pass.
            SampleShadows.Enable();

            // Setup the camera.
            UpdateCameraMain();
        }

        protected override void InitData()
        {
            base.InitData();

            int resolution = OceanRenderer.Instance.LodDataResolution;
            var desc = new RenderTextureDescriptor(resolution, resolution, TextureFormat, 0);
            _sources = CreateLodDataTextures(desc, SimName + "_1", NeedToReadWriteTextureData);

            TextureArrayHelpers.ClearToBlack(_sources);
            TextureArrayHelpers.ClearToBlack(_targets);
        }

        bool StartInitLight()
        {
            if (_mainLight == null)
            {
                _mainLight = OceanRenderer.Instance._primaryLight;

                if (_mainLight == null)
                {
                    if (!Settings._allowNullLight)
                    {
                        Debug.LogWarning("Primary light must be specified on OceanRenderer script to enable shadows.", OceanRenderer.Instance);
                    }
                    return false;
                }

                if (_mainLight.type != LightType.Directional)
                {
                    Debug.LogError("Primary light must be of type Directional.", OceanRenderer.Instance);
                    return false;
                }

                if (_mainLight.shadows == LightShadows.None)
                {
                    Debug.LogError("Shadows must be enabled on primary light to enable ocean shadowing (types Hard and Soft are equivalent for the ocean system).", OceanRenderer.Instance);
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// May happen if scenes change etc
        /// </summary>
        void ClearBufferIfLightChanged()
        {
            if (_mainLight != OceanRenderer.Instance._primaryLight)
            {
                if (_mainLight)
                {
                    BufCopyShadowMap = null;
                    TextureArrayHelpers.ClearToBlack(_sources);
                    TextureArrayHelpers.ClearToBlack(_targets);
                }
                _mainLight = null;
            }
        }

        public override void UpdateLodData()
        {
            if (!enabled)
            {
                return;
            }

            base.UpdateLodData();

            ClearBufferIfLightChanged();

            if (!StartInitLight())
            {
                enabled = false;
                return;
            }

            if (!s_processData)
            {
                return;
            }

            if (BufCopyShadowMap == null)
            {
                BufCopyShadowMap = new CommandBuffer();
                BufCopyShadowMap.name = "Shadow data";
            }

            if (!s_processData)
            {
                return;
            }

            // Update the camera if it has changed.
            if (_cameraMain.transform != OceanRenderer.Instance.Viewpoint)
            {
                UpdateCameraMain();
            }

            Swap(ref _sources, ref _targets);

            BufCopyShadowMap.Clear();

            ValidateSourceData();

            // clear the shadow collection. it will be overwritten with shadow values IF the shadows render,
            // which only happens if there are (nontransparent) shadow receivers around. this is only reliable
            // in play mode, so don't do it in edit mode.
#if UNITY_EDITOR
            if (UnityEditor.EditorApplication.isPlaying)
#endif
            {
                TextureArrayHelpers.ClearToBlack(_targets);
            }

            // TODO - this is in SRP, so i can't ifdef it? what is a good plan here - wait for it to be removed completely?
#pragma warning disable 618
            using (new ProfilingSample(BufCopyShadowMap, "CrestSampleShadows"))
#pragma warning restore 618
            {
                var lt = OceanRenderer.Instance._lodTransform;
                for (var lodIdx = lt.LodCount - 1; lodIdx >= 0; lodIdx--)
                {
#if UNITY_EDITOR
                    lt._renderData[lodIdx].Validate(0, SimName);
#endif

                    _renderMaterial[lodIdx].SetVector(sp_CenterPos, lt._renderData[lodIdx]._posSnapped);
                    var scale = OceanRenderer.Instance.CalcLodScale(lodIdx);
                    _renderMaterial[lodIdx].SetVector(sp_Scale, new Vector3(scale, 1f, scale));
                    _renderMaterial[lodIdx].SetVector(sp_JitterDiameters_CurrentFrameWeights, new Vector4(Settings._jitterDiameterSoft, Settings._jitterDiameterHard, Settings._currentFrameWeightSoft, Settings._currentFrameWeightHard));
                    _renderMaterial[lodIdx].SetMatrix(sp_MainCameraProjectionMatrix, _cameraMain.projectionMatrix * _cameraMain.worldToCameraMatrix);
                    _renderMaterial[lodIdx].SetFloat(sp_SimDeltaTime, Time.deltaTime);

                    // compute which lod data we are sampling previous frame shadows from. if a scale change has happened this can be any lod up or down the chain.
                    var srcDataIdx = lodIdx + ScaleDifferencePow2;
                    srcDataIdx = Mathf.Clamp(srcDataIdx, 0, lt.LodCount - 1);
                    _renderMaterial[lodIdx].SetFloat(sp_LD_SliceIndex, lodIdx);
                    _renderMaterial[lodIdx].SetFloat(sp_LD_SliceIndex_Source, srcDataIdx);
                    _renderMaterial[lodIdx].SetTexture(GetParamIdSampler(true), _sources);
                    _renderMaterial[lodIdx].SetBuffer(sp_cascadeDataSrc, OceanRenderer.Instance._bufCascadeDataSrc);

                    BufCopyShadowMap.Blit(Texture2D.blackTexture, _targets, _renderMaterial[lodIdx].material, -1, lodIdx);
                }

                // Disable single pass double-wide stereo rendering for these commands since we are rendering to
                // rendering texture. Otherwise, it will render double. Single pass instanced is broken here, but that
                // appears to be a Unity bug only for the legacy VR system.
                if (_cameraMain.stereoEnabled && XRSettings.stereoRenderingMode == XRSettings.StereoRenderingMode.SinglePass)
                {
                    BufCopyShadowMap.SetSinglePassStereo(SinglePassStereoMode.None);
                    BufCopyShadowMap.DisableShaderKeyword("UNITY_SINGLE_PASS_STEREO");
                }

                // Process registered inputs.
                for (var lodIdx = lt.LodCount - 1; lodIdx >= 0; lodIdx--)
                {
                    BufCopyShadowMap.SetRenderTarget(_targets, _targets.depthBuffer, 0, CubemapFace.Unknown, lodIdx);
                    SubmitDraws(lodIdx, BufCopyShadowMap);
                }

                // Restore single pass double-wide as we cannot rely on remaining pipeline to do it for us.
                if (_cameraMain.stereoEnabled && XRSettings.stereoRenderingMode == XRSettings.StereoRenderingMode.SinglePass)
                {
                    BufCopyShadowMap.SetSinglePassStereo(SinglePassStereoMode.SideBySide);
                    BufCopyShadowMap.EnableShaderKeyword("UNITY_SINGLE_PASS_STEREO");
                }

                // Set the target texture as to make sure we catch the 'pong' each frame
                Shader.SetGlobalTexture(GetParamIdSampler(), _targets);
            }
        }

        void UpdateCameraMain()
        {
            var viewpoint = OceanRenderer.Instance.Viewpoint;
            _cameraMain = viewpoint != null ? viewpoint.GetComponent<Camera>() : null;

            if (_cameraMain == null)
            {
                Debug.LogError("Could not find main camera, disabling shadow data", _ocean);
                enabled = false;
                return;
            }
        }

        public void ValidateSourceData()
        {
#if UNITY_EDITOR
            if (!UnityEditor.EditorApplication.isPlaying)
            {
                // Don't validate when not in play mode in editor as shadows won't be updating.
                return;
            }
#endif

            foreach (var renderData in OceanRenderer.Instance._lodTransform._renderDataSource)
            {
                renderData.Validate(BuildCommandBufferBase._lastUpdateFrame - OceanRenderer.FrameCount, SimName);
            }
        }

        internal override void OnEnable()
        {
            base.OnEnable();

            RemoveCommandBuffers();
            SampleShadows.Enable();
        }

        internal override void OnDisable()
        {
            base.OnDisable();

            RemoveCommandBuffers();
            SampleShadows.Disable();
        }

        void RemoveCommandBuffers()
        {
            if (BufCopyShadowMap != null)
            {
                if (_mainLight)
                {
                    _mainLight.RemoveCommandBuffer(LightEvent.BeforeScreenspaceMask, BufCopyShadowMap);
                }
                BufCopyShadowMap = null;
            }
        }

        readonly static string s_textureArrayName = "_LD_TexArray_Shadow";
        private static TextureArrayParamIds s_textureArrayParamIds = new TextureArrayParamIds(s_textureArrayName);
        public static int ParamIdSampler(bool sourceLod = false) { return s_textureArrayParamIds.GetId(sourceLod); }

        protected override int GetParamIdSampler(bool sourceLod = false)
        {
            return ParamIdSampler(sourceLod);
        }

        public static void Bind(IPropertyWrapper properties)
        {
            if (OceanRenderer.Instance._lodDataShadow != null)
            {
                properties.SetTexture(OceanRenderer.Instance._lodDataShadow.GetParamIdSampler(), OceanRenderer.Instance._lodDataShadow.DataTexture);
            }
            else
            {
                properties.SetTexture(ParamIdSampler(), TextureArrayHelpers.BlackTextureArray);
            }
        }

#if UNITY_2019_3_OR_NEWER
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
#endif
        static void InitStatics()
        {
            // Init here from 2019.3 onwards
            s_textureArrayParamIds = new TextureArrayParamIds(s_textureArrayName);
        }
    }
}
