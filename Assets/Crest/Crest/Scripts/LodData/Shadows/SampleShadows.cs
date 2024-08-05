// Crest Ocean System

// Copyright 2020 Wave Harmonic Ltd

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Crest
{
    class SampleShadows : CustomPass
    {
        static GameObject gameObject;
        static readonly string GameObjectName = "Sample Shadows";
        static readonly string CustomPassName = "Crest Sample Shadows";

        protected override void Execute(ScriptableRenderContext context, CommandBuffer cmd, HDCamera hdCamera, CullingResults cullingResult)
        {
            if (OceanRenderer.Instance == null || OceanRenderer.Instance._lodDataShadow == null) return;

            var camera = hdCamera.camera;

            // Custom passes execute for every camera. We only support one camera for now.
            if (!ReferenceEquals(camera.transform, OceanRenderer.Instance.Viewpoint)) return;
            if (context == null) throw new System.ArgumentNullException("context");
            // TODO: bail when not executing for main light or when no main light exists?
            // if (renderingData.lightData.mainLightIndex == -1) return;
            var commandBuffer = OceanRenderer.Instance._lodDataShadow.BufCopyShadowMap;
            if (commandBuffer == null) return;

            // Target is not multi-eye so stop mult-eye rendering for this command buffer. Breaks registered shadow
            // inputs without this.
            if (camera.stereoEnabled)
            {
                context.StopMultiEye(camera);
            }

            context.ExecuteCommandBuffer(commandBuffer);

            // Even if we do not call StopMultiEye, it is necessary to call StartMultiEye otherwise one eye no longer
            // renders.
            if (camera.stereoEnabled)
            {
                context.StartMultiEye(camera);
            }
            else
            {
                // This is required for URP, but uncertain if required for HDRP. Seems fine without it.
                // // Restore matrices otherwise remaining render will have incorrect matrices. Each pass is responsible
                // // for restoring matrices if required.
                // commandBuffer.Clear();
                // commandBuffer.SetViewProjectionMatrices(camera.worldToCameraMatrix, camera.projectionMatrix);
                // context.ExecuteCommandBuffer(commandBuffer);
            }
        }

        public static void Enable()
        {
            // Find the existing custom pass volume.
            // During recompiles, the reference will be lost so we need to find the game object. It could be limited to
            // the editor if it is safe to do so. The last thing we want is leaking objects.
            if (gameObject == null)
            {
                var transform = OceanRenderer.Instance.transform.Find(GameObjectName);
                if (transform != null)
                {
                    gameObject = transform.gameObject;
                }
            }

            // Create or update the custom pass volume.
            if (gameObject == null)
            {
                gameObject = new GameObject()
                {
                    name = GameObjectName,
                    hideFlags = OceanRenderer.Instance._hideOceanTileGameObjects
                        ? HideFlags.HideAndDontSave : HideFlags.DontSave,
                };
                // Place the custom pass under the ocean renderer since it is easier to find later. Transform.Find can
                // find inactive game objects unlike GameObject.Find.
                gameObject.transform.parent = OceanRenderer.Instance.transform;
                // It appears that this is currently the only way to add a custom pass.
                var volume = gameObject.AddComponent<CustomPassVolume>();
                volume.injectionPoint = CustomPassInjectionPoint.BeforeTransparent;
                volume.isGlobal = true;
                volume.customPasses.Add(new SampleShadows()
                {
                    name = CustomPassName,
                    targetColorBuffer = TargetBuffer.None,
                    targetDepthBuffer = TargetBuffer.None,
                });
            }
            else
            {
                gameObject.hideFlags = OceanRenderer.Instance._hideOceanTileGameObjects
                        ? HideFlags.HideAndDontSave : HideFlags.DontSave;
                gameObject.SetActive(true);
            }

        }

        public static void Disable()
        {
            // It should be safe to rely on this reference for this reference to fail.
            if (gameObject != null)
            {
                gameObject.SetActive(false);
            }
        }
    }
}
