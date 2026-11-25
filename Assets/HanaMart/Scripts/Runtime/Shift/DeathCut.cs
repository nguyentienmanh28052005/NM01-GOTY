using System.Collections.Generic;
using UnityEngine;

namespace HanaMart.Shift
{
    public sealed class DeathCuts
    {
        readonly Dictionary<string, int> visits = new();

        public DeathCut Begin(int rule)
        {
            string kind = rule switch
            {
                3 or 15 or 4 => "counter", 18 => "childMom", 9 or 19 or 20 => "storage", 13 => "mirae",
                12 or 16 => "phone", 6 => "freezer", 7 => "cctv", 8 or 21 => "box13", 5 => "alley",
                10 => "blackout", 14 => "red", 17 => "chase", 0 => "fired", _ => "lunge",
            };
            int variant = visits.GetValueOrDefault(kind) + 1;
            visits[kind] = variant;
            return new DeathCut(kind, variant);
        }
    }

    public sealed class DeathCut
    {
        public string Kind { get; }
        public int Variant { get; }
        public float Time { get; private set; }
        public float Delta { get; private set; }
        public bool Done { get; private set; }
        public bool Skipped { get; private set; }
        public bool CanSkip => !Done && Time > 1.4f + 1e-6f;
        public bool CounterTurnsHead(int rule) => Kind == "counter" && (rule == 15 || Variant % 2 == 0);

        internal DeathCut(string kind, int variant) { Kind = kind; Variant = variant; }

        public void Tick(float seconds)
        {
            Delta = Mathf.Clamp(seconds, 0, .05f);
            Time += Delta;
            if (!Done && Time > 20) Finish();
        }

        public void Finish() => Done = true;

        public bool Skip()
        {
            if (!CanSkip) return false;
            Skipped = Done = true;
            return true;
        }
    }

    public sealed class DeathCamera
    {
        public Vector3 Position { get; private set; }
        public Quaternion Rotation { get; private set; }
        public Vector3 TargetPosition, LookAt;
        public float Roll, PositionRate = 3, RotationRate = 4;

        public DeathCamera(Vector3 position, Quaternion rotation)
        {
            Position = TargetPosition = position;
            Rotation = rotation;
            LookAt = position + rotation * Vector3.forward;
        }
        public void Tick(float dt)
        {
            Position = Vector3.Lerp(Position, TargetPosition, 1 - Mathf.Exp(-PositionRate * dt));
            Rotation = Quaternion.Slerp(Rotation, Aim(), 1 - Mathf.Exp(-RotationRate * dt));
        }

        public void Snap(Vector3? position, Vector3? look)
        {
            if (position.HasValue) Position = TargetPosition = position.Value;
            if (look.HasValue) { LookAt = look.Value; Rotation = Aim(); }
        }

        Quaternion Aim()
        {
            var facing = LookAt - Position;
            if (facing.sqrMagnitude < 1e-8f) return Rotation;
            return Quaternion.LookRotation(facing) * Quaternion.AngleAxis(Roll * Mathf.Rad2Deg, Vector3.forward);
        }
    }
}
