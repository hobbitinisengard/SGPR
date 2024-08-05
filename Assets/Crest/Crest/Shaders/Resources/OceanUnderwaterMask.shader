// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

Shader "Crest/Underwater/Ocean Mask"
{
	SubShader
	{
		Pass
		{
			// We always disable culling when rendering ocean mask, as we only
			// use it for underwater rendering features.
			Cull Off

			CGPROGRAM
			#pragma vertex Vert
			#pragma fragment Frag
			// for VFACE
			#pragma target 3.0

			struct Attributes
			{
				// The old unity macros require this name and type.
				float4 vertex : POSITION;
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
			};

			#include "../OceanConstants.hlsl"
			#include "../OceanInputsDriven.hlsl"
			#include "../OceanGlobals.hlsl"
			#include "../ShadergraphFramework/CrestNodeDrivenInputs.hlsl"
			#include "../OceanHelpersNew.hlsl"
			#include "../OceanVertHelpers.hlsl"

			// Hack - due to SV_IsFrontFace occasionally coming through as true for backfaces,
			// add a param here that forces ocean to be in undrwater state. I think the root
			// cause here might be imprecision or numerical issues at ocean tile boundaries, although
			// i'm not sure why cracks are not visible in this case.
			float _ForceUnderwater;

			Varyings Vert(Attributes v)
			{
				Varyings output;

				float3 worldPos = mul(unity_ObjectToWorld, float4(v.vertex.xyz, 1.0));

				float meshScaleAlpha;
				float lodDataTexelSize;
				float geometryGridSize;
				float3 oceanPosScale0;
				float3 oceanPosScale1;
				float4 oceanParams0;
				float4 oceanParams1;
				float sliceIndex0;
				CrestOceanSurfaceValues_half
				(
					meshScaleAlpha,
					lodDataTexelSize,
					geometryGridSize,
					oceanPosScale0,
					oceanPosScale1,
					oceanParams0,
					oceanParams1,
					sliceIndex0
				);

				const CascadeParams cascadeData0 = _CrestCascadeData[sliceIndex0];
				const CascadeParams cascadeData1 = _CrestCascadeData[sliceIndex0 + 1];
				const PerCascadeInstanceData instanceData = _CrestPerCascadeInstanceData[sliceIndex0];

				// Vertex snapping and lod transition
				float lodAlpha;
				const float meshScaleLerp = instanceData._meshScaleLerp;
				const float gridSize = instanceData._geoGridWidth;
				SnapAndTransitionVertLayout(meshScaleAlpha, oceanPosScale0, geometryGridSize, worldPos, lodAlpha);

				// Calculate sample weights. params.z allows shape to be faded out (used on last lod to support pop-less scale transitions)
				const float wt_smallerLod = (1. - lodAlpha) * cascadeData0._weight;
				const float wt_biggerLod = (1. - wt_smallerLod) * cascadeData1._weight;
				// Sample displacement textures, add results to current world pos / normal / foam
				const float2 positionWS_XZ_before = worldPos.xz;

				// Data that needs to be sampled at the undisplaced position
				if (wt_smallerLod > 0.001)
				{
					half sss = 0.0;
					const float3 uv_slice_smallerLod = WorldToUV(positionWS_XZ_before, cascadeData0, sliceIndex0);
					SampleDisplacements(_LD_TexArray_AnimatedWaves, uv_slice_smallerLod, wt_smallerLod, worldPos, sss);
				}
				if (wt_biggerLod > 0.001)
				{
					half sss = 0.0;
					const float3 uv_slice_biggerLod = WorldToUV(positionWS_XZ_before, cascadeData1, sliceIndex0 + 1);
					SampleDisplacements(_LD_TexArray_AnimatedWaves, uv_slice_biggerLod, wt_biggerLod, worldPos, sss);
				}

				output.positionCS = mul(UNITY_MATRIX_VP, float4(worldPos, 1.0));
				return output;
			}


			bool IsUnderwater(in const float facing, in const float forceUnderwater)
			{
				const bool backface = facing < 0.0;
				return backface || forceUnderwater > 0.0;
			}

			half4 Frag(const Varyings input, const float facing : VFACE) : SV_Target
			{
				if (IsUnderwater(facing, _ForceUnderwater))
				{
					return (half4)UNDERWATER_MASK_WATER_SURFACE_BELOW;
				}
				else
				{
					return (half4)UNDERWATER_MASK_WATER_SURFACE_ABOVE;
				}
			}
			ENDCG
		}
	}
}
