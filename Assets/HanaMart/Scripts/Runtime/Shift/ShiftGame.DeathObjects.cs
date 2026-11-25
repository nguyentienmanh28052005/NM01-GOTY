using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HanaMart.Actors;
using HanaMart.Interaction;
using HanaMart.Store;
using UnityEngine;

namespace HanaMart.Shift
{
    public sealed partial class ShiftGame
    {
        IEnumerator DeathPhone(DeathStage stage)
        {
            var eye = stage.Eye;
            var handset = Marker("R:handset");
            Vector3 phonePosition = handset ? ThreeCoords.ToUnity(handset.transform.position) : new Vector3(.5f, 1.1f, 3.4f);
            stage.WarpTarget = .1f;
            stage.Aim(eye - new Vector3(.05f, .05f, 0), phonePosition, 3, 3);
            yield return stage.Wait(.5f);
            stage.Camera.Roll = .35f;
            stage.Aim(null, eye + new Vector3(3, -.15f, .6f), 2, 1.6f);
            bool own = stage.Rule == 16;
            Say(own ? "Giọng của chính bạn: \"...Đừng quay lại. Nó đứng ngay sau quầy.\"" : "Đầu dây: \"...bốn... bốn... bốn...\"");
            var previous = PosDisplay;
            stage.Restore.Add(() => PosDisplay = previous);
            PosDisplay = own ? "\nSAU LƯNG\n" : "04:44\n04:44\n04:44";
            SaveMonitor(stage);
            for (int burst = 0; burst < 3; ++burst)
            {
                for (int blink = 0; blink < 4; ++blink)
                {
                    Power(false, outsidePower);
                    MonitorBright(.2f);
                    yield return stage.Wait(.07f);
                    Power(true, outsidePower);
                    MonitorBright(1.4f);
                    yield return stage.Wait(.07f);
                }
                stage.WarpTarget = .3f + burst * .25f;
                PlayAt("buzz", new Vector3(1.5f, 2.9f, 4.4f));
                yield return stage.Wait(.4f);
            }
            stage.Camera.Roll = 0;
            Actor creature;
            if (own)
            {
                creature = DeathActor(stage, new ActorOptions { coat = "uniform", color = 0x00a98f, hair = 0x1a1410, face = "none" }, DeathSpot(stage, .75f, Mathf.PI));
                FaceActor(creature, eye);
                creature.lookTarget = stage.Eye;
                creature.StepPose(0, 0);
                stage.Aim(null, Head(creature), 1.6f, 1.1f);
                yield return stage.Wait(2);
                creature.Motion.revealTarget = 1;
                yield return stage.Wait(.6f);
            }
            else
            {
                var forward = stage.Forward;
                var origin = eye.x < 1.3f ? new Vector3(2.25f, -1.7f, eye.z + forward.z * .3f) :
                    new Vector3(eye.x + forward.x * .9f, -1.7f, eye.z + forward.z * .9f);
                creature = DeathActor(stage, new ActorOptions
                    { coat = "raincoat", color = 0x0b0b0b, hood = true, face = "wide", mask = "smile", pale = true, uncanny = 1, tall = 1.05f }, origin);
                FaceActor(creature, eye);
                creature.lookTarget = stage.Eye;
                creature.StepPose(0, 0);
                stage.Aim(null, new Vector3(origin.x, .7f, origin.z), 2.4f, 2.4f);
                yield return MoveDeathActor(stage, creature, new Vector3(origin.x, 0, origin.z), 1.5f);
                creature.Motion.revealTarget = 1;
                yield return stage.Wait(.5f);
            }
            DeathSting(stage, creature, own ? .4f : .5f);
            yield return stage.Wait(.35f);
            stage.CutTo(1);
            PlayClip("ring");
            yield return stage.Wait(1.1f);
        }

