Shader "Unify/Internal/Blur"
{
    HLSLINCLUDE
        #pragma editor_sync_compilation
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
        #include "Common.hlsl"
    ENDHLSL
    
    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
        }
        Cull Off
        ZWrite Off
        ZTest Always
        
        // 0 - Kawase Blur
        Pass
        {
            Name "Kawase"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment KawaseBlur
            ENDHLSL
        }
    }
}