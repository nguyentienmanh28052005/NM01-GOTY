Shader "Universal Render Pipeline/HanaMart/LampCone"
{
    Properties
    {
        _Color ("Color", Color) = (1.0, 0.62745, 0.2902, 1.0)
        _Opacity ("Opacity", Range(0, 1)) = 0.22
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent+6" "RenderPipeline" = "UniversalPipeline" }
        Blend One One
        ZWrite Off
        Cull Off

        Pass
        {
            Name "LampCone"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalVS : TEXCOORD1;
                float3 viewDirVS : TEXCOORD2;
                float3 localPos : TEXCOORD3;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _Opacity;
            CBUFFER_END

            float Hash21(float2 p)
            {
                return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float3 posWS = TransformObjectToWorld(IN.positionOS.xyz);
                float3 posVS = TransformWorldToView(posWS);
                float3 normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.positionHCS = TransformWorldToHClip(posWS);
                OUT.uv = IN.uv;
                OUT.normalVS = normalize(mul((float3x3)UNITY_MATRIX_V, normalWS));
                OUT.viewDirVS = normalize(-posVS);
                OUT.localPos = IN.positionOS.xyz;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float edge = pow(abs(dot(normalize(IN.normalVS), normalize(IN.viewDirVS))), 1.6);
                float fall = pow(saturate(IN.uv.y), 1.4);
                float rain = 0.75 + 0.25 * step(0.82, Hash21(float2(
                    floor(atan2(IN.localPos.x, IN.localPos.z) * 40.0),
                    floor(IN.localPos.y * 3.0 + _Time.y * 9.0))));
                return half4(_Color.rgb * (_Opacity * edge * fall * rain), 1.0);
            }
            ENDHLSL
        }
    }
}
