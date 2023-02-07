using UnityEngine;
using UnityEngine.Rendering;

public class Shadows
{
    const string bufferName = "Shadows";
    CommandBuffer buffer = new CommandBuffer() { name = bufferName };
    CullingResults cullingResults;
    ShadowSettings shadowSettings;
    ScriptableRenderContext context;
    const int maxShadowedDirectiionalLightCount = 4;
    const int maxShadowedOtherLightCount = 16;
    const int maxCascades = 4;
    int shadowedDirectionaLightCount = 0;
    int shadowedOtherLightCount = 0;
    bool useShadowMask;

    static int shadowPancakingID = Shader.PropertyToID("_ShadowPancaking");

    static int dirShadowAtlasID = Shader.PropertyToID("_DirectionalShadowAtlas");
    static int shadowAtlasSizeID = Shader.PropertyToID("_ShadowAtlasSize");
    static int dirShadowMatricesID = Shader.PropertyToID("_DirectionalShadowMatrices");
    static int cascadeCountID = Shader.PropertyToID("_CascadeCount");
    static int cascadeCullingSpheresID = Shader.PropertyToID("_CascadeCullingSpheres");
    static int shadowDistanceFadeID = Shader.PropertyToID("_ShadowDistanceFade");
    static int cascadeDataID = Shader.PropertyToID("_CascadeData");
    static int otherShadowAtlasID = Shader.PropertyToID("_OtherShadowAtlas");
    static int otherShadowMatricesID = Shader.PropertyToID("_OtherShadowMatrices");
    static int otherShadowTilesID = Shader.PropertyToID("_OtherShadowTiles");

    static Matrix4x4[] dirShadowMatrices = new Matrix4x4[maxShadowedDirectiionalLightCount * maxCascades];
    static Matrix4x4[] otherShadowMatrices = new Matrix4x4[maxShadowedOtherLightCount];
    static Vector4[] otherShadowTiles = new Vector4[maxShadowedOtherLightCount];
    static Vector4[] cascadeCullingSpheres = new Vector4[maxCascades];
    static Vector4[] cascadeDatas = new Vector4[maxCascades];

    Vector4 atlasSizes;

    static string[] directionalFilterKeywords =
    {
        "_DIRECTIONAL_PCF3",
        "_DIRECTIONAL_PCF5",
        "_DIRECTIONAL_PCF7",
    };
    static string[] otherFilterKeywords =
    {
        "_OTHER_PCF3",
        "_OTHER_PCF5",
        "_OTHER_PCF7",
    };


    static string[] cascadeBlendKeywords =
    {
        "_CASCADE_BLEND_SOFT",
        "_CASCADE_BLEND_DITHER"
    };
    static string[] shadowMaskKeywords =
    {
        "_SHADOW_MASK_DISTANCE",
        "_SHADOW_MASK_ALWAYS",
    };

    struct ShadowedDirectionalLight
    {
        public int visibleLightIndex;
        public float slopeScaleBias;
        public float nearPlaneOffset;
    }
    struct ShadowedOtherLight
    {
        public int visibleLightIndex;
        public float slopeScaleBias;
        public float normalBias;
        public bool isPoint;
    }
    ShadowedDirectionalLight[] shadowedDirectionalLights = new ShadowedDirectionalLight[maxShadowedDirectiionalLightCount];
    ShadowedOtherLight[] shadowedOtherLights = new ShadowedOtherLight[maxShadowedOtherLightCount];

