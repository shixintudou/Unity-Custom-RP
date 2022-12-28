#ifndef GI_HLSL
#define GI_HLSL

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/EntityLighting.hlsl"

#if defined(LIGHTMAP_ON)
	#define GI_ATTRIBUTE_DATA float2 lightMapUV : TEXCOORD1;
	#define GI_VARYINGS_DATA float2 lightMapUV : VAR_LIGHT_MAP_UV;
	#define TRANSFER_GI_DATA(input, output) output.lightMapUV = input.lightMapUV*unity_LightmapST.xy+unity_LightmapST.zw;
	#define GI_FRAGMENT_DATA(input) input.lightMapUV
#else
    #define GI_ATTRIBUTE_DATA
    #define GI_VARYINGS_DATA
    #define TRANSFER_GI_DATA(input, output)
    #define GI_FRAGMENT_DATA(input) 0.0
#endif
    
TEXTURE2D(unity_Lightmap);
SAMPLER(samplerunity_Lightmap);
TEXTURE3D_FLOAT(unity_ProbeVolumeSH);
SAMPLER(samplerunity_ProbeVolumeSH);
TEXTURE2D(unity_ShadowMask);
SAMPLER(samplerunity_ShadowMask);

struct GI
{
    float3 diffuse;
    ShadowMask shadowMask;
};

float3 SampleLightmap(float2 lightmapUV)
{
    #if defined(LIGHTMAP_ON)
        return SampleSingleLightmap(TEXTURE2D_ARGS(unity_Lightmap,samplerunity_Lightmap),
        lightmapUV,float4(1,1,0,0),
        #if defined(UNITY_LIGHTMAP_FULL_HDR)
				false,
			#else
				true,
			#endif
			float4(LIGHTMAP_HDR_MULTIPLIER, LIGHTMAP_HDR_EXPONENT, 0.0, 0.0));
    #else
        return 0.0;
    #endif
}
float3 SampleLightprob(Surface surface)
{
    #if defined(LIGHTMAP_ON)
		return 0.0;
	#else
        if(unity_ProbeVolumeParams.x)
        {
            return SampleProbeVolumeSH4(
				TEXTURE3D_ARGS(unity_ProbeVolumeSH, samplerunity_ProbeVolumeSH),
				surface.position, surface.normal,
				unity_ProbeVolumeWorldToObject,
				unity_ProbeVolumeParams.y, unity_ProbeVolumeParams.z,
				unity_ProbeVolumeMin.xyz, unity_ProbeVolumeSizeInv.xyz
			);
        }
        else
        {
            float4 coefficients[7];
            coefficients[0] = unity_SHAr;
            coefficients[1] = unity_SHAg;
            coefficients[2] = unity_SHAb;
            coefficients[3] = unity_SHBr;
            coefficients[4] = unity_SHBg;
            coefficients[5] = unity_SHBb;
            coefficients[6] = unity_SHC;
            return max(0.0, SampleSH9(coefficients, surface.normal));
        }
	#endif
}
float4 SampleBakedShadow(float2 lightmapUV,Surface surface)
{
    #if defined(LIGHTMAP_ON)
        return SAMPLE_TEXTURE2D(unity_ShadowMask,samplerunity_ShadowMask,lightmapUV);
    #else
        if (unity_ProbeVolumeParams.x) 
        {
			return SampleProbeOcclusion(
				TEXTURE3D_ARGS(unity_ProbeVolumeSH, samplerunity_ProbeVolumeSH),
				surface.position, unity_ProbeVolumeWorldToObject,
				unity_ProbeVolumeParams.y, unity_ProbeVolumeParams.z,
				unity_ProbeVolumeMin.xyz, unity_ProbeVolumeSizeInv.xyz
			);
		}
        else
        {
            return unity_ProbesOcclusion;
        }
    #endif
}


GI GetGI(float2 lightmapUV,Surface surface)
{
    GI gi;
    gi.diffuse=SampleLightmap(lightmapUV)+SampleLightprob(surface);
    gi.shadowMask.distance=false;
    gi.shadowMask.always=false;
    gi.shadowMask.shadows=1.0;
    #if defined(_SHADOW_MASK_DISTANCE)
        gi.shadowMask.distance=true;
        gi.shadowMask.shadows=SampleBakedShadow(lightmapUV,surface);
    #elif defined(_SHADOW_MASK_ALWAYS)
        gi.shadowMask.always=true;
        gi.shadowMask.shadows=SampleBakedShadow(lightmapUV,surface);
    #endif
    return gi;
}




#endif