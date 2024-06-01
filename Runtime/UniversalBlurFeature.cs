using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Unified.UniversalBlur.Runtime
{
    public class UniversalBlurFeature : ScriptableRendererFeature
    {
        private enum InjectionPoint
        {
            BeforeRenderingTransparents = RenderPassEvent.BeforeRenderingTransparents,
            BeforeRenderingPostProcessing = RenderPassEvent.BeforeRenderingPostProcessing,
            AfterRenderingPostProcessing = RenderPassEvent.AfterRenderingPostProcessing
        }
        
        [field: SerializeField] public Material PassMaterial { get; private set; }
        
        [field: Tooltip("Do not change this value unless you are using non-default shader.")]
        [field: SerializeField] public int PassIndex { get; set; } = 0;

        [field: Header("Blur Settings")]
        [field: SerializeField] private InjectionPoint injectionPoint = InjectionPoint.AfterRenderingPostProcessing;

        [Space]
        
        [Range(0f, 5f)] [SerializeField] private float intensity = 1.0f;
        [Range(1f, 10f)] [SerializeField] private float downsample = 2.0f;
        [Range(0f, 5f)] [SerializeField] private float scale = .5f;
        [Range(1, 20)] [SerializeField] private int iterations = 6;
        
        private readonly ScriptableRenderPassInput _requirements = ScriptableRenderPassInput.Color;

        // Hidden by scope because of incorrect behaviour in the editor
        private bool disableInSceneView = true;

        private UniversalBlurPass _fullScreenPass;
        private bool _injectedBeforeTransparents;

        /// <inheritdoc/>
        public override void Create()
        {
            _fullScreenPass = new UniversalBlurPass();
            _fullScreenPass.renderPassEvent = (RenderPassEvent)injectionPoint;

            ScriptableRenderPassInput modifiedRequirements = _requirements;

            var requiresColor = (_requirements & ScriptableRenderPassInput.Color) != 0;
            _injectedBeforeTransparents = injectionPoint <= InjectionPoint.BeforeRenderingTransparents;

            if (requiresColor && !_injectedBeforeTransparents)
            {
                modifiedRequirements ^= ScriptableRenderPassInput.Color;
            }

            _fullScreenPass.ConfigureInput(modifiedRequirements);
        }

        /// <inheritdoc/>
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (PassMaterial == null)
            {
                Debug.LogWarningFormat("Missing Post Processing effect Material. {0} Fullscreen pass will not execute. Check for missing reference in the assigned renderer.", GetType().Name);
                return;
            }
            
            var passData = GetBlurPassData();
            
            _fullScreenPass.Setup(passData, renderingData);

            renderer.EnqueuePass(_fullScreenPass);
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            _fullScreenPass?.Dispose();
        }
        
        private BlurPassData GetBlurPassData()
        {
            return new BlurPassData()
            {
                EffectMaterial = PassMaterial,
                Downsample = downsample,
                Intensity = intensity,
                PassIndex = PassIndex,
                Scale = scale,
                Iterations = iterations,
            };
        }
    }
}