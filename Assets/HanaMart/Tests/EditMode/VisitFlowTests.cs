using HanaMart.Shift;
using NUnit.Framework;

namespace HanaMart.Tests
{
    [Timeout(10000)]
    public sealed class VisitFlowTests
    {
        [TestCase(0, 28)] [TestCase(120, 26)] [TestCase(240, 18)]
        public void VisitsUseSourceSpacingAndCapTheNumberInside(double minute, double nextDelay)
        {
            var progress = new ShiftProgress();
            var visit = new VisitDirector(() => .5);
            visit.StartIn(2);
            Assert.IsNull(visit.Tick(2.5, minute, 2, null, false, false, progress));
            Assert.IsNotNull(visit.Tick(.01, minute, 1, null, false, false, progress));
            Assert.AreEqual(nextDelay, visit.Remaining);
        }

        [Test]
        public void QuietAndImminentAnchorDontConsumeVisitTime()
        {
            var progress = new ShiftProgress();
            var visit = new VisitDirector(() => .5);
            visit.Reset(true);
            Assert.AreEqual(10, visit.Remaining);
            Assert.IsNull(visit.Tick(18, 158, 0, null, false, false, progress));
            visit.Calm(5);
            visit.Calm(2);
            Assert.IsNull(visit.Tick(5, 158, 0, null, false, false, progress));
            Assert.IsNull(visit.Tick(1, 158, 0, 160, false, false, progress));
            Assert.IsNull(visit.Tick(1, 158, 0, null, true, false, progress));
            Assert.IsNull(visit.Tick(1, 353, 0, null, false, false, progress));
            Assert.AreEqual(10, visit.Remaining);
            Assert.IsNotNull(visit.Tick(10, 160, 0, null, false, false, progress));
        }

        [Test]
        public void VisitWeightsHonorOpeningHoursMercyAndTheLastVisit()
        {
            var p = new ShiftProgress();
            var v = new VisitDirector(() => .999999);
            Assert.AreEqual(0, v.Weight("ghost", 20, false, p));
            Assert.AreEqual(1.5, v.Weight("ghost", 21, false, p));
            Assert.AreEqual(0, v.Weight("loneChild", 63.99, false, p));
            Assert.AreEqual(1.4, v.Weight("loneChild", 64, false, p));
            Assert.AreEqual(0, v.Weight("phoneHQ", 200, true, p));
            Assert.AreEqual(0, v.Weight("raincoatChild", 150, false, p));
            p.Flags["addendum"] = 1;
            Assert.AreEqual(3, v.Weight("raincoatChild", 150, false, p));
            p.RecordDeath(15);
            Assert.AreEqual(1.6 * .55, v.Weight("noShadow", 150, false, p), 1e-9);
            v.StartIn(0);
            Assert.AreEqual("ghost", v.Tick(1, 21, 0, null, false, false, p));
            Assert.AreEqual(1.5 * .2, v.Weight("ghost", 21, false, p), 1e-9);
            for (int visit = 0; visit < 4; ++visit)
                Assert.AreEqual("ghost", v.Tick(40, 21, 0, null, false, false, p));
            Assert.AreEqual(0, v.Weight("ghost", 21, false, p));
            Assert.AreEqual("normal", v.Tick(40, 21, 0, null, false, false, p));
        }

        [TestCase(false, false, true, true, GreetingResult.Welcome)]
        [TestCase(true, false, true, false, GreetingResult.GhostStrike)]
        [TestCase(true, false, false, false, GreetingResult.None)]
        [TestCase(false, false, false, true, GreetingResult.Complaint)]
        [TestCase(false, false, false, false, GreetingResult.None)]
        [TestCase(false, true, false, true, GreetingResult.BlackoutStrike)]
        public void GreetingConsumesOnlyOneSixSecondWindow(bool ghost, bool blackout, bool pressed, bool present, GreetingResult result)
        {
            var window = new GreetingWindow();
            window.Open(ghost, blackout);
            Assert.AreEqual(6, window.Remaining);
            Assert.AreEqual(GreetingResult.None, window.Tick(5.9f, false, present));
            Assert.AreEqual(result, window.Tick(.1f, pressed, present));
            Assert.AreEqual(GreetingResult.None, window.Tick(1, true, present));
        }

        [TestCase("raincoatBad", CheckoutChoice.Charge, CheckoutEffect.Die, 3)]
        [TestCase("raincoatBad", CheckoutChoice.RegisterBroken, CheckoutEffect.Refusal, 0)]
        [TestCase("raincoatChild", CheckoutChoice.Charge, CheckoutEffect.Sale, 0)]
        [TestCase("raincoatChild", CheckoutChoice.SendHome, CheckoutEffect.Strike, 3)]
        [TestCase("loneChild", CheckoutChoice.Charge, CheckoutEffect.Strike, 4)]
        [TestCase("childParent", CheckoutChoice.Charge, CheckoutEffect.Sale, 0)]
        [TestCase("childParent", CheckoutChoice.SendHome, CheckoutEffect.Complain, 0)]
        [TestCase("noShadow", CheckoutChoice.Charge, CheckoutEffect.Die, 15)]
        [TestCase("childMom", CheckoutChoice.Charge, CheckoutEffect.Die, 18)]
        [TestCase("restroom", CheckoutChoice.AllowStorage, CheckoutEffect.Strike, 9)]
        [TestCase("restroom", CheckoutChoice.DenyStorage, CheckoutEffect.Refusal, 0)]
        public void CorrectAndIncorrectCheckoutChoicesKeepRulePriority(string visit, CheckoutChoice choice, CheckoutEffect effect, int rule)
        {
            Assert.AreEqual((effect, rule), CheckoutRules.Decide(visit, choice));
        }

        [Test]
        public void PricesStayTheSameForReplacedProducts()
        {
            Assert.AreEqual(3000, CheckoutRules.Total(new[] { "ramenCup", "ramenCup" }));
            Assert.AreEqual(3000, CheckoutRules.Total(new[] { "kimbap", "banana" }));
            Assert.AreEqual(5700, CheckoutRules.Total(new[] { "soju", "soju", "chips" }));
        }

        [Test]
        public void CheckoutQueueKeepsWaitingOrderAndRemovesGuestsWhoLeave()
        {
            var queue = new CheckoutQueue();
            Assert.IsTrue(queue.TryEnter(1));
            Assert.IsFalse(queue.TryEnter(2));
            Assert.IsFalse(queue.TryEnter(2));
            Assert.IsFalse(queue.TryEnter(3));
            Assert.AreEqual(2, queue.WaitingCount);
            Assert.AreEqual(0, queue.IndexOf(2));
            Assert.AreEqual(1, queue.IndexOf(3));
            queue.Remove(1);
            Assert.IsFalse(queue.TryEnter(3));
            Assert.IsTrue(queue.TryEnter(2));
            Assert.AreEqual(2, queue.Current);
            queue.Remove(2);
            queue.Remove(3);
            Assert.AreEqual(0, queue.WaitingCount);
            Assert.IsTrue(queue.TryEnter(4));
            queue.Clear();
            Assert.IsNull(queue.Current);
        }
    }
}
