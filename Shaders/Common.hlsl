// Based on SSAO.hlsl from Unity Universal RP

// Includes
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderVariablesFunctions.hlsl"

// Textures & Samplers
SAMPLER(sampler_BlitTexture);

// Function defines
#define SAMPLE(textureName, coord2) SAMPLE_TEXTURE2D_LOD(textureName, sampler_LinearClamp, coord2, _BlitMipLevel);

#define SAMPLE_BASEMAP(uv) half4(SAMPLE_TEXTURE2D_LOD(_BlitTexture, sampler_LinearClamp, UnityStereoTransformScreenSpaceTex(uv), _BlitMipLevel));

// Constants
static const half  HALF_POINT_ONE   = half(0.1);
static const half  HALF_MINUS_ONE   = half(-1.0);
static const half  HALF_ZERO        = half(0.0);
static const half  HALF_HALF        = half(0.5);
static const half  HALF_ONE         = half(1.0);
static const half4 HALF4_ONE        = half4(1.0, 1.0, 1.0, 1.0);
static const half  HALF_TWO         = half(2.0);
static const half  HALF_TWO_PI      = half(6.28318530717958647693);
static const half  HALF_FOUR        = half(4.0);
static const half  HALF_NINE        = half(9.0);
static const half  HALF_HUNDRED     = half(100.0);

// Params
int _Iteration;
half _BlurOffset;
half4 _BlurParams;

#if UNITY_VERSION <= 600000
half2 _BlitTexture_TexelSize;
#endif

// Settings
#define TEXEL_SIZE _BlitTexture_TexelSize
#define ITERATION _Iteration
#define INTENSITY _BlurParams.x
#define RADIUS _BlurParams.y
#define DOWNSAMPLE _BlurParams.z
#define OFFSET _BlurParams.w

// ------------------------------------------------------------------
// Gaussian Blur
// ------------------------------------------------------------------

half GaussianBlur(half2 uv, half2 pixelOffset)
{
    half colOut = 0;

    // Kernel width 7 x 7
    const int stepCount = 2;

    const half gWeights[stepCount] ={
        0.44908,
        0.05092
     };
    const half gOffsets[stepCount] ={
        0.53805,
        2.06278
     };

    UNITY_UNROLL
    for( int i = 0; i < stepCount; i++ )
    {
        half2 texCoordOffset = gOffsets[i] * pixelOffset;
        half4 p1 = SAMPLE_BASEMAP(uv + texCoordOffset);
        half4 p2 = SAMPLE_BASEMAP(uv - texCoordOffset);
        half col = p1.r + p2.r;
        colOut += gWeights[i] * col;
    }

    return colOut;
}

half4 HorizontalGaussianBlur(Varyings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    half2 uv = input.texcoord;
    half2 delta = half2(TEXEL_SIZE.y * rcp(DOWNSAMPLE), HALF_ZERO);

    half g = GaussianBlur(uv, delta);
    return half4(g, g, g, 1);
}

half4 VerticalGaussianBlur(Varyings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    half2 uv = input.texcoord;
    half2 delta = half2(HALF_ZERO, TEXEL_SIZE.x * rcp(DOWNSAMPLE));

    half g = HALF_ONE - GaussianBlur(uv, delta);
    return half4(g, g, g, 1);
}


// ------------------------------------------------------------------
// Kawase Blur - Custom
// ------------------------------------------------------------------

half4 KawaseBlurFilterCustom(Texture2D blurTexture, float2 uv, float offset, float2 texelSize)
{
    float i = offset;

    half4 col;

    col = SAMPLE(blurTexture, saturate(uv));
    col += SAMPLE(blurTexture, saturate(uv + float2(i, i) * texelSize));
    col += SAMPLE(blurTexture, saturate(uv + float2(i, -i) * texelSize));
    col += SAMPLE(blurTexture, saturate(uv + float2(-i, i) * texelSize));
    col += SAMPLE(blurTexture, saturate(uv + float2(-i, -i) * texelSize));
    col /= 5.0f;

    col.a = 1;

    return col;
}

half4 KawaseBlurCustom(Varyings input) : SV_Target
{
    // this is needed so we account XR platform differences in how they handle texture arrays
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    float2 uv = input.texcoord.xy;
                
    #ifndef UNITY_UV_STARTS_AT_TOP
    uv.y = 1.0 - uv.y;
    #endif

    half4 color = KawaseBlurFilterCustom(_BlitTexture, uv, OFFSET, TEXEL_SIZE.xy);
    return color;
}

///////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Developed by Masaki Kawase, Bunkasha Games
// Used in DOUBLE-S.T.E.A.L. (aka Wreckless)
// From his GDC2003 Presentation: Frame Buffer Postprocessing Effects in  DOUBLE-S.T.E.A.L (Wreckless)
///////////////////////////////////////////////////////////////////////////////////////////////////////////////
half4 KawaseBlurFilter( half2 texCoord, half2 pixelSize, half iteration )
{
    half2 texCoordSample;
    half2 halfPixelSize = pixelSize * HALF_HALF;
    half2 dUV = ( pixelSize.xy * half2( iteration, iteration ) ) + halfPixelSize.xy;

    half4 cOut;

    // Sample top left pixel
    texCoordSample.x = texCoord.x - dUV.x;
    texCoordSample.y = texCoord.y + dUV.y;

    cOut = SAMPLE_BASEMAP(texCoordSample);

    // Sample top right pixel
    texCoordSample.x = texCoord.x + dUV.x;
    texCoordSample.y = texCoord.y + dUV.y;

    cOut += SAMPLE_BASEMAP(texCoordSample);

    // Sample bottom right pixel
    texCoordSample.x = texCoord.x + dUV.x;
    texCoordSample.y = texCoord.y - dUV.y;
    cOut += SAMPLE_BASEMAP(texCoordSample);

    // Sample bottom left pixel
    texCoordSample.x = texCoord.x - dUV.x;
    texCoordSample.y = texCoord.y - dUV.y;

    cOut += SAMPLE_BASEMAP(texCoordSample);

    // Average
    cOut *= half(0.25);

    return cOut;
}

half4 KawaseBlur(Varyings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    half2 uv = input.texcoord;
    half2 texelSize = TEXEL_SIZE.xy * rcp(DOWNSAMPLE);

    half4 col = KawaseBlurFilter(uv, texelSize * INTENSITY, ITERATION * OFFSET);
    
    return col;
}