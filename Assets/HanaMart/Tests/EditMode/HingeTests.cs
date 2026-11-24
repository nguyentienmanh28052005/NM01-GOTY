using HanaMart.Store;
using NUnit.Framework;
using UnityEngine;

namespace HanaMart.Tests
{
    [Timeout(10000)]
    public class HingeTests
    {
        [TestCase(1.35f,5f)]
        [TestCase(-1.6f,4f)]
        [TestCase(1.45f,5f)]
        [TestCase(-1.45f,5f)]
        [TestCase(1.75f,7f)]
        public void OpenAngleMatchesReflectedThree(float radians, float rate)
        {
            var h = new HingeMotor(radians, rate) { target = 1 };
            for (int frame = 0; frame < 180; frame++) h.Tick(1f/60);
            Assert.That(h.open, Is.EqualTo(1).Within(.001f));
            Assert.That(Quaternion.Angle(Quaternion.AngleAxis(radians*Mathf.Rad2Deg,Vector3.up), h.Rotation), Is.LessThan(.1f));
        }
        [Test] public void UsesOriginalExponentialInterpolation()
        {
            var h = new HingeMotor(1.35f,5f) { target = 1 }; h.Tick(.02f);
            Assert.That(h.open, Is.EqualTo(.1f).Within(.00001f));
        }
        [Test] public void FreezerLidReflectsXAxis()
        {
            var h = new HingeMotor(1.2f,100f) { target=1, axis=Vector3.right }; h.Tick(1);
            Assert.That(Quaternion.Angle(Quaternion.Euler(1.2f*Mathf.Rad2Deg,0,0),h.Rotation),Is.LessThan(.001f));
        }
        [Test] public void ClosingReturnsToIdentityWithoutOvershoot()
        {
            var h = new HingeMotor(1.75f,7) { open=1, target=0 }; h.Tick(1);
            Assert.That(h.open,Is.Zero); Assert.That(Quaternion.Angle(Quaternion.identity,h.Rotation),Is.LessThan(.001f));
        }
    }
}
