Shader "Custom/Glass"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.6, 0.8, 1.0, 0.08)
        _EdgeTint ("Edge Tint", Color) = (0.7, 0.9, 1.0, 0.5)
        _Opacity ("Opacity", Range(0, 1)) = 0.2
        _FresnelPower ("Fresnel Power", Range(0.5, 8.0)) = 3.0
        _Smoothness ("Smoothness", Range(0, 1)) = 0.85
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }
        LOD 200

        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForward" }
            Cull Back
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 viewDirWS : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _EdgeTint;
                float _Opacity;
                float _FresnelPower;
                float _Smoothness;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionHCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.viewDirWS = GetWorldSpaceViewDir(output.positionWS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 normalWS = normalize(input.normalWS);
                float3 viewDirWS = normalize(input.viewDirWS);
                float fresnel = pow(1.0 - saturate(dot(normalWS, viewDirWS)), _FresnelPower);

                Light mainLight = GetMainLight();
                float3 lightDir = normalize(mainLight.direction);
                float ndotl = saturate(dot(normalWS, -lightDir));
                float3 litColor = _BaseColor.rgb * (0.2 + ndotl * 0.8) * mainLight.color.rgb;

                float3 color = lerp(litColor, _EdgeTint.rgb, fresnel);
                float alpha = saturate(_Opacity + fresnel * _EdgeTint.a);

                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
}