        IEnumerator DeathFreezer(DeathStage stage)
        {
            var freezer = new Vector3(3, .85f, -3.95f);
            var leaf = doors ? doors.Freezer : null;
            if (leaf != null)
            {
                var original = leaf.transform.localRotation;
                stage.Restore.Add(() => { if (leaf.transform) leaf.transform.localRotation = original; });
            }
            if (doors)
            {
                bool doorsEnabled = doors.enabled;
                doors.enabled = false;
                stage.Restore.Add(() => { if (doors) doors.enabled = doorsEnabled; });
            }
            stage.WarpTarget = .25f;
            TintStore(0x7fb0ff, 1);
            for (float time = 0; time < .25f; time += stage.Dt)
            {
                FreezerAngle(-1.9f * Mathf.Clamp01((time + stage.Dt) / .25f));
                yield return null;
            }
            FreezerAngle(-1.9f);
            var abyssMat = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { color = Hex(0x0b121c) };
            stage.Objects.Add(abyssMat);
            var abyss = Decal("Lòng tủ đông", new Vector3(freezer.x, .852f, freezer.z), Quaternion.Euler(90, 0, 0), new Vector2(1.32f, .62f), abyssMat);
            dynamics.Remove(abyss);
            stage.Objects.Add(abyss);
            DeathBurst(stage, freezer, new Color32(190, 221, 255, 172), 35, .4f);
            PlayAt("creak", freezer);
            stage.Shake = Mathf.Max(stage.Shake, .4f);
            var eye = stage.Eye;
            var backward = new Vector3(eye.x - freezer.x, 0, eye.z - freezer.z).normalized;
            stage.Aim(new Vector3(freezer.x, 1.75f, freezer.z) + backward * 1.25f, new Vector3(freezer.x, .45f, freezer.z), 2.5f, 3);
            yield return KeepLid(.8f, -1.9f);
            Say("(Từ trong tủ đông, giọng của mẹ bạn: \"...Con ơi, lạnh quá.\")", 3);
            PlayAt("whisper", freezer);
            var skin = Lit(0xe8e2dc, .45f);
            skin.EnableKeyword("_EMISSION");
            skin.SetColor("_EmissionColor", Hex(0x223040) * .2f);
            stage.Objects.Add(skin);
            var arms = new Transform[6];
            for (int arm = 0; arm < arms.Length; ++arm)
            {
                var root = DeathProp(stage, "Tay trong tủ đông", new Vector3(freezer.x - .5f + RandomValue(), .76f, freezer.z - .2f + RandomValue() * .35f));
                arms[arm] = root;
                DeathCylinder(root, new Vector3(0, .45f, 0), new Vector3(.075f, .45f, .075f), skin);
                Block(root, "Bàn tay", new Vector3(0, .95f, 0), new Vector3(.08f, .16f, .03f), skin);
                for (int finger = 0; finger < 4; ++finger)
                    DeathCylinder(root, new Vector3(-.03f + finger * .02f, 1.08f, 0), new Vector3(.014f, .06f, .014f), skin);
                root.localScale = new Vector3(1, .01f, 1);
            }
            for (float time = 0; time < 1.1f; time += stage.Dt)
            {
                FreezerAngle(-1.9f);
                for (int arm = 0; arm < arms.Length; ++arm)
                {
                    var item = arms[arm];
                    float height = .01f + Mathf.Clamp01((time - arm * .08f) / .5f) * (.65f + arm * .06f);
                    item.localScale = new Vector3(1, height, 1);
                    item.rotation = Quaternion.LookRotation(ThreeCoords.ToUnity(stage.Eye) - item.position) * Quaternion.Euler(90, 0, 0);
                }
                yield return null;
            }
            foreach (var arm in arms) arm.localScale = Vector3.one;
            Scare(stage);
            stage.RedTarget = .3f;
            stage.GlitchTarget = .8f;
            stage.Camera.Roll = .7f;
            stage.Aim(new Vector3(freezer.x, .9f, freezer.z + .15f), new Vector3(freezer.x, 0, freezer.z), 4.5f, 5);
            yield return KeepLid(.55f, -1.9f);
            for (float time = 0; time < .18f; time += stage.Dt)
            {
                FreezerAngle(Mathf.Lerp(-1.9f, 0, (time + stage.Dt) / .18f));
                yield return null;
            }
            FreezerAngle(0);
            stage.CutTo(1);
            PlayClip("thud");
            yield return KeepLid(1, 0);
            PlayAt("knock_323", freezer);
            Say("(Bây giờ, người gõ là bạn.)", 3);
            yield return KeepLid(2, 0);

            void FreezerAngle(float radians)
            {
                if (leaf != null) leaf.transform.localRotation = leaf.closed * Quaternion.Euler(radians * Mathf.Rad2Deg, 0, 0);
            }

            IEnumerator KeepLid(float seconds, float angle)
            {
                for (float time = 0; time < seconds; time += stage.Dt) { FreezerAngle(angle); yield return null; }
            }
        }

