using System;
using System.Globalization;

namespace HanaMart.Shift
{
    public sealed class ShiftClock
    {
        public double Minutes { get; private set; }
        public int Phase => Minutes < 120 ? 1 : Minutes < 240 ? 2 : 3;
        public bool Running { get; set; }
        public string Display => showFakeTime ? "04:44" : Format(Minutes);
        bool showFakeTime;
        double fakeRemaining;

        public static string Format(double minutes)
        {
            int minute = (int)Math.Floor(minutes);
            return (minute / 60).ToString("00", CultureInfo.InvariantCulture) + ":" +
                (minute % 60).ToString("00", CultureInfo.InvariantCulture);
        }

        public void Reset(int hour)
        {
            Minutes = Math.Clamp(hour, 0, 6) * 60;
            Running = true;
            fakeRemaining = 0;
            showFakeTime = false;
        }

        public void FakeTime(double seconds) => fakeRemaining = seconds;

        public void Tick(double seconds, bool dialogOpen = false, bool paperOpen = false, double timeScale = 1)
        {
            if (Running && !dialogOpen && !paperOpen)
            {
                double elapsed = seconds * timeScale;
                Minutes = Math.Min(360, Minutes + Math.Max(0, elapsed) / (5 + Phase));
            }
            showFakeTime = fakeRemaining > 0;
            if (showFakeTime) fakeRemaining -= seconds;
        }
    }
}
