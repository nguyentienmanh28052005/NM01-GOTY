using System;
using System.Collections.Generic;
using System.Text;
using HanaMart.Shift;
using UnityEngine;

namespace HanaMart.UI
{
    public sealed class GuideState
    {
        public string HitId;
        public string Title;
        public string Where;
    }

    public static class RulePaperFormatter
    {
        public static string FormatRules(ShiftProgress progress)
        {
            var view = RuleBook.BuildView(progress);
            var sb = new StringBuilder();
            foreach (var r in view.Rules)
            {
                if (r.IsFake)
                {
                    sb.Append("<color=#5a1a14><i><b>").Append(r.Number).Append(".</b> ").Append(r.Text).AppendLine("</i></color>");
                }
                else if (r.IsRed)
                {
                    sb.Append("<color=#a00000><b>").Append(r.Number).Append(". ").Append(r.Text).AppendLine("</b></color>");
                }
                else
                {
                    sb.Append("<b>").Append(r.Number).Append(".</b> ").Append(r.Text)
                        .AppendLine(" <color=#c1121f><b>[HANA]</b></color>");
                }
            }
            if (!string.IsNullOrEmpty(view.Addendum))
            {
                sb.AppendLine("<color=#999999>------------------------------</color>");
                sb.Append("<b>").Append(view.Addendum).AppendLine("</b> <color=#c1121f><b>[HANA]</b></color>");
            }
            if (view.Notes.Length > 0)
            {
                sb.AppendLine().AppendLine("<color=#24304a><b>SỔ TAY SỐNG SÓT (tự viết sau mỗi lần chết):</b></color>");
                foreach (string note in view.Notes)
                    sb.Append("<color=#24304a><i>— ").Append(note).AppendLine("</i></color>");
            }
            return sb.ToString();
        }
    }

    public static class GuideDirector
    {
        public static GuideState Evaluate(bool guideEnabled, bool dead, bool red, bool blackout,
            bool dialogOpen, bool paperOpen, IReadOnlyList<ShiftTask> tasks, string carryKind,
            bool restocking, float playerZ, bool microwaveReady, string microwaveOven,
            bool backDoorOpen, bool canServe)
        {
            if (!guideEnabled || dead || red || blackout || dialogOpen || paperOpen) return null;

            if (tasks != null)
            {
                for (int i = 0; i < tasks.Count; i++)
                {
                    var t = tasks[i];
                    if (t.Done) continue;
                    GuideState state = ForTask(t.Id, carryKind, restocking, playerZ, microwaveReady, microwaveOven, backDoorOpen);
                    if (state != null) return state;
                }
            }

            if (canServe && carryKind != "heat" && !restocking)
            {
                return new GuideState
                {
                    HitId = "pos",
                    Title = "Tính tiền cho khách",
                    Where = "Máy tính tiền ở quầy"
                };
            }

            return null;
        }

