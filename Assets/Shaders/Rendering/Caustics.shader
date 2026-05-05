Shader "Hidden/Caustics"
{
    Properties
    {
        _LayerCount ("Layer Count", Int) = 3
        _CellSize ("Cell Size", Float) = 0.5
        _Jitter ("Jitter", Float) = 0.9
        _CausticsIntensity ("Intensity", Float) = 1.0
        _PixelCount ("Pixel Count", Float) = 0
        _Speed1 ("Speed 1", Vector) = (0.5, 0.3, 0, 0)
        _Speed2 ("Speed 2", Vector) = (-0.4, 0.5, 0, 0)
        _CausticsMaxDepth ("Max Depth", Float) = 30
        _WaterSurfaceY ("Water Surface Y", Float) = 0
        _FadeDistance ("Fade Distance", Float) = 100
        _FadePower ("Fade Power", Float) = 0.5
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
        }
        LOD 100

        Pass
        {
            Name "CausticsPass"
            ZWrite Off
            ZTest Always
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            TEXTURE2D(_BlitTexture);
            SAMPLER(sampler_BlitTexture);

            CBUFFER_START(UnityPerMaterial)
                int _LayerCount;
                float _CellSize;
                float _Jitter;
                float _CausticsIntensity;
                float _PixelCount;
                float4 _Speed1;
                float4 _Speed2;
                float _CausticsMaxDepth;
                float _WaterSurfaceY;
                float _FadeDistance;
                float _FadePower;
            CBUFFER_END

            struct CausticsAttributes
            {
                uint vertexID : SV_VertexID;
            };

            struct CausticsVaryings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            // Hash function for randomness
            float2 Hash22(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * float3(0.1031, 0.1030, 0.0973));
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.xx + p3.yz) * p3.zy);
            }

            // Voronoi noise - returns distance to nearest cell center
            float Voronoi(float2 uv, float cellSize, float jitter)
            {
                float2 scaledUV = uv / cellSize;
                float2 cell = floor(scaledUV);
                float2 fracUV = frac(scaledUV);

                float minDist = 1.0;

                // Check 3x3 neighborhood
                [unroll]
                for (int y = -1; y <= 1; y++)
                {
                    [unroll]
                    for (int x = -1; x <= 1; x++)
                    {
                        float2 neighbor = float2(x, y);
                        float2 cellCenter = Hash22(cell + neighbor) * jitter;
                        float dist = length(neighbor + cellCenter - fracUV);
                        minDist = min(minDist, dist);
                    }
                }

                return minDist;
            }

            // Generate caustics pattern using multiple layers of Voronoi
            float GenerateCaustics(float2 uv, float time)
            {
                float caustics = 1.0;

                // Generate multiple layers and blend with min
                for (int i = 0; i < _LayerCount; i++)
                {
                    // Each layer has different scale, speed, and offset
                    float scale = 1.0 + i * 0.27;
                    float speedMult = 1.0 - i * 0.15;
                    float2 offset = float2(i * 1.7, i * 2.3);

                    // Alternate between speed1 and speed2
                    float2 speed = (i % 2 == 0) ? _Speed1.xy : _Speed2.xy;

                    float2 layerUV = uv * scale + time * speed * speedMult + offset;
                    float v = Voronoi(layerUV, _CellSize, _Jitter);

                    // Min blending - each layer fills in gaps from others
                    caustics = min(caustics, v);
                }

                // Boost and sharpen the pattern
                caustics = pow(caustics, 1.2);
                caustics = saturate(caustics * 3.0);

                return caustics;
            }

            CausticsVaryings Vert(CausticsAttributes input)
            {
                CausticsVaryings output;
                output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
                output.uv = GetFullScreenTriangleTexCoord(input.vertexID);
                return output;
            }

            half4 Frag(CausticsVaryings input) : SV_Target
            {
                float2 screenUV = input.uv;

                // Sample scene color
                half4 sceneColor = SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, screenUV);

                // Sample depth
                float depth = SampleSceneDepth(screenUV);

                // Check for skybox (far plane)
                #if UNITY_REVERSED_Z
                if (depth < 0.0001)
                    return sceneColor;
                #else
                if (depth > 0.9999)
                    return sceneColor;
                #endif

                // Reconstruct world position from depth
                float3 positionWS = ComputeWorldSpacePosition(screenUV, depth, UNITY_MATRIX_I_VP);

                // Skip if above water surface
                if (positionWS.y > _WaterSurfaceY)
                    return sceneColor;

                // Calculate underwater depth
                float underwaterDepth = _WaterSurfaceY - positionWS.y;

                // Depth attenuation (quadratic falloff)
                float depthAttenuation = saturate(1.0 - underwaterDepth / _CausticsMaxDepth);
                depthAttenuation = depthAttenuation * depthAttenuation;

                // Skip if too deep
                if (depthAttenuation < 0.001)
                    return sceneColor;

                // Camera distance attenuation (configurable)
                float3 viewVector = positionWS - _WorldSpaceCameraPos;
                float cameraDistance = length(viewVector);
                float distanceAttenuation = saturate(1.0 - cameraDistance / _FadeDistance);
                distanceAttenuation = pow(distanceAttenuation, _FadePower * 2.0 + 0.5);

                // Get shadow coordinate and sample shadow map
                float4 shadowCoord = TransformWorldToShadowCoord(positionWS);
                Light mainLight = GetMainLight(shadowCoord);
                float3 lightDir = mainLight.direction;

                // Shadow attenuation (0 = in shadow, 1 = lit)
                float shadowAttenuation = mainLight.shadowAttenuation;

                // Project caustics along light direction
                float2 causticsUV = positionWS.xz;
                causticsUV += lightDir.xz * (positionWS.y - _WaterSurfaceY) * 0.3;

                // Pixelate UV if pixel count > 0
                if (_PixelCount > 0)
                {
                    causticsUV = floor(causticsUV * _PixelCount) / _PixelCount;
                }

                // Generate caustics pattern
                float caustics = GenerateCaustics(causticsUV, _Time.y);

                // Apply intensity, depth attenuation, distance attenuation, and shadow
                float totalAttenuation = depthAttenuation * distanceAttenuation * shadowAttenuation;

                float3 causticsColor = mainLight.color * caustics * _CausticsIntensity * totalAttenuation;

                // Modulate by light direction (caustics are stronger when light hits from above)
                float lightAngleFactor = saturate(lightDir.y);
                causticsColor *= lightAngleFactor;

                // Additive blending
                return half4(sceneColor.rgb + causticsColor, sceneColor.a);
            }
            ENDHLSL
        }
    }
}
