using System.Runtime.CompilerServices;
using Unified.UniversalBlur.Runtime.CommandBuffer;
using Unified.UniversalBlur.Runtime.PassData;
using UnityEngine;

namespace Unified.UniversalBlur.Runtime
{
    public static class BlurPasses
    {
        public static void KawaseExecutePass<TPass, T>(TPass data, T cmd) where TPass : IPassData where T : IWrappedCommandBuffer
        {
            // Note: renderGraphPool.GetTempMaterialPropertyBlock is a preferable way to get a MaterialPropertyBlock,
            // but it causes per-frame allocations in this context.
            var mpb = data.GetMaterialPropertyBlock();
            
            mpb.Clear();
            mpb.SetVector(Constants.BlitScaleBiasId, Constants.DefaultBlitBias);

            var blurConfig = data.GetBlurConfig();
            var colorSource = data.GetColorSource();
            var source = data.GetSource();
            var destination = data.GetDestination();
            
            // If the number of iterations is odd, it means that the last iteration will blit to the source texture
            // So we need to swap the source and destination textures
            if (blurConfig.Iterations % 2 == 1)
            {
                (source, destination) = (destination, source);
            }
            
            // Initial blit from camera color to allow blit between target textures
            BlitTexture(cmd, colorSource, source, blurConfig, mpb, CalculateOffset(blurConfig, 0), 0);
            
            for (int i = 1; i < blurConfig.Iterations; i++)
            {
                var offset = CalculateOffset(blurConfig, i);
                        
                BlitTexture(cmd, source, destination, blurConfig, mpb, offset, i - 1);
                
                (source, destination) = (destination, source);
            }
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float CalculateOffset(BlurConfig blurConfig, int iteration)
        {
            return (blurConfig.Offset + iteration * blurConfig.Scale) / blurConfig.Downsample;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void BlitTexture<T>(T cmd, Texture sourceHandle, Texture destinationHandle,
            BlurConfig blurConfig, MaterialPropertyBlock mpb, float offset, int iteration) where T : IWrappedCommandBuffer
        {
            mpb.SetInt(Constants.IterationId, iteration);
            mpb.SetVector(Constants.BlurParamsId, new Vector4(blurConfig.Intensity, blurConfig.Scale, blurConfig.Downsample, blurConfig.Offset));
            mpb.SetTexture(Constants.BlitTextureId, sourceHandle);
            
            // TODO: add a lookup for getting mipmap level from offset value
            mpb.SetFloat(Constants.BlitMipLevelId, blurConfig.EnableMipMaps ? Mathf.Log(offset, 2) : 0);
            
            cmd.SetRenderTarget(destinationHandle, 0, CubemapFace.Unknown, 0);
            cmd.DrawProcedural(Matrix4x4.identity, blurConfig.Material, 0, MeshTopology.Quads, 4, 1, mpb);
        }
    }
}