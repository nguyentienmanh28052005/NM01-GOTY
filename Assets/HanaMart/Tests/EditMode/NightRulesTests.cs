using HanaMart.Shift;
using NUnit.Framework;
using UnityEngine;

namespace HanaMart.Tests
{
    [Timeout(10000)]
    public sealed class NightRulesTests
    {
        [TestCase(119.999, false)] [TestCase(120, true)] [TestCase(129.999, true)] [TestCase(130, false)]
        public void FreezerUsesTheActualTwoTenMinuteBoundary(double minute, bool forbidden) =>
            Assert.AreEqual(forbidden, NightRules.FreezerForbidden(minute));

        [Test]
        public void JihoNeedsFivePagesAtFiveOClockAndCanOnlyBeRescuedOnce()
        {
            Assert.IsFalse(NightRules.CanRescueJiho(4, 300, false));
            Assert.IsFalse(NightRules.CanRescueJiho(5, 299.99, false));
            Assert.IsFalse(NightRules.CanRescueJiho(5, 300, true));
            Assert.IsTrue(NightRules.CanRescueJiho(5, 300, false));
        }

        [Test]
        public void BlackoutRequiresCrouchAndTheSourceCounterBounds()
        {
            Assert.IsTrue(NightRules.HasShelter(new Vector2(.85f, 4.4f), .61f));
            Assert.IsFalse(NightRules.HasShelter(new Vector2(1.25f, 4.4f), 1));
            Assert.IsFalse(NightRules.HasShelter(new Vector2(1.2f, 2.3f), 1));
            Assert.IsFalse(NightRules.HasShelter(new Vector2(1.2f, 6.9f), 1));
            Assert.IsFalse(NightRules.HasShelter(new Vector2(.85f, 4.4f), .6f));
            Assert.AreEqual(6, NightRules.BlackoutGrace(0));
            Assert.AreEqual(9, NightRules.BlackoutGrace(2));
            Assert.AreEqual(10.5, NightRules.BlackoutGrace(10));
        }

        [TestCase(12, true, DeliveryResult.Accepted)] [TestCase(12, false, DeliveryResult.Strike)]
        [TestCase(13, false, DeliveryResult.Refused)] [TestCase(13, true, DeliveryResult.ThirteenthBox)]
        public void SigningDependsOnCountingTheActualBoxes(int boxes, bool signed, DeliveryResult result) =>
            Assert.AreEqual(result, NightRules.Deliver(boxes, signed));

        [Test]
        public void RedHourUsesSixDarkCyclesWithGraceAndPositionThreshold()
        {
            var red = new RedHour(() => .5);
            Assert.AreEqual(54.6, red.Duration, 1e-7);
            Assert.IsFalse(red.Tick(8.99, Vector2.zero));
            Assert.IsFalse(red.Dark);
            Assert.IsFalse(red.Tick(.011, new Vector2(1, 2)));
            Assert.IsTrue(red.Dark);
            Assert.AreEqual(0, red.Cycle);
            Assert.AreEqual(new Vector2(10.6f, 1.3f), RedHour.EntityPosition(0));
            Assert.IsFalse(red.Tick(.24, new Vector2(1.5f, 2)));
            Assert.IsFalse(red.Tick(.02, new Vector2(1.09f, 2)));
            Assert.IsTrue(red.Tick(.01, new Vector2(1.11f, 2)));
            Assert.IsFalse(red.Tick(3, new Vector2(2, 3)));
            Assert.IsFalse(red.Dark);
            Assert.IsFalse(red.Tick(50, new Vector2(2, 3)));
            Assert.IsTrue(red.Finished);
            Assert.AreEqual(new Vector2(2.4f, 5.3f), RedHour.EntityPosition(12));
        }

        [TestCase(355, false)] [TestCase(359.49, false)] [TestCase(359.5, true)] [TestCase(360, true)]
        public void MorningHandoverKeepsSourceTolerance(double minute, bool allowed) =>
            Assert.AreEqual(allowed, NightRules.CanHandOver(minute));
    }
}
