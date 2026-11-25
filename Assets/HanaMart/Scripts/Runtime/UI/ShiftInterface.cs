using System;
using System.Collections;
using System.Collections.Generic;
using HanaMart.Interaction;
using HanaMart.Player;
using HanaMart.Shift;
using HanaMart.Store;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace HanaMart.UI
{
    [DefaultExecutionOrder(20)]
    [RequireComponent(typeof(ShiftGame), typeof(PlayerController))]
    public sealed partial class ShiftInterface : MonoBehaviour
    {
        static readonly Color Teal = new(0f, 0.702f, 0.604f, 1f);
        static readonly Color Cyan = new(0.49f, 1f, 0.882f, 1f);
        static readonly Color Red = new(0.843f, 0.149f, 0.184f, 1f);
        static readonly Color Muted = new(0.624f, 0.663f, 0.651f, 1f);
        static readonly Color Bright = new(0.933f, 0.941f, 0.925f, 1f);
        static readonly Color FooterText = new(0.451f, 0.502f, 0.490f, 1f);
        static readonly Color PaperCream = new(0.953f, 0.937f, 0.894f, 0.985f);
        static readonly Color PaperInk = new(0.086f, 0.094f, 0.102f, 1f);

        public Font sourceFont;
        public string SavePathOverride { get; set; }
        public UiFlow Flow { get; } = new();
        public SaveData Save { get; private set; }
        public bool TaskPanelOpen { get; private set; }
        public int DialogFocusIndex => dialogFocus.Index;
        public string ActiveGuideTitle { get; private set; }
        public bool HudVisible { get; private set; }

        ShiftGame game;
        PlayerController player;
        Interactor interactor;
        CarryController carry;
        Camera view;

        InputAction navInput, confirmInput, backInput, pauseInput, useInput, tabPrevInput, tabNextInput, hideUiInput, debugInput;
        readonly InputAction[] optionInputs = new InputAction[3];

        GameObject root;
        TMP_FontAsset font, hangulFallback;
        readonly Dictionary<UiScreen, GameObject> screens = new();
        readonly Dictionary<UiScreen, List<MenuItemView>> menuItems = new();
        readonly List<TextMeshProUGUI> promptTexts = new();
        TextMeshProUGUI bootPressText, menuEndingsText;

        float attractTime;
        float savedTimeScale = 1f;
        bool wasPaused;
        int lastNavDir;
        float nextNavRepeatAt;
        Coroutine tutorialCoroutine;

        sealed class MenuItemView
        {
            public UiAction Action;
            public GameObject Go;
            public Image Bar;
            public TextMeshProUGUI Label;
        }

        sealed class PointerRelay : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
        {
            public Action Entered, Clicked;
            public void OnPointerEnter(PointerEventData eventData) => Entered?.Invoke();
            public void OnPointerClick(PointerEventData eventData) => Clicked?.Invoke();
        }

        void Awake()
        {
            game = GetComponent<ShiftGame>();
            player = GetComponent<PlayerController>();
            interactor = GetComponent<Interactor>();
            carry = GetComponent<CarryController>();
            view = GetComponent<Camera>();

            if (TryGetComponent<ShiftHud>(out var oldHud)) oldHud.enabled = false;
            if (TryGetComponent<InteractionPrompt>(out var oldPrompt)) oldPrompt.enabled = false;

            game.StartIdle = true;
            game.ExternalDialogInput = true;
            game.ExternalTasksPanel = true;
            player.AutoManageCursor = false;
            SnapshotGlobals();
            Save = SaveStore.Load(SavePathOverride);
        }

        void Start()
        {
            game.ShiftStarted += OnShiftStarted;
            game.EndingReached += OnEndingReached;
            game.ToastRequested += ShowToast;
            game.BannerRequested += ShowBanner;
            game.WarnRequested += ShowWarn;
            game.TasksPanelToggled += () => ToggleTaskPanel();
            game.DeathRecapShown += ShowDeathRecap;

            if (player.Input)
            {
                var uiMap = player.Input.FindActionMap("UI", false);
                if (uiMap != null)
                {
                    uiMap.Enable();
                    navInput = uiMap.FindAction("UiNavigate", false);
                    confirmInput = uiMap.FindAction("UiConfirm", false);
                    backInput = uiMap.FindAction("UiBack", false);
                    tabPrevInput = uiMap.FindAction("UiTabPrev", false);
                    tabNextInput = uiMap.FindAction("UiTabNext", false);
                }
                pauseInput = player.Input.FindAction("Pause", false);
                useInput = player.Input.FindAction("Use", false);
                hideUiInput = player.Input.FindAction("HideUI", false);
                debugInput = player.Input.FindAction("Debug", false);
                for (int i = 0; i < optionInputs.Length; i++)
                    optionInputs[i] = player.Input.FindAction("Option" + (i + 1), false);
            }

            BuildUi();
            BuildHud();
            BuildHackPanel();
            BuildSettingsScreen();
            ApplyAllSettings();
            SyncState();
        }

        public void ReloadSave(string path = null)
        {
            SavePathOverride = path;
            Save = SaveStore.Load(SavePathOverride);
            ApplyAllSettings();
            RenderSettingsTab();
            RefreshEndingsText();
        }

        public void TriggerBootAdvance()
        {
            if (Flow.AdvanceBoot())
            {
                game.PlayClip("ui_ok");
                SyncState();
            }
        }

        public void ToggleTaskPanel(bool? force = null)
        {
            if (game.Idle || game.Dead || game.Ended) return;
            TaskPanelOpen = force ?? !TaskPanelOpen;
            if (taskPanelGo) taskPanelGo.SetActive(TaskPanelOpen);
        }

        public void ShowTutorial()
        {
            if (game.Idle || game.Dead || game.Ended) return;
            game.ShowPaper("CA ĐÊM ĐẦU TIÊN",
                "Bạn là nhân viên ca đêm. Làm việc vặt, phục vụ khách — và <b>tuân thủ tuyệt đối nội quy dán sau quầy</b>.\n\n" +
                "<b>E</b> tương tác · <b>Q</b> đặt đồ xuống · <b>G</b> chào khách · <b>C</b> ngồi thấp · <b>R</b> nội quy · <b>Tab</b> sổ việc\n\n" +
                "<color=#666666>Không ai chỉ đường cho bạn. Nghe, nhìn, và tự quyết định.\n(Cần gợi ý: Cài đặt → Điều khiển → Gợi ý nhiệm vụ.)</color>");
        }

        public void ExecuteAction(UiAction action)
        {
            if (tutorialCoroutine != null && (action == UiAction.ToMenu || action == UiAction.Retry || action == UiAction.Start))
            {
                StopCoroutine(tutorialCoroutine);
                tutorialCoroutine = null;
            }

            switch (action)
            {
                case UiAction.Start:
                    Flow.Execute(UiAction.Start);
                    SyncState();
                    game.NewShift();
                    if (Save != null && !Save.TutSeen)
                    {
                        Save.TutSeen = true;
                        SaveStore.Save(Save, SavePathOverride);
                        tutorialCoroutine = StartCoroutine(ShowTutorialDelayed());
                    }
                    break;
                case UiAction.Resume:
                    Flow.Execute(UiAction.Resume);
                    SyncState();
                    break;
                case UiAction.Retry:
                    Flow.Execute(UiAction.Retry);
                    SyncState();
                    game.Retry();
                    break;
                case UiAction.Settings:
                case UiAction.Help:
                case UiAction.Back:
                    Flow.Execute(action);
                    SyncState();
                    break;
                case UiAction.ToMenu:
                    Flow.Execute(UiAction.ToMenu);
                    attractTime = 0f;
                    TaskPanelOpen = false;
                    toastT = bannerT = warnT = flashT = 0f;
                    game.ReturnToMenu();
                    SyncState();
                    break;
                case UiAction.Quit:
#if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
#else
                    Application.Quit();
#endif
                    break;
            }
        }

        IEnumerator ShowTutorialDelayed()
        {
            yield return new WaitForSeconds(2.6f);
            tutorialCoroutine = null;
            if (Flow.State == AppState.Play && Flow.Screen == UiScreen.None && !game.Idle && !game.Dead && !game.Ended &&
                game.Dialogue == null && game.PaperTitle == null)
            {
                ShowTutorial();
            }
        }

        void OnShiftStarted()
        {
            TaskPanelOpen = false;
            if (taskPanelGo) taskPanelGo.SetActive(false);
            if (Flow.State != AppState.Play || Flow.Screen == UiScreen.Death || Flow.Screen == UiScreen.Ending)
            {
                Flow.EnterPlay();
                SyncState();
            }
        }

        void OnEndingReached(string endingId)
        {
            if (Save.RecordEnding(endingId))
                SaveStore.Save(Save, SavePathOverride);
            RefreshEndingsText();
            ShowEndingScreen(endingId == "B");
        }

        void Update()
        {
            TrackActiveDevice();

            if (Flow.AttractCameraActive)
                UpdateAttractCamera(Time.unscaledDeltaTime);

            if (debugInput?.WasPressedThisFrame() == true) TogglePerfOverlay();
            if (Keyboard.current?.f11Key.wasPressedThisFrame == true) Screen.fullScreen = !Screen.fullScreen;
            if (hideUiInput?.WasPressedThisFrame() == true && Flow.State == AppState.Play && Flow.Screen == UiScreen.None)
                ToggleRecMode();

            if (HackPanelOpen && (Flow.State != AppState.Play || Flow.Screen != UiScreen.None || game.InputBlocked))
                ToggleHackPanel(false);
            else if (Flow.State == AppState.Play && Flow.Screen == UiScreen.None && !game.InputBlocked &&
                     Keyboard.current?.f4Key.wasPressedThisFrame == true)
                ToggleHackPanel();

            if (Flow.State == AppState.Boot)
            {
                if (bootPressText)
                {
                    float alpha = 0.25f + 0.75f * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 3.8f));
                    bootPressText.alpha = alpha;
                }
                if (AnyBootInputPressed())
                    TriggerBootAdvance();
            }
            else if (Flow.Screen == UiScreen.Settings)
            {
                HandleSettingsInput();
            }
            else if (Flow.Screen != UiScreen.None)
            {
                HandleMenuInput();
            }
            else if (Flow.State == AppState.Play)
            {
                if (game.Dialogue != null)
                {
                    HandleDialogInput();
                }
                else if (game.PaperTitle != null)
                {
                    if (useInput?.WasPressedThisFrame() == true || confirmInput?.WasPressedThisFrame() == true ||
                        backInput?.WasPressedThisFrame() == true)
                    {
                        game.ClosePaper();
                    }
                }
                else if (HackPanelOpen && HandleHackInput())
                {
                }
                else if (pauseInput?.WasPressedThisFrame() == true && !game.InputBlocked && !game.ClosedOverlayThisFrame)
                {
                    if (Flow.TryPause(false))
                    {
                        game.PlayClip("ui_move");
                        SyncState();
                    }
                }
                else if (Flow.LastDevice == InputDeviceKind.KeyboardMouse &&
                         player.IsFocused &&
                         !game.InputBlocked &&
                         Mouse.current?.leftButton.wasPressedThisFrame == true &&
                         Cursor.lockState != CursorLockMode.Locked)
                {
                    player.LockCursor(true);
                }
            }

            if (Flow.State == AppState.Play)
            {
                bool shouldLock = Flow.ShouldLockCursor(game.InputBlocked, player.IsFocused);
                if (shouldLock != (Cursor.lockState == CursorLockMode.Locked))
                    player.LockCursor(shouldLock);
            }

            UpdateHud(Time.unscaledDeltaTime);
            UpdateSettingsRuntime(Time.unscaledDeltaTime);
        }

        void HandleDialogInput()
        {
            int count = game.Options != null ? game.Options.Length : 0;
            if (count <= 0) return;

            if (lastDialogRef != game.Dialogue || lastDialogCount != count)
            {
                lastDialogRef = game.Dialogue;
                lastDialogCount = count;
                dialogFocus.Reset(count);
                UpdateDialogFocusVisuals();
            }

            int dir = ReadNavStep();
            if (dir != 0)
            {
                dialogFocus.Move(dir, count);
                game.PlayClip("ui_move");
                UpdateDialogFocusVisuals();
            }

            for (int i = 0; i < optionInputs.Length; i++)
            {
                if (i < count && optionInputs[i]?.WasPressedThisFrame() == true)
                {
                    PickDialogOption(i);
                    return;
                }
            }

            if (confirmInput?.WasPressedThisFrame() == true)
                PickDialogOption(Mathf.Clamp(dialogFocus.Index, 0, count - 1));
        }

        void PickDialogOption(int index)
        {
            game.PlayClip("ui_ok");
            game.SelectOption(index);
        }

        void HandleMenuInput()
        {
            int dir = ReadNavStep();
            if (dir != 0)
            {
                Flow.MoveFocus(dir);
                game.PlayClip("ui_move");
                UpdateFocusVisuals();
            }

            if (confirmInput?.WasPressedThisFrame() == true)
            {
                var actions = Flow.ScreenActions(Flow.Screen);
                if (actions.Length > 0)
                {
                    int idx = Mathf.Clamp(Flow.FocusIndex, 0, actions.Length - 1);
                    game.PlayClip("ui_ok");
                    ExecuteAction(actions[idx]);
                    return;
                }
            }

            bool backPressed = backInput?.WasPressedThisFrame() == true ||
                               (Flow.Screen == UiScreen.Pause && pauseInput?.WasPressedThisFrame() == true);
            if (backPressed)
            {
                var result = Flow.Back();
                if (result != UiAction.None)
                {
                    game.PlayClip("ui_back");
                    SyncState();
                }
            }
        }

        int ReadNavStep()
        {
            if (navInput == null) return 0;
            Vector2 v = navInput.ReadValue<Vector2>();
            int dir = v.y > 0.45f ? -1 : v.y < -0.45f ? 1 : 0;
            if (dir == 0)
            {
                lastNavDir = 0;
                return 0;
            }

            float now = Time.unscaledTime;
            if (dir != lastNavDir)
            {
                lastNavDir = dir;
                nextNavRepeatAt = now + 0.32f;
                return dir;
            }
            if (now >= nextNavRepeatAt)
            {
                nextNavRepeatAt = now + 0.14f;
                return dir;
            }
            return 0;
        }

        void TrackActiveDevice()
        {
            var prev = Flow.LastDevice;
            bool padUsed = Gamepad.current != null &&
                           (Gamepad.current.buttonSouth.wasPressedThisFrame ||
                            Gamepad.current.buttonEast.wasPressedThisFrame ||
                            Gamepad.current.buttonWest.wasPressedThisFrame ||
                            Gamepad.current.buttonNorth.wasPressedThisFrame ||
                            Gamepad.current.startButton.wasPressedThisFrame ||
                            Gamepad.current.selectButton.wasPressedThisFrame ||
                            Gamepad.current.dpad.ReadValue().sqrMagnitude > 0.1f ||
                            Gamepad.current.leftStick.ReadValue().sqrMagnitude > 0.15f ||
                            Gamepad.current.rightStick.ReadValue().sqrMagnitude > 0.15f);
            if (padUsed)
            {
                Flow.SetDevice(InputDeviceKind.Gamepad);
            }
            else if ((Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame) ||
                     (Mouse.current != null && (Mouse.current.leftButton.wasPressedThisFrame ||
                                                Mouse.current.rightButton.wasPressedThisFrame ||
                                                Mouse.current.delta.ReadValue().sqrMagnitude > 4f)))
            {
                Flow.SetDevice(InputDeviceKind.KeyboardMouse);
            }

            if (Flow.LastDevice != prev)
            {
                UpdatePrompts();
                if (Flow.State == AppState.Play)
                    player.LockCursor(Flow.ShouldLockCursor(game.InputBlocked, player.IsFocused));
            }
        }

        bool AnyBootInputPressed()
        {
            if (confirmInput?.WasPressedThisFrame() == true) return true;
            if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame) return true;
            if (Mouse.current != null && (Mouse.current.leftButton.wasPressedThisFrame || Mouse.current.rightButton.wasPressedThisFrame))
                return true;
            var pad = Gamepad.current;
            if (pad != null && (pad.buttonSouth.wasPressedThisFrame || pad.buttonEast.wasPressedThisFrame ||
                                pad.buttonWest.wasPressedThisFrame || pad.buttonNorth.wasPressedThisFrame ||
                                pad.startButton.wasPressedThisFrame || pad.selectButton.wasPressedThisFrame))
                return true;
            return false;
        }

        void UpdateAttractCamera(float dt)
        {
            if (!view) return;
            attractTime += dt;
            float a = attractTime * 0.06f;
            var threePos = new Vector3(6.3f + Mathf.Sin(a) * 1.2f, 1.55f, 6.9f - Mathf.Cos(a * 0.7f) * 0.4f);
            float pitch = -0.06f;
            float yaw = Mathf.PI + 0.35f + Mathf.Sin(a * 0.8f) * 0.5f;
            view.transform.SetPositionAndRotation(ThreeCoords.ToUnity(threePos), ThreeCoords.CameraRotation(pitch, yaw, 0f));
            view.fieldOfView = Save != null ? Save.Settings.fov : 72f;
        }

        void SyncState()
        {
            bool paused = Flow.WorldPaused;
            if (paused && !wasPaused)
            {
                savedTimeScale = Time.timeScale > 0.001f ? Time.timeScale : 1f;
                Time.timeScale = 0f;
            }
            else if (!paused && wasPaused)
            {
                Time.timeScale = savedTimeScale;
            }
            wasPaused = paused;

            game.Paused = paused;
            bool allowWorld = Flow.GameplayAllowed(game.InputBlocked);
            player.GameplayEnabled = allowWorld;
            player.Motor.frozen = !allowWorld;
            if (interactor) interactor.Enabled = allowWorld;

            if (!Flow.AttractCameraActive && !game.Dead)
                player.ExternalView = false;
            else if (Flow.AttractCameraActive)
                player.ExternalView = true;

            if (Flow.State == AppState.Play)
                player.LockCursor(Flow.ShouldLockCursor(game.InputBlocked, player.IsFocused));
            else
                player.LockCursor(false);

            foreach (var kv in screens)
                kv.Value.SetActive(kv.Key == Flow.Screen);

            if (Flow.Screen == UiScreen.Settings) RenderSettingsTab();
            RefreshEndingsText();
            UpdateFocusVisuals();
            UpdatePrompts();
        }

        void RefreshEndingsText()
        {
            if (menuEndingsText && Save != null)
                menuEndingsText.text = $"Kết thúc đã mở: {Save.UnlockedEndingsCount}/2";
        }

        void UpdateFocusVisuals()
        {
            if (!menuItems.TryGetValue(Flow.Screen, out var list)) return;
            bool isEnding = Flow.Screen == UiScreen.Ending;
            for (int i = 0; i < list.Count; i++)
            {
                bool focused = i == Flow.FocusIndex;
                var item = list[i];
                if (item.Bar) item.Bar.enabled = focused && !isEnding && Flow.Screen != UiScreen.Death;
                if (item.Label)
                {
                    if (isEnding)
                        item.Label.color = focused ? Color.black : new Color(0.33f, 0.33f, 0.33f, 1f);
                    else
                        item.Label.color = focused ? Color.white : Muted;

                    if (Flow.Screen != UiScreen.Death && Flow.Screen != UiScreen.Ending)
                        item.Label.rectTransform.anchoredPosition = new Vector2(focused ? 34f : 24f, 0f);
                }
            }
        }

        void UpdatePrompts()
        {
            string text = Flow.LastDevice == InputDeviceKind.Gamepad
                ? "[A] Chọn    [B] Quay lại"
                : "[Enter] Chọn    [Esc] Quay lại";
            foreach (var label in promptTexts)
                if (label) label.text = text;
        }

        void BuildUi()
        {
            if (FindFirstObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                es.transform.SetParent(null);
            }

            if (sourceFont)
            {
                font = TMP_FontAsset.CreateFontAsset(sourceFont);
                hangulFallback = TMP_FontAsset.CreateFontAsset("Malgun Gothic", "Regular", 48);
                if (hangulFallback)
                {
                    font.fallbackFontAssetTable ??= new List<TMP_FontAsset>();
                    font.fallbackFontAssetTable.Add(hangulFallback);
                }
            }

            root = new GameObject("HanaMart UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 25;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = HudLayoutSpec.ReferenceResolution;
            scaler.matchWidthOrHeight = 0.5f;

            BuildBootScreen();
            BuildMenuScreen();
            BuildPauseScreen();
            BuildHelpScreen();
            BuildDeathScreen();
            BuildEndingScreen();
        }

        void BuildBootScreen()
        {
            var panel = CreateScreen(UiScreen.Boot, Color.black);
            var click = panel.AddComponent<PointerRelay>();
            click.Clicked = TriggerBootAdvance;

            var kr = CreateText(panel.transform, "KR", "야간 근무 규칙", 20f, Teal, TextAlignmentOptions.Center);
            kr.characterSpacing = 10f;
            SetAnchored(kr.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 95f), new Vector2(720f, 36f));

            var warn = CreateText(panel.transform, "Warning",
                "Trò chơi có yếu tố kinh dị tâm lý, âm thanh đột ngột và hình ảnh gây sợ hãi.\nNên đeo tai nghe. Hỗ trợ chuột & bàn phím hoặc tay cầm.",
                18f, Muted, TextAlignmentOptions.Center);
            SetAnchored(warn.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 15f), new Vector2(720f, 90f));

            bootPressText = CreateText(panel.transform, "Press", "BẤM BẤT KỲ PHÍM NÀO", 18f, Teal, TextAlignmentOptions.Center);
            bootPressText.characterSpacing = 4f;
            SetAnchored(bootPressText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -95f), new Vector2(540f, 36f));
        }

        void BuildMenuScreen()
        {
            var panel = CreateScreen(UiScreen.Menu, new Color(0.015f, 0.039f, 0.039f, 0.78f));

            var kr = CreateText(panel.transform, "KR", "야간 근무 규칙", 16f, Teal, TextAlignmentOptions.Left);
            kr.characterSpacing = 8f;
            SetAnchored(kr.rectTransform, new Vector2(0f, 1f), new Vector2(90f, -96f), new Vector2(520f, 30f));

            var title = CreateText(panel.transform, "Title", "QUY TẮC\n<color=#d7262f>CA ĐÊM</color>", 66f, Bright, TextAlignmentOptions.Left);
            title.fontStyle = FontStyles.Bold;
            title.lineSpacing = -12f;
            SetAnchored(title.rectTransform, new Vector2(0f, 1f), new Vector2(90f, -130f), new Vector2(520f, 155f));

            var badgeBg = CreateBox(panel.transform, "StoreBadge", Teal);
            SetAnchored(badgeBg, new Vector2(0f, 1f), new Vector2(90f, -295f), new Vector2(310f, 36f));
            var badgeText = CreateText(badgeBg, "Label", "<b>H</b>  HANA MART 24 · 하나마트", 17f, Color.white, TextAlignmentOptions.Center);
            badgeText.fontStyle = FontStyles.Bold;
            Stretch(badgeText.rectTransform);

            var tag = CreateText(panel.transform, "Tag",
                "00:00 → 06:00. Một cửa hàng tiện lợi vắng trong mưa. Mười ba điều nội quy. Làm sai một điều, ca đêm sẽ không bao giờ kết thúc.",
                15f, Muted, TextAlignmentOptions.TopLeft);
            SetAnchored(tag.rectTransform, new Vector2(0f, 1f), new Vector2(90f, -350f), new Vector2(500f, 70f));

            AddMenuButtons(UiScreen.Menu, panel.transform, 90f, -435f, new[]
            {
                (UiAction.Start, "Nhận ca đêm"),
                (UiAction.Settings, "Cài đặt"),
                (UiAction.Help, "Hướng dẫn"),
                (UiAction.Quit, "Thoát"),
            });

            AddFooterPrompts(panel.transform);
            menuEndingsText = CreateText(panel.transform, "Endings", "Kết thúc đã mở: 0/2", 14f, FooterText, TextAlignmentOptions.Right);
            SetAnchored(menuEndingsText.rectTransform, new Vector2(1f, 0f), new Vector2(-56f, 28f), new Vector2(280f, 28f));
        }

        void BuildPauseScreen()
        {
            var panel = CreateScreen(UiScreen.Pause, new Color(0.015f, 0.031f, 0.031f, 0.88f));

            var kr = CreateText(panel.transform, "KR", "일시 정지", 16f, Teal, TextAlignmentOptions.Left);
            kr.characterSpacing = 8f;
            SetAnchored(kr.rectTransform, new Vector2(0f, 1f), new Vector2(90f, -150f), new Vector2(480f, 30f));

            var title = CreateText(panel.transform, "Title", "TẠM DỪNG", 54f, Bright, TextAlignmentOptions.Left);
            title.fontStyle = FontStyles.Bold;
            SetAnchored(title.rectTransform, new Vector2(0f, 1f), new Vector2(90f, -186f), new Vector2(500f, 74f));

            AddMenuButtons(UiScreen.Pause, panel.transform, 90f, -290f, new[]
            {
                (UiAction.Resume, "Tiếp tục"),
                (UiAction.Retry, "Chơi lại từ mốc giờ gần nhất"),
                (UiAction.Settings, "Cài đặt"),
                (UiAction.Help, "Hướng dẫn"),
                (UiAction.ToMenu, "Về màn hình chính"),
            });

            AddFooterPrompts(panel.transform);
        }

        GameObject CreateScreen(UiScreen screen, Color backdrop)
        {
            var go = new GameObject("Screen_" + screen, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(root.transform, false);
            var rt = go.GetComponent<RectTransform>();
            Stretch(rt);
            var img = go.GetComponent<Image>();
            img.color = backdrop;
            screens[screen] = go;
            return go;
        }

        void AddMenuButtons(UiScreen screen, Transform parent, float left, float topY, (UiAction action, string label)[] items,
            bool centered = false, float width = 480f)
        {
            var list = new List<MenuItemView>(items.Length);
            for (int i = 0; i < items.Length; i++)
            {
                int index = i;
                var (action, text) = items[i];
                var row = new GameObject("Item_" + action, typeof(RectTransform), typeof(Image), typeof(Button));
                row.transform.SetParent(parent, false);
                if (centered)
                {
                    var rt = row.GetComponent<RectTransform>();
                    rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.anchoredPosition = new Vector2(left, topY - i * 48f);
                    rt.sizeDelta = new Vector2(width, 44f);
                }
                else
                {
                    SetAnchored(row.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(left, topY - i * 48f), new Vector2(width, 44f));
                }

                var hit = row.GetComponent<Image>();
                hit.color = new Color(0f, 0f, 0f, 0.001f);

                var btn = row.GetComponent<Button>();
                btn.transition = Selectable.Transition.None;
                btn.navigation = new Navigation { mode = Navigation.Mode.None };

                var barGo = CreateBox(row.transform, "Bar", Teal);
                SetAnchored(barGo, new Vector2(0f, 0.5f), new Vector2(0f, 0f), new Vector2(6f, 28f));
                barGo.pivot = new Vector2(0f, 0.5f);
                barGo.anchoredPosition = new Vector2(0f, 0f);
                var bar = barGo.GetComponent<Image>();
                bar.enabled = false;

                var label = CreateText(row.transform, "Text", text, 24f, Muted, centered ? TextAlignmentOptions.Center : TextAlignmentOptions.Left);
                label.fontStyle = FontStyles.Bold;
                if (centered)
                {
                    Stretch(label.rectTransform);
                }
                else
                {
                    SetAnchored(label.rectTransform, new Vector2(0f, 0.5f), new Vector2(24f, 0f), new Vector2(width - 40f, 40f));
                    label.rectTransform.pivot = new Vector2(0f, 0.5f);
                    label.rectTransform.anchoredPosition = new Vector2(24f, 0f);
                }

                var relay = row.AddComponent<PointerRelay>();
                relay.Entered = () =>
                {
                    if (Flow.Screen == screen && Flow.FocusIndex != index)
                    {
                        Flow.SetFocus(index);
                        UpdateFocusVisuals();
                    }
                };
                relay.Clicked = () =>
                {
                    if (Flow.Screen != screen) return;
                    Flow.SetFocus(index);
                    game.PlayClip("ui_ok");
                    ExecuteAction(action);
                };

                list.Add(new MenuItemView { Action = action, Go = row, Bar = bar, Label = label });
            }
            menuItems[screen] = list;
        }

        void AddFooterPrompts(Transform parent)
        {
            var prompt = CreateText(parent, "Prompts", "[Enter] Chọn    [Esc] Quay lại", 14f, FooterText, TextAlignmentOptions.Left);
            SetAnchored(prompt.rectTransform, new Vector2(0f, 0f), new Vector2(90f, 28f), new Vector2(460f, 28f));
            promptTexts.Add(prompt);
        }

        TextMeshProUGUI CreateText(Transform parent, string name, string text, float size, Color color, TextAlignmentOptions align)
        {
            var tmp = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
            tmp.transform.SetParent(parent, false);
            if (font) tmp.font = font;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = align;
            tmp.text = text;
            tmp.raycastTarget = false;
            return tmp;
        }

        static RectTransform CreateBox(Transform parent, string name, Color color)
        {
            var img = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            img.transform.SetParent(parent, false);
            img.color = color;
            img.raycastTarget = false;
            return img.rectTransform;
        }

        static void SetAnchored(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(anchor.x, anchor.y);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        void OnDestroy()
        {
            if (wasPaused) Time.timeScale = savedTimeScale;
            RestoreGlobals();
            if (game)
            {
                game.ShiftStarted -= OnShiftStarted;
                game.EndingReached -= OnEndingReached;
                game.ToastRequested -= ShowToast;
                game.BannerRequested -= ShowBanner;
                game.WarnRequested -= ShowWarn;
                game.DeathRecapShown -= ShowDeathRecap;
            }
            if (hudRoot) Destroy(hudRoot);
            if (root) Destroy(root);
            DestroyFontAsset(hangulFallback);
            DestroyFontAsset(font);
        }

        static void DestroyFontAsset(TMP_FontAsset asset)
        {
            if (!asset) return;
            if (asset.material) Destroy(asset.material);
            if (asset.atlasTextures != null)
                foreach (var tex in asset.atlasTextures)
                    if (tex) Destroy(tex);
            Destroy(asset);
        }
    }
}
