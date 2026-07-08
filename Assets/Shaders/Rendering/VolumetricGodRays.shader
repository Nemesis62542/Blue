Shader "Hidden/VolumetricGodRays"
{
    Properties
    {
        _StepCount ("Step Count", Float) = 24
        _MaxRayDistance ("Max Ray Distance", Float) = 50
        _GodRaysIntensity ("Intensity", Float) = 1.0
        _Density ("Density", Float) = 1.0
        _Anisotropy ("Anisotropy", Float) = 0.6
        _DepthFalloff ("Depth Falloff", Float) = 0.05
        _Tint ("Tint", Color) = (1, 1, 1, 1)
        _NoiseCellSize ("Noise Cell Size", Float) = 4
        _NoiseInfluence ("Noise Influence", Float) = 0.8
        _NoiseSpeed ("Noise Speed", Vector) = (0.5, 0.3, 0, 0)
        _NoisePower ("Noise Power", Float) = 2.0
        _WaterSurfaceY ("Water Surface Y", Float) = 0
        _TexelSize ("Texel Size", Vector) = (0, 0, 0, 0)
        _GodRaysTexture ("God Rays Texture", 2D) = "black" {}
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
        }
        LOD 100

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

        TEXTURE2D(_BlitTexture);
        TEXTURE2D(_GodRaysTexture);
        // sampler_LinearClamp は Core.hlsl 経由の GlobalSamplers.hlsl で宣言済み

        CBUFFER_START(UnityPerMaterial)
            float _StepCount;
            float _MaxRayDistance;
            float _GodRaysIntensity;
            float _Density;
            float _Anisotropy;
            float _DepthFalloff;
            float4 _Tint;
            float _NoiseCellSize;
            float _NoiseInfluence;
            float4 _NoiseSpeed;
            float _NoisePower;
            float _WaterSurfaceY;
            float4 _TexelSize;
        CBUFFER_END

        struct GodRaysAttributes
        {
            uint vertexID : SV_VertexID;
        };

        struct GodRaysVaryings
        {
            float4 positionCS : SV_POSITION;
            float2 uv : TEXCOORD0;
        };

        GodRaysVaryings Vert(GodRaysAttributes input)
        {
            GodRaysVaryings output;
            output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
            output.uv = GetFullScreenTriangleTexCoord(input.vertexID);
            return output;
        }

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

        // Henyey-Greenstein phase function
        float HenyeyGreenstein(float cosTheta, float g)
        {
            float g2 = g * g;
            float denom = 1.0 + g2 - 2.0 * g * cosTheta;
            return (1.0 - g2) / (4.0 * PI * denom * sqrt(max(denom, 1e-4)));
        }
        ENDHLSL

        // Pass 0: Raymarch (low resolution, R=scattering, G=representative distance)
        Pass
        {
            Name "GodRaysRaymarch"
            ZWrite Off
            ZTest Always
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragRaymarch

            // _MAIN_LIGHT_SHADOWS_SCREEN は含めない（レイ上の点でスクリーンUVが破綻するため）
            // _SHADOWS_SOFT も含めない（ループ内PCFは高コスト。ディザ＋ブラーで平滑化）
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE

            half4 FragRaymarch(GodRaysVaryings input) : SV_Target
            {
                float2 screenUV = input.uv;

                // Sample depth
                float depth = SampleSceneDepth(screenUV);

                // Check for skybox (far plane)
                #if UNITY_REVERSED_Z
                bool isSkybox = depth < 0.0001;
                #else
                bool isSkybox = depth > 0.9999;
                #endif

                // Reconstruct world position from depth (for skybox this gives the far plane; only direction is used)
                float3 positionWS = ComputeWorldSpacePosition(screenUV, depth, UNITY_MATRIX_I_VP);

                float3 cameraPos = _WorldSpaceCameraPos;
                float3 rayVector = positionWS - cameraPos;
                float sceneDistance = length(rayVector);
                float3 rayDir = rayVector / max(sceneDistance, 1e-4);

                float tStart = 0.0;
                float tEnd = isSkybox ? _MaxRayDistance : min(sceneDistance, _MaxRayDistance);

                // レイ区間を水面平面 Y = _WaterSurfaceY でクリップ
                if (cameraPos.y > _WaterSurfaceY)
                {
                    // 水上カメラ: 水中区間のみレイマーチ
                    if (rayDir.y >= -1e-4)
                        return half4(0, 0, 0, 1);

                    float tPlane = (_WaterSurfaceY - cameraPos.y) / rayDir.y;
                    tStart = max(tStart, tPlane);
                }
                else
                {
                    // 水中カメラ: 水面で打ち切り
                    if (rayDir.y > 1e-4)
                    {
                        float tPlane = (_WaterSurfaceY - cameraPos.y) / rayDir.y;
                        tEnd = min(tEnd, tPlane);
                    }
                }

                if (tStart >= tEnd)
                    return half4(0, 0, 0, 1);

                Light mainLight = GetMainLight();
                float3 lightDir = mainLight.direction;
                float invLightY = 1.0 / max(lightDir.y, 0.05);

                // ディザで開始位置をジッター（バンディング低減）
                float dither = InterleavedGradientNoise(input.positionCS.xy, 0);

                float stepSize = (tEnd - tStart) / _StepCount;
                float t = tStart + stepSize * dither;

                float scattering = 0.0;
                float weightedDistance = 0.0;

                [loop]
                for (float i = 0.0; i < _StepCount; i += 1.0)
                {
                    float3 p = cameraPos + rayDir * t;

                    // シャドウマップをサンプリング（カスケードは内部で処理される）
                    float shadow = MainLightRealtimeShadow(TransformWorldToShadowCoord(p));

                    // サンプル点をライト方向に水面へ投影し、Voronoiノイズで光筋を作る
                    float2 projUV = p.xz + lightDir.xz * (_WaterSurfaceY - p.y) * invLightY;
                    projUV += _Time.y * _NoiseSpeed.xy;
                    float noise = Voronoi(projUV, _NoiseCellSize, 0.9);
                    noise = saturate(pow(noise, _NoisePower));
                    float shaft = lerp(1.0, noise, _NoiseInfluence);

                    // 深度減衰（水面から深いほど暗く）
                    float depthAttenuation = exp(-max(_WaterSurfaceY - p.y, 0.0) * _DepthFalloff);

                    // 距離フェード（ShadowDistance超えでカスケード外が全点灯するのを防ぐ）
                    float distanceFade = 1.0 - smoothstep(_MaxRayDistance * 0.7, _MaxRayDistance, t);

                    float sampleValue = shadow * shaft * depthAttenuation * distanceFade;
                    scattering += sampleValue;
                    weightedDistance += sampleValue * t;

                    t += stepSize;
                }

                // 代表距離（将来のdepth-aware upsampling用）
                float averageDistance = weightedDistance / max(scattering, 1e-4);

                // Henyey-Greenstein位相関数（cosThetaはレイ上で一定なのでループ外で計算）
                float cosTheta = dot(rayDir, lightDir);
                float phase = HenyeyGreenstein(cosTheta, _Anisotropy);

                float rays = scattering * stepSize * phase * _Density * _GodRaysIntensity;

                return half4(rays, averageDistance, 0, 1);
            }
            ENDHLSL
        }

        // Pass 1: Horizontal blur (5-tap separable gaussian)
        Pass
        {
            Name "GodRaysBlurH"
            ZWrite Off
            ZTest Always
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragBlurH

            half4 FragBlurH(GodRaysVaryings input) : SV_Target
            {
                const float weights[5] = { 0.0625, 0.25, 0.375, 0.25, 0.0625 };

                half2 result = 0;
                [unroll]
                for (int i = 0; i < 5; i++)
                {
                    float2 offset = float2((i - 2) * _TexelSize.x, 0);
                    result += SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, input.uv + offset).rg * weights[i];
                }

                return half4(result, 0, 1);
            }
            ENDHLSL
        }

        // Pass 2: Vertical blur (5-tap separable gaussian)
        Pass
        {
            Name "GodRaysBlurV"
            ZWrite Off
            ZTest Always
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragBlurV

            half4 FragBlurV(GodRaysVaryings input) : SV_Target
            {
                const float weights[5] = { 0.0625, 0.25, 0.375, 0.25, 0.0625 };

                half2 result = 0;
                [unroll]
                for (int i = 0; i < 5; i++)
                {
                    float2 offset = float2(0, (i - 2) * _TexelSize.y);
                    result += SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, input.uv + offset).rg * weights[i];
                }

                return half4(result, 0, 1);
            }
            ENDHLSL
        }

        // Pass 3: Composite (full resolution, additive blend with camera color)
        Pass
        {
            Name "GodRaysComposite"
            ZWrite Off
            ZTest Always
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragComposite

            half4 FragComposite(GodRaysVaryings input) : SV_Target
            {
                half4 sceneColor = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, input.uv);

                // 低解像度のゴッドレイをバイリニアでアップサンプル
                half rays = SAMPLE_TEXTURE2D(_GodRaysTexture, sampler_LinearClamp, input.uv).r;

                Light mainLight = GetMainLight();
                half3 raysColor = rays * mainLight.color * _Tint.rgb;

                return half4(sceneColor.rgb + raysColor, sceneColor.a);
            }
            ENDHLSL
        }
    }
}
