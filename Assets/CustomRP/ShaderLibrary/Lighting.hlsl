#ifndef LIGHTING_HLSL
#define LIGHTING_HLSL



float3 InComingLight(Surface surface,Light light)
{
    return saturate(dot(surface.normal,light.direction)*light.attenuation)*light.color;
}

float3 GetLighting(Surface surface,BRDF brdf,Light light)
{
    return InComingLight(surface,light)*DirectBRDF(surface,brdf,light);
}

float3 GetLighting(Surface surface,BRDF brdf,GI gi)
{
    int t=GetDirectionlLightCount();
    ShadowData data=GetShadowData(surface);
    data.shadowMask=gi.shadowMask;
    float3 color=IndirectBRDF(surface,brdf,gi.diffuse,gi.specular);
    for(int i=0;i<t;i++)
    {
        Light light=GetDirectionLight(i,surface,data);
        color+= GetLighting(surface,brdf,light);
    }   
    for (int j = 0; j < GetOtherLightCount(); j++) 
    {
		Light light = GetOtherLight(j, surface, data);
		color += GetLighting(surface, brdf, light);
	}
    return color;
}

#endif