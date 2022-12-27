#ifndef UNLIT_PASS_HLSL
#define UNLIT_PASS_HLSL


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

Varings UnlitVertexShader(Atrributes input) 
{
    Varings output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input,output);
    float3 worldPos=TransformObjectToWorld(input.position);
    output.clipPosition=TransformWorldToHClip(worldPos);
    output.baseUV=TransformBaseUV(input.baseUV);
    return output;
}
float4 UnlitFragmentShader(Varings input): SV_TARGET
{
    UNITY_SETUP_INSTANCE_ID(input);
    float4 base= GetBase(input.baseUV);
#if defined(_CLIPPING)
    clip(base.a-GetCutoff(input.baseUV));
#endif
    return base;
}



#endif