using System;
using System.Collections.Generic;
using System.Linq;
using HanaMart.Shift;
using NUnit.Framework;

namespace HanaMart.Tests
{
    [Timeout(10000)]
    public sealed class ShiftTimeTests
    {
        [Test]
        public void ClockUsesSixSevenAndEightSecondMinutesAndCapsAtDawn()
        {
            var clock = new ShiftClock();
            clock.Reset(0);
            clock.Tick(719.5);
            Assert.AreEqual(119.91666667, clock.Minutes, 1e-7);
            Assert.AreEqual(1, clock.Phase);
            Assert.AreEqual("01:59", clock.Display);
            clock.Tick(.5);
            Assert.AreEqual(120, clock.Minutes, 1e-8);
            Assert.AreEqual(2, clock.Phase);
            clock.Tick(840);
            Assert.AreEqual(240, clock.Minutes, 1e-8);
            Assert.AreEqual(3, clock.Phase);
            clock.Tick(8);
            Assert.AreEqual(241, clock.Minutes, 1e-8);
            clock.Tick(1000);
            Assert.AreEqual(360, clock.Minutes);
            Assert.AreEqual("06:00", clock.Display);
        }

        [Test]
        public void FakeClockDoesNotChangeTimeAndPaperOrDialogFreezeTheShift()
        {
            var clock = new ShiftClock();
            clock.Reset(3);
            clock.FakeTime(2);
            clock.Tick(1, dialogOpen: true);
            Assert.AreEqual("04:44", clock.Display);
            clock.Tick(1, paperOpen: true);
            Assert.AreEqual("04:44", clock.Display);
            Assert.AreEqual(180, clock.Minutes);
            clock.Tick(0);
            Assert.AreEqual("03:00", clock.Display);
            clock.Tick(.7, timeScale: 10);
            Assert.AreEqual(181, clock.Minutes);
            clock.Running = false;
            clock.Tick(60);
            Assert.AreEqual(181, clock.Minutes);
        }

        [Test]
        public void FixedHoursAndJitterUseThePublishedGameSourceValues()
        {
            var timeline = new ShiftTimeline(() => .5);
            var fixedEvents = timeline.Events.Where(e => !e.IsAmbient).ToDictionary(e => e.Id, e => e.Minute);
            Assert.AreEqual(29, fixedEvents.Count);
            Assert.AreEqual(.2, fixedEvents["intro"]);
            Assert.AreEqual(60, fixedEvents["fax"]);
            Assert.AreEqual(120, fixedEvents["phase2"]);
            Assert.AreEqual(180, fixedEvents["delivery"]);
            Assert.AreEqual(213, fixedEvents["clockGlitch"]);
            Assert.AreEqual(227, fixedEvents["blackout"]);
            Assert.AreEqual(240, fixedEvents["phase3"]);
            Assert.AreEqual(284, fixedEvents["phone444"]);
            Assert.AreEqual(300, fixedEvents["freezerJiho"]);
            Assert.AreEqual(315, fixedEvents["redHour"]);
            Assert.AreEqual(355, fixedEvents["realMirae"]);
        }

        [TestCase(1u, 5, 50.03379928832874, 350.7270626947284)]
        [TestCase(2u, 7, 19.37604805501178, 350.4178644567728)]
        public void AmbientWindowsMatchSourceWithTheSameRandomSequence(uint seed, int lateCount, double first, double last)
        {
            // The same unsigned generator was supplied to Math.random in game.js.
            double Next() => (seed = unchecked(seed * 1664525u + 1013904223u)) / 4294967296d;
            var events = new ShiftTimeline(Next).Events;
            var ambient = events.Where(e => e.IsAmbient).ToArray();
            Assert.AreEqual(10 + lateCount, ambient.Length);
            Assert.AreEqual(4, ambient.Count(e => e.Minute is >= 18 and <= 116));
            Assert.AreEqual(6, ambient.Count(e => e.Minute is >= 124 and <= 236));
            Assert.AreEqual(lateCount, ambient.Count(e => e.Minute is >= 244 and <= 356));
            Assert.AreEqual(first, ambient[0].Minute, 1e-8);
            Assert.AreEqual(last, ambient[ambient.Length - 1].Minute, 1e-8);
            for (int i = 0; i < ambient.Length; ++i)
            {
                double minute = ambient[i].Minute;
                Assert.IsFalse(minute is > 224 and < 236 or > 312 and < 330);
                Assert.IsTrue(events.Where(e => !e.IsAmbient).All(e => Math.Abs(e.Minute - minute) > 4));
                if (i > 0) Assert.GreaterOrEqual(minute - ambient[i - 1].Minute, 7);
            }
            Assert.IsTrue(events.Select(e => e.Minute).SequenceEqual(events.Select(e => e.Minute).OrderBy(m => m)));
        }

