using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using HanaMart.Store;

namespace HanaMart.EditorTools
{
    /// Tách các món hàng đơn lẻ từ asset siêu thị (GLB) thành prefab + ProductCatalog,
    /// căn mặt in ảnh quay ra trước (+Z) và dựng thẳng đứng (+dP/dv -> +Y), đồng thời chọn SKU đa dạng màu sắc.
    public static class ProductExtractor
    {
        public const string GlbPath = "Assets/Art/Model/super-market-for-free/source/магазин.glb";
        public const string PrefabDir = "Assets/HanaMart/Art/Prefabs/Products";
        public const string MatDir = "Assets/HanaMart/Art/Materials/Products";
        public const string CatalogPath = "Assets/HanaMart/Data/ProductCatalog.asset";
        const int PerCategory = 16;

        // material trong asset → nhóm
        static readonly Dictionary<string, string> Categories = new Dictionary<string, string>
        {
            { "Material.009", "ramen" },
            { "Material.008", "chips" },
            { "Material.007", "candy" },
            { "суши", "sushi" },
            { "Material.005", "mandu" },
            { "Material.011", "meat" },
            { "Material.006", "greens" },
            { "Material.010", "fruit" },
            { "Material.012", "fresh" },
            { "быт", "household" },
            { "БЫТ2", "household2" },
        };

        // loại hàng bản Three.js + nhóm bổ sung → (nhóm, tên hiển thị)
        static readonly (string type, string cat, string name)[] Mapping =
        {
            ("ramenCup", "ramen", "Mì ly"),
            ("ramenBag", "ramen", "Mì gói"),
            ("chips", "chips", "Snack"),
            ("candy", "candy", "Kẹo dẻo"),
            ("kimbap", "sushi", "Cơm cuộn"),
            ("lunch", "sushi", "Cơm hộp"),
            ("banana", "mandu", "Bánh mandu"),
            ("can", "meat", "Thịt"),
            ("bottle", "greens", "Rau"),
            ("coffee", "fruit", "Trái cây"),
            ("soju", "fresh", "Đồ tươi"),
            ("household", "household", "Đồ dùng"),
            ("household2", "household2", "Vệ sinh"),
        };

        struct Candidate
        {
            public MeshRenderer renderer;
            public Matrix4x4 matrix;
            public Vector3 front;
            public Vector3 dPdv;
            public Rect uvRect;
            public Color color;
            public float logVolDiff;
        }

