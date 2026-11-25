using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HanaMart.EditorTools;
using HanaMart.Player;
using HanaMart.Sim;
using HanaMart.Store;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;

namespace HanaMart.Tests
{
    [Timeout(15000)]
    public sealed class SimBotTests
    {
        static StoreNavigation OriginalLayout()
        {
            var boxes = (JArray)JObject.Parse(File.ReadAllText(StoreImporter.MetaPath))["colliders"];
            return new StoreNavigation(boxes.Select(c => new Box2((float)c[0], (float)c[1], (float)c[2], (float)c[3])));
        }

        [Test]
        public void SkillProfilesMatchSimJsConstants()
        {
            var careful = SimSkillProfile.Get(SimSkillKind.Careful);
            Assert.IsNotNull(careful);
            Assert.AreEqual("careful", careful.Id);
            Assert.AreEqual("cẩn thận", careful.Label);
            Assert.AreEqual(0.55, careful.F1, 1e-9);
            Assert.AreEqual(0.96, careful.P, 1e-9);
            Assert.AreEqual(0.6, careful.ReactMin, 1e-9);
            Assert.AreEqual(1.8, careful.ReactMax, 1e-9);
            Assert.AreEqual(1.5, careful.ReadMin, 1e-9);
            Assert.AreEqual(3.0, careful.ReadMax, 1e-9);
            Assert.AreEqual(2.4, careful.Speed, 1e-9);
            Assert.AreEqual(0.75, careful.Diary, 1e-9);

            var average = SimSkillProfile.Get(SimSkillKind.Average);
            Assert.IsNotNull(average);
            Assert.AreEqual("average", average.Id);
            Assert.AreEqual("trung bình", average.Label);
            Assert.AreEqual(0.7, average.F1, 1e-9);
            Assert.AreEqual(0.86, average.P, 1e-9);
            Assert.AreEqual(0.9, average.ReactMin, 1e-9);
            Assert.AreEqual(2.8, average.ReactMax, 1e-9);
            Assert.AreEqual(2.0, average.ReadMin, 1e-9);
            Assert.AreEqual(4.5, average.ReadMax, 1e-9);
            Assert.AreEqual(2.2, average.Speed, 1e-9);
            Assert.AreEqual(0.45, average.Diary, 1e-9);

            var careless = SimSkillProfile.Get(SimSkillKind.Careless);
            Assert.IsNotNull(careless);
            Assert.AreEqual("careless", careless.Id);
            Assert.AreEqual("ẩu", careless.Label);
            Assert.AreEqual(0.85, careless.F1, 1e-9);
            Assert.AreEqual(0.7, careless.P, 1e-9);
            Assert.AreEqual(1.2, careless.ReactMin, 1e-9);
            Assert.AreEqual(4.0, careless.ReactMax, 1e-9);
            Assert.AreEqual(1.0, careless.ReadMin, 1e-9);
            Assert.AreEqual(2.5, careless.ReadMax, 1e-9);
            Assert.AreEqual(2.6, careless.Speed, 1e-9);
            Assert.AreEqual(0.2, careless.Diary, 1e-9);
        }

        [Test]
        public void SpotsAndTrapMapMatchSimJs()
        {
            var expectedSpots = new Dictionary<string, Vector2>
            {
                ["counter"] = new(0.85f, 4.4f),
                ["monitor"] = new(0.7f, 2.9f),
                ["rules"] = new(0.6f, 6.9f),
                ["phone"] = new(0.75f, 2.9f),
                ["temp"] = new(2.4f, 1.4f),
                ["storage"] = new(1.4f, -1.6f),
                ["boxRamen"] = new(1.6f, -0.5f),
                ["boxMilk"] = new(2.4f, -0.6f),
                ["shelfRamen"] = new(3.6f, 3.7f),
                ["shelfMilk"] = new(8.8f, 3.7f),
                ["mop"] = new(3.6f, -0.9f),
                ["spill"] = new(5.4f, 4.6f),
                ["trash"] = new(0.85f, 6.3f),
                ["backDoor"] = new(2.0f, -4.1f),
                ["alley"] = new(4.4f, -7.6f),
                ["dumpster"] = new(4.4f, -7.6f),
                ["micro"] = new(10.9f, 6.5f),
                ["freezer"] = new(3.0f, -3.2f),
                ["door"] = new(3.1f, 7.2f),
                ["aisle"] = new(7.2f, 2.0f),
                ["fridge"] = new(5.0f, 1.4f),
                ["glass"] = new(6.5f, 7.3f),
                ["locker"] = new(0.8f, -3.8f),
                ["drawer"] = new(0.85f, 3.3f),
            };
            Assert.AreEqual(24, SimSpots.All.Count);
            foreach (var kv in expectedSpots)
            {
                var actual = SimSpots.Get(kv.Key);
                Assert.That(Vector2.Distance(kv.Value, actual), Is.LessThan(1e-4f), $"Spot {kv.Key} mismatch");
            }

            var expectedTraps = new Dictionary<int, string>
            {
                [15] = "noShadow",
                [16] = "selfCall",
                [17] = "chase",
                [18] = "childMom",
                [12] = "phone444",
                [9] = "manager",
                [10] = "blackout",
                [14] = "red",
                [5] = "alley",
                [3] = "raincoat",
                [7] = "cctv",
                [13] = "mirae",
                [8] = "delivery",
                [6] = "freezer",
                [19] = "dbl",
                [20] = "scream",
                [21] = "late",
            };
            Assert.AreEqual(17, SimTrapMap.ByDeathRule.Count);
            foreach (var kv in expectedTraps)
                Assert.AreEqual(kv.Value, SimTrapMap.ByDeathRule[kv.Key], $"Death rule {kv.Key} trap mismatch");

            Assert.AreEqual(0.9, SimTrapMap.TrapWeight("manager", hqTrap: true, fake13: false), 1e-9);
            Assert.AreEqual(0.4, SimTrapMap.TrapWeight("manager", hqTrap: false, fake13: false), 1e-9);
            Assert.AreEqual(0.8, SimTrapMap.TrapWeight("mirae", hqTrap: false, fake13: true), 1e-9);
            Assert.AreEqual(0.3, SimTrapMap.TrapWeight("mirae", hqTrap: false, fake13: false), 1e-9);
        }