        IEnumerator DeathCctv(DeathStage stage)
        {
            var eye = stage.Eye;
            var screen = monitor ? ThreeCoords.ToUnity(monitor.transform.position) : new Vector3(.3f, 1.5f, 2.6f);
            stage.WarpTarget = .2f;
            stage.Aim(new Vector3(Mathf.Lerp(eye.x, screen.x, .3f), eye.y, Mathf.Lerp(eye.z, screen.z, .3f)), screen, 2, 3);
            stage.DollyTarget = -.25f;
            Say("(Trên màn hình, \"bạn\" quay đầu lại. Nhìn thẳng vào camera.)", 3);
            if (!cctvDouble)
            {
                cctvDouble = CameraActor(new ActorOptions { coat = "uniform", color = 0x00a98f, face = "none" });
                cctvDouble.pos = new Vector3(2, 0, -2.2f);
            }
            cctvDouble.Motion.reveal = cctvDouble.Motion.revealTarget = 1;
            cameraTick = 21;
            RenderSecurityFeed(true, false);
            SaveMonitor(stage);
            yield return stage.Wait(1.6f);
            Run.Progress.Flags.Remove("double");
            RenderSecurityFeed(false, false);
            for (int blink = 0; blink < 10; ++blink)
            {
                MonitorBright(RandomValue() * 1.6f);
                yield return stage.Wait(.05f);
            }
            MonitorBright(.05f);
            Say("(Màn hình trống. Có tiếng thở ngay sau gáy bạn.)", 3);
            PlayAt("whisper", stage.Eye);
            yield return stage.Wait(1.2f);
            var copy = DeathActor(stage, new ActorOptions { coat = "uniform", color = 0x00a98f, hair = 0x1a1410, face = "none" }, DeathSpot(stage, .6f, Mathf.PI));
            FaceActor(copy, stage.Eye);
            copy.lookTarget = stage.Eye;
            copy.StepPose(0, 0);
            stage.DollyTarget = 0;
            stage.Aim(null, Head(copy), 1.6f, .9f);
            yield return stage.Wait(2.2f);
            copy.Motion.revealTarget = 1;
            yield return stage.Wait(.6f);
            DeathSting(stage, copy, .45f);
            yield return stage.Wait(.3f);
            stage.CutTo(1);
            yield return stage.Wait(1);
        }

