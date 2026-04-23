Shader "Blue/CoralBlend"
{
    Properties
    {
        [Header(Material 1 Base)]
        _BaseMap1 ("Base Map 1", 2D) = "white" {}
        _BaseColor1 ("Base Color 1", Color) = (1, 1, 1, 1)

        [Header(Material 1 Normal)]
        [Toggle(_NORMALMAP1)] _EnableNormal1 ("Enable Normal 1", Float) = 0
        [NoScaleOffset] _NormalMap1 ("Normal Map 1", 2D) = "bump" {}
        _NormalScale1 ("Normal Scale 1", Range(0, 2)) = 1.0

        [Header(Material 1 Emission)]
        [Toggle(_EMISSION1)] _EnableEmission1 ("Enable Emission 1", Float) = 0
        _EmissionMap1 ("Emission Map 1", 2D) = "black" {}
        [HDR] _EmissionColor1 ("Emission Color 1", Color) = (0, 0, 0, 1)

        [Header(Material 2 Tip)]
        _BaseMap2 ("Base Map 2", 2D) = "white" {}
        _BaseColor2 ("Base Color 2", Color) = (1, 1, 1, 1)

        [Header(Material 2 Normal)]
        [Toggle(_NORMALMAP2)] _EnableNormal2 ("Enable Normal 2", Float) = 0
        [NoScaleOffset] _NormalMap2 ("Normal Map 2", 2D) = "bump" {}
        _NormalScale2 ("Normal Scale 2", Range(0, 2)) = 1.0

        [Header(Material 2 Emission)]
        [Toggle(_EMISSION2)] _EnableEmission2 ("Enable Emission 2", Float) = 0
        _EmissionMap2 ("Emission Map 2", 2D) = "black" {}
        [HDR] _EmissionColor2 ("Emission Color 2", Color) = (0, 0, 0, 1)

        [Header(Surface)]
        _Smoothness ("Smoothness", Range(0, 1)) = 0.5
        _Metallic ("Metallic", Range(0, 1)) = 0.0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma shader_feature_local _EMISSION1
            #pragma shader_feature_local _EMISSION2
            #pragma shader_feature_local _NORMALMAP1
            #pragma shader_feature_local _NORMALMAP2

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float4 color : COLOR;
                float fogFactor : TEXCOORD3;
                #if defined(_NORMALMAP1) || defined(_NORMALMAP2)
                    float3 tangentWS : TEXCOORD4;
                    float3 bitangentWS : TEXCOORD5;
                #endif
            };

            TEXTURE2D(_BaseMap1);
            TEXTURE2D(_BaseMap2);
            TEXTURE2D(_NormalMap1);
            TEXTURE2D(_NormalMap2);
            TEXTURE2D(_EmissionMap1);
            TEXTURE2D(_EmissionMap2);
            SAMPLER(sampler_BaseMap1);
            SAMPLER(sampler_BaseMap2);
            SAMPLER(sampler_NormalMap1);
            SAMPLER(sampler_NormalMap2);
            SAMPLER(sampler_EmissionMap1);
            SAMPLER(sampler_EmissionMap2);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap1_ST;
                float4 _BaseMap2_ST;
                half4 _BaseColor1;
                half4 _BaseColor2;
                half _NormalScale1;
                half _NormalScale2;
                half4 _EmissionColor1;
                half4 _EmissionColor2;
                half _Smoothness;
                half _Metallic;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;

                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normInputs = GetVertexNormalInputs(input.normalOS, input.tangentOS);

                output.positionCS = posInputs.positionCS;
                output.positionWS = posInputs.positionWS;
                output.normalWS = normInputs.normalWS;
                output.uv = input.uv;
                output.color = input.color;
                output.fogFactor = ComputeFogFactor(posInputs.positionCS.z);

                #if defined(_NORMALMAP1) || defined(_NORMALMAP2)
                    output.tangentWS = normInputs.tangentWS;
                    output.bitangentWS = normInputs.bitangentWS;
                #endif

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // ブレンド係数（頂点カラーのR値）
                half blend = input.color.r;

                // UV座標（各マテリアルのタイリングを適用）
                float2 uv1 = input.uv * _BaseMap1_ST.xy + _BaseMap1_ST.zw;
                float2 uv2 = input.uv * _BaseMap2_ST.xy + _BaseMap2_ST.zw;

                // ベースカラーをサンプリング
                half4 baseColor1 = SAMPLE_TEXTURE2D(_BaseMap1, sampler_BaseMap1, uv1) * _BaseColor1;
                half4 baseColor2 = SAMPLE_TEXTURE2D(_BaseMap2, sampler_BaseMap2, uv2) * _BaseColor2;

                // ブレンド
                half4 albedo = lerp(baseColor1, baseColor2, blend);

                // ノーマルマップ処理
                half3 normalTS = half3(0, 0, 1);
                half3 normalWS = normalize(input.normalWS);

                #if defined(_NORMALMAP1) || defined(_NORMALMAP2)
                    half3 normal1 = half3(0, 0, 1);
                    half3 normal2 = half3(0, 0, 1);

                    #ifdef _NORMALMAP1
                        normal1 = UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalMap1, sampler_NormalMap1, uv1), _NormalScale1);
                    #endif

                    #ifdef _NORMALMAP2
                        normal2 = UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalMap2, sampler_NormalMap2, uv2), _NormalScale2);
                    #endif

                    // ノーマルをブレンド
                    normalTS = normalize(lerp(normal1, normal2, blend));

                    // タンジェント空間からワールド空間へ変換
                    half3x3 tangentToWorld = half3x3(
                        normalize(input.tangentWS),
                        normalize(input.bitangentWS),
                        normalWS
                    );
                    normalWS = normalize(mul(normalTS, tangentToWorld));
                #endif

                // Emission
                half3 emission = half3(0, 0, 0);

                #ifdef _EMISSION1
                    half3 emission1 = SAMPLE_TEXTURE2D(_EmissionMap1, sampler_EmissionMap1, uv1).rgb * _EmissionColor1.rgb;
                    emission = lerp(emission1, emission, blend);
                #endif

                #ifdef _EMISSION2
                    half3 emission2 = SAMPLE_TEXTURE2D(_EmissionMap2, sampler_EmissionMap2, uv2).rgb * _EmissionColor2.rgb;
                    #ifdef _EMISSION1
                        emission = lerp(emission, emission2, blend);
                    #else
                        emission = emission2 * blend;
                    #endif
                #endif

                // ライティング計算
                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.normalWS = normalWS;
                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                inputData.fogCoord = input.fogFactor;

                #if defined(_MAIN_LIGHT_SHADOWS) || defined(_MAIN_LIGHT_SHADOWS_CASCADE)
                    inputData.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                #else
                    inputData.shadowCoord = float4(0, 0, 0, 0);
                #endif

                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = albedo.rgb;
                surfaceData.alpha = albedo.a;
                surfaceData.metallic = _Metallic;
                surfaceData.smoothness = _Smoothness;
                surfaceData.normalTS = normalTS;
                surfaceData.emission = emission;
                surfaceData.occlusion = 1.0;

                half4 color = UniversalFragmentPBR(inputData, surfaceData);
                color.rgb = MixFog(color.rgb, input.fogFactor);

                return color;
            }
            ENDHLSL
        }

        // Shadow caster pass
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            float3 _LightDirection;

            Varyings ShadowPassVertex(Attributes input)
            {
                Varyings output;

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));

                #if UNITY_REVERSED_Z
                    positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif

                output.positionCS = positionCS;
                return output;
            }

            half4 ShadowPassFragment(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }

        // Depth pass
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings DepthOnlyVertex(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half4 DepthOnlyFragment(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}
