using UnityEngine;
using UnityEngine.Rendering;

public class Shadows
{
    const string bufferName = "Shadows";
    CommandBuffer buffer = new CommandBuffer() { name = bufferName };
    CullingResults cullingResults;
    ShadowSettings shadowSettings;
    ScriptableRenderContext context;
    const int maxShadowedDirectiionalLightCount = 4, maxCascades = 4;
    int shadowedDirectionaLightCount = 0;

    static int dirShadowAtlasID = Shader.PropertyToID("_DirectionalShadowAtlas");
    static int shadowAtlasSizeID = Shader.PropertyToID("_ShadowAtlasSize");
    static int dirShadowMatricesID = Shader.PropertyToID("_DirectionalShadowMatrices");
    static int cascadeCountID = Shader.PropertyToID("_CascadeCount");
    static int cascadeCullingSpheresID = Shader.PropertyToID("_CascadeCullingSpheres");
    static int shadowDistanceFadeID = Shader.PropertyToID("_ShadowDistanceFade");
    static int cascadeDataID = Shader.PropertyToID("_CascadeData");

    static Matrix4x4[] dirShadowMatrices = new Matrix4x4[maxShadowedDirectiionalLightCount * maxCascades];
    static Vector4[] cascadeCullingSpheres = new Vector4[maxCascades];
    static Vector4[] cascadeDatas = new Vector4[maxCascades];

    static string[] directionalFilterKeywords =
    {
        "_DIRECTIONAL_PCF3",
        "_DIRECTIONAL_PCF5",
        "_DIRECTIONAL_PCF7",
    };

    static string[] cascadeBlendKeywords =
    {
        "_CASCADE_BLEND_SOFT",
        "_CASCADE_BLEND_DITHER"
    };

    struct ShadowedDirectionalLight
    {
        public int visibleLightIndex;
        public float slopeScaleBias;
        public float nearPlaneOffset;
    }
    ShadowedDirectionalLight[] shadowedDirectionalLights = new ShadowedDirectionalLight[maxShadowedDirectiionalLightCount];

