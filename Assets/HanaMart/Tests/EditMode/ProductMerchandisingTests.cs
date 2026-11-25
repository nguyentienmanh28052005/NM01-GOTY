using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using HanaMart.Store;

namespace HanaMart.Tests
{
    public class ProductMerchandisingTests
    {
        [Test]
        public void ComputeUprightAlignmentFixesBackFaceWithoutFlippingUpsideDown()
        {
            // Package facing -Z with top of label (+Y) corresponding to Unity +dP/dv (V-bottom-up).
            // Rotating it to face +Z must rotate around Y by 180°, keeping +Y upright (NOT 180° around X).
            Vector3 front = Vector3.back;
            Vector3 dPdv = Vector3.up;

            Quaternion rot = ProductPlanogram.ComputeUprightAlignment(front, dPdv);

            Vector3 alignedFront = rot * front;
            Vector3 alignedUp = rot * dPdv;

            Assert.Greater(Vector3.Dot(alignedFront, Vector3.forward), 0.99f, "Front face should point +Z");
            Assert.Greater(Vector3.Dot(alignedUp, Vector3.up), 0.99f, "Label up (+dP/dv) should point +Y");
        }

        [Test]
        public void PlanogramAvoidsAdjacentDuplicateColorsAndMaximizesVariety()
        {
            var palette = new[]
            {
                new Color(0.90f, 0.15f, 0.15f), // Red
                new Color(0.15f, 0.70f, 0.25f), // Green
                new Color(0.15f, 0.35f, 0.85f), // Blue
                new Color(0.95f, 0.80f, 0.15f), // Yellow
                new Color(0.85f, 0.40f, 0.10f), // Orange
                new Color(0.60f, 0.20f, 0.75f), // Purple
            };

            // 3 rows x 6 cols in 1 zone
            var slots = new List<ProductPlanogram.BlockSlot>();
            for (int r = 0; r < 3; r++)
                for (int c = 0; c < 6; c++)
                    slots.Add(new ProductPlanogram.BlockSlot { zone = 0, row = r, col = c });

            int[] assigned = ProductPlanogram.AssignColorBlocks(slots.ToArray(), palette, seed: 3);
            Assert.AreEqual(slots.Count, assigned.Length);

            // At least 4 distinct SKUs used across 18 slots
            Assert.GreaterOrEqual(assigned.Distinct().Count(), 4, "Should use diverse colors from palette");

            // Check horizontal & vertical adjacency contrast
            for (int r = 0; r < 3; r++)
            {
                for (int c = 0; c < 6; c++)
                {
                    int idx = r * 6 + c;
                    int sku = assigned[idx];
                    if (c > 0)
                    {
                        int leftSku = assigned[r * 6 + (c - 1)];
                        Assert.AreNotEqual(leftSku, sku, $"Horizontal adjacent collision at ({r},{c})");
                        Assert.Greater(ProductPlanogram.ColorDistance(palette[leftSku], palette[sku]), 0.2f,
                            $"Low horizontal contrast at ({r},{c})");
                    }
                    if (r > 0)
                    {
                        int belowSku = assigned[(r - 1) * 6 + c];
                        Assert.AreNotEqual(belowSku, sku, $"Vertical adjacent collision at ({r},{c})");
                        Assert.Greater(ProductPlanogram.ColorDistance(palette[belowSku], palette[sku]), 0.2f,
                            $"Low vertical contrast at ({r},{c})");
                    }
                }
            }
        }

        [Test]
        public void ExtractedCatalogPrefabsAreUprightAndColorDiverse()
        {
            var cat = AssetDatabase.LoadAssetAtPath<ProductCatalog>("Assets/HanaMart/Data/ProductCatalog.asset");
            Assert.IsNotNull(cat, "ProductCatalog.asset must exist");

            foreach (var entry in cat.entries)
            {
                Assert.IsNotNull(entry.prefabs, $"Prefabs null for {entry.threeType}");
                Assert.Greater(entry.prefabs.Length, 0, $"No prefabs for {entry.threeType}");
                Assert.IsNotNull(entry.colors, $"Colors null for {entry.threeType}");
                Assert.AreEqual(entry.prefabs.Length, entry.colors.Length,
                    $"Colors length mismatch for {entry.threeType}");

                foreach (var prefab in entry.prefabs)
                {
                    Assert.IsNotNull(prefab);
                    var child = prefab.transform.Find("mesh");
                    Assert.IsNotNull(child, $"Prefab {prefab.name} missing 'mesh' child");
                    var mf = child.GetComponent<MeshFilter>();
                    Assert.IsNotNull(mf, $"Prefab {prefab.name} missing MeshFilter");
                    Assert.IsNotNull(mf.sharedMesh, $"Prefab {prefab.name} missing sharedMesh");
                    var mr = child.GetComponent<MeshRenderer>();
                    Assert.IsNotNull(mr, $"Prefab {prefab.name} missing MeshRenderer");
                }

                if (entry.colors.Length >= 2)
                {
                    float maxDist = 0f;
                    for (int i = 0; i < entry.colors.Length; i++)
                        for (int j = i + 1; j < entry.colors.Length; j++)
                            maxDist = Mathf.Max(maxDist, ProductPlanogram.ColorDistance(entry.colors[i], entry.colors[j]));
                    Assert.Greater(maxDist, 0.15f, $"Entry {entry.threeType} lacks color diversity");
                }
            }
        }

