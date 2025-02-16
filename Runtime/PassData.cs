using UnityEngine;
using UnityEngine.Rendering.RenderGraphModule;

namespace Unified.UniversalBlur.Runtime
{
    public class PassData
    {
        public TextureHandle ColorSource;
        public TextureHandle Source;
        public TextureHandle Destination;

        public MaterialPropertyBlock MaterialPropertyBlock;
        
        public BlurConfig BlurConfig;
    }
}