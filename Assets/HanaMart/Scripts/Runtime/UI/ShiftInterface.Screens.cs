using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HanaMart.UI
{
    public sealed partial class ShiftInterface
    {
        TextMeshProUGUI deathCountText, deathRuleText, deathWhyText, deathLearnText, deathCheckpointText;
        TextMeshProUGUI endingTitleText, endingBodyText;
        readonly TextMeshProUGUI[] endingStatLabels = new TextMeshProUGUI[5];
        readonly TextMeshProUGUI[] endingStatValues = new TextMeshProUGUI[5];

        void BuildHelpScreen()
        {
            var panel = CreateScreen(UiScreen.Help, new Color(0.015f, 0.031f, 0.031f, 0.9f));
            var title = CreateText(panel.transform, "Title", "Hướng dẫn", 42f, Bright, TextAlignmentOptions.Left);
            title.fontStyle = FontStyles.Bold;
            SetAnchored(title.rectTransform, new Vector2(0f, 1f), new Vector2(140f, -68f), new Vector2(500f, 52f));

            var kbHeader = CreateText(panel.transform, "KbHeader", "BÀN PHÍM & CHUỘT", 15f, Teal, TextAlignmentOptions.Left);
            kbHeader.fontStyle = FontStyles.Bold;
            kbHeader.characterSpacing = 3f;
            SetAnchored(kbHeader.rectTransform, new Vector2(0f, 1f), new Vector2(140f, -138f), new Vector2(480f, 24f));

            var controls = CreateText(panel.transform, "Controls",
                "<b>W A S D</b>  Di chuyển · <b>Shift</b>  Đi nhanh · <b>C</b>  Ngồi thấp\n" +
                "<b>E / Chuột trái</b>  Tương tác (giữ để làm lâu)\n" +
                "<b>Q</b>  Giữ để ngắm chỗ đặt, thả để đặt đồ xuống\n" +
                "<b>G</b>  Chào khách · <b>R</b>  Đọc nội quy · <b>Tab</b>  Việc cần làm\n" +
                "<b>Chuột phải</b>  Nhìn kỹ · <b>Esc</b>  Tạm dừng\n" +
                "<b>H</b>  Ẩn giao diện (quay video) · <b>F3</b>  Xem FPS",
                16.5f, Bright, TextAlignmentOptions.TopLeft);
            controls.lineSpacing = 8f;
            SetAnchored(controls.rectTransform, new Vector2(0f, 1f), new Vector2(140f, -168f), new Vector2(520f, 220f));

            var padHeader = CreateText(panel.transform, "PadHeader", "TAY CẦM", 15f, Teal, TextAlignmentOptions.Left);
            padHeader.fontStyle = FontStyles.Bold;
            padHeader.characterSpacing = 3f;
            SetAnchored(padHeader.rectTransform, new Vector2(0f, 1f), new Vector2(680f, -138f), new Vector2(480f, 24f));

            var pad = CreateText(panel.transform, "Gamepad",
                "<b>L</b>  Di chuyển · <b>R</b>  Nhìn\n" +
                "<b>A</b>  Tương tác · <b>RB</b>  Đặt đồ xuống\n" +
                "<b>Y</b>  Chào khách · <b>X</b>  Nội quy\n" +
                "<b>B</b>  Ngồi thấp · <b>LT</b>  Nhìn kỹ\n" +
                "<b>View</b>  Việc cần làm · <b>Menu</b>  Tạm dừng",
                16.5f, Bright, TextAlignmentOptions.TopLeft);
            pad.lineSpacing = 8f;
            SetAnchored(pad.rectTransform, new Vector2(0f, 1f), new Vector2(680f, -168f), new Vector2(480f, 220f));

            var howHeader = CreateText(panel.transform, "HowHeader", "CÁCH CHƠI", 15f, Teal, TextAlignmentOptions.Left);
            howHeader.fontStyle = FontStyles.Bold;
            howHeader.characterSpacing = 3f;
            SetAnchored(howHeader.rectTransform, new Vector2(0f, 1f), new Vector2(140f, -396f), new Vector2(980f, 24f));

            var how = CreateText(panel.transform, "HowBody",
                "Bạn là nhân viên ca đêm mới của HANA MART 24. Phục vụ khách, làm việc vặt, và <b>tuân thủ tuyệt đối 13 điều nội quy</b>. " +
                "Vi phạm điều nhẹ: bị cảnh cáo (3 lần là hết). Vi phạm điều nguy hiểm: chết ngay. Mỗi giờ trong game là một mốc lưu. " +
                "Sống tới 06:00 để kết thúc ca. Có một kết thúc khác cho người đủ tò mò…",
                16.5f, Muted, TextAlignmentOptions.TopLeft);
            how.lineSpacing = 6f;
            SetAnchored(how.rectTransform, new Vector2(0f, 1f), new Vector2(140f, -424f), new Vector2(1000f, 96f));

            AddMenuButtons(UiScreen.Help, panel.transform, 140f, -560f, new[]
            {
                (UiAction.Back, "← Quay lại"),
            });
            AddFooterPrompts(panel.transform);
        }

        void BuildDeathScreen()
        {
            var panel = CreateScreen(UiScreen.Death, new Color(0.08f, 0.005f, 0.01f, 0.94f));

            var lbl = CreateText(panel.transform, "Label", "CA ĐÊM CHƯA KẾT THÚC", 14f, new Color(0.67f, 0.34f, 0.34f, 1f), TextAlignmentOptions.Center);
            lbl.characterSpacing = 8f;
            SetAnchored(lbl.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 190f), new Vector2(760f, 28f));

            deathCountText = CreateText(panel.transform, "Count", "Lần chết thứ 1", 13f, new Color(0.67f, 0.47f, 0.47f, 1f), TextAlignmentOptions.Center);
            deathCountText.characterSpacing = 3f;
            SetAnchored(deathCountText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 158f), new Vector2(560f, 24f));

            deathRuleText = CreateText(panel.transform, "Rule", "BẠN ĐÃ VI PHẠM QUY TẮC", 40f, new Color(1f, 0.23f, 0.28f, 1f), TextAlignmentOptions.Center);
            deathRuleText.fontStyle = FontStyles.Bold;
            SetAnchored(deathRuleText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 110f), new Vector2(960f, 64f));

            deathWhyText = CreateText(panel.transform, "Why", "", 19f, new Color(0.87f, 0.8f, 0.8f, 1f), TextAlignmentOptions.Center);
            SetAnchored(deathWhyText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 36f), new Vector2(760f, 76f));

            deathLearnText = CreateText(panel.transform, "Learn", "", 15f, new Color(0.91f, 0.77f, 0.42f, 1f), TextAlignmentOptions.Center);
            SetAnchored(deathLearnText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -24f), new Vector2(760f, 44f));

            deathCheckpointText = CreateText(panel.transform, "From", "Tiếp tục từ mốc 00:00", 14f, new Color(0.6f, 0.53f, 0.53f, 1f), TextAlignmentOptions.Center);
            deathCheckpointText.characterSpacing = 2f;
            SetAnchored(deathCheckpointText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -66f), new Vector2(560f, 26f));

            AddMenuButtons(UiScreen.Death, panel.transform, 0f, -124f, new[]
            {
                (UiAction.Retry, "Thử lại"),
                (UiAction.ToMenu, "Về màn hình chính"),
            }, centered: true, width: 380f);
        }

        void BuildEndingScreen()
        {
            var panel = CreateScreen(UiScreen.Ending, new Color(0.95f, 0.88f, 0.80f, 0.98f));

            endingTitleText = CreateText(panel.transform, "Title", "KẾT THÚC: HẾT CA", 52f, new Color(0.07f, 0.07f, 0.07f, 1f), TextAlignmentOptions.Center);
            endingTitleText.fontStyle = FontStyles.Bold;
            SetAnchored(endingTitleText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 170f), new Vector2(920f, 72f));

            endingBodyText = CreateText(panel.transform, "Body", "", 18.5f, new Color(0.2f, 0.2f, 0.2f, 1f), TextAlignmentOptions.Center);
            endingBodyText.lineSpacing = 6f;
            SetAnchored(endingBodyText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 76f), new Vector2(780f, 110f));

            for (int i = 0; i < 5; i++)
            {
                float x = (i - 2) * 168f;
                endingStatLabels[i] = CreateText(panel.transform, "StatLabel" + i, "", 13f, new Color(0.33f, 0.33f, 0.33f, 1f), TextAlignmentOptions.Center);
                endingStatLabels[i].characterSpacing = 2f;
                SetAnchored(endingStatLabels[i].rectTransform, new Vector2(0.5f, 0.5f), new Vector2(x, -26f), new Vector2(150f, 24f));

                endingStatValues[i] = CreateText(panel.transform, "StatVal" + i, "", 36f, new Color(0.07f, 0.07f, 0.07f, 1f), TextAlignmentOptions.Center);
                endingStatValues[i].fontStyle = FontStyles.Bold;
                SetAnchored(endingStatValues[i].rectTransform, new Vector2(0.5f, 0.5f), new Vector2(x, -66f), new Vector2(150f, 46f));
            }

            AddMenuButtons(UiScreen.Ending, panel.transform, 0f, -150f, new[]
            {
                (UiAction.ToMenu, "Về màn hình chính"),
            }, centered: true, width: 340f);
        }

        void ShowDeathRecap(int rule, string reason)
        {
            if (game.Run == null) return;
            var model = DeathRecapModel.Create(rule, reason, game.Run.Progress.SavedHour, game.Run.Progress.DeathCount);
            if (deathCountText) deathCountText.text = model.CountLabel;
            if (deathRuleText) deathRuleText.text = model.RuleHeading;
            if (deathWhyText) deathWhyText.text = model.Reason;
            if (deathLearnText) deathLearnText.text = model.LearnedNote;
            if (deathCheckpointText) deathCheckpointText.text = "Tiếp tục từ mốc " + model.CheckpointLabel;
            Flow.ShowScreen(UiScreen.Death);
            SyncState();
        }

        void ShowEndingScreen(bool endingB)
        {
            if (game.Run == null) return;
            var progress = game.Run.Progress;
            int done = progress.Flags.GetValueOrDefault("tDone");
            int all = progress.Flags.GetValueOrDefault("tAll");
            var model = EndingModel.Create(endingB, done, all, progress.Strikes, progress.DeathCount,
                progress.Diaries.Count, Save != null ? Save.UnlockedEndingsCount : 1);

            if (endingTitleText) endingTitleText.text = model.Title;
            if (endingBodyText) endingBodyText.text = model.Body;
            for (int i = 0; i < 5; i++)
            {
                if (endingStatLabels[i]) endingStatLabels[i].text = model.Stats[i].Label;
                if (endingStatValues[i]) endingStatValues[i].text = model.Stats[i].Value;
            }
            Flow.ShowScreen(UiScreen.Ending);
            SyncState();
        }
    }
}
