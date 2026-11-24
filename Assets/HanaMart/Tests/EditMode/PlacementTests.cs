using System.Collections.Generic;
using HanaMart.Interaction;
using HanaMart.Player;
using NUnit.Framework;
using UnityEngine;

namespace HanaMart.Tests
{
    [Timeout(10000)]
    public class PlacementTests
    {
        static WalkArea Empty() => new WalkArea(new List<Box2>());
        [TestCase(1.6f,4f,1.02f)]
        [TestCase(.2f,4f,.92f)]
        [TestCase(9f,7.6f,1.02f)]
        [TestCase(6f,4f,0f)]
        public void MatchesOriginalSurfaces(float x,float z,float y) => Assert.That(Placement.SurfaceAt(x,z),Is.EqualTo(y).Within(.0001f));
        [TestCase(1.6f,4f,1.02f)]
        [TestCase(.2f,4f,.92f)]
        [TestCase(6f,4f,0f)]
        public void LookingDownPlacesOnSupportingSurface(float x,float z,float y)
        {
            var t = Placement.Target(new Ray(new Vector3(x,1.6f,z),Vector3.down),new Vector2(x,z),Empty());
            Assert.That(Vector3.Distance(t,new Vector3(x,y,z)),Is.LessThan(.001f));
        }
        [Test] public void LookingForwardUsesWalkableFallbackWithinReach()
        {
            var area = new WalkArea(new List<Box2> { new Box2(5.5f,2f,6.5f,2.9f) });
            var target = Placement.Target(new Ray(new Vector3(6,1.6f,4),Vector3.back),new Vector2(6,4),area);
            Assert.IsTrue(area.Walkable(target.x,target.z));
            Assert.That(Vector2.Distance(new Vector2(target.x,target.z),new Vector2(6,4)),Is.InRange(.49f,1.61f));
        }
        [Test] public void ArcStartsInHandAndLandsExactly()
        {
            var a=new Vector3(1,1,1); var b=new Vector3(2,0,2);
            Assert.That(Vector3.Distance(Placement.Arc(a,b,0,.18f),a),Is.LessThan(.00001f));
            Assert.That(Vector3.Distance(Placement.Arc(a,b,1,.18f),b),Is.LessThan(.00001f));
            Assert.That(Placement.Arc(a,b,.5f,.18f).y,Is.EqualTo(.68f).Within(.00001f));
        }
        [Test] public void FallingItemBouncesThenSleepsOnCounter()
        {
            var body=new ItemBody { position=new Vector3(1.6f,2,4), halfHeight=.1f };
            for(int frame=0;frame<1200 && !body.sleeping;frame++) ItemPhysics.Step(body,1f/120);
            Assert.IsTrue(body.sleeping); Assert.Greater(body.lands,0);
            Assert.That(body.position.y,Is.EqualTo(1.12f).Within(.001f));
        }
        [Test] public void FallingItemRespectsStoreWalls()
        {
            var body=new ItemBody {position=new Vector3(.01f,.1f,4),velocity=new Vector3(-2,0,0)};
            ItemPhysics.Step(body,.02f);
            Assert.GreaterOrEqual(body.position.x,.08f); Assert.Greater(body.velocity.x,0);
        }
    }
}
