Shader "Unify/Internal/Blur"
{
    SubShader
    {
        Tags
        {
            "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline"
        }
        ZWrite Off Cull Off
        Pass
        {
            Name "Fast - Kawase"

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            #pragma vertex Vert
            #pragma fragment Frag
            
            #define SAMPLE(textureName, coord2) SAMPLE_TEXTURE2D_LOD(textureName, sampler_LinearClamp, coord2, _BlitMipLevel)

            float _BlurOffset;

            half4 KawaseBlur(Texture2D blurTexture, float2 uv, float offset, float2 texelSize)
            {
                float i = offset;

                half4 col;

                col = SAMPLE(blurTexture, saturate(uv));
                col += SAMPLE(blurTexture, saturate(uv + float2(i, i) * texelSize));
                col += SAMPLE(blurTexture, saturate(uv + float2(i, -i) * texelSize));
                col += SAMPLE(blurTexture, saturate(uv + float2(-i, i) * texelSize));
                col += SAMPLE(blurTexture, saturate(uv + float2(-i, -i) * texelSize));
                col /= 5.0f;

                col.a = 1;

                return col;
            }

            float4 Frag(Varyings input) : SV_Target0
            {
                // this is needed so we account XR platform differences in how they handle texture arrays
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.texcoord.xy;
                
                #ifndef UNITY_UV_STARTS_AT_TOP
                    uv.y = 1.0 - uv.y;
                #endif

                half4 color = KawaseBlur(_BlitTexture, uv, _BlurOffset, _BlitTexture_TexelSize.xy);
                return color;
            }
            ENDHLSL
        }
    }
}