using UnityEngine;

namespace Unified.UniversalBlur.Runtime.PassData
{
    public interface IPassData
    {
        public BlurConfig GetBlurConfig();
        
        public MaterialPropertyBlock GetMaterialPropertyBlock();
        
        public Texture GetColorSource();
        public Texture GetSource();
        public Texture GetDestination();
    }
}