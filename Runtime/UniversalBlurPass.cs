using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

using BlitParams = UnityEngine.Rendering.RenderGraphModule.Util.RenderGraphUtils.BlitMaterialParameters;

namespace Unified.UniversalBlur.Runtime
{
    internal class UniversalBlurPass : ScriptableRenderPass, IDisposable
    {
        class PassData { }
        
        private const string k_PassName = "KawaseBlurMain";
        private const string k_BlurTextureName1 = k_PassName + " - Blur RT1";
        private const string k_BlurTextureName2 = k_PassName + " - Blur RT2";

        private static readonly int s_kawaseOffsetID = Shader.PropertyToID("_KawaseOffset");
        private static readonly int s_globalFullScreenBlurTexture = Shader.PropertyToID("_GlobalFullScreenBlurTexture");

        private readonly ProfilingSampler _profilingSampler = new(k_PassName);

        private BlurPassData _blurPassData;

        public void Setup(BlurPassData blurPassData)
        {
            _blurPassData = blurPassData;
        }

        public void Dispose()
        {
            // Nothing to dispose
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var resourceData = frameData.Get<UniversalResourceData>();
            // var cameraData = frameData.Get<UniversalCameraData>();

            if (resourceData.isActiveTargetBackBuffer)
            {
                Debug.LogError(
                    $"Skipping render pass. BlitAndSwapColorRendererFeature requires an intermediate ColorTexture, we can't use the BackBuffer as a texture input.");
                return;
            }

            var cameraColorSource = resourceData.activeColorTexture;
            
            var rtDescriptor = renderGraph.GetTextureDesc(cameraColorSource);
            rtDescriptor.width = _blurPassData.Width;
            rtDescriptor.height = _blurPassData.Height;
            rtDescriptor.wrapMode = TextureWrapMode.Clamp;
            rtDescriptor.clearBuffer = false;

            rtDescriptor.name = k_BlurTextureName1;
            TextureHandle rt1 = renderGraph.CreateTexture(rtDescriptor);
            rtDescriptor.name = k_BlurTextureName2;
            TextureHandle rt2 = renderGraph.CreateTexture(rtDescriptor);
            
            GraphAddBlitPass(renderGraph, cameraColorSource, rt1, _blurPassData.EffectMaterial, _blurPassData.PassIndex, _blurPassData.Offset);
            
            for (int i = 0; i < _blurPassData.Iterations; i++)
            {
                var offset = (_blurPassData.Offset + i * _blurPassData.Scale) * (_blurPassData.Intensity / _blurPassData.Downsample);
                        
                GraphAddBlitPass(renderGraph, rt1, rt2, _blurPassData.EffectMaterial, _blurPassData.PassIndex, offset);
                
                (rt1, rt2) = (rt2, rt1);
            }
            
            using (var builder = renderGraph.AddRasterRenderPass<PassData>(k_PassName, out var passData, _profilingSampler))
            {
                builder.AllowPassCulling(false);
                
                builder.UseTexture(rt1);
                builder.UseTexture(rt2);
                
                builder.SetGlobalTextureAfterPass(rt2, s_globalFullScreenBlurTexture);
                
                builder.SetRenderFunc((PassData data, RasterGraphContext context) => { /* Do nothing */ });
            }
        }

        private static void GraphAddBlitPass(RenderGraph renderGraph, TextureHandle source, TextureHandle destination, Material material, int passIndex, float blurOffset)
        {
            // TODO: Implement cached material property blocks
            var mpb = new MaterialPropertyBlock();
            mpb.SetFloat(s_kawaseOffsetID, blurOffset);
            
            renderGraph.AddBlitPass(new BlitParams(source, destination, material, passIndex, mpb: mpb), passName: k_PassName);
        }
    }
}