#ifndef SURFACE_HLSL
#define SURFACE_HLSL

struct Surface
{
    float3 normal;
    float3 color;
    float3 position;
    float alpha;
    float metallic;
    float smoothness;
    float3 viewDirection;
    float depth;
    float dither;
};



#endif