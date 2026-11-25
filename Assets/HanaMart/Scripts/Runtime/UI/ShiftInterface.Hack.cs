using HanaMart.Shift;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace HanaMart.UI
{
    public sealed partial class ShiftInterface
    {
        public readonly struct TimeJumpPreset
        {
            public readonly double Minutes;
            public readonly string Label;

            public TimeJumpPreset(double minutes, string label)
            {
                Minutes = minutes;
                Label = label;
            }
        }

        public static readonly TimeJumpPreset[] TimeJumpPresets =
        {
            new(0.0, "00:00 · Bắt đầu ca (Phase 1)"),
            new(59.8, "01:00 · Máy fax & Mốc 1"),
            new(119.8, "02:00 · Phase 2 (Thay đổi)"),
            new(179.8, "03:00 · Giao hàng 13 thùng"),
            new(223.0, "03:45 · Trước mất điện"),
            new(239.8, "04:00 · Phase 3 (Sắp sáng)"),
            new(283.5, "04:44 · Điện thoại 04:44"),
            new(299.8, "05:00 · Tủ đông (Ji-ho)"),
            new(314.8, "05:15 · Giờ Đỏ (Red Hour)"),
            new(354.8, "05:55 · Sắp hết ca / 06:00"),
        };

        public bool HackPanelOpen { get; private set; }
        GameObject hackPanelGo;

        public void ToggleHackPanel(bool? force = null)
        {
            if (game.Idle || game.Dead || game.Ended)
            {
                HackPanelOpen = false;
                if (hackPanelGo) hackPanelGo.SetActive(false);
                return;
            }

            HackPanelOpen = force ?? !HackPanelOpen;
            if (hackPanelGo) hackPanelGo.SetActive(HackPanelOpen);
        }

        public void JumpToPreset(int index)
        {
            if (index < 0 || index >= TimeJumpPresets.Length) return;
            var preset = TimeJumpPresets[index];
            ToggleHackPanel(false);
            game.PlayClip("ui_ok");
            game.JumpToTime(preset.Minutes, preset.Label);
        }

        void BuildHackPanel()
        {
            if (!hudRoot) return;
            var panel = CreateBox(hudRoot.transform, "HackTimePanel", new Color(0.02f, 0.04f, 0.05f, 0.92f));
            SetTopLeft(panel, 860f, 30f, 384f, 376f);
            hackPanelGo = panel.gameObject;

            var bar = CreateBox(panel, "Bar", Teal);
            SetTopLeft(bar, 0f, 0f, 4f, 376f);

            var title = CreateText(panel, "Title", "HACK NHẢY GIỜ [F4]", 15f, Cyan, TextAlignmentOptions.Left);
            title.fontStyle = FontStyles.Bold;
            title.characterSpacing = 1.5f;
            SetTopLeft(title.rectTransform, 16f, 12f, 352f, 24f);

            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < TimeJumpPresets.Length; i++)
                sb.Append('[').Append(i).Append("] ").AppendLine(TimeJumpPresets[i].Label);

            var list = CreateText(panel, "List", sb.ToString().TrimEnd(), 16f, Bright, TextAlignmentOptions.TopLeft);
            list.lineSpacing = 6f;
            SetTopLeft(list.rectTransform, 16f, 44f, 356f, 294f);

            var footer = CreateText(panel, "Footer", "Bấm số 0–9 để nhảy · [F4 / Esc] Đóng", 13f, Muted, TextAlignmentOptions.Left);
            SetTopLeft(footer.rectTransform, 16f, 344f, 356f, 22f);

            hackPanelGo.SetActive(false);
        }

        bool HandleHackInput()
        {
            var kb = Keyboard.current;
            if (kb == null) return false;

            if (kb.escapeKey.wasPressedThisFrame)
            {
                ToggleHackPanel(false);
                game.PlayClip("ui_move");
                return true;
            }

            if (kb.digit0Key.wasPressedThisFrame || kb.numpad0Key.wasPressedThisFrame) { JumpToPreset(0); return true; }
            if (kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame) { JumpToPreset(1); return true; }
            if (kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame) { JumpToPreset(2); return true; }
            if (kb.digit3Key.wasPressedThisFrame || kb.numpad3Key.wasPressedThisFrame) { JumpToPreset(3); return true; }
            if (kb.digit4Key.wasPressedThisFrame || kb.numpad4Key.wasPressedThisFrame) { JumpToPreset(4); return true; }
            if (kb.digit5Key.wasPressedThisFrame || kb.numpad5Key.wasPressedThisFrame) { JumpToPreset(5); return true; }
            if (kb.digit6Key.wasPressedThisFrame || kb.numpad6Key.wasPressedThisFrame) { JumpToPreset(6); return true; }
            if (kb.digit7Key.wasPressedThisFrame || kb.numpad7Key.wasPressedThisFrame) { JumpToPreset(7); return true; }
            if (kb.digit8Key.wasPressedThisFrame || kb.numpad8Key.wasPressedThisFrame) { JumpToPreset(8); return true; }
            if (kb.digit9Key.wasPressedThisFrame || kb.numpad9Key.wasPressedThisFrame) { JumpToPreset(9); return true; }

            return false;
        }
    }
}
