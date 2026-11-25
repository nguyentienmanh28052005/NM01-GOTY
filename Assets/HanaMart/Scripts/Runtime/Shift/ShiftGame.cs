using System;
using System.Collections;
using System.Globalization;
using HanaMart.Actors;
using HanaMart.Audio;
using HanaMart.Interaction;
using HanaMart.Player;
using HanaMart.Store;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HanaMart.Shift
{
    [DefaultExecutionOrder(10)]
    [RequireComponent(typeof(PlayerController), typeof(Interactor), typeof(CarryController))]
    public sealed partial class ShiftGame : MonoBehaviour
    {
        public sealed class Choice
        {
            public string Label { get; }
            internal Action Choose { get; }
            public Choice(string label, Action choose) { Label = label; Choose = choose; }
        }

        [SerializeField] ActorCatalog actorCatalog;
        [SerializeField] AudioBank audioBank;
        [SerializeField] ProductCatalog products;
        public ShiftRun Run { get; private set; }
        public bool StartIdle { get; set; }
        public bool Idle { get; private set; }
        public bool Paused { get; set; }
        public bool Dead { get; private set; }
        public bool Ended { get; private set; }
        public bool InputBlocked => Dead || Ended || Dialogue != null || PaperTitle != null;
        public bool ClosedOverlayThisFrame => closedOverlayFrame == Time.frameCount;
        public bool ExternalDialogInput { get; set; }
        public bool ExternalTasksPanel { get; set; }
        public bool PaperIsRules { get; private set; }
        public bool PaperIsHandwritten { get; private set; }
        public int LastDeathRule { get; private set; } = -1;
        public string Subtitle => subtitleUntil > Time.time ? subtitle : null;
        public string Speaker { get; private set; }
        public string Dialogue { get; private set; }
        public Choice[] Options { get; private set; } = Array.Empty<Choice>();
        public float GreetingTime => greeting.Remaining;
        public bool CanServe => counterCustomer != null && counterCustomer.Waiting && !scanning;
        public int TotalSold { get; private set; }
        public string PosDisplay { get; private set; }
        public event Action<int, string> Died;
        public event Action<ShiftEvent> StoryEvent;
        public event Action<string> RingRequested;
        public event Action<string> SceneAction;
        public event Action ShiftStarted;
        public event Action<string> EndingReached;
        public event Action<string> ToastRequested;
        public event Action<string, string> BannerRequested;
        public event Action<string, string> WarnRequested;
        public event Action TasksPanelToggled;
        public event Action<int, string> DeathRecapShown;

        PlayerController player;
        Interactor interactor;
        CarryController carry;
        StoreDoors doors;
        AudioSource audioSource;
        InputAction greetInput, confirmInput;
        readonly InputAction[] optionInput = new InputAction[3];
        System.Random random;
        VisitDirector director;
        float subtitleUntil;
        string subtitle;
        int closedOverlayFrame = -1;

        void Start()
        {
            player = GetComponent<PlayerController>();
            interactor = GetComponent<Interactor>();
            carry = GetComponent<CarryController>();
            doors = FindFirstObjectByType<StoreDoors>();
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            if (player.Input)
            {
                greetInput = player.Input.FindAction("Greet", true);
                confirmInput = player.Input.FindAction("Confirm", true);
                for (int i = 0; i < optionInput.Length; ++i) optionInput[i] = player.Input.FindAction("Option" + (i + 1), true);
            }
            interactor.Pressed += Pressed;
            random = new System.Random();
            Run = new ShiftRun(random.NextDouble);
            director = new VisitDirector(random.NextDouble);
            Run.EventReached += OnStoryEvent;
            Run.CheckpointSaved += (hour, reduced) =>
            {
                string msg = reduced ? "Một giờ không vi phạm — bớt 1 cảnh cáo" : $"Đã lưu · {ShiftClock.Format(hour * 60)}";
                Say(msg, 3);
                ToastRequested?.Invoke(msg);
            };
            InitializeJobs();
            InitializeCinematics();
            if (StartIdle)
            {
                Idle = true;
                SetGameplay(false);
            }
            else
            {
                NewShift();
            }
        }

        public void NewShift() => ResetShift(false);
        public void Retry() => ResetShift(true);

        public void ReturnToMenu()
        {
            StopAllCoroutines();
            StopDeath();
            ResetCinematics();
            ClearGuests();
            ClosePaper();
            CloseDialog();
            ResetJobs(false);
            Power(true, true);
            TintStore(0xffffff, 1);
            Dead = Ended = false;
            LastDeathRule = -1;
            Ending = null;
            Idle = true;
            Paused = false;
            subtitle = null;
            subtitleUntil = 0;
            SetGameplay(false);
        }

        void ResetShift(bool retry)
        {
            StopAllCoroutines();
            StopDeath();
            ResetCinematics();
            ClearGuests();
            if (retry) Run.Retry();
            else { Run.NewShift(); TotalSold = 0; }
            director.Reset(Run.Progress.SavedHour > 0);
            Idle = false;
            Paused = false;
            Dead = Ended = false;
            LastDeathRule = -1;
            ResetJobs(retry);
            CloseDialog();
            player.Motor.pos = new Vector2(.85f, 4.4f);
            player.Motor.vel = Vector2.zero;
            player.Motor.SetYawFacing(1, 0);
            player.Motor.pitch = player.Motor.crouch = 0;
            if (doors)
            {
                doors.Front.motor.target = doors.Back.motor.target = 0;
                doors.Freezer.motor.target = 0;
            }
            string startBanner = retry && Run.Progress.SavedHour > 0 ? "Tiếp tục ca đêm" : "Ca đêm bắt đầu · HANA MART 24";
            Say(startBanner, 4);
            BannerRequested?.Invoke(Run.Clock.Display, startBanner);
            ShiftStarted?.Invoke();
        }

        void Update()
        {
            if (Run == null || Idle || Paused) return;
            ReadDialogInput();
            ReadPaperInput();
            if (Dead || Ended) return;
            float dt = Time.deltaTime * Cinematics.Slowmo;
            Run.Tick(dt, dialogOpen: Dialogue != null, paperOpen: PaperTitle != null);
            double minute = Run.Clock.Minutes;
            var next = Run.Timeline.NextAnchor(minute, .5, 60);
            var kind = director.Tick(dt, minute, LiveVisitors(), next?.Minute,
                HasFlag("red") || HasFlag("blackout"), phone.Ringing, Run.Progress);
            if (kind != null) SpawnVisit(kind);
            UpdateJobs(dt);
            if (Dialogue == null && PaperTitle == null) UpdateApparitions(dt);
            UpdateFrontDoor(dt, greetInput?.WasPressedThisFrame() == true);
        }

        void OnStoryEvent(ShiftEvent entry)
        {
            if (entry.Id == "intro") director.StartIn(14);
            if (entry.Id == "calm2") director.Calm(35);
            StoryEvent?.Invoke(entry);
        }

        void Pressed(string id)
        {
            if (Dialogue != null || PaperTitle != null || Dead || Ended) return;
            if (id == "pos") ServeAtCounter();
            else UseJob(id);
        }

        void ReadDialogInput()
        {
            if (ExternalDialogInput || Dialogue == null) return;
            for (int i = 0; i < optionInput.Length; ++i)
                if (optionInput[i]?.WasPressedThisFrame() == true) { SelectOption(i); return; }
            if (Options.Length == 1 && confirmInput?.WasPressedThisFrame() == true) SelectOption(0);
        }

        public void Say(string text, float seconds = 4)
        {
            subtitle = text;
            subtitleUntil = Time.time + seconds;
        }

        public void ShowDialog(string speaker, string message, params Choice[] choices)
        {
            Speaker = speaker;
            Dialogue = message;
            Options = choices;
            SetGameplay(false);
        }

        public void SelectOption(int index)
        {
            if (index < 0 || index >= Options.Length) return;
            var selected = Options[index];
            CloseDialog();
            selected.Choose?.Invoke();
        }

        public void CloseDialog()
        {
            if (Dialogue != null) closedOverlayFrame = Time.frameCount;
            Speaker = Dialogue = null;
            Options = Array.Empty<Choice>();
            SetGameplay(!Dead && !Ended && PaperTitle == null);
        }

        void SetGameplay(bool enabled)
        {
            bool active = enabled && !Idle && !Paused;
            player.GameplayEnabled = active;
            player.Motor.frozen = !active;
            interactor.Enabled = active;
        }

        public bool HasFlag(string key) => Run.Progress.Flags.TryGetValue(key, out int value) && value != 0;

        int RandomIndex(int count) => random.Next(count);
        float RandomValue() => (float)random.NextDouble();

        public void PlayClip(string id)
        {
            var clip = audioBank ? audioBank.Find(id) : null;
            if (clip) audioSource.PlayOneShot(clip);
        }

        public void Strike(int rule, string reason)
        {
            int strikes = Run.Progress.Strike();
            Say($"CẢNH CÁO {strikes}/3 · Quy tắc số {rule}\n{reason}", 5);
            WarnRequested?.Invoke($"CẢNH CÁO {strikes}/3 · QUY TẮC SỐ {rule}", reason);
            PlayClip("sting");
            player.Rumble(.6f, .4f, .3f);
            if (strikes >= 3) Later(1.8f, () => Die(0, "Bạn đã vi phạm nội quy ba lần. Cửa hàng không giữ nhân viên như bạn."));
        }

        public void Die(int rule, string reason)
        {
            if (Dead || Ended) return;
            Run.Progress.RecordDeath(rule);
            LastDeathRule = rule;
            Run.Clock.Running = false;
            Dead = true;
            ClosePaper();
            CloseDialog();
            player.Rumble(1, 1, .9f);
            BeginDeath(rule, reason);
            Died?.Invoke(rule, reason);
        }

        void Later(float seconds, Action action) => StartCoroutine(Delayed(seconds, action));

        static IEnumerator Delayed(float seconds, Action action)
        {
            yield return new WaitForSeconds(seconds);
            action();
        }

        static string PriceText(int amount) => "₩" + amount.ToString("N0", CultureInfo.GetCultureInfo("en-US"));

        void OnDestroy()
        {
            if (interactor)
            {
                interactor.Pressed -= Pressed;
                interactor.Holding -= Hold;
            }
            StopDeath();
            ReleaseCinematics();
            ClearGuests();
            ReleaseCheckoutMaterials();
            ReleaseEnvironment();
        }
    }
}
