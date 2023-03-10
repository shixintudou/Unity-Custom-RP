Shader "CustomSRP/Lit"
{
    Properties
    {
        _BaseMap("Texture",2D)="white"{}
        _BaseColor("Color",Color)=(0.5,0.5,0.5,1)
        _Cutoff("Alpha Cutoff",Range(0.0,1.0))=0.5
        [Toggle(_MASK_MAP)] 
        _MaskMapToggle ("Mask Map", Float) = 0
        [NoScaleOffset]
        _MaskMap("MaskMap",2D)="white"{}
        _Metallic("MetalLic",Range(0.0,1.0))=1.0
        _Smoothness("Smoothness",Range(0.0,1.0))=1.0
        _Fresnel("Fresnel",Range(0.0,1.0))=1.0
        _Occlusion("Occlusion",Range(0.0,1.0))=1.0
        [Toggle(_CLIPPING)]
        _clipping("Alpha Cliping",Float)=0
        [Toggle(_SPREMULTIPLY_ALPHA)]
        _PremultiplyAlpha("PremultiplyAlpha",Float)=0
        [Enum(UnityEngine.Rendering.BlendMode)]
        _SrcBlend("SrcBlend",Float)=1
        [Enum(UnityEngine.Rendering.BlendMode)]
        _DestBlend("DestBlend",Float)=1
        [Enum(off,0,on,1)]
        _ZWrite("ZWrite",Float)=1
        [KeywordEnum(On,Clip,Dither,Off)]
        _Shadows("Shadows",Float)=0
        [Toggle(_RECEIVE_SHADOWS)] 
        _ReceiveShadows ("Receive Shadows", Float) = 1
        [NoScaleOffset]
        _EmissionMap("Emission",2D)="white"{}
        [HDR]
        _EmissionColor("Emission",Color)=(0,0,0,0)
        [Toggle(_DETAIL_MAP)]
        _DetailMapToggle("Detail Map",Float)=0
        _DetailMap("DetailMap",2D)="linearGrey"{}
        _DetailAlbedo("Detail Albedo", Range(0, 1)) = 1
        _DetailSmoothness("Detail Smoothness", Range(0, 1)) = 1
        [Toggle(_NORMAL_MAP)] 
        _NormalMapToggle ("Normal Map", Float) = 0
        [NoScaleOffset] 
        _NormalMap("Normals", 2D) = "bump" {}
		_NormalScale("Normal Scale", Range(0, 1)) = 1
        [NoScaleOffset]
        _DetailNormalMap("Detail Normal Map",2D)="bump"{}
        _DetailNormalScale("Detail Normal Scale",Range(0,1))=1
        [HideInInspector] 
        _MainTex("Texture for Lightmap", 2D) = "white" {}
		[HideInInspector] 
        _Color("Color for Lightmap", Color) = (0.5, 0.5, 0.5, 1.0)
    }
    SubShader
    {
        HLSLINCLUDE
        #include "../ShaderLibrary/Common.hlsl"
        #include "LitInput.hlsl"
        ENDHLSL
        pass
        {
            Tags
            {
                "LightMode"="CustomLit"
            }
            Blend [_SrcBlend] [_DestBlend]
            ZWrite [_ZWrite]
            HLSLPROGRAM
            #pragma target 3.5
            #pragma shader_feature _PREMULTIPLY_ALPHA
            #pragma vertex LitPassVertex
            #pragma fragment LitPassFragment
            #pragma shader_feature _CLIPPING
            #pragma multi_compile_instancing
            #pragma multi_compile _ _DIRECTIONAL_PCF3 _DIRECTIONAL_PCF5 _DIRECTIONAL_PCF7
            #pragma multi_compile _ _OTHER_PCF3 _OTHER_PCF5 _OTHER_PCF7
            #pragma multi_compile _ _CASCADE_BLEND_SOFT _CASCADE_BLEND_DITHER
            #pragma shader_feature _RECEIVE_SHADOWS
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ _SHADOW_MASK_DISTANCE _SHADOW_MASK_ALWAYS
            #pragma multi_compile _ LOD_FADE_CROSSFADE
            #pragma shader_feature _NORMAL_MAP
            #pragma shader_feature _MASK_MAP
            #pragma shader_feature _DETAIL_MAP

            #include "LitPass.hlsl"

            ENDHLSL
        }

        pass
        {
            Tags
            {
                "LightMode"="ShadowCaster"
            }
            ColorMask 0

            HLSLPROGRAM
            #pragma target 3.5  
            #pragma vertex ShadowCasterPassVertex
            #pragma fragment ShadowCasterPassFragment
            #pragma shader_feature _ _SHADOWS_CLIP _SHADOWS_DITHER
            #pragma multi_compile_instancing
            #pragma multi_compile _ LOD_FADE_CROSSFADE

            #include "ShadowCasterPass.hlsl"

            ENDHLSL
        }
        
        pass
        {
            Tags
            {
                "LightMode"="Meta"
            }
            Cull Off

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex MetaPassVertex
            #pragma fragment MetaPassFragment

            #include "MetaPass.hlsl"

            ENDHLSL
        }
    }
    CustomEditor "CustomShaderGUI"
    
}
