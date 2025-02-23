using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Unified.UniversalBlur.Runtime
{
    public class UniversalBlurFeature : ScriptableRendererFeature
    {
        [Header("Blur Settings")]
        [Range(1, 8)] [SerializeField] private int iterations = 4;
        
        [Space]
        
        [Range(0f, 1f)] [SerializeField] public float intensity = 1.0f;
        [Range(1f, 10f)] [SerializeField] private float downsample = 2.0f;
        // [Range(0f, 10f)] 
        [SerializeField] private float scale = 1f;
        // [Range(0f, 10f)] 
        [SerializeField] private float offset = 2f;
        
        [Space]
        
        [Header("Advanced Settings")]
        [SerializeField] private ScaleBlurWith scaleBlurWith = ScaleBlurWith.ScreenHeight;
        [SerializeField] private float scaleReferenceSize = 1080f;
        
        [Space]
        
        // [SerializeField, ShowAsPass(nameof(_material))] public int shaderPass;
        [SerializeField] private BlurType blurType;
        
        [Tooltip("For Overlay Canvas: AfterRenderingPostProcessing" +
                 "\n\nOther: BeforeRenderingTransparents (will hide transparents)")]
        [SerializeField] private RenderPassEvent injectionPoint = RenderPassEvent.AfterRenderingPostProcessing;
        
        [SerializeField]
        [HideInInspector]
        [Reload("Shaders/Blur.shader")]
        private Shader shader;
        
        private Material _material;
        private UniversalBlurPass _blurPass;
        private float _renderScale; 

        /// <inheritdoc/>
        public override void Create()
        {
            _blurPass = new UniversalBlurPass();
            _blurPass.renderPassEvent = injectionPoint;
        }

        /// <inheritdoc/>
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
            
            // Important to halt rendering here if camera is different, otherwise render textures will detect descriptor changes
            if (renderingData.cameraData.isPreviewCamera ||
                (renderingData.cameraData.isSceneViewCamera))
            {
                _blurPass.DrawDefaultTexture();
                
                return;
            }
            
            var passData = GetBlurConfig(renderingData);
            
            _blurPass.Setup(passData);
            
            renderer.EnqueuePass(_blurPass);
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            _blurPass?.Dispose();
            CoreUtils.Destroy(_material);
        }
    
        private bool TrySetShadersAndMaterials()
        {
            if (shader == null) 
                shader = Shader.Find("Unify/Internal/Blur");
            
            if (_material == null && shader != null)
                _material = CoreUtils.CreateEngineMaterial(shader);
            
            return _material != null;
        }
        
        private BlurConfig GetBlurConfig(in RenderingData renderingData)
        {
            var (width, height) = GetTargetResolution(renderingData);
            
            return new BlurConfig
            {
                Scale = CalculateScale(),
                
                Width = width,
                Height = height,
                
                Material = _material,
                Intensity = intensity,
                Downsample = downsample,
                Offset = offset,
                BlurType = blurType,
                Iterations = iterations,
            };
        }

        private (int width, int height) GetTargetResolution(in RenderingData renderingData)
        {
            RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;
            
            var width =
                Mathf.RoundToInt(descriptor.width / downsample);
            var height =
                Mathf.RoundToInt(descriptor.height / downsample);

            return (width, height);
        }
        
        private float CalculateScale() => scaleBlurWith switch
        {
            ScaleBlurWith.ScreenHeight => scale * (Screen.height / scaleReferenceSize) * _renderScale,
            ScaleBlurWith.ScreenWidth => scale * (Screen.width / scaleReferenceSize) * _renderScale,
            _ => scale
        };
    }
}