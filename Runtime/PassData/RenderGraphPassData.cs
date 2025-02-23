#if UNITY_6000_0_OR_NEWER

using UnityEngine;
using UnityEngine.Rendering.RenderGraphModule;

namespace Unified.UniversalBlur.Runtime.PassData
{
    public class RenderGraphPassData : IPassData
    {
        public TextureHandle ColorSource;
        public TextureHandle Source;
        public TextureHandle Destination;

        public MaterialPropertyBlock MaterialPropertyBlock;

        public BlurConfig BlurConfig;

        public Texture GetColorSource()
        {
            return ColorSource;
        }

        public Texture GetSource()
        {
            return Source;
        }

        public Texture GetDestination()
        {
            return Destination;
        }

        public MaterialPropertyBlock GetMaterialPropertyBlock()
        {
            return MaterialPropertyBlock;
        }

        public BlurConfig GetBlurConfig()
        {
            return BlurConfig;
        }
    }
}
#endif