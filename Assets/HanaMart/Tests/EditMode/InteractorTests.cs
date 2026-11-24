using HanaMart.Interaction;
using HanaMart.Store;
using NUnit.Framework;
using UnityEngine;

namespace HanaMart.Tests
{
    [Timeout(10000)]
    public class InteractorTests
    {
        GameObject target, host;
        Interactor interactor;
        readonly Vector3 origin = new Vector3(1000, 1000, 1000);
        [SetUp] public void Setup()
        {
            host = new GameObject("InteractorTest"); interactor = host.AddComponent<Interactor>();
            target = new GameObject("H:rules"); target.layer = LayerMask.NameToLayer("Interact");
            var marker = target.AddComponent<StoreMarker>(); marker.kind = StoreMarker.Kind.Hit; marker.key = "H:rules"; marker.json = "{\"id\":\"rules\",\"label\":\"Đọc nội quy\"}";
            target.AddComponent<BoxCollider>().isTrigger = true;
            target.transform.position = origin + Vector3.forward * 2;
            Physics.SyncTransforms();
        }
        [TearDown] public void Cleanup() { Object.DestroyImmediate(host); Object.DestroyImmediate(target); }
        [Test] public void RayFindsHitInFrontWithin21m() => Assert.AreEqual(target.GetComponent<StoreMarker>(), interactor.Trace(new Ray(origin, Vector3.forward)));
        [Test] public void RayIgnoresHitBeyond21m()
        {
            target.transform.position = origin + Vector3.forward * 3; Physics.SyncTransforms();
            Assert.IsNull(interactor.Trace(new Ray(origin, Vector3.forward)));
        }
        [Test] public void PressUsesOriginalId()
        {
            string id = null; interactor.Pressed += v => id = v;
            interactor.Tick(.02f, new Ray(origin, Vector3.forward), true, false);
            Assert.AreEqual("rules", id);
        }
        [Test] public void HoldProgressSurvivesSmallLookJitter()
        {
            float progress = 0; interactor.Holding += (_, p) => progress = p;
            for (int frame = 0; frame < 25; frame++) interactor.Tick(.02f, new Ray(origin, Vector3.forward), false, true);
            interactor.Tick(.02f, new Ray(origin, new Vector3(.5f, 0, 1)), false, true);
            interactor.Tick(.02f, new Ray(origin, Vector3.forward), false, true);
            Assert.Greater(progress, .5f);
        }
        [Test] public void HoldResetsAfterLookingAway()
        {
            float progress = 0; interactor.Holding += (_, p) => progress = p;
            for (int frame = 0; frame < 20; frame++) interactor.Tick(.02f, new Ray(origin, Vector3.forward), false, true);
            for (int frame = 0; frame < 20; frame++) interactor.Tick(.02f, new Ray(origin, Vector3.back), false, true);
            interactor.Tick(.02f, new Ray(origin, Vector3.forward), false, true);
            Assert.That(progress, Is.EqualTo(.02f).Within(.001f));
        }
    }
}
