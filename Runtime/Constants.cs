using UnityEngine;

namespace Unified.UniversalBlur.Runtime
{
    public static class Constants
    {
        public static readonly Vector4 DefaultBlitBias = new(1f, 1f, 0f, 0f);
        
        public static readonly int IterationId = Shader.PropertyToID("_Iteration");
        public static readonly int BlurParamsId = Shader.PropertyToID("_BlurParams");
        public static readonly int BlitTextureId = Shader.PropertyToID("_BlitTexture");
        public static readonly int BlitScaleBiasId = Shader.PropertyToID("_BlitScaleBias");
        public static readonly int BlitMipLevelId = Shader.PropertyToID("_BlitMipLevel");
        public static readonly int GlobalFullScreenBlurTextureId = Shader.PropertyToID("_GlobalUniversalBlurTexture");
    }
}