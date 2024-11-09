using UnityEngine;
using UnityEngine.Rendering;
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
        
        [field: SerializeField, HideInInspector] public int PassIndex { get; set; } = 0;

        [field: Header("Blur Settings")]
        [field: SerializeField] private InjectionPoint injectionPoint = InjectionPoint.AfterRenderingPostProcessing;

        [Space]
        [Range(0f, 1f)] [SerializeField] public float intensity = 1.0f;
        [Range(1f, 10f)] [SerializeField] private float downsample = 2.0f;
        [Range(1, 8)] [SerializeField] private int iterations = 5;
        [Range(0f, 10f)] [SerializeField] private float scale = 5f;
        [Range(0f, 10f)] [SerializeField] private float offset = 2;
        [SerializeField] private ScaleBlurWith scaleBlurWith;
        [SerializeField] private float scaleReferenceSize = 1080f;
        
        [SerializeField]
        [HideInInspector]
        [Reload("Shaders/Blur.shader")]
        private Shader shader;
        
        private readonly ScriptableRenderPassInput _requirements = ScriptableRenderPassInput.Color;
        
        public Material PassMaterial => _material;

        // Hidden by scope because of incorrect behaviour in the editor
        private bool disableInSceneView = true;
        
        private Material _material;
        private UniversalBlurPass _fullScreenPass;
        private bool _injectedBeforeTransparents;
        private float _renderScale; 

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

        public override void OnCameraPreCull(ScriptableRenderer renderer, in CameraData cameraData)
        {
            base.OnCameraPreCull(renderer, in cameraData);
            _renderScale = cameraData.renderScale;
        }

        /// <inheritdoc/>
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (!TrySetShadersAndMaterials())
            {
                Debug.LogErrorFormat("{0}.AddRenderPasses(): Missing material. {1} render pass will not be added.", GetType().Name, name);
                return;
            }
            
            var passData = GetBlurPassData(renderingData);
            
            _fullScreenPass.Setup(passData);

            if (renderingData.cameraData.cameraType == CameraType.Game)
            {
                renderer.EnqueuePass(_fullScreenPass);
            }
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            _fullScreenPass?.Dispose();
            CoreUtils.Destroy(_material);
        }
    
        private bool TrySetShadersAndMaterials()
        {
            if (shader == null)
            {
                shader = Shader.Find("Unify/Internal/Blur");
            }
            
            if (_material == null && shader != null)
                _material = CoreUtils.CreateEngineMaterial(shader);
            return _material != null;
        }
        
        private float CalculateScale() => scaleBlurWith switch
        {
            ScaleBlurWith.ScreenHeight => scale * (Screen.height / scaleReferenceSize) * _renderScale,
            ScaleBlurWith.ScreenWidth => scale * (Screen.width / scaleReferenceSize) * _renderScale,
            _ => scale
        };
        
        private BlurPassData GetBlurPassData(in RenderingData renderingData)
        {
            return new BlurPassData
            {
                Scale = CalculateScale(),
                Descriptor = GetDescriptor(renderingData),
                
                EffectMaterial = _material,
                Intensity = intensity,
                Downsample = downsample,
                Offset = offset,
                PassIndex = PassIndex,
                Iterations = iterations,
            };
        }

        private RenderTextureDescriptor GetDescriptor(in RenderingData renderingData)
        {
            RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;
            descriptor.depthBufferBits = (int)DepthBits.None;
            
            descriptor.width =
                Mathf.RoundToInt(descriptor.width / downsample);
            descriptor.height =
                Mathf.RoundToInt(descriptor.height / downsample);

            return descriptor;
        }
    }
}