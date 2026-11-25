using HanaMart.EditorTools;
using HanaMart.Store;
using NUnit.Framework;
using UnityEngine;

namespace HanaMart.Tests
{
    [Timeout(10000)]
    public sealed class StoreWeatherTests
    {
        [Test]
        public void DecayCompensationBoostsSubQuadraticLightsAndPreservesPhysicalDecay()
        {
            float pointAlley = StoreImporter.ComputeLightIntensity("Point", 30f, 1.2f);
            Assert.That(pointAlley, Is.InRange(25f, 35f));
            Assert.Greater(pointAlley, 2.5f * (30f / Mathf.PI));

            float spotAlley = StoreImporter.ComputeLightIntensity("Spot", 90f, 1.3f);
            Assert.That(spotAlley, Is.InRange(65f, 85f));

            float physicalPoint = StoreImporter.ComputeLightIntensity("Point", 10f, 2.0f);
            Assert.AreEqual(10f / Mathf.PI, physicalPoint, 1e-4f);

            float rectArea = StoreImporter.ComputeLightIntensity("RectArea", 2f, 2f, 0.3f, 4.3f);
            Assert.AreEqual(2f * 0.3f * 4.3f / Mathf.PI * 1.2f, rectArea, 1e-4f);
        }

        [Test]
        public void EffectiveSmoothnessAccountsForRoughnessMapMean()
        {
            Assert.AreEqual(0.65f, StoreImporter.EffectiveSmoothness(1f, 0.35f), 1e-5f);
            Assert.AreEqual(0f, StoreImporter.EffectiveSmoothness(1f, null), 1e-5f);
            Assert.AreEqual(0.5f, StoreImporter.EffectiveSmoothness(0.5f, null), 1e-5f);
        }

        [Test]
        public void WeatherShadersExistAndAreSupportedUnderUrpPrefix()
        {
            var rain = Shader.Find("Universal Render Pipeline/HanaMart/RainStreaks");
            Assert.IsNotNull(rain);
            Assert.IsTrue(rain.isSupported);
            StringAssert.StartsWith("Universal Render Pipeline/", rain.name);

            var cone = Shader.Find("Universal Render Pipeline/HanaMart/LampCone");
            Assert.IsNotNull(cone);
            Assert.IsTrue(cone.isSupported);
            StringAssert.StartsWith("Universal Render Pipeline/", cone.name);
        }

        [Test]
        public void WetStreakSpecsMatchLightingJs()
        {
            Assert.AreEqual(12, StoreWeather.StreakSpecs.Length);
        }

        [Test]
        public void RainMeshVertexAndIndexCountMatchRequestedCount()
        {
            var mesh = StoreWeather.BuildRainMesh(1200);
            try
            {
                Assert.AreEqual(4800, mesh.vertexCount);
                Assert.AreEqual(7200, mesh.GetIndexCount(0));
            }
            finally
            {
                Object.DestroyImmediate(mesh);
            }

            var empty = StoreWeather.BuildRainMesh(0);
            try
            {
                Assert.AreEqual(0, empty.vertexCount);
            }
            finally
            {
                Object.DestroyImmediate(empty);
            }
        }
    }
}
