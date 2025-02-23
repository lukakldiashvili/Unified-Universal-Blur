using UnityEngine;
using UnityEngine.Rendering;

namespace Unified.UniversalBlur.Runtime.CommandBuffer
{
    public readonly struct WrappedCommandBuffer : IWrappedCommandBuffer
    {
        private readonly UnityEngine.Rendering.CommandBuffer _commandBuffer;

        public WrappedCommandBuffer(UnityEngine.Rendering.CommandBuffer commandBuffer)
        {
            _commandBuffer = commandBuffer;
        }

        public void SetRenderTarget(RenderTargetIdentifier rt, int mipLevel, CubemapFace cubemapFace, int depthSlice)
        {
            _commandBuffer.SetRenderTarget(rt, mipLevel, cubemapFace, depthSlice);
        }

        public void DrawProcedural(Matrix4x4 matrix, Material material, int shaderPass, MeshTopology topology,
            int vertexCount, int instanceCount, MaterialPropertyBlock properties)
        {
            _commandBuffer.DrawProcedural(matrix, material, shaderPass, topology, vertexCount, instanceCount, properties);
        }
    }
}