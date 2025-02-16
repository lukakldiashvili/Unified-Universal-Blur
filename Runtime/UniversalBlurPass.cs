using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

using BlitParams = UnityEngine.Rendering.RenderGraphModule.Util.RenderGraphUtils.BlitMaterialParameters;

namespace Unified.UniversalBlur.Runtime
{
    internal class UniversalBlurPass : ScriptableRenderPass, IDisposable
    {
        private const string k_PassName = "Universal Blur";
        private const string k_BlurTextureSourceName = k_PassName + " - Blur Source";
        private const string k_BlurTextureDestinationName = k_PassName + " - Blur Destination";

        private readonly ProfilingSampler _profilingSampler;
        private readonly MaterialPropertyBlock _propertyBlock;

        private BlurConfig _blurConfig;
        
        public UniversalBlurPass()
        {
            _profilingSampler = new(k_PassName);
            _propertyBlock = new();
        }

        public void Setup(BlurConfig blurConfig)
        {
            _blurConfig = blurConfig;
        }

        public void Dispose()
        {
            // Nothing to dispose
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var resourceData = frameData.Get<UniversalResourceData>();

            if (resourceData.isActiveTargetBackBuffer)
            {
                Debug.LogError(
                    $"Skipping render pass. UniversalBlurPass requires an intermediate ColorTexture, we can't use the BackBuffer as a texture input.");
                return;
            }

            var cameraColorSource = resourceData.activeColorTexture;
            
            var rtDescriptor = renderGraph.GetTextureDesc(cameraColorSource);
            rtDescriptor.width = _blurConfig.Width;
            rtDescriptor.height = _blurConfig.Height;
            rtDescriptor.clearBuffer = false;
            
            rtDescriptor.name = k_BlurTextureSourceName;
            TextureHandle source = renderGraph.CreateTexture(rtDescriptor);
            rtDescriptor.name = k_BlurTextureDestinationName;
            TextureHandle destination = renderGraph.CreateTexture(rtDescriptor);
            
            using (var builder = renderGraph.AddUnsafePass<PassData>(k_PassName, out var passData, _profilingSampler))
            {
                passData.ColorSource = cameraColorSource;
                passData.Source = source;
                passData.Destination = destination;

                passData.MaterialPropertyBlock = _propertyBlock;
                
                passData.BlurConfig = _blurConfig;
                
                builder.AllowPassCulling(false);
                
                builder.UseTexture(source, AccessFlags.ReadWrite);
                builder.UseTexture(destination, AccessFlags.ReadWrite);
                
                builder.SetGlobalTextureAfterPass(destination, Constants.GlobalFullScreenBlurTextureId);
                
                builder.SetRenderFunc<PassData>(BlurPasses.KawaseExecutePass);
            }
        }
    }
}