        [MenuItem("HanaMart/Extract Products")]
        public static void Run()
        {
            Directory.CreateDirectory(PrefabDir);
            Directory.CreateDirectory(MatDir);
            Directory.CreateDirectory(Path.GetDirectoryName(CatalogPath));

            var src = AssetDatabase.LoadAssetAtPath<GameObject>(GlbPath);
            var byCat = new Dictionary<string, List<MeshRenderer>>();
            foreach (var r in src.GetComponentsInChildren<MeshRenderer>(true))
            {
                var m = r.sharedMaterial;
                if (m == null || !Categories.TryGetValue(m.name, out var c)) continue;
                if (!byCat.TryGetValue(c, out var l)) byCat[c] = l = new List<MeshRenderer>();
                l.Add(r);
            }

            var prefabs = new Dictionary<string, List<GameObject>>();
            var catColors = new Dictionary<string, List<Color>>();

            foreach (var kv in byCat)
            {
                var mat = CategoryMaterial(kv.Key, kv.Value[0].sharedMaterial);
                var readableTex = LoadReadableTexture(mat.GetTexture("_BaseMap"));

                // Ưu tiên thư viện mẫu đơn lẻ chuẩn ở vùng trên cao của GLB (y > 15) thay vì cụm kệ ghép
                var templates = kv.Value.Where(r => r.bounds.center.y > 15f).ToList();
                var thickTemplates = templates.Where(r =>
                {
                    var s = r.bounds.size;
                    return Mathf.Min(s.x, Mathf.Min(s.y, s.z)) >= 0.07f;
                }).ToList();
                var pool = thickTemplates.Count >= 3 ? thickTemplates : (templates.Count >= 3 ? templates : kv.Value);

                var vols = pool.Select(r => r.bounds.size.x * r.bounds.size.y * r.bounds.size.z).OrderBy(v => v).ToList();
                float med = Mathf.Max(vols[vols.Count / 2], 1e-6f);

                // Phân tích hình học, mặt nhãn, UV rect và màu đại diện cho từng mesh đơn lẻ
                var candidates = new List<Candidate>();
                foreach (var r in pool)
                {
                    float vol = r.bounds.size.x * r.bounds.size.y * r.bounds.size.z;
                    if (vol < med * 0.2f || vol > med * 5.0f) continue;

                    var mf = r.GetComponent<MeshFilter>();
                    if (mf == null || mf.sharedMesh == null) continue;

                    Matrix4x4 M = src.transform.worldToLocalMatrix * r.transform.localToWorldMatrix;
                    var (front, dPdv, uvRect) = AnalyzeFrontFace(mf.sharedMesh, M);
                    Color col = readableTex != null ? SampleDominantColor(readableTex, uvRect) : Color.gray;

                    candidates.Add(new Candidate
                    {
                        renderer = r,
                        matrix = M,
                        front = front,
                        dPdv = dPdv,
                        uvRect = uvRect,
                        color = col,
                        logVolDiff = Mathf.Abs(Mathf.Log(Mathf.Max(vol, 1e-6f) / med)),
                    });
                }

                if (candidates.Count == 0)
                {
                    foreach (var r in kv.Value.Take(PerCategory))
                    {
                        var mf = r.GetComponent<MeshFilter>();
                        Matrix4x4 M = src.transform.worldToLocalMatrix * r.transform.localToWorldMatrix;
                        var (front, dPdv, uvRect) = AnalyzeFrontFace(mf.sharedMesh, M);
                        candidates.Add(new Candidate
                        {
                            renderer = r, matrix = M, front = front, dPdv = dPdv,
                            uvRect = uvRect, color = Color.gray, logVolDiff = 0f
                        });
                    }
                }

                // Khử trùng lặp theo ô UV trên atlas (giữ món gần kích thước trung vị nhất mỗi ô)
                var uniqueByUv = candidates
                    .GroupBy(c => (
                        Mathf.RoundToInt(Mathf.Repeat(c.uvRect.center.x, 1f) * 20f),
                        Mathf.RoundToInt(Mathf.Repeat(c.uvRect.center.y, 1f) * 20f)))
                    .Select(g => g.OrderBy(c => c.logVolDiff).First())
                    .ToList();

                if (uniqueByUv.Count < Mathf.Min(4, candidates.Count))
                    uniqueByUv = candidates;

                // Chọn tối đa PerCategory SKU bằng farthest-point sampling theo khoảng cách màu
                var picked = PickColorDiverse(uniqueByUv, PerCategory);

                var list = new List<GameObject>();
                var colors = new List<Color>();
                for (int n = 0; n < picked.Count; n++)
                {
                    var c = picked[n];
                    list.Add(MakePrefab(c.renderer, c.matrix, c.front, c.dPdv, mat, $"Product_{kv.Key}_{n}"));
                    colors.Add(c.color);
                }

                prefabs[kv.Key] = list;
                catColors[kv.Key] = colors;

                if (readableTex != null) Object.DestroyImmediate(readableTex);
            }

            var cat = AssetDatabase.LoadAssetAtPath<ProductCatalog>(CatalogPath);
            if (cat == null)
            {
                cat = ScriptableObject.CreateInstance<ProductCatalog>();
                AssetDatabase.CreateAsset(cat, CatalogPath);
            }

            cat.entries = Mapping
                .Where(m => prefabs.ContainsKey(m.cat))
                .Select(m => new ProductCatalog.Entry
                {
                    threeType = m.type,
                    displayName = m.name,
                    prefabs = prefabs[m.cat].ToArray(),
                    colors = catColors[m.cat].ToArray(),
                })
                .ToArray();

            EditorUtility.SetDirty(cat);
            AssetDatabase.SaveAssets();
        }