        IEnumerator DeathBox(DeathStage stage)
        {
            var eye = stage.Eye;
            var forward = stage.Forward;
            var position = DeathStaging.BoxOrigin(stage.Rule, eye, forward);
            if (stage.Rule != 8 || position != new Vector3(3, 0, -5.55f))
                if (!player.Navigation.Walkable(position.x, position.z)) position = DeathSpot(stage, 1.2f);
            var box = DeathProp(stage, "Thùng thứ 13", position);
            cardboard ??= Lit(0xa98758, .15f);
            var label = Lit(0xf2f2f2, .1f);
            stage.Objects.Add(label);
            Block(box, "Thân hộp", new Vector3(0, .21f, 0), new Vector3(.5f, .42f, .42f), cardboard);
            Block(box, "Nhãn hộp", new Vector3(0, .26f, .211f), new Vector3(.2f, .12f, .002f), label);
            var flaps = new Transform[2];
            for (int f = 0; f < 2; ++f)
            {
                float side = f == 0 ? -1 : 1;
                flaps[f] = new GameObject("Nắp thùng").transform;
                flaps[f].SetParent(box, false);
                flaps[f].localPosition = new Vector3(0, .42f, side * .21f);
                Block(flaps[f], "Bìa", new Vector3(0, 0, -side * .105f), new Vector3(.5f, .01f, .21f), cardboard);
            }
            stage.WarpTarget = .15f;
            stage.Aim(null, position + Vector3.up * .3f, 2.5f, 3);
            for (float time = 0; time < 1.8f; time += stage.Dt)
            {
                float fraction = time / 1.8f;
                box.localRotation = Quaternion.Euler(0, 0, -(RandomValue() - .5f) * .12f * fraction * Mathf.Rad2Deg);
                box.position = ThreeCoords.ToUnity(position + Vector3.up * (RandomValue() < .15f ? .03f * fraction : 0));
                if (RandomValue() < .06f) { PlayClip("thud"); stage.Shake = Mathf.Max(stage.Shake, .15f); }
                yield return null;
            }
            flaps[0].localRotation = Quaternion.Euler(-2.4f * Mathf.Rad2Deg, 0, 0);
            flaps[1].localRotation = Quaternion.Euler(2.4f * Mathf.Rad2Deg, 0, 0);
            DeathBurst(stage, position + .5f * Vector3.up, new Color32(12, 8, 15, 208), 90, 1.2f);
            PlayClip("sting");
            stage.Shake = .6f;
            foreach (var light in lamps) if (light.Inside && light.Lamp.transform.position.z < 0) light.Off = true;
            var creature = DeathActor(stage, new ActorOptions
                { color = 0x050505, longHair = true, hair = 0x030303, face = "wide", pale = true, uncanny = 1, noShadow = true }, position);
            FaceActor(creature, eye);
            creature.lookTarget = stage.Eye;
            creature.Motion.revealTarget = 1;
            for (float time = 0; time < 1; time += stage.Dt)
            {
                float size = .25f + Mathf.Clamp01(time) * .8f;
                creature.transform.localScale = Vector3.one * size;
                stage.Aim(null, creature.pos + Vector3.up * Head(creature).y * size);
                yield return null;
            }
            Scare(stage);
            stage.Camera.Roll = 1.25f;
            stage.Aim(new Vector3(eye.x, .3f, eye.z), Head(creature), 6, 4);
            stage.RedTarget = .3f;
            stage.GlitchTarget = .7f;
            yield return stage.Wait(.8f);
            creature.pitch = .5f;
            var start = creature.pos;
            var goal = new Vector3(eye.x + forward.x * .4f, 0, eye.z + forward.z * .4f);
            bool auto = creature.AutoTick;
            creature.AutoTick = false;
            for (float time = 0; time < .5f; time += stage.Dt)
            {
                creature.pos = Vector3.Lerp(start, goal, Mathf.Clamp01((time + stage.Dt) / .5f));
                creature.StepPose(stage.Dt, 0);
                stage.Aim(null, new Vector3(creature.pos.x, .5f, creature.pos.z), 6, 8);
                yield return null;
            }
            creature.pos = goal;
            creature.AutoTick = auto;
            Scare(stage);
            stage.CutTo(1);
            yield return stage.Wait(1);
        }

        IEnumerator DeathAlley(DeathStage stage)
        {
            var eye = stage.Eye;
            var target = callPosition.sqrMagnitude > .01f ? callPosition : new Vector3(eye.x, 1.5f, eye.z - 3);
            var direction = new Vector3(target.x - eye.x, 0, target.z - eye.z);
            direction = direction.sqrMagnitude > .0001f ? direction.normalized : Vector3.back;
            stage.WarpTarget = .1f;
            stage.Aim(null, new Vector3(eye.x + direction.x * 5, 1.5f, eye.z + direction.z * 5), 3, 3.5f);
            var actor = DeathActor(stage, new ActorOptions
                { coat = "raincoat", color = 0x0d0d0d, hood = true, face = "wide", pale = true, uncanny = 1, tall = 1.12f, noShadow = true },
                new Vector3(eye.x + direction.x * 3, 0, eye.z + direction.z * 3));
            FaceActor(actor, eye);
            actor.lookTarget = eye;
            actor.StepPose(0, 0);
            Say("(Giọng gọi tên bạn — từ ngay chỗ bạn vừa nhìn.)", 2.5f);
            float available = DeathStaging.AlleySpace(eye, direction);
            var bulbMarker = Marker("S:alleyBulb");
            var bulbRenderer = bulbMarker ? bulbMarker.GetComponent<Renderer>() : null;
            MaterialPropertyBlock savedBulb = null;
            if (bulbRenderer)
            {
                savedBulb = new MaterialPropertyBlock();
                bulbRenderer.GetPropertyBlock(savedBulb);
                stage.Restore.Add(() => { if (bulbRenderer) bulbRenderer.SetPropertyBlock(savedBulb); });
            }
            foreach (var (dist, rv) in new[] { (available, 0f), (available * .62f, .3f), (available * .32f, .6f), (.65f, 1f) })
            {
                actor.pos = new Vector3(eye.x + direction.x * dist, 0, eye.z + direction.z * dist);
                actor.Motion.reveal = actor.Motion.revealTarget = rv;
                actor.Motion.armUp = dist < 1 ? 1 : 0;
                actor.StepPose(0, 0);
                SetAlleyLight(true);
                PlayAt("buzz", new Vector3(2.1f, 2.7f, -4.8f));
                if (dist < 1)
                {
                    DeathSting(stage, actor, .5f);
                    stage.Aim(null, Head(actor), 8, 10);
                }
                else stage.Aim(null, Head(actor), 3, 4);
                yield return stage.Wait(dist < 1 ? .45f : .55f);
                SetAlleyLight(false);
                if (dist >= 1) yield return stage.Wait(.75f);
            }
            stage.CutTo(1);
            yield return stage.Wait(1);

            void SetAlleyLight(bool on)
            {
                foreach (var light in lamps) if (light.Alley) light.Off = !on;
                if (!bulbRenderer) return;
                if (on) bulbRenderer.SetPropertyBlock(savedBulb);
                else
                {
                    tintBlock ??= new MaterialPropertyBlock();
                    tintBlock.Clear();
                    tintBlock.SetColor("_BaseColor", new Color(.15f, .15f, .15f));
                    tintBlock.SetColor("_EmissionColor", Color.black);
                    bulbRenderer.SetPropertyBlock(tintBlock);
                }
            }
        }

