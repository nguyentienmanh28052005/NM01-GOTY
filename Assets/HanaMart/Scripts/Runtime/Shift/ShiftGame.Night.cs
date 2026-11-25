using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HanaMart.Actors;
using HanaMart.Store;
using UnityEngine;

namespace HanaMart.Shift
{
    public sealed partial class ShiftGame
    {
        Guest glassFigure, starer, redThing, jiho, deliveryGuest;
        GameObject deliveryStack;
        RedHour redHour;
        ShopLight chaseLight;
        float blackoutTime, blackoutEntryAt, glassLook, starerTime, starerLook, knockTime, deliveryDeadline, callTime, chaseRemaining;
        Vector3 callPosition;
        int deliveryCount;
        public string Ending { get; private set; }

        void ResetHazards()
        {
            glassFigure = starer = redThing = jiho = deliveryGuest = null;
            deliveryStack = null;
            redHour = null;
            chaseLight = null;
            blackoutTime = blackoutEntryAt = glassLook = starerTime = starerLook = knockTime = deliveryDeadline = callTime = chaseRemaining = 0;
            deliveryCount = 0;
            callPosition = Vector3.zero;
            Ending = null;
        }

        void HazardEvent(ShiftEvent entry)
        {
            switch (entry.Id)
            {
                case "phase2":
                    Say("02:00 · Có gì đó trong cửa hàng đã thay đổi");
                    PlayClip("sting");
                    Run.Progress.Flags["knock"] = 1;
                    knockTime = 2;
                    break;
                case "phase3": Say("04:00 · Trời sắp sáng. Cửa hàng không muốn bạn đi"); PlayClip("sting"); break;
                case "freezerJiho":
                    if (NightRules.CanRescueJiho(Run.Progress.Diaries.Count, Run.Clock.Minutes, HasFlag("jiho")))
                    {
                        Run.Progress.Flags["knock323"] = 1;
                        knockTime = 1;
                        Say("...cốc cốc cốc ... cốc cốc ... cốc cốc cốc...");
                    }
                    break;
                case "delivery": StartDelivery(); break;
                case "blackout": Blackout(); break;
                case "starer": OutsideStarer(1); break;
                case "starerLate": OutsideStarer(3); break;
                case "redHour": StartRedHour(); break;
                case "forge12": ForgeRule(12); break;
                case "forge13": ForgeRule(13); break;
                case "screamGlass": ScreamGlass(); break;
                case "lateDelivery": LateDelivery(); break;
                case "manager": SpecialVisitor("manager"); break;
                case "doubleShift": SpecialVisitor("dblShift"); break;
                case "fakeMirae": SpecialVisitor("mirae"); break;
                case "realMirae": SpecialVisitor("realMirae"); break;
            }
        }

        void UpdateHazards(float dt)
        {
            UpdateFreezer(dt);
            UpdateRedHour(dt);
            if (Dead) return;
            UpdateBlackout(dt);
            if (Dead) return;
            UpdateStarer(dt);
            UpdateAlley(dt);
            UpdateChase(dt);
            if (HasFlag("box13") && (deliveryDeadline -= dt) < 0)
            {
                Run.Progress.Flags.Remove("box13");
                Die(8, "Bạn đã ký nhận thùng hàng thứ 13.");
            }
        }

        void ForgeRule(int number)
        {
            Run.Progress.Flags["fake" + number] = 1;
            PlayAt("whisper", new Vector3(.1f, 1.5f, 6.95f));
            Say("(Tờ nội quy sau quầy... hình như có chữ vừa thay đổi.)");
        }

        void UpdateFreezer(float dt)
        {
            var origin = new Vector3(3, .8f, -3.95f);
            if (HasFlag("knock") && NightRules.FreezerForbidden(Run.Clock.Minutes) && (knockTime -= dt) < 0)
            {
                knockTime = 4 + RandomValue() * 3;
                PlayAt("knock", origin);
                if (RandomValue() < .35f) PlayAt("whisper", origin);
                if (doors && doors.Freezer.motor.target < .1f)
                {
                    doors.Freezer.motor.target = .04f / 1.2f;
                    Later(.12f, () => { if (doors.Freezer.motor.target < .1f) doors.Freezer.motor.target = 0; });
                }
            }
            if (HasFlag("knock323") && (knockTime -= dt) < 0)
            {
                knockTime = 9;
                PlayAt("knock_323", origin);
            }
        }

