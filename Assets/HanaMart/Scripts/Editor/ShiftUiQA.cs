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

        [Serializable]
        sealed class QaResult { public bool passed; public int endings; public string deviceAfterGamepad; }
    }
}
