using System.Collections.Generic;
using HanaMart.Shift;
using HanaMart.UI;
using NUnit.Framework;
using UnityEngine;

namespace HanaMart.Tests
{
    [Timeout(10000)]
    public class HudModelTests
    {
        [Test]
        public void RuleBookViewMarksForgedRulesRedHourAddendumAndHandbook()
        {
            var progress = new ShiftProgress();
            progress.Flags["fake12"] = 1;
            progress.Flags["fake13"] = 1;
            progress.Flags["red"] = 1;
            progress.Flags["addendum"] = 1;
            progress.RecordDeath(15);

            var view = RuleBook.BuildView(progress);
            Assert.AreEqual(14, view.Rules.Length);
            Assert.IsTrue(view.Rules[0].HasStamp);
            Assert.IsFalse(view.Rules[0].IsFake);
            Assert.IsTrue(view.Rules[11].IsFake);
            Assert.IsFalse(view.Rules[11].HasStamp);
            Assert.IsTrue(view.Rules[12].IsFake);
            Assert.IsFalse(view.Rules[12].HasStamp);
            Assert.IsTrue(view.Rules[13].IsRed);
            Assert.AreEqual(RuleBook.Addendum, view.Addendum);
            Assert.AreEqual(1, view.Notes.Length);

            Assert.IsNotNull(RuleBook.NoteFor(15));
            Assert.IsNull(RuleBook.NoteFor(6));

            string formatted = RulePaperFormatter.FormatRules(progress);
            StringAssert.Contains("[HANA]", formatted);
            StringAssert.Contains("SỔ TAY SỐNG SÓT", formatted);
            StringAssert.Contains(RuleBook.NoteFor(15), formatted);
            StringAssert.Contains("14.", formatted);
        }

        [Test]
        public void GuideDirectorSelectsCorrectTargetAndHidesDuringHazards()
        {
            var tasks = new ShiftTasks(new ShiftProgress());
            tasks.Add("rules", "Đọc nội quy ca đêm");
            tasks.Add("temp", "Kiểm tra nhiệt độ tủ mát");

            var rulesGuide = GuideDirector.Evaluate(true, false, false, false, false, false,
                tasks.Items, null, false, 0f, false, null, false, false);
            Assert.AreEqual("rules", rulesGuide.HitId);

            tasks.Complete("rules");
            var tempGuide = GuideDirector.Evaluate(true, false, false, false, false, false,
                tasks.Items, null, false, 0f, false, null, false, false);
            Assert.AreEqual("fridgeTemp", tempGuide.HitId);

            tasks.Complete("temp");
            tasks.Add("milk", "Bổ sung bánh mandu");
            var boxGuide = GuideDirector.Evaluate(true, false, false, false, false, false,
                tasks.Items, null, false, 0f, false, null, false, false);
            Assert.AreEqual("boxMilk", boxGuide.HitId);

            var shelfGuide = GuideDirector.Evaluate(true, false, false, false, false, false,
                tasks.Items, "boxMilk", false, 0f, false, null, false, false);
            Assert.AreEqual("shelfMilk", shelfGuide.HitId);
            StringAssert.Contains("bánh mandu", shelfGuide.Title);

            tasks.Complete("milk");
            tasks.Add("trash", "Đổ rác");
            var trashStore = GuideDirector.Evaluate(true, false, false, false, false, false,
                tasks.Items, "trash", false, 0f, false, null, false, false);
            Assert.AreEqual("backDoor", trashStore.HitId);

            var trashAlley = GuideDirector.Evaluate(true, false, false, false, false, false,
                tasks.Items, "trash", false, -5f, false, null, false, false);
            Assert.AreEqual("dumpster", trashAlley.HitId);

            tasks.Complete("trash");
            tasks.Add("heat", "Hâm cơm cuộn");
            Assert.AreEqual("micro1", GuideDirector.Evaluate(true, false, false, false, false, false,
                tasks.Items, "heat", false, 0f, false, null, false, false).HitId);
            Assert.AreEqual("micro2", GuideDirector.Evaluate(true, false, false, false, false, false,
                tasks.Items, null, false, 0f, true, "micro2", false, false).HitId);
            Assert.AreEqual("pos", GuideDirector.Evaluate(true, false, false, false, false, false,
                tasks.Items, "hot", false, 0f, false, null, false, false).HitId);

            tasks.Complete("heat");
            tasks.Add("delivery", "Nhận hàng");
            Assert.AreEqual("backDoor", GuideDirector.Evaluate(true, false, false, false, false, false,
                tasks.Items, null, false, 0f, false, null, false, false).HitId);
            Assert.AreEqual("deliveryStack", GuideDirector.Evaluate(true, false, false, false, false, false,
                tasks.Items, null, false, 0f, false, null, true, false).HitId);

            tasks.Complete("delivery");
            Assert.AreEqual("pos", GuideDirector.Evaluate(true, false, false, false, false, false,
                tasks.Items, null, false, 0f, false, null, false, true).HitId);

            Assert.IsNull(GuideDirector.Evaluate(false, false, false, false, false, false,
                tasks.Items, null, false, 0f, false, null, false, true));
            Assert.IsNull(GuideDirector.Evaluate(true, true, false, false, false, false,
                tasks.Items, null, false, 0f, false, null, false, true));
            Assert.IsNull(GuideDirector.Evaluate(true, false, true, false, false, false,
                tasks.Items, null, false, 0f, false, null, false, true));
            Assert.IsNull(GuideDirector.Evaluate(true, false, false, true, false, false,
                tasks.Items, null, false, 0f, false, null, false, true));
            Assert.IsNull(GuideDirector.Evaluate(true, false, false, false, true, false,
                tasks.Items, null, false, 0f, false, null, false, true));
            Assert.IsNull(GuideDirector.Evaluate(true, false, false, false, false, true,
                tasks.Items, null, false, 0f, false, null, false, true));
        }

