using System;
using System.Collections.Generic;
using UnityEngine;

namespace HanaMart.Store
{
    [ExecuteAlways]
    public sealed class StoreWeather : MonoBehaviour
    {
        public readonly struct StreakSpec
        {
            public readonly float X;
            public readonly float Z;
            public readonly float Width;
            public readonly float Length;
            public readonly int ColorHex;
            public readonly float Opacity;
            public readonly float Dir;
            public readonly bool InsidePower;

            public StreakSpec(float x, float z, float width, float length, int colorHex, float opacity, float dir, bool insidePower = false)
            {
                X = x;
                Z = z;
                Width = width;
                Length = length;
                ColorHex = colorHex;
                Opacity = opacity;
                Dir = dir;
                InsidePower = insidePower;
            }
        }

        public static readonly StreakSpec[] StreakSpecs =
        {
            new(1.2f, 8.1f, 2.4f, 6.5f, 0xd8f4ff, 0.22f, 1f, insidePower: true),
            new(4.6f, 8.1f, 2.4f, 6.5f, 0xd8f4ff, 0.22f, 1f, insidePower: true),
            new(7.9f, 8.1f, 2.4f, 6.5f, 0xd8f4ff, 0.22f, 1f, insidePower: true),
            new(10.7f, 8.1f, 2.4f, 6.5f, 0xd8f4ff, 0.22f, 1f, insidePower: true),
            new(6.0f, 8.6f, 5.0f, 5.0f, 0x00f5d4, 0.16f, 1f, insidePower: false),
            new(1.0f, 21.2f, 2.2f, 8.0f, 0x2bff88, 0.30f, -1f, insidePower: false),
            new(13.0f, 21.2f, 2.2f, 8.0f, 0x4cc9f0, 0.30f, -1f, insidePower: false),
            new(-2.3f, 12.6f, 1.6f, 6.0f, 0xffa04a, 0.35f, -1f, insidePower: false),
            new(-2.3f, 12.6f, 1.6f, 6.0f, 0xffa04a, 0.25f, 1f, insidePower: false),
            new(15.7f, 12.6f, 1.6f, 6.0f, 0xffa04a, 0.35f, -1f, insidePower: false),
            new(15.7f, 12.6f, 1.6f, 6.0f, 0xffa04a, 0.25f, 1f, insidePower: false),
            new(2.1f, -4.7f, 1.2f, 4.0f, 0xffa25a, 0.35f, -1f, insidePower: false),
        };

        const string WeatherChildName = "WeatherFX";
        [SerializeField] int rainCount = 2400;

        readonly List<(GameObject go, bool insidePower)> streakObjects = new();
        readonly List<Renderer> coneRenderers = new();
        readonly List<UnityEngine.Object> ownedAssets = new();
        MeshFilter rainFilter;
        Mesh rainMesh;
        int builtRainCount = -1;

        public int RainCount => rainCount;

        void OnEnable()
        {
            if (transform.Find(WeatherChildName) == null) Rebuild();
            else RebindExisting();
        }

        void OnDestroy() => ReleaseOwned();

