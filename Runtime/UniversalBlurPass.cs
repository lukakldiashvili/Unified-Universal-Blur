using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Unified.UniversalBlur.Runtime
{
    internal class UniversalBlurPass : ScriptableRenderPass
    {
        private const string k_PassName = "Unified Universal Blur";
        
        private static readonly int m_KawaseOffsetID = Shader.PropertyToID("_KawaseOffset");
        private static readonly int m_globalFullScreenBlurTexture = Shader.PropertyToID("_GlobalFullScreenBlurTexture");
        
        private ProfilingSampler m_ProfilingSampler = new(k_PassName);
        
        private BlurPassData _blurPassData;
        
        public void Setup(BlurPassData blurPassData, in RenderingData renderingData)
        {
            _blurPassData = blurPassData;
            
            _blurPassData.Descriptor = renderingData.cameraData.cameraTargetDescriptor;
            _blurPassData.Descriptor.depthBufferBits = (int)DepthBits.None;
            
            _blurPassData.Descriptor.width =
                Mathf.RoundToInt(_blurPassData.Descriptor.width / _blurPassData.Downsample);
            _blurPassData.Descriptor.height =
                Mathf.RoundToInt(_blurPassData.Descriptor.height / _blurPassData.Downsample);
            
            
            if ((_blurPassData.RT1 == null || _blurPassData.RT2 == null)
                || Helpers.HasDescriptorChanged(_blurPassData.RT1.descriptor, _blurPassData.Descriptor, false))
            {
                _blurPassData.RT1 = new RenderTexture(_blurPassData.Descriptor);
                _blurPassData.RT2 = new RenderTexture(_blurPassData.Descriptor);
            }
        }
        
        public void Dispose()
        {
            if (_blurPassData.RT1 != null)
                _blurPassData.RT1.Release();
            
            if (_blurPassData.RT2 != null)
                _blurPassData.RT2.Release();
        }
        
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            ExecutePass(_blurPassData, ref renderingData, ref context);
        }
        
        private void ExecutePass(BlurPassData blurPassData, ref RenderingData renderingData,
            ref ScriptableRenderContext context)
        {
            var passMaterial = blurPassData.EffectMaterial;
            var rt1 = blurPassData.RT1;
            var rt2 = blurPassData.RT2;
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
                
                SetBlurOffset(1.5f);
                Blit1To2();
                
                if (blurPassData.Intensity > 0f)
                {
                    for (int i = 1; i <= blurPassData.Iterations; i++)
                    {
                        var offset = (0.5f + i * scale) * blurPassData.Intensity;
                        
                        SetBlurOffset(offset);
                        Blit1To2();
                        SwapRTs();
                    }
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