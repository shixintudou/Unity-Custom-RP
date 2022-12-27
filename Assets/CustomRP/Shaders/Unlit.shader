Shader "CustomSRP/Unlit"
{
    Properties
    {
        _BaseMap("Texture",2D)="white"{}
        _BaseColor("Color",Color)=(1,1,1,1)
        _Cutoff("Alpha Cutoff",Range(0.0,1.0))=0.5
        [Toggle(_CLIPPING)]
        _clipping("Alpha Cliping",Float)=0
        [Enum(UnityEngine.Rendering.BlendMode)]
        _SrcBlend("SrcBlend",Float)=1
        [Enum(UnityEngine.Rendering.BlendMode)]
        _DestBlend("DestBlend",Float)=1
        [Enum(off,0,on,1)]
        _ZWrite("ZWrite",Float)=1
    }
    SubShader
    {
        HLSLINCLUDE
        #include "../ShaderLibrary/Common.hlsl"
        #include "UnlitInput.hlsl"
        ENDHLSL
        pass
        {
            Blend [_SrcBlend] [_DestBlend]
            ZWrite [_ZWrite]
            HLSLPROGRAM

            #pragma target 3.5
            #pragma shader_feature _CLIPPING
            #pragma multi_compile_instancing
            #pragma vertex UnlitVertexShader
            #pragma fragment UnlitFragmentShader

            #include "UnlitPass.hlsl"


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
