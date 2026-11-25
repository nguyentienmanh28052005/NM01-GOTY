using HanaMart.Interaction;
using HanaMart.Shift;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HanaMart.UI
{
    public sealed partial class ShiftInterface
    {
        GameObject hudRoot, hudElements, taskPanelGo, guideGo, guideArrowGo, promptGo, holdGo, greetPillGo;
        GameObject toastGo, bannerGo, warnGo, dialogGo, paperGo, paperRulesTopBar, paperStampGo;
        RectTransform letterTop, letterBottom, holdFill, guideArrowRt;
        Image flashImage, paperSheetImg;
        TextMeshProUGUI clockText, guideTitleText, guideWhereText, taskListText, taskPanelText;
        TextMeshProUGUI promptText, holdText, greetPillText, carryText, hintsText, subtitleText;
        TextMeshProUGUI toastText, bannerMainText, bannerSubText, warnTitleText, warnBodyText;
        TextMeshProUGUI dlgSpeakerText, dlgBodyText, paperTitleText, paperBodyText, paperCloseText;
        readonly Image[] strikeBars = new Image[3];
        readonly GameObject[] dlgOptGos = new GameObject[3];
        readonly Image[] dlgOptBgs = new Image[3];
        readonly Image[] dlgOptBars = new Image[3];
        readonly TextMeshProUGUI[] dlgOptTexts = new TextMeshProUGUI[3];

        readonly DialogFocus dialogFocus = new();
        string lastDialogRef;
        int lastDialogCount = -1;
        string lastPaperRef;
        float letterboxHeight;
        float toastT, bannerT, warnT, flashT;

        void BuildHud()
        {
            hudRoot = new GameObject("HanaMart HUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = hudRoot.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 15;
            var scaler = hudRoot.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = HudLayoutSpec.ReferenceResolution;
            scaler.matchWidthOrHeight = 0.5f;

            // Letterbox top & bottom
            letterTop = CreateBox(hudRoot.transform, "LetterboxTop", Color.black);
            letterTop.anchorMin = new Vector2(0f, 1f);
            letterTop.anchorMax = new Vector2(1f, 1f);
            letterTop.pivot = new Vector2(0.5f, 1f);
            letterTop.anchoredPosition = Vector2.zero;
            letterTop.sizeDelta = new Vector2(0f, 0f);

            letterBottom = CreateBox(hudRoot.transform, "LetterboxBottom", Color.black);
            letterBottom.anchorMin = new Vector2(0f, 0f);
            letterBottom.anchorMax = new Vector2(1f, 0f);
            letterBottom.pivot = new Vector2(0.5f, 0f);
            letterBottom.anchoredPosition = Vector2.zero;
            letterBottom.sizeDelta = new Vector2(0f, 0f);

            // Screen flash
            var flashRt = CreateBox(hudRoot.transform, "Flash", new Color(Red.r, Red.g, Red.b, 0f));
            Stretch(flashRt);
            flashImage = flashRt.GetComponent<Image>();

            // Gameplay HUD container (hidden on Boot/Menu or recMode)
            hudElements = new GameObject("HudElements", typeof(RectTransform));
            hudElements.transform.SetParent(hudRoot.transform, false);
            Stretch(hudElements.GetComponent<RectTransform>());

            // Crosshair dot
            var dotRt = CreateBox(hudElements.transform, "Dot", new Color(1f, 1f, 1f, 0.78f));
            SetAnchored(dotRt, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(4f, 4f));

            // Clock + Strikes
            var clkBg = CreateBox(hudElements.transform, "ClockBox", new Color(0f, 0.08f, 0.06f, 0.65f));
            SetTopLeft(clkBg, 36f, 30f, 146f, 50f);
            clockText = CreateText(clkBg, "Clock", "00:00", 34f, Cyan, TextAlignmentOptions.Center);
            clockText.fontStyle = FontStyles.Bold;
            Stretch(clockText.rectTransform);

            for (int i = 0; i < 3; i++)
            {
                var border = CreateBox(hudElements.transform, "StrikeBorder" + i, new Color(1f, 1f, 1f, 0.35f));
                SetTopLeft(border, 198f + i * 20f, 43f, 14f, 22f);
                var fill = CreateBox(border, "Fill", new Color(0.05f, 0.08f, 0.08f, 0.85f));
                Stretch(fill);
                fill.offsetMin = new Vector2(2f, 2f);
                fill.offsetMax = new Vector2(-2f, -2f);
                strikeBars[i] = fill.GetComponent<Image>();
            }

            // Guide box + edge arrow
            var guideRt = CreateBox(hudElements.transform, "GuideBox", new Color(0f, 0.09f, 0.08f, 0.72f));
            SetTopLeft(guideRt, 36f, 96f, 340f, 58f);
            guideGo = guideRt.gameObject;
            var guideBar = CreateBox(guideRt, "Bar", Cyan);
            SetTopLeft(guideBar, 0f, 0f, 3f, 58f);
            guideTitleText = CreateText(guideRt, "Title", "", 16f, Color.white, TextAlignmentOptions.Left);
            guideTitleText.fontStyle = FontStyles.Bold;
            SetTopLeft(guideTitleText.rectTransform, 12f, 6f, 320f, 24f);
            guideWhereText = CreateText(guideRt, "Where", "", 13f, new Color(0.66f, 0.91f, 0.85f, 1f), TextAlignmentOptions.Left);
            SetTopLeft(guideWhereText.rectTransform, 12f, 31f, 320f, 20f);
            guideGo.SetActive(false);

            var arrowText = CreateText(hudElements.transform, "GuideArrow", "▶", 24f, Cyan, TextAlignmentOptions.Center);
            guideArrowRt = arrowText.rectTransform;
            SetAnchored(guideArrowRt, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(40f, 40f));
            guideArrowGo = arrowText.gameObject;
            guideArrowGo.SetActive(false);

            // Task list (left)
            var taskHeader = CreateText(hudElements.transform, "TaskHeader", "VIỆC CẦN LÀM", 12f, new Color(0.6f, 0.95f, 0.75f, 0.72f), TextAlignmentOptions.Left);
            taskHeader.characterSpacing = 3f;
            SetTopLeft(taskHeader.rectTransform, 36f, 174f, 380f, 18f);
            taskListText = CreateText(hudElements.transform, "TaskList", "", 15f, Bright, TextAlignmentOptions.TopLeft);
            SetTopLeft(taskListText.rectTransform, 36f, 196f, 380f, 140f);

            // Task panel (Tab clipboard, top-right)
            var tpRt = CreateBox(hudElements.transform, "TaskPanel", PaperCream);
            SetTopLeft(tpRt, 860f, 110f, 380f, 290f);
            taskPanelGo = tpRt.gameObject;
            var tpTop = CreateBox(tpRt, "TopBar", Teal);
            SetTopLeft(tpTop, 0f, 0f, 380f, 8f);
            var tpHead = CreateText(tpRt, "Head", "VIỆC CẦN LÀM · CA ĐÊM", 14f, PaperInk, TextAlignmentOptions.Left);
            tpHead.fontStyle = FontStyles.Bold;
            tpHead.characterSpacing = 2f;
            SetTopLeft(tpHead.rectTransform, 20f, 18f, 340f, 24f);
            taskPanelText = CreateText(tpRt, "Body", "", 15f, PaperInk, TextAlignmentOptions.TopLeft);
            SetTopLeft(taskPanelText.rectTransform, 20f, 48f, 340f, 224f);
            taskPanelGo.SetActive(false);

            // Interaction prompt + hold bar
            var pRt = CreateBox(hudElements.transform, "Prompt", new Color(0f, 0f, 0f, 0.58f));
            SetAnchored(pRt, new Vector2(0.5f, 0.5f), new Vector2(0f, -44f), new Vector2(420f, 36f));
            promptGo = pRt.gameObject;
            var pBar = CreateBox(pRt, "Bar", Teal);
            SetTopLeft(pBar, 0f, 0f, 3f, 36f);
            promptText = CreateText(pRt, "Text", "", 17f, Bright, TextAlignmentOptions.Center);
            Stretch(promptText.rectTransform);
            promptGo.SetActive(false);

            var hRt = new GameObject("Hold", typeof(RectTransform)).GetComponent<RectTransform>();
            hRt.SetParent(hudElements.transform, false);
            SetAnchored(hRt, new Vector2(0.5f, 0.5f), new Vector2(0f, -84f), new Vector2(240f, 34f));
            holdGo = hRt.gameObject;
            holdText = CreateText(hRt, "Label", "...", 14f, Bright, TextAlignmentOptions.Center);
            SetTopLeft(holdText.rectTransform, 0f, 0f, 240f, 20f);
            var hTrack = CreateBox(hRt, "Track", new Color(1f, 1f, 1f, 0.18f));
            SetTopLeft(hTrack, 0f, 24f, 240f, 5f);
            holdFill = CreateBox(hTrack, "Fill", Teal);
            holdFill.anchorMin = new Vector2(0f, 0f);
            holdFill.anchorMax = new Vector2(0f, 1f);
            holdFill.pivot = new Vector2(0f, 0.5f);
            holdFill.anchoredPosition = Vector2.zero;
            holdFill.sizeDelta = new Vector2(0f, 0f);
            holdGo.SetActive(false);

            // Greet / Skip pill
            var g2Rt = CreateBox(hudElements.transform, "GreetPill", new Color(0f, 0.16f, 0.13f, 0.76f));
            SetAnchored(g2Rt, new Vector2(0.5f, 1f), new Vector2(0f, -158f), new Vector2(360f, 40f));
            greetPillGo = g2Rt.gameObject;
            greetPillText = CreateText(g2Rt, "Text", "", 19f, Color.white, TextAlignmentOptions.Center);
            greetPillText.fontStyle = FontStyles.Bold;
            Stretch(greetPillText.rectTransform);
            greetPillGo.SetActive(false);

            // Carry & hints (bottom-right)
            carryText = CreateText(hudElements.transform, "Carry", "", 15f, new Color(0.8f, 1f, 0.93f, 1f), TextAlignmentOptions.Right);
            SetAnchored(carryText.rectTransform, new Vector2(1f, 0f), new Vector2(-40f, 40f), new Vector2(460f, 26f));

            hintsText = CreateText(hudElements.transform, "Hints", "", 12f, new Color(1f, 1f, 1f, 0.52f), TextAlignmentOptions.Right);
            SetAnchored(hintsText.rectTransform, new Vector2(1f, 0f), new Vector2(-40f, 14f), new Vector2(740f, 22f));

            // Subtitle (above recMode-independent layer)
            subtitleText = CreateText(hudRoot.transform, "Subtitle", "", 21f, Color.white, TextAlignmentOptions.Center);
            SetAnchored(subtitleText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 78f), new Vector2(900f, 64f));

            // Toast (top-right)
            var toastRt = CreateBox(hudElements.transform, "Toast", new Color(0.015f, 0.04f, 0.04f, 0.82f));
            SetAnchored(toastRt, new Vector2(1f, 1f), new Vector2(-40f, -36f), new Vector2(420f, 52f));
            toastGo = toastRt.gameObject;
            var toastBar = CreateBox(toastRt, "Bar", Teal);
            SetTopLeft(toastBar, 0f, 0f, 4f, 52f);
            toastText = CreateText(toastRt, "Text", "", 15f, Bright, TextAlignmentOptions.Left);
            SetTopLeft(toastText.rectTransform, 16f, 8f, 392f, 36f);
            toastGo.SetActive(false);

            // Banner (phase / hour)
            var banRt = new GameObject("Banner", typeof(RectTransform)).GetComponent<RectTransform>();
            banRt.SetParent(hudRoot.transform, false);
            SetAnchored(banRt, new Vector2(0.5f, 0.5f), new Vector2(0f, 60f), new Vector2(1000f, 140f));
            bannerGo = banRt.gameObject;
            bannerMainText = CreateText(banRt, "Main", "", 76f, Cyan, TextAlignmentOptions.Center);
            bannerMainText.fontStyle = FontStyles.Bold;
            SetTopLeft(bannerMainText.rectTransform, 0f, 0f, 1000f, 88f);
            bannerSubText = CreateText(banRt, "Sub", "", 18f, new Color(0.8f, 1f, 0.93f, 1f), TextAlignmentOptions.Center);
            bannerSubText.characterSpacing = 5f;
            SetTopLeft(bannerSubText.rectTransform, 0f, 94f, 1000f, 34f);
            bannerGo.SetActive(false);

            // Warning (strike)
            var warnRt = CreateBox(hudRoot.transform, "Warn", new Color(0.16f, 0f, 0.02f, 0.84f));
            SetAnchored(warnRt, new Vector2(0.5f, 1f), new Vector2(0f, -110f), new Vector2(640f, 96f));
            warnGo = warnRt.gameObject;
            warnTitleText = CreateText(warnRt, "Title", "", 24f, new Color(1f, 0.33f, 0.38f, 1f), TextAlignmentOptions.Center);
            warnTitleText.fontStyle = FontStyles.Bold;
            SetTopLeft(warnTitleText.rectTransform, 20f, 12f, 600f, 34f);
            warnBodyText = CreateText(warnRt, "Body", "", 16f, new Color(1f, 0.86f, 0.86f, 1f), TextAlignmentOptions.Center);
            SetTopLeft(warnBodyText.rectTransform, 20f, 48f, 600f, 38f);
            warnGo.SetActive(false);

            BuildDialogBox();
            BuildPaperOverlay();
        }

        void BuildDialogBox()
        {
            var dlgRt = CreateBox(hudRoot.transform, "Dialog", new Color(0.015f, 0.04f, 0.038f, 0.95f));
            SetAnchored(dlgRt, new Vector2(0.5f, 0f), new Vector2(0f, 48f), new Vector2(860f, 248f));
            dlgRt.GetComponent<Image>().raycastTarget = true;
            dialogGo = dlgRt.gameObject;

            var topBar = CreateBox(dlgRt, "TopBar", Teal);
            SetTopLeft(topBar, 0f, 0f, 860f, 4f);

            dlgSpeakerText = CreateText(dlgRt, "Speaker", "", 14f, Teal, TextAlignmentOptions.Left);
            dlgSpeakerText.fontStyle = FontStyles.Bold;
            dlgSpeakerText.characterSpacing = 2f;
            SetTopLeft(dlgSpeakerText.rectTransform, 26f, 16f, 808f, 22f);

            dlgBodyText = CreateText(dlgRt, "Body", "", 19f, Bright, TextAlignmentOptions.TopLeft);
            dlgBodyText.enableAutoSizing = true;
            dlgBodyText.fontSizeMin = 14f;
            dlgBodyText.fontSizeMax = 19f;
            SetTopLeft(dlgBodyText.rectTransform, 26f, 42f, 808f, 76f);

            for (int i = 0; i < 3; i++)
            {
                int idx = i;
                var optRt = CreateBox(dlgRt, "Opt" + i, new Color(0f, 0f, 0f, 0.001f));
                SetTopLeft(optRt, 26f, 126f + i * 38f, 808f, 34f);
                var bg = optRt.GetComponent<Image>();
                bg.raycastTarget = true;
                var btn = optRt.gameObject.AddComponent<Button>();
                btn.transition = Selectable.Transition.None;
                btn.navigation = new Navigation { mode = Navigation.Mode.None };

                var barRt = CreateBox(optRt, "Bar", Teal);
                SetTopLeft(barRt, 0f, 2f, 3f, 30f);
                var bar = barRt.GetComponent<Image>();
                bar.enabled = false;

                var label = CreateText(optRt, "Text", "", 17f, Muted, TextAlignmentOptions.Left);
                SetTopLeft(label.rectTransform, 14f, 3f, 784f, 28f);

                var relay = optRt.gameObject.AddComponent<PointerRelay>();
                relay.Entered = () =>
                {
                    if (game.Dialogue != null && game.Options != null && idx < game.Options.Length)
                    {
                        dialogFocus.Set(idx, game.Options.Length);
                        UpdateDialogFocusVisuals();
                    }
                };
                relay.Clicked = () =>
                {
                    if (game.Dialogue != null && game.Options != null && idx < game.Options.Length)
                        PickDialogOption(idx);
                };

                dlgOptGos[i] = optRt.gameObject;
                dlgOptBgs[i] = bg;
                dlgOptBars[i] = bar;
                dlgOptTexts[i] = label;
            }
            dialogGo.SetActive(false);
        }

        void BuildPaperOverlay()
        {
            var backdropRt = CreateBox(hudRoot.transform, "PaperOverlay", new Color(0f, 0f, 0f, 0.56f));
            Stretch(backdropRt);
            backdropRt.GetComponent<Image>().raycastTarget = true;
            paperGo = backdropRt.gameObject;
            var closeRelay = paperGo.AddComponent<PointerRelay>();
            closeRelay.Clicked = () =>
            {
                if (game.PaperTitle != null) game.ClosePaper();
            };

            var sheetRt = CreateBox(backdropRt, "Sheet", PaperCream);
            SetAnchored(sheetRt, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(720f, 620f));
            paperSheetImg = sheetRt.GetComponent<Image>();
            paperSheetImg.raycastTarget = true;

            var topBar = CreateBox(sheetRt, "RulesBar", Red);
            SetTopLeft(topBar, 0f, 0f, 720f, 12f);
            paperRulesTopBar = topBar.gameObject;

            var stampRt = CreateBox(sheetRt, "Stamp", new Color(Red.r, Red.g, Red.b, 0.14f));
            SetTopLeft(stampRt, 535f, 20f, 150f, 34f);
            stampRt.localRotation = Quaternion.Euler(0f, 0f, -9f);
            paperStampGo = stampRt.gameObject;
            var stampLabel = CreateText(stampRt, "StampText", "HANA MART", 15f, new Color(Red.r, Red.g, Red.b, 0.7f), TextAlignmentOptions.Center);
            stampLabel.fontStyle = FontStyles.Bold;
            stampLabel.characterSpacing = 2f;
            Stretch(stampLabel.rectTransform);

            paperTitleText = CreateText(sheetRt, "Title", "", 24f, PaperInk, TextAlignmentOptions.Left);
            paperTitleText.fontStyle = FontStyles.Bold;
            SetTopLeft(paperTitleText.rectTransform, 38f, 24f, 490f, 36f);

            paperBodyText = CreateText(sheetRt, "Body", "", 16f, PaperInk, TextAlignmentOptions.TopLeft);
            paperBodyText.enableAutoSizing = true;
            paperBodyText.fontSizeMin = 11f;
            paperBodyText.fontSizeMax = 16f;
            SetTopLeft(paperBodyText.rectTransform, 38f, 68f, 644f, 504f);

            paperCloseText = CreateText(sheetRt, "Close", "BẤM ĐỂ ĐÓNG · [Esc] / [E]", 13f, new Color(0.45f, 0.45f, 0.45f, 1f), TextAlignmentOptions.Left);
            paperCloseText.characterSpacing = 2f;
            SetAnchored(paperCloseText.rectTransform, new Vector2(0f, 0f), new Vector2(38f, 18f), new Vector2(460f, 22f));

            paperGo.SetActive(false);
        }

        void UpdateHud(float realDt)
        {
            if (!hudRoot) return;

            float targetLetter = game.Letterbox ? 720f * 0.11f : 0f;
            letterboxHeight = Mathf.Lerp(letterboxHeight, targetLetter, 1f - Mathf.Exp(-8f * realDt));
            if (letterTop) letterTop.sizeDelta = new Vector2(0f, letterboxHeight);
            if (letterBottom) letterBottom.sizeDelta = new Vector2(0f, letterboxHeight);

            if (flashT > 0f)
            {
                flashT = Mathf.Max(0f, flashT - realDt);
                if (flashImage) flashImage.color = new Color(Red.r, Red.g, Red.b, 0.45f * (flashT / 0.5f));
            }
            if (toastT > 0f && (toastT -= realDt) <= 0f && toastGo) toastGo.SetActive(false);
            if (bannerT > 0f && (bannerT -= realDt) <= 0f && bannerGo) bannerGo.SetActive(false);
            if (warnT > 0f && (warnT -= realDt) <= 0f && warnGo) warnGo.SetActive(false);

            bool showHud = Flow.State == AppState.Play && Flow.Screen == UiScreen.None && !game.Idle && (Save == null || !Save.Settings.recMode);
            HudVisible = showHud;
            if (hudElements) hudElements.SetActive(showHud);

            bool inPlay = Flow.State == AppState.Play && Flow.Screen == UiScreen.None && !game.Idle && game.Run != null;
            if (!inPlay)
            {
                if (subtitleText) subtitleText.text = "";
                if (bannerGo) bannerGo.SetActive(false);
                if (warnGo) warnGo.SetActive(false);
                if (dialogGo) dialogGo.SetActive(false);
                if (paperGo) paperGo.SetActive(false);
                return;
            }

            if (showHud)
            {
                if (clockText) clockText.text = game.ClockCaption ?? game.Run.Clock.Display;
                int strikes = game.Run.Progress.Strikes;
                for (int i = 0; i < 3; i++)
                    if (strikeBars[i]) strikeBars[i].color = i < strikes ? Red : new Color(0.05f, 0.08f, 0.08f, 0.85f);

                if (taskListText) taskListText.text = TaskListModel.FormatHud(game.Tasks.Items);
                if (TaskPanelOpen && taskPanelText) taskPanelText.text = TaskListModel.FormatPanel(game.Tasks.Items);

                bool showPrompt = interactor && interactor.Enabled && interactor.Current && !string.IsNullOrEmpty(interactor.Label);
                if (promptGo) promptGo.SetActive(showPrompt);
                if (showPrompt && promptText)
                {
                    string keyTag = Flow.LastDevice == InputDeviceKind.Gamepad ? "[A]" : "[E]";
                    promptText.text = $"{keyTag}  {interactor.Label}";
                }

                bool showHold = showPrompt && interactor.Progress > 0.001f;
                if (holdGo) holdGo.SetActive(showHold);
                if (showHold)
                {
                    if (holdFill) holdFill.sizeDelta = new Vector2(240f * Mathf.Clamp01(interactor.Progress), 0f);
                    if (holdText)
                    {
                        holdText.text = interactor.CurrentId switch
                        {
                            "monitor" => $"Đếm... {Mathf.Min(10, Mathf.FloorToInt(interactor.Progress * 10f) + 1)}",
                            "prints" => "Đang lau kính...",
                            "spill" => "Đang lau...",
                            _ => "...",
                        };
                    }
                }

                bool canSkipDeath = game.ActiveDeath?.CanSkip == true && Flow.Screen == UiScreen.None;
                bool canGreet = game.GreetingTime > 0f && game.Dialogue == null && game.PaperTitle == null && !game.Dead;
                if (greetPillGo) greetPillGo.SetActive(canSkipDeath || canGreet);
                if (greetPillText)
                {
                    if (canSkipDeath)
                        greetPillText.text = Flow.LastDevice == InputDeviceKind.Gamepad ? "[A] Bỏ qua" : "[E] Bỏ qua";
                    else if (canGreet)
                        greetPillText.text = Flow.LastDevice == InputDeviceKind.Gamepad ? "[Y] Chào khách" : "[G] Chào khách";
                }

                if (carryText)
                {
                    string carryName = carry && !string.IsNullOrEmpty(carry.Kind) ? CarryController.DisplayName(carry.Kind) : "";
                    carryText.text = string.IsNullOrEmpty(carryName) ? "" : "Đang cầm: " + carryName;
                }

                if (hintsText)
                {
                    hintsText.text = Flow.LastDevice == InputDeviceKind.Gamepad
                        ? "<b>[A]</b> Tương tác · <b>[RB]</b> Đặt đồ · <b>[Y]</b> Chào khách · <b>[B]</b> Ngồi thấp · <b>[X]</b> Nội quy · <b>[View]</b> Việc"
                        : "<b>E</b> Tương tác · <b>Q</b> Đặt đồ · <b>G</b> Chào khách · <b>C</b> Ngồi thấp · <b>R</b> Nội quy · <b>Tab</b> Sổ việc";
                }

                UpdateGuideHud();
            }

            if (subtitleText)
            {
                string sub = game.Subtitle;
                bool allowSub = Save == null || Save.Settings.subtitles || (!string.IsNullOrEmpty(sub) && sub.StartsWith("Bạn"));
                subtitleText.text = allowSub && !string.IsNullOrEmpty(sub) ? sub : "";
            }

            UpdatePaperHud();
            UpdateDialogHud();
        }

        void UpdateGuideHud()
        {
            var guide = GuideDirector.Evaluate(
                Save != null && Save.Settings.guide,
                game.Dead,
                game.HasFlag("red"),
                game.HasFlag("blackout"),
                game.Dialogue != null,
                game.PaperTitle != null,
                game.Tasks?.Items,
                carry ? carry.Kind : null,
                game.Restocking,
                player.Motor.pos.y,
                game.MicrowaveReady,
                game.ActiveMicrowaveOven,
                game.BackDoorOpen,
                game.CanServe);

            ActiveGuideTitle = guide?.Title;
            if (guide == null)
            {
                if (guideGo) guideGo.SetActive(false);
                if (guideArrowGo) guideArrowGo.SetActive(false);
                return;
            }

            Vector3? worldPos = game.HitWorldPosition(guide.HitId);
            string distSuffix = "";
            if (worldPos.HasValue && view)
            {
                Vector3 targetPos = worldPos.Value + Vector3.up * 0.4f;
                float dist = Vector3.Distance(targetPos, view.transform.position);
                if (dist >= 1.6f) distSuffix = $" · {dist:F0} m";

                Vector3 vp = view.WorldToViewportPoint(targetPos);
                bool behind = vp.z < 0f;
                float vx = behind ? 1f - vp.x : vp.x;
                float vy = behind ? 1f - vp.y : vp.y;
                bool offScreen = behind || vx < 0.04f || vx > 0.96f || vy < 0.06f || vy > 0.94f;
                if (guideArrowGo) guideArrowGo.SetActive(offScreen);
                if (offScreen && guideArrowRt)
                {
                    float dx = vx - 0.5f;
                    float dy = vy - 0.5f;
                    float angle = Mathf.Atan2(dy, dx);
                    float cx = Mathf.Clamp(0.5f + Mathf.Cos(angle) * 0.42f, 0.06f, 0.94f) * 1280f - 640f;
                    float cy = Mathf.Clamp(0.5f + Mathf.Sin(angle) * 0.40f, 0.10f, 0.90f) * 720f - 360f;
                    guideArrowRt.anchoredPosition = new Vector2(cx, cy);
                    guideArrowRt.localRotation = Quaternion.Euler(0f, 0f, angle * Mathf.Rad2Deg);
                }
            }
            else if (guideArrowGo)
            {
                guideArrowGo.SetActive(false);
            }

            if (guideGo) guideGo.SetActive(true);
            if (guideTitleText) guideTitleText.text = "◆ " + guide.Title;
            if (guideWhereText) guideWhereText.text = guide.Where + distSuffix;
        }

        void UpdatePaperHud()
        {
            bool open = game.PaperTitle != null && Flow.Screen == UiScreen.None;
            if (paperGo) paperGo.SetActive(open);
            if (!open) { lastPaperRef = null; return; }

            string key = game.PaperTitle + "|" + game.PaperText;
            if (lastPaperRef == key) return;
            lastPaperRef = key;

            if (paperRulesTopBar) paperRulesTopBar.SetActive(game.PaperIsRules);
            if (paperStampGo) paperStampGo.SetActive(game.PaperIsRules);
            if (paperTitleText) paperTitleText.text = game.PaperTitle;
            if (paperBodyText)
            {
                if (game.PaperIsRules)
                    paperBodyText.text = RulePaperFormatter.FormatRules(game.Run.Progress);
                else if (game.PaperIsHandwritten)
                    paperBodyText.text = $"<color=#24304a><i>{game.PaperText}</i></color>";
                else
                    paperBodyText.text = game.PaperText;
            }
            if (paperCloseText)
                paperCloseText.text = Flow.LastDevice == InputDeviceKind.Gamepad ? "BẤM [A] / [B] ĐỂ ĐÓNG" : "BẤM ĐỂ ĐÓNG · [Esc] / [E]";
        }

        void UpdateDialogHud()
        {
            bool open = game.Dialogue != null && Flow.Screen == UiScreen.None;
            if (dialogGo) dialogGo.SetActive(open);
            if (!open) { lastDialogRef = null; lastDialogCount = -1; return; }

            int count = game.Options != null ? game.Options.Length : 0;
            if (lastDialogRef != game.Dialogue || lastDialogCount != count)
            {
                lastDialogRef = game.Dialogue;
                lastDialogCount = count;
                dialogFocus.Reset(count);
            }

            if (dlgSpeakerText) dlgSpeakerText.text = game.Speaker ?? "";
            if (dlgBodyText) dlgBodyText.text = game.Dialogue ?? "";
            for (int i = 0; i < 3; i++)
            {
                bool active = i < count;
                if (dlgOptGos[i]) dlgOptGos[i].SetActive(active);
                if (active && dlgOptTexts[i])
                    dlgOptTexts[i].text = $"<b>[{i + 1}]</b>  {game.Options[i].Label}";
            }
            UpdateDialogFocusVisuals();
        }

        void UpdateDialogFocusVisuals()
        {
            int count = game.Options != null ? game.Options.Length : 0;
            for (int i = 0; i < 3; i++)
            {
                if (i >= count) continue;
                bool focused = i == dialogFocus.Index;
                if (dlgOptBars[i]) dlgOptBars[i].enabled = focused;
                if (dlgOptBgs[i]) dlgOptBgs[i].color = focused ? new Color(Teal.r, Teal.g, Teal.b, 0.14f) : new Color(0f, 0f, 0f, 0.001f);
                if (dlgOptTexts[i]) dlgOptTexts[i].color = focused ? Color.white : Muted;
            }
        }

        void ShowToast(string message)
        {
            if (!toastGo || !toastText) return;
            toastText.text = message;
            toastGo.SetActive(true);
            toastT = 3.2f;
        }

        void ShowBanner(string main, string sub)
        {
            if (!bannerGo) return;
            if (bannerMainText) bannerMainText.text = main;
            if (bannerSubText) bannerSubText.text = sub;
            bannerGo.SetActive(true);
            bannerT = 3.5f;
        }

        void ShowWarn(string title, string body)
        {
            if (!warnGo) return;
            if (warnTitleText) warnTitleText.text = title;
            if (warnBodyText) warnBodyText.text = body;
            warnGo.SetActive(true);
            warnT = 4f;
            flashT = 0.5f;
        }

        static void SetTopLeft(RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(w, h);
        }
    }
}
