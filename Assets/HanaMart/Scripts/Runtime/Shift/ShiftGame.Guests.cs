using System.Collections;
using System.Collections.Generic;
using HanaMart.Actors;
using HanaMart.Store;
using UnityEngine;

namespace HanaMart.Shift
{
    public sealed partial class ShiftGame
    {
        sealed class Profile
        {
            public readonly string Name, Line;
            public readonly ActorOptions Look;
            public readonly int Aisle;
            public readonly float Speed;
            public readonly bool Heat, Station;
            public readonly string[] Items;

            public Profile(string name, ActorOptions look, int aisle, string line, string[] items,
                float speed = 1.15f, bool heat = false, bool station = false)
            {
                Name = name; Look = look; Aisle = aisle; Line = line; Items = items;
                Speed = speed; Heat = heat; Station = station;
            }
        }

        sealed class Guest
        {
            public readonly List<GameObject> Items = new();
            public Actor Actor;
            public Profile Profile;
            public string Kind;
            public int Id;
            public bool Visitor, Chime, Inside, Greeted, BlackEntry, Waiting, Paid, IntoStorage, HeatAsked, Heated;
            public Guest Companion;
        }

        static readonly string[] OrdinaryKinds = { "office", "student", "nurse", "rider", "couple", "worker", "office2", "gamer" };
        static readonly Dictionary<string, Profile> Ordinary = new()
        {
            ["office"] = new("Nhân viên văn phòng", new ActorOptions { color = 0x2b2d42, hair = 0x1a1414 },
                0, "Hâm nóng cơm cuộn giúp tôi được không?", new[] { "kimbap", "banana" }, heat: true),
            ["student"] = new("Sinh viên", new ActorOptions { coat = "hoodie", color = 0x6c757d, hair = 0x2a1c14 },
                0, "Ôn thi xuyên đêm... cho em mì ly với.", new[] { "ramenCup", "can" }, station: true),
            ["drunk"] = new("Chú say rượu", new ActorOptions { coat = "suit", color = 0x343a40, hair = 0x101010, skin = 0xe8a98a },
                1, "Này... thằng nhóc ca đêm trước đâu rồi? Ji-ho ấy. Tuần trước còn thấy mà...", new[] { "soju", "soju", "chips" }, .85f),
            ["nurse"] = new("Y tá vừa tan ca", new ActorOptions { color = 0xe9ecef, longHair = true, hair = 0x1a1210 },
                2, "Đêm nay đường vắng quá... cẩn thận nhé.", new[] { "coffee", "kimbap" }),
            ["rider"] = new("Shipper", new ActorOptions { color = 0x1d3557, cap = true },
                1, "Còn đơn cuối rồi về. Nghe nói chỗ này ca đêm hay có chuyện lạ hả?", new[] { "can", "can" }),
            ["couple"] = new("Khách nữ", new ActorOptions { color = 0xffadad, longHair = true, hair = 0x3a2418 },
                2, "Bánh mandu là chân ái. Mà sao bạn cứ nhìn ra cửa kính thế?", new[] { "banana", "banana", "chips" }),
            ["worker"] = new("Công nhân ca sớm", new ActorOptions { color = 0x3a5a40, cap = true },
                0, "Sắp sáng rồi. Ca đêm chịu khó ghê.", new[] { "kimbap", "kimbap", "coffee" }),
            ["office2"] = new("Nhân viên tăng ca", new ActorOptions { coat = "suit", color = 0x22333b, hair = 0x111111 },
                2, "Hâm giúp tôi hộp cơm này với.", new[] { "lunch", "coffee" }, heat: true),
            ["gamer"] = new("Khách đeo tai nghe", new ActorOptions { coat = "hoodie", color = 0x2d6a4f, hair = 0x101010 },
                1, "PC bang bên kia hết mì rồi.", new[] { "chips", "can", "can" }),
        };
        static readonly Vector2 DoorIn = new(3.1f, 7);
        static readonly Vector2 DoorOut = new(3.1f, 9.2f);
        static readonly Vector2 Register = new(2.45f, 4.4f);
        static readonly Vector2 Cashier = new(1.5f, 4.4f);
        static readonly Vector2[] AisleFront = { new(5.8f, 6.7f), new(8.4f, 6.7f), new(10.6f, 6.7f) };
        static readonly Vector2[] AisleMiddle = { new(5.8f, 3.8f), new(8.4f, 3.8f), new(10.6f, 3.8f) };

