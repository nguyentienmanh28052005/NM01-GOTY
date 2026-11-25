using System;
using System.Collections;
using System.IO;
using HanaMart.Interaction;
using HanaMart.Player;
using HanaMart.Shift;
using HanaMart.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace HanaMart.EditorTools
{
    public static class ShiftUiQA
    {
        const string Output = "Logs/HanaMart/ui-flow-smoke.json";

        [MenuItem("HanaMart/QA/Run Ui Flow")]
        public static void Run()
        {
            if (!Application.isPlaying) throw new InvalidOperationException("UI flow QA runs in Play mode.");
            Application.runInBackground = true;
            var camera = Camera.main;
            camera.GetComponent<PlayerController>().StartCoroutine(Check(camera));
        }

        [MenuItem("HanaMart/QA/Run Hud And Dialog")]
        public static void RunHudAndDialog()
        {
            if (!Application.isPlaying) throw new InvalidOperationException("HUD/Dialog QA runs in Play mode.");
            Application.runInBackground = true;
            var camera = Camera.main;
            camera.GetComponent<PlayerController>().StartCoroutine(CheckHudAndDialog(camera));
        }

        [MenuItem("HanaMart/QA/Run Settings And Presets")]
        public static void RunSettingsAndPresets()
        {
            if (!Application.isPlaying) throw new InvalidOperationException("Settings QA runs in Play mode.");
            Application.runInBackground = true;
            var camera = Camera.main;
            camera.GetComponent<PlayerController>().StartCoroutine(CheckSettingsAndPresets(camera));
        }

        static IEnumerator Check(Camera view)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Output));
            Directory.CreateDirectory("docs/visual/unity");
            File.Delete(Output);

            var game = view.GetComponent<ShiftGame>();
            var player = view.GetComponent<PlayerController>();
            var interactor = view.GetComponent<Interactor>();
            var ui = view.GetComponent<ShiftInterface>();
            if (!ui) throw new InvalidOperationException("ShiftInterface missing on Main Camera. Run HanaMart/Setup Shift.");

            string tempSaveDir = Path.Combine(Path.GetTempPath(), "HanaMartUiQA_" + Guid.NewGuid().ToString("N"));
            string tempSavePath = Path.Combine(tempSaveDir, SaveStore.FileName);
            Keyboard virtualKb = null;
            Gamepad virtualPad = null;
            var prevBg = InputSystem.settings.backgroundBehavior;
            var prevEditorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;

            try
            {
                yield return null;
                virtualKb = InputSystem.AddDevice<Keyboard>();
                InputSystem.EnableDevice(virtualKb);
                virtualKb.MakeCurrent();

                ui.ReloadSave(tempSavePath);
                yield return null;

                if (ui.Flow.State != AppState.Boot || !game.Idle || player.GameplayEnabled ||
                    Cursor.lockState != CursorLockMode.None || Mathf.Abs(Time.timeScale - 1f) > 0.01f)
                    throw new InvalidOperationException($"Boot state mismatch: state={ui.Flow.State}, idle={game.Idle}, gp={player.GameplayEnabled}, cursor={Cursor.lockState}, ts={Time.timeScale}");

                // Advance Boot -> Menu via virtual keyboard
                InputSystem.QueueStateEvent(virtualKb, new KeyboardState(Key.Space));
                yield return null;
                InputSystem.QueueStateEvent(virtualKb, new KeyboardState());
                yield return null;

                if (ui.Flow.State != AppState.Menu || ui.Flow.Screen != UiScreen.Menu || !game.Idle)
                    throw new InvalidOperationException("Boot did not advance to Menu.");

                yield return CaptureScreen("docs/visual/unity/p6-t1-menu.png");

                // Menu -> Settings -> Back -> Menu -> Start
                ui.ExecuteAction(UiAction.Settings);
                yield return null;
                if (ui.Flow.Screen != UiScreen.Settings || ui.Flow.BackScreen != UiScreen.Menu)
                    throw new InvalidOperationException("Menu -> Settings failed.");
                ui.ExecuteAction(UiAction.Back);
                yield return null;
                if (ui.Flow.Screen != UiScreen.Menu)
                    throw new InvalidOperationException("Settings -> Back did not return to Menu.");

                ui.ExecuteAction(UiAction.Start);
                yield return null;
                yield return null;
                if (ui.Flow.State != AppState.Play || game.Idle || !player.GameplayEnabled || !interactor.Enabled)
                    throw new InvalidOperationException("Start did not enter Play state cleanly.");

                double clockBefore = game.Run.Clock.Minutes;
                yield return new WaitForSecondsRealtime(0.25f);
                if (game.Run.Clock.Minutes <= clockBefore)
                    throw new InvalidOperationException("Shift clock did not advance in Play state.");

                // Play -> Pause via Escape key
                InputSystem.QueueStateEvent(virtualKb, new KeyboardState(Key.Escape));
                yield return null;
                InputSystem.QueueStateEvent(virtualKb, new KeyboardState());
                yield return null;

                if (ui.Flow.State != AppState.Pause || Mathf.Abs(Time.timeScale) > 0.001f || !game.Paused ||
                    player.GameplayEnabled || Cursor.lockState != CursorLockMode.None)
                    throw new InvalidOperationException($"Pause mismatch: state={ui.Flow.State}, ts={Time.timeScale}, paused={game.Paused}, gp={player.GameplayEnabled}");

                double pausedClock = game.Run.Clock.Minutes;
                yield return new WaitForSecondsRealtime(0.15f);
                if (Math.Abs(game.Run.Clock.Minutes - pausedClock) > 1e-6)
                    throw new InvalidOperationException("Clock advanced while paused.");

                yield return CaptureScreen("docs/visual/unity/p6-t1-pause.png");

                // Gamepad device switch & Resume (B button on Pause -> Resume)
                virtualPad = InputSystem.AddDevice<Gamepad>();
                InputSystem.EnableDevice(virtualPad);
                virtualPad.MakeCurrent();
                InputSystem.QueueStateEvent(virtualPad, new GamepadState { buttons = 1u << (int)GamepadButton.East });
                yield return null;
                InputSystem.QueueStateEvent(virtualPad, new GamepadState());
                yield return null;

                if (ui.Flow.LastDevice != InputDeviceKind.Gamepad || ui.Flow.State != AppState.Play ||
                    Mathf.Abs(Time.timeScale - 1f) > 0.01f || !player.GameplayEnabled)
                    throw new InvalidOperationException($"Gamepad resume mismatch: dev={ui.Flow.LastDevice}, state={ui.Flow.State}, ts={Time.timeScale}, gp={player.GameplayEnabled}");

                // Pause -> ToMenu
                ui.ExecuteAction(UiAction.ToMenu);
                yield return null;
                if (ui.Flow.State != AppState.Menu || !game.Idle || Mathf.Abs(Time.timeScale - 1f) > 0.01f || !ui.Flow.AttractCameraActive)
                    throw new InvalidOperationException("ToMenu did not reset to Menu state.");

                // Save persistence check for endings A and B in isolated temp path
                ui.Save.RecordEnding("A");
                ui.Save.RecordEnding("B");
                SaveStore.Save(ui.Save, tempSavePath);
                ui.ReloadSave(tempSavePath);
                if (ui.Save.UnlockedEndingsCount != 2)
                    throw new InvalidOperationException("Save reload did not persist both endings.");

                File.WriteAllText(Output, JsonUtility.ToJson(new QaResult
                {
                    passed = true,
                    endings = ui.Save.UnlockedEndingsCount,
                    deviceAfterGamepad = ui.Flow.LastDevice.ToString(),
                }, true));
            }
            finally
            {
                InputSystem.settings.backgroundBehavior = prevBg;
                InputSystem.settings.editorInputBehaviorInPlayMode = prevEditorInput;
                if (virtualKb != null) InputSystem.RemoveDevice(virtualKb);
                if (virtualPad != null) InputSystem.RemoveDevice(virtualPad);
                if (Directory.Exists(tempSaveDir)) Directory.Delete(tempSaveDir, true);
                Time.timeScale = 1f;
            }
        }

        static IEnumerator CaptureScreen(string path)
        {
            yield return new WaitForEndOfFrame();
            var shot = ScreenCapture.CaptureScreenshotAsTexture();
            if (shot)
            {
                File.WriteAllBytes(path, shot.EncodeToPNG());
                UnityEngine.Object.Destroy(shot);
            }
        }

        static IEnumerator CheckHudAndDialog(Camera view)
        {
            const string HudOutput = "Logs/HanaMart/hud-dialog-smoke.json";
            Directory.CreateDirectory(Path.GetDirectoryName(HudOutput));
            Directory.CreateDirectory("docs/visual/unity");
            File.Delete(HudOutput);

            var game = view.GetComponent<ShiftGame>();
            var player = view.GetComponent<PlayerController>();
            var interactor = view.GetComponent<Interactor>();
            var ui = view.GetComponent<ShiftInterface>();
            if (!ui) throw new InvalidOperationException("ShiftInterface missing on Main Camera.");

            string tempSaveDir = Path.Combine(Path.GetTempPath(), "HanaMartHudQA_" + Guid.NewGuid().ToString("N"));
            string tempSavePath = Path.Combine(tempSaveDir, SaveStore.FileName);
            Gamepad virtualPad = null;
            Keyboard virtualKb = null;
            var prevBg = InputSystem.settings.backgroundBehavior;
            var prevEditorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;

            try
            {
                yield return null;
                virtualKb = InputSystem.AddDevice<Keyboard>();
                InputSystem.EnableDevice(virtualKb);
                virtualKb.MakeCurrent();
                virtualPad = InputSystem.AddDevice<Gamepad>();
                InputSystem.EnableDevice(virtualPad);

                ui.ReloadSave(tempSavePath);
                ui.Save.Settings.guide = true;
                ui.ExecuteAction(UiAction.Start);
                yield return null;
                game.Run.Tick(1.3);
                game.Run.Clock.Running = false;
                yield return null;
                yield return null;

                if (!ui.HudVisible || string.IsNullOrEmpty(ui.ActiveGuideTitle))
                    throw new InvalidOperationException($"Play HUD or guide missing: hud={ui.HudVisible}, guide={ui.ActiveGuideTitle}");

                yield return CaptureScreen("docs/visual/unity/p6-t2-hud.png");

                // Rules paper with fake12, addendum, red, and learned note 15
                game.Run.Progress.Flags["fake12"] = 1;
                game.Run.Progress.Flags["addendum"] = 1;
                game.Run.Progress.Flags["red"] = 1;
                game.Run.Progress.RecordDeath(15);
                game.ShowPaper("NỘI QUY CA ĐÊM", RuleBook.Text(game.Run.Progress), isRules: true);
                yield return null;
                if (!game.PaperIsRules || player.GameplayEnabled)
                    throw new InvalidOperationException("Rules paper did not open or lock player.");
                yield return CaptureScreen("docs/visual/unity/p6-t2-rules.png");
                game.ClosePaper();
                yield return null;

                // Tab task panel (non-blocking)
                ui.ToggleTaskPanel(true);
                yield return null;
                if (!ui.TaskPanelOpen || !player.GameplayEnabled)
                    throw new InvalidOperationException("Task panel should stay non-blocking.");
                ui.ToggleTaskPanel(false);
                yield return null;

                // Dialogue navigation with virtual Gamepad (D-pad down -> select option 1)
                int picked = -1;
                game.ShowDialog("Khách thử nghiệm", "Bạn muốn chọn phương án nào?",
                    new ShiftGame.Choice("Phương án 1", () => picked = 0),
                    new ShiftGame.Choice("Phương án 2", () => picked = 1));
                yield return null;
                if (player.GameplayEnabled || ui.DialogFocusIndex != 0)
                    throw new InvalidOperationException("Dialogue did not lock gameplay or reset focus to 0.");

                virtualPad.MakeCurrent();
                InputSystem.QueueStateEvent(virtualPad, new GamepadState { buttons = 1u << (int)GamepadButton.DpadDown });
                yield return null;
                InputSystem.QueueStateEvent(virtualPad, new GamepadState());
                yield return null;
                if (ui.DialogFocusIndex != 1)
                    throw new InvalidOperationException($"Gamepad D-pad down did not move dialog focus (got {ui.DialogFocusIndex}).");

                yield return CaptureScreen("docs/visual/unity/p6-t2-dialog.png");

                InputSystem.QueueStateEvent(virtualPad, new GamepadState { buttons = 1u << (int)GamepadButton.South });
                yield return null;
                InputSystem.QueueStateEvent(virtualPad, new GamepadState());
                yield return null;
                if (picked != 1 || game.Dialogue != null || !player.GameplayEnabled)
                    throw new InvalidOperationException($"Dialogue confirm failed: picked={picked}, dlg={game.Dialogue}, gp={player.GameplayEnabled}");

                // Death recap screen (Rule 15) + Retry
                game.Die(15, "Bạn đã tính tiền cho khách không có bóng dưới chân.");
                double deadline = Time.realtimeSinceStartupAsDouble + 8;
                for (int frame = 0; frame < 3000 && ui.Flow.Screen != UiScreen.Death && Time.realtimeSinceStartupAsDouble < deadline; frame++)
                {
                    if (game.ActiveDeath?.CanSkip == true) game.SkipDeath();
                    yield return null;
                }
                if (ui.Flow.Screen != UiScreen.Death)
                    throw new InvalidOperationException("Death recap screen did not open.");
                yield return CaptureScreen("docs/visual/unity/p6-t2-death.png");

                ui.ExecuteAction(UiAction.Retry);
                yield return null;
                if (ui.Flow.State != AppState.Play || ui.Flow.Screen != UiScreen.None || game.Dead || !player.GameplayEnabled)
                    throw new InvalidOperationException("Death Retry did not restore Play state.");

                // Ending B screen + ToMenu
                game.Run.Progress.Flags["jiho"] = 1;
                game.Run.Clock.Reset(6);
                game.GetType().GetMethod("PresentEnding", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    ?.Invoke(game, new object[] { true });
                yield return null;
                if (ui.Flow.Screen != UiScreen.Ending || ui.Save.UnlockedEndingsCount != 1)
                    throw new InvalidOperationException($"Ending screen mismatch: screen={ui.Flow.Screen}, endings={ui.Save.UnlockedEndingsCount}");
                yield return CaptureScreen("docs/visual/unity/p6-t2-ending.png");

                ui.ExecuteAction(UiAction.ToMenu);
                yield return null;
                if (ui.Flow.Screen != UiScreen.Menu || !game.Idle)
                    throw new InvalidOperationException("Ending ToMenu did not return to Menu.");

                // Help screen & back
                ui.ExecuteAction(UiAction.Help);
                yield return null;
                if (ui.Flow.Screen != UiScreen.Help) throw new InvalidOperationException("Menu -> Help failed.");
                ui.ExecuteAction(UiAction.Back);
                yield return null;

                // World input isolation check while Menu/Pause is open
                if (player.GameplayEnabled || interactor.Enabled)
                    throw new InvalidOperationException("World input remained enabled in Menu.");
                InputSystem.QueueStateEvent(virtualKb, new KeyboardState(Key.R));
                yield return null;
                InputSystem.QueueStateEvent(virtualKb, new KeyboardState());
                yield return null;
                if (game.PaperTitle != null)
                    throw new InvalidOperationException("R key opened rules paper while in Menu.");

                File.WriteAllText(HudOutput, JsonUtility.ToJson(new HudQaResult
                {
                    passed = true,
                    dialogPicked = picked,
                    endings = ui.Save.UnlockedEndingsCount,
                }, true));
            }
            finally
            {
                InputSystem.settings.backgroundBehavior = prevBg;
                InputSystem.settings.editorInputBehaviorInPlayMode = prevEditorInput;
                if (virtualKb != null) InputSystem.RemoveDevice(virtualKb);
                if (virtualPad != null) InputSystem.RemoveDevice(virtualPad);
                if (Directory.Exists(tempSaveDir)) Directory.Delete(tempSaveDir, true);
                Time.timeScale = 1f;
            }
        }

        static IEnumerator CheckSettingsAndPresets(Camera view)
        {
            const string SettingsOutput = "Logs/HanaMart/settings-smoke.json";
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsOutput));
            Directory.CreateDirectory("docs/visual/unity");
            File.Delete(SettingsOutput);

            var game = view.GetComponent<ShiftGame>();
            var player = view.GetComponent<PlayerController>();
            var film = view.GetComponent<HanaMart.Render.FilmSettings>();
            var ui = view.GetComponent<ShiftInterface>();
            if (!ui) throw new InvalidOperationException("ShiftInterface missing on Main Camera.");

            string tempSaveDir = Path.Combine(Path.GetTempPath(), "HanaMartSettingsQA_" + Guid.NewGuid().ToString("N"));
            string tempSavePath = Path.Combine(tempSaveDir, SaveStore.FileName);
            Keyboard virtualKb = null;
            var prevBg = InputSystem.settings.backgroundBehavior;
            var prevEditorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;

            try
            {
                yield return null;
                virtualKb = InputSystem.AddDevice<Keyboard>();
                InputSystem.EnableDevice(virtualKb);
                virtualKb.MakeCurrent();

                ui.ReloadSave(tempSavePath);
                ui.ExecuteAction(UiAction.Start);
                yield return null;
                game.Run.Clock.Running = false;
                yield return null;

                // Pause -> Settings mid-shift
                ui.Flow.TryPause(false);
                ui.GetType().GetMethod("SyncState", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?.Invoke(ui, null);
                ui.ExecuteAction(UiAction.Settings);
                yield return null;
                if (ui.Flow.Screen != UiScreen.Settings || ui.Flow.BackScreen != UiScreen.Pause)
                    throw new InvalidOperationException("Pause -> Settings failed.");

                ui.SelectSettingsTab(SettingsTab.Gfx);
                yield return null;
                yield return CaptureScreen("docs/visual/unity/p6-t3-settings-gfx.png");

                ui.SelectSettingsTab(SettingsTab.Audio);
                yield return null;
                yield return CaptureScreen("docs/visual/unity/p6-t3-settings-audio.png");

                ui.SelectSettingsTab(SettingsTab.Ctrl);
                yield return null;
                yield return CaptureScreen("docs/visual/unity/p6-t3-settings-ctrl.png");

                // Modify settings across all 3 tabs
                ui.SelectSettingsTab(SettingsTab.Gfx);
                ui.StepSettingRow(0, 3); // medium -> cine
                for (int i = 0; i < 8; i++) ui.StepSettingRow(3, 1); // fov 72 -> 88
                for (int i = 0; i < 4; i++) ui.StepSettingRow(4, 1); // brightness 1.0 -> 1.2
                ui.StepSettingRow(5, 1); // grain false

                ui.SelectSettingsTab(SettingsTab.Audio);
                for (int i = 0; i < 8; i++) ui.StepSettingRow(0, -1); // master 0.9 -> 0.5
                for (int i = 0; i < 8; i++) ui.StepSettingRow(1, -1); // amb 0.8 -> 0.4
                for (int i = 0; i < 6; i++) ui.StepSettingRow(2, -1); // sfx 0.9 -> 0.6
                ui.StepSettingRow(3, 1); // subtitles false

                ui.SelectSettingsTab(SettingsTab.Ctrl);
                for (int i = 0; i < 8; i++) ui.StepSettingRow(0, 1); // mouseSens 1.0 -> 1.8
                ui.StepSettingRow(2, 1); // guide true
                ui.StepSettingRow(3, 1); // invertY true
                ui.StepSettingRow(4, 1); // rumble false
                yield return null;

                if (player.BaseFov != 88f || Mathf.Abs(film.bright - 1.2f) > 0.02f || Mathf.Abs(film.grain - 0.01f) > 0.002f ||
                    Mathf.Abs(film.cine - 1f) > 0.01f || Mathf.Abs(AudioListener.volume - 0.5f) > 0.02f ||
                    Mathf.Abs(game.AmbientVolume - 0.4f) > 0.02f || Mathf.Abs(game.SfxVolume - 0.6f) > 0.02f ||
                    !player.settings.invertY || player.RumbleEnabled)
                    throw new InvalidOperationException("Live settings did not apply to runtime components.");

                // Reload from disk and verify persistence
                ui.ReloadSave(tempSavePath);
                yield return null;
                if (ui.Save.Settings.quality != "cine" || ui.Save.Settings.fov != 88 || !ui.Save.Settings.guide || !ui.Save.Settings.invertY)
                    throw new InvalidOperationException("Settings did not persist across reload.");

                // Reset defaults and verify
                ui.ResetSettingsToDefault();
                yield return null;
                ui.ReloadSave(tempSavePath);
                if (ui.Save.Settings.quality != "medium" || ui.Save.Settings.fov != 72 || ui.Save.Settings.guide ||
                    ui.Save.Settings.invertY || !ui.Save.Settings.rumble || Mathf.Abs(ui.Save.Settings.master - 0.9f) > 0.01f)
                    throw new InvalidOperationException("ResetSettingsToDefault did not restore hana24_v3 defaults.");

                // Resume Play -> F3 perf overlay -> H recording mode
                ui.ExecuteAction(UiAction.Back);
                ui.ExecuteAction(UiAction.Resume);
                yield return null;

                InputSystem.QueueStateEvent(virtualKb, new KeyboardState(Key.F3));
                yield return null;
                InputSystem.QueueStateEvent(virtualKb, new KeyboardState());
                yield return new WaitForSecondsRealtime(0.6f);
                if (!ui.PerfOverlayVisible)
                    throw new InvalidOperationException("F3 did not toggle performance overlay.");
                yield return CaptureScreen("docs/visual/unity/p6-t3-f3-hud.png");

                InputSystem.QueueStateEvent(virtualKb, new KeyboardState(Key.H));
                yield return null;
                InputSystem.QueueStateEvent(virtualKb, new KeyboardState());
                yield return null;
                if (!ui.Save.Settings.recMode || ui.HudVisible)
                    throw new InvalidOperationException("H did not toggle recording mode (hide HUD).");

                // Cleanly turn off F3 and recMode
                ui.TogglePerfOverlay();
                ui.ToggleRecMode();
                ui.ExecuteAction(UiAction.ToMenu);
                yield return null;

                File.WriteAllText(SettingsOutput, JsonUtility.ToJson(new SettingsQaResult
                {
                    passed = true,
                    effectiveScaleAtReset = ui.EffectiveRenderScale,
                }, true));
            }
            finally
            {
                InputSystem.settings.backgroundBehavior = prevBg;
                InputSystem.settings.editorInputBehaviorInPlayMode = prevEditorInput;
                if (virtualKb != null) InputSystem.RemoveDevice(virtualKb);
                if (Directory.Exists(tempSaveDir)) Directory.Delete(tempSaveDir, true);
                Time.timeScale = 1f;
            }
        }

        [Serializable]
        sealed class SettingsQaResult { public bool passed; public float effectiveScaleAtReset; }
        [Serializable]
        sealed class HudQaResult { public bool passed; public int dialogPicked, endings; }
        [Serializable]
        sealed class QaResult { public bool passed; public int endings; public string deviceAfterGamepad; }
    }
}
