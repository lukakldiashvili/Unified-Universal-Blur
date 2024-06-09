using UnityEngine;

namespace Unified.UniversalBlur.Runtime
{
    public static class Helpers
    {
        // Derived from RenderingUtils.RTHandleNeedsReAlloc
        public static bool HasDescriptorChanged(RenderTextureDescriptor descriptorA, RenderTextureDescriptor descriptorB, bool scaled)
        {
            if (descriptorA.useDynamicScale != scaled)
                return true;
            if (!scaled && (descriptorA.width != descriptorB.width || descriptorA.height != descriptorB.height))
                return true;
            return
                descriptorA.depthBufferBits != descriptorB.depthBufferBits ||
                descriptorA.dimension != descriptorB.dimension ||
                descriptorA.enableRandomWrite != descriptorB.enableRandomWrite ||
                descriptorA.useMipMap != descriptorB.useMipMap ||
                descriptorA.autoGenerateMips != descriptorB.autoGenerateMips ||
                descriptorA.msaaSamples != descriptorB.msaaSamples ||
                descriptorA.bindMS != descriptorB.bindMS ||
                descriptorA.useDynamicScale != descriptorB.useDynamicScale ||
                descriptorA.memoryless != descriptorB.memoryless;
        }
    }
}