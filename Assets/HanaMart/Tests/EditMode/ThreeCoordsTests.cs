using HanaMart.Store;
using NUnit.Framework;
using UnityEngine;

namespace HanaMart.Tests
{
    [Timeout(10000)]
    public class ThreeCoordsTests
    {
        [TestCase(0f, 0f)]
        [TestCase(1.2f, .6f)]
        [TestCase(-2.4f, -1.1f)]
        [TestCase(3.14f, 1.35f)]
        public void YawPitchMatchThreeForward(float yaw, float pitch)
        {
            var forward = new Vector3(-Mathf.Sin(yaw) * Mathf.Cos(pitch), Mathf.Sin(pitch), -Mathf.Cos(yaw) * Mathf.Cos(pitch));
            Assert.That(Vector3.Distance(ThreeCoords.ToUnity(forward), ThreeCoords.CameraRotation(pitch, yaw, 0) * Vector3.forward), Is.LessThan(.00001f));
        }

        [Test]
        public void RollMatchesThreeCameraUp()
        {
            float pitch = .4f, yaw = -.7f, roll = .2f;
            var three = Quaternion.AngleAxis(yaw * Mathf.Rad2Deg, Vector3.up)
                * Quaternion.AngleAxis(pitch * Mathf.Rad2Deg, Vector3.right)
                * Quaternion.AngleAxis(roll * Mathf.Rad2Deg, Vector3.forward);
            Assert.That(Vector3.Distance(ThreeCoords.ToUnity(three * Vector3.up), ThreeCoords.CameraRotation(pitch, yaw, roll) * Vector3.up), Is.LessThan(.00001f));
        }
    }
}
