using System;
using System.Collections.Generic;
using System.Linq;
using HanaMart.Shift;
using HanaMart.Store;
using UnityEngine;

namespace HanaMart.Sim
{
    public sealed class SimDialogOption
    {
        public string Label { get; }
        public bool Ok { get; }
        public string Trap { get; }
        public Action Choose { get; }

        public SimDialogOption(string label, Action choose, bool ok = true, string trap = null)
        {
            Label = label;
            Choose = choose;
            Ok = ok;
            Trap = trap;
        }
    }

    public sealed class SimDialog
    {
        public string Speaker { get; }
        public string Text { get; }
        public SimDialogOption[] Options { get; }

        public SimDialog(string speaker, string text, SimDialogOption[] options)
        {
            Speaker = speaker;
            Text = text;
            Options = options;
        }
    }

    public sealed class ShiftSimHost
    {
        sealed class SimTimer
        {
            public double Remaining;
            public Action Fn;
        }

        sealed class SimGuest
        {
            public int Id;
            public string Kind;
            public string Name;
            public string Line;
            public string[] Items;
            public int Aisle;
            public bool Heat;
            public bool Station;
            public bool Visitor;
            public bool Chime;
            public bool Inside;
            public bool Greeted;
            public bool BlackEntry;
            public bool Waiting;
            public bool Paid;
            public bool IntoStorage;
            public bool HeatAsked;
            public bool Heated;
            public bool Alive = true;
            public float Speed;
            public Vector2 Pos;
            public SimGuest Companion;
            public IEnumerator<bool> Routine;
            public IReadOnlyList<Vector2> Waypoints;
            public int WaypointIndex;
            public bool Walking;
            public double WaitRemaining;
        }

        static readonly string[] OrdinaryKinds =
            { "office", "student", "nurse", "rider", "couple", "worker", "office2", "gamer" };

        static readonly Vector2 DoorIn = new(3.1f, 7.0f);
        static readonly Vector2 DoorOut = new(3.1f, 9.2f);
        static readonly Vector2 Register = new(2.45f, 4.4f);
        static readonly Vector2[] AisleFront = { new(5.8f, 6.7f), new(8.4f, 6.7f), new(10.6f, 6.7f) };
        static readonly Vector2[] AisleMiddle = { new(5.8f, 3.8f), new(8.4f, 3.8f), new(10.6f, 3.8f) };

        static readonly Vector2[] CeilingLights =
        {
            new(3.4f, 2.3f), new(3.4f, 5.3f),
            new(5.9f, 2.3f), new(5.9f, 5.3f),
            new(8.5f, 2.3f), new(8.5f, 5.3f),
            new(11.0f, 2.3f), new(11.0f, 5.3f),
            new(0.8f, 3.4f), new(0.8f, 6.0f),
            new(5.9f, 7.1f), new(8.5f, 7.1f), new(11.0f, 7.1f),
        };

        readonly System.Random rng;
        readonly StoreNavigation navigation;
        readonly List<SimTimer> timers = new();
        readonly List<SimGuest> guests = new();
        readonly CheckoutQueue queue = new();
        readonly GreetingWindow greeting = new();
        readonly MicrowaveCycle microwave = new();
        readonly ShiftPhone phone = new();

        public ShiftRun Run { get; }
        public VisitDirector Director { get; }
        public ShiftTasks Tasks { get; }
        public SimDialog CurrentDialog { get; private set; }
        public bool DialogOpen => CurrentDialog != null;
        public bool Dead { get; private set; }
        public bool Ended { get; private set; }
        public string Ending { get; private set; }
        public int LastDeathRule { get; private set; } = -1;
        public string LastDeathReason { get; private set; }

        public Vector2 PlayerPos { get; set; } = new(0.85f, 4.4f);
        public Vector2 PlayerForward { get; set; } = new(1f, 0f);
        public float PlayerCrouch { get; set; }
        public string Carry { get; set; }
        public string CarryFoodType { get; set; }
        public bool RulesRead { get; set; }
        public bool BackDoorOpen { get; set; }
        public bool FridgeDoorOpen { get; set; }
        public bool Restocking { get; private set; }
        public bool MicrowaveReady => microwave.Ready;
        public string ActiveMicrowaveOven => microwave.Oven;

        public float GreetWindow => greeting.Remaining;
        public bool GreetGhost { get; private set; }
        public int GreetSerial { get; private set; }
        public bool PhoneRinging => phone.Ringing;
        public string PhoneKind => phone.Kind;

        public bool HasCounterCustomer => counterCustomer != null && counterCustomer.Waiting;
        public int CounterCustomerId => counterCustomer?.Id ?? -1;
        public bool CounterCustomerHeatAsked => counterCustomer?.HeatAsked ?? false;
        public bool Scanning { get; private set; }

        public bool HasStarer => starer != null && starer.Alive;
        public int StarerId => starer?.Id ?? -1;
        public float StarerLookAcc { get; set; }

        public bool RedDark => redHour?.Dark ?? false;
        public Vector2 CallPosition { get; private set; }
        public Vector2 ChaseSafeLight { get; private set; }
        public double ChaseDeadline { get; private set; }

        public event Action<int, string> StrikeLogged;

        SimGuest counterCustomer;
        SimGuest greetingFor;
        SimGuest starer;
        RedHour redHour;
        int nextGuestId;
        int deliveryCount;
        double scanRemaining;
        SimGuest scanGuest;
        float blackoutTime, blackoutEntryAt, glassLook, starerTime, knockTime, deliveryDeadline, callTime, chaseRemaining;
        float fridgeOpenTime, duplicateTime, checkTime;
        bool hasChaseLight;

        public ShiftSimHost(int seed, StoreNavigation navigation = null)
        {
            rng = new System.Random(seed);
            this.navigation = navigation;
            Run = new ShiftRun(rng.NextDouble);
            Director = new VisitDirector(rng.NextDouble);
            Tasks = new ShiftTasks(Run.Progress);
            Run.EventReached += OnStoryEvent;
        }

        public void NewShift() => ResetShift(false);
        public void Retry() => ResetShift(true);

