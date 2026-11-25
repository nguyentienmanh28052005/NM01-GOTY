using System;
using System.IO;
using HanaMart.Player;
using HanaMart.Shift;
using HanaMart.Sim;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace HanaMart.EditorTools
{
    public static class ShiftSimQA
    {
        const string Output = "Logs/HanaMart/sim-report.json";

        [MenuItem("HanaMart/QA/Run Sim Batch")]
        public static void Run()
        {
            if (!Application.isPlaying) throw new InvalidOperationException("Sim QA runs in Play mode.");
            Application.runInBackground = true;
            var camera = Camera.main;
            var player = camera.GetComponent<PlayerController>();
            var game = camera.GetComponent<ShiftGame>();

            game.NewShift(1001);
            if (game.Idle || game.Run.Clock.Minutes != 0 || game.Dead)
                throw new InvalidOperationException("Seeded ShiftGame.NewShift(1001) did not initialize cleanly.");

            var nav = player.Navigation;
            var careful = SimBot.RunBatch(SimSkillKind.Careful, 5, 1001, nav);
            var average = SimBot.RunBatch(SimSkillKind.Average, 5, 2001, nav);
            var careless = SimBot.RunBatch(SimSkillKind.Careless, 4, 3001, nav);

            Directory.CreateDirectory(Path.GetDirectoryName(Output));
            var report = new
            {
                generatedAt = DateTime.UtcNow.ToString("o"),
                referenceV22 = new
                {
                    careful = "4/4 reach 06:00, 7-19 deaths (avg ~13), 76-118 sim min",
                    average = "4/4 reach 06:00, 13-30 deaths (avg ~19, median ~16), 92-154 sim min",
                    careless = "0/4 reach 06:00 (45 retry cap in phase 2, 46+ deaths)",
                },
                careful,
                average,
                careless,
            };
            File.WriteAllText(Output, JsonConvert.SerializeObject(report, Formatting.Indented));

            if (careful.TotalStalls > 0 || average.TotalStalls > 0 || careless.TotalStalls > 0)
                throw new InvalidOperationException("Stall detected during simulation batch.");
            if (careful.Reached6Count < 4 || average.Reached6Count < 4)
                throw new InvalidOperationException($"Expected >=4/5 runs reaching 06:00, got careful={careful.Reached6Count}/5, average={average.Reached6Count}/5.");
        }
    }
}