        IEnumerator DeathBlackout(DeathStage stage)
        {
            var eye = stage.Eye;
            var forward = stage.Forward;
            if (forward.sqrMagnitude < .001f) forward = Vector3.forward;
            DeathDark();
            stage.WarpTarget = .05f;
            if (glassFigure != null) { RemoveGuest(glassFigure); glassFigure = null; }
            PlayClip("glass_knock");
            yield return stage.Wait(.6f);
            PlayClip("glass_knock");
            yield return stage.Wait(1.2f);
            var actor = DeathActor(stage, new ActorOptions
                { coat = "padding", color = 0x050505, hood = true, face = "wide", pale = true, uncanny = 1, noShadow = true },
                DeathSpot(stage, 3.2f));
            FaceActor(actor, eye);
            actor.lookTarget = eye;
            actor.Motion.reveal = actor.Motion.revealTarget = 1;
            actor.StepPose(0, 0);
            stage.Aim(null, Head(actor), 2.4f, 2.6f);
            Say("...Tìm thấy rồi.", 2.5f);
            PlayAt("whisper", eye);
            foreach (float dist in new[] { 2.2f, 1.4f })
            {
                yield return stage.Wait(1);
                actor.pos = DeathSpot(stage, dist);
                FaceActor(actor, eye);
                actor.StepPose(0, 0);
                stage.Shake = Mathf.Max(stage.Shake, .2f);
                PlayClip("thud");
            }
            yield return stage.Wait(.8f);
            actor.pos = DeathSpot(stage, .45f);
            FaceActor(actor, eye);
            actor.StepPose(0, 0);
            var sparkPos = new Vector3(eye.x + forward.x * .2f - forward.z * .4f, 2.2f, eye.z + forward.z * .2f + forward.x * .4f);
            var sparkLight = DeathLight(stage, "Tia lửa cầu dao", 0xff9a40, 0, 5, sparkPos);
            DeathBurst(stage, sparkPos, new Color32(255, 190, 96, 255), 90, 2.2f);
            DeathSting(stage, actor, .3f);
            stage.Aim(null, Head(actor), 8, 10);
            for (float time = 0; time < .6f; time += stage.Dt)
            {
                sparkLight.intensity = (40f / Mathf.PI) * Mathf.Max(0, 1 - time / .6f) * (RandomValue() < .7f ? 1 : .2f);
                yield return null;
            }
            sparkLight.intensity = 0;
            stage.CutTo(1);
            yield return stage.Wait(1);
        }

