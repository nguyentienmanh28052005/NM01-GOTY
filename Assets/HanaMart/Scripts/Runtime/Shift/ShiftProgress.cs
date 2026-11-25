using System.Collections.Generic;

namespace HanaMart.Shift
{
    public sealed class ShiftProgress
    {
        static readonly string[] TransientFlags =
        {
            "double", "box13", "alleyCall", "alleyPending", "chase", "clock444", "cctvGhost", "cctvGlitch",
            "delivery", "strangerIn", "red", "blackEntry", "blackout",
        };
        static readonly Dictionary<int, string> MercyByRule = new()
        {
            { 15, "noShadow" }, { 3, "raincoatBad" }, { 18, "childMom" },
            { 16, "selfCall" }, { 17, "chase" }, { 4, "loneChild" },
        };

        public int SavedHour { get; private set; }
        public int Strikes { get; private set; }
        public int DeathCount { get; private set; }
        public HashSet<string> Diaries { get; } = new();
        public Dictionary<string, int> Flags { get; } = new();
        public HashSet<int> LearnedRules { get; } = new();
        public Dictionary<string, int> Mercy { get; } = new();

        readonly HashSet<string> savedDiaries = new();
        readonly Dictionary<string, int> savedFlags = new();
        int savedStrikes, strikesAtLastCheckpoint;

        public void NewShift()
        {
            SavedHour = 0;
            savedStrikes = 0;
            savedDiaries.Clear();
            savedFlags.Clear();
            LearnedRules.Clear();
            Mercy.Clear();
            DeathCount = 0;
            Retry();
        }

        public int Strike() => ++Strikes;

        public void RecordDeath(int rule)
        {
            ++DeathCount;
            if (RuleBook.HasNote(rule)) LearnedRules.Add(rule);
            RecordMercy("r" + rule);
            if (MercyByRule.TryGetValue(rule, out var scenario)) RecordMercy(scenario);
        }

        void RecordMercy(string key) => Mercy[key] = Mercy.GetValueOrDefault(key) + 1;

        public bool Checkpoint(int hour)
        {
            bool reduced = Strikes > 0 && Strikes == strikesAtLastCheckpoint;
            if (reduced) --Strikes;
            SavedHour = hour;
            savedStrikes = strikesAtLastCheckpoint = Strikes;
            savedDiaries.Clear();
            savedDiaries.UnionWith(Diaries);
            savedFlags.Clear();
            foreach (var flag in Flags) savedFlags.Add(flag.Key, flag.Value);
            return reduced;
        }

        public void Retry()
        {
            Strikes = strikesAtLastCheckpoint = savedStrikes;
            Diaries.Clear();
            Diaries.UnionWith(savedDiaries);
            Flags.Clear();
            foreach (var flag in savedFlags) Flags.Add(flag.Key, flag.Value);
            foreach (var transient in TransientFlags) Flags.Remove(transient);
        }

        public void ClearTransientFlags()
        {
            foreach (var transient in TransientFlags) Flags.Remove(transient);
            Flags.Remove("knock");
            Flags.Remove("knock323");
            Flags.Remove("fake12");
            Flags.Remove("fake13");
        }

        public void SetSavedHour(int hour) => SavedHour = System.Math.Clamp(hour, 0, 5);
    }
}