        void ResetShift(bool retry)
        {
            timers.Clear();
            ClearGuests();
            if (retry) Run.Retry();
            else Run.NewShift();
            Director.Reset(Run.Progress.SavedHour > 0);
            Tasks.Clear();
            phone.Stop();
            microwave.Reset();
            Dead = false;
            Ended = false;
            Ending = null;
            LastDeathRule = -1;
            LastDeathReason = null;
            CurrentDialog = null;
            if (!retry) RulesRead = false;
            Carry = null;
            CarryFoodType = null;
            BackDoorOpen = false;
            FridgeDoorOpen = false;
            Restocking = false;
            Scanning = false;
            scanRemaining = 0;
            scanGuest = null;
            redHour = null;
            starer = null;
            hasChaseLight = false;
            blackoutTime = blackoutEntryAt = glassLook = starerTime = StarerLookAcc = knockTime = 0;
            deliveryDeadline = callTime = chaseRemaining = fridgeOpenTime = duplicateTime = checkTime = 0;
            deliveryCount = 0;
            PlayerPos = new Vector2(0.85f, 4.4f);
            PlayerForward = new Vector2(1f, 0f);
            PlayerCrouch = 0f;
            GreetGhost = false;
        }

        public bool HasFlag(string key) => Run.Progress.Flags.TryGetValue(key, out int v) && v != 0;

        public int LiveVisitors()
        {
            int count = 0;
            for (int i = 0; i < guests.Count; ++i)
                if (guests[i].Visitor && guests[i].Alive) ++count;
            return count;
        }

        public void Later(double seconds, Action fn)
        {
            if (fn == null) return;
            timers.Add(new SimTimer { Remaining = seconds, Fn = fn });
        }

        public void SelectDialogOption(int index)
        {
            if (CurrentDialog == null || index < 0 || index >= CurrentDialog.Options.Length) return;
            var opt = CurrentDialog.Options[index];
            CurrentDialog = null;
            opt.Choose?.Invoke();
        }

        public void Tick(double dt, bool greetPressed)
        {
            if (Dead || Ended) return;
            float fdt = (float)dt;
            Run.Tick(dt, dialogOpen: DialogOpen, paperOpen: false);

            double minute = Run.Clock.Minutes;
            var next = Run.Timeline.NextAnchor(minute, 0.5, 60);
            string visitKind = Director.Tick(dt, minute, LiveVisitors(), next?.Minute,
                HasFlag("red") || HasFlag("blackout"), phone.Ringing, Run.Progress);
            if (visitKind != null) SpawnVisit(visitKind);

            UpdateTimers(dt);
            if (Dead || Ended) return;

            UpdatePhone(dt);
            UpdateOven(dt);
            UpdateScanning(dt);
            UpdateHazards(fdt);
            if (Dead || Ended) return;

            UpdateTaskChecks(fdt);
            if (HasFlag("double"))
            {
                duplicateTime -= fdt;
                if (duplicateTime < 0)
                {
                    Die(7, "Bạn đã không tắt màn hình camera khi thấy chính mình ở nơi khác.");
                    return;
                }
            }

            UpdateGuests(fdt);
            UpdateFrontDoor(fdt, greetPressed);
        }

        void UpdateTimers(double dt)
        {
            if (timers.Count == 0) return;
            var due = new List<Action>();
            for (int i = timers.Count - 1; i >= 0; --i)
            {
                timers[i].Remaining -= dt;
                if (timers[i].Remaining <= 0)
                {
                    due.Add(timers[i].Fn);
                    timers.RemoveAt(i);
                }
            }
            for (int i = due.Count - 1; i >= 0; --i)
            {
                if (Dead || Ended) break;
                due[i]?.Invoke();
            }
        }

        void OnStoryEvent(ShiftEvent entry)
        {
            if (entry.IsAmbient)
            {
                Ambient(entry.AmbientKind);
                return;
            }
            switch (entry.Id)
            {
                case "intro":
                    AddTask("rules");
                    AddTask("temp");
                    AddTask("ramen");
                    Director.StartIn(14);
                    break;
                case "managerPhone": RingPhone("manager"); break;
                case "fax": Run.Progress.Flags["addendum"] = 1; break;
                case "trash": AddTask("trash"); break;
                case "milk":
                    if (!HasFlag("milkDone")) AddTask("milk");
                    break;
                case "phase2":
                    Run.Progress.Flags["knock"] = 1;
                    knockTime = 2;
                    break;
                case "calm2": Director.Calm(35); break;
                case "cctvDouble": BeginDuplicate(24); break;
                case "delivery": StartDelivery(); break;
                case "hqTrap": RingPhone("hqTrap"); break;
                case "manager": SpecialVisitor("manager"); break;
                case "clockGlitch":
                    Run.Clock.FakeTime(22);
                    RingPhone("clock444");
                    break;
                case "blackout": Blackout(); break;
                case "starer": OutsideStarer(1); break;
                case "forge12": Run.Progress.Flags["fake12"] = 1; break;
                case "trashAgain": AddTask("trash"); break;
                case "phone444": RingPhone("444"); break;
                case "cctvDoubleLate": BeginDuplicate(18); break;
                case "freezerJiho":
                    if (NightRules.CanRescueJiho(Run.Progress.Diaries.Count, Run.Clock.Minutes, HasFlag("jiho")))
                    {
                        Run.Progress.Flags["knock323"] = 1;
                        knockTime = 1;
                    }
                    break;
                case "forge13": Run.Progress.Flags["fake13"] = 1; break;
                case "redHour": StartRedHour(); break;
                case "doubleShift": SpecialVisitor("dblShift"); break;
                case "screamGlass": ScreamGlass(); break;
                case "lateDelivery": LateDelivery(); break;
                case "fakeMirae": SpecialVisitor("mirae"); break;
                case "starerLate": OutsideStarer(3); break;
                case "realMirae": SpecialVisitor("realMirae"); break;
            }
        }

