Shader "Custom/WaterFill"
{
    Properties
    {
        _WaterColor ("Water Color", Color) = (0.1, 0.5, 0.8, 0.6)
        _SurfaceColor ("Surface Color", Color) = (0.5, 0.8, 1.0, 0.9)
        [PerRendererData]_FillLevel01 ("Fill Level (0-1, Local)", Range(0, 1)) = 0.5
        [PerRendererData]_ObjectHeight ("Object Height (Local Units)", Float) = 1.0
        [PerRendererData]_PivotOffsetY ("Pivot Offset Y (Local Units)", Float) = 0.0
        _SurfaceLineThickness ("Surface Line Thickness", Range(0.001, 0.2)) = 0.03
        _SurfaceLineSoftness ("Surface Line Softness", Range(0.0, 0.2)) = 0.02
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }
        LOD 100

        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForward" }
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

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
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _WaterColor;
                float4 _SurfaceColor;
                float _SurfaceLineThickness;
                float _SurfaceLineSoftness;
            CBUFFER_END

            UNITY_INSTANCING_BUFFER_START(WaterFill)
                UNITY_DEFINE_INSTANCED_PROP(float, _FillLevel01)
                UNITY_DEFINE_INSTANCED_PROP(float, _ObjectHeight)
                UNITY_DEFINE_INSTANCED_PROP(float, _PivotOffsetY)
            UNITY_INSTANCING_BUFFER_END(WaterFill)

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionHCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float fillLevel01 = UNITY_ACCESS_INSTANCED_PROP(WaterFill, _FillLevel01);
                float objectHeight = UNITY_ACCESS_INSTANCED_PROP(WaterFill, _ObjectHeight);
                float pivotOffsetY = UNITY_ACCESS_INSTANCED_PROP(WaterFill, _PivotOffsetY);
                float fillLevelLocal = (fillLevel01 * objectHeight) + pivotOffsetY;

                float3 positionOS = TransformWorldToObject(input.positionWS);
                float fillDistance = fillLevelLocal - positionOS.y;
                float edgeWidth = max(fwidth(positionOS.y), 1e-4);
                float fillMask = saturate(smoothstep(-edgeWidth, edgeWidth, fillDistance));

                float3 normalWS = normalize(input.normalWS);
                float upMask = saturate(dot(normalWS, float3(0.0, 1.0, 0.0)));

                float lineDistance = abs(positionOS.y - fillLevelLocal);
                float lineMask = 1.0 - smoothstep(_SurfaceLineThickness, _SurfaceLineThickness + _SurfaceLineSoftness, lineDistance);

                float surfaceBlend = max(_SurfaceColor.a * upMask, lineMask);

                float3 color = lerp(_WaterColor.rgb, _SurfaceColor.rgb, surfaceBlend);
                float alpha = lerp(_WaterColor.a, _SurfaceColor.a, surfaceBlend) * fillMask;

                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
}