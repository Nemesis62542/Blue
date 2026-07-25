Shader "Blue/FlatLitParticle"
{
    Properties
    {
        _BaseMap ("Base Map", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
        [Toggle] _UseVertexColor ("Use Vertex Color", Float) = 1

        [Header(Alpha Clipping)]
        [Toggle(_ALPHATEST_ON)] _AlphaClip ("Alpha Clipping", Float) = 0
        _Cutoff ("Alpha Cutoff", Range(0, 1)) = 0.5

        [Header(Blending)]
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 5
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 10
        [Enum(Off, 0, On, 1)] _ZWrite ("Z Write", Float) = 0
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull Mode", Float) = 2

        [Header(Lighting)]
        _AmbientStrength ("Ambient Strength", Float) = 1.0
        _MainLightStrength ("Main Light Strength", Float) = 1.0
        _AdditionalLightStrength ("Additional Light Strength", Float) = 1.0

        [Header(Distance Fade)]
        [Toggle] _UseDistanceFade ("Use Distance Fade", Float) = 0
        _FadeStartDistance ("Fade Start Distance", Float) = 10
        _FadeEndDistance ("Fade End Distance", Float) = 30
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma shader_feature_local_fragment _ALPHATEST_ON

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float4 color : COLOR;
                float fogFactor : TEXCOORD2;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _UseVertexColor;
                half _Cutoff;
                half _AmbientStrength;
                half _MainLightStrength;
                half _AdditionalLightStrength;
                half _UseDistanceFade;
                float _FadeStartDistance;
                float _FadeEndDistance;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);

                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);

                output.positionCS = posInputs.positionCS;
                output.positionWS = posInputs.positionWS;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.color = input.color;
                output.fogFactor = ComputeFogFactor(posInputs.positionCS.z);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;

                // パーティクルの頂点カラー（Start Color / Color over Lifetime）
                albedo *= lerp(half4(1, 1, 1, 1), input.color, _UseVertexColor);

                #if defined(_ALPHATEST_ON)
                    clip(albedo.a - _Cutoff);
                #endif

                // 法線を一切使わず「その場所に届いている光の量」だけを積算する。
                // ビルボードの法線は常にカメラを向くため、N・Lを含めると
                // プレイヤーの向きで明るさが変わってしまう。

                // アンビエント（SHのL0項のみ＝方向に依存しない成分）
                half3 lighting = SampleSH(half3(0, 0, 0)) * _AmbientStrength;

                // メインライト（影・減衰のみ反映、向きは無視）
                #if defined(_MAIN_LIGHT_SHADOWS) || defined(_MAIN_LIGHT_SHADOWS_CASCADE)
                    float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                #else
                    float4 shadowCoord = float4(0, 0, 0, 0);
                #endif
                Light mainLight = GetMainLight(shadowCoord);
                lighting += mainLight.color * mainLight.distanceAttenuation * mainLight.shadowAttenuation * _MainLightStrength;

                // 追加ライト（ポイント・スポット等。距離減衰と影のみ反映）
                #if defined(_ADDITIONAL_LIGHTS)
                    // LIGHT_LOOP_BEGINがForward+のクラスタ走査で参照する
                    InputData inputData = (InputData)0;
                    inputData.positionWS = input.positionWS;
                    inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);

                    uint pixelLightCount = GetAdditionalLightsCount();

                    #if USE_CLUSTER_LIGHT_LOOP
                    // Forward+ではメインライト以外のディレクショナルライトはクラスタ外
                    [loop] for (uint dirIndex = 0; dirIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); dirIndex++)
                    {
                        Light light = GetAdditionalLight(dirIndex, input.positionWS, half4(1, 1, 1, 1));
                        lighting += light.color * light.distanceAttenuation * light.shadowAttenuation * _AdditionalLightStrength;
                    }
                    #endif

                    LIGHT_LOOP_BEGIN(pixelLightCount)
                        Light light = GetAdditionalLight(lightIndex, input.positionWS, half4(1, 1, 1, 1));
                        lighting += light.color * light.distanceAttenuation * light.shadowAttenuation * _AdditionalLightStrength;
                    LIGHT_LOOP_END
                #endif

                half3 color = albedo.rgb * lighting;
                half alpha = albedo.a;

                // 距離フェード（End <= Start の不正値でも全消えしないようにガード）
                float dist = distance(_WorldSpaceCameraPos, input.positionWS);
                float fadeEnd = max(_FadeEndDistance, _FadeStartDistance + 0.0001);
                half distanceFade = 1.0 - smoothstep(_FadeStartDistance, fadeEnd, dist);
                alpha *= lerp(1.0, distanceFade, _UseDistanceFade);

                color = MixFog(color, input.fogFactor);

                return half4(color, alpha);
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Particles/Unlit"
}
