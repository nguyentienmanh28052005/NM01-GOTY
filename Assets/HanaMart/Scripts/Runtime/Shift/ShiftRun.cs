using System;

namespace HanaMart.Shift
{
    public sealed class ShiftRun
    {
        public ShiftClock Clock { get; } = new();
        public ShiftProgress Progress { get; } = new();
        public ShiftTimeline Timeline { get; private set; }
        public event Action<ShiftEvent> EventReached;
        public event Action<int, bool> CheckpointSaved;
        readonly Func<double> random;

        public ShiftRun(Func<double> random) => this.random = random ?? throw new ArgumentNullException(nameof(random));

        public void NewShift()
        {
            Progress.NewShift();
            RebuildSchedule();
        }

        public void Retry()
        {
            Progress.Retry();
            RebuildSchedule();
        }

        void RebuildSchedule()
        {
            Clock.Reset(Progress.SavedHour);
            Timeline = new ShiftTimeline(random);
            Timeline.SkipBefore(Clock.Minutes);
        }

        public void Tick(double seconds, bool dialogOpen = false, bool paperOpen = false, double timeScale = 1)
        {
            Clock.Tick(seconds, dialogOpen, paperOpen, timeScale);
            Timeline.FireThrough(Clock.Minutes, ReachEvent);
        }

        void ReachEvent(ShiftEvent entry)
        {
            int saveHour = entry.Id switch
            {
                "fax" => 1,
                "phase2" => 2,
                "delivery" => 3,
                "phase3" => 4,
                "freezerJiho" => 5,
                _ => 0,
            };
            if (saveHour > 0)
            {
                bool strikeRemoved = Progress.Checkpoint(saveHour);
                CheckpointSaved?.Invoke(saveHour, strikeRemoved);
            }
            EventReached?.Invoke(entry);
        }
    }
}
