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
struct Varyings
{
    float4 positionCS : SV_POSITION;
    float3 positionWS : TEXCOORD0;
    float3 normalWS : TEXCOORD1;
    float4 tangentWS : TEXCOORD2;
    float2 uv : TEXCOORD3;
    float4 screenPos : TEXCOORD4;
    float3 viewDirWS : TEXCOORD5;
    float fogFactor : TEXCOORD6;
    float3 positionOS : TEXCOORD7;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

// ============================================================================
// Material Properties (CBUFFER)
// ============================================================================
CBUFFER_START(UnityPerMaterial)
    // World scale
    float _WorldScale;
    float _NormalTiling;

    // Wave parameters - Primary wave
    float _WaveAmplitude;
    float _WaveFrequency;
    float _WaveSpeed;
    float _WaveSteepness;
    float4 _WaveDirection;

    // Wave parameters - Secondary wave
    float _Wave2Amplitude;
    float _Wave2Frequency;
    float _Wave2Speed;
    float4 _Wave2Direction;

    // Wave parameters - Tertiary wave
    float _Wave3Amplitude;
    float _Wave3Frequency;
    float _Wave3Speed;
    float4 _Wave3Direction;

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

    // Snell's window
    float _SnellEdgeSoftness;

    // Underwater detection
    float _WaterSurfaceY;

    // Specular
    float _SpecularPower;
    float _SpecularIntensity;
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

// Check if camera is underwater
bool IsCameraUnderwater()
{
    return _WorldSpaceCameraPos.y < _WaterSurfaceY;
}

// Get camera position
float3 GetCameraPosition()
{
    return _WorldSpaceCameraPos;
}

#endif // OCEAN_SURFACE_INPUT_HLSL
