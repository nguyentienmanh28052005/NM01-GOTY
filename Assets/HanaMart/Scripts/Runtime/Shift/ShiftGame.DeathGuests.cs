using System.Collections;
using System.Linq;
using HanaMart.Actors;
using HanaMart.Store;
using UnityEngine;

namespace HanaMart.Shift
{
    public sealed partial class ShiftGame
    {
        IEnumerator DeathCounter(DeathStage stage)
        {
            var actor = PlayingActor(counterCustomer) ?? DeathActor(stage, new ActorOptions
                { coat = "raincoat", color = 0xf2c200, hood = true, face = "wide", pale = true, uncanny = 1 }, new Vector3(2.45f, 0, 4.4f));
            FaceActor(actor, stage.Eye);
            actor.lookTarget = stage.Eye;
            stage.Aim(null, Head(actor), 2.5f, 2.2f);
            stage.WarpTarget = .15f;
            PlayClip("sting");
            var lights = lamps.Where(light => light.Inside && light.Lamp.transform.position.z >= 0 && light.Lamp.transform.position.y > 2.5f)
                .OrderByDescending(light => HorizontalDistance(ThreeCoords.ToUnity(light.Lamp.transform.position), 2.4f, 4.4f)).ToArray();
            for (int index = 0; index < lights.Length - 1; ++index)
            {
                ShutLamp(lights[index]);
                yield return stage.Wait(.09f);
            }
            if (lights.Length > 0) lights[lights.Length - 1].FlickerUntil = Time.time + 8;
            yield return stage.Wait(.5f);
            if (stage.Cut.CounterTurnsHead(stage.Rule))
            {
                for (float time = 0; time < 1.6f; time += stage.Dt)
                {
                    actor.Motion.headSpin = Mathf.PI * 2 * Mathf.Pow(Mathf.Clamp01((time + stage.Dt) / 1.6f), 2);
                    yield return null;
                }
                actor.Motion.headSpin = 0;
                actor.Motion.revealTarget = 1;
            }
            else
            {
                actor.Motion.revealTarget = 1;
                for (float time = 0; time < 1.4f; time += stage.Dt) { stage.Aim(null, Head(actor)); yield return null; }
            }
            stage.DollyTarget = -.22f;
            stage.WarpTarget = .6f;
            PlayAt("whisper", stage.Eye);
            yield return stage.Wait(.7f);
            actor.Motion.armUp = 1;
            var eye = stage.Eye;
            Scare(stage);
            stage.RedTarget = .55f;
            stage.GlitchTarget = 1;
            yield return MoveDeathActor(stage, actor, new Vector3(eye.x + .55f, 0, eye.z), .32f, 6, 9);
            stage.Aim(new Vector3(1.5f, 1.2f, stage.Eye.z), Head(actor), 5, 6);
            stage.Camera.Roll = .5f;
            yield return stage.Wait(.35f);
            stage.CutTo(1);
            PlayClip("thud");
            yield return stage.Wait(.9f);
        }

