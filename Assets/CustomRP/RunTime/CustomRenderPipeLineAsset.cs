using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(menuName = "Rendering/CustomRenderPipeline")]
public class CustomRenderPipeLineAsset : RenderPipelineAsset
{
    [SerializeField]
    bool dynamicBatching = false;
    [SerializeField]
    bool instancing = false;
    [SerializeField]
    bool SRPBatching = false;
    [SerializeField]
    ShadowSettings shadowSettings = default;
    protected override RenderPipeline CreatePipeline()
    {
        return new CustomRenderPipeline(dynamicBatching, instancing, SRPBatching,shadowSettings);
    }
    
}
