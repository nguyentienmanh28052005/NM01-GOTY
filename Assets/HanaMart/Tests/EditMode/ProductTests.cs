using System.IO;
using System.Linq;
using HanaMart.EditorTools;
using HanaMart.Store;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HanaMart.Tests
{
    public class ProductTests
    {
        Scene scene; GameObject root; JObject meta; StoreMarker[] products;

        [OneTimeSetUp]
        public void Setup()
        {
            scene = EditorSceneManager.OpenScene(StoreImporter.ScenePath, OpenSceneMode.Additive);
            root = StoreImporter.Build(scene);
            meta = JObject.Parse(File.ReadAllText(StoreImporter.MetaPath));
            products = root.GetComponentsInChildren<StoreMarker>(true).Where(m => m.kind == StoreMarker.Kind.Product).ToArray();
        }

        [OneTimeTearDown]
        public void TearDown() { if (scene.IsValid()) EditorSceneManager.CloseScene(scene, true); }

        static string TypeOf(StoreMarker m) => (string)JObject.Parse(m.json)["type"];

        [Test] public void CatalogCoversAllTypes()
        {
            var cat = AssetDatabase.LoadAssetAtPath<ProductCatalog>(ProductExtractor.CatalogPath);
            Assert.IsNotNull(cat);
            foreach (var t in ((JObject)meta["geos"]).Properties().Select(p => p.Name).Where(t => t != "cig"))
            {
                var e = cat.Find(t);
                Assert.IsNotNull(e, t);
                Assert.GreaterOrEqual(e.prefabs.Length, 3, t);
                Assert.IsFalse(string.IsNullOrEmpty(e.displayName), t);
            }
        }

        [Test] public void CountsMatchMeta() => Assert.AreEqual((int)meta["counts"]["productInstances"], products.Length);

        [Test] public void EveryProductNodeReplaced()
        {
            foreach (var m in products)
            {
                bool cig = TypeOf(m) == "cig";
                Assert.AreEqual(cig, m.GetComponent<MeshRenderer>() != null && m.GetComponent<MeshRenderer>().enabled, m.key);
                if (!cig) Assert.AreEqual(1, m.transform.Cast<Transform>().Count(c => c.name.StartsWith("Product_")), m.key);
            }
        }

        [Test] public void FitsThreeSlot()
        {
            foreach (var m in products.Where(p => TypeOf(p) != "cig"))
            {
                var ex = JObject.Parse(m.json);
                var slot = new Vector3((float)ex["w"], (float)ex["h"], (float)ex["d"]) * 1.05f;
                var b = new Bounds(); bool first = true;
                foreach (var mf in m.GetComponentsInChildren<MeshFilter>())
                    foreach (var v in mf.sharedMesh.bounds.Corners())
                    {
                        var p = m.transform.InverseTransformPoint(mf.transform.TransformPoint(v));
                        if (first) { b = new Bounds(p, Vector3.zero); first = false; } else b.Encapsulate(p);
                    }
                Assert.LessOrEqual(b.size.x, slot.x, m.key); Assert.LessOrEqual(b.size.y, slot.y, m.key); Assert.LessOrEqual(b.size.z, slot.z, m.key);
            }
        }

        [Test] public void RestockGroupsKept()
        {
            Assert.Greater(products.Count(m => m.json.Contains("\"shelfRamen\"")), 0);
            Assert.Greater(products.Count(m => m.json.Contains("\"shelfMilk\"")), 0);
        }
    }

    static class BoundsExt
    {
        public static Vector3[] Corners(this Bounds b)
        {
            var c = new Vector3[8]; int i = 0;
            for (int x = -1; x <= 1; x += 2) for (int y = -1; y <= 1; y += 2) for (int z = -1; z <= 1; z += 2)
                c[i++] = b.center + Vector3.Scale(b.extents, new Vector3(x, y, z));
            return c;
        }
    }
}
