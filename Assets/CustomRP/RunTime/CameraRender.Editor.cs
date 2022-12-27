using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEngine.Profiling;

public partial class CameraRender
{
    partial void DrawUnSupportedShaders();
    partial void DrawGizmos();
    partial void PrepareForSceneWindow();
    partial void PrePareBuffer();
#if UNITY_EDITOR
    string sampleName { get; set; }
    partial void PrePareBuffer()
    {
        Profiler.BeginSample("Editor Only");
        sampleName = camera.name;
        commandBuffer.name = sampleName;
        Profiler.EndSample();
    }
#else

    string sampleName=>bufferName;

#endif
#if UNITY_EDITOR
    static ShaderTagId[] legacyShaderTagIds =
    {
        new ShaderTagId("Always"),
        new ShaderTagId("ForwardBase"),
        new ShaderTagId("PrepassBase"),
        new ShaderTagId("Vertex"),
        new ShaderTagId("VertexLMRGBM"),
        new ShaderTagId("VertexLM")
    };
    static Material errorMaterial;
    partial void DrawUnSupportedShaders()
    {
        if (errorMaterial == null)
        {
            errorMaterial = new Material(Shader.Find("Hidden/InternalErrorShader"));
        }
        DrawingSettings drawing = new DrawingSettings(legacyShaderTagIds[0], new SortingSettings(camera))
        { overrideMaterial = errorMaterial };

        for (int i = 1; i < legacyShaderTagIds.Length; i++)
        {
            drawing.SetShaderPassName(i, legacyShaderTagIds[i]);
        }
        FilteringSettings filtering = new FilteringSettings(RenderQueueRange.all);
        context.DrawRenderers(cullingResults, ref drawing, ref filtering);
    }
    partial void DrawGizmos()
    {
        if(Handles.ShouldRenderGizmos())
        {
            context.DrawGizmos(camera, GizmoSubset.PreImageEffects);
            context.DrawGizmos(camera, GizmoSubset.PostImageEffects);
        }
    }
    partial void PrepareForSceneWindow()
    {
        if(camera.cameraType==CameraType.SceneView)
        {
            ScriptableRenderContext.EmitWorldGeometryForSceneView(camera);
        }
    }
    
#endif
}

