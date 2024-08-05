Shader "Hidden/Crest/Underwater/Post Process HDRP"
{
	SubShader
	{
		Pass
		{
			Name "UnderwaterPostProcessHDRP"

			ZWrite Off
			ZTest Always
			Blend Off
			Cull Off

			HLSLPROGRAM

			#pragma fragment Frag
			#pragma vertex Vert

			// Use multi_compile because these keywords are copied over from the ocean material. With shader_feature,
			// the keywords would be stripped from builds. Unused shader variants are stripped using a build processor.
			// #pragma multi_compile_local __ CREST_SUBSURFACESCATTERING_ON
			// #pragma multi_compile_local __ CREST_SUBSURFACESHALLOWCOLOUR_ON
			// #pragma multi_compile_local __ CREST_TRANSPARENCY_ON
			#pragma multi_compile_local __ CREST_CAUSTICS_ON
			#pragma multi_compile_local __ CREST_FLOW_ON
			#pragma multi_compile_local __ CREST_FOAM_ON
			// #pragma multi_compile_local __ CREST_SHADOWS_ON
			#pragma multi_compile_local __ CREST_COMPILESHADERWITHDEBUGINFO_ON

			#pragma shader_feature_local CREST_MENISCUS_ON

			#pragma multi_compile_local __ _FULL_SCREEN_EFFECT
			#pragma multi_compile_local __ _DEBUG_VIEW_OCEAN_MASK

			#if _COMPILESHADERWITHDEBUGINFO_ON
			#pragma enable_d3d11_debug_symbols
			#endif

			#pragma target 4.5
			#pragma only_renderers d3d11 ps4 xboxone vulkan metal switch

			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/PostProcessing/Shaders/FXAA.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/PostProcessing/Shaders/RTUpscale.hlsl"

			#include "../OceanConstants.hlsl"
			#include "../OceanInputsDriven.hlsl"
			#include "../OceanGlobals.hlsl"
			#include "../OceanHelpersNew.hlsl"
			#include "../ShadergraphFramework/CrestNodeDrivenInputs.hlsl"
			#include "../ShadergraphFramework/CrestNodeLightWaterVolume.hlsl"
			#include "../ShadergraphFramework/CrestNodeApplyCaustics.hlsl"
			#include "../ShadergraphFramework/CrestNodeAmbientLight.hlsl"

			float _OceanHeight;
			float4x4 _InvViewProjection;
			float4x4 _InvViewProjectionRight;
			float4 _HorizonPosNormal;
			float4 _HorizonPosNormalRight;
			int _DataSliceOffset;

			half4 _ScatterColourBase;
			half3 _ScatterColourShadow;
			float4 _ScatterColourShallow;
			half _ScatterColourShallowDepthMax;
			half _ScatterColourShallowDepthFalloff;

			half _SSSIntensityBase;
			half _SSSIntensitySun;
			half4 _SSSTint;
			half _SSSSunFalloff;

			half3 _AmbientLighting;

			half3 _DepthFogDensity;

			float _CausticsTextureScale;
			float _CausticsTextureAverage;
			float _CausticsStrength;
			float _CausticsFocalDepth;
			float _CausticsDepthOfField;
			float _CausticsDistortionStrength;
			float _CausticsDistortionScale;


			struct Attributes
			{
				uint vertexID : SV_VertexID;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float2 uv         : TEXCOORD0;
				float3 viewWS     : TEXCOORD1;
				UNITY_VERTEX_OUTPUT_STEREO
			};

			Varyings Vert(Attributes input)
			{
				Varyings output;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
				output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
				output.uv = GetFullScreenTriangleTexCoord(input.vertexID);

				// Compute world space view vector
				{
					const float2 pixelCS = output.uv * 2 - float2(1.0, 1.0);
#if CREST_HANDLE_XR
					const float4x4 InvViewProjection = unity_StereoEyeIndex == 0 ? _InvViewProjection : _InvViewProjectionRight;
#else
					const float4x4 InvViewProjection = _InvViewProjection;
#endif
					const float4 pixelWS_H = mul(InvViewProjection, float4(pixelCS, 1.0, 1.0));
					const float3 pixelWS = pixelWS_H.xyz / pixelWS_H.w;
					output.viewWS = _WorldSpaceCameraPos - pixelWS;
				}

				return output;
			}

			// List of properties to control your post process effect
			float _Intensity;
			TEXTURE2D_X(_MainTex);
			TEXTURE2D_X(_CrestOceanMaskTexture);
			TEXTURE2D_X(_CrestOceanMaskDepthTexture);
			TEXTURE2D_X(_CrestCameraDepthTexture);

			TEXTURE2D(_CausticsTexture); SAMPLER(sampler_CausticsTexture); float4 _CausticsTexture_TexelSize;
			TEXTURE2D(_CausticsDistortionTexture); SAMPLER(sampler_CausticsDistortionTexture); float4 _CausticsDistortionTexture_TexelSize;

			float LinearToDeviceDepth(float linearDepth, float4 zBufferParam)
			{
				//linear = 1.0 / (zBufferParam.z * device + zBufferParam.w);
				float device = (1.0 / linearDepth - zBufferParam.w) / zBufferParam.z;
				return device;
			}

			half3 ApplyUnderwaterEffect(
				half3 sceneColour, const float sceneZ01,
				const half3 view,
				const float2 screenPos,
				bool isOceanSurface
			) {
				const bool isUnderwater = true;
				const float sceneZ = LinearEyeDepth(sceneZ01, _ZBufferParams);

				half3 volumeLight = 0.0;
				float3 displacement = 0.0;
				{
					half sss = 0.0;
					// Offset slice so that we dont get high freq detail. But never use last lod as this has crossfading.
					int sliceIndex = clamp(_DataSliceOffset, 0, _SliceCount - 2);
					float3 uv_slice = WorldToUV(_WorldSpaceCameraPos.xz, _CrestCascadeData[sliceIndex], sliceIndex);
					SampleDisplacements(_LD_TexArray_AnimatedWaves, uv_slice, 1.0, displacement, sss);

					half depth = CREST_OCEAN_DEPTH_BASELINE;
					half2 shadow = 0.0;
					{
						SampleSeaDepth(_LD_TexArray_SeaFloorDepth, uv_slice, 1.0, depth);
// #if CREST_SHADOWS_ON
						SampleShadow(_LD_TexArray_Shadow, uv_slice, 1.0, shadow);
// #endif
					}

					half3 ambientLighting = _AmbientLighting;
					ApplyIndirectLightingMultiplier(ambientLighting);

					CrestNodeLightWaterVolume_half
					(
						_ScatterColourBase.xyz,
						_ScatterColourShadow.xyz,
						_ScatterColourShallow.xyz,
						_ScatterColourShallowDepthMax,
						_ScatterColourShallowDepthFalloff,
						_SSSIntensityBase,
						_SSSIntensitySun,
						_SSSTint.xyz,
						_SSSSunFalloff,
						depth,
						shadow,
						sss,
						view,
						_WorldSpaceCameraPos,
						ambientLighting,
						_PrimaryLightDirection,
						_PrimaryLightIntensity,
						volumeLight
					);
				}

#if CREST_CAUSTICS_ON
				float3 worldPos;
				{

					// HDRP needs a different way to unproject to world space. I tried to put this code into URP but it didnt work on 2019.3.0f1
					float deviceZ = LinearToDeviceDepth(sceneZ, _ZBufferParams);
					PositionInputs posInput = GetPositionInput(screenPos * _ScreenSize.xy, _ScreenSize.zw, deviceZ, UNITY_MATRIX_I_VP, UNITY_MATRIX_V);
					worldPos = posInput.positionWS;
#if (SHADEROPTIONS_CAMERA_RELATIVE_RENDERING != 0)
					worldPos += _WorldSpaceCameraPos;
#endif
				}

				if (sceneZ01 != 0.0 && !isOceanSurface)
				{
					CrestNodeApplyCaustics_float
					(
						sceneColour,
						worldPos,
						displacement.y + _OceanHeight,
						_DepthFogDensity,
						_PrimaryLightIntensity,
						_PrimaryLightDirection,
						sceneZ,
						_CausticsTexture,
						_CausticsTextureScale,
						_CausticsTextureAverage,
						_CausticsStrength,
						_CausticsFocalDepth,
						_CausticsDepthOfField,
						_CausticsDistortionTexture,
						_CausticsDistortionStrength,
						_CausticsDistortionScale,
						isUnderwater,
						sceneColour
					);
				}
#endif // CREST_CAUSTICS_ON

				return lerp(sceneColour, volumeLight * GetCurrentExposureMultiplier(), saturate(1.0 - exp(-_DepthFogDensity.xyz * sceneZ)));
			}

			float4 Frag(Varyings input) : SV_Target
			{
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

#if !_FULL_SCREEN_EFFECT
				// The horizon line is the intersection between the far plane and the ocean plane. The pos and normal of this
				// intersection line is passed in.
#if CREST_HANDLE_XR
				const bool isBelowHorizon = unity_StereoEyeIndex == 0 ?
					dot(input.uv - _HorizonPosNormal.xy, _HorizonPosNormal.zw) > 0.0 :
					dot(input.uv - _HorizonPosNormalRight.xy, _HorizonPosNormalRight.zw) > 0.0;
#else // CREST_HANDLE_XR
				const bool isBelowHorizon = dot(input.uv - _HorizonPosNormal.xy, _HorizonPosNormal.zw) > 0.0;
#endif // CREST_HANDLE_XR
#else // !_FULL_SCREEN_EFFECT
				const bool isBelowHorizon = true;
#endif // !_FULL_SCREEN_EFFECT


				// TODO: Test that this works with VR
				uint2 uvScreenSpace = input.uv * _ScreenSize.xy; // /*UnityStereoTransformScreenSpaceTex*/(input.uv);
				half3 sceneColour = LOAD_TEXTURE2D_X(_MainTex, uvScreenSpace).rgb;

				// We are loading both the stencil buffer and depth buffer respectively.
				float sceneZ01 = LOAD_TEXTURE2D_X(_CameraDepthTexture, uvScreenSpace).x;
				float sceneWithoutTransparentsZ01 = LOAD_TEXTURE2D_X(_CrestCameraDepthTexture, uvScreenSpace).x;

				// RTHandles do not decrease in size when resizing viewport or using dynamic scaling. We need to apply
				// the scale otherwise the coordinates will be incorrect. XY is current frame. ZW is previous frame.
				uvScreenSpace.xy /= _RTHandleScale.xy;

				float mask = LOAD_TEXTURE2D_X(_CrestOceanMaskTexture, uvScreenSpace).x;
				const float oceanDepth01 = LOAD_TEXTURE2D_X(_CrestOceanMaskDepthTexture, uvScreenSpace).x;

				// Ocean surface check is used avoid drawing caustics on the water surface.
				// With MSAA, we could change this to use the stencil buffer since it does not include the ocean
				// surface. This would allow us to include other transparents that have a depth postpass.
				bool isOceanSurface = mask != UNDERWATER_MASK_NO_MASK && (sceneWithoutTransparentsZ01 <= oceanDepth01);
				bool isUnderwater = mask == UNDERWATER_MASK_WATER_SURFACE_BELOW || (isBelowHorizon && mask != UNDERWATER_MASK_WATER_SURFACE_ABOVE);

				// If MSAA is used, the ocean depth will not be rendered into the stencil buffer. Transparent objects
				// require a depth postpass which is not available to us yet (Lit shader has it). Even if it was
				// available, it would be an unecessary resource cost unless it was needed for other purposes.
				if (oceanDepth01 > sceneZ01)
				{
					sceneZ01 = oceanDepth01;
				}

				float wt = 1.0;

#if CREST_MENISCUS_ON
				// Detect water to no water transitions which happen if mask values on below pixels are less than this mask
				//if (mask <= 1.0)
				{
					// Looks at pixels below this pixel and if there is a transition from above to below, darken the pixel
					// to emulate a meniscus effect. It does a few to get a thicker line than 1 pixel. The line it produces is
					// smooth on the top side and sharp at the bottom. It might be possible to detect where the edge is and do
					// a calculation to get it smooth both above and below, but might be more complex.
					float wt_mul = 0.9;
					float4 dy = float4(0.0, -1.0, -2.0, -3.0) / _ScreenParams.y;
					wt *= (LOAD_TEXTURE2D_X(_CrestOceanMaskTexture, uvScreenSpace + dy.xy).x > mask) ? wt_mul : 1.0;
					wt *= (LOAD_TEXTURE2D_X(_CrestOceanMaskTexture, uvScreenSpace + dy.xz).x > mask) ? wt_mul : 1.0;
					wt *= (LOAD_TEXTURE2D_X(_CrestOceanMaskTexture, uvScreenSpace + dy.xw).x > mask) ? wt_mul : 1.0;
				}
#endif // CREST_MENISCUS_ON

#if _DEBUG_VIEW_OCEAN_MASK
				if(isOceanSurface)
				{
					return float4(sceneColour * float3(mask == UNDERWATER_MASK_WATER_SURFACE_ABOVE, mask == UNDERWATER_MASK_WATER_SURFACE_BELOW, 0.0), 1.0);
				}
				else
				{
					return float4(sceneColour * float3(isUnderwater * 0.5, (1.0 - isUnderwater) * 0.5, 1.0), 1.0);
				}
#else
				if(isUnderwater)
				{
					const half3 view = normalize(input.viewWS);
					sceneColour = ApplyUnderwaterEffect(sceneColour, sceneZ01, view, input.uv, isOceanSurface);
				}

				return half4(wt * sceneColour, 1.0);
#endif // _DEBUG_VIEW_OCEAN_MASK
			}

			ENDHLSL
		}
	}
	Fallback Off
}