        public void Rebuild()
        {
            ReleaseOwned();
            streakObjects.Clear();
            coneRenderers.Clear();

            var old = transform.Find(WeatherChildName);
            if (old) DestroySafe(old.gameObject);

            var root = new GameObject(WeatherChildName);
            root.transform.SetParent(transform, false);

            var streakTex = BuildStreakTexture();
            var unlit = Shader.Find("Universal Render Pipeline/Unlit");
            for (int i = 0; i < StreakSpecs.Length; i++)
            {
                var spec = StreakSpecs[i];
                var mat = new Material(unlit)
                {
                    name = $"WetStreak_{i}",
                    mainTexture = streakTex,
                    renderQueue = 3000,
                };
                var col = HexGamma(spec.ColorHex);
                col.a = spec.Opacity;
                mat.SetColor("_BaseColor", col);
                mat.SetFloat("_Surface", 1f);
                mat.SetFloat("_Blend", 2f); // Additive
                mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
                mat.SetFloat("_ZWrite", 0f);
                mat.SetFloat("_Cull", 0f);
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                ownedAssets.Add(mat);

                var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                quad.name = $"WetStreak_{i}";
                var colComp = quad.GetComponent<Collider>();
                if (colComp) DestroySafe(colComp);
                quad.transform.SetParent(root.transform, false);
                quad.transform.localPosition = ThreeCoords.ToUnity(spec.X, 0.012f, spec.Z + spec.Dir * spec.Length * 0.5f);
                quad.transform.localRotation = Quaternion.Euler(90f, spec.Dir > 0f ? 180f : 0f, 0f);
                quad.transform.localScale = new Vector3(spec.Width, spec.Length, 1f);
                var mr = quad.GetComponent<MeshRenderer>();
                mr.sharedMaterial = mat;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                streakObjects.Add((quad, spec.InsidePower));
            }

            var coneShader = Shader.Find("Universal Render Pipeline/HanaMart/LampCone");
            if (coneShader)
            {
                var coneMat = new Material(coneShader) { name = "LampConeMat", renderQueue = 3006 };
                coneMat.SetColor("_Color", HexGamma(0xffa04a));
                coneMat.SetFloat("_Opacity", 0.22f);
                ownedAssets.Add(coneMat);
                foreach (var marker in GetComponentsInChildren<StoreMarker>(true))
                {
                    if (marker.kind != StoreMarker.Kind.ShaderMesh || !marker.key.StartsWith("S:cones[", StringComparison.Ordinal))
                        continue;
                    if (marker.TryGetComponent<MeshRenderer>(out var mr))
                    {
                        mr.sharedMaterial = coneMat;
                        mr.enabled = true;
                        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                        coneRenderers.Add(mr);
                    }
                }
            }

            var rainGo = new GameObject("Rain");
            rainGo.transform.SetParent(root.transform, false);
            rainFilter = rainGo.AddComponent<MeshFilter>();
            var rainMr = rainGo.AddComponent<MeshRenderer>();
            rainMr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var rainShader = Shader.Find("Universal Render Pipeline/HanaMart/RainStreaks");
            if (rainShader)
            {
                var rainMat = new Material(rainShader) { name = "RainStreaksMat", renderQueue = 3005 };
                rainMat.SetColor("_Color", new Color(0.78f, 0.85f, 1.0f, 1.0f));
                rainMat.SetFloat("_Opacity", 0.22f);
                ownedAssets.Add(rainMat);
                rainMr.sharedMaterial = rainMat;
            }
            ApplyRainCount(rainCount);
        }

        void RebindExisting()
        {
            streakObjects.Clear();
            coneRenderers.Clear();
            var root = transform.Find(WeatherChildName);
            if (root)
            {
                for (int i = 0; i < StreakSpecs.Length; i++)
                {
                    var t = root.Find($"WetStreak_{i}");
                    if (t) streakObjects.Add((t.gameObject, StreakSpecs[i].InsidePower));
                }
                var rain = root.Find("Rain");
                if (rain) rainFilter = rain.GetComponent<MeshFilter>();
            }
            foreach (var marker in GetComponentsInChildren<StoreMarker>(true))
            {
                if (marker.kind == StoreMarker.Kind.ShaderMesh && marker.key.StartsWith("S:cones[", StringComparison.Ordinal) &&
                    marker.TryGetComponent<MeshRenderer>(out var mr))
                {
                    coneRenderers.Add(mr);
                }
            }
        }

        public void ApplyRainCount(int count)
        {
            rainCount = Mathf.Clamp(count, 0, 6000);
            if (!rainFilter) return;
            if (builtRainCount == rainCount && rainMesh != null) return;
            if (rainMesh)
            {
                ownedAssets.Remove(rainMesh);
                DestroySafe(rainMesh);
                rainMesh = null;
            }
            rainMesh = BuildRainMesh(rainCount);
            ownedAssets.Add(rainMesh);
            rainFilter.sharedMesh = rainMesh;
            builtRainCount = rainCount;
        }

