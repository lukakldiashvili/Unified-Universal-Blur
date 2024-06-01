using System;
using UnityEngine;

namespace Unified.UniversalBlur.Runtime
{
    internal struct BlurPassData : IDisposable
    {
        internal Material EffectMaterial;
        internal int PassIndex;
        
        public float Downsample;
        public float Intensity;
        public float Scale;
        public int Iterations;
        
        public RenderTextureDescriptor Descriptor;
        
        public
            // #if UNITY_2022_1_OR_NEWER
            // RTHandle
            // #else
            RenderTexture
            // #endif
            RT1, RT2;
        
        public void Dispose()
        {
            if (RT1 != null)
                RT1.Release();
            
            if (RT2 != null)
                RT2.Release();
        }
    }
}