        [Test]
        public void DialogFocusNavigatesWrapsAndBlocksWorldInput()
        {
            var focus = new DialogFocus();
            focus.Reset(3);
            Assert.AreEqual(0, focus.Index);
            focus.Move(-1, 3);
            Assert.AreEqual(2, focus.Index);
            focus.Move(1, 3);
            Assert.AreEqual(0, focus.Index);
            focus.Set(1, 3);
            Assert.AreEqual(1, focus.Index);
            focus.Reset(2);
            Assert.AreEqual(0, focus.Index);

            var flow = new UiFlow();
            flow.EnterPlay();
            Assert.IsFalse(flow.GameplayAllowed(true));
            Assert.IsFalse(flow.TryPause(true));
        }

        [Test]
        public void DeathRecapFormatsRuleReasonHandbookAndCheckpoint()
        {
            var fired = DeathRecapModel.Create(0, "Sa thải", 1, 1);
            Assert.AreEqual("BẠN ĐÃ BỊ ĐUỔI VIỆC", fired.RuleHeading);
            Assert.AreEqual("01:00", fired.CheckpointLabel);
            Assert.AreEqual("Lần chết thứ 1", fired.CountLabel);
            Assert.IsTrue(string.IsNullOrEmpty(fired.LearnedNote));

            var freezer = DeathRecapModel.Create(6, "Mở tủ đông", 2, 2);
            Assert.AreEqual("BẠN ĐÃ VI PHẠM QUY TẮC SỐ 6", freezer.RuleHeading);
            Assert.AreEqual("02:00", freezer.CheckpointLabel);
            Assert.AreEqual("Lần chết thứ 2", freezer.CountLabel);
            Assert.IsTrue(string.IsNullOrEmpty(freezer.LearnedNote));

            var shadow = DeathRecapModel.Create(15, "Không có bóng", 3, 3);
            Assert.AreEqual("ĐIỀU KHÔNG CÓ TRONG NỘI QUY", shadow.RuleHeading);
            StringAssert.StartsWith("Đã ghi vào Sổ tay sống sót (phím R): ", shadow.LearnedNote);
        }

        [Test]
        public void EndingModelFormatsEndingAAndEndingBWithStats()
        {
            var a = EndingModel.Create(false, 6, 8, 1, 2, 3, 1);
            Assert.AreEqual("KẾT THÚC: HẾT CA", a.Title);
            StringAssert.Contains("Em làm tốt lắm", a.Body);
            Assert.AreEqual(5, a.Stats.Length);
            Assert.AreEqual("6/8", a.Stats[0].Value);
            Assert.AreEqual("1/3", a.Stats[1].Value);
            Assert.AreEqual("2", a.Stats[2].Value);
            Assert.AreEqual("3/5", a.Stats[3].Value);
            Assert.AreEqual("1/2", a.Stats[4].Value);

            var b = EndingModel.Create(true, 8, 8, 0, 1, 5, 2);
            Assert.AreEqual("KẾT THÚC: NGƯỜI THỨ HAI", b.Title);
            StringAssert.Contains("Đừng bao giờ quay lại ca đêm", b.Body);
            Assert.AreEqual("2/2", b.Stats[4].Value);
        }

        [Test]
        public void TaskListModelLimitsHudToLastFourPendingAndKeepsAllInPanel()
        {
            var tasks = new ShiftTasks(new ShiftProgress());
            tasks.Add("t1", "Việc 1");
            tasks.Complete("t1");
            tasks.Add("t2", "Việc 2");
            tasks.Add("t3", "Việc 3");
            tasks.Add("t4", "Việc 4");
            tasks.Add("t5", "Việc 5");
            tasks.Add("t6", "Việc 6");

            string hud = TaskListModel.FormatHud(tasks.Items);
            StringAssert.DoesNotContain("Việc 1", hud);
            StringAssert.DoesNotContain("Việc 2", hud);
            StringAssert.Contains("Việc 3", hud);
            StringAssert.Contains("Việc 6", hud);

            string panel = TaskListModel.FormatPanel(tasks.Items);
            StringAssert.Contains("Việc 1", panel);
            StringAssert.Contains("Việc 2", panel);
            StringAssert.Contains("Việc 6", panel);

            var doneOnly = new ShiftTasks(new ShiftProgress());
            doneOnly.Add("done", "Xong");
            doneOnly.Complete("done");
            string emptyHud = TaskListModel.FormatHud(doneOnly.Items);
            StringAssert.Contains("Không có việc — trông quầy.", emptyHud);
        }

        [Test]
        public void ScreenSpaceHudLayoutFitsInside16By9ReferenceBounds()
        {
            Assert.AreEqual(new Vector2(1280f, 720f), HudLayoutSpec.ReferenceResolution);
            foreach (var rect in HudLayoutSpec.AllRects)
            {
                Assert.GreaterOrEqual(rect.xMin, 0f);
                Assert.GreaterOrEqual(rect.yMin, 0f);
                Assert.LessOrEqual(rect.xMax, 1280f);
                Assert.LessOrEqual(rect.yMax, 720f);
                Assert.Greater(rect.width, 0f);
                Assert.Greater(rect.height, 0f);
            }
        }
    }
}