        IEnumerator DeathChildMom(DeathStage stage)
        {
            var kid = PlayingActor(counterCustomer);
            var mother = PlayingActor(counterCustomer?.Companion) ?? DeathActor(stage, new ActorOptions
                { color = 0x1a1a1a, longHair = true, hair = 0x050505, face = "none", pale = true, uncanny = 1, tall = 1.12f }, new Vector3(5.2f, 0, 8.45f));
            mother.lookTarget = stage.Eye;
            mother.Motion.revealTarget = 1;
            stage.WarpTarget = .2f;
            float x = Mathf.Clamp(mother.pos.x, 4.2f, 9);
            yield return MoveDeathActor(stage, mother, new Vector3(x, 0, 8.14f), .6f, aim: false);
            stage.Aim(null, new Vector3(x, 1.75f, 8), 2.2f, 2.5f);
            var hand = Lit(0xd9cfc6, .4f);
            stage.Objects.Add(hand);
            foreach (float offset in new[] { -.32f, .32f })
                DeathBlock(stage, "Tay áp kính", new Vector3(x + offset, 1.55f, 8.07f), new Vector3(.11f, .18f, .02f), hand);
            for (int tap = 0; tap < 3; ++tap)
            {
                yield return stage.Wait(.45f);
                PlayClip("glass_knock");
                stage.Shake = Mathf.Max(stage.Shake, .35f);
            }
            GlassCrack(stage, new Vector3(x, 1.6f, 7.98f), 1.4f);
            PlayClip("thud");
            stage.Shake = .8f;
            stage.White = 1;
            yield return stage.Wait(.9f);
            var eye = stage.Eye;
            kid ??= DeathActor(stage, new ActorOptions { coat = "child", color = 0xffb703, hair = 0x101010, face = "wide" }, new Vector3(2, 0, 4.4f));
            kid.pos = DeathSpot(stage, .6f, 1.2f);
            FaceActor(kid, eye);
            kid.lookTarget = eye;
            kid.Motion.revealTarget = 1;
            stage.Aim(null, new Vector3(kid.pos.x, .95f, kid.pos.z), 2, 2.6f);
            Say("Em bé: ...Mẹ vào được rồi.", 3);
            yield return stage.Wait(1.4f);
            DeathDark();
            PlayClip("power_down");
            yield return stage.Wait(.6f);
            kid.Motion.armUp = 1;
            Scare(stage);
            stage.RedTarget = .4f;
            stage.GlitchTarget = 1;
            stage.DollyTarget = -.3f;
            var initial = kid.pos;
            for (float time = 0; time < .25f; time += stage.Dt)
            {
                float step = time + stage.Dt;
                kid.pos = Vector3.Lerp(initial, eye, step * 1.6f);
                kid.pos.y = Mathf.Sin(Mathf.Clamp01(step / .25f) * Mathf.PI) * .6f;
                stage.Aim(null, Head(kid), 8, 9);
                yield return null;
            }
            stage.White = 1;
            stage.CutTo(1);
            yield return stage.Wait(1);
        }

        IEnumerator DeathStorage(DeathStage stage)
        {
            var actor = PlayingActor(counterCustomer) ?? DeathActor(stage, new ActorOptions
                { coat = "manager", color = 0x14213d, longHair = true, hair = 0x0b0b0b, face = "wide", pale = true, uncanny = 1 }, new Vector3(2.4f, 0, 4.4f));
            actor.lookTarget = stage.Eye;
            stage.WarpTarget = .1f;
            stage.Aim(null, Head(actor), 2.5f, 3);
            Say(stage.Rule switch
            {
                20 => "Người phụ nữ: ...Cảm ơn. Ở trong này ấm quá.",
                19 => "Nhân viên HANA: Cảm ơn. Từ giờ chỗ này là của tớ.",
                _ => (counterCustomer?.Profile.Name ?? "Quản lý") + ": Cảm ơn em. Chị chỉ cần... một chút thôi.",
            }, 3);
            yield return stage.Wait(1.2f);
            foreach (var point in new[] { new Vector3(1.6f, 0, 3), new Vector3(.8f, 0, 1), new Vector3(.8f, 0, -.9f) })
                yield return MoveDeathActor(stage, actor, point, Vector3.Distance(actor.pos, point) / 1.1f, speed: 1.1f);
            HideForDeath(stage, actor.Model);
            PlayClip("door");
            yield return stage.Wait(1.3f);
            var eye = stage.Eye;
            var ordered = lamps.Where(light => light.Inside && light.Lamp.transform.position.y > 2.5f).OrderBy(light => light.Lamp.transform.position.z).ToArray();
            var nearest = ordered.OrderBy(light => HorizontalDistance(ThreeCoords.ToUnity(light.Lamp.transform.position), eye.x, eye.z)).FirstOrDefault();
            foreach (var lamp in ordered)
            {
                if (lamp == nearest) continue;
                ShutLamp(lamp);
                stage.Shake = Mathf.Max(stage.Shake, .08f);
                yield return stage.Wait(.16f);
            }
            var at = nearest == null ? eye + Vector3.up : ThreeCoords.ToUnity(nearest.Lamp.transform.position);
            at.y = 2.95f;
            stage.Aim(null, at, 2.2f, 3);
            if (nearest != null) nearest.FlickerUntil = Time.time + 1.1f;
            yield return stage.Wait(1.1f);
            if (nearest != null) nearest.Off = true;
            yield return stage.Wait(.5f);
            actor.Model.SetActive(true);
            actor.pos = DeathSpot(stage, .5f);
            FaceActor(actor, eye);
            actor.Motion.reveal = actor.Motion.revealTarget = 1;
            actor.Motion.headSpin = .35f;
            if (nearest != null) { nearest.Off = false; nearest.FlickerUntil = 0; }
            stage.Snap(null, at);
            stage.Aim(null, Head(actor), 1.6f, 1.8f);
            yield return stage.Wait(1.1f);
            Scare(stage);
            stage.RedTarget = .5f;
            stage.GlitchTarget = 1;
            stage.DollyTarget = -.35f;
            actor.Motion.armUp = 1;
            yield return stage.Wait(.3f);
            stage.CutTo(1);
            yield return stage.Wait(.9f);
        }