        IEnumerator DeathRed(DeathStage stage)
        {
            var eye = stage.Eye;
            stage.WarpTarget = .35f;
            stage.RedTarget = .3f;
            TintStore(0xff1a1a, .35f);
            var coats = new[] { "raincoat", "padding", "uniform", "manager", "office", "padding", "raincoat" };
            var ring = new (Actor actor, float angle)[coats.Length];
            for (int i = 0; i < coats.Length; ++i)
            {
                float angle = i * 2 * Mathf.PI / coats.Length + RandomValue() * .3f;
                var actor = DeathActor(stage, new ActorOptions
                    { coat = coats[i], color = 0x0b0b0b, hood = i % 2 == 0, face = i % 3 == 0 ? "wide" : "none", pale = true, uncanny = 1, noShadow = true },
                    new Vector3(eye.x, 0, eye.z));
                actor.lookTarget = eye;
                actor.Model.SetActive(false);
                ring[i] = (actor, angle);
            }
            var forward = stage.Forward;
            if (forward.sqrMagnitude < .001f) forward = Vector3.forward;
            float spin = Mathf.Atan2(forward.x, forward.z);
            foreach (var (radius, rev) in new[] { (4.6f, .2f), (3f, .5f), (1.7f, .8f), (.75f, 1f) })
            {
                DeathDark();
                foreach (var entry in ring) entry.actor.Model.SetActive(false);
                yield return stage.Wait(radius < 1 ? .9f : .55f);
                foreach (var entry in ring)
                {
                    entry.actor.pos = new Vector3(
                        Mathf.Clamp(eye.x + Mathf.Sin(entry.angle) * radius, .3f, 11.7f), 0,
                        Mathf.Clamp(eye.z + Mathf.Cos(entry.angle) * radius, .3f, 7.7f));
                    FaceActor(entry.actor, eye);
                    entry.actor.Motion.reveal = entry.actor.Motion.revealTarget = rev;
                    entry.actor.Model.SetActive(true);
                    entry.actor.StepPose(0, 0);
                }
                foreach (var light in lamps) if (light.Inside) light.Off = false;
                Power(true, outsidePower);
                TintStore(0xff1a1a, .45f);
                PlayAt("buzz", new Vector3(eye.x, 2.9f, eye.z));
                stage.Shake = Mathf.Max(stage.Shake, .15f + rev * .3f);
                spin += 1.3f;
                stage.Aim(null, new Vector3(eye.x + Mathf.Sin(spin) * 3, 1.5f, eye.z + Mathf.Cos(spin) * 3), 2, 2.2f);
                if (radius < 1)
                {
                    var closest = ring.OrderBy(entry => Mathf.Abs(Mathf.Atan2(Mathf.Sin(entry.angle - spin), Mathf.Cos(entry.angle - spin)))).First().actor;
                    stage.Aim(null, Head(closest), 6, 6);
                    DeathSting(stage, closest, .7f);
                }
                yield return stage.Wait(radius < 1 ? .5f : .35f);
            }
            stage.CutTo(1);
            yield return stage.Wait(1);
        }

        IEnumerator DeathChase(DeathStage stage)
        {
            var eye = stage.Eye;
            var lampPos = chaseLight != null && chaseLight.Lamp
                ? ThreeCoords.ToUnity(chaseLight.Lamp.transform.position)
                : new Vector3(eye.x + 2, 2.95f, eye.z);
            lampPos.y = 2.95f;
            stage.WarpTarget = .15f;
            stage.Aim(null, lampPos, 2.6f, 3);
            if (chaseLight != null) chaseLight.FlickerUntil = Time.time + 2;
            yield return stage.Wait(.9f);
            DeathDark();
            DeathBurst(stage, new Vector3(lampPos.x, 2.9f, lampPos.z), new Color32(255, 192, 96, 255), 120, 2.4f);
            PlayClip("thud");
            stage.Shake = Mathf.Max(stage.Shake, .5f);
            var sparkLight = DeathLight(stage, "Tia lửa đèn trần", 0xffa050, 30f / Mathf.PI, 6, new Vector3(lampPos.x, 2.6f, lampPos.z));
            var forward = stage.Forward;
            if (forward.sqrMagnitude < .001f) forward = Vector3.forward;
            var actor = DeathActor(stage, new ActorOptions
                { coat = "padding", color = 0x070707, longHair = true, hair = 0x020202, face = "wide", pale = true, uncanny = 1, noShadow = true },
                new Vector3(eye.x + forward.x * .9f, 3.75f, eye.z + forward.z * .9f));
            FaceActor(actor, eye);
            actor.roll = Mathf.PI;
            actor.Motion.revealTarget = 1;
            actor.StepPose(0, 0);
            for (float time = 0; time < 1.3f; time += stage.Dt)
            {
                sparkLight.intensity = (30f / Mathf.PI) * Mathf.Max(.1f, 1 - time / 1.3f) * (RandomValue() < .8f ? 1 : .3f);
                stage.Aim(null, new Vector3(actor.pos.x, 2.1f, actor.pos.z), 2.2f, 2.4f);
                yield return null;
            }
            Say("(Ngay trên đầu bạn.)", 2);
            float y0 = actor.pos.y;
            for (float time = 0; time < .6f; time += stage.Dt)
            {
                actor.pos.y = y0 - (time + stage.Dt) * .9f;
                actor.StepPose(0, 0);
                sparkLight.intensity = (12f / Mathf.PI) * (RandomValue() < .6f ? 1 : 0);
                stage.Aim(null, new Vector3(actor.pos.x, actor.pos.y - 1.75f, actor.pos.z), 4, 4);
                yield return null;
            }
            DeathSting(stage, null, .5f, -.35f);
            sparkLight.intensity = 25f / Mathf.PI;
            yield return stage.Wait(.35f);
            stage.CutTo(1);
            yield return stage.Wait(1);
        }

