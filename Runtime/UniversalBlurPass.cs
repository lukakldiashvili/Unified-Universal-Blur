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
        private class PassData
        {
            public TextureHandle ColorSource;
            public TextureHandle Source;
            public TextureHandle Destination;

            public MaterialPropertyBlock MaterialPropertyBlock;
            public Material Material;
            public int ShaderPass;
            
            public float Downsample;
            public float Intensity;
            public float Scale;
            public float Offset;
            public int Iterations;
        }
        
        private const string k_PassName = "Universal Blur";
        private const string k_BlurTextureSourceName = k_PassName + " - Blur Source";
        private const string k_BlurTextureDestinationName = k_PassName + " - Blur Destination";

        private static readonly Vector4 s_DefaultBlitBias = new(1f, 1f, 0f, 0f);
        
        private static readonly int s_BlurOffsetID = Shader.PropertyToID("_BlurOffset");
        private static readonly int s_BlitTextureID = Shader.PropertyToID("_BlitTexture");
        private static readonly int s_BlitScaleBias = Shader.PropertyToID("_BlitScaleBias");
        private static readonly int s_GlobalFullScreenBlurTextureID = Shader.PropertyToID("_GlobalUniversalBlurTexture");

        private readonly ProfilingSampler _profilingSampler;
        private readonly MaterialPropertyBlock _propertyBlock;

        private BlurPassData _blurPassData;
        
        public UniversalBlurPass()
        {
            _profilingSampler = new(k_PassName);
            _propertyBlock = new();
        }

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

            if (resourceData.isActiveTargetBackBuffer)
            {
                Debug.LogError(
                    $"Skipping render pass. UniversalBlurPass requires an intermediate ColorTexture, we can't use the BackBuffer as a texture input.");
                return;
            }

            var cameraColorSource = resourceData.activeColorTexture;
            
            var rtDescriptor = renderGraph.GetTextureDesc(cameraColorSource);
            rtDescriptor.width = _blurPassData.Width;
            rtDescriptor.height = _blurPassData.Height;
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

                passData.Material = _blurPassData.Material;
                passData.ShaderPass = _blurPassData.ShaderPass;
                passData.Iterations = _blurPassData.Iterations;
                passData.Intensity = _blurPassData.Intensity;
                passData.Downsample = _blurPassData.Downsample;
                passData.Scale = _blurPassData.Scale;
                passData.Offset = _blurPassData.Offset;
                
                
                builder.AllowPassCulling(false);
                
                builder.UseTexture(source, AccessFlags.ReadWrite);
                builder.UseTexture(destination, AccessFlags.ReadWrite);
                
                builder.SetGlobalTextureAfterPass(destination, s_GlobalFullScreenBlurTextureID);
                
                builder.SetRenderFunc((PassData data, UnsafeGraphContext ctx) => BlitMaterialRenderFunc(data, ctx));
            }
        }

        private static void BlitMaterialRenderFunc(PassData data, UnsafeGraphContext ctx)
        {
            // Note: renderGraphPool.GetTempMaterialPropertyBlock is a preferable way to get a MaterialPropertyBlock,
            // but it causes per-frame allocations in this context.
            var mpb = data.MaterialPropertyBlock;
            
            mpb.Clear();
            mpb.SetVector(s_BlitScaleBias, s_DefaultBlitBias);
            
            var source = data.Source;
            var destination = data.Destination;
            
            // If the number of iterations is odd, it means that the last iteration will blit to the source texture
            // So we need to swap the source and destination textures
            if (data.Iterations % 2 == 1)
            {
                (source, destination) = (destination, source);
            }
            
            // Initial blit from camera color to allow blit between target textures
            BlitTexture(ctx, data.ColorSource, source, data, mpb, CalculateOffset(data, 0));
            
            for (int i = 1; i < data.Iterations; i++)
            {
                var offset = CalculateOffset(data, i);
                        
                BlitTexture(ctx, source, destination, data, mpb, offset);
                
                (source, destination) = (destination, source);
            }
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float CalculateOffset(PassData data, int iteration)
        {
            return (data.Offset + iteration * data.Scale) / data.Downsample * data.Intensity;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void BlitTexture(UnsafeGraphContext context, TextureHandle sourceHandle, TextureHandle destinationHandle,
            PassData data, MaterialPropertyBlock mpb, float offset)
        {
            mpb.SetFloat(s_BlurOffsetID, offset);
            mpb.SetTexture(s_BlitTextureID, sourceHandle);
            
            // TODO: Implement mipLevel and depthSlice support, if relevant
            context.cmd.SetRenderTarget(destinationHandle, 0, CubemapFace.Unknown, 0);
            context.cmd.DrawProcedural(Matrix4x4.identity, data.Material, data.ShaderPass, MeshTopology.Quads, 4, 1, mpb);
        }
    }
}