using UnityEngine;

namespace HanaMart.Render
{
    /// AgX tone mapping của three.js r160 (bản C# để kiểm thử; shader AgxFilm.shader dùng cùng công thức).
    public static class Agx
    {
        // Ma trận GLSL mat3(c0,c1,c2) xếp theo CỘT
        static readonly Vector3[] Rec2020ToSrgb = { new Vector3(1.6605f, -0.1246f, -0.0182f), new Vector3(-0.5876f, 1.1329f, -0.1006f), new Vector3(-0.0728f, -0.0083f, 1.1187f) };
        static readonly Vector3[] SrgbToRec2020 = { new Vector3(0.6274f, 0.0691f, 0.0164f), new Vector3(0.3293f, 0.9195f, 0.0880f), new Vector3(0.0433f, 0.0113f, 0.8956f) };
        static readonly Vector3[] Inset = { new Vector3(0.856627153315983f, 0.137318972929847f, 0.11189821299995f), new Vector3(0.0951212405381588f, 0.761241990602591f, 0.0767994186031903f), new Vector3(0.0482516061458583f, 0.101439036467562f, 0.811302368396859f) };
        static readonly Vector3[] Outset = { new Vector3(1.1271005818144368f, -0.1413297634984383f, -0.14132976349843826f), new Vector3(-0.11060664309660323f, 1.157823702216272f, -0.11060664309660294f), new Vector3(-0.016493938717834573f, -0.016493938717834257f, 1.2519364065950405f) };
        const float MinEv = -12.47393f, MaxEv = 4.026069f;

        static Vector3 Mul(Vector3[] cols, Vector3 v) => cols[0] * v.x + cols[1] * v.y + cols[2] * v.z;
        static float Contrast(float x) { float x2 = x * x, x4 = x2 * x2; return 15.5f * x4 * x2 - 40.14f * x4 * x + 31.96f * x4 - 6.868f * x2 * x + 0.4298f * x2 + 0.1191f * x - 0.00232f; }

        public static Vector3 Tonemap(Vector3 c, float exposure = 1.02f)
        {
            c = Mul(Inset, Mul(SrgbToRec2020, c * exposure));
            for (int i = 0; i < 3; i++) c[i] = Contrast(Mathf.Clamp01((Mathf.Log(Mathf.Max(c[i], 1e-10f), 2f) - MinEv) / (MaxEv - MinEv)));
            c = Mul(Outset, c);
            for (int i = 0; i < 3; i++) c[i] = Mathf.Pow(Mathf.Max(0f, c[i]), 2.2f);
            c = Mul(Rec2020ToSrgb, c);
            for (int i = 0; i < 3; i++) c[i] = Mathf.Clamp01(c[i]);
            return c;
        }
    }
}
