using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Rendering.RenderGraphModule;

namespace Unified.UniversalBlur.Runtime
{
    public static class BlurPasses
    {
        public static void KawaseExecutePass(PassData data, UnsafeGraphContext ctx)
        {
            // Note: renderGraphPool.GetTempMaterialPropertyBlock is a preferable way to get a MaterialPropertyBlock,
            // but it causes per-frame allocations in this context.
            var mpb = data.MaterialPropertyBlock;
            
            mpb.Clear();
            mpb.SetVector(Constants.BlitScaleBiasId, Constants.DefaultBlitBias);
            
            var source = data.Source;
            var destination = data.Destination;
            
            // If the number of iterations is odd, it means that the last iteration will blit to the source texture
            // So we need to swap the source and destination textures
            if (data.BlurConfig.Iterations % 2 == 1)
            {
                (source, destination) = (destination, source);
            }
            
            // Initial blit from camera color to allow blit between target textures
            BlitTexture(ctx, data.ColorSource, source, data, mpb, CalculateOffset(data, 0), 0);
            
            for (int i = 1; i < data.BlurConfig.Iterations; i++)
            {
                var offset = CalculateOffset(data, i);
                        
                BlitTexture(ctx, source, destination, data, mpb, offset, i - 1);
                
                (source, destination) = (destination, source);
            }
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float CalculateOffset(PassData data, int iteration)
        {
            return (data.BlurConfig.Offset + iteration * data.BlurConfig.Scale) / data.BlurConfig.Downsample * data.BlurConfig.Intensity;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void BlitTexture(UnsafeGraphContext context, TextureHandle sourceHandle, TextureHandle destinationHandle,
            PassData data, MaterialPropertyBlock mpb, float offset, int iteration)
        {
            mpb.SetInt(Constants.IterationId, iteration);
            mpb.SetVector(Constants.BlurParamsId, new Vector4(data.BlurConfig.Intensity, data.BlurConfig.Scale, data.BlurConfig.Downsample, data.BlurConfig.Offset));
            mpb.SetTexture(Constants.BlitTextureId, sourceHandle);
            
            // TODO: Implement mipLevel and depthSlice support, if relevant
            context.cmd.SetRenderTarget(destinationHandle, 0, CubemapFace.Unknown, 0);
            context.cmd.DrawProcedural(Matrix4x4.identity, data.BlurConfig.Material, 0, MeshTopology.Quads, 4, 1, mpb);
        }
    }
}