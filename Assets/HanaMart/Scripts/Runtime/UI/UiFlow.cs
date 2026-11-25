using System;

namespace HanaMart.UI
{
    public enum AppState { Boot, Menu, Play, Pause }
    public enum UiScreen { Boot, Menu, Pause, Settings, Help, Death, Ending, None }
    public enum UiAction { None, Start, Resume, Retry, Settings, Help, Back, ToMenu, Quit }
    public enum InputDeviceKind { KeyboardMouse, Gamepad }

    public sealed class UiFlow
    {
        static readonly UiAction[] EmptyActions = Array.Empty<UiAction>();
        static readonly UiAction[] MenuActions = { UiAction.Start, UiAction.Settings, UiAction.Help, UiAction.Quit };
        static readonly UiAction[] PauseActions = { UiAction.Resume, UiAction.Retry, UiAction.Settings, UiAction.Help, UiAction.ToMenu };
        static readonly UiAction[] BackOnlyActions = { UiAction.Back };
        static readonly UiAction[] DeathActions = { UiAction.Retry, UiAction.ToMenu };
        static readonly UiAction[] EndingActions = { UiAction.ToMenu };

        public AppState State { get; private set; } = AppState.Boot;
        public UiScreen Screen { get; private set; } = UiScreen.Boot;
        public UiScreen BackScreen { get; private set; } = UiScreen.Menu;
        public int FocusIndex { get; private set; }
        public InputDeviceKind LastDevice { get; private set; } = InputDeviceKind.KeyboardMouse;

        public bool WorldPaused => State == AppState.Pause;
        public bool AttractCameraActive => State == AppState.Boot || State == AppState.Menu;

        public UiAction[] ScreenActions(UiScreen screen) => screen switch
        {
            UiScreen.Menu => MenuActions,
            UiScreen.Pause => PauseActions,
            UiScreen.Settings => BackOnlyActions,
            UiScreen.Help => BackOnlyActions,
            UiScreen.Death => DeathActions,
            UiScreen.Ending => EndingActions,
            _ => EmptyActions,
        };

        public bool GameplayAllowed(bool overlayOrEndActive) => State == AppState.Play && !overlayOrEndActive;

        public bool ShouldLockCursor(bool overlayOrEndActive, bool windowFocused = true) =>
            windowFocused && GameplayAllowed(overlayOrEndActive) && LastDevice == InputDeviceKind.KeyboardMouse;

        public bool AdvanceBoot()
        {
            if (State != AppState.Boot) return false;
            State = AppState.Menu;
            Screen = UiScreen.Menu;
            FocusIndex = 0;
            return true;
        }

        public void MoveFocus(int delta)
        {
            var actions = ScreenActions(Screen);
            if (actions.Length == 0) return;
            FocusIndex = ((FocusIndex + delta) % actions.Length + actions.Length) % actions.Length;
        }

        public void SetFocus(int index)
        {
            var actions = ScreenActions(Screen);
            if (index >= 0 && index < actions.Length) FocusIndex = index;
        }

        public UiAction Confirm()
        {
            var actions = ScreenActions(Screen);
            if (actions.Length == 0) return UiAction.None;
            int idx = Math.Clamp(FocusIndex, 0, actions.Length - 1);
            return Execute(actions[idx]);
        }

        public UiAction Execute(UiAction action)
        {
            switch (action)
            {
                case UiAction.Start:
                case UiAction.Retry:
                    EnterPlay();
                    break;
                case UiAction.Resume:
                    Resume();
                    break;
                case UiAction.Settings:
                    BackScreen = Screen;
                    Screen = UiScreen.Settings;
                    FocusIndex = 0;
                    break;
                case UiAction.Help:
                    BackScreen = Screen;
                    Screen = UiScreen.Help;
                    FocusIndex = 0;
                    break;
                case UiAction.Back:
                    if (Screen == UiScreen.Settings || Screen == UiScreen.Help)
                    {
                        Screen = BackScreen;
                        FocusIndex = 0;
                    }
                    break;
                case UiAction.ToMenu:
                    ToMenu();
                    break;
            }
            return action;
        }

        public UiAction Back()
        {
            if (Screen == UiScreen.Settings || Screen == UiScreen.Help)
                return Execute(UiAction.Back);
            if (Screen == UiScreen.Pause)
            {
                Resume();
                return UiAction.Resume;
            }
            return UiAction.None;
        }

        public bool TryPause(bool blocked)
        {
            if (State != AppState.Play || blocked) return false;
            State = AppState.Pause;
            Screen = UiScreen.Pause;
            FocusIndex = 0;
            return true;
        }

        public bool Resume()
        {
            if (State != AppState.Pause) return false;
            State = AppState.Play;
            Screen = UiScreen.None;
            FocusIndex = 0;
            return true;
        }

        public void EnterPlay()
        {
            State = AppState.Play;
            Screen = UiScreen.None;
            FocusIndex = 0;
        }

        public void ToMenu()
        {
            State = AppState.Menu;
            Screen = UiScreen.Menu;
            FocusIndex = 0;
        }

        public void SetDevice(InputDeviceKind device) => LastDevice = device;
    }
}