        public void SetPower(bool inside, bool outside)
        {
            foreach (var (go, insidePower) in streakObjects)
                if (go) go.SetActive(insidePower ? inside : outside);
            foreach (var mr in coneRenderers)
                if (mr) mr.enabled = outside;
        }

        public static Mesh BuildRainMesh(int count)
        {
            count = Mathf.Clamp(count, 0, 6000);
            var mesh = new Mesh { name = "HanaMart Rain" };
            if (count == 0) return mesh;

            var rng = new System.Random(20261125);
            var verts = new Vector3[count * 4];
            var norms = new Vector3[count * 4];
            var uvs = new Vector2[count * 4];
            var indices = new int[count * 6];

            for (int i = 0; i < count; i++)
            {
                bool front = rng.NextDouble() < 0.78;
                float x = front ? -10f + (float)rng.NextDouble() * 32f : -1.5f + (float)rng.NextDouble() * 10f;
                float z = front ? 8.3f + (float)rng.NextDouble() * 13.5f : -9.2f + (float)rng.NextDouble() * 4.5f;
                float baseY = (float)(rng.NextDouble() * 9.0);
                var p = ThreeCoords.ToUnity(x, 0f, z);

                int v = i * 4;
                verts[v + 0] = p;
                verts[v + 1] = p;
                verts[v + 2] = p;
                verts[v + 3] = p;

                norms[v + 0] = new Vector3(-1f, 0f, 0f);
                norms[v + 1] = new Vector3(1f, 0f, 0f);
                norms[v + 2] = new Vector3(-1f, 0f, 0f);
                norms[v + 3] = new Vector3(1f, 0f, 0f);

                uvs[v + 0] = new Vector2(0f, baseY);
                uvs[v + 1] = new Vector2(0f, baseY);
                uvs[v + 2] = new Vector2(1f, baseY);
                uvs[v + 3] = new Vector2(1f, baseY);

                int idx = i * 6;
                indices[idx + 0] = v + 0;
                indices[idx + 1] = v + 2;
                indices[idx + 2] = v + 1;
                indices[idx + 3] = v + 1;
                indices[idx + 4] = v + 2;
                indices[idx + 5] = v + 3;
            }

            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(verts);
            mesh.SetNormals(norms);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(indices, 0, false);
            mesh.bounds = new Bounds(new Vector3(-6f, 4.5f, 6f), new Vector3(45f, 12f, 40f));
            return mesh;
        }

        Texture2D BuildStreakTexture()
        {
            const int w = 128, h = 256;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                name = "WetStreakTex",
                wrapMode = TextureWrapMode.Clamp,
            };
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
            {
                float k = Mathf.Pow(1f - y / (float)h, 1.6f);
                float br = 0.55f + 0.45f * Mathf.Sin(y * 0.55f) * Mathf.Sin(y * 0.13f + 1f);
                float aRow = Mathf.Clamp01(k * br);
                for (int x = 0; x < w; x++)
                {
                    float u = x / (float)(w - 1);
                    float center = 1f - Mathf.Abs(u * 2f - 1f);
                    float a = Mathf.Clamp01(aRow * Mathf.Pow(center, 1.4f));
                    px[y * w + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
            }
            tex.SetPixels32(px);
            tex.Apply();
            ownedAssets.Add(tex);
            return tex;
        }

        void ReleaseOwned()
        {
            for (int i = 0; i < ownedAssets.Count; i++)
                if (ownedAssets[i]) DestroySafe(ownedAssets[i]);
            ownedAssets.Clear();
            rainMesh = null;
            builtRainCount = -1;
        }

        static void DestroySafe(UnityEngine.Object obj)
        {
            if (!obj) return;
            if (Application.isPlaying) Destroy(obj);
            else DestroyImmediate(obj);
        }

        static Color HexGamma(int rgb) => new Color32((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb, 255);
    }
}
