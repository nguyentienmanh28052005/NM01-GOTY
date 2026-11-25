using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HanaMart.EditorTools;
using HanaMart.Player;
using HanaMart.Shift;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;

namespace HanaMart.Tests
{
    [Timeout(10000)]
    public sealed class ShiftIntegrationTests
    {
        [TestCase(1)] [TestCase(21)] [TestCase(76801)]
        public void EveryHourAndStoryAnchorRunsInOrderFromMidnightToDawn(int seed)
        {
            var run = new ShiftRun(new System.Random(seed).NextDouble);
            run.NewShift();
            var events = new List<ShiftEvent>();
            var saves = new List<int>();
            run.EventReached += events.Add;
            run.CheckpointSaved += (hour, reduced) => saves.Add(hour);
            for (int step = 0; step < 2600 && run.Clock.Minutes < 360; ++step) run.Tick(1);
            Assert.AreEqual("06:00", run.Clock.Display);
            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4, 5 }, saves);
            Assert.AreEqual(29, events.Count(e => !e.IsAmbient));
            Assert.AreEqual(events.Count, events.Select(e => e.Id).Distinct().Count());
            Assert.AreEqual("intro", events[0].Id);
            Assert.Less(events.FindIndex(e => e.Id == "blackout"), events.FindIndex(e => e.Id == "phase3"));
            Assert.Less(events.FindIndex(e => e.Id == "redHour"), events.FindIndex(e => e.Id == "realMirae"));
            Assert.IsTrue(events.Select(e => e.Minute).SequenceEqual(events.Select(e => e.Minute).OrderBy(m => m)));
        }

        [Test]
        public void PlayerReachesTheAlleyThroughTheOpenBackDoor()
        {
            var boxes = (JArray)JObject.Parse(File.ReadAllText(StoreImporter.MetaPath))["colliders"];
            var collisions = boxes.Select(c => new Box2((float)c[0], (float)c[1], (float)c[2], (float)c[3])).ToList();
            var area = new WalkArea(collisions);
            var settings = new PlayerSettings();
            var player = new PlayerMotor();
            Travel(player, settings, area, new Vector2(.85f, -.7f));
            Travel(player, settings, area, new Vector2(1, -1));
            Travel(player, settings, area, new Vector2(1, -1.64f));
            Travel(player, settings, area, new Vector2(1.91f, -2.1f));
            Travel(player, settings, area, new Vector2(1.92f, -4.15f));
            for (int step = 0; step < 60; ++step)
                Steer(player, settings, area, new Vector2(1.92f, -6));
            Assert.Greater(player.pos.y, -4.3f, "Closed back door blocks passage.");
            collisions.Single(box => Mathf.Abs(box.x0 - 1.6f) < .001f && Mathf.Abs(box.z0 + 4.68f) < .001f).off = true;
            Travel(player, settings, area, new Vector2(1.92f, -5.1f));
            Travel(player, settings, area, new Vector2(2.15f, -7));
            Assert.That(player.pos.y, Is.LessThan(-6.9f));
        }

        static void Travel(PlayerMotor motor, PlayerSettings settings, WalkArea area, Vector2 target)
        {
            for (int frame = 0; frame < 375 && Vector2.Distance(motor.pos, target) >= .022f; ++frame)
                Steer(motor, settings, area, target);
            Assert.That(Vector2.Distance(motor.pos, target), Is.LessThan(.028f), $"Destination {target:F3}; player {motor.pos:F3}");
            motor.vel = Vector2.zero;
        }

        static void Steer(PlayerMotor motor, PlayerSettings settings, WalkArea area, Vector2 target)
        {
            var delta = target - motor.pos;
            motor.SetYawFacing(delta.x, delta.y);
            float throttle = Mathf.Clamp(delta.magnitude * 12, .15f, 1);
            motor.Update(1f / 60, new PlayerInputState { move = new Vector2(0, -throttle) }, settings, area);
        }
    }
}
