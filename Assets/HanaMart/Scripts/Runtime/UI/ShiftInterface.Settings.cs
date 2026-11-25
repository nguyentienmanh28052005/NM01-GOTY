using System;
using System.Collections.Generic;
using HanaMart.Render;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace HanaMart.UI
{
    public sealed partial class ShiftInterface
    {
        public SettingsTab ActiveSettingsTab { get; private set; } = SettingsTab.Gfx;
        public bool PerfOverlayVisible { get; private set; }
        public float EffectiveRenderScale { get; private set; } = 1f;

        readonly DynamicResolutionController dynRes = new();
        readonly Dictionary<SettingsTab, Image> tabUnderscores = new();
        readonly Dictionary<SettingsTab, TextMeshProUGUI> tabLabels = new();
        readonly List<SettingsRowView> settingsRows = new();

        GameObject perfOverlayGo;
        TextMeshProUGUI perfOverlayText;
        float perfAccTime;
        int perfFrames;
        int lastNavX;
        float nextNavXAt;

        float initialUrpScale = 1f;
        AnisotropicFiltering initialAniso;
        float initialListenerVolume = 1f;
        bool savedGlobals;

        sealed class SettingsRowView
        {
            public GameObject Go;
            public Image Bg, Bar, Track, Fill;
            public TextMeshProUGUI Label, Value, LeftBtn, RightBtn;
        }

        void SnapshotGlobals()
        {
            if (savedGlobals) return;
            savedGlobals = true;
            var urp = UniversalRenderPipeline.asset;
            if (urp) initialUrpScale = urp.renderScale;
            initialAniso = QualitySettings.anisotropicFiltering;
            initialListenerVolume = AudioListener.volume;
        }

        void RestoreGlobals()
        {
            if (!savedGlobals) return;
            var urp = UniversalRenderPipeline.asset;
            if (urp) urp.renderScale = initialUrpScale;
            QualitySettings.anisotropicFiltering = initialAniso;
            AudioListener.volume = initialListenerVolume;
        }

        public void SelectSettingsTab(SettingsTab tab)
        {
            ActiveSettingsTab = tab;
            Flow.SetFocus(0);
            settingsFocusIdx = 0;
            RenderSettingsTab();
        }

        public void StepSettingRow(int rowIndex, int delta)
        {
            if (Save == null) return;
            var rows = SettingsModel.GetRows(ActiveSettingsTab);
            if (rowIndex < 0 || rowIndex >= rows.Length) return;
            var spec = rows[rowIndex];

            if (spec.Kind == SettingsRowKind.Fullscreen)
            {
                Screen.fullScreen = !Screen.fullScreen;
                game.PlayClip("ui_ok");
                return;
            }
            if (spec.Kind == SettingsRowKind.Reset)
            {
                ResetSettingsToDefault();
                return;
            }
            if (spec.Kind == SettingsRowKind.Back)
            {
                game.PlayClip("ui_back");
                ExecuteAction(UiAction.Back);
                return;
            }

            if (SettingsModel.StepRow(ActiveSettingsTab, rowIndex, delta, Save.Settings))
            {
                SaveStore.Save(Save, SavePathOverride);
                ApplyAllSettings();
                RenderSettingsTab();
                game.PlayClip("ui_move");
            }
        }

        public void ResetSettingsToDefault()
        {
            if (Save == null) return;
            SettingsModel.ResetDefaults(Save.Settings);
            dynRes.Reset();
            SaveStore.Save(Save, SavePathOverride);
            ApplyAllSettings();
            RenderSettingsTab();
            ShowToast("Đã đặt lại cài đặt mặc định.");
            game.PlayClip("ui_ok");
        }

        public void TogglePerfOverlay()
        {
            PerfOverlayVisible = !PerfOverlayVisible;
            if (perfOverlayGo) perfOverlayGo.SetActive(PerfOverlayVisible);
            perfAccTime = 0f;
            perfFrames = 0;
        }

        public void ToggleRecMode()
        {
            if (Save == null) return;
            Save.Settings.recMode = !Save.Settings.recMode;
            SaveStore.Save(Save, SavePathOverride);
            ShowToast(Save.Settings.recMode ? "Chế độ quay video: BẬT (H để tắt)" : "Chế độ quay video: TẮT");
        }

        public void ApplyAllSettings()
        {
            if (Save == null) return;
            var s = Save.Settings;
            var preset = GraphicsPresets.For(s.quality);

            if (player)
            {
                player.BaseFov = s.fov;
                player.RumbleEnabled = s.rumble;
                player.settings.mouseSens = s.mouseSens;
                player.settings.padSens = s.padSens;
                player.settings.invertY = s.invertY;
                player.settings.headBob = s.headBob;
                player.settings.smoothCam = s.smoothCam;
                if (!player.ExternalView && view) view.fieldOfView = player.CurrentFov;
            }

            AudioListener.volume = Mathf.Clamp01(s.master);
            if (game)
            {
                game.AmbientVolume = s.amb;
                game.SfxVolume = s.sfx;
                game.ApplyLightPreset(preset.AreaLights, preset.ExtraLights);
            }

            var film = view ? view.GetComponent<FilmSettings>() : null;
            if (film && (!game || !game.Dead))
            {
                film.bright = s.brightness;
                film.grain = s.grain ? 0.045f : 0.01f;
                film.cine = preset.Cine > 0f ? preset.Cine : (s.cineFx ? 0.7f : 0f);
                film.Push();
            }

            EffectiveRenderScale = SettingsModel.ComputeEffectiveScale(s.quality, s.renderScale, dynRes.Scale);
            var urp = UniversalRenderPipeline.asset;
            if (urp) urp.renderScale = EffectiveRenderScale;
            if (view)
            {
                var camData = view.GetUniversalAdditionalCameraData();
                if (camData)
                {
                    camData.antialiasing = preset.Smaa
                        ? AntialiasingMode.SubpixelMorphologicalAntiAliasing
                        : AntialiasingMode.None;
                }
            }
            QualitySettings.anisotropicFiltering = preset.Aniso >= 16
                ? AnisotropicFiltering.ForceEnable
                : AnisotropicFiltering.Enable;
        }

        int settingsFocusIdx;

        void BuildSettingsScreen()
        {
            var panel = CreateScreen(UiScreen.Settings, new Color(0.015f, 0.031f, 0.031f, 0.92f));
            var title = CreateText(panel.transform, "Title", "Cài đặt", 38f, Bright, TextAlignmentOptions.Left);
            title.fontStyle = FontStyles.Bold;
            SetAnchored(title.rectTransform, new Vector2(0f, 1f), new Vector2(130f, -54f), new Vector2(400f, 48f));

            // 3 Tabs
            var tabs = new[]
            {
                (SettingsTab.Gfx, "Đồ họa", 130f),
                (SettingsTab.Audio, "Âm thanh", 268f),
                (SettingsTab.Ctrl, "Điều khiển", 418f),
            };
            foreach (var (tab, labelText, x) in tabs)
            {
                var tabGo = new GameObject("Tab_" + tab, typeof(RectTransform), typeof(Image), typeof(Button));
                tabGo.transform.SetParent(panel.transform, false);
                SetTopLeft(tabGo.GetComponent<RectTransform>(), x, 108f, 130f, 38f);
                tabGo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.001f);
                var btn = tabGo.GetComponent<Button>();
                btn.transition = Selectable.Transition.None;
                btn.navigation = new Navigation { mode = Navigation.Mode.None };

                var lbl = CreateText(tabGo.transform, "Label", labelText, 17f, Muted, TextAlignmentOptions.Center);
                lbl.fontStyle = FontStyles.Bold;
                Stretch(lbl.rectTransform);
                tabLabels[tab] = lbl;

                var barRt = CreateBox(tabGo.transform, "ActiveBar", Teal);
                barRt.anchorMin = new Vector2(0f, 0f);
                barRt.anchorMax = new Vector2(1f, 0f);
                barRt.pivot = new Vector2(0.5f, 0f);
                barRt.anchoredPosition = Vector2.zero;
                barRt.sizeDelta = new Vector2(0f, 3f);
                tabUnderscores[tab] = barRt.GetComponent<Image>();

                var relay = tabGo.AddComponent<PointerRelay>();
                relay.Clicked = () =>
                {
                    SelectSettingsTab(tab);
                    game.PlayClip("ui_move");
                };
            }

            var tabHint = CreateText(panel.transform, "TabHint", "Q / E hoặc LB / RB để đổi tab", 13f, FooterText, TextAlignmentOptions.Right);
            SetAnchored(tabHint.rectTransform, new Vector2(0f, 1f), new Vector2(730f, -118f), new Vector2(420f, 24f));

            // Pool 13 rows (max rows on Gfx tab)
            for (int i = 0; i < 13; i++)
            {
                int idx = i;
                var rowRt = CreateBox(panel.transform, "SetRow" + i, new Color(0f, 0f, 0f, 0.001f));
                SetTopLeft(rowRt, 130f, 158f + i * 36f, 1020f, 34f);
                var bg = rowRt.GetComponent<Image>();
                bg.raycastTarget = true;

                var barRt = CreateBox(rowRt, "FocusBar", Teal);
                SetTopLeft(barRt, 0f, 4f, 4f, 26f);
                var bar = barRt.GetComponent<Image>();
                bar.enabled = false;

                var label = CreateText(rowRt, "Label", "", 17f, Muted, TextAlignmentOptions.Left);
                SetTopLeft(label.rectTransform, 18f, 3f, 540f, 28f);

                var leftBtn = CreateText(rowRt, "Left", "◀", 14f, Teal, TextAlignmentOptions.Center);
                SetTopLeft(leftBtn.rectTransform, 610f, 3f, 28f, 28f);
                leftBtn.raycastTarget = true;
                var leftRelay = leftBtn.gameObject.AddComponent<PointerRelay>();
                leftRelay.Clicked = () => { settingsFocusIdx = idx; StepSettingRow(idx, -1); };

                var trackRt = CreateBox(rowRt, "Track", new Color(1f, 1f, 1f, 0.16f));
                SetTopLeft(trackRt, 648f, 15f, 150f, 4f);
                var fillRt = CreateBox(trackRt, "Fill", Teal);
                fillRt.anchorMin = new Vector2(0f, 0f);
                fillRt.anchorMax = new Vector2(0f, 1f);
                fillRt.pivot = new Vector2(0f, 0.5f);
                fillRt.anchoredPosition = Vector2.zero;
                fillRt.sizeDelta = new Vector2(0f, 0f);

                var val = CreateText(rowRt, "Value", "", 16.5f, Color.white, TextAlignmentOptions.Center);
                val.fontStyle = FontStyles.Bold;
                SetTopLeft(val.rectTransform, 806f, 3f, 174f, 28f);

                var rightBtn = CreateText(rowRt, "Right", "▶", 14f, Teal, TextAlignmentOptions.Center);
                SetTopLeft(rightBtn.rectTransform, 984f, 3f, 28f, 28f);
                rightBtn.raycastTarget = true;
                var rightRelay = rightBtn.gameObject.AddComponent<PointerRelay>();
                rightRelay.Clicked = () => { settingsFocusIdx = idx; StepSettingRow(idx, 1); };

                var relay = rowRt.gameObject.AddComponent<PointerRelay>();
                relay.Entered = () =>
                {
                    if (Flow.Screen != UiScreen.Settings) return;
                    settingsFocusIdx = idx;
                    UpdateSettingsFocusVisuals();
                };
                relay.Clicked = () =>
                {
                    if (Flow.Screen != UiScreen.Settings) return;
                    settingsFocusIdx = idx;
                    StepSettingRow(idx, 1);
                };

                settingsRows.Add(new SettingsRowView
                {
                    Go = rowRt.gameObject,
                    Bg = bg,
                    Bar = bar,
                    Track = trackRt.GetComponent<Image>(),
                    Fill = fillRt.GetComponent<Image>(),
                    Label = label,
                    Value = val,
                    LeftBtn = leftBtn,
                    RightBtn = rightBtn,
                });
            }

            AddFooterPrompts(panel.transform);

            // F3 perf overlay (top-right on hudRoot)
            var perfRt = CreateBox(hudRoot.transform, "PerfOverlay", new Color(0f, 0f, 0f, 0.62f));
            SetAnchored(perfRt, new Vector2(1f, 1f), new Vector2(-12f, -10f), new Vector2(460f, 26f));
            perfOverlayGo = perfRt.gameObject;
            perfOverlayText = CreateText(perfRt, "Text", "", 13f, Cyan, TextAlignmentOptions.Right);
            SetTopLeft(perfOverlayText.rectTransform, 8f, 3f, 444f, 20f);
            perfOverlayGo.SetActive(false);

            RenderSettingsTab();
        }

        void RenderSettingsTab()
        {
            if (settingsRows.Count == 0 || Save == null) return;
            foreach (var kv in tabUnderscores)
                if (kv.Value) kv.Value.enabled = kv.Key == ActiveSettingsTab;
            foreach (var kv in tabLabels)
                if (kv.Value) kv.Value.color = kv.Key == ActiveSettingsTab ? Color.white : Muted;

            var specs = SettingsModel.GetRows(ActiveSettingsTab);
            settingsFocusIdx = Mathf.Clamp(settingsFocusIdx, 0, Mathf.Max(0, specs.Length - 1));

            for (int i = 0; i < settingsRows.Count; i++)
            {
                var row = settingsRows[i];
                bool active = i < specs.Length;
                row.Go.SetActive(active);
                if (!active) continue;

                var spec = specs[i];
                row.Label.text = spec.Label;
                row.Value.text = SettingsModel.FormatValue(ActiveSettingsTab, i, Save.Settings);

                bool adjustable = spec.Kind == SettingsRowKind.Enum || spec.Kind == SettingsRowKind.Range || spec.Kind == SettingsRowKind.Bool;
                row.LeftBtn.gameObject.SetActive(adjustable);
                row.RightBtn.gameObject.SetActive(adjustable);

                float fill = SettingsModel.NormalizedFill(ActiveSettingsTab, i, Save.Settings);
                bool showBar = fill >= 0f;
                row.Track.gameObject.SetActive(showBar);
                if (showBar) row.Fill.rectTransform.sizeDelta = new Vector2(150f * Mathf.Clamp01(fill), 0f);
            }
            UpdateSettingsFocusVisuals();
        }

        void UpdateSettingsFocusVisuals()
        {
            var specs = SettingsModel.GetRows(ActiveSettingsTab);
            for (int i = 0; i < settingsRows.Count && i < specs.Length; i++)
            {
                bool focused = i == settingsFocusIdx;
                var row = settingsRows[i];
                row.Bar.enabled = focused;
                row.Bg.color = focused ? new Color(Teal.r, Teal.g, Teal.b, 0.13f) : new Color(0f, 0f, 0f, 0.001f);
                row.Label.color = focused ? Color.white : Muted;
            }
        }

        void HandleSettingsInput()
        {
            if (tabPrevInput?.WasPressedThisFrame() == true)
            {
                SelectSettingsTab((SettingsTab)(((int)ActiveSettingsTab + 2) % 3));
                game.PlayClip("ui_move");
                return;
            }
            if (tabNextInput?.WasPressedThisFrame() == true)
            {
                SelectSettingsTab((SettingsTab)(((int)ActiveSettingsTab + 1) % 3));
                game.PlayClip("ui_move");
                return;
            }

            var specs = SettingsModel.GetRows(ActiveSettingsTab);
            int dirY = ReadNavStep();
            if (dirY != 0 && specs.Length > 0)
            {
                settingsFocusIdx = ((settingsFocusIdx + dirY) % specs.Length + specs.Length) % specs.Length;
                game.PlayClip("ui_move");
                UpdateSettingsFocusVisuals();
            }

            int dirX = ReadHorizontalStep();
            if (dirX != 0)
            {
                StepSettingRow(settingsFocusIdx, dirX);
                return;
            }

            if (confirmInput?.WasPressedThisFrame() == true)
            {
                StepSettingRow(settingsFocusIdx, 1);
                return;
            }

            if (backInput?.WasPressedThisFrame() == true)
            {
                game.PlayClip("ui_back");
                ExecuteAction(UiAction.Back);
            }
        }

        int ReadHorizontalStep()
        {
            if (navInput == null) return 0;
            Vector2 v = navInput.ReadValue<Vector2>();
            int dir = v.x > 0.45f ? 1 : v.x < -0.45f ? -1 : 0;
            if (dir == 0) { lastNavX = 0; return 0; }
            float now = Time.unscaledTime;
            if (dir != lastNavX) { lastNavX = dir; nextNavXAt = now + 0.30f; return dir; }
            if (now >= nextNavXAt) { nextNavXAt = now + 0.12f; return dir; }
            return 0;
        }

        void UpdateSettingsRuntime(float realDt)
        {
            if (Save == null) return;
            bool allowDynRes = Flow.State == AppState.Play || Flow.State == AppState.Menu;
            if (allowDynRes && dynRes.Tick(realDt, Save.Settings.dynRes))
            {
                EffectiveRenderScale = SettingsModel.ComputeEffectiveScale(Save.Settings.quality, Save.Settings.renderScale, dynRes.Scale);
                var urp = UniversalRenderPipeline.asset;
                if (urp) urp.renderScale = EffectiveRenderScale;
            }

            if (PerfOverlayVisible && perfOverlayText)
            {
                perfAccTime += realDt;
                perfFrames++;
                if (perfAccTime >= 0.5f)
                {
                    int fps = Mathf.RoundToInt(perfFrames / perfAccTime);
                    perfAccTime = 0f;
                    perfFrames = 0;
                    perfOverlayText.text = PerfOverlayModel.Format(
                        fps, Screen.width, Screen.height, EffectiveRenderScale,
                        GraphicsPresets.For(Save.Settings.quality).Label);
                }
            }
        }
    }
}
