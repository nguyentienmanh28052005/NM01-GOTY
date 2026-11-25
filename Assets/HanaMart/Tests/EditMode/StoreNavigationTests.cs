using System.Collections.Generic;
using System.IO;
using System.Linq;
using HanaMart.Actors;
using HanaMart.EditorTools;
using HanaMart.Interaction;
using HanaMart.Player;
using HanaMart.Store;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace HanaMart.Tests
{
    [Timeout(10000)]
    public sealed class StoreNavigationTests
    {
        static StoreNavigation OriginalLayout()
        {
            var boxes = (JArray)JObject.Parse(File.ReadAllText(StoreImporter.MetaPath))["colliders"];
            return new StoreNavigation(boxes.Select(c => new Box2((float)c[0], (float)c[1], (float)c[2], (float)c[3])));
        }

        [TestCase(3.1f, 11.6f, 2.45f, 4.4f)]
        [TestCase(2.45f, 4.4f, .8f, -.8f)]
        [TestCase(.8f, -.8f, 2.15f, -7f)]
        [TestCase(5.8f, 3.8f, 10.6f, 3.8f)]
        public void RoutesFollowFreeSegmentsThroughStore(float x0, float z0, float x1, float z1)
        {
            var grid = OriginalLayout();
            var start = new Vector2(x0, z0);
            var end = new Vector2(x1, z1);
            Assert.IsTrue(grid.TryPath(start, end, out var route));
            Assert.That(route.Count, Is.InRange(1, 64));
            foreach (var waypoint in route)
            {
                Assert.IsTrue(grid.LineFree(start, waypoint), $"Blocked segment {start} → {waypoint}");
                start = waypoint;
            }
            Assert.That(Vector2.Distance(start, end), Is.LessThan(.08f));
        }

        [Test]
        public void FrontWindowAndShelfRemainBlocked()
        {
            var grid = OriginalLayout();
            Assert.IsTrue(grid.Walkable(3.1f, 8));
            Assert.IsFalse(grid.Walkable(5.7f, 8));
            Assert.IsTrue(grid.Walkable(.8f, -.16f));
            Assert.IsTrue(grid.Walkable(1.88f, -4.6f));
            Assert.IsFalse(grid.Walkable(2.14f, -4.6f));
            Assert.IsFalse(grid.LineFree(new Vector2(5.8f, 3.8f), new Vector2(10.6f, 3.8f)));
            Assert.IsFalse(grid.LineFree(new Vector2(0, 8.1f), new Vector2(10, 8.1f)));
        }

        [Test]
        public void UnreachableRoutesDontFallBackToCrossingTheWall()
        {
            var grid = new StoreNavigation(new[] { new Box2(-6, 3, 18, 3.3f) });
            Assert.IsFalse(grid.TryPath(new Vector2(3, 2), new Vector2(3, 4), out var route));
            Assert.IsEmpty(route);
            Assert.IsFalse(grid.TryPath(Vector2.one, new Vector2(1e6f, 1e6f), out _));
        }

        [Test]
        public void DirectRouteReturnsExactTarget()
        {
            var grid = new StoreNavigation(new List<Box2>());
            var end = new Vector2(3.27f, 5.03f);
            Assert.IsTrue(grid.TryPath(new Vector2(3.1f, 6f), end, out var route));
            CollectionAssert.AreEqual(new[] { end }, route);
        }

        [Test]
        public void PlacementUsesStreetCellsForDownwardRay()
        {
            var grid = OriginalLayout();
            var ray = new Ray(new Vector3(3.1f, 1, 10.5f), new Vector3(0, -1, .3f));
            Vector3 target = Placement.Target(ray, new Vector2(3.1f, 10.5f), grid.Walkable);
            Assert.That(Vector3.Distance(target, new Vector3(3.1f, 0, 10.8f)), Is.LessThan(.001f));
        }

        [Test]
        public void BuiltNavMeshConnectsStreetToAlley()
        {
            var nav = AssetDatabase.LoadAssetAtPath<NavMeshData>("Assets/HanaMart/Data/StoreNavigation.asset");
            Assert.IsNotNull(nav, "Bake via HanaMart/Build Store Navigation.");
            var instance = NavMesh.AddNavMeshData(nav);
            try
            {
                var path = new NavMeshPath();
                Assert.IsTrue(NavMesh.CalculatePath(ThreeCoords.ToUnity(3.1f, 0, 11.6f),
                    ThreeCoords.ToUnity(2.15f, 0, -7), NavMesh.AllAreas, path));
                Assert.AreEqual(NavMeshPathStatus.PathComplete, path.status);
            }
            finally { instance.Remove(); }
        }

        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)]
        public void ActorFollowsStreetToAlleyAndCleansUpAfterSixtySeconds(int model)
        {
            var grid = OriginalLayout();
            var catalog = AssetDatabase.LoadAssetAtPath<ActorCatalog>("Assets/HanaMart/Data/ActorCatalog.asset");
            int baseline =  Resources.FindObjectsOfTypeAll<Mesh>().Count(mesh => mesh.name == "Actor floor contact");
            var actor = Actor.Spawn(catalog, new ActorOptions { modelIndex = model, coat = model == 2 ? "child" : "padding" },
                new Vector3(3.1f, 0, 11.6f));
            try
            {
                var end = new Vector2(2.15f, -7);
                Assert.IsTrue(actor.TryMoveTo(grid, end));
                float strideWhileWalking = 0;
                for (int frame = 0; frame < 1200; ++frame)
                {
                    var start = new Vector2(actor.pos.x, actor.pos.z);
                    actor.Advance(.05f);
                    var next = new Vector2(actor.pos.x, actor.pos.z);
                    float distance = Vector2.Distance(next, start);
                    Assert.That(distance, Is.LessThanOrEqualTo(actor.speed * .05f + .0001f));
                    Assert.IsTrue(grid.LineFree(start, next), $"Frame {frame}: {start:F4} → {next:F4}");
                    if (actor.moving) strideWhileWalking += distance;
                }
                Assert.Greater(strideWhileWalking, Vector2.Distance(new Vector2(3.1f, 11.6f), end));
                Assert.That(Vector2.Distance(new Vector2(actor.pos.x, actor.pos.z), end), Is.LessThan(.06f));
                Assert.That(actor.Motion.blend, Is.LessThan(.001f));
                var stationary = actor.Model.transform.localPosition;
                for (int frame = 0; frame < 30; ++frame) actor.Advance(.05f);
                Assert.That(Vector3.Distance(stationary, actor.Model.transform.localPosition), Is.LessThan(.0001f));
            }
            finally { actor.Remove(); }
            Assert.IsTrue(actor == null);
            Assert.AreEqual(baseline, Resources.FindObjectsOfTypeAll<Mesh>().Count(mesh => mesh.name == "Actor floor contact"));
        }
    }
}
