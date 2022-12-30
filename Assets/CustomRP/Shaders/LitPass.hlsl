#ifndef LITPASS_HLSL
#define LITPASS_HLSL

#include "../ShaderLibrary/Surface.hlsl"
#include "../ShaderLibrary/Shadow.hlsl"
#include "../ShaderLibrary/Light.hlsl"
#include "../ShaderLibrary/BRDF.hlsl"
#include "../ShaderLibrary/GI.hlsl"
#include "../ShaderLibrary/Lighting.hlsl"




struct Atrributes
{
    float3 position:POSITION;
    float2 baseUV:TEXCOORD0;
    float3 objectNormal:NORMAL;
    float4 tangentOS:TANGENT;
    GI_ATTRIBUTE_DATA
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varings
{
    float4 clipPosition:SV_POSITION;
    float2 baseUV:VAR_BASE_UV;
#if defined(_DETAIL_MAP)
    float2 detailUV:VAR_DETAIL_UV;
#endif
    float3 worldNormal:VAR_NORMAL;
    float3 worldPos:BAR_POSITION;
#if defined(_NORMAL_MAP)
    float4 tangentWS:VAR_TANGENT;
#endif
    GI_VARYINGS_DATA
    UNITY_VERTEX_INPUT_INSTANCE_ID
};



Varings LitPassVertex(Atrributes input)
{
    Varings output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input,output);
    TRANSFER_GI_DATA(input,output);
    float3 worldPos=TransformObjectToWorld(input.position);
    output.clipPosition=TransformWorldToHClip(worldPos);
    output.worldNormal=TransformObjectToWorldNormal(input.objectNormal);
    output.worldPos=TransformObjectToWorld(input.position);
    output.baseUV=TransformBaseUV(input.baseUV);
#if defined(_DETAIL_MAP)
    output.detailUV=TransformDetailUV(input.baseUV);
#endif
#if defined(_NORMAL_MAP)
    output.tangentWS=float4(TransformObjectToWorldDir(input.tangentOS.xyz),input.tangentOS.w);
#endif
    return output;
}
float4 LitPassFragment(Varings input): SV_TARGET
{
    UNITY_SETUP_INSTANCE_ID(input);
    ClipLOD(input.clipPosition.xy,unity_LODFade.x);
    InputConfig config = GetInputConfig(input.baseUV);
	#if defined(_MASK_MAP)
		config.useMask = true;
	#endif
	#if defined(_DETAIL_MAP)
		config.detailUV = input.detailUV;
		config.useDetail = true;
	#endif
    float4 base= GetBase(config);
    Surface surface;
    surface.position=input.worldPos;
#if defined(_NORMAL_MAP)
    surface.normal=NormalTangentToWorld(GetNormalTS(config),input.worldNormal,input.tangentWS);
    surface.interpolatedNormal=input.worldNormal;
#else
    surface.normal = normalize(input.worldNormal);
	surface.interpolatedNormal = surface.normal;
#endif
    surface.color=base.rgb;
    surface.alpha=base.a;
    surface.metallic=GetMetallic(config);
    surface.smoothness=GetSmoothness(config);
    surface.viewDirection=normalize(_WorldSpaceCameraPos-input.worldPos);
    surface.depth=-TransformWorldToView(input.worldPos).z;
    surface.dither=InterleavedGradientNoise(input.clipPosition.xy, 0);
    surface.fresnelStrength=GetFresnel(config);
    surface.occlusion=GetOcclusion(config);
    
    
#if defined(__PREMULTIPLY_ALPHA)
    BRDF brdf=GetBRDF(surface,true);
#else
    BRDF brdf=GetBRDF(surface);
#endif
    GI gi=GetGI(GI_FRAGMENT_DATA(input),surface,brdf);
    float3 color=GetLighting(surface,brdf,gi);
    color+=GetEmission(config);
#if defined(_CLIPPING)
    clip(surface.alpha-GetCutoff(config));
#endif
    return float4(color,surface.alpha);
}

#endif