        void Ambient(string kind)
        {
            if (HasFlag("red") || HasFlag("blackout")) return;
            switch (kind)
            {
                case "doorHeld": AddTask("door"); break;
                case "cctvGhost":
                    Run.Progress.Flags["cctvGhost"] = 1;
                    AddTask("aisle");
                    break;
                case "handprints": AddTask("prints"); break;
                case "fridge":
                    FridgeDoorOpen = true;
                    AddTask("fridgeClose");
                    fridgeOpenTime = 0;
                    break;
                case "steps": AddTask("storage"); break;
                case "can": AddTask("fallen"); break;
            }
        }

        void AddTask(string id) => Tasks.Add(id, id);
        public void DoneTask(string id) => Tasks.Complete(id);

        void UpdateTaskChecks(float dt)
        {
            if (Tasks.Pending("storage") && PlayerPos.y < -0.6f && (checkTime += dt) > 1.5f)
            {
                checkTime = 0;
                DoneTask("storage");
            }
            if (Tasks.Pending("aisle") && Vector2.Distance(PlayerPos, new Vector2(7.2f, 1.5f)) < 2.4f)
            {
                DoneTask("aisle");
                Run.Progress.Flags.Remove("cctvGhost");
            }
            if (Tasks.Pending("door") && Vector2.Distance(PlayerPos, new Vector2(3.1f, 7.3f)) < 1.4f)
            {
                DoneTask("door");
            }
            if (Tasks.Pending("fridgeClose"))
            {
                fridgeOpenTime += dt;
                if (!FridgeDoorOpen) DoneTask("fridgeClose");
                else if (fridgeOpenTime > 90 && !HasFlag("fridgeWarm"))
                {
                    Run.Progress.Flags["fridgeWarm"] = 1;
                    AddTask("temp2");
                }
            }
        }

        public void Use(string id)
        {
            if (Dead || Ended) return;
            switch (id)
            {
                case "rules":
                    RulesRead = true;
                    DoneTask("rules");
                    break;
                case "fridgeTemp":
                    if (Tasks.Pending("temp2")) DoneTask("temp2");
                    else DoneTask("temp");
                    break;
                case "boxRamen":
                    if (Carry == null) Carry = "boxRamen";
                    break;
                case "boxMilk":
                    if (Carry == null) Carry = "boxMilk";
                    break;
                case "shelfRamen":
                    if (!Restocking && Carry == "boxRamen")
                    {
                        Carry = null;
                        Restocking = true;
                        Later(3.0, () =>
                        {
                            Restocking = false;
                            Run.Progress.Flags["ramenDone"] = 1;
                            DoneTask("ramen");
                        });
                    }
                    break;
                case "shelfMilk":
                    if (!Restocking && Carry == "boxMilk")
                    {
                        Carry = null;
                        Restocking = true;
                        Later(3.0, () =>
                        {
                            Restocking = false;
                            Run.Progress.Flags["milkDone"] = 1;
                            DoneTask("milk");
                        });
                    }
                    break;
                case "mop":
                    if (Carry == null) Carry = "mop";
                    else if (Carry == "mop") Carry = null;
                    break;
                case "trash":
                    if (Carry == null && Tasks.Pending("trash")) Carry = "trash";
                    break;
                case "backDoor":
                    BackDoorOpen = !BackDoorOpen;
                    break;
                case "dumpster":
                    if (Carry == "trash")
                    {
                        Carry = null;
                        Run.Progress.Flags["trashDone"] = 1;
                        Run.Progress.Flags["alleyPending"] = 1;
                        DoneTask("trash");
                    }
                    else if (HasFlag("trashDone"))
                    {
                        FindDiary("dumpster");
                    }
                    break;
                case "fallen":
                    DoneTask("fallen");
                    break;
                case "micro1":
                case "micro2":
                    UseMicrowave(id);
                    break;
                case "freezer":
                    OpenFreezer();
                    break;
                case "fridgeDoor":
                    FridgeDoorOpen = !FridgeDoorOpen;
                    break;
            }
        }

        public void CleanSpill()
        {
            DoneTask("mop");
        }

        public void CleanPrints()
        {
            DoneTask("prints");
        }

        public void CloseFridge()
        {
            FridgeDoorOpen = false;
            DoneTask("fridgeClose");
        }

        public void FindDiary(string id) => Run.Progress.Diaries.Add(id);

        void UseMicrowave(string id)
        {
            if (Carry == "heat" && microwave.Begin(id))
            {
                Carry = null;
            }
            else if (Carry == null && microwave.Take(id))
            {
                Carry = "hot";
            }
            else if (id == "micro2" && !microwave.Active)
            {
                FindDiary(id);
            }
        }

        void UpdateOven(double dt)
        {
            if (!microwave.Active) return;
            microwave.Tick(dt);
        }

        public void CctvOff()
        {
            if (!HasFlag("double")) return;
            Run.Progress.Flags.Remove("double");
            Director.Calm(30);
        }

        void BeginDuplicate(float seconds)
        {
            duplicateTime = seconds + 5 * Mathf.Min(3, Run.Progress.Mercy.GetValueOrDefault("r7"));
            Run.Progress.Flags["double"] = 1;
        }

        void RingPhone(string kind) => phone.Call(kind);

        void UpdatePhone(double dt) => phone.Tick(dt, out _);

        public void AnswerPhone()
        {
            string kind = phone.Kind;
            if (kind == null) return;
            phone.Stop();
            switch (kind)
            {
                case "manager":
                    ShowDialog("Quản lý (điện thoại)", "Chúc em... một đêm yên ổn.",
                        new SimDialogOption("Vâng ạ.", null));
                    break;
                case "444":
                    Die(12, "Bạn đã nghe điện thoại lúc 04:44.");
                    break;
                case "self":
                    Die(16, "Cuộc gọi đến từ chính chiếc điện thoại này. Đầu dây bên kia là giọng của bạn.");
                    break;
                case "clock444":
                    Die(12, "Đồng hồ đang chỉ 04:44 khi bạn nhấc máy.");
                    break;
                case "hqTrap":
                    Run.Progress.Flags["hqTrap"] = 1;
                    ShowDialog("Trụ sở (điện thoại)", "Em MỞ KHO cho chị ấy nhé.",
                        new SimDialogOption("Vâng ạ.", null));
                    break;
                case "hq":
                    if (Math.Floor(Run.Clock.Minutes) == 284)
                    {
                        Die(12, "Bạn đã nghe điện thoại lúc 04:44.");
                        break;
                    }
                    ShowDialog("Điện thoại bàn", "Trụ sở đây.", new SimDialogOption("Vâng, em nhớ rồi.", null));
                    break;
            }
        }

