
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Collections;

public class Lighting
{
    const string bufferName = "Lighting";
    CommandBuffer buffer = new CommandBuffer { name = bufferName };
    static int directionalLightCountID = Shader.PropertyToID("_DirectionalLightCount");
    static int directionalLightColorID = Shader.PropertyToID("_DirectionalLightColors");
    static int directionalLightDirID = Shader.PropertyToID("_DirectionalLightDirs");
    static int dirLightShadowDataID = Shader.PropertyToID("_DirectionalLightShadowData");

    static int otherLightCountID = Shader.PropertyToID("_OtherLightCount");
    static int otherLightColorID = Shader.PropertyToID("_OtherLightColors");
    static int otherLightPositionID = Shader.PropertyToID("_OtherLightPositions");
    static int otherLightDirectionsID = Shader.PropertyToID("_OtherLightDirections");
    static int otherLightSpotAnglesID = Shader.PropertyToID("_OtherLightSpotAngles");
    static int otherLightShadowDataID = Shader.PropertyToID("_OtherLightShadowData");

    CullingResults cullingResults;
    Shadows shadows = new Shadows();
    const int maxDirectionalLights = 8;
    const int maxOtherLights = 64;
    static Vector4[] dirLightColors = new Vector4[maxDirectionalLights];
    static Vector4[] dirLightDirs = new Vector4[maxDirectionalLights];
    static Vector4[] dirLightShadowDatas = new Vector4[maxDirectionalLights];

    static Vector4[] otherLightColors = new Vector4[maxOtherLights];
    static Vector4[] otherLightPositions = new Vector4[maxOtherLights];
    static Vector4[] otherLightDirections = new Vector4[maxOtherLights];
    static Vector4[] otherLightSpotAngles = new Vector4[maxOtherLights];
    static Vector4[] otherLightShadowDatas = new Vector4[maxOtherLights];

    public void SetUp(ScriptableRenderContext context, CullingResults cullingResults, ShadowSettings shadowSettings)
    {
        this.cullingResults = cullingResults;
        buffer.BeginSample(bufferName);
        shadows.SetUp(cullingResults, shadowSettings, context);
        SetUpLights();
        shadows.Render();
        buffer.EndSample(bufferName);
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }
    void SetUpLights()
    {
        int dirCount = 0;
        int otherCount = 0;
        NativeArray<VisibleLight> visibleLights = cullingResults.visibleLights;
        for (int i = 0; i < visibleLights.Length; i++)
        {
            VisibleLight visibleLight = visibleLights[i];
            switch (visibleLight.lightType)
            {
                case LightType.Directional:
                    if (dirCount < maxDirectionalLights)
                        SetUpDirectionalLights(dirCount++, i, ref visibleLight);
                    break;
                case LightType.Point:
                    if (otherCount < maxOtherLights)
                        SetUpPointLights(otherCount++, i, ref visibleLight);
                    break;
                case LightType.Spot:
                    if (otherCount < maxOtherLights)
                        SetUpSpotLights(otherCount++, i, ref visibleLight);
                    break;
            }

        }
        buffer.SetGlobalInt(directionalLightCountID, dirCount);
        if (dirCount > 0)
        {
            buffer.SetGlobalVectorArray(directionalLightColorID, dirLightColors);
            buffer.SetGlobalVectorArray(directionalLightDirID, dirLightDirs);
            buffer.SetGlobalVectorArray(dirLightShadowDataID, dirLightShadowDatas);
        }
        buffer.SetGlobalInt(otherLightCountID, otherCount);
        if (otherCount > 0)
        {
            buffer.SetGlobalVectorArray(otherLightColorID, otherLightColors);
            buffer.SetGlobalVectorArray(otherLightPositionID, otherLightPositions);
            buffer.SetGlobalVectorArray(otherLightDirectionsID, otherLightDirections);
            buffer.SetGlobalVectorArray(otherLightSpotAnglesID, otherLightSpotAngles);
            buffer.SetGlobalVectorArray(otherLightShadowDataID, otherLightShadowDatas);
        }

    }
    void SetUpDirectionalLights(int index, int visibleIndex, ref VisibleLight light)
    {
        dirLightColors[index] = light.finalColor;
        dirLightDirs[index] = -light.localToWorldMatrix.GetColumn(2);
        dirLightShadowDatas[index] = shadows.ReserveDirectionalShadows(light.light, visibleIndex);
    }
    void SetUpPointLights(int index, int visibleIndex, ref VisibleLight light)
    {
        otherLightColors[index] = light.finalColor;
        Vector4 position = light.localToWorldMatrix.GetColumn(3);
        position.w = 1f / Mathf.Max(0.00001f, light.range * light.range);
        otherLightPositions[index] = position;
        otherLightSpotAngles[index] = new Vector4(0, 1);
        otherLightShadowDatas[index] = shadows.ReserveOtherShadows(light.light, visibleIndex);
    }
    void SetUpSpotLights(int index, int visibleIndex, ref VisibleLight vislight)
    {
        otherLightColors[index] = vislight.finalColor;
        Vector4 position = vislight.localToWorldMatrix.GetColumn(3);
        position.w = 1f / Mathf.Max(0.00001f, vislight.range * vislight.range);
        otherLightPositions[index] = position;
        otherLightDirections[index] = -vislight.localToWorldMatrix.GetColumn(2);

        Light light = vislight.light;
        float innerCos = Mathf.Cos(Mathf.Deg2Rad * 0.5f * light.innerSpotAngle);
        float outerCos = Mathf.Cos(Mathf.Deg2Rad * 0.5f * vislight.spotAngle);
        float angleRangeInv = 1f / Mathf.Max(innerCos - outerCos, 0.001f);
        otherLightSpotAngles[index] = new Vector4(angleRangeInv, -outerCos * angleRangeInv);
        otherLightShadowDatas[index] = shadows.ReserveOtherShadows(light, visibleIndex);
    }
    public void CleanUp()
    {
        shadows.CleanUp();
    }
}
