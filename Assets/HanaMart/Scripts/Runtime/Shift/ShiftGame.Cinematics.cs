using System.Collections.Generic;
using HanaMart.Actors;
using HanaMart.Render;
using HanaMart.Store;
using UnityEngine;

namespace HanaMart.Shift
{
    public sealed partial class ShiftGame
    {
        public ShiftCinematics Cinematics { get; } = new();
        public bool Letterbox => Cinematics.Letterbox;
        public int ActiveApparitions => apparitions.Count;

        readonly List<(Actor actor, float age)> apparitions = new();
        readonly Dictionary<string, AudioSource> ambientSources = new();
        Camera cineView;
        FilmSettings cineFilm;
        bool cineShaking;

        void InitializeCinematics()
        {
            cineView = GetComponent<Camera>();
            cineFilm = GetComponent<FilmSettings>();
            foreach (string id in new[] { "amb_rain_inside", "amb_rain_outside", "amb_hum", "amb_hvac", "amb_drone", "amb_breath" })
            {
                var clip = audioBank ? audioBank.Find(id) : null;
                if (!clip) continue;
                var source = gameObject.AddComponent<AudioSource>();
                source.clip = clip;
                source.loop = true;
                source.playOnAwake = false;
                source.volume = 0;
                source.Play();
                ambientSources[id] = source;
            }
        }

        public void TriggerCinematic(float duration = 2.2f, float zoom = .35f, float slow = .45f, bool sting = true)
        {
            Cinematics.Begin(duration, zoom, slow);
            if (sting) PlayClip("sting");
        }

        public bool SpawnApparition()
        {
            if (!actorCatalog) return false;
            var forward = ThreeCoords.ToUnity(transform.forward);
            if (!ShiftCinematics.TryApparitionSpot(player.Motor.pos, new Vector2(forward.x, forward.z), RandomValue() < .5f ? 1 : -1, RandomValue(), out var spot))
                return false;
            var actor = Actor.Spawn(actorCatalog, new ActorOptions
                { coat = "raincoat", color = 0x0c0c0e, hood = true, face = "none", pale = true, uncanny = 1, tall = 1.06f, noShadow = true },
                new Vector3(spot.x, 0, spot.y));
            actor.name = "Bóng rìa mắt";
            actor.lookTarget = ThreeCoords.ToUnity(transform.position);
            apparitions.Add((actor, 0));
            PlayAt("whisper", new Vector3(spot.x, 1.5f, spot.y));
            return true;
        }

        void UpdateApparitions(float dt)
        {
            var camPos3 = ThreeCoords.ToUnity(transform.position);
            var camFwd3 = ThreeCoords.ToUnity(transform.forward);
            var camPos = new Vector2(camPos3.x, camPos3.z);
            var camFwd = new Vector2(camFwd3.x, camFwd3.z);
            for (int i = apparitions.Count - 1; i >= 0; --i)
            {
                var entry = apparitions[i];
                if (!entry.actor || !entry.actor.alive) { apparitions.RemoveAt(i); continue; }
                entry.age += dt;
                if (ShiftCinematics.ShouldDismissApparition(entry.age, camPos, camFwd, new Vector2(entry.actor.pos.x, entry.actor.pos.z), out bool lookedAt))
                {
                    if (lookedAt) { Cinematics.TriggerLookSpike(); PlayClip("sting"); }
                    entry.actor.Remove();
                    apparitions.RemoveAt(i);
                }
                else apparitions[i] = entry;
            }
            if (Cinematics.TickApparitionTimer(dt, Run.Clock.Minutes, HasFlag("red"), HasFlag("blackout"), Dead, random.NextDouble))
                SpawnApparition();
        }

        void ClearApparitions()
        {
            foreach (var entry in apparitions) if (entry.actor) entry.actor.Remove();
            apparitions.Clear();
        }

        void UpdateCinematicsLate(float realDt)
        {
            bool starerAlive = starer != null && starer.Actor && starer.Actor.alive;
            float tension = ShiftCinematics.ComputeTension(
                HasFlag("blackout"), HasFlag("double"), HasFlag("alleyCall"), starerAlive,
                phone.Kind == "444" || phone.Kind == "clock444", Run.Clock.Minutes);
            Cinematics.Tick(realDt, death != null, Dead, tension);
            UpdateAmbientAudio(realDt);
            if (death != null) return;

            var cam = cineView ? cineView : (cineView = GetComponent<Camera>());
            if (cam) cam.fieldOfView = player.CurrentFov * (1 + (Ended ? 0 : Cinematics.Dolly));
            if (Cinematics.Shake > .001f && !Dead && !Ended && !player.ExternalView)
            {
                player.ApplyPose();
                float s = Cinematics.Shake * Cinematics.Shake * .05f * Mathf.Rad2Deg;
                cam.transform.rotation *= Quaternion.Euler((RandomValue() - .5f) * s, (RandomValue() - .5f) * s, (RandomValue() - .5f) * s);
                cineShaking = true;
            }
            else if (cineShaking)
            {
                cineShaking = false;
                if (!Dead && !Ended && !player.ExternalView) player.ApplyPose();
            }

            var fs = cineFilm ? cineFilm : (cineFilm = GetComponent<FilmSettings>());
            if (!fs) return;
            float targetWarp = ShiftCinematics.ComputeWarp(HasFlag("red"), false, 0, starerAlive, starerLook, HasFlag("double"));
            fs.pulse = Cinematics.Pulse;
            fs.glitch = ShiftCinematics.ComputeGlitch(false, 0, tension);
            fs.warp = Mathf.Lerp(fs.warp, targetWarp, 1 - Mathf.Exp(-3f * realDt));
            fs.Push();
        }

        void UpdateAmbientAudio(float dt)
        {
            if (ambientSources.Count == 0) return;
            var mix = AmbientMix.Compute(
                insidePower, outsidePower, player.Motor.pos.y < -4.5f && !Dead && !Ended,
                redHour != null && redHour.Dark, HasFlag("double"), HasFlag("alleyCall"), Dead, Cinematics.Tension);
            float k = 1 - Mathf.Exp(-6f * dt);
            SetVolume("amb_rain_inside", mix.RainInside, k);
            SetVolume("amb_rain_outside", mix.RainOutside, k);
            SetVolume("amb_hum", mix.Hum, k);
            SetVolume("amb_hvac", mix.Hvac, k);
            SetVolume("amb_drone", mix.Drone, k);
            SetVolume("amb_breath", mix.Breath, k);
        }

        void SetVolume(string id, float target, float blend)
        {
            if (ambientSources.TryGetValue(id, out var source) && source)
                source.volume = Mathf.Lerp(source.volume, target, blend);
        }

        void ResetCinematics()
        {
            Cinematics.Reset();
            ClearApparitions();
            cineShaking = false;
            var fs = cineFilm ? cineFilm : (cineFilm = GetComponent<FilmSettings>());
            if (fs) { fs.pulse = fs.glitch = fs.warp = 0; fs.Push(); }
            var cam = cineView ? cineView : (cineView = GetComponent<Camera>());
            if (cam && player) cam.fieldOfView = player.CurrentFov;
        }

        void ReleaseCinematics()
        {
            ResetCinematics();
            foreach (var source in ambientSources.Values) if (source) source.Stop();
            ambientSources.Clear();
        }
    }
}
