using UnityEngine;
using UnityEngine.Rendering;

namespace Unified.UniversalBlur.Runtime.CommandBuffer
{
    public interface IWrappedCommandBuffer
    {
        public void SetRenderTarget(RenderTargetIdentifier rt, int mipLevel, CubemapFace cubemapFace, int depthSlice);
        public void DrawProcedural(Matrix4x4 matrix, Material material, int shaderPass, MeshTopology topology,
            int vertexCount, int instanceCount, MaterialPropertyBlock properties);
    }
}