        readonly CheckoutQueue queue = new();
        readonly GreetingWindow greeting = new();
        readonly List<Guest> guests = new();
        Guest counterCustomer, greetingFor;
        float ghostDoorTime;
        int nextGuest;
        bool scanning;

        public int LiveGuests => guests.Count;

        int LiveVisitors()
        {
            int count = 0;
            foreach (var guest in guests)
                if (guest.Visitor && guest.Actor.alive) ++count;
            return count;
        }

        public void SpawnVisit(string kind)
        {
            if (kind == "ghost") { GhostChime(); return; }
            if (kind == "phoneHQ" || kind == "selfCall") { RingRequested?.Invoke(kind == "phoneHQ" ? "hq" : "self"); return; }
            if (kind == "chase") { SceneAction?.Invoke("chase"); return; }
            if (kind == "drunk") Run.Progress.Flags["drunk"] = 1;
            if (kind == "raincoatChild") Run.Progress.Flags["rcChild"] = 1;

            var profile = kind switch
            {
                "normal" => Ordinary[OrdinaryKinds[RandomIndex(OrdinaryKinds.Length)]],
                "drunk" => Ordinary["drunk"],
                "raincoatOk" or "raincoatBad" => Raincoat(kind == "raincoatBad"),
                "raincoatChild" => new Profile("Em bé mặc áo mưa vàng", new ActorOptions
                    { coat = "child", color = 0xf2c200, hood = true, rainShell = true, face = "wide", pale = true, uncanny = .4f },
                    0, null, new[] { "ramenCup" }, .75f),
                "loneChild" => Child("Em bé", 0xff6b6b, new[] { "chips", "banana" }, .8f, .3f),
                "childParent" => Child("Em bé đi cùng mẹ", 0x48cae4, new[] { "banana", "chips" }, .85f),
                "childMom" => Child("Em bé", 0xffb703, new[] { "banana" }, .8f),
                "noShadow" => NoShadow(),
                "restroom" => Restroom(),
                _ => null,
            };
            if (profile == null) return;
            var visitor = SpawnGuest(kind, profile);
            if (kind == "childParent")
            {
                visitor.Actor.pos += new Vector3(.5f, 0, .6f);
                var parent = SpawnGuest("parent", new Profile("Người mẹ", new ActorOptions
                    { color = 0x6d597a, mask = "white", longHair = true, hair = 0x2b1d14 }, -1, null, null));
                parent.Companion = visitor;
                StartCoroutine(ParentVisit(parent));
            }
            else if (kind == "childMom")
            {
                var outside = new Profile("Người mẹ", new ActorOptions
                    { color = 0x1a1a1a, longHair = true, hair = 0x050505, face = "none", pale = true, uncanny = 1, tall = 1.12f },
                    -1, null, null);
                visitor.Companion = SpawnGuest("outsideMother", outside, new Vector3(5.2f, 0, 8.45f), false);
            }
            StartCoroutine(Visit(visitor));
        }

        static Profile Raincoat(bool bad) => new("Người áo mưa vàng", new ActorOptions
        {
            coat = "raincoat", color = 0xf2c200, hood = true, face = "none", pale = true, uncanny = bad ? .7f : .25f,
        }, bad ? 2 : 0, null, bad ? new[] { "kimbap", "soju" } : new[] { "ramenCup", "ramenCup" }, .9f);

        static Profile Child(string name, int color, string[] items, float speed, float uncanny = 0) =>
            new(name, new ActorOptions { coat = "child", color = color, hair = 0x101010, face = "wide", uncanny = uncanny }, 1, null, items, speed);

        Profile NoShadow()
        {
            var look = RandomIndex(2) == 0
                ? new ActorOptions { color = 0x2f3e46, mask = "white", hair = 0x101010 }
                : new ActorOptions { coat = "suit", color = 0x1b263b, hair = 0x0d0d0d, pale = true };
            look.noShadow = true;
            look.uncanny = .15f;
            return new("Khách", look, RandomIndex(3), null, new[] { "kimbap", "can" });
        }

        Profile Restroom()
        {
            var look = RandomIndex(3) switch
            {
                0 => new ActorOptions { color = 0x588157, mask = "white", hair = 0x1b1b1b },
                1 => new ActorOptions { coat = "suit", color = 0x3d405b, hair = 0x111111 },
                _ => new ActorOptions { coat = "hoodie", color = 0x9d0208, hood = true, mask = "black" },
            };
            return new("Khách vãng lai", look, -1, null, null);
        }