    public void SetUp(CullingResults results,ShadowSettings settings,ScriptableRenderContext context)
    {
        cullingResults = results;
        shadowSettings = settings;
        this.context = context;
        shadowedDirectionaLightCount = 0;
        shadowedOtherLightCount = 0;
        useShadowMask = false;
    }
    void ExecuteBuffer()
    {
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }
    public Vector4 ReserveDirectionalShadows(Light light,int index)
    {
        
        if (shadowedDirectionaLightCount < maxShadowedDirectiionalLightCount&&
            light.shadows!=LightShadows.None&&light.shadowStrength>0f)
        {
            float maskChannel = -1;
            LightBakingOutput lightBaking = light.bakingOutput;
            if (lightBaking.lightmapBakeType == LightmapBakeType.Mixed && lightBaking.mixedLightingMode == MixedLightingMode.Shadowmask)
            {
                useShadowMask = true;
                maskChannel = lightBaking.occlusionMaskChannel;
            }
            if (!cullingResults.GetShadowCasterBounds(index, out Bounds b))
            {
                return new Vector4(-light.shadowStrength, 0f, 0f, maskChannel);
            }
            shadowedDirectionalLights[shadowedDirectionaLightCount] = new ShadowedDirectionalLight()
            {
                visibleLightIndex = index,
                slopeScaleBias = light.shadowBias,
                nearPlaneOffset = light.shadowNearPlane              
            };


            return new Vector4(light.shadowStrength,
                shadowSettings.directional.cascadeCount * shadowedDirectionaLightCount++,
                light.shadowNormalBias, maskChannel);
        }
        return new Vector4(0, 0, 0, -1f);
    }
    public Vector4 ReserveOtherShadows(Light light ,int index)
    {
        
        if (light.shadows == LightShadows.None || light.shadowStrength <= 0)
            return new Vector4(0, 0, 0, -1);
        float maskChannel = -1f;

        LightBakingOutput lightBaking = light.bakingOutput;
        if (lightBaking.lightmapBakeType == LightmapBakeType.Mixed && lightBaking.mixedLightingMode == MixedLightingMode.Shadowmask)
        {
            useShadowMask = true;
            maskChannel = lightBaking.occlusionMaskChannel;           
        }
        bool isPoint = light.type == LightType.Point;
        int newLightCount = shadowedOtherLightCount + (isPoint ? 6 : 1);
        if (newLightCount>=maxShadowedOtherLightCount||!cullingResults.GetShadowCasterBounds(index,out Bounds bounds))
        {
            return new Vector4(-light.shadowStrength, 0, 0, maskChannel);
        }
        shadowedOtherLights[shadowedOtherLightCount] = new ShadowedOtherLight
        {
            visibleLightIndex = index,
            slopeScaleBias = light.shadowBias,
            normalBias = light.shadowNormalBias,
            isPoint= isPoint,
        };
        Vector4 data = new Vector4(
            light.shadowStrength, shadowedOtherLightCount,
            isPoint ? 1f : 0f, maskChannel
        );
        shadowedOtherLightCount = newLightCount;
        return data;

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
        if(shadowedOtherLightCount>0)
        {
            RenderOtherShadows();
        }
        else
        {
            buffer.SetGlobalTexture(otherShadowAtlasID, dirShadowAtlasID);
        }
        buffer.BeginSample(bufferName);
        SetShadowMaskKeywords();

        buffer.SetGlobalInt(cascadeCountID, shadowedDirectionaLightCount > 0 ? shadowSettings.directional.cascadeCount : 0);
        float f = 1f - shadowSettings.directional.cascadeFade;
        buffer.SetGlobalVector(shadowDistanceFadeID, new Vector4(1f / shadowSettings.maxDistance, 1f / shadowSettings.distanceFade, 1f / (1f - f * f)));
        buffer.SetGlobalVector(shadowAtlasSizeID, atlasSizes);
        buffer.EndSample(bufferName);
        ExecuteBuffer();
    }
    void RenderDirectionalShadows()
    {      
        int atlasSize = (int)shadowSettings.directional.atlasSize;
        atlasSizes.x = atlasSize;
        atlasSizes.y = 1f / atlasSize;
        buffer.GetTemporaryRT(dirShadowAtlasID, atlasSize, atlasSize, 32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
        buffer.SetRenderTarget(dirShadowAtlasID, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
        buffer.ClearRenderTarget(true, false, Color.clear);
        buffer.SetGlobalFloat(shadowPancakingID, 0f);
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
        buffer.SetGlobalVectorArray(cascadeCullingSpheresID, cascadeCullingSpheres);
        buffer.SetGlobalVectorArray(cascadeDataID, cascadeDatas);
        SetKeywords(directionalFilterKeywords, (int)shadowSettings.directional.filter - 1);
        SetKeywords(cascadeBlendKeywords, (int)shadowSettings.directional.cascadeBlend - 1);
     
        buffer.EndSample(bufferName);
        ExecuteBuffer();
    }
    void RenderOtherShadows()
    {
        int atlasSize = (int)shadowSettings.other.atlasSize;
        atlasSizes.z = atlasSize;
        atlasSizes.w = 1f / atlasSize;
        buffer.GetTemporaryRT(otherShadowAtlasID, atlasSize, atlasSize, 32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
        buffer.SetRenderTarget(otherShadowAtlasID, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
        buffer.ClearRenderTarget(true, false, Color.clear);
        buffer.SetGlobalFloat(shadowPancakingID, 0f);
        buffer.BeginSample(bufferName);
        ExecuteBuffer();

        int tiles = shadowedOtherLightCount;
        int split = tiles <= 1 ? 1 : tiles <= 4 ? 2 : 4;
        int tileSize = atlasSize / split;
        for (int i = 0; i < shadowedOtherLightCount;)
        {
            if (shadowedOtherLights[i].isPoint)
            {
                RenderPointShadows(i, split, tileSize);
                i += 6;
            }
            else
            {
                RenderSpotShadows(i, split, tileSize);
                i++;
            }           
        }
        buffer.SetGlobalVectorArray(otherShadowTilesID, otherShadowTiles);
        buffer.SetGlobalMatrixArray(otherShadowMatricesID, otherShadowMatrices);
        SetKeywords(otherFilterKeywords, (int)shadowSettings.other.filter - 1);
        
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
        float tileScale = 1f / split;
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
            dirShadowMatrices[tileIndex] = ConvertToAtlasMatrix(projMatrix * viewMatrix, tileScale, offset);
            buffer.SetGlobalDepthBias(0f, light.slopeScaleBias);
            ExecuteBuffer();
            context.DrawShadows(ref shadowSettings);
            buffer.SetGlobalDepthBias(0, 0);
        }

        
    }
    void RenderPointShadows(int index,int split,int tileSize)
    {
        ShadowedOtherLight light = shadowedOtherLights[index];
        var shadowsettings = new ShadowDrawingSettings(cullingResults, light.visibleLightIndex);
        float texelSize = 2f / tileSize;
        float filterSize = texelSize * ((float)this.shadowSettings.other.filter + 1f);
        float tileScale = 1f / split;
        float bias = light.normalBias * filterSize * 1.4142136f;
        float fovBias =
            Mathf.Atan(1f + bias + filterSize) * Mathf.Rad2Deg * 2f - 90f;
        for (int i=0;i<6;i++)
        {
            cullingResults.ComputePointShadowMatricesAndCullingPrimitives(light.visibleLightIndex, (CubemapFace)i,
                fovBias, out Matrix4x4 viewMatrix, out Matrix4x4 projMatrix, out ShadowSplitData shadowSplitData);
            shadowsettings.splitData = shadowSplitData;
            int tileIndex = index + i;
            viewMatrix.m11 = -viewMatrix.m11;
            viewMatrix.m12 = -viewMatrix.m12;
            viewMatrix.m13 = -viewMatrix.m13;

            Vector2 offset = SetTileViewPort(tileIndex, split, tileSize);
            
            SetOtherTileData(tileIndex, offset, tileScale, bias);

            otherShadowMatrices[tileIndex] = ConvertToAtlasMatrix(projMatrix * viewMatrix, tileScale, offset);
            buffer.SetViewProjectionMatrices(viewMatrix, projMatrix);
            buffer.SetGlobalDepthBias(0f, light.slopeScaleBias);
            ExecuteBuffer();
            context.DrawShadows(ref shadowsettings);
            buffer.SetGlobalDepthBias(0f, 0f);
        }
               
    }
    void RenderSpotShadows(int index, int split, int tileSize)
    {
        ShadowedOtherLight light = shadowedOtherLights[index];
        var shadowsettings = new ShadowDrawingSettings(cullingResults, light.visibleLightIndex);
        cullingResults.ComputeSpotShadowMatricesAndCullingPrimitives(light.visibleLightIndex, out Matrix4x4 viewMatrix,
            out Matrix4x4 projMatrix, out ShadowSplitData shadowSplitData);
        shadowsettings.splitData = shadowSplitData;

        float texelSize = 2f / (tileSize * projMatrix.m00);
        float filterSize = texelSize * ((float)this.shadowSettings.other.filter + 1f);
        float bias = light.normalBias * filterSize * 1.4142136f;
        Vector2 offset = SetTileViewPort(index, split, tileSize);
        float tileScale = 1f / split;
        SetOtherTileData(index, offset, tileScale, bias);

        otherShadowMatrices[index] = ConvertToAtlasMatrix(projMatrix * viewMatrix, tileScale, offset);
        buffer.SetViewProjectionMatrices(viewMatrix, projMatrix);
        buffer.SetGlobalDepthBias(0f, light.slopeScaleBias);
        ExecuteBuffer();
        context.DrawShadows(ref shadowsettings);
        buffer.SetGlobalDepthBias(0f, 0f);
    }
    Vector2 SetTileViewPort(int index,int split,float tileSize)
    {
        Vector2 offset = new Vector2(index % split, index / split);
        buffer.SetViewport(new Rect(offset.x * tileSize, offset.y * tileSize, tileSize, tileSize));
        return offset;
    }
    Matrix4x4 ConvertToAtlasMatrix(Matrix4x4 m,float scale,Vector2 offset)
    {
        if(SystemInfo.usesReversedZBuffer)
        {
            m.m20 = -m.m20;
            m.m21 = -m.m21;
            m.m22 = -m.m22;
            m.m23 = -m.m23;
        }
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
    void SetOtherTileData(int index,Vector2 offset,float scale, float bias)
    {
        Vector4 data = Vector4.zero;
        float border = atlasSizes.w * 0.5f;
        data.x = offset.x * scale + border;
        data.y = offset.y * scale + border;
        data.z = scale - border - border;
        data.w = bias;
        otherShadowTiles[index] = data;
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
        if (shadowedOtherLightCount > 0)
            buffer.ReleaseTemporaryRT(otherShadowAtlasID);
        ExecuteBuffer();
    }
    void SetShadowMaskKeywords()
    {
        if(!useShadowMask)
        {
            SetKeywords(shadowMaskKeywords, -1);
        }
        else
        {
            if(QualitySettings.shadowmaskMode==ShadowmaskMode.Shadowmask)
            {
                SetKeywords(shadowMaskKeywords, 1);
            }
            else
            {
                SetKeywords(shadowMaskKeywords, 0);
            }    
        }
    }
}
