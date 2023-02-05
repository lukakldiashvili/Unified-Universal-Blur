#if !UNITY_2022_1_OR_NEWER
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// This class is trying to emulate RTHandleNeedsReAlloc from 2022 version of unity
/// </summary>
public class RenderEmul_2021 {
	
	/// <summary>
	/// Return true if handle does not match descriptor
	/// </summary>
	/// <param name="handle">RTHandle to check (can be null)</param>
	/// <param name="descriptor">Descriptor for the RTHandle to match</param>
	/// <param name="filterMode">Filtering mode of the RTHandle.</param>
	/// <param name="wrapMode">Addressing mode of the RTHandle.</param>
	/// <param name="isShadowMap">Set to true if the depth buffer should be used as a shadow map.</param>
	/// <param name="anisoLevel">Anisotropic filtering level.</param>
	/// <param name="mipMapBias">Bias applied to mipmaps during filtering.</param>
	/// <param name="name">Name of the RTHandle.</param>
	/// <param name="scaled">Check if the RTHandle has auto scaling enabled if not, check the widths and heights</param>
	/// <returns></returns>
	internal static bool RTHandleNeedsReAlloc(RTHandle handle, in RenderTextureDescriptor descriptor,
	                                          FilterMode filterMode, TextureWrapMode wrapMode, bool isShadowMap,
	                                          int anisoLevel, float mipMapBias, string name, bool scaled) {
		if (handle == null || handle.rt == null)
			return true;
		if (handle.useScaling != scaled)
			return true;
		if (!scaled && (handle.rt.width != descriptor.width || handle.rt.height != descriptor.height))
			return true;
		return handle.rt.descriptor.depthBufferBits != descriptor.depthBufferBits ||
		       (handle.rt.descriptor.depthBufferBits == (int) DepthBits.None && !isShadowMap &&
		        handle.rt.descriptor.graphicsFormat  != descriptor.graphicsFormat) ||
		       handle.rt.descriptor.dimension         != descriptor.dimension ||
		       handle.rt.descriptor.enableRandomWrite != descriptor.enableRandomWrite ||
		       handle.rt.descriptor.useMipMap         != descriptor.useMipMap ||
		       handle.rt.descriptor.autoGenerateMips  != descriptor.autoGenerateMips ||
		       handle.rt.descriptor.msaaSamples       != descriptor.msaaSamples ||
		       handle.rt.descriptor.bindMS            != descriptor.bindMS ||
		       handle.rt.descriptor.useDynamicScale   != descriptor.useDynamicScale ||
		       handle.rt.descriptor.memoryless        != descriptor.memoryless || handle.rt.filterMode != filterMode ||
		       handle.rt.wrapMode                     != wrapMode || handle.rt.anisoLevel != anisoLevel ||
		       handle.rt.mipMapBias                   != mipMapBias || handle.name != name;
	}

	public static bool ReAllocateIfNeeded(ref RTHandle handle, in RenderTextureDescriptor descriptor,
	                                      FilterMode filterMode = FilterMode.Point,
	                                      TextureWrapMode wrapMode = TextureWrapMode.Repeat, bool isShadowMap = false,
	                                      int anisoLevel = 1, float mipMapBias = 0, string name = "") {
		if (RTHandleNeedsReAlloc(handle, descriptor, filterMode, wrapMode, isShadowMap, anisoLevel, mipMapBias, name,
			    false)) {
			handle?.Release();
			handle = RTHandles.Alloc(
				descriptor.width,
				descriptor.height,
				descriptor.volumeDepth,
				(DepthBits)descriptor.depthBufferBits,
				descriptor.graphicsFormat,
				filterMode,
				wrapMode,
				descriptor.dimension,
				descriptor.enableRandomWrite,
				descriptor.useMipMap,
				descriptor.autoGenerateMips,
				isShadowMap,
				anisoLevel,
				mipMapBias,
				(MSAASamples)descriptor.msaaSamples,
				descriptor.bindMS,
				descriptor.useDynamicScale,
				descriptor.memoryless,
				name
			);
			return true;
		}

		return false;
	}
}
#endif