        void OpenFreezer()
        {
            if (doors) doors.Use("freezer", interactor.Current);
            SteamAt(new Vector3(3, .95f, -3.85f), 1.2f, 11);
            if (NightRules.FreezerForbidden(Run.Clock.Minutes)) { Die(6, "Bạn đã mở tủ đông lúc 02:00–02:10."); return; }
            if (!HasFlag("knock323") || HasFlag("jiho")) { Say("Tủ đông: đầy kem và đá. Lạnh buốt.", 2); return; }
            Run.Progress.Flags["jiho"] = 1;
            Run.Progress.Flags.Remove("knock323");
            jiho = Figure("jiho", "Ji-ho", new Vector3(3, 0, -3.3f), new ActorOptions
                { coat = "uniform", color = 0x00a98f, hair = 0x241a14, pale = true, skin = 0xd6dde0 }, yaw: 0);
            jiho.Actor.lookTarget = ThreeCoords.ToUnity(transform.position);
            PlayClip("gasp");
            ShowDialog("Ji-ho", "(Một cậu thanh niên run rẩy bò ra, tóc đóng băng.) ...Cậu đọc được rồi. Cảm ơn. Mấy giờ rồi? ...Đừng giao ca cho ai trước 6 giờ. Tôi sẽ ở bên cậu.",
                new Choice("Ra quầy với tôi.", () => StartCoroutine(BringJihoInside(jiho))));
        }

        IEnumerator BringJihoInside(Guest guest)
        {
            yield return Walk(guest, new Vector2(.8f, -.6f), new Vector2(.75f, 2.2f), new Vector2(.75f, 3));
            yield return Face(guest.Actor, new Vector2(3, 3));
        }

        Guest Figure(string kind, string name, Vector3 position, ActorOptions look, float yaw = Mathf.PI, float speed = 1.15f)
        {
            var guest = SpawnGuest(kind, new Profile(name, look, -1, null, null, speed), position, false);
            guest.Actor.yaw = yaw;
            return guest;
        }

        void StartDelivery()
        {
            Run.Progress.Flags["delivery"] = 1;
            deliveryCount = RandomValue() < .6f ? 13 : 12;
            PlayAt("knock", new Vector3(2.1f, 1.4f, -4.7f));
            Say("(Tiếng gõ cửa sau: \"Giao hàng đây!\")");
            AddTask("delivery", "Mở cửa sau và nhận hàng giao (đếm số thùng!)");
            deliveryGuest = Figure("delivery", "Người giao hàng", new Vector3(2.1f, 0, -6.2f), new ActorOptions
                { color = 0x1d4e89, cap = true, mask = "black" }, yaw: 0);
            deliveryStack = PropGroup("Thùng hàng giao", new Vector3(3, 0, -6.2f));
            cardboard ??= Lit(0xa98758, .15f);
            for (int box = 0; box < deliveryCount; ++box)
            {
                var center = new Vector3((1 - box % 3) * .45f, .15f + box / 3 * .31f, (RandomValue() - .5f) * .04f);
                var prop = Block(deliveryStack.transform, $"Thùng {box + 1}", center, new Vector3(.42f, .3f, .38f), cardboard);
                prop.localRotation = Quaternion.Euler(0, -(RandomValue() - .5f) * .1f * Mathf.Rad2Deg, 0);
            }
            NewHit("deliveryStack", "Hàng giao", new Vector3(3, 1, -6.2f), new Vector3(1.5f, 2, .8f));
        }

        void DeliveryDialog()
        {
            if (!HasFlag("delivery") || HasFlag("deliveryDone")) return;
            ShowDialog("Người giao hàng", $"Tổng cộng {(deliveryCount == 13 ? "mười ba" : "mười hai")}... à, cứ đếm lại đi. Ký vào đây là xong.",
                new Choice("Ký nhận", () => FinishDelivery(true)),
                new Choice("Không ký nhận (số thùng không đúng)", () => FinishDelivery(false)));
        }

