using HanaMart.Shift;
using NUnit.Framework;
using UnityEngine;

namespace HanaMart.Tests
{
    [Timeout(10000)]
    public sealed class CinematicTests
    {
        [Test]
        public void CinematicSlowmoDollyAndLetterboxEndOnSchedule()
        {
            var cine = new ShiftCinematics();
            cine.Begin(2.2f, .35f, .45f);
            Assert.IsTrue(cine.Active);
            Assert.IsTrue(cine.Letterbox);

            cine.Tick(.05f, deathActive: false, dead: false, tension: .1f);
            float midSlow = cine.Slowmo;
            float midDolly = cine.Dolly;
            for (int i = 0; i < 15; ++i)
            {
                cine.Tick(.05f, deathActive: false, dead: false, tension: .1f);
                midSlow = Mathf.Min(midSlow, cine.Slowmo);
                midDolly = Mathf.Min(midDolly, cine.Dolly);
            }
            Assert.Less(midSlow, .8f);
            Assert.Less(midDolly, -.15f);

            for (int i = 0; i < 35; ++i) cine.Tick(.05f, deathActive: false, dead: false, tension: .1f);
            Assert.IsFalse(cine.Active);
            Assert.IsFalse(cine.Letterbox);
            Assert.AreEqual(1f, cine.Slowmo, .0001f);
            Assert.AreEqual(0f, cine.Dolly, .0001f);
        }

        [Test]
        public void HeartbeatPulseTracksTensionThresholdsAndSilencesDuringDeath()
        {
            var cine = new ShiftCinematics();
            for (int i = 0; i < 40; ++i)
            {
                cine.Tick(.05f, deathActive: false, dead: false, tension: .1f);
                Assert.AreEqual(0f, cine.Pulse, .0001f);
            }

            float maxMid = 0;
            for (int i = 0; i < 40; ++i)
            {
                cine.Tick(.05f, deathActive: false, dead: false, tension: .6f);
                maxMid = Mathf.Max(maxMid, cine.Pulse);
            }
            Assert.Greater(maxMid, 0f);
            Assert.LessOrEqual(maxMid, .401f);

            float maxHigh = 0;
            for (int i = 0; i < 40; ++i)
            {
                cine.Tick(.05f, deathActive: false, dead: false, tension: 1f);
                maxHigh = Mathf.Max(maxHigh, cine.Pulse);
            }
            Assert.Greater(maxHigh, .5f);
            Assert.LessOrEqual(maxHigh, .701f);

            cine.Begin(2f);
            cine.Tick(.05f, deathActive: true, dead: true, tension: 1f);
            Assert.IsTrue(cine.Letterbox);
            Assert.AreEqual(1f, cine.Slowmo, .0001f);
            Assert.AreEqual(0f, cine.Pulse, .0001f);
        }

        [Test]
        public void TensionWarpAndGlitchMatchSourceConditions()
        {
            Assert.AreEqual(1f, ShiftCinematics.ComputeTension(true, false, false, false, false, 60), .0001f);
            Assert.AreEqual(1f, ShiftCinematics.ComputeTension(false, true, false, false, false, 60), .0001f);
            Assert.AreEqual(1f, ShiftCinematics.ComputeTension(false, false, true, false, false, 60), .0001f);
            Assert.AreEqual(1f, ShiftCinematics.ComputeTension(false, false, false, true, false, 60), .0001f);
            Assert.AreEqual(1f, ShiftCinematics.ComputeTension(false, false, false, false, true, 60), .0001f);
            Assert.AreEqual(.6f, ShiftCinematics.ComputeTension(false, false, false, false, false, 125), .0001f);
            Assert.AreEqual(.1f, ShiftCinematics.ComputeTension(false, false, false, false, false, 150), .0001f);

            Assert.AreEqual(.5f, ShiftCinematics.ComputeWarp(true, false, 0, false, 0, false), .0001f);
            Assert.AreEqual(.3f, ShiftCinematics.ComputeWarp(false, false, 0, false, 0, true), .0001f);
            Assert.AreEqual(.4f, ShiftCinematics.ComputeWarp(false, false, 0, true, 1.2f, false), .0001f);

            Assert.AreEqual(.06f, ShiftCinematics.ComputeGlitch(false, 0, 1f), .0001f);
            Assert.AreEqual(0f, ShiftCinematics.ComputeGlitch(false, 0, .6f), .0001f);
        }

        [Test]
        public void ApparitionSpawnsInPeripheralStoreBoundsAndDismissesOnLookOrTimeout()
        {
            var player = new Vector2(6f, 4f);
            var forward = Vector2.up;
            Assert.IsTrue(ShiftCinematics.TryApparitionSpot(player, forward, 1, .5f, out var spot));
            Assert.That(spot.x, Is.InRange(.4f, 11.6f));
            Assert.That(spot.y, Is.InRange(.4f, 7.6f));
            float dot = Vector2.Dot((spot - player).normalized, forward);
            Assert.Less(dot, .4f);

            Assert.IsFalse(ShiftCinematics.ShouldDismissApparition(1f, player, forward, spot, out bool looked));
            Assert.IsFalse(looked);

            var lookAtSpot = (spot - player).normalized;
            Assert.IsTrue(ShiftCinematics.ShouldDismissApparition(1f, player, lookAtSpot, spot, out looked));
            Assert.IsTrue(looked);

            Assert.IsTrue(ShiftCinematics.ShouldDismissApparition(5.1f, player, forward, spot, out looked));
            Assert.IsFalse(looked);
        }

        [Test]
        public void AmbientMixSilencesHumAndHvacDuringPowerOutAndRedDark()
        {
            var normal = AmbientMix.Compute(true, true, false, false, false, false, false, .1f);
            Assert.Greater(normal.RainInside, 0f);
            Assert.AreEqual(0f, normal.RainOutside, .0001f);
            Assert.Greater(normal.Hum, 0f);
            Assert.Greater(normal.Hvac, 0f);

            var alley = AmbientMix.Compute(true, true, true, false, false, false, false, .1f);
            Assert.AreEqual(0f, alley.RainInside, .0001f);
            Assert.Greater(alley.RainOutside, normal.RainInside);

            var blackout = AmbientMix.Compute(false, true, false, false, false, false, false, 1f);
            Assert.AreEqual(0f, blackout.Hum, .0001f);
            Assert.AreEqual(0f, blackout.Hvac, .0001f);
            Assert.Greater(blackout.Drone, normal.Drone);

            var redDark = AmbientMix.Compute(true, false, false, true, false, false, false, .9f);
            Assert.AreEqual(0f, redDark.Hum, .0001f);
            Assert.AreEqual(0f, redDark.Hvac, .0001f);
            Assert.AreEqual(0f, redDark.RainInside, .0001f);
            Assert.Greater(redDark.Breath, 0f);
        }
    }
}