        Guest SpawnGuest(string kind, Profile profile, Vector3? position = null, bool visitor = true)
        {
            var actor = Actor.Spawn(actorCatalog, profile.Look, position ?? new Vector3(3.1f, 0, 11.5f + RandomValue()));
            actor.yaw = Mathf.PI;
            actor.speed = profile.Speed;
            actor.name = profile.Name;
            var guest = new Guest
            {
                Kind = kind, Profile = profile, Actor = actor, Id = ++nextGuest,
                Visitor = visitor, Chime = visitor, Inside = actor.pos.z < 8,
            };
            guests.Add(guest);
            return guest;
        }

        IEnumerator Visit(Guest guest)
        {
            yield return Walk(guest, DoorOut, DoorIn);
            if (guest.Profile.Aisle >= 0) yield return Browse(guest);
            yield return ToCounter(guest);
            if (guest.IntoStorage)
            {
                yield return Walk(guest, new Vector2(.8f, .6f), new Vector2(.8f, -1.2f), new Vector2(2.6f, -3.2f));
                RemoveGuest(guest);
                Later(14, () => SceneAction?.Invoke("steps"));
                yield break;
            }
            if (guest.Profile.Station)
            {
                yield return Walk(guest, AisleFront[1], new Vector2(10.7f, 6.1f));
                yield return Face(guest.Actor, new Vector2(12, 5.4f));
                SceneAction?.Invoke("stationSteam");
                yield return new WaitForSeconds(7);
                yield return Walk(guest, new Vector2(8, 7));
                yield return Face(guest.Actor, new Vector2(8, 9));
                SceneAction?.Invoke("barSteam");
                yield return new WaitForSeconds(22);
            }
            yield return Leave(guest);
            if (guest.Kind == "childMom" && guest.Companion != null) RemoveGuest(guest.Companion);
        }

        IEnumerator ParentVisit(Guest parent)
        {
            yield return Walk(parent, DoorOut, DoorIn, new Vector2(3.6f, 6.4f));
            yield return Face(parent.Actor, Cashier);
            while (parent.Companion.Actor.alive && !parent.Companion.Paid) yield return null;
            yield return Leave(parent);
        }

        IEnumerator Browse(Guest guest)
        {
            int aisle = guest.Profile.Aisle;
            yield return Walk(guest, AisleFront[aisle], AisleMiddle[aisle]);
            yield return Face(guest.Actor, new Vector2(guest.Actor.pos.x - 1, guest.Actor.pos.z));
            yield return new WaitForSeconds(3 + RandomValue() * 3);
            yield return Walk(guest, AisleFront[aisle]);
        }

        IEnumerator Leave(Guest guest)
        {
            yield return Walk(guest, DoorIn, DoorOut, new Vector2(3.1f + (RandomValue() < .5f ? -6 : 7), 10.2f));
            RemoveGuest(guest);
        }

        IEnumerator Walk(Guest guest, params Vector2[] points)
        {
            foreach (var target in points)
            {
                if (!guest.Actor.alive || !guest.Actor.TryMoveTo(player.Navigation, target)) yield break;
                for (int frame = 0; frame < 4000 && guest.Actor.alive && guest.Actor.OnRoute; ++frame) yield return null;
            }
        }

        static IEnumerator Face(Actor actor, Vector2 point)
        {
            float target = Mathf.Atan2(point.x - actor.pos.x, point.y - actor.pos.z);
            for (int frame = 0; frame < 30 && actor.alive; ++frame)
            {
                actor.yaw += Mathf.DeltaAngle(actor.yaw * Mathf.Rad2Deg, target * Mathf.Rad2Deg) * Mathf.Deg2Rad * .25f;
                yield return null;
            }
        }

