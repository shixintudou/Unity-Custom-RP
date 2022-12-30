#ifndef COMMON_HLSL_INCLUDE
#define COMMON_HLSL_INCLUDE

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
#include "UnityInput.hlsl"

#define UNITY_MATRIX_M unity_ObjectToWorld
#define UNITY_MATRIX_I_M unity_WorldToObject
#define UNITY_MATRIX_V unity_MatrixV
#define UNITY_MATRIX_VP unity_MatrixVP
#define UNITY_MATRIX_P glstate_matrix_projection
#if defined(_SHADOW_MASK_DISTANCE)||defined(_SHADOW_MASK_ALWAYS)
	#define SHADOWS_SHADOWMASK
#endif

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"



float3 DecodeNormal(float4 sample,float scale)
{
    #if defined(UNITY_NO_DXT5nm)
        return UnpackNormalRGB(sample,scale);
    #else
        return UnpackNormalmapRGorAG(sample,scale);
    #endif
}
float3 NormalTangentToWorld(float3 normalTS,float3 normalWS,float4 tangentWS)
{
    float3x3 tangentToWorld=CreateTangentToWorld(normalWS,tangentWS.xyz,tangentWS.w);
    return TransformTangentToWorld(normalTS,tangentToWorld);
}
float Square(float v)
{
    return v*v;
}

float SquaredDistance(float3 a,float3 b)
{
    return dot(a-b,a-b);
}

void ClipLOD(float2 clipPosition,float fade)
{
    #if defined(LOD_FADE_CROSSFADE)
        float dither=InterleavedGradientNoise(clipPosition, 0);
        clip(fade+ (fade < 0.0 ? dither : -dither));
    #endif
}

#endif