        static GuideState ForTask(string id, string carry, bool restocking, float playerZ,
            bool microwaveReady, string microwaveOven, bool backDoorOpen)
        {
            switch (id)
            {
                case "rules":
                    return new GuideState { HitId = "rules", Title = "Đọc nội quy ca đêm", Where = "Tường sau quầy · hoặc bấm R" };
                case "temp":
                    return new GuideState { HitId = "fridgeTemp", Title = "Xem nhiệt độ tủ mát", Where = "Đầu trái dãy tủ mát" };
                case "ramen":
                    if (restocking) return null;
                    return carry == "boxRamen"
                        ? new GuideState { HitId = "shelfRamen", Title = "Xếp mì ly vào chỗ trống", Where = "Kệ sát quầy · thẻ đỏ HẾT HÀNG" }
                        : new GuideState { HitId = "boxRamen", Title = "Lấy thùng mì ly", Where = "Kho · cửa rèm nhựa sau quầy" };
                case "milk":
                    if (restocking) return null;
                    return carry == "boxMilk"
                        ? new GuideState { HitId = "shelfMilk", Title = "Xếp bánh mandu vào chỗ trống", Where = "Dãy kệ thứ ba · thẻ đỏ HẾT HÀNG" }
                        : new GuideState { HitId = "boxMilk", Title = "Lấy thùng bánh mandu", Where = "Kho · cạnh thùng mì" };
                case "mop":
                    return carry == "mop"
                        ? new GuideState { HitId = "spill", Title = "Lau vết nước (giữ E)", Where = "Lối giữa cửa hàng" }
                        : new GuideState { HitId = "mop", Title = "Lấy cây lau nhà", Where = "Kho · góc phải, trong xô vàng" };
                case "trash":
                    if (carry == "trash")
                    {
                        return playerZ > -4.4f
                            ? new GuideState { HitId = "backDoor", Title = "Mang rác ra hẻm", Where = "Cửa sau cuối kho" }
                            : new GuideState { HitId = "dumpster", Title = "Bỏ rác vào thùng lớn", Where = "Cuối hẻm · đừng quay đầu nếu bị gọi" };
                    }
                    return new GuideState { HitId = "trash", Title = "Lấy túi rác", Where = "Thùng rác nhỏ sau quầy" };
                case "heat":
                    if (carry == "heat")
                        return new GuideState { HitId = "micro1", Title = "Hâm cơm cuộn", Where = "Lò vi sóng · quầy mì bên phải" };
                    if (microwaveReady)
                        return new GuideState { HitId = string.IsNullOrEmpty(microwaveOven) ? "micro1" : microwaveOven, Title = "Lấy cơm cuộn ra", Where = "Lò vi sóng" };
                    if (carry == "hot")
                        return new GuideState { HitId = "pos", Title = "Đưa cơm cuộn cho khách", Where = "Máy tính tiền" };
                    return null;
                case "delivery":
                    return backDoorOpen
                        ? new GuideState { HitId = "deliveryStack", Title = "Đếm thùng & ký nhận", Where = "Hẻm sau · phải đúng 12 thùng" }
                        : new GuideState { HitId = "backDoor", Title = "Mở cửa nhận hàng", Where = "Cửa sau cuối kho" };
                default:
                    return null;
            }
        }
    }

    public sealed class DialogFocus
    {
        public int Index { get; private set; }

        public void Reset(int count)
        {
            Index = count > 0 ? 0 : -1;
        }

        public void Move(int delta, int count)
        {
            if (count <= 0) { Index = -1; return; }
            Index = ((Index + delta) % count + count) % count;
        }

        public void Set(int index, int count)
        {
            if (index >= 0 && index < count) Index = index;
        }
    }

    public sealed class DeathRecapModel
    {
        public string CountLabel, RuleHeading, Reason, LearnedNote, CheckpointLabel;

        public static DeathRecapModel Create(int rule, string reason, int savedHour, int deathCount)
        {
            string note = RuleBook.NoteFor(rule);
            return new DeathRecapModel
            {
                CountLabel = $"Lần chết thứ {Math.Max(1, deathCount)}",
                RuleHeading = rule >= 15
                    ? "ĐIỀU KHÔNG CÓ TRONG NỘI QUY"
                    : rule > 0
                        ? $"BẠN ĐÃ VI PHẠM QUY TẮC SỐ {rule}"
                        : "BẠN ĐÃ BỊ ĐUỔI VIỆC",
                Reason = reason ?? "",
                LearnedNote = !string.IsNullOrEmpty(note) ? "Đã ghi vào Sổ tay sống sót (phím R): " + note : "",
                CheckpointLabel = ShiftClock.Format(Math.Max(0, savedHour) * 60),
            };
        }
    }

    public sealed class EndingModel
    {
        public sealed class StatEntry { public string Label, Value; }
        public string Title, Body;
        public StatEntry[] Stats;

