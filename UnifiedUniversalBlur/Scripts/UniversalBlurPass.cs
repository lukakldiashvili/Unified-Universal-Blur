using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Unified.Universal.Blur
{
    class UniversalBlurPass : ScriptableRenderPass
    {
        private const string k_GlobalFullScreenBlurTexture = "_GlobalFullScreenBlurTexture";

        private static readonly int m_KawaseOffsetID = Shader.PropertyToID("_KawaseOffset");

        private PassData m_PassData;

        public void Setup(Action<PassData> passDataOptions, float downsample, in RenderingData renderingData)
        {
            m_PassData ??= new PassData();

            passDataOptions?.Invoke(m_PassData);

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
            #if UNITY_2022_1_OR_NEWER
            if (m_PassData != null)
            {
                m_PassData.tmpRT1.Release();
                m_PassData.tmpRT2.Release();
            }
            #else 
            m_PassData.tmpRT1.Release();
            m_PassData.tmpRT2.Release();
            #endif
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            ExecutePass(m_PassData, ref renderingData, ref context);
        }

        private static void ExecutePass(PassData passData, ref RenderingData renderingData, ref ScriptableRenderContext context)
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

            // if is scene camera and we want to disable in scene view
            if (renderingData.cameraData.isSceneViewCamera)
            {
                // renderingData.cameraData.is
                // return;
            }

            CommandBuffer cmd = CommandBufferPool.Get("FullScreenPassRendererFeature");

            var cameraData = renderingData.cameraData;
            
            if (renderingData.cameraData.isSceneViewCamera)
            {
                cmd.SetGlobalTexture(k_GlobalFullScreenBlurTexture, tmpRT2);
            }
            else
            {
		        #if UNITY_2022_1_OR_NEWER
		        using (new ProfilingScope(cmd, passData.profilingSampler)) {
			        ProcessEffect(ref context);
		        }
		        #else
                ProcessEffect(ref context);
		        #endif
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);

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

                    // --- Start
                    cmd.Blit(source, tmpRT1);

                    void DoBlit()
                    {
                        cmd.Blit(tmpRT1, tmpRT2, passMaterial, 0);
                    }

                    {
                        cmd.SetGlobalFloat(m_KawaseOffsetID, 1.5f);
                        DoBlit();

                        if (passData.intensity > 0f)
                        {
                            for (var i = 1; i <= passData.iterations; i++)
                            {
                                var offset = (0.5f + i * scale) * passData.intensity;
                            
                                cmd.SetGlobalFloat(m_KawaseOffsetID, offset);
                                DoBlit();

                                (tmpRT1, tmpRT2) = (tmpRT2, tmpRT1);
                            }
                        }
                    }

                    cmd.SetGlobalTexture(k_GlobalFullScreenBlurTexture, tmpRT2);
                    // --- End
                }
            }
        }

        internal class PassData
        {
            internal Material effectMaterial;
            internal int passIndex;
            internal bool requiresColor;
            internal bool disableInSceneView;

            public ProfilingSampler profilingSampler;

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