        [Test]
        public void ResumeReplaysTheEventExactlyAtTheCheckpointOnce()
        {
            var timeline = new ShiftTimeline(() => .5);
            timeline.SkipBefore(120);
            var fired = new List<string>();
            timeline.FireThrough(120, e => fired.Add(e.Id));
            timeline.FireThrough(120, e => fired.Add(e.Id));
            CollectionAssert.AreEqual(new[] { "phase2" }, fired);
            Assert.AreEqual("calm2", timeline.NextAnchor(120, .5, 60).Id);
            timeline.FireThrough(131, e => fired.Add(e.Id));
            CollectionAssert.AreEqual(new[] { "phase2", "calm2" }, fired);
        }

        [Test]
        public void EachCleanHourRemovesOneStrikeAndAChangedHourDoesNot()
        {
            var shift = new ShiftProgress();
            shift.NewShift();
            Assert.AreEqual(1, shift.Strike());
            Assert.IsFalse(shift.Checkpoint(1));
            Assert.AreEqual(2, shift.Strike());
            Assert.IsFalse(shift.Checkpoint(2));
            Assert.IsTrue(shift.Checkpoint(3));
            Assert.AreEqual(1, shift.Strikes);
            Assert.IsTrue(shift.Checkpoint(4));
            Assert.AreEqual(0, shift.Strikes);
            Assert.IsFalse(shift.Checkpoint(5));
        }

        [Test]
        public void RetryKeepsSavedProgressAndWhatThePlayerLearned()
        {
            var shift = new ShiftProgress();
            shift.NewShift();
            shift.Diaries.Add("fax");
            shift.Flags["ramenDone"] = 1;
            shift.Flags["double"] = 1;
            shift.Flags["cctvGlitch"] = 10;
            shift.Flags["complaints"] = 2;
            shift.Strike();
            shift.Checkpoint(2);
            shift.Strike();
            shift.Diaries.Add("trash");
            shift.Flags["complaints"] = 4;
            shift.RecordDeath(15);
            shift.Retry();
            Assert.AreEqual(2, shift.SavedHour);
            Assert.AreEqual(1, shift.Strikes);
            CollectionAssert.AreEquivalent(new[] { "fax" }, shift.Diaries);
            Assert.AreEqual(1, shift.Flags["ramenDone"]);
            Assert.AreEqual(2, shift.Flags["complaints"]);
            Assert.IsFalse(shift.Flags.ContainsKey("double"));
            Assert.IsFalse(shift.Flags.ContainsKey("cctvGlitch"));
            Assert.AreEqual(1, shift.DeathCount);
            CollectionAssert.Contains(shift.LearnedRules, 15);
            Assert.AreEqual(1, shift.Mercy["noShadow"]);
            shift.NewShift();
            Assert.AreEqual(0, shift.SavedHour);
            Assert.AreEqual(0, shift.DeathCount);
            Assert.IsEmpty(shift.LearnedRules);
            Assert.IsEmpty(shift.Mercy);
        }

        [Test]
        public void ShiftSavesBeforeFaxAndRetryReplaysItInsteadOfLaterStory()
        {
            var run = new ShiftRun(() => .5);
            run.NewShift();
            var order = new List<string>();
            run.CheckpointSaved += (hour, reduced) => order.Add($"save{hour}");
            run.EventReached += e =>
            {
                if (e.Id != "fax") return;
                Assert.AreEqual(1, run.Progress.SavedHour);
                Assert.IsFalse(run.Progress.Flags.ContainsKey("addendum"));
                run.Progress.Flags.Add("addendum", 1);
                order.Add(e.Id);
            };
            run.Tick(360);
            Assert.AreEqual(60, run.Clock.Minutes);
            CollectionAssert.AreEqual(new[] { "save1", "fax" }, order);
            run.Tick(60);
            Assert.AreEqual(70, run.Clock.Minutes);
            run.Retry();
            Assert.AreEqual(60, run.Clock.Minutes);
            run.Tick(0);
            CollectionAssert.AreEqual(new[] { "save1", "fax", "save1", "fax" }, order);
        }
    }
}