        public void OpenFreezer()
        {
            if (NightRules.FreezerForbidden(Run.Clock.Minutes))
            {
                Die(6, "Bạn đã mở tủ đông lúc 02:00–02:10.");
                return;
            }
            if (!HasFlag("knock323") || HasFlag("jiho")) return;
            Run.Progress.Flags["jiho"] = 1;
            Run.Progress.Flags.Remove("knock323");
            ShowDialog("Ji-ho", "Đừng giao ca cho ai trước 6 giờ.",
                new SimDialogOption("Ra quầy với tôi.", null));
        }

        void StartDelivery()
        {
            Run.Progress.Flags["delivery"] = 1;
            deliveryCount = rng.NextDouble() < 0.6 ? 13 : 12;
            AddTask("delivery");
        }

        public void DeliveryDialog()
        {
            if (!HasFlag("delivery") || HasFlag("deliveryDone")) return;
            bool okSign = deliveryCount == 12;
            ShowDialog("Người giao hàng", "Ký vào đây là xong.",
                new SimDialogOption("Ký nhận", () => FinishDelivery(true), ok: okSign, trap: "delivery"),
                new SimDialogOption("Không ký nhận (số thùng không đúng)", () => FinishDelivery(false), ok: !okSign, trap: "delivery"));
        }

        void FinishDelivery(bool sign)
        {
            Run.Progress.Flags["deliveryDone"] = 1;
            DoneTask("delivery");
            switch (NightRules.Deliver(deliveryCount, sign))
            {
                case DeliveryResult.Strike:
                    Strike(8, "Hàng có đủ 12 thùng mà bạn không ký nhận.");
                    break;
                case DeliveryResult.ThirteenthBox:
                    Run.Progress.Flags["box13"] = 1;
                    deliveryDeadline = 20;
                    break;
            }
        }

        void Blackout()
        {
            Run.Progress.Flags["blackout"] = 1;
            Run.Progress.Flags.Remove("blackEntry");
            blackoutTime = glassLook = 0;
            blackoutEntryAt = 10f + (float)rng.NextDouble() * 6f;
        }

        void UpdateHazards(float dt)
        {
            if (HasFlag("knock") && NightRules.FreezerForbidden(Run.Clock.Minutes) && (knockTime -= dt) < 0)
                knockTime = 4f + (float)rng.NextDouble() * 3f;
            if (HasFlag("knock323") && (knockTime -= dt) < 0)
                knockTime = 9f;

            if (redHour != null)
            {
                if (redHour.Tick(dt, PlayerPos))
                {
                    Die(14, "Bạn đã cử động trong bóng tối của Giờ Đỏ.");
                    return;
                }
                if (redHour.Finished)
                {
                    Run.Progress.Flags.Remove("red");
                    redHour = null;
                }
            }

            if (HasFlag("blackout"))
            {
                blackoutTime += dt;
                if (blackoutTime > blackoutEntryAt && !HasFlag("blackEntry"))
                {
                    Run.Progress.Flags["blackEntry"] = 1;
                    var entrant = new SimGuest
                    {
                        Id = ++nextGuestId,
                        Kind = "blackEntry",
                        Name = "Bóng trong cửa hàng",
                        Pos = new Vector2(3.1f, 10f),
                        Speed = 1.15f,
                        BlackEntry = true,
                        Chime = true,
                        Inside = false,
                    };
                    entrant.Routine = BlackoutGuestRoutine(entrant);
                    guests.Add(entrant);
                }
                bool looking = PlayerForward.y > 0.35f;
                glassLook = looking ? glassLook + dt : 0f;
                if (blackoutTime > NightRules.BlackoutGrace(Run.Progress.Mercy.GetValueOrDefault("r10")) &&
                    !NightRules.HasShelter(PlayerPos, PlayerCrouch))
                {
                    Die(10, "Bạn đã không ngồi thấp sau quầy khi mất điện.");
                    return;
                }
                if (glassLook > 1.4f)
                {
                    Die(10, "Bạn đã nhìn ra cửa kính khi mất điện.");
                    return;
                }
                if (blackoutTime > 38f)
                {
                    Run.Progress.Flags.Remove("blackout");
                    Director.Calm(45);
                }
            }

            if (starer != null && starer.Alive)
            {
                starerTime -= dt;
                if (starerTime <= 0)
                {
                    RemoveGuest(starer);
                    starer = null;
                }
            }

            UpdateAlley(dt);
            if (Dead) return;
            UpdateChase(dt);
            if (Dead) return;

            if (HasFlag("box13") && (deliveryDeadline -= dt) < 0)
            {
                Run.Progress.Flags.Remove("box13");
                Die(8, "Bạn đã ký nhận thùng hàng thứ 13.");
            }
        }

        void UpdateAlley(float dt)
        {
            if (HasFlag("alleyPending") && PlayerPos.y < -4.4f)
            {
                var toBack = (new Vector2(2f, -4.6f) - PlayerPos).normalized;
                if (Vector2.Dot(PlayerForward.normalized, toBack) > 0.6f)
                {
                    Run.Progress.Flags.Remove("alleyPending");
                    Run.Progress.Flags["alleyCall"] = 1;
                    CallPosition = new Vector2(PlayerPos.x - PlayerForward.x * 5f, Mathf.Clamp(PlayerPos.y - PlayerForward.y * 5f, -8.9f, -5f));
                    callTime = 0;
                }
            }
            if (PlayerPos.y > -4.2f)
            {
                Run.Progress.Flags.Remove("alleyPending");
                Run.Progress.Flags.Remove("alleyCall");
                return;
            }
            if (!HasFlag("alleyCall")) return;
            callTime += dt;
            var toward = (CallPosition - PlayerPos).normalized;
            if (callTime > 1.2f && Vector2.Dot(toward, PlayerForward.normalized) > Mathf.Cos(1f))
                Die(5, "Bạn đã quay đầu khi có tiếng gọi tên mình ở hẻm sau.");
        }

