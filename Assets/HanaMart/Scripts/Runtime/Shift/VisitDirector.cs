using System;
using System.Collections.Generic;

namespace HanaMart.Shift
{
    public sealed class VisitDirector
    {
        static readonly string[] Kinds =
        {
            "normal", "ghost", "raincoatOk", "raincoatBad", "loneChild", "childParent", "restroom", "phoneHQ",
            "raincoatChild", "drunk", "noShadow", "childMom", "selfCall", "chase",
        };
        static readonly Dictionary<string, int> Limits = new()
        {
            { "noShadow", 2 }, { "childMom", 1 }, { "selfCall", 1 }, { "chase", 2 }, { "ghost", 5 },
            { "raincoatOk", 2 }, { "raincoatBad", 2 }, { "loneChild", 2 }, { "childParent", 2 }, { "restroom", 3 }, { "phoneHQ", 3 },
        };
        readonly Func<double> random;
        readonly Dictionary<string, int> counts = new();
        readonly double[] weights = new double[Kinds.Length];
        string last;
        double calmRemaining;

        public double? Remaining { get; private set; }
        public VisitDirector(Func<double> random) => this.random = random;

        public void Reset(bool continuing)
        {
            Remaining = continuing ? 10 : null;
            calmRemaining = continuing ? 18 : 0;
            last = null;
            counts.Clear();
        }

        public void StartIn(double seconds) => Remaining = seconds;
        public void Calm(double seconds) => calmRemaining = Math.Max(calmRemaining, seconds);

        public string Tick(double elapsed, double minute, int visitors, double? anchor,
            bool blocked, bool phoneRinging, ShiftProgress progress)
        {
            if (!Remaining.HasValue || blocked || minute > 352) return null;
            if (calmRemaining > 0)
            {
                calmRemaining -= elapsed;
                return null;
            }
            if (anchor.HasValue && anchor.Value > minute && anchor.Value - minute < 2.2) return null;
            Remaining -= elapsed;
            if (Remaining > 0 || visitors >= 2) return null;

            Remaining = minute < 120 ? 22 + random() * 12 : minute < 240 ? 20 + random() * 12 : 14 + random() * 8;
            double total = 0;
            for (int i = 0; i < Kinds.Length; ++i) total += weights[i] = Weight(Kinds[i], minute, phoneRinging, progress);
            double target = random() * total;
            string selected = "normal";
            for (int i = 0; i < Kinds.Length; ++i)
            {
                target -= weights[i];
                if (target > 0) continue;
                selected = Kinds[i];
                break;
            }
            counts[selected] = counts.GetValueOrDefault(selected) + 1;
            last = selected;
            return selected;
        }

        public double Weight(string kind, double minute, bool phoneRinging, ShiftProgress progress)
        {
            if (Limits.TryGetValue(kind, out int cap) && counts.GetValueOrDefault(kind) >= cap) return 0;
            int phase = minute < 120 ? 1 : minute < 240 ? 2 : 3;
            var flags = progress.Flags;
            double weight = kind switch
            {
                "normal" => phase == 1 ? 5 : phase == 2 ? 3 : 2,
                "ghost" when minute > 20 => phase == 1 ? 1.5 : 2.5,
                "raincoatOk" when minute > 40 => 1,
                "raincoatBad" when minute > 100 => phase == 1 ? .6 : 2,
                "loneChild" when minute >= 64 => 1.4,
                "childParent" when minute >= 64 => 1,
                "restroom" when minute > 30 => phase == 1 ? .8 : 1.6,
                "phoneHQ" when minute > 40 && !phoneRinging => phase == 3 ? 1.5 : .8,
                "raincoatChild" when minute > 125 && flags.GetValueOrDefault("addendum") != 0 && flags.GetValueOrDefault("rcChild") == 0 => 3,
                "drunk" when minute > 70 && flags.GetValueOrDefault("drunk") == 0 => 2,
                "noShadow" when minute > 128 => phase == 2 ? 1.6 : 2.2,
                "childMom" when minute > 150 => 1.4,
                "selfCall" when minute > 135 && !phoneRinging => 1.2,
                "chase" when minute > 140 && flags.GetValueOrDefault("chase") == 0 => 1.1,
                _ => 0,
            };
            weight *= Math.Pow(.55, progress.Mercy.GetValueOrDefault(kind));
            return kind == last && kind != "normal" ? weight * .2 : weight;
        }
    }

    public enum GreetingResult { None, Welcome, GhostStrike, Complaint, BlackoutStrike }

    public sealed class GreetingWindow
    {
        public float Remaining { get; private set; }
        bool ghost, blackout;

        public void Open(bool ghost = false, bool blackout = false)
        {
            this.ghost = ghost;
            this.blackout = blackout;
            Remaining = 6;
        }

        public void Reset() => Remaining = 0;

        public GreetingResult Tick(float dt, bool greet, bool visitorStillHere)
        {
            if (Remaining <= 0) return GreetingResult.None;
            Remaining = Math.Max(0, Remaining - dt);
            if (greet)
            {
                Remaining = 0;
                return ghost ? GreetingResult.GhostStrike : GreetingResult.Welcome;
            }
            if (Remaining > 0 || ghost || !visitorStillHere) return GreetingResult.None;
            return blackout ? GreetingResult.BlackoutStrike : GreetingResult.Complaint;
        }
    }
}
