Shader "Custom/OceanSurface"
{
    Properties
    {
        [Header(World Scale)]
        _WorldScale ("World Scale", Float) = 0.1
        _NormalTiling ("Normal Map Tiling", Float) = 0.05

        [Header(Wave Settings Primary)]
        _WaveAmplitude ("Wave Amplitude", Float) = 0.5
        _WaveFrequency ("Wave Frequency", Float) = 1.0
        _WaveSpeed ("Wave Speed", Float) = 1.0
        _WaveSteepness ("Wave Steepness (Q)", Range(0, 1)) = 0.5
        _WaveDirection ("Wave Direction", Vector) = (1, 0, 0, 0)

        [Header(Wave Settings Secondary)]
        _Wave2Amplitude ("Wave 2 Amplitude", Float) = 0.3
        _Wave2Frequency ("Wave 2 Frequency", Float) = 1.5
        _Wave2Speed ("Wave 2 Speed", Float) = 0.8
        _Wave2Direction ("Wave 2 Direction", Vector) = (0.7, 0, 0.7, 0)

        [Header(Wave Settings Tertiary)]
        _Wave3Amplitude ("Wave 3 Amplitude", Float) = 0.15
        _Wave3Frequency ("Wave 3 Frequency", Float) = 2.5
        _Wave3Speed ("Wave 3 Speed", Float) = 1.2
        _Wave3Direction ("Wave 3 Direction", Vector) = (0.5, 0, -0.866, 0)

        [Header(Water Appearance)]
        _ShallowColor ("Shallow Color", Color) = (0.2, 0.5, 0.7, 0.8)
        _DeepColor ("Deep Color", Color) = (0.05, 0.15, 0.3, 0.95)
        _DepthFade ("Depth Fade Distance", Float) = 5.0

        [Header(Refraction)]
        _RefractionStrength ("Refraction Strength", Range(0, 1)) = 0.3
        _IOR ("Index of Refraction", Float) = 1.333

        [Header(Reflection)]
        _ReflectionTex ("Reflection Texture", 2D) = "black" {}
        _ReflectionStrength ("Reflection Strength", Range(0, 1)) = 0.5

        [Header(Fresnel)]
        _FresnelPower ("Fresnel Power", Range(1, 10)) = 5.0
        _FresnelBias ("Fresnel Bias (F0)", Range(0, 0.1)) = 0.02

        [Header(Normal Maps)]
        _NormalMap1 ("Normal Map 1", 2D) = "bump" {}
        _NormalMap2 ("Normal Map 2", 2D) = "bump" {}
        _NormalScale ("Normal Scale", Range(0, 2)) = 1.0
        _NormalSpeed1 ("Normal Speed 1", Vector) = (0.02, 0.01, 0, 0)
        _NormalSpeed2 ("Normal Speed 2", Vector) = (-0.01, 0.02, 0, 0)

        [Header(Specular)]
        _SpecularPower ("Specular Power", Range(1, 256)) = 128.0
        _SpecularIntensity ("Specular Intensity", Range(0, 2)) = 0.5

        [Header(Snells Window)]
        [Toggle(_SNELLS_WINDOW)] _EnableSnellsWindow ("Enable Snells Window", Float) = 0
        _SnellEdgeSoftness ("Edge Softness", Range(0.01, 0.3)) = 0.1

        [Header(Underwater Detection)]
        _WaterSurfaceY ("Water Surface Y Position", Float) = 0.0

        [Header(Rendering)]
        [Enum(Both,0,Front,1,Back,2)] _CullMode ("Cull Mode", Float) = 0

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

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull [_CullMode]

            HLSLPROGRAM
            #pragma target 4.6
            #pragma vertex TessellationVert
            #pragma hull OceanHullShader
            #pragma domain OceanDomainShader
            #pragma fragment OceanFrag

            // Shader features
            #pragma shader_feature_local _SNELLS_WINDOW

            // Multi-compile variants
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile_fog

            #include "OceanSurfaceInput.hlsl"
            #include "GerstnerWaves.hlsl"
            #include "OceanLighting.hlsl"
            #include "OceanTessellation.hlsl"

            // ================================================================
            // Fragment Shader
            // ================================================================
            half4 OceanFrag(Varyings input, half facing : VFACE) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                // Check if rendering back face (underwater view)
                bool isBackFace = facing < 0;

                // Normalize vectors
                float3 normalWS = normalize(input.normalWS);

                // Flip normal for back face rendering
                normalWS = isBackFace ? -normalWS : normalWS;

                float3 viewDirWS = normalize(input.viewDirWS);
                float3 tangentWS = normalize(input.tangentWS.xyz);
                float3 bitangentWS = cross(normalWS, tangentWS) * input.tangentWS.w;

                // Flip tangent space for back face
                if (isBackFace)
                {
                    tangentWS = -tangentWS;
                }

                // Sample and blend normal maps using world position
                float time = _Time.y;

                // Use world XZ coordinates for consistent tiling regardless of mesh scale
                // input.positionOS now contains world position (passed from vertex shader)
                float2 worldUV = input.positionOS.xz * _NormalTiling;

                float3 normalTS = SampleBlendedNormals(
                    TEXTURE2D_ARGS(_NormalMap1, sampler_NormalMap1),
                    TEXTURE2D_ARGS(_NormalMap2, sampler_NormalMap2),
                    worldUV,
                    _NormalMap1_ST,
                    _NormalMap2_ST,
                    _NormalSpeed1,
                    _NormalSpeed2,
                    _NormalScale,
                    time
                );

                // Transform normal to world space
                float3 perturbedNormalWS = TransformNormalToWorld(normalTS, tangentWS, bitangentWS, normalWS);

                // Calculate screen UV
                float2 screenUV = input.screenPos.xy / input.screenPos.w;

                // Sample scene depth
                float sceneDepthRaw = SampleSceneDepth(screenUV);
                float sceneDepth = LinearEyeDepth(sceneDepthRaw, _ZBufferParams);
                float surfaceDepth = LinearEyeDepth(input.positionCS.z, _ZBufferParams);
                float depthDifference = sceneDepth - surfaceDepth;

                // Check if camera is underwater
                // Back face rendering = underwater view
                bool isUnderwater = isBackFace || IsCameraUnderwater();

                // Initialize output
                float3 finalColor = float3(0, 0, 0);
                float finalAlpha = 1.0;

                if (isUnderwater)
                {
                    // ========================================================
                    // Underwater View
                    // ========================================================

                    // For underwater view, we need the upward-facing normal
                    // If back face, perturbedNormalWS points down, so we flip it
                    float3 surfaceNormalUp = isBackFace ? -perturbedNormalWS : perturbedNormalWS;

                    // View direction from camera to surface (for Snell's window calculation)
                    // viewDirWS points from surface to camera, so we negate it
                    float3 incidentDir = -viewDirWS;

                    #if defined(_SNELLS_WINDOW)
                    // Evaluate Snell's window
                    // We need the angle between the incident ray and the surface normal
                    float cosTheta = abs(dot(incidentDir, surfaceNormalUp));
                    float theta = acos(saturate(cosTheta));

                    // Critical angle for water-to-air (approximately 48.6 degrees)
                    float criticalAngle = 0.8481; // radians

                    // Check if outside critical angle (total internal reflection)
                    bool isTotalReflection = theta > criticalAngle;

                    // Smooth edge blend
                    float edgeDistance = (criticalAngle - theta) / max(_SnellEdgeSoftness, 0.01);
                    float windowBlend = saturate(smoothstep(0.0, 1.0, edgeDistance));

                    if (isTotalReflection)
                    {
                        // Total internal reflection - mirror-like surface
                        float3 reflectDir = reflect(incidentDir, surfaceNormalUp);

                        // Dark reflection of underwater environment
                        float3 reflectionColor = _DeepColor.rgb * 0.5;

                        // Slight color variation based on reflection angle
                        reflectionColor += saturate(-reflectDir.y) * _DeepColor.rgb * 0.3;

                        finalColor = reflectionColor;
                    }
                    else
                    {
                        // Inside Snell's window - can see above water (bright)
                        float3 refractionColor = SampleRefraction(
                            screenUV,
                            perturbedNormalWS,
                            _RefractionStrength * 0.3,
                            sceneDepth,
                            surfaceDepth,
                            _DepthFade
                        );

                        // Bright window color (sky/above water is visible)
                        float3 skyColor = float3(0.5, 0.7, 0.9);
                        float3 windowColor = lerp(refractionColor, skyColor, 0.4);

                        // Blend at the edge of Snell's window
                        float3 edgeColor = _DeepColor.rgb * 0.6;
                        finalColor = lerp(edgeColor, windowColor, windowBlend);
                    }
                    #else
                    // Simple underwater view without Snell's window
                    // Just show the water color with slight depth variation
                    float3 underwaterColor = lerp(_ShallowColor.rgb, _DeepColor.rgb, 0.5);

                    // Add slight brightness variation based on view angle
                    float upFactor = saturate(dot(-incidentDir, float3(0, 1, 0)));
                    underwaterColor += upFactor * 0.15;

                    finalColor = underwaterColor;
                    #endif

                    finalAlpha = _DeepColor.a;
                }
                else
                {
                    // ========================================================
                    // Above Water View
                    // ========================================================

                    // Calculate depth-based water color
                    float3 waterColor = EvaluateWaterColor(
                        _ShallowColor.rgb,
                        _DeepColor.rgb,
                        depthDifference,
                        _DepthFade
                    );

                    // Sample refraction (underwater scene)
                    float3 refractionColor = SampleRefraction(
                        screenUV,
                        perturbedNormalWS,
                        _RefractionStrength,
                        sceneDepth,
                        surfaceDepth,
                        _DepthFade
                    );

                    // Tint refraction with water color based on depth
                    float depthTint = saturate(depthDifference / _DepthFade);
                    refractionColor = lerp(refractionColor, waterColor, depthTint * 0.7);

                    // Sample reflection
                    float3 reflectionColor = SampleReflection(
                        TEXTURE2D_ARGS(_ReflectionTex, sampler_ReflectionTex),
                        screenUV,
                        perturbedNormalWS,
                        _ReflectionStrength
                    );

                    // Add sky color approximation if reflection is dark
                    float reflectionLuminance = dot(reflectionColor, float3(0.299, 0.587, 0.114));
                    if (reflectionLuminance < 0.1)
                    {
                        // Approximate sky reflection using view direction
                        float skyBlend = saturate(dot(reflect(-viewDirWS, perturbedNormalWS), float3(0, 1, 0)));
                        reflectionColor = lerp(_DeepColor.rgb, float3(0.6, 0.7, 0.9), skyBlend) * _ReflectionStrength;
                    }

                    // Calculate fresnel
                    float fresnel = WaterFresnel(viewDirWS, perturbedNormalWS, _FresnelPower, _FresnelBias);

                    // Blend reflection and refraction using fresnel
                    finalColor = lerp(refractionColor, reflectionColor, fresnel);

                    // Add specular highlight
                    Light mainLight = GetMainLight();
                    float3 specular = CalculateSpecular(
                        perturbedNormalWS,
                        viewDirWS,
                        mainLight.direction,
                        mainLight.color,
                        _SpecularPower,
                        _SpecularIntensity
                    );
                    finalColor += specular;

                    // Calculate alpha based on depth and fresnel
                    float depthAlpha = lerp(_ShallowColor.a, _DeepColor.a, depthTint);
                    finalAlpha = lerp(depthAlpha, 1.0, fresnel * 0.5);
                }

                // Apply fog
                finalColor = MixFog(finalColor, input.fogFactor);

                return half4(finalColor, finalAlpha);
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
            #pragma vertex DepthTessVert
            #pragma hull DepthHullShader
            #pragma domain DepthDomainShader
            #pragma fragment DepthOnlyFrag

            #include "OceanSurfaceInput.hlsl"
            #include "GerstnerWaves.hlsl"

            struct DepthOnlyVaryings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            struct DepthTessControlPoint
            {
                float4 positionOS : INTERNALTESSPOS;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct DepthTessFactors
            {
                float edge[3] : SV_TessFactor;
                float inside : SV_InsideTessFactor;
            };

            float CalcDepthEdgeTessFactor(float3 pos0WS, float3 pos1WS)
            {
                float3 edgeCenter = (pos0WS + pos1WS) * 0.5;
                float3 cameraPos = GetCameraPositionWS();
                float dist = distance(edgeCenter, cameraPos);
                float f = saturate((_TessellationMaxDistance - dist) / (_TessellationMaxDistance - _TessellationMinDistance));
                return lerp(1.0, _TessellationFactor, f);
            }

            DepthTessControlPoint DepthTessVert(Attributes input)
            {
                DepthTessControlPoint output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                output.positionOS = input.positionOS;
                return output;
            }

            DepthTessFactors DepthPatchConstant(InputPatch<DepthTessControlPoint, 3> patch)
            {
                UNITY_SETUP_INSTANCE_ID(patch[0]);
                DepthTessFactors f;
                float3 pos0WS = TransformObjectToWorld(patch[0].positionOS.xyz);
                float3 pos1WS = TransformObjectToWorld(patch[1].positionOS.xyz);
                float3 pos2WS = TransformObjectToWorld(patch[2].positionOS.xyz);
                f.edge[0] = CalcDepthEdgeTessFactor(pos1WS, pos2WS);
                f.edge[1] = CalcDepthEdgeTessFactor(pos2WS, pos0WS);
                f.edge[2] = CalcDepthEdgeTessFactor(pos0WS, pos1WS);
                f.inside = (f.edge[0] + f.edge[1] + f.edge[2]) / 3.0;
                return f;
            }

            [domain("tri")]
            [partitioning("fractional_odd")]
            [outputtopology("triangle_cw")]
            [outputcontrolpoints(3)]
            [patchconstantfunc("DepthPatchConstant")]
            [maxtessfactor(64)]
            DepthTessControlPoint DepthHullShader(InputPatch<DepthTessControlPoint, 3> patch, uint id : SV_OutputControlPointID)
            {
                return patch[id];
            }

            [domain("tri")]
            DepthOnlyVaryings DepthDomainShader(DepthTessFactors factors, OutputPatch<DepthTessControlPoint, 3> patch, float3 bary : SV_DomainLocation)
            {
                DepthOnlyVaryings output;
                UNITY_SETUP_INSTANCE_ID(patch[0]);
                UNITY_TRANSFER_INSTANCE_ID(patch[0], output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 positionOS = patch[0].positionOS.xyz * bary.x + patch[1].positionOS.xyz * bary.y + patch[2].positionOS.xyz * bary.z;
                float3 positionWS = TransformObjectToWorld(positionOS);
                GerstnerWaveOutput waveOutput = EvaluateGerstnerWavesSimple(positionWS, _Time.y);
                float3 displacedPositionWS = positionWS + waveOutput.displacement;
                output.positionCS = TransformWorldToHClip(displacedPositionWS);
                return output;
            }

            half4 DepthOnlyFrag(DepthOnlyVaryings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }

        // ====================================================================
        // Shadow Caster Pass (optional - water usually doesn't cast shadows)
        // ====================================================================
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull [_CullMode]

            HLSLPROGRAM
            #pragma target 4.6
            #pragma vertex ShadowTessVert
            #pragma hull ShadowHullShader
            #pragma domain ShadowDomainShader
            #pragma fragment ShadowPassFrag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            #include "OceanSurfaceInput.hlsl"
            #include "GerstnerWaves.hlsl"

            float3 _LightDirection;

            struct ShadowVaryings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            struct ShadowTessControlPoint
            {
                float4 positionOS : INTERNALTESSPOS;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct ShadowTessFactors
            {
                float edge[3] : SV_TessFactor;
                float inside : SV_InsideTessFactor;
            };

            float CalcShadowEdgeTessFactor(float3 pos0WS, float3 pos1WS)
            {
                float3 edgeCenter = (pos0WS + pos1WS) * 0.5;
                float3 cameraPos = GetCameraPositionWS();
                float dist = distance(edgeCenter, cameraPos);
                float f = saturate((_TessellationMaxDistance - dist) / (_TessellationMaxDistance - _TessellationMinDistance));
                return lerp(1.0, _TessellationFactor, f);
            }

            ShadowTessControlPoint ShadowTessVert(Attributes input)
            {
                ShadowTessControlPoint output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                output.positionOS = input.positionOS;
                output.normalOS = input.normalOS;
                return output;
            }

            ShadowTessFactors ShadowPatchConstant(InputPatch<ShadowTessControlPoint, 3> patch)
            {
                UNITY_SETUP_INSTANCE_ID(patch[0]);
                ShadowTessFactors f;
                float3 pos0WS = TransformObjectToWorld(patch[0].positionOS.xyz);
                float3 pos1WS = TransformObjectToWorld(patch[1].positionOS.xyz);
                float3 pos2WS = TransformObjectToWorld(patch[2].positionOS.xyz);
                f.edge[0] = CalcShadowEdgeTessFactor(pos1WS, pos2WS);
                f.edge[1] = CalcShadowEdgeTessFactor(pos2WS, pos0WS);
                f.edge[2] = CalcShadowEdgeTessFactor(pos0WS, pos1WS);
                f.inside = (f.edge[0] + f.edge[1] + f.edge[2]) / 3.0;
                return f;
            }

            [domain("tri")]
            [partitioning("fractional_odd")]
            [outputtopology("triangle_cw")]
            [outputcontrolpoints(3)]
            [patchconstantfunc("ShadowPatchConstant")]
            [maxtessfactor(64)]
            ShadowTessControlPoint ShadowHullShader(InputPatch<ShadowTessControlPoint, 3> patch, uint id : SV_OutputControlPointID)
            {
                return patch[id];
            }

            [domain("tri")]
            ShadowVaryings ShadowDomainShader(ShadowTessFactors factors, OutputPatch<ShadowTessControlPoint, 3> patch, float3 bary : SV_DomainLocation)
            {
                ShadowVaryings output;
                UNITY_SETUP_INSTANCE_ID(patch[0]);
                UNITY_TRANSFER_INSTANCE_ID(patch[0], output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 positionOS = patch[0].positionOS.xyz * bary.x + patch[1].positionOS.xyz * bary.y + patch[2].positionOS.xyz * bary.z;
                float3 positionWS = TransformObjectToWorld(positionOS);

                GerstnerWaveOutput waveOutput = EvaluateGerstnerWavesSimple(positionWS, _Time.y);
                float3 displacedPositionWS = positionWS + waveOutput.displacement;
                float3 normalWS = waveOutput.normal;

                output.positionCS = TransformWorldToHClip(ApplyShadowBias(displacedPositionWS, normalWS, _LightDirection));

                #if UNITY_REVERSED_Z
                output.positionCS.z = min(output.positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                output.positionCS.z = max(output.positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif

                return output;
            }

            half4 ShadowPassFrag(ShadowVaryings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}
