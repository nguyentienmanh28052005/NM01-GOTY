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
    public class StoreImportTests
    {
        Scene scene; GameObject root; JObject meta, gltf;

        [OneTimeSetUp]
        public void Setup()
        {
            scene = EditorSceneManager.OpenScene(StoreImporter.ScenePath, OpenSceneMode.Additive);
            root = StoreImporter.Build(scene);
            meta = JObject.Parse(File.ReadAllText(StoreImporter.MetaPath));
            gltf = JObject.Parse(File.ReadAllText(StoreImporter.GltfPath));
        }

        [OneTimeTearDown]
        public void TearDown() { if (scene.IsValid()) EditorSceneManager.CloseScene(scene, true); }

        [Test] public void StoreRootExists()
        {
            Assert.IsNotNull(root);
            Assert.AreEqual("Store", root.name);
            Assert.AreEqual(scene, root.scene);
        }

        [Test] public void CollidersMatchMeta()
        {
            int n = root.GetComponentsInChildren<StoreMarker>(true).Count(m => m.kind == StoreMarker.Kind.Collider && m.GetComponent<BoxCollider>() && !m.GetComponent<BoxCollider>().isTrigger);
            Assert.AreEqual(((JArray)meta["colliders"]).Count, n);
        }

        [Test] public void LightsMatchMeta()
        {
            var lights = root.GetComponentsInChildren<Light>(true);
            Assert.AreEqual(14, lights.Length, "15 đèn Three.js trừ HemisphereLight (thành ánh sáng môi trường)");
            Assert.AreEqual(9, root.GetComponentsInChildren<StoreMarker>(true).Count(m => m.kind == StoreMarker.Kind.Light && m.json.Contains("\"RectArea\"") && m.GetComponentInChildren<Light>(true).type == LightType.Spot));
        }

        [Test] public void TransparentAndEmissiveSurvive()
        {
            var mats = root.GetComponentsInChildren<Renderer>(true).SelectMany(r => r.sharedMaterials).Where(m => m).Distinct().ToList();
            var src = (JArray)gltf["materials"];
            int checkedBlend = 0, checkedEmis = 0;
            foreach (var m in mats)
            {
                var s = src.FirstOrDefault(j => (string)j["name"] == m.name.Split('|')[0]);
                if (s == null) continue;
                if ((string)s["alphaMode"] == "BLEND") { Assert.AreEqual(1f, m.GetFloat("_Surface"), m.name); checkedBlend++; }
                var e = s["emissiveFactor"] as JArray;
                if (e != null && e.Any(v => (float)v > 0) && !m.shader.name.Contains("Unlit")) { Assert.IsTrue(m.IsKeywordEnabled("_EMISSION"), m.name); checkedEmis++; }
            }
            Assert.Greater(checkedBlend, 5); Assert.Greater(checkedEmis, 5);
        }

        [Test] public void NoErrorShader()
        {
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
                foreach (var m in r.sharedMaterials)
                {
                    Assert.IsNotNull(m, r.name);
                    Assert.AreNotEqual("Hidden/InternalErrorShader", m.shader.name, r.name);
                    StringAssert.StartsWith("Universal Render Pipeline/", m.shader.name, r.name);
                }
        }

        [Test] public void ProbeConfigured()
        {
            var p = root.GetComponentsInChildren<ReflectionProbe>(true).Single(x => x.name == "StoreProbe");
            Assert.IsTrue(p.boxProjection);
            Assert.That(Vector3.Distance(p.transform.position, new Vector3(-6.2f, 1.55f, 4f)), Is.LessThan(0.01f));
            Assert.That(Vector3.Distance(p.size, new Vector3(12, 3, 8)), Is.LessThan(0.01f));
            Assert.That(Vector3.Distance(p.transform.position + p.center, new Vector3(-6f, 1.5f, 4f)), Is.LessThan(0.01f));
        }

        [Test] public void RebuildIsIdempotent()
        {
            int before = root.GetComponentsInChildren<Renderer>(true).Length;
            root = StoreImporter.Build(scene);
            Assert.AreEqual(1, scene.GetRootGameObjects().Count(g => g.name == "Store"));
            Assert.AreEqual(before, root.GetComponentsInChildren<Renderer>(true).Length);
        }
    }
}
