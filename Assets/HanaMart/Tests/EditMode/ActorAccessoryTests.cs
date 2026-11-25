using HanaMart.Actors;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HanaMart.Tests
{
    [Timeout(10000)]
    public class ActorAccessoryTests
    {
        GameObject head;Material source;GameObject accessory;
        [SetUp]public void Setup(){head=new GameObject("Head");source=new Material(Shader.Find("HanaMart/Character"));source.SetColor("_CoatColor",Color.yellow);}
        [TearDown]public void Cleanup(){Object.DestroyImmediate(head);Object.DestroyImmediate(source);}
        [Test]public void HoodFollowsHeadAndDoesNotChangeSharedMaterial()
        {
            accessory=ActorAccessories.Hood(head.transform,Quaternion.identity,.68f,source);
            Assert.IsNotNull(accessory);Assert.AreEqual(head.transform,accessory.transform.parent);
            Assert.AreNotSame(source,accessory.GetComponent<Renderer>().sharedMaterial);
            Assert.AreEqual(Color.yellow,source.GetColor("_CoatColor"));
            Assert.That(accessory.GetComponent<Renderer>().bounds.size.x,Is.InRange(.15f,.3f));
        }
        [TestCase("white")] [TestCase("black")] [TestCase("smile")]
        public void FaceMasksHaveGeometryAndOwnedMaterial(string kind)
        {
            accessory=ActorAccessories.Mask(head.transform,Quaternion.identity,1,source,kind);
            Assert.IsNotNull(accessory);Assert.AreEqual(head.transform,accessory.transform.parent);
            Assert.Greater(accessory.GetComponent<MeshFilter>().sharedMesh.vertexCount,20);
            Assert.AreNotSame(source,accessory.GetComponent<Renderer>().sharedMaterial);
        }

        [TestCase("white")] [TestCase("black")] [TestCase("smile")]
        public void MaskIsSnugDoubleSidedAndImmuneToCoatTint(string kind)
        {
            accessory = ActorAccessories.Mask(head.transform, Quaternion.identity, 1f, source, kind);
            var mat = accessory.GetComponent<Renderer>().sharedMaterial;
            Assert.AreEqual(0f, mat.GetFloat("_Cull"), 0.01f, "Mask material should be double-sided (_Cull = 0)");

            var region = mat.GetTexture("_RegionMask") as Texture2D;
            Assert.IsNotNull(region, "Mask should have _RegionMask");
            Assert.AreEqual(0f, region.GetPixel(0, 0).r, 0.01f, "Mask _RegionMask.r must be 0 so _CoatColor never tints it");

            var bounds = accessory.GetComponent<Renderer>().bounds;
            float frontZ = bounds.max.z - head.transform.position.z;
            Assert.That(frontZ, Is.InRange(0.125f, 0.155f), $"Mask front z={frontZ:F3} should sit snugly over nose/mouth");
            Assert.That(bounds.size.x, Is.InRange(0.11f, 0.17f), $"Mask width={bounds.size.x:F3} should fit face cleanly");
        }

        [Test]
        public void HoodIsDoubleSidedAndConnectsDownToCollar()
        {
            accessory = ActorAccessories.Hood(head.transform, Quaternion.identity, 1f, source);
            var mat = accessory.GetComponent<Renderer>().sharedMaterial;
            Assert.AreEqual(0f, mat.GetFloat("_Cull"), 0.01f, "Hood material should be double-sided (_Cull = 0)");

            var bounds = accessory.GetComponent<Renderer>().bounds;
            float minY = bounds.min.y - head.transform.position.y;
            float maxY = bounds.max.y - head.transform.position.y;
            Assert.LessOrEqual(minY, -0.045f, $"Hood bottom ({minY:F3}) should drape down to collar");
            Assert.LessOrEqual(maxY, 0.21f, $"Hood top ({maxY:F3}) should hug top of head without floating");
        }

        [Test]
        public void SpawnedActorDoesNotAttachExtraneousHeadOrFacePrimitives()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<ActorCatalog>("Assets/HanaMart/Data/ActorCatalog.asset");
            Assert.IsNotNull(catalog, "ActorCatalog.asset must exist");

            var actor = Actor.Spawn(catalog, new ActorOptions
            {
                coat = "raincoat",
                color = 0xf2c200,
                hood = true,
                mask = "white"
            }, Vector3.zero);

            try
            {
                foreach (var t in actor.GetComponentsInChildren<Transform>(true))
                {
                    Assert.AreNotEqual("Rain hood", t.name, "Extraneous Rain hood should not be attached to Mixamo actor");
                    Assert.IsFalse(t.name.StartsWith("Face mask"), $"Extraneous {t.name} should not be attached to Mixamo actor");
                }
            }
            finally
            {
                actor.Remove();
            }
        }
    }
}
