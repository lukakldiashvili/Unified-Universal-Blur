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
        
        private RenderTextureDescriptor _lastDescriptor;
        private RenderTexture _blurRT1;
        private RenderTexture _blurRT2;
        
        public void Setup(BlurPassData blurPassData, in RenderingData renderingData)
        {
            _blurPassData = blurPassData;
            
            var descriptor = renderingData.cameraData.cameraTargetDescriptor;
            descriptor.depthBufferBits = (int)DepthBits.None;
            
            descriptor.width =
                Mathf.RoundToInt(descriptor.width / _blurPassData.Downsample);
            descriptor.height =
                Mathf.RoundToInt(descriptor.height / _blurPassData.Downsample);
            
            if (Helpers.HasDescriptorChanged(_lastDescriptor, descriptor, false))
            {
                Dispose(false);
                AllocateRenderTextures(descriptor);
            }
        }
        
        private void AllocateRenderTextures(RenderTextureDescriptor descriptor)
        {
            _lastDescriptor = descriptor;
            
            if (_blurRT1 == null)
            {
                _blurRT1 = new RenderTexture(descriptor)
                {
                    name = "Universal Blur RT1"
                };
            }
            
            if (_blurRT2 == null)
            {
                _blurRT2 = new RenderTexture(descriptor)
                {
                    name = "Universal Blur RT2"
                };
            }
        }
        
        ~UniversalBlurPass() => Dispose(false);
        
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        
        private void Dispose(bool disposing)
        {
            if (_blurRT1 != null)
            {
                _blurRT1.Release();
            }
            
            if (_blurRT2 != null)
            {
                _blurRT2.Release();
            }
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
            var rt1 = _blurRT1;
            var rt2 = _blurRT2;
            
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
                cmd.Blit(source, rt1);
                
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
                
                cmd.SetGlobalTexture(m_globalFullScreenBlurTexture, rt2);
            }
            
            void Blit1To2()
            {
                cmd.Blit(rt1, rt2, passMaterial, blurPassData.PassIndex);
            }
            
            void SetBlurOffset(float offset)
            {
                cmd.SetGlobalFloat(m_KawaseOffsetID, offset);
            }
            
            void SwapRTs()
            {
                (rt1, rt2) = (rt2, rt1);
            }
        }
    }
}