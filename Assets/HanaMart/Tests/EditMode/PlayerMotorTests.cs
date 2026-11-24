using System.Collections.Generic;
using HanaMart.Player;
using NUnit.Framework;
using UnityEngine;

namespace HanaMart.Tests
{
    [Timeout(10000)]
    public class PlayerMotorTests
    {
        const float Dt = 1f / 60f;
        static readonly PlayerSettings S = new PlayerSettings();
        static WalkArea Empty() => new WalkArea(new List<Box2>());
        static PlayerInputState Move(float x, float y, bool run = false, bool crouch = false) => new PlayerInputState { move = new Vector2(x, y), run = run, crouch = crouch };
        static float Speed(PlayerMotor m, PlayerInputState i, WalkArea a, float secs) { StepResult r = default; for (int k = 0; k < secs / Dt; k++) r = m.Update(Dt, i, S, a); return r.speed; }

        [Test] public void WalkableRespectsZones()
        {
            var a = Empty();
            Assert.IsTrue(a.Walkable(6, 4)); Assert.IsFalse(a.Walkable(6, 9)); Assert.IsTrue(a.Walkable(2, -2));
            Assert.IsFalse(a.Walkable(5, -2)); Assert.IsTrue(a.Walkable(0, -6));
        }

        [Test] public void WalkableRespectsColliders()
        {
            var b = new Box2(5, 5, 6, 6); var a = new WalkArea(new List<Box2> { b });
            Assert.IsFalse(a.Walkable(5.5f, 5.5f)); Assert.IsFalse(a.Walkable(4.8f, 5.5f)); Assert.IsTrue(a.Walkable(4.7f, 5.5f));
            b.off = true; Assert.IsTrue(a.Walkable(5.5f, 5.5f));
        }

        [Test] public void WalkSpeedReaches16()
        {
            var m = new PlayerMotor { pos = new Vector2(6, 7.5f) };
            Assert.AreEqual(1.6f, Speed(m, Move(0, -1), Empty(), 2f), 0.03f);
            Assert.Less(m.pos.y, 7.5f - 2.5f); Assert.AreEqual(6f, m.pos.x, 0.001f);
        }

        [Test] public void RunSpeedReaches30() => Assert.AreEqual(3.0f, Speed(new PlayerMotor { pos = new Vector2(6, 7.9f) }, Move(0, -1, run: true), Empty(), 1.5f), 0.05f);

        [Test] public void CrouchSlowsTo45Percent()
        {
            var m = new PlayerMotor { pos = new Vector2(6, 7.9f) };
            Assert.AreEqual(1.6f * 0.45f, Speed(m, Move(0, -1, crouch: true), Empty(), 2f), 0.03f);
            Assert.Greater(m.crouch, 0.99f);
        }

        [Test] public void SlidesAlongWall()
        {
            var m = new PlayerMotor { pos = new Vector2(0.3f, 6f) };
            Speed(m, Move(-0.7071f, -0.7071f), Empty(), 1f);
            Assert.GreaterOrEqual(m.pos.x, 0f); Assert.Less(m.pos.y, 5.5f);
        }

        [Test] public void CannotCrossColliderDiagonally()
        {
            var a = new WalkArea(new List<Box2> { new Box2(5, 3, 7, 5) });
            var m = new PlayerMotor { pos = new Vector2(4.5f, 6f) };
            for (int k = 0; k < 240; k++)
            {
                m.Update(Dt, Move(0.7071f, -0.7071f, run: true), S, a);
                bool inside = m.pos.x > 5 - WalkArea.R && m.pos.x < 7 + WalkArea.R && m.pos.y > 3 - WalkArea.R && m.pos.y < 5 + WalkArea.R;
                Assert.IsFalse(inside, "đi xuyên hộp ở bước " + k);
            }
        }

        [Test] public void TwoStepsAfter136m()
        {
            var m = new PlayerMotor { pos = new Vector2(6, 7.9f) }; int steps = 0; float dist = 0; var last = m.pos;
            for (int guard = 0; dist < 1.37f && guard < 2000; guard++) { if (m.Update(Dt, Move(0, -1), S, Empty()).stepped) steps++; dist += Vector2.Distance(last, m.pos); last = m.pos; } // guard: không bao giờ treo Editor
            Assert.AreEqual(2, steps);
        }

        [Test] public void PitchClamped()
        {
            var m = new PlayerMotor(); m.Update(Dt, new PlayerInputState { lookDY = 100000f }, S, Empty());
            Assert.AreEqual(-1.35f, m.pitch, 1e-4f);
            m.Update(Dt, new PlayerInputState { lookDY = -100000f }, S, Empty());
            Assert.AreEqual(1.35f, m.pitch, 1e-4f);
        }

        [Test] public void EyeHeightWithCrouch()
        {
            var m = new PlayerMotor();
            m.Update(Dt, default, S, Empty());
            Assert.AreEqual(1.62f - 0.02f, m.Pose.threePos.y, 1e-3f);
            for (int k = 0; k < 120; k++) m.Update(Dt, new PlayerInputState { crouch = true }, S, Empty());
            Assert.AreEqual(1.62f - 0.72f * m.crouch - 0.02f, m.Pose.threePos.y, 1e-3f);
            Assert.Less(m.Pose.threePos.y, 0.92f);
        }
    }
}
