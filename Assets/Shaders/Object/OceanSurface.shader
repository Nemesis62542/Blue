Shader "Custom/OceanSurface"
{
    Properties
    {
        [Header(View)]
        [KeywordEnum(Underwater, Above, Both)] _ViewMode ("View Mode", Float) = 0
        [Enum(Both,0,Front,1,Back,2)] _CullMode ("Cull Mode", Float) = 1

        [Header(World Scale)]
        _WorldScale ("World Scale", Float) = 0.1
        _NormalTiling ("Normal Map Tiling", Float) = 0.05

        [Header(Wave Spectrum)]
        // Six components are generated from these: directions fanned across
        // _WaveSpread around _WaveDirection, frequencies on a golden-ratio
        // series above _WaveFrequency. _WaveAmplitude is the TOTAL amplitude
        // (sum over all components), so _FoamHeight stays meaningful against it.
        _WaveAmplitude ("Total Amplitude", Float) = 1.0
        _WaveFrequency ("Base Frequency", Float) = 1.0
        _WaveSpeed ("Base Speed", Float) = 1.0
        _WaveSteepness ("Wave Steepness (Q)", Range(0, 1)) = 0.9
        _WaveDirection ("Wind Direction", Vector) = (1, 0, 0, 0)
        _WaveSpread ("Direction Spread (deg)", Range(0, 60)) = 35

        [Header(Wave Groups)]
        // Amplitude envelope a few wavelengths across. This is what breaks the
        // regular rows of interference peaks - and with them, the regular foam.
        _WaveGroupAmount ("Wave Group Strength", Range(0, 1)) = 0.6
        _WaveGroupScale ("Wave Group Scale (1/m)", Float) = 0.015

        [Header(Water Appearance)]
        _ShallowColor ("Shallow Color", Color) = (0.2, 0.5, 0.7, 0.8)
        _DeepColor ("Deep Color", Color) = (0.05, 0.15, 0.3, 0.95)
        _DepthFade ("Depth Fade Distance", Float) = 5.0

        [Header(Normal Maps)]
        [Normal] _NormalMap1 ("Normal Map 1", 2D) = "bump" {}
        [Normal] _NormalMap2 ("Normal Map 2", 2D) = "bump" {}
        _NormalScale ("Normal Scale", Range(0, 2)) = 1.0
        _NormalSpeed1 ("Normal Speed 1", Vector) = (0.02, 0.01, 0, 0)
        _NormalSpeed2 ("Normal Speed 2", Vector) = (-0.01, 0.02, 0, 0)
        _NormalFadeDistance ("Normal Fade Distance (0 = off)", Float) = 80.0

        [Header(Underwater   Snells Window)]
        _IOR ("Index of Refraction", Range(1.01, 2.0)) = 1.333
        // Schlick exponent for the water->air fresnel. 5 is the physical value and
        // gives the real window profile; lower fades the edge in earlier.
        _WindowFalloff ("Window Edge Falloff", Range(1, 8)) = 5.0
        _WindowNormalInfluence ("Window Normal Influence", Range(0, 1)) = 0.45
        // Distance at which the window stops following the waves and becomes the
        // clean circle of calm water. Without this the far window degenerates into
        // the wave interference lattice.
        _WindowFlattenDistance ("Window Flatten Distance (0 = off)", Float) = 50.0
        _AboveWaterColor ("Above Water Fallback Color", Color) = (0.55, 0.75, 0.95, 1)
        _SkyProbeStrength ("Sky Probe Strength", Range(0, 1)) = 1.0
        _SunIntensity ("Sun Intensity", Range(0, 20)) = 3.0
        _SunSharpness ("Sun Sharpness", Range(16, 4096)) = 512
        _SunHalo ("Sun Halo", Range(0, 1)) = 0.2
        _MirrorColor ("Total Reflection Color (horizon)", Color) = (0.14, 0.42, 0.68, 1)
        _MirrorDeepColor ("Total Reflection Color (down)", Color) = (0.04, 0.17, 0.34, 1)
        _UnderwaterFogColor ("Underwater Fog Color", Color) = (0.02, 0.25, 0.4, 1)
        _UnderwaterDensity ("Underwater Fog Density", Range(0, 0.3)) = 0.02

        [Header(Above Water)]
        _RefractionStrength ("Refraction Strength", Range(0, 1)) = 0.3
        _ReflectionTex ("Planar Reflection Texture", 2D) = "black" {}
        _ReflectionStrength ("Planar vs Sky Blend", Range(0, 1)) = 0.5
        _FresnelPower ("Fresnel Power", Range(1, 10)) = 5.0
        _FresnelBias ("Fresnel Bias (F0)", Range(0, 0.1)) = 0.02
        _SpecularPower ("Specular Power", Range(1, 256)) = 128.0
        _SpecularIntensity ("Specular Intensity", Range(0, 2)) = 0.5

        [Header(Foam)]
        _FoamColor ("Foam Color", Color) = (0.85, 0.93, 1.0, 1)
        // Crest height in world units, so it has to be retuned if the wave
        // amplitudes change. Total amplitude is the sum of the three waves.
        _FoamHeight ("Foam Crest Height", Float) = 0.3
        _FoamSoftness ("Foam Edge Softness", Float) = 0.15
        _FoamSteepness ("Foam Steepness Bias", Range(0, 8)) = 3.0
        // Density of the lace pattern (Voronoi cells per world unit, before streak).
        _FoamWebScale ("Foam Web Scale", Float) = 0.4
        // Thickness of the lace filaments, in web-cell units.
        _FoamWebWidth ("Foam Web Width", Range(0.05, 1)) = 0.35
        // Below 1 the pattern stretches along the primary wave direction, so the
        // lace reads as wind-driven streaks.
        _FoamStreak ("Foam Streak", Range(0.05, 1)) = 0.3
        // Scattering blurs fine detail long before it dims large shapes, so foam
        // has to wash out much sooner than the fog eats the big window gradient.
        _FoamFadeDistance ("Foam Fade Distance (0 = off)", Float) = 50.0

        [Header(Depth Environment)]
        // 0 keeps the old behaviour of taking the whole palette from this material.
        // 1 hands brightness and fog colour to whatever DepthEnvironmentController
        // has set for the current depth.
        _EnvironmentResponse ("Environment Response", Range(0, 1)) = 1.0
        // Ambient luminance the colours above were authored against. Raise or lower
        // it until the shallows look the way they did before enabling the response.
        _EnvAmbientReference ("Authored Ambient Level", Float) = 0.67

        [Header(Style)]
        // Size of one shading block in world units. This is where the pixel look
        // comes from - quantizing the position, not the output values.
        _PixelSize ("Pixel Size (world units, 0 = off)", Float) = 0.625

        [Header(Tessellation)]
        _TessellationFactor ("Tessellation Factor", Range(1, 64)) = 8
        _TessellationMinDistance ("Min Distance", Float) = 10
        _TessellationMaxDistance ("Max Distance", Float) = 100
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }
        LOD 300

        // ====================================================================
        // Forward Pass - Main Rendering
        // ====================================================================
        Pass
        {
            Name "OceanForward"
            Tags { "LightMode" = "UniversalForward" }

            // Premultiplied alpha: both shading paths return colour * alpha with
            // the sun / specular added on top, so highlights are not dimmed by it.
            Blend One OneMinusSrcAlpha
            ZWrite Off
            Cull [_CullMode]

            HLSLPROGRAM
            #pragma target 4.6
            #pragma vertex TessellationVert
            #pragma hull OceanHullShader
            #pragma domain OceanDomainShader
            #pragma fragment OceanFrag

            // Underwater / Above are compiled separately so neither pays for the
            // other. Both keeps the runtime VFACE branch for cases where the same
            // surface is visible from inside and outside at once (e.g. a tank the
            // player can swim into).
            #pragma shader_feature_local_fragment _VIEWMODE_UNDERWATER _VIEWMODE_ABOVE _VIEWMODE_BOTH

            #pragma multi_compile_fog

            #include "OceanSurfaceInput.hlsl"
            #include "GerstnerWaves.hlsl"
            #include "OceanLighting.hlsl"
            #include "OceanTessellation.hlsl"
            #include "OceanUnderwater.hlsl"
            #include "OceanAbove.hlsl"

            // ================================================================
            // Fragment Shader
            // ================================================================
            half4 OceanFrag(Varyings input, half facing : VFACE) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float3 flatWS = input.flatPositionWS;
                float3 cameraPosWS = GetCameraPositionWS();

                // Distance is taken from the unsnapped position: snapping it too
                // would make the fog and the normal fade step in blocks as well.
                float distToCamera = distance(flatWS, cameraPosWS);

                // ---- world-space pixel grid --------------------------------
                // Snapping the shading position to a grid is what makes the surface
                // read as blocks. Doing it here instead of posterising the output
                // is the whole point: quantising a smooth value only produces
                // contour banding, whereas quantising the position gives blocks
                // that are genuinely attached to the water in world space.
                float2 cellXZ = _PixelSize > 0.0
                    ? (floor(flatWS.xz / _PixelSize) + 0.5) * _PixelSize
                    : flatWS.xz;
                float3 cellFlatWS = float3(cellXZ.x, flatWS.y, cellXZ.y);

                // Analytic wave, evaluated per fragment at the cell centre: one
                // flat normal per block, and completely independent of the
                // tessellation level - which is what made the distant surface warp.
                GerstnerWaveOutput cellWave = EvaluateGerstnerWavesSimple(cellFlatWS, _Time.y);
                float3 cellPosWS = cellFlatWS + cellWave.displacement;
                float3 normalUpWS = normalize(cellWave.normal);

                // Snapping the position the view ray is measured against is what
                // pixelates Snell's window itself.
                float3 viewDirWS = normalize(cameraPosWS - cellPosWS);

                // The surface is a horizontal plane waved in world XZ and the normal
                // map is tiled by world XZ, so the tangent frame can be built from
                // the world axes. No mesh tangent, no handedness to get wrong.
                float3 tangentWS = normalize(float3(1, 0, 0) - normalUpWS * normalUpWS.x);
                float3 bitangentWS = cross(tangentWS, normalUpWS);

                // A point-filtered 32px normal aliases hard at grazing angles, and
                // the far surface should read as flat anyway.
                float normalFade = _NormalFadeDistance > 0.0
                    ? saturate(1.0 - distToCamera / _NormalFadeDistance)
                    : 1.0;

                // Snapped UV for the sample, unsnapped one for the derivatives.
                float2 smoothUV = flatWS.xz * _NormalTiling;
                float2 worldUV = cellXZ * _NormalTiling;

                float3 normalTS = SampleBlendedNormals(
                    TEXTURE2D_ARGS(_NormalMap1, sampler_NormalMap1),
                    TEXTURE2D_ARGS(_NormalMap2, sampler_NormalMap2),
                    worldUV,
                    ddx(smoothUV),
                    ddy(smoothUV),
                    _NormalMap1_ST,
                    _NormalMap2_ST,
                    _NormalSpeed1,
                    _NormalSpeed2,
                    _NormalScale,
                    _Time.y
                );
                normalTS = normalize(lerp(float3(0, 0, 1), normalTS, normalFade));

                // Second, much flatter copy for anything thresholded on angle.
                float3 macroNormalTS = normalize(lerp(float3(0, 0, 1), normalTS, _WindowNormalInfluence));
                float3 macroNormalWS = TransformNormalToWorld(macroNormalTS, tangentWS, bitangentWS, normalUpWS);

                // Near the critical angle the boundary is where cos(theta) crosses a
                // constant. Looking straight up, theta changes quickly with position
                // so the edge is well defined; towards the horizon theta flattens out
                // against 90 deg while the wave tilt stays the same size, so a couple
                // of degrees of tilt slides the boundary by tens of metres. That is
                // what replaced the far window with a lattice of the wave interference
                // pattern. Settling the macro normal back onto true up with distance
                // makes the window converge on the clean circle of calm water.
                float windowFlatten = _WindowFlattenDistance > 0.0
                    ? saturate(distToCamera / _WindowFlattenDistance)
                    : 0.0;
                macroNormalWS = normalize(lerp(macroNormalWS, float3(0, 1, 0), windowFlatten));

                OceanSurfaceData surface;
                surface.positionWS = cellPosWS;
                surface.normalUpWS = TransformNormalToWorld(normalTS, tangentWS, bitangentWS, normalUpWS);
                surface.macroNormalUpWS = macroNormalWS;
                surface.viewDirWS = viewDirWS;
                surface.screenUV = input.screenPos.xy / input.screenPos.w;
                surface.sceneDepth = LinearEyeDepth(SampleSceneDepth(surface.screenUV), _ZBufferParams);
                surface.surfaceDepth = LinearEyeDepth(input.positionCS.z, _ZBufferParams);
                surface.distToCamera = distToCamera;
                surface.fogFactor = input.fogFactor;

                // Foam rides the crests. From below it is the only genuinely opaque
                // part of the surface, so it reads without leaning on the palette -
                // every other cue here is blue against blue. The crest height is
                // already in hand from the per-fragment wave, and evaluating it at
                // the cell centre makes it come out blocky like everything else.
                // Steep faces break first, so they bias into foam earlier.
                float crest = saturate((cellWave.displacement.y - _FoamHeight) / max(_FoamSoftness, 1e-4));
                float steep = saturate((1.0 - dot(surface.normalUpWS, float3(0, 1, 0))) * _FoamSteepness);

                // Frame aligned to the primary wave, squashed along it so every
                // foam feature comes out stretched with the wind. Built from the
                // SNAPPED cell position like every other foam input - fed with the
                // smooth position instead, the lace comes out as thin sub-cell
                // slivers with smooth edges cutting across the pixel grid.
                float2 waveDir = normalize(_WaveDirection.xz + float2(1e-6, 0));
                float2 waveSide = float2(-waveDir.y, waveDir.x);
                float2 alignedUV = float2(dot(cellXZ, waveDir) * _FoamStreak,
                                          dot(cellXZ, waveSide));

                // How foamy this cell wants to be. No masking noise on top: with a
                // short-crested spectrum the peaks scatter irregularly on their own,
                // and the wave groups keep whole stretches of sea calm.
                float foamAmount = saturate(crest * (1.0 + steep));

                // foamAmount does not gate the foam directly - it decides how deep to
                // cut into a lace pattern. Weak crests keep only the brightest
                // filaments as scraps, mid strength shows the connected web, and only
                // a fully breaking crest floods the holes into solid white. Cutting a
                // threshold into blob noise - every previous attempt - can only yield
                // dots, because blob noise has no connected structure to reveal.
                float web = OceanFoamWeb(alignedUV * _FoamWebScale + float2(_Time.y * 0.06, 0), _FoamWebWidth);
                float cut = 1.2 - foamAmount * 1.45;
                surface.foam = smoothstep(cut - 0.15, cut + 0.15, web);

                // Scattering kills fine detail well before the fog kills the big
                // luminance shapes, so foam gets its own, much shorter fade. This is
                // also what keeps any residual crest regularity out of the far field.
                float foamFade = _FoamFadeDistance > 0.0
                    ? saturate(1.0 - distToCamera / _FoamFadeDistance)
                    : 1.0;
                surface.foam *= foamFade * foamFade;

                #if defined(_VIEWMODE_ABOVE)
                    return ShadeAboveWater(surface);
                #elif defined(_VIEWMODE_BOTH)
                    bool viewFromBelow = (facing < 0) || IsCameraUnderwater();
                    return viewFromBelow ? ShadeUnderwater(surface) : ShadeAboveWater(surface);
                #else
                    return ShadeUnderwater(surface);
                #endif
            }
            ENDHLSL
        }

        // ====================================================================
        // Depth Only Pass
        // ====================================================================
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask 0
            Cull [_CullMode]

            HLSLPROGRAM
            #pragma target 4.6
            #pragma vertex TessellationVert
            #pragma hull OceanHullShader
            #pragma domain DepthDomainShader
            #pragma fragment DepthOnlyFrag

            #include "OceanSurfaceInput.hlsl"
            #include "GerstnerWaves.hlsl"
            #include "OceanTessellation.hlsl"
            ENDHLSL
        }
    }

    FallBack Off
}
