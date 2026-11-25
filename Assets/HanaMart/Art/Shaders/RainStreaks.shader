Shader "Universal Render Pipeline/HanaMart/RainStreaks"
{
    Properties
    {
        _Color ("Color", Color) = (0.78, 0.85, 1.0, 1.0)
        _Opacity ("Opacity", Range(0, 1)) = 0.22
        _Width ("Streak Width", Range(0.002, 0.03)) = 0.007
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent+5" "RenderPipeline" = "UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "RainStreaks"
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
                float alpha : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _Opacity;
                float _Width;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float topFlag = IN.uv.x;
                float baseY = IN.uv.y;
                float y = fmod(baseY - _Time.y * 9.0, 9.0);
                if (y < 0.0) y += 9.0;

                float3 posOS = IN.positionOS.xyz;
                posOS.y = y + topFlag * 0.34;
                posOS.x += topFlag * 0.015;

                float3 posWS = TransformObjectToWorld(posOS);
                float3 camRight = UNITY_MATRIX_I_V._m00_m10_m20;
                posWS += camRight * (IN.normalOS.x * _Width);

                OUT.positionHCS = TransformWorldToHClip(posWS);
                OUT.alpha = smoothstep(0.0, 0.6, y) * (0.4 + 0.6 * topFlag);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                return half4(_Color.rgb, _Opacity * IN.alpha);
            }
            ENDHLSL
        }
    }
}
