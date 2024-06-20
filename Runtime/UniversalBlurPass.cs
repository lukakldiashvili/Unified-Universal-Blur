using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Unified.UniversalBlur.Runtime
{
    internal class UniversalBlurPass : ScriptableRenderPass, IDisposable
    {
        private const string k_PassName = "Unified Universal Blur";
        
        private static readonly int m_KawaseOffsetID = Shader.PropertyToID("_KawaseOffset");
        private static readonly int m_globalFullScreenBlurTexture = Shader.PropertyToID("_GlobalFullScreenBlurTexture");
        
        private ProfilingSampler m_ProfilingSampler = new(k_PassName);
        
        private BlurPassData _blurPassData;
        private int renderTextureId1;
        private int renderTextureId2;
        
        
        public void Setup(BlurPassData blurPassData, in RenderingData renderingData)
        {
            _blurPassData = blurPassData;
            
            renderTextureId1 = Shader.PropertyToID("Unified Blur RT1");
            renderTextureId2 = Shader.PropertyToID("Unified Blur RT2");
        }
        
        public void Dispose()
        {
        }
        
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public void DrawDefaultTexture()
        {
            // For better preview experience in editor, we just use a gray texture
            Shader.SetGlobalTexture(m_globalFullScreenBlurTexture, Texture2D.linearGrayTexture);
        }
        
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            ExecutePass(_blurPassData, ref renderingData, ref context);
        }
        
        private void ExecutePass(BlurPassData blurPassData, ref RenderingData renderingData,
            ref ScriptableRenderContext context)
        {
            var passMaterial = blurPassData.EffectMaterial;
            var scale = blurPassData.Scale;
            
            // should not happen as we check it in feature
            if (passMaterial == null)
                return;
            
            if (renderingData.cameraData.isPreviewCamera)
                return;
            
            if (renderingData.cameraData.isSceneViewCamera)
            {
#if UNITY_EDITOR
                // For better preview experience in editor, we just use a gray texture
                Shader.SetGlobalTexture(m_globalFullScreenBlurTexture, Texture2D.linearGrayTexture);
#endif
                return;
            }
            
            CommandBuffer cmd = CommandBufferPool.Get(k_PassName);
            
            cmd.GetTemporaryRT(renderTextureId1, blurPassData.Descriptor, filter: FilterMode.Bilinear);
            cmd.GetTemporaryRT(renderTextureId2, blurPassData.Descriptor, filter: FilterMode.Bilinear);
            
            var cameraData = renderingData.cameraData;
            
            using (new ProfilingScope(cmd, m_ProfilingSampler))
            {
                ProcessEffect(ref context);
            }
            
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
            
            // End of function
            
            void ProcessEffect(ref ScriptableRenderContext context)
            {
                var source =
#if UNITY_2022_1_OR_NEWER
                    cameraData.renderer.cameraColorTargetHandle;
#else
                        cameraData.renderer.cameraColorTarget;
#endif
                
                // Setup
                cmd.Blit(source, renderTextureId1);
                
                if (blurPassData.Intensity > 0f)
                {
                    SetBlurOffset(1.5f);
                    Blit1To2();
                    
                    for (int i = 1; i <= blurPassData.Iterations; i++)
                    {
                        var offset = (0.5f + i * scale) * (blurPassData.Intensity / blurPassData.Downsample);
                        
                        SetBlurOffset(offset);
                        Blit1To2();
                        SwapRTs();
                    }
                }
                else
                {
                    Blit1To2();
                }
                
                cmd.SetGlobalTexture(m_globalFullScreenBlurTexture, renderTextureId2);
                
                cmd.ReleaseTemporaryRT(renderTextureId1);
                cmd.ReleaseTemporaryRT(renderTextureId2);
            }
            
            void Blit1To2()
            {
                cmd.Blit(renderTextureId1, renderTextureId2, passMaterial, blurPassData.PassIndex);
            }
            
            void SetBlurOffset(float offset)
            {
                cmd.SetGlobalFloat(m_KawaseOffsetID, offset);
            }
            
            void SwapRTs()
            {
                (renderTextureId1, renderTextureId2) = (renderTextureId2, renderTextureId1);
            }
        }
    }
}