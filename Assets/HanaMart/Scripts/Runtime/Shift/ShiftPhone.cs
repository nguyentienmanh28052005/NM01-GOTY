using System;

namespace HanaMart.Shift
{
    public sealed class ShiftPhone
    {
        public string Kind { get; private set; }
        public bool Ringing => Kind != null;
        public string Caller => Kind switch
        {
            "manager" => "QUẢN LÝ CỬA HÀNG",
            "hq" or "hqTrap" => "TRỤ SỞ HANA MART",
            "444" or "clock444" => "KHÔNG RÕ SỐ",
            "self" => "HANA MART 24 · 02-555-0124 (số của chính cửa hàng này)",
            _ => "???",
        };
        double elapsed, deadline;

        public void Call(string kind)
        {
            Kind = kind;
            elapsed = 0;
            deadline = kind switch { "manager" => 40, "444" => 22, "hq" => 20, _ => 18 };
        }

        public bool Tick(double seconds, out string unanswered)
        {
            unanswered = null;
            if (!Ringing) return false;
            double before = elapsed;
            elapsed += seconds;
            bool ring = Math.Floor(before / 3) < Math.Floor(elapsed / 3);
            if (elapsed > deadline) unanswered = Stop();
            return ring;
        }

        public string Stop()
        {
            string previous = Kind;
            Kind = null;
            elapsed = 0;
            return previous;
        }
    }
}
