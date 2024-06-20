using UnityEngine;

namespace Unified.UniversalBlur.Runtime
{
    internal struct BlurPassData
    {
        internal Material EffectMaterial;
        internal int PassIndex;
        
        public float Downsample;
        public float Intensity;
        public float Scale;
        public int Iterations;

        public RenderTextureDescriptor Descriptor;
    }
}