using System;
using System.Collections;
using System.Collections.Generic;
using HanaMart.Actors;
using HanaMart.Render;
using HanaMart.Store;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HanaMart.Shift
{
    public sealed partial class ShiftGame
    {
        sealed class DeathStage
        {
            public Camera View;
            public FilmSettings Film;
            public DeathCamera Camera;
            public DeathCut Cut;
            public int Rule;
            public string Reason;
            public float Fade, FadeTarget, FadeRate = 6, Red, RedTarget, Glitch = .6f, GlitchTarget, Warp = .3f, WarpTarget;
            public float Dolly, DollyTarget, Shake = .6f, White;
            public bool Shown, ClockSpin;
            public readonly Stack<IEnumerator> Sequence = new();
            public readonly List<Actor> Actors = new();
            public readonly List<UnityEngine.Object> Objects = new();
            public readonly List<(GameObject item, bool active)> Hidden = new();
            public readonly List<(ShopLight light, bool off, float flicker, Color color)> Lights = new();
            public readonly List<Action> Restore = new();
            public float SavedCine, SavedFade, SavedRed, SavedGlitch, SavedWarp, SavedWhite, SavedPulse, SavedAmbient, SavedReflection, SavedLitAmbient, SavedAmbientLevel;
            public Color SavedLitTint, SavedAmbientTint;
            public bool InsidePower, OutsidePower;
            public float Dt => Cut.Delta;

            public Vector3 Eye => ThreeCoords.ToUnity(Camera.Position);
            public Vector3 Forward
            {
                get
                {
                    var direction = ThreeCoords.ToUnity(Camera.Rotation * Vector3.forward);
                    direction.y = 0;
                    return direction.normalized;
                }
            }

            public IEnumerator Wait(float seconds)
            {
                for (float remaining = seconds; remaining > 0; remaining -= Dt) yield return null;
            }

            public void Aim(Vector3? position, Vector3? look, float? positionRate = null, float? rotationRate = null)
            {
                if (position.HasValue) Camera.TargetPosition = ThreeCoords.ToUnity(position.Value);
                if (look.HasValue) Camera.LookAt = ThreeCoords.ToUnity(look.Value);
                if (positionRate.HasValue) Camera.PositionRate = positionRate.Value;
                if (rotationRate.HasValue) Camera.RotationRate = rotationRate.Value;
            }

            public void Snap(Vector3? position, Vector3? look) => Camera.Snap(
                position.HasValue ? ThreeCoords.ToUnity(position.Value) : null,
                look.HasValue ? ThreeCoords.ToUnity(look.Value) : null);

            public void CutTo(float value) => Fade = FadeTarget = value;
        }

        readonly DeathCuts deathCuts = new();
        DeathStage death;
        public DeathCut ActiveDeath => death?.Cut;
        public string ClockCaption { get; private set; }

        void BeginDeath(int rule, string reason)
        {
            StopDeath();
            StopAllCoroutines();
            foreach (var guest in guests) guest.Actor.SetPath(null);
            var view = GetComponent<Camera>();
            var stage = new DeathStage
            {
                View = view, Film = GetComponent<FilmSettings>(), Rule = rule, Reason = reason,
                Camera = new DeathCamera(view.transform.position, view.transform.rotation), Cut = deathCuts.Begin(rule),
                InsidePower = insidePower, OutsidePower = outsidePower,
                SavedAmbient = RenderSettings.ambientIntensity, SavedReflection = RenderSettings.reflectionIntensity,
                SavedLitAmbient = litAmbient, SavedLitTint = litTint, SavedAmbientLevel = ambientLevel, SavedAmbientTint = ambientTint,
            };
            if (stage.Film)
            {
                stage.SavedCine = stage.Film.cine;
                stage.SavedFade = stage.Film.fade;
                stage.SavedRed = stage.Film.red;
                stage.SavedGlitch = stage.Film.glitch;
                stage.SavedWarp = stage.Film.warp;
                stage.SavedWhite = stage.Film.white;
                stage.SavedPulse = stage.Film.pulse;
            }
            foreach (var light in lamps) stage.Lights.Add((light, light.Off, light.FlickerUntil, light.Lamp.color));
            stage.Sequence.Push(stage.Cut.Kind switch
            {
                "counter" => DeathCounter(stage),
                "childMom" => DeathChildMom(stage),
                "storage" => DeathStorage(stage),
                "mirae" => DeathMirae(stage),
                "phone" => DeathPhone(stage),
                "freezer" => DeathFreezer(stage),
                "cctv" => DeathCctv(stage),
                "box13" => DeathBox(stage),
                "alley" => DeathAlley(stage),
                "blackout" => DeathBlackout(stage),
                "red" => DeathRed(stage),
                "chase" => DeathChase(stage),
                "fired" => DeathFired(stage),
                _ => DeathLunge(stage),
            });
            player.ExternalView = true;
            death = stage;
        }

        void LateUpdate()
        {
            var stage = death;
            if (stage == null) return;
            float dt = Mathf.Min(.05f, Time.unscaledDeltaTime);
            stage.Cut.Tick(dt);
            if (!stage.Shown && (confirmInput?.WasPressedThisFrame() == true || player.Input?.FindAction("Use")?.WasPressedThisFrame() == true ||
                Mouse.current?.leftButton.wasPressedThisFrame == true)) SkipDeath();
            if (!stage.Cut.Done) ContinueDeath(stage);
            UpdateDeathCamera(stage, dt);
            if (stage.Cut.Done && !stage.Shown && stage.Cut.Time > .4f) PresentDeath(stage);
        }

        public bool SkipDeath()
        {
            if (death == null || !death.Cut.Skip()) return false;
            death.CutTo(1);
            death.Sequence.Clear();
            return true;
        }

        void ContinueDeath(DeathStage stage)
        {
            try
            {
                for (int command = 0; command < 256; ++command)
                {
                    if (stage.Sequence.Count == 0) { stage.Cut.Finish(); return; }
                    var routine = stage.Sequence.Peek();
                    if (!routine.MoveNext()) { stage.Sequence.Pop(); continue; }
                    if (routine.Current is IEnumerator child) { stage.Sequence.Push(child); continue; }
                    return;
                }
                stage.Cut.Finish();
            }
            catch (Exception error)
            {
                Debug.LogException(error);
                stage.Sequence.Clear();
                stage.CutTo(1);
                stage.Cut.Finish();
            }
        }

        void UpdateDeathCamera(DeathStage stage, float dt)
        {
            stage.Camera.Tick(dt);
            var position = stage.Camera.Position;
            var orientation = stage.Camera.Rotation;
            if (stage.Shake > .001f)
            {
                float amount = stage.Shake * stage.Shake * .06f;
                orientation *= Quaternion.Euler((RandomValue() - .5f) * amount * Mathf.Rad2Deg, (RandomValue() - .5f) * amount * Mathf.Rad2Deg, 0);
                position.x += (RandomValue() - .5f) * amount * .15f;
                stage.Shake = Mathf.Max(0, stage.Shake - dt * 1.4f);
            }
            stage.View.transform.SetPositionAndRotation(position, orientation);
            stage.Fade = Smooth(stage.Fade, stage.FadeTarget, stage.FadeRate, dt);
            stage.Red = Smooth(stage.Red, stage.RedTarget, 5, dt);
            stage.Glitch = Smooth(stage.Glitch, stage.GlitchTarget, 6, dt);
            stage.Warp = Smooth(stage.Warp, stage.WarpTarget, 3, dt);
            stage.Dolly = Smooth(stage.Dolly, stage.DollyTarget, 3, dt);
            stage.White = Smooth(stage.White, 0, 14, dt);
            stage.View.fieldOfView = player.CurrentFov * (1 + stage.Dolly);
            foreach (var light in stage.Lights)
            {
                var state = light.light;
                float flicker = state.FlickerUntil > Time.time && Mathf.Sin(Time.time * 47 + state.Lamp.GetInstanceID()) < .35f ? .19f : 1;
                ApplyLamp(state, state.Off || !(state.Inside ? insidePower : outsidePower) ? 0 : flicker);
            }
            if (stage.ClockSpin && !stage.Shown) ClockCaption = $"{RandomIndex(24):00}:{RandomIndex(60):00}";
            if (!stage.Film) return;
            stage.Film.cine = 1;
            stage.Film.fade = stage.Fade;
            stage.Film.red = stage.Red;
            stage.Film.warp = stage.Warp;
            stage.Film.glitch = stage.Glitch;
            stage.Film.white = stage.White;
            stage.Film.pulse = 0;
            stage.Film.Push();
        }

        static float Smooth(float current, float goal, float rate, float dt) => Mathf.Lerp(current, goal, 1 - Mathf.Exp(-rate * dt));

        void PresentDeath(DeathStage stage)
        {
            stage.Shown = true;
            stage.CutTo(1);
            stage.Sequence.Clear();
            ClockCaption = null;
            var title = stage.Rule == 0 ? "CA ĐÊM CHẤM DỨT" : $"Quy tắc số {stage.Rule}";
            ShowDialog(title, stage.Reason, new Choice($"Tiếp tục từ {ShiftClock.Format(Run.Progress.SavedHour * 60)}", Retry));
        }

        void StopDeath()
        {
            if (death == null) return;
            var stage = death;
            death = null;
            ClockCaption = null;
            if (player) { player.ExternalView = false; player.ApplyPose(); }
            stage.Sequence.Clear();
            foreach (var actor in stage.Actors) if (actor) actor.Remove();
            foreach (var item in stage.Objects) if (item) Destroy(item);
            foreach (var state in stage.Hidden) if (state.item) state.item.SetActive(state.active);
            litAmbient = stage.SavedLitAmbient;
            litTint = stage.SavedLitTint;
            foreach (var state in stage.Lights)
            {
                state.light.Off = state.off;
                state.light.FlickerUntil = state.flicker;
                if (state.light.Lamp) state.light.Lamp.color = state.color;
            }
            foreach (var restore in stage.Restore) restore();
            Power(stage.InsidePower, stage.OutsidePower);
            ApplyAmbient(stage.SavedAmbientLevel, stage.SavedAmbientTint);
            RenderSettings.ambientIntensity = stage.SavedAmbient;
            RenderSettings.reflectionIntensity = stage.SavedReflection;
            if (!stage.Film) return;
            stage.Film.cine = stage.SavedCine;
            stage.Film.fade = stage.SavedFade;
            stage.Film.red = stage.SavedRed;
            stage.Film.glitch = stage.SavedGlitch;
            stage.Film.warp = stage.SavedWarp;
            stage.Film.white = stage.SavedWhite;
            stage.Film.pulse = stage.SavedPulse;
            stage.Film.Push();
        }

        Vector3 DeathSpot(DeathStage stage, float distance, float preference = 0) =>
            DeathStaging.FindSpot(stage.Eye, stage.Forward, distance, preference, player.Navigation.Walkable);

        Actor DeathActor(DeathStage stage, ActorOptions look, Vector3 position)
        {
            var actor = Actor.Spawn(actorCatalog, look, position);
            actor.name = "Cảnh thua · " + stage.Cut.Kind;
            stage.Actors.Add(actor);
            return actor;
        }

        static Vector3 Head(Actor actor) => actor.pos + Vector3.up * (1.58f * actor.Options.tall + actor.Motion.reveal * .3f);
        static void FaceActor(Actor actor, Vector3 point) => actor.yaw = Mathf.Atan2(point.x - actor.pos.x, point.z - actor.pos.z);

        void Scare(DeathStage stage)
        {
            PlayClip("scare");
            player.Rumble(1, 1, .7f);
            stage.Shake = 1;
        }

        IEnumerator DeathLunge(DeathStage stage)
        {
            var eye = stage.Eye;
            var actor = DeathActor(stage, new ActorOptions
                { coat = "raincoat", color = 0x0b0b0b, hood = true, face = "wide", mask = "smile", pale = true, uncanny = 1, tall = 1.08f },
                DeathSpot(stage, 2.4f));
            FaceActor(actor, eye);
            actor.lookTarget = eye;
            stage.Aim(null, Head(actor), 3, 4);
            foreach (var light in lamps) light.FlickerUntil = Time.time + 4;
            yield return stage.Wait(.6f);
            actor.Motion.revealTarget = 1;
            var from = actor.pos;
            var to = DeathSpot(stage, .5f);
            for (float t = 0; t < .3f; t += stage.Dt)
            {
                actor.pos = Vector3.Lerp(from, to, t / .3f);
                stage.Aim(null, Head(actor), 8, 10);
                yield return null;
            }
            actor.Motion.armUp = 1;
            Scare(stage);
            stage.RedTarget = .6f;
            stage.GlitchTarget = 1;
            stage.DollyTarget = -.3f;
            yield return stage.Wait(.4f);
            stage.CutTo(1);
            yield return stage.Wait(1);
        }
    }
}
