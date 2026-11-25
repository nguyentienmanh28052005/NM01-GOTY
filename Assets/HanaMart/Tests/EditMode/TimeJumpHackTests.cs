using System.Collections.Generic;
using HanaMart.Shift;
using HanaMart.UI;
using NUnit.Framework;

namespace HanaMart.Tests
{
    public class TimeJumpHackTests
    {
        [Test]
        public void ShiftClockSetMinutesClampsAndClearsFakeTime()
        {
            var clock = new ShiftClock();
            clock.Reset(0);
            clock.FakeTime(10);
            clock.Tick(0.1);

            clock.SetMinutes(314.8);
            Assert.AreEqual(314.8, clock.Minutes, 1e-6);
            Assert.IsTrue(clock.Running);
            Assert.AreEqual("05:14", clock.Display);

            clock.SetMinutes(-15);
            Assert.AreEqual(0.0, clock.Minutes, 1e-6);

            clock.SetMinutes(999);
            Assert.AreEqual(360.0, clock.Minutes, 1e-6);
        }

        [Test]
        public void ShiftRunJumpToMinutesForwardAndBackwardRebuildsTimelineAndPreservesDiaries()
        {
            var run = new ShiftRun(() => 0.5);
            run.NewShift();
            run.Progress.Diaries.Add("diary1");

            var firedForward = new List<string>();
            run.EventReached += e => firedForward.Add(e.Id);

            // Jump right before Red Hour (315)
            run.JumpToMinutes(314.8);
            run.Tick(3.0); // phase 3 -> +3 / 8 = +0.375 min -> 315.175
            Assert.Contains("redHour", firedForward);
            Assert.IsFalse(firedForward.Contains("intro"));
            Assert.IsFalse(firedForward.Contains("phase2"));
            Assert.IsTrue(run.Progress.Diaries.Contains("diary1"), "Collected diaries should be preserved on time jump");

            // Jump backward to just before Phase 2 (120)
            var firedBackward = new List<string>();
            run.EventReached += e => firedBackward.Add(e.Id);
            run.JumpToMinutes(119.8);
            run.Tick(3.0); // phase 1 -> +3 / 6 = +0.5 min -> 120.3
            Assert.Contains("phase2", firedBackward);
        }

        [Test]
        public void AllTenPresetsCoverZeroToNineInAscendingOrder()
        {
            var presets = ShiftInterface.TimeJumpPresets;
            Assert.IsNotNull(presets);
            Assert.AreEqual(10, presets.Length, "Expected 10 presets for keys 0-9");

            double prev = -1.0;
            for (int i = 0; i < presets.Length; i++)
            {
                Assert.IsFalse(string.IsNullOrEmpty(presets[i].Label), $"Preset {i} missing label");
                Assert.That(presets[i].Minutes, Is.InRange(0.0, 360.0));
                Assert.Greater(presets[i].Minutes, prev, $"Preset {i} minutes not strictly ascending");
                prev = presets[i].Minutes;
            }
        }
    }
}
