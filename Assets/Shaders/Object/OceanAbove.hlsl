#ifndef OCEAN_ABOVE_HLSL
#define OCEAN_ABOVE_HLSL

// Looking down at the surface from the air: fresnel blend between the refracted
// scene below and the reflected sky, plus a sun specular.
// Kept for tanks / aquariums and anything viewed from outside the water.
//
// Returns a premultiplied colour for "Blend One OneMinusSrcAlpha".

half4 ShadeAboveWater(OceanSurfaceData surface)
{
    float depthDifference = surface.sceneDepth - surface.surfaceDepth;
    float depthTint = saturate(depthDifference / _DepthFade);

    // ------------------------------------------------------------- refraction
    float3 waterColor = EvaluateWaterColor(_ShallowColor.rgb, _DeepColor.rgb, depthDifference, _DepthFade);

    float3 refraction = SampleRefraction(
        surface.screenUV,
        surface.normalUpWS,
        _RefractionStrength,
        surface.sceneDepth,
        surface.surfaceDepth,
        _DepthFade);

    // Tint the refracted scene with the water body it travelled through.
    refraction = lerp(refraction, waterColor, depthTint * 0.7);

    // ------------------------------------------------------------- reflection
    // Planar RT where one is supplied, sky probe otherwise; _ReflectionStrength
    // is the blend between them rather than a brightness multiplier.
    float3 reflDir = reflect(-surface.viewDirWS, surface.normalUpWS);
    float3 planar = SampleReflection(
        TEXTURE2D_ARGS(_ReflectionTex, sampler_ReflectionTex),
        surface.screenUV,
        surface.normalUpWS,
        0.03);
    float3 sky = lerp(_AboveWaterColor.rgb, SampleSkyProbe(reflDir), _SkyProbeStrength);
    float3 reflection = lerp(sky, planar, _ReflectionStrength);

    float fresnel = WaterFresnel(surface.viewDirWS, surface.normalUpWS, _FresnelPower, _FresnelBias);
    float3 color = lerp(refraction, reflection, fresnel);

    Light mainLight = GetMainLight();
    float3 emissive = CalculateSpecular(
        surface.normalUpWS,
        surface.viewDirWS,
        mainLight.direction,
        mainLight.color,
        _SpecularPower,
        _SpecularIntensity);

    // Foam is opaque, so it sits on top and nothing transmits through it.
    color = lerp(color, _FoamColor.rgb, surface.foam);
    emissive *= 1.0 - surface.foam;

    color = MixFog(color, surface.fogFactor);

    float depthAlpha = lerp(_ShallowColor.a, _DeepColor.a, depthTint);
    float alpha = lerp(depthAlpha, 1.0, fresnel * 0.5);
    alpha = lerp(alpha, 1.0, surface.foam);

    // Premultiplied so the specular keeps full strength no matter what alpha is.
    return half4(color * alpha + emissive, alpha);
}

#endif // OCEAN_ABOVE_HLSL