        void FinishDelivery(bool sign)
        {
            Run.Progress.Flags["deliveryDone"] = 1;
            CompleteTask("delivery");
            switch (NightRules.Deliver(deliveryCount, sign))
            {
                case DeliveryResult.Strike: Strike(8, "Hàng có đủ 12 thùng mà bạn không ký nhận."); break;
                case DeliveryResult.ThirteenthBox:
                    Run.Progress.Flags["box13"] = 1;
                    deliveryDeadline = 20;
                    break;
                case DeliveryResult.Refused:
                    Say("Người giao hàng nhìn bạn rất lâu, rồi kéo cả chồng thùng đi.");
                    if (deliveryStack) deliveryStack.SetActive(false);
                    break;
            }
            if (deliveryGuest != null) StartCoroutine(DeliveryLeave(deliveryGuest));
        }

        IEnumerator DeliveryLeave(Guest guest)
        {
            yield return Walk(guest, new Vector2(2.1f, -8.6f), new Vector2(-1, -8.6f));
            RemoveGuest(guest);
        }

        void UpdateAlley(float dt)
        {
            var position = player.Motor.pos;
            var camera = ThreeCoords.ToUnity(transform.position);
            var forward = ThreeCoords.ToUnity(transform.forward);
            var flat = new Vector2(forward.x, forward.z).normalized;
            if (HasFlag("alleyPending") && position.y < -4.4f && Vector2.Dot(flat, (new Vector2(2, -4.6f) - position).normalized) > .6f)
            {
                Run.Progress.Flags.Remove("alleyPending");
                Run.Progress.Flags["alleyCall"] = 1;
                callPosition = new Vector3(camera.x - flat.x * 5, 1.5f, Mathf.Clamp(camera.z - flat.y * 5, -8.9f, -5));
                callTime = 0;
                Later(.4f, () =>
                {
                    if (!HasFlag("alleyCall")) return;
                    PlayAt("whisper_name", callPosition);
                    Say("(sau lưng bạn) ...Minh ơi... Minh à... mẹ đây... quay lại nhìn mẹ đi...");
                });
            }
            if (position.y > -4.2f)
            {
                Run.Progress.Flags.Remove("alleyPending");
                Run.Progress.Flags.Remove("alleyCall");
                return;
            }
            if (!HasFlag("alleyCall")) return;
            callTime += dt;
            var toward = new Vector2(callPosition.x - camera.x, callPosition.z - camera.z).normalized;
            if (callTime > 1.2f && Vector2.Dot(toward, flat) > Mathf.Cos(1)) Die(5, "Bạn đã quay đầu khi có tiếng gọi tên mình ở hẻm sau.");
        }

        void Blackout()
        {
            Run.Progress.Flags["blackout"] = 1;
            Run.Progress.Flags.Remove("blackEntry");
            blackoutTime = glassLook = 0;
            Power(false, true);
            PlayClip("power_down");
            Say("(Mất điện!)", 2);
            float x = new[] { 4.4f, 5.6f, 7.4f, 9, 10.6f }[RandomIndex(5)];
            glassFigure = Figure("blackoutGlass", "Bóng ngoài kính", new Vector3(x, 0, 8.35f), new ActorOptions
            {
                coat = RandomValue() < .5f ? "raincoat" : "padding", color = 0x111111, hood = true, face = "none",
                tall = 1.12f + RandomValue() * .18f, pale = true, uncanny = 1,
            });
            blackoutEntryAt = 10 + RandomValue() * 6;
            if (RandomValue() < .45f)
            {
                var other = Figure("blackoutGlass", "Bóng ngoài kính", new Vector3(x < 7 ? x + 3.5f : x - 3.5f, 0, 8.4f), new ActorOptions
                    { color = 0x0a0a0a, longHair = true, hair = 0x050505, face = "none", pale = true, uncanny = 1 });
                Later(40, () => RemoveGuest(other));
            }
        }