        void OutsideStarer(int count)
        {
            starer = new SimGuest
            {
                Id = ++nextGuestId,
                Kind = "starer",
                Name = "Người đứng ngoài cửa kính",
                Pos = new Vector2(5f + (float)rng.NextDouble() * 5.6f, 8.35f),
            };
            guests.Add(starer);
            starerTime = 55f;
            StarerLookAcc = 0;
        }

        void StartChase()
        {
            var sorted = CeilingLights
                .OrderByDescending(l => Vector2.Distance(PlayerPos, l))
                .ToArray();
            ChaseSafeLight = sorted[Mathf.Min(sorted.Length - 1, Mathf.FloorToInt(sorted.Length * (0.35f + (float)rng.NextDouble() * 0.3f)))];
            hasChaseLight = true;
            Run.Progress.Flags["chase"] = 1;
            chaseRemaining = 0.6f + sorted.Length * 0.45f + 2.6f + 1.2f * Mathf.Min(3, Run.Progress.Mercy.GetValueOrDefault("r17"));
            ChaseDeadline = chaseRemaining;
        }

        void UpdateChase(float dt)
        {
            if (!hasChaseLight || !HasFlag("chase") || (chaseRemaining -= dt) >= 0) return;
            hasChaseLight = false;
            Run.Progress.Flags.Remove("chase");
            if (Vector2.Distance(PlayerPos, ChaseSafeLight) >= 1.3f)
            {
                Die(17, "Bóng tối đã tới chỗ bạn đứng. Bạn không đứng dưới bóng đèn cuối cùng.");
                return;
            }
            Director.Calm(30);
        }

        void StartRedHour()
        {
            Run.Progress.Flags["red"] = 1;
            redHour = new RedHour(rng.NextDouble);
        }

        void ScreamGlass()
        {
            if (Dead) return;
            Later(2.4, () =>
            {
                if (Dead) return;
                ShowDialog("Người phụ nữ ngoài kính", "Cho tôi trốn trong kho một lát thôi!",
                    new SimDialogOption("Mở cửa, đưa chị vào kho trốn.",
                        () => Die(20, "Bạn đã đưa người lạ vào kho."), ok: false, trap: "scream"),
                    new SimDialogOption("Chị đứng dưới đèn trước cửa, em gọi cảnh sát ngay.",
                        () => Director.Calm(25), ok: true, trap: "scream"));
            });
        }

        void LateDelivery()
        {
            if (Dead) return;
            Later(2.2, () =>
            {
                if (Dead) return;
                ShowDialog("Giọng ở cửa sau", "Mở cửa giúp tôi, nhận nốt thùng thiếu rồi tôi đi.",
                    new SimDialogOption("Ra mở cửa sau nhận thùng.",
                        () => Die(21, "Hàng chỉ giao lúc 03:00. Thứ đứng ngoài cửa sau không mang theo thùng nào cả."), ok: false, trap: "late"),
                    new SimDialogOption("Hàng chỉ giao lúc 3 giờ. Tôi không mở.",
                        () => Director.Calm(25), ok: true, trap: "late"));
            });
        }

        void SpawnVisit(string kind)
        {
            if (kind == "ghost")
            {
                GreetGhost = true;
                ++GreetSerial;
                greetingFor = null;
                greeting.Open(ghost: true);
                return;
            }
            if (kind == "phoneHQ" || kind == "selfCall")
            {
                RingPhone(kind == "phoneHQ" ? "hq" : "self");
                return;
            }
            if (kind == "chase")
            {
                StartChase();
                return;
            }
            if (kind == "drunk") Run.Progress.Flags["drunk"] = 1;
            if (kind == "raincoatChild") Run.Progress.Flags["rcChild"] = 1;

            var guest = CreateVisitor(kind);
            if (guest == null) return;
            guest.Routine = VisitRoutine(guest);
            guests.Add(guest);
        }

        void SpecialVisitor(string kind)
        {
            var guest = new SimGuest
            {
                Id = ++nextGuestId,
                Kind = kind,
                Name = kind switch
                {
                    "manager" => "Người phụ nữ mặc đồng phục quản lý",
                    "dblShift" => "Nhân viên HANA",
                    "mirae" => "Chị Mi-rae?",
                    _ => "Chị Mi-rae",
                },
                Aisle = -1,
                Speed = kind == "manager" ? 1f : kind == "dblShift" ? 0.95f : 1.15f,
                Visitor = false,
                Chime = false,
                Pos = new Vector2(3.1f, 11.5f + (float)rng.NextDouble()),
            };
            guest.Inside = guest.Pos.y < 8f;
            guest.Routine = kind == "realMirae" ? MorningVisitRoutine(guest) : VisitRoutine(guest);
            guests.Add(guest);
        }

