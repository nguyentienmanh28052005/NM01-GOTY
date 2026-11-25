using HanaMart.Shift;
using NUnit.Framework;
using UnityEngine;

namespace HanaMart.Tests
{
    [Timeout(10000)]
    public sealed class DeathCutTests
    {
        [TestCase(3, "counter")] [TestCase(15, "counter")] [TestCase(4, "counter")]
        [TestCase(18, "childMom")] [TestCase(9, "storage")] [TestCase(19, "storage")] [TestCase(20, "storage")]
        [TestCase(13, "mirae")] [TestCase(12, "phone")] [TestCase(16, "phone")] [TestCase(6, "freezer")]
        [TestCase(7, "cctv")] [TestCase(8, "box13")] [TestCase(21, "box13")] [TestCase(5, "alley")]
        [TestCase(10, "blackout")] [TestCase(14, "red")] [TestCase(17, "chase")] [TestCase(0, "fired")] [TestCase(99, "lunge")]
        public void RulesUseTheOriginalDeathSceneMapping(int rule, string name) => Assert.AreEqual(name, new DeathCuts().Begin(rule).Kind);

        [Test]
        public void VariantCountIsSharedBySceneInsteadOfRuleNumber()
        {
            var cuts = new DeathCuts();
            Assert.AreEqual(1, cuts.Begin(3).Variant);
            Assert.AreEqual(2, cuts.Begin(15).Variant);
            Assert.AreEqual(1, cuts.Begin(18).Variant);
            Assert.AreEqual(3, cuts.Begin(3).Variant);
        }

        [Test]
        public void TimeStepIsClampedAndSkipCannotInterruptTheFirstOnePointFourSeconds()
        {
            var cut = new DeathCuts().Begin(6);
            cut.Tick(.8f);
            Assert.AreEqual(.05f, cut.Delta, .00001f);
            Assert.AreEqual(.05f, cut.Time, .00001f);
            Assert.IsFalse(cut.Skip());
            for (int frame = 0; frame < 27; ++frame) cut.Tick(.05f);
            Assert.IsFalse(cut.CanSkip);
            cut.Tick(.025f);
            Assert.IsTrue(cut.Skip());
            Assert.IsTrue(cut.Skipped);
            Assert.IsTrue(cut.Done);
            Assert.IsFalse(cut.Skip());
        }

        [Test]
        public void BrokenSequenceCannotKeepThePlayerInACutPastTwentySeconds()
        {
            var cut = new DeathCuts().Begin(8);
            for (int frame = 0; frame < 402; ++frame) cut.Tick(.05f);
            Assert.IsTrue(cut.Done);
            Assert.IsFalse(cut.Skipped);
        }

        [Test]
        public void EarlyTerminationStillReachesTheRecapTime()
        {
            var cut = new DeathCuts().Begin(8);
            cut.Tick(.01f);
            cut.Finish();
            for (int frame = 0; frame < 9; ++frame) cut.Tick(.05f);
            Assert.Greater(cut.Time, .4f);
            Assert.IsTrue(cut.Done);
            Assert.IsFalse(cut.CanSkip);
        }

        [Test]
        public void CameraMovesByTheSourceExponentialAndLooksAtItsTarget()
        {
            var camera = new DeathCamera(Vector3.zero, Quaternion.identity);
            camera.TargetPosition = Vector3.right * 2;
            camera.LookAt = new Vector3(3, 1, 2);
            camera.Tick(.05f);
            Assert.AreEqual(2 * (1 - Mathf.Exp(-.15f)), camera.Position.x, .0001f);
            Assert.Greater(Vector3.Dot(camera.Rotation * Vector3.forward, Vector3.right), .05f);
            camera.Roll = .3f;
            for (int frame = 0; frame < 120; ++frame) camera.Tick(.05f);
            Assert.That(Vector3.Distance(camera.Position, camera.TargetPosition), Is.LessThan(.001f));
            Assert.That(Vector3.Dot(camera.Rotation * Vector3.forward, (camera.LookAt - camera.Position).normalized), Is.GreaterThan(.999f));
            Assert.Greater(Vector3.Angle(camera.Rotation * Vector3.up, Vector3.up), 12);
        }
    }
}
