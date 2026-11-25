using System.Linq;
using HanaMart.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.InputSystem;

namespace HanaMart.Tests
{
    [Timeout(10000)]
    public class UiFlowTests
    {
        [Test]
        public void StartsInBootAndAdvancesToMenu()
        {
            var flow = new UiFlow();
            Assert.AreEqual(AppState.Boot, flow.State);
            Assert.AreEqual(UiScreen.Boot, flow.Screen);
            Assert.IsFalse(flow.WorldPaused);
            Assert.IsFalse(flow.GameplayAllowed(false));
            Assert.IsFalse(flow.ShouldLockCursor(false));

            Assert.IsTrue(flow.AdvanceBoot());
            Assert.AreEqual(AppState.Menu, flow.State);
            Assert.AreEqual(UiScreen.Menu, flow.Screen);
            Assert.AreEqual(0, flow.FocusIndex);
            Assert.IsFalse(flow.AdvanceBoot());
        }

        [Test]
        public void MenuNavigationWrapsAndTransitionsSettingsHelpBackStart()
        {
            var flow = new UiFlow();
            flow.AdvanceBoot();
            Assert.AreEqual(0, flow.FocusIndex);

            flow.MoveFocus(-1);
            Assert.AreEqual(3, flow.FocusIndex);
            flow.MoveFocus(1);
            Assert.AreEqual(0, flow.FocusIndex);

            flow.MoveFocus(1); // Settings
            Assert.AreEqual(UiAction.Settings, flow.Confirm());
            Assert.AreEqual(UiScreen.Settings, flow.Screen);
            Assert.AreEqual(UiScreen.Menu, flow.BackScreen);
            Assert.AreEqual(UiAction.Back, flow.Back());
            Assert.AreEqual(UiScreen.Menu, flow.Screen);

            flow.SetFocus(2); // Help
            Assert.AreEqual(UiAction.Help, flow.Confirm());
            Assert.AreEqual(UiScreen.Help, flow.Screen);
            Assert.AreEqual(UiScreen.Menu, flow.BackScreen);
            Assert.AreEqual(UiAction.Back, flow.Back());
            Assert.AreEqual(UiScreen.Menu, flow.Screen);

            flow.SetFocus(0); // Start
            Assert.AreEqual(UiAction.Start, flow.Confirm());
            Assert.AreEqual(AppState.Play, flow.State);
            Assert.AreEqual(UiScreen.None, flow.Screen);
            Assert.IsTrue(flow.GameplayAllowed(false));
            Assert.IsTrue(flow.ShouldLockCursor(false));
        }

        [Test]
        public void PauseFreezesWorldUnlocksCursorAndResumeRestores()
        {
            var flow = new UiFlow();
            flow.AdvanceBoot();
            flow.Confirm(); // Start

            Assert.IsTrue(flow.TryPause(false));
            Assert.AreEqual(AppState.Pause, flow.State);
            Assert.AreEqual(UiScreen.Pause, flow.Screen);
            Assert.IsTrue(flow.WorldPaused);
            Assert.IsFalse(flow.GameplayAllowed(false));
            Assert.IsFalse(flow.ShouldLockCursor(false));

            flow.SetFocus(2); // Settings
            Assert.AreEqual(UiAction.Settings, flow.Confirm());
            Assert.AreEqual(UiScreen.Settings, flow.Screen);
            Assert.AreEqual(UiScreen.Pause, flow.BackScreen);
            Assert.IsTrue(flow.WorldPaused);

            Assert.AreEqual(UiAction.Back, flow.Back());
            Assert.AreEqual(UiScreen.Pause, flow.Screen);
            Assert.IsTrue(flow.WorldPaused);

            Assert.AreEqual(UiAction.Resume, flow.Back());
            Assert.AreEqual(AppState.Play, flow.State);
            Assert.AreEqual(UiScreen.None, flow.Screen);
            Assert.IsFalse(flow.WorldPaused);
            Assert.IsTrue(flow.GameplayAllowed(false));
            Assert.IsTrue(flow.ShouldLockCursor(false));
        }

        [Test]
        public void PauseBlockedWhenOverlayOrEndStateActive()
        {
            var flow = new UiFlow();
            flow.EnterPlay();
            Assert.IsFalse(flow.TryPause(true));
            Assert.AreEqual(AppState.Play, flow.State);
            Assert.AreEqual(UiScreen.None, flow.Screen);
        }

        [Test]
        public void ToMenuFromPauseResetsStateAndUnpausesWorld()
        {
            var flow = new UiFlow();
            flow.EnterPlay();
            Assert.IsTrue(flow.TryPause(false));
            flow.ToMenu();

            Assert.AreEqual(AppState.Menu, flow.State);
            Assert.AreEqual(UiScreen.Menu, flow.Screen);
            Assert.IsFalse(flow.WorldPaused);
            Assert.IsFalse(flow.GameplayAllowed(false));
            Assert.IsFalse(flow.ShouldLockCursor(false));
        }

        [Test]
        public void DeviceSwitchingSyncsCursorLockAndPromptMode()
        {
            var flow = new UiFlow();
            flow.EnterPlay();

            flow.SetDevice(InputDeviceKind.Gamepad);
            Assert.AreEqual(InputDeviceKind.Gamepad, flow.LastDevice);
            Assert.IsFalse(flow.ShouldLockCursor(false));

            flow.SetDevice(InputDeviceKind.KeyboardMouse);
            Assert.AreEqual(InputDeviceKind.KeyboardMouse, flow.LastDevice);
            Assert.IsTrue(flow.ShouldLockCursor(false));
            Assert.IsFalse(flow.ShouldLockCursor(true));
        }

        [Test]
        public void ControlsAssetContainsPlayerAndUiKeyboardGamepadBindings()
        {
            var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/HanaMart/Input/HanaMartControls.inputactions");
            Assert.IsNotNull(asset);

            var playerMap = asset.FindActionMap("Player", false);
            Assert.IsNotNull(playerMap);
            foreach (string name in new[] { "Move", "Look", "Use", "Pause", "Confirm", "Rules", "Tasks", "HideUI", "Debug" })
                Assert.IsNotNull(playerMap.FindAction(name, false), "Missing Player action " + name);

            var uiMap = asset.FindActionMap("UI", false);
            Assert.IsNotNull(uiMap, "Missing UI action map");
            foreach (string name in new[] { "UiNavigate", "UiConfirm", "UiBack", "UiTabPrev", "UiTabNext" })
            {
                var action = uiMap.FindAction(name, false);
                Assert.IsNotNull(action, "Missing UI action " + name);
                Assert.IsTrue(action.bindings.Any(b => b.path.Contains("<Keyboard>")), name + " missing Keyboard binding");
                Assert.IsTrue(action.bindings.Any(b => b.path.Contains("<Gamepad>")), name + " missing Gamepad binding");
            }
        }
    }
}
