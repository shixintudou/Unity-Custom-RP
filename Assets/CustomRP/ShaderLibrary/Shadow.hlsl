#ifndef Shadow_HLSL
#define Shadow_HLSL

#define MAX_SHADOWED_DIRECTIONAL_LIGHT_COUNT 4
#define MAX_CASCADE_COUNT 4

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Shadow/ShadowSamplingTent.hlsl"

#if defined(_DIRECTIONAL_PCF3)
	#define DIRECTIONAL_FILTER_SAMPLES 4
	#define DIRECTIONAL_FILTER_SETUP SampleShadow_ComputeSamples_Tent_3x3
#elif defined(_DIRECTIONAL_PCF5)
	#define DIRECTIONAL_FILTER_SAMPLES 9
	#define DIRECTIONAL_FILTER_SETUP SampleShadow_ComputeSamples_Tent_5x5
#elif defined(_DIRECTIONAL_PCF7)
	#define DIRECTIONAL_FILTER_SAMPLES 16
	#define DIRECTIONAL_FILTER_SETUP SampleShadow_ComputeSamples_Tent_7x7
#endif

TEXTURE2D_SHADOW(_DirectionalShadowAtlas);
#define SHADOW_SAMPLER sampler_linear_clamp_compare
SAMPLER_CMP(SHADOW_SAMPLER);

CBUFFER_START(_CustomShadows)
    int _CascadeCount;
    float4 _ShadowDistanceFade;  
    float4 _ShadowAtlasSize;
    float4 _CascadeCullingSpheres[MAX_CASCADE_COUNT];
    float4 _CascadeData[MAX_CASCADE_COUNT];
    float4x4 _DirectionalShadowMatrices[MAX_SHADOWED_DIRECTIONAL_LIGHT_COUNT*MAX_CASCADE_COUNT];
CBUFFER_END

struct DirectionalShadowData
{
    float strength;
    int tileIndex;
    float normalBias;
    int shadowMaskChannel;
};
struct ShadowMask
{
    bool distance;
    bool always;
    float4 shadows;
};

struct ShadowData
{
    int cascadeIndex;
    float strength;
    float cascadeBlend;
    ShadowMask shadowMask;
};
float FadedShadowStrength(float distance,float scale,float fade)
{
    return saturate((1.0-distance*scale)*fade);
}

ShadowData GetShadowData(Surface surface)
{
    ShadowData data;
    data.shadowMask.distance=false;
    data.shadowMask.always=false;
    data.shadowMask.shadows=1.0;
    data.cascadeBlend=1;
    data.strength=FadedShadowStrength(surface.depth,_ShadowDistanceFade.x,_ShadowDistanceFade.y);
    int i;
    for(i=0;i<_CascadeCount;i++)
    {
        float4 sphere=_CascadeCullingSpheres[i];
        float distanceSqr=SquaredDistance(surface.position,sphere.xyz);
        if(distanceSqr<sphere.w)
        {
            float fade=FadedShadowStrength(distanceSqr,_CascadeData[i].x,_ShadowDistanceFade.z);
            if(i==_CascadeCount-1)
            {
                data.strength*=fade;
            }
            else
            {
                data.cascadeBlend=fade;
            }
            break;
        }
        
    }
    if(i==_CascadeCount)
        data.strength=0;
    #if defined(_CASCADE_BLEND_DITHER)
        else if(data.cascadeBlend<surface.dither)
        {
            i+=1;
        }
    #endif
    #if !defined(_CASCADE_BLEND_SOFT)
        data.cascadeBlend=1.0;
    #endif
    data.cascadeIndex=i;
    return data;
}

float SampleDirectionalShadowAtlas (float3 positionSTS) 
{
	return SAMPLE_TEXTURE2D_SHADOW( _DirectionalShadowAtlas, SHADOW_SAMPLER, positionSTS);
}


float FilterDirectionalShadow(float3 positionSTS)
{
    #if defined(DIRECTIONAL_FILTER_SETUP)
        float weights[DIRECTIONAL_FILTER_SAMPLES];
		float2 positions[DIRECTIONAL_FILTER_SAMPLES];
		float4 size = _ShadowAtlasSize.yyxx;
		DIRECTIONAL_FILTER_SETUP(size, positionSTS.xy, weights, positions);
		float shadow = 0;
		for (int i = 0; i < DIRECTIONAL_FILTER_SAMPLES; i++) 
        {
			shadow += weights[i] * SampleDirectionalShadowAtlas(float3(positions[i].xy, positionSTS.z));
		}
        return shadow;
    #else
        return SampleDirectionalShadowAtlas(positionSTS);
    #endif
}
float GetBakedShadow(ShadowMask mask,int channel)
{
    float shadow=1.0;
    if(mask.distance||mask.always)
    {
        if(channel>=0)
        {   
            shadow=mask.shadows[channel];
        }
        
    }
    return shadow;
}
float GetBakedShadow(ShadowMask mask,float strength,int channel)
{
    if(mask.distance||mask.always)
        return lerp(1.0,GetBakedShadow(mask,channel),strength);
    return 1.0;
}
float MixBakedAndRealtimeShadow(ShadowData data,float shadow,float strength,int channel)
{
    float baked=GetBakedShadow(data.shadowMask,channel);
    if(data.shadowMask.distance)
    {
        shadow=lerp(baked,shadow,data.strength);
        return lerp(1.0,shadow,strength);
    }
    else if(data.shadowMask.always)
    {
        shadow = lerp(1.0, shadow, data.strength);
		shadow = min(baked, shadow);
		return lerp(1.0, shadow, strength);
    }
    return lerp(1.0,shadow,strength*data.strength);
}
float GetCascadeShadow(DirectionalShadowData directional,ShadowData data,Surface surface)
{
    if(directional.strength<0.0)
        return 1.0;
    #if !defined(_RECEIVE_SHADOWS)
		return 1.0;
	#endif
    float3 normalBias=surface.interpolatedNormal*(directional.normalBias* _CascadeData[data.cascadeIndex].y);
    float3 positionSTS=mul(_DirectionalShadowMatrices[directional.tileIndex],float4(surface.position+normalBias,1.0)).xyz;
    float shadow=FilterDirectionalShadow(positionSTS);
    if(data.cascadeBlend<1.0)
    {
        normalBias=surface.interpolatedNormal*(directional.normalBias*_CascadeData[data.cascadeIndex+1].y);
        positionSTS=mul(_DirectionalShadowMatrices[directional.tileIndex+1],float4(surface.position+normalBias,1.0)).xyz;
        shadow=lerp(FilterDirectionalShadow(positionSTS),shadow,data.cascadeBlend);
    }
    return shadow;
}
float GetDirectionalShadowAttenuation(DirectionalShadowData directional,ShadowData data,Surface surface)
{
    #if !defined(_RECEIVE_SHADOWS)
		return 1.0;
	#endif
    float shadow;
	if (directional.strength*data.strength <= 0.0) 
    {
		shadow = GetBakedShadow(data.shadowMask,abs(directional.strength),directional.shadowMaskChannel);
	}
	else 
    {
		shadow = GetCascadeShadow(directional, data, surface);
		shadow = MixBakedAndRealtimeShadow(data,shadow,directional.strength,directional.shadowMaskChannel);
	}
	return shadow;
}



#endif