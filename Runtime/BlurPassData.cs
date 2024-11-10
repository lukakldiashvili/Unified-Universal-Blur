using UnityEngine;

namespace Unified.UniversalBlur.Runtime
{
    internal struct BlurPassData
    {
        internal Material Material;
        internal int ShaderPass;
        
        public float Downsample;
        public float Intensity;
        public float Scale;
        public float Offset;
        public int Iterations;

        public int Width;
        public int Height;
    }
}