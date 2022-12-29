#ifndef BRDF_HLSL
#define BRDF_HLSL

#define MIN_REFLECTIVITY 0.04


struct BRDF
{
    float3 diffuse;
    float3 specular;
    float roughness;
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
    return diffuse*brdf.diffuse;
}
BRDF GetBRDF(Surface surface,bool applyAlphaToDiffuse=false)
{
    BRDF brdf;
    brdf.diffuse=surface.color*DiffuseResult(surface.metallic);
    if(applyAlphaToDiffuse)
    brdf.diffuse*=surface.alpha;
    brdf.specular=lerp(MIN_REFLECTIVITY,surface.color,surface.metallic);
    float perceptualRoughness=PerceptualSmoothnessToPerceptualRoughness(surface.smoothness);
    brdf.roughness = PerceptualRoughnessToRoughness(perceptualRoughness);
    return brdf;
}


#endif