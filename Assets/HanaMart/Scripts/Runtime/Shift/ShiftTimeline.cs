using System;
using System.Collections.Generic;
using System.Linq;

namespace HanaMart.Shift
{
    public sealed class ShiftEvent
    {
        public string Id { get; }
        public double Minute { get; }
        public string AmbientKind { get; }
        public bool IsAmbient => AmbientKind != null;

        internal ShiftEvent(string id, double minute, string ambientKind = null)
        {
            Id = id;
            Minute = minute;
            AmbientKind = ambientKind;
        }
    }

    public sealed class ShiftTimeline
    {
        public IReadOnlyList<ShiftEvent> Events => events;
        readonly ShiftEvent[] events;
        int next;

        public ShiftTimeline(Func<double> randomValue)
        {
            double Jitter(double target, double amount) => target + (randomValue() * 2 - 1) * amount;
            var schedule = new List<ShiftEvent>
            {
                new("intro", .2), new("managerPhone", 2), new("passerby", 9), new("fax", 60),
                new("trash", 92), new("milk", 110), new("phase2", 120), new("calm2", 131),
                new("cctvDouble", Jitter(158, 6)), new("delivery", 180), new("hqTrap", Jitter(191, 3)),
                new("manager", Jitter(200, 3)), new("clockGlitch", 208 + randomValue() * 10),
                new("blackout", Jitter(227, 4)), new("phase3", 240), new("starer", Jitter(249, 4)),
                new("forge12", 256), new("trashAgain", 268), new("phone444", 284),
                new("cctvDoubleLate", Jitter(293, 3)), new("freezerJiho", 300), new("forge13", 306), new("redHour", 315),
                new("doubleShift", Jitter(272, 5)), new("screamGlass", Jitter(141, 5)), new("lateDelivery", Jitter(262, 2)),
                new("fakeMirae", Jitter(333, 5)), new("starerLate", Jitter(347, 2)), new("realMirae", 355),
            };
            var storyMinutes = schedule.Select(e => e.Minute).ToArray();
            AddAmbient(schedule, storyMinutes, randomValue, 18, 116, 4, "can", "fridge", "doorHeld", "radio", "lightDie");
            AddAmbient(schedule, storyMinutes, randomValue, 124, 236, 6, "can", "fridge", "steps", "cctvGhost", "still",
                "radio", "flicker", "fakePhone", "backKnock");
            AddAmbient(schedule, storyMinutes, randomValue, 244, 356, 8, "still", "steps", "cctvGhost", "handprints",
                "fridge", "can", "radio", "whisper", "flicker", "fakePhone", "lightDie", "backKnock");
            events = schedule.OrderBy(e => e.Minute).ToArray();
        }

        static void AddAmbient(List<ShiftEvent> schedule, double[] storyMinutes, Func<double> randomValue,
            double from, double to, int count, params string[] kinds)
        {
            for (int i = kinds.Length - 1; i > 0; --i)
            {
                int other = (int)(randomValue() * (i + 1));
                (kinds[i], kinds[other]) = (kinds[other], kinds[i]);
            }
            int placed = 0;
            for (int attempt = 0; attempt < 300 && placed < count; ++attempt)
            {
                double minute = from + randomValue() * (to - from);
                if (minute is > 312 and < 330 or > 224 and < 236) continue;
                if (storyMinutes.Any(story => Math.Abs(story - minute) <= 4)) continue;
                if (schedule.Any(e => e.IsAmbient && Math.Abs(e.Minute - minute) < 7)) continue;
                schedule.Add(new ShiftEvent("ambient" + schedule.Count, minute, kinds[placed++]));
            }
        }

        public void SkipBefore(double minutes)
        {
            for (; next < events.Length && events[next].Minute < minutes; ++next) { }
        }

        public void FireThrough(double minutes, Action<ShiftEvent> fire)
        {
            while (next < events.Length && events[next].Minute <= minutes) fire(events[next++]);
        }

        public ShiftEvent NextAnchor(double minutes, double lead = 0, double after = 0)
        {
            double threshold = Math.Max(minutes + lead, after);
            for (int i = next; i < events.Length; ++i)
                if (!events[i].IsAmbient && events[i].Minute > threshold) return events[i];
            return null;
        }
    }
}