        IEnumerator DeathFired(DeathStage stage)
        {
            var eye = stage.Eye;
            stage.WarpTarget = .2f;
            Say("LOA CỬA HÀNG: \"Thông báo... nhân viên ca đêm... đã bị cho thôi việc.\"");
            PlayClip("radio_glitch");
            var forward0 = stage.Forward;
            if (forward0.sqrMagnitude < .001f) forward0 = Vector3.right;
            stage.Aim(null, new Vector3(eye.x + forward0.x * 5, 1.2f, eye.z + forward0.z * 5), 1.5f, 1.6f);
            yield return stage.Wait(1.2f);
            var forward1 = stage.Forward;
            if (forward1.sqrMagnitude < .001f) forward1 = Vector3.right;
            var items = shelfProducts
                .Where(marker => marker && marker.gameObject.activeInHierarchy)
                .Select(marker => (marker, threePos: ThreeCoords.ToUnity(marker.transform.position)))
                .Where(entry => DeathStaging.FiredProductInView(entry.threePos, eye, forward1))
                .Take(70)
                .OrderByDescending(entry => HorizontalDistance(entry.threePos, eye.x, eye.z))
                .ToList();
            if (items.Count > 0)
            {
                var center = Vector3.zero;
                foreach (var entry in items) center += entry.threePos;
                center /= items.Count;
                center.y = .7f;
                stage.Aim(null, center, 1.5f, 1.8f);
            }
            float startTime = stage.Cut.Time;
            for (int i = 0; i < items.Count;)
            {
                var entry = items[i++];
                var source = entry.marker.gameObject;
                var clone = Instantiate(source, source.transform.position, source.transform.rotation);
                clone.name = "Hàng đổ · sa thải";
                clone.transform.localScale = source.transform.lossyScale;
                clone.SetActive(true);
                if (clone.TryGetComponent<StoreMarker>(out var clonedMarker))
                {
                    clonedMarker.kind = StoreMarker.Kind.Static;
                    Destroy(clonedMarker);
                }
                stage.Objects.Add(clone);
                HideForDeath(stage, source);
                var body = clone.AddComponent<ItemPhysicsBody>();
                var rotation = source.transform.rotation;
                float side = (entry.threePos.x % 2.6f) > 1.3f ? 1 : -1;
                body.Body = new ItemBody
                {
                    position = entry.threePos,
                    rotation = new Quaternion(rotation.x, -rotation.y, -rotation.z, rotation.w),
                    velocity = new Vector3(side * (.6f + RandomValue()), .4f + RandomValue() * .8f, RandomValue() - .5f),
                    spin = new Vector3(RandomValue() * 8, RandomValue() * 4, RandomValue() * 8),
                    halfHeight = .05f, bounce = .32f,
                };
                body.Landed += impact => { if (impact > 2 && RandomValue() < .3f) PlayClip("shelf"); };
                if (i % 6 == 0) { PlayClip("shelf"); stage.Shake = Mathf.Max(stage.Shake, .12f); yield return stage.Wait(.06f); }
                if (stage.Cut.Time - startTime > 2.4f) break;
            }
            yield return stage.Wait(1);
            var ceiling = lamps.Where(light => light.Inside && light.Lamp.transform.position.y > 2.5f)
                .OrderByDescending(light => HorizontalDistance(ThreeCoords.ToUnity(light.Lamp.transform.position), eye.x, eye.z)).ToArray();
            foreach (var lamp in ceiling)
            {
                ShutLamp(lamp);
                yield return stage.Wait(.1f);
            }
            DeathDark();
            var actor = DeathActor(stage, new ActorOptions
                { coat = "manager", color = 0x14213d, longHair = true, hair = 0x0b0b0b, face = "wide", mask = "smile", pale = true, uncanny = 1 },
                DeathSpot(stage, .55f));
            FaceActor(actor, eye);
            actor.lookTarget = eye;
            actor.Motion.reveal = actor.Motion.revealTarget = 1;
            actor.StepPose(0, 0);
            var overhead = DeathLight(stage, "Đèn sa thải", 0xbfe7ff, 0, 4, new Vector3(eye.x, 2.8f, eye.z));
            yield return stage.Wait(.8f);
            overhead.intensity = 14f / Mathf.PI;
            stage.Aim(null, Head(actor), 8, 10);
            DeathSting(stage, actor, .5f);
            yield return stage.Wait(.4f);
            stage.CutTo(1);
            yield return stage.Wait(1);
        }