        public static EndingModel Create(bool endingB, int tasksDone, int tasksAll, int strikes,
            int deathCount, int diaries, int unlockedEndings)
        {
            return new EndingModel
            {
                Title = endingB ? "KẾT THÚC: NGƯỜI THỨ HAI" : "KẾT THÚC: HẾT CA",
                Body = endingB
                    ? "Bạn và Ji-ho bước ra ngoài đúng lúc trời hửng sáng. Mưa đã tạnh. Ji-ho nhìn lại cửa hàng rất lâu rồi nói: \"Đừng bao giờ quay lại ca đêm.\" Phía sau lưng, tấm biển HANA MART 24 tắt phụt — lần đầu tiên sau nhiều năm."
                    : "Bạn sống sót qua ca đêm đầu tiên. Trên đường về, điện thoại rung: \"Em làm tốt lắm. Tối mai 12 giờ nhé.\" Trong kho, tủ đông vẫn còn tiếng gõ... Có lẽ bạn đã bỏ sót điều gì đó.",
                Stats = new[]
                {
                    new StatEntry { Label = "VIỆC XONG", Value = $"{tasksDone}/{tasksAll}" },
                    new StatEntry { Label = "CẢNH CÁO", Value = $"{strikes}/3" },
                    new StatEntry { Label = "SỐ LẦN CHẾT", Value = deathCount.ToString() },
                    new StatEntry { Label = "NHẬT KÝ", Value = $"{diaries}/5" },
                    new StatEntry { Label = "KẾT THÚC", Value = $"{unlockedEndings}/2" },
                }
            };
        }
    }

    public static class TaskListModel
    {
        public static string FormatHud(IReadOnlyList<ShiftTask> tasks)
        {
            if (tasks == null) return "<color=#99bbbb>Không có việc — trông quầy.</color>";
            var pending = new List<string>();
            for (int i = 0; i < tasks.Count; i++)
                if (!tasks[i].Done) pending.Add("□ " + tasks[i].Text);

            if (pending.Count == 0)
                return "<color=#99bbbb>Không có việc — trông quầy.</color>";

            int start = Math.Max(0, pending.Count - 4);
            var sb = new StringBuilder();
            for (int i = start; i < pending.Count; i++)
            {
                if (i > start) sb.Append('\n');
                sb.Append(pending[i]);
            }
            return sb.ToString();
        }

        public static string FormatPanel(IReadOnlyList<ShiftTask> tasks)
        {
            if (tasks == null || tasks.Count == 0)
                return "<color=#556060>Không có việc.</color>";
            var sb = new StringBuilder();
            for (int i = 0; i < tasks.Count; i++)
            {
                if (i > 0) sb.Append('\n');
                var t = tasks[i];
                if (t.Done)
                    sb.Append("<s><alpha=#80>☑ ").Append(t.Text).Append("</alpha></s>");
                else
                    sb.Append("☐ ").Append(t.Text);
            }
            return sb.ToString();
        }
    }

    public static class HudLayoutSpec
    {
        public static readonly Vector2 ReferenceResolution = new(1280f, 720f);
        public static readonly Rect ClockBox = new(36f, 30f, 220f, 52f);
        public static readonly Rect GuideBox = new(36f, 96f, 340f, 58f);
        public static readonly Rect TaskBox = new(36f, 178f, 380f, 150f);
        public static readonly Rect TaskPanel = new(860f, 110f, 380f, 280f);
        public static readonly Rect PromptBox = new(440f, 388f, 400f, 36f);
        public static readonly Rect HoldBox = new(520f, 430f, 240f, 34f);
        public static readonly Rect GreetPill = new(440f, 158f, 400f, 42f);
        public static readonly Rect SubtitleBox = new(190f, 576f, 900f, 64f);
        public static readonly Rect ToastBox = new(820f, 36f, 420f, 52f);
        public static readonly Rect BannerBox = new(140f, 220f, 1000f, 150f);
        public static readonly Rect WarnBox = new(320f, 110f, 640f, 96f);
        public static readonly Rect CarryBox = new(780f, 650f, 460f, 28f);
        public static readonly Rect HintsBox = new(560f, 682f, 680f, 24f);
        public static readonly Rect DialogBox = new(210f, 440f, 860f, 230f);
        public static readonly Rect PaperSheet = new(280f, 50f, 720f, 620f);

        public static readonly Rect[] AllRects =
        {
            ClockBox, GuideBox, TaskBox, TaskPanel, PromptBox, HoldBox, GreetPill,
            SubtitleBox, ToastBox, BannerBox, WarnBox, CarryBox, HintsBox, DialogBox, PaperSheet
        };
    }
}
