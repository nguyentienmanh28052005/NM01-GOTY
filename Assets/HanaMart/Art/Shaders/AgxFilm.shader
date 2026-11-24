// AgX (three.js r160, exposure 1.02) + shader "Film" của HanaMart24 main.js — chạy sau hậu kỳ URP (bloom).
// Film làm việc trong không gian sRGB như bản gốc (sau OutputPass), rồi đổi lại tuyến tính cho URP.
Shader "HanaMart/AgxFilm"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off ZTest Always Blend Off Cull Off
        Pass
        {
            Name "AgxFilm"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

            float _HM_Exposure, _HM_Time, _HM_Cine, _HM_Pulse, _HM_Warp, _HM_Grain, _HM_Vignette, _HM_CA, _HM_Fade, _HM_White, _HM_Glitch, _HM_Bright, _HM_Red, _HM_Sat;

            float3 Agx(float3 c)
            {
                const float3x3 SRGB_TO_REC2020 = float3x3(0.6274, 0.0691, 0.0164, 0.3293, 0.9195, 0.0880, 0.0433, 0.0113, 0.8956);
                const float3x3 REC2020_TO_SRGB = float3x3(1.6605, -0.1246, -0.0182, -0.5876, 1.1329, -0.1006, -0.0728, -0.0083, 1.1187);
                const float3x3 INSET = float3x3(0.856627153315983, 0.137318972929847, 0.11189821299995, 0.0951212405381588, 0.761241990602591, 0.0767994186031903, 0.0482516061458583, 0.101439036467562, 0.811302368396859);
                const float3x3 OUTSET = float3x3(1.1271005818144368, -0.1413297634984383, -0.14132976349843826, -0.11060664309660323, 1.157823702216272, -0.11060664309660294, -0.016493938717834573, -0.016493938717834257, 1.2519364065950405);
                // mat3 GLSL theo cột ⇒ mul(v, M) với M ghi theo hàng = các cột GLSL
                c *= _HM_Exposure;
                c = mul(c, SRGB_TO_REC2020); c = mul(c, INSET);
                c = saturate((log2(max(c, 1e-10)) - (-12.47393)) / (4.026069 - (-12.47393)));
                float3 x2 = c * c, x4 = x2 * x2;
                c = 15.5 * x4 * x2 - 40.14 * x4 * c + 31.96 * x4 - 6.868 * x2 * c + 0.4298 * x2 + 0.1191 * c - 0.00232;
                c = mul(c, OUTSET);
                c = pow(max(0.0, c), 2.2);
                c = mul(c, REC2020_TO_SRGB);
                return saturate(c);
            }

            float H(float2 p) { return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453); }
            float3 Src(float2 uv) { return LinearToSRGB(Agx(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv).rgb)); }

            half4 Frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float2 vUv = i.texcoord;
                float2 res = _ScreenParams.xy;
                float2 c = vUv - 0.5; float2 uv = 0.5 + c * (1.0 - 0.03 * dot(c, c));
                uv += float2(sin(uv.y * 26.0 + _HM_Time * 2.7), cos(uv.x * 19.0 + _HM_Time * 2.1)) * 0.0035 * _HM_Warp * (0.4 + length(c) * 1.6);
                if (_HM_Glitch > 0.0) { float band = step(0.9, H(float2(floor(uv.y * 50.0), floor(_HM_Time * 24.0)))); uv.x += (H(float2(_HM_Time, uv.y)) - 0.5) * 0.08 * _HM_Glitch * band; }
                uv += float2(sin(_HM_Time * 1.3) * 0.0004, sin(_HM_Time * 0.7 + 1.0) * 0.0006) * _HM_Cine;
                float k = _HM_CA * (1.0 + _HM_Glitch * 8.0 + _HM_Pulse * 3.0);
                float3 col = float3(Src(0.5 + (uv - 0.5) * (1.0 + k)).r, Src(uv).g, Src(0.5 + (uv - 0.5) * (1.0 - k)).b);
                col *= _HM_Bright;
                if (_HM_Cine > 0.0)
                {
                    float3 st = 0; float px = 1.0 / res.x;
                    for (int s = 1; s <= 12; s++) { float o = s * px * 9.0; float3 a = Src(uv + float2(o, 0)); float3 b = Src(uv - float2(o, 0)); float w = 1.0 - s / 13.0; st += (max(a - 0.85, 0) + max(b - 0.85, 0)) * w; }
                    col += st * float3(0.25, 0.5, 1.0) * 0.09 * _HM_Cine;
                    float3 hl = 0; for (int q = 0; q < 6; q++) { float an = q * 1.047; hl += max(Src(uv + float2(cos(an), sin(an)) * 3.0 / res) - 0.8, 0); }
                    col += hl * float3(1.0, 0.25, 0.12) * 0.06 * _HM_Cine;
                }
                float l = dot(col, float3(0.299, 0.587, 0.114));
                col = lerp(l.xxx, col, _HM_Sat);
                float sh = (1.0 - smoothstep(0.0, 0.55, l));
                col = lerp(col, col * float3(0.66, 0.92, 1.36) + float3(0.0, 0.008, 0.026), sh * 0.9);
                col = lerp(col, col * float3(0.88, 1.0, 1.12), (1.0 - smoothstep(0.3, 0.85, l)) * 0.6);
                col *= float3(0.96, 1.0, 1.04);
                col = lerp(col, col * col * (3.0 - 2.0 * col), 0.35);
                col = max(col - 0.01, 0.0) * 1.012;
                col += float3(0.012, 0.0, -0.01) * smoothstep(0.7, 1.0, l);
                col *= lerp(1.0, (1.0 - smoothstep(0.25, 0.98, length(c) * 1.1)), _HM_Vignette + _HM_Pulse * 0.35);
                col = lerp(col, col * float3(1.3, 0.35, 0.35), _HM_Red);
                col += (H(vUv * float2(1920.0, 1080.0) + frac(_HM_Time) * 91.0) - 0.5) * _HM_Grain;
                col = lerp(col, 1.0, _HM_White); col *= 1.0 - _HM_Fade;
                return half4(SRGBToLinear(saturate(col)), 1.0);
            }
            ENDHLSL
        }
    }
}
