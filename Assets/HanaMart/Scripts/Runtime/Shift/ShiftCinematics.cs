using System;
using UnityEngine;

namespace HanaMart.Shift
{
    public readonly struct AmbientMix
    {
        public readonly float RainInside, RainOutside, Hum, Hvac, Drone, Breath;

        public AmbientMix(float rainInside, float rainOutside, float hum, float hvac, float drone, float breath)
        {
            RainInside = rainInside;
            RainOutside = rainOutside;
            Hum = hum;
            Hvac = hvac;
            Drone = drone;
            Breath = breath;
        }

        public static AmbientMix Compute(bool insidePower, bool outsidePower, bool outside, bool redDark, bool cctvDouble, bool alleyCall, bool dead, float tension) =>
            new(
                rainInside: outsidePower && !outside ? .10f : 0,
                rainOutside: outsidePower && outside ? .30f : 0,
                hum: insidePower && !redDark && !dead ? .08f : 0,
                hvac: insidePower && !redDark && !dead ? .12f : 0,
                drone: dead ? 0 : Mathf.Clamp01(tension) * .16f,
                breath: !dead && (cctvDouble || alleyCall || redDark) ? .12f : 0);
    }

    public sealed class ShiftCinematics
    {
        public bool Active { get; private set; }
        public bool Letterbox { get; private set; }
        public float Slowmo { get; private set; } = 1;
        public float Dolly { get; private set; }
        public float Pulse { get; set; }
        public float Shake { get; private set; }
        public float Tension { get; private set; }
        public float ApparitionCountdown { get; private set; } = 40;

        float elapsed, duration, zoom, slow, heartbeatTime, pulseSpike;

        public void Reset()
        {
            Active = Letterbox = false;
            Slowmo = 1;
            Dolly = Pulse = Shake = Tension = 0;
            elapsed = duration = zoom = slow = pulseSpike = 0;
            ApparitionCountdown = 40;
        }

        public void Begin(float duration = 2.2f, float zoom = .35f, float slow = .45f)
        {
            this.duration = Mathf.Max(.05f, duration);
            this.zoom = zoom;
            this.slow = slow;
            elapsed = 0;
            Active = Letterbox = true;
        }

        public void AddShake(float amount) => Shake = Mathf.Max(Shake, amount);

        public void TriggerLookSpike()
        {
            pulseSpike = 1;
            Pulse = 1;
            Shake = Mathf.Max(Shake, .25f);
        }

        public void Tick(float realDt, bool deathActive, bool dead, float tension)
        {
            realDt = Mathf.Clamp(realDt, 0, .05f);
            Tension = tension;
            if (deathActive)
            {
                Letterbox = true;
                Slowmo = 1;
                Pulse = 0;
                return;
            }
            if (Active)
            {
                elapsed += realDt;
                float k = Mathf.Min(1, elapsed / duration);
                Slowmo = 1 - (1 - slow) * Mathf.Min(1, elapsed * 4) * (1 - k);
                Dolly = -zoom * Mathf.Sin(Mathf.Min(1, k * 1.6f) * Mathf.PI * .5f) * (1 - Mathf.Max(0, k - .75f) * 4);
                if (k >= 1)
                {
                    Active = false;
                    Letterbox = dead;
                    Slowmo = 1;
                    Dolly = 0;
                }
            }
            else
            {
                Dolly *= .9f;
                if (Mathf.Abs(Dolly) < .0001f) Dolly = 0;
                Slowmo = 1;
            }

            float hb = tension > .8f ? 1.25f : tension > .5f ? .9f : 0;
            heartbeatTime += realDt * (hb > 0 ? hb : .5f);
            float beat = hb > 0 ? Mathf.Pow(Mathf.Max(0, Mathf.Sin(heartbeatTime * Mathf.PI * 2)), 12) : 0;
            Pulse = Mathf.Max(pulseSpike, beat * (hb > 1 ? .7f : .4f));
            pulseSpike = Mathf.Max(0, pulseSpike - realDt * 2.5f);
            if (Shake > 0 && !dead) Shake = Mathf.Max(0, Shake - realDt * .9f);
            if (dead) Letterbox = true;
        }

        public bool TickApparitionTimer(float dt, double minutes, bool red, bool blackout, bool dead, Func<double> nextRandom)
        {
            if (minutes <= 100 || red || blackout || dead) return false;
            ApparitionCountdown -= dt;
            if (ApparitionCountdown >= 0) return false;
            ApparitionCountdown = (minutes < 240 ? 70f : 40f) + (float)(nextRandom?.Invoke() ?? .5) * 50f;
            return true;
        }

        public static float ComputeTension(bool blackout, bool cctvDouble, bool alleyCall, bool starerAlive, bool phone444, double minutes) =>
            blackout || cctvDouble || alleyCall || starerAlive || phone444 ? 1f : (minutes >= 120 && minutes < 130 ? .6f : .1f);

        public static float ComputeWarp(bool red, bool dead, float deathWarp, bool starerAlive, float starerLook, bool cctvDouble) =>
            Mathf.Max(red ? .5f : 0, dead ? deathWarp : 0, starerAlive ? Mathf.Max(0, starerLook) / 3f : 0, cctvDouble ? .3f : 0);

        public static float ComputeGlitch(bool dead, float deathGlitch, float tension) =>
            dead ? deathGlitch : (tension > .9f ? .06f : 0);

        public static bool TryApparitionSpot(Vector2 playerPos, Vector2 forward, int side, float angleFactor, out Vector2 spot)
        {
            if (forward.sqrMagnitude < .0001f) forward = Vector2.up;
            else forward.Normalize();
            float ang = (side >= 0 ? 1 : -1) * (1.25f + Mathf.Clamp01(angleFactor) * .35f);
            float c = Mathf.Cos(ang), s = Mathf.Sin(ang);
            var dir = new Vector2(forward.x * c - forward.y * s, forward.x * s + forward.y * c);
            for (float r = 5.5f; r > 2.51f; r -= .5f)
            {
                float x = playerPos.x + dir.x * r, z = playerPos.y + dir.y * r;
                if (x > .4f && x < 11.6f && z > .4f && z < 7.6f)
                {
                    spot = new Vector2(x, z);
                    return true;
                }
            }
            spot = default;
            return false;
        }

        public static bool ShouldDismissApparition(float age, Vector2 cameraPos, Vector2 cameraForward, Vector2 actorPos, out bool lookedAt)
        {
            var to = actorPos - cameraPos;
            if (to.sqrMagnitude > .0001f) to.Normalize();
            if (cameraForward.sqrMagnitude > .0001f) cameraForward.Normalize();
            lookedAt = Vector2.Dot(cameraForward, to) > .82f;
            return lookedAt || age > 5f;
        }
    }
}