        static List<Candidate> PickColorDiverse(List<Candidate> pool, int maxCount)
        {
            var result = new List<Candidate>();
            if (pool.Count == 0) return result;

            var remaining = pool.OrderBy(c => c.logVolDiff).ToList();
            result.Add(remaining[0]);
            remaining.RemoveAt(0);

            while (result.Count < maxCount && remaining.Count > 0)
            {
                int bestIdx = 0;
                float bestScore = float.NegativeInfinity;
                for (int i = 0; i < remaining.Count; i++)
                {
                    float minDist = float.MaxValue;
                    for (int j = 0; j < result.Count; j++)
                        minDist = Mathf.Min(minDist, ProductPlanogram.ColorDistance(remaining[i].color, result[j].color));

                    float score = minDist - remaining[i].logVolDiff * 0.05f;
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestIdx = i;
                    }
                }
                result.Add(remaining[bestIdx]);
                remaining.RemoveAt(bestIdx);
            }
            return result;
        }

        static Texture2D LoadReadableTexture(Texture tex)
        {
            if (tex == null) return null;
            int w = Mathf.Clamp(tex.width, 64, 512);
            int h = Mathf.Clamp(tex.height, 64, 512);
            var rt = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var prev = RenderTexture.active;
            Graphics.Blit(tex, rt);
            RenderTexture.active = rt;
            var copy = new Texture2D(w, h, TextureFormat.RGBA32, false);
            copy.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            copy.Apply();
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);
            return copy;
        }

        static Color SampleDominantColor(Texture2D tex, Rect uvRect)
        {
            float sumR = 0f, sumG = 0f, sumB = 0f, sumW = 0f;
            const int grid = 6;
            for (int iy = 0; iy < grid; iy++)
            {
                float v = Mathf.Lerp(uvRect.yMin, uvRect.yMax, (iy + 0.5f) / grid);
                v = Mathf.Repeat(v, 1f);
                for (int ix = 0; ix < grid; ix++)
                {
                    float u = Mathf.Lerp(uvRect.xMin, uvRect.xMax, (ix + 0.5f) / grid);
                    u = Mathf.Repeat(u, 1f);
                    Color c = tex.GetPixelBilinear(u, v);
                    float maxC = Mathf.Max(c.r, Mathf.Max(c.g, c.b));
                    float minC = Mathf.Min(c.r, Mathf.Min(c.g, c.b));
                    float sat = maxC - minC;
                    float w = sat * 3f + 0.08f;
                    sumR += c.r * w;
                    sumG += c.g * w;
                    sumB += c.b * w;
                    sumW += w;
                }
            }
            return sumW > 0f ? new Color(sumR / sumW, sumG / sumW, sumB / sumW, 1f) : Color.gray;
        }

        static Material CategoryMaterial(string cat, Material srcMat)
        {
            string path = $"{MatDir}/{cat}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) { mat = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(mat, path); }
            mat.SetTexture("_BaseMap", srcMat.HasProperty("baseColorTexture") ? srcMat.GetTexture("baseColorTexture") : srcMat.mainTexture);
            mat.SetColor("_BaseColor", Color.white); mat.SetFloat("_Smoothness", 0.35f); mat.SetFloat("_Metallic", 0f);
            mat.enableInstancing = true; EditorUtility.SetDirty(mat);
            return mat;
        }

        static GameObject MakePrefab(MeshRenderer r, Matrix4x4 M, Vector3 front, Vector3 dPdv, Material mat, string name)
        {
            var mesh = r.GetComponent<MeshFilter>().sharedMesh;
            var root = new GameObject(name);
            var child = new GameObject("mesh");
            child.transform.SetParent(root.transform, false);
            child.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = child.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;

            child.transform.localRotation = ProductPlanogram.ComputeUprightAlignment(front, dPdv) * M.rotation;
            child.transform.localScale = M.lossyScale;

            var b = ChildBounds(root);
            child.transform.localPosition = new Vector3(-b.center.x, -b.min.y, -b.center.z);

            string path = $"{PrefabDir}/{name}.prefab";
            var p = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return p;
        }

        /// Tìm mặt chính in nhãn (diện tích UV * hình học lớn nhất), vector dP/dv trên mặt đó, và vùng UV của mặt.
        static (Vector3 front, Vector3 dPdv, Rect uvRect) AnalyzeFrontFace(Mesh mesh, Matrix4x4 M)
        {
            var v = mesh.vertices;
            var n = mesh.normals;
            var uv = mesh.uv;
            var tri = mesh.triangles;

            var area = new Dictionary<Vector3, float>();
            var dPdvSum = new Dictionary<Vector3, Vector3>();
            var uvMin = new Dictionary<Vector3, Vector2>();
            var uvMax = new Dictionary<Vector3, Vector2>();

            for (int t = 0; t < tri.Length; t += 3)
            {
                int i0 = tri[t], i1 = tri[t + 1], i2 = tri[t + 2];
                Vector3 nn = M.MultiplyVector(n[i0]).normalized;
                int ax = Mathf.Abs(nn.x) > Mathf.Abs(nn.y)
                    ? (Mathf.Abs(nn.x) > Mathf.Abs(nn.z) ? 0 : 2)
                    : (Mathf.Abs(nn.y) > Mathf.Abs(nn.z) ? 1 : 2);
                var dir = Vector3.zero;
                dir[ax] = Mathf.Sign(nn[ax]);

                Vector2 a = uv[i0], b = uv[i1], c = uv[i2];
                float uvA = Mathf.Abs((b.x - a.x) * (c.y - a.y) - (c.x - a.x) * (b.y - a.y)) * 0.5f;
                Vector3 p0 = M.MultiplyPoint3x4(v[i0]);
                Vector3 p1 = M.MultiplyPoint3x4(v[i1]);
                Vector3 p2 = M.MultiplyPoint3x4(v[i2]);
                float geoA = Vector3.Cross(p1 - p0, p2 - p0).magnitude * 0.5f;
                float ar = uvA * geoA;

                area[dir] = (area.TryGetValue(dir, out var x) ? x : 0f) + ar;

                Vector3 dp1 = p1 - p0, dp2 = p2 - p0;
                float du1 = b.x - a.x, dv1 = b.y - a.y;
                float du2 = c.x - a.x, dv2 = c.y - a.y;
                float det = du1 * dv2 - du2 * dv1;
                if (Mathf.Abs(det) > 1e-8f)
                {
                    Vector3 dPdvTri = (du1 * dp2 - du2 * dp1) / det;
                    if (dPdvTri.sqrMagnitude > 1e-8f)
                    {
                        dPdvSum[dir] = (dPdvSum.TryGetValue(dir, out var s) ? s : Vector3.zero)
                                       + dPdvTri.normalized * ar;
                    }
                }

                Vector2 triMin = Vector2.Min(a, Vector2.Min(b, c));
                Vector2 triMax = Vector2.Max(a, Vector2.Max(b, c));
                uvMin[dir] = uvMin.TryGetValue(dir, out var curMin) ? Vector2.Min(curMin, triMin) : triMin;
                uvMax[dir] = uvMax.TryGetValue(dir, out var curMax) ? Vector2.Max(curMax, triMax) : triMax;
            }

            if (area.Count == 0)
                return (Vector3.forward, Vector3.up, new Rect(0, 0, 1, 1));

            Vector3 bestDir = area.OrderByDescending(kv => kv.Value).First().Key;
            Vector3 dPdv = dPdvSum.TryGetValue(bestDir, out var sum) && sum.sqrMagnitude > 1e-6f
                ? sum.normalized
                : Vector3.up;
            Vector2 mn = uvMin.TryGetValue(bestDir, out var mnv) ? mnv : Vector2.zero;
            Vector2 mx = uvMax.TryGetValue(bestDir, out var mxv) ? mxv : Vector2.one;
            return (bestDir, dPdv, Rect.MinMaxRect(mn.x, mn.y, mx.x, mx.y));
        }

        static Bounds ChildBounds(GameObject root)
        {
            Bounds b = default; bool first = true;
            foreach (var mf in root.GetComponentsInChildren<MeshFilter>())
            {
                var mb = mf.sharedMesh.bounds;
                for (int i = 0; i < 8; i++)
                {
                    var corner = mb.center + Vector3.Scale(mb.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                    var p = root.transform.InverseTransformPoint(mf.transform.TransformPoint(corner));
                    if (first) { b = new Bounds(p, Vector3.zero); first = false; } else b.Encapsulate(p);
                }
            }
            return b;
        }

        /// Kích thước prefab (đơn vị cục bộ) để co vừa ô hàng.
        public static Vector3 PrefabSize(GameObject prefab) => ChildBounds(prefab).size;
    }
}
