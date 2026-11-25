using System;
using System.Collections.Generic;
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
    public sealed class ShiftInterface : MonoBehaviour
    {
        static readonly Color Teal = new(0f, 0.702f, 0.604f, 1f);
        static readonly Color Red = new(0.843f, 0.149f, 0.184f, 1f);
        static readonly Color Muted = new(0.624f, 0.663f, 0.651f, 1f);
        static readonly Color Bright = new(0.933f, 0.941f, 0.925f, 1f);
        static readonly Color FooterText = new(0.451f, 0.502f, 0.490f, 1f);

        public Font sourceFont;
        public string SavePathOverride { get; set; }
        public UiFlow Flow { get; } = new();
        public SaveData Save { get; private set; }

        ShiftGame game;
        PlayerController player;
        Camera view;

        InputAction navInput, confirmInput, backInput, pauseInput;
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
            view = GetComponent<Camera>();
            game.StartIdle = true;
            player.AutoManageCursor = false;
            Save = SaveStore.Load(SavePathOverride);
        }

        void Start()
        {
            game.ShiftStarted += OnShiftStarted;
            game.EndingReached += OnEndingReached;

            if (player.Input)
            {
                var uiMap = player.Input.FindActionMap("UI", false);
                if (uiMap != null)
                {
                    uiMap.Enable();
                    navInput = uiMap.FindAction("UiNavigate", false);
                    confirmInput = uiMap.FindAction("UiConfirm", false);
                    backInput = uiMap.FindAction("UiBack", false);
                }
                pauseInput = player.Input.FindAction("Pause", false);
            }

            BuildUi();
            SyncState();
        }

        public void ReloadSave(string path = null)
        {
            SavePathOverride = path;
            Save = SaveStore.Load(SavePathOverride);
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

        public void ExecuteAction(UiAction action)
        {
            switch (action)
            {
                case UiAction.Start:
                    Flow.Execute(UiAction.Start);
                    SyncState();
                    game.NewShift();
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

        void OnShiftStarted()
        {
            if (Flow.State != AppState.Play)
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
        }

        void Update()
        {
            TrackActiveDevice();

            if (Flow.AttractCameraActive)
                UpdateAttractCamera(Time.unscaledDeltaTime);

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
            else if (Flow.Screen != UiScreen.None)
            {
                HandleMenuInput();
            }
            else if (Flow.State == AppState.Play)
            {
                if (pauseInput?.WasPressedThisFrame() == true && !game.InputBlocked && !game.ClosedOverlayThisFrame)
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
            for (int i = 0; i < list.Count; i++)
            {
                bool focused = i == Flow.FocusIndex;
                var item = list[i];
                if (item.Bar) item.Bar.enabled = focused;
                if (item.Label)
                {
                    item.Label.color = focused ? Color.white : Muted;
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
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;

            BuildBootScreen();
            BuildMenuScreen();
            BuildPauseScreen();
            BuildSettingsShell();
            BuildHelpShell();
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

        void BuildSettingsShell()
        {
            var panel = CreateScreen(UiScreen.Settings, new Color(0.015f, 0.031f, 0.031f, 0.9f));
            var title = CreateText(panel.transform, "Title", "Cài đặt", 42f, Bright, TextAlignmentOptions.Left);
            title.fontStyle = FontStyles.Bold;
            SetAnchored(title.rectTransform, new Vector2(0f, 1f), new Vector2(140f, -80f), new Vector2(500f, 56f));

            var note = CreateText(panel.transform, "Note",
                "Đồ họa · Âm thanh · Điều khiển\n(Các tùy chọn chi tiết sẽ mở khóa ở bước Cài đặt & Mức đồ họa.)",
                18f, Muted, TextAlignmentOptions.TopLeft);
            SetAnchored(note.rectTransform, new Vector2(0f, 1f), new Vector2(140f, -165f), new Vector2(820f, 90f));

            AddMenuButtons(UiScreen.Settings, panel.transform, 140f, -560f, new[]
            {
                (UiAction.Back, "← Quay lại"),
            });
            AddFooterPrompts(panel.transform);
        }

        void BuildHelpShell()
        {
            var panel = CreateScreen(UiScreen.Help, new Color(0.015f, 0.031f, 0.031f, 0.9f));
            var title = CreateText(panel.transform, "Title", "Hướng dẫn", 42f, Bright, TextAlignmentOptions.Left);
            title.fontStyle = FontStyles.Bold;
            SetAnchored(title.rectTransform, new Vector2(0f, 1f), new Vector2(140f, -80f), new Vector2(500f, 56f));

            var controls = CreateText(panel.transform, "Controls",
                "<b>BÀN PHÍM & CHUỘT</b>\nW A S D: Di chuyển · Shift: Đi nhanh · C: Ngồi thấp\nE / Chuột trái: Tương tác · Q: Đặt đồ · G: Chào khách\nR: Nội quy · Tab: Việc cần làm · Esc: Tạm dừng",
                18f, Bright, TextAlignmentOptions.TopLeft);
            SetAnchored(controls.rectTransform, new Vector2(0f, 1f), new Vector2(140f, -160f), new Vector2(520f, 220f));

            var pad = CreateText(panel.transform, "Gamepad",
                "<b>TAY CẦM</b>\nL: Di chuyển · R: Nhìn · A: Tương tác · RB: Đặt đồ\nY: Chào khách · X: Nội quy · B: Ngồi thấp · Menu: Tạm dừng",
                18f, Bright, TextAlignmentOptions.TopLeft);
            SetAnchored(pad.rectTransform, new Vector2(0f, 1f), new Vector2(680f, -160f), new Vector2(520f, 220f));

            AddMenuButtons(UiScreen.Help, panel.transform, 140f, -560f, new[]
            {
                (UiAction.Back, "← Quay lại"),
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

        void AddMenuButtons(UiScreen screen, Transform parent, float left, float topY, (UiAction action, string label)[] items)
        {
            var list = new List<MenuItemView>(items.Length);
            for (int i = 0; i < items.Length; i++)
            {
                int index = i;
                var (action, text) = items[i];
                var row = new GameObject("Item_" + action, typeof(RectTransform), typeof(Image), typeof(Button));
                row.transform.SetParent(parent, false);
                SetAnchored(row.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(left, topY - i * 48f), new Vector2(480f, 44f));

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

                var label = CreateText(row.transform, "Text", text, 24f, Muted, TextAlignmentOptions.Left);
                label.fontStyle = FontStyles.Bold;
                SetAnchored(label.rectTransform, new Vector2(0f, 0.5f), new Vector2(24f, 0f), new Vector2(440f, 40f));
                label.rectTransform.pivot = new Vector2(0f, 0.5f);
                label.rectTransform.anchoredPosition = new Vector2(24f, 0f);

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
            if (game)
            {
                game.ShiftStarted -= OnShiftStarted;
                game.EndingReached -= OnEndingReached;
            }
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
