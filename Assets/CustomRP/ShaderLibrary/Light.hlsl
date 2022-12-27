#ifndef LIGHT_HLSL
#define LIGHT_HLSL



#define MAX_DIRECTIONAL_LIGHT_COUNT 8

CBUFFER_START(_CustomLight)
    
int _DirectionalLightCount;
float4 _DirectionalLightColors[MAX_DIRECTIONAL_LIGHT_COUNT];
float4 _DirectionalLightDirs[MAX_DIRECTIONAL_LIGHT_COUNT];
float4 _DirectionalLightShadowData[MAX_DIRECTIONAL_LIGHT_COUNT];

CBUFFER_END


struct Light
{
    float3 color;
    float3 direction;
    float attenuation;
};
DirectionalShadowData GetDirectionalShadowData(int lightIndex,ShadowData shadowData)
{
    DirectionalShadowData data;
    data.strength=_DirectionalLightShadowData[lightIndex].x*shadowData.strength;
    data.tileIndex=_DirectionalLightShadowData[lightIndex].y+shadowData.cascadeIndex;
    data.normalBias=_DirectionalLightShadowData[lightIndex].z;
    return data;
}

Light GetDirectionLight(int index,Surface surface,ShadowData shadowData)
{
    Light outLight;
    outLight.color=_DirectionalLightColors[index].xyz;
    outLight.direction=_DirectionalLightDirs[index].xyz;
    DirectionalShadowData data= GetDirectionalShadowData(index,shadowData);
    outLight.attenuation=GetDirectionalShadowAttenuation(data,shadowData,surface);
    return outLight;
}
int GetDirectionlLightCount()
{
    return _DirectionalLightCount;
}


#endif