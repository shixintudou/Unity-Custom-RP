using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public partial class CameraRender
{
    ScriptableRenderContext context;
    Camera camera;
    const string bufferName = "RenderCamera";
    CommandBuffer commandBuffer = new CommandBuffer { name = bufferName };
    CullingResults cullingResults;
    Lighting lighting = new Lighting();
    static ShaderTagId unlitShaderTagId = new ShaderTagId("SRPDefaultUnlit");
    static ShaderTagId litShaderTagId = new ShaderTagId("CustomLit");
    
    public void Render(ScriptableRenderContext context,Camera camera, bool dynamicBatching, bool instancing,ShadowSettings shadowSettings)
    {
        this.context = context;
        this.camera = camera;
        PrePareBuffer();
        PrepareForSceneWindow();
        if (!Cull(shadowSettings.maxDistance))
            return;
        commandBuffer.BeginSample(sampleName);
        ExecuteBuffer();
        lighting.SetUp(context, cullingResults, shadowSettings);
        commandBuffer.EndSample(sampleName);
        SetUp();      
        DrawVisiableGeometry(dynamicBatching,instancing);     
        DrawUnSupportedShaders();
        DrawGizmos();
        lighting.CleanUp();
        Submit();
    }
    void SetUp()
    {
        context.SetupCameraProperties(camera);
        CameraClearFlags flags = camera.clearFlags;
        commandBuffer.ClearRenderTarget(flags <= CameraClearFlags.Depth, flags == CameraClearFlags.Color, flags == CameraClearFlags.Color ? camera.backgroundColor.linear : Color.clear);
        commandBuffer.BeginSample(sampleName);
        ExecuteBuffer();        
    }
    void DrawVisiableGeometry(bool dynamicBatching,bool instancing)
    {
        SortingSettings sorting = new SortingSettings(camera)
        {
            criteria = SortingCriteria.CommonOpaque
        };
        DrawingSettings drawing = new DrawingSettings(unlitShaderTagId, sorting)
        {
            enableDynamicBatching = dynamicBatching,
            enableInstancing = instancing,
            perObjectData = PerObjectData.Lightmaps | PerObjectData.LightProbe | PerObjectData.LightProbeProxyVolume
        };
        drawing.SetShaderPassName(1, litShaderTagId);
        FilteringSettings filtering = new FilteringSettings(RenderQueueRange.opaque);
        context.DrawRenderers(cullingResults, ref drawing, ref filtering);
        context.DrawSkybox(camera);
        sorting.criteria = SortingCriteria.CommonTransparent;
        drawing.sortingSettings = sorting;
        filtering.renderQueueRange = RenderQueueRange.transparent;
        context.DrawRenderers(cullingResults, ref drawing, ref filtering);
    }
    void Submit()
    {
        commandBuffer.EndSample(sampleName);
        ExecuteBuffer();
        context.Submit();
    }
    void ExecuteBuffer()
    {
        context.ExecuteCommandBuffer(commandBuffer);
        commandBuffer.Clear();
    }
    bool Cull(float distance)
    {
        if(camera.TryGetCullingParameters(out var parameters))
        {
            parameters.shadowDistance = Mathf.Min(distance, camera.farClipPlane);
            cullingResults = context.Cull(ref parameters);
            return true;
        }
        return false;
    }
    
}