        void UpdateBlackout(float dt)
        {
            if (!HasFlag("blackout") || glassFigure == null) return;
            blackoutTime += dt;
            if (blackoutTime > blackoutEntryAt && !HasFlag("blackEntry"))
            {
                Run.Progress.Flags["blackEntry"] = 1;
                var entrant = Figure("blackEntry", "Bóng trong cửa hàng", new Vector3(3.1f, 0, 10), new ActorOptions
                    { color = 0x0d0e10, hood = true, face = "none", pale = true, uncanny = .8f });
                entrant.BlackEntry = entrant.Chime = true;
                StartCoroutine(BlackoutGuest(entrant));
            }
            var camera = ThreeCoords.ToUnity(transform.position);
            var forward = ThreeCoords.ToUnity(transform.forward);
            var toward = new Vector2(glassFigure.Actor.pos.x - camera.x, 8 - camera.z).normalized;
            bool looking = forward.z > .35f && Vector2.Dot(new Vector2(forward.x, forward.z).normalized, toward) > .75f;
            glassLook = looking ? glassLook + dt : 0;
            glassFigure.Actor.pos.x += Mathf.Sin(blackoutTime * .7f) * dt * .6f;
            var motor = player.Motor;
            if (blackoutTime > NightRules.BlackoutGrace(Run.Progress.Mercy.GetValueOrDefault("r10")) && !NightRules.HasShelter(motor.pos, motor.crouch))
                Die(10, "Bạn đã không ngồi thấp sau quầy khi mất điện.");
            if (glassLook > 1.4f) Die(10, "Bạn đã nhìn ra cửa kính khi mất điện.");
            if (blackoutTime <= 38 || Dead) return;
            Run.Progress.Flags.Remove("blackout");
            director.Calm(45);
            Power(true, true);
            PlayClip("power_up");
            RemoveGuest(glassFigure);
            glassFigure = null;
            Say("(Có điện lại.)", 2);
        }

        IEnumerator BlackoutGuest(Guest guest)
        {
            yield return Walk(guest, DoorOut, DoorIn, new Vector2(4.4f, 6.2f));
            guest.Actor.lookTarget = ThreeCoords.ToUnity(transform.position);
            while (!guest.Greeted && HasFlag("blackout")) yield return null;
            if (guest.Greeted) Say("(Một giọng khàn, rất gần:) ...Cảm ơn. Bạn đã chào tôi.");
            yield return new WaitForSeconds(2);
            yield return Walk(guest, DoorIn, DoorOut, new Vector2(-3, 10));
            RemoveGuest(guest);
        }

        void OutsideStarer(int count)
        {
            for (int i = 1; i < count; ++i)
            {
                var bystander = Figure("starer", "Bóng đứng ngoài kính", new Vector3(2 + i * 3.3f, 0, 8.4f), new ActorOptions
                    { color = 0x0d0d0f, hood = true, face = "wide", pale = true, uncanny = 1 });
                bystander.Actor.lookTarget = ThreeCoords.ToUnity(transform.position);
                Later(50, () => RemoveGuest(bystander));
            }
            starer = Figure("starer", "Người đứng ngoài cửa kính", new Vector3(5 + RandomValue() * 5.6f, 0, 8.35f), new ActorOptions
            {
                coat = RandomValue() < .5f ? "padding" : "raincoat", color = 0x0d0d0f, hood = true,
                mask = RandomValue() < .6f ? "smile" : null, face = "wide", pale = true, uncanny = 1, tall = 1 + RandomValue() * .15f,
            });
            starerTime = 55;
            starerLook = 0;
        }

        void UpdateStarer(float dt)
        {
            if (starer == null || !starer.Actor.alive) return;
            var actor = starer.Actor;
            actor.lookTarget = ThreeCoords.ToUnity(transform.position);
            var toward = ThreeCoords.ToUnity(actor.pos + 1.6f * Vector3.up) - transform.position;
            actor.Motion.revealTarget = Mathf.Clamp01(starerLook / 3);
            if (Vector3.Dot(transform.forward, toward.normalized) > .96f)
            {
                starerLook += dt;
                if (starerLook > 3)
                {
                    starerLook = -999;
                    PlayClip("glass_knock");
                    Strike(11, "Bạn đã nhìn lại người đứng ngoài cửa kính quá 3 giây.");
                }
            }
            else if (starerLook > 0) starerLook = 0;
            if ((starerTime -= dt) > 0) return;
            RemoveGuest(starer);
            starer = null;
        }

