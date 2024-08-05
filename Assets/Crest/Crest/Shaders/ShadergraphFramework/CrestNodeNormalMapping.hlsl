// Crest Ocean System

// Copyright 2020 Wave Harmonic Ltd

#include "../OceanConstants.hlsl"
#include "../OceanInputsDriven.hlsl"
#include "../OceanGlobals.hlsl"
#include "../OceanHelpersNew.hlsl"
#include "OceanNormalMapping.hlsl"

void OceanNormals_half
(
	in const float3 i_oceanPosScale0,
	in const float3 i_oceanPosScale1,
	in const float4 i_oceanParams0,
	in const float4 i_oceanParams1,
	in const float i_sliceIndex0,
	in const Texture2D<float4> i_normals,
	in const half i_normalsScale,
	in const half i_normalsStrength,
	in const float i_lodAlpha,
	in const float2 i_positionXZWSUndisplaced,
	in const half2 i_flow,
	in const half3 viewNorm,
	in const bool i_isUnderwater,
	in const half i_minReflectionDirectionY,
	out half3 o_normalTS
)
{
	o_normalTS = half3(0.0, 0.0, 1.0);

	// TODO pass this in? it needs _normalScrollSpeeds and _farNormalsWeight
	const PerCascadeInstanceData instanceData = _CrestPerCascadeInstanceData[i_sliceIndex0];
	
	CascadeParams cascadeData0 = MakeCascadeParams(i_oceanPosScale0, i_oceanParams0);
	CascadeParams cascadeData1 = MakeCascadeParams(i_oceanPosScale1, i_oceanParams1);

	// Normal - geom + normal mapping. Subsurface scattering.
	const float3 uv_slice_smallerLod = WorldToUV(i_positionXZWSUndisplaced, cascadeData0, i_sliceIndex0);
	const float3 uv_slice_biggerLod = WorldToUV(i_positionXZWSUndisplaced, cascadeData1, i_sliceIndex0 + 1.0);
	const float wt_smallerLod = (1.0 - i_lodAlpha) * i_oceanParams0.z;
	const float wt_biggerLod = (1.0 - wt_smallerLod) * i_oceanParams1.z;
	float3 dummy = 0.0;
	real3 n_geom = half3(0.0, 1.0, 0.0);
	real sss = 0.0;
	if (wt_smallerLod > 0.001) SampleDisplacementsNormals(_LD_TexArray_AnimatedWaves, uv_slice_smallerLod, wt_smallerLod, i_oceanParams0.w, i_oceanParams0.x, dummy, n_geom.xz, sss);
	if (wt_biggerLod > 0.001) SampleDisplacementsNormals(_LD_TexArray_AnimatedWaves, uv_slice_biggerLod, wt_biggerLod, i_oceanParams1.w, i_oceanParams1.x, dummy, n_geom.xz, sss);
	n_geom = normalize(n_geom);

	if (i_isUnderwater) n_geom = -n_geom;
	real3 n_pixel = n_geom;
//#if _APPLYNORMALMAPPING_ON
#if defined(CREST_FLOW_ON)
	ApplyNormalMapsWithFlow(i_flow, i_positionXZWSUndisplaced, i_normals, i_normalsScale, i_normalsStrength, i_lodAlpha, cascadeData0, instanceData, n_pixel);
#else
	n_pixel.xz += (i_isUnderwater ? -1.0 : 1.0) * SampleNormalMaps(i_positionXZWSUndisplaced, i_normals, i_normalsScale, i_normalsStrength, i_lodAlpha, cascadeData0, instanceData);
	n_pixel = normalize(n_pixel);
#endif
//#endif

	// Make sure normal faces viewer. This has nothing to do with reflections being above horizon.
	/*const half3 viewNormTS = viewNorm.xzy;
	half dp = dot(o_normalTS, viewNormTS);
	if (dp < 0.0)
	{
		o_normalTS -= 2.0 * dp * viewNormTS;
	}*/

	if (!i_isUnderwater)
	{
		float3 refl = reflect(-viewNorm, n_pixel);
		if (refl.y < i_minReflectionDirectionY)
		{
			// Find the normal that keeps the reflection direction above the horizon. Compute the reflection dir that does work, normalize it, and
			// then normal is half vector between this good refl dir and view dir
			float3 FL = refl;
			FL.y = i_minReflectionDirectionY;
			FL = normalize(FL);
			n_pixel = normalize(FL + viewNorm);
		}
	}

	o_normalTS = n_pixel.xzy;

	// Seems like the tangent frame has a different handedness - this seems to work well in SRP.
	o_normalTS.y *= -1.0;
}