        IEnumerator DeathMirae(DeathStage stage)
        {
            var actor = PlayingActor(counterCustomer) ?? DeathActor(stage, new ActorOptions
                { color = 0x6d597a, longHair = true, hair = 0x2b1d14, face = "wide", pale = true, uncanny = 1 }, new Vector3(2.4f, 0, 4.4f));
            actor.lookTarget = stage.Eye;
            stage.WarpTarget = .2f;
            stage.Aim(null, Head(actor), 2.5f, 3);
            Say("\"Chị Mi-rae\": Hì... hì hì... Cảm ơn em đã ở lại.");
            PlayAt("whisper", stage.Eye);
            stage.ClockSpin = true;
            for (float time = 0; time < 1.8f; time += stage.Dt)
            {
                actor.Motion.headRoll = Mathf.Clamp01((time + stage.Dt) / 1.8f) * 1.5f;
                yield return null;
            }
            Power(insidePower, false);
            PlayClip("power_down");
            var coats = new[] { "padding", "raincoat", "uniform", "office" };
            for (int index = 0; index < 6; ++index)
            {
                var follower = DeathActor(stage, new ActorOptions
                    { coat = coats[index % coats.Length], color = 0x0a0a0a, face = "none", pale = true, uncanny = 1, noShadow = true }, new Vector3(4.1f + index * 1.25f, 0, 8.55f));
                follower.yaw = Mathf.PI;
                follower.lookTarget = stage.Eye;
                follower.Motion.revealTarget = .6f;
            }
            stage.Aim(null, new Vector3(7, 1.5f, 8), 1.4f, 1.6f);
            Say("(Ngoài cửa kính không còn con phố nào. Chỉ có họ.)", 3);
            yield return stage.Wait(2.4f);
            stage.Aim(null, Head(actor), 1.8f, 2.2f);
            actor.Motion.revealTarget = 1;
            yield return stage.Wait(1);
            var eye = stage.Eye;
            actor.pos = DeathSpot(stage, .55f);
            FaceActor(actor, eye);
            stage.Snap(null, Head(actor));
            actor.Motion.armUp = 1;
            Scare(stage);
            stage.RedTarget = .35f;
            stage.GlitchTarget = .8f;
            stage.DollyTarget = -.25f;
            stage.FadeRate = .9f;
            stage.FadeTarget = 1;
            yield return stage.Wait(2.4f);
        }

        static Actor PlayingActor(Guest guest) => guest != null && guest.Actor && guest.Actor.alive ? guest.Actor : null;
        static float HorizontalDistance(Vector3 p, float x, float z) => (new Vector2(p.x - x, p.z - z)).magnitude;

        void ShutLamp(ShopLight light)
        {
            light.Off = true;
            PlayAt("buzz", ThreeCoords.ToUnity(light.Lamp.transform.position));
        }

        void DeathDark()
        {
            Power(false, outsidePower);
            ApplyAmbient(.02f, Color.white);
            RenderSettings.reflectionIntensity = baseReflection * .04f;
        }

        static void HideForDeath(DeathStage stage, GameObject item)
        {
            stage.Hidden.Add((item, item.activeSelf));
            item.SetActive(false);
        }