        SimGuest CreateVisitor(string kind)
        {
            string ord = kind == "normal" ? OrdinaryKinds[rng.Next(OrdinaryKinds.Length)] : kind;
            bool heat = ord is "office" or "office2";
            bool station = ord == "student";
            int aisle = ord switch
            {
                "office" or "student" or "worker" or "raincoatOk" or "raincoatChild" => 0,
                "drunk" or "rider" or "gamer" or "loneChild" or "childParent" or "childMom" => 1,
                "nurse" or "couple" or "office2" or "raincoatBad" => 2,
                "noShadow" => rng.Next(3),
                _ => -1,
            };
            string[] items = ord switch
            {
                "office" => new[] { "kimbap", "banana" },
                "student" => new[] { "ramenCup", "can" },
                "drunk" => new[] { "soju", "soju", "chips" },
                "nurse" => new[] { "coffee", "kimbap" },
                "rider" => new[] { "can", "can" },
                "couple" => new[] { "banana", "banana", "chips" },
                "worker" => new[] { "kimbap", "kimbap", "coffee" },
                "office2" => new[] { "lunch", "coffee" },
                "gamer" => new[] { "chips", "can", "can" },
                "raincoatBad" => new[] { "kimbap", "soju" },
                "raincoatOk" => new[] { "ramenCup", "ramenCup" },
                "raincoatChild" => new[] { "ramenCup" },
                "loneChild" => new[] { "chips", "banana" },
                "childParent" => new[] { "banana", "chips" },
                "childMom" => new[] { "banana" },
                "noShadow" => new[] { "kimbap", "can" },
                _ => null,
            };
            float speed = ord switch
            {
                "drunk" => 0.85f,
                "raincoatBad" or "raincoatOk" => 0.9f,
                "raincoatChild" or "loneChild" or "childMom" => 0.75f,
                "childParent" => 0.85f,
                _ => 1.15f,
            };
            return new SimGuest
            {
                Id = ++nextGuestId,
                Kind = kind,
                Name = kind switch
                {
                    "raincoatOk" or "raincoatBad" => "Người áo mưa vàng",
                    "raincoatChild" => "Em bé mặc áo mưa vàng",
                    "loneChild" or "childMom" => "Em bé",
                    "childParent" => "Em bé đi cùng mẹ",
                    "noShadow" => "Khách",
                    "restroom" => "Khách vãng lai",
                    _ => ord,
                },
                Aisle = aisle,
                Items = items,
                Heat = heat,
                Station = station,
                Speed = speed,
                Visitor = true,
                Chime = true,
                Pos = new Vector2(3.1f, 11.5f + (float)rng.NextDouble()),
            };
        }

        void UpdateGuests(float dt)
        {
            for (int i = guests.Count - 1; i >= 0; --i)
            {
                var g = guests[i];
                if (!g.Alive)
                {
                    guests.RemoveAt(i);
                    continue;
                }
                if (g.Routine != null)
                {
                    StepGuest(g, dt);
                    if (!g.Routine.MoveNext()) g.Routine = null;
                }
            }
        }

        void StepGuest(SimGuest g, float dt)
        {
            if (g.WaitRemaining > 0)
            {
                g.WaitRemaining = Math.Max(0, g.WaitRemaining - dt);
                return;
            }
            if (!g.Walking || g.Waypoints == null) return;
            float budget = g.Speed * dt;
            while (budget > 1e-5f && g.WaypointIndex < g.Waypoints.Count)
            {
                var target = g.Waypoints[g.WaypointIndex];
                var diff = target - g.Pos;
                float dist = diff.magnitude;
                if (dist <= budget)
                {
                    g.Pos = target;
                    budget -= dist;
                    ++g.WaypointIndex;
                }
                else
                {
                    g.Pos += diff * (budget / dist);
                    budget = 0;
                }
            }
            if (g.WaypointIndex >= g.Waypoints.Count) g.Walking = false;
        }

        IEnumerable<bool> Walk(SimGuest g, params Vector2[] points)
        {
            foreach (var target in points)
            {
                if (!g.Alive) yield break;
                if (navigation != null && navigation.TryPath(g.Pos, target, out var path) && path.Count > 0)
                {
                    g.Waypoints = path;
                }
                else
                {
                    g.Waypoints = new[] { target };
                }
                g.WaypointIndex = 0;
                g.Walking = true;
                while (g.Alive && g.Walking) yield return true;
            }
        }

        IEnumerable<bool> WaitSeconds(SimGuest g, double seconds)
        {
            g.WaitRemaining = seconds;
            while (g.Alive && g.WaitRemaining > 0) yield return true;
        }

        IEnumerator<bool> VisitRoutine(SimGuest guest)
        {
            foreach (bool _ in Walk(guest, DoorOut, DoorIn)) yield return _;
            if (guest.Aisle >= 0)
            {
                int a = guest.Aisle;
                foreach (bool _ in Walk(guest, AisleFront[a], AisleMiddle[a])) yield return _;
                foreach (bool _ in WaitSeconds(guest, 3 + rng.NextDouble() * 3)) yield return _;
                foreach (bool _ in Walk(guest, AisleFront[a])) yield return _;
            }
            foreach (bool _ in ToCounterRoutine(guest)) yield return _;
            if (guest.IntoStorage)
            {
                foreach (bool _ in Walk(guest, new Vector2(0.8f, 0.6f), new Vector2(0.8f, -1.2f), new Vector2(2.6f, -3.2f))) yield return _;
                RemoveGuest(guest);
                Later(14, () => AddTask("storage"));
                yield break;
            }
            if (guest.Station)
            {
                foreach (bool _ in Walk(guest, AisleFront[1], new Vector2(10.7f, 6.1f))) yield return _;
                foreach (bool _ in WaitSeconds(guest, 7)) yield return _;
                foreach (bool _ in Walk(guest, new Vector2(8f, 7f))) yield return _;
                foreach (bool _ in WaitSeconds(guest, 22)) yield return _;
            }
            foreach (bool _ in Walk(guest, DoorIn, DoorOut, new Vector2(3.1f + (rng.NextDouble() < 0.5 ? -6f : 7f), 10.2f))) yield return _;
            RemoveGuest(guest);
        }

        IEnumerator<bool> MorningVisitRoutine(SimGuest guest)
        {
            foreach (bool _ in Walk(guest, new Vector2(3.1f, 9.6f))) yield return _;
            guest.Speed = 1.6f;
            while (Run.Clock.Minutes < 359.7 && !Dead && guest.Alive) yield return true;
            if (Dead || !guest.Alive) yield break;
            foreach (bool _ in Walk(guest, DoorOut, DoorIn)) yield return _;
            foreach (bool _ in ToCounterRoutine(guest)) yield return _;
        }

        IEnumerator<bool> BlackoutGuestRoutine(SimGuest guest)
        {
            foreach (bool _ in Walk(guest, DoorOut, DoorIn, new Vector2(4.4f, 6.2f))) yield return _;
            while (!guest.Greeted && HasFlag("blackout")) yield return true;
            foreach (bool _ in WaitSeconds(guest, 2)) yield return _;
            foreach (bool _ in Walk(guest, DoorIn, DoorOut, new Vector2(-3f, 10f))) yield return _;
            RemoveGuest(guest);
        }

