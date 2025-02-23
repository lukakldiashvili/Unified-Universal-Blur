using UnityEngine;

namespace Unified.UniversalBlur.Runtime.PassData
{
    public struct LegacyPassData : IPassData
    {
        public Texture ColorSource;
        public Texture Source;
        public Texture Destination;

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