    public void SetUp(CullingResults results,ShadowSettings settings,ScriptableRenderContext context)
    {
        cullingResults = results;
        shadowSettings = settings;
        this.context = context;
        shadowedDirectionaLightCount = 0;
    }
    void ExecuteBuffer()
    {
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }
    public Vector3 ReserveDirectionalShadows(Light light,int index)
    {
        if (shadowedDirectionaLightCount < maxShadowedDirectiionalLightCount&&
            light.shadows!=LightShadows.None&&light.shadowStrength>0f&&
            cullingResults.GetShadowCasterBounds(index,out Bounds bounds))
        {
            shadowedDirectionalLights[shadowedDirectionaLightCount] = new ShadowedDirectionalLight()
            {
                visibleLightIndex = index,
                slopeScaleBias = light.shadowBias,
                nearPlaneOffset = light.shadowNearPlane
            };

            return new Vector3(light.shadowStrength,
                shadowSettings.directional.cascadeCount * shadowedDirectionaLightCount++,
                light.shadowNormalBias);
        }
        return Vector3.zero;
    }
    public void Render()
    {
        if(shadowedDirectionaLightCount>0)
        {
            RenderDirectionalShadows();           
        }
        else
        {
            buffer.GetTemporaryRT(dirShadowAtlasID, 1, 1, 32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
        }
    }
    void RenderDirectionalShadows()
    {
        float f = 1f - shadowSettings.directional.cascadeFade;
        buffer.SetGlobalVector(shadowDistanceFadeID, new Vector4(1f / shadowSettings.maxDistance, 1f / shadowSettings.distanceFade, 1f / (1f - f * f)));
        int atlasSize = (int)shadowSettings.directional.atlasSize;
        buffer.GetTemporaryRT(dirShadowAtlasID, atlasSize, atlasSize, 32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
        buffer.SetRenderTarget(dirShadowAtlasID, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
        buffer.ClearRenderTarget(true, false, Color.clear);
        buffer.BeginSample(bufferName);
        ExecuteBuffer();

        int tiles = shadowedDirectionaLightCount * shadowSettings.directional.cascadeCount;
        int split = tiles <= 1 ? 1 : tiles <= 4 ? 2 : 4;
        int tileSize = atlasSize / split;
        for(int i=0;i<shadowedDirectionaLightCount;i++)
        {
            RenderDirectionalShadows(i, split, tileSize);
        }
        buffer.SetGlobalMatrixArray(dirShadowMatricesID, dirShadowMatrices);
        buffer.SetGlobalInt(cascadeCountID, shadowSettings.directional.cascadeCount);
        buffer.SetGlobalVectorArray(cascadeCullingSpheresID, cascadeCullingSpheres);
        buffer.SetGlobalVectorArray(cascadeDataID, cascadeDatas);
        SetKeywords(directionalFilterKeywords, (int)shadowSettings.directional.filter - 1);
        SetKeywords(cascadeBlendKeywords, (int)shadowSettings.directional.cascadeBlend - 1);
        buffer.SetGlobalVector(shadowAtlasSizeID, new Vector4(atlasSize, 1f / atlasSize));       
        buffer.EndSample(bufferName);
        ExecuteBuffer();
    }
    void RenderDirectionalShadows(int index,int split,int tileSize)
    {
        ShadowedDirectionalLight light = shadowedDirectionalLights[index];
        var shadowSettings = new ShadowDrawingSettings(cullingResults, light.visibleLightIndex);

        int cascadeCount = this.shadowSettings.directional.cascadeCount;
        int tileoffset = index * cascadeCount;
        Vector3 rastios = this.shadowSettings.directional.cascadeRatios;
        float cullingFactor = Mathf.Max(0f, 0.8f - this.shadowSettings.directional.cascadeFade);
        for (int i = 0; i < cascadeCount; i++)
        {
            cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(light.visibleLightIndex, i, cascadeCount,
            rastios, tileSize, light.nearPlaneOffset, out Matrix4x4 viewMatrix, out Matrix4x4 projMatrix, out ShadowSplitData shadowSplitData);
            shadowSplitData.shadowCascadeBlendCullingFactor = cullingFactor;
            shadowSettings.splitData = shadowSplitData;

            if(index==0)
            {
                SetCascadeData(i, shadowSplitData.cullingSphere, tileSize);
            }

            int tileIndex = tileoffset + i;
            Vector2 offset = SetTileViewPort(tileIndex, split, tileSize);
            buffer.SetViewProjectionMatrices(viewMatrix, projMatrix);
            dirShadowMatrices[tileIndex] = ConvertToAtlasMatrix(projMatrix * viewMatrix, split, offset);
            buffer.SetGlobalDepthBias(0f, light.slopeScaleBias);
            ExecuteBuffer();
            context.DrawShadows(ref shadowSettings);
            buffer.SetGlobalDepthBias(0, 0);
        }

        
    }
    Vector2 SetTileViewPort(int index,int split,float tileSize)
    {
        Vector2 offset = new Vector2(index % split, index / split);
        buffer.SetViewport(new Rect(offset.x * tileSize, offset.y * tileSize, tileSize, tileSize));
        return offset;
    }
    Matrix4x4 ConvertToAtlasMatrix(Matrix4x4 m,int split,Vector2 offset)
    {
        if(SystemInfo.usesReversedZBuffer)
        {
            m.m20 = -m.m20;
            m.m21 = -m.m21;
            m.m22 = -m.m22;
            m.m23 = -m.m23;
        }
        float scale = 1f / split;
        m.m00 = (0.5f * (m.m00 + m.m30) + offset.x * m.m30) * scale;
        m.m01 = (0.5f * (m.m01 + m.m31) + offset.x * m.m31) * scale;
        m.m02 = (0.5f * (m.m02 + m.m32) + offset.x * m.m32) * scale;
        m.m03 = (0.5f * (m.m03 + m.m33) + offset.x * m.m33) * scale;
        m.m10 = (0.5f * (m.m10 + m.m30) + offset.y * m.m30) * scale;
        m.m11 = (0.5f * (m.m11 + m.m31) + offset.y * m.m31) * scale;
        m.m12 = (0.5f * (m.m12 + m.m32) + offset.y * m.m32) * scale;
        m.m13 = (0.5f * (m.m13 + m.m33) + offset.y * m.m33) * scale;
        m.m20 = 0.5f * (m.m20 + m.m30);
        m.m21 = 0.5f * (m.m21 + m.m31);
        m.m22 = 0.5f * (m.m22 + m.m32);
        m.m23 = 0.5f * (m.m23 + m.m33);
        return m;
    }
    void SetCascadeData(int index,Vector4 cullingSphere,float tileSize)
    {
        float texelSize = 2f * cullingSphere.w / tileSize;
        float filterSize = texelSize * ((float)shadowSettings.directional.filter + 1f);
        cullingSphere.w -= filterSize;
        cullingSphere.w *= cullingSphere.w;
        cascadeCullingSpheres[index] = cullingSphere;

        cascadeDatas[index] = new Vector4(1f / cullingSphere.w, filterSize * 1.4142136f);
    }
    void SetKeywords(string[] keywords,int enableIndex)
    {
        for(int i=0;i<keywords.Length;i++)
        {
            if(i==enableIndex)
            {
                buffer.EnableShaderKeyword(keywords[i]);
            }
            else
            {
                buffer.DisableShaderKeyword(keywords[i]);
            }
        }
    }
    public void CleanUp()
    {
        buffer.ReleaseTemporaryRT(dirShadowAtlasID);
        ExecuteBuffer();
    }
}