        void StartChase()
        {
            var playerPosition = transform.position;
            var lights = lamps.Where(light => light.Inside && light.Lamp.transform.position.z >= 0)
                .OrderByDescending(light => Vector2.Distance(new Vector2(playerPosition.x, playerPosition.z),
                    new Vector2(light.Lamp.transform.position.x, light.Lamp.transform.position.z))).ToArray();
            if (lights.Length == 0) return;
            chaseLight = lights[Mathf.Min(lights.Length - 1, Mathf.FloorToInt(lights.Length * (.35f + RandomValue() * .3f)))];
            Run.Progress.Flags["chase"] = 1;
            chaseRemaining = .6f + lights.Length * .45f + 2.6f + 1.2f * Mathf.Min(3, Run.Progress.Mercy.GetValueOrDefault("r17"));
            PlayClip("power_down");
            Say("(Tách... tách... từng bóng đèn tắt dần về phía bạn.)", 3);
            for (int i = 0; i < lights.Length; ++i)
            {
                var light = lights[i];
                if (light == chaseLight) continue;
                Later(.6f + i * .45f, () =>
                {
                    if (!HasFlag("chase")) return;
                    light.Off = true;
                    PlayAt("buzz", ThreeCoords.ToUnity(light.Lamp.transform.position));
                });
            }
        }

        void UpdateChase(float dt)
        {
            if (chaseLight == null || !HasFlag("chase") || (chaseRemaining -= dt) >= 0) return;
            Run.Progress.Flags.Remove("chase");
            var p = ThreeCoords.ToUnity(chaseLight.Lamp.transform.position);
            if (Vector2.Distance(player.Motor.pos, new Vector2(p.x, p.z)) >= 1.3f)
            {
                Die(17, "Bóng tối đã tới chỗ bạn đứng. Bạn không đứng dưới bóng đèn cuối cùng.");
                return;
            }
            director.Calm(30);
            foreach (var light in lamps) light.Off = false;
            Say("(Bạn đứng trong vũng sáng cuối cùng. Thứ gì đó lướt qua sát bên... rồi đèn sáng lại.)");
            PlayClip("power_up");
        }

        void StartRedHour()
        {
            Run.Progress.Flags["red"] = 1;
            redHour = new RedHour(random.NextDouble);
            Power(true, false);
            TintStore(0xff2416, .35f);
            PlayClip("sting");
            PlayClip("printer");
            Say("05:15 · GIỜ ĐỎ");
            Later(1.8f, () =>
            {
                if (!Dead) ShowPaper("HÓA ĐƠN #0000", "HANA MART 24\n------------------------------\nQUY TẮC BỔ SUNG x1 .......... ₩0\n------------------------------\n\n14. Trong GIỜ ĐỎ, khi đèn tắt: ĐỨNG YÊN. Đừng nhúc nhích cho tới khi đèn sáng lại.\n[DẤU ĐỎ HANA]");
            });
            redThing = Figure("redThing", "Thực thể Giờ Đỏ", new Vector3(10.6f, 0, 1.3f), new ActorOptions
                { coat = "raincoat", color = 0x0a0a0a, hood = true, face = "wide", pale = true, uncanny = 1, tall = 1.15f });
            redThing.Actor.Motion.revealTarget = 1;
            redThing.Actor.Model.SetActive(false);
        }

        void UpdateRedHour(float dt)
        {
            if (redHour == null) return;
            bool dark = redHour.Dark;
            if (redHour.Tick(dt, player.Motor.pos)) { Die(14, "Bạn đã cử động trong bóng tối của Giờ Đỏ."); return; }
            if (dark != redHour.Dark)
            {
                Power(!redHour.Dark, false);
                PlayClip(redHour.Dark ? "power_down" : "power_up");
                if (redHour.Dark)
                {
                    var p = RedHour.EntityPosition(redHour.Cycle);
                    redThing.Actor.pos = new Vector3(p.x, 0, p.y);
                    redThing.Actor.Model.SetActive(true);
                    redThing.Actor.lookTarget = ThreeCoords.ToUnity(transform.position);
                }
            }
            if (!redHour.Finished) return;
            Run.Progress.Flags.Remove("red");
            redHour = null;
            Power(true, true);
            TintStore(0xffffff, 1);
            RemoveGuest(redThing);
            redThing = null;
            Say("(Đèn trắng trở lại. Tiếng mưa trở lại. Như chưa từng có gì.)");
            PlayClip("power_up");
        }
    }
}
