using HanaMart.Actors;
using NUnit.Framework;
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
    }
}
