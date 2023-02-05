using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

class UniversalBlurPass : ScriptableRenderPass {
	private const string k_GlobalFullScreenBlurTexture = "_GlobalFullScreenBlurTexture";
	
	private static readonly int m_BlitTextureShaderID = Shader.PropertyToID("_BlitTexture");
	private static readonly int m_KawaseOffsetID = Shader.PropertyToID("_KawaseOffset");

	private PassData m_PassData;

	private RTHandle m_tmpRT1;
	private RTHandle m_tmpRT2;

	public void Setup(Action<PassData> passDataOptions, float downsample, in RenderingData renderingData) {
		m_PassData ??= new PassData();
		
		passDataOptions?.Invoke(m_PassData);

		RenderTextureDescriptor rtDesc = renderingData.cameraData.cameraTargetDescriptor;
		rtDesc.depthBufferBits = (int) DepthBits.None;

		rtDesc.width  = Mathf.RoundToInt(rtDesc.width  / downsample);
		rtDesc.height = Mathf.RoundToInt(rtDesc.height / downsample);
		
		#if UNITY_2022_1_OR_NEWER
		RenderingUtils.ReAllocateIfNeeded(ref m_tmpRT1, rtDesc, name: "_PassRT1");
		RenderingUtils.ReAllocateIfNeeded(ref m_tmpRT2, rtDesc, name: "_PassRT2");
		#else
		RenderEmul_2021.ReAllocateIfNeeded(ref m_tmpRT1, rtDesc, name: "_PassRT1");
		RenderEmul_2021.ReAllocateIfNeeded(ref m_tmpRT2, rtDesc, name: "_PassRT2");
		#endif
		
		m_PassData.tmpRT1 = m_tmpRT1;
		m_PassData.tmpRT2 = m_tmpRT2;
	}

	public void Dispose() {
		m_tmpRT1?.Release();
		m_tmpRT2?.Release();
	}

	public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) {
		ExecutePass(m_PassData, ref renderingData, ref context);
	}

	private static void ExecutePass(PassData passData, ref RenderingData renderingData,
	                                ref ScriptableRenderContext context) 
	{
		var passMaterial = passData.effectMaterial;
		var tmpRT1       = passData.tmpRT1;
		var tmpRT2       = passData.tmpRT2;
		var scale    = passData.scale;

		// should not happen as we check it in feature
		if (passMaterial == null)
			return;

		if (renderingData.cameraData.isPreviewCamera)
			return;

		// if is scene camera and we want to disable in scene view
		if (renderingData.cameraData.isSceneViewCamera && passData.disableInSceneView)
			return;

		CommandBuffer cmd        = 
			#if UNITY_2022_1_OR_NEWER
			renderingData.commandBuffer;
			#else
			CommandBufferPool.Get(); 
			#endif
			
		var cameraData = renderingData.cameraData;

		using (new ProfilingScope(cmd, passData.profilingSampler)) {
			ProcessEffect(ref context);
		}


		void ProcessEffect(ref ScriptableRenderContext context) {
			if (passData.requiresColor) {
				var source =
					#if UNITY_2022_1_OR_NEWER
					passData.isBeforeTransparents
					? cameraData.renderer.GetCameraColorBackBuffer(cmd)
					: cameraData.renderer.cameraColorTargetHandle;
					#else
					cameraData.renderer.cameraColorTarget;
					#endif
				
				// --- Start
				#if UNITY_2022_1_OR_NEWER
				Blitter.BlitCameraTexture(cmd, source, tmpRT1);
				#else
				cmd.Blit(source, tmpRT1);
				#endif
				
				void DoBlit () {
					cmd.Blit(tmpRT1, tmpRT2, passMaterial, 0);
					// Blitter.BlitCameraTexture(cmd, tmpRT1, tmpRT2, passMaterial, 0);
				}
				
				{
					cmd.SetGlobalFloat(m_KawaseOffsetID, 1.5f);
					DoBlit();
				
					for (var i = 1; i <= passData.iterations; i++) {
						cmd.SetGlobalFloat(m_KawaseOffsetID, 0.5f + i * scale);
						DoBlit();
						
						(tmpRT1, tmpRT2) = (tmpRT2, tmpRT1);
					}
				}
				
				cmd.SetGlobalTexture(k_GlobalFullScreenBlurTexture, tmpRT2);
				// --- End
			}
			
			context.ExecuteCommandBuffer(cmd);
			cmd.Clear();
		}
	}

	internal class PassData {
		internal Material effectMaterial;
		internal int passIndex;
		internal bool requiresColor;
		internal bool disableInSceneView;
		internal bool isBeforeTransparents;
		
		public ProfilingSampler profilingSampler;

		public float scale;
		public int iterations;
		
		public RTHandle tmpRT1;
		public RTHandle tmpRT2;
	}
}