        IEnumerator ToCounter(Guest guest)
        {
            yield return Walk(guest, AisleFront[0]);
            int waitingAt = -1;
            while (guest.Actor.alive && !queue.TryEnter(guest.Id))
            {
                int spot = queue.IndexOf(guest.Id);
                if (spot != waitingAt)
                {
                    waitingAt = spot;
                    yield return Walk(guest, new Vector2(3 + spot * .75f, 5.7f + spot * .35f));
                    yield return Face(guest.Actor, Cashier);
                }
                yield return null;
            }
            if (!guest.Actor.alive) yield break;
            yield return Walk(guest, Register);
            yield return Face(guest.Actor, Cashier);
            guest.Actor.lookTarget = ThreeCoords.ToUnity(transform.position);
            guest.Waiting = true;
            counterCustomer = guest;
            FillCounter(guest);
            if (guest.Profile.Items != null) PlayClip("ui_move");
            float patience = 80;
            bool mayLeave = guest.Kind == "normal" || guest.Kind == "drunk";
            while (guest.Actor.alive && !guest.Paid)
            {
                if (!Dead && mayLeave && !scanning && Dialogue == null && (patience -= Time.deltaTime) < 0)
                {
                    Speak(guest, "Thôi khỏi, chậm quá.");
                    break;
                }
                yield return null;
            }
            LeaveCounter(guest);
        }

        void LeaveCounter(Guest guest)
        {
            guest.Waiting = false;
            if (counterCustomer == guest) { counterCustomer = null; scanning = false; PosDisplay = null; }
            queue.Remove(guest.Id);
            guest.Actor.lookTarget = null;
            foreach (var item in guest.Items) if (item) Destroy(item);
            guest.Items.Clear();
        }

        void RemoveGuest(Guest guest)
        {
            if (!guests.Remove(guest)) return;
            LeaveCounter(guest);
            guest.Actor.Remove();
        }

        void ClearGuests()
        {
            for (int i = guests.Count - 1; i >= 0; --i) RemoveGuest(guests[i]);
            queue.Clear();
            greeting.Reset();
            greetingFor = null;
            ghostDoorTime = 0;
        }

        void GhostChime()
        {
            PlayClip("chime");
            ghostDoorTime = 1.8f;
            ghostDoorFraction = .35f;
            greetingFor = null;
            greeting.Open(ghost: true);
        }

        void UpdateFrontDoor(float dt, bool greet)
        {
            bool near = false;
            foreach (var guest in guests)
            {
                var a = guest.Actor;
                if (!a.alive) continue;
                if (guest.Waiting || guest.Kind == "outsideMother") a.lookTarget = ThreeCoords.ToUnity(transform.position);
                near |= Mathf.Abs(a.pos.x - 3.1f) < 1.3f && Mathf.Abs(a.pos.z - 8) < 1.4f;
                bool inside = a.pos.z < 8;
                if (!guest.Chime || inside == guest.Inside) continue;
                guest.Inside = inside;
                PlayClip("chime");
                if (!inside) continue;
                greetingFor = guest;
                greeting.Open(blackout: guest.BlackEntry);
            }
            if (doors)
            {
                doors.Front.motor.target = ghostDoorTime > 0 ? ghostDoorFraction : near ? 1 : 0;
                ghostDoorTime = Mathf.Max(0, ghostDoorTime - dt);
            }
            switch (greeting.Tick(dt, greet, greetingFor != null && greetingFor.Actor.alive))
            {
                case GreetingResult.Welcome:
                    if (greetingFor != null) greetingFor.Greeted = true;
                    Say("Bạn: Xin chào quý khách!", 2);
                    PlayClip("ui_ok");
                    break;
                case GreetingResult.GhostStrike:
                    Say("Bạn: Xin chào quý khách!", 2);
                    PlayClip("ui_ok");
                    Later(.9f, () =>
                    {
                        Say("...(một giọng thì thầm sát tai) Cảm ơn đã mời tôi vào...", 4);
                        PlayClip("whisper");
                        Strike(2, "Bạn đã chào khi không có ai bước vào.");
                    });
                    break;
                case GreetingResult.Complaint:
                    Complain();
                    Speak(greetingFor, new[] { "...Không ai chào à?", "Nhân viên đâu rồi nhỉ.", "(Khách liếc nhìn bạn, không vui.)" }[RandomIndex(3)]);
                    break;
                case GreetingResult.BlackoutStrike:
                    Strike(2, "Có người bước vào mà bạn không chào. Điều 2 có số nhỏ hơn điều 10.");
                    break;
            }
            if (greeting.Remaining <= 0) greetingFor = null;
        }

        void Speak(Guest guest, string text) => Say($"{guest.Profile.Name}: {text}");

        void Complain()
        {
            var flags = Run.Progress.Flags;
            flags["complaints"] = flags.GetValueOrDefault("complaints") + 1;
        }
    }
}
