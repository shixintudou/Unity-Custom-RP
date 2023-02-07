#ifndef SHADOWCASTERPASS_HLSL
#define SHADOWCASTERPASS_HLSL

bool _ShadowPancaking;

struct Atrributes
{
    float3 position:POSITION;
    float2 baseUV:TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varings
{
    float4 clipPosition:SV_POSITION;
    float2 baseUV:VAR_BASE_UV;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};



Varings ShadowCasterPassVertex(Atrributes input)
{
    Varings output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input,output);
    float3 worldPos=TransformObjectToWorld(input.position);
    output.clipPosition=TransformWorldToHClip(worldPos);

    if(_ShadowPancaking)
    {
    #if UNITY_REVERSED_Z
        output.clipPosition.z=min(output.clipPosition.z,output.clipPosition.w*UNITY_NEAR_CLIP_VALUE);
    #else
        output.clipPosition.z=max(output.clipPosition.z,output.clipPosition.w*UNITY_NEAR_CLIP_VALUE);
    #endif
    }
    

    output.baseUV=TransformBaseUV(input.baseUV);
    return output;
}
void ShadowCasterPassFragment(Varings input)
{
    UNITY_SETUP_INSTANCE_ID(input);
    InputConfig config = GetInputConfig(input.baseUV);
    float4 base= GetBase(config);
    ClipLOD(input.clipPosition.xy,unity_LODFade.x);
#if defined(_SHADOWS_CLIP)
    clip(base.a-GetCutoff(config));
#elif defined(_SHADOW_DITHER)
    float dither = InterleavedGradientNoise(input.clipPosition.xy, 0);
	clip(base.a - dither);
#endif

}

#endif