using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Unified.Universal.Blur
{
    public class UniversalBlurFeature : ScriptableRendererFeature
    {
        public enum InjectionPoint
        {
            BeforeRenderingTransparents = RenderPassEvent.BeforeRenderingTransparents,
            BeforeRenderingPostProcessing = RenderPassEvent.BeforeRenderingPostProcessing,
            AfterRenderingPostProcessing = RenderPassEvent.AfterRenderingPostProcessing
        }

        public Material passMaterial;
        [HideInInspector] public int passIndex = 0;

        [Header("Blur Settings")]
        public InjectionPoint injectionPoint = InjectionPoint.AfterRenderingPostProcessing;

        [Space]
        [Range(0f, 1f)] public float intensity = 1.0f;
        [Range(1f, 10f)] public float downsample = 2.0f;
        [Range(0f, 5f)] public float scale = .5f;
        [Range(1, 20)] public int iterations = 6;


        // Hidden by scope because of no need
        private ScriptableRenderPassInput requirements = ScriptableRenderPassInput.Color;

        // Hidden by scope because of incorrect behaviour in the editor
        private bool disableInSceneView = true;

        private UniversalBlurPass fullScreenPass;
        private bool requiresColor;
        private bool injectedBeforeTransparents;

        /// <inheritdoc/>
        public override void Create()
        {
            fullScreenPass = new UniversalBlurPass();
            fullScreenPass.renderPassEvent = (RenderPassEvent)injectionPoint;

            ScriptableRenderPassInput modifiedRequirements = requirements;

            requiresColor = (requirements & ScriptableRenderPassInput.Color) != 0;
            injectedBeforeTransparents = injectionPoint <= InjectionPoint.BeforeRenderingTransparents;

            if (requiresColor && !injectedBeforeTransparents)
            {
                modifiedRequirements ^= ScriptableRenderPassInput.Color;
            }

            fullScreenPass.ConfigureInput(modifiedRequirements);
        }

        /// <inheritdoc/>
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (passMaterial == null)
            {
                Debug.LogWarningFormat("Missing Post Processing effect Material. {0} Fullscreen pass will not execute. Check for missing reference in the assigned renderer.", GetType().Name);
                return;
            }

            fullScreenPass.Setup(SetupPassData, downsample, renderingData);

            renderer.EnqueuePass(fullScreenPass);
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            fullScreenPass.Dispose();
        }

        void SetupPassData(UniversalBlurPass.PassData passData)
        {
            passData.effectMaterial = passMaterial;
            passData.intensity = intensity;
            passData.passIndex = passIndex;
            passData.requiresColor = requiresColor;
            passData.profilingSampler ??= new ProfilingSampler("FullScreenPassRendererFeature");
            passData.scale = scale;
            passData.iterations = iterations;
            passData.disableInSceneView = disableInSceneView;
        }
    }

}