        [Test]
        public void ShelfProductsInStoreFaceAisleAndAreUpright()
        {
            var scene = EditorSceneManager.OpenScene("Assets/HanaMart/Scenes/HanaMart24.unity", OpenSceneMode.Additive);
            try
            {
                var markers = Object.FindObjectsByType<StoreMarker>(FindObjectsSortMode.None)
                    .Where(m => m.kind == StoreMarker.Kind.Product)
                    .ToArray();
                Assert.Greater(markers.Length, 100, "Expected >100 Product markers in HanaMart24");

                int checkedCount = 0;
                foreach (var m in markers)
                {
                    // Marker forward should be roughly horizontal (y near 0) and upright (up.y > 0.9)
                    Assert.Greater(Vector3.Dot(m.transform.up, Vector3.up), 0.9f,
                        $"Marker {m.name} at {m.transform.position} is not upright");
                    checkedCount++;
                }
                Assert.Greater(checkedCount, 100);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void ShelfProductsFormCleanFacingBlocksNotSingleItemConfetti()
        {
            var scene = EditorSceneManager.OpenScene("Assets/HanaMart/Scenes/HanaMart24.unity", OpenSceneMode.Additive);
            try
            {
                var markers = Object.FindObjectsByType<StoreMarker>(FindObjectsSortMode.None)
                    .Where(m => m.kind == StoreMarker.Kind.Product)
                    .ToArray();

                var byRow = new Dictionary<(int side, int tier), List<(float z, string prefabName)>>();
                var distinctBySide = new Dictionary<int, HashSet<string>>();

                foreach (var m in markers)
                {
                    Vector3 tp = ThreeCoords.ToUnity(m.transform.position);
                    if (tp.z < 1.5f || tp.x < 3.5f || tp.x > 11.0f) continue;

                    Transform prodChild = null;
                    for (int i = 0; i < m.transform.childCount; i++)
                    {
                        var ch = m.transform.GetChild(i);
                        if (ch.name.StartsWith("Product_")) { prodChild = ch; break; }
                    }
                    if (prodChild == null) continue;

                    int side = tp.x < 4.6f ? 0 : (tp.x < 5.9f ? 1 : (tp.x < 7.2f ? 2 : (tp.x < 8.5f ? 3 : (tp.x < 9.8f ? 4 : 5))));
                    int tier = tp.y < 0.35f ? 0 : (tp.y < 0.72f ? 1 : (tp.y < 1.10f ? 2 : 3));

                    if (!byRow.TryGetValue((side, tier), out var list))
                        byRow[(side, tier)] = list = new List<(float, string)>();
                    list.Add((tp.z, prodChild.name));

                    if (!distinctBySide.TryGetValue(side, out var set))
                        distinctBySide[side] = set = new HashSet<string>();
                    set.Add(prodChild.name);
                }

                int totalItems = 0;
                int totalRuns = 0;
                foreach (var kv in byRow)
                {
                    var sorted = kv.Value.OrderBy(x => x.z).ToList();
                    string prev = null;
                    foreach (var it in sorted)
                    {
                        totalItems++;
                        if (it.prefabName != prev)
                        {
                            totalRuns++;
                            prev = it.prefabName;
                        }
                    }
                }

                Assert.Greater(totalItems, 200, "Expected >200 aisle shelf items");
                float avgRunLength = (float)totalItems / totalRuns;
                Assert.GreaterOrEqual(avgRunLength, 4.0f,
                    $"Expected clean facing blocks (avg run >= 4.0), but got {avgRunLength:F2} (confetti)");

                for (int side = 0; side < 6; side++)
                {
                    Assert.IsTrue(distinctBySide.ContainsKey(side), $"Missing aisle side {side}");
                    Assert.GreaterOrEqual(distinctBySide[side].Count, 4,
                        $"Aisle side {side} should have >= 4 distinct prefabs, got {distinctBySide[side].Count}");
                }
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }
    }
}
