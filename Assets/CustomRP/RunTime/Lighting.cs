
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
    CullingResults cullingResults;
    Shadows shadows=new Shadows();
    const int maxDirectionalLights = 8;
    static Vector4[] dirLightColors = new Vector4[maxDirectionalLights];
    static Vector4[] dirLightDirs = new Vector4[maxDirectionalLights];
    static Vector4[] dirLightShadowDatas = new Vector4[maxDirectionalLights];
    public void SetUp(ScriptableRenderContext context,CullingResults cullingResults,ShadowSettings shadowSettings)
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
        int count = 0;
        NativeArray<VisibleLight> visibleLights = cullingResults.visibleLights;
        for(int i=0;i<visibleLights.Length;i++)
        {
            VisibleLight visibleLight = visibleLights[i];
            if (visibleLight.lightType==LightType.Directional)
            {
                SetUpDirectionalLights(count++, ref visibleLight);
                if (count >= maxDirectionalLights)
                    break;
            }
        }
        
        
        buffer.SetGlobalInt(directionalLightCountID, visibleLights.Length);
        buffer.SetGlobalVectorArray(directionalLightColorID, dirLightColors);
        buffer.SetGlobalVectorArray(directionalLightDirID, dirLightDirs);
        buffer.SetGlobalVectorArray(dirLightShadowDataID, dirLightShadowDatas);
    }
    void SetUpDirectionalLights(int index ,ref VisibleLight light)
    {
        dirLightColors[index] = light.finalColor;
        dirLightDirs[index] = -light.localToWorldMatrix.GetColumn(2);
        dirLightShadowDatas[index] = shadows.ReserveDirectionalShadows(light.light, index);
    }
    public void CleanUp()
    {
        shadows.CleanUp();
    }
}
