#ifndef OCEAN_LIGHTING_HLSL
#define OCEAN_LIGHTING_HLSL

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

// ============================================================================
// Constants
// ============================================================================
#define WATER_IOR 1.333
#define AIR_IOR 1.0

// ============================================================================
// Fresnel Calculation (Schlick Approximation)
// ============================================================================
float WaterFresnel(float3 viewDir, float3 normal, float fresnelPower, float fresnelBias)
{
    float NdotV = saturate(dot(normal, viewDir));

    // Water's F0 = ((n1 - n2) / (n1 + n2))^2 ~= 0.02, exposed as fresnelBias
    float F0 = fresnelBias;

    // Custom power for artistic control
    return saturate(F0 + (1.0 - F0) * pow(1.0 - NdotV, fresnelPower));
}

// ============================================================================
// Snell's Window
// Looking up from below, everything above the surface is squeezed into a cone
// of half-angle asin(n_air / n_water) ~= 48.6 deg. Outside that cone the
// surface turns into a mirror (total internal reflection).
// Working in cosine space keeps it branch-free and avoids an acos.
// ============================================================================
struct WaterAirRefraction
{
    float3 direction;       // refracted ray in air; grazes the horizon past the critical angle
    float reflectance;      // Fresnel; 0.02 straight up, exactly 1 at and beyond the critical angle
};

// Refract a ray travelling upwards through the surface into the air above.
// incidentDir points from the eye towards the surface (i.e. upwards),
// normalUp is the surface normal pointing up out of the water.
//
// Built by hand rather than with the refract() intrinsic: refract() returns 0
// past the critical angle, and snapping the direction back to the zenith is
// visible wherever the window is still partly open. Saturating sin(theta_t)
// instead makes the ray settle on the horizon, which is also what it physically
// does as the window closes.
//
// The reflectance falls out of the same numbers, so Snell's window needs no
// hand-authored edge blend: 1 - reflectance IS the window. Schlick is evaluated
// on the angle in air, which is the correct form going from dense to rare - it
// reaches 1 exactly at the critical angle instead of asymptoting.
WaterAirRefraction RefractIntoAir(float3 incidentDir, float3 normalUp, float ior, float falloff)
{
    float cosI = saturate(dot(incidentDir, normalUp));
    float sinI = sqrt(saturate(1.0 - cosI * cosI));

    // Snell: n_water * sin(theta_i) = n_air * sin(theta_t)
    float sinT = saturate(ior * sinI / AIR_IOR);
    float cosT = sqrt(saturate(1.0 - sinT * sinT));

    // Tangential component of the incident ray, i.e. the plane of incidence.
    float3 tangentDir = incidentDir - normalUp * cosI;
    float tangentLen = length(tangentDir);
    tangentDir = tangentLen > 1e-5 ? tangentDir / tangentLen : float3(0, 0, 0);

    float f0 = (ior - AIR_IOR) / (ior + AIR_IOR);
    f0 *= f0;

    WaterAirRefraction result;
    result.direction = normalize(normalUp * cosT + tangentDir * sinT);
    result.reflectance = saturate(f0 + (1.0 - f0) * pow(1.0 - cosT, falloff));
    return result;
}

// ============================================================================
// Environment
// ============================================================================
// DepthEnvironmentController drives RenderSettings ambient / fog colour /
// reflection intensity from the player's depth. The ocean used to take its whole
// palette from material constants, so the surface stayed lit for the shallows no
// matter how far down you were. Reading the ambient probe gives one scalar to
// drive the underwater palette with.
//
// SampleSHPixel with a zero L2 term falls through to the fully-per-pixel branch
// when no EVALUATE_SH_* keyword is set, which is what we want here - and unlike
// unity_AmbientSky it is correct whatever ambient mode the scene uses.
//
// Normalised against the ambient the material colours were authored at, so the
// look in the shallows is unchanged and only the descent darkens it.
float EnvironmentLevel(float authoredAmbient, float response)
{
    float3 ambient = SampleSHPixel(half3(0, 0, 0), half3(0, 1, 0));
    float luma = dot(ambient, float3(0.2126, 0.7152, 0.0722));
    return lerp(1.0, saturate(luma / max(authoredAmbient, 1e-4)), response);
}

float3 SampleSkyProbe(float3 direction)
{
    half4 encoded = SAMPLE_TEXTURECUBE_LOD(unity_SpecCube0, samplerunity_SpecCube0, direction, 0);
    return DecodeHDREnvironment(encoded, unity_SpecCube0_HDR);
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

    // Clamp to prevent sampling outside screen
    float2 refractionUV = clamp(screenUV + uvOffset, 0.001, 0.999);

    return SampleSceneColor(refractionUV);
}

// ============================================================================
// Sample Planar Reflection Texture
// Returns the raw reflection; callers apply _ReflectionStrength themselves so
// the property stays a blend weight rather than a brightness multiplier.
// ============================================================================
float3 SampleReflection(
    TEXTURE2D_PARAM(reflectionTex, samplerReflection),
    float2 screenUV,
    float3 normal,
    float distortion)
{
    // Flip Y for reflection UV
    float2 reflectionUV = float2(screenUV.x, 1.0 - screenUV.y);

    // Add normal-based distortion
    reflectionUV += normal.xz * distortion;

    reflectionUV = clamp(reflectionUV, 0.001, 0.999);

    return SAMPLE_TEXTURE2D(reflectionTex, samplerReflection, reflectionUV).rgb;
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
    return lerp(shallowColor, deepColor, saturate(depth / depthFade));
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
    return pow(NdotH, specularPower) * specularIntensity * lightColor;
}

// ============================================================================
// Sample and Blend Normal Maps
// ============================================================================
// duvdx / duvdy must come from the *unsnapped* UV. Once the UV is quantised to
// the pixel grid its screen derivatives spike at every cell boundary, the
// hardware picks a near-top mip there and the blocks smear away.
float3 SampleBlendedNormals(
    TEXTURE2D_PARAM(normalMap1, sampler1),
    TEXTURE2D_PARAM(normalMap2, sampler2),
    float2 uv,
    float2 duvdx,
    float2 duvdy,
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
    float3 normal1 = UnpackNormalScale(
        SAMPLE_TEXTURE2D_GRAD(normalMap1, sampler1, uv1, duvdx * normalMap1_ST.xy, duvdy * normalMap1_ST.xy), normalScale);
    float3 normal2 = UnpackNormalScale(
        SAMPLE_TEXTURE2D_GRAD(normalMap2, sampler2, uv2, duvdx * normalMap2_ST.xy, duvdy * normalMap2_ST.xy), normalScale * 0.5);

    // Whiteout blend
    return normalize(float3(
        normal1.xy + normal2.xy,
        normal1.z * normal2.z
    ));
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
