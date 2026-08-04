#ifndef OCEAN_SURFACE_INPUT_HLSL
#define OCEAN_SURFACE_INPUT_HLSL

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

// ============================================================================
// Vertex Attributes
// ============================================================================
struct Attributes
{
    float4 positionOS : POSITION;
    float3 normalOS : NORMAL;
    float4 tangentOS : TANGENT;
    float2 uv : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

// ============================================================================
// Varyings
// ============================================================================
// The fragment shader re-evaluates the wave analytically from flatPositionWS, so
// the normal, the displaced position and the tangent frame are all derived there
// rather than interpolated. That keeps the shading identical no matter how
// coarsely the patch was tessellated, and cuts this down to three interpolators.
struct Varyings
{
    float4 positionCS : SV_POSITION;
    float4 screenPos : TEXCOORD0;
    float3 flatPositionWS : TEXCOORD1;      // before displacement
    float fogFactor : TEXCOORD2;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

// ============================================================================
// Everything the two shading paths need, gathered once in the fragment shader.
// normalUpWS is the perturbed surface normal and always points up out of the
// water, whichever side is being rendered.
// ============================================================================
struct OceanSurfaceData
{
    float3 positionWS;
    float3 normalUpWS;
    // Same normal with most of the normal map taken back out. The 32px normal can
    // tilt the surface by tens of degrees, which is fine for shading detail but
    // shreds anything angle-thresholded: driving Snell's window off normalUpWS
    // punches holes in the window at the zenith and leaks sun through the mirror.
    float3 macroNormalUpWS;
    float3 viewDirWS;       // surface -> camera, normalized
    float2 screenUV;
    float sceneDepth;       // linear eye depth of whatever is behind the surface
    float surfaceDepth;     // linear eye depth of the surface itself
    float distToCamera;
    float fogFactor;
    float foam;             // 1 where the crest has broken; opaque, so it overrides everything
};

// ============================================================================
// Material Properties (CBUFFER)
// Every non-texture property declared in the Properties block must appear here
// or the shader drops out of SRP Batcher compatibility.
// ============================================================================
CBUFFER_START(UnityPerMaterial)
    // World scale
    float _WorldScale;
    float _NormalTiling;

    // Wave spectrum (six components generated from these)
    float _WaveAmplitude;
    float _WaveFrequency;
    float _WaveSpeed;
    float _WaveSteepness;
    float4 _WaveDirection;
    float _WaveSpread;

    // Wave groups
    float _WaveGroupAmount;
    float _WaveGroupScale;

    // Water appearance
    float4 _ShallowColor;
    float4 _DeepColor;
    float _DepthFade;

    // Refraction
    float _RefractionStrength;
    float _IOR;

    // Reflection
    float _ReflectionStrength;

    // Fresnel
    float _FresnelPower;
    float _FresnelBias;

    // Normal maps
    float4 _NormalMap1_ST;
    float4 _NormalMap2_ST;
    float _NormalScale;
    float4 _NormalSpeed1;
    float4 _NormalSpeed2;
    float _NormalFadeDistance;

    // Specular
    float _SpecularPower;
    float _SpecularIntensity;

    // Foam
    float4 _FoamColor;
    float _FoamHeight;
    float _FoamSoftness;
    float _FoamSteepness;
    float _FoamWebScale;
    float _FoamWebWidth;
    float _FoamStreak;
    float _FoamFadeDistance;

    // Depth environment
    float _EnvironmentResponse;
    float _EnvAmbientReference;

    // Style
    float _PixelSize;

    // Underwater / Snell's window
    float _WindowFalloff;
    float _WindowNormalInfluence;
    float _WindowFlattenDistance;
    float4 _AboveWaterColor;
    float _SkyProbeStrength;
    float _SunIntensity;
    float _SunSharpness;
    float _SunHalo;
    float4 _MirrorColor;
    float4 _MirrorDeepColor;
    float4 _UnderwaterFogColor;
    float _UnderwaterDensity;

    // Underwater detection (driven from WaterReflection.cs)
    float _WaterSurfaceY;

    // Rendering
    float _ViewMode;
    float _CullMode;

    // Tessellation
    float _TessellationFactor;
    float _TessellationMinDistance;
    float _TessellationMaxDistance;
CBUFFER_END

// ============================================================================
// Textures and Samplers
// ============================================================================
TEXTURE2D(_NormalMap1);
TEXTURE2D(_NormalMap2);
TEXTURE2D(_ReflectionTex);
SAMPLER(sampler_NormalMap1);
SAMPLER(sampler_NormalMap2);
SAMPLER(sampler_ReflectionTex);

// ============================================================================
// Helper Functions
// ============================================================================

// Only consulted by the _VIEWMODE_BOTH variant; the dedicated variants know
// which side they are rendering at compile time.
bool IsCameraUnderwater()
{
    return _WorldSpaceCameraPos.y < _WaterSurfaceY;
}

// The wave field is three sines whose frequencies are usually left in small
// integer ratios, which makes the sum strictly periodic. Thresholding a strictly
// periodic function - as the foam does - yields an exact lattice of identical
// islands, so the threshold gets jittered by this aperiodic field instead.
float OceanHash(float2 p)
{
    p = frac(p * float2(127.1, 311.7));
    p += dot(p, p.yx + 19.19);
    return frac(p.x * p.y);
}

float OceanValueNoise(float2 p)
{
    float2 i = floor(p);
    float2 f = frac(p);
    f = f * f * (3.0 - 2.0 * f);
    return lerp(lerp(OceanHash(i),                OceanHash(i + float2(1, 0)), f.x),
                lerp(OceanHash(i + float2(0, 1)), OceanHash(i + float2(1, 1)), f.x), f.y);
}

// Cellular web: distance to the border between the two nearest Voronoi cells.
// Thresholded blob noise can only ever produce dots; foam that reads as lace
// needs a pattern that is *made of* connected filaments, and cell borders are
// exactly that - closed loops around every cell, meeting in a web.
float2 OceanHash2(float2 p)
{
    return float2(OceanHash(p), OceanHash(p + 47.13));
}

float OceanFoamWeb(float2 uv, float webWidth)
{
    float2 baseCell = floor(uv);
    float2 f = frac(uv);

    float f1 = 8.0, f2 = 8.0;
    [unroll] for (int y = -1; y <= 1; y++)
    [unroll] for (int x = -1; x <= 1; x++)
    {
        float2 offset = float2(x, y);
        float2 toFeature = offset + OceanHash2(baseCell + offset) - f;
        float d = dot(toFeature, toFeature);
        if (d < f1) { f2 = f1; f1 = d; }
        else f2 = min(f2, d);
    }

    // F2-F1 is 0 exactly on the border between two cells, so this is 1 on the
    // filaments and falls to 0 inside the cells.
    float edge = sqrt(f2) - sqrt(f1);
    return 1.0 - smoothstep(0.0, max(webWidth, 1e-3), edge);
}

#endif // OCEAN_SURFACE_INPUT_HLSL
