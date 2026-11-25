using System.Collections.Generic;

namespace HanaMart.Shift
{
    public sealed class CheckoutQueue
    {
        public int? Current { get; private set; }
        public int WaitingCount => waiting.Count;
        readonly List<int> waiting = new();

        public bool TryEnter(int guest)
        {
            if (Current == guest) return true;
            if (!Current.HasValue && (waiting.Count == 0 || waiting[0] == guest))
            {
                waiting.Remove(guest);
                Current = guest;
                return true;
            }
            if (!waiting.Contains(guest)) waiting.Add(guest);
            return false;
        }

        public int IndexOf(int guest) => waiting.IndexOf(guest);

        public void Remove(int guest)
        {
            waiting.Remove(guest);
            if (Current == guest) Current = null;
        }

        public void Clear()
        {
            Current = null;
            waiting.Clear();
        }
    }
}
