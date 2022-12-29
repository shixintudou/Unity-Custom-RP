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
    GI_ATTRIBUTE_DATA
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varings
{
    float4 clipPosition:SV_POSITION;
    float2 baseUV:VAR_BASE_UV;
    float3 worldNormal:VAR_NORMAL;
    float3 worldPos:BAR_POSITION;
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
    return output;
}
float4 LitPassFragment(Varings input): SV_TARGET
{
    UNITY_SETUP_INSTANCE_ID(input);
    ClipLOD(input.clipPosition.xy,unity_LODFade.x);
    float4 base= GetBase(input.baseUV);
    Surface surface;
    surface.position=input.worldPos;
    surface.normal=normalize(input.worldNormal);
    surface.color=base.rgb;
    surface.alpha=base.a;
    surface.metallic=GetMetallic(input.baseUV);
    surface.smoothness=GetSmoothness(input.baseUV);
    surface.viewDirection=normalize(_WorldSpaceCameraPos-input.worldPos);
    surface.depth=-TransformWorldToView(input.worldPos).z;
    surface.dither=InterleavedGradientNoise(input.clipPosition.xy, 0);
    surface.fresnelStrength=GetFresnel(input.baseUV);
    
#if defined(__PREMULTIPLY_ALPHA)
    BRDF brdf=GetBRDF(surface,true);
#else
    BRDF brdf=GetBRDF(surface);
#endif
    GI gi=GetGI(GI_FRAGMENT_DATA(input),surface,brdf);
    float3 color=GetLighting(surface,brdf,gi);
    color+=GetEmission(input.baseUV);
#if defined(_CLIPPING)
    clip(surface.alpha-GetCutoff(input.baseUV));
#endif
    return float4(color,surface.alpha);
}

#endif