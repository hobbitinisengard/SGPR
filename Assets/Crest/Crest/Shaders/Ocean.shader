// Crest Ocean System

// Copyright 2020 Wave Harmonic Ltd

Shader "Crest/Ocean"
{
    Properties
    {
		[Header(Normal Mapping)]
		[NoScaleOffset]_TextureNormals("Normals", 2D) = "bump" {}
        _NormalsScale("Normals Scale", Range(0.01, 200)) = 40
        _NormalsStrength("Normals Strength", Range(0, 1)) = 0.5

		// Base light scattering settings which give water colour
		[Header(Scattering)]
        _ScatterColourBase("Scatter Colour Base", Color) = (0.384167, 0.6049405, 0.8396226, 0)
        _ScatterColourShadow("Scatter Colour Shadow", Color) = (0, 0.03921569, 0.07843138, 1)
        _ScatterColourShallow("Scatter Colour Shallow", Color) = (0.3758455, 0.7105559, 0.7735849, 0)
        _ScatterColourShallowDepthMax("Scatter Colour Shallow Depth Max", Range(0, 50)) = 7
        _ScatterColourShallowDepthFalloff("Scatter Colour Shallow Depth Falloff", Range(0.01, 10)) = 2.57

		[Header(Subsurface Scattering)]
		_SSSIntensityBase("SSS Intensity Base", Range(0, 4)) = 1.04
        _SSSIntensitySun("SSS Intensity Sun", Range(0, 10)) = 4.46
        _SSSTint("SSS Tint", Color) = (0.089, 0.497, 0.456, 0)
        _SSSSunFalloff("SSS Sun Falloff", Range(0.01, 16)) = 5

		[Header(Reflection Environment)]
		_Specular("Specular", Range(0, 1)) = 0.036
        _Occlusion("Occlusion", Range(0, 1)) = 0.332
        _Smoothness("Smoothness", Range(0, 1)) = 0.8
        _SmoothnessFar("Smoothness Far", Range(0, 1)) = 0.35
        _SmoothnessFarDistance("Smoothness Far Distance", Range(1, 8000)) = 2000
        _SmoothnessFalloff("Smoothness Falloff", Range(0, 5)) = 0.5
        _MinReflectionDirectionY("Min Reflection Direction Y", Range(-1, 1)) = 0

		[Header(Foam)]
		[Toggle]CREST_FOAM("Enable", Float) = 1
        [NoScaleOffset]_TextureFoam("Foam", 2D) = "white" {}
        _FoamScale("Foam Scale", Range(0.01, 50)) = 10
        _FoamFeather("Foam Feather", Range(0.001, 1)) = 0.32
        _FoamIntensityAlbedo("Foam Albedo Intensity", Range(0, 3)) = 1
        _FoamIntensityEmissive("Foam Emissive Intensity", Range(0, 2)) = 0.5
        _FoamSmoothness("Foam Smoothness", Range(0, 1)) = 0.7
        _FoamNormalStrength("Foam Normal Strength", Range(0, 30)) = 1
        _FoamBubbleColor("Foam Bubbles Colour", Color) = (0.6392157, 0.8313726, 0.8196079, 0)
        _FoamBubbleParallax("Foam Bubbles Parallax", Range(0, 0.5)) = 0.14
        _FoamBubblesCoverage("Foam Bubbles Coverage", Range(0, 5)) = 1.68

		[Header(Transparency)]
		_RefractionStrength("Refraction Strength", Range(0, 2)) = 0.2
        _DepthFogDensity("Depth Fog Density", Vector) = (0.33, 0.23, 0.37, 0)

		[Header(Caustics)]
		[Toggle]CREST_CAUSTICS("Enable", Float) = 1
        [NoScaleOffset]_CausticsTexture("Caustics", 2D) = "black" {}
        _CausticsTextureScale("Caustics Scale", Range(0, 25)) = 5

        _CausticsTextureAverage("Caustics Texture Grey Point", Range(0, 1)) = 0.07
        _CausticsStrength("Caustics Strength", Range(0, 10)) = 3.2
        _CausticsFocalDepth("Caustics Focal Depth", Range(0, 25)) = 2
        _CausticsDepthOfField("Caustics Depth of Field", Range(0.01, 10)) = 6
        [NoScaleOffset]_CausticsDistortionTexture("Caustics Distortion Texture", 2D) = "grey" {}
        _CausticsDistortionStrength("Caustics Distortion Strength", Range(0, 0.25)) = 0.16
        _CausticsDistortionScale("Caustics Distortion Scale", Range(0.01, 50)) = 25

		// To use the underwater effect the UnderWaterCurtainGeom and UnderWaterMeniscus prefabs must be parented to the camera.
		[Header(Underwater)]
		// Ordinarily set this to Back to cull back faces, but set to Off to make sure both sides of the surface draw if the
		// underwater effect is being used.
		[Enum(CullMode)] _CullMode("Cull Mode", Int) = 2

		[Header(Flow)]
		// Flow is horizontal motion in water as demonstrated in the 'whirlpool' example scene. 'Create Flow Sim' must be
		// enabled on the OceanRenderer to generate flow data.
		[Toggle]CREST_FLOW("Enable", Float) = 0
	}
    SubShader
    {
        Tags
        {
            "RenderPipeline"="HDRenderPipeline"
            "RenderType"="HDLitShader"
            "Queue"="Transparent+0"
        }
        
        Pass
        {
            // based on HDPBRPass.template
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
        
            //-------------------------------------------------------------------------------------
            // Render Modes (Blend, Cull, ZTest, Stencil, etc)
            //-------------------------------------------------------------------------------------
            Blend One Zero
        
			// Culling user defined - can be turned off for underwater
			Cull[_CullMode]
        
            
            ZWrite On
        
            
            
            ColorMask 0
        
            //-------------------------------------------------------------------------------------
            // End Render Modes
            //-------------------------------------------------------------------------------------
        
            HLSLPROGRAM
        
            #pragma target 4.5
            #pragma only_renderers d3d11 ps4 xboxone vulkan metal switch
            //#pragma enable_d3d11_debug_symbols
        
            #pragma multi_compile_instancing
        #pragma instancing_options renderinglayer
        
            #pragma multi_compile _ LOD_FADE_CROSSFADE
        
            //-------------------------------------------------------------------------------------
            // Graph Defines
            //-------------------------------------------------------------------------------------
                    #define SHADERPASS SHADERPASS_SHADOWS
                #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                #define REQUIRE_DEPTH_TEXTURE
                #endif
                // ACTIVE FIELDS:
                //   DoubleSided
                //   DoubleSided.Mirror
                //   FragInputs.isFrontFace
                //   Material.SpecularColor
                //   features.NormalDropOffTS
                //   SurfaceType.Transparent
                //   BlendMode.Alpha
                //   AlphaFog
                //   SurfaceDescription.Alpha
                //   SurfaceDescription.AlphaClipThreshold
                //   features.modifyMesh
                //   VertexDescription.VertexPosition
                //   VertexDescription.VertexNormal
                //   VertexDescription.VertexTangent
                //   VaryingsMeshToPS.cullFace
                // Shared Graph Keywords
                    #pragma shader_feature_local _ CREST_FOAM_ON
                    #pragma shader_feature_local _ CREST_CAUSTICS_ON
                    #pragma shader_feature_local _ CREST_FLOW_ON
                
                    #if defined(CREST_FOAM_ON) && defined(CREST_CAUSTICS_ON) && defined(CREST_FLOW_ON)
                        #define KEYWORD_PERMUTATION_0
                    #elif defined(CREST_FOAM_ON) && defined(CREST_CAUSTICS_ON)
                        #define KEYWORD_PERMUTATION_1
                    #elif defined(CREST_FOAM_ON) && defined(CREST_FLOW_ON)
                        #define KEYWORD_PERMUTATION_2
                    #elif defined(CREST_FOAM_ON)
                        #define KEYWORD_PERMUTATION_3
                    #elif defined(CREST_CAUSTICS_ON) && defined(CREST_FLOW_ON)
                        #define KEYWORD_PERMUTATION_4
                    #elif defined(CREST_CAUSTICS_ON)
                        #define KEYWORD_PERMUTATION_5
                    #elif defined(CREST_FLOW_ON)
                        #define KEYWORD_PERMUTATION_6
                    #else
                        #define KEYWORD_PERMUTATION_7
                    #endif
                
                
            //-------------------------------------------------------------------------------------
            // End Defines
            //-------------------------------------------------------------------------------------
        
            //-------------------------------------------------------------------------------------
            // Variant Definitions (active field translations to HDRP defines)
            //-------------------------------------------------------------------------------------
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
        #define _MATERIAL_FEATURE_SPECULAR_COLOR 1
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
        #define _SURFACE_TYPE_TRANSPARENT 1
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
        #define _BLENDMODE_ALPHA 1
        #endif
        
        
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
        #define _DOUBLESIDED_ON 1
        #endif

            #define _NORMAL_DROPOFF_TS	1
            // #define _NORMAL_DROPOFF_OS	1
            // #define _NORMAL_DROPOFF_WS	1
        
            #define CREST_GENERATED_SHADER_ON 1
        
            //-------------------------------------------------------------------------------------
            // End Variant Definitions
            //-------------------------------------------------------------------------------------
        
            #pragma vertex Vert
            #pragma fragment Frag
        
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/NormalSurfaceGradient.hlsl"
        
            // define FragInputs structure
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl"
        
            //-------------------------------------------------------------------------------------
            // Active Field Defines
            //-------------------------------------------------------------------------------------
        
            // this translates the new dependency tracker into the old preprocessor definitions for the existing HDRP shader code
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
        #define ATTRIBUTES_NEED_NORMAL
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
        #define ATTRIBUTES_NEED_TANGENT
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
        #define ATTRIBUTES_NEED_TEXCOORD0
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
        #define ATTRIBUTES_NEED_TEXCOORD1
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
        #define ATTRIBUTES_NEED_TEXCOORD2
        #endif
        
        
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
        #define VARYINGS_NEED_POSITION_WS
        #endif
        
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
        #define VARYINGS_NEED_TEXCOORD0
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
        #define VARYINGS_NEED_TEXCOORD1
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
        #define VARYINGS_NEED_TEXCOORD2
        #endif
        
        
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
        #define VARYINGS_NEED_CULLFACE
        #endif
        
            #define HAVE_MESH_MODIFICATION
        
            //-------------------------------------------------------------------------------------
            // End Defines
            //-------------------------------------------------------------------------------------
        	
        
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #ifdef DEBUG_DISPLAY
                #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Debug/DebugDisplay.hlsl"
            #endif
        
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl"
        
        #if (SHADERPASS == SHADERPASS_FORWARD)
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/Lighting.hlsl"
        
            #define HAS_LIGHTLOOP
        
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoopDef.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/Lit.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoop.hlsl"
        #else
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/Lit.hlsl"
        #endif
        
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalUtilities.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/LitDecalData.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl"
        
            //Used by SceneSelectionPass
            int _ObjectId;
            int _PassValue;
        
            //-------------------------------------------------------------------------------------
            // Interpolator Packing And Struct Declarations
            //-------------------------------------------------------------------------------------
            // Generated Type: AttributesMesh
            struct AttributesMesh
            {
                #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                float3 positionOS : POSITION;
                #endif // Shader Graph Keywords
                #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                float3 normalOS : NORMAL; // optional
                #endif // Shader Graph Keywords
                #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                float4 tangentOS : TANGENT; // optional
                #endif // Shader Graph Keywords
                #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                float4 uv0 : TEXCOORD0; // optional
                #endif // Shader Graph Keywords
                #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                float4 uv1 : TEXCOORD1; // optional
                #endif // Shader Graph Keywords
                #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                float4 uv2 : TEXCOORD2; // optional
                #endif // Shader Graph Keywords
                #if UNITY_ANY_INSTANCING_ENABLED
                #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                uint instanceID : INSTANCEID_SEMANTIC;
                #endif // Shader Graph Keywords
                #endif // UNITY_ANY_INSTANCING_ENABLED
            };
            // Generated Type: VaryingsMeshToPS
            struct VaryingsMeshToPS
            {
                #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                float4 positionCS : SV_POSITION;
                #endif // Shader Graph Keywords
                #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                float3 positionRWS; // optional
                #endif // Shader Graph Keywords
                #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                float4 texCoord0; // optional
                #endif // Shader Graph Keywords
                #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                float4 texCoord1; // optional
                #endif // Shader Graph Keywords
                #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                float4 texCoord2; // optional
                #endif // Shader Graph Keywords
                #if UNITY_ANY_INSTANCING_ENABLED
                #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                uint instanceID : CUSTOM_INSTANCE_ID;
                #endif // Shader Graph Keywords
                #endif // UNITY_ANY_INSTANCING_ENABLED
                #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
                #endif // Shader Graph Keywords
                #endif // defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            };
            
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
            // Generated Type: PackedVaryingsMeshToPS
            struct PackedVaryingsMeshToPS
            {
                float4 positionCS : SV_POSITION; // unpacked
                #if UNITY_ANY_INSTANCING_ENABLED
                uint instanceID : CUSTOM_INSTANCE_ID; // unpacked
                #endif // conditional
                float3 interp00 : TEXCOORD0; // auto-packed
                float4 interp01 : TEXCOORD1; // auto-packed
                float4 interp02 : TEXCOORD2; // auto-packed
                float4 interp03 : TEXCOORD3; // auto-packed
                #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC; // unpacked
                #endif // conditional
            };
            
            // Packed Type: VaryingsMeshToPS
            PackedVaryingsMeshToPS PackVaryingsMeshToPS(VaryingsMeshToPS input)
            {
                PackedVaryingsMeshToPS output = (PackedVaryingsMeshToPS)0;
                output.positionCS = input.positionCS;
                output.interp00.xyz = input.positionRWS;
                output.interp01.xyzw = input.texCoord0;
                output.interp02.xyzw = input.texCoord1;
                output.interp03.xyzw = input.texCoord2;
                #if UNITY_ANY_INSTANCING_ENABLED
                output.instanceID = input.instanceID;
                #endif // conditional
                #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                output.cullFace = input.cullFace;
                #endif // conditional
                return output;
            }
            
            // Unpacked Type: VaryingsMeshToPS
            VaryingsMeshToPS UnpackVaryingsMeshToPS(PackedVaryingsMeshToPS input)
            {
                VaryingsMeshToPS output = (VaryingsMeshToPS)0;
                output.positionCS = input.positionCS;
                output.positionRWS = input.interp00.xyz;
                output.texCoord0 = input.interp01.xyzw;
                output.texCoord1 = input.interp02.xyzw;
                output.texCoord2 = input.interp03.xyzw;
                #if UNITY_ANY_INSTANCING_ENABLED
                output.instanceID = input.instanceID;
                #endif // conditional
                #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                output.cullFace = input.cullFace;
                #endif // conditional
                return output;
            }
            #endif
            // Generated Type: VaryingsMeshToDS
            struct VaryingsMeshToDS
            {
                #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                float3 positionRWS;
                #endif // Shader Graph Keywords
                #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                float3 normalWS;
                #endif // Shader Graph Keywords
                #if UNITY_ANY_INSTANCING_ENABLED
                #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                uint instanceID : CUSTOM_INSTANCE_ID;
                #endif // Shader Graph Keywords
                #endif // UNITY_ANY_INSTANCING_ENABLED
            };
            
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
            // Generated Type: PackedVaryingsMeshToDS
            struct PackedVaryingsMeshToDS
            {
                #if UNITY_ANY_INSTANCING_ENABLED
                uint instanceID : CUSTOM_INSTANCE_ID; // unpacked
                #endif // conditional
                float3 interp00 : TEXCOORD0; // auto-packed
                float3 interp01 : TEXCOORD1; // auto-packed
            };
            
            // Packed Type: VaryingsMeshToDS
            PackedVaryingsMeshToDS PackVaryingsMeshToDS(VaryingsMeshToDS input)
            {
                PackedVaryingsMeshToDS output = (PackedVaryingsMeshToDS)0;
                output.interp00.xyz = input.positionRWS;
                output.interp01.xyz = input.normalWS;
                #if UNITY_ANY_INSTANCING_ENABLED
                output.instanceID = input.instanceID;
                #endif // conditional
                return output;
            }
            
            // Unpacked Type: VaryingsMeshToDS
            VaryingsMeshToDS UnpackVaryingsMeshToDS(PackedVaryingsMeshToDS input)
            {
                VaryingsMeshToDS output = (VaryingsMeshToDS)0;
                output.positionRWS = input.interp00.xyz;
                output.normalWS = input.interp01.xyz;
                #if UNITY_ANY_INSTANCING_ENABLED
                output.instanceID = input.instanceID;
                #endif // conditional
                return output;
            }
            #endif
            //-------------------------------------------------------------------------------------
            // End Interpolator Packing And Struct Declarations
            //-------------------------------------------------------------------------------------
        
            //-------------------------------------------------------------------------------------
            // Graph generated code
            //-------------------------------------------------------------------------------------
                    // Shared Graph Properties (uniform inputs)
                    CBUFFER_START(UnityPerMaterial)
                    half _NormalsScale;
                    half _NormalsStrength;
                    half4 _ScatterColourBase;
                    half4 _ScatterColourShadow;
                    float4 _ScatterColourShallow;
                    half _ScatterColourShallowDepthMax;
                    half _ScatterColourShallowDepthFalloff;
                    half _SSSIntensityBase;
                    half _SSSIntensitySun;
                    half4 _SSSTint;
                    half _SSSSunFalloff;
                    float _Specular;
                    float _Occlusion;
                    half _Smoothness;
                    float _SmoothnessFar;
                    float _SmoothnessFarDistance;
                    float _SmoothnessFalloff;
                    float _MinReflectionDirectionY;
                    half _FoamScale;
                    half _FoamFeather;
                    half _FoamIntensityAlbedo;
                    half _FoamIntensityEmissive;
                    half _FoamSmoothness;
                    half _FoamNormalStrength;
                    half4 _FoamBubbleColor;
                    half _FoamBubbleParallax;
                    half _FoamBubblesCoverage;
                    half _RefractionStrength;
                    half3 _DepthFogDensity;
                    float _CausticsTextureScale;
                    float _CausticsTextureAverage;
                    float _CausticsStrength;
                    float _CausticsFocalDepth;
                    float _CausticsDepthOfField;
                    float _CausticsDistortionStrength;
                    float _CausticsDistortionScale;
                    CBUFFER_END
                    TEXTURE2D(_TextureNormals); SAMPLER(sampler_TextureNormals); float4 _TextureNormals_TexelSize;
                    TEXTURE2D(_TextureFoam); SAMPLER(sampler_TextureFoam); half4 _TextureFoam_TexelSize;
                    TEXTURE2D(_CausticsTexture); SAMPLER(sampler_CausticsTexture); float4 _CausticsTexture_TexelSize;
                    TEXTURE2D(_CausticsDistortionTexture); SAMPLER(sampler_CausticsDistortionTexture); float4 _CausticsDistortionTexture_TexelSize;
                
                // Vertex Graph Inputs
                    struct VertexDescriptionInputs
                    {
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        float3 WorldSpaceNormal; // optional
                        #endif // Shader Graph Keywords
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        float3 WorldSpaceTangent; // optional
                        #endif // Shader Graph Keywords
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        float3 ObjectSpaceBiTangent; // optional
                        #endif // Shader Graph Keywords
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        float3 WorldSpaceBiTangent; // optional
                        #endif // Shader Graph Keywords
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        float3 AbsoluteWorldSpacePosition; // optional
                        #endif // Shader Graph Keywords
                    };
                // Vertex Graph Outputs
                    struct VertexDescription
                    {
                        float3 VertexPosition;
                        float3 VertexNormal;
                        float3 VertexTangent;
                    };
                    
                // Pixel Graph Inputs
                    struct SurfaceDescriptionInputs
                    {
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        float3 ViewSpacePosition; // optional
                        #endif // Shader Graph Keywords
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        float3 WorldSpacePosition; // optional
                        #endif // Shader Graph Keywords
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        float3 AbsoluteWorldSpacePosition; // optional
                        #endif // Shader Graph Keywords
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        float4 ScreenPosition; // optional
                        #endif // Shader Graph Keywords
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        float4 uv0; // optional
                        #endif // Shader Graph Keywords
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        float4 uv1; // optional
                        #endif // Shader Graph Keywords
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        float4 uv2; // optional
                        #endif // Shader Graph Keywords
                    };
                // Pixel Graph Outputs
                    struct SurfaceDescription
                    {
                        float Alpha;
                        float AlphaClipThreshold;
                    };
                    
                // Shared Graph Node Functions
                
                    void Unity_SceneDepth_Eye_float(float4 UV, out float Out)
                    {
                        Out = LinearEyeDepth(SHADERGRAPH_SAMPLE_SCENE_DEPTH(UV.xy), _ZBufferParams);
                    }
                
                    void Unity_Negate_float(float In, out float Out)
                    {
                        Out = -1 * In;
                    }
                
                    void Unity_Subtract_float(float A, float B, out float Out)
                    {
                        Out = A - B;
                    }
                
                    void Unity_Remap_float(float In, float2 InMinMax, float2 OutMinMax, out float Out)
                    {
                        Out = OutMinMax.x + (In - InMinMax.x) * (OutMinMax.y - OutMinMax.x) / (InMinMax.y - InMinMax.x);
                    }
                
                    void Unity_Saturate_float(float In, out float Out)
                    {
                        Out = saturate(In);
                    }
                
                    // 73abdd69b930b80b268ce9c97db7e24b
                    #include "ShadergraphFramework/CrestNodeComputeSamplingData.hlsl"
                
                    // 2973156898769ad700b9d1820ecfd177
                    #include "ShadergraphFramework/CrestNodeSampleOceanData.hlsl"
                
                    struct Bindings_CrestSampleOceanData_f19d6d2aa60f8294e870f100303ee5a4
                    {
                    };
                
                    void SG_CrestSampleOceanData_f19d6d2aa60f8294e870f100303ee5a4(float2 Vector2_3171933F, float Vector1_CD41515B, float3 Vector3_7E91D336, float3 Vector3_3A95DCDF, float4 Vector4_C0B2B5EA, float4 Vector4_9C46108E, float Vector1_8EA8B92B, Bindings_CrestSampleOceanData_f19d6d2aa60f8294e870f100303ee5a4 IN, out float3 Displacement_1, out float OceanWaterDepth_5, out float Foam_6, out float2 Shadow_7, out float2 Flow_8, out float SubSurfaceScattering_9)
                    {
                        float2 _Property_1A287CC6_Out_0 = Vector2_3171933F;
                        float _Property_2D1D1700_Out_0 = Vector1_CD41515B;
                        float3 _Property_C925A867_Out_0 = Vector3_7E91D336;
                        float3 _Property_6C273401_Out_0 = Vector3_3A95DCDF;
                        float4 _Property_467D1BE7_Out_0 = Vector4_C0B2B5EA;
                        float4 _Property_4E045F45_Out_0 = Vector4_9C46108E;
                        float _Property_59E019ED_Out_0 = Vector1_8EA8B92B;
                        float3 _CustomFunction_487C31E1_Displacement_3;
                        float _CustomFunction_487C31E1_OceanDepth_8;
                        float _CustomFunction_487C31E1_Foam_4;
                        float2 _CustomFunction_487C31E1_Shadow_5;
                        float2 _CustomFunction_487C31E1_Flow_6;
                        float _CustomFunction_487C31E1_SSS_17;
                        CrestNodeSampleOceanData_float(_Property_1A287CC6_Out_0, _Property_2D1D1700_Out_0, _Property_C925A867_Out_0, _Property_6C273401_Out_0, _Property_467D1BE7_Out_0, _Property_4E045F45_Out_0, _Property_59E019ED_Out_0, _CustomFunction_487C31E1_Displacement_3, _CustomFunction_487C31E1_OceanDepth_8, _CustomFunction_487C31E1_Foam_4, _CustomFunction_487C31E1_Shadow_5, _CustomFunction_487C31E1_Flow_6, _CustomFunction_487C31E1_SSS_17);
                        Displacement_1 = _CustomFunction_487C31E1_Displacement_3;
                        OceanWaterDepth_5 = _CustomFunction_487C31E1_OceanDepth_8;
                        Foam_6 = _CustomFunction_487C31E1_Foam_4;
                        Shadow_7 = _CustomFunction_487C31E1_Shadow_5;
                        Flow_8 = _CustomFunction_487C31E1_Flow_6;
                        SubSurfaceScattering_9 = _CustomFunction_487C31E1_SSS_17;
                    }
                
                    void Unity_Subtract_float2(float2 A, float2 B, out float2 Out)
                    {
                        Out = A - B;
                    }
                
                    struct Bindings_CrestComputeOceanDataFromDisplacedPosition_1f483fe321b10c64e8b93a023ae60dd6
                    {
                    };
                
                    void SG_CrestComputeOceanDataFromDisplacedPosition_1f483fe321b10c64e8b93a023ae60dd6(float3 Vector3_A7B8495A, Bindings_CrestComputeOceanDataFromDisplacedPosition_1f483fe321b10c64e8b93a023ae60dd6 IN, out float2 UndisplacedPosXZAWS_7, out float LodAlpha_6, out float3 Displacement_8, out float OceanWaterDepth_1, out float Foam_2, out float2 Shadow_3, out float2 Flow_4, out float SubSurfaceScattering_5)
                    {
                        float3 _Property_232E7FDA_Out_0 = Vector3_A7B8495A;
                        float _Split_8E8A6DCA_R_1 = _Property_232E7FDA_Out_0[0];
                        float _Split_8E8A6DCA_G_2 = _Property_232E7FDA_Out_0[1];
                        float _Split_8E8A6DCA_B_3 = _Property_232E7FDA_Out_0[2];
                        float _Split_8E8A6DCA_A_4 = 0;
                        float2 _Vector2_A3499051_Out_0 = float2(_Split_8E8A6DCA_R_1, _Split_8E8A6DCA_B_3);
                        half _CustomFunction_A082C8F2_LodAlpha_3;
                        half3 _CustomFunction_A082C8F2_OceanPosScale0_4;
                        half3 _CustomFunction_A082C8F2_OceanPosScale1_5;
                        half4 _CustomFunction_A082C8F2_OceanParams0_6;
                        half4 _CustomFunction_A082C8F2_OceanParams1_7;
                        half _CustomFunction_A082C8F2_Slice0_1;
                        half _CustomFunction_A082C8F2_Slice1_2;
                        CrestComputeSamplingData_half(_Vector2_A3499051_Out_0, _CustomFunction_A082C8F2_LodAlpha_3, _CustomFunction_A082C8F2_OceanPosScale0_4, _CustomFunction_A082C8F2_OceanPosScale1_5, _CustomFunction_A082C8F2_OceanParams0_6, _CustomFunction_A082C8F2_OceanParams1_7, _CustomFunction_A082C8F2_Slice0_1, _CustomFunction_A082C8F2_Slice1_2);
                        Bindings_CrestSampleOceanData_f19d6d2aa60f8294e870f100303ee5a4 _CrestSampleOceanData_87ACC65F;
                        float3 _CrestSampleOceanData_87ACC65F_Displacement_1;
                        float _CrestSampleOceanData_87ACC65F_OceanWaterDepth_5;
                        float _CrestSampleOceanData_87ACC65F_Foam_6;
                        float2 _CrestSampleOceanData_87ACC65F_Shadow_7;
                        float2 _CrestSampleOceanData_87ACC65F_Flow_8;
                        float _CrestSampleOceanData_87ACC65F_SubSurfaceScattering_9;
                        SG_CrestSampleOceanData_f19d6d2aa60f8294e870f100303ee5a4(_Vector2_A3499051_Out_0, _CustomFunction_A082C8F2_LodAlpha_3, _CustomFunction_A082C8F2_OceanPosScale0_4, _CustomFunction_A082C8F2_OceanPosScale1_5, _CustomFunction_A082C8F2_OceanParams0_6, _CustomFunction_A082C8F2_OceanParams1_7, _CustomFunction_A082C8F2_Slice0_1, _CrestSampleOceanData_87ACC65F, _CrestSampleOceanData_87ACC65F_Displacement_1, _CrestSampleOceanData_87ACC65F_OceanWaterDepth_5, _CrestSampleOceanData_87ACC65F_Foam_6, _CrestSampleOceanData_87ACC65F_Shadow_7, _CrestSampleOceanData_87ACC65F_Flow_8, _CrestSampleOceanData_87ACC65F_SubSurfaceScattering_9);
                        float _Split_CD3A9051_R_1 = _CrestSampleOceanData_87ACC65F_Displacement_1[0];
                        float _Split_CD3A9051_G_2 = _CrestSampleOceanData_87ACC65F_Displacement_1[1];
                        float _Split_CD3A9051_B_3 = _CrestSampleOceanData_87ACC65F_Displacement_1[2];
                        float _Split_CD3A9051_A_4 = 0;
                        float2 _Vector2_B8C0C1F0_Out_0 = float2(_Split_CD3A9051_R_1, _Split_CD3A9051_B_3);
                        float2 _Subtract_8977A663_Out_2;
                        Unity_Subtract_float2(_Vector2_A3499051_Out_0, _Vector2_B8C0C1F0_Out_0, _Subtract_8977A663_Out_2);
                        half _CustomFunction_9D8B14F0_LodAlpha_3;
                        half3 _CustomFunction_9D8B14F0_OceanPosScale0_4;
                        half3 _CustomFunction_9D8B14F0_OceanPosScale1_5;
                        half4 _CustomFunction_9D8B14F0_OceanParams0_6;
                        half4 _CustomFunction_9D8B14F0_OceanParams1_7;
                        half _CustomFunction_9D8B14F0_Slice0_1;
                        half _CustomFunction_9D8B14F0_Slice1_2;
                        CrestComputeSamplingData_half(_Subtract_8977A663_Out_2, _CustomFunction_9D8B14F0_LodAlpha_3, _CustomFunction_9D8B14F0_OceanPosScale0_4, _CustomFunction_9D8B14F0_OceanPosScale1_5, _CustomFunction_9D8B14F0_OceanParams0_6, _CustomFunction_9D8B14F0_OceanParams1_7, _CustomFunction_9D8B14F0_Slice0_1, _CustomFunction_9D8B14F0_Slice1_2);
                        Bindings_CrestSampleOceanData_f19d6d2aa60f8294e870f100303ee5a4 _CrestSampleOceanData_D8619779;
                        float3 _CrestSampleOceanData_D8619779_Displacement_1;
                        float _CrestSampleOceanData_D8619779_OceanWaterDepth_5;
                        float _CrestSampleOceanData_D8619779_Foam_6;
                        float2 _CrestSampleOceanData_D8619779_Shadow_7;
                        float2 _CrestSampleOceanData_D8619779_Flow_8;
                        float _CrestSampleOceanData_D8619779_SubSurfaceScattering_9;
                        SG_CrestSampleOceanData_f19d6d2aa60f8294e870f100303ee5a4(_Subtract_8977A663_Out_2, _CustomFunction_9D8B14F0_LodAlpha_3, _CustomFunction_9D8B14F0_OceanPosScale0_4, _CustomFunction_9D8B14F0_OceanPosScale1_5, _CustomFunction_9D8B14F0_OceanParams0_6, _CustomFunction_9D8B14F0_OceanParams1_7, _CustomFunction_9D8B14F0_Slice0_1, _CrestSampleOceanData_D8619779, _CrestSampleOceanData_D8619779_Displacement_1, _CrestSampleOceanData_D8619779_OceanWaterDepth_5, _CrestSampleOceanData_D8619779_Foam_6, _CrestSampleOceanData_D8619779_Shadow_7, _CrestSampleOceanData_D8619779_Flow_8, _CrestSampleOceanData_D8619779_SubSurfaceScattering_9);
                        float _Split_1616DE7_R_1 = _CrestSampleOceanData_D8619779_Displacement_1[0];
                        float _Split_1616DE7_G_2 = _CrestSampleOceanData_D8619779_Displacement_1[1];
                        float _Split_1616DE7_B_3 = _CrestSampleOceanData_D8619779_Displacement_1[2];
                        float _Split_1616DE7_A_4 = 0;
                        float2 _Vector2_B871614F_Out_0 = float2(_Split_1616DE7_R_1, _Split_1616DE7_B_3);
                        float2 _Subtract_39E2CE30_Out_2;
                        Unity_Subtract_float2(_Vector2_A3499051_Out_0, _Vector2_B871614F_Out_0, _Subtract_39E2CE30_Out_2);
                        half _CustomFunction_10AEAD9A_LodAlpha_3;
                        half3 _CustomFunction_10AEAD9A_OceanPosScale0_4;
                        half3 _CustomFunction_10AEAD9A_OceanPosScale1_5;
                        half4 _CustomFunction_10AEAD9A_OceanParams0_6;
                        half4 _CustomFunction_10AEAD9A_OceanParams1_7;
                        half _CustomFunction_10AEAD9A_Slice0_1;
                        half _CustomFunction_10AEAD9A_Slice1_2;
                        CrestComputeSamplingData_half(_Subtract_39E2CE30_Out_2, _CustomFunction_10AEAD9A_LodAlpha_3, _CustomFunction_10AEAD9A_OceanPosScale0_4, _CustomFunction_10AEAD9A_OceanPosScale1_5, _CustomFunction_10AEAD9A_OceanParams0_6, _CustomFunction_10AEAD9A_OceanParams1_7, _CustomFunction_10AEAD9A_Slice0_1, _CustomFunction_10AEAD9A_Slice1_2);
                        Bindings_CrestSampleOceanData_f19d6d2aa60f8294e870f100303ee5a4 _CrestSampleOceanData_A1195FE2;
                        float3 _CrestSampleOceanData_A1195FE2_Displacement_1;
                        float _CrestSampleOceanData_A1195FE2_OceanWaterDepth_5;
                        float _CrestSampleOceanData_A1195FE2_Foam_6;
                        float2 _CrestSampleOceanData_A1195FE2_Shadow_7;
                        float2 _CrestSampleOceanData_A1195FE2_Flow_8;
                        float _CrestSampleOceanData_A1195FE2_SubSurfaceScattering_9;
                        SG_CrestSampleOceanData_f19d6d2aa60f8294e870f100303ee5a4(_Subtract_39E2CE30_Out_2, _CustomFunction_10AEAD9A_LodAlpha_3, _CustomFunction_10AEAD9A_OceanPosScale0_4, _CustomFunction_10AEAD9A_OceanPosScale1_5, _CustomFunction_10AEAD9A_OceanParams0_6, _CustomFunction_10AEAD9A_OceanParams1_7, _CustomFunction_10AEAD9A_Slice0_1, _CrestSampleOceanData_A1195FE2, _CrestSampleOceanData_A1195FE2_Displacement_1, _CrestSampleOceanData_A1195FE2_OceanWaterDepth_5, _CrestSampleOceanData_A1195FE2_Foam_6, _CrestSampleOceanData_A1195FE2_Shadow_7, _CrestSampleOceanData_A1195FE2_Flow_8, _CrestSampleOceanData_A1195FE2_SubSurfaceScattering_9);
                        UndisplacedPosXZAWS_7 = _Subtract_39E2CE30_Out_2;
                        LodAlpha_6 = _CustomFunction_10AEAD9A_LodAlpha_3;
                        Displacement_8 = _CrestSampleOceanData_A1195FE2_Displacement_1;
                        OceanWaterDepth_1 = _CrestSampleOceanData_A1195FE2_OceanWaterDepth_5;
                        Foam_2 = _CrestSampleOceanData_A1195FE2_Foam_6;
                        Shadow_3 = _CrestSampleOceanData_A1195FE2_Shadow_7;
                        Flow_4 = _CrestSampleOceanData_A1195FE2_Flow_8;
                        SubSurfaceScattering_5 = _CrestSampleOceanData_A1195FE2_SubSurfaceScattering_9;
                    }
                
                    struct Bindings_CrestUnpackData_5bc4cf08f88035c489658d60edefeec8
                    {
                        float3 AbsoluteWorldSpacePosition;
                        half4 uv0;
                        half4 uv1;
                        half4 uv2;
                    };
                
                    void SG_CrestUnpackData_5bc4cf08f88035c489658d60edefeec8(Bindings_CrestUnpackData_5bc4cf08f88035c489658d60edefeec8 IN, out float2 PositionXZWSUndisp_2, out float LodAlpha_1, out float OceanDepth_3, out float Foam_4, out float2 Shadow_5, out float2 Flow_6, out float SubSurfaceScattering_7)
                    {
                        float4 _UV_CF6CD5F2_Out_0 = IN.uv0;
                        float _Split_B10345C8_R_1 = _UV_CF6CD5F2_Out_0[0];
                        float _Split_B10345C8_G_2 = _UV_CF6CD5F2_Out_0[1];
                        float _Split_B10345C8_B_3 = _UV_CF6CD5F2_Out_0[2];
                        float _Split_B10345C8_A_4 = _UV_CF6CD5F2_Out_0[3];
                        float2 _Vector2_552A5E1F_Out_0 = float2(_Split_B10345C8_R_1, _Split_B10345C8_G_2);
                        Bindings_CrestComputeOceanDataFromDisplacedPosition_1f483fe321b10c64e8b93a023ae60dd6 _CrestComputeOceanDataFromDisplacedPosition_3B9EB7BB;
                        float2 _CrestComputeOceanDataFromDisplacedPosition_3B9EB7BB_UndisplacedPosXZAWS_7;
                        float _CrestComputeOceanDataFromDisplacedPosition_3B9EB7BB_LodAlpha_6;
                        float3 _CrestComputeOceanDataFromDisplacedPosition_3B9EB7BB_Displacement_8;
                        float _CrestComputeOceanDataFromDisplacedPosition_3B9EB7BB_OceanWaterDepth_1;
                        float _CrestComputeOceanDataFromDisplacedPosition_3B9EB7BB_Foam_2;
                        float2 _CrestComputeOceanDataFromDisplacedPosition_3B9EB7BB_Shadow_3;
                        float2 _CrestComputeOceanDataFromDisplacedPosition_3B9EB7BB_Flow_4;
                        float _CrestComputeOceanDataFromDisplacedPosition_3B9EB7BB_SubSurfaceScattering_5;
                        SG_CrestComputeOceanDataFromDisplacedPosition_1f483fe321b10c64e8b93a023ae60dd6(IN.AbsoluteWorldSpacePosition, _CrestComputeOceanDataFromDisplacedPosition_3B9EB7BB, _CrestComputeOceanDataFromDisplacedPosition_3B9EB7BB_UndisplacedPosXZAWS_7, _CrestComputeOceanDataFromDisplacedPosition_3B9EB7BB_LodAlpha_6, _CrestComputeOceanDataFromDisplacedPosition_3B9EB7BB_Displacement_8, _CrestComputeOceanDataFromDisplacedPosition_3B9EB7BB_OceanWaterDepth_1, _CrestComputeOceanDataFromDisplacedPosition_3B9EB7BB_Foam_2, _CrestComputeOceanDataFromDisplacedPosition_3B9EB7BB_Shadow_3, _CrestComputeOceanDataFromDisplacedPosition_3B9EB7BB_Flow_4, _CrestComputeOceanDataFromDisplacedPosition_3B9EB7BB_SubSurfaceScattering_5);
                        #if defined(CREST_GENERATED_SHADER_ON)
                        float2 _GENERATEDSHADER_71C0694B_Out_0 = _Vector2_552A5E1F_Out_0;
                        #else
                        float2 _GENERATEDSHADER_71C0694B_Out_0 = _CrestComputeOceanDataFromDisplacedPosition_3B9EB7BB_UndisplacedPosXZAWS_7;
                        #endif
                        #if defined(CREST_GENERATED_SHADER_ON)
                        float _GENERATEDSHADER_2A933A74_Out_0 = _Split_B10345C8_B_3;
                        #else
                        float _GENERATEDSHADER_2A933A74_Out_0 = _CrestComputeOceanDataFromDisplacedPosition_3B9EB7BB_LodAlpha_6;
                        #endif
                        #if defined(CREST_GENERATED_SHADER_ON)
                        float _GENERATEDSHADER_EFBF6036_Out_0 = _Split_B10345C8_A_4;
                        #else
                        float _GENERATEDSHADER_EFBF6036_Out_0 = _CrestComputeOceanDataFromDisplacedPosition_3B9EB7BB_OceanWaterDepth_1;
                        #endif
                        float4 _UV_39E1D2DF_Out_0 = IN.uv1;
                        float _Split_CB2CA9B8_R_1 = _UV_39E1D2DF_Out_0[0];
                        float _Split_CB2CA9B8_G_2 = _UV_39E1D2DF_Out_0[1];
                        float _Split_CB2CA9B8_B_3 = _UV_39E1D2DF_Out_0[2];
                        float _Split_CB2CA9B8_A_4 = _UV_39E1D2DF_Out_0[3];
                        #if defined(CREST_GENERATED_SHADER_ON)
                        float _GENERATEDSHADER_1BBAE801_Out_0 = _Split_CB2CA9B8_G_2;
                        #else
                        float _GENERATEDSHADER_1BBAE801_Out_0 = _CrestComputeOceanDataFromDisplacedPosition_3B9EB7BB_Foam_2;
                        #endif
                        float4 _UV_33A67BF5_Out_0 = IN.uv2;
                        float _Split_753DFB28_R_1 = _UV_33A67BF5_Out_0[0];
                        float _Split_753DFB28_G_2 = _UV_33A67BF5_Out_0[1];
                        float _Split_753DFB28_B_3 = _UV_33A67BF5_Out_0[2];
                        float _Split_753DFB28_A_4 = _UV_33A67BF5_Out_0[3];
                        float2 _Vector2_7883B8A6_Out_0 = float2(_Split_753DFB28_R_1, _Split_753DFB28_G_2);
                        #if defined(CREST_GENERATED_SHADER_ON)
                        float2 _GENERATEDSHADER_B499BDE6_Out_0 = _Vector2_7883B8A6_Out_0;
                        #else
                        float2 _GENERATEDSHADER_B499BDE6_Out_0 = _CrestComputeOceanDataFromDisplacedPosition_3B9EB7BB_Shadow_3;
                        #endif
                        float2 _Vector2_3A83E1FC_Out_0 = float2(_Split_753DFB28_B_3, _Split_753DFB28_A_4);
                        #if defined(CREST_GENERATED_SHADER_ON)
                        float2 _GENERATEDSHADER_84CB20AD_Out_0 = _Vector2_3A83E1FC_Out_0;
                        #else
                        float2 _GENERATEDSHADER_84CB20AD_Out_0 = _CrestComputeOceanDataFromDisplacedPosition_3B9EB7BB_Flow_4;
                        #endif
                        #if defined(CREST_GENERATED_SHADER_ON)
                        float _GENERATEDSHADER_6BDC98D1_Out_0 = _Split_CB2CA9B8_R_1;
                        #else
                        float _GENERATEDSHADER_6BDC98D1_Out_0 = _CrestComputeOceanDataFromDisplacedPosition_3B9EB7BB_SubSurfaceScattering_5;
                        #endif
                        PositionXZWSUndisp_2 = _GENERATEDSHADER_71C0694B_Out_0;
                        LodAlpha_1 = _GENERATEDSHADER_2A933A74_Out_0;
                        OceanDepth_3 = _GENERATEDSHADER_EFBF6036_Out_0;
                        Foam_4 = _GENERATEDSHADER_1BBAE801_Out_0;
                        Shadow_5 = _GENERATEDSHADER_B499BDE6_Out_0;
                        Flow_6 = _GENERATEDSHADER_84CB20AD_Out_0;
                        SubSurfaceScattering_7 = _GENERATEDSHADER_6BDC98D1_Out_0;
                    }
                
                    // 2a24b14f4becad7f23cf06f270571445
                    #include "ShadergraphFramework/CrestNodeDrivenInputs.hlsl"
                
                    struct Bindings_CrestDrivenData_40572ccadf7d9704a83d283fb4c9f19d
                    {
                    };
                
                    void SG_CrestDrivenData_40572ccadf7d9704a83d283fb4c9f19d(Bindings_CrestDrivenData_40572ccadf7d9704a83d283fb4c9f19d IN, out half MeshScaleAlpha_1, out half LodDataTexelSize_8, out half GeometryGridSize_2, out half3 OceanPosScale0_3, out half3 OceanPosScale1_4, out half4 OceanParams0_5, out half4 OceanParams1_6, out half SliceIndex0_7)
                    {
                        half _CustomFunction_CD9A5F8F_MeshScaleAlpha_9;
                        half _CustomFunction_CD9A5F8F_LodDataTexelSize_10;
                        half _CustomFunction_CD9A5F8F_GeometryGridSize_14;
                        half3 _CustomFunction_CD9A5F8F_OceanPosScale0_7;
                        half3 _CustomFunction_CD9A5F8F_OceanPosScale1_8;
                        half4 _CustomFunction_CD9A5F8F_OceanParams0_11;
                        half4 _CustomFunction_CD9A5F8F_OceanParams1_12;
                        half _CustomFunction_CD9A5F8F_SliceIndex0_13;
                        CrestOceanSurfaceValues_half(_CustomFunction_CD9A5F8F_MeshScaleAlpha_9, _CustomFunction_CD9A5F8F_LodDataTexelSize_10, _CustomFunction_CD9A5F8F_GeometryGridSize_14, _CustomFunction_CD9A5F8F_OceanPosScale0_7, _CustomFunction_CD9A5F8F_OceanPosScale1_8, _CustomFunction_CD9A5F8F_OceanParams0_11, _CustomFunction_CD9A5F8F_OceanParams1_12, _CustomFunction_CD9A5F8F_SliceIndex0_13);
                        MeshScaleAlpha_1 = _CustomFunction_CD9A5F8F_MeshScaleAlpha_9;
                        LodDataTexelSize_8 = _CustomFunction_CD9A5F8F_LodDataTexelSize_10;
                        GeometryGridSize_2 = _CustomFunction_CD9A5F8F_GeometryGridSize_14;
                        OceanPosScale0_3 = _CustomFunction_CD9A5F8F_OceanPosScale0_7;
                        OceanPosScale1_4 = _CustomFunction_CD9A5F8F_OceanPosScale1_8;
                        OceanParams0_5 = _CustomFunction_CD9A5F8F_OceanParams0_11;
                        OceanParams1_6 = _CustomFunction_CD9A5F8F_OceanParams1_12;
                        SliceIndex0_7 = _CustomFunction_CD9A5F8F_SliceIndex0_13;
                    }
                
                    // cb9e19acd0b7f8a1a1c0b9aed89ebacc
                    #include "ShadergraphFramework/CrestNodeSampleClipSurfaceData.hlsl"
                
                    struct Bindings_CrestClipSurface_8c73b4813486448849bd38d01267f186
                    {
                        float3 AbsoluteWorldSpacePosition;
                        half4 uv0;
                        half4 uv1;
                        half4 uv2;
                    };
                
                    void SG_CrestClipSurface_8c73b4813486448849bd38d01267f186(Bindings_CrestClipSurface_8c73b4813486448849bd38d01267f186 IN, out float ClipSurfaceValue_1)
                    {
                        float _Split_A2A81B90_R_1 = IN.AbsoluteWorldSpacePosition[0];
                        float _Split_A2A81B90_G_2 = IN.AbsoluteWorldSpacePosition[1];
                        float _Split_A2A81B90_B_3 = IN.AbsoluteWorldSpacePosition[2];
                        float _Split_A2A81B90_A_4 = 0;
                        float2 _Vector2_5DE2DA1_Out_0 = float2(_Split_A2A81B90_R_1, _Split_A2A81B90_B_3);
                        Bindings_CrestUnpackData_5bc4cf08f88035c489658d60edefeec8 _CrestUnpackData_ECB44FF9;
                        _CrestUnpackData_ECB44FF9.AbsoluteWorldSpacePosition = IN.AbsoluteWorldSpacePosition;
                        _CrestUnpackData_ECB44FF9.uv0 = IN.uv0;
                        _CrestUnpackData_ECB44FF9.uv1 = IN.uv1;
                        _CrestUnpackData_ECB44FF9.uv2 = IN.uv2;
                        float2 _CrestUnpackData_ECB44FF9_PositionXZWSUndisp_2;
                        float _CrestUnpackData_ECB44FF9_LodAlpha_1;
                        float _CrestUnpackData_ECB44FF9_OceanDepth_3;
                        float _CrestUnpackData_ECB44FF9_Foam_4;
                        float2 _CrestUnpackData_ECB44FF9_Shadow_5;
                        float2 _CrestUnpackData_ECB44FF9_Flow_6;
                        float _CrestUnpackData_ECB44FF9_SubSurfaceScattering_7;
                        SG_CrestUnpackData_5bc4cf08f88035c489658d60edefeec8(_CrestUnpackData_ECB44FF9, _CrestUnpackData_ECB44FF9_PositionXZWSUndisp_2, _CrestUnpackData_ECB44FF9_LodAlpha_1, _CrestUnpackData_ECB44FF9_OceanDepth_3, _CrestUnpackData_ECB44FF9_Foam_4, _CrestUnpackData_ECB44FF9_Shadow_5, _CrestUnpackData_ECB44FF9_Flow_6, _CrestUnpackData_ECB44FF9_SubSurfaceScattering_7);
                        Bindings_CrestDrivenData_40572ccadf7d9704a83d283fb4c9f19d _CrestDrivenData_EA2C4302;
                        half _CrestDrivenData_EA2C4302_MeshScaleAlpha_1;
                        half _CrestDrivenData_EA2C4302_LodDataTexelSize_8;
                        half _CrestDrivenData_EA2C4302_GeometryGridSize_2;
                        half3 _CrestDrivenData_EA2C4302_OceanPosScale0_3;
                        half3 _CrestDrivenData_EA2C4302_OceanPosScale1_4;
                        half4 _CrestDrivenData_EA2C4302_OceanParams0_5;
                        half4 _CrestDrivenData_EA2C4302_OceanParams1_6;
                        half _CrestDrivenData_EA2C4302_SliceIndex0_7;
                        SG_CrestDrivenData_40572ccadf7d9704a83d283fb4c9f19d(_CrestDrivenData_EA2C4302, _CrestDrivenData_EA2C4302_MeshScaleAlpha_1, _CrestDrivenData_EA2C4302_LodDataTexelSize_8, _CrestDrivenData_EA2C4302_GeometryGridSize_2, _CrestDrivenData_EA2C4302_OceanPosScale0_3, _CrestDrivenData_EA2C4302_OceanPosScale1_4, _CrestDrivenData_EA2C4302_OceanParams0_5, _CrestDrivenData_EA2C4302_OceanParams1_6, _CrestDrivenData_EA2C4302_SliceIndex0_7);
                        float _CustomFunction_26C15E74_ClipSurfaceValue_7;
                        CrestNodeSampleClipSurfaceData_float(_Vector2_5DE2DA1_Out_0, _CrestUnpackData_ECB44FF9_LodAlpha_1, _CrestDrivenData_EA2C4302_OceanPosScale0_3, _CrestDrivenData_EA2C4302_OceanPosScale1_4, _CrestDrivenData_EA2C4302_OceanParams0_5, _CrestDrivenData_EA2C4302_OceanParams1_6, _CrestDrivenData_EA2C4302_SliceIndex0_7, _CustomFunction_26C15E74_ClipSurfaceValue_7);
                        ClipSurfaceValue_1 = _CustomFunction_26C15E74_ClipSurfaceValue_7;
                    }
                
                    #include "ShadergraphFramework/CrestNodeGeoMorph.hlsl"
                
                    struct Bindings_CrestGeoMorph_9ab91ec3462438049923ca0ff16f68ad
                    {
                    };
                
                    void SG_CrestGeoMorph_9ab91ec3462438049923ca0ff16f68ad(float3 Vector3_28A0F264, float3 Vector3_F1111B56, float Vector1_691AFD6A, float Vector1_37DEE8F3, Bindings_CrestGeoMorph_9ab91ec3462438049923ca0ff16f68ad IN, out half3 MorphedPositionWS_1, out half LodAlpha_2)
                    {
                        float3 _Property_C4B6B1D5_Out_0 = Vector3_28A0F264;
                        float3 _Property_13BC6D1A_Out_0 = Vector3_F1111B56;
                        float _Property_DE4BC103_Out_0 = Vector1_691AFD6A;
                        float _Property_B3D2A4DF_Out_0 = Vector1_37DEE8F3;
                        half3 _CustomFunction_C8F1D6C4_MorphedPositionWS_4;
                        half _CustomFunction_C8F1D6C4_LodAlpha_5;
                        GeoMorph_half(_Property_C4B6B1D5_Out_0, _Property_13BC6D1A_Out_0, _Property_DE4BC103_Out_0, _Property_B3D2A4DF_Out_0, _CustomFunction_C8F1D6C4_MorphedPositionWS_4, _CustomFunction_C8F1D6C4_LodAlpha_5);
                        MorphedPositionWS_1 = _CustomFunction_C8F1D6C4_MorphedPositionWS_4;
                        LodAlpha_2 = _CustomFunction_C8F1D6C4_LodAlpha_5;
                    }
                
                    void Unity_Add_float3(float3 A, float3 B, out float3 Out)
                    {
                        Out = A + B;
                    }
                
                // Vertex Graph Evaluation
                    VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
                    {
                        VertexDescription description = (VertexDescription)0;
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        Bindings_CrestDrivenData_40572ccadf7d9704a83d283fb4c9f19d _CrestDrivenData_A1078169;
                        half _CrestDrivenData_A1078169_MeshScaleAlpha_1;
                        half _CrestDrivenData_A1078169_LodDataTexelSize_8;
                        half _CrestDrivenData_A1078169_GeometryGridSize_2;
                        half3 _CrestDrivenData_A1078169_OceanPosScale0_3;
                        half3 _CrestDrivenData_A1078169_OceanPosScale1_4;
                        half4 _CrestDrivenData_A1078169_OceanParams0_5;
                        half4 _CrestDrivenData_A1078169_OceanParams1_6;
                        half _CrestDrivenData_A1078169_SliceIndex0_7;
                        SG_CrestDrivenData_40572ccadf7d9704a83d283fb4c9f19d(_CrestDrivenData_A1078169, _CrestDrivenData_A1078169_MeshScaleAlpha_1, _CrestDrivenData_A1078169_LodDataTexelSize_8, _CrestDrivenData_A1078169_GeometryGridSize_2, _CrestDrivenData_A1078169_OceanPosScale0_3, _CrestDrivenData_A1078169_OceanPosScale1_4, _CrestDrivenData_A1078169_OceanParams0_5, _CrestDrivenData_A1078169_OceanParams1_6, _CrestDrivenData_A1078169_SliceIndex0_7);
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        Bindings_CrestGeoMorph_9ab91ec3462438049923ca0ff16f68ad _CrestGeoMorph_8F1A4FF1;
                        half3 _CrestGeoMorph_8F1A4FF1_MorphedPositionWS_1;
                        half _CrestGeoMorph_8F1A4FF1_LodAlpha_2;
                        SG_CrestGeoMorph_9ab91ec3462438049923ca0ff16f68ad(IN.AbsoluteWorldSpacePosition, _CrestDrivenData_A1078169_OceanPosScale0_3, _CrestDrivenData_A1078169_MeshScaleAlpha_1, _CrestDrivenData_A1078169_GeometryGridSize_2, _CrestGeoMorph_8F1A4FF1, _CrestGeoMorph_8F1A4FF1_MorphedPositionWS_1, _CrestGeoMorph_8F1A4FF1_LodAlpha_2);
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        float _Split_CC063A43_R_1 = _CrestGeoMorph_8F1A4FF1_MorphedPositionWS_1[0];
                        float _Split_CC063A43_G_2 = _CrestGeoMorph_8F1A4FF1_MorphedPositionWS_1[1];
                        float _Split_CC063A43_B_3 = _CrestGeoMorph_8F1A4FF1_MorphedPositionWS_1[2];
                        float _Split_CC063A43_A_4 = 0;
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        float2 _Vector2_2D46FD43_Out_0 = float2(_Split_CC063A43_R_1, _Split_CC063A43_B_3);
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        Bindings_CrestSampleOceanData_f19d6d2aa60f8294e870f100303ee5a4 _CrestSampleOceanData_340A6610;
                        float3 _CrestSampleOceanData_340A6610_Displacement_1;
                        float _CrestSampleOceanData_340A6610_OceanWaterDepth_5;
                        float _CrestSampleOceanData_340A6610_Foam_6;
                        float2 _CrestSampleOceanData_340A6610_Shadow_7;
                        float2 _CrestSampleOceanData_340A6610_Flow_8;
                        float _CrestSampleOceanData_340A6610_SubSurfaceScattering_9;
                        SG_CrestSampleOceanData_f19d6d2aa60f8294e870f100303ee5a4(_Vector2_2D46FD43_Out_0, _CrestGeoMorph_8F1A4FF1_LodAlpha_2, _CrestDrivenData_A1078169_OceanPosScale0_3, _CrestDrivenData_A1078169_OceanPosScale1_4, _CrestDrivenData_A1078169_OceanParams0_5, _CrestDrivenData_A1078169_OceanParams1_6, _CrestDrivenData_A1078169_SliceIndex0_7, _CrestSampleOceanData_340A6610, _CrestSampleOceanData_340A6610_Displacement_1, _CrestSampleOceanData_340A6610_OceanWaterDepth_5, _CrestSampleOceanData_340A6610_Foam_6, _CrestSampleOceanData_340A6610_Shadow_7, _CrestSampleOceanData_340A6610_Flow_8, _CrestSampleOceanData_340A6610_SubSurfaceScattering_9);
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        float3 _Add_2D79A354_Out_2;
                        Unity_Add_float3(_CrestGeoMorph_8F1A4FF1_MorphedPositionWS_1, _CrestSampleOceanData_340A6610_Displacement_1, _Add_2D79A354_Out_2);
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        float3 _Transform_9C2ACF28_Out_1 = TransformWorldToObject(GetCameraRelativePositionWS(_Add_2D79A354_Out_2.xyz));
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        float3 _Transform_86205EE4_Out_1 = TransformWorldToObjectDir(float3 (0, 1, 0).xyz);
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        float3 _Transform_22BD1A85_Out_1 = TransformWorldToObjectDir(float3 (1, 0, 0).xyz);
                        #endif
                        description.VertexPosition = _Transform_9C2ACF28_Out_1;
                        description.VertexNormal = _Transform_86205EE4_Out_1;
                        description.VertexTangent = _Transform_22BD1A85_Out_1;
                        return description;
                    }
                    
                // Pixel Graph Evaluation
                    SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
                    {
                        SurfaceDescription surface = (SurfaceDescription)0;
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        float _SceneDepth_E2A24470_Out_1;
                        Unity_SceneDepth_Eye_float(float4(IN.ScreenPosition.xy / IN.ScreenPosition.w, 0, 0), _SceneDepth_E2A24470_Out_1);
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        float _Split_68C2565_R_1 = IN.ViewSpacePosition[0];
                        float _Split_68C2565_G_2 = IN.ViewSpacePosition[1];
                        float _Split_68C2565_B_3 = IN.ViewSpacePosition[2];
                        float _Split_68C2565_A_4 = 0;
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        float _Negate_847D9376_Out_1;
                        Unity_Negate_float(_Split_68C2565_B_3, _Negate_847D9376_Out_1);
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        float _Subtract_B338E0_Out_2;
                        Unity_Subtract_float(_SceneDepth_E2A24470_Out_1, _Negate_847D9376_Out_1, _Subtract_B338E0_Out_2);
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        float _Remap_6CD3C222_Out_3;
                        Unity_Remap_float(_Subtract_B338E0_Out_2, float2 (0, 0.2), float2 (0, 1), _Remap_6CD3C222_Out_3);
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        float _Saturate_FEF634C4_Out_1;
                        Unity_Saturate_float(_Remap_6CD3C222_Out_3, _Saturate_FEF634C4_Out_1);
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        Bindings_CrestClipSurface_8c73b4813486448849bd38d01267f186 _CrestClipSurface_AA3EF9C5;
                        _CrestClipSurface_AA3EF9C5.AbsoluteWorldSpacePosition = IN.AbsoluteWorldSpacePosition;
                        _CrestClipSurface_AA3EF9C5.uv0 = IN.uv0;
                        _CrestClipSurface_AA3EF9C5.uv1 = IN.uv1;
                        _CrestClipSurface_AA3EF9C5.uv2 = IN.uv2;
                        float _CrestClipSurface_AA3EF9C5_ClipSurfaceValue_1;
                        SG_CrestClipSurface_8c73b4813486448849bd38d01267f186(_CrestClipSurface_AA3EF9C5, _CrestClipSurface_AA3EF9C5_ClipSurfaceValue_1);
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        float _Subtract_900E903A_Out_2;
                        Unity_Subtract_float(_Saturate_FEF634C4_Out_1, _CrestClipSurface_AA3EF9C5_ClipSurfaceValue_1, _Subtract_900E903A_Out_2);
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        float _Saturate_A1AFAC8_Out_1;
                        Unity_Saturate_float(_Subtract_900E903A_Out_2, _Saturate_A1AFAC8_Out_1);
                        #endif
                        surface.Alpha = _Saturate_A1AFAC8_Out_1;
                        surface.AlphaClipThreshold = 0;
                        return surface;
                    }
                    
            //-------------------------------------------------------------------------------------
            // End graph generated code
            //-------------------------------------------------------------------------------------
        
        //-------------------------------------------------------------------------------------
            // TEMPLATE INCLUDE : VertexAnimation.template.hlsl
            //-------------------------------------------------------------------------------------
            
            
            VertexDescriptionInputs AttributesMeshToVertexDescriptionInputs(AttributesMesh input)
            {
                VertexDescriptionInputs output;
                ZERO_INITIALIZE(VertexDescriptionInputs, output);
            
            
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
            output.WorldSpaceNormal =            TransformObjectToWorldNormal(input.normalOS);
            #endif
            
            
            
            
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
            output.WorldSpaceTangent =           TransformObjectToWorldDir(input.tangentOS.xyz);
            #endif
            
            
            
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
            output.ObjectSpaceBiTangent =        normalize(cross(input.normalOS.xyz, input.tangentOS.xyz) * (input.tangentOS.w > 0.0f ? 1.0f : -1.0f) * GetOddNegativeScale());
            #endif
            
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
            output.WorldSpaceBiTangent =         TransformObjectToWorldDir(output.ObjectSpaceBiTangent);
            #endif
            
            
            
            
            
            
            
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
            output.AbsoluteWorldSpacePosition =  GetAbsolutePositionWS(TransformObjectToWorld(input.positionOS));
            #endif
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
                return output;
            }
            
            AttributesMesh ApplyMeshModification(AttributesMesh input, float3 timeParameters)
            {
                // build graph inputs
                VertexDescriptionInputs vertexDescriptionInputs = AttributesMeshToVertexDescriptionInputs(input);
                // Override time paramters with used one (This is required to correctly handle motion vector for vertex animation based on time)
            
            
                // evaluate vertex graph
                VertexDescription vertexDescription = VertexDescriptionFunction(vertexDescriptionInputs);
            
                // copy graph output to the results
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
            input.positionOS = vertexDescription.VertexPosition;
            #endif
            
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
            input.normalOS = vertexDescription.VertexNormal;
            #endif
            
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
            input.tangentOS.xyz = vertexDescription.VertexTangent;
            #endif
            
            
                return input;
            }
            
            //-------------------------------------------------------------------------------------
            // END TEMPLATE INCLUDE : VertexAnimation.template.hlsl
            //-------------------------------------------------------------------------------------
            
        
        //-------------------------------------------------------------------------------------
            // TEMPLATE INCLUDE : SharedCode.template.hlsl
            //-------------------------------------------------------------------------------------
            
            #if !defined(SHADER_STAGE_RAY_TRACING)
                FragInputs BuildFragInputs(VaryingsMeshToPS input)
                {
                    FragInputs output;
                    ZERO_INITIALIZE(FragInputs, output);
            
                    // Init to some default value to make the computer quiet (else it output 'divide by zero' warning even if value is not used).
                    // TODO: this is a really poor workaround, but the variable is used in a bunch of places
                    // to compute normals which are then passed on elsewhere to compute other values...
                    output.tangentToWorld = k_identity3x3;
                    output.positionSS = input.positionCS;       // input.positionCS is SV_Position
            
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
            output.positionRWS = input.positionRWS;
            #endif
            
            
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
            output.texCoord0 = input.texCoord0;
            #endif
            
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
            output.texCoord1 = input.texCoord1;
            #endif
            
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
            output.texCoord2 = input.texCoord2;
            #endif
            
            
            
                    #if _DOUBLESIDED_ON && SHADER_STAGE_FRAGMENT
                    output.isFrontFace = IS_FRONT_VFACE(input.cullFace, true, false);
                    #elif SHADER_STAGE_FRAGMENT
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
            output.isFrontFace = IS_FRONT_VFACE(input.cullFace, true, false);
            #endif
            
                    #endif // SHADER_STAGE_FRAGMENT
            
                    return output;
                }
            #endif
                SurfaceDescriptionInputs FragInputsToSurfaceDescriptionInputs(FragInputs input, float3 viewWS)
                {
                    SurfaceDescriptionInputs output;
                    ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
            output.WorldSpacePosition =          input.positionRWS;
            #endif
            
            
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
            output.ViewSpacePosition =           TransformWorldToView(input.positionRWS);
            #endif
            
            
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
            output.AbsoluteWorldSpacePosition =  GetAbsolutePositionWS(input.positionRWS);
            #endif
            
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
            output.ScreenPosition =              ComputeScreenPos(TransformWorldToHClip(input.positionRWS), _ProjectionParams.x);
            #endif
            
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
            output.uv0 =                         input.texCoord0;
            #endif
            
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
            output.uv1 =                         input.texCoord1;
            #endif
            
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
            output.uv2 =                         input.texCoord2;
            #endif
            
            
            
            
            
            
                    return output;
                }
            
            #if !defined(SHADER_STAGE_RAY_TRACING)
            
                // existing HDRP code uses the combined function to go directly from packed to frag inputs
                FragInputs UnpackVaryingsMeshToFragInputs(PackedVaryingsMeshToPS input)
                {
                    UNITY_SETUP_INSTANCE_ID(input);
                    VaryingsMeshToPS unpacked= UnpackVaryingsMeshToPS(input);
                    return BuildFragInputs(unpacked);
                }
            #endif
            
            //-------------------------------------------------------------------------------------
            // END TEMPLATE INCLUDE : SharedCode.template.hlsl
            //-------------------------------------------------------------------------------------
            
        
        
            void BuildSurfaceData(FragInputs fragInputs, inout SurfaceDescription surfaceDescription, float3 V, PositionInputs posInput, out SurfaceData surfaceData)
            {
                // setup defaults -- these are used if the graph doesn't output a value
                ZERO_INITIALIZE(SurfaceData, surfaceData);
                surfaceData.ambientOcclusion = 1.0;
                surfaceData.specularOcclusion = 1.0; // This need to be init here to quiet the compiler in case of decal, but can be override later.
        
                // copy across graph values, if defined
        
        
        
        
        
        
                // These static material feature allow compile time optimization
                surfaceData.materialFeatures = MATERIALFEATUREFLAGS_LIT_STANDARD;
        #ifdef _MATERIAL_FEATURE_SPECULAR_COLOR
                surfaceData.materialFeatures |= MATERIALFEATUREFLAGS_LIT_SPECULAR_COLOR;
        #endif
        
                float3 doubleSidedConstants = float3(1.0, 1.0, 1.0);
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
        doubleSidedConstants = float3( 1.0,  1.0, -1.0);
        #endif
        
        
                // normal delivered to master node
                float3 normalSrc = float3(0.0f, 0.0f, 1.0f);
        
        
                // compute world space normal
        #if _NORMAL_DROPOFF_TS
                GetNormalWS(fragInputs, normalSrc, surfaceData.normalWS, doubleSidedConstants);
        #elif _NORMAL_DROPOFF_OS
        		surfaceData.normalWS = TransformObjectToWorldNormal(normalSrc);
        #elif _NORMAL_DROPOFF_WS
        		surfaceData.normalWS = normalSrc;
        #endif
        
                surfaceData.geomNormalWS = fragInputs.tangentToWorld[2];
                surfaceData.tangentWS = normalize(fragInputs.tangentToWorld[0].xyz);    // The tangent is not normalize in tangentToWorld for mikkt. TODO: Check if it expected that we normalize with Morten. Tag: SURFACE_GRADIENT
        
        #if HAVE_DECALS
                if (_EnableDecals)
                {
                    // Both uses and modifies 'surfaceData.normalWS'.
                    DecalSurfaceData decalSurfaceData = GetDecalSurfaceData(posInput, surfaceDescription.Alpha);
                    ApplyDecalToSurfaceData(decalSurfaceData, surfaceData);
                }
        #endif
        
                surfaceData.tangentWS = Orthonormalize(surfaceData.tangentWS, surfaceData.normalWS);
        
                // By default we use the ambient occlusion with Tri-ace trick (apply outside) for specular occlusion as PBR master node don't have any option
                surfaceData.specularOcclusion = GetSpecularOcclusionFromAmbientOcclusion(ClampNdotV(dot(surfaceData.normalWS, V)), surfaceData.ambientOcclusion, PerceptualSmoothnessToRoughness(surfaceData.perceptualSmoothness));
        
        #ifdef DEBUG_DISPLAY
                if (_DebugMipMapMode != DEBUGMIPMAPMODE_NONE)
                {
                    // TODO: need to update mip info
                    surfaceData.metallic = 0;
                }
        
                // We need to call ApplyDebugToSurfaceData after filling the surfarcedata and before filling builtinData
                // as it can modify attribute use for static lighting
                ApplyDebugToSurfaceData(fragInputs.tangentToWorld, surfaceData);
        #endif
            }
        
            void GetSurfaceAndBuiltinData(FragInputs fragInputs, float3 V, inout PositionInputs posInput, out SurfaceData surfaceData, out BuiltinData builtinData)
            {
        #ifdef LOD_FADE_CROSSFADE // enable dithering LOD transition if user select CrossFade transition in LOD group
                LODDitheringTransition(ComputeFadeMaskSeed(V, posInput.positionSS), unity_LODFade.x);
        #endif
        
                float3 doubleSidedConstants = float3(1.0, 1.0, 1.0);
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
        doubleSidedConstants = float3( 1.0,  1.0, -1.0);
        #endif
        
        
                ApplyDoubleSidedFlipOrMirror(fragInputs, doubleSidedConstants);
        
                SurfaceDescriptionInputs surfaceDescriptionInputs = FragInputsToSurfaceDescriptionInputs(fragInputs, V);
                SurfaceDescription surfaceDescription = SurfaceDescriptionFunction(surfaceDescriptionInputs);
        
                // Perform alpha test very early to save performance (a killed pixel will not sample textures)
                // TODO: split graph evaluation to grab just alpha dependencies first? tricky..
        
        
                BuildSurfaceData(fragInputs, surfaceDescription, V, posInput, surfaceData);
        
                // Builtin Data
                // For back lighting we use the oposite vertex normal
                InitBuiltinData(posInput, surfaceDescription.Alpha, surfaceData.normalWS, -fragInputs.tangentToWorld[2], fragInputs.texCoord1, fragInputs.texCoord2, builtinData);
        
        
        
                PostInitBuiltinData(V, posInput, surfaceData, builtinData);
            }
        
            //-------------------------------------------------------------------------------------
            // Pass Includes
            //-------------------------------------------------------------------------------------
                #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl"
            //-------------------------------------------------------------------------------------
            // End Pass Includes
            //-------------------------------------------------------------------------------------
        
            ENDHLSL
        }
        
        Pass
        {
            // based on HDPBRPass.template
            Name "META"
            Tags { "LightMode" = "META" }
        
            //-------------------------------------------------------------------------------------
            // Render Modes (Blend, Cull, ZTest, Stencil, etc)
            //-------------------------------------------------------------------------------------
            
			// Culling user defined - can be turned off for underwater
			Cull[_CullMode]

            
            
            
            
            
            //-------------------------------------------------------------------------------------
            // End Render Modes
            //-------------------------------------------------------------------------------------
        
            HLSLPROGRAM
        
            #pragma target 4.5
            #pragma only_renderers d3d11 ps4 xboxone vulkan metal switch
            //#pragma enable_d3d11_debug_symbols
        
            #pragma multi_compile_instancing
        #pragma instancing_options renderinglayer
        
            #pragma multi_compile _ LOD_FADE_CROSSFADE
        
            //-------------------------------------------------------------------------------------
            // Graph Defines
            //-------------------------------------------------------------------------------------
                    #define SHADERPASS SHADERPASS_LIGHT_TRANSPORT
                #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                #define REQUIRE_DEPTH_TEXTURE
                #endif
                #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                #define REQUIRE_OPAQUE_TEXTURE
                #endif
                // ACTIVE FIELDS:
                //   DoubleSided
                //   DoubleSided.Mirror
                //   FragInputs.isFrontFace
                //   Material.SpecularColor
                //   features.NormalDropOffTS
                //   SurfaceType.Transparent
                //   BlendMode.Alpha
                //   AlphaFog
                //   SurfaceDescription.Albedo
                //   SurfaceDescription.Normal
                //   SurfaceDescription.Specular
                //   SurfaceDescription.Emission
                //   SurfaceDescription.Smoothness
                //   SurfaceDescription.Occlusion
                //   SurfaceDescription.Alpha
                //   SurfaceDescription.AlphaClipThreshold
                //   features.modifyMesh
                //   AttributesMesh.normalOS
                //   AttributesMesh.tangentOS
                //   AttributesMesh.uv0
                //   AttributesMesh.uv1
                //   AttributesMesh.color
                //   AttributesMesh.uv2
                //   VaryingsMeshToPS.cullFace
                // Shared Graph Keywords
                    #pragma shader_feature_local _ CREST_FOAM_ON
                    #pragma shader_feature_local _ CREST_CAUSTICS_ON
                    #pragma shader_feature_local _ CREST_FLOW_ON
                
                    #if defined(CREST_FOAM_ON) && defined(CREST_CAUSTICS_ON) && defined(CREST_FLOW_ON)
                        #define KEYWORD_PERMUTATION_0
                    #elif defined(CREST_FOAM_ON) && defined(CREST_CAUSTICS_ON)
                        #define KEYWORD_PERMUTATION_1
                    #elif defined(CREST_FOAM_ON) && defined(CREST_FLOW_ON)
                        #define KEYWORD_PERMUTATION_2
                    #elif defined(CREST_FOAM_ON)
                        #define KEYWORD_PERMUTATION_3
                    #elif defined(CREST_CAUSTICS_ON) && defined(CREST_FLOW_ON)
                        #define KEYWORD_PERMUTATION_4
                    #elif defined(CREST_CAUSTICS_ON)
                        #define KEYWORD_PERMUTATION_5
                    #elif defined(CREST_FLOW_ON)
                        #define KEYWORD_PERMUTATION_6
                    #else
                        #define KEYWORD_PERMUTATION_7
                    #endif
                
                
            //-------------------------------------------------------------------------------------
            // End Defines
            //-------------------------------------------------------------------------------------
        
            //-------------------------------------------------------------------------------------
            // Variant Definitions (active field translations to HDRP defines)
            //-------------------------------------------------------------------------------------
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
        #define _MATERIAL_FEATURE_SPECULAR_COLOR 1
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
        #define _SURFACE_TYPE_TRANSPARENT 1
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
        #define _BLENDMODE_ALPHA 1
        #endif
        
        
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
        #define _DOUBLESIDED_ON 1
        #endif
        
            #define _NORMAL_DROPOFF_TS	1
            // #define _NORMAL_DROPOFF_OS	1
            // #define _NORMAL_DROPOFF_WS	1
        
			#define CREST_GENERATED_SHADER_ON 1

            //-------------------------------------------------------------------------------------
            // End Variant Definitions
            //-------------------------------------------------------------------------------------
        
            #pragma vertex Vert
            #pragma fragment Frag
        
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/NormalSurfaceGradient.hlsl"
        
            // define FragInputs structure
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl"
        
            //-------------------------------------------------------------------------------------
            // Active Field Defines
            //-------------------------------------------------------------------------------------
        
            // this translates the new dependency tracker into the old preprocessor definitions for the existing HDRP shader code
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
        #define ATTRIBUTES_NEED_NORMAL
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
        #define ATTRIBUTES_NEED_TANGENT
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
        #define ATTRIBUTES_NEED_TEXCOORD0
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
        #define ATTRIBUTES_NEED_TEXCOORD1
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
        #define ATTRIBUTES_NEED_TEXCOORD2
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
        #define ATTRIBUTES_NEED_TEXCOORD3
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
        #define ATTRIBUTES_NEED_COLOR
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
        #define VARYINGS_NEED_POSITION_WS
        #endif
        
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
        #define VARYINGS_NEED_TEXCOORD0
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
        #define VARYINGS_NEED_TEXCOORD1
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
        #define VARYINGS_NEED_TEXCOORD2
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
        #define VARYINGS_NEED_TEXCOORD3
        #endif
        
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
        #define VARYINGS_NEED_CULLFACE
        #endif
        
            #define HAVE_MESH_MODIFICATION
        
            //-------------------------------------------------------------------------------------
            // End Defines
            //-------------------------------------------------------------------------------------
        	
        
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #ifdef DEBUG_DISPLAY
                #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Debug/DebugDisplay.hlsl"
            #endif
        
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl"
        
        #if (SHADERPASS == SHADERPASS_FORWARD)
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/Lighting.hlsl"
        
            #define HAS_LIGHTLOOP
        
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoopDef.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/Lit.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoop.hlsl"
        #else
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/Lit.hlsl"
        #endif
        
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalUtilities.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/LitDecalData.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl"
        
            //Used by SceneSelectionPass
            int _ObjectId;
            int _PassValue;
        
            //-------------------------------------------------------------------------------------
            // Interpolator Packing And Struct Declarations
            //-------------------------------------------------------------------------------------
            // Generated Type: AttributesMesh
            struct AttributesMesh
            {
                #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                float3 positionOS : POSITION;
                #endif // Shader Graph Keywords
                #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                float3 normalOS : NORMAL; // optional
                #endif // Shader Graph Keywords
                #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                float4 tangentOS : TANGENT; // optional
                #endif // Shader Graph Keywords
                #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                float4 uv0 : TEXCOORD0; // optional
                #endif // Shader Graph Keywords
                #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                float4 uv1 : TEXCOORD1; // optional
                #endif // Shader Graph Keywords
                #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                float4 uv2 : TEXCOORD2; // optional
                #endif // Shader Graph Keywords
                #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                float4 uv3 : TEXCOORD3; // optional
                #endif // Shader Graph Keywords
                #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                float4 color : COLOR; // optional
                #endif // Shader Graph Keywords
                #if UNITY_ANY_INSTANCING_ENABLED
                #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                uint instanceID : INSTANCEID_SEMANTIC;
                #endif // Shader Graph Keywords
                #endif // UNITY_ANY_INSTANCING_ENABLED
            };
            // Generated Type: VaryingsMeshToPS
            struct VaryingsMeshToPS
            {
                #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                float4 positionCS : SV_POSITION;
                #endif // Shader Graph Keywords
                #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                float3 positionRWS; // optional
                #endif // Shader Graph Keywords
                #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                float4 texCoord0; // optional
                #endif // Shader Graph Keywords
                #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                float4 texCoord1; // optional
                #endif // Shader Graph Keywords
                #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                float4 texCoord2; // optional
                #endif // Shader Graph Keywords
                #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                float4 texCoord3; // optional
                #endif // Shader Graph Keywords
                #if UNITY_ANY_INSTANCING_ENABLED
                #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                uint instanceID : CUSTOM_INSTANCE_ID;
                #endif // Shader Graph Keywords
                #endif // UNITY_ANY_INSTANCING_ENABLED
                #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
                #endif // Shader Graph Keywords
                #endif // defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            };
            
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
            // Generated Type: PackedVaryingsMeshToPS
            struct PackedVaryingsMeshToPS
            {
                float4 positionCS : SV_POSITION; // unpacked
                #if UNITY_ANY_INSTANCING_ENABLED
                uint instanceID : CUSTOM_INSTANCE_ID; // unpacked
                #endif // conditional
                float3 interp00 : TEXCOORD0; // auto-packed
                float4 interp01 : TEXCOORD1; // auto-packed
                float4 interp02 : TEXCOORD2; // auto-packed
                float4 interp03 : TEXCOORD3; // auto-packed
                float4 interp04 : TEXCOORD4; // auto-packed
                #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC; // unpacked
                #endif // conditional
            };
            
            // Packed Type: VaryingsMeshToPS
            PackedVaryingsMeshToPS PackVaryingsMeshToPS(VaryingsMeshToPS input)
            {
                PackedVaryingsMeshToPS output = (PackedVaryingsMeshToPS)0;
                output.positionCS = input.positionCS;
                output.interp00.xyz = input.positionRWS;
                output.interp01.xyzw = input.texCoord0;
                output.interp02.xyzw = input.texCoord1;
                output.interp03.xyzw = input.texCoord2;
                output.interp04.xyzw = input.texCoord3;
                #if UNITY_ANY_INSTANCING_ENABLED
                output.instanceID = input.instanceID;
                #endif // conditional
                #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                output.cullFace = input.cullFace;
                #endif // conditional
                return output;
            }
            
            // Unpacked Type: VaryingsMeshToPS
            VaryingsMeshToPS UnpackVaryingsMeshToPS(PackedVaryingsMeshToPS input)
            {
                VaryingsMeshToPS output = (VaryingsMeshToPS)0;
                output.positionCS = input.positionCS;
                output.positionRWS = input.interp00.xyz;
                output.texCoord0 = input.interp01.xyzw;
                output.texCoord1 = input.interp02.xyzw;
                output.texCoord2 = input.interp03.xyzw;
                output.texCoord3 = input.interp04.xyzw;
                #if UNITY_ANY_INSTANCING_ENABLED
                output.instanceID = input.instanceID;
                #endif // conditional
                #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                output.cullFace = input.cullFace;
                #endif // conditional
                return output;
            }
            #endif
            // Generated Type: VaryingsMeshToDS
            struct VaryingsMeshToDS
            {
                #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                float3 positionRWS;
                #endif // Shader Graph Keywords
                #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                float3 normalWS;
                #endif // Shader Graph Keywords
                #if UNITY_ANY_INSTANCING_ENABLED
                #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                uint instanceID : CUSTOM_INSTANCE_ID;
                #endif // Shader Graph Keywords
                #endif // UNITY_ANY_INSTANCING_ENABLED
            };
            
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
            // Generated Type: PackedVaryingsMeshToDS
            struct PackedVaryingsMeshToDS
            {
                #if UNITY_ANY_INSTANCING_ENABLED
                uint instanceID : CUSTOM_INSTANCE_ID; // unpacked
                #endif // conditional
                float3 interp00 : TEXCOORD0; // auto-packed
                float3 interp01 : TEXCOORD1; // auto-packed
            };
            
            // Packed Type: VaryingsMeshToDS
            PackedVaryingsMeshToDS PackVaryingsMeshToDS(VaryingsMeshToDS input)
            {
                PackedVaryingsMeshToDS output = (PackedVaryingsMeshToDS)0;
                output.interp00.xyz = input.positionRWS;
                output.interp01.xyz = input.normalWS;
                #if UNITY_ANY_INSTANCING_ENABLED
                output.instanceID = input.instanceID;
                #endif // conditional
                return output;
            }
            
            // Unpacked Type: VaryingsMeshToDS
            VaryingsMeshToDS UnpackVaryingsMeshToDS(PackedVaryingsMeshToDS input)
            {
                VaryingsMeshToDS output = (VaryingsMeshToDS)0;
                output.positionRWS = input.interp00.xyz;
                output.normalWS = input.interp01.xyz;
                #if UNITY_ANY_INSTANCING_ENABLED
                output.instanceID = input.instanceID;
                #endif // conditional
                return output;
            }
            #endif
            //-------------------------------------------------------------------------------------
            // End Interpolator Packing And Struct Declarations
            //-------------------------------------------------------------------------------------
        
            //-------------------------------------------------------------------------------------
            // Graph generated code
            //-------------------------------------------------------------------------------------
                    // Shared Graph Properties (uniform inputs)
                    CBUFFER_START(UnityPerMaterial)
                    half _NormalsScale;
                    half _NormalsStrength;
                    half4 _ScatterColourBase;
                    half4 _ScatterColourShadow;
                    float4 _ScatterColourShallow;
                    half _ScatterColourShallowDepthMax;
                    half _ScatterColourShallowDepthFalloff;
                    half _SSSIntensityBase;
                    half _SSSIntensitySun;
                    half4 _SSSTint;
                    half _SSSSunFalloff;
                    float _Specular;
                    float _Occlusion;
                    half _Smoothness;
                    float _SmoothnessFar;
                    float _SmoothnessFarDistance;
                    float _SmoothnessFalloff;
                    float _MinReflectionDirectionY;
                    half _FoamScale;
                    half _FoamFeather;
                    half _FoamIntensityAlbedo;
                    half _FoamIntensityEmissive;
                    half _FoamSmoothness;
                    half _FoamNormalStrength;
                    half4 _FoamBubbleColor;
                    half _FoamBubbleParallax;
                    half _FoamBubblesCoverage;
                    half _RefractionStrength;
                    half3 _DepthFogDensity;
                    float _CausticsTextureScale;
                    float _CausticsTextureAverage;
                    float _CausticsStrength;
                    float _CausticsFocalDepth;
                    float _CausticsDepthOfField;
                    float _CausticsDistortionStrength;
                    float _CausticsDistortionScale;
                    CBUFFER_END
                    TEXTURE2D(_TextureNormals); SAMPLER(sampler_TextureNormals); float4 _TextureNormals_TexelSize;
                    TEXTURE2D(_TextureFoam); SAMPLER(sampler_TextureFoam); half4 _TextureFoam_TexelSize;
                    TEXTURE2D(_CausticsTexture); SAMPLER(sampler_CausticsTexture); float4 _CausticsTexture_TexelSize;
                    TEXTURE2D(_CausticsDistortionTexture); SAMPLER(sampler_CausticsDistortionTexture); float4 _CausticsDistortionTexture_TexelSize;
                
                // Vertex Graph Inputs
                    struct VertexDescriptionInputs
                    {
                    };
                // Vertex Graph Outputs
                    struct VertexDescription
                    {
                    };
                    
                // Pixel Graph Inputs
                    struct SurfaceDescriptionInputs
                    {
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        float3 WorldSpaceViewDirection; // optional
                        #endif // Shader Graph Keywords
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        float3 ViewSpacePosition; // optional
                        #endif // Shader Graph Keywords
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        float3 WorldSpacePosition; // optional
                        #endif // Shader Graph Keywords
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        float3 AbsoluteWorldSpacePosition; // optional
                        #endif // Shader Graph Keywords
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        float4 ScreenPosition; // optional
                        #endif // Shader Graph Keywords
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        float4 uv0; // optional
                        #endif // Shader Graph Keywords
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        float4 uv1; // optional
                        #endif // Shader Graph Keywords
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        float4 uv2; // optional
                        #endif // Shader Graph Keywords
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        float4 uv3; // optional
                        #endif // Shader Graph Keywords
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        float FaceSign; // optional
                        #endif // Shader Graph Keywords
                    };
                // Pixel Graph Outputs
                    struct SurfaceDescription
                    {
                        float3 Albedo;
                        float3 Normal;
                        float3 Specular;
                        float3 Emission;
                        float Smoothness;
                        float Occlusion;
                        float Alpha;
                        float AlphaClipThreshold;
                    };
                    
                // Shared Graph Node Functions
                
                    // 73abdd69b930b80b268ce9c97db7e24b
                    #include "ShadergraphFramework/CrestNodeComputeSamplingData.hlsl"
                
                    // 2973156898769ad700b9d1820ecfd177
                    #include "ShadergraphFramework/CrestNodeSampleOceanData.hlsl"
                
                    struct Bindings_CrestSampleOceanData_f19d6d2aa60f8294e870f100303ee5a4
                    {
                    };
                
                    void SG_CrestSampleOceanData_f19d6d2aa60f8294e870f100303ee5a4(float2 Vector2_3171933F, float Vector1_CD41515B, float3 Vector3_7E91D336, float3 Vector3_3A95DCDF, float4 Vector4_C0B2B5EA, float4 Vector4_9C46108E, float Vector1_8EA8B92B, Bindings_CrestSampleOceanData_f19d6d2aa60f8294e870f100303ee5a4 IN, out float3 Displacement_1, out float OceanWaterDepth_5, out float Foam_6, out float2 Shadow_7, out float2 Flow_8, out float SubSurfaceScattering_9)
                    {
                        float2 _Property_1A287CC6_Out_0 = Vector2_3171933F;
                        float _Property_2D1D1700_Out_0 = Vector1_CD41515B;
                        float3 _Property_C925A867_Out_0 = Vector3_7E91D336;
                        float3 _Property_6C273401_Out_0 = Vector3_3A95DCDF;
                        float4 _Property_467D1BE7_Out_0 = Vector4_C0B2B5EA;
                        float4 _Property_4E045F45_Out_0 = Vector4_9C46108E;
                        float _Property_59E019ED_Out_0 = Vector1_8EA8B92B;
                        float3 _CustomFunction_487C31E1_Displacement_3;
                        float _CustomFunction_487C31E1_OceanDepth_8;
                        float _CustomFunction_487C31E1_Foam_4;
                        float2 _CustomFunction_487C31E1_Shadow_5;
                        float2 _CustomFunction_487C31E1_Flow_6;
                        float _CustomFunction_487C31E1_SSS_17;
                        CrestNodeSampleOceanData_float(_Property_1A287CC6_Out_0, _Property_2D1D1700_Out_0, _Property_C925A867_Out_0, _Property_6C273401_Out_0, _Property_467D1BE7_Out_0, _Property_4E045F45_Out_0, _Property_59E019ED_Out_0, _CustomFunction_487C31E1_Displacement_3, _CustomFunction_487C31E1_OceanDepth_8, _CustomFunction_487C31E1_Foam_4, _CustomFunction_487C31E1_Shadow_5, _CustomFunction_487C31E1_Flow_6, _CustomFunction_487C31E1_SSS_17);
                        Displacement_1 = _CustomFunction_487C31E1_Displacement_3;
                        OceanWaterDepth_5 = _CustomFunction_487C31E1_OceanDepth_8;
                        Foam_6 = _CustomFunction_487C31E1_Foam_4;
                        Shadow_7 = _CustomFunction_487C31E1_Shadow_5;
                        Flow_8 = _CustomFunction_487C31E1_Flow_6;
                        SubSurfaceScattering_9 = _CustomFunction_487C31E1_SSS_17;
                    }
                
                    void Unity_Subtract_float2(float2 A, float2 B, out float2 Out)
                    {
                        Out = A - B;
                    }
                
                    struct Bindings_CrestComputeOceanDataFromDisplacedPosition_1f483fe321b10c64e8b93a023ae60dd6
                    {
                    };
                
                    void SG_CrestComputeOceanDataFromDisplacedPosition_1f483fe321b10c64e8b93a023ae60dd6(float3 Vector3_A7B8495A, Bindings_CrestComputeOceanDataFromDisplacedPosition_1f483fe321b10c64e8b93a023ae60dd6 IN, out float2 UndisplacedPosXZAWS_7, out float LodAlpha_6, out float3 Displacement_8, out float OceanWaterDepth_1, out float Foam_2, out float2 Shadow_3, out float2 Flow_4, out float SubSurfaceScattering_5)
                    {
                        float3 _Property_232E7FDA_Out_0 = Vector3_A7B8495A;
                        float _Split_8E8A6DCA_R_1 = _Property_232E7FDA_Out_0[0];
                        float _Split_8E8A6DCA_G_2 = _Property_232E7FDA_Out_0[1];
                        float _Split_8E8A6DCA_B_3 = _Property_232E7FDA_Out_0[2];
                        float _Split_8E8A6DCA_A_4 = 0;
                        float2 _Vector2_A3499051_Out_0 = float2(_Split_8E8A6DCA_R_1, _Split_8E8A6DCA_B_3);
                        half _CustomFunction_A082C8F2_LodAlpha_3;
                        half3 _CustomFunction_A082C8F2_OceanPosScale0_4;
                        half3 _CustomFunction_A082C8F2_OceanPosScale1_5;
                        half4 _CustomFunction_A082C8F2_OceanParams0_6;
                        half4 _CustomFunction_A082C8F2_OceanParams1_7;
                        half _CustomFunction_A082C8F2_Slice0_1;
                        half _CustomFunction_A082C8F2_Slice1_2;
                        CrestComputeSamplingData_half(_Vector2_A3499051_Out_0, _CustomFunction_A082C8F2_LodAlpha_3, _CustomFunction_A082C8F2_OceanPosScale0_4, _CustomFunction_A082C8F2_OceanPosScale1_5, _CustomFunction_A082C8F2_OceanParams0_6, _CustomFunction_A082C8F2_OceanParams1_7, _CustomFunction_A082C8F2_Slice0_1, _CustomFunction_A082C8F2_Slice1_2);
                        Bindings_CrestSampleOceanData_f19d6d2aa60f8294e870f100303ee5a4 _CrestSampleOceanData_87ACC65F;
                        float3 _CrestSampleOceanData_87ACC65F_Displacement_1;
                        float _CrestSampleOceanData_87ACC65F_OceanWaterDepth_5;
                        float _CrestSampleOceanData_87ACC65F_Foam_6;
                        float2 _CrestSampleOceanData_87ACC65F_Shadow_7;
                        float2 _CrestSampleOceanData_87ACC65F_Flow_8;
                        float _CrestSampleOceanData_87ACC65F_SubSurfaceScattering_9;
                        SG_CrestSampleOceanData_f19d6d2aa60f8294e870f100303ee5a4(_Vector2_A3499051_Out_0, _CustomFunction_A082C8F2_LodAlpha_3, _CustomFunction_A082C8F2_OceanPosScale0_4, _CustomFunction_A082C8F2_OceanPosScale1_5, _CustomFunction_A082C8F2_OceanParams0_6, _CustomFunction_A082C8F2_OceanParams1_7, _CustomFunction_A082C8F2_Slice0_1, _CrestSampleOceanData_87ACC65F, _CrestSampleOceanData_87ACC65F_Displacement_1, _CrestSampleOceanData_87ACC65F_OceanWaterDepth_5, _CrestSampleOceanData_87ACC65F_Foam_6, _CrestSampleOceanData_87ACC65F_Shadow_7, _CrestSampleOceanData_87ACC65F_Flow_8, _CrestSampleOceanData_87ACC65F_SubSurfaceScattering_9);
                        float _Split_CD3A9051_R_1 = _CrestSampleOceanData_87ACC65F_Displacement_1[0];
                        float _Split_CD3A9051_G_2 = _CrestSampleOceanData_87ACC65F_Displacement_1[1];
                        float _Split_CD3A9051_B_3 = _CrestSampleOceanData_87ACC65F_Displacement_1[2];
                        float _Split_CD3A9051_A_4 = 0;
                        float2 _Vector2_B8C0C1F0_Out_0 = float2(_Split_CD3A9051_R_1, _Split_CD3A9051_B_3);
                        float2 _Subtract_8977A663_Out_2;
                        Unity_Subtract_float2(_Vector2_A3499051_Out_0, _Vector2_B8C0C1F0_Out_0, _Subtract_8977A663_Out_2);
                        half _CustomFunction_9D8B14F0_LodAlpha_3;
                        half3 _CustomFunction_9D8B14F0_OceanPosScale0_4;
                        half3 _CustomFunction_9D8B14F0_OceanPosScale1_5;
                        half4 _CustomFunction_9D8B14F0_OceanParams0_6;
                        half4 _CustomFunction_9D8B14F0_OceanParams1_7;
                        half _CustomFunction_9D8B14F0_Slice0_1;
                        half _CustomFunction_9D8B14F0_Slice1_2;
                        CrestComputeSamplingData_half(_Subtract_8977A663_Out_2, _CustomFunction_9D8B14F0_LodAlpha_3, _CustomFunction_9D8B14F0_OceanPosScale0_4, _CustomFunction_9D8B14F0_OceanPosScale1_5, _CustomFunction_9D8B14F0_OceanParams0_6, _CustomFunction_9D8B14F0_OceanParams1_7, _CustomFunction_9D8B14F0_Slice0_1, _CustomFunction_9D8B14F0_Slice1_2);
                        Bindings_CrestSampleOceanData_f19d6d2aa60f8294e870f100303ee5a4 _CrestSampleOceanData_D8619779;
                        float3 _CrestSampleOceanData_D8619779_Displacement_1;
                        float _CrestSampleOceanData_D8619779_OceanWaterDepth_5;
                        float _CrestSampleOceanData_D8619779_Foam_6;
                        float2 _CrestSampleOceanData_D8619779_Shadow_7;
                        float2 _CrestSampleOceanData_D8619779_Flow_8;
                        float _CrestSampleOceanData_D8619779_SubSurfaceScattering_9;
                        SG_CrestSampleOceanData_f19d6d2aa60f8294e870f100303ee5a4(_Subtract_8977A663_Out_2, _CustomFunction_9D8B14F0_LodAlpha_3, _CustomFunction_9D8B14F0_OceanPosScale0_4, _CustomFunction_9D8B14F0_OceanPosScale1_5, _CustomFunction_9D8B14F0_OceanParams0_6, _CustomFunction_9D8B14F0_OceanParams1_7, _CustomFunction_9D8B14F0_Slice0_1, _CrestSampleOceanData_D8619779, _CrestSampleOceanData_D8619779_Displacement_1, _CrestSampleOceanData_D8619779_OceanWaterDepth_5, _CrestSampleOceanData_D8619779_Foam_6, _CrestSampleOceanData_D8619779_Shadow_7, _CrestSampleOceanData_D8619779_Flow_8, _CrestSampleOceanData_D8619779_SubSurfaceScattering_9);
                        float _Split_1616DE7_R_1 = _CrestSampleOceanData_D8619779_Displacement_1[0];
                        float _Split_1616DE7_G_2 = _CrestSampleOceanData_D8619779_Displacement_1[1];
                        float _Split_1616DE7_B_3 = _CrestSampleOceanData_D8619779_Displacement_1[2];
                        float _Split_1616DE7_A_4 = 0;
                        float2 _Vector2_B871614F_Out_0 = float2(_Split_1616DE7_R_1, _Split_1616DE7_B_3);
                        float2 _Subtract_39E2CE30_Out_2;
                        Unity_Subtract_float2(_Vector2_A3499051_Out_0, _Vector2_B871614F_Out_0, _Subtract_39E2CE30_Out_2);
                        half _CustomFunction_10AEAD9A_LodAlpha_3;
                        half3 _CustomFunction_10AEAD9A_OceanPosScale0_4;
                        half3 _CustomFunction_10AEAD9A_OceanPosScale1_5;
                        half4 _CustomFunction_10AEAD9A_OceanParams0_6;
                        half4 _CustomFunction_10AEAD9A_OceanParams1_7;
                        half _CustomFunction_10AEAD9A_Slice0_1;
                        half _CustomFunction_10AEAD9A_Slice1_2;
                        CrestComputeSamplingData_half(_Subtract_39E2CE30_Out_2, _CustomFunction_10AEAD9A_LodAlpha_3, _CustomFunction_10AEAD9A_OceanPosScale0_4, _CustomFunction_10AEAD9A_OceanPosScale1_5, _CustomFunction_10AEAD9A_OceanParams0_6, _CustomFunction_10AEAD9A_OceanParams1_7, _CustomFunction_10AEAD9A_Slice0_1, _CustomFunction_10AEAD9A_Slice1_2);
                        Bindings_CrestSampleOceanData_f19d6d2aa60f8294e870f100303ee5a4 _CrestSampleOceanData_A1195FE2;
                        float3 _CrestSampleOceanData_A1195FE2_Displacement_1;
                        float _CrestSampleOceanData_A1195FE2_OceanWaterDepth_5;
                        float _CrestSampleOceanData_A1195FE2_Foam_6;
                        float2 _CrestSampleOceanData_A1195FE2_Shadow_7;
                        float2 _CrestSampleOceanData_A1195FE2_Flow_8;
                        float _CrestSampleOceanData_A1195FE2_SubSurfaceScattering_9;
                        SG_CrestSampleOceanData_f19d6d2aa60f8294e870f100303ee5a4(_Subtract_39E2CE30_Out_2, _CustomFunction_10AEAD9A_LodAlpha_3, _CustomFunction_10AEAD9A_OceanPosScale0_4, _CustomFunction_10AEAD9A_OceanPosScale1_5, _CustomFunction_10AEAD9A_OceanParams0_6, _CustomFunction_10AEAD9A_OceanParams1_7, _CustomFunction_10AEAD9A_Slice0_1, _CrestSampleOceanData_A1195FE2, _CrestSampleOceanData_A1195FE2_Displacement_1, _CrestSampleOceanData_A1195FE2_OceanWaterDepth_5, _CrestSampleOceanData_A1195FE2_Foam_6, _CrestSampleOceanData_A1195FE2_Shadow_7, _CrestSampleOceanData_A1195FE2_Flow_8, _CrestSampleOceanData_A1195FE2_SubSurfaceScattering_9);
                        UndisplacedPosXZAWS_7 = _Subtract_39E2CE30_Out_2;
                        LodAlpha_6 = _CustomFunction_10AEAD9A_LodAlpha_3;
                        Displacement_8 = _CrestSampleOceanData_A1195FE2_Displacement_1;
                        OceanWaterDepth_1 = _CrestSampleOceanData_A1195FE2_OceanWaterDepth_5;
                        Foam_2 = _CrestSampleOceanData_A1195FE2_Foam_6;
                        Shadow_3 = _CrestSampleOceanData_A1195FE2_Shadow_7;
                        Flow_4 = _CrestSampleOceanData_A1195FE2_Flow_8;
                        SubSurfaceScattering_5 = _CrestSampleOceanData_A1195FE2_SubSurfaceScattering_9;
                    }
                
                    struct Bindings_CrestUnpackData_5bc4cf08f88035c489658d60edefeec8
                    {
                        float3 AbsoluteWorldSpacePosition;
                        half4 uv0;
                        half4 uv1;
                        half4 uv2;
                    };
                
                    void SG_CrestUnpackData_5bc4cf08f88035c489658d60edefeec8(Bindings_CrestUnpackData_5bc4cf08f88035c489658d60edefeec8 IN, out float2 PositionXZWSUndisp_2, out float LodAlpha_1, out float OceanDepth_3, out float Foam_4, out float2 Shadow_5, out float2 Flow_6, out float SubSurfaceScattering_7)
                    {
                        float4 _UV_CF6CD5F2_Out_0 = IN.uv0;
                        float _Split_B10345C8_R_1 = _UV_CF6CD5F2_Out_0[0];
                        float _Split_B10345C8_G_2 = _UV_CF6CD5F2_Out_0[1];
                        float _Split_B10345C8_B_3 = _UV_CF6CD5F2_Out_0[2];
                        float _Split_B10345C8_A_4 = _UV_CF6CD5F2_Out_0[3];
                        float2 _Vector2_552A5E1F_Out_0 = float2(_Split_B10345C8_R_1, _Split_B10345C8_G_2);
                        Bindings_CrestComputeOceanDataFromDisplacedPosition_1f483fe321b10c64e8b93a023ae60dd6 _CrestComputeOceanDataFromDisplacedPosition_3B9EB7BB;
                        float2 _CrestComputeOceanDataFromDisplacedPosition_3B9EB7BB_UndisplacedPosXZAWS_7;
                        float _CrestComputeOceanDataFromDisplacedPosition_3B9EB7BB_LodAlpha_6;
                        float3 _CrestComputeOceanDataFromDisplacedPosition_3B9EB7BB_Displacement_8;
                        float _CrestComputeOceanDataFromDisplacedPosition_3B9EB7BB_OceanWaterDepth_1;
                        float _CrestComputeOceanDataFromDisplacedPosition_3B9EB7BB_Foam_2;
                        float2 _CrestComputeOceanDataFromDisplacedPosition_3B9EB7BB_Shadow_3;
                        float2 _CrestComputeOceanDataFromDisplacedPosition_3B9EB7BB_Flow_4;
                        float _CrestComputeOceanDataFromDisplacedPosition_3B9EB7BB_SubSurfaceScattering_5;
                        SG_CrestComputeOceanDataFromDisplacedPosition_1f483fe321b10c64e8b93a023ae60dd6(IN.AbsoluteWorldSpacePosition, _CrestComputeOceanDataFromDisplacedPosition_3B9EB7BB, _CrestComputeOceanDataFromDisplacedPosition_3B9EB7BB_UndisplacedPosXZAWS_7, _CrestComputeOceanDataFromDisplacedPosition_3B9EB7BB_LodAlpha_6, _CrestComputeOceanDataFromDisplacedPosition_3B9EB7BB_Displacement_8, _CrestComputeOceanDataFromDisplacedPosition_3B9EB7BB_OceanWaterDepth_1, _CrestComputeOceanDataFromDisplacedPosition_3B9EB7BB_Foam_2, _CrestComputeOceanDataFromDisplacedPosition_3B9EB7BB_Shadow_3, _CrestComputeOceanDataFromDisplacedPosition_3B9EB7BB_Flow_4, _CrestComputeOceanDataFromDisplacedPosition_3B9EB7BB_SubSurfaceScattering_5);
                        #if defined(CREST_GENERATED_SHADER_ON)
                        float2 _GENERATEDSHADER_71C0694B_Out_0 = _Vector2_552A5E1F_Out_0;
                        #else
                        float2 _GENERATEDSHADER_71C0694B_Out_0 = _CrestComputeOceanDataFromDisplacedPosition_3B9EB7BB_UndisplacedPosXZAWS_7;
                        #endif
                        #if defined(CREST_GENERATED_SHADER_ON)
                        float _GENERATEDSHADER_2A933A74_Out_0 = _Split_B10345C8_B_3;
                        #else
                        float _GENERATEDSHADER_2A933A74_Out_0 = _CrestComputeOceanDataFromDisplacedPosition_3B9EB7BB_LodAlpha_6;
                        #endif
                        #if defined(CREST_GENERATED_SHADER_ON)
                        float _GENERATEDSHADER_EFBF6036_Out_0 = _Split_B10345C8_A_4;
                        #else
                        float _GENERATEDSHADER_EFBF6036_Out_0 = _CrestComputeOceanDataFromDisplacedPosition_3B9EB7BB_OceanWaterDepth_1;
                        #endif
                        float4 _UV_39E1D2DF_Out_0 = IN.uv1;
                        float _Split_CB2CA9B8_R_1 = _UV_39E1D2DF_Out_0[0];
                        float _Split_CB2CA9B8_G_2 = _UV_39E1D2DF_Out_0[1];
                        float _Split_CB2CA9B8_B_3 = _UV_39E1D2DF_Out_0[2];
                        float _Split_CB2CA9B8_A_4 = _UV_39E1D2DF_Out_0[3];
                        #if defined(CREST_GENERATED_SHADER_ON)
                        float _GENERATEDSHADER_1BBAE801_Out_0 = _Split_CB2CA9B8_G_2;
                        #else
                        float _GENERATEDSHADER_1BBAE801_Out_0 = _CrestComputeOceanDataFromDisplacedPosition_3B9EB7BB_Foam_2;
                        #endif
                        float4 _UV_33A67BF5_Out_0 = IN.uv2;
                        float _Split_753DFB28_R_1 = _UV_33A67BF5_Out_0[0];
                        float _Split_753DFB28_G_2 = _UV_33A67BF5_Out_0[1];
                        float _Split_753DFB28_B_3 = _UV_33A67BF5_Out_0[2];
                        float _Split_753DFB28_A_4 = _UV_33A67BF5_Out_0[3];
                        float2 _Vector2_7883B8A6_Out_0 = float2(_Split_753DFB28_R_1, _Split_753DFB28_G_2);
                        #if defined(CREST_GENERATED_SHADER_ON)
                        float2 _GENERATEDSHADER_B499BDE6_Out_0 = _Vector2_7883B8A6_Out_0;
                        #else
                        float2 _GENERATEDSHADER_B499BDE6_Out_0 = _CrestComputeOceanDataFromDisplacedPosition_3B9EB7BB_Shadow_3;
                        #endif
                        float2 _Vector2_3A83E1FC_Out_0 = float2(_Split_753DFB28_B_3, _Split_753DFB28_A_4);
                        #if defined(CREST_GENERATED_SHADER_ON)
                        float2 _GENERATEDSHADER_84CB20AD_Out_0 = _Vector2_3A83E1FC_Out_0;
                        #else
                        float2 _GENERATEDSHADER_84CB20AD_Out_0 = _CrestComputeOceanDataFromDisplacedPosition_3B9EB7BB_Flow_4;
                        #endif
                        #if defined(CREST_GENERATED_SHADER_ON)
                        float _GENERATEDSHADER_6BDC98D1_Out_0 = _Split_CB2CA9B8_R_1;
                        #else
                        float _GENERATEDSHADER_6BDC98D1_Out_0 = _CrestComputeOceanDataFromDisplacedPosition_3B9EB7BB_SubSurfaceScattering_5;
                        #endif
                        PositionXZWSUndisp_2 = _GENERATEDSHADER_71C0694B_Out_0;
                        LodAlpha_1 = _GENERATEDSHADER_2A933A74_Out_0;
                        OceanDepth_3 = _GENERATEDSHADER_EFBF6036_Out_0;
                        Foam_4 = _GENERATEDSHADER_1BBAE801_Out_0;
                        Shadow_5 = _GENERATEDSHADER_B499BDE6_Out_0;
                        Flow_6 = _GENERATEDSHADER_84CB20AD_Out_0;
                        SubSurfaceScattering_7 = _GENERATEDSHADER_6BDC98D1_Out_0;
                    }
                
                    #include "ShadergraphFramework/CrestNodeLightData.hlsl"
                
                    struct Bindings_CrestLightData_b74b6e8c0b489314ca7aea3e2cc9c54c
                    {
                    };
                
                    void SG_CrestLightData_b74b6e8c0b489314ca7aea3e2cc9c54c(Bindings_CrestLightData_b74b6e8c0b489314ca7aea3e2cc9c54c IN, out half3 Direction_1, out half3 Intensity_2)
                    {
                        half3 _CustomFunction_5D41A6E0_Direction_0;
                        half3 _CustomFunction_5D41A6E0_Colour_1;
                        CrestNodeLightData_half(_CustomFunction_5D41A6E0_Direction_0, _CustomFunction_5D41A6E0_Colour_1);
                        Direction_1 = _CustomFunction_5D41A6E0_Direction_0;
                        Intensity_2 = _CustomFunction_5D41A6E0_Colour_1;
                    }
                
                    // 2a24b14f4becad7f23cf06f270571445
                    #include "ShadergraphFramework/CrestNodeDrivenInputs.hlsl"
                
                    struct Bindings_CrestDrivenData_40572ccadf7d9704a83d283fb4c9f19d
                    {
                    };
                
                    void SG_CrestDrivenData_40572ccadf7d9704a83d283fb4c9f19d(Bindings_CrestDrivenData_40572ccadf7d9704a83d283fb4c9f19d IN, out half MeshScaleAlpha_1, out half LodDataTexelSize_8, out half GeometryGridSize_2, out half3 OceanPosScale0_3, out half3 OceanPosScale1_4, out half4 OceanParams0_5, out half4 OceanParams1_6, out half SliceIndex0_7)
                    {
                        half _CustomFunction_CD9A5F8F_MeshScaleAlpha_9;
                        half _CustomFunction_CD9A5F8F_LodDataTexelSize_10;
                        half _CustomFunction_CD9A5F8F_GeometryGridSize_14;
                        half3 _CustomFunction_CD9A5F8F_OceanPosScale0_7;
                        half3 _CustomFunction_CD9A5F8F_OceanPosScale1_8;
                        half4 _CustomFunction_CD9A5F8F_OceanParams0_11;
                        half4 _CustomFunction_CD9A5F8F_OceanParams1_12;
                        half _CustomFunction_CD9A5F8F_SliceIndex0_13;
                        CrestOceanSurfaceValues_half(_CustomFunction_CD9A5F8F_MeshScaleAlpha_9, _CustomFunction_CD9A5F8F_LodDataTexelSize_10, _CustomFunction_CD9A5F8F_GeometryGridSize_14, _CustomFunction_CD9A5F8F_OceanPosScale0_7, _CustomFunction_CD9A5F8F_OceanPosScale1_8, _CustomFunction_CD9A5F8F_OceanParams0_11, _CustomFunction_CD9A5F8F_OceanParams1_12, _CustomFunction_CD9A5F8F_SliceIndex0_13);
                        MeshScaleAlpha_1 = _CustomFunction_CD9A5F8F_MeshScaleAlpha_9;
                        LodDataTexelSize_8 = _CustomFunction_CD9A5F8F_LodDataTexelSize_10;
                        GeometryGridSize_2 = _CustomFunction_CD9A5F8F_GeometryGridSize_14;
                        OceanPosScale0_3 = _CustomFunction_CD9A5F8F_OceanPosScale0_7;
                        OceanPosScale1_4 = _CustomFunction_CD9A5F8F_OceanPosScale1_8;
                        OceanParams0_5 = _CustomFunction_CD9A5F8F_OceanParams0_11;
                        OceanParams1_6 = _CustomFunction_CD9A5F8F_OceanParams1_12;
                        SliceIndex0_7 = _CustomFunction_CD9A5F8F_SliceIndex0_13;
                    }
                
                    void Unity_Normalize_float3(float3 In, out float3 Out)
                    {
                        Out = normalize(In);
                    }
                
                    void Unity_Not_float(float In, out float Out)
                    {
                        Out = !In;
                    }
                
                    struct Bindings_CrestIsUnderwater_52f7750f15e114937b54a0fd27f0d2f2
                    {
                        float FaceSign;
                    };
                
                    void SG_CrestIsUnderwater_52f7750f15e114937b54a0fd27f0d2f2(Bindings_CrestIsUnderwater_52f7750f15e114937b54a0fd27f0d2f2 IN, out float OutBoolean_1)
                    {
                        float _IsFrontFace_F6DF08D5_Out_0 = max(0, IN.FaceSign);
                        float _Not_3B19614D_Out_1;
                        Unity_Not_float(_IsFrontFace_F6DF08D5_Out_0, _Not_3B19614D_Out_1);
                        OutBoolean_1 = _Not_3B19614D_Out_1;
                    }
                
                    // 3c543d21fa1847bbfea4c56ca61fd4f3
                    #include "ShadergraphFramework/CrestNodeNormalMapping.hlsl"
                
                    struct Bindings_CrestComputeNormal_61b9efc6612ab3b4f84174344af5e12a
                    {
                        float FaceSign;
                    };
                
                    void SG_CrestComputeNormal_61b9efc6612ab3b4f84174344af5e12a(float3 Vector3_FE793823, float3 Vector3_C8190B61, float4 Vector4_F18E4948, float4 Vector4_43DD8E03, float Vector1_8771A258, TEXTURE2D_PARAM(Texture2D_6CA3A26C, samplerTexture2D_6CA3A26C), float4 Texture2D_6CA3A26C_TexelSize, float Vector1_418D6270, float Vector1_6EC9A7C0, float Vector1_5D9D8139, float2 Vector2_3ED47A62, float2 Vector2_891575B0, float3 Vector3_A9F402BF, float Vector1_2ABAF0E6, Bindings_CrestComputeNormal_61b9efc6612ab3b4f84174344af5e12a IN, out half3 Normal_1)
                    {
                        float3 _Property_9021A08B_Out_0 = Vector3_FE793823;
                        float3 _Property_9C8BC1F1_Out_0 = Vector3_C8190B61;
                        float4 _Property_BA13B38B_Out_0 = Vector4_F18E4948;
                        float4 _Property_587E24D5_Out_0 = Vector4_43DD8E03;
                        float _Property_1A49C52D_Out_0 = Vector1_8771A258;
                        float _Property_514FBFB9_Out_0 = Vector1_418D6270;
                        float _Property_27A6DF1E_Out_0 = Vector1_6EC9A7C0;
                        float _Property_A277E64F_Out_0 = Vector1_5D9D8139;
                        float2 _Property_805F9A1D_Out_0 = Vector2_3ED47A62;
                        float2 _Property_100A6EB8_Out_0 = Vector2_891575B0;
                        float3 _Property_11AD0CE_Out_0 = Vector3_A9F402BF;
                        Bindings_CrestIsUnderwater_52f7750f15e114937b54a0fd27f0d2f2 _CrestIsUnderwater_BA18A1A;
                        _CrestIsUnderwater_BA18A1A.FaceSign = IN.FaceSign;
                        float _CrestIsUnderwater_BA18A1A_OutBoolean_1;
                        SG_CrestIsUnderwater_52f7750f15e114937b54a0fd27f0d2f2(_CrestIsUnderwater_BA18A1A, _CrestIsUnderwater_BA18A1A_OutBoolean_1);
                        float _Property_347CBD07_Out_0 = Vector1_2ABAF0E6;
                        half3 _CustomFunction_61A7F8B0_NormalTS_1;
                        OceanNormals_half(_Property_9021A08B_Out_0, _Property_9C8BC1F1_Out_0, _Property_BA13B38B_Out_0, _Property_587E24D5_Out_0, _Property_1A49C52D_Out_0, Texture2D_6CA3A26C, _Property_514FBFB9_Out_0, _Property_27A6DF1E_Out_0, _Property_A277E64F_Out_0, _Property_805F9A1D_Out_0, _Property_100A6EB8_Out_0, _Property_11AD0CE_Out_0, _CrestIsUnderwater_BA18A1A_OutBoolean_1, _Property_347CBD07_Out_0, _CustomFunction_61A7F8B0_NormalTS_1);
                        Normal_1 = _CustomFunction_61A7F8B0_NormalTS_1;
                    }
                
                    // 8e3c4891a0a191b55617faf4fca7b22b
                    #include "ShadergraphFramework/CrestNodeApplyFresnel.hlsl"
                
                    struct Bindings_CrestFresnel_c6f6c13c4fdb04e42b427e4c2610d3c9
                    {
                        float FaceSign;
                    };
                
                    void SG_CrestFresnel_c6f6c13c4fdb04e42b427e4c2610d3c9(float3 Vector3_FFFD5D37, float3 Vector3_50713CBB, float Vector1_C2909293, float Vector1_DDF5B66E, Bindings_CrestFresnel_c6f6c13c4fdb04e42b427e4c2610d3c9 IN, out float LightTransmitted_1, out float LightReflected_2)
                    {
                        float3 _Property_3166A32_Out_0 = Vector3_50713CBB;
                        float3 _Property_ED2FFB18_Out_0 = Vector3_FFFD5D37;
                        Bindings_CrestIsUnderwater_52f7750f15e114937b54a0fd27f0d2f2 _CrestIsUnderwater_291EED16;
                        _CrestIsUnderwater_291EED16.FaceSign = IN.FaceSign;
                        float _CrestIsUnderwater_291EED16_OutBoolean_1;
                        SG_CrestIsUnderwater_52f7750f15e114937b54a0fd27f0d2f2(_CrestIsUnderwater_291EED16, _CrestIsUnderwater_291EED16_OutBoolean_1);
                        float _Property_D5B7D059_Out_0 = Vector1_DDF5B66E;
                        float _Property_2395CED2_Out_0 = Vector1_C2909293;
                        float _CustomFunction_6DEBC54E_LightTransmitted_1;
                        float _CustomFunction_6DEBC54E_LightReflected_10;
                        CrestNodeApplyFresnel_float(_Property_3166A32_Out_0, _Property_ED2FFB18_Out_0, _CrestIsUnderwater_291EED16_OutBoolean_1, _Property_D5B7D059_Out_0, _Property_2395CED2_Out_0, _CustomFunction_6DEBC54E_LightTransmitted_1, _CustomFunction_6DEBC54E_LightReflected_10);
                        LightTransmitted_1 = _CustomFunction_6DEBC54E_LightTransmitted_1;
                        LightReflected_2 = _CustomFunction_6DEBC54E_LightReflected_10;
                    }
                
                    #include "ShadergraphFramework/CrestNodeAmbientLight.hlsl"
                
                    struct Bindings_CrestAmbientLight_a6ec89b3ca0ab4e98b300ec3ba0e6013
                    {
                    };
                
                    void SG_CrestAmbientLight_a6ec89b3ca0ab4e98b300ec3ba0e6013(Bindings_CrestAmbientLight_a6ec89b3ca0ab4e98b300ec3ba0e6013 IN, out half3 Color_1)
                    {
                        half3 _CustomFunction_84E91696_AmbientLighting_0;
                        CrestNodeAmbientLight_half(_CustomFunction_84E91696_AmbientLighting_0);
                        Color_1 = _CustomFunction_84E91696_AmbientLighting_0;
                    }
                
                    // cfa426252240e9cb5f56ca043dd9bcce
                    #include "ShadergraphFramework/CrestNodeLightWaterVolume.hlsl"
                
                    struct Bindings_CrestVolumeLighting_e9ed6e11710a50640bb4b811d0fa84f2
                    {
                    };
                
                    void SG_CrestVolumeLighting_e9ed6e11710a50640bb4b811d0fa84f2(float4 Vector4_7C3B7892, float3 Vector3_CB562741, float4 Vector4_1925E051, float Vector1_E556918C, float Vector1_67342C54, float Vector1_D7DF1AB, float Vector1_6823579F, float4 Vector4_79A1BE1F, float Vector1_7223C6AC, float Vector1_66E9E54, float2 Vector2_D3558D33, float Vector1_96B7F6DA, float3 Vector3_1559C54, float3 Vector3_EBF06BD4, float3 Vector3_4BB1E4A, float3 Vector3_938EEF71, float3 Vector3_CAD84B7A, Bindings_CrestVolumeLighting_e9ed6e11710a50640bb4b811d0fa84f2 IN, out half3 VolumeLighting_1)
                    {
                        float4 _Property_3BF186D2_Out_0 = Vector4_7C3B7892;
                        float3 _Property_36BD3B33_Out_0 = Vector3_CB562741;
                        float4 _Property_DEEB4017_Out_0 = Vector4_1925E051;
                        float _Property_DEF8261_Out_0 = Vector1_E556918C;
                        float _Property_E59433A0_Out_0 = Vector1_67342C54;
                        float _Property_B909C546_Out_0 = Vector1_D7DF1AB;
                        float _Property_E4FBA75D_Out_0 = Vector1_6823579F;
                        float4 _Property_856C2495_Out_0 = Vector4_79A1BE1F;
                        float _Property_D81EDA3D_Out_0 = Vector1_7223C6AC;
                        float _Property_9368CB3D_Out_0 = Vector1_66E9E54;
                        float2 _Property_BF038BF7_Out_0 = Vector2_D3558D33;
                        float _Property_F35AF73E_Out_0 = Vector1_96B7F6DA;
                        float3 _Property_32B01413_Out_0 = Vector3_1559C54;
                        float3 _Property_5B1BCADB_Out_0 = Vector3_EBF06BD4;
                        float3 _Property_6BBACEFC_Out_0 = Vector3_4BB1E4A;
                        float3 _Property_C8DE9461_Out_0 = Vector3_938EEF71;
                        float3 _Property_919832B1_Out_0 = Vector3_CAD84B7A;
                        half3 _CustomFunction_F6F194A9_VolumeLighting_5;
                        CrestNodeLightWaterVolume_half((_Property_3BF186D2_Out_0.xyz), _Property_36BD3B33_Out_0, (_Property_DEEB4017_Out_0.xyz), _Property_DEF8261_Out_0, _Property_E59433A0_Out_0, _Property_B909C546_Out_0, _Property_E4FBA75D_Out_0, (_Property_856C2495_Out_0.xyz), _Property_D81EDA3D_Out_0, _Property_9368CB3D_Out_0, _Property_BF038BF7_Out_0, _Property_F35AF73E_Out_0, _Property_32B01413_Out_0, _Property_5B1BCADB_Out_0, _Property_6BBACEFC_Out_0, _Property_C8DE9461_Out_0, _Property_919832B1_Out_0, _CustomFunction_F6F194A9_VolumeLighting_5);
                        VolumeLighting_1 = _CustomFunction_F6F194A9_VolumeLighting_5;
                    }
                
                    void Unity_Negate_float(float In, out float Out)
                    {
                        Out = -1 * In;
                    }
                
                    void Unity_SceneColor_float(float4 UV, out float3 Out)
                    {
                        Out = SHADERGRAPH_SAMPLE_SCENE_COLOR(UV.xy);
                    }
                
                    void Unity_SceneDepth_Eye_float(float4 UV, out float Out)
                    {
                        Out = LinearEyeDepth(SHADERGRAPH_SAMPLE_SCENE_DEPTH(UV.xy), _ZBufferParams);
                    }
                
                    // d20b265501ae4875cc4a70806a8e6acb
                    #include "ShadergraphFramework/CrestNodeFoamBubbles.hlsl"
                
                    // 04f98a25a24e78d5ca68a12d00c95fbf
                    #include "ShadergraphFramework/CrestNodeVolumeEmission.hlsl"
                
                    void Unity_Add_float(float A, float B, out float Out)
                    {
                        Out = A + B;
                    }
                
                    struct Bindings_CrestSampleOceanDataSingle_a667f031fd6a3dd42beee0ccc432233d
                    {
                    };
                
                    void SG_CrestSampleOceanDataSingle_a667f031fd6a3dd42beee0ccc432233d(float2 Vector2_1BD49B8D, float3 Vector3_7E91D336, float4 Vector4_C0B2B5EA, float Vector1_8EA8B92B, Bindings_CrestSampleOceanDataSingle_a667f031fd6a3dd42beee0ccc432233d IN, out float3 Displacement_1, out float OceanWaterDepth_5, out float Foam_6, out float2 Shadow_7, out float2 Flow_8, out float SubSurfaceScattering_9)
                    {
                        float2 _Property_FA70A3E6_Out_0 = Vector2_1BD49B8D;
                        float3 _Property_C925A867_Out_0 = Vector3_7E91D336;
                        float4 _Property_467D1BE7_Out_0 = Vector4_C0B2B5EA;
                        float _Property_59E019ED_Out_0 = Vector1_8EA8B92B;
                        float3 _CustomFunction_487C31E1_Displacement_3;
                        float _CustomFunction_487C31E1_OceanDepth_8;
                        float _CustomFunction_487C31E1_Foam_4;
                        float2 _CustomFunction_487C31E1_Shadow_5;
                        float2 _CustomFunction_487C31E1_Flow_6;
                        float _CustomFunction_487C31E1_SSS_17;
                        CrestNodeSampleOceanDataSingle_float(_Property_FA70A3E6_Out_0, _Property_C925A867_Out_0, _Property_467D1BE7_Out_0, _Property_59E019ED_Out_0, _CustomFunction_487C31E1_Displacement_3, _CustomFunction_487C31E1_OceanDepth_8, _CustomFunction_487C31E1_Foam_4, _CustomFunction_487C31E1_Shadow_5, _CustomFunction_487C31E1_Flow_6, _CustomFunction_487C31E1_SSS_17);
                        Displacement_1 = _CustomFunction_487C31E1_Displacement_3;
                        OceanWaterDepth_5 = _CustomFunction_487C31E1_OceanDepth_8;
                        Foam_6 = _CustomFunction_487C31E1_Foam_4;
                        Shadow_7 = _CustomFunction_487C31E1_Shadow_5;
                        Flow_8 = _CustomFunction_487C31E1_Flow_6;
                        SubSurfaceScattering_9 = _CustomFunction_487C31E1_SSS_17;
                    }
                
                    // d3bb4f720a39af4b0dbd1cddc779836d
                    #include "ShadergraphFramework/CrestNodeOceanGlobals.hlsl"
                
                    struct Bindings_CrestOceanGlobals_d50a85284893ec447a25a093505a2120
                    {
                    };
                
                    void SG_CrestOceanGlobals_d50a85284893ec447a25a093505a2120(Bindings_CrestOceanGlobals_d50a85284893ec447a25a093505a2120 IN, out float CrestTime_1, out float TexelsPerWave_2, out float3 OceanCenterPosWorld_3, out float SliceCount_4, out float MeshScaleLerp_5)
                    {
                        float _CustomFunction_9ED6B15_CrestTime_0;
                        float _CustomFunction_9ED6B15_TexelsPerWave_1;
                        float3 _CustomFunction_9ED6B15_OceanCenterPosWorld_2;
                        float _CustomFunction_9ED6B15_SliceCount_3;
                        float _CustomFunction_9ED6B15_MeshScaleLerp_4;
                        CrestNodeOceanGlobals_float(_CustomFunction_9ED6B15_CrestTime_0, _CustomFunction_9ED6B15_TexelsPerWave_1, _CustomFunction_9ED6B15_OceanCenterPosWorld_2, _CustomFunction_9ED6B15_SliceCount_3, _CustomFunction_9ED6B15_MeshScaleLerp_4);
                        CrestTime_1 = _CustomFunction_9ED6B15_CrestTime_0;
                        TexelsPerWave_2 = _CustomFunction_9ED6B15_TexelsPerWave_1;
                        OceanCenterPosWorld_3 = _CustomFunction_9ED6B15_OceanCenterPosWorld_2;
                        SliceCount_4 = _CustomFunction_9ED6B15_SliceCount_3;
                        MeshScaleLerp_5 = _CustomFunction_9ED6B15_MeshScaleLerp_4;
                    }
                
                    void Unity_Add_float3(float3 A, float3 B, out float3 Out)
                    {
                        Out = A + B;
                    }
                
                    // 8e5fdc28d5dd56d17a96643305c4ad1c
                    #include "ShadergraphFramework/CrestNodeApplyCaustics.hlsl"
                
                    void Unity_Multiply_float(float3 A, float3 B, out float3 Out)
                    {
                        Out = A * B;
                    }
                
                    void Unity_Negate_float3(float3 In, out float3 Out)
                    {
                        Out = -1 * In;
                    }
                
                    void Unity_Exponential_float3(float3 In, out float3 Out)
                    {
                        Out = exp(In);
                    }
                
                    void Unity_OneMinus_float3(float3 In, out float3 Out)
                    {
                        Out = 1 - In;
                    }
                
                    void Unity_Lerp_float3(float3 A, float3 B, float3 T, out float3 Out)
                    {
                        Out = lerp(A, B, T);
                    }
                
                    void Unity_Branch_float3(float Predicate, float3 True, float3 False, out float3 Out)
                    {
                        Out = Predicate ? True : False;
                    }
                
                    struct Bindings_CrestEmission_8c56460232fde1e46ae90d905a00f908
                    {
                        float FaceSign;
                    };
                
                    void SG_CrestEmission_8c56460232fde1e46ae90d905a00f908(float Vector1_82EC3C0B, float3 Vector3_6C3F4D52, float3 Vector3_83F0ECB8, float3 Vector3_D6B78A76, float3 Vector3_703F2DD, float4 Vector4_5D0F731B, float Vector1_73B9F9E8, float3 Vector3_D4CABAFD, float Vector1_6C78E163, float3 Vector3_BFE779C0, half3 Vector3_71E7580E, TEXTURE2D_PARAM(Texture2D_BA141407, samplerTexture2D_BA141407), float4 Texture2D_BA141407_TexelSize, half Vector1_460D9038, half Vector1_D5CE42D3, half Vector1_AC9C2A2, half Vector1_8DBC6E14, half Vector1_EA8F2BEC, TEXTURE2D_PARAM(Texture2D_AC91C4C3, samplerTexture2D_AC91C4C3), float4 Texture2D_AC91C4C3_TexelSize, half Vector1_DB8E128A, half Vector1_CFFD6A53, float3 Vector3_32FB76B3, Bindings_CrestEmission_8c56460232fde1e46ae90d905a00f908 IN, out float3 EmittedLight_1)
                    {
                        Bindings_CrestIsUnderwater_52f7750f15e114937b54a0fd27f0d2f2 _CrestIsUnderwater_6DC72EB9;
                        _CrestIsUnderwater_6DC72EB9.FaceSign = IN.FaceSign;
                        float _CrestIsUnderwater_6DC72EB9_OutBoolean_1;
                        SG_CrestIsUnderwater_52f7750f15e114937b54a0fd27f0d2f2(_CrestIsUnderwater_6DC72EB9, _CrestIsUnderwater_6DC72EB9_OutBoolean_1);
                        float _Property_101786D0_Out_0 = Vector1_82EC3C0B;
                        float3 _Property_833375DE_Out_0 = Vector3_83F0ECB8;
                        float3 _Property_83DC9AD9_Out_0 = Vector3_703F2DD;
                        float4 _Property_83CEC55B_Out_0 = Vector4_5D0F731B;
                        float _Property_37502285_Out_0 = Vector1_73B9F9E8;
                        float3 _Property_5935620A_Out_0 = Vector3_D4CABAFD;
                        float _Property_AF5C7A5F_Out_0 = Vector1_6C78E163;
                        Bindings_CrestIsUnderwater_52f7750f15e114937b54a0fd27f0d2f2 _CrestIsUnderwater_76914462;
                        _CrestIsUnderwater_76914462.FaceSign = IN.FaceSign;
                        float _CrestIsUnderwater_76914462_OutBoolean_1;
                        SG_CrestIsUnderwater_52f7750f15e114937b54a0fd27f0d2f2(_CrestIsUnderwater_76914462, _CrestIsUnderwater_76914462_OutBoolean_1);
                        half3 _CustomFunction_F95A252D_SceneColour_5;
                        half _CustomFunction_F95A252D_SceneDistance_9;
                        half3 _CustomFunction_F95A252D_ScenePositionWS_10;
                        CrestNodeSceneColour_half(_Property_101786D0_Out_0, _Property_833375DE_Out_0, _Property_83DC9AD9_Out_0, _Property_83CEC55B_Out_0, _Property_37502285_Out_0, _Property_5935620A_Out_0, _Property_AF5C7A5F_Out_0, _CrestIsUnderwater_76914462_OutBoolean_1, _CustomFunction_F95A252D_SceneColour_5, _CustomFunction_F95A252D_SceneDistance_9, _CustomFunction_F95A252D_ScenePositionWS_10);
                        half _Split_550754E3_R_1 = _CustomFunction_F95A252D_ScenePositionWS_10[0];
                        half _Split_550754E3_G_2 = _CustomFunction_F95A252D_ScenePositionWS_10[1];
                        half _Split_550754E3_B_3 = _CustomFunction_F95A252D_ScenePositionWS_10[2];
                        half _Split_550754E3_A_4 = 0;
                        half2 _Vector2_B1CFC7F6_Out_0 = half2(_Split_550754E3_R_1, _Split_550754E3_B_3);
                        Bindings_CrestDrivenData_40572ccadf7d9704a83d283fb4c9f19d _CrestDrivenData_541C70CC;
                        half _CrestDrivenData_541C70CC_MeshScaleAlpha_1;
                        half _CrestDrivenData_541C70CC_LodDataTexelSize_8;
                        half _CrestDrivenData_541C70CC_GeometryGridSize_2;
                        half3 _CrestDrivenData_541C70CC_OceanPosScale0_3;
                        half3 _CrestDrivenData_541C70CC_OceanPosScale1_4;
                        half4 _CrestDrivenData_541C70CC_OceanParams0_5;
                        half4 _CrestDrivenData_541C70CC_OceanParams1_6;
                        half _CrestDrivenData_541C70CC_SliceIndex0_7;
                        SG_CrestDrivenData_40572ccadf7d9704a83d283fb4c9f19d(_CrestDrivenData_541C70CC, _CrestDrivenData_541C70CC_MeshScaleAlpha_1, _CrestDrivenData_541C70CC_LodDataTexelSize_8, _CrestDrivenData_541C70CC_GeometryGridSize_2, _CrestDrivenData_541C70CC_OceanPosScale0_3, _CrestDrivenData_541C70CC_OceanPosScale1_4, _CrestDrivenData_541C70CC_OceanParams0_5, _CrestDrivenData_541C70CC_OceanParams1_6, _CrestDrivenData_541C70CC_SliceIndex0_7);
                        float _Add_87784A87_Out_2;
                        Unity_Add_float(_CrestDrivenData_541C70CC_SliceIndex0_7, 1, _Add_87784A87_Out_2);
                        Bindings_CrestSampleOceanDataSingle_a667f031fd6a3dd42beee0ccc432233d _CrestSampleOceanDataSingle_5C7B7E58;
                        float3 _CrestSampleOceanDataSingle_5C7B7E58_Displacement_1;
                        float _CrestSampleOceanDataSingle_5C7B7E58_OceanWaterDepth_5;
                        float _CrestSampleOceanDataSingle_5C7B7E58_Foam_6;
                        float2 _CrestSampleOceanDataSingle_5C7B7E58_Shadow_7;
                        float2 _CrestSampleOceanDataSingle_5C7B7E58_Flow_8;
                        float _CrestSampleOceanDataSingle_5C7B7E58_SubSurfaceScattering_9;
                        SG_CrestSampleOceanDataSingle_a667f031fd6a3dd42beee0ccc432233d(_Vector2_B1CFC7F6_Out_0, _CrestDrivenData_541C70CC_OceanPosScale1_4, _CrestDrivenData_541C70CC_OceanParams1_6, _Add_87784A87_Out_2, _CrestSampleOceanDataSingle_5C7B7E58, _CrestSampleOceanDataSingle_5C7B7E58_Displacement_1, _CrestSampleOceanDataSingle_5C7B7E58_OceanWaterDepth_5, _CrestSampleOceanDataSingle_5C7B7E58_Foam_6, _CrestSampleOceanDataSingle_5C7B7E58_Shadow_7, _CrestSampleOceanDataSingle_5C7B7E58_Flow_8, _CrestSampleOceanDataSingle_5C7B7E58_SubSurfaceScattering_9);
                        Bindings_CrestOceanGlobals_d50a85284893ec447a25a093505a2120 _CrestOceanGlobals_FCFEE3C8;
                        float _CrestOceanGlobals_FCFEE3C8_CrestTime_1;
                        float _CrestOceanGlobals_FCFEE3C8_TexelsPerWave_2;
                        float3 _CrestOceanGlobals_FCFEE3C8_OceanCenterPosWorld_3;
                        float _CrestOceanGlobals_FCFEE3C8_SliceCount_4;
                        float _CrestOceanGlobals_FCFEE3C8_MeshScaleLerp_5;
                        SG_CrestOceanGlobals_d50a85284893ec447a25a093505a2120(_CrestOceanGlobals_FCFEE3C8, _CrestOceanGlobals_FCFEE3C8_CrestTime_1, _CrestOceanGlobals_FCFEE3C8_TexelsPerWave_2, _CrestOceanGlobals_FCFEE3C8_OceanCenterPosWorld_3, _CrestOceanGlobals_FCFEE3C8_SliceCount_4, _CrestOceanGlobals_FCFEE3C8_MeshScaleLerp_5);
                        float3 _Add_13827433_Out_2;
                        Unity_Add_float3(_CrestSampleOceanDataSingle_5C7B7E58_Displacement_1, _CrestOceanGlobals_FCFEE3C8_OceanCenterPosWorld_3, _Add_13827433_Out_2);
                        float _Split_8D03CDFB_R_1 = _Add_13827433_Out_2[0];
                        float _Split_8D03CDFB_G_2 = _Add_13827433_Out_2[1];
                        float _Split_8D03CDFB_B_3 = _Add_13827433_Out_2[2];
                        float _Split_8D03CDFB_A_4 = 0;
                        float3 _Property_6045F26_Out_0 = Vector3_6C3F4D52;
                        float3 _Property_5E1977C4_Out_0 = Vector3_BFE779C0;
                        half3 _Property_513534C6_Out_0 = Vector3_71E7580E;
                        half _Property_EF9152A2_Out_0 = Vector1_460D9038;
                        half _Property_3D2898B2_Out_0 = Vector1_D5CE42D3;
                        half _Property_35078BD5_Out_0 = Vector1_AC9C2A2;
                        half _Property_1B866598_Out_0 = Vector1_8DBC6E14;
                        half _Property_2016136C_Out_0 = Vector1_EA8F2BEC;
                        half _Property_A64ADC3_Out_0 = Vector1_DB8E128A;
                        half _Property_DDF09C09_Out_0 = Vector1_CFFD6A53;
                        Bindings_CrestIsUnderwater_52f7750f15e114937b54a0fd27f0d2f2 _CrestIsUnderwater_2020F895;
                        _CrestIsUnderwater_2020F895.FaceSign = IN.FaceSign;
                        float _CrestIsUnderwater_2020F895_OutBoolean_1;
                        SG_CrestIsUnderwater_52f7750f15e114937b54a0fd27f0d2f2(_CrestIsUnderwater_2020F895, _CrestIsUnderwater_2020F895_OutBoolean_1);
                        float3 _CustomFunction_2F6A103B_SceneColourOut_8;
                        CrestNodeApplyCaustics_float(_CustomFunction_F95A252D_SceneColour_5, _CustomFunction_F95A252D_ScenePositionWS_10, _Split_8D03CDFB_G_2, _Property_6045F26_Out_0, _Property_5E1977C4_Out_0, _Property_513534C6_Out_0, _CustomFunction_F95A252D_SceneDistance_9, Texture2D_BA141407, _Property_EF9152A2_Out_0, _Property_3D2898B2_Out_0, _Property_35078BD5_Out_0, _Property_1B866598_Out_0, _Property_2016136C_Out_0, Texture2D_AC91C4C3, _Property_A64ADC3_Out_0, _Property_DDF09C09_Out_0, _CrestIsUnderwater_2020F895_OutBoolean_1, _CustomFunction_2F6A103B_SceneColourOut_8);
                        #if defined(CREST_CAUSTICS_ON)
                        float3 _CAUSTICS_6D445714_Out_0 = _CustomFunction_2F6A103B_SceneColourOut_8;
                        #else
                        float3 _CAUSTICS_6D445714_Out_0 = _CustomFunction_F95A252D_SceneColour_5;
                        #endif
                        float3 _Property_78DFA568_Out_0 = Vector3_83F0ECB8;
                        float3 _Property_54BACED9_Out_0 = Vector3_32FB76B3;
                        float3 _Add_954C4741_Out_2;
                        Unity_Add_float3(_Property_78DFA568_Out_0, _Property_54BACED9_Out_0, _Add_954C4741_Out_2);
                        float3 _Property_D76B9A0B_Out_0 = Vector3_6C3F4D52;
                        float3 _Multiply_C51E63DE_Out_2;
                        Unity_Multiply_float((_CustomFunction_F95A252D_SceneDistance_9.xxx), _Property_D76B9A0B_Out_0, _Multiply_C51E63DE_Out_2);
                        float3 _Negate_ADFF8761_Out_1;
                        Unity_Negate_float3(_Multiply_C51E63DE_Out_2, _Negate_ADFF8761_Out_1);
                        float3 _Exponential_6FCB9AC3_Out_1;
                        Unity_Exponential_float3(_Negate_ADFF8761_Out_1, _Exponential_6FCB9AC3_Out_1);
                        float3 _OneMinus_9962618_Out_1;
                        Unity_OneMinus_float3(_Exponential_6FCB9AC3_Out_1, _OneMinus_9962618_Out_1);
                        float3 _Lerp_E9270AE8_Out_3;
                        Unity_Lerp_float3(_CAUSTICS_6D445714_Out_0, _Add_954C4741_Out_2, _OneMinus_9962618_Out_1, _Lerp_E9270AE8_Out_3);
                        float3 _Branch_D043DFC1_Out_3;
                        Unity_Branch_float3(_CrestIsUnderwater_6DC72EB9_OutBoolean_1, _CAUSTICS_6D445714_Out_0, _Lerp_E9270AE8_Out_3, _Branch_D043DFC1_Out_3);
                        EmittedLight_1 = _Branch_D043DFC1_Out_3;
                    }
                
                    void Unity_Reflection_float3(float3 In, float3 Normal, out float3 Out)
                    {
                        Out = reflect(In, Normal);
                    }
                
                    void Unity_Divide_float(float A, float B, out float Out)
                    {
                        Out = A / B;
                    }
                
                    void Unity_Saturate_float(float In, out float Out)
                    {
                        Out = saturate(In);
                    }
                
                    void Unity_Power_float(float A, float B, out float Out)
                    {
                        Out = pow(A, B);
                    }
                
                    void Unity_Lerp_float(float A, float B, float T, out float Out)
                    {
                        Out = lerp(A, B, T);
                    }
                
                    void Unity_Modulo_float(float A, float B, out float Out)
                    {
                        Out = fmod(A, B);
                    }
                
                    void Unity_Multiply_float(float2 A, float2 B, out float2 Out)
                    {
                        Out = A * B;
                    }
                
                    void Unity_Comparison_Greater_float(float A, float B, out float Out)
                    {
                        Out = A > B ? 1 : 0;
                    }
                
                    void Unity_Subtract_float(float A, float B, out float Out)
                    {
                        Out = A - B;
                    }
                
                    void Unity_Branch_float(float Predicate, float True, float False, out float Out)
                    {
                        Out = Predicate ? True : False;
                    }
                
                    struct Bindings_CrestFlow_f9c7f2c7774dd4b5bad87d6a350b47f1
                    {
                    };
                
                    void SG_CrestFlow_f9c7f2c7774dd4b5bad87d6a350b47f1(float2 Vector2_B562EFB1, float2 Vector2_83AA31A8, Bindings_CrestFlow_f9c7f2c7774dd4b5bad87d6a350b47f1 IN, out float2 DisplacedA_1, out float WeightA_3, out float2 DisplacedB_2, out float WeightB_4)
                    {
                        float2 _Property_DB670C03_Out_0 = Vector2_83AA31A8;
                        float2 _Property_6ED023E0_Out_0 = Vector2_B562EFB1;
                        Bindings_CrestOceanGlobals_d50a85284893ec447a25a093505a2120 _CrestOceanGlobals_40DCBE3E;
                        float _CrestOceanGlobals_40DCBE3E_CrestTime_1;
                        float _CrestOceanGlobals_40DCBE3E_TexelsPerWave_2;
                        float3 _CrestOceanGlobals_40DCBE3E_OceanCenterPosWorld_3;
                        float _CrestOceanGlobals_40DCBE3E_SliceCount_4;
                        float _CrestOceanGlobals_40DCBE3E_MeshScaleLerp_5;
                        SG_CrestOceanGlobals_d50a85284893ec447a25a093505a2120(_CrestOceanGlobals_40DCBE3E, _CrestOceanGlobals_40DCBE3E_CrestTime_1, _CrestOceanGlobals_40DCBE3E_TexelsPerWave_2, _CrestOceanGlobals_40DCBE3E_OceanCenterPosWorld_3, _CrestOceanGlobals_40DCBE3E_SliceCount_4, _CrestOceanGlobals_40DCBE3E_MeshScaleLerp_5);
                        float _Modulo_CC9BC285_Out_2;
                        Unity_Modulo_float(_CrestOceanGlobals_40DCBE3E_CrestTime_1, 2, _Modulo_CC9BC285_Out_2);
                        float2 _Multiply_821FCA56_Out_2;
                        Unity_Multiply_float(_Property_6ED023E0_Out_0, (_Modulo_CC9BC285_Out_2.xx), _Multiply_821FCA56_Out_2);
                        float2 _Subtract_273125FF_Out_2;
                        Unity_Subtract_float2(_Property_DB670C03_Out_0, _Multiply_821FCA56_Out_2, _Subtract_273125FF_Out_2);
                        float2 _Property_C2A19C1C_Out_0 = Vector2_83AA31A8;
                        #if defined(CREST_FLOW_ON)
                        float2 _FLOW_963D27CC_Out_0 = _Subtract_273125FF_Out_2;
                        #else
                        float2 _FLOW_963D27CC_Out_0 = _Property_C2A19C1C_Out_0;
                        #endif
                        float _Comparison_67E5EC91_Out_2;
                        Unity_Comparison_Greater_float(_Modulo_CC9BC285_Out_2, 1, _Comparison_67E5EC91_Out_2);
                        float _Subtract_289E2A26_Out_2;
                        Unity_Subtract_float(2, _Modulo_CC9BC285_Out_2, _Subtract_289E2A26_Out_2);
                        float _Branch_57B6378F_Out_3;
                        Unity_Branch_float(_Comparison_67E5EC91_Out_2, _Subtract_289E2A26_Out_2, _Modulo_CC9BC285_Out_2, _Branch_57B6378F_Out_3);
                        #if defined(CREST_FLOW_ON)
                        float _FLOW_AB68F2D0_Out_0 = _Branch_57B6378F_Out_3;
                        #else
                        float _FLOW_AB68F2D0_Out_0 = 0;
                        #endif
                        float2 _Property_CF607B59_Out_0 = Vector2_83AA31A8;
                        float2 _Property_8EFEBF1A_Out_0 = Vector2_B562EFB1;
                        float _Add_43F824C8_Out_2;
                        Unity_Add_float(_CrestOceanGlobals_40DCBE3E_CrestTime_1, 1, _Add_43F824C8_Out_2);
                        float _Modulo_2FF365BF_Out_2;
                        Unity_Modulo_float(_Add_43F824C8_Out_2, 2, _Modulo_2FF365BF_Out_2);
                        float2 _Multiply_28DD98EB_Out_2;
                        Unity_Multiply_float(_Property_8EFEBF1A_Out_0, (_Modulo_2FF365BF_Out_2.xx), _Multiply_28DD98EB_Out_2);
                        float2 _Subtract_1C7014D5_Out_2;
                        Unity_Subtract_float2(_Property_CF607B59_Out_0, _Multiply_28DD98EB_Out_2, _Subtract_1C7014D5_Out_2);
                        #if defined(CREST_FLOW_ON)
                        float2 _FLOW_D38C09E5_Out_0 = _Subtract_1C7014D5_Out_2;
                        #else
                        float2 _FLOW_D38C09E5_Out_0 = _Property_C2A19C1C_Out_0;
                        #endif
                        float _Subtract_563D329D_Out_2;
                        Unity_Subtract_float(1, _Branch_57B6378F_Out_3, _Subtract_563D329D_Out_2);
                        #if defined(CREST_FLOW_ON)
                        float _FLOW_CB3A12C4_Out_0 = _Subtract_563D329D_Out_2;
                        #else
                        float _FLOW_CB3A12C4_Out_0 = 0;
                        #endif
                        DisplacedA_1 = _FLOW_963D27CC_Out_0;
                        WeightA_3 = _FLOW_AB68F2D0_Out_0;
                        DisplacedB_2 = _FLOW_D38C09E5_Out_0;
                        WeightB_4 = _FLOW_CB3A12C4_Out_0;
                    }
                
                    // dfcd16226d95b1a66d66c0c937cc47e9
                    #include "ShadergraphFramework/CrestNodeFoam.hlsl"
                
                    void Unity_Multiply_float(float A, float B, out float Out)
                    {
                        Out = A * B;
                    }
                
                    struct Bindings_CrestFoamWithFlow_3a6c22dceac0847a2a31cb05a577b963
                    {
                    };
                
                    void SG_CrestFoamWithFlow_3a6c22dceac0847a2a31cb05a577b963(TEXTURE2D_PARAM(Texture2D_D9E7A343, samplerTexture2D_D9E7A343), float4 Texture2D_D9E7A343_TexelSize, float2 Vector2_C4E9ED58, float Vector1_7492C815, float Vector1_33DC93D, float Vector1_3439FC0A, float Vector1_FB133018, float Vector1_7CFAFAC1, float Vector1_53F45327, float4 Vector4_2FA9AEA5, float4 Vector4_FD283A50, float Vector1_D6DA4100, float Vector1_13F2A8C8, float2 Vector2_28A2C5B9, float3 Vector3_3AD012AA, float3 Vector3_E4AC4432, float Vector1_3E452608, float Vector1_D80F067A, float2 Vector2_5AD72ED, Bindings_CrestFoamWithFlow_3a6c22dceac0847a2a31cb05a577b963 IN, out float3 Albedo_1, out float3 NormalTS_2, out float3 Emission_3, out float Smoothness_4)
                    {
                        float2 _Property_B71BFD9B_Out_0 = Vector2_C4E9ED58;
                        float _Property_7C437CB0_Out_0 = Vector1_7492C815;
                        float _Property_BD5882DA_Out_0 = Vector1_33DC93D;
                        float _Property_3EF95B2A_Out_0 = Vector1_3439FC0A;
                        float _Property_38B853C9_Out_0 = Vector1_FB133018;
                        float _Property_8F25A4DF_Out_0 = Vector1_7CFAFAC1;
                        float _Property_561CFE5E_Out_0 = Vector1_53F45327;
                        float4 _Property_A1E249A2_Out_0 = Vector4_2FA9AEA5;
                        float4 _Property_9A7BA0BB_Out_0 = Vector4_FD283A50;
                        float _Property_4C4E1C83_Out_0 = Vector1_D6DA4100;
                        float _Property_D81DA51E_Out_0 = Vector1_13F2A8C8;
                        float2 _Property_4BF1D6EA_Out_0 = Vector2_5AD72ED;
                        float2 _Property_DEC66EB9_Out_0 = Vector2_28A2C5B9;
                        Bindings_CrestFlow_f9c7f2c7774dd4b5bad87d6a350b47f1 _CrestFlow_B4596B;
                        float2 _CrestFlow_B4596B_DisplacedA_1;
                        float _CrestFlow_B4596B_WeightA_3;
                        float2 _CrestFlow_B4596B_DisplacedB_2;
                        float _CrestFlow_B4596B_WeightB_4;
                        SG_CrestFlow_f9c7f2c7774dd4b5bad87d6a350b47f1(_Property_4BF1D6EA_Out_0, _Property_DEC66EB9_Out_0, _CrestFlow_B4596B, _CrestFlow_B4596B_DisplacedA_1, _CrestFlow_B4596B_WeightA_3, _CrestFlow_B4596B_DisplacedB_2, _CrestFlow_B4596B_WeightB_4);
                        float _Property_99DCA7A4_Out_0 = Vector1_D80F067A;
                        float3 _Property_EB1DCAFB_Out_0 = Vector3_3AD012AA;
                        float3 _Property_A2BD94BA_Out_0 = Vector3_E4AC4432;
                        float _Property_7BFA2B51_Out_0 = Vector1_3E452608;
                        half3 _CustomFunction_2B0D0960_Albedo_8;
                        half3 _CustomFunction_2B0D0960_NormalTS_7;
                        half3 _CustomFunction_2B0D0960_Emission_9;
                        half _CustomFunction_2B0D0960_Smoothness_10;
                        CrestNodeFoam_half(Texture2D_D9E7A343, _Property_B71BFD9B_Out_0, _Property_7C437CB0_Out_0, _Property_BD5882DA_Out_0, _Property_3EF95B2A_Out_0, _Property_38B853C9_Out_0, _Property_8F25A4DF_Out_0, _Property_561CFE5E_Out_0, _Property_A1E249A2_Out_0, _Property_9A7BA0BB_Out_0, _Property_4C4E1C83_Out_0, _Property_D81DA51E_Out_0, _CrestFlow_B4596B_DisplacedA_1, _Property_99DCA7A4_Out_0, _Property_EB1DCAFB_Out_0, _Property_A2BD94BA_Out_0, _Property_7BFA2B51_Out_0, _CustomFunction_2B0D0960_Albedo_8, _CustomFunction_2B0D0960_NormalTS_7, _CustomFunction_2B0D0960_Emission_9, _CustomFunction_2B0D0960_Smoothness_10);
                        #if defined(CREST_FOAM_ON)
                        float3 _FOAM_384819B4_Out_0 = _CustomFunction_2B0D0960_Albedo_8;
                        #else
                        float3 _FOAM_384819B4_Out_0 = float3(0, 0, 0);
                        #endif
                        float3 _Multiply_278EF57C_Out_2;
                        Unity_Multiply_float(_FOAM_384819B4_Out_0, (_CrestFlow_B4596B_WeightA_3.xxx), _Multiply_278EF57C_Out_2);
                        float2 _Property_6338038D_Out_0 = Vector2_C4E9ED58;
                        float _Property_C8444C80_Out_0 = Vector1_7492C815;
                        float _Property_3DF87814_Out_0 = Vector1_33DC93D;
                        float _Property_589C4301_Out_0 = Vector1_3439FC0A;
                        float _Property_E9D88423_Out_0 = Vector1_FB133018;
                        float _Property_366CB823_Out_0 = Vector1_7CFAFAC1;
                        float _Property_1F5469F5_Out_0 = Vector1_53F45327;
                        float4 _Property_920AF23F_Out_0 = Vector4_2FA9AEA5;
                        float4 _Property_37F6AC84_Out_0 = Vector4_FD283A50;
                        float _Property_E7E25E74_Out_0 = Vector1_D6DA4100;
                        float _Property_FA2913B0_Out_0 = Vector1_13F2A8C8;
                        float _Property_BFBF141C_Out_0 = Vector1_D80F067A;
                        float3 _Property_4E6AF5FF_Out_0 = Vector3_3AD012AA;
                        float3 _Property_A2DA52BE_Out_0 = Vector3_E4AC4432;
                        float _Property_9DE15AC9_Out_0 = Vector1_3E452608;
                        half3 _CustomFunction_16EDEEAC_Albedo_8;
                        half3 _CustomFunction_16EDEEAC_NormalTS_7;
                        half3 _CustomFunction_16EDEEAC_Emission_9;
                        half _CustomFunction_16EDEEAC_Smoothness_10;
                        CrestNodeFoam_half(Texture2D_D9E7A343, _Property_6338038D_Out_0, _Property_C8444C80_Out_0, _Property_3DF87814_Out_0, _Property_589C4301_Out_0, _Property_E9D88423_Out_0, _Property_366CB823_Out_0, _Property_1F5469F5_Out_0, _Property_920AF23F_Out_0, _Property_37F6AC84_Out_0, _Property_E7E25E74_Out_0, _Property_FA2913B0_Out_0, _CrestFlow_B4596B_DisplacedB_2, _Property_BFBF141C_Out_0, _Property_4E6AF5FF_Out_0, _Property_A2DA52BE_Out_0, _Property_9DE15AC9_Out_0, _CustomFunction_16EDEEAC_Albedo_8, _CustomFunction_16EDEEAC_NormalTS_7, _CustomFunction_16EDEEAC_Emission_9, _CustomFunction_16EDEEAC_Smoothness_10);
                        #if defined(CREST_FOAM_ON)
                        float3 _FOAM_E9D68DEC_Out_0 = _CustomFunction_16EDEEAC_Albedo_8;
                        #else
                        float3 _FOAM_E9D68DEC_Out_0 = float3(0, 0, 0);
                        #endif
                        float3 _Multiply_BCF80692_Out_2;
                        Unity_Multiply_float((_CrestFlow_B4596B_WeightB_4.xxx), _FOAM_E9D68DEC_Out_0, _Multiply_BCF80692_Out_2);
                        float3 _Add_48FC5557_Out_2;
                        Unity_Add_float3(_Multiply_278EF57C_Out_2, _Multiply_BCF80692_Out_2, _Add_48FC5557_Out_2);
                        #if defined(CREST_FLOW_ON)
                        float3 _FLOW_1260C700_Out_0 = _Add_48FC5557_Out_2;
                        #else
                        float3 _FLOW_1260C700_Out_0 = _FOAM_384819B4_Out_0;
                        #endif
                        #if defined(CREST_FOAM_ON)
                        float3 _FOAM_14E9F7C1_Out_0 = _CustomFunction_2B0D0960_NormalTS_7;
                        #else
                        float3 _FOAM_14E9F7C1_Out_0 = _Property_EB1DCAFB_Out_0;
                        #endif
                        float3 _Multiply_FAB0278F_Out_2;
                        Unity_Multiply_float(_FOAM_14E9F7C1_Out_0, (_CrestFlow_B4596B_WeightA_3.xxx), _Multiply_FAB0278F_Out_2);
                        #if defined(CREST_FOAM_ON)
                        float3 _FOAM_9C6E353_Out_0 = _CustomFunction_16EDEEAC_NormalTS_7;
                        #else
                        float3 _FOAM_9C6E353_Out_0 = _Property_4E6AF5FF_Out_0;
                        #endif
                        float3 _Multiply_CE1E9F74_Out_2;
                        Unity_Multiply_float((_CrestFlow_B4596B_WeightB_4.xxx), _FOAM_9C6E353_Out_0, _Multiply_CE1E9F74_Out_2);
                        float3 _Add_3A03EC0D_Out_2;
                        Unity_Add_float3(_Multiply_FAB0278F_Out_2, _Multiply_CE1E9F74_Out_2, _Add_3A03EC0D_Out_2);
                        #if defined(CREST_FLOW_ON)
                        float3 _FLOW_2D8BA180_Out_0 = _Add_3A03EC0D_Out_2;
                        #else
                        float3 _FLOW_2D8BA180_Out_0 = _FOAM_14E9F7C1_Out_0;
                        #endif
                        #if defined(CREST_FOAM_ON)
                        float3 _FOAM_D8E821F0_Out_0 = _CustomFunction_2B0D0960_Emission_9;
                        #else
                        float3 _FOAM_D8E821F0_Out_0 = _Property_A2BD94BA_Out_0;
                        #endif
                        float3 _Multiply_716921DC_Out_2;
                        Unity_Multiply_float(_FOAM_D8E821F0_Out_0, (_CrestFlow_B4596B_WeightA_3.xxx), _Multiply_716921DC_Out_2);
                        #if defined(CREST_FOAM_ON)
                        float3 _FOAM_9468CE46_Out_0 = _CustomFunction_16EDEEAC_Emission_9;
                        #else
                        float3 _FOAM_9468CE46_Out_0 = _Property_A2DA52BE_Out_0;
                        #endif
                        float3 _Multiply_A0DE696C_Out_2;
                        Unity_Multiply_float((_CrestFlow_B4596B_WeightB_4.xxx), _FOAM_9468CE46_Out_0, _Multiply_A0DE696C_Out_2);
                        float3 _Add_2EBEBFCF_Out_2;
                        Unity_Add_float3(_Multiply_716921DC_Out_2, _Multiply_A0DE696C_Out_2, _Add_2EBEBFCF_Out_2);
                        #if defined(CREST_FLOW_ON)
                        float3 _FLOW_225158E3_Out_0 = _Add_2EBEBFCF_Out_2;
                        #else
                        float3 _FLOW_225158E3_Out_0 = _FOAM_D8E821F0_Out_0;
                        #endif
                        #if defined(CREST_FOAM_ON)
                        float _FOAM_F0F633C_Out_0 = _CustomFunction_2B0D0960_Smoothness_10;
                        #else
                        float _FOAM_F0F633C_Out_0 = _Property_7BFA2B51_Out_0;
                        #endif
                        float _Multiply_C03E7AFC_Out_2;
                        Unity_Multiply_float(_FOAM_F0F633C_Out_0, _CrestFlow_B4596B_WeightA_3, _Multiply_C03E7AFC_Out_2);
                        #if defined(CREST_FOAM_ON)
                        float _FOAM_AB9E199D_Out_0 = _CustomFunction_16EDEEAC_Smoothness_10;
                        #else
                        float _FOAM_AB9E199D_Out_0 = _Property_9DE15AC9_Out_0;
                        #endif
                        float _Multiply_8F8BE539_Out_2;
                        Unity_Multiply_float(_CrestFlow_B4596B_WeightB_4, _FOAM_AB9E199D_Out_0, _Multiply_8F8BE539_Out_2);
                        float _Add_7629FC1B_Out_2;
                        Unity_Add_float(_Multiply_C03E7AFC_Out_2, _Multiply_8F8BE539_Out_2, _Add_7629FC1B_Out_2);
                        #if defined(CREST_FLOW_ON)
                        float _FLOW_386EA56_Out_0 = _Add_7629FC1B_Out_2;
                        #else
                        float _FLOW_386EA56_Out_0 = _FOAM_F0F633C_Out_0;
                        #endif
                        Albedo_1 = _FLOW_1260C700_Out_0;
                        NormalTS_2 = _FLOW_2D8BA180_Out_0;
                        Emission_3 = _FLOW_225158E3_Out_0;
                        Smoothness_4 = _FLOW_386EA56_Out_0;
                    }
                
                    struct Bindings_CrestOceanPixel_6f6706d805d8e8649adddaaa94260269
                    {
                        float3 WorldSpaceViewDirection;
                        float3 ViewSpacePosition;
                        float3 AbsoluteWorldSpacePosition;
                        float4 ScreenPosition;
                        float FaceSign;
                    };
                
                    void SG_CrestOceanPixel_6f6706d805d8e8649adddaaa94260269(TEXTURE2D_PARAM(Texture2D_BE500045, samplerTexture2D_BE500045), float4 Texture2D_BE500045_TexelSize, float2 Vector2_2F51BFFE, float Vector1_1CEB35D8, float Vector1_43921196, float Vector1_E301593B, float Vector1_958E8942, float Vector1_2ED4C943, float Vector1_BF3AF964, float3 Vector3_EEEBAAB5, float Vector1_D3410E3, float Vector1_1EC1FE35, float2 Vector2_9C73A0C6, float Vector1_B01E1A6A, float Vector1_D74C6609, float Vector1_B61034CA, float2 Vector2_AE8873FA, float2 Vector2_69CC43DC, float Vector1_255AB964, float Vector1_47308CC2, float Vector1_26549044, float Vector1_177E111C, float Vector1_33255829, TEXTURE2D_PARAM(Texture2D_40AB1455, samplerTexture2D_40AB1455), float4 Texture2D_40AB1455_TexelSize, float Vector1_23A72EC7, float Vector1_F406EE17, float4 Vector4_2F6E352, float3 Vector3_57B74D6A, float4 Vector4_B3AD63B4, float Vector1_FA688590, float Vector1_9835666E, float Vector1_B331E24E, float Vector1_951CF2DF, float4 Vector4_ADCA8891, float Vector1_2E8E2C59, float Vector1_9BD9C342, float3 Vector3_8228B74C, float3 Vector3_B2D6AD84, float3 Vector3_D2C93D25, TEXTURE2D_PARAM(Texture2D_EB8C8549, samplerTexture2D_EB8C8549), float4 Texture2D_EB8C8549_TexelSize, float Vector1_9AE39B77, half Vector1_1B073674, float Vector1_C885385, float Vector1_90CEE6B8, float Vector1_E27586E2, TEXTURE2D_PARAM(Texture2D_DA8A756A, samplerTexture2D_DA8A756A), float4 Texture2D_DA8A756A_TexelSize, float Vector1_C96A6500, float Vector1_1AD36684, float Vector1_9E87174F, float Vector1_80D0DF9E, float Vector1_96F28EC5, Bindings_CrestOceanPixel_6f6706d805d8e8649adddaaa94260269 IN, out float3 Albedo_2, out float3 NormalTS_3, out float3 Emission_4, out float Smoothness_5, out float Specular_6)
                    {
                        float2 _Property_B9BA8901_Out_0 = Vector2_2F51BFFE;
                        float _Property_6AEC3552_Out_0 = Vector1_1CEB35D8;
                        float _Property_76EA797B_Out_0 = Vector1_43921196;
                        float _Property_83C2E998_Out_0 = Vector1_E301593B;
                        float _Property_705FD8D9_Out_0 = Vector1_958E8942;
                        float _Property_735B5A53_Out_0 = Vector1_2ED4C943;
                        float _Property_EB4C03CF_Out_0 = Vector1_BF3AF964;
                        Bindings_CrestDrivenData_40572ccadf7d9704a83d283fb4c9f19d _CrestDrivenData_6635D6E9;
                        half _CrestDrivenData_6635D6E9_MeshScaleAlpha_1;
                        half _CrestDrivenData_6635D6E9_LodDataTexelSize_8;
                        half _CrestDrivenData_6635D6E9_GeometryGridSize_2;
                        half3 _CrestDrivenData_6635D6E9_OceanPosScale0_3;
                        half3 _CrestDrivenData_6635D6E9_OceanPosScale1_4;
                        half4 _CrestDrivenData_6635D6E9_OceanParams0_5;
                        half4 _CrestDrivenData_6635D6E9_OceanParams1_6;
                        half _CrestDrivenData_6635D6E9_SliceIndex0_7;
                        SG_CrestDrivenData_40572ccadf7d9704a83d283fb4c9f19d(_CrestDrivenData_6635D6E9, _CrestDrivenData_6635D6E9_MeshScaleAlpha_1, _CrestDrivenData_6635D6E9_LodDataTexelSize_8, _CrestDrivenData_6635D6E9_GeometryGridSize_2, _CrestDrivenData_6635D6E9_OceanPosScale0_3, _CrestDrivenData_6635D6E9_OceanPosScale1_4, _CrestDrivenData_6635D6E9_OceanParams0_5, _CrestDrivenData_6635D6E9_OceanParams1_6, _CrestDrivenData_6635D6E9_SliceIndex0_7);
                        float _Property_EE920C2C_Out_0 = Vector1_B61034CA;
                        float _Property_8BC6F5AD_Out_0 = Vector1_B01E1A6A;
                        float2 _Property_A2CEB215_Out_0 = Vector2_9C73A0C6;
                        float _Property_A636CE7E_Out_0 = Vector1_23A72EC7;
                        float _Property_C1531E4C_Out_0 = Vector1_F406EE17;
                        float _Property_C7723E93_Out_0 = Vector1_B01E1A6A;
                        float2 _Property_94C56295_Out_0 = Vector2_9C73A0C6;
                        float2 _Property_E65AA85_Out_0 = Vector2_69CC43DC;
                        float3 _Normalize_3574419A_Out_1;
                        Unity_Normalize_float3(IN.WorldSpaceViewDirection, _Normalize_3574419A_Out_1);
                        float _Property_5A245779_Out_0 = Vector1_96F28EC5;
                        Bindings_CrestComputeNormal_61b9efc6612ab3b4f84174344af5e12a _CrestComputeNormal_6DAE4B39;
                        _CrestComputeNormal_6DAE4B39.FaceSign = IN.FaceSign;
                        half3 _CrestComputeNormal_6DAE4B39_Normal_1;
                        SG_CrestComputeNormal_61b9efc6612ab3b4f84174344af5e12a(_CrestDrivenData_6635D6E9_OceanPosScale0_3, _CrestDrivenData_6635D6E9_OceanPosScale1_4, _CrestDrivenData_6635D6E9_OceanParams0_5, _CrestDrivenData_6635D6E9_OceanParams1_6, _CrestDrivenData_6635D6E9_SliceIndex0_7, TEXTURE2D_ARGS(Texture2D_40AB1455, samplerTexture2D_40AB1455), Texture2D_40AB1455_TexelSize, _Property_A636CE7E_Out_0, _Property_C1531E4C_Out_0, _Property_C7723E93_Out_0, _Property_94C56295_Out_0, _Property_E65AA85_Out_0, _Normalize_3574419A_Out_1, _Property_5A245779_Out_0, _CrestComputeNormal_6DAE4B39, _CrestComputeNormal_6DAE4B39_Normal_1);
                        Bindings_CrestIsUnderwater_52f7750f15e114937b54a0fd27f0d2f2 _CrestIsUnderwater_5AB8F4C9;
                        _CrestIsUnderwater_5AB8F4C9.FaceSign = IN.FaceSign;
                        float _CrestIsUnderwater_5AB8F4C9_OutBoolean_1;
                        SG_CrestIsUnderwater_52f7750f15e114937b54a0fd27f0d2f2(_CrestIsUnderwater_5AB8F4C9, _CrestIsUnderwater_5AB8F4C9_OutBoolean_1);
                        float _Property_FCAEBD15_Out_0 = Vector1_9E87174F;
                        float _Property_2D376CE_Out_0 = Vector1_80D0DF9E;
                        Bindings_CrestFresnel_c6f6c13c4fdb04e42b427e4c2610d3c9 _CrestFresnel_FD3200EB;
                        _CrestFresnel_FD3200EB.FaceSign = IN.FaceSign;
                        float _CrestFresnel_FD3200EB_LightTransmitted_1;
                        float _CrestFresnel_FD3200EB_LightReflected_2;
                        SG_CrestFresnel_c6f6c13c4fdb04e42b427e4c2610d3c9(_CrestComputeNormal_6DAE4B39_Normal_1, _Normalize_3574419A_Out_1, _Property_FCAEBD15_Out_0, _Property_2D376CE_Out_0, _CrestFresnel_FD3200EB, _CrestFresnel_FD3200EB_LightTransmitted_1, _CrestFresnel_FD3200EB_LightReflected_2);
                        float _Property_2599690E_Out_0 = Vector1_9BD9C342;
                        float3 _Property_A57CAD6E_Out_0 = Vector3_8228B74C;
                        float4 _Property_479D2E13_Out_0 = Vector4_2F6E352;
                        float3 _Property_F631E4E0_Out_0 = Vector3_57B74D6A;
                        float4 _Property_D9155CD7_Out_0 = Vector4_B3AD63B4;
                        float _Property_D8B923AB_Out_0 = Vector1_FA688590;
                        float _Property_F7F4791A_Out_0 = Vector1_9835666E;
                        float _Property_2ABF6C61_Out_0 = Vector1_B331E24E;
                        float _Property_22A5FA38_Out_0 = Vector1_951CF2DF;
                        float4 _Property_C1E8071C_Out_0 = Vector4_ADCA8891;
                        float _Property_9AD6466F_Out_0 = Vector1_2E8E2C59;
                        float _Property_4D8B881_Out_0 = Vector1_D74C6609;
                        float2 _Property_5A879B44_Out_0 = Vector2_AE8873FA;
                        float _Property_41C453D9_Out_0 = Vector1_255AB964;
                        Bindings_CrestAmbientLight_a6ec89b3ca0ab4e98b300ec3ba0e6013 _CrestAmbientLight_5A61E85E;
                        half3 _CrestAmbientLight_5A61E85E_Color_1;
                        SG_CrestAmbientLight_a6ec89b3ca0ab4e98b300ec3ba0e6013(_CrestAmbientLight_5A61E85E, _CrestAmbientLight_5A61E85E_Color_1);
                        float3 _Property_13E6764D_Out_0 = Vector3_B2D6AD84;
                        float3 _Property_2FA7DB0_Out_0 = Vector3_D2C93D25;
                        Bindings_CrestVolumeLighting_e9ed6e11710a50640bb4b811d0fa84f2 _CrestVolumeLighting_5A1A391;
                        half3 _CrestVolumeLighting_5A1A391_VolumeLighting_1;
                        SG_CrestVolumeLighting_e9ed6e11710a50640bb4b811d0fa84f2(_Property_479D2E13_Out_0, _Property_F631E4E0_Out_0, _Property_D9155CD7_Out_0, _Property_D8B923AB_Out_0, _Property_F7F4791A_Out_0, _Property_2ABF6C61_Out_0, _Property_22A5FA38_Out_0, _Property_C1E8071C_Out_0, _Property_9AD6466F_Out_0, _Property_4D8B881_Out_0, _Property_5A879B44_Out_0, _Property_41C453D9_Out_0, _Normalize_3574419A_Out_1, IN.AbsoluteWorldSpacePosition, _CrestAmbientLight_5A61E85E_Color_1, _Property_13E6764D_Out_0, _Property_2FA7DB0_Out_0, _CrestVolumeLighting_5A1A391, _CrestVolumeLighting_5A1A391_VolumeLighting_1);
                        float4 _ScreenPosition_42FE0E3E_Out_0 = float4(IN.ScreenPosition.xy / IN.ScreenPosition.w, 0, 0);
                        float _Split_66230827_R_1 = IN.ViewSpacePosition[0];
                        float _Split_66230827_G_2 = IN.ViewSpacePosition[1];
                        float _Split_66230827_B_3 = IN.ViewSpacePosition[2];
                        float _Split_66230827_A_4 = 0;
                        float _Negate_5F3C7D3D_Out_1;
                        Unity_Negate_float(_Split_66230827_B_3, _Negate_5F3C7D3D_Out_1);
                        float3 _SceneColor_61626E7C_Out_1;
                        Unity_SceneColor_float(float4(IN.ScreenPosition.xy / IN.ScreenPosition.w, 0, 0), _SceneColor_61626E7C_Out_1);
                        float _SceneDepth_FD35F7D9_Out_1;
                        Unity_SceneDepth_Eye_float(float4(IN.ScreenPosition.xy / IN.ScreenPosition.w, 0, 0), _SceneDepth_FD35F7D9_Out_1);
                        float3 _Property_4DD76B1_Out_0 = Vector3_D2C93D25;
                        float3 _Property_DC1C088D_Out_0 = Vector3_B2D6AD84;
                        float _Property_8BC39E79_Out_0 = Vector1_9AE39B77;
                        half _Property_683CAF2_Out_0 = Vector1_1B073674;
                        float _Property_B4B35559_Out_0 = Vector1_C885385;
                        float _Property_E694F888_Out_0 = Vector1_90CEE6B8;
                        float _Property_50A87698_Out_0 = Vector1_E27586E2;
                        float _Property_8BC9D755_Out_0 = Vector1_C96A6500;
                        float _Property_6F95DFBF_Out_0 = Vector1_1AD36684;
                        float3 _Property_4729CD24_Out_0 = Vector3_EEEBAAB5;
                        float _Property_C36E9B6_Out_0 = Vector1_D3410E3;
                        float _Property_2C173F60_Out_0 = Vector1_1EC1FE35;
                        float2 _Property_4258AA49_Out_0 = Vector2_2F51BFFE;
                        float _Property_3FC34BC9_Out_0 = Vector1_1CEB35D8;
                        float _Property_D42BE95B_Out_0 = Vector1_B61034CA;
                        float _Split_CEEBC462_R_1 = IN.AbsoluteWorldSpacePosition[0];
                        float _Split_CEEBC462_G_2 = IN.AbsoluteWorldSpacePosition[1];
                        float _Split_CEEBC462_B_3 = IN.AbsoluteWorldSpacePosition[2];
                        float _Split_CEEBC462_A_4 = 0;
                        float2 _Vector2_BB7CEEF5_Out_0 = float2(_Split_CEEBC462_R_1, _Split_CEEBC462_B_3);
                        float2 _Property_305FB73_Out_0 = Vector2_9C73A0C6;
                        float _Property_88A7570_Out_0 = Vector1_B01E1A6A;
                        Bindings_CrestAmbientLight_a6ec89b3ca0ab4e98b300ec3ba0e6013 _CrestAmbientLight_BC7EB0E;
                        half3 _CrestAmbientLight_BC7EB0E_Color_1;
                        SG_CrestAmbientLight_a6ec89b3ca0ab4e98b300ec3ba0e6013(_CrestAmbientLight_BC7EB0E, _CrestAmbientLight_BC7EB0E_Color_1);
                        float2 _Property_5CBA815_Out_0 = Vector2_69CC43DC;
                        half3 _CustomFunction_3910AE92_Colour_16;
                        CrestNodeFoamBubbles_half(_Property_4729CD24_Out_0, _Property_C36E9B6_Out_0, _Property_2C173F60_Out_0, Texture2D_BE500045, _Property_4258AA49_Out_0, _Property_3FC34BC9_Out_0, _Property_D42BE95B_Out_0, _CrestComputeNormal_6DAE4B39_Normal_1, _CrestDrivenData_6635D6E9_OceanParams0_5, _CrestDrivenData_6635D6E9_OceanParams1_6, _Vector2_BB7CEEF5_Out_0, _Property_305FB73_Out_0, _Property_88A7570_Out_0, _Normalize_3574419A_Out_1, _CrestAmbientLight_BC7EB0E_Color_1, _Property_5CBA815_Out_0, _CustomFunction_3910AE92_Colour_16);
                        Bindings_CrestEmission_8c56460232fde1e46ae90d905a00f908 _CrestEmission_6580C0F9;
                        _CrestEmission_6580C0F9.FaceSign = IN.FaceSign;
                        float3 _CrestEmission_6580C0F9_EmittedLight_1;
                        SG_CrestEmission_8c56460232fde1e46ae90d905a00f908(_Property_2599690E_Out_0, _Property_A57CAD6E_Out_0, _CrestVolumeLighting_5A1A391_VolumeLighting_1, _Normalize_3574419A_Out_1, _CrestComputeNormal_6DAE4B39_Normal_1, _ScreenPosition_42FE0E3E_Out_0, _Negate_5F3C7D3D_Out_1, _SceneColor_61626E7C_Out_1, _SceneDepth_FD35F7D9_Out_1, _Property_4DD76B1_Out_0, _Property_DC1C088D_Out_0, TEXTURE2D_ARGS(Texture2D_EB8C8549, samplerTexture2D_EB8C8549), Texture2D_EB8C8549_TexelSize, _Property_8BC39E79_Out_0, _Property_683CAF2_Out_0, _Property_B4B35559_Out_0, _Property_E694F888_Out_0, _Property_50A87698_Out_0, TEXTURE2D_ARGS(Texture2D_DA8A756A, samplerTexture2D_DA8A756A), Texture2D_DA8A756A_TexelSize, _Property_8BC9D755_Out_0, _Property_6F95DFBF_Out_0, _CustomFunction_3910AE92_Colour_16, _CrestEmission_6580C0F9, _CrestEmission_6580C0F9_EmittedLight_1);
                        float3 _Multiply_CFF25F4B_Out_2;
                        Unity_Multiply_float((_CrestFresnel_FD3200EB_LightTransmitted_1.xxx), _CrestEmission_6580C0F9_EmittedLight_1, _Multiply_CFF25F4B_Out_2);
                        float4 _Property_2B229CB_Out_0 = Vector4_2F6E352;
                        float3 _Property_EB21E5D5_Out_0 = Vector3_57B74D6A;
                        float4 _Property_C16A4E4D_Out_0 = Vector4_B3AD63B4;
                        float _Property_496C0D8_Out_0 = Vector1_FA688590;
                        float _Property_D1C2E8AB_Out_0 = Vector1_9835666E;
                        float _Property_62C11254_Out_0 = Vector1_B331E24E;
                        float _Property_5977537D_Out_0 = Vector1_951CF2DF;
                        float4 _Property_A64A7625_Out_0 = Vector4_ADCA8891;
                        float _Property_6BDEE46A_Out_0 = Vector1_2E8E2C59;
                        float _Property_26AD77F8_Out_0 = Vector1_D74C6609;
                        float2 _Property_80C3F22F_Out_0 = Vector2_AE8873FA;
                        float _Property_5BAA7C5F_Out_0 = Vector1_255AB964;
                        float3 _Negate_99287013_Out_1;
                        Unity_Negate_float3(_Normalize_3574419A_Out_1, _Negate_99287013_Out_1);
                        float3 _Reflection_2C6907C_Out_2;
                        Unity_Reflection_float3(_Negate_99287013_Out_1, _CrestComputeNormal_6DAE4B39_Normal_1, _Reflection_2C6907C_Out_2);
                        Bindings_CrestAmbientLight_a6ec89b3ca0ab4e98b300ec3ba0e6013 _CrestAmbientLight_744A95D5;
                        half3 _CrestAmbientLight_744A95D5_Color_1;
                        SG_CrestAmbientLight_a6ec89b3ca0ab4e98b300ec3ba0e6013(_CrestAmbientLight_744A95D5, _CrestAmbientLight_744A95D5_Color_1);
                        float3 _Property_FC57CF9F_Out_0 = Vector3_B2D6AD84;
                        float3 _Property_BAEF74C8_Out_0 = Vector3_D2C93D25;
                        Bindings_CrestVolumeLighting_e9ed6e11710a50640bb4b811d0fa84f2 _CrestVolumeLighting_B895C731;
                        half3 _CrestVolumeLighting_B895C731_VolumeLighting_1;
                        SG_CrestVolumeLighting_e9ed6e11710a50640bb4b811d0fa84f2(_Property_2B229CB_Out_0, _Property_EB21E5D5_Out_0, _Property_C16A4E4D_Out_0, _Property_496C0D8_Out_0, _Property_D1C2E8AB_Out_0, _Property_62C11254_Out_0, _Property_5977537D_Out_0, _Property_A64A7625_Out_0, _Property_6BDEE46A_Out_0, _Property_26AD77F8_Out_0, _Property_80C3F22F_Out_0, _Property_5BAA7C5F_Out_0, _Reflection_2C6907C_Out_2, IN.AbsoluteWorldSpacePosition, _CrestAmbientLight_744A95D5_Color_1, _Property_FC57CF9F_Out_0, _Property_BAEF74C8_Out_0, _CrestVolumeLighting_B895C731, _CrestVolumeLighting_B895C731_VolumeLighting_1);
                        float3 _Multiply_C794DFA8_Out_2;
                        Unity_Multiply_float((_CrestFresnel_FD3200EB_LightReflected_2.xxx), _CrestVolumeLighting_B895C731_VolumeLighting_1, _Multiply_C794DFA8_Out_2);
                        float3 _Add_906C47A2_Out_2;
                        Unity_Add_float3(_Multiply_CFF25F4B_Out_2, _Multiply_C794DFA8_Out_2, _Add_906C47A2_Out_2);
                        float3 _Branch_A82A52E6_Out_3;
                        Unity_Branch_float3(_CrestIsUnderwater_5AB8F4C9_OutBoolean_1, _Add_906C47A2_Out_2, _Multiply_CFF25F4B_Out_2, _Branch_A82A52E6_Out_3);
                        float _Property_5DA83C2A_Out_0 = Vector1_47308CC2;
                        float _Property_44533AA4_Out_0 = Vector1_26549044;
                        float _Property_19CC00AB_Out_0 = Vector1_177E111C;
                        float _Divide_6B6C7F25_Out_2;
                        Unity_Divide_float(_Negate_5F3C7D3D_Out_1, _Property_19CC00AB_Out_0, _Divide_6B6C7F25_Out_2);
                        float _Saturate_D69CDED6_Out_1;
                        Unity_Saturate_float(_Divide_6B6C7F25_Out_2, _Saturate_D69CDED6_Out_1);
                        float _Property_8033277F_Out_0 = Vector1_33255829;
                        float _Power_BC121A67_Out_2;
                        Unity_Power_float(_Saturate_D69CDED6_Out_1, _Property_8033277F_Out_0, _Power_BC121A67_Out_2);
                        float _Lerp_3E7590A8_Out_3;
                        Unity_Lerp_float(_Property_5DA83C2A_Out_0, _Property_44533AA4_Out_0, _Power_BC121A67_Out_2, _Lerp_3E7590A8_Out_3);
                        float2 _Property_374C5B7E_Out_0 = Vector2_69CC43DC;
                        Bindings_CrestFoamWithFlow_3a6c22dceac0847a2a31cb05a577b963 _CrestFoamWithFlow_193F69CA;
                        float3 _CrestFoamWithFlow_193F69CA_Albedo_1;
                        float3 _CrestFoamWithFlow_193F69CA_NormalTS_2;
                        float3 _CrestFoamWithFlow_193F69CA_Emission_3;
                        float _CrestFoamWithFlow_193F69CA_Smoothness_4;
                        SG_CrestFoamWithFlow_3a6c22dceac0847a2a31cb05a577b963(TEXTURE2D_ARGS(Texture2D_BE500045, samplerTexture2D_BE500045), Texture2D_BE500045_TexelSize, _Property_B9BA8901_Out_0, _Property_6AEC3552_Out_0, _Property_76EA797B_Out_0, _Property_83C2E998_Out_0, _Property_705FD8D9_Out_0, _Property_735B5A53_Out_0, _Property_EB4C03CF_Out_0, _CrestDrivenData_6635D6E9_OceanParams0_5, _CrestDrivenData_6635D6E9_OceanParams1_6, _Property_EE920C2C_Out_0, _Property_8BC6F5AD_Out_0, _Property_A2CEB215_Out_0, _CrestComputeNormal_6DAE4B39_Normal_1, _Branch_A82A52E6_Out_3, _Lerp_3E7590A8_Out_3, _Negate_5F3C7D3D_Out_1, _Property_374C5B7E_Out_0, _CrestFoamWithFlow_193F69CA, _CrestFoamWithFlow_193F69CA_Albedo_1, _CrestFoamWithFlow_193F69CA_NormalTS_2, _CrestFoamWithFlow_193F69CA_Emission_3, _CrestFoamWithFlow_193F69CA_Smoothness_4);
                        Albedo_2 = _CrestFoamWithFlow_193F69CA_Albedo_1;
                        NormalTS_3 = _CrestFoamWithFlow_193F69CA_NormalTS_2;
                        Emission_4 = _CrestFoamWithFlow_193F69CA_Emission_3;
                        Smoothness_5 = _CrestFoamWithFlow_193F69CA_Smoothness_4;
                        Specular_6 = _CrestFresnel_FD3200EB_LightReflected_2;
                    }
                
                    // af89d2205c5dc03c016b842f3f74cf56
                    #include "ShadergraphFramework/CrestNodeOcclusion.hlsl"
                
                    struct Bindings_CrestOcclusion_8f9c2c1ada00f40a6a372f3868116540
                    {
                        float FaceSign;
                    };
                
                    void SG_CrestOcclusion_8f9c2c1ada00f40a6a372f3868116540(half Vector1_A0828619, Bindings_CrestOcclusion_8f9c2c1ada00f40a6a372f3868116540 IN, out half Occlusion_1)
                    {
                        half _Property_F6E978B_Out_0 = Vector1_A0828619;
                        Bindings_CrestIsUnderwater_52f7750f15e114937b54a0fd27f0d2f2 _CrestIsUnderwater_31591C4B;
                        _CrestIsUnderwater_31591C4B.FaceSign = IN.FaceSign;
                        float _CrestIsUnderwater_31591C4B_OutBoolean_1;
                        SG_CrestIsUnderwater_52f7750f15e114937b54a0fd27f0d2f2(_CrestIsUnderwater_31591C4B, _CrestIsUnderwater_31591C4B_OutBoolean_1);
                        half _CustomFunction_970B34CE_OcclusionOut_2;
                        CrestNodeOcclusion_half(_Property_F6E978B_Out_0, _CrestIsUnderwater_31591C4B_OutBoolean_1, _CustomFunction_970B34CE_OcclusionOut_2);
                        Occlusion_1 = _CustomFunction_970B34CE_OcclusionOut_2;
                    }
                
                    void Unity_Remap_float(float In, float2 InMinMax, float2 OutMinMax, out float Out)
                    {
                        Out = OutMinMax.x + (In - InMinMax.x) * (OutMinMax.y - OutMinMax.x) / (InMinMax.y - InMinMax.x);
                    }
                
                    // cb9e19acd0b7f8a1a1c0b9aed89ebacc
                    #include "ShadergraphFramework/CrestNodeSampleClipSurfaceData.hlsl"
                
                    struct Bindings_CrestClipSurface_8c73b4813486448849bd38d01267f186
                    {
                        float3 AbsoluteWorldSpacePosition;
                        half4 uv0;
                        half4 uv1;
                        half4 uv2;
                    };
                
                    void SG_CrestClipSurface_8c73b4813486448849bd38d01267f186(Bindings_CrestClipSurface_8c73b4813486448849bd38d01267f186 IN, out float ClipSurfaceValue_1)
                    {
                        float _Split_A2A81B90_R_1 = IN.AbsoluteWorldSpacePosition[0];
                        float _Split_A2A81B90_G_2 = IN.AbsoluteWorldSpacePosition[1];
                        float _Split_A2A81B90_B_3 = IN.AbsoluteWorldSpacePosition[2];
                        float _Split_A2A81B90_A_4 = 0;
                        float2 _Vector2_5DE2DA1_Out_0 = float2(_Split_A2A81B90_R_1, _Split_A2A81B90_B_3);
                        Bindings_CrestUnpackData_5bc4cf08f88035c489658d60edefeec8 _CrestUnpackData_ECB44FF9;
                        _CrestUnpackData_ECB44FF9.AbsoluteWorldSpacePosition = IN.AbsoluteWorldSpacePosition;
                        _CrestUnpackData_ECB44FF9.uv0 = IN.uv0;
                        _CrestUnpackData_ECB44FF9.uv1 = IN.uv1;
                        _CrestUnpackData_ECB44FF9.uv2 = IN.uv2;
                        float2 _CrestUnpackData_ECB44FF9_PositionXZWSUndisp_2;
                        float _CrestUnpackData_ECB44FF9_LodAlpha_1;
                        float _CrestUnpackData_ECB44FF9_OceanDepth_3;
                        float _CrestUnpackData_ECB44FF9_Foam_4;
                        float2 _CrestUnpackData_ECB44FF9_Shadow_5;
                        float2 _CrestUnpackData_ECB44FF9_Flow_6;
                        float _CrestUnpackData_ECB44FF9_SubSurfaceScattering_7;
                        SG_CrestUnpackData_5bc4cf08f88035c489658d60edefeec8(_CrestUnpackData_ECB44FF9, _CrestUnpackData_ECB44FF9_PositionXZWSUndisp_2, _CrestUnpackData_ECB44FF9_LodAlpha_1, _CrestUnpackData_ECB44FF9_OceanDepth_3, _CrestUnpackData_ECB44FF9_Foam_4, _CrestUnpackData_ECB44FF9_Shadow_5, _CrestUnpackData_ECB44FF9_Flow_6, _CrestUnpackData_ECB44FF9_SubSurfaceScattering_7);
                        Bindings_CrestDrivenData_40572ccadf7d9704a83d283fb4c9f19d _CrestDrivenData_EA2C4302;
                        half _CrestDrivenData_EA2C4302_MeshScaleAlpha_1;
                        half _CrestDrivenData_EA2C4302_LodDataTexelSize_8;
                        half _CrestDrivenData_EA2C4302_GeometryGridSize_2;
                        half3 _CrestDrivenData_EA2C4302_OceanPosScale0_3;
                        half3 _CrestDrivenData_EA2C4302_OceanPosScale1_4;
                        half4 _CrestDrivenData_EA2C4302_OceanParams0_5;
                        half4 _CrestDrivenData_EA2C4302_OceanParams1_6;
                        half _CrestDrivenData_EA2C4302_SliceIndex0_7;
                        SG_CrestDrivenData_40572ccadf7d9704a83d283fb4c9f19d(_CrestDrivenData_EA2C4302, _CrestDrivenData_EA2C4302_MeshScaleAlpha_1, _CrestDrivenData_EA2C4302_LodDataTexelSize_8, _CrestDrivenData_EA2C4302_GeometryGridSize_2, _CrestDrivenData_EA2C4302_OceanPosScale0_3, _CrestDrivenData_EA2C4302_OceanPosScale1_4, _CrestDrivenData_EA2C4302_OceanParams0_5, _CrestDrivenData_EA2C4302_OceanParams1_6, _CrestDrivenData_EA2C4302_SliceIndex0_7);
                        float _CustomFunction_26C15E74_ClipSurfaceValue_7;
                        CrestNodeSampleClipSurfaceData_float(_Vector2_5DE2DA1_Out_0, _CrestUnpackData_ECB44FF9_LodAlpha_1, _CrestDrivenData_EA2C4302_OceanPosScale0_3, _CrestDrivenData_EA2C4302_OceanPosScale1_4, _CrestDrivenData_EA2C4302_OceanParams0_5, _CrestDrivenData_EA2C4302_OceanParams1_6, _CrestDrivenData_EA2C4302_SliceIndex0_7, _CustomFunction_26C15E74_ClipSurfaceValue_7);
                        ClipSurfaceValue_1 = _CustomFunction_26C15E74_ClipSurfaceValue_7;
                    }
                
                // Vertex Graph Evaluation
                    VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
                    {
                        VertexDescription description = (VertexDescription)0;
                        return description;
                    }
                    
                // Pixel Graph Evaluation
                    SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
                    {
                        SurfaceDescription surface = (SurfaceDescription)0;
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        half _TexelSize_CA3A0DD6_Width_0 = _TextureFoam_TexelSize.z;
                        half _TexelSize_CA3A0DD6_Height_2 = _TextureFoam_TexelSize.w;
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        half2 _Vector2_15E252FB_Out_0 = half2(_TexelSize_CA3A0DD6_Width_0, _TexelSize_CA3A0DD6_Height_2);
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        half _Property_87E9391A_Out_0 = _FoamScale;
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        half _Property_BC30BCB6_Out_0 = _FoamFeather;
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        half _Property_5E8CB038_Out_0 = _FoamIntensityAlbedo;
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        half _Property_77A524C7_Out_0 = _FoamIntensityEmissive;
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        half _Property_CED296A4_Out_0 = _FoamSmoothness;
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        half _Property_D1CA3C84_Out_0 = _FoamNormalStrength;
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        half4 _Property_3BAB5FF_Out_0 = _FoamBubbleColor;
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        half _Property_EDC4DC3C_Out_0 = _FoamBubbleParallax;
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        half _Property_E7925FBA_Out_0 = _FoamBubblesCoverage;
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        Bindings_CrestUnpackData_5bc4cf08f88035c489658d60edefeec8 _CrestUnpackData_C3998C1C;
                        _CrestUnpackData_C3998C1C.AbsoluteWorldSpacePosition = IN.AbsoluteWorldSpacePosition;
                        _CrestUnpackData_C3998C1C.uv0 = IN.uv0;
                        _CrestUnpackData_C3998C1C.uv1 = IN.uv1;
                        _CrestUnpackData_C3998C1C.uv2 = IN.uv2;
                        float2 _CrestUnpackData_C3998C1C_PositionXZWSUndisp_2;
                        float _CrestUnpackData_C3998C1C_LodAlpha_1;
                        float _CrestUnpackData_C3998C1C_OceanDepth_3;
                        float _CrestUnpackData_C3998C1C_Foam_4;
                        float2 _CrestUnpackData_C3998C1C_Shadow_5;
                        float2 _CrestUnpackData_C3998C1C_Flow_6;
                        float _CrestUnpackData_C3998C1C_SubSurfaceScattering_7;
                        SG_CrestUnpackData_5bc4cf08f88035c489658d60edefeec8(_CrestUnpackData_C3998C1C, _CrestUnpackData_C3998C1C_PositionXZWSUndisp_2, _CrestUnpackData_C3998C1C_LodAlpha_1, _CrestUnpackData_C3998C1C_OceanDepth_3, _CrestUnpackData_C3998C1C_Foam_4, _CrestUnpackData_C3998C1C_Shadow_5, _CrestUnpackData_C3998C1C_Flow_6, _CrestUnpackData_C3998C1C_SubSurfaceScattering_7);
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        half _Property_994CB3A3_Out_0 = _Smoothness;
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        float _Property_7079FD32_Out_0 = _SmoothnessFar;
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        float _Property_FAEF961B_Out_0 = _SmoothnessFarDistance;
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        float _Property_35DA1E70_Out_0 = _SmoothnessFalloff;
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        half _Property_D1430252_Out_0 = _NormalsScale;
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        half _Property_B882DB1E_Out_0 = _NormalsStrength;
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        half4 _Property_B5E474DF_Out_0 = _ScatterColourBase;
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        half4 _Property_5D62FE57_Out_0 = _ScatterColourShadow;
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        float4 _Property_B98AE6FA_Out_0 = _ScatterColourShallow;
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        half _Property_CF7CEF3A_Out_0 = _ScatterColourShallowDepthMax;
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        half _Property_698A3C81_Out_0 = _ScatterColourShallowDepthFalloff;
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        half _Property_5924F1E9_Out_0 = _SSSIntensityBase;
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        half _Property_67308B5F_Out_0 = _SSSIntensitySun;
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        half4 _Property_A723E757_Out_0 = _SSSTint;
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        half _Property_5AD6818A_Out_0 = _SSSSunFalloff;
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        half _Property_888FA49_Out_0 = _RefractionStrength;
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        half3 _Property_F1315B19_Out_0 = _DepthFogDensity;
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        Bindings_CrestLightData_b74b6e8c0b489314ca7aea3e2cc9c54c _CrestLightData_5AD806DD;
                        half3 _CrestLightData_5AD806DD_Direction_1;
                        half3 _CrestLightData_5AD806DD_Intensity_2;
                        SG_CrestLightData_b74b6e8c0b489314ca7aea3e2cc9c54c(_CrestLightData_5AD806DD, _CrestLightData_5AD806DD_Direction_1, _CrestLightData_5AD806DD_Intensity_2);
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        float _Property_A26763AB_Out_0 = _CausticsTextureScale;
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        float _Property_3230CB90_Out_0 = _CausticsTextureAverage;
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        float _Property_6B2F7D3E_Out_0 = _CausticsStrength;
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        float _Property_D5C90779_Out_0 = _CausticsFocalDepth;
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        float _Property_2743B36F_Out_0 = _CausticsDepthOfField;
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        float _Property_61877218_Out_0 = _CausticsDistortionStrength;
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        float _Property_5F1E3794_Out_0 = _CausticsDistortionScale;
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        float _Property_7BD0FFAF_Out_0 = _MinReflectionDirectionY;
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        Bindings_CrestOceanPixel_6f6706d805d8e8649adddaaa94260269 _CrestOceanPixel_51593A7C;
                        _CrestOceanPixel_51593A7C.WorldSpaceViewDirection = IN.WorldSpaceViewDirection;
                        _CrestOceanPixel_51593A7C.ViewSpacePosition = IN.ViewSpacePosition;
                        _CrestOceanPixel_51593A7C.AbsoluteWorldSpacePosition = IN.AbsoluteWorldSpacePosition;
                        _CrestOceanPixel_51593A7C.ScreenPosition = IN.ScreenPosition;
                        _CrestOceanPixel_51593A7C.FaceSign = IN.FaceSign;
                        float3 _CrestOceanPixel_51593A7C_Albedo_2;
                        float3 _CrestOceanPixel_51593A7C_NormalTS_3;
                        float3 _CrestOceanPixel_51593A7C_Emission_4;
                        float _CrestOceanPixel_51593A7C_Smoothness_5;
                        float _CrestOceanPixel_51593A7C_Specular_6;
                        SG_CrestOceanPixel_6f6706d805d8e8649adddaaa94260269(TEXTURE2D_ARGS(_TextureFoam, sampler_TextureFoam), _TextureFoam_TexelSize, _Vector2_15E252FB_Out_0, _Property_87E9391A_Out_0, _Property_BC30BCB6_Out_0, _Property_5E8CB038_Out_0, _Property_77A524C7_Out_0, _Property_CED296A4_Out_0, _Property_D1CA3C84_Out_0, (_Property_3BAB5FF_Out_0.xyz), _Property_EDC4DC3C_Out_0, _Property_E7925FBA_Out_0, _CrestUnpackData_C3998C1C_PositionXZWSUndisp_2, _CrestUnpackData_C3998C1C_LodAlpha_1, _CrestUnpackData_C3998C1C_OceanDepth_3, _CrestUnpackData_C3998C1C_Foam_4, _CrestUnpackData_C3998C1C_Shadow_5, _CrestUnpackData_C3998C1C_Flow_6, _CrestUnpackData_C3998C1C_SubSurfaceScattering_7, _Property_994CB3A3_Out_0, _Property_7079FD32_Out_0, _Property_FAEF961B_Out_0, _Property_35DA1E70_Out_0, TEXTURE2D_ARGS(_TextureNormals, sampler_TextureNormals), _TextureNormals_TexelSize, _Property_D1430252_Out_0, _Property_B882DB1E_Out_0, _Property_B5E474DF_Out_0, (_Property_5D62FE57_Out_0.xyz), _Property_B98AE6FA_Out_0, _Property_CF7CEF3A_Out_0, _Property_698A3C81_Out_0, _Property_5924F1E9_Out_0, _Property_67308B5F_Out_0, _Property_A723E757_Out_0, _Property_5AD6818A_Out_0, _Property_888FA49_Out_0, _Property_F1315B19_Out_0, _CrestLightData_5AD806DD_Direction_1, _CrestLightData_5AD806DD_Intensity_2, TEXTURE2D_ARGS(_CausticsTexture, sampler_CausticsTexture), _CausticsTexture_TexelSize, _Property_A26763AB_Out_0, _Property_3230CB90_Out_0, _Property_6B2F7D3E_Out_0, _Property_D5C90779_Out_0, _Property_2743B36F_Out_0, TEXTURE2D_ARGS(_CausticsDistortionTexture, sampler_CausticsDistortionTexture), _CausticsDistortionTexture_TexelSize, _Property_61877218_Out_0, _Property_5F1E3794_Out_0, 1.33, 1, _Property_7BD0FFAF_Out_0, _CrestOceanPixel_51593A7C, _CrestOceanPixel_51593A7C_Albedo_2, _CrestOceanPixel_51593A7C_NormalTS_3, _CrestOceanPixel_51593A7C_Emission_4, _CrestOceanPixel_51593A7C_Smoothness_5, _CrestOceanPixel_51593A7C_Specular_6);
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        float _Property_A3329432_Out_0 = _Specular;
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        float _Multiply_E01A7830_Out_2;
                        Unity_Multiply_float(_Property_A3329432_Out_0, _CrestOceanPixel_51593A7C_Specular_6, _Multiply_E01A7830_Out_2);
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        float _Property_E132C977_Out_0 = _Occlusion;
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        Bindings_CrestOcclusion_8f9c2c1ada00f40a6a372f3868116540 _CrestOcclusion_1BA57722;
                        _CrestOcclusion_1BA57722.FaceSign = IN.FaceSign;
                        half _CrestOcclusion_1BA57722_Occlusion_1;
                        SG_CrestOcclusion_8f9c2c1ada00f40a6a372f3868116540(_Property_E132C977_Out_0, _CrestOcclusion_1BA57722, _CrestOcclusion_1BA57722_Occlusion_1);
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        float _SceneDepth_E2A24470_Out_1;
                        Unity_SceneDepth_Eye_float(float4(IN.ScreenPosition.xy / IN.ScreenPosition.w, 0, 0), _SceneDepth_E2A24470_Out_1);
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        float _Split_68C2565_R_1 = IN.ViewSpacePosition[0];
                        float _Split_68C2565_G_2 = IN.ViewSpacePosition[1];
                        float _Split_68C2565_B_3 = IN.ViewSpacePosition[2];
                        float _Split_68C2565_A_4 = 0;
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        float _Negate_847D9376_Out_1;
                        Unity_Negate_float(_Split_68C2565_B_3, _Negate_847D9376_Out_1);
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        float _Subtract_B338E0_Out_2;
                        Unity_Subtract_float(_SceneDepth_E2A24470_Out_1, _Negate_847D9376_Out_1, _Subtract_B338E0_Out_2);
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        float _Remap_6CD3C222_Out_3;
                        Unity_Remap_float(_Subtract_B338E0_Out_2, float2 (0, 0.2), float2 (0, 1), _Remap_6CD3C222_Out_3);
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        float _Saturate_FEF634C4_Out_1;
                        Unity_Saturate_float(_Remap_6CD3C222_Out_3, _Saturate_FEF634C4_Out_1);
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        Bindings_CrestClipSurface_8c73b4813486448849bd38d01267f186 _CrestClipSurface_AA3EF9C5;
                        _CrestClipSurface_AA3EF9C5.AbsoluteWorldSpacePosition = IN.AbsoluteWorldSpacePosition;
                        _CrestClipSurface_AA3EF9C5.uv0 = IN.uv0;
                        _CrestClipSurface_AA3EF9C5.uv1 = IN.uv1;
                        _CrestClipSurface_AA3EF9C5.uv2 = IN.uv2;
                        float _CrestClipSurface_AA3EF9C5_ClipSurfaceValue_1;
                        SG_CrestClipSurface_8c73b4813486448849bd38d01267f186(_CrestClipSurface_AA3EF9C5, _CrestClipSurface_AA3EF9C5_ClipSurfaceValue_1);
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        float _Subtract_900E903A_Out_2;
                        Unity_Subtract_float(_Saturate_FEF634C4_Out_1, _CrestClipSurface_AA3EF9C5_ClipSurfaceValue_1, _Subtract_900E903A_Out_2);
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        float _Saturate_A1AFAC8_Out_1;
                        Unity_Saturate_float(_Subtract_900E903A_Out_2, _Saturate_A1AFAC8_Out_1);
                        #endif
                        surface.Albedo = _CrestOceanPixel_51593A7C_Albedo_2;
                        surface.Normal = _CrestOceanPixel_51593A7C_NormalTS_3;
                        surface.Specular = (_Multiply_E01A7830_Out_2.xxx);
                        surface.Emission = _CrestOceanPixel_51593A7C_Emission_4;
                        surface.Smoothness = _CrestOceanPixel_51593A7C_Smoothness_5;
                        surface.Occlusion = _CrestOcclusion_1BA57722_Occlusion_1;
                        surface.Alpha = _Saturate_A1AFAC8_Out_1;
                        surface.AlphaClipThreshold = 0;
                        return surface;
                    }
                    
            //-------------------------------------------------------------------------------------
            // End graph generated code
            //-------------------------------------------------------------------------------------
        
        //-------------------------------------------------------------------------------------
            // TEMPLATE INCLUDE : VertexAnimation.template.hlsl
            //-------------------------------------------------------------------------------------
            
            
            VertexDescriptionInputs AttributesMeshToVertexDescriptionInputs(AttributesMesh input)
            {
                VertexDescriptionInputs output;
                ZERO_INITIALIZE(VertexDescriptionInputs, output);
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
                return output;
            }
            
            AttributesMesh ApplyMeshModification(AttributesMesh input, float3 timeParameters)
            {
                // build graph inputs
                VertexDescriptionInputs vertexDescriptionInputs = AttributesMeshToVertexDescriptionInputs(input);
                // Override time paramters with used one (This is required to correctly handle motion vector for vertex animation based on time)
            
            
                // evaluate vertex graph
                VertexDescription vertexDescription = VertexDescriptionFunction(vertexDescriptionInputs);
            
                // copy graph output to the results
            
            
            
            
                return input;
            }
            
            //-------------------------------------------------------------------------------------
            // END TEMPLATE INCLUDE : VertexAnimation.template.hlsl
            //-------------------------------------------------------------------------------------
            
        
        //-------------------------------------------------------------------------------------
            // TEMPLATE INCLUDE : SharedCode.template.hlsl
            //-------------------------------------------------------------------------------------
            
            #if !defined(SHADER_STAGE_RAY_TRACING)
                FragInputs BuildFragInputs(VaryingsMeshToPS input)
                {
                    FragInputs output;
                    ZERO_INITIALIZE(FragInputs, output);
            
                    // Init to some default value to make the computer quiet (else it output 'divide by zero' warning even if value is not used).
                    // TODO: this is a really poor workaround, but the variable is used in a bunch of places
                    // to compute normals which are then passed on elsewhere to compute other values...
                    output.tangentToWorld = k_identity3x3;
                    output.positionSS = input.positionCS;       // input.positionCS is SV_Position
            
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
            output.positionRWS = input.positionRWS;
            #endif
            
            
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
            output.texCoord0 = input.texCoord0;
            #endif
            
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
            output.texCoord1 = input.texCoord1;
            #endif
            
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
            output.texCoord2 = input.texCoord2;
            #endif
            
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
            output.texCoord3 = input.texCoord3;
            #endif
            
            
                    #if _DOUBLESIDED_ON && SHADER_STAGE_FRAGMENT
                    output.isFrontFace = IS_FRONT_VFACE(input.cullFace, true, false);
                    #elif SHADER_STAGE_FRAGMENT
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
            output.isFrontFace = IS_FRONT_VFACE(input.cullFace, true, false);
            #endif
            
                    #endif // SHADER_STAGE_FRAGMENT
            
                    return output;
                }
            #endif
                SurfaceDescriptionInputs FragInputsToSurfaceDescriptionInputs(FragInputs input, float3 viewWS)
                {
                    SurfaceDescriptionInputs output;
                    ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
            
            
            
            
            
            
            
            
            
            
            
            
            
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
            output.WorldSpaceViewDirection =     normalize(viewWS);
            #endif
            
            
            
            
            
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
            output.WorldSpacePosition =          input.positionRWS;
            #endif
            
            
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
            output.ViewSpacePosition =           TransformWorldToView(input.positionRWS);
            #endif
            
            
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
            output.AbsoluteWorldSpacePosition =  GetAbsolutePositionWS(input.positionRWS);
            #endif
            
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
            output.ScreenPosition =              ComputeScreenPos(TransformWorldToHClip(input.positionRWS), _ProjectionParams.x);
            #endif
            
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
            output.uv0 =                         input.texCoord0;
            #endif
            
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
            output.uv1 =                         input.texCoord1;
            #endif
            
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
            output.uv2 =                         input.texCoord2;
            #endif
            
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
            output.uv3 =                         input.texCoord3;
            #endif
            
            
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
            output.FaceSign =                    input.isFrontFace;
            #endif
            
            
            
                    return output;
                }
            
            #if !defined(SHADER_STAGE_RAY_TRACING)
            
                // existing HDRP code uses the combined function to go directly from packed to frag inputs
                FragInputs UnpackVaryingsMeshToFragInputs(PackedVaryingsMeshToPS input)
                {
                    UNITY_SETUP_INSTANCE_ID(input);
                    VaryingsMeshToPS unpacked= UnpackVaryingsMeshToPS(input);
                    return BuildFragInputs(unpacked);
                }
            #endif
            
            //-------------------------------------------------------------------------------------
            // END TEMPLATE INCLUDE : SharedCode.template.hlsl
            //-------------------------------------------------------------------------------------
            
        
        
            void BuildSurfaceData(FragInputs fragInputs, inout SurfaceDescription surfaceDescription, float3 V, PositionInputs posInput, out SurfaceData surfaceData)
            {
                // setup defaults -- these are used if the graph doesn't output a value
                ZERO_INITIALIZE(SurfaceData, surfaceData);
                surfaceData.ambientOcclusion = 1.0;
                surfaceData.specularOcclusion = 1.0; // This need to be init here to quiet the compiler in case of decal, but can be override later.
        
                // copy across graph values, if defined
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
        surfaceData.baseColor =             surfaceDescription.Albedo;
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
        surfaceData.perceptualSmoothness =  surfaceDescription.Smoothness;
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
        surfaceData.ambientOcclusion =      surfaceDescription.Occlusion;
        #endif
        
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
        surfaceData.specularColor =         surfaceDescription.Specular;
        #endif
        
        
                // These static material feature allow compile time optimization
                surfaceData.materialFeatures = MATERIALFEATUREFLAGS_LIT_STANDARD;
        #ifdef _MATERIAL_FEATURE_SPECULAR_COLOR
                surfaceData.materialFeatures |= MATERIALFEATUREFLAGS_LIT_SPECULAR_COLOR;
        #endif
        
                float3 doubleSidedConstants = float3(1.0, 1.0, 1.0);
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
        doubleSidedConstants = float3( 1.0,  1.0, -1.0);
        #endif
        
        
                // normal delivered to master node
                float3 normalSrc = float3(0.0f, 0.0f, 1.0f);
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
        normalSrc = surfaceDescription.Normal;
        #endif
        
        
                // compute world space normal
        #if _NORMAL_DROPOFF_TS
                GetNormalWS(fragInputs, normalSrc, surfaceData.normalWS, doubleSidedConstants);
        #elif _NORMAL_DROPOFF_OS
        		surfaceData.normalWS = TransformObjectToWorldNormal(normalSrc);
        #elif _NORMAL_DROPOFF_WS
        		surfaceData.normalWS = normalSrc;
        #endif
        
                surfaceData.geomNormalWS = fragInputs.tangentToWorld[2];
                surfaceData.tangentWS = normalize(fragInputs.tangentToWorld[0].xyz);    // The tangent is not normalize in tangentToWorld for mikkt. TODO: Check if it expected that we normalize with Morten. Tag: SURFACE_GRADIENT
        
        #if HAVE_DECALS
                if (_EnableDecals)
                {
                    // Both uses and modifies 'surfaceData.normalWS'.
                    DecalSurfaceData decalSurfaceData = GetDecalSurfaceData(posInput, surfaceDescription.Alpha);
                    ApplyDecalToSurfaceData(decalSurfaceData, surfaceData);
                }
        #endif
        
                surfaceData.tangentWS = Orthonormalize(surfaceData.tangentWS, surfaceData.normalWS);
        
                // By default we use the ambient occlusion with Tri-ace trick (apply outside) for specular occlusion as PBR master node don't have any option
                surfaceData.specularOcclusion = GetSpecularOcclusionFromAmbientOcclusion(ClampNdotV(dot(surfaceData.normalWS, V)), surfaceData.ambientOcclusion, PerceptualSmoothnessToRoughness(surfaceData.perceptualSmoothness));
        
        #ifdef DEBUG_DISPLAY
                if (_DebugMipMapMode != DEBUGMIPMAPMODE_NONE)
                {
                    // TODO: need to update mip info
                    surfaceData.metallic = 0;
                }
        
                // We need to call ApplyDebugToSurfaceData after filling the surfarcedata and before filling builtinData
                // as it can modify attribute use for static lighting
                ApplyDebugToSurfaceData(fragInputs.tangentToWorld, surfaceData);
        #endif
            }
        
            void GetSurfaceAndBuiltinData(FragInputs fragInputs, float3 V, inout PositionInputs posInput, out SurfaceData surfaceData, out BuiltinData builtinData)
            {
        #ifdef LOD_FADE_CROSSFADE // enable dithering LOD transition if user select CrossFade transition in LOD group
                LODDitheringTransition(ComputeFadeMaskSeed(V, posInput.positionSS), unity_LODFade.x);
        #endif
        
                float3 doubleSidedConstants = float3(1.0, 1.0, 1.0);
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
        doubleSidedConstants = float3( 1.0,  1.0, -1.0);
        #endif
        
        
                ApplyDoubleSidedFlipOrMirror(fragInputs, doubleSidedConstants);
        
                SurfaceDescriptionInputs surfaceDescriptionInputs = FragInputsToSurfaceDescriptionInputs(fragInputs, V);
                SurfaceDescription surfaceDescription = SurfaceDescriptionFunction(surfaceDescriptionInputs);
        
                // Perform alpha test very early to save performance (a killed pixel will not sample textures)
                // TODO: split graph evaluation to grab just alpha dependencies first? tricky..
        
        
                BuildSurfaceData(fragInputs, surfaceDescription, V, posInput, surfaceData);
        
                // Builtin Data
                // For back lighting we use the oposite vertex normal
                InitBuiltinData(posInput, surfaceDescription.Alpha, surfaceData.normalWS, -fragInputs.tangentToWorld[2], fragInputs.texCoord1, fragInputs.texCoord2, builtinData);
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
        builtinData.emissiveColor = surfaceDescription.Emission;
        #endif
        
        
                PostInitBuiltinData(V, posInput, surfaceData, builtinData);
            }
        
            //-------------------------------------------------------------------------------------
            // Pass Includes
            //-------------------------------------------------------------------------------------
                #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassLightTransport.hlsl"
            //-------------------------------------------------------------------------------------
            // End Pass Includes
            //-------------------------------------------------------------------------------------
        
            ENDHLSL
        }
        
        Pass
        {
            // based on HDPBRPass.template
            Name "SceneSelectionPass"
            Tags { "LightMode" = "SceneSelectionPass" }
        
            //-------------------------------------------------------------------------------------
            // Render Modes (Blend, Cull, ZTest, Stencil, etc)
            //-------------------------------------------------------------------------------------
            
			// Culling user defined - can be turned off for underwater
			Cull[_CullMode]

            
            ZWrite Off
        
            
            
            ColorMask 0
        
            //-------------------------------------------------------------------------------------
            // End Render Modes
            //-------------------------------------------------------------------------------------
        
            HLSLPROGRAM
        
            #pragma target 4.5
            #pragma only_renderers d3d11 ps4 xboxone vulkan metal switch
            //#pragma enable_d3d11_debug_symbols
        
            #pragma multi_compile_instancing
        #pragma instancing_options renderinglayer
        
            #pragma multi_compile _ LOD_FADE_CROSSFADE
        
            //-------------------------------------------------------------------------------------
            // Graph Defines
            //-------------------------------------------------------------------------------------
                    #define SHADERPASS SHADERPASS_DEPTH_ONLY
                #define SCENESELECTIONPASS
                #pragma editor_sync_compilation
                #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                #define REQUIRE_DEPTH_TEXTURE
                #endif
                // ACTIVE FIELDS:
                //   DoubleSided
                //   DoubleSided.Mirror
                //   FragInputs.isFrontFace
                //   Material.SpecularColor
                //   features.NormalDropOffTS
                //   SurfaceType.Transparent
                //   BlendMode.Alpha
                //   AlphaFog
                //   SurfaceDescription.Alpha
                //   SurfaceDescription.AlphaClipThreshold
                //   features.modifyMesh
                //   VertexDescription.VertexPosition
                //   VertexDescription.VertexNormal
                //   VertexDescription.VertexTangent
                //   VaryingsMeshToPS.cullFace
                // Shared Graph Keywords
                    #pragma shader_feature_local _ CREST_FOAM_ON
                    #pragma shader_feature_local _ CREST_CAUSTICS_ON
                    #pragma shader_feature_local _ CREST_FLOW_ON
                
                    #if defined(CREST_FOAM_ON) && defined(CREST_CAUSTICS_ON) && defined(CREST_FLOW_ON)
                        #define KEYWORD_PERMUTATION_0
                    #elif defined(CREST_FOAM_ON) && defined(CREST_CAUSTICS_ON)
                        #define KEYWORD_PERMUTATION_1
                    #elif defined(CREST_FOAM_ON) && defined(CREST_FLOW_ON)
                        #define KEYWORD_PERMUTATION_2
                    #elif defined(CREST_FOAM_ON)
                        #define KEYWORD_PERMUTATION_3
                    #elif defined(CREST_CAUSTICS_ON) && defined(CREST_FLOW_ON)
                        #define KEYWORD_PERMUTATION_4
                    #elif defined(CREST_CAUSTICS_ON)
                        #define KEYWORD_PERMUTATION_5
                    #elif defined(CREST_FLOW_ON)
                        #define KEYWORD_PERMUTATION_6
                    #else
                        #define KEYWORD_PERMUTATION_7
                    #endif
                
                
            //-------------------------------------------------------------------------------------
            // End Defines
            //-------------------------------------------------------------------------------------
        
            //-------------------------------------------------------------------------------------
            // Variant Definitions (active field translations to HDRP defines)
            //-------------------------------------------------------------------------------------
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
        #define _MATERIAL_FEATURE_SPECULAR_COLOR 1
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
        #define _SURFACE_TYPE_TRANSPARENT 1
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
        #define _BLENDMODE_ALPHA 1
        #endif
        
        
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
        #define _DOUBLESIDED_ON 1
        #endif
        
            #define _NORMAL_DROPOFF_TS	1
            // #define _NORMAL_DROPOFF_OS	1
            // #define _NORMAL_DROPOFF_WS	1
        
			#define CREST_GENERATED_SHADER_ON 1

            //-------------------------------------------------------------------------------------
            // End Variant Definitions
            //-------------------------------------------------------------------------------------
        
            #pragma vertex Vert
            #pragma fragment Frag
        
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/NormalSurfaceGradient.hlsl"
        
            // define FragInputs structure
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl"
        
            //-------------------------------------------------------------------------------------
            // Active Field Defines
            //-------------------------------------------------------------------------------------
        
            // this translates the new dependency tracker into the old preprocessor definitions for the existing HDRP shader code
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
        #define ATTRIBUTES_NEED_NORMAL
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
        #define ATTRIBUTES_NEED_TANGENT
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
        #define ATTRIBUTES_NEED_TEXCOORD0
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
        #define ATTRIBUTES_NEED_TEXCOORD1
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
        #define ATTRIBUTES_NEED_TEXCOORD2
        #endif
        
        
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
        #define VARYINGS_NEED_POSITION_WS
        #endif
        
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
        #define VARYINGS_NEED_TEXCOORD0
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
        #define VARYINGS_NEED_TEXCOORD1
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
        #define VARYINGS_NEED_TEXCOORD2
        #endif
        
        
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
        #define VARYINGS_NEED_CULLFACE
        #endif
        
            #define HAVE_MESH_MODIFICATION
        
            //-------------------------------------------------------------------------------------
            // End Defines
            //-------------------------------------------------------------------------------------
        	
        
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #ifdef DEBUG_DISPLAY
                #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Debug/DebugDisplay.hlsl"
            #endif
        
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl"
        
        #if (SHADERPASS == SHADERPASS_FORWARD)
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/Lighting.hlsl"
        
            #define HAS_LIGHTLOOP
        
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoopDef.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/Lit.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoop.hlsl"
        #else
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/Lit.hlsl"
        #endif
        
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalUtilities.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/LitDecalData.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl"
        
            //Used by SceneSelectionPass
            int _ObjectId;
            int _PassValue;
        
            //-------------------------------------------------------------------------------------
            // Interpolator Packing And Struct Declarations
            //-------------------------------------------------------------------------------------
            // Generated Type: AttributesMesh
            struct AttributesMesh
            {
                #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                float3 positionOS : POSITION;
                #endif // Shader Graph Keywords
                #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                float3 normalOS : NORMAL; // optional
                #endif // Shader Graph Keywords
                #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                float4 tangentOS : TANGENT; // optional
                #endif // Shader Graph Keywords
                #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                float4 uv0 : TEXCOORD0; // optional
                #endif // Shader Graph Keywords
                #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                float4 uv1 : TEXCOORD1; // optional
                #endif // Shader Graph Keywords
                #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                float4 uv2 : TEXCOORD2; // optional
                #endif // Shader Graph Keywords
                #if UNITY_ANY_INSTANCING_ENABLED
                #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                uint instanceID : INSTANCEID_SEMANTIC;
                #endif // Shader Graph Keywords
                #endif // UNITY_ANY_INSTANCING_ENABLED
            };
            // Generated Type: VaryingsMeshToPS
            struct VaryingsMeshToPS
            {
                #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                float4 positionCS : SV_POSITION;
                #endif // Shader Graph Keywords
                #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                float3 positionRWS; // optional
                #endif // Shader Graph Keywords
                #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                float4 texCoord0; // optional
                #endif // Shader Graph Keywords
                #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                float4 texCoord1; // optional
                #endif // Shader Graph Keywords
                #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                float4 texCoord2; // optional
                #endif // Shader Graph Keywords
                #if UNITY_ANY_INSTANCING_ENABLED
                #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                uint instanceID : CUSTOM_INSTANCE_ID;
                #endif // Shader Graph Keywords
                #endif // UNITY_ANY_INSTANCING_ENABLED
                #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
                #endif // Shader Graph Keywords
                #endif // defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            };
            
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
            // Generated Type: PackedVaryingsMeshToPS
            struct PackedVaryingsMeshToPS
            {
                float4 positionCS : SV_POSITION; // unpacked
                #if UNITY_ANY_INSTANCING_ENABLED
                uint instanceID : CUSTOM_INSTANCE_ID; // unpacked
                #endif // conditional
                float3 interp00 : TEXCOORD0; // auto-packed
                float4 interp01 : TEXCOORD1; // auto-packed
                float4 interp02 : TEXCOORD2; // auto-packed
                float4 interp03 : TEXCOORD3; // auto-packed
                #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC; // unpacked
                #endif // conditional
            };
            
            // Packed Type: VaryingsMeshToPS
            PackedVaryingsMeshToPS PackVaryingsMeshToPS(VaryingsMeshToPS input)
            {
                PackedVaryingsMeshToPS output = (PackedVaryingsMeshToPS)0;
                output.positionCS = input.positionCS;
                output.interp00.xyz = input.positionRWS;
                output.interp01.xyzw = input.texCoord0;
                output.interp02.xyzw = input.texCoord1;
                output.interp03.xyzw = input.texCoord2;
                #if UNITY_ANY_INSTANCING_ENABLED
                output.instanceID = input.instanceID;
                #endif // conditional
                #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                output.cullFace = input.cullFace;
                #endif // conditional
                return output;
            }
            
            // Unpacked Type: VaryingsMeshToPS
            VaryingsMeshToPS UnpackVaryingsMeshToPS(PackedVaryingsMeshToPS input)
            {
                VaryingsMeshToPS output = (VaryingsMeshToPS)0;
                output.positionCS = input.positionCS;
                output.positionRWS = input.interp00.xyz;
                output.texCoord0 = input.interp01.xyzw;
                output.texCoord1 = input.interp02.xyzw;
                output.texCoord2 = input.interp03.xyzw;
                #if UNITY_ANY_INSTANCING_ENABLED
                output.instanceID = input.instanceID;
                #endif // conditional
                #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                output.cullFace = input.cullFace;
                #endif // conditional
                return output;
            }
            #endif
            // Generated Type: VaryingsMeshToDS
            struct VaryingsMeshToDS
            {
                #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                float3 positionRWS;
                #endif // Shader Graph Keywords
                #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                float3 normalWS;
                #endif // Shader Graph Keywords
                #if UNITY_ANY_INSTANCING_ENABLED
                #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                uint instanceID : CUSTOM_INSTANCE_ID;
                #endif // Shader Graph Keywords
                #endif // UNITY_ANY_INSTANCING_ENABLED
            };
            
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
            // Generated Type: PackedVaryingsMeshToDS
            struct PackedVaryingsMeshToDS
            {
                #if UNITY_ANY_INSTANCING_ENABLED
                uint instanceID : CUSTOM_INSTANCE_ID; // unpacked
                #endif // conditional
                float3 interp00 : TEXCOORD0; // auto-packed
                float3 interp01 : TEXCOORD1; // auto-packed
            };
            
            // Packed Type: VaryingsMeshToDS
            PackedVaryingsMeshToDS PackVaryingsMeshToDS(VaryingsMeshToDS input)
            {
                PackedVaryingsMeshToDS output = (PackedVaryingsMeshToDS)0;
                output.interp00.xyz = input.positionRWS;
                output.interp01.xyz = input.normalWS;
                #if UNITY_ANY_INSTANCING_ENABLED
                output.instanceID = input.instanceID;
                #endif // conditional
                return output;
            }
            
            // Unpacked Type: VaryingsMeshToDS
            VaryingsMeshToDS UnpackVaryingsMeshToDS(PackedVaryingsMeshToDS input)
            {
                VaryingsMeshToDS output = (VaryingsMeshToDS)0;
                output.positionRWS = input.interp00.xyz;
                output.normalWS = input.interp01.xyz;
                #if UNITY_ANY_INSTANCING_ENABLED
                output.instanceID = input.instanceID;
                #endif // conditional
                return output;
            }
            #endif
            //-------------------------------------------------------------------------------------
            // End Interpolator Packing And Struct Declarations
            //-------------------------------------------------------------------------------------
        
            //-------------------------------------------------------------------------------------
            // Graph generated code
            //-------------------------------------------------------------------------------------
                    // Shared Graph Properties (uniform inputs)
                    CBUFFER_START(UnityPerMaterial)
                    half _NormalsScale;
                    half _NormalsStrength;
                    half4 _ScatterColourBase;
                    half4 _ScatterColourShadow;
                    float4 _ScatterColourShallow;
                    half _ScatterColourShallowDepthMax;
                    half _ScatterColourShallowDepthFalloff;
                    half _SSSIntensityBase;
                    half _SSSIntensitySun;
                    half4 _SSSTint;
                    half _SSSSunFalloff;
                    float _Specular;
                    float _Occlusion;
                    half _Smoothness;
                    float _SmoothnessFar;
                    float _SmoothnessFarDistance;
                    float _SmoothnessFalloff;
                    float _MinReflectionDirectionY;
                    half _FoamScale;
                    half _FoamFeather;
                    half _FoamIntensityAlbedo;
                    half _FoamIntensityEmissive;
                    half _FoamSmoothness;
                    half _FoamNormalStrength;
                    half4 _FoamBubbleColor;
                    half _FoamBubbleParallax;
                    half _FoamBubblesCoverage;
                    half _RefractionStrength;
                    half3 _DepthFogDensity;
                    float _CausticsTextureScale;
                    float _CausticsTextureAverage;
                    float _CausticsStrength;
                    float _CausticsFocalDepth;
                    float _CausticsDepthOfField;
                    float _CausticsDistortionStrength;
                    float _CausticsDistortionScale;
                    CBUFFER_END
                    TEXTURE2D(_TextureNormals); SAMPLER(sampler_TextureNormals); float4 _TextureNormals_TexelSize;
                    TEXTURE2D(_TextureFoam); SAMPLER(sampler_TextureFoam); half4 _TextureFoam_TexelSize;
                    TEXTURE2D(_CausticsTexture); SAMPLER(sampler_CausticsTexture); float4 _CausticsTexture_TexelSize;
                    TEXTURE2D(_CausticsDistortionTexture); SAMPLER(sampler_CausticsDistortionTexture); float4 _CausticsDistortionTexture_TexelSize;
                
                // Vertex Graph Inputs
                    struct VertexDescriptionInputs
                    {
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        float3 WorldSpaceNormal; // optional
                        #endif // Shader Graph Keywords
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        float3 WorldSpaceTangent; // optional
                        #endif // Shader Graph Keywords
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        float3 ObjectSpaceBiTangent; // optional
                        #endif // Shader Graph Keywords
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        float3 WorldSpaceBiTangent; // optional
                        #endif // Shader Graph Keywords
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        float3 AbsoluteWorldSpacePosition; // optional
                        #endif // Shader Graph Keywords
                    };
                // Vertex Graph Outputs
                    struct VertexDescription
                    {
                        float3 VertexPosition;
                        float3 VertexNormal;
                        float3 VertexTangent;
                    };
                    
                // Pixel Graph Inputs
                    struct SurfaceDescriptionInputs
                    {
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        float3 ViewSpacePosition; // optional
                        #endif // Shader Graph Keywords
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        float3 WorldSpacePosition; // optional
                        #endif // Shader Graph Keywords
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        float3 AbsoluteWorldSpacePosition; // optional
                        #endif // Shader Graph Keywords
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        float4 ScreenPosition; // optional
                        #endif // Shader Graph Keywords
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        float4 uv0; // optional
                        #endif // Shader Graph Keywords
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        float4 uv1; // optional
                        #endif // Shader Graph Keywords
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        float4 uv2; // optional
                        #endif // Shader Graph Keywords
                    };
                // Pixel Graph Outputs
                    struct SurfaceDescription
                    {
                        float Alpha;
                        float AlphaClipThreshold;
                    };
                    
                // Shared Graph Node Functions
                
                    void Unity_SceneDepth_Eye_float(float4 UV, out float Out)
                    {
                        Out = LinearEyeDepth(SHADERGRAPH_SAMPLE_SCENE_DEPTH(UV.xy), _ZBufferParams);
                    }
                
                    void Unity_Negate_float(float In, out float Out)
                    {
                        Out = -1 * In;
                    }
                
                    void Unity_Subtract_float(float A, float B, out float Out)
                    {
                        Out = A - B;
                    }
                
                    void Unity_Remap_float(float In, float2 InMinMax, float2 OutMinMax, out float Out)
                    {
                        Out = OutMinMax.x + (In - InMinMax.x) * (OutMinMax.y - OutMinMax.x) / (InMinMax.y - InMinMax.x);
                    }
                
                    void Unity_Saturate_float(float In, out float Out)
                    {
                        Out = saturate(In);
                    }
                
                    // 73abdd69b930b80b268ce9c97db7e24b
                    #include "ShadergraphFramework/CrestNodeComputeSamplingData.hlsl"
                
                    // 2973156898769ad700b9d1820ecfd177
                    #include "ShadergraphFramework/CrestNodeSampleOceanData.hlsl"
                
                    struct Bindings_CrestSampleOceanData_f19d6d2aa60f8294e870f100303ee5a4
                    {
                    };
                
                    void SG_CrestSampleOceanData_f19d6d2aa60f8294e870f100303ee5a4(float2 Vector2_3171933F, float Vector1_CD41515B, float3 Vector3_7E91D336, float3 Vector3_3A95DCDF, float4 Vector4_C0B2B5EA, float4 Vector4_9C46108E, float Vector1_8EA8B92B, Bindings_CrestSampleOceanData_f19d6d2aa60f8294e870f100303ee5a4 IN, out float3 Displacement_1, out float OceanWaterDepth_5, out float Foam_6, out float2 Shadow_7, out float2 Flow_8, out float SubSurfaceScattering_9)
                    {
                        float2 _Property_1A287CC6_Out_0 = Vector2_3171933F;
                        float _Property_2D1D1700_Out_0 = Vector1_CD41515B;
                        float3 _Property_C925A867_Out_0 = Vector3_7E91D336;
                        float3 _Property_6C273401_Out_0 = Vector3_3A95DCDF;
                        float4 _Property_467D1BE7_Out_0 = Vector4_C0B2B5EA;
                        float4 _Property_4E045F45_Out_0 = Vector4_9C46108E;
                        float _Property_59E019ED_Out_0 = Vector1_8EA8B92B;
                        float3 _CustomFunction_487C31E1_Displacement_3;
                        float _CustomFunction_487C31E1_OceanDepth_8;
                        float _CustomFunction_487C31E1_Foam_4;
                        float2 _CustomFunction_487C31E1_Shadow_5;
                        float2 _CustomFunction_487C31E1_Flow_6;
                        float _CustomFunction_487C31E1_SSS_17;
                        CrestNodeSampleOceanData_float(_Property_1A287CC6_Out_0, _Property_2D1D1700_Out_0, _Property_C925A867_Out_0, _Property_6C273401_Out_0, _Property_467D1BE7_Out_0, _Property_4E045F45_Out_0, _Property_59E019ED_Out_0, _CustomFunction_487C31E1_Displacement_3, _CustomFunction_487C31E1_OceanDepth_8, _CustomFunction_487C31E1_Foam_4, _CustomFunction_487C31E1_Shadow_5, _CustomFunction_487C31E1_Flow_6, _CustomFunction_487C31E1_SSS_17);
                        Displacement_1 = _CustomFunction_487C31E1_Displacement_3;
                        OceanWaterDepth_5 = _CustomFunction_487C31E1_OceanDepth_8;
                        Foam_6 = _CustomFunction_487C31E1_Foam_4;
                        Shadow_7 = _CustomFunction_487C31E1_Shadow_5;
                        Flow_8 = _CustomFunction_487C31E1_Flow_6;
                        SubSurfaceScattering_9 = _CustomFunction_487C31E1_SSS_17;
                    }
                
                    void Unity_Subtract_float2(float2 A, float2 B, out float2 Out)
                    {
                        Out = A - B;
                    }
                
                    struct Bindings_CrestComputeOceanDataFromDisplacedPosition_1f483fe321b10c64e8b93a023ae60dd6
                    {
                    };
                
                    void SG_CrestComputeOceanDataFromDisplacedPosition_1f483fe321b10c64e8b93a023ae60dd6(float3 Vector3_A7B8495A, Bindings_CrestComputeOceanDataFromDisplacedPosition_1f483fe321b10c64e8b93a023ae60dd6 IN, out float2 UndisplacedPosXZAWS_7, out float LodAlpha_6, out float3 Displacement_8, out float OceanWaterDepth_1, out float Foam_2, out float2 Shadow_3, out float2 Flow_4, out float SubSurfaceScattering_5)
                    {
                        float3 _Property_232E7FDA_Out_0 = Vector3_A7B8495A;
                        float _Split_8E8A6DCA_R_1 = _Property_232E7FDA_Out_0[0];
                        float _Split_8E8A6DCA_G_2 = _Property_232E7FDA_Out_0[1];
                        float _Split_8E8A6DCA_B_3 = _Property_232E7FDA_Out_0[2];
                        float _Split_8E8A6DCA_A_4 = 0;
                        float2 _Vector2_A3499051_Out_0 = float2(_Split_8E8A6DCA_R_1, _Split_8E8A6DCA_B_3);
                        half _CustomFunction_A082C8F2_LodAlpha_3;
                        half3 _CustomFunction_A082C8F2_OceanPosScale0_4;
                        half3 _CustomFunction_A082C8F2_OceanPosScale1_5;
                        half4 _CustomFunction_A082C8F2_OceanParams0_6;
                        half4 _CustomFunction_A082C8F2_OceanParams1_7;
                        half _CustomFunction_A082C8F2_Slice0_1;
                        half _CustomFunction_A082C8F2_Slice1_2;
                        CrestComputeSamplingData_half(_Vector2_A3499051_Out_0, _CustomFunction_A082C8F2_LodAlpha_3, _CustomFunction_A082C8F2_OceanPosScale0_4, _CustomFunction_A082C8F2_OceanPosScale1_5, _CustomFunction_A082C8F2_OceanParams0_6, _CustomFunction_A082C8F2_OceanParams1_7, _CustomFunction_A082C8F2_Slice0_1, _CustomFunction_A082C8F2_Slice1_2);
                        Bindings_CrestSampleOceanData_f19d6d2aa60f8294e870f100303ee5a4 _CrestSampleOceanData_87ACC65F;
                        float3 _CrestSampleOceanData_87ACC65F_Displacement_1;
                        float _CrestSampleOceanData_87ACC65F_OceanWaterDepth_5;
                        float _CrestSampleOceanData_87ACC65F_Foam_6;
                        float2 _CrestSampleOceanData_87ACC65F_Shadow_7;
                        float2 _CrestSampleOceanData_87ACC65F_Flow_8;
                        float _CrestSampleOceanData_87ACC65F_SubSurfaceScattering_9;
                        SG_CrestSampleOceanData_f19d6d2aa60f8294e870f100303ee5a4(_Vector2_A3499051_Out_0, _CustomFunction_A082C8F2_LodAlpha_3, _CustomFunction_A082C8F2_OceanPosScale0_4, _CustomFunction_A082C8F2_OceanPosScale1_5, _CustomFunction_A082C8F2_OceanParams0_6, _CustomFunction_A082C8F2_OceanParams1_7, _CustomFunction_A082C8F2_Slice0_1, _CrestSampleOceanData_87ACC65F, _CrestSampleOceanData_87ACC65F_Displacement_1, _CrestSampleOceanData_87ACC65F_OceanWaterDepth_5, _CrestSampleOceanData_87ACC65F_Foam_6, _CrestSampleOceanData_87ACC65F_Shadow_7, _CrestSampleOceanData_87ACC65F_Flow_8, _CrestSampleOceanData_87ACC65F_SubSurfaceScattering_9);
                        float _Split_CD3A9051_R_1 = _CrestSampleOceanData_87ACC65F_Displacement_1[0];
                        float _Split_CD3A9051_G_2 = _CrestSampleOceanData_87ACC65F_Displacement_1[1];
                        float _Split_CD3A9051_B_3 = _CrestSampleOceanData_87ACC65F_Displacement_1[2];
                        float _Split_CD3A9051_A_4 = 0;
                        float2 _Vector2_B8C0C1F0_Out_0 = float2(_Split_CD3A9051_R_1, _Split_CD3A9051_B_3);
                        float2 _Subtract_8977A663_Out_2;
                        Unity_Subtract_float2(_Vector2_A3499051_Out_0, _Vector2_B8C0C1F0_Out_0, _Subtract_8977A663_Out_2);
                        half _CustomFunction_9D8B14F0_LodAlpha_3;
                        half3 _CustomFunction_9D8B14F0_OceanPosScale0_4;
                        half3 _CustomFunction_9D8B14F0_OceanPosScale1_5;
                        half4 _CustomFunction_9D8B14F0_OceanParams0_6;
                        half4 _CustomFunction_9D8B14F0_OceanParams1_7;
                        half _CustomFunction_9D8B14F0_Slice0_1;
                        half _CustomFunction_9D8B14F0_Slice1_2;
                        CrestComputeSamplingData_half(_Subtract_8977A663_Out_2, _CustomFunction_9D8B14F0_LodAlpha_3, _CustomFunction_9D8B14F0_OceanPosScale0_4, _CustomFunction_9D8B14F0_OceanPosScale1_5, _CustomFunction_9D8B14F0_OceanParams0_6, _CustomFunction_9D8B14F0_OceanParams1_7, _CustomFunction_9D8B14F0_Slice0_1, _CustomFunction_9D8B14F0_Slice1_2);
                        Bindings_CrestSampleOceanData_f19d6d2aa60f8294e870f100303ee5a4 _CrestSampleOceanData_D8619779;
                        float3 _CrestSampleOceanData_D8619779_Displacement_1;
                        float _CrestSampleOceanData_D8619779_OceanWaterDepth_5;
                        float _CrestSampleOceanData_D8619779_Foam_6;
                        float2 _CrestSampleOceanData_D8619779_Shadow_7;
                        float2 _CrestSampleOceanData_D8619779_Flow_8;
                        float _CrestSampleOceanData_D8619779_SubSurfaceScattering_9;
                        SG_CrestSampleOceanData_f19d6d2aa60f8294e870f100303ee5a4(_Subtract_8977A663_Out_2, _CustomFunction_9D8B14F0_LodAlpha_3, _CustomFunction_9D8B14F0_OceanPosScale0_4, _CustomFunction_9D8B14F0_OceanPosScale1_5, _CustomFunction_9D8B14F0_OceanParams0_6, _CustomFunction_9D8B14F0_OceanParams1_7, _CustomFunction_9D8B14F0_Slice0_1, _CrestSampleOceanData_D8619779, _CrestSampleOceanData_D8619779_Displacement_1, _CrestSampleOceanData_D8619779_OceanWaterDepth_5, _CrestSampleOceanData_D8619779_Foam_6, _CrestSampleOceanData_D8619779_Shadow_7, _CrestSampleOceanData_D8619779_Flow_8, _CrestSampleOceanData_D8619779_SubSurfaceScattering_9);
                        float _Split_1616DE7_R_1 = _CrestSampleOceanData_D8619779_Displacement_1[0];
                        float _Split_1616DE7_G_2 = _CrestSampleOceanData_D8619779_Displacement_1[1];
                        float _Split_1616DE7_B_3 = _CrestSampleOceanData_D8619779_Displacement_1[2];
                        float _Split_1616DE7_A_4 = 0;
                        float2 _Vector2_B871614F_Out_0 = float2(_Split_1616DE7_R_1, _Split_1616DE7_B_3);
                        float2 _Subtract_39E2CE30_Out_2;
                        Unity_Subtract_float2(_Vector2_A3499051_Out_0, _Vector2_B871614F_Out_0, _Subtract_39E2CE30_Out_2);
                        half _CustomFunction_10AEAD9A_LodAlpha_3;
                        half3 _CustomFunction_10AEAD9A_OceanPosScale0_4;
                        half3 _CustomFunction_10AEAD9A_OceanPosScale1_5;
                        half4 _CustomFunction_10AEAD9A_OceanParams0_6;
                        half4 _CustomFunction_10AEAD9A_OceanParams1_7;
                        half _CustomFunction_10AEAD9A_Slice0_1;
                        half _CustomFunction_10AEAD9A_Slice1_2;
                        CrestComputeSamplingData_half(_Subtract_39E2CE30_Out_2, _CustomFunction_10AEAD9A_LodAlpha_3, _CustomFunction_10AEAD9A_OceanPosScale0_4, _CustomFunction_10AEAD9A_OceanPosScale1_5, _CustomFunction_10AEAD9A_OceanParams0_6, _CustomFunction_10AEAD9A_OceanParams1_7, _CustomFunction_10AEAD9A_Slice0_1, _CustomFunction_10AEAD9A_Slice1_2);
                        Bindings_CrestSampleOceanData_f19d6d2aa60f8294e870f100303ee5a4 _CrestSampleOceanData_A1195FE2;
                        float3 _CrestSampleOceanData_A1195FE2_Displacement_1;
                        float _CrestSampleOceanData_A1195FE2_OceanWaterDepth_5;
                        float _CrestSampleOceanData_A1195FE2_Foam_6;
                        float2 _CrestSampleOceanData_A1195FE2_Shadow_7;
                        float2 _CrestSampleOceanData_A1195FE2_Flow_8;
                        float _CrestSampleOceanData_A1195FE2_SubSurfaceScattering_9;
                        SG_CrestSampleOceanData_f19d6d2aa60f8294e870f100303ee5a4(_Subtract_39E2CE30_Out_2, _CustomFunction_10AEAD9A_LodAlpha_3, _CustomFunction_10AEAD9A_OceanPosScale0_4, _CustomFunction_10AEAD9A_OceanPosScale1_5, _CustomFunction_10AEAD9A_OceanParams0_6, _CustomFunction_10AEAD9A_OceanParams1_7, _CustomFunction_10AEAD9A_Slice0_1, _CrestSampleOceanData_A1195FE2, _CrestSampleOceanData_A1195FE2_Displacement_1, _CrestSampleOceanData_A1195FE2_OceanWaterDepth_5, _CrestSampleOceanData_A1195FE2_Foam_6, _CrestSampleOceanData_A1195FE2_Shadow_7, _CrestSampleOceanData_A1195FE2_Flow_8, _CrestSampleOceanData_A1195FE2_SubSurfaceScattering_9);
                        UndisplacedPosXZAWS_7 = _Subtract_39E2CE30_Out_2;
                        LodAlpha_6 = _CustomFunction_10AEAD9A_LodAlpha_3;
                        Displacement_8 = _CrestSampleOceanData_A1195FE2_Displacement_1;
                        OceanWaterDepth_1 = _CrestSampleOceanData_A1195FE2_OceanWaterDepth_5;
                        Foam_2 = _CrestSampleOceanData_A1195FE2_Foam_6;
                        Shadow_3 = _CrestSampleOceanData_A1195FE2_Shadow_7;
                        Flow_4 = _CrestSampleOceanData_A1195FE2_Flow_8;
                        SubSurfaceScattering_5 = _CrestSampleOceanData_A1195FE2_SubSurfaceScattering_9;
                    }
                
                    struct Bindings_CrestUnpackData_5bc4cf08f88035c489658d60edefeec8
                    {
                        float3 AbsoluteWorldSpacePosition;
                        half4 uv0;
                        half4 uv1;
                        half4 uv2;
                    };
                
                    void SG_CrestUnpackData_5bc4cf08f88035c489658d60edefeec8(Bindings_CrestUnpackData_5bc4cf08f88035c489658d60edefeec8 IN, out float2 PositionXZWSUndisp_2, out float LodAlpha_1, out float OceanDepth_3, out float Foam_4, out float2 Shadow_5, out float2 Flow_6, out float SubSurfaceScattering_7)
                    {
                        float4 _UV_CF6CD5F2_Out_0 = IN.uv0;
                        float _Split_B10345C8_R_1 = _UV_CF6CD5F2_Out_0[0];
                        float _Split_B10345C8_G_2 = _UV_CF6CD5F2_Out_0[1];
                        float _Split_B10345C8_B_3 = _UV_CF6CD5F2_Out_0[2];
                        float _Split_B10345C8_A_4 = _UV_CF6CD5F2_Out_0[3];
                        float2 _Vector2_552A5E1F_Out_0 = float2(_Split_B10345C8_R_1, _Split_B10345C8_G_2);
                        Bindings_CrestComputeOceanDataFromDisplacedPosition_1f483fe321b10c64e8b93a023ae60dd6 _CrestComputeOceanDataFromDisplacedPosition_3B9EB7BB;
                        float2 _CrestComputeOceanDataFromDisplacedPosition_3B9EB7BB_UndisplacedPosXZAWS_7;
                        float _CrestComputeOceanDataFromDisplacedPosition_3B9EB7BB_LodAlpha_6;
                        float3 _CrestComputeOceanDataFromDisplacedPosition_3B9EB7BB_Displacement_8;
                        float _CrestComputeOceanDataFromDisplacedPosition_3B9EB7BB_OceanWaterDepth_1;
                        float _CrestComputeOceanDataFromDisplacedPosition_3B9EB7BB_Foam_2;
                        float2 _CrestComputeOceanDataFromDisplacedPosition_3B9EB7BB_Shadow_3;
                        float2 _CrestComputeOceanDataFromDisplacedPosition_3B9EB7BB_Flow_4;
                        float _CrestComputeOceanDataFromDisplacedPosition_3B9EB7BB_SubSurfaceScattering_5;
                        SG_CrestComputeOceanDataFromDisplacedPosition_1f483fe321b10c64e8b93a023ae60dd6(IN.AbsoluteWorldSpacePosition, _CrestComputeOceanDataFromDisplacedPosition_3B9EB7BB, _CrestComputeOceanDataFromDisplacedPosition_3B9EB7BB_UndisplacedPosXZAWS_7, _CrestComputeOceanDataFromDisplacedPosition_3B9EB7BB_LodAlpha_6, _CrestComputeOceanDataFromDisplacedPosition_3B9EB7BB_Displacement_8, _CrestComputeOceanDataFromDisplacedPosition_3B9EB7BB_OceanWaterDepth_1, _CrestComputeOceanDataFromDisplacedPosition_3B9EB7BB_Foam_2, _CrestComputeOceanDataFromDisplacedPosition_3B9EB7BB_Shadow_3, _CrestComputeOceanDataFromDisplacedPosition_3B9EB7BB_Flow_4, _CrestComputeOceanDataFromDisplacedPosition_3B9EB7BB_SubSurfaceScattering_5);
                        #if defined(CREST_GENERATED_SHADER_ON)
                        float2 _GENERATEDSHADER_71C0694B_Out_0 = _Vector2_552A5E1F_Out_0;
                        #else
                        float2 _GENERATEDSHADER_71C0694B_Out_0 = _CrestComputeOceanDataFromDisplacedPosition_3B9EB7BB_UndisplacedPosXZAWS_7;
                        #endif
                        #if defined(CREST_GENERATED_SHADER_ON)
                        float _GENERATEDSHADER_2A933A74_Out_0 = _Split_B10345C8_B_3;
                        #else
                        float _GENERATEDSHADER_2A933A74_Out_0 = _CrestComputeOceanDataFromDisplacedPosition_3B9EB7BB_LodAlpha_6;
                        #endif
                        #if defined(CREST_GENERATED_SHADER_ON)
                        float _GENERATEDSHADER_EFBF6036_Out_0 = _Split_B10345C8_A_4;
                        #else
                        float _GENERATEDSHADER_EFBF6036_Out_0 = _CrestComputeOceanDataFromDisplacedPosition_3B9EB7BB_OceanWaterDepth_1;
                        #endif
                        float4 _UV_39E1D2DF_Out_0 = IN.uv1;
                        float _Split_CB2CA9B8_R_1 = _UV_39E1D2DF_Out_0[0];
                        float _Split_CB2CA9B8_G_2 = _UV_39E1D2DF_Out_0[1];
                        float _Split_CB2CA9B8_B_3 = _UV_39E1D2DF_Out_0[2];
                        float _Split_CB2CA9B8_A_4 = _UV_39E1D2DF_Out_0[3];
                        #if defined(CREST_GENERATED_SHADER_ON)
                        float _GENERATEDSHADER_1BBAE801_Out_0 = _Split_CB2CA9B8_G_2;
                        #else
                        float _GENERATEDSHADER_1BBAE801_Out_0 = _CrestComputeOceanDataFromDisplacedPosition_3B9EB7BB_Foam_2;
                        #endif
                        float4 _UV_33A67BF5_Out_0 = IN.uv2;
                        float _Split_753DFB28_R_1 = _UV_33A67BF5_Out_0[0];
                        float _Split_753DFB28_G_2 = _UV_33A67BF5_Out_0[1];
                        float _Split_753DFB28_B_3 = _UV_33A67BF5_Out_0[2];
                        float _Split_753DFB28_A_4 = _UV_33A67BF5_Out_0[3];
                        float2 _Vector2_7883B8A6_Out_0 = float2(_Split_753DFB28_R_1, _Split_753DFB28_G_2);
                        #if defined(CREST_GENERATED_SHADER_ON)
                        float2 _GENERATEDSHADER_B499BDE6_Out_0 = _Vector2_7883B8A6_Out_0;
                        #else
                        float2 _GENERATEDSHADER_B499BDE6_Out_0 = _CrestComputeOceanDataFromDisplacedPosition_3B9EB7BB_Shadow_3;
                        #endif
                        float2 _Vector2_3A83E1FC_Out_0 = float2(_Split_753DFB28_B_3, _Split_753DFB28_A_4);
                        #if defined(CREST_GENERATED_SHADER_ON)
                        float2 _GENERATEDSHADER_84CB20AD_Out_0 = _Vector2_3A83E1FC_Out_0;
                        #else
                        float2 _GENERATEDSHADER_84CB20AD_Out_0 = _CrestComputeOceanDataFromDisplacedPosition_3B9EB7BB_Flow_4;
                        #endif
                        #if defined(CREST_GENERATED_SHADER_ON)
                        float _GENERATEDSHADER_6BDC98D1_Out_0 = _Split_CB2CA9B8_R_1;
                        #else
                        float _GENERATEDSHADER_6BDC98D1_Out_0 = _CrestComputeOceanDataFromDisplacedPosition_3B9EB7BB_SubSurfaceScattering_5;
                        #endif
                        PositionXZWSUndisp_2 = _GENERATEDSHADER_71C0694B_Out_0;
                        LodAlpha_1 = _GENERATEDSHADER_2A933A74_Out_0;
                        OceanDepth_3 = _GENERATEDSHADER_EFBF6036_Out_0;
                        Foam_4 = _GENERATEDSHADER_1BBAE801_Out_0;
                        Shadow_5 = _GENERATEDSHADER_B499BDE6_Out_0;
                        Flow_6 = _GENERATEDSHADER_84CB20AD_Out_0;
                        SubSurfaceScattering_7 = _GENERATEDSHADER_6BDC98D1_Out_0;
                    }
                
                    // 2a24b14f4becad7f23cf06f270571445
                    #include "ShadergraphFramework/CrestNodeDrivenInputs.hlsl"
                
                    struct Bindings_CrestDrivenData_40572ccadf7d9704a83d283fb4c9f19d
                    {
                    };
                
                    void SG_CrestDrivenData_40572ccadf7d9704a83d283fb4c9f19d(Bindings_CrestDrivenData_40572ccadf7d9704a83d283fb4c9f19d IN, out half MeshScaleAlpha_1, out half LodDataTexelSize_8, out half GeometryGridSize_2, out half3 OceanPosScale0_3, out half3 OceanPosScale1_4, out half4 OceanParams0_5, out half4 OceanParams1_6, out half SliceIndex0_7)
                    {
                        half _CustomFunction_CD9A5F8F_MeshScaleAlpha_9;
                        half _CustomFunction_CD9A5F8F_LodDataTexelSize_10;
                        half _CustomFunction_CD9A5F8F_GeometryGridSize_14;
                        half3 _CustomFunction_CD9A5F8F_OceanPosScale0_7;
                        half3 _CustomFunction_CD9A5F8F_OceanPosScale1_8;
                        half4 _CustomFunction_CD9A5F8F_OceanParams0_11;
                        half4 _CustomFunction_CD9A5F8F_OceanParams1_12;
                        half _CustomFunction_CD9A5F8F_SliceIndex0_13;
                        CrestOceanSurfaceValues_half(_CustomFunction_CD9A5F8F_MeshScaleAlpha_9, _CustomFunction_CD9A5F8F_LodDataTexelSize_10, _CustomFunction_CD9A5F8F_GeometryGridSize_14, _CustomFunction_CD9A5F8F_OceanPosScale0_7, _CustomFunction_CD9A5F8F_OceanPosScale1_8, _CustomFunction_CD9A5F8F_OceanParams0_11, _CustomFunction_CD9A5F8F_OceanParams1_12, _CustomFunction_CD9A5F8F_SliceIndex0_13);
                        MeshScaleAlpha_1 = _CustomFunction_CD9A5F8F_MeshScaleAlpha_9;
                        LodDataTexelSize_8 = _CustomFunction_CD9A5F8F_LodDataTexelSize_10;
                        GeometryGridSize_2 = _CustomFunction_CD9A5F8F_GeometryGridSize_14;
                        OceanPosScale0_3 = _CustomFunction_CD9A5F8F_OceanPosScale0_7;
                        OceanPosScale1_4 = _CustomFunction_CD9A5F8F_OceanPosScale1_8;
                        OceanParams0_5 = _CustomFunction_CD9A5F8F_OceanParams0_11;
                        OceanParams1_6 = _CustomFunction_CD9A5F8F_OceanParams1_12;
                        SliceIndex0_7 = _CustomFunction_CD9A5F8F_SliceIndex0_13;
                    }
                
                    // cb9e19acd0b7f8a1a1c0b9aed89ebacc
                    #include "ShadergraphFramework/CrestNodeSampleClipSurfaceData.hlsl"
                
                    struct Bindings_CrestClipSurface_8c73b4813486448849bd38d01267f186
                    {
                        float3 AbsoluteWorldSpacePosition;
                        half4 uv0;
                        half4 uv1;
                        half4 uv2;
                    };
                
                    void SG_CrestClipSurface_8c73b4813486448849bd38d01267f186(Bindings_CrestClipSurface_8c73b4813486448849bd38d01267f186 IN, out float ClipSurfaceValue_1)
                    {
                        float _Split_A2A81B90_R_1 = IN.AbsoluteWorldSpacePosition[0];
                        float _Split_A2A81B90_G_2 = IN.AbsoluteWorldSpacePosition[1];
                        float _Split_A2A81B90_B_3 = IN.AbsoluteWorldSpacePosition[2];
                        float _Split_A2A81B90_A_4 = 0;
                        float2 _Vector2_5DE2DA1_Out_0 = float2(_Split_A2A81B90_R_1, _Split_A2A81B90_B_3);
                        Bindings_CrestUnpackData_5bc4cf08f88035c489658d60edefeec8 _CrestUnpackData_ECB44FF9;
                        _CrestUnpackData_ECB44FF9.AbsoluteWorldSpacePosition = IN.AbsoluteWorldSpacePosition;
                        _CrestUnpackData_ECB44FF9.uv0 = IN.uv0;
                        _CrestUnpackData_ECB44FF9.uv1 = IN.uv1;
                        _CrestUnpackData_ECB44FF9.uv2 = IN.uv2;
                        float2 _CrestUnpackData_ECB44FF9_PositionXZWSUndisp_2;
                        float _CrestUnpackData_ECB44FF9_LodAlpha_1;
                        float _CrestUnpackData_ECB44FF9_OceanDepth_3;
                        float _CrestUnpackData_ECB44FF9_Foam_4;
                        float2 _CrestUnpackData_ECB44FF9_Shadow_5;
                        float2 _CrestUnpackData_ECB44FF9_Flow_6;
                        float _CrestUnpackData_ECB44FF9_SubSurfaceScattering_7;
                        SG_CrestUnpackData_5bc4cf08f88035c489658d60edefeec8(_CrestUnpackData_ECB44FF9, _CrestUnpackData_ECB44FF9_PositionXZWSUndisp_2, _CrestUnpackData_ECB44FF9_LodAlpha_1, _CrestUnpackData_ECB44FF9_OceanDepth_3, _CrestUnpackData_ECB44FF9_Foam_4, _CrestUnpackData_ECB44FF9_Shadow_5, _CrestUnpackData_ECB44FF9_Flow_6, _CrestUnpackData_ECB44FF9_SubSurfaceScattering_7);
                        Bindings_CrestDrivenData_40572ccadf7d9704a83d283fb4c9f19d _CrestDrivenData_EA2C4302;
                        half _CrestDrivenData_EA2C4302_MeshScaleAlpha_1;
                        half _CrestDrivenData_EA2C4302_LodDataTexelSize_8;
                        half _CrestDrivenData_EA2C4302_GeometryGridSize_2;
                        half3 _CrestDrivenData_EA2C4302_OceanPosScale0_3;
                        half3 _CrestDrivenData_EA2C4302_OceanPosScale1_4;
                        half4 _CrestDrivenData_EA2C4302_OceanParams0_5;
                        half4 _CrestDrivenData_EA2C4302_OceanParams1_6;
                        half _CrestDrivenData_EA2C4302_SliceIndex0_7;
                        SG_CrestDrivenData_40572ccadf7d9704a83d283fb4c9f19d(_CrestDrivenData_EA2C4302, _CrestDrivenData_EA2C4302_MeshScaleAlpha_1, _CrestDrivenData_EA2C4302_LodDataTexelSize_8, _CrestDrivenData_EA2C4302_GeometryGridSize_2, _CrestDrivenData_EA2C4302_OceanPosScale0_3, _CrestDrivenData_EA2C4302_OceanPosScale1_4, _CrestDrivenData_EA2C4302_OceanParams0_5, _CrestDrivenData_EA2C4302_OceanParams1_6, _CrestDrivenData_EA2C4302_SliceIndex0_7);
                        float _CustomFunction_26C15E74_ClipSurfaceValue_7;
                        CrestNodeSampleClipSurfaceData_float(_Vector2_5DE2DA1_Out_0, _CrestUnpackData_ECB44FF9_LodAlpha_1, _CrestDrivenData_EA2C4302_OceanPosScale0_3, _CrestDrivenData_EA2C4302_OceanPosScale1_4, _CrestDrivenData_EA2C4302_OceanParams0_5, _CrestDrivenData_EA2C4302_OceanParams1_6, _CrestDrivenData_EA2C4302_SliceIndex0_7, _CustomFunction_26C15E74_ClipSurfaceValue_7);
                        ClipSurfaceValue_1 = _CustomFunction_26C15E74_ClipSurfaceValue_7;
                    }
                
                    #include "ShadergraphFramework/CrestNodeGeoMorph.hlsl"
                
                    struct Bindings_CrestGeoMorph_9ab91ec3462438049923ca0ff16f68ad
                    {
                    };
                
                    void SG_CrestGeoMorph_9ab91ec3462438049923ca0ff16f68ad(float3 Vector3_28A0F264, float3 Vector3_F1111B56, float Vector1_691AFD6A, float Vector1_37DEE8F3, Bindings_CrestGeoMorph_9ab91ec3462438049923ca0ff16f68ad IN, out half3 MorphedPositionWS_1, out half LodAlpha_2)
                    {
                        float3 _Property_C4B6B1D5_Out_0 = Vector3_28A0F264;
                        float3 _Property_13BC6D1A_Out_0 = Vector3_F1111B56;
                        float _Property_DE4BC103_Out_0 = Vector1_691AFD6A;
                        float _Property_B3D2A4DF_Out_0 = Vector1_37DEE8F3;
                        half3 _CustomFunction_C8F1D6C4_MorphedPositionWS_4;
                        half _CustomFunction_C8F1D6C4_LodAlpha_5;
                        GeoMorph_half(_Property_C4B6B1D5_Out_0, _Property_13BC6D1A_Out_0, _Property_DE4BC103_Out_0, _Property_B3D2A4DF_Out_0, _CustomFunction_C8F1D6C4_MorphedPositionWS_4, _CustomFunction_C8F1D6C4_LodAlpha_5);
                        MorphedPositionWS_1 = _CustomFunction_C8F1D6C4_MorphedPositionWS_4;
                        LodAlpha_2 = _CustomFunction_C8F1D6C4_LodAlpha_5;
                    }
                
                    void Unity_Add_float3(float3 A, float3 B, out float3 Out)
                    {
                        Out = A + B;
                    }
                
                // Vertex Graph Evaluation
                    VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
                    {
                        VertexDescription description = (VertexDescription)0;
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        Bindings_CrestDrivenData_40572ccadf7d9704a83d283fb4c9f19d _CrestDrivenData_A1078169;
                        half _CrestDrivenData_A1078169_MeshScaleAlpha_1;
                        half _CrestDrivenData_A1078169_LodDataTexelSize_8;
                        half _CrestDrivenData_A1078169_GeometryGridSize_2;
                        half3 _CrestDrivenData_A1078169_OceanPosScale0_3;
                        half3 _CrestDrivenData_A1078169_OceanPosScale1_4;
                        half4 _CrestDrivenData_A1078169_OceanParams0_5;
                        half4 _CrestDrivenData_A1078169_OceanParams1_6;
                        half _CrestDrivenData_A1078169_SliceIndex0_7;
                        SG_CrestDrivenData_40572ccadf7d9704a83d283fb4c9f19d(_CrestDrivenData_A1078169, _CrestDrivenData_A1078169_MeshScaleAlpha_1, _CrestDrivenData_A1078169_LodDataTexelSize_8, _CrestDrivenData_A1078169_GeometryGridSize_2, _CrestDrivenData_A1078169_OceanPosScale0_3, _CrestDrivenData_A1078169_OceanPosScale1_4, _CrestDrivenData_A1078169_OceanParams0_5, _CrestDrivenData_A1078169_OceanParams1_6, _CrestDrivenData_A1078169_SliceIndex0_7);
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        Bindings_CrestGeoMorph_9ab91ec3462438049923ca0ff16f68ad _CrestGeoMorph_8F1A4FF1;
                        half3 _CrestGeoMorph_8F1A4FF1_MorphedPositionWS_1;
                        half _CrestGeoMorph_8F1A4FF1_LodAlpha_2;
                        SG_CrestGeoMorph_9ab91ec3462438049923ca0ff16f68ad(IN.AbsoluteWorldSpacePosition, _CrestDrivenData_A1078169_OceanPosScale0_3, _CrestDrivenData_A1078169_MeshScaleAlpha_1, _CrestDrivenData_A1078169_GeometryGridSize_2, _CrestGeoMorph_8F1A4FF1, _CrestGeoMorph_8F1A4FF1_MorphedPositionWS_1, _CrestGeoMorph_8F1A4FF1_LodAlpha_2);
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        float _Split_CC063A43_R_1 = _CrestGeoMorph_8F1A4FF1_MorphedPositionWS_1[0];
                        float _Split_CC063A43_G_2 = _CrestGeoMorph_8F1A4FF1_MorphedPositionWS_1[1];
                        float _Split_CC063A43_B_3 = _CrestGeoMorph_8F1A4FF1_MorphedPositionWS_1[2];
                        float _Split_CC063A43_A_4 = 0;
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        float2 _Vector2_2D46FD43_Out_0 = float2(_Split_CC063A43_R_1, _Split_CC063A43_B_3);
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        Bindings_CrestSampleOceanData_f19d6d2aa60f8294e870f100303ee5a4 _CrestSampleOceanData_340A6610;
                        float3 _CrestSampleOceanData_340A6610_Displacement_1;
                        float _CrestSampleOceanData_340A6610_OceanWaterDepth_5;
                        float _CrestSampleOceanData_340A6610_Foam_6;
                        float2 _CrestSampleOceanData_340A6610_Shadow_7;
                        float2 _CrestSampleOceanData_340A6610_Flow_8;
                        float _CrestSampleOceanData_340A6610_SubSurfaceScattering_9;
                        SG_CrestSampleOceanData_f19d6d2aa60f8294e870f100303ee5a4(_Vector2_2D46FD43_Out_0, _CrestGeoMorph_8F1A4FF1_LodAlpha_2, _CrestDrivenData_A1078169_OceanPosScale0_3, _CrestDrivenData_A1078169_OceanPosScale1_4, _CrestDrivenData_A1078169_OceanParams0_5, _CrestDrivenData_A1078169_OceanParams1_6, _CrestDrivenData_A1078169_SliceIndex0_7, _CrestSampleOceanData_340A6610, _CrestSampleOceanData_340A6610_Displacement_1, _CrestSampleOceanData_340A6610_OceanWaterDepth_5, _CrestSampleOceanData_340A6610_Foam_6, _CrestSampleOceanData_340A6610_Shadow_7, _CrestSampleOceanData_340A6610_Flow_8, _CrestSampleOceanData_340A6610_SubSurfaceScattering_9);
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        float3 _Add_2D79A354_Out_2;
                        Unity_Add_float3(_CrestGeoMorph_8F1A4FF1_MorphedPositionWS_1, _CrestSampleOceanData_340A6610_Displacement_1, _Add_2D79A354_Out_2);
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        float3 _Transform_9C2ACF28_Out_1 = TransformWorldToObject(GetCameraRelativePositionWS(_Add_2D79A354_Out_2.xyz));
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        float3 _Transform_86205EE4_Out_1 = TransformWorldToObjectDir(float3 (0, 1, 0).xyz);
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        float3 _Transform_22BD1A85_Out_1 = TransformWorldToObjectDir(float3 (1, 0, 0).xyz);
                        #endif
                        description.VertexPosition = _Transform_9C2ACF28_Out_1;
                        description.VertexNormal = _Transform_86205EE4_Out_1;
                        description.VertexTangent = _Transform_22BD1A85_Out_1;
                        return description;
                    }
                    
                // Pixel Graph Evaluation
                    SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
                    {
                        SurfaceDescription surface = (SurfaceDescription)0;
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        float _SceneDepth_E2A24470_Out_1;
                        Unity_SceneDepth_Eye_float(float4(IN.ScreenPosition.xy / IN.ScreenPosition.w, 0, 0), _SceneDepth_E2A24470_Out_1);
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        float _Split_68C2565_R_1 = IN.ViewSpacePosition[0];
                        float _Split_68C2565_G_2 = IN.ViewSpacePosition[1];
                        float _Split_68C2565_B_3 = IN.ViewSpacePosition[2];
                        float _Split_68C2565_A_4 = 0;
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        float _Negate_847D9376_Out_1;
                        Unity_Negate_float(_Split_68C2565_B_3, _Negate_847D9376_Out_1);
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        float _Subtract_B338E0_Out_2;
                        Unity_Subtract_float(_SceneDepth_E2A24470_Out_1, _Negate_847D9376_Out_1, _Subtract_B338E0_Out_2);
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        float _Remap_6CD3C222_Out_3;
                        Unity_Remap_float(_Subtract_B338E0_Out_2, float2 (0, 0.2), float2 (0, 1), _Remap_6CD3C222_Out_3);
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        float _Saturate_FEF634C4_Out_1;
                        Unity_Saturate_float(_Remap_6CD3C222_Out_3, _Saturate_FEF634C4_Out_1);
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        Bindings_CrestClipSurface_8c73b4813486448849bd38d01267f186 _CrestClipSurface_AA3EF9C5;
                        _CrestClipSurface_AA3EF9C5.AbsoluteWorldSpacePosition = IN.AbsoluteWorldSpacePosition;
                        _CrestClipSurface_AA3EF9C5.uv0 = IN.uv0;
                        _CrestClipSurface_AA3EF9C5.uv1 = IN.uv1;
                        _CrestClipSurface_AA3EF9C5.uv2 = IN.uv2;
                        float _CrestClipSurface_AA3EF9C5_ClipSurfaceValue_1;
                        SG_CrestClipSurface_8c73b4813486448849bd38d01267f186(_CrestClipSurface_AA3EF9C5, _CrestClipSurface_AA3EF9C5_ClipSurfaceValue_1);
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        float _Subtract_900E903A_Out_2;
                        Unity_Subtract_float(_Saturate_FEF634C4_Out_1, _CrestClipSurface_AA3EF9C5_ClipSurfaceValue_1, _Subtract_900E903A_Out_2);
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        float _Saturate_A1AFAC8_Out_1;
                        Unity_Saturate_float(_Subtract_900E903A_Out_2, _Saturate_A1AFAC8_Out_1);
                        #endif
                        surface.Alpha = _Saturate_A1AFAC8_Out_1;
                        surface.AlphaClipThreshold = 0;
                        return surface;
                    }
                    
            //-------------------------------------------------------------------------------------
            // End graph generated code
            //-------------------------------------------------------------------------------------
        
        //-------------------------------------------------------------------------------------
            // TEMPLATE INCLUDE : VertexAnimation.template.hlsl
            //-------------------------------------------------------------------------------------
            
            
            VertexDescriptionInputs AttributesMeshToVertexDescriptionInputs(AttributesMesh input)
            {
                VertexDescriptionInputs output;
                ZERO_INITIALIZE(VertexDescriptionInputs, output);
            
            
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
            output.WorldSpaceNormal =            TransformObjectToWorldNormal(input.normalOS);
            #endif
            
            
            
            
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
            output.WorldSpaceTangent =           TransformObjectToWorldDir(input.tangentOS.xyz);
            #endif
            
            
            
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
            output.ObjectSpaceBiTangent =        normalize(cross(input.normalOS.xyz, input.tangentOS.xyz) * (input.tangentOS.w > 0.0f ? 1.0f : -1.0f) * GetOddNegativeScale());
            #endif
            
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
            output.WorldSpaceBiTangent =         TransformObjectToWorldDir(output.ObjectSpaceBiTangent);
            #endif
            
            
            
            
            
            
            
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
            output.AbsoluteWorldSpacePosition =  GetAbsolutePositionWS(TransformObjectToWorld(input.positionOS));
            #endif
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
                return output;
            }
            
            AttributesMesh ApplyMeshModification(AttributesMesh input, float3 timeParameters)
            {
                // build graph inputs
                VertexDescriptionInputs vertexDescriptionInputs = AttributesMeshToVertexDescriptionInputs(input);
                // Override time paramters with used one (This is required to correctly handle motion vector for vertex animation based on time)
            
            
                // evaluate vertex graph
                VertexDescription vertexDescription = VertexDescriptionFunction(vertexDescriptionInputs);
            
                // copy graph output to the results
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
            input.positionOS = vertexDescription.VertexPosition;
            #endif
            
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
            input.normalOS = vertexDescription.VertexNormal;
            #endif
            
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
            input.tangentOS.xyz = vertexDescription.VertexTangent;
            #endif
            
            
                return input;
            }
            
            //-------------------------------------------------------------------------------------
            // END TEMPLATE INCLUDE : VertexAnimation.template.hlsl
            //-------------------------------------------------------------------------------------
            
        
        //-------------------------------------------------------------------------------------
            // TEMPLATE INCLUDE : SharedCode.template.hlsl
            //-------------------------------------------------------------------------------------
            
            #if !defined(SHADER_STAGE_RAY_TRACING)
                FragInputs BuildFragInputs(VaryingsMeshToPS input)
                {
                    FragInputs output;
                    ZERO_INITIALIZE(FragInputs, output);
            
                    // Init to some default value to make the computer quiet (else it output 'divide by zero' warning even if value is not used).
                    // TODO: this is a really poor workaround, but the variable is used in a bunch of places
                    // to compute normals which are then passed on elsewhere to compute other values...
                    output.tangentToWorld = k_identity3x3;
                    output.positionSS = input.positionCS;       // input.positionCS is SV_Position
            
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
            output.positionRWS = input.positionRWS;
            #endif
            
            
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
            output.texCoord0 = input.texCoord0;
            #endif
            
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
            output.texCoord1 = input.texCoord1;
            #endif
            
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
            output.texCoord2 = input.texCoord2;
            #endif
            
            
            
                    #if _DOUBLESIDED_ON && SHADER_STAGE_FRAGMENT
                    output.isFrontFace = IS_FRONT_VFACE(input.cullFace, true, false);
                    #elif SHADER_STAGE_FRAGMENT
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
            output.isFrontFace = IS_FRONT_VFACE(input.cullFace, true, false);
            #endif
            
                    #endif // SHADER_STAGE_FRAGMENT
            
                    return output;
                }
            #endif
                SurfaceDescriptionInputs FragInputsToSurfaceDescriptionInputs(FragInputs input, float3 viewWS)
                {
                    SurfaceDescriptionInputs output;
                    ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
            output.WorldSpacePosition =          input.positionRWS;
            #endif
            
            
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
            output.ViewSpacePosition =           TransformWorldToView(input.positionRWS);
            #endif
            
            
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
            output.AbsoluteWorldSpacePosition =  GetAbsolutePositionWS(input.positionRWS);
            #endif
            
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
            output.ScreenPosition =              ComputeScreenPos(TransformWorldToHClip(input.positionRWS), _ProjectionParams.x);
            #endif
            
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
            output.uv0 =                         input.texCoord0;
            #endif
            
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
            output.uv1 =                         input.texCoord1;
            #endif
            
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
            output.uv2 =                         input.texCoord2;
            #endif
            
            
            
            
            
            
                    return output;
                }
            
            #if !defined(SHADER_STAGE_RAY_TRACING)
            
                // existing HDRP code uses the combined function to go directly from packed to frag inputs
                FragInputs UnpackVaryingsMeshToFragInputs(PackedVaryingsMeshToPS input)
                {
                    UNITY_SETUP_INSTANCE_ID(input);
                    VaryingsMeshToPS unpacked= UnpackVaryingsMeshToPS(input);
                    return BuildFragInputs(unpacked);
                }
            #endif
            
            //-------------------------------------------------------------------------------------
            // END TEMPLATE INCLUDE : SharedCode.template.hlsl
            //-------------------------------------------------------------------------------------
            
        
        
            void BuildSurfaceData(FragInputs fragInputs, inout SurfaceDescription surfaceDescription, float3 V, PositionInputs posInput, out SurfaceData surfaceData)
            {
                // setup defaults -- these are used if the graph doesn't output a value
                ZERO_INITIALIZE(SurfaceData, surfaceData);
                surfaceData.ambientOcclusion = 1.0;
                surfaceData.specularOcclusion = 1.0; // This need to be init here to quiet the compiler in case of decal, but can be override later.
        
                // copy across graph values, if defined
        
        
        
        
        
        
                // These static material feature allow compile time optimization
                surfaceData.materialFeatures = MATERIALFEATUREFLAGS_LIT_STANDARD;
        #ifdef _MATERIAL_FEATURE_SPECULAR_COLOR
                surfaceData.materialFeatures |= MATERIALFEATUREFLAGS_LIT_SPECULAR_COLOR;
        #endif
        
                float3 doubleSidedConstants = float3(1.0, 1.0, 1.0);
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
        doubleSidedConstants = float3( 1.0,  1.0, -1.0);
        #endif
        
        
                // normal delivered to master node
                float3 normalSrc = float3(0.0f, 0.0f, 1.0f);
        
        
                // compute world space normal
        #if _NORMAL_DROPOFF_TS
                GetNormalWS(fragInputs, normalSrc, surfaceData.normalWS, doubleSidedConstants);
        #elif _NORMAL_DROPOFF_OS
        		surfaceData.normalWS = TransformObjectToWorldNormal(normalSrc);
        #elif _NORMAL_DROPOFF_WS
        		surfaceData.normalWS = normalSrc;
        #endif
        
                surfaceData.geomNormalWS = fragInputs.tangentToWorld[2];
                surfaceData.tangentWS = normalize(fragInputs.tangentToWorld[0].xyz);    // The tangent is not normalize in tangentToWorld for mikkt. TODO: Check if it expected that we normalize with Morten. Tag: SURFACE_GRADIENT
        
        #if HAVE_DECALS
                if (_EnableDecals)
                {
                    // Both uses and modifies 'surfaceData.normalWS'.
                    DecalSurfaceData decalSurfaceData = GetDecalSurfaceData(posInput, surfaceDescription.Alpha);
                    ApplyDecalToSurfaceData(decalSurfaceData, surfaceData);
                }
        #endif
        
                surfaceData.tangentWS = Orthonormalize(surfaceData.tangentWS, surfaceData.normalWS);
        
                // By default we use the ambient occlusion with Tri-ace trick (apply outside) for specular occlusion as PBR master node don't have any option
                surfaceData.specularOcclusion = GetSpecularOcclusionFromAmbientOcclusion(ClampNdotV(dot(surfaceData.normalWS, V)), surfaceData.ambientOcclusion, PerceptualSmoothnessToRoughness(surfaceData.perceptualSmoothness));
        
        #ifdef DEBUG_DISPLAY
                if (_DebugMipMapMode != DEBUGMIPMAPMODE_NONE)
                {
                    // TODO: need to update mip info
                    surfaceData.metallic = 0;
                }
        
                // We need to call ApplyDebugToSurfaceData after filling the surfarcedata and before filling builtinData
                // as it can modify attribute use for static lighting
                ApplyDebugToSurfaceData(fragInputs.tangentToWorld, surfaceData);
        #endif
            }
        
            void GetSurfaceAndBuiltinData(FragInputs fragInputs, float3 V, inout PositionInputs posInput, out SurfaceData surfaceData, out BuiltinData builtinData)
            {
        #ifdef LOD_FADE_CROSSFADE // enable dithering LOD transition if user select CrossFade transition in LOD group
                LODDitheringTransition(ComputeFadeMaskSeed(V, posInput.positionSS), unity_LODFade.x);
        #endif
        
                float3 doubleSidedConstants = float3(1.0, 1.0, 1.0);
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
        doubleSidedConstants = float3( 1.0,  1.0, -1.0);
        #endif
        
        
                ApplyDoubleSidedFlipOrMirror(fragInputs, doubleSidedConstants);
        
                SurfaceDescriptionInputs surfaceDescriptionInputs = FragInputsToSurfaceDescriptionInputs(fragInputs, V);
                SurfaceDescription surfaceDescription = SurfaceDescriptionFunction(surfaceDescriptionInputs);
        
                // Perform alpha test very early to save performance (a killed pixel will not sample textures)
                // TODO: split graph evaluation to grab just alpha dependencies first? tricky..
        
        
                BuildSurfaceData(fragInputs, surfaceDescription, V, posInput, surfaceData);
        
                // Builtin Data
                // For back lighting we use the oposite vertex normal
                InitBuiltinData(posInput, surfaceDescription.Alpha, surfaceData.normalWS, -fragInputs.tangentToWorld[2], fragInputs.texCoord1, fragInputs.texCoord2, builtinData);
        
        
        
                PostInitBuiltinData(V, posInput, surfaceData, builtinData);
            }
        
            //-------------------------------------------------------------------------------------
            // Pass Includes
            //-------------------------------------------------------------------------------------
                #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl"
            //-------------------------------------------------------------------------------------
            // End Pass Includes
            //-------------------------------------------------------------------------------------
        
            ENDHLSL
        }
        
        Pass
        {
            // based on HDPBRPass.template
            Name "Forward"
            Tags { "LightMode" = "Forward" }
        
            //-------------------------------------------------------------------------------------
            // Render Modes (Blend, Cull, ZTest, Stencil, etc)
            //-------------------------------------------------------------------------------------
            Blend One OneMinusSrcAlpha, One OneMinusSrcAlpha
        
			// Culling user defined - can be turned off for underwater
			Cull[_CullMode]

            
            
            
            // Stencil setup
        Stencil
        {
           WriteMask 6
           Ref  0
           Comp Always
           Pass Replace
        }
        
            
            //-------------------------------------------------------------------------------------
            // End Render Modes
            //-------------------------------------------------------------------------------------
        
            HLSLPROGRAM
        
            #pragma target 4.5
            #pragma only_renderers d3d11 ps4 xboxone vulkan metal switch
            //#pragma enable_d3d11_debug_symbols
        
            #pragma multi_compile_instancing
        #pragma instancing_options renderinglayer
        
            #pragma multi_compile _ LOD_FADE_CROSSFADE
        
            //-------------------------------------------------------------------------------------
            // Graph Defines
            //-------------------------------------------------------------------------------------
                    #define SHADERPASS SHADERPASS_FORWARD
                #pragma only_renderers d3d11 ps4 xboxone vulkan metal switch
                #pragma multi_compile _ DEBUG_DISPLAY
                #pragma multi_compile _ LIGHTMAP_ON
                #pragma multi_compile _ DIRLIGHTMAP_COMBINED
                #pragma multi_compile _ DYNAMICLIGHTMAP_ON
                #pragma multi_compile _ SHADOWS_SHADOWMASK
                #pragma multi_compile DECALS_OFF DECALS_3RT DECALS_4RT
                #define USE_CLUSTERED_LIGHTLIST
                #pragma multi_compile SHADOW_LOW SHADOW_MEDIUM SHADOW_HIGH
                #define RAYTRACING_SHADER_GRAPH_HIGH
		        // HB: Seems to require this to be forced on
                //#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                #define REQUIRE_DEPTH_TEXTURE
                //#endif
		        // HB: This needed to be forced on as well as _SURFACE_TYPE_TRANSPARENT to get refraction working
                //#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                #define REQUIRE_OPAQUE_TEXTURE
                //#endif
                // ACTIVE FIELDS:
                //   DoubleSided
                //   DoubleSided.Mirror
                //   FragInputs.isFrontFace
                //   Material.SpecularColor
                //   features.NormalDropOffTS
                //   SurfaceType.Transparent
                //   BlendMode.Alpha
                //   AlphaFog
                //   SurfaceDescription.Albedo
                //   SurfaceDescription.Normal
                //   SurfaceDescription.Specular
                //   SurfaceDescription.Emission
                //   SurfaceDescription.Smoothness
                //   SurfaceDescription.Occlusion
                //   SurfaceDescription.Alpha
                //   SurfaceDescription.AlphaClipThreshold
                //   features.modifyMesh
                //   VertexDescription.VertexPosition
                //   VertexDescription.VertexNormal
                //   VertexDescription.VertexTangent
                //   FragInputs.tangentToWorld
                //   FragInputs.positionRWS
                //   FragInputs.texCoord1
                //   FragInputs.texCoord2
                //   VaryingsMeshToPS.cullFace
                //   VaryingsMeshToPS.tangentWS
                //   VaryingsMeshToPS.normalWS
                //   VaryingsMeshToPS.positionRWS
                //   VaryingsMeshToPS.texCoord1
                //   VaryingsMeshToPS.texCoord2
                //   AttributesMesh.tangentOS
                //   AttributesMesh.normalOS
                //   AttributesMesh.positionOS
                //   AttributesMesh.uv1
                //   AttributesMesh.uv2
                // Shared Graph Keywords
                    #pragma shader_feature_local _ CREST_FOAM_ON
                    #pragma shader_feature_local _ CREST_CAUSTICS_ON
                    #pragma shader_feature_local _ CREST_FLOW_ON
                
                    #if defined(CREST_FOAM_ON) && defined(CREST_CAUSTICS_ON) && defined(CREST_FLOW_ON)
                        #define KEYWORD_PERMUTATION_0
                    #elif defined(CREST_FOAM_ON) && defined(CREST_CAUSTICS_ON)
                        #define KEYWORD_PERMUTATION_1
                    #elif defined(CREST_FOAM_ON) && defined(CREST_FLOW_ON)
                        #define KEYWORD_PERMUTATION_2
                    #elif defined(CREST_FOAM_ON)
                        #define KEYWORD_PERMUTATION_3
                    #elif defined(CREST_CAUSTICS_ON) && defined(CREST_FLOW_ON)
                        #define KEYWORD_PERMUTATION_4
                    #elif defined(CREST_CAUSTICS_ON)
                        #define KEYWORD_PERMUTATION_5
                    #elif defined(CREST_FLOW_ON)
                        #define KEYWORD_PERMUTATION_6
                    #else
                        #define KEYWORD_PERMUTATION_7
                    #endif
                
                
            //-------------------------------------------------------------------------------------
            // End Defines
            //-------------------------------------------------------------------------------------
        
            //-------------------------------------------------------------------------------------
            // Variant Definitions (active field translations to HDRP defines)
            //-------------------------------------------------------------------------------------

        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
        #define _MATERIAL_FEATURE_SPECULAR_COLOR 1
        #endif
        // HB: This needed to be forced on as well as REQUIRE_OPAQUE_TEXTURE to get refraction to work..
        //#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
        #define _SURFACE_TYPE_TRANSPARENT 1
        //#endif

	    // HB: This needed to be forced on and _ENABLE_FOG_ON_TRANSPARENT added below to enable fogging
        //#if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
        #define _BLENDMODE_ALPHA 1
        //#endif
        
		// HB: This needed to be added and to _BLENDMODE_ALPHA forced on above to enable fogging
        #define _ENABLE_FOG_ON_TRANSPARENT 1
        
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
        #define _DOUBLESIDED_ON 1
        #endif
        
            #define _NORMAL_DROPOFF_TS	1
            // #define _NORMAL_DROPOFF_OS	1
            // #define _NORMAL_DROPOFF_WS	1
        
			#define CREST_GENERATED_SHADER_ON 1

            //-------------------------------------------------------------------------------------
            // End Variant Definitions
            //-------------------------------------------------------------------------------------
        
            #pragma vertex Vert
            #pragma fragment Frag
        
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/NormalSurfaceGradient.hlsl"
        
            // define FragInputs structure
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl"
        
            //-------------------------------------------------------------------------------------
            // Active Field Defines
            //-------------------------------------------------------------------------------------
        
            // this translates the new dependency tracker into the old preprocessor definitions for the existing HDRP shader code
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
        #define ATTRIBUTES_NEED_NORMAL
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
        #define ATTRIBUTES_NEED_TANGENT
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
        #define ATTRIBUTES_NEED_TEXCOORD0
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
        #define ATTRIBUTES_NEED_TEXCOORD1
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
        #define ATTRIBUTES_NEED_TEXCOORD2
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
        #define ATTRIBUTES_NEED_TEXCOORD3
        #endif
        
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
        #define VARYINGS_NEED_POSITION_WS
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
        #define VARYINGS_NEED_TANGENT_TO_WORLD
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
        #define VARYINGS_NEED_TEXCOORD0
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
        #define VARYINGS_NEED_TEXCOORD1
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
        #define VARYINGS_NEED_TEXCOORD2
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
        #define VARYINGS_NEED_TEXCOORD3
        #endif
        
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
        #define VARYINGS_NEED_CULLFACE
        #endif
        
            #define HAVE_MESH_MODIFICATION
        
            //-------------------------------------------------------------------------------------
            // End Defines
            //-------------------------------------------------------------------------------------
        	
        
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #ifdef DEBUG_DISPLAY
                #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Debug/DebugDisplay.hlsl"
            #endif
        
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl"
        
        #if (SHADERPASS == SHADERPASS_FORWARD)
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/Lighting.hlsl"
        
            #define HAS_LIGHTLOOP
        
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoopDef.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/Lit.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoop.hlsl"
        #else
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/Lit.hlsl"
        #endif
        
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalUtilities.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/LitDecalData.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl"
        
            //Used by SceneSelectionPass
            int _ObjectId;
            int _PassValue;
        
            //-------------------------------------------------------------------------------------
            // Interpolator Packing And Struct Declarations
            //-------------------------------------------------------------------------------------
            // Generated Type: AttributesMesh
            struct AttributesMesh
            {
                #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                float3 positionOS : POSITION;
                #endif // Shader Graph Keywords
                #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                float3 normalOS : NORMAL; // optional
                #endif // Shader Graph Keywords
                #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                float4 tangentOS : TANGENT; // optional
                #endif // Shader Graph Keywords
                #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                float4 uv0 : TEXCOORD0; // optional
                #endif // Shader Graph Keywords
                #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                float4 uv1 : TEXCOORD1; // optional
                #endif // Shader Graph Keywords
                #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                float4 uv2 : TEXCOORD2; // optional
                #endif // Shader Graph Keywords
                #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                float4 uv3 : TEXCOORD3; // optional
                #endif // Shader Graph Keywords
                #if UNITY_ANY_INSTANCING_ENABLED
                #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                uint instanceID : INSTANCEID_SEMANTIC;
                #endif // Shader Graph Keywords
                #endif // UNITY_ANY_INSTANCING_ENABLED
            };
            // Generated Type: VaryingsMeshToPS
            struct VaryingsMeshToPS
            {
                #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                float4 positionCS : SV_POSITION;
                #endif // Shader Graph Keywords
                #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                float3 positionRWS; // optional
                #endif // Shader Graph Keywords
                #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                float3 normalWS; // optional
                #endif // Shader Graph Keywords
                #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                float4 tangentWS; // optional
                #endif // Shader Graph Keywords
                #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                float4 texCoord0; // optional
                #endif // Shader Graph Keywords
                #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                float4 texCoord1; // optional
                #endif // Shader Graph Keywords
                #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                float4 texCoord2; // optional
                #endif // Shader Graph Keywords
                #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                float4 texCoord3; // optional
                #endif // Shader Graph Keywords
                #if UNITY_ANY_INSTANCING_ENABLED
                #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                uint instanceID : CUSTOM_INSTANCE_ID;
                #endif // Shader Graph Keywords
                #endif // UNITY_ANY_INSTANCING_ENABLED
                #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
                #endif // Shader Graph Keywords
                #endif // defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            };
            
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
            // Generated Type: PackedVaryingsMeshToPS
            struct PackedVaryingsMeshToPS
            {
                float4 positionCS : SV_POSITION; // unpacked
                #if UNITY_ANY_INSTANCING_ENABLED
                uint instanceID : CUSTOM_INSTANCE_ID; // unpacked
                #endif // conditional
                float3 interp00 : TEXCOORD0; // auto-packed
                float3 interp01 : TEXCOORD1; // auto-packed
                float4 interp02 : TEXCOORD2; // auto-packed
                float4 interp03 : TEXCOORD3; // auto-packed
                float4 interp04 : TEXCOORD4; // auto-packed
                float4 interp05 : TEXCOORD5; // auto-packed
                float4 interp06 : TEXCOORD6; // auto-packed
                #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC; // unpacked
                #endif // conditional
            };
            
            // Packed Type: VaryingsMeshToPS
            PackedVaryingsMeshToPS PackVaryingsMeshToPS(VaryingsMeshToPS input)
            {
                PackedVaryingsMeshToPS output = (PackedVaryingsMeshToPS)0;
                output.positionCS = input.positionCS;
                output.interp00.xyz = input.positionRWS;
                output.interp01.xyz = input.normalWS;
                output.interp02.xyzw = input.tangentWS;
                output.interp03.xyzw = input.texCoord0;
                output.interp04.xyzw = input.texCoord1;
                output.interp05.xyzw = input.texCoord2;
                output.interp06.xyzw = input.texCoord3;
                #if UNITY_ANY_INSTANCING_ENABLED
                output.instanceID = input.instanceID;
                #endif // conditional
                #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                output.cullFace = input.cullFace;
                #endif // conditional
                return output;
            }
            
            // Unpacked Type: VaryingsMeshToPS
            VaryingsMeshToPS UnpackVaryingsMeshToPS(PackedVaryingsMeshToPS input)
            {
                VaryingsMeshToPS output = (VaryingsMeshToPS)0;
                output.positionCS = input.positionCS;
                output.positionRWS = input.interp00.xyz;
                output.normalWS = input.interp01.xyz;
                output.tangentWS = input.interp02.xyzw;
                output.texCoord0 = input.interp03.xyzw;
                output.texCoord1 = input.interp04.xyzw;
                output.texCoord2 = input.interp05.xyzw;
                output.texCoord3 = input.interp06.xyzw;
                #if UNITY_ANY_INSTANCING_ENABLED
                output.instanceID = input.instanceID;
                #endif // conditional
                #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                output.cullFace = input.cullFace;
                #endif // conditional
                return output;
            }
            #endif
            // Generated Type: VaryingsMeshToDS
            struct VaryingsMeshToDS
            {
                #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                float3 positionRWS;
                #endif // Shader Graph Keywords
                #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                float3 normalWS;
                #endif // Shader Graph Keywords
                #if UNITY_ANY_INSTANCING_ENABLED
                #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                uint instanceID : CUSTOM_INSTANCE_ID;
                #endif // Shader Graph Keywords
                #endif // UNITY_ANY_INSTANCING_ENABLED
            };
            
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
            // Generated Type: PackedVaryingsMeshToDS
            struct PackedVaryingsMeshToDS
            {
                #if UNITY_ANY_INSTANCING_ENABLED
                uint instanceID : CUSTOM_INSTANCE_ID; // unpacked
                #endif // conditional
                float3 interp00 : TEXCOORD0; // auto-packed
                float3 interp01 : TEXCOORD1; // auto-packed
            };
            
            // Packed Type: VaryingsMeshToDS
            PackedVaryingsMeshToDS PackVaryingsMeshToDS(VaryingsMeshToDS input)
            {
                PackedVaryingsMeshToDS output = (PackedVaryingsMeshToDS)0;
                output.interp00.xyz = input.positionRWS;
                output.interp01.xyz = input.normalWS;
                #if UNITY_ANY_INSTANCING_ENABLED
                output.instanceID = input.instanceID;
                #endif // conditional
                return output;
            }
            
            // Unpacked Type: VaryingsMeshToDS
            VaryingsMeshToDS UnpackVaryingsMeshToDS(PackedVaryingsMeshToDS input)
            {
                VaryingsMeshToDS output = (VaryingsMeshToDS)0;
                output.positionRWS = input.interp00.xyz;
                output.normalWS = input.interp01.xyz;
                #if UNITY_ANY_INSTANCING_ENABLED
                output.instanceID = input.instanceID;
                #endif // conditional
                return output;
            }
            #endif
            //-------------------------------------------------------------------------------------
            // End Interpolator Packing And Struct Declarations
            //-------------------------------------------------------------------------------------
        
            //-------------------------------------------------------------------------------------
            // Graph generated code
            //-------------------------------------------------------------------------------------
                    // Shared Graph Properties (uniform inputs)
                    CBUFFER_START(UnityPerMaterial)
                    half _NormalsScale;
                    half _NormalsStrength;
                    half4 _ScatterColourBase;
                    half4 _ScatterColourShadow;
                    float4 _ScatterColourShallow;
                    half _ScatterColourShallowDepthMax;
                    half _ScatterColourShallowDepthFalloff;
                    half _SSSIntensityBase;
                    half _SSSIntensitySun;
                    half4 _SSSTint;
                    half _SSSSunFalloff;
                    float _Specular;
                    float _Occlusion;
                    half _Smoothness;
                    float _SmoothnessFar;
                    float _SmoothnessFarDistance;
                    float _SmoothnessFalloff;
                    float _MinReflectionDirectionY;
                    half _FoamScale;
                    half _FoamFeather;
                    half _FoamIntensityAlbedo;
                    half _FoamIntensityEmissive;
                    half _FoamSmoothness;
                    half _FoamNormalStrength;
                    half4 _FoamBubbleColor;
                    half _FoamBubbleParallax;
                    half _FoamBubblesCoverage;
                    half _RefractionStrength;
                    half3 _DepthFogDensity;
                    float _CausticsTextureScale;
                    float _CausticsTextureAverage;
                    float _CausticsStrength;
                    float _CausticsFocalDepth;
                    float _CausticsDepthOfField;
                    float _CausticsDistortionStrength;
                    float _CausticsDistortionScale;
                    CBUFFER_END
                    TEXTURE2D(_TextureNormals); SAMPLER(sampler_TextureNormals); float4 _TextureNormals_TexelSize;
                    TEXTURE2D(_TextureFoam); SAMPLER(sampler_TextureFoam); half4 _TextureFoam_TexelSize;
                    TEXTURE2D(_CausticsTexture); SAMPLER(sampler_CausticsTexture); float4 _CausticsTexture_TexelSize;
                    TEXTURE2D(_CausticsDistortionTexture); SAMPLER(sampler_CausticsDistortionTexture); float4 _CausticsDistortionTexture_TexelSize;
                
                // Vertex Graph Inputs
                    struct VertexDescriptionInputs
                    {
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        float3 WorldSpaceNormal; // optional
                        #endif // Shader Graph Keywords
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        float3 WorldSpaceTangent; // optional
                        #endif // Shader Graph Keywords
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        float3 ObjectSpaceBiTangent; // optional
                        #endif // Shader Graph Keywords
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        float3 WorldSpaceBiTangent; // optional
                        #endif // Shader Graph Keywords
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        float3 AbsoluteWorldSpacePosition; // optional
                        #endif // Shader Graph Keywords
                    };
                // Vertex Graph Outputs
                    struct VertexDescription
                    {
                        float3 VertexPosition;
                        float3 VertexNormal;
                        float3 VertexTangent;

						// Crest -------------------
						float LodAlpha;
						float2 WorldXZUndisplaced;
						half OceanDepth;
						half Foam;
						half2 Shadow;
						half2 Flow;
						half SSS;
						// -------------------------
					};
                    
                // Pixel Graph Inputs
                    struct SurfaceDescriptionInputs
                    {
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        float3 WorldSpaceViewDirection; // optional
                        #endif // Shader Graph Keywords
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        float3 ViewSpacePosition; // optional
                        #endif // Shader Graph Keywords
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        float3 WorldSpacePosition; // optional
                        #endif // Shader Graph Keywords
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        float3 AbsoluteWorldSpacePosition; // optional
                        #endif // Shader Graph Keywords
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        float4 ScreenPosition; // optional
                        #endif // Shader Graph Keywords
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        float4 uv0; // optional
                        #endif // Shader Graph Keywords
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        float4 uv1; // optional
                        #endif // Shader Graph Keywords
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        float4 uv2; // optional
                        #endif // Shader Graph Keywords
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        float4 uv3; // optional
                        #endif // Shader Graph Keywords
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        float FaceSign; // optional
                        #endif // Shader Graph Keywords
                    };
                // Pixel Graph Outputs
                    struct SurfaceDescription
                    {
                        float3 Albedo;
                        float3 Normal;
                        float3 Specular;
                        float3 Emission;
                        float Smoothness;
                        float Occlusion;
                        float Alpha;
                        float AlphaClipThreshold;
                    };
                    
                // Shared Graph Node Functions
                
                    // 73abdd69b930b80b268ce9c97db7e24b
                    #include "ShadergraphFramework/CrestNodeComputeSamplingData.hlsl"
                
                    // 2973156898769ad700b9d1820ecfd177
                    #include "ShadergraphFramework/CrestNodeSampleOceanData.hlsl"
                
                    struct Bindings_CrestSampleOceanData_f19d6d2aa60f8294e870f100303ee5a4
                    {
                    };
                
                    void SG_CrestSampleOceanData_f19d6d2aa60f8294e870f100303ee5a4(float2 Vector2_3171933F, float Vector1_CD41515B, float3 Vector3_7E91D336, float3 Vector3_3A95DCDF, float4 Vector4_C0B2B5EA, float4 Vector4_9C46108E, float Vector1_8EA8B92B, Bindings_CrestSampleOceanData_f19d6d2aa60f8294e870f100303ee5a4 IN, out float3 Displacement_1, out float OceanWaterDepth_5, out float Foam_6, out float2 Shadow_7, out float2 Flow_8, out float SubSurfaceScattering_9)
                    {
                        float2 _Property_1A287CC6_Out_0 = Vector2_3171933F;
                        float _Property_2D1D1700_Out_0 = Vector1_CD41515B;
                        float3 _Property_C925A867_Out_0 = Vector3_7E91D336;
                        float3 _Property_6C273401_Out_0 = Vector3_3A95DCDF;
                        float4 _Property_467D1BE7_Out_0 = Vector4_C0B2B5EA;
                        float4 _Property_4E045F45_Out_0 = Vector4_9C46108E;
                        float _Property_59E019ED_Out_0 = Vector1_8EA8B92B;
                        float3 _CustomFunction_487C31E1_Displacement_3;
                        float _CustomFunction_487C31E1_OceanDepth_8;
                        float _CustomFunction_487C31E1_Foam_4;
                        float2 _CustomFunction_487C31E1_Shadow_5;
                        float2 _CustomFunction_487C31E1_Flow_6;
                        float _CustomFunction_487C31E1_SSS_17;
                        CrestNodeSampleOceanData_float(_Property_1A287CC6_Out_0, _Property_2D1D1700_Out_0, _Property_C925A867_Out_0, _Property_6C273401_Out_0, _Property_467D1BE7_Out_0, _Property_4E045F45_Out_0, _Property_59E019ED_Out_0, _CustomFunction_487C31E1_Displacement_3, _CustomFunction_487C31E1_OceanDepth_8, _CustomFunction_487C31E1_Foam_4, _CustomFunction_487C31E1_Shadow_5, _CustomFunction_487C31E1_Flow_6, _CustomFunction_487C31E1_SSS_17);
                        Displacement_1 = _CustomFunction_487C31E1_Displacement_3;
                        OceanWaterDepth_5 = _CustomFunction_487C31E1_OceanDepth_8;
                        Foam_6 = _CustomFunction_487C31E1_Foam_4;
                        Shadow_7 = _CustomFunction_487C31E1_Shadow_5;
                        Flow_8 = _CustomFunction_487C31E1_Flow_6;
                        SubSurfaceScattering_9 = _CustomFunction_487C31E1_SSS_17;
                    }
                
                    void Unity_Subtract_float2(float2 A, float2 B, out float2 Out)
                    {
                        Out = A - B;
                    }
                
                    struct Bindings_CrestComputeOceanDataFromDisplacedPosition_1f483fe321b10c64e8b93a023ae60dd6
                    {
                    };
                
                    void SG_CrestComputeOceanDataFromDisplacedPosition_1f483fe321b10c64e8b93a023ae60dd6(float3 Vector3_A7B8495A, Bindings_CrestComputeOceanDataFromDisplacedPosition_1f483fe321b10c64e8b93a023ae60dd6 IN, out float2 UndisplacedPosXZAWS_7, out float LodAlpha_6, out float3 Displacement_8, out float OceanWaterDepth_1, out float Foam_2, out float2 Shadow_3, out float2 Flow_4, out float SubSurfaceScattering_5)
                    {
                        float3 _Property_232E7FDA_Out_0 = Vector3_A7B8495A;
                        float _Split_8E8A6DCA_R_1 = _Property_232E7FDA_Out_0[0];
                        float _Split_8E8A6DCA_G_2 = _Property_232E7FDA_Out_0[1];
                        float _Split_8E8A6DCA_B_3 = _Property_232E7FDA_Out_0[2];
                        float _Split_8E8A6DCA_A_4 = 0;
                        float2 _Vector2_A3499051_Out_0 = float2(_Split_8E8A6DCA_R_1, _Split_8E8A6DCA_B_3);
                        half _CustomFunction_A082C8F2_LodAlpha_3;
                        half3 _CustomFunction_A082C8F2_OceanPosScale0_4;
                        half3 _CustomFunction_A082C8F2_OceanPosScale1_5;
                        half4 _CustomFunction_A082C8F2_OceanParams0_6;
                        half4 _CustomFunction_A082C8F2_OceanParams1_7;
                        half _CustomFunction_A082C8F2_Slice0_1;
                        half _CustomFunction_A082C8F2_Slice1_2;
                        CrestComputeSamplingData_half(_Vector2_A3499051_Out_0, _CustomFunction_A082C8F2_LodAlpha_3, _CustomFunction_A082C8F2_OceanPosScale0_4, _CustomFunction_A082C8F2_OceanPosScale1_5, _CustomFunction_A082C8F2_OceanParams0_6, _CustomFunction_A082C8F2_OceanParams1_7, _CustomFunction_A082C8F2_Slice0_1, _CustomFunction_A082C8F2_Slice1_2);
                        Bindings_CrestSampleOceanData_f19d6d2aa60f8294e870f100303ee5a4 _CrestSampleOceanData_87ACC65F;
                        float3 _CrestSampleOceanData_87ACC65F_Displacement_1;
                        float _CrestSampleOceanData_87ACC65F_OceanWaterDepth_5;
                        float _CrestSampleOceanData_87ACC65F_Foam_6;
                        float2 _CrestSampleOceanData_87ACC65F_Shadow_7;
                        float2 _CrestSampleOceanData_87ACC65F_Flow_8;
                        float _CrestSampleOceanData_87ACC65F_SubSurfaceScattering_9;
                        SG_CrestSampleOceanData_f19d6d2aa60f8294e870f100303ee5a4(_Vector2_A3499051_Out_0, _CustomFunction_A082C8F2_LodAlpha_3, _CustomFunction_A082C8F2_OceanPosScale0_4, _CustomFunction_A082C8F2_OceanPosScale1_5, _CustomFunction_A082C8F2_OceanParams0_6, _CustomFunction_A082C8F2_OceanParams1_7, _CustomFunction_A082C8F2_Slice0_1, _CrestSampleOceanData_87ACC65F, _CrestSampleOceanData_87ACC65F_Displacement_1, _CrestSampleOceanData_87ACC65F_OceanWaterDepth_5, _CrestSampleOceanData_87ACC65F_Foam_6, _CrestSampleOceanData_87ACC65F_Shadow_7, _CrestSampleOceanData_87ACC65F_Flow_8, _CrestSampleOceanData_87ACC65F_SubSurfaceScattering_9);
                        float _Split_CD3A9051_R_1 = _CrestSampleOceanData_87ACC65F_Displacement_1[0];
                        float _Split_CD3A9051_G_2 = _CrestSampleOceanData_87ACC65F_Displacement_1[1];
                        float _Split_CD3A9051_B_3 = _CrestSampleOceanData_87ACC65F_Displacement_1[2];
                        float _Split_CD3A9051_A_4 = 0;
                        float2 _Vector2_B8C0C1F0_Out_0 = float2(_Split_CD3A9051_R_1, _Split_CD3A9051_B_3);
                        float2 _Subtract_8977A663_Out_2;
                        Unity_Subtract_float2(_Vector2_A3499051_Out_0, _Vector2_B8C0C1F0_Out_0, _Subtract_8977A663_Out_2);
                        half _CustomFunction_9D8B14F0_LodAlpha_3;
                        half3 _CustomFunction_9D8B14F0_OceanPosScale0_4;
                        half3 _CustomFunction_9D8B14F0_OceanPosScale1_5;
                        half4 _CustomFunction_9D8B14F0_OceanParams0_6;
                        half4 _CustomFunction_9D8B14F0_OceanParams1_7;
                        half _CustomFunction_9D8B14F0_Slice0_1;
                        half _CustomFunction_9D8B14F0_Slice1_2;
                        CrestComputeSamplingData_half(_Subtract_8977A663_Out_2, _CustomFunction_9D8B14F0_LodAlpha_3, _CustomFunction_9D8B14F0_OceanPosScale0_4, _CustomFunction_9D8B14F0_OceanPosScale1_5, _CustomFunction_9D8B14F0_OceanParams0_6, _CustomFunction_9D8B14F0_OceanParams1_7, _CustomFunction_9D8B14F0_Slice0_1, _CustomFunction_9D8B14F0_Slice1_2);
                        Bindings_CrestSampleOceanData_f19d6d2aa60f8294e870f100303ee5a4 _CrestSampleOceanData_D8619779;
                        float3 _CrestSampleOceanData_D8619779_Displacement_1;
                        float _CrestSampleOceanData_D8619779_OceanWaterDepth_5;
                        float _CrestSampleOceanData_D8619779_Foam_6;
                        float2 _CrestSampleOceanData_D8619779_Shadow_7;
                        float2 _CrestSampleOceanData_D8619779_Flow_8;
                        float _CrestSampleOceanData_D8619779_SubSurfaceScattering_9;
                        SG_CrestSampleOceanData_f19d6d2aa60f8294e870f100303ee5a4(_Subtract_8977A663_Out_2, _CustomFunction_9D8B14F0_LodAlpha_3, _CustomFunction_9D8B14F0_OceanPosScale0_4, _CustomFunction_9D8B14F0_OceanPosScale1_5, _CustomFunction_9D8B14F0_OceanParams0_6, _CustomFunction_9D8B14F0_OceanParams1_7, _CustomFunction_9D8B14F0_Slice0_1, _CrestSampleOceanData_D8619779, _CrestSampleOceanData_D8619779_Displacement_1, _CrestSampleOceanData_D8619779_OceanWaterDepth_5, _CrestSampleOceanData_D8619779_Foam_6, _CrestSampleOceanData_D8619779_Shadow_7, _CrestSampleOceanData_D8619779_Flow_8, _CrestSampleOceanData_D8619779_SubSurfaceScattering_9);
                        float _Split_1616DE7_R_1 = _CrestSampleOceanData_D8619779_Displacement_1[0];
                        float _Split_1616DE7_G_2 = _CrestSampleOceanData_D8619779_Displacement_1[1];
                        float _Split_1616DE7_B_3 = _CrestSampleOceanData_D8619779_Displacement_1[2];
                        float _Split_1616DE7_A_4 = 0;
                        float2 _Vector2_B871614F_Out_0 = float2(_Split_1616DE7_R_1, _Split_1616DE7_B_3);
                        float2 _Subtract_39E2CE30_Out_2;
                        Unity_Subtract_float2(_Vector2_A3499051_Out_0, _Vector2_B871614F_Out_0, _Subtract_39E2CE30_Out_2);
                        half _CustomFunction_10AEAD9A_LodAlpha_3;
                        half3 _CustomFunction_10AEAD9A_OceanPosScale0_4;
                        half3 _CustomFunction_10AEAD9A_OceanPosScale1_5;
                        half4 _CustomFunction_10AEAD9A_OceanParams0_6;
                        half4 _CustomFunction_10AEAD9A_OceanParams1_7;
                        half _CustomFunction_10AEAD9A_Slice0_1;
                        half _CustomFunction_10AEAD9A_Slice1_2;
                        CrestComputeSamplingData_half(_Subtract_39E2CE30_Out_2, _CustomFunction_10AEAD9A_LodAlpha_3, _CustomFunction_10AEAD9A_OceanPosScale0_4, _CustomFunction_10AEAD9A_OceanPosScale1_5, _CustomFunction_10AEAD9A_OceanParams0_6, _CustomFunction_10AEAD9A_OceanParams1_7, _CustomFunction_10AEAD9A_Slice0_1, _CustomFunction_10AEAD9A_Slice1_2);
                        Bindings_CrestSampleOceanData_f19d6d2aa60f8294e870f100303ee5a4 _CrestSampleOceanData_A1195FE2;
                        float3 _CrestSampleOceanData_A1195FE2_Displacement_1;
                        float _CrestSampleOceanData_A1195FE2_OceanWaterDepth_5;
                        float _CrestSampleOceanData_A1195FE2_Foam_6;
                        float2 _CrestSampleOceanData_A1195FE2_Shadow_7;
                        float2 _CrestSampleOceanData_A1195FE2_Flow_8;
                        float _CrestSampleOceanData_A1195FE2_SubSurfaceScattering_9;
                        SG_CrestSampleOceanData_f19d6d2aa60f8294e870f100303ee5a4(_Subtract_39E2CE30_Out_2, _CustomFunction_10AEAD9A_LodAlpha_3, _CustomFunction_10AEAD9A_OceanPosScale0_4, _CustomFunction_10AEAD9A_OceanPosScale1_5, _CustomFunction_10AEAD9A_OceanParams0_6, _CustomFunction_10AEAD9A_OceanParams1_7, _CustomFunction_10AEAD9A_Slice0_1, _CrestSampleOceanData_A1195FE2, _CrestSampleOceanData_A1195FE2_Displacement_1, _CrestSampleOceanData_A1195FE2_OceanWaterDepth_5, _CrestSampleOceanData_A1195FE2_Foam_6, _CrestSampleOceanData_A1195FE2_Shadow_7, _CrestSampleOceanData_A1195FE2_Flow_8, _CrestSampleOceanData_A1195FE2_SubSurfaceScattering_9);
                        UndisplacedPosXZAWS_7 = _Subtract_39E2CE30_Out_2;
                        LodAlpha_6 = _CustomFunction_10AEAD9A_LodAlpha_3;
                        Displacement_8 = _CrestSampleOceanData_A1195FE2_Displacement_1;
                        OceanWaterDepth_1 = _CrestSampleOceanData_A1195FE2_OceanWaterDepth_5;
                        Foam_2 = _CrestSampleOceanData_A1195FE2_Foam_6;
                        Shadow_3 = _CrestSampleOceanData_A1195FE2_Shadow_7;
                        Flow_4 = _CrestSampleOceanData_A1195FE2_Flow_8;
                        SubSurfaceScattering_5 = _CrestSampleOceanData_A1195FE2_SubSurfaceScattering_9;
                    }
                
                    struct Bindings_CrestUnpackData_5bc4cf08f88035c489658d60edefeec8
                    {
                        float3 AbsoluteWorldSpacePosition;
                        half4 uv0;
                        half4 uv1;
                        half4 uv2;
                    };
                
                    void SG_CrestUnpackData_5bc4cf08f88035c489658d60edefeec8(Bindings_CrestUnpackData_5bc4cf08f88035c489658d60edefeec8 IN, out float2 PositionXZWSUndisp_2, out float LodAlpha_1, out float OceanDepth_3, out float Foam_4, out float2 Shadow_5, out float2 Flow_6, out float SubSurfaceScattering_7)
                    {
                        float4 _UV_CF6CD5F2_Out_0 = IN.uv0;
                        float _Split_B10345C8_R_1 = _UV_CF6CD5F2_Out_0[0];
                        float _Split_B10345C8_G_2 = _UV_CF6CD5F2_Out_0[1];
                        float _Split_B10345C8_B_3 = _UV_CF6CD5F2_Out_0[2];
                        float _Split_B10345C8_A_4 = _UV_CF6CD5F2_Out_0[3];
                        float2 _Vector2_552A5E1F_Out_0 = float2(_Split_B10345C8_R_1, _Split_B10345C8_G_2);
                        Bindings_CrestComputeOceanDataFromDisplacedPosition_1f483fe321b10c64e8b93a023ae60dd6 _CrestComputeOceanDataFromDisplacedPosition_3B9EB7BB;
                        float2 _CrestComputeOceanDataFromDisplacedPosition_3B9EB7BB_UndisplacedPosXZAWS_7;
                        float _CrestComputeOceanDataFromDisplacedPosition_3B9EB7BB_LodAlpha_6;
                        float3 _CrestComputeOceanDataFromDisplacedPosition_3B9EB7BB_Displacement_8;
                        float _CrestComputeOceanDataFromDisplacedPosition_3B9EB7BB_OceanWaterDepth_1;
                        float _CrestComputeOceanDataFromDisplacedPosition_3B9EB7BB_Foam_2;
                        float2 _CrestComputeOceanDataFromDisplacedPosition_3B9EB7BB_Shadow_3;
                        float2 _CrestComputeOceanDataFromDisplacedPosition_3B9EB7BB_Flow_4;
                        float _CrestComputeOceanDataFromDisplacedPosition_3B9EB7BB_SubSurfaceScattering_5;
                        SG_CrestComputeOceanDataFromDisplacedPosition_1f483fe321b10c64e8b93a023ae60dd6(IN.AbsoluteWorldSpacePosition, _CrestComputeOceanDataFromDisplacedPosition_3B9EB7BB, _CrestComputeOceanDataFromDisplacedPosition_3B9EB7BB_UndisplacedPosXZAWS_7, _CrestComputeOceanDataFromDisplacedPosition_3B9EB7BB_LodAlpha_6, _CrestComputeOceanDataFromDisplacedPosition_3B9EB7BB_Displacement_8, _CrestComputeOceanDataFromDisplacedPosition_3B9EB7BB_OceanWaterDepth_1, _CrestComputeOceanDataFromDisplacedPosition_3B9EB7BB_Foam_2, _CrestComputeOceanDataFromDisplacedPosition_3B9EB7BB_Shadow_3, _CrestComputeOceanDataFromDisplacedPosition_3B9EB7BB_Flow_4, _CrestComputeOceanDataFromDisplacedPosition_3B9EB7BB_SubSurfaceScattering_5);
                        #if defined(CREST_GENERATED_SHADER_ON)
                        float2 _GENERATEDSHADER_71C0694B_Out_0 = _Vector2_552A5E1F_Out_0;
                        #else
                        float2 _GENERATEDSHADER_71C0694B_Out_0 = _CrestComputeOceanDataFromDisplacedPosition_3B9EB7BB_UndisplacedPosXZAWS_7;
                        #endif
                        #if defined(CREST_GENERATED_SHADER_ON)
                        float _GENERATEDSHADER_2A933A74_Out_0 = _Split_B10345C8_B_3;
                        #else
                        float _GENERATEDSHADER_2A933A74_Out_0 = _CrestComputeOceanDataFromDisplacedPosition_3B9EB7BB_LodAlpha_6;
                        #endif
                        #if defined(CREST_GENERATED_SHADER_ON)
                        float _GENERATEDSHADER_EFBF6036_Out_0 = _Split_B10345C8_A_4;
                        #else
                        float _GENERATEDSHADER_EFBF6036_Out_0 = _CrestComputeOceanDataFromDisplacedPosition_3B9EB7BB_OceanWaterDepth_1;
                        #endif
                        float4 _UV_39E1D2DF_Out_0 = IN.uv1;
                        float _Split_CB2CA9B8_R_1 = _UV_39E1D2DF_Out_0[0];
                        float _Split_CB2CA9B8_G_2 = _UV_39E1D2DF_Out_0[1];
                        float _Split_CB2CA9B8_B_3 = _UV_39E1D2DF_Out_0[2];
                        float _Split_CB2CA9B8_A_4 = _UV_39E1D2DF_Out_0[3];
                        #if defined(CREST_GENERATED_SHADER_ON)
                        float _GENERATEDSHADER_1BBAE801_Out_0 = _Split_CB2CA9B8_G_2;
                        #else
                        float _GENERATEDSHADER_1BBAE801_Out_0 = _CrestComputeOceanDataFromDisplacedPosition_3B9EB7BB_Foam_2;
                        #endif
                        float4 _UV_33A67BF5_Out_0 = IN.uv2;
                        float _Split_753DFB28_R_1 = _UV_33A67BF5_Out_0[0];
                        float _Split_753DFB28_G_2 = _UV_33A67BF5_Out_0[1];
                        float _Split_753DFB28_B_3 = _UV_33A67BF5_Out_0[2];
                        float _Split_753DFB28_A_4 = _UV_33A67BF5_Out_0[3];
                        float2 _Vector2_7883B8A6_Out_0 = float2(_Split_753DFB28_R_1, _Split_753DFB28_G_2);
                        #if defined(CREST_GENERATED_SHADER_ON)
                        float2 _GENERATEDSHADER_B499BDE6_Out_0 = _Vector2_7883B8A6_Out_0;
                        #else
                        float2 _GENERATEDSHADER_B499BDE6_Out_0 = _CrestComputeOceanDataFromDisplacedPosition_3B9EB7BB_Shadow_3;
                        #endif
                        float2 _Vector2_3A83E1FC_Out_0 = float2(_Split_753DFB28_B_3, _Split_753DFB28_A_4);
                        #if defined(CREST_GENERATED_SHADER_ON)
                        float2 _GENERATEDSHADER_84CB20AD_Out_0 = _Vector2_3A83E1FC_Out_0;
                        #else
                        float2 _GENERATEDSHADER_84CB20AD_Out_0 = _CrestComputeOceanDataFromDisplacedPosition_3B9EB7BB_Flow_4;
                        #endif
                        #if defined(CREST_GENERATED_SHADER_ON)
                        float _GENERATEDSHADER_6BDC98D1_Out_0 = _Split_CB2CA9B8_R_1;
                        #else
                        float _GENERATEDSHADER_6BDC98D1_Out_0 = _CrestComputeOceanDataFromDisplacedPosition_3B9EB7BB_SubSurfaceScattering_5;
                        #endif
                        PositionXZWSUndisp_2 = _GENERATEDSHADER_71C0694B_Out_0;
                        LodAlpha_1 = _GENERATEDSHADER_2A933A74_Out_0;
                        OceanDepth_3 = _GENERATEDSHADER_EFBF6036_Out_0;
                        Foam_4 = _GENERATEDSHADER_1BBAE801_Out_0;
                        Shadow_5 = _GENERATEDSHADER_B499BDE6_Out_0;
                        Flow_6 = _GENERATEDSHADER_84CB20AD_Out_0;
                        SubSurfaceScattering_7 = _GENERATEDSHADER_6BDC98D1_Out_0;
                    }
                
                    #include "ShadergraphFramework/CrestNodeLightData.hlsl"
                
                    struct Bindings_CrestLightData_b74b6e8c0b489314ca7aea3e2cc9c54c
                    {
                    };
                
                    void SG_CrestLightData_b74b6e8c0b489314ca7aea3e2cc9c54c(Bindings_CrestLightData_b74b6e8c0b489314ca7aea3e2cc9c54c IN, out half3 Direction_1, out half3 Intensity_2)
                    {
                        half3 _CustomFunction_5D41A6E0_Direction_0;
                        half3 _CustomFunction_5D41A6E0_Colour_1;
                        CrestNodeLightData_half(_CustomFunction_5D41A6E0_Direction_0, _CustomFunction_5D41A6E0_Colour_1);
                        Direction_1 = _CustomFunction_5D41A6E0_Direction_0;
                        Intensity_2 = _CustomFunction_5D41A6E0_Colour_1;
                    }
                
                    // 2a24b14f4becad7f23cf06f270571445
                    #include "ShadergraphFramework/CrestNodeDrivenInputs.hlsl"
                
                    struct Bindings_CrestDrivenData_40572ccadf7d9704a83d283fb4c9f19d
                    {
                    };
                
                    void SG_CrestDrivenData_40572ccadf7d9704a83d283fb4c9f19d(Bindings_CrestDrivenData_40572ccadf7d9704a83d283fb4c9f19d IN, out half MeshScaleAlpha_1, out half LodDataTexelSize_8, out half GeometryGridSize_2, out half3 OceanPosScale0_3, out half3 OceanPosScale1_4, out half4 OceanParams0_5, out half4 OceanParams1_6, out half SliceIndex0_7)
                    {
                        half _CustomFunction_CD9A5F8F_MeshScaleAlpha_9;
                        half _CustomFunction_CD9A5F8F_LodDataTexelSize_10;
                        half _CustomFunction_CD9A5F8F_GeometryGridSize_14;
                        half3 _CustomFunction_CD9A5F8F_OceanPosScale0_7;
                        half3 _CustomFunction_CD9A5F8F_OceanPosScale1_8;
                        half4 _CustomFunction_CD9A5F8F_OceanParams0_11;
                        half4 _CustomFunction_CD9A5F8F_OceanParams1_12;
                        half _CustomFunction_CD9A5F8F_SliceIndex0_13;
                        CrestOceanSurfaceValues_half(_CustomFunction_CD9A5F8F_MeshScaleAlpha_9, _CustomFunction_CD9A5F8F_LodDataTexelSize_10, _CustomFunction_CD9A5F8F_GeometryGridSize_14, _CustomFunction_CD9A5F8F_OceanPosScale0_7, _CustomFunction_CD9A5F8F_OceanPosScale1_8, _CustomFunction_CD9A5F8F_OceanParams0_11, _CustomFunction_CD9A5F8F_OceanParams1_12, _CustomFunction_CD9A5F8F_SliceIndex0_13);
                        MeshScaleAlpha_1 = _CustomFunction_CD9A5F8F_MeshScaleAlpha_9;
                        LodDataTexelSize_8 = _CustomFunction_CD9A5F8F_LodDataTexelSize_10;
                        GeometryGridSize_2 = _CustomFunction_CD9A5F8F_GeometryGridSize_14;
                        OceanPosScale0_3 = _CustomFunction_CD9A5F8F_OceanPosScale0_7;
                        OceanPosScale1_4 = _CustomFunction_CD9A5F8F_OceanPosScale1_8;
                        OceanParams0_5 = _CustomFunction_CD9A5F8F_OceanParams0_11;
                        OceanParams1_6 = _CustomFunction_CD9A5F8F_OceanParams1_12;
                        SliceIndex0_7 = _CustomFunction_CD9A5F8F_SliceIndex0_13;
                    }
                
                    void Unity_Normalize_float3(float3 In, out float3 Out)
                    {
                        Out = normalize(In);
                    }
                
                    void Unity_Not_float(float In, out float Out)
                    {
                        Out = !In;
                    }
                
                    struct Bindings_CrestIsUnderwater_52f7750f15e114937b54a0fd27f0d2f2
                    {
                        float FaceSign;
                    };
                
                    void SG_CrestIsUnderwater_52f7750f15e114937b54a0fd27f0d2f2(Bindings_CrestIsUnderwater_52f7750f15e114937b54a0fd27f0d2f2 IN, out float OutBoolean_1)
                    {
                        float _IsFrontFace_F6DF08D5_Out_0 = max(0, IN.FaceSign);
                        float _Not_3B19614D_Out_1;
                        Unity_Not_float(_IsFrontFace_F6DF08D5_Out_0, _Not_3B19614D_Out_1);
                        OutBoolean_1 = _Not_3B19614D_Out_1;
                    }
                
                    // 3c543d21fa1847bbfea4c56ca61fd4f3
                    #include "ShadergraphFramework/CrestNodeNormalMapping.hlsl"
                
                    struct Bindings_CrestComputeNormal_61b9efc6612ab3b4f84174344af5e12a
                    {
                        float FaceSign;
                    };
                
                    void SG_CrestComputeNormal_61b9efc6612ab3b4f84174344af5e12a(float3 Vector3_FE793823, float3 Vector3_C8190B61, float4 Vector4_F18E4948, float4 Vector4_43DD8E03, float Vector1_8771A258, TEXTURE2D_PARAM(Texture2D_6CA3A26C, samplerTexture2D_6CA3A26C), float4 Texture2D_6CA3A26C_TexelSize, float Vector1_418D6270, float Vector1_6EC9A7C0, float Vector1_5D9D8139, float2 Vector2_3ED47A62, float2 Vector2_891575B0, float3 Vector3_A9F402BF, float Vector1_2ABAF0E6, Bindings_CrestComputeNormal_61b9efc6612ab3b4f84174344af5e12a IN, out half3 Normal_1)
                    {
                        float3 _Property_9021A08B_Out_0 = Vector3_FE793823;
                        float3 _Property_9C8BC1F1_Out_0 = Vector3_C8190B61;
                        float4 _Property_BA13B38B_Out_0 = Vector4_F18E4948;
                        float4 _Property_587E24D5_Out_0 = Vector4_43DD8E03;
                        float _Property_1A49C52D_Out_0 = Vector1_8771A258;
                        float _Property_514FBFB9_Out_0 = Vector1_418D6270;
                        float _Property_27A6DF1E_Out_0 = Vector1_6EC9A7C0;
                        float _Property_A277E64F_Out_0 = Vector1_5D9D8139;
                        float2 _Property_805F9A1D_Out_0 = Vector2_3ED47A62;
                        float2 _Property_100A6EB8_Out_0 = Vector2_891575B0;
                        float3 _Property_11AD0CE_Out_0 = Vector3_A9F402BF;
                        Bindings_CrestIsUnderwater_52f7750f15e114937b54a0fd27f0d2f2 _CrestIsUnderwater_BA18A1A;
                        _CrestIsUnderwater_BA18A1A.FaceSign = IN.FaceSign;
                        float _CrestIsUnderwater_BA18A1A_OutBoolean_1;
                        SG_CrestIsUnderwater_52f7750f15e114937b54a0fd27f0d2f2(_CrestIsUnderwater_BA18A1A, _CrestIsUnderwater_BA18A1A_OutBoolean_1);
                        float _Property_347CBD07_Out_0 = Vector1_2ABAF0E6;
                        half3 _CustomFunction_61A7F8B0_NormalTS_1;
                        OceanNormals_half(_Property_9021A08B_Out_0, _Property_9C8BC1F1_Out_0, _Property_BA13B38B_Out_0, _Property_587E24D5_Out_0, _Property_1A49C52D_Out_0, Texture2D_6CA3A26C, _Property_514FBFB9_Out_0, _Property_27A6DF1E_Out_0, _Property_A277E64F_Out_0, _Property_805F9A1D_Out_0, _Property_100A6EB8_Out_0, _Property_11AD0CE_Out_0, _CrestIsUnderwater_BA18A1A_OutBoolean_1, _Property_347CBD07_Out_0, _CustomFunction_61A7F8B0_NormalTS_1);
                        Normal_1 = _CustomFunction_61A7F8B0_NormalTS_1;
                    }
                
                    // 8e3c4891a0a191b55617faf4fca7b22b
                    #include "ShadergraphFramework/CrestNodeApplyFresnel.hlsl"
                
                    struct Bindings_CrestFresnel_c6f6c13c4fdb04e42b427e4c2610d3c9
                    {
                        float FaceSign;
                    };
                
                    void SG_CrestFresnel_c6f6c13c4fdb04e42b427e4c2610d3c9(float3 Vector3_FFFD5D37, float3 Vector3_50713CBB, float Vector1_C2909293, float Vector1_DDF5B66E, Bindings_CrestFresnel_c6f6c13c4fdb04e42b427e4c2610d3c9 IN, out float LightTransmitted_1, out float LightReflected_2)
                    {
                        float3 _Property_3166A32_Out_0 = Vector3_50713CBB;
                        float3 _Property_ED2FFB18_Out_0 = Vector3_FFFD5D37;
                        Bindings_CrestIsUnderwater_52f7750f15e114937b54a0fd27f0d2f2 _CrestIsUnderwater_291EED16;
                        _CrestIsUnderwater_291EED16.FaceSign = IN.FaceSign;
                        float _CrestIsUnderwater_291EED16_OutBoolean_1;
                        SG_CrestIsUnderwater_52f7750f15e114937b54a0fd27f0d2f2(_CrestIsUnderwater_291EED16, _CrestIsUnderwater_291EED16_OutBoolean_1);
                        float _Property_D5B7D059_Out_0 = Vector1_DDF5B66E;
                        float _Property_2395CED2_Out_0 = Vector1_C2909293;
                        float _CustomFunction_6DEBC54E_LightTransmitted_1;
                        float _CustomFunction_6DEBC54E_LightReflected_10;
                        CrestNodeApplyFresnel_float(_Property_3166A32_Out_0, _Property_ED2FFB18_Out_0, _CrestIsUnderwater_291EED16_OutBoolean_1, _Property_D5B7D059_Out_0, _Property_2395CED2_Out_0, _CustomFunction_6DEBC54E_LightTransmitted_1, _CustomFunction_6DEBC54E_LightReflected_10);
                        LightTransmitted_1 = _CustomFunction_6DEBC54E_LightTransmitted_1;
                        LightReflected_2 = _CustomFunction_6DEBC54E_LightReflected_10;
                    }
                
                    #include "ShadergraphFramework/CrestNodeAmbientLight.hlsl"
                
                    struct Bindings_CrestAmbientLight_a6ec89b3ca0ab4e98b300ec3ba0e6013
                    {
                    };
                
                    void SG_CrestAmbientLight_a6ec89b3ca0ab4e98b300ec3ba0e6013(Bindings_CrestAmbientLight_a6ec89b3ca0ab4e98b300ec3ba0e6013 IN, out half3 Color_1)
                    {
                        half3 _CustomFunction_84E91696_AmbientLighting_0;
                        CrestNodeAmbientLight_half(_CustomFunction_84E91696_AmbientLighting_0);
                        Color_1 = _CustomFunction_84E91696_AmbientLighting_0;
                    }
                
                    // cfa426252240e9cb5f56ca043dd9bcce
                    #include "ShadergraphFramework/CrestNodeLightWaterVolume.hlsl"
                
                    struct Bindings_CrestVolumeLighting_e9ed6e11710a50640bb4b811d0fa84f2
                    {
                    };
                
                    void SG_CrestVolumeLighting_e9ed6e11710a50640bb4b811d0fa84f2(float4 Vector4_7C3B7892, float3 Vector3_CB562741, float4 Vector4_1925E051, float Vector1_E556918C, float Vector1_67342C54, float Vector1_D7DF1AB, float Vector1_6823579F, float4 Vector4_79A1BE1F, float Vector1_7223C6AC, float Vector1_66E9E54, float2 Vector2_D3558D33, float Vector1_96B7F6DA, float3 Vector3_1559C54, float3 Vector3_EBF06BD4, float3 Vector3_4BB1E4A, float3 Vector3_938EEF71, float3 Vector3_CAD84B7A, Bindings_CrestVolumeLighting_e9ed6e11710a50640bb4b811d0fa84f2 IN, out half3 VolumeLighting_1)
                    {
                        float4 _Property_3BF186D2_Out_0 = Vector4_7C3B7892;
                        float3 _Property_36BD3B33_Out_0 = Vector3_CB562741;
                        float4 _Property_DEEB4017_Out_0 = Vector4_1925E051;
                        float _Property_DEF8261_Out_0 = Vector1_E556918C;
                        float _Property_E59433A0_Out_0 = Vector1_67342C54;
                        float _Property_B909C546_Out_0 = Vector1_D7DF1AB;
                        float _Property_E4FBA75D_Out_0 = Vector1_6823579F;
                        float4 _Property_856C2495_Out_0 = Vector4_79A1BE1F;
                        float _Property_D81EDA3D_Out_0 = Vector1_7223C6AC;
                        float _Property_9368CB3D_Out_0 = Vector1_66E9E54;
                        float2 _Property_BF038BF7_Out_0 = Vector2_D3558D33;
                        float _Property_F35AF73E_Out_0 = Vector1_96B7F6DA;
                        float3 _Property_32B01413_Out_0 = Vector3_1559C54;
                        float3 _Property_5B1BCADB_Out_0 = Vector3_EBF06BD4;
                        float3 _Property_6BBACEFC_Out_0 = Vector3_4BB1E4A;
                        float3 _Property_C8DE9461_Out_0 = Vector3_938EEF71;
                        float3 _Property_919832B1_Out_0 = Vector3_CAD84B7A;
                        half3 _CustomFunction_F6F194A9_VolumeLighting_5;
                        CrestNodeLightWaterVolume_half((_Property_3BF186D2_Out_0.xyz), _Property_36BD3B33_Out_0, (_Property_DEEB4017_Out_0.xyz), _Property_DEF8261_Out_0, _Property_E59433A0_Out_0, _Property_B909C546_Out_0, _Property_E4FBA75D_Out_0, (_Property_856C2495_Out_0.xyz), _Property_D81EDA3D_Out_0, _Property_9368CB3D_Out_0, _Property_BF038BF7_Out_0, _Property_F35AF73E_Out_0, _Property_32B01413_Out_0, _Property_5B1BCADB_Out_0, _Property_6BBACEFC_Out_0, _Property_C8DE9461_Out_0, _Property_919832B1_Out_0, _CustomFunction_F6F194A9_VolumeLighting_5);
                        VolumeLighting_1 = _CustomFunction_F6F194A9_VolumeLighting_5;
                    }
                
                    void Unity_Negate_float(float In, out float Out)
                    {
                        Out = -1 * In;
                    }
                
                    void Unity_SceneColor_float(float4 UV, out float3 Out)
                    {
                        Out = SHADERGRAPH_SAMPLE_SCENE_COLOR(UV.xy);
                    }
                
                    void Unity_SceneDepth_Eye_float(float4 UV, out float Out)
                    {
                        Out = LinearEyeDepth(SHADERGRAPH_SAMPLE_SCENE_DEPTH(UV.xy), _ZBufferParams);
                    }
                
                    // d20b265501ae4875cc4a70806a8e6acb
                    #include "ShadergraphFramework/CrestNodeFoamBubbles.hlsl"
                
                    // 04f98a25a24e78d5ca68a12d00c95fbf
                    #include "ShadergraphFramework/CrestNodeVolumeEmission.hlsl"
                
                    void Unity_Add_float(float A, float B, out float Out)
                    {
                        Out = A + B;
                    }
                
                    struct Bindings_CrestSampleOceanDataSingle_a667f031fd6a3dd42beee0ccc432233d
                    {
                    };
                
                    void SG_CrestSampleOceanDataSingle_a667f031fd6a3dd42beee0ccc432233d(float2 Vector2_1BD49B8D, float3 Vector3_7E91D336, float4 Vector4_C0B2B5EA, float Vector1_8EA8B92B, Bindings_CrestSampleOceanDataSingle_a667f031fd6a3dd42beee0ccc432233d IN, out float3 Displacement_1, out float OceanWaterDepth_5, out float Foam_6, out float2 Shadow_7, out float2 Flow_8, out float SubSurfaceScattering_9)
                    {
                        float2 _Property_FA70A3E6_Out_0 = Vector2_1BD49B8D;
                        float3 _Property_C925A867_Out_0 = Vector3_7E91D336;
                        float4 _Property_467D1BE7_Out_0 = Vector4_C0B2B5EA;
                        float _Property_59E019ED_Out_0 = Vector1_8EA8B92B;
                        float3 _CustomFunction_487C31E1_Displacement_3;
                        float _CustomFunction_487C31E1_OceanDepth_8;
                        float _CustomFunction_487C31E1_Foam_4;
                        float2 _CustomFunction_487C31E1_Shadow_5;
                        float2 _CustomFunction_487C31E1_Flow_6;
                        float _CustomFunction_487C31E1_SSS_17;
                        CrestNodeSampleOceanDataSingle_float(_Property_FA70A3E6_Out_0, _Property_C925A867_Out_0, _Property_467D1BE7_Out_0, _Property_59E019ED_Out_0, _CustomFunction_487C31E1_Displacement_3, _CustomFunction_487C31E1_OceanDepth_8, _CustomFunction_487C31E1_Foam_4, _CustomFunction_487C31E1_Shadow_5, _CustomFunction_487C31E1_Flow_6, _CustomFunction_487C31E1_SSS_17);
                        Displacement_1 = _CustomFunction_487C31E1_Displacement_3;
                        OceanWaterDepth_5 = _CustomFunction_487C31E1_OceanDepth_8;
                        Foam_6 = _CustomFunction_487C31E1_Foam_4;
                        Shadow_7 = _CustomFunction_487C31E1_Shadow_5;
                        Flow_8 = _CustomFunction_487C31E1_Flow_6;
                        SubSurfaceScattering_9 = _CustomFunction_487C31E1_SSS_17;
                    }
                
                    // d3bb4f720a39af4b0dbd1cddc779836d
                    #include "ShadergraphFramework/CrestNodeOceanGlobals.hlsl"
                
                    struct Bindings_CrestOceanGlobals_d50a85284893ec447a25a093505a2120
                    {
                    };
                
                    void SG_CrestOceanGlobals_d50a85284893ec447a25a093505a2120(Bindings_CrestOceanGlobals_d50a85284893ec447a25a093505a2120 IN, out float CrestTime_1, out float TexelsPerWave_2, out float3 OceanCenterPosWorld_3, out float SliceCount_4, out float MeshScaleLerp_5)
                    {
                        float _CustomFunction_9ED6B15_CrestTime_0;
                        float _CustomFunction_9ED6B15_TexelsPerWave_1;
                        float3 _CustomFunction_9ED6B15_OceanCenterPosWorld_2;
                        float _CustomFunction_9ED6B15_SliceCount_3;
                        float _CustomFunction_9ED6B15_MeshScaleLerp_4;
                        CrestNodeOceanGlobals_float(_CustomFunction_9ED6B15_CrestTime_0, _CustomFunction_9ED6B15_TexelsPerWave_1, _CustomFunction_9ED6B15_OceanCenterPosWorld_2, _CustomFunction_9ED6B15_SliceCount_3, _CustomFunction_9ED6B15_MeshScaleLerp_4);
                        CrestTime_1 = _CustomFunction_9ED6B15_CrestTime_0;
                        TexelsPerWave_2 = _CustomFunction_9ED6B15_TexelsPerWave_1;
                        OceanCenterPosWorld_3 = _CustomFunction_9ED6B15_OceanCenterPosWorld_2;
                        SliceCount_4 = _CustomFunction_9ED6B15_SliceCount_3;
                        MeshScaleLerp_5 = _CustomFunction_9ED6B15_MeshScaleLerp_4;
                    }
                
                    void Unity_Add_float3(float3 A, float3 B, out float3 Out)
                    {
                        Out = A + B;
                    }
                
                    // 8e5fdc28d5dd56d17a96643305c4ad1c
                    #include "ShadergraphFramework/CrestNodeApplyCaustics.hlsl"
                
                    void Unity_Multiply_float(float3 A, float3 B, out float3 Out)
                    {
                        Out = A * B;
                    }
                
                    void Unity_Negate_float3(float3 In, out float3 Out)
                    {
                        Out = -1 * In;
                    }
                
                    void Unity_Exponential_float3(float3 In, out float3 Out)
                    {
                        Out = exp(In);
                    }
                
                    void Unity_OneMinus_float3(float3 In, out float3 Out)
                    {
                        Out = 1 - In;
                    }
                
                    void Unity_Lerp_float3(float3 A, float3 B, float3 T, out float3 Out)
                    {
                        Out = lerp(A, B, T);
                    }
                
                    void Unity_Branch_float3(float Predicate, float3 True, float3 False, out float3 Out)
                    {
                        Out = Predicate ? True : False;
                    }
                
                    struct Bindings_CrestEmission_8c56460232fde1e46ae90d905a00f908
                    {
                        float FaceSign;
                    };
                
                    void SG_CrestEmission_8c56460232fde1e46ae90d905a00f908(float Vector1_82EC3C0B, float3 Vector3_6C3F4D52, float3 Vector3_83F0ECB8, float3 Vector3_D6B78A76, float3 Vector3_703F2DD, float4 Vector4_5D0F731B, float Vector1_73B9F9E8, float3 Vector3_D4CABAFD, float Vector1_6C78E163, float3 Vector3_BFE779C0, half3 Vector3_71E7580E, TEXTURE2D_PARAM(Texture2D_BA141407, samplerTexture2D_BA141407), float4 Texture2D_BA141407_TexelSize, half Vector1_460D9038, half Vector1_D5CE42D3, half Vector1_AC9C2A2, half Vector1_8DBC6E14, half Vector1_EA8F2BEC, TEXTURE2D_PARAM(Texture2D_AC91C4C3, samplerTexture2D_AC91C4C3), float4 Texture2D_AC91C4C3_TexelSize, half Vector1_DB8E128A, half Vector1_CFFD6A53, float3 Vector3_32FB76B3, Bindings_CrestEmission_8c56460232fde1e46ae90d905a00f908 IN, out float3 EmittedLight_1)
                    {
                        Bindings_CrestIsUnderwater_52f7750f15e114937b54a0fd27f0d2f2 _CrestIsUnderwater_6DC72EB9;
                        _CrestIsUnderwater_6DC72EB9.FaceSign = IN.FaceSign;
                        float _CrestIsUnderwater_6DC72EB9_OutBoolean_1;
                        SG_CrestIsUnderwater_52f7750f15e114937b54a0fd27f0d2f2(_CrestIsUnderwater_6DC72EB9, _CrestIsUnderwater_6DC72EB9_OutBoolean_1);
                        float _Property_101786D0_Out_0 = Vector1_82EC3C0B;
                        float3 _Property_833375DE_Out_0 = Vector3_83F0ECB8;
                        float3 _Property_83DC9AD9_Out_0 = Vector3_703F2DD;
                        float4 _Property_83CEC55B_Out_0 = Vector4_5D0F731B;
                        float _Property_37502285_Out_0 = Vector1_73B9F9E8;
                        float3 _Property_5935620A_Out_0 = Vector3_D4CABAFD;
                        float _Property_AF5C7A5F_Out_0 = Vector1_6C78E163;
                        Bindings_CrestIsUnderwater_52f7750f15e114937b54a0fd27f0d2f2 _CrestIsUnderwater_76914462;
                        _CrestIsUnderwater_76914462.FaceSign = IN.FaceSign;
                        float _CrestIsUnderwater_76914462_OutBoolean_1;
                        SG_CrestIsUnderwater_52f7750f15e114937b54a0fd27f0d2f2(_CrestIsUnderwater_76914462, _CrestIsUnderwater_76914462_OutBoolean_1);
                        half3 _CustomFunction_F95A252D_SceneColour_5;
                        half _CustomFunction_F95A252D_SceneDistance_9;
                        half3 _CustomFunction_F95A252D_ScenePositionWS_10;
                        CrestNodeSceneColour_half(_Property_101786D0_Out_0, _Property_833375DE_Out_0, _Property_83DC9AD9_Out_0, _Property_83CEC55B_Out_0, _Property_37502285_Out_0, _Property_5935620A_Out_0, _Property_AF5C7A5F_Out_0, _CrestIsUnderwater_76914462_OutBoolean_1, _CustomFunction_F95A252D_SceneColour_5, _CustomFunction_F95A252D_SceneDistance_9, _CustomFunction_F95A252D_ScenePositionWS_10);
                        half _Split_550754E3_R_1 = _CustomFunction_F95A252D_ScenePositionWS_10[0];
                        half _Split_550754E3_G_2 = _CustomFunction_F95A252D_ScenePositionWS_10[1];
                        half _Split_550754E3_B_3 = _CustomFunction_F95A252D_ScenePositionWS_10[2];
                        half _Split_550754E3_A_4 = 0;
                        half2 _Vector2_B1CFC7F6_Out_0 = half2(_Split_550754E3_R_1, _Split_550754E3_B_3);
                        Bindings_CrestDrivenData_40572ccadf7d9704a83d283fb4c9f19d _CrestDrivenData_541C70CC;
                        half _CrestDrivenData_541C70CC_MeshScaleAlpha_1;
                        half _CrestDrivenData_541C70CC_LodDataTexelSize_8;
                        half _CrestDrivenData_541C70CC_GeometryGridSize_2;
                        half3 _CrestDrivenData_541C70CC_OceanPosScale0_3;
                        half3 _CrestDrivenData_541C70CC_OceanPosScale1_4;
                        half4 _CrestDrivenData_541C70CC_OceanParams0_5;
                        half4 _CrestDrivenData_541C70CC_OceanParams1_6;
                        half _CrestDrivenData_541C70CC_SliceIndex0_7;
                        SG_CrestDrivenData_40572ccadf7d9704a83d283fb4c9f19d(_CrestDrivenData_541C70CC, _CrestDrivenData_541C70CC_MeshScaleAlpha_1, _CrestDrivenData_541C70CC_LodDataTexelSize_8, _CrestDrivenData_541C70CC_GeometryGridSize_2, _CrestDrivenData_541C70CC_OceanPosScale0_3, _CrestDrivenData_541C70CC_OceanPosScale1_4, _CrestDrivenData_541C70CC_OceanParams0_5, _CrestDrivenData_541C70CC_OceanParams1_6, _CrestDrivenData_541C70CC_SliceIndex0_7);
                        float _Add_87784A87_Out_2;
                        Unity_Add_float(_CrestDrivenData_541C70CC_SliceIndex0_7, 1, _Add_87784A87_Out_2);
                        Bindings_CrestSampleOceanDataSingle_a667f031fd6a3dd42beee0ccc432233d _CrestSampleOceanDataSingle_5C7B7E58;
                        float3 _CrestSampleOceanDataSingle_5C7B7E58_Displacement_1;
                        float _CrestSampleOceanDataSingle_5C7B7E58_OceanWaterDepth_5;
                        float _CrestSampleOceanDataSingle_5C7B7E58_Foam_6;
                        float2 _CrestSampleOceanDataSingle_5C7B7E58_Shadow_7;
                        float2 _CrestSampleOceanDataSingle_5C7B7E58_Flow_8;
                        float _CrestSampleOceanDataSingle_5C7B7E58_SubSurfaceScattering_9;
                        SG_CrestSampleOceanDataSingle_a667f031fd6a3dd42beee0ccc432233d(_Vector2_B1CFC7F6_Out_0, _CrestDrivenData_541C70CC_OceanPosScale1_4, _CrestDrivenData_541C70CC_OceanParams1_6, _Add_87784A87_Out_2, _CrestSampleOceanDataSingle_5C7B7E58, _CrestSampleOceanDataSingle_5C7B7E58_Displacement_1, _CrestSampleOceanDataSingle_5C7B7E58_OceanWaterDepth_5, _CrestSampleOceanDataSingle_5C7B7E58_Foam_6, _CrestSampleOceanDataSingle_5C7B7E58_Shadow_7, _CrestSampleOceanDataSingle_5C7B7E58_Flow_8, _CrestSampleOceanDataSingle_5C7B7E58_SubSurfaceScattering_9);
                        Bindings_CrestOceanGlobals_d50a85284893ec447a25a093505a2120 _CrestOceanGlobals_FCFEE3C8;
                        float _CrestOceanGlobals_FCFEE3C8_CrestTime_1;
                        float _CrestOceanGlobals_FCFEE3C8_TexelsPerWave_2;
                        float3 _CrestOceanGlobals_FCFEE3C8_OceanCenterPosWorld_3;
                        float _CrestOceanGlobals_FCFEE3C8_SliceCount_4;
                        float _CrestOceanGlobals_FCFEE3C8_MeshScaleLerp_5;
                        SG_CrestOceanGlobals_d50a85284893ec447a25a093505a2120(_CrestOceanGlobals_FCFEE3C8, _CrestOceanGlobals_FCFEE3C8_CrestTime_1, _CrestOceanGlobals_FCFEE3C8_TexelsPerWave_2, _CrestOceanGlobals_FCFEE3C8_OceanCenterPosWorld_3, _CrestOceanGlobals_FCFEE3C8_SliceCount_4, _CrestOceanGlobals_FCFEE3C8_MeshScaleLerp_5);
                        float3 _Add_13827433_Out_2;
                        Unity_Add_float3(_CrestSampleOceanDataSingle_5C7B7E58_Displacement_1, _CrestOceanGlobals_FCFEE3C8_OceanCenterPosWorld_3, _Add_13827433_Out_2);
                        float _Split_8D03CDFB_R_1 = _Add_13827433_Out_2[0];
                        float _Split_8D03CDFB_G_2 = _Add_13827433_Out_2[1];
                        float _Split_8D03CDFB_B_3 = _Add_13827433_Out_2[2];
                        float _Split_8D03CDFB_A_4 = 0;
                        float3 _Property_6045F26_Out_0 = Vector3_6C3F4D52;
                        float3 _Property_5E1977C4_Out_0 = Vector3_BFE779C0;
                        half3 _Property_513534C6_Out_0 = Vector3_71E7580E;
                        half _Property_EF9152A2_Out_0 = Vector1_460D9038;
                        half _Property_3D2898B2_Out_0 = Vector1_D5CE42D3;
                        half _Property_35078BD5_Out_0 = Vector1_AC9C2A2;
                        half _Property_1B866598_Out_0 = Vector1_8DBC6E14;
                        half _Property_2016136C_Out_0 = Vector1_EA8F2BEC;
                        half _Property_A64ADC3_Out_0 = Vector1_DB8E128A;
                        half _Property_DDF09C09_Out_0 = Vector1_CFFD6A53;
                        Bindings_CrestIsUnderwater_52f7750f15e114937b54a0fd27f0d2f2 _CrestIsUnderwater_2020F895;
                        _CrestIsUnderwater_2020F895.FaceSign = IN.FaceSign;
                        float _CrestIsUnderwater_2020F895_OutBoolean_1;
                        SG_CrestIsUnderwater_52f7750f15e114937b54a0fd27f0d2f2(_CrestIsUnderwater_2020F895, _CrestIsUnderwater_2020F895_OutBoolean_1);
                        float3 _CustomFunction_2F6A103B_SceneColourOut_8;
                        CrestNodeApplyCaustics_float(_CustomFunction_F95A252D_SceneColour_5, _CustomFunction_F95A252D_ScenePositionWS_10, _Split_8D03CDFB_G_2, _Property_6045F26_Out_0, _Property_5E1977C4_Out_0, _Property_513534C6_Out_0, _CustomFunction_F95A252D_SceneDistance_9, Texture2D_BA141407, _Property_EF9152A2_Out_0, _Property_3D2898B2_Out_0, _Property_35078BD5_Out_0, _Property_1B866598_Out_0, _Property_2016136C_Out_0, Texture2D_AC91C4C3, _Property_A64ADC3_Out_0, _Property_DDF09C09_Out_0, _CrestIsUnderwater_2020F895_OutBoolean_1, _CustomFunction_2F6A103B_SceneColourOut_8);
                        #if defined(CREST_CAUSTICS_ON)
                        float3 _CAUSTICS_6D445714_Out_0 = _CustomFunction_2F6A103B_SceneColourOut_8;
                        #else
                        float3 _CAUSTICS_6D445714_Out_0 = _CustomFunction_F95A252D_SceneColour_5;
                        #endif
                        float3 _Property_78DFA568_Out_0 = Vector3_83F0ECB8;
                        float3 _Property_54BACED9_Out_0 = Vector3_32FB76B3;
                        float3 _Add_954C4741_Out_2;
                        Unity_Add_float3(_Property_78DFA568_Out_0, _Property_54BACED9_Out_0, _Add_954C4741_Out_2);
                        float3 _Property_D76B9A0B_Out_0 = Vector3_6C3F4D52;
                        float3 _Multiply_C51E63DE_Out_2;
                        Unity_Multiply_float((_CustomFunction_F95A252D_SceneDistance_9.xxx), _Property_D76B9A0B_Out_0, _Multiply_C51E63DE_Out_2);
                        float3 _Negate_ADFF8761_Out_1;
                        Unity_Negate_float3(_Multiply_C51E63DE_Out_2, _Negate_ADFF8761_Out_1);
                        float3 _Exponential_6FCB9AC3_Out_1;
                        Unity_Exponential_float3(_Negate_ADFF8761_Out_1, _Exponential_6FCB9AC3_Out_1);
                        float3 _OneMinus_9962618_Out_1;
                        Unity_OneMinus_float3(_Exponential_6FCB9AC3_Out_1, _OneMinus_9962618_Out_1);
                        float3 _Lerp_E9270AE8_Out_3;
                        Unity_Lerp_float3(_CAUSTICS_6D445714_Out_0, _Add_954C4741_Out_2, _OneMinus_9962618_Out_1, _Lerp_E9270AE8_Out_3);
                        float3 _Branch_D043DFC1_Out_3;
                        Unity_Branch_float3(_CrestIsUnderwater_6DC72EB9_OutBoolean_1, _CAUSTICS_6D445714_Out_0, _Lerp_E9270AE8_Out_3, _Branch_D043DFC1_Out_3);
                        EmittedLight_1 = _Branch_D043DFC1_Out_3;
                    }
                
                    void Unity_Reflection_float3(float3 In, float3 Normal, out float3 Out)
                    {
                        Out = reflect(In, Normal);
                    }
                
                    void Unity_Divide_float(float A, float B, out float Out)
                    {
                        Out = A / B;
                    }
                
                    void Unity_Saturate_float(float In, out float Out)
                    {
                        Out = saturate(In);
                    }
                
                    void Unity_Power_float(float A, float B, out float Out)
                    {
                        Out = pow(A, B);
                    }
                
                    void Unity_Lerp_float(float A, float B, float T, out float Out)
                    {
                        Out = lerp(A, B, T);
                    }
                
                    void Unity_Modulo_float(float A, float B, out float Out)
                    {
                        Out = fmod(A, B);
                    }
                
                    void Unity_Multiply_float(float2 A, float2 B, out float2 Out)
                    {
                        Out = A * B;
                    }
                
                    void Unity_Comparison_Greater_float(float A, float B, out float Out)
                    {
                        Out = A > B ? 1 : 0;
                    }
                
                    void Unity_Subtract_float(float A, float B, out float Out)
                    {
                        Out = A - B;
                    }
                
                    void Unity_Branch_float(float Predicate, float True, float False, out float Out)
                    {
                        Out = Predicate ? True : False;
                    }
                
                    struct Bindings_CrestFlow_f9c7f2c7774dd4b5bad87d6a350b47f1
                    {
                    };
                
                    void SG_CrestFlow_f9c7f2c7774dd4b5bad87d6a350b47f1(float2 Vector2_B562EFB1, float2 Vector2_83AA31A8, Bindings_CrestFlow_f9c7f2c7774dd4b5bad87d6a350b47f1 IN, out float2 DisplacedA_1, out float WeightA_3, out float2 DisplacedB_2, out float WeightB_4)
                    {
                        float2 _Property_DB670C03_Out_0 = Vector2_83AA31A8;
                        float2 _Property_6ED023E0_Out_0 = Vector2_B562EFB1;
                        Bindings_CrestOceanGlobals_d50a85284893ec447a25a093505a2120 _CrestOceanGlobals_40DCBE3E;
                        float _CrestOceanGlobals_40DCBE3E_CrestTime_1;
                        float _CrestOceanGlobals_40DCBE3E_TexelsPerWave_2;
                        float3 _CrestOceanGlobals_40DCBE3E_OceanCenterPosWorld_3;
                        float _CrestOceanGlobals_40DCBE3E_SliceCount_4;
                        float _CrestOceanGlobals_40DCBE3E_MeshScaleLerp_5;
                        SG_CrestOceanGlobals_d50a85284893ec447a25a093505a2120(_CrestOceanGlobals_40DCBE3E, _CrestOceanGlobals_40DCBE3E_CrestTime_1, _CrestOceanGlobals_40DCBE3E_TexelsPerWave_2, _CrestOceanGlobals_40DCBE3E_OceanCenterPosWorld_3, _CrestOceanGlobals_40DCBE3E_SliceCount_4, _CrestOceanGlobals_40DCBE3E_MeshScaleLerp_5);
                        float _Modulo_CC9BC285_Out_2;
                        Unity_Modulo_float(_CrestOceanGlobals_40DCBE3E_CrestTime_1, 2, _Modulo_CC9BC285_Out_2);
                        float2 _Multiply_821FCA56_Out_2;
                        Unity_Multiply_float(_Property_6ED023E0_Out_0, (_Modulo_CC9BC285_Out_2.xx), _Multiply_821FCA56_Out_2);
                        float2 _Subtract_273125FF_Out_2;
                        Unity_Subtract_float2(_Property_DB670C03_Out_0, _Multiply_821FCA56_Out_2, _Subtract_273125FF_Out_2);
                        float2 _Property_C2A19C1C_Out_0 = Vector2_83AA31A8;
                        #if defined(CREST_FLOW_ON)
                        float2 _FLOW_963D27CC_Out_0 = _Subtract_273125FF_Out_2;
                        #else
                        float2 _FLOW_963D27CC_Out_0 = _Property_C2A19C1C_Out_0;
                        #endif
                        float _Comparison_67E5EC91_Out_2;
                        Unity_Comparison_Greater_float(_Modulo_CC9BC285_Out_2, 1, _Comparison_67E5EC91_Out_2);
                        float _Subtract_289E2A26_Out_2;
                        Unity_Subtract_float(2, _Modulo_CC9BC285_Out_2, _Subtract_289E2A26_Out_2);
                        float _Branch_57B6378F_Out_3;
                        Unity_Branch_float(_Comparison_67E5EC91_Out_2, _Subtract_289E2A26_Out_2, _Modulo_CC9BC285_Out_2, _Branch_57B6378F_Out_3);
                        #if defined(CREST_FLOW_ON)
                        float _FLOW_AB68F2D0_Out_0 = _Branch_57B6378F_Out_3;
                        #else
                        float _FLOW_AB68F2D0_Out_0 = 0;
                        #endif
                        float2 _Property_CF607B59_Out_0 = Vector2_83AA31A8;
                        float2 _Property_8EFEBF1A_Out_0 = Vector2_B562EFB1;
                        float _Add_43F824C8_Out_2;
                        Unity_Add_float(_CrestOceanGlobals_40DCBE3E_CrestTime_1, 1, _Add_43F824C8_Out_2);
                        float _Modulo_2FF365BF_Out_2;
                        Unity_Modulo_float(_Add_43F824C8_Out_2, 2, _Modulo_2FF365BF_Out_2);
                        float2 _Multiply_28DD98EB_Out_2;
                        Unity_Multiply_float(_Property_8EFEBF1A_Out_0, (_Modulo_2FF365BF_Out_2.xx), _Multiply_28DD98EB_Out_2);
                        float2 _Subtract_1C7014D5_Out_2;
                        Unity_Subtract_float2(_Property_CF607B59_Out_0, _Multiply_28DD98EB_Out_2, _Subtract_1C7014D5_Out_2);
                        #if defined(CREST_FLOW_ON)
                        float2 _FLOW_D38C09E5_Out_0 = _Subtract_1C7014D5_Out_2;
                        #else
                        float2 _FLOW_D38C09E5_Out_0 = _Property_C2A19C1C_Out_0;
                        #endif
                        float _Subtract_563D329D_Out_2;
                        Unity_Subtract_float(1, _Branch_57B6378F_Out_3, _Subtract_563D329D_Out_2);
                        #if defined(CREST_FLOW_ON)
                        float _FLOW_CB3A12C4_Out_0 = _Subtract_563D329D_Out_2;
                        #else
                        float _FLOW_CB3A12C4_Out_0 = 0;
                        #endif
                        DisplacedA_1 = _FLOW_963D27CC_Out_0;
                        WeightA_3 = _FLOW_AB68F2D0_Out_0;
                        DisplacedB_2 = _FLOW_D38C09E5_Out_0;
                        WeightB_4 = _FLOW_CB3A12C4_Out_0;
                    }
                
                    // dfcd16226d95b1a66d66c0c937cc47e9
                    #include "ShadergraphFramework/CrestNodeFoam.hlsl"
                
                    void Unity_Multiply_float(float A, float B, out float Out)
                    {
                        Out = A * B;
                    }
                
                    struct Bindings_CrestFoamWithFlow_3a6c22dceac0847a2a31cb05a577b963
                    {
                    };
                
                    void SG_CrestFoamWithFlow_3a6c22dceac0847a2a31cb05a577b963(TEXTURE2D_PARAM(Texture2D_D9E7A343, samplerTexture2D_D9E7A343), float4 Texture2D_D9E7A343_TexelSize, float2 Vector2_C4E9ED58, float Vector1_7492C815, float Vector1_33DC93D, float Vector1_3439FC0A, float Vector1_FB133018, float Vector1_7CFAFAC1, float Vector1_53F45327, float4 Vector4_2FA9AEA5, float4 Vector4_FD283A50, float Vector1_D6DA4100, float Vector1_13F2A8C8, float2 Vector2_28A2C5B9, float3 Vector3_3AD012AA, float3 Vector3_E4AC4432, float Vector1_3E452608, float Vector1_D80F067A, float2 Vector2_5AD72ED, Bindings_CrestFoamWithFlow_3a6c22dceac0847a2a31cb05a577b963 IN, out float3 Albedo_1, out float3 NormalTS_2, out float3 Emission_3, out float Smoothness_4)
                    {
                        float2 _Property_B71BFD9B_Out_0 = Vector2_C4E9ED58;
                        float _Property_7C437CB0_Out_0 = Vector1_7492C815;
                        float _Property_BD5882DA_Out_0 = Vector1_33DC93D;
                        float _Property_3EF95B2A_Out_0 = Vector1_3439FC0A;
                        float _Property_38B853C9_Out_0 = Vector1_FB133018;
                        float _Property_8F25A4DF_Out_0 = Vector1_7CFAFAC1;
                        float _Property_561CFE5E_Out_0 = Vector1_53F45327;
                        float4 _Property_A1E249A2_Out_0 = Vector4_2FA9AEA5;
                        float4 _Property_9A7BA0BB_Out_0 = Vector4_FD283A50;
                        float _Property_4C4E1C83_Out_0 = Vector1_D6DA4100;
                        float _Property_D81DA51E_Out_0 = Vector1_13F2A8C8;
                        float2 _Property_4BF1D6EA_Out_0 = Vector2_5AD72ED;
                        float2 _Property_DEC66EB9_Out_0 = Vector2_28A2C5B9;
                        Bindings_CrestFlow_f9c7f2c7774dd4b5bad87d6a350b47f1 _CrestFlow_B4596B;
                        float2 _CrestFlow_B4596B_DisplacedA_1;
                        float _CrestFlow_B4596B_WeightA_3;
                        float2 _CrestFlow_B4596B_DisplacedB_2;
                        float _CrestFlow_B4596B_WeightB_4;
                        SG_CrestFlow_f9c7f2c7774dd4b5bad87d6a350b47f1(_Property_4BF1D6EA_Out_0, _Property_DEC66EB9_Out_0, _CrestFlow_B4596B, _CrestFlow_B4596B_DisplacedA_1, _CrestFlow_B4596B_WeightA_3, _CrestFlow_B4596B_DisplacedB_2, _CrestFlow_B4596B_WeightB_4);
                        float _Property_99DCA7A4_Out_0 = Vector1_D80F067A;
                        float3 _Property_EB1DCAFB_Out_0 = Vector3_3AD012AA;
                        float3 _Property_A2BD94BA_Out_0 = Vector3_E4AC4432;
                        float _Property_7BFA2B51_Out_0 = Vector1_3E452608;
                        half3 _CustomFunction_2B0D0960_Albedo_8;
                        half3 _CustomFunction_2B0D0960_NormalTS_7;
                        half3 _CustomFunction_2B0D0960_Emission_9;
                        half _CustomFunction_2B0D0960_Smoothness_10;
                        CrestNodeFoam_half(Texture2D_D9E7A343, _Property_B71BFD9B_Out_0, _Property_7C437CB0_Out_0, _Property_BD5882DA_Out_0, _Property_3EF95B2A_Out_0, _Property_38B853C9_Out_0, _Property_8F25A4DF_Out_0, _Property_561CFE5E_Out_0, _Property_A1E249A2_Out_0, _Property_9A7BA0BB_Out_0, _Property_4C4E1C83_Out_0, _Property_D81DA51E_Out_0, _CrestFlow_B4596B_DisplacedA_1, _Property_99DCA7A4_Out_0, _Property_EB1DCAFB_Out_0, _Property_A2BD94BA_Out_0, _Property_7BFA2B51_Out_0, _CustomFunction_2B0D0960_Albedo_8, _CustomFunction_2B0D0960_NormalTS_7, _CustomFunction_2B0D0960_Emission_9, _CustomFunction_2B0D0960_Smoothness_10);
                        #if defined(CREST_FOAM_ON)
                        float3 _FOAM_384819B4_Out_0 = _CustomFunction_2B0D0960_Albedo_8;
                        #else
                        float3 _FOAM_384819B4_Out_0 = float3(0, 0, 0);
                        #endif
                        float3 _Multiply_278EF57C_Out_2;
                        Unity_Multiply_float(_FOAM_384819B4_Out_0, (_CrestFlow_B4596B_WeightA_3.xxx), _Multiply_278EF57C_Out_2);
                        float2 _Property_6338038D_Out_0 = Vector2_C4E9ED58;
                        float _Property_C8444C80_Out_0 = Vector1_7492C815;
                        float _Property_3DF87814_Out_0 = Vector1_33DC93D;
                        float _Property_589C4301_Out_0 = Vector1_3439FC0A;
                        float _Property_E9D88423_Out_0 = Vector1_FB133018;
                        float _Property_366CB823_Out_0 = Vector1_7CFAFAC1;
                        float _Property_1F5469F5_Out_0 = Vector1_53F45327;
                        float4 _Property_920AF23F_Out_0 = Vector4_2FA9AEA5;
                        float4 _Property_37F6AC84_Out_0 = Vector4_FD283A50;
                        float _Property_E7E25E74_Out_0 = Vector1_D6DA4100;
                        float _Property_FA2913B0_Out_0 = Vector1_13F2A8C8;
                        float _Property_BFBF141C_Out_0 = Vector1_D80F067A;
                        float3 _Property_4E6AF5FF_Out_0 = Vector3_3AD012AA;
                        float3 _Property_A2DA52BE_Out_0 = Vector3_E4AC4432;
                        float _Property_9DE15AC9_Out_0 = Vector1_3E452608;
                        half3 _CustomFunction_16EDEEAC_Albedo_8;
                        half3 _CustomFunction_16EDEEAC_NormalTS_7;
                        half3 _CustomFunction_16EDEEAC_Emission_9;
                        half _CustomFunction_16EDEEAC_Smoothness_10;
                        CrestNodeFoam_half(Texture2D_D9E7A343, _Property_6338038D_Out_0, _Property_C8444C80_Out_0, _Property_3DF87814_Out_0, _Property_589C4301_Out_0, _Property_E9D88423_Out_0, _Property_366CB823_Out_0, _Property_1F5469F5_Out_0, _Property_920AF23F_Out_0, _Property_37F6AC84_Out_0, _Property_E7E25E74_Out_0, _Property_FA2913B0_Out_0, _CrestFlow_B4596B_DisplacedB_2, _Property_BFBF141C_Out_0, _Property_4E6AF5FF_Out_0, _Property_A2DA52BE_Out_0, _Property_9DE15AC9_Out_0, _CustomFunction_16EDEEAC_Albedo_8, _CustomFunction_16EDEEAC_NormalTS_7, _CustomFunction_16EDEEAC_Emission_9, _CustomFunction_16EDEEAC_Smoothness_10);
                        #if defined(CREST_FOAM_ON)
                        float3 _FOAM_E9D68DEC_Out_0 = _CustomFunction_16EDEEAC_Albedo_8;
                        #else
                        float3 _FOAM_E9D68DEC_Out_0 = float3(0, 0, 0);
                        #endif
                        float3 _Multiply_BCF80692_Out_2;
                        Unity_Multiply_float((_CrestFlow_B4596B_WeightB_4.xxx), _FOAM_E9D68DEC_Out_0, _Multiply_BCF80692_Out_2);
                        float3 _Add_48FC5557_Out_2;
                        Unity_Add_float3(_Multiply_278EF57C_Out_2, _Multiply_BCF80692_Out_2, _Add_48FC5557_Out_2);
                        #if defined(CREST_FLOW_ON)
                        float3 _FLOW_1260C700_Out_0 = _Add_48FC5557_Out_2;
                        #else
                        float3 _FLOW_1260C700_Out_0 = _FOAM_384819B4_Out_0;
                        #endif
                        #if defined(CREST_FOAM_ON)
                        float3 _FOAM_14E9F7C1_Out_0 = _CustomFunction_2B0D0960_NormalTS_7;
                        #else
                        float3 _FOAM_14E9F7C1_Out_0 = _Property_EB1DCAFB_Out_0;
                        #endif
                        float3 _Multiply_FAB0278F_Out_2;
                        Unity_Multiply_float(_FOAM_14E9F7C1_Out_0, (_CrestFlow_B4596B_WeightA_3.xxx), _Multiply_FAB0278F_Out_2);
                        #if defined(CREST_FOAM_ON)
                        float3 _FOAM_9C6E353_Out_0 = _CustomFunction_16EDEEAC_NormalTS_7;
                        #else
                        float3 _FOAM_9C6E353_Out_0 = _Property_4E6AF5FF_Out_0;
                        #endif
                        float3 _Multiply_CE1E9F74_Out_2;
                        Unity_Multiply_float((_CrestFlow_B4596B_WeightB_4.xxx), _FOAM_9C6E353_Out_0, _Multiply_CE1E9F74_Out_2);
                        float3 _Add_3A03EC0D_Out_2;
                        Unity_Add_float3(_Multiply_FAB0278F_Out_2, _Multiply_CE1E9F74_Out_2, _Add_3A03EC0D_Out_2);
                        #if defined(CREST_FLOW_ON)
                        float3 _FLOW_2D8BA180_Out_0 = _Add_3A03EC0D_Out_2;
                        #else
                        float3 _FLOW_2D8BA180_Out_0 = _FOAM_14E9F7C1_Out_0;
                        #endif
                        #if defined(CREST_FOAM_ON)
                        float3 _FOAM_D8E821F0_Out_0 = _CustomFunction_2B0D0960_Emission_9;
                        #else
                        float3 _FOAM_D8E821F0_Out_0 = _Property_A2BD94BA_Out_0;
                        #endif
                        float3 _Multiply_716921DC_Out_2;
                        Unity_Multiply_float(_FOAM_D8E821F0_Out_0, (_CrestFlow_B4596B_WeightA_3.xxx), _Multiply_716921DC_Out_2);
                        #if defined(CREST_FOAM_ON)
                        float3 _FOAM_9468CE46_Out_0 = _CustomFunction_16EDEEAC_Emission_9;
                        #else
                        float3 _FOAM_9468CE46_Out_0 = _Property_A2DA52BE_Out_0;
                        #endif
                        float3 _Multiply_A0DE696C_Out_2;
                        Unity_Multiply_float((_CrestFlow_B4596B_WeightB_4.xxx), _FOAM_9468CE46_Out_0, _Multiply_A0DE696C_Out_2);
                        float3 _Add_2EBEBFCF_Out_2;
                        Unity_Add_float3(_Multiply_716921DC_Out_2, _Multiply_A0DE696C_Out_2, _Add_2EBEBFCF_Out_2);
                        #if defined(CREST_FLOW_ON)
                        float3 _FLOW_225158E3_Out_0 = _Add_2EBEBFCF_Out_2;
                        #else
                        float3 _FLOW_225158E3_Out_0 = _FOAM_D8E821F0_Out_0;
                        #endif
                        #if defined(CREST_FOAM_ON)
                        float _FOAM_F0F633C_Out_0 = _CustomFunction_2B0D0960_Smoothness_10;
                        #else
                        float _FOAM_F0F633C_Out_0 = _Property_7BFA2B51_Out_0;
                        #endif
                        float _Multiply_C03E7AFC_Out_2;
                        Unity_Multiply_float(_FOAM_F0F633C_Out_0, _CrestFlow_B4596B_WeightA_3, _Multiply_C03E7AFC_Out_2);
                        #if defined(CREST_FOAM_ON)
                        float _FOAM_AB9E199D_Out_0 = _CustomFunction_16EDEEAC_Smoothness_10;
                        #else
                        float _FOAM_AB9E199D_Out_0 = _Property_9DE15AC9_Out_0;
                        #endif
                        float _Multiply_8F8BE539_Out_2;
                        Unity_Multiply_float(_CrestFlow_B4596B_WeightB_4, _FOAM_AB9E199D_Out_0, _Multiply_8F8BE539_Out_2);
                        float _Add_7629FC1B_Out_2;
                        Unity_Add_float(_Multiply_C03E7AFC_Out_2, _Multiply_8F8BE539_Out_2, _Add_7629FC1B_Out_2);
                        #if defined(CREST_FLOW_ON)
                        float _FLOW_386EA56_Out_0 = _Add_7629FC1B_Out_2;
                        #else
                        float _FLOW_386EA56_Out_0 = _FOAM_F0F633C_Out_0;
                        #endif
                        Albedo_1 = _FLOW_1260C700_Out_0;
                        NormalTS_2 = _FLOW_2D8BA180_Out_0;
                        Emission_3 = _FLOW_225158E3_Out_0;
                        Smoothness_4 = _FLOW_386EA56_Out_0;
                    }
                
                    struct Bindings_CrestOceanPixel_6f6706d805d8e8649adddaaa94260269
                    {
                        float3 WorldSpaceViewDirection;
                        float3 ViewSpacePosition;
                        float3 AbsoluteWorldSpacePosition;
                        float4 ScreenPosition;
                        float FaceSign;
                    };
                
                    void SG_CrestOceanPixel_6f6706d805d8e8649adddaaa94260269(TEXTURE2D_PARAM(Texture2D_BE500045, samplerTexture2D_BE500045), float4 Texture2D_BE500045_TexelSize, float2 Vector2_2F51BFFE, float Vector1_1CEB35D8, float Vector1_43921196, float Vector1_E301593B, float Vector1_958E8942, float Vector1_2ED4C943, float Vector1_BF3AF964, float3 Vector3_EEEBAAB5, float Vector1_D3410E3, float Vector1_1EC1FE35, float2 Vector2_9C73A0C6, float Vector1_B01E1A6A, float Vector1_D74C6609, float Vector1_B61034CA, float2 Vector2_AE8873FA, float2 Vector2_69CC43DC, float Vector1_255AB964, float Vector1_47308CC2, float Vector1_26549044, float Vector1_177E111C, float Vector1_33255829, TEXTURE2D_PARAM(Texture2D_40AB1455, samplerTexture2D_40AB1455), float4 Texture2D_40AB1455_TexelSize, float Vector1_23A72EC7, float Vector1_F406EE17, float4 Vector4_2F6E352, float3 Vector3_57B74D6A, float4 Vector4_B3AD63B4, float Vector1_FA688590, float Vector1_9835666E, float Vector1_B331E24E, float Vector1_951CF2DF, float4 Vector4_ADCA8891, float Vector1_2E8E2C59, float Vector1_9BD9C342, float3 Vector3_8228B74C, float3 Vector3_B2D6AD84, float3 Vector3_D2C93D25, TEXTURE2D_PARAM(Texture2D_EB8C8549, samplerTexture2D_EB8C8549), float4 Texture2D_EB8C8549_TexelSize, float Vector1_9AE39B77, half Vector1_1B073674, float Vector1_C885385, float Vector1_90CEE6B8, float Vector1_E27586E2, TEXTURE2D_PARAM(Texture2D_DA8A756A, samplerTexture2D_DA8A756A), float4 Texture2D_DA8A756A_TexelSize, float Vector1_C96A6500, float Vector1_1AD36684, float Vector1_9E87174F, float Vector1_80D0DF9E, float Vector1_96F28EC5, Bindings_CrestOceanPixel_6f6706d805d8e8649adddaaa94260269 IN, out float3 Albedo_2, out float3 NormalTS_3, out float3 Emission_4, out float Smoothness_5, out float Specular_6)
                    {
                        float2 _Property_B9BA8901_Out_0 = Vector2_2F51BFFE;
                        float _Property_6AEC3552_Out_0 = Vector1_1CEB35D8;
                        float _Property_76EA797B_Out_0 = Vector1_43921196;
                        float _Property_83C2E998_Out_0 = Vector1_E301593B;
                        float _Property_705FD8D9_Out_0 = Vector1_958E8942;
                        float _Property_735B5A53_Out_0 = Vector1_2ED4C943;
                        float _Property_EB4C03CF_Out_0 = Vector1_BF3AF964;
                        Bindings_CrestDrivenData_40572ccadf7d9704a83d283fb4c9f19d _CrestDrivenData_6635D6E9;
                        half _CrestDrivenData_6635D6E9_MeshScaleAlpha_1;
                        half _CrestDrivenData_6635D6E9_LodDataTexelSize_8;
                        half _CrestDrivenData_6635D6E9_GeometryGridSize_2;
                        half3 _CrestDrivenData_6635D6E9_OceanPosScale0_3;
                        half3 _CrestDrivenData_6635D6E9_OceanPosScale1_4;
                        half4 _CrestDrivenData_6635D6E9_OceanParams0_5;
                        half4 _CrestDrivenData_6635D6E9_OceanParams1_6;
                        half _CrestDrivenData_6635D6E9_SliceIndex0_7;
                        SG_CrestDrivenData_40572ccadf7d9704a83d283fb4c9f19d(_CrestDrivenData_6635D6E9, _CrestDrivenData_6635D6E9_MeshScaleAlpha_1, _CrestDrivenData_6635D6E9_LodDataTexelSize_8, _CrestDrivenData_6635D6E9_GeometryGridSize_2, _CrestDrivenData_6635D6E9_OceanPosScale0_3, _CrestDrivenData_6635D6E9_OceanPosScale1_4, _CrestDrivenData_6635D6E9_OceanParams0_5, _CrestDrivenData_6635D6E9_OceanParams1_6, _CrestDrivenData_6635D6E9_SliceIndex0_7);
                        float _Property_EE920C2C_Out_0 = Vector1_B61034CA;
                        float _Property_8BC6F5AD_Out_0 = Vector1_B01E1A6A;
                        float2 _Property_A2CEB215_Out_0 = Vector2_9C73A0C6;
                        float _Property_A636CE7E_Out_0 = Vector1_23A72EC7;
                        float _Property_C1531E4C_Out_0 = Vector1_F406EE17;
                        float _Property_C7723E93_Out_0 = Vector1_B01E1A6A;
                        float2 _Property_94C56295_Out_0 = Vector2_9C73A0C6;
                        float2 _Property_E65AA85_Out_0 = Vector2_69CC43DC;
                        float3 _Normalize_3574419A_Out_1;
                        Unity_Normalize_float3(IN.WorldSpaceViewDirection, _Normalize_3574419A_Out_1);
                        float _Property_5A245779_Out_0 = Vector1_96F28EC5;
                        Bindings_CrestComputeNormal_61b9efc6612ab3b4f84174344af5e12a _CrestComputeNormal_6DAE4B39;
                        _CrestComputeNormal_6DAE4B39.FaceSign = IN.FaceSign;
                        half3 _CrestComputeNormal_6DAE4B39_Normal_1;
                        SG_CrestComputeNormal_61b9efc6612ab3b4f84174344af5e12a(_CrestDrivenData_6635D6E9_OceanPosScale0_3, _CrestDrivenData_6635D6E9_OceanPosScale1_4, _CrestDrivenData_6635D6E9_OceanParams0_5, _CrestDrivenData_6635D6E9_OceanParams1_6, _CrestDrivenData_6635D6E9_SliceIndex0_7, TEXTURE2D_ARGS(Texture2D_40AB1455, samplerTexture2D_40AB1455), Texture2D_40AB1455_TexelSize, _Property_A636CE7E_Out_0, _Property_C1531E4C_Out_0, _Property_C7723E93_Out_0, _Property_94C56295_Out_0, _Property_E65AA85_Out_0, _Normalize_3574419A_Out_1, _Property_5A245779_Out_0, _CrestComputeNormal_6DAE4B39, _CrestComputeNormal_6DAE4B39_Normal_1);
                        Bindings_CrestIsUnderwater_52f7750f15e114937b54a0fd27f0d2f2 _CrestIsUnderwater_5AB8F4C9;
                        _CrestIsUnderwater_5AB8F4C9.FaceSign = IN.FaceSign;
                        float _CrestIsUnderwater_5AB8F4C9_OutBoolean_1;
                        SG_CrestIsUnderwater_52f7750f15e114937b54a0fd27f0d2f2(_CrestIsUnderwater_5AB8F4C9, _CrestIsUnderwater_5AB8F4C9_OutBoolean_1);
                        float _Property_FCAEBD15_Out_0 = Vector1_9E87174F;
                        float _Property_2D376CE_Out_0 = Vector1_80D0DF9E;
                        Bindings_CrestFresnel_c6f6c13c4fdb04e42b427e4c2610d3c9 _CrestFresnel_FD3200EB;
                        _CrestFresnel_FD3200EB.FaceSign = IN.FaceSign;
                        float _CrestFresnel_FD3200EB_LightTransmitted_1;
                        float _CrestFresnel_FD3200EB_LightReflected_2;
                        SG_CrestFresnel_c6f6c13c4fdb04e42b427e4c2610d3c9(_CrestComputeNormal_6DAE4B39_Normal_1, _Normalize_3574419A_Out_1, _Property_FCAEBD15_Out_0, _Property_2D376CE_Out_0, _CrestFresnel_FD3200EB, _CrestFresnel_FD3200EB_LightTransmitted_1, _CrestFresnel_FD3200EB_LightReflected_2);
                        float _Property_2599690E_Out_0 = Vector1_9BD9C342;
                        float3 _Property_A57CAD6E_Out_0 = Vector3_8228B74C;
                        float4 _Property_479D2E13_Out_0 = Vector4_2F6E352;
                        float3 _Property_F631E4E0_Out_0 = Vector3_57B74D6A;
                        float4 _Property_D9155CD7_Out_0 = Vector4_B3AD63B4;
                        float _Property_D8B923AB_Out_0 = Vector1_FA688590;
                        float _Property_F7F4791A_Out_0 = Vector1_9835666E;
                        float _Property_2ABF6C61_Out_0 = Vector1_B331E24E;
                        float _Property_22A5FA38_Out_0 = Vector1_951CF2DF;
                        float4 _Property_C1E8071C_Out_0 = Vector4_ADCA8891;
                        float _Property_9AD6466F_Out_0 = Vector1_2E8E2C59;
                        float _Property_4D8B881_Out_0 = Vector1_D74C6609;
                        float2 _Property_5A879B44_Out_0 = Vector2_AE8873FA;
                        float _Property_41C453D9_Out_0 = Vector1_255AB964;
                        Bindings_CrestAmbientLight_a6ec89b3ca0ab4e98b300ec3ba0e6013 _CrestAmbientLight_5A61E85E;
                        half3 _CrestAmbientLight_5A61E85E_Color_1;
                        SG_CrestAmbientLight_a6ec89b3ca0ab4e98b300ec3ba0e6013(_CrestAmbientLight_5A61E85E, _CrestAmbientLight_5A61E85E_Color_1);
                        float3 _Property_13E6764D_Out_0 = Vector3_B2D6AD84;
                        float3 _Property_2FA7DB0_Out_0 = Vector3_D2C93D25;
                        Bindings_CrestVolumeLighting_e9ed6e11710a50640bb4b811d0fa84f2 _CrestVolumeLighting_5A1A391;
                        half3 _CrestVolumeLighting_5A1A391_VolumeLighting_1;
                        SG_CrestVolumeLighting_e9ed6e11710a50640bb4b811d0fa84f2(_Property_479D2E13_Out_0, _Property_F631E4E0_Out_0, _Property_D9155CD7_Out_0, _Property_D8B923AB_Out_0, _Property_F7F4791A_Out_0, _Property_2ABF6C61_Out_0, _Property_22A5FA38_Out_0, _Property_C1E8071C_Out_0, _Property_9AD6466F_Out_0, _Property_4D8B881_Out_0, _Property_5A879B44_Out_0, _Property_41C453D9_Out_0, _Normalize_3574419A_Out_1, IN.AbsoluteWorldSpacePosition, _CrestAmbientLight_5A61E85E_Color_1, _Property_13E6764D_Out_0, _Property_2FA7DB0_Out_0, _CrestVolumeLighting_5A1A391, _CrestVolumeLighting_5A1A391_VolumeLighting_1);
                        float4 _ScreenPosition_42FE0E3E_Out_0 = float4(IN.ScreenPosition.xy / IN.ScreenPosition.w, 0, 0);
                        float _Split_66230827_R_1 = IN.ViewSpacePosition[0];
                        float _Split_66230827_G_2 = IN.ViewSpacePosition[1];
                        float _Split_66230827_B_3 = IN.ViewSpacePosition[2];
                        float _Split_66230827_A_4 = 0;
                        float _Negate_5F3C7D3D_Out_1;
                        Unity_Negate_float(_Split_66230827_B_3, _Negate_5F3C7D3D_Out_1);
                        float3 _SceneColor_61626E7C_Out_1;
                        Unity_SceneColor_float(float4(IN.ScreenPosition.xy / IN.ScreenPosition.w, 0, 0), _SceneColor_61626E7C_Out_1);
                        float _SceneDepth_FD35F7D9_Out_1;
                        Unity_SceneDepth_Eye_float(float4(IN.ScreenPosition.xy / IN.ScreenPosition.w, 0, 0), _SceneDepth_FD35F7D9_Out_1);
                        float3 _Property_4DD76B1_Out_0 = Vector3_D2C93D25;
                        float3 _Property_DC1C088D_Out_0 = Vector3_B2D6AD84;
                        float _Property_8BC39E79_Out_0 = Vector1_9AE39B77;
                        half _Property_683CAF2_Out_0 = Vector1_1B073674;
                        float _Property_B4B35559_Out_0 = Vector1_C885385;
                        float _Property_E694F888_Out_0 = Vector1_90CEE6B8;
                        float _Property_50A87698_Out_0 = Vector1_E27586E2;
                        float _Property_8BC9D755_Out_0 = Vector1_C96A6500;
                        float _Property_6F95DFBF_Out_0 = Vector1_1AD36684;
                        float3 _Property_4729CD24_Out_0 = Vector3_EEEBAAB5;
                        float _Property_C36E9B6_Out_0 = Vector1_D3410E3;
                        float _Property_2C173F60_Out_0 = Vector1_1EC1FE35;
                        float2 _Property_4258AA49_Out_0 = Vector2_2F51BFFE;
                        float _Property_3FC34BC9_Out_0 = Vector1_1CEB35D8;
                        float _Property_D42BE95B_Out_0 = Vector1_B61034CA;
                        float _Split_CEEBC462_R_1 = IN.AbsoluteWorldSpacePosition[0];
                        float _Split_CEEBC462_G_2 = IN.AbsoluteWorldSpacePosition[1];
                        float _Split_CEEBC462_B_3 = IN.AbsoluteWorldSpacePosition[2];
                        float _Split_CEEBC462_A_4 = 0;
                        float2 _Vector2_BB7CEEF5_Out_0 = float2(_Split_CEEBC462_R_1, _Split_CEEBC462_B_3);
                        float2 _Property_305FB73_Out_0 = Vector2_9C73A0C6;
                        float _Property_88A7570_Out_0 = Vector1_B01E1A6A;
                        Bindings_CrestAmbientLight_a6ec89b3ca0ab4e98b300ec3ba0e6013 _CrestAmbientLight_BC7EB0E;
                        half3 _CrestAmbientLight_BC7EB0E_Color_1;
                        SG_CrestAmbientLight_a6ec89b3ca0ab4e98b300ec3ba0e6013(_CrestAmbientLight_BC7EB0E, _CrestAmbientLight_BC7EB0E_Color_1);
                        float2 _Property_5CBA815_Out_0 = Vector2_69CC43DC;
                        half3 _CustomFunction_3910AE92_Colour_16;
                        CrestNodeFoamBubbles_half(_Property_4729CD24_Out_0, _Property_C36E9B6_Out_0, _Property_2C173F60_Out_0, Texture2D_BE500045, _Property_4258AA49_Out_0, _Property_3FC34BC9_Out_0, _Property_D42BE95B_Out_0, _CrestComputeNormal_6DAE4B39_Normal_1, _CrestDrivenData_6635D6E9_OceanParams0_5, _CrestDrivenData_6635D6E9_OceanParams1_6, _Vector2_BB7CEEF5_Out_0, _Property_305FB73_Out_0, _Property_88A7570_Out_0, _Normalize_3574419A_Out_1, _CrestAmbientLight_BC7EB0E_Color_1, _Property_5CBA815_Out_0, _CustomFunction_3910AE92_Colour_16);
                        Bindings_CrestEmission_8c56460232fde1e46ae90d905a00f908 _CrestEmission_6580C0F9;
                        _CrestEmission_6580C0F9.FaceSign = IN.FaceSign;
                        float3 _CrestEmission_6580C0F9_EmittedLight_1;
                        SG_CrestEmission_8c56460232fde1e46ae90d905a00f908(_Property_2599690E_Out_0, _Property_A57CAD6E_Out_0, _CrestVolumeLighting_5A1A391_VolumeLighting_1, _Normalize_3574419A_Out_1, _CrestComputeNormal_6DAE4B39_Normal_1, _ScreenPosition_42FE0E3E_Out_0, _Negate_5F3C7D3D_Out_1, _SceneColor_61626E7C_Out_1, _SceneDepth_FD35F7D9_Out_1, _Property_4DD76B1_Out_0, _Property_DC1C088D_Out_0, TEXTURE2D_ARGS(Texture2D_EB8C8549, samplerTexture2D_EB8C8549), Texture2D_EB8C8549_TexelSize, _Property_8BC39E79_Out_0, _Property_683CAF2_Out_0, _Property_B4B35559_Out_0, _Property_E694F888_Out_0, _Property_50A87698_Out_0, TEXTURE2D_ARGS(Texture2D_DA8A756A, samplerTexture2D_DA8A756A), Texture2D_DA8A756A_TexelSize, _Property_8BC9D755_Out_0, _Property_6F95DFBF_Out_0, _CustomFunction_3910AE92_Colour_16, _CrestEmission_6580C0F9, _CrestEmission_6580C0F9_EmittedLight_1);
                        float3 _Multiply_CFF25F4B_Out_2;
                        Unity_Multiply_float((_CrestFresnel_FD3200EB_LightTransmitted_1.xxx), _CrestEmission_6580C0F9_EmittedLight_1, _Multiply_CFF25F4B_Out_2);
                        float4 _Property_2B229CB_Out_0 = Vector4_2F6E352;
                        float3 _Property_EB21E5D5_Out_0 = Vector3_57B74D6A;
                        float4 _Property_C16A4E4D_Out_0 = Vector4_B3AD63B4;
                        float _Property_496C0D8_Out_0 = Vector1_FA688590;
                        float _Property_D1C2E8AB_Out_0 = Vector1_9835666E;
                        float _Property_62C11254_Out_0 = Vector1_B331E24E;
                        float _Property_5977537D_Out_0 = Vector1_951CF2DF;
                        float4 _Property_A64A7625_Out_0 = Vector4_ADCA8891;
                        float _Property_6BDEE46A_Out_0 = Vector1_2E8E2C59;
                        float _Property_26AD77F8_Out_0 = Vector1_D74C6609;
                        float2 _Property_80C3F22F_Out_0 = Vector2_AE8873FA;
                        float _Property_5BAA7C5F_Out_0 = Vector1_255AB964;
                        float3 _Negate_99287013_Out_1;
                        Unity_Negate_float3(_Normalize_3574419A_Out_1, _Negate_99287013_Out_1);
                        float3 _Reflection_2C6907C_Out_2;
                        Unity_Reflection_float3(_Negate_99287013_Out_1, _CrestComputeNormal_6DAE4B39_Normal_1, _Reflection_2C6907C_Out_2);
                        Bindings_CrestAmbientLight_a6ec89b3ca0ab4e98b300ec3ba0e6013 _CrestAmbientLight_744A95D5;
                        half3 _CrestAmbientLight_744A95D5_Color_1;
                        SG_CrestAmbientLight_a6ec89b3ca0ab4e98b300ec3ba0e6013(_CrestAmbientLight_744A95D5, _CrestAmbientLight_744A95D5_Color_1);
                        float3 _Property_FC57CF9F_Out_0 = Vector3_B2D6AD84;
                        float3 _Property_BAEF74C8_Out_0 = Vector3_D2C93D25;
                        Bindings_CrestVolumeLighting_e9ed6e11710a50640bb4b811d0fa84f2 _CrestVolumeLighting_B895C731;
                        half3 _CrestVolumeLighting_B895C731_VolumeLighting_1;
                        SG_CrestVolumeLighting_e9ed6e11710a50640bb4b811d0fa84f2(_Property_2B229CB_Out_0, _Property_EB21E5D5_Out_0, _Property_C16A4E4D_Out_0, _Property_496C0D8_Out_0, _Property_D1C2E8AB_Out_0, _Property_62C11254_Out_0, _Property_5977537D_Out_0, _Property_A64A7625_Out_0, _Property_6BDEE46A_Out_0, _Property_26AD77F8_Out_0, _Property_80C3F22F_Out_0, _Property_5BAA7C5F_Out_0, _Reflection_2C6907C_Out_2, IN.AbsoluteWorldSpacePosition, _CrestAmbientLight_744A95D5_Color_1, _Property_FC57CF9F_Out_0, _Property_BAEF74C8_Out_0, _CrestVolumeLighting_B895C731, _CrestVolumeLighting_B895C731_VolumeLighting_1);
                        float3 _Multiply_C794DFA8_Out_2;
                        Unity_Multiply_float((_CrestFresnel_FD3200EB_LightReflected_2.xxx), _CrestVolumeLighting_B895C731_VolumeLighting_1, _Multiply_C794DFA8_Out_2);
                        float3 _Add_906C47A2_Out_2;
                        Unity_Add_float3(_Multiply_CFF25F4B_Out_2, _Multiply_C794DFA8_Out_2, _Add_906C47A2_Out_2);
                        float3 _Branch_A82A52E6_Out_3;
                        Unity_Branch_float3(_CrestIsUnderwater_5AB8F4C9_OutBoolean_1, _Add_906C47A2_Out_2, _Multiply_CFF25F4B_Out_2, _Branch_A82A52E6_Out_3);
                        float _Property_5DA83C2A_Out_0 = Vector1_47308CC2;
                        float _Property_44533AA4_Out_0 = Vector1_26549044;
                        float _Property_19CC00AB_Out_0 = Vector1_177E111C;
                        float _Divide_6B6C7F25_Out_2;
                        Unity_Divide_float(_Negate_5F3C7D3D_Out_1, _Property_19CC00AB_Out_0, _Divide_6B6C7F25_Out_2);
                        float _Saturate_D69CDED6_Out_1;
                        Unity_Saturate_float(_Divide_6B6C7F25_Out_2, _Saturate_D69CDED6_Out_1);
                        float _Property_8033277F_Out_0 = Vector1_33255829;
                        float _Power_BC121A67_Out_2;
                        Unity_Power_float(_Saturate_D69CDED6_Out_1, _Property_8033277F_Out_0, _Power_BC121A67_Out_2);
                        float _Lerp_3E7590A8_Out_3;
                        Unity_Lerp_float(_Property_5DA83C2A_Out_0, _Property_44533AA4_Out_0, _Power_BC121A67_Out_2, _Lerp_3E7590A8_Out_3);
                        float2 _Property_374C5B7E_Out_0 = Vector2_69CC43DC;
                        Bindings_CrestFoamWithFlow_3a6c22dceac0847a2a31cb05a577b963 _CrestFoamWithFlow_193F69CA;
                        float3 _CrestFoamWithFlow_193F69CA_Albedo_1;
                        float3 _CrestFoamWithFlow_193F69CA_NormalTS_2;
                        float3 _CrestFoamWithFlow_193F69CA_Emission_3;
                        float _CrestFoamWithFlow_193F69CA_Smoothness_4;
                        SG_CrestFoamWithFlow_3a6c22dceac0847a2a31cb05a577b963(TEXTURE2D_ARGS(Texture2D_BE500045, samplerTexture2D_BE500045), Texture2D_BE500045_TexelSize, _Property_B9BA8901_Out_0, _Property_6AEC3552_Out_0, _Property_76EA797B_Out_0, _Property_83C2E998_Out_0, _Property_705FD8D9_Out_0, _Property_735B5A53_Out_0, _Property_EB4C03CF_Out_0, _CrestDrivenData_6635D6E9_OceanParams0_5, _CrestDrivenData_6635D6E9_OceanParams1_6, _Property_EE920C2C_Out_0, _Property_8BC6F5AD_Out_0, _Property_A2CEB215_Out_0, _CrestComputeNormal_6DAE4B39_Normal_1, _Branch_A82A52E6_Out_3, _Lerp_3E7590A8_Out_3, _Negate_5F3C7D3D_Out_1, _Property_374C5B7E_Out_0, _CrestFoamWithFlow_193F69CA, _CrestFoamWithFlow_193F69CA_Albedo_1, _CrestFoamWithFlow_193F69CA_NormalTS_2, _CrestFoamWithFlow_193F69CA_Emission_3, _CrestFoamWithFlow_193F69CA_Smoothness_4);
                        Albedo_2 = _CrestFoamWithFlow_193F69CA_Albedo_1;
                        NormalTS_3 = _CrestFoamWithFlow_193F69CA_NormalTS_2;
                        Emission_4 = _CrestFoamWithFlow_193F69CA_Emission_3;
                        Smoothness_5 = _CrestFoamWithFlow_193F69CA_Smoothness_4;
                        Specular_6 = _CrestFresnel_FD3200EB_LightReflected_2;
                    }
                
                    // af89d2205c5dc03c016b842f3f74cf56
                    #include "ShadergraphFramework/CrestNodeOcclusion.hlsl"
                
                    struct Bindings_CrestOcclusion_8f9c2c1ada00f40a6a372f3868116540
                    {
                        float FaceSign;
                    };
                
                    void SG_CrestOcclusion_8f9c2c1ada00f40a6a372f3868116540(half Vector1_A0828619, Bindings_CrestOcclusion_8f9c2c1ada00f40a6a372f3868116540 IN, out half Occlusion_1)
                    {
                        half _Property_F6E978B_Out_0 = Vector1_A0828619;
                        Bindings_CrestIsUnderwater_52f7750f15e114937b54a0fd27f0d2f2 _CrestIsUnderwater_31591C4B;
                        _CrestIsUnderwater_31591C4B.FaceSign = IN.FaceSign;
                        float _CrestIsUnderwater_31591C4B_OutBoolean_1;
                        SG_CrestIsUnderwater_52f7750f15e114937b54a0fd27f0d2f2(_CrestIsUnderwater_31591C4B, _CrestIsUnderwater_31591C4B_OutBoolean_1);
                        half _CustomFunction_970B34CE_OcclusionOut_2;
                        CrestNodeOcclusion_half(_Property_F6E978B_Out_0, _CrestIsUnderwater_31591C4B_OutBoolean_1, _CustomFunction_970B34CE_OcclusionOut_2);
                        Occlusion_1 = _CustomFunction_970B34CE_OcclusionOut_2;
                    }
                
                    void Unity_Remap_float(float In, float2 InMinMax, float2 OutMinMax, out float Out)
                    {
                        Out = OutMinMax.x + (In - InMinMax.x) * (OutMinMax.y - OutMinMax.x) / (InMinMax.y - InMinMax.x);
                    }
                
                    // cb9e19acd0b7f8a1a1c0b9aed89ebacc
                    #include "ShadergraphFramework/CrestNodeSampleClipSurfaceData.hlsl"
                
                    struct Bindings_CrestClipSurface_8c73b4813486448849bd38d01267f186
                    {
                        float3 AbsoluteWorldSpacePosition;
                        half4 uv0;
                        half4 uv1;
                        half4 uv2;
                    };
                
                    void SG_CrestClipSurface_8c73b4813486448849bd38d01267f186(Bindings_CrestClipSurface_8c73b4813486448849bd38d01267f186 IN, out float ClipSurfaceValue_1)
                    {
                        float _Split_A2A81B90_R_1 = IN.AbsoluteWorldSpacePosition[0];
                        float _Split_A2A81B90_G_2 = IN.AbsoluteWorldSpacePosition[1];
                        float _Split_A2A81B90_B_3 = IN.AbsoluteWorldSpacePosition[2];
                        float _Split_A2A81B90_A_4 = 0;
                        float2 _Vector2_5DE2DA1_Out_0 = float2(_Split_A2A81B90_R_1, _Split_A2A81B90_B_3);
                        Bindings_CrestUnpackData_5bc4cf08f88035c489658d60edefeec8 _CrestUnpackData_ECB44FF9;
                        _CrestUnpackData_ECB44FF9.AbsoluteWorldSpacePosition = IN.AbsoluteWorldSpacePosition;
                        _CrestUnpackData_ECB44FF9.uv0 = IN.uv0;
                        _CrestUnpackData_ECB44FF9.uv1 = IN.uv1;
                        _CrestUnpackData_ECB44FF9.uv2 = IN.uv2;
                        float2 _CrestUnpackData_ECB44FF9_PositionXZWSUndisp_2;
                        float _CrestUnpackData_ECB44FF9_LodAlpha_1;
                        float _CrestUnpackData_ECB44FF9_OceanDepth_3;
                        float _CrestUnpackData_ECB44FF9_Foam_4;
                        float2 _CrestUnpackData_ECB44FF9_Shadow_5;
                        float2 _CrestUnpackData_ECB44FF9_Flow_6;
                        float _CrestUnpackData_ECB44FF9_SubSurfaceScattering_7;
                        SG_CrestUnpackData_5bc4cf08f88035c489658d60edefeec8(_CrestUnpackData_ECB44FF9, _CrestUnpackData_ECB44FF9_PositionXZWSUndisp_2, _CrestUnpackData_ECB44FF9_LodAlpha_1, _CrestUnpackData_ECB44FF9_OceanDepth_3, _CrestUnpackData_ECB44FF9_Foam_4, _CrestUnpackData_ECB44FF9_Shadow_5, _CrestUnpackData_ECB44FF9_Flow_6, _CrestUnpackData_ECB44FF9_SubSurfaceScattering_7);
                        Bindings_CrestDrivenData_40572ccadf7d9704a83d283fb4c9f19d _CrestDrivenData_EA2C4302;
                        half _CrestDrivenData_EA2C4302_MeshScaleAlpha_1;
                        half _CrestDrivenData_EA2C4302_LodDataTexelSize_8;
                        half _CrestDrivenData_EA2C4302_GeometryGridSize_2;
                        half3 _CrestDrivenData_EA2C4302_OceanPosScale0_3;
                        half3 _CrestDrivenData_EA2C4302_OceanPosScale1_4;
                        half4 _CrestDrivenData_EA2C4302_OceanParams0_5;
                        half4 _CrestDrivenData_EA2C4302_OceanParams1_6;
                        half _CrestDrivenData_EA2C4302_SliceIndex0_7;
                        SG_CrestDrivenData_40572ccadf7d9704a83d283fb4c9f19d(_CrestDrivenData_EA2C4302, _CrestDrivenData_EA2C4302_MeshScaleAlpha_1, _CrestDrivenData_EA2C4302_LodDataTexelSize_8, _CrestDrivenData_EA2C4302_GeometryGridSize_2, _CrestDrivenData_EA2C4302_OceanPosScale0_3, _CrestDrivenData_EA2C4302_OceanPosScale1_4, _CrestDrivenData_EA2C4302_OceanParams0_5, _CrestDrivenData_EA2C4302_OceanParams1_6, _CrestDrivenData_EA2C4302_SliceIndex0_7);
                        float _CustomFunction_26C15E74_ClipSurfaceValue_7;
                        CrestNodeSampleClipSurfaceData_float(_Vector2_5DE2DA1_Out_0, _CrestUnpackData_ECB44FF9_LodAlpha_1, _CrestDrivenData_EA2C4302_OceanPosScale0_3, _CrestDrivenData_EA2C4302_OceanPosScale1_4, _CrestDrivenData_EA2C4302_OceanParams0_5, _CrestDrivenData_EA2C4302_OceanParams1_6, _CrestDrivenData_EA2C4302_SliceIndex0_7, _CustomFunction_26C15E74_ClipSurfaceValue_7);
                        ClipSurfaceValue_1 = _CustomFunction_26C15E74_ClipSurfaceValue_7;
                    }
                
                    #include "ShadergraphFramework/CrestNodeGeoMorph.hlsl"
                
                    struct Bindings_CrestGeoMorph_9ab91ec3462438049923ca0ff16f68ad
                    {
                    };
                
                    void SG_CrestGeoMorph_9ab91ec3462438049923ca0ff16f68ad(float3 Vector3_28A0F264, float3 Vector3_F1111B56, float Vector1_691AFD6A, float Vector1_37DEE8F3, Bindings_CrestGeoMorph_9ab91ec3462438049923ca0ff16f68ad IN, out half3 MorphedPositionWS_1, out half LodAlpha_2)
                    {
                        float3 _Property_C4B6B1D5_Out_0 = Vector3_28A0F264;
                        float3 _Property_13BC6D1A_Out_0 = Vector3_F1111B56;
                        float _Property_DE4BC103_Out_0 = Vector1_691AFD6A;
                        float _Property_B3D2A4DF_Out_0 = Vector1_37DEE8F3;
                        half3 _CustomFunction_C8F1D6C4_MorphedPositionWS_4;
                        half _CustomFunction_C8F1D6C4_LodAlpha_5;
                        GeoMorph_half(_Property_C4B6B1D5_Out_0, _Property_13BC6D1A_Out_0, _Property_DE4BC103_Out_0, _Property_B3D2A4DF_Out_0, _CustomFunction_C8F1D6C4_MorphedPositionWS_4, _CustomFunction_C8F1D6C4_LodAlpha_5);
                        MorphedPositionWS_1 = _CustomFunction_C8F1D6C4_MorphedPositionWS_4;
                        LodAlpha_2 = _CustomFunction_C8F1D6C4_LodAlpha_5;
                    }
                
                // Vertex Graph Evaluation
                    VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
                    {
                        VertexDescription description = (VertexDescription)0;
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        Bindings_CrestDrivenData_40572ccadf7d9704a83d283fb4c9f19d _CrestDrivenData_A1078169;
                        half _CrestDrivenData_A1078169_MeshScaleAlpha_1;
                        half _CrestDrivenData_A1078169_LodDataTexelSize_8;
                        half _CrestDrivenData_A1078169_GeometryGridSize_2;
                        half3 _CrestDrivenData_A1078169_OceanPosScale0_3;
                        half3 _CrestDrivenData_A1078169_OceanPosScale1_4;
                        half4 _CrestDrivenData_A1078169_OceanParams0_5;
                        half4 _CrestDrivenData_A1078169_OceanParams1_6;
                        half _CrestDrivenData_A1078169_SliceIndex0_7;
                        SG_CrestDrivenData_40572ccadf7d9704a83d283fb4c9f19d(_CrestDrivenData_A1078169, _CrestDrivenData_A1078169_MeshScaleAlpha_1, _CrestDrivenData_A1078169_LodDataTexelSize_8, _CrestDrivenData_A1078169_GeometryGridSize_2, _CrestDrivenData_A1078169_OceanPosScale0_3, _CrestDrivenData_A1078169_OceanPosScale1_4, _CrestDrivenData_A1078169_OceanParams0_5, _CrestDrivenData_A1078169_OceanParams1_6, _CrestDrivenData_A1078169_SliceIndex0_7);
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        Bindings_CrestGeoMorph_9ab91ec3462438049923ca0ff16f68ad _CrestGeoMorph_8F1A4FF1;
                        half3 _CrestGeoMorph_8F1A4FF1_MorphedPositionWS_1;
                        half _CrestGeoMorph_8F1A4FF1_LodAlpha_2;
                        SG_CrestGeoMorph_9ab91ec3462438049923ca0ff16f68ad(IN.AbsoluteWorldSpacePosition, _CrestDrivenData_A1078169_OceanPosScale0_3, _CrestDrivenData_A1078169_MeshScaleAlpha_1, _CrestDrivenData_A1078169_GeometryGridSize_2, _CrestGeoMorph_8F1A4FF1, _CrestGeoMorph_8F1A4FF1_MorphedPositionWS_1, _CrestGeoMorph_8F1A4FF1_LodAlpha_2);
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        float _Split_CC063A43_R_1 = _CrestGeoMorph_8F1A4FF1_MorphedPositionWS_1[0];
                        float _Split_CC063A43_G_2 = _CrestGeoMorph_8F1A4FF1_MorphedPositionWS_1[1];
                        float _Split_CC063A43_B_3 = _CrestGeoMorph_8F1A4FF1_MorphedPositionWS_1[2];
                        float _Split_CC063A43_A_4 = 0;
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        float2 _Vector2_2D46FD43_Out_0 = float2(_Split_CC063A43_R_1, _Split_CC063A43_B_3);
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        Bindings_CrestSampleOceanData_f19d6d2aa60f8294e870f100303ee5a4 _CrestSampleOceanData_340A6610;
                        float3 _CrestSampleOceanData_340A6610_Displacement_1;
                        float _CrestSampleOceanData_340A6610_OceanWaterDepth_5;
                        float _CrestSampleOceanData_340A6610_Foam_6;
                        float2 _CrestSampleOceanData_340A6610_Shadow_7;
                        float2 _CrestSampleOceanData_340A6610_Flow_8;
                        float _CrestSampleOceanData_340A6610_SubSurfaceScattering_9;
                        SG_CrestSampleOceanData_f19d6d2aa60f8294e870f100303ee5a4(_Vector2_2D46FD43_Out_0, _CrestGeoMorph_8F1A4FF1_LodAlpha_2, _CrestDrivenData_A1078169_OceanPosScale0_3, _CrestDrivenData_A1078169_OceanPosScale1_4, _CrestDrivenData_A1078169_OceanParams0_5, _CrestDrivenData_A1078169_OceanParams1_6, _CrestDrivenData_A1078169_SliceIndex0_7, _CrestSampleOceanData_340A6610, _CrestSampleOceanData_340A6610_Displacement_1, _CrestSampleOceanData_340A6610_OceanWaterDepth_5, _CrestSampleOceanData_340A6610_Foam_6, _CrestSampleOceanData_340A6610_Shadow_7, _CrestSampleOceanData_340A6610_Flow_8, _CrestSampleOceanData_340A6610_SubSurfaceScattering_9);
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        float3 _Add_2D79A354_Out_2;
                        Unity_Add_float3(_CrestGeoMorph_8F1A4FF1_MorphedPositionWS_1, _CrestSampleOceanData_340A6610_Displacement_1, _Add_2D79A354_Out_2);
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        float3 _Transform_9C2ACF28_Out_1 = TransformWorldToObject(GetCameraRelativePositionWS(_Add_2D79A354_Out_2.xyz));
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        float3 _Transform_86205EE4_Out_1 = TransformWorldToObjectDir(float3 (0, 1, 0).xyz);
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        float3 _Transform_22BD1A85_Out_1 = TransformWorldToObjectDir(float3 (1, 0, 0).xyz);
                        #endif
                        description.VertexPosition = _Transform_9C2ACF28_Out_1;
                        description.VertexNormal = _Transform_86205EE4_Out_1;
                        description.VertexTangent = _Transform_22BD1A85_Out_1;

						// Crest -------------------
						description.LodAlpha = _CrestGeoMorph_8F1A4FF1_LodAlpha_2;
						description.WorldXZUndisplaced = _Vector2_2D46FD43_Out_0;
						description.OceanDepth = _CrestSampleOceanData_340A6610_OceanWaterDepth_5;
						description.Foam = _CrestSampleOceanData_340A6610_Foam_6;
						description.Shadow = _CrestSampleOceanData_340A6610_Shadow_7;
						description.Flow = _CrestSampleOceanData_340A6610_Flow_8;
						description.SSS = _CrestSampleOceanData_340A6610_SubSurfaceScattering_9;
						// -------------------------

						return description;
                    }
                    
                // Pixel Graph Evaluation
                    SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
                    {
                        SurfaceDescription surface = (SurfaceDescription)0;
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        half _TexelSize_CA3A0DD6_Width_0 = _TextureFoam_TexelSize.z;
                        half _TexelSize_CA3A0DD6_Height_2 = _TextureFoam_TexelSize.w;
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        half2 _Vector2_15E252FB_Out_0 = half2(_TexelSize_CA3A0DD6_Width_0, _TexelSize_CA3A0DD6_Height_2);
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        half _Property_87E9391A_Out_0 = _FoamScale;
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        half _Property_BC30BCB6_Out_0 = _FoamFeather;
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        half _Property_5E8CB038_Out_0 = _FoamIntensityAlbedo;
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        half _Property_77A524C7_Out_0 = _FoamIntensityEmissive;
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        half _Property_CED296A4_Out_0 = _FoamSmoothness;
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        half _Property_D1CA3C84_Out_0 = _FoamNormalStrength;
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        half4 _Property_3BAB5FF_Out_0 = _FoamBubbleColor;
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        half _Property_EDC4DC3C_Out_0 = _FoamBubbleParallax;
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        half _Property_E7925FBA_Out_0 = _FoamBubblesCoverage;
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        Bindings_CrestUnpackData_5bc4cf08f88035c489658d60edefeec8 _CrestUnpackData_C3998C1C;
                        _CrestUnpackData_C3998C1C.AbsoluteWorldSpacePosition = IN.AbsoluteWorldSpacePosition;
                        _CrestUnpackData_C3998C1C.uv0 = IN.uv0;
                        _CrestUnpackData_C3998C1C.uv1 = IN.uv1;
                        _CrestUnpackData_C3998C1C.uv2 = IN.uv2;
                        float2 _CrestUnpackData_C3998C1C_PositionXZWSUndisp_2;
                        float _CrestUnpackData_C3998C1C_LodAlpha_1;
                        float _CrestUnpackData_C3998C1C_OceanDepth_3;
                        float _CrestUnpackData_C3998C1C_Foam_4;
                        float2 _CrestUnpackData_C3998C1C_Shadow_5;
                        float2 _CrestUnpackData_C3998C1C_Flow_6;
                        float _CrestUnpackData_C3998C1C_SubSurfaceScattering_7;
                        SG_CrestUnpackData_5bc4cf08f88035c489658d60edefeec8(_CrestUnpackData_C3998C1C, _CrestUnpackData_C3998C1C_PositionXZWSUndisp_2, _CrestUnpackData_C3998C1C_LodAlpha_1, _CrestUnpackData_C3998C1C_OceanDepth_3, _CrestUnpackData_C3998C1C_Foam_4, _CrestUnpackData_C3998C1C_Shadow_5, _CrestUnpackData_C3998C1C_Flow_6, _CrestUnpackData_C3998C1C_SubSurfaceScattering_7);
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        half _Property_994CB3A3_Out_0 = _Smoothness;
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        float _Property_7079FD32_Out_0 = _SmoothnessFar;
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        float _Property_FAEF961B_Out_0 = _SmoothnessFarDistance;
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        float _Property_35DA1E70_Out_0 = _SmoothnessFalloff;
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        half _Property_D1430252_Out_0 = _NormalsScale;
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        half _Property_B882DB1E_Out_0 = _NormalsStrength;
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        half4 _Property_B5E474DF_Out_0 = _ScatterColourBase;
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        half4 _Property_5D62FE57_Out_0 = _ScatterColourShadow;
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        float4 _Property_B98AE6FA_Out_0 = _ScatterColourShallow;
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        half _Property_CF7CEF3A_Out_0 = _ScatterColourShallowDepthMax;
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        half _Property_698A3C81_Out_0 = _ScatterColourShallowDepthFalloff;
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        half _Property_5924F1E9_Out_0 = _SSSIntensityBase;
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        half _Property_67308B5F_Out_0 = _SSSIntensitySun;
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        half4 _Property_A723E757_Out_0 = _SSSTint;
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        half _Property_5AD6818A_Out_0 = _SSSSunFalloff;
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        half _Property_888FA49_Out_0 = _RefractionStrength;
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        half3 _Property_F1315B19_Out_0 = _DepthFogDensity;
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        Bindings_CrestLightData_b74b6e8c0b489314ca7aea3e2cc9c54c _CrestLightData_5AD806DD;
                        half3 _CrestLightData_5AD806DD_Direction_1;
                        half3 _CrestLightData_5AD806DD_Intensity_2;
                        SG_CrestLightData_b74b6e8c0b489314ca7aea3e2cc9c54c(_CrestLightData_5AD806DD, _CrestLightData_5AD806DD_Direction_1, _CrestLightData_5AD806DD_Intensity_2);
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        float _Property_A26763AB_Out_0 = _CausticsTextureScale;
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        float _Property_3230CB90_Out_0 = _CausticsTextureAverage;
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        float _Property_6B2F7D3E_Out_0 = _CausticsStrength;
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        float _Property_D5C90779_Out_0 = _CausticsFocalDepth;
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        float _Property_2743B36F_Out_0 = _CausticsDepthOfField;
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        float _Property_61877218_Out_0 = _CausticsDistortionStrength;
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        float _Property_5F1E3794_Out_0 = _CausticsDistortionScale;
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        float _Property_7BD0FFAF_Out_0 = _MinReflectionDirectionY;
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        Bindings_CrestOceanPixel_6f6706d805d8e8649adddaaa94260269 _CrestOceanPixel_51593A7C;
                        _CrestOceanPixel_51593A7C.WorldSpaceViewDirection = IN.WorldSpaceViewDirection;
                        _CrestOceanPixel_51593A7C.ViewSpacePosition = IN.ViewSpacePosition;
                        _CrestOceanPixel_51593A7C.AbsoluteWorldSpacePosition = IN.AbsoluteWorldSpacePosition;
                        _CrestOceanPixel_51593A7C.ScreenPosition = IN.ScreenPosition;
                        _CrestOceanPixel_51593A7C.FaceSign = IN.FaceSign;
                        float3 _CrestOceanPixel_51593A7C_Albedo_2;
                        float3 _CrestOceanPixel_51593A7C_NormalTS_3;
                        float3 _CrestOceanPixel_51593A7C_Emission_4;
                        float _CrestOceanPixel_51593A7C_Smoothness_5;
                        float _CrestOceanPixel_51593A7C_Specular_6;
                        SG_CrestOceanPixel_6f6706d805d8e8649adddaaa94260269(TEXTURE2D_ARGS(_TextureFoam, sampler_TextureFoam), _TextureFoam_TexelSize, _Vector2_15E252FB_Out_0, _Property_87E9391A_Out_0, _Property_BC30BCB6_Out_0, _Property_5E8CB038_Out_0, _Property_77A524C7_Out_0, _Property_CED296A4_Out_0, _Property_D1CA3C84_Out_0, (_Property_3BAB5FF_Out_0.xyz), _Property_EDC4DC3C_Out_0, _Property_E7925FBA_Out_0, _CrestUnpackData_C3998C1C_PositionXZWSUndisp_2, _CrestUnpackData_C3998C1C_LodAlpha_1, _CrestUnpackData_C3998C1C_OceanDepth_3, _CrestUnpackData_C3998C1C_Foam_4, _CrestUnpackData_C3998C1C_Shadow_5, _CrestUnpackData_C3998C1C_Flow_6, _CrestUnpackData_C3998C1C_SubSurfaceScattering_7, _Property_994CB3A3_Out_0, _Property_7079FD32_Out_0, _Property_FAEF961B_Out_0, _Property_35DA1E70_Out_0, TEXTURE2D_ARGS(_TextureNormals, sampler_TextureNormals), _TextureNormals_TexelSize, _Property_D1430252_Out_0, _Property_B882DB1E_Out_0, _Property_B5E474DF_Out_0, (_Property_5D62FE57_Out_0.xyz), _Property_B98AE6FA_Out_0, _Property_CF7CEF3A_Out_0, _Property_698A3C81_Out_0, _Property_5924F1E9_Out_0, _Property_67308B5F_Out_0, _Property_A723E757_Out_0, _Property_5AD6818A_Out_0, _Property_888FA49_Out_0, _Property_F1315B19_Out_0, _CrestLightData_5AD806DD_Direction_1, _CrestLightData_5AD806DD_Intensity_2, TEXTURE2D_ARGS(_CausticsTexture, sampler_CausticsTexture), _CausticsTexture_TexelSize, _Property_A26763AB_Out_0, _Property_3230CB90_Out_0, _Property_6B2F7D3E_Out_0, _Property_D5C90779_Out_0, _Property_2743B36F_Out_0, TEXTURE2D_ARGS(_CausticsDistortionTexture, sampler_CausticsDistortionTexture), _CausticsDistortionTexture_TexelSize, _Property_61877218_Out_0, _Property_5F1E3794_Out_0, 1.33, 1, _Property_7BD0FFAF_Out_0, _CrestOceanPixel_51593A7C, _CrestOceanPixel_51593A7C_Albedo_2, _CrestOceanPixel_51593A7C_NormalTS_3, _CrestOceanPixel_51593A7C_Emission_4, _CrestOceanPixel_51593A7C_Smoothness_5, _CrestOceanPixel_51593A7C_Specular_6);
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        float _Property_A3329432_Out_0 = _Specular;
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        float _Multiply_E01A7830_Out_2;
                        Unity_Multiply_float(_Property_A3329432_Out_0, _CrestOceanPixel_51593A7C_Specular_6, _Multiply_E01A7830_Out_2);
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        float _Property_E132C977_Out_0 = _Occlusion;
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        Bindings_CrestOcclusion_8f9c2c1ada00f40a6a372f3868116540 _CrestOcclusion_1BA57722;
                        _CrestOcclusion_1BA57722.FaceSign = IN.FaceSign;
                        half _CrestOcclusion_1BA57722_Occlusion_1;
                        SG_CrestOcclusion_8f9c2c1ada00f40a6a372f3868116540(_Property_E132C977_Out_0, _CrestOcclusion_1BA57722, _CrestOcclusion_1BA57722_Occlusion_1);
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        float _SceneDepth_E2A24470_Out_1;
                        Unity_SceneDepth_Eye_float(float4(IN.ScreenPosition.xy / IN.ScreenPosition.w, 0, 0), _SceneDepth_E2A24470_Out_1);
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        float _Split_68C2565_R_1 = IN.ViewSpacePosition[0];
                        float _Split_68C2565_G_2 = IN.ViewSpacePosition[1];
                        float _Split_68C2565_B_3 = IN.ViewSpacePosition[2];
                        float _Split_68C2565_A_4 = 0;
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        float _Negate_847D9376_Out_1;
                        Unity_Negate_float(_Split_68C2565_B_3, _Negate_847D9376_Out_1);
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        float _Subtract_B338E0_Out_2;
                        Unity_Subtract_float(_SceneDepth_E2A24470_Out_1, _Negate_847D9376_Out_1, _Subtract_B338E0_Out_2);
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        float _Remap_6CD3C222_Out_3;
                        Unity_Remap_float(_Subtract_B338E0_Out_2, float2 (0, 0.2), float2 (0, 1), _Remap_6CD3C222_Out_3);
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        float _Saturate_FEF634C4_Out_1;
                        Unity_Saturate_float(_Remap_6CD3C222_Out_3, _Saturate_FEF634C4_Out_1);
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        Bindings_CrestClipSurface_8c73b4813486448849bd38d01267f186 _CrestClipSurface_AA3EF9C5;
                        _CrestClipSurface_AA3EF9C5.AbsoluteWorldSpacePosition = IN.AbsoluteWorldSpacePosition;
                        _CrestClipSurface_AA3EF9C5.uv0 = IN.uv0;
                        _CrestClipSurface_AA3EF9C5.uv1 = IN.uv1;
                        _CrestClipSurface_AA3EF9C5.uv2 = IN.uv2;
                        float _CrestClipSurface_AA3EF9C5_ClipSurfaceValue_1;
                        SG_CrestClipSurface_8c73b4813486448849bd38d01267f186(_CrestClipSurface_AA3EF9C5, _CrestClipSurface_AA3EF9C5_ClipSurfaceValue_1);
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        float _Subtract_900E903A_Out_2;
                        Unity_Subtract_float(_Saturate_FEF634C4_Out_1, _CrestClipSurface_AA3EF9C5_ClipSurfaceValue_1, _Subtract_900E903A_Out_2);
                        #endif
                        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
                        float _Saturate_A1AFAC8_Out_1;
                        Unity_Saturate_float(_Subtract_900E903A_Out_2, _Saturate_A1AFAC8_Out_1);
                        #endif
                        surface.Albedo = _CrestOceanPixel_51593A7C_Albedo_2;
                        surface.Normal = _CrestOceanPixel_51593A7C_NormalTS_3;
                        surface.Specular = (_Multiply_E01A7830_Out_2.xxx);
                        surface.Emission = _CrestOceanPixel_51593A7C_Emission_4;
                        surface.Smoothness = _CrestOceanPixel_51593A7C_Smoothness_5;
                        surface.Occlusion = _CrestOcclusion_1BA57722_Occlusion_1;
                        surface.Alpha = _Saturate_A1AFAC8_Out_1;
                        surface.AlphaClipThreshold = 0;
                        return surface;
                    }
                    
            //-------------------------------------------------------------------------------------
            // End graph generated code
            //-------------------------------------------------------------------------------------
        
        //-------------------------------------------------------------------------------------
            // TEMPLATE INCLUDE : VertexAnimation.template.hlsl
            //-------------------------------------------------------------------------------------
            
            
            VertexDescriptionInputs AttributesMeshToVertexDescriptionInputs(AttributesMesh input)
            {
                VertexDescriptionInputs output;
                ZERO_INITIALIZE(VertexDescriptionInputs, output);
            
            
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
            output.WorldSpaceNormal =            TransformObjectToWorldNormal(input.normalOS);
            #endif
            
            
            
            
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
            output.WorldSpaceTangent =           TransformObjectToWorldDir(input.tangentOS.xyz);
            #endif
            
            
            
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
            output.ObjectSpaceBiTangent =        normalize(cross(input.normalOS, input.tangentOS.xyz) * (input.tangentOS.w > 0.0f ? 1.0f : -1.0f) * GetOddNegativeScale());
            #endif
            
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
            output.WorldSpaceBiTangent =         TransformObjectToWorldDir(output.ObjectSpaceBiTangent);
            #endif
            
            
            
            
            
            
            
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
            output.AbsoluteWorldSpacePosition =  GetAbsolutePositionWS(TransformObjectToWorld(input.positionOS));
            #endif
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
                return output;
            }
            
            AttributesMesh ApplyMeshModification(AttributesMesh input, float3 timeParameters)
            {
                // build graph inputs
                VertexDescriptionInputs vertexDescriptionInputs = AttributesMeshToVertexDescriptionInputs(input);
                // Override time paramters with used one (This is required to correctly handle motion vector for vertex animation based on time)
            
            
                // evaluate vertex graph
                VertexDescription vertexDescription = VertexDescriptionFunction(vertexDescriptionInputs);
            
                // copy graph output to the results
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
            input.positionOS = vertexDescription.VertexPosition;
            #endif
            
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
            input.normalOS = vertexDescription.VertexNormal;
            #endif
            
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
            input.tangentOS.xyz = vertexDescription.VertexTangent;
            #endif
            
            
				// Send ocean data through UV channels 0-2
				input.uv0.xy = vertexDescription.WorldXZUndisplaced;
				input.uv0.z = vertexDescription.LodAlpha;
				input.uv0.w = vertexDescription.OceanDepth;

				input.uv1.x = vertexDescription.SSS;
				input.uv1.y = vertexDescription.Foam;

				input.uv2.xy = vertexDescription.Shadow;
				input.uv2.zw = vertexDescription.Flow;

                return input;
            }
            
            //-------------------------------------------------------------------------------------
            // END TEMPLATE INCLUDE : VertexAnimation.template.hlsl
            //-------------------------------------------------------------------------------------
            
        
        //-------------------------------------------------------------------------------------
            // TEMPLATE INCLUDE : SharedCode.template.hlsl
            //-------------------------------------------------------------------------------------
            
            #if !defined(SHADER_STAGE_RAY_TRACING)
                FragInputs BuildFragInputs(VaryingsMeshToPS input)
                {
                    FragInputs output;
                    ZERO_INITIALIZE(FragInputs, output);
            
                    // Init to some default value to make the computer quiet (else it output 'divide by zero' warning even if value is not used).
                    // TODO: this is a really poor workaround, but the variable is used in a bunch of places
                    // to compute normals which are then passed on elsewhere to compute other values...
                    output.tangentToWorld = k_identity3x3;
                    output.positionSS = input.positionCS;       // input.positionCS is SV_Position
            
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
            output.positionRWS = input.positionRWS;
            #endif
            
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
            output.tangentToWorld = BuildTangentToWorld(input.tangentWS, input.normalWS);
            #endif
            
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
            output.texCoord0 = input.texCoord0;
            #endif
            
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
            output.texCoord1 = input.texCoord1;
            #endif
            
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
            output.texCoord2 = input.texCoord2;
            #endif
            
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
            output.texCoord3 = input.texCoord3;
            #endif
            
            
                    #if _DOUBLESIDED_ON && SHADER_STAGE_FRAGMENT
                    output.isFrontFace = IS_FRONT_VFACE(input.cullFace, true, false);
                    #elif SHADER_STAGE_FRAGMENT
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
            output.isFrontFace = IS_FRONT_VFACE(input.cullFace, true, false);
            #endif
            
                    #endif // SHADER_STAGE_FRAGMENT
            
                    return output;
                }
            #endif
                SurfaceDescriptionInputs FragInputsToSurfaceDescriptionInputs(FragInputs input, float3 viewWS)
                {
                    SurfaceDescriptionInputs output;
                    ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
            
            
            
            
            
            
            
            
            
            
            
            
            
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
            output.WorldSpaceViewDirection =     normalize(viewWS);
            #endif
            
            
            
            
            
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
            output.WorldSpacePosition =          input.positionRWS;
            #endif
            
            
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
            output.ViewSpacePosition =           TransformWorldToView(input.positionRWS);
            #endif
            
            
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
            output.AbsoluteWorldSpacePosition =  GetAbsolutePositionWS(input.positionRWS);
            #endif
            
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
            output.ScreenPosition =              ComputeScreenPos(TransformWorldToHClip(input.positionRWS), _ProjectionParams.x);
            #endif
            
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
            output.uv0 =                         input.texCoord0;
            #endif
            
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
            output.uv1 =                         input.texCoord1;
            #endif
            
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
            output.uv2 =                         input.texCoord2;
            #endif
            
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
            output.uv3 =                         input.texCoord3;
            #endif
            
            
            #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
            output.FaceSign =                    input.isFrontFace;
            #endif
            
            
            
                    return output;
                }
            
            #if !defined(SHADER_STAGE_RAY_TRACING)
            
                // existing HDRP code uses the combined function to go directly from packed to frag inputs
                FragInputs UnpackVaryingsMeshToFragInputs(PackedVaryingsMeshToPS input)
                {
                    UNITY_SETUP_INSTANCE_ID(input);
                    VaryingsMeshToPS unpacked= UnpackVaryingsMeshToPS(input);
                    return BuildFragInputs(unpacked);
                }
            #endif
            
            //-------------------------------------------------------------------------------------
            // END TEMPLATE INCLUDE : SharedCode.template.hlsl
            //-------------------------------------------------------------------------------------
            
        
        
            void BuildSurfaceData(FragInputs fragInputs, inout SurfaceDescription surfaceDescription, float3 V, PositionInputs posInput, out SurfaceData surfaceData)
            {
                // setup defaults -- these are used if the graph doesn't output a value
                ZERO_INITIALIZE(SurfaceData, surfaceData);
                surfaceData.ambientOcclusion = 1.0;
                surfaceData.specularOcclusion = 1.0; // This need to be init here to quiet the compiler in case of decal, but can be override later.
        
                // copy across graph values, if defined
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
        surfaceData.baseColor =             surfaceDescription.Albedo;
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
        surfaceData.perceptualSmoothness =  surfaceDescription.Smoothness;
        #endif
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
        surfaceData.ambientOcclusion =      surfaceDescription.Occlusion;
        #endif
        
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
        surfaceData.specularColor =         surfaceDescription.Specular;
        #endif
        
        
                // These static material feature allow compile time optimization
                surfaceData.materialFeatures = MATERIALFEATUREFLAGS_LIT_STANDARD;
        #ifdef _MATERIAL_FEATURE_SPECULAR_COLOR
                surfaceData.materialFeatures |= MATERIALFEATUREFLAGS_LIT_SPECULAR_COLOR;
        #endif
        
                float3 doubleSidedConstants = float3(1.0, 1.0, 1.0);
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
        doubleSidedConstants = float3( 1.0,  1.0, -1.0);
        #endif
        
        
                // normal delivered to master node
                float3 normalSrc = float3(0.0f, 0.0f, 1.0f);
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
        normalSrc = surfaceDescription.Normal;
        #endif
        
        
                // compute world space normal
        #if _NORMAL_DROPOFF_TS
                GetNormalWS(fragInputs, normalSrc, surfaceData.normalWS, doubleSidedConstants);
        #elif _NORMAL_DROPOFF_OS
        		surfaceData.normalWS = TransformObjectToWorldNormal(normalSrc);
        #elif _NORMAL_DROPOFF_WS
        		surfaceData.normalWS = normalSrc;
        #endif
        
                surfaceData.geomNormalWS = fragInputs.tangentToWorld[2];
                surfaceData.tangentWS = normalize(fragInputs.tangentToWorld[0].xyz);    // The tangent is not normalize in tangentToWorld for mikkt. TODO: Check if it expected that we normalize with Morten. Tag: SURFACE_GRADIENT
        
        #if HAVE_DECALS
                if (_EnableDecals)
                {
                    // Both uses and modifies 'surfaceData.normalWS'.
                    DecalSurfaceData decalSurfaceData = GetDecalSurfaceData(posInput, surfaceDescription.Alpha);
                    ApplyDecalToSurfaceData(decalSurfaceData, surfaceData);
                }
        #endif
        
                surfaceData.tangentWS = Orthonormalize(surfaceData.tangentWS, surfaceData.normalWS);
        
                // By default we use the ambient occlusion with Tri-ace trick (apply outside) for specular occlusion as PBR master node don't have any option
                surfaceData.specularOcclusion = GetSpecularOcclusionFromAmbientOcclusion(ClampNdotV(dot(surfaceData.normalWS, V)), surfaceData.ambientOcclusion, PerceptualSmoothnessToRoughness(surfaceData.perceptualSmoothness));
        
        #ifdef DEBUG_DISPLAY
                if (_DebugMipMapMode != DEBUGMIPMAPMODE_NONE)
                {
                    // TODO: need to update mip info
                    surfaceData.metallic = 0;
                }
        
                // We need to call ApplyDebugToSurfaceData after filling the surfarcedata and before filling builtinData
                // as it can modify attribute use for static lighting
                ApplyDebugToSurfaceData(fragInputs.tangentToWorld, surfaceData);
        #endif
            }
        
            void GetSurfaceAndBuiltinData(FragInputs fragInputs, float3 V, inout PositionInputs posInput, out SurfaceData surfaceData, out BuiltinData builtinData)
            {
        #ifdef LOD_FADE_CROSSFADE // enable dithering LOD transition if user select CrossFade transition in LOD group
                LODDitheringTransition(ComputeFadeMaskSeed(V, posInput.positionSS), unity_LODFade.x);
        #endif
        
                float3 doubleSidedConstants = float3(1.0, 1.0, 1.0);
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
        doubleSidedConstants = float3( 1.0,  1.0, -1.0);
        #endif
        
        
                ApplyDoubleSidedFlipOrMirror(fragInputs, doubleSidedConstants);
        
                SurfaceDescriptionInputs surfaceDescriptionInputs = FragInputsToSurfaceDescriptionInputs(fragInputs, V);
                SurfaceDescription surfaceDescription = SurfaceDescriptionFunction(surfaceDescriptionInputs);
        
                // Perform alpha test very early to save performance (a killed pixel will not sample textures)
                // TODO: split graph evaluation to grab just alpha dependencies first? tricky..
        
        
                BuildSurfaceData(fragInputs, surfaceDescription, V, posInput, surfaceData);
        
                // Builtin Data
                // For back lighting we use the oposite vertex normal
                InitBuiltinData(posInput, surfaceDescription.Alpha, surfaceData.normalWS, -fragInputs.tangentToWorld[2], fragInputs.texCoord1, fragInputs.texCoord2, builtinData);
        
        #if defined(KEYWORD_PERMUTATION_0) || defined(KEYWORD_PERMUTATION_1) || defined(KEYWORD_PERMUTATION_2) || defined(KEYWORD_PERMUTATION_3) || defined(KEYWORD_PERMUTATION_4) || defined(KEYWORD_PERMUTATION_5) || defined(KEYWORD_PERMUTATION_6) || defined(KEYWORD_PERMUTATION_7)
        builtinData.emissiveColor = surfaceDescription.Emission;
        #endif
        
        
                PostInitBuiltinData(V, posInput, surfaceData, builtinData);
            }
        
            //-------------------------------------------------------------------------------------
            // Pass Includes
            //-------------------------------------------------------------------------------------
                #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassForward.hlsl"
            //-------------------------------------------------------------------------------------
            // End Pass Includes
            //-------------------------------------------------------------------------------------
        
            ENDHLSL
        }
        
    }
    CustomEditor "UnityEditor.Rendering.HighDefinition.HDPBRLitGUI"
    FallBack "Hidden/Shader Graph/FallbackError"
}
