#ifndef OCEAN_UNDERWATER_HLSL
#define OCEAN_UNDERWATER_HLSL

// Looking up at the surface from inside the water.
//
// Two regions, split by the critical angle (~48.6 deg from vertical for water):
//   inside  Snell's window -> the entire hemisphere above is squeezed into a
//                             ~97 deg cone, sun included
//   outside Snell's window -> total internal reflection, the surface becomes a
//                             mirror showing the underwater scene
//
// Returns a premultiplied colour for "Blend One OneMinusSrcAlpha".

half4 ShadeUnderwater(OceanSurfaceData surface)
{
    // Ray from the eye towards the surface, i.e. pointing upwards.
    float3 incidentDir = -surface.viewDirWS;

    // One scalar and one colour, both driven by DepthEnvironmentController, so the
    // surface descends with the rest of the scene instead of staying at whatever
    // brightness the material was authored for. The material colours keep control
    // of hue; the environment supplies the level.
    float envLevel = EnvironmentLevel(_EnvAmbientReference, _EnvironmentResponse);
    float3 envFogColor = lerp(_UnderwaterFogColor.rgb, unity_FogColor.rgb, _EnvironmentResponse);

    // Everything angle-dependent runs off the macro normal so the window keeps a
    // readable shape and the sun stays a single blob; the fine normal only wobbles
    // the edge via _WindowNormalInfluence.
    WaterAirRefraction refraction = RefractIntoAir(
        incidentDir, surface.macroNormalUpWS, _IOR, _WindowFalloff);

    // Transmittance is the window. It sits near 0.98 out to ~40 deg and then
    // collapses over the last few degrees to 0 at the critical angle, which is
    // the flat bright disc with a hard darkening ring that real Snell's windows
    // have. An authored smoothstep cannot produce that shape.
    float window = 1.0 - refraction.reflectance;

    // ------------------------------------------------------------------ window
    // The probe already tracks depth through RenderSettings.reflectionIntensity
    // and the skybox tint, so only the constant fallback needs dimming.
    float3 refrDir = refraction.direction;
    float3 aboveWater = lerp(_AboveWaterColor.rgb * envLevel, SampleSkyProbe(refrDir), _SkyProbeStrength);

    // ------------------------------------------------------------------ mirror
    // Full-detail normal here, not the macro one: the mirror shade is a smooth
    // ramp rather than an angular threshold, so the fine normal adds wave detail
    // without any of the aliasing that made the window need a stabilised normal.
    float3 reflDir = reflect(incidentDir, surface.normalUpWS);
    float3 planar = SampleReflection(
        TEXTURE2D_ARGS(_ReflectionTex, sampler_ReflectionTex),
        surface.screenUV,
        surface.normalUpWS,
        0.03);

    // A flat mirror colour hides the waves completely. Grading it by how far the
    // reflected ray tips downwards gives every wave facet its own shade, which is
    // what makes the swell readable outside the window - the per-cell normals mean
    // this lands as flat blocks rather than a smooth gradient.
    // What the mirror shows is the underwater scene, so its brightness is the
    // ambient light down here - the one part of this that is genuinely lit.
    float mirrorFacing = saturate(-reflDir.y);
    float3 mirrorBase = lerp(_MirrorColor.rgb, _MirrorDeepColor.rgb, mirrorFacing) * envLevel;
    float3 mirror = lerp(mirrorBase, planar, _ReflectionStrength);

    float3 color = lerp(mirror, aboveWater, window);

    // The sun through the window is the single strongest "I am looking up" cue.
    // A bare disc reads as a sticker; the real thing scatters into a wide glow,
    // so the core gets a much softer lobe layered underneath it.
    Light mainLight = GetMainLight();
    float sunAlign = saturate(dot(refrDir, mainLight.direction));
    float sunDisc = pow(sunAlign, _SunSharpness);
    float sunGlow = pow(sunAlign, max(_SunSharpness * 0.08, 1.0));
    float3 emissive = (sunDisc + sunGlow * _SunHalo) * mainLight.color * _SunIntensity * window;

    // Foam is opaque, so it sits on top and nothing transmits through it. It is
    // lit by the same ambient as everything else down here.
    color = lerp(color, _FoamColor.rgb * envLevel, surface.foam);
    emissive *= 1.0 - surface.foam;

    // Underwater visibility is short. Letting distance eat the surface is what
    // stops it reading as a crisp ceiling stretching to the horizon.
    float visibility = exp(-surface.distToCamera * _UnderwaterDensity);
    color = lerp(envFogColor, color, visibility);
    emissive *= visibility;

    // Mirror side still shows the water body behind it; the window replaces it.
    float alpha = lerp(_DeepColor.a, 1.0, window);
    alpha = lerp(alpha, 1.0, surface.foam);

    // Premultiplied so the sun keeps full strength no matter what alpha is.
    return half4(color * alpha + emissive, alpha);
}

#endif // OCEAN_UNDERWATER_HLSL
