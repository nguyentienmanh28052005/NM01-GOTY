using System;
using System.Collections.Generic;

namespace HanaMart.Shift
{
    public sealed class ShiftTask
    {
        public string Id { get; }
        public string Text { get; }
        public bool Done { get; internal set; }
        internal ShiftTask(string id, string text) { Id = id; Text = text; }
    }

    public sealed class ShiftTasks
    {
        readonly List<ShiftTask> items = new();
        readonly ShiftProgress progress;

        public IReadOnlyList<ShiftTask> Items => items;
        public ShiftTasks(ShiftProgress progress) => this.progress = progress;
        public bool Pending(string id) => items.Exists(item => item.Id == id && !item.Done);

        public bool Add(string id, string text)
        {
            int index = items.FindIndex(item => item.Id == id);
            if (index >= 0)
            {
                if (!items[index].Done) return false;
                items.RemoveAt(index);
            }
            items.Add(new ShiftTask(id, text));
            Increment("tAll");
            return true;
        }

        public bool Complete(string id)
        {
            var item = items.Find(entry => entry.Id == id && !entry.Done);
            if (item == null) return false;
            item.Done = true;
            Increment("tDone");
            return true;
        }

        void Increment(string key) => progress.Flags[key] = progress.Flags.GetValueOrDefault(key) + 1;

        public void Clear() => items.Clear();
    }

    [Flags]
    public enum MicrowaveChange { None = 0, Insert = 1, Close = 2, Heat = 4, Ready = 8 }

    public sealed class MicrowaveCycle
    {
        const double End = 7.3;
        double elapsed;
        public string Oven { get; private set; }
        public bool Active => Oven != null;
        public bool Ready { get; private set; }
        public bool Heating => Active && !Ready && elapsed >= 1.3 - 1e-9;
        public double SecondsRemaining => Heating ? End - elapsed : 0;

        public bool Begin(string oven)
        {
            if (Active || (oven != "micro1" && oven != "micro2")) return false;
            Oven = oven;
            return true;
        }

        public MicrowaveChange Tick(double seconds)
        {
            if (!Active || Ready) return MicrowaveChange.None;
            double before = elapsed;
            elapsed += seconds;
            bool Crossed(double mark) => before < mark - 1e-9 && elapsed >= mark - 1e-9;
            var changes = MicrowaveChange.None;
            if (Crossed(.55)) changes |= MicrowaveChange.Insert;
            if (Crossed(.95)) changes |= MicrowaveChange.Close;
            if (Crossed(1.3)) changes |= MicrowaveChange.Heat;
            if (Crossed(End)) { changes |= MicrowaveChange.Ready; Ready = true; }
            return changes;
        }

        public bool Take(string oven)
        {
            if (!Ready || Oven != oven) return false;
            Reset();
            return true;
        }

        public void Reset()
        {
            elapsed = 0;
            Oven = null;
            Ready = false;
        }
    }
}
