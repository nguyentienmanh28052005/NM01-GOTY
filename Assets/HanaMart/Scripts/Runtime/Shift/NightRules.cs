using System;
using UnityEngine;

namespace HanaMart.Shift
{
    public enum DeliveryResult { Accepted, Refused, Strike, ThirteenthBox }

    public static class NightRules
    {
        public static bool FreezerForbidden(double minute) => minute >= 120 && minute < 130;
        public static bool CanRescueJiho(int pages, double minute, bool rescued) => pages >= 5 && minute >= 300 && !rescued;
        public static bool HasShelter(Vector2 position, float crouch) => crouch > .6f && position.x < 1.25f && position.y > 2.3f && position.y < 6.9f;
        public static double BlackoutGrace(int retries) => 6 + 1.5 * Math.Min(3, retries);
        public static bool CanHandOver(double minute) => minute >= 359.5;
        public static DeliveryResult Deliver(int boxes, bool accepted) => (boxes, accepted) switch
        {
            (12, true) => DeliveryResult.Accepted,
            (12, false) => DeliveryResult.Strike,
            (13, true) => DeliveryResult.ThirteenthBox,
            _ => DeliveryResult.Refused,
        };
    }

    public sealed class RedHour
    {
        static readonly Vector2[] Positions =
        {
            new(10.6f, 1.3f), new(8.4f, 2.2f), new(8.4f, 4.2f), new(7.2f, 6.4f), new(5.4f, 6.7f), new(3.6f, 6), new(2.4f, 5.3f),
        };
        readonly (double start, double end)[] darkness = new (double, double)[6];
        double elapsed;
        Vector2 expected;
        public bool Dark { get; private set; }
        public bool Finished { get; private set; }
        public int Cycle { get; private set; } = -1;
        public double Duration { get; }

        public RedHour(Func<double> random)
        {
            double time = 4;
            for (int cycle = 0; cycle < darkness.Length; ++cycle)
            {
                double start = time + 2.5 + random() * 5;
                time = start + 1.6 + random() * 3;
                darkness[cycle] = (start, time);
            }
            Duration = time + 2;
        }

        public bool Tick(double dt, Vector2 position)
        {
            elapsed += dt;
            Finished = elapsed > Duration;
            bool wasDark = Dark;
            Dark = false;
            for (int i = 0; i < darkness.Length && elapsed >= darkness[i].start; ++i)
            {
                if (elapsed >= darkness[i].end) continue;
                if (!wasDark || Cycle != i) expected = position;
                Cycle = i;
                Dark = true;
                return elapsed - darkness[i].start > .25 && Vector2.Distance(position, expected) > .1f;
            }
            return false;
        }

        public static Vector2 EntityPosition(int cycle) => Positions[Mathf.Clamp(cycle, 0, Positions.Length - 1)];
    }
}
