using System;
using System.Collections.Generic;
using System.Linq;
using HanaMart.Player;
using HanaMart.Store;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HanaMart.Shift
{
    public sealed partial class ShiftGame
    {
        public ShiftTasks Tasks { get; private set; }
        public string PaperTitle { get; private set; }
        public string PaperText { get; private set; }

        readonly MicrowaveCycle microwave = new();
        readonly ShiftPhone phone = new();
        readonly Dictionary<string, StoreMarker> markers = new();
        InputAction rulesInput, tasksInput, pauseInput;
        bool rulesRead, restocking;
        float checkTime, fridgeOpenTime, duplicateTime;
        GameObject ovenFood;

        void InitializeJobs()
        {
            Tasks = new ShiftTasks(Run.Progress);
            var sceneObjects = FindObjectsByType<StoreMarker>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var marker in sceneObjects) markers.TryAdd(marker.key, marker);
            BindStock(sceneObjects);
            BindEnvironment(sceneObjects);
            if (player.Input)
            {
                rulesInput = player.Input.FindAction("Rules", true);
                tasksInput = player.Input.FindAction("Tasks", true);
                pauseInput = player.Input.FindAction("Pause", true);
            }
            carry.defaultInteractions = false;
            if (doors) doors.defaultInteractions = false;
            interactor.LabelFor = LabelFor;
            interactor.HoldSeconds = id => id switch { "monitor" => 10, "prints" => 2, "spill" => 2.5f, _ => 1 };
            interactor.Holding += Hold;
            StoryEvent += JobEvent;
            SceneAction += JobAction;
            RingRequested += RingPhone;
        }

        void ResetJobs(bool retry)
        {
            Tasks.Clear();
            PaperTitle = PaperText = null;
            if (!retry) rulesRead = false;
            checkTime = fridgeOpenTime = duplicateTime = 0;
            StopPhone();
            microwave.Reset();
            if (ovenFood) Destroy(ovenFood);
            ovenFood = null;
            carry.ResetCarry();
            carry.SetSourcePresent("R:mop", true);
            carry.SetSourcePresent("S:trashBag", true);
            bool ramenFull = Run.Progress.SavedHour > 0 || HasFlag("ramenDone");
            bool milkFull = Run.Progress.SavedHour > 2 || HasFlag("milkDone");
            carry.SetSourcePresent("R:boxRamen", !ramenFull);
            carry.SetSourcePresent("R:boxMilk", !milkFull);
            Stock("shelfRamen", ramenFull);
            Stock("shelfMilk", milkFull);
            restocking = false;
            ResetEnvironment();
            if (doors)
            {
                foreach (var door in doors.Fridges) door.motor.target = 0;
                foreach (var door in doors.Microwaves) door.motor.target = 0;
            }
        }

        void UpdateJobs(float dt)
        {
            TickPhone(dt);
            UpdateOven(dt);
            UpdateEnvironment(dt);
            var p = player.Motor.pos;
            if (Tasks.Pending("storage") && p.y < -.6f && (checkTime += dt) > 1.5f)
            {
                checkTime = 0;
                CompleteTask("storage");
                Say(HasFlag("strangerIn") ? "(Không thấy ai... nhưng thùng hàng bị xê dịch, dấu giày ướt dẫn vào góc tối.)" :
                    "(Kho trống trơn. Chỉ có tiếng tủ đông chạy rì rì.)");
            }
            if (Tasks.Pending("aisle") && Vector2.Distance(p, new Vector2(7.2f, 1.5f)) < 2.4f)
            {
                CompleteTask("aisle");
                Run.Progress.Flags.Remove("cctvGhost");
                Say("(Cuối lối giữa không có ai. Trên sàn có một vũng nước nhỏ hình bàn chân.)");
            }
            if (Tasks.Pending("door") && Vector2.Distance(p, new Vector2(3.1f, 7.3f)) < 1.4f)
            {
                CompleteTask("door");
                ghostDoorTime = 0;
                PlayClip("door");
                Say("(Bạn kéo cửa đóng lại. Cảm biến nhấp nháy đỏ một lúc rồi tắt.)", 3);
            }
            if (Tasks.Pending("fridgeClose") && doors)
            {
                fridgeOpenTime += dt;
                if (doors.Fridges.All(leaf => leaf.motor.target < .5f)) CompleteTask("fridgeClose");
                else if (fridgeOpenTime > 90 && !HasFlag("fridgeWarm"))
                {
                    Run.Progress.Flags["fridgeWarm"] = 1;
                    AddTask("temp2", "Tủ mát lên 9°C vì để mở — kiểm tra lại nhiệt độ");
                }
            }
            if (HasFlag("double") && (duplicateTime -= dt) < 0)
                Die(7, "Bạn đã không tắt màn hình camera khi thấy chính mình ở nơi khác.");
        }

        void JobEvent(ShiftEvent entry)
        {
            if (entry.IsAmbient) { Ambient(entry.AmbientKind); return; }
            switch (entry.Id)
            {
                case "intro":
                    AddTask("rules", "Đọc nội quy ca đêm");
                    AddTask("temp", "Kiểm tra nhiệt độ tủ mát");
                    AddTask("ramen", "Bổ sung mì ly (kệ có thẻ HẾT HÀNG)");
                    break;
                case "managerPhone": RingPhone("manager"); break;
                case "passerby": SpawnPasserby(); break;
                case "fax":
                    PlayClip("printer");
                    Run.Progress.Flags["addendum"] = 1;
                    Say("(Máy fax sau quầy tự in ra một tờ giấy...)", 3);
                    Later(1.5f, () => { if (!Dead) ShowPaper("FAX · TRỤ SỞ HANA MART", RuleBook.Addendum + "\n[DẤU ĐỎ HANA]"); });
                    break;
                case "trash": AddTask("trash", "Đổ rác ra thùng lớn ở hẻm sau"); break;
                case "milk":
                    if (!HasFlag("milkDone")) AddTask("milk", "Bổ sung bánh mandu (kệ có thẻ HẾT HÀNG)");
                    break;
                case "trashAgain": AddTask("trash", "Đổ rác lần hai ra hẻm sau"); break;
                case "cctvDouble": BeginDuplicate(24); break;
                case "cctvDoubleLate": BeginDuplicate(18); break;
                case "hqTrap": RingPhone("hqTrap"); break;
                case "clockGlitch": Run.Clock.FakeTime(22); RingPhone("clock444"); PlayClip("sting"); break;
                case "phone444": RingPhone("444"); break;
            }
        }

        void JobAction(string action)
        {
            switch (action)
            {
                case "heatOrdered": AddTask("heat", $"Hâm {ProductName(carry.FoodType).ToLowerInvariant()} ở lò vi sóng (tường phải) rồi mang lại quầy"); break;
                case "heatSold": CompleteTask("heat"); break;
                case "steps": Ambient("steps"); break;
                case "stationSteam": SteamAt(new Vector3(11.5f, 1.1f, 5.35f), 6, 14); break;
                case "barSteam": SteamAt(new Vector3(8, .98f, 7.6f), 22, 7); break;
            }
        }

        void AddTask(string id, string text)
        {
            if (!Tasks.Add(id, text)) return;
            Say("Việc mới: " + text, 3);
            PlayClip("ui_task");
        }

        void CompleteTask(string id)
        {
            if (Tasks.Complete(id)) PlayClip("ui_ok");
        }

        string LabelFor(string id, string label)
        {
            string kind = carry.Kind;
            return id switch
            {
                "pos" => !CanServe ? null : counterCustomer.Profile.Items == null ? "Nói chuyện" :
                    kind == "hot" ? "Đưa món nóng & tính tiền" : "Tính tiền",
                "rules" => "Đọc nội quy",
                "clipboard" => "Xem việc cần làm",
                "phone" => phone.Ringing ? "Nghe máy" : null,
                "monitor" => HasFlag("double") ? "Giữ để tắt màn hình và đếm đến 10" : "Xem camera",
                "fridgeTemp" => "Kiểm tra nhiệt độ",
                "boxRamen" => kind == null && Visible("R:boxRamen") ? "Bê thùng mì ly" : null,
                "boxMilk" => kind == null && Visible("R:boxMilk") ? "Bê thùng bánh mandu" : null,
                "shelfRamen" => RestockLabel("shelfRamen", "boxRamen", "mì ly", kind),
                "shelfMilk" => RestockLabel("shelfMilk", "boxMilk", "bánh mandu", kind),
                "mop" => kind == "mop" ? "Dựng cây lau lại chỗ cũ" : kind == null && Visible("R:mop") ? "Cầm cây lau nhà" : null,
                "dropped" or "fallen" => kind == null ? label : null,
                "prints" => kind == "mop" ? "Giữ để lau dấu tay" : "Dấu tay ướt (cần cây lau)",
                "spill" => kind == "mop" ? "Giữ để lau sàn" : "Vết nước đổ (cần cây lau)",
                "fridgeDoor" => doors && doors.FridgeIsOpen(interactor.Current) ? "Đóng cửa tủ" : "Mở cửa tủ mát",
                "trash" => kind == null && Tasks.Pending("trash") && Visible("S:trashBag") ? "Lấy túi rác" : null,
                "dumpster" => kind == "trash" ? "Bỏ rác" : HasFlag("trashDone") && !ReadDiary(id) ? "Có tờ giấy dính trên nắp" : null,
                "micro1" or "micro2" => MicrowaveLabel(id, kind),
                "freezer" => HasFlag("jiho") ? null : "Mở tủ đông",
                "backDoor" => doors && doors.Back.motor.target > .5f ? "Đóng cửa sau" : "Mở cửa sau",
                "deliveryStack" => HasFlag("delivery") && !HasFlag("deliveryDone") ? "Đếm hàng & ký nhận" : null,
                "drawer" => ReadDiary(id) ? null : "Mở ngăn kéo",
                "locker" => ReadDiary(id) ? null : "Mở tủ đồ của Ji-ho",
                _ => label,
            };
        }

        void UseJob(string id)
        {
            switch (id)
            {
                case "rules": OpenRules(); break;
                case "clipboard": OpenTasks(); break;
                case "phone": AnswerPhone(); break;
                case "monitor":
                    if (!HasFlag("double")) Say("Camera: kho và cửa hàng. Mọi thứ có vẻ bình thường.", 3);
                    break;
                case "fridgeTemp":
                    if (Tasks.Pending("temp2")) { Say("Tủ mát: 9°C → đã chỉnh về 3°C. Một ít hàng phải bỏ.", 3); CompleteTask("temp2"); }
                    else { Say("Tủ mát: 3.0°C. Ổn.", 3); CompleteTask("temp"); }
                    break;
                case "boxRamen": case "boxMilk": case "mop": case "trash": case "dropped":
                    carry.Use(id, interactor.Current);
                    if (id.StartsWith("box", StringComparison.Ordinal)) PlayClip("door");
                    break;
                case "shelfRamen": case "shelfMilk": BeginRestock(id); break;
                case "dumpster": ThrowTrash(); break;
                case "micro1": case "micro2": UseMicrowave(id); break;
                case "freezer": case "backDoor": case "fridgeDoor":
                    if (doors) doors.Use(id, interactor.Current);
                    PlayClip("door");
                    break;
                case "drawer": case "locker": FindDiary(id); break;
                case "breaker": Say(HasFlag("blackout") ? "Nội quy số 10: ngồi thấp sau quầy. Đừng động vào." : "Cầu dao vẫn bình thường.", 3); break;
                case "switch": Say("Không được tắt đèn trong ca đêm.", 2); break;
                case "fallen": PickFallen(interactor.Current); break;
            }
        }

        void Hold(string id, float amount)
        {
            if (amount < 1 || Dead) return;
            if (id == "monitor" && HasFlag("double"))
            {
                Run.Progress.Flags.Remove("double");
                director.Calm(30);
                Say("...10. Bạn bật lại màn hình. Kho trống trơn.", 3);
            }
            if (carry.Kind == "mop")
            {
                if (id == "spill") CleanSpill();
                if (id == "prints") CleanPrints();
            }
        }

        void ReadPaperInput()
        {
            if (PaperTitle != null)
            {
                if (pauseInput?.WasPressedThisFrame() == true || confirmInput?.WasPressedThisFrame() == true ||
                    rulesInput?.WasPressedThisFrame() == true || tasksInput?.WasPressedThisFrame() == true) ClosePaper();
                return;
            }
            if (Dialogue != null || Dead || Ended) return;
            if (rulesInput?.WasPressedThisFrame() == true) OpenRules();
            if (tasksInput?.WasPressedThisFrame() == true) OpenTasks();
        }

        public void ShowPaper(string title, string text)
        {
            PaperTitle = title;
            PaperText = text;
            SetGameplay(false);
        }

        public void ClosePaper()
        {
            PaperTitle = PaperText = null;
            SetGameplay(!Dead && !Ended && Dialogue == null);
        }

        void OpenRules()
        {
            ShowPaper("NỘI QUY CA ĐÊM", RuleBook.Text(Run.Progress));
            if (rulesRead && !ReadDiary("rulesBack")) FindDiary("rulesBack");
            rulesRead = true;
            CompleteTask("rules");
        }

        void OpenTasks() => ShowPaper("VIỆC CẦN LÀM", string.Join("\n", Tasks.Items.Select(t => (t.Done ? "☑ " : "☐ ") + t.Text)));
        bool ReadDiary(string id) => Run.Progress.Diaries.Contains(id);

        void FindDiary(string id)
        {
            if (!Run.Progress.Diaries.Add(id)) return;
            int pages = Run.Progress.Diaries.Count;
            ShowPaper($"NHẬT KÝ CỦA JI-HO · {pages}/5", RuleBook.DiaryPage(pages));
            PlayClip("paper");
        }

        void ThrowTrash()
        {
            if (carry.Kind != "trash")
            {
                if (HasFlag("trashDone")) FindDiary("dumpster");
                return;
            }
            carry.SetCarry(null);
            Run.Progress.Flags["trashDone"] = 1;
            Run.Progress.Flags["alleyPending"] = 1;
            CompleteTask("trash");
            PlayClip("thud");
        }

        string MicrowaveLabel(string id, string kind)
        {
            if (microwave.Oven == id) return microwave.Ready ? "Lấy món nóng ra" : $"Đang hâm... ({Math.Ceiling(microwave.SecondsRemaining)}s)";
            if (kind == "heat" && !microwave.Active) return $"Mở lò & cho {ProductName(carry.FoodType).ToLowerInvariant()} vào hâm";
            return id == "micro2" && !ReadDiary(id) && !microwave.Active ? "Có tờ giấy kẹt trong lò" : null;
        }

        void UseMicrowave(string id)
        {
            int index = id == "micro1" ? 0 : 1;
            if (carry.Kind == "heat" && microwave.Begin(id))
            {
                carry.SetCarry(null);
                SetOvenDoor(index, true);
                var holder = Marker($"S:microwaves[{index}].food");
                if (holder) ovenFood = CreateProp(carry.FoodType, ThreeCoords.ToUnity(holder.transform.position), .13f);
                if (ovenFood) ovenFood.SetActive(false);
            }
            else if (carry.Kind == null && microwave.Take(id))
            {
                if (ovenFood) Destroy(ovenFood);
                ovenFood = null;
                carry.SetCarry("hot");
                Later(.4f, () => { if (microwave.Oven != id) SetOvenDoor(index, false); });
            }
            else if (id == "micro2" && !microwave.Active) FindDiary(id);
        }

        void SetOvenDoor(int index, bool open)
        {
            if (doors && index < doors.Microwaves.Count) doors.Microwaves[index].motor.target = open ? 1 : 0;
            PlayClip("door");
        }

        void UpdateOven(float dt)
        {
            if (!microwave.Active) return;
            int index = microwave.Oven == "micro1" ? 0 : 1;
            var change = microwave.Tick(dt);
            if ((change & MicrowaveChange.Insert) != 0 && ovenFood) ovenFood.SetActive(true);
            if ((change & MicrowaveChange.Close) != 0) SetOvenDoor(index, false);
            if ((change & MicrowaveChange.Heat) != 0) PlayClip("microwave");
            if (microwave.Heating && ovenFood) ovenFood.transform.Rotate(0, dt * 1.2f * Mathf.Rad2Deg, 0);
            if ((change & MicrowaveChange.Ready) == 0) return;
            SetOvenDoor(index, true);
            PlayClip("ding");
            Say("Ding! Món đã nóng — lấy ra rồi mang lại quầy.", 3);
            SteamAt(new Vector3(11.45f, 1.12f, index == 0 ? 6.15f : 6.95f), 5, 14);
        }

        void RingPhone(string kind)
        {
            phone.Call(kind);
            Say($"(Điện thoại bàn reo. Màn hình: {phone.Caller})");
            PhoneLed(0x30ff60);
        }

        void StopPhone()
        {
            phone.Stop();
            PhoneLed(0x103010);
        }

        void PhoneLed(int rgb)
        {
            var led = Marker("S:phoneLed");
            if (led && led.TryGetComponent<Renderer>(out var renderer)) ChangeTint(renderer, rgb);
        }

        void TickPhone(float dt)
        {
            if (phone.Tick(dt, out var expired)) PlayClip("ring");
            if (expired == null) return;
            PhoneLed(0x103010);
            if (expired == "manager") return;
            Say(expired == "self" ? "(Ngừng reo. Đèn tin nhắn thoại nhấp nháy... bạn không bật nghe.)" :
                expired == "hq" ? "(Cuộc gọi nhỡ.)" : "(Điện thoại ngừng reo.)", expired == "444" || expired == "hq" ? 2 : 3);
        }

        void AnswerPhone()
        {
            string kind = phone.Kind;
            if (kind == null) return;
            StopPhone();
            switch (kind)
            {
                case "manager":
                    ShowDialog("Quản lý (điện thoại)", "Minh à? Chị quản lý đây. Ca đầu tiên nhỉ. Nội quy dán sau quầy, đọc kỹ và làm ĐÚNG từng điều. Không hỏi, không tò mò. Sáng 6 giờ có chị Mi-rae vào nhận ca. Chúc em... một đêm yên ổn.", new Choice("Vâng ạ.", null));
                    break;
                case "444": Die(12, "Bạn đã nghe điện thoại lúc 04:44."); break;
                case "self": Die(16, "Cuộc gọi đến từ chính chiếc điện thoại này. Đầu dây bên kia là giọng của bạn."); break;
                case "clock444": Die(12, "Đồng hồ đang chỉ 04:44 khi bạn nhấc máy."); break;
                case "hqTrap":
                    Run.Progress.Flags["hqTrap"] = 1;
                    ShowDialog("Trụ sở (điện thoại)", "Trụ sở đây. Lát nữa chị quản lý vùng tới kiểm kho đột xuất. Em MỞ KHO cho chị ấy nhé, đừng làm khó.", new Choice("Vâng ạ.", null));
                    break;
                case "hq":
                    double minute = Run.Clock.Minutes;
                    if (Math.Floor(minute) == 284) { Die(12, "Bạn đã nghe điện thoại lúc 04:44."); break; }
                    string clue = minute < 170 ? "Trụ sở đây. 3 giờ có xe giao hàng. Nhớ ĐẾM số thùng trước khi ký." :
                        minute < 240 ? "Trụ sở đây. Gần 4 giờ hay mất điện khu đó. Nhớ nội quy số 10." :
                        minute < 300 ? "Trụ sở đây. Tờ nội quy trên tường... có điều nào KHÔNG có dấu đỏ không? Đừng tin điều đó." :
                        "Trụ sở đây. Ca sáng sẽ đến đúng 6 giờ. Không sớm hơn một phút.";
                    ShowDialog("Điện thoại bàn", clue, new Choice("Vâng, em nhớ rồi.", null));
                    break;
            }
        }

        StoreMarker Marker(string key) => markers.GetValueOrDefault(key);
        bool Visible(string key) { var marker = Marker(key); return marker && marker.gameObject.activeSelf; }
    }
}