        IEnumerator MoveDeathActor(DeathStage stage, Actor actor, Vector3 destination, float duration,
            float? cameraPositionRate = null, float? cameraRotationRate = null, bool aim = true, float speed = 0)
        {
            var initial = actor.pos;
            bool auto = actor.AutoTick;
            actor.AutoTick = false;
            for (float time = 0; time < duration; time += stage.Dt)
            {
                actor.pos = Vector3.Lerp(initial, destination, (time + stage.Dt) / duration);
                actor.StepPose(stage.Dt, speed);
                if (aim) stage.Aim(null, Head(actor), cameraPositionRate, cameraRotationRate);
                yield return null;
            }
            actor.pos = destination;
            actor.AutoTick = auto;
        }

        static Transform DeathBlock(DeathStage stage, string label, Vector3 position, Vector3 size, Material material)
        {
            var root = new GameObject(label);
            root.transform.position = ThreeCoords.ToUnity(position);
            stage.Objects.Add(root);
            return Block(root.transform, label, Vector3.zero, size, material);
        }

        void GlassCrack(DeathStage stage, Vector3 position, float size)
        {
            const int resolution = 512;
            var pixels = new Color32[resolution * resolution];
            var center = new Vector2(256, 256);
            for (int index = 0; index < 13; ++index)
                Branch(center, index * 2 * Mathf.PI / 13 + RandomValue() * .3f, 60 + RandomValue() * 50, 3, 0);
            foreach (float radius in new[] { 28, 60, 105 })
            {
                Vector2 first = Vector2.zero, previous = Vector2.zero;
                for (int i = 0; i <= 20; ++i)
                {
                    float angle = i * 2 * Mathf.PI / 20;
                    var sample = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (radius * (.8f + RandomValue() * .4f));
                    if (i == 0) first = sample;
                    else Line(previous, sample, 1.2f);
                    previous = sample;
                }
                Line(previous, first, 1.2f);
            }
            var texture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false) { name = "Vết nứt kính" };
            texture.SetPixels32(pixels);
            texture.Apply();
            var material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            material.mainTexture = texture;
            MakeTransparent(material);
            var glass = Decal("Kính nứt", position, Quaternion.Euler(0, 180, 0), new Vector2(size, size), material);
            dynamics.Remove(glass);
            stage.Objects.Add(glass);
            stage.Objects.Add(material);
            stage.Objects.Add(texture);

            void Branch(Vector2 from, float angle, float length, float width, int depth)
            {
                if (depth > 5 || length < 6) return;
                var to = from + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * length;
                var bend = (from + to) * .5f + new Vector2(RandomValue() - .5f, RandomValue() - .5f) * (length * .3f);
                var last = from;
                for (int piece = 1; piece <= 12; ++piece)
                {
                    float t = piece / 12f, u = 1 - t;
                    var point = u * u * from + 2 * u * t * bend + t * t * to;
                    Line(last, point, width);
                    last = point;
                }
                Branch(to, angle + (RandomValue() - .5f) * .9f, length * .72f, width * .7f, depth + 1);
                if (RandomValue() < .55f)
                    Branch(to, angle + (RandomValue() < .5f ? 1 : -1) * (.5f + RandomValue() * .6f), length * .55f, width * .6f, depth + 1);
            }

            void Line(Vector2 from, Vector2 to, float width)
            {
                var direction = to - from;
                float denominator = Mathf.Max(.0001f, direction.sqrMagnitude);
                int left = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(from.x, to.x) - width));
                int right = Mathf.Min(resolution - 1, Mathf.CeilToInt(Mathf.Max(from.x, to.x) + width));
                int bottom = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(from.y, to.y) - width));
                int top = Mathf.Min(resolution - 1, Mathf.CeilToInt(Mathf.Max(from.y, to.y) + width));
                for (int y = bottom; y <= top; ++y)
                    for (int x = left; x <= right; ++x)
                    {
                        var sample = new Vector2(x, y) - from;
                        float along = Mathf.Clamp01(Vector2.Dot(sample, direction) / denominator);
                        float gap = (sample - direction * along).magnitude;
                        byte alpha = (byte)(195 * Mathf.Clamp01(width * .5f + .6f - gap));
                        int pixel = y * resolution + x;
                        if (alpha > pixels[pixel].a) pixels[pixel] = new Color32(235, 245, 255, alpha);
                    }
            }
        }
    }
}
