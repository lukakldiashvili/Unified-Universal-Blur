using UnityEngine;

namespace Unified.UniversalBlur.Runtime
{
    public struct BlurConfig
    {
        internal Material Material;
        internal BlurType BlurType;
        
        public float Downsample;
        public float Intensity;
        public float Scale;
        public float Offset;
        public int Iterations;

        public int Width;
        public int Height;

        public bool EnableMipMaps;
    }
}