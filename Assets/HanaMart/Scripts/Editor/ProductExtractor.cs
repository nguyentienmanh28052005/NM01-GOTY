using System.Collections.Generic;
using System.IO;
using System.Linq;
using HanaMart.Store;
using UnityEditor;
using UnityEngine;

namespace HanaMart.EditorTools
{
    /// Tạo prefab hàng hóa từ asset siêu thị (магазин.glb): mỗi khối sản phẩm → prefab, mặt có ảnh quay về +Z, gốc ở đáy-giữa.
    public static class ProductExtractor
    {
        public const string CatalogPath = "Assets/HanaMart/Data/ProductCatalog.asset";
        const string GlbPath = "Assets/Art/Model/super-market-for-free/source/магазин.glb";
        const string PrefabDir = "Assets/HanaMart/Art/Prefabs/Products";
        const string MatDir = "Assets/HanaMart/Art/Materials/Products";
        const int PerCategory = 8;

        // material trong asset → nhóm
        static readonly Dictionary<string, string> Categories = new Dictionary<string, string>
        {
            { "Material.009", "ramen" }, { "Material.008", "chips" }, { "суши", "sushi" }, { "Material.005", "mandu" },
            { "Material.011", "meat" }, { "Material.006", "greens" }, { "Material.010", "fruit" }, { "Material.012", "fresh" },
        };

        // loại hàng bản Three.js → (nhóm, tên hiển thị) — spec mục 6.5 + Q1
        static readonly (string type, string cat, string name)[] Mapping =
        {
            ("ramenCup", "ramen", "Mì ly"), ("ramenBag", "ramen", "Mì gói"), ("chips", "chips", "Snack"),
            ("kimbap", "sushi", "Cơm cuộn"), ("lunch", "sushi", "Cơm hộp"), ("banana", "mandu", "Bánh mandu"),
            ("can", "meat", "Thịt"), ("bottle", "greens", "Rau"), ("coffee", "fruit", "Trái cây"), ("soju", "fresh", "Đồ tươi"),
        };

        [MenuItem("HanaMart/Extract Products")]
        public static void Run()
        {
            Directory.CreateDirectory(PrefabDir); Directory.CreateDirectory(MatDir); Directory.CreateDirectory(Path.GetDirectoryName(CatalogPath));
            var src = AssetDatabase.LoadAssetAtPath<GameObject>(GlbPath);
            var byCat = new Dictionary<string, List<MeshRenderer>>();
            foreach (var r in src.GetComponentsInChildren<MeshRenderer>(true))
            {
                var m = r.sharedMaterial; if (m == null || !Categories.TryGetValue(m.name, out var c)) continue;
                if (!byCat.TryGetValue(c, out var l)) byCat[c] = l = new List<MeshRenderer>();
                l.Add(r);
            }
            var prefabs = new Dictionary<string, List<GameObject>>();
            foreach (var kv in byCat)
            {
                var mat = CategoryMaterial(kv.Key, kv.Value[0].sharedMaterial);
                var list = new List<GameObject>();
                // chọn các khối có kích thước gần trung vị nhất (một món đơn lẻ, không phải mảng nhiều món)
                var vols = kv.Value.Select(r => r.bounds.size.x * r.bounds.size.y * r.bounds.size.z).OrderBy(v => v).ToList();
                float med = vols[vols.Count / 2];
                var pick = kv.Value.OrderBy(r => Mathf.Abs(Mathf.Log(r.bounds.size.x * r.bounds.size.y * r.bounds.size.z / med))).Take(PerCategory).ToList();
                for (int n = 0; n < pick.Count; n++)
                    list.Add(MakePrefab(src.transform, pick[n], mat, $"Product_{kv.Key}_{n}"));
                prefabs[kv.Key] = list;
            }
            var cat = AssetDatabase.LoadAssetAtPath<ProductCatalog>(CatalogPath);
            if (cat == null) { cat = ScriptableObject.CreateInstance<ProductCatalog>(); AssetDatabase.CreateAsset(cat, CatalogPath); }
            cat.entries = Mapping.Select(m => new ProductCatalog.Entry { threeType = m.type, displayName = m.name, prefabs = prefabs[m.cat].ToArray() }).ToArray();
            EditorUtility.SetDirty(cat); AssetDatabase.SaveAssets();
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

        static GameObject MakePrefab(Transform srcRoot, MeshRenderer r, Material mat, string name)
        {
            var mesh = r.GetComponent<MeshFilter>().sharedMesh;
            Matrix4x4 M = srcRoot.worldToLocalMatrix * r.transform.localToWorldMatrix;
            Vector3 face = TexturedFace(mesh, M);
            var root = new GameObject(name);
            var child = new GameObject("mesh"); child.transform.SetParent(root.transform, false);
            child.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = child.AddComponent<MeshRenderer>(); mr.sharedMaterial = mat;
            child.transform.localRotation = Quaternion.FromToRotation(face, Vector3.forward) * M.rotation;
            child.transform.localScale = M.lossyScale;
            var b = ChildBounds(root);
            child.transform.localPosition = new Vector3(-b.center.x, -b.min.y, -b.center.z);
            string path = $"{PrefabDir}/{name}.prefab";
            var p = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return p;
        }

        /// Trục (±X/±Y/±Z, sau khi xoay/co theo node gốc) có tổng diện tích UV lớn nhất = mặt in ảnh sản phẩm.
        static Vector3 TexturedFace(Mesh mesh, Matrix4x4 M)
        {
            var v = mesh.vertices; var n = mesh.normals; var uv = mesh.uv; var tri = mesh.triangles;
            var area = new Dictionary<Vector3, float>();
            for (int t = 0; t < tri.Length; t += 3)
            {
                Vector3 nn = M.MultiplyVector(n[tri[t]]).normalized;
                int ax = Mathf.Abs(nn.x) > Mathf.Abs(nn.y) ? (Mathf.Abs(nn.x) > Mathf.Abs(nn.z) ? 0 : 2) : (Mathf.Abs(nn.y) > Mathf.Abs(nn.z) ? 1 : 2);
                var dir = Vector3.zero; dir[ax] = Mathf.Sign(nn[ax]);
                Vector2 a = uv[tri[t]], b = uv[tri[t + 1]], c = uv[tri[t + 2]];
                float uvA = Mathf.Abs((b.x - a.x) * (c.y - a.y) - (c.x - a.x) * (b.y - a.y)) * 0.5f;
                Vector3 p0 = M.MultiplyPoint3x4(v[tri[t]]), p1 = M.MultiplyPoint3x4(v[tri[t + 1]]), p2 = M.MultiplyPoint3x4(v[tri[t + 2]]);
                float geoA = Vector3.Cross(p1 - p0, p2 - p0).magnitude * 0.5f;
                float ar = uvA * geoA; // mặt in ảnh: vừa có nhiều UV vừa đủ lớn
                area[dir] = (area.TryGetValue(dir, out var x) ? x : 0) + ar;
            }
            return area.OrderByDescending(kv => kv.Value).First().Key;
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
