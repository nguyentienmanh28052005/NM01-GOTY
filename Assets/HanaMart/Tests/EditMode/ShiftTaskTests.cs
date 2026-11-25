using HanaMart.Shift;
using NUnit.Framework;

namespace HanaMart.Tests
{
    [Timeout(10000)]
    public sealed class ShiftTaskTests
    {
        [Test]
        public void CompletedTaskMayReturnWithoutDoubleCountingOpenWork()
        {
            var progress = new ShiftProgress();
            var tasks = new ShiftTasks(progress);
            Assert.IsTrue(tasks.Add("trash", "Đổ rác"));
            Assert.IsFalse(tasks.Add("trash", "Đổ rác"));
            Assert.IsTrue(tasks.Pending("trash"));
            Assert.IsTrue(tasks.Complete("trash"));
            Assert.IsFalse(tasks.Complete("trash"));
            Assert.IsTrue(tasks.Items[0].Done);
            Assert.IsTrue(tasks.Add("trash", "Đổ rác lần hai"));
            Assert.AreEqual(1, tasks.Items.Count);
            Assert.AreEqual("Đổ rác lần hai", tasks.Items[0].Text);
            Assert.AreEqual(2, progress.Flags["tAll"]);
            Assert.AreEqual(1, progress.Flags["tDone"]);
            tasks.Clear();
            Assert.IsEmpty(tasks.Items);
            Assert.AreEqual(2, progress.Flags["tAll"]);
        }

        [Test]
        public void OvenOpensLoadsClosesAndHeatsForSixSecondsBeforePickup()
        {
            var oven = new MicrowaveCycle();
            Assert.IsTrue(oven.Begin("micro2"));
            Assert.IsFalse(oven.Begin("micro1"));
            Assert.IsTrue(oven.Active);
            Assert.AreEqual(MicrowaveChange.None, oven.Tick(.54));
            Assert.AreEqual(MicrowaveChange.Insert, oven.Tick(.01));
            Assert.AreEqual(MicrowaveChange.Close, oven.Tick(.4));
            Assert.AreEqual(MicrowaveChange.Heat, oven.Tick(.35));
            Assert.IsTrue(oven.Heating);
            Assert.AreEqual(6, oven.SecondsRemaining, 1e-7);
            Assert.AreEqual(MicrowaveChange.None, oven.Tick(5.9));
            Assert.AreEqual(MicrowaveChange.Ready, oven.Tick(.1));
            Assert.IsTrue(oven.Ready);
            Assert.IsFalse(oven.Take("micro1"));
            Assert.IsTrue(oven.Take("micro2"));
            Assert.IsFalse(oven.Active);
        }

        [Test]
        public void OvenCollectsCrossedMilestonesAndClearsOnRetry()
        {
            var oven = new MicrowaveCycle();
            oven.Begin("micro1");
            Assert.AreEqual(MicrowaveChange.Insert | MicrowaveChange.Close | MicrowaveChange.Heat | MicrowaveChange.Ready, oven.Tick(8));
            oven.Reset();
            Assert.IsFalse(oven.Heating);
            Assert.IsFalse(oven.Ready);
            Assert.IsFalse(oven.Active);
            Assert.IsNull(oven.Oven);
        }

        [TestCase("manager", 40)] [TestCase("hq", 20)] [TestCase("444", 22)]
        [TestCase("self", 18)] [TestCase("clock444", 18)] [TestCase("hqTrap", 18)]
        public void PhoneRingsEveryThreeSecondsUntilItsOwnDeadline(string kind, double seconds)
        {
            var phone = new ShiftPhone();
            phone.Call(kind);
            Assert.IsTrue(phone.Ringing);
            Assert.IsFalse(phone.Tick(2.99, out var expired));
            Assert.IsNull(expired);
            Assert.IsTrue(phone.Tick(.02, out expired));
            Assert.IsNull(expired);
            phone.Tick(seconds - 3, out expired);
            Assert.AreEqual(kind, expired);
            Assert.IsFalse(phone.Ringing);
        }

        [Test]
        public void PhoneIdentifiesTheStoreNumberAndStopsWhenAnswered()
        {
            var phone = new ShiftPhone();
            phone.Call("self");
            StringAssert.Contains("02-555-0124", phone.Caller);
            Assert.AreEqual("self", phone.Stop());
            Assert.IsFalse(phone.Ringing);
            phone.Call("444");
            Assert.AreEqual("KHÔNG RÕ SỐ", phone.Caller);
        }
    }
}
