using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;

public class CustomShaderGUI : ShaderGUI
{
    MaterialEditor editor;
    Object[] materials;
    MaterialProperty[] properties;
    bool showPresets;
    bool Clipping
    {
        set => SetProperty("_clipping", "_CLIPPING", value);
    }
    bool PremultiplyAlpha
    {
        set => SetProperty("_PremultiplyAlpha", "_SPREMULTIPLY_ALPHA", value);
    }
    BlendMode SrcBlend
    {
        set=>SetProperty("_SrcBlend",(float)value);
    }
    BlendMode DestBlend
    {
        set => SetProperty("_DestBlend", (float)value);
    }
    bool ZWrite
    {
        set => SetProperty("_ZWrite", value ? 1f : 0f);
    }
    ShadowMode Shadows
    { 
        set
        {
            if(SetProperty("Shadows",(float)value))
            {
                SetKeyWord("_SHADOWS_CLIP", value == ShadowMode.Clip);
                SetKeyWord("_SHADOWS_DITHER", value == ShadowMode.Dither);
            }
        }
    }
    bool HasPremultiplyAlpha => HasProperty("_PremultiplyAlpha");
    RenderQueue renderQueue
    {
        set
        {
            foreach(Material m in materials)
            {
                m.renderQueue=(int)value;
            }
        }
    }
    enum ShadowMode 
    { 
        On,Clip,Dither,Off
    }

    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        base.OnGUI(materialEditor, properties);
        EditorGUI.BeginChangeCheck();
        editor = materialEditor;
        this.properties = properties;
        BakeEmission();
        materials = materialEditor.targets;     
        EditorGUILayout.Space();
        showPresets = EditorGUILayout.Foldout(showPresets, "Presets", true);
        if(showPresets)
        {
            OpaquePressed();
            ClipPressed();
            FadePressed();
            TransparentPressed();
        }
        if(EditorGUI.EndChangeCheck())
        {
            SetShadowCasterPass();
            CopyLighmappingProperties();
        }
    }
    bool SetProperty(string name, float value)
    {
        MaterialProperty property = FindProperty(name, properties, false);
        if(property!= null)
        {
            property.floatValue= value;
            return true;
        }
        return false;
    }
    void SetKeyWord(string keyword, bool enabled)
    {
        if (enabled)
        {
            foreach (Material m in materials)
            {
                m.EnableKeyword(keyword);
            }
        }
        else
        {
            foreach (Material m in materials)
            {
                m.DisableKeyword(keyword);
            }
        }
    }
    void SetProperty(string name, string keyword,bool value)
    {
        if (SetProperty(name, value ? 1f : 0f))
            SetKeyWord(keyword, value);
    }
    bool HasProperty(string name)
    {
        return FindProperty(name, properties, false) != null;
    }
    bool PressButton(string name)
    {
        if(GUILayout.Button(name))
        {
            editor.RegisterPropertyChangeUndo(name);
            return true;
        }
        return false;
    }
    void OpaquePressed()
    {
        if(PressButton("Opaque"))
        {
            Clipping = false;
            PremultiplyAlpha = false;
            SrcBlend = BlendMode.One;
            DestBlend = BlendMode.Zero;
            ZWrite = true;
            renderQueue = RenderQueue.Geometry;
        }
    }
    void ClipPressed()
    {
        if (PressButton("Clip"))
        {
            Clipping = true;
            PremultiplyAlpha = false;
            SrcBlend = BlendMode.One;
            DestBlend = BlendMode.Zero;
            ZWrite = true;
            renderQueue = RenderQueue.AlphaTest;
        }
    }
    void FadePressed()
    {
        if (PressButton("Fade"))
        {
            Clipping = false;
            PremultiplyAlpha = false;
            SrcBlend = BlendMode.SrcAlpha;
            DestBlend = BlendMode.OneMinusSrcAlpha;
            ZWrite = false;
            renderQueue = RenderQueue.Transparent;
        }
    }
    void TransparentPressed()
    {
        if (HasPremultiplyAlpha && PressButton("Transparent"))
        {
            Clipping = false;
            PremultiplyAlpha = true;
            SrcBlend = BlendMode.One;
            DestBlend = BlendMode.OneMinusSrcAlpha;
            ZWrite = false;
            renderQueue = RenderQueue.Transparent;
        }
    }
    void SetShadowCasterPass()
    {
        MaterialProperty shadows = FindProperty("_Shadows", properties, false);
        if (shadows == null || shadows.hasMixedValue)
            return;
        bool enabled = shadows.floatValue < (float)ShadowMode.Off;
        foreach(Material m in materials)
        {
            m.SetShaderPassEnabled("ShadowCaster", enabled);
        }
    }
    void BakeEmission()
    {
        EditorGUI.BeginChangeCheck();
        editor.LightmapEmissionProperty();
        if(EditorGUI.EndChangeCheck())
        {
            foreach(Material m in editor.targets)
            {
                m.globalIlluminationFlags &= ~MaterialGlobalIlluminationFlags.EmissiveIsBlack;
            }
        }
    }
    void CopyLighmappingProperties()
    {
        MaterialProperty mainTex = FindProperty("_MainTex", properties, false);
        MaterialProperty baseMap = FindProperty("_BaseMap", properties, false);
        if (mainTex != null && baseMap != null)
        {
            mainTex.textureValue = baseMap.textureValue;
            mainTex.textureScaleAndOffset = baseMap.textureScaleAndOffset;
        }
        MaterialProperty color = FindProperty("_Color", properties, false);
        MaterialProperty baseColor =
            FindProperty("_BaseColor", properties, false);
        if (color != null && baseColor != null)
        {
            color.colorValue = baseColor.colorValue;
        }
    }
}
