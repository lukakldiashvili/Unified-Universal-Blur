using System;
using Unified.UniversalBlur.Runtime.CommandBuffer;
using Unified.UniversalBlur.Runtime.PassData;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

#if UNITY_6000_0_OR_NEWER
using UnityEngine.Rendering.RenderGraphModule;
#endif

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
        private RTHandle _sourceRT;
        private RTHandle _destinationRT;
        
        public UniversalBlurPass()
        {
            _profilingSampler = new(k_PassName);
            _propertyBlock = new();

#if UNITY_6000_0_OR_NEWER
            requiresIntermediateTexture = true;
#endif
        }

        public void Setup(BlurConfig blurConfig)
        {
            _blurConfig = blurConfig;

            // declare the need for intermediate texture
            ConfigureInput(ScriptableRenderPassInput.Color);
        }

        public void Dispose()
        {
            // Nothing to dispose
        }
        
        public void DrawDefaultTexture()
        {
            // For better preview experience in editor, we just use a gray texture
            Shader.SetGlobalTexture(Constants.GlobalFullScreenBlurTextureId, Texture2D.linearGrayTexture);
        }

        private RenderTextureDescriptor GetDescriptor() =>
            new(_blurConfig.Width, _blurConfig.Height, _blurConfig.GraphicsFormat, 0)
            {
                useMipMap = _blurConfig.EnableMipMaps,
                autoGenerateMips = _blurConfig.EnableMipMaps
            };

#if !UNITY_6000_4_OR_NEWER
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var cmd = CommandBufferPool.Get();

            var descriptor = GetDescriptor();

#if UNITY_6000_0_OR_NEWER
            RenderingUtils.ReAllocateHandleIfNeeded(ref _sourceRT, descriptor, FilterMode.Bilinear, TextureWrapMode.Clamp, name: k_BlurTextureSourceName);
            RenderingUtils.ReAllocateHandleIfNeeded(ref _destinationRT, descriptor, FilterMode.Bilinear, TextureWrapMode.Clamp, name: k_BlurTextureDestinationName);
            #else
            RenderingUtils.ReAllocateIfNeeded(ref _sourceRT, descriptor, FilterMode.Bilinear, TextureWrapMode.Clamp, name: k_BlurTextureSourceName);
            RenderingUtils.ReAllocateIfNeeded(ref _destinationRT, descriptor, FilterMode.Bilinear, TextureWrapMode.Clamp, name: k_BlurTextureDestinationName);
            #endif
            

            var colorTarget = renderingData.cameraData.renderer.cameraColorTargetHandle;

            if (colorTarget == null || colorTarget.rt == null)
            {
                CommandBufferPool.Release(cmd);
                DrawDefaultTexture();
                return;
            }

            using (new ProfilingScope(cmd, _profilingSampler))
            {
                BlurPasses.KawaseExecutePass(new LegacyPassData()
                {
                    BlurConfig = _blurConfig,
                    MaterialPropertyBlock = _propertyBlock,
                    ColorSource = colorTarget,
                    Source = _sourceRT,
                    Destination = _destinationRT
                }, new WrappedCommandBuffer(cmd));

                cmd.SetGlobalTexture(Constants.GlobalFullScreenBlurTextureId, _destinationRT);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
#endif
        
#if UNITY_6000_0_OR_NEWER
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var resourceData = frameData.Get<UniversalResourceData>();

            // afterPostProcessColor is [Obsolete] in URP 17.6 and not assigned by the renderer; cameraColor is the supported backbuffer fallback.
            var cameraColorSource = resourceData.isActiveTargetBackBuffer
                ? resourceData.cameraColor
                : resourceData.activeColorTexture;

            // Depth-only cameras (e.g. offscreen shadow passes) can hand us a null handle here.
            if (!cameraColorSource.IsValid())
                return;

            var descriptor = new TextureDesc(GetDescriptor());

            descriptor.name = k_BlurTextureSourceName;
            TextureHandle source = renderGraph.CreateTexture(descriptor);
            descriptor.name = k_BlurTextureDestinationName;
            TextureHandle destination = renderGraph.CreateTexture(descriptor);
            
            using (var builder = renderGraph.AddUnsafePass<RenderGraphPassData>(k_PassName, out var passData, _profilingSampler))
            {
                passData.ColorSource = cameraColorSource;
                passData.Source = source;
                passData.Destination = destination;

                passData.MaterialPropertyBlock = _propertyBlock;
                
                passData.BlurConfig = _blurConfig;
                
                builder.AllowPassCulling(false);

                builder.UseTexture(cameraColorSource, AccessFlags.Read);
                builder.UseTexture(source, AccessFlags.ReadWrite);
                builder.UseTexture(destination, AccessFlags.ReadWrite);
                
                builder.SetGlobalTextureAfterPass(destination, Constants.GlobalFullScreenBlurTextureId);
                
                builder.SetRenderFunc<RenderGraphPassData>((data, ctx) =>
                {
                    BlurPasses.KawaseExecutePass(data, new WrappedUnsafeCommandBuffer(ctx.cmd));
                });
            }
        }
#endif
    }
}