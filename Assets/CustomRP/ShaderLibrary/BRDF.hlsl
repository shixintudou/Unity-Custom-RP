#ifndef BRDF_HLSL
#define BRDF_HLSL

#define MIN_REFLECTIVITY 0.04


struct BRDF
{
    float3 diffuse;
    float3 specular;
    float roughness;
    float perceptualRoughness;
    float fresnel;
};
float DiffuseResult(float metallic)
{
    return 1-MIN_REFLECTIVITY-metallic*(1-MIN_REFLECTIVITY);
}
float SpecularStrength(Surface surface,BRDF brdf,Light light)
{
    float r2=Square(brdf.roughness);
    float3 h=SafeNormalize(light.direction+surface.viewDirection);
    float d=Square(saturate(dot(surface.normal,h)))*(r2-1.0)+1.0001;
    float m=max(0.1,Square(saturate(dot(light.direction,h))));
    float n=brdf.roughness*4.0+2.0;
    return r2/(Square(d)*m*n);
}
float3 DirectBRDF(Surface surface,BRDF brdf,Light light)
{
    return SpecularStrength(surface,brdf,light)*brdf.specular+brdf.diffuse;
}
float3 IndirectBRDF(Surface surface,BRDF brdf,float3 diffuse, float3 specular)
{
    float fresnelStrength =surface.fresnelStrength *
		Pow4(1.0 - saturate(dot(surface.normal, surface.viewDirection)));
    float3 reflection=specular*lerp(brdf.specular, brdf.fresnel, fresnelStrength);
    reflection/=brdf.roughness*brdf.roughness+1.0;
    return diffuse*brdf.diffuse+reflection;
}
BRDF GetBRDF(Surface surface,bool applyAlphaToDiffuse=false)
{
    BRDF brdf;
    float diffuseResult=DiffuseResult(surface.metallic);
    brdf.diffuse=surface.color*diffuseResult;
    if(applyAlphaToDiffuse)
    brdf.diffuse*=surface.alpha;
    brdf.specular=lerp(MIN_REFLECTIVITY,surface.color,surface.metallic);
    brdf.perceptualRoughness=PerceptualSmoothnessToPerceptualRoughness(surface.smoothness);
    brdf.roughness = PerceptualRoughnessToRoughness(brdf.perceptualRoughness);
    brdf.fresnel = saturate(surface.smoothness + 1.0 - diffuseResult);
    return brdf;
}


#endif