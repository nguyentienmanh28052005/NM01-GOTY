using System.Collections;
using System.Collections.Generic;
using HanaMart.Actors;
using HanaMart.Store;
using UnityEngine;

namespace HanaMart.Shift
{
    public sealed partial class ShiftGame
    {
        void SpecialVisitor(string kind)
        {
            var role = kind switch
            {
                "manager" => new Profile("Người phụ nữ mặc đồng phục quản lý", new ActorOptions
                    { coat = "manager", color = 0x14213d, longHair = true, hair = 0x0b0b0b, face = "wide", pale = true, uncanny = .5f }, -1, null, null, 1),
                "dblShift" => new Profile("Nhân viên HANA", new ActorOptions
                    { coat = "uniform", color = 0x00a98f, hair = 0x1a1410, face = "none", pale = true, uncanny = .6f }, -1, null, null, .95f),
                "mirae" => new Profile("Chị Mi-rae?", new ActorOptions
                    { coat = "uniform", color = 0x00a98f, longHair = true, hair = 0x0e0b0b, face = "wide", uncanny = .35f }, -1, null, null),
                _ => new Profile("Chị Mi-rae", new ActorOptions
                    { coat = "uniform", color = 0x00a98f, mask = "white", longHair = true, hair = 0x3a2418 }, -1, null, null),
            };
            if (kind == "mirae" && HasFlag("jiho")) Say("Ji-ho (thì thầm): Người sắp vào... không phải chị Mi-rae đâu.", 5);
            var guest = SpawnGuest(kind, role);
            StartCoroutine(kind == "realMirae" ? MorningVisit(guest) : Visit(guest));
        }

        IEnumerator MorningVisit(Guest guest)
        {
            yield return Walk(guest, new Vector2(3.1f, 9.6f));
            yield return Face(guest.Actor, new Vector2(3.1f, 0));
            guest.Actor.speed = 1.6f;
            while (Run.Clock.Minutes < 359.7 && !Dead && guest.Actor.alive) yield return null;
            if (Dead || !guest.Actor.alive) yield break;
            yield return Walk(guest, DoorOut, DoorIn);
            yield return ToCounter(guest);
        }

        bool TryStoryCheckout(Guest guest)
        {
            switch (guest.Kind)
            {
                case "manager":
                    ShowDialog(guest.Profile.Name, "Chị là quản lý khu vực. Có báo cáo thiếu hàng. Mở kho cho chị kiểm tra ngay.",
                        new Choice("Vâng, mời chị vào kho.", () => Die(9, "Bạn đã cho người mặc đồng phục quản lý vào kho.")),
                        new Choice("Xin lỗi, nội quy không cho ai vào kho.", () =>
                        {
                            Speak(guest, "Nội quy... Tốt. Rất... tốt.");
                            guest.Actor.Motion.revealTarget = 1;
                            PlayClip("sting");
                            Later(3.2f, () => { guest.Paid = true; guest.Actor.speed = .6f; director.Calm(35); });
                        }));
                    return true;
                case "dblShift":
                    ShowDialog(guest.Profile.Name, "Chào cậu. Tớ đến sớm. Cậu về trước đi, để tớ trông nốt cho. (Bảng tên trên ngực nó ghi... tên của bạn. Chỗ khuôn mặt chỉ là một mảng da trơn.)",
                        new Choice("Ừ, cảm ơn. Tớ về nhé.", () => Die(19, "Bạn đã rời ca cho một thứ mặc đồng phục giống hệt bạn. Bảng tên của nó là tên BẠN — giờ nó là nhân viên ca đêm.")),
                        new Choice("Ca của tớ tới 6 giờ. Tớ không giao ca cho ai.", () =>
                        {
                            Speak(guest, "...Vậy à. Tớ sẽ đợi.");
                            guest.Actor.Motion.revealTarget = 1;
                            PlayClip("sting");
                            Later(2.8f, () => { guest.Paid = true; director.Calm(30); });
                        }));
                    return true;
                case "mirae":
                    ShowDialog(guest.Profile.Name, "Chị đến sớm. Em mệt rồi đúng không? Đưa chìa khóa đây, về nghỉ đi em.",
                        new Choice("Cảm ơn chị. Em giao ca nhé.", () => Die(13, "Bạn đã giao ca cho \"chị Mi-rae\" trước 06:00.")),
                        new Choice("Ca của em tới 6 giờ ạ.", () =>
                        {
                            Speak(guest, "...Ừ. Sáu. Giờ.");
                            guest.Actor.Motion.revealTarget = 1;
                            guest.Actor.Motion.headSpin = .4f;
                            PlayClip("sting");
                            foreach (var light in lamps) light.FlickerUntil = Time.time + 3.5f;
                            Later(3.5f, () => { guest.Paid = true; guest.Actor.speed = .5f; });
                        }));
                    return true;
                case "realMirae":
                    if (NightRules.CanHandOver(Run.Clock.Minutes)) EndShift(guest);
                    else Speak(guest, "Chưa đúng 6 giờ đâu em. Đợi chút.");
                    return true;
                default: return false;
            }
        }

