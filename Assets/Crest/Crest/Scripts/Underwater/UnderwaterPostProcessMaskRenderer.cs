// Crest Ocean System for HDRP

// Copyright 2020 Wave Harmonic Ltd

using UnityEngine;
using UnityEngine.Rendering;
using static Crest.UnderwaterPostProcessUtils;

namespace Crest
{
    [RequireComponent(typeof(Camera))]
    internal class UnderwaterPostProcessMaskRenderer : MonoBehaviour
    {
        private Camera _mainCamera;
        private Plane[] _cameraFrustumPlanes;
        private CommandBuffer _maskCommandBuffer;
        private Material _oceanMaskMaterial;
        internal RenderTexture _textureMask;
        internal RenderTexture _depthBuffer;
        private RenderTextureDescriptor _renderTextureDescriptor;
        private BoolParameter _disableOceanMask;
        internal readonly SampleHeightHelper _sampleHeightHelper = new SampleHeightHelper();
        private int _xrPassIndex = -1;

        internal void Initialise(Material oceanMaskMaterial, RenderTextureDescriptor renderTextureDescriptor, BoolParameter disableOceanMask)
        {
            _mainCamera = GetComponent<Camera>();
            _cameraFrustumPlanes = GeometryUtility.CalculateFrustumPlanes(_mainCamera);
            _maskCommandBuffer = new CommandBuffer();
            _maskCommandBuffer.name = "Ocean Mask Command Buffer";
            _oceanMaskMaterial = oceanMaskMaterial;
            _renderTextureDescriptor = renderTextureDescriptor;
            _disableOceanMask = disableOceanMask;
        }

        internal static void BeginCameraRendering(ScriptableRenderContext scriptableRenderContext, Camera camera)
        {
            if (OceanRenderer.Instance == null)
            {
                return;
            }

            UnderwaterPostProcessMaskRenderer maskRenderer = camera.GetComponent<UnderwaterPostProcessMaskRenderer>();
            if (maskRenderer == null)
            {
                return;
            }

            if (XRHelpers.IsRunning)
            {
                XRHelpers.UpdatePassIndex(ref maskRenderer._xrPassIndex);
            }
            else
            {
                maskRenderer._xrPassIndex = 0;
            }

            GeometryUtility.CalculateFrustumPlanes(maskRenderer._mainCamera, maskRenderer._cameraFrustumPlanes);

            // // NOTE: Old RenderTexture way
            // {
            //     maskRenderer._renderTextureDescriptor.width = maskRenderer._mainCamera.pixelWidth;
            //     maskRenderer._renderTextureDescriptor.height = maskRenderer._mainCamera.pixelHeight;
            //     InitialiseMaskTextures(maskRenderer._renderTextureDescriptor, ref maskRenderer._textureMask, ref maskRenderer._depthBuffer);
            // }

            maskRenderer._maskCommandBuffer.Clear();

            // For XR SPI, we draw both eyes at once. Load up both eyes and execute command buffer once.
            for (uint depthSlice = 0; depthSlice < XRHelpers.MaximumViews; ++depthSlice)
            {
                // // NOTE: I don't think we need these yet. It says they are for the new XR SDK.
                // maskRenderer._maskCommandBuffer.EnableShaderKeyword("STEREO_INSTANCING_ON");
                // maskRenderer._maskCommandBuffer.SetInstanceMultiplier(depthSlice);
                PopulateOceanMask(
                    maskRenderer._maskCommandBuffer, maskRenderer._mainCamera, OceanRenderer.Instance.Tiles, maskRenderer._cameraFrustumPlanes,
                    maskRenderer._textureMask, maskRenderer._depthBuffer,
                    maskRenderer._oceanMaskMaterial, (int)depthSlice, maskRenderer._xrPassIndex,
                    maskRenderer._disableOceanMask.value
                );
            }

            scriptableRenderContext.ExecuteCommandBuffer(maskRenderer._maskCommandBuffer);
        }
    }
}
