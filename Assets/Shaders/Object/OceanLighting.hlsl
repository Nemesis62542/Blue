#ifndef OCEAN_LIGHTING_HLSL
#define OCEAN_LIGHTING_HLSL

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

// ============================================================================
// Constants
// ============================================================================

// Water's index of refraction
#define WATER_IOR 1.333
#define AIR_IOR 1.0

// Critical angle for total internal reflection (water to air)
// sin(critical_angle) = n_air / n_water = 1.0 / 1.333
// critical_angle = arcsin(0.75) ≈ 48.6 degrees ≈ 0.8481 radians
#define CRITICAL_ANGLE_RAD 0.8481

// ============================================================================
// Snell's Window Result
// ============================================================================
struct SnellsWindowResult
{
    float windowMask;           // 1.0 inside window, 0.0 outside (total reflection)
    float edgeBlend;            // Smooth blend at the edge
    float3 refractedDir;        // Refracted ray direction
    bool isTotalReflection;     // True if total internal reflection
};

// ============================================================================
// Fresnel Calculation (Schlick Approximation)
// ============================================================================
float SchlickFresnel(float cosTheta, float F0)
{
    return F0 + (1.0 - F0) * pow(1.0 - saturate(cosTheta), 5.0);
}

// Water-specific fresnel with adjustable power
float WaterFresnel(float3 viewDir, float3 normal, float fresnelPower, float fresnelBias)
{
    float NdotV = saturate(dot(normal, viewDir));

    // Water's F0 = ((n1 - n2) / (n1 + n2))^2 ≈ 0.02
    float F0 = fresnelBias;

    // Custom power for artistic control
    float fresnel = F0 + (1.0 - F0) * pow(1.0 - NdotV, fresnelPower);

    return saturate(fresnel);
}

// ============================================================================
// Refraction Vector Calculation
// ============================================================================
float3 RefractVector(float3 incidentDir, float3 normal, float eta)
{
    float NdotI = dot(normal, incidentDir);
    float k = 1.0 - eta * eta * (1.0 - NdotI * NdotI);

    if (k < 0.0)
    {
        // Total internal reflection
        return float3(0, 0, 0);
    }

    return eta * incidentDir - (eta * NdotI + sqrt(k)) * normal;
}

// ============================================================================
// Snell's Window Evaluation (for underwater view)
// ============================================================================
SnellsWindowResult EvaluateSnellsWindow(
    float3 viewDir,
    float3 normal,
    float edgeSoftness)
{
    SnellsWindowResult result;

    // Calculate angle between view direction and surface normal
    float cosTheta = saturate(dot(viewDir, normal));
    float theta = acos(cosTheta);

    // Compare with critical angle
    float criticalAngle = CRITICAL_ANGLE_RAD;

    // Check if we're outside the critical angle (total reflection)
    result.isTotalReflection = (theta > criticalAngle);

    // Calculate soft edge blend
    float edgeDistance = (criticalAngle - theta) / max(edgeSoftness, 0.001);
    result.windowMask = saturate(edgeDistance);
    result.edgeBlend = smoothstep(0.0, 1.0, result.windowMask);

    // Calculate refracted direction
    if (!result.isTotalReflection)
    {
        // Water to air refraction (eta = n_water / n_air)
        float eta = WATER_IOR / AIR_IOR;
        result.refractedDir = RefractVector(-viewDir, normal, eta);
    }
    else
    {
        // Total internal reflection
        result.refractedDir = reflect(-viewDir, normal);
    }

    return result;
}

// ============================================================================
// Sample Refraction from Opaque Texture
// ============================================================================
float3 SampleRefraction(
    float2 screenUV,
    float3 normal,
    float refractionStrength,
    float sceneDepth,
    float surfaceDepth,
    float depthFade)
{
    // Calculate UV offset based on normal
    float2 uvOffset = normal.xz * refractionStrength;

    // Scale offset by depth difference (weaker refraction in shallow water)
    float depthDifference = saturate((sceneDepth - surfaceDepth) / depthFade);
    uvOffset *= depthDifference;

    // Apply offset to screen UV
    float2 refractionUV = screenUV + uvOffset;

    // Clamp to prevent sampling outside screen
    refractionUV = clamp(refractionUV, 0.001, 0.999);

    // Sample scene color
    return SampleSceneColor(refractionUV);
}

// ============================================================================
// Sample Reflection Texture
// ============================================================================
float3 SampleReflection(
    TEXTURE2D_PARAM(reflectionTex, samplerReflection),
    float2 screenUV,
    float3 normal,
    float reflectionStrength)
{
    // Flip Y for reflection UV
    float2 reflectionUV = float2(screenUV.x, 1.0 - screenUV.y);

    // Add normal-based distortion
    reflectionUV += normal.xz * 0.03;

    // Clamp UV
    reflectionUV = clamp(reflectionUV, 0.001, 0.999);

    // Sample reflection texture
    float3 reflection = SAMPLE_TEXTURE2D(reflectionTex, samplerReflection, reflectionUV).rgb;

    return reflection * reflectionStrength;
}

// ============================================================================
// Calculate Depth-based Water Color
// ============================================================================
float3 EvaluateWaterColor(
    float3 shallowColor,
    float3 deepColor,
    float depth,
    float depthFade)
{
    float depthFactor = saturate(depth / depthFade);
    return lerp(shallowColor, deepColor, depthFactor);
}

// ============================================================================
// Specular Highlight Calculation
// ============================================================================
float3 CalculateSpecular(
    float3 normal,
    float3 viewDir,
    float3 lightDir,
    float3 lightColor,
    float specularPower,
    float specularIntensity)
{
    float3 halfDir = normalize(lightDir + viewDir);
    float NdotH = saturate(dot(normal, halfDir));
    float specular = pow(NdotH, specularPower) * specularIntensity;
    return specular * lightColor;
}

// ============================================================================
// Sample and Blend Normal Maps
// ============================================================================
float3 SampleBlendedNormals(
    TEXTURE2D_PARAM(normalMap1, sampler1),
    TEXTURE2D_PARAM(normalMap2, sampler2),
    float2 uv,
    float4 normalMap1_ST,
    float4 normalMap2_ST,
    float4 normalSpeed1,
    float4 normalSpeed2,
    float normalScale,
    float time)
{
    // Calculate scrolling UVs
    float2 uv1 = uv * normalMap1_ST.xy + normalMap1_ST.zw + normalSpeed1.xy * time;
    float2 uv2 = uv * normalMap2_ST.xy + normalMap2_ST.zw + normalSpeed2.xy * time;

    // Sample normal maps
    float3 normal1 = UnpackNormalScale(SAMPLE_TEXTURE2D(normalMap1, sampler1, uv1), normalScale);
    float3 normal2 = UnpackNormalScale(SAMPLE_TEXTURE2D(normalMap2, sampler2, uv2), normalScale * 0.5);

    // Blend normals using partial derivative blending
    float3 blendedNormal = normalize(float3(
        normal1.xy + normal2.xy,
        normal1.z * normal2.z
    ));

    return blendedNormal;
}

// ============================================================================
// Transform Normal from Tangent to World Space
// ============================================================================
float3 TransformNormalToWorld(float3 normalTS, float3 tangentWS, float3 bitangentWS, float3 normalWS)
{
    float3x3 TBN = float3x3(tangentWS, bitangentWS, normalWS);
    return normalize(mul(normalTS, TBN));
}

#endif // OCEAN_LIGHTING_HLSL