        void ScreamGlass()
        {
            if (Dead) return;
            var woman = Figure("scream", "Người phụ nữ ngoài kính", new Vector3(5.6f, 0, 8.4f), new ActorOptions
                { color = 0x8d99ae, longHair = true, hair = 0x1a1410, face = "wide", pale = true, uncanny = .3f });
            woman.Actor.lookTarget = ThreeCoords.ToUnity(transform.position);
            foreach (float wait in new[] { 0, .35f, .7f, 1.4f, 1.75f }) Later(wait, () => PlayClip("glass_knock"));
            Say("(Một người phụ nữ đập cửa kính: \"Cứu tôi! Có người đuổi theo! Cho tôi trốn vào kho!\")");
            Later(2.4f, () =>
            {
                if (Dead || !woman.Actor.alive) return;
                ShowDialog(woman.Profile.Name, "\"Làm ơn! Mở cửa, cho tôi trốn trong kho một lát thôi!\" (Trời mưa như trút, nhưng tóc chị ta khô cong.)",
                    new Choice("Mở cửa, đưa chị vào kho trốn.", () =>
                    {
                        counterCustomer = woman;
                        Die(20, "Bạn đã đưa người lạ vào kho. Tóc chị ta khô — chị ta chưa từng đứng dưới mưa.");
                    }),
                    new Choice("Chị đứng dưới đèn trước cửa, em gọi cảnh sát ngay.", () =>
                    {
                        Speak(woman, "...");
                        woman.Actor.Motion.revealTarget = 1;
                        PlayClip("sting");
                        Later(2.6f, () =>
                        {
                            RemoveGuest(woman);
                            director.Calm(25);
                            Say("(Chớp mắt, trước cửa không còn ai. Trên kính còn lại dấu tay — ở mặt TRONG.)");
                        });
                    }));
            });
        }

        void LateDelivery()
        {
            if (Dead) return;
            var door = new Vector3(2.1f, 1.4f, -4.7f);
            PlayAt("knock", door);
            Later(.6f, () => PlayAt("knock", door));
            Say("(Cửa sau: \"Giao hàng đây! Lúc nãy giao thiếu một thùng!\")");
            Later(2.2f, () =>
            {
                if (Dead) return;
                ShowDialog("Giọng ở cửa sau", "\"Mở cửa giúp tôi, nhận nốt thùng thiếu rồi tôi đi.\" (Không có tiếng xe tải. Giọng y hệt người giao hàng lúc 3 giờ — từng chữ một.)",
                    new Choice("Ra mở cửa sau nhận thùng.", () => Die(21, "Hàng chỉ giao lúc 03:00. Thứ đứng ngoài cửa sau không mang theo thùng nào cả.")),
                    new Choice("Hàng chỉ giao lúc 3 giờ. Tôi không mở.", () =>
                    {
                        Say("(Im lặng. Rồi tiếng móng cào dọc cánh cửa sắt, xa dần.)");
                        PlayAt("creak", door);
                        director.Calm(25);
                    }));
            });
        }

        void EndShift(Guest guest)
        {
            guest.Paid = true;
            Ended = true;
            Run.Clock.Running = false;
            bool rescued = HasFlag("jiho");
            ShowDialog("Chị Mi-rae", rescued
                ? "Chào em, chị... Ji-ho? Em... cả tuần nay mọi người tìm em khắp nơi... Trời ơi, mặt em trắng bệch..."
                : "Chào em! 6 giờ đúng rồi. Ca đầu tiên ổn chứ? Mặt em trắng bệch kìa... Về nghỉ đi, tối mai gặp lại.",
                new Choice(rescued ? "(Đưa Ji-ho ra ngoài trời sáng)" : "(Bàn giao ca và về nhà)", () => PresentEnding(rescued)));
        }

        void PresentEnding(bool rescued)
        {
            Ending = rescued ? "B" : "A";
            var progress = Run.Progress;
            string body = rescued
                ? "Bạn và Ji-ho bước ra ngoài đúng lúc trời hửng sáng. Mưa đã tạnh. Ji-ho nhìn lại cửa hàng rất lâu rồi nói: \"Đừng bao giờ quay lại ca đêm.\" Phía sau lưng, tấm biển HANA MART 24 tắt phụt — lần đầu tiên sau nhiều năm."
                : "Bạn sống sót qua ca đêm đầu tiên. Trên đường về, điện thoại rung: \"Em làm tốt lắm. Tối mai 12 giờ nhé.\" Trong kho, tủ đông vẫn còn tiếng gõ... Có lẽ bạn đã bỏ sót điều gì đó.";
            int done = progress.Flags.GetValueOrDefault("tDone"), all = progress.Flags.GetValueOrDefault("tAll");
            ShowDialog(rescued ? "KẾT THÚC: NGƯỜI THỨ HAI" : "KẾT THÚC: HẾT CA",
                $"{body}\n\nViệc xong {done}/{all} · Cảnh cáo {progress.Strikes}/3 · Số lần chết {progress.DeathCount} · Nhật ký {progress.Diaries.Count}/5",
                new Choice("Chơi lại", NewShift));
        }
    }
}