        static Light DeathLight(DeathStage stage, string name, int rgb, float intensity, float range, Vector3 position)
        {
            var light = DeathProp(stage, name, position).gameObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = Hex(rgb);
            light.intensity = intensity;
            light.range = range;
            light.shadows = LightShadows.None;
            return light;
        }

        void DeathSting(DeathStage stage, Actor actor, float red, float dolly = -.3f)
        {
            Scare(stage);
            stage.RedTarget = red;
            stage.GlitchTarget = 1;
            stage.DollyTarget = dolly;
            if (actor) actor.Motion.armUp = 1;
        }

        void SaveMonitor(DeathStage stage)
        {
            if (!monitor) return;
            var saved = new MaterialPropertyBlock();
            monitor.GetPropertyBlock(saved);
            stage.Restore.Add(() => { if (monitor) monitor.SetPropertyBlock(saved); });
        }

        void MonitorBright(float brightness)
        {
            if (!monitor) return;
            monitor.GetPropertyBlock(tintBlock);
            tintBlock.SetColor("_BaseColor", new Color(brightness, brightness, brightness));
            tintBlock.SetColor("_EmissionColor", new Color(brightness, brightness, brightness));
            monitor.SetPropertyBlock(tintBlock);
        }

        static Transform DeathProp(DeathStage stage, string name, Vector3 position)
        {
            var prop = new GameObject(name);
            prop.transform.position = ThreeCoords.ToUnity(position);
            stage.Objects.Add(prop);
            return prop.transform;
        }

        static void DeathCylinder(Transform parent, Vector3 position, Vector3 size, Material material)
        {
            var cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            var collider = cylinder.GetComponent<Collider>();
            collider.enabled = false;
            Destroy(collider);
            cylinder.transform.SetParent(parent, false);
            cylinder.transform.localPosition = position;
            cylinder.transform.localScale = size;
            cylinder.GetComponent<Renderer>().sharedMaterial = material;
        }

        static void DeathBurst(DeathStage stage, Vector3 position, Color color, int count, float velocity)
        {
            var root = DeathProp(stage, "Hạt điện ảnh", position);
            var particles = root.gameObject.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particles.main;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(.15f, .7f);
            main.startSize = new ParticleSystem.MinMaxCurve(.008f, .035f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(velocity * .3f, velocity);
            main.startColor = color;
            main.gravityModifier = .3f;
            var emission = particles.emission;
            emission.enabled = false;
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = .06f;
            var texture = new Texture2D(32, 32, TextureFormat.RGBA32, false) { name = "Hạt sáng" };
            var pixels = new Color32[32 * 32];
            for (int y = 0; y < 32; ++y)
                for (int x = 0; x < 32; ++x)
                {
                    float r = Mathf.Clamp01(new Vector2(x - 15.5f, y - 15.5f).magnitude / 15.5f);
                    float alpha = (1 - r) * (1 - r);
                    pixels[y * 32 + x] = new Color32(255, 255, 255, (byte)(alpha * 255));
                }
            texture.SetPixels32(pixels);
            texture.Apply();
            stage.Objects.Add(texture);
            var material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")) { mainTexture = texture };
            MakeTransparent(material);
            stage.Objects.Add(material);
            root.GetComponent<ParticleSystemRenderer>().sharedMaterial = material;
            particles.Emit(count);
            particles.Play();
        }
    }
}
