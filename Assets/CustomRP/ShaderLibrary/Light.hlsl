#ifndef LIGHT_HLSL
#define LIGHT_HLSL



#define MAX_DIRECTIONAL_LIGHT_COUNT 8
#define MAX_OTHER_LIGHT_COUNT 64

CBUFFER_START(_CustomLight)
    
int _DirectionalLightCount;
float4 _DirectionalLightColors[MAX_DIRECTIONAL_LIGHT_COUNT];
float4 _DirectionalLightDirs[MAX_DIRECTIONAL_LIGHT_COUNT];
float4 _DirectionalLightShadowData[MAX_DIRECTIONAL_LIGHT_COUNT];

int _OtherLightCount;
float4 _OtherLightColors[MAX_OTHER_LIGHT_COUNT];
float4 _OtherLightPositions[MAX_OTHER_LIGHT_COUNT];
float4 _OtherLightDirections[MAX_OTHER_LIGHT_COUNT];
float4 _OtherLightSpotAngles[MAX_OTHER_LIGHT_COUNT];
float4 _OtherLightShadowData[MAX_OTHER_LIGHT_COUNT];

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
    data.strength=_DirectionalLightShadowData[lightIndex].x;
    data.tileIndex=_DirectionalLightShadowData[lightIndex].y+shadowData.cascadeIndex;
    data.normalBias=_DirectionalLightShadowData[lightIndex].z;
    data.shadowMaskChannel = _DirectionalLightShadowData[lightIndex].w;
    return data;
}
OtherShadowData GetOtherShadowData(int index,ShadowData shadowData)
{
    OtherShadowData data;
    data.strength=_OtherLightShadowData[index].x;
    data.tileIndex=_OtherLightShadowData[index].y;
    data.shadowMaskChannel=_OtherLightShadowData[index].w;
    data.lightPositionWS=0;
    data.SpotDirectionWS=0;
    data.isPoint = _OtherLightShadowData[index].z == 1.0;
    data.lightDirectionWS=0;
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
Light GetOtherLight(int index, Surface surface, ShadowData shadowData)
{
    Light outLight;
    float3 position=_OtherLightPositions[index].xyz;
    outLight.color=_OtherLightColors[index].xyz;
    float3 ray =position-surface.position;
    float3 spotDirection = _OtherLightDirections[index].xyz;
    float sqrDistance=max(dot(ray,ray),0.00001);
    float rangeAttenuation=Square(saturate(1.0-Square(sqrDistance*_OtherLightPositions[index].w)));
    outLight.direction=normalize(ray);
    


    float4 spotAngles=_OtherLightSpotAngles[index];
    float spotAttenuation=Square(saturate(dot(spotDirection,outLight.direction)
    *spotAngles.x+spotAngles.y));

    OtherShadowData data=GetOtherShadowData(index,shadowData);
    data.lightPositionWS=position;
    data.SpotDirectionWS=spotDirection;
    data.lightDirectionWS=outLight.direction;

    outLight.attenuation=GetOtherShadowAttenuation(data,shadowData,surface)* spotAttenuation* rangeAttenuation/sqrDistance;
    return outLight;
}
int GetDirectionlLightCount()
{
    return _DirectionalLightCount;
}
int GetOtherLightCount()
{
    return _OtherLightCount;
}

#endif