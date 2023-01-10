
using UnityEngine;
using UnityEngine.Rendering;

public partial class CustomRenderPipeline : RenderPipeline
{
    CameraRender cameraRender = new CameraRender();
    bool dynamicBatching;
    bool instancing;
    ShadowSettings shadowSettings;
    public CustomRenderPipeline(bool dynamicBatching, bool instancing,bool SRPBatching, ShadowSettings shadowSettings)
    {
        GraphicsSettings.useScriptableRenderPipelineBatching = SRPBatching;
        QualitySettings.shadows = ShadowQuality.All;
        this.dynamicBatching = dynamicBatching;
        this.instancing = instancing;
        GraphicsSettings.lightsUseLinearIntensity = true;
        this.shadowSettings = shadowSettings;
        InitializeForEditor();
    }
    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        foreach(Camera camera in cameras) 
        {
            cameraRender.Render(context, camera, dynamicBatching, instancing, shadowSettings);
        }
    }
}
