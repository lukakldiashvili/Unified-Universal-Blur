#if UNITY_6000_0_OR_NEWER
using UnityEngine;
using UnityEngine.Rendering;

namespace Unified.UniversalBlur.Runtime.CommandBuffer
{
    public readonly struct WrappedUnsafeCommandBuffer : IWrappedCommandBuffer
    {
        private readonly UnsafeCommandBuffer _unsafeCommandBuffer;

        public WrappedUnsafeCommandBuffer(UnsafeCommandBuffer unsafeCommandBuffer)
        {
            _unsafeCommandBuffer = unsafeCommandBuffer;
        }

        public void SetRenderTarget(RenderTargetIdentifier rt, int mipLevel, CubemapFace cubemapFace, int depthSlice)
        {
            _unsafeCommandBuffer.SetRenderTarget(rt, mipLevel, cubemapFace, depthSlice);
        }

        public void DrawProcedural(Matrix4x4 matrix, Material material, int shaderPass, MeshTopology topology,
            int vertexCount, int instanceCount, MaterialPropertyBlock properties)
        {
            _unsafeCommandBuffer.DrawProcedural(matrix, material, shaderPass, topology, vertexCount, instanceCount, properties);
        }
    }
}
#endif