        IEnumerable<bool> ToCounterRoutine(SimGuest guest)
        {
            foreach (bool _ in Walk(guest, AisleFront[0])) yield return _;
            int waitingAt = -1;
            while (guest.Alive && !queue.TryEnter(guest.Id))
            {
                int spot = queue.IndexOf(guest.Id);
                if (spot != waitingAt)
                {
                    waitingAt = spot;
                    foreach (bool _ in Walk(guest, new Vector2(3f + spot * 0.75f, 5.7f + spot * 0.35f))) yield return _;
                }
                yield return true;
            }
            if (!guest.Alive) yield break;
            foreach (bool _ in Walk(guest, Register)) yield return _;
            guest.Waiting = true;
            counterCustomer = guest;
            float patience = 80f;
            bool mayLeave = guest.Kind is "normal" or "drunk";
            while (guest.Alive && !guest.Paid)
            {
                if (!Dead && mayLeave && !Scanning && !DialogOpen)
                {
                    patience -= 0.1f;
                    if (patience < 0) break;
                }
                yield return true;
            }
            LeaveCounter(guest);
        }

        void LeaveCounter(SimGuest guest)
        {
            guest.Waiting = false;
            if (counterCustomer == guest)
            {
                counterCustomer = null;
                Scanning = false;
                scanGuest = null;
            }
            queue.Remove(guest.Id);
        }

        void RemoveGuest(SimGuest guest)
        {
            if (guest == null) return;
            guest.Alive = false;
            LeaveCounter(guest);
            guests.Remove(guest);
        }

        void ClearGuests()
        {
            for (int i = guests.Count - 1; i >= 0; --i) RemoveGuest(guests[i]);
            queue.Clear();
            greeting.Reset();
            greetingFor = null;
            counterCustomer = null;
        }

        void UpdateFrontDoor(float dt, bool greet)
        {
            for (int i = 0; i < guests.Count; ++i)
            {
                var g = guests[i];
                if (!g.Alive || !g.Chime) continue;
                bool inside = g.Pos.y < 8f;
                if (inside == g.Inside) continue;
                g.Inside = inside;
                if (!inside) continue;
                greetingFor = g;
                GreetGhost = false;
                ++GreetSerial;
                greeting.Open(blackout: g.BlackEntry);
            }
            switch (greeting.Tick(dt, greet, greetingFor != null && greetingFor.Alive))
            {
                case GreetingResult.Welcome:
                    if (greetingFor != null) greetingFor.Greeted = true;
                    break;
                case GreetingResult.GhostStrike:
                    Later(0.9, () => Strike(2, "Bạn đã chào khi không có ai bước vào."));
                    break;
                case GreetingResult.Complaint:
                    Run.Progress.Flags["complaints"] = Run.Progress.Flags.GetValueOrDefault("complaints") + 1;
                    break;
                case GreetingResult.BlackoutStrike:
                    Strike(2, "Có người bước vào mà bạn không chào. Điều 2 có số nhỏ hơn điều 10.");
                    break;
            }
            if (greeting.Remaining <= 0) greetingFor = null;
        }

        public void ServeAtCounter()
        {
            if (!HasCounterCustomer || Scanning || DialogOpen || Dead || Ended) return;
            var guest = counterCustomer;
            if (TryStoryCheckout(guest)) return;
            if (guest.Kind == "restroom")
            {
                ShowDialog(guest.Name, "Cho tôi mượn nhà vệ sinh trong kho một chút?",
                    SaleChoice(guest, "Vâng, đi qua rèm nhựa sau quầy ạ.", CheckoutChoice.AllowStorage, ok: false),
                    SaleChoice(guest, "Xin lỗi, cửa hàng không có nhà vệ sinh cho khách.", CheckoutChoice.DenyStorage, ok: true));
                return;
            }
            if (guest.Heat && !guest.Heated)
            {
                if (Carry == "hot")
                {
                    guest.Heated = true;
                    Carry = null;
                }
                else if (!guest.HeatAsked)
                {
                    guest.HeatAsked = true;
                    ShowDialog(guest.Name, "Hâm nóng giúp tôi được không?",
                        new SimDialogOption("Vâng, quý khách đợi chút ạ.", () =>
                        {
                            CarryFoodType = guest.Items[0];
                            Carry = "heat";
                            AddTask("heat");
                        }));
                    return;
                }
                else
                {
                    return;
                }
            }
            Scanning = true;
            scanGuest = guest;
            scanRemaining = (guest.Items?.Length ?? 0) * 0.5;
        }

        void UpdateScanning(double dt)
        {
            if (!Scanning) return;
            scanRemaining -= dt;
            if (scanRemaining > 0) return;
            var guest = scanGuest;
            Scanning = false;
            scanGuest = null;
            if (guest == null || guest != counterCustomer || Dead) return;
            ShowPayment(guest);
        }

        bool TryStoryCheckout(SimGuest guest)
        {
            switch (guest.Kind)
            {
                case "manager":
                    ShowDialog(guest.Name, "Mở kho cho chị kiểm tra ngay.",
                        new SimDialogOption("Vâng, mời chị vào kho.",
                            () => Die(9, "Bạn đã cho người mặc đồng phục quản lý vào kho."), ok: false, trap: "manager"),
                        new SimDialogOption("Xin lỗi, nội quy không cho ai vào kho.",
                            () => Later(3.2, () => { guest.Paid = true; Director.Calm(35); }), ok: true, trap: "manager"));
                    return true;
                case "dblShift":
                    ShowDialog(guest.Name, "Cậu về trước đi, để tớ trông nốt cho.",
                        new SimDialogOption("Ừ, cảm ơn. Tớ về nhé.",
                            () => Die(19, "Bạn đã rời ca cho một thứ mặc đồng phục giống hệt bạn."), ok: false, trap: "dbl"),
                        new SimDialogOption("Ca của tớ tới 6 giờ. Tớ không giao ca cho ai.",
                            () => Later(2.8, () => { guest.Paid = true; Director.Calm(30); }), ok: true, trap: "dbl"));
                    return true;
                case "mirae":
                    ShowDialog(guest.Name, "Đưa chìa khóa đây, về nghỉ đi em.",
                        new SimDialogOption("Cảm ơn chị. Em giao ca nhé.",
                            () => Die(13, "Bạn đã giao ca cho \"chị Mi-rae\" trước 06:00."), ok: false, trap: "mirae"),
                        new SimDialogOption("Ca của em tới 6 giờ ạ.",
                            () => Later(3.5, () => { guest.Paid = true; }), ok: true, trap: "mirae"));
                    return true;
                case "realMirae":
                    if (NightRules.CanHandOver(Run.Clock.Minutes)) EndShift(guest);
                    return true;
                default:
                    return false;
            }
        }

        void ShowPayment(SimGuest guest)
        {
            var charge = SaleChoice(guest, "Tính tiền", CheckoutChoice.Charge,
                ok: guest.Kind is not ("raincoatBad" or "noShadow" or "childMom" or "loneChild"));
            var broken = SaleChoice(guest, "Máy hỏng", CheckoutChoice.RegisterBroken,
                ok: guest.Kind is "raincoatBad" or "noShadow");
            var sendHome = SaleChoice(guest, "Về nhà đi", CheckoutChoice.SendHome,
                ok: guest.Kind is "loneChild" or "childMom");

            switch (guest.Kind)
            {
                case "raincoatBad":
                    ShowDialog(guest.Name, "Áo mưa vàng đồ tươi", charge, broken);
                    break;
                case "raincoatOk":
                    ShowDialog(guest.Name, "Áo mưa vàng mì ly", SaleChoice(guest, "Tính tiền", CheckoutChoice.Charge, ok: true));
                    break;
                case "raincoatChild":
                    ShowDialog(guest.Name, "Em bé áo mưa vàng mì ly",
                        SaleChoice(guest, "Tính tiền", CheckoutChoice.Charge, ok: true, trap: "raincoat"),
                        SaleChoice(guest, "Về nhà đi", CheckoutChoice.SendHome, ok: false, trap: "raincoat"),
                        SaleChoice(guest, "Máy hỏng", CheckoutChoice.RegisterBroken, ok: false, trap: "raincoat"));
                    break;
                case "noShadow":
                    ShowDialog(guest.Name, "Không bóng",
                        SaleChoice(guest, "Tính tiền", CheckoutChoice.Charge, ok: false, trap: "noShadow"),
                        SaleChoice(guest, "Máy hỏng", CheckoutChoice.RegisterBroken, ok: true, trap: "noShadow"));
                    break;
                case "childMom":
                    ShowDialog(guest.Name, "Mẹ em đứng ngoài kia kìa.",
                        SaleChoice(guest, "Tính tiền", CheckoutChoice.Charge, ok: false, trap: "childMom"),
                        SaleChoice(guest, "Về nhà đi", CheckoutChoice.SendHome, ok: true, trap: "childMom"));
                    break;
                case "childParent":
                    ShowDialog(guest.Name, "Em bé đi cùng mẹ",
                        SaleChoice(guest, "Tính tiền", CheckoutChoice.Charge, ok: true),
                        SaleChoice(guest, "Về nhà đi", CheckoutChoice.SendHome, ok: false));
                    break;
                case "loneChild":
                    ShowDialog(guest.Name, "Em bé đi một mình", charge, sendHome);
                    break;
                default:
                    ShowDialog(guest.Name, "Cảm ơn.", SaleChoice(guest, "Tính tiền", CheckoutChoice.Charge, ok: true));
                    break;
            }
        }

        SimDialogOption SaleChoice(SimGuest guest, string label, CheckoutChoice choice, bool ok, string trap = null)
            => new(label, () => ApplyCheckout(guest, choice), ok, trap);

        void ApplyCheckout(SimGuest guest, CheckoutChoice choice)
        {
            if (guest != counterCustomer || !guest.Alive) return;
            var (effect, rule) = CheckoutRules.Decide(guest.Kind, choice);
            if (effect == CheckoutEffect.Die)
            {
                Die(rule, "Vi phạm quy tắc tại quầy.");
                return;
            }
            guest.Paid = true;
            if (effect == CheckoutEffect.Sale)
            {
                if (guest.Heated) DoneTask("heat");
                return;
            }
            if (effect == CheckoutEffect.Strike)
            {
                if (guest.Kind == "restroom")
                {
                    guest.IntoStorage = true;
                    Run.Progress.Flags["strangerIn"] = 1;
                    Strike(9, "Bạn đã cho người khác vào kho.");
                }
                else if (guest.Kind == "loneChild")
                {
                    Strike(4, "Bạn đã bán đồ cho trẻ em đi một mình sau 01:00.");
                }
                else
                {
                    Strike(3, "Khách áo mưa vàng mua mì ly PHẢI được phục vụ.");
                }
            }
            else if (effect == CheckoutEffect.Complain)
            {
                Run.Progress.Flags["complaints"] = Run.Progress.Flags.GetValueOrDefault("complaints") + 1;
            }
        }

        void EndShift(SimGuest guest)
        {
            guest.Paid = true;
            Ended = true;
            Run.Clock.Running = false;
            bool rescued = HasFlag("jiho");
            ShowDialog("Chị Mi-rae", "Bàn giao ca",
                new SimDialogOption(rescued ? "Đưa Ji-ho ra ngoài" : "Bàn giao ca", () => Ending = rescued ? "B" : "A"));
        }

        void ShowDialog(string speaker, string text, params SimDialogOption[] options)
        {
            CurrentDialog = new SimDialog(speaker, text, options);
        }

        public void Strike(int rule, string reason)
        {
            int strikes = Run.Progress.Strike();
            StrikeLogged?.Invoke((int)Math.Round(Run.Clock.Minutes), reason);
            if (strikes >= 3) Later(1.8, () => Die(0, "Bạn đã vi phạm nội quy ba lần. Cửa hàng không giữ nhân viên như bạn."));
        }

        public void Die(int rule, string reason)
        {
            if (Dead || Ended) return;
            Run.Progress.RecordDeath(rule);
            LastDeathRule = rule;
            LastDeathReason = reason;
            Run.Clock.Running = false;
            Dead = true;
            CurrentDialog = null;
            timers.Clear();
        }
    }
}