        [Test]
        public void DeterministicSeedProducesIdenticalRunLog()
        {
            var nav = OriginalLayout();
            var a = SimBot.Run(new SimOptions { Skill = SimSkillKind.Careful, Seed = 1001 }, nav);
            var b = SimBot.Run(new SimOptions { Skill = SimSkillKind.Careful, Seed = 1001 }, nav);

            Assert.AreEqual(a.Reached6, b.Reached6);
            Assert.AreEqual(a.Ending, b.Ending);
            Assert.AreEqual(a.Retries, b.Retries);
            Assert.AreEqual(a.Deaths.Count, b.Deaths.Count);
            CollectionAssert.AreEqual(a.Deaths.Select(d => d.rule), b.Deaths.Select(d => d.rule));
            CollectionAssert.AreEqual(a.Deaths.Select(d => d.minute), b.Deaths.Select(d => d.minute));
            Assert.AreEqual(a.Strikes.Count, b.Strikes.Count);
            Assert.AreEqual(a.TasksDone, b.TasksDone);
            Assert.AreEqual(a.Diaries, b.Diaries);
            Assert.AreEqual(a.SimSeconds, b.SimSeconds, 1e-9);
        }

        [Test]
        public void BoundsCapStepsTimeAndRetriesWithoutHanging()
        {
            var nav = OriginalLayout();
            var stepCapped = SimBot.Run(new SimOptions { Skill = SimSkillKind.Careful, Seed = 1001, MaxSteps = 50 }, nav);
            Assert.IsTrue(stepCapped.HitStepOrTimeCap);
            Assert.AreEqual(50, stepCapped.Steps);

            var timeCapped = SimBot.Run(new SimOptions { Skill = SimSkillKind.Careful, Seed = 1001, MaxSimSeconds = 25 }, nav);
            Assert.IsTrue(timeCapped.HitStepOrTimeCap);
            Assert.LessOrEqual(timeCapped.SimSeconds, 25.1);

            var retryCapped = SimBot.Run(new SimOptions { Skill = SimSkillKind.Careless, Seed = 3001, MaxRetries = 1 }, nav);
            Assert.IsFalse(retryCapped.Reached6);
            Assert.GreaterOrEqual(retryCapped.Retries, 1);
            Assert.IsTrue(retryCapped.Warnings.Any(w => w.Contains("quá nhiều lần chết", StringComparison.Ordinal)));
        }

        [Test]
        public void CarefulAndAverageBatchesReachDawnWithZeroStalls()
        {
            var nav = OriginalLayout();
            var careful = SimBot.RunBatch(SimSkillKind.Careful, 5, 1001, nav);
            var average = SimBot.RunBatch(SimSkillKind.Average, 5, 2001, nav);
            var careless = SimBot.RunBatch(SimSkillKind.Careless, 4, 3001, nav);

            Assert.AreEqual(5, careful.Reached6Count);
            Assert.AreEqual(0, careful.TotalStalls);
            Assert.IsFalse(careful.Logs.Any(l => l.Warnings.Any(w => w.Contains("Exception", StringComparison.OrdinalIgnoreCase) || w.StartsWith("update:", StringComparison.Ordinal))));
            Assert.IsTrue(careful.Logs.All(l => l.Ending == "A" || l.Ending == "B"));
            Assert.That(careful.AvgDeaths, Is.InRange(3.0, 25.0));
            Assert.That(careful.AvgSimMinutes, Is.InRange(40.0, 160.0));

            Assert.GreaterOrEqual(average.Reached6Count, 4);
            Assert.AreEqual(0, average.TotalStalls);
            Assert.IsFalse(average.Logs.Any(l => l.Warnings.Any(w => w.Contains("Exception", StringComparison.OrdinalIgnoreCase) || w.StartsWith("update:", StringComparison.Ordinal))));
            Assert.That(average.AvgDeaths, Is.InRange(6.0, 38.0));
            Assert.GreaterOrEqual(average.AvgDeaths, careful.AvgDeaths * 0.8);

            Assert.AreEqual(0, careless.TotalStalls);
            Assert.IsFalse(careless.Logs.Any(l => l.Warnings.Any(w => w.Contains("Exception", StringComparison.OrdinalIgnoreCase) || w.StartsWith("update:", StringComparison.Ordinal))));
            Assert.Greater(careless.AvgDeaths, careful.AvgDeaths);
        }
    }
}
