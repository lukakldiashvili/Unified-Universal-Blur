using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Unified.Universal.Blur
{
    class UniversalBlurPass : ScriptableRenderPass
    {
        
        private const string k_PassName = "Unified Universal Blur";
        
        private static readonly int m_KawaseOffsetID = Shader.PropertyToID("_KawaseOffset");
        private static readonly int m_globalFullScreenBlurTexture = Shader.PropertyToID("_GlobalFullScreenBlurTexture");
        
        private ProfilingSampler m_ProfilingSampler = new(k_PassName);

        private PassData m_PassData;

        public void Setup(PassData passData, float downsample, in RenderingData renderingData)
        {
            m_PassData = passData;

            m_PassData.rtDesc = renderingData.cameraData.cameraTargetDescriptor;
            m_PassData.rtDesc.depthBufferBits = (int)DepthBits.None;

            m_PassData.rtDesc.width = Mathf.RoundToInt(m_PassData.rtDesc.width / downsample);
            m_PassData.rtDesc.height = Mathf.RoundToInt(m_PassData.rtDesc.height / downsample);

		#if UNITY_2022_1_OR_NEWER
		    RenderingUtils.ReAllocateIfNeeded(ref m_PassData.tmpRT1, m_PassData.rtDesc, name: "_PassRT1", wrapMode: TextureWrapMode.Clamp);
		    RenderingUtils.ReAllocateIfNeeded(ref m_PassData.tmpRT2, m_PassData.rtDesc, name: "_PassRT2", wrapMode: TextureWrapMode.Clamp);
        #else
            m_PassData.tmpRT1 ??= new RenderTexture(m_PassData.rtDesc);
            m_PassData.tmpRT2 ??= new RenderTexture(m_PassData.rtDesc);
        #endif
        }

        public void Dispose()
        {
            if (m_PassData == null)
                return;
            
            if (m_PassData.tmpRT1 != null)
                m_PassData.tmpRT1.Release();
            
            if (m_PassData.tmpRT2 != null)
                m_PassData.tmpRT2.Release();
        }


        // public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        // { }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            ExecutePass(m_PassData, ref renderingData, ref context);
        }

        private void ExecutePass(PassData passData, ref RenderingData renderingData, ref ScriptableRenderContext context)
        {
            var passMaterial = passData.effectMaterial;
            var tmpRT1 = passData.tmpRT1;
            var tmpRT2 = passData.tmpRT2;
            var scale = passData.scale;

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
            
		    using (new ProfilingScope(cmd, m_ProfilingSampler)) {
			    ProcessEffect(ref context);
		    }
            
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
            
            // End of function

            void ProcessEffect(ref ScriptableRenderContext context)
            {
                if (passData.requiresColor)
                {
                    var source =
					    #if UNITY_2022_1_OR_NEWER
                        cameraData.renderer.cameraColorTargetHandle;
					    #else
                        cameraData.renderer.cameraColorTarget;
					    #endif

                    // Setup
                    cmd.Blit(source, tmpRT1);
                    
                    SetBlurOffset(1.5f);
                    Blit1To2();

                    if (passData.intensity > 0f)
                    {
                        for (int i = 1; i <= passData.iterations; i++)
                        {
                            var offset = (0.5f + i * scale) * passData.intensity;
                            
                            SetBlurOffset(offset);
                            Blit1To2();
                            SwapRTs();
                        }
                    }

                    cmd.SetGlobalTexture(m_globalFullScreenBlurTexture, tmpRT2);
                }
            }
            
            void Blit1To2()
            {
                cmd.Blit(tmpRT1, tmpRT2, passMaterial, passData.passIndex);
            }

            void SetBlurOffset(float offset)
            {
                cmd.SetGlobalFloat(m_KawaseOffsetID, offset);
            }
            
            void SwapRTs()
            {
                (tmpRT1, tmpRT2) = (tmpRT2, tmpRT1);
            }
        }

        internal class PassData
        {
            internal Material effectMaterial;
            internal int passIndex;
            internal bool requiresColor;

            public float intensity;
            public float scale;
            public int iterations;

            public RenderTextureDescriptor rtDesc;

            public
            #if UNITY_2022_1_OR_NEWER
            RTHandle
            #else
                RenderTexture
            #endif
                tmpRT1, tmpRT2;
        }
    }
}
