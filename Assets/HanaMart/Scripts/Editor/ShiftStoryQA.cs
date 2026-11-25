using System;
using System.Collections;
using System.IO;
using HanaMart.Interaction;
using HanaMart.Player;
using HanaMart.Shift;
using HanaMart.Store;
using UnityEditor;
using UnityEngine;

namespace HanaMart.EditorTools
{
    public static class ShiftStoryQA
    {
        const string Output = "Logs/HanaMart/story-smoke.json";

        [MenuItem("HanaMart/QA/Run Story Milestones")]
        public static void Run()
        {
            if (!Application.isPlaying) throw new InvalidOperationException("Story QA runs in Play mode.");
            Application.runInBackground = true;
            var camera = Camera.main;
            camera.GetComponent<PlayerController>().StartCoroutine(Check(camera));
        }

        static IEnumerator Check(Camera camera)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Output));
            File.Delete(Output);
            var game = camera.GetComponent<ShiftGame>();
            var input = camera.GetComponent<Interactor>();
            var markers = UnityEngine.Object.FindObjectsByType<StoreMarker>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var results = new StoryResult();
            float previousScale = Time.timeScale;
            Time.timeScale = 4;
            try
            {
                game.NewShift();
                Reach(game, 2, 120);
                ShiftTaskQA.Use(input, markers, "freezer");
                if (!game.Dead || game.LastDeathRule != 6) throw new InvalidOperationException("Opening the freezer at 02:00 did not trigger rule 6.");
                game.SelectOption(0);
                if (game.Dead || game.Run.Clock.Minutes != 120 || !input.Enabled || game.Run.Progress.DeathCount != 1)
                    throw new InvalidOperationException("Retry did not restore input and the 02:00 checkpoint.");
                results.freezerRetry = true;
                yield return null;

                game.NewShift();
                Reach(game, 5, 315);
                double end = Time.realtimeSinceStartupAsDouble + 35;
                for (int frame = 0; frame < 10000 && game.HasFlag("red") && Time.realtimeSinceStartupAsDouble < end; ++frame)
                {
                    if (game.PaperTitle == "HÓA ĐƠN #0000") { results.redReceipt = true; game.ClosePaper(); }
                    yield return null;
                }
                if (game.HasFlag("red") || !results.redReceipt || game.Dead) throw new InvalidOperationException("Standing still did not survive the six Red Hour cycles.");
                results.redCleared = true;

                game.NewShift();
                Reach(game, 5, 355);
                yield return ServeMorning(game, input, markers, "A");
                results.endingA = game.Ending;

                game.NewShift();
                game.Run.Progress.Diaries.UnionWith(new[] { "drawer", "locker", "micro2", "dumpster", "rulesBack" });
                Reach(game, 5, 300);
                ShiftTaskQA.Use(input, markers, "freezer");
                if (game.Speaker != "Ji-ho" || !game.HasFlag("jiho")) throw new InvalidOperationException("The five diary pages did not unlock Ji-ho.");
                game.SelectOption(0);
                Reach(game, 5, 355);
                yield return ServeMorning(game, input, markers, "B");
                results.endingB = game.Ending;
                results.passed = true;
                File.WriteAllText(Output, JsonUtility.ToJson(results, true));
            }
            finally { Time.timeScale = previousScale; }
        }

        static void Reach(ShiftGame game, int hour, double minute)
        {
            var run = game.Run;
            run.Clock.Reset(hour);
            run.Timeline.SkipBefore(minute);
            run.Tick((minute - hour * 60) * (5 + run.Clock.Phase));
            run.Clock.Running = false;
        }

        static IEnumerator ServeMorning(ShiftGame game, Interactor input, StoreMarker[] markers, string ending)
        {
            game.Run.Clock.Running = true;
            game.Run.Tick(40);
            double cutoff = Time.realtimeSinceStartupAsDouble + 18;
            for (int frame = 0; frame < 6000 && !game.CanServe && Time.realtimeSinceStartupAsDouble < cutoff; ++frame) yield return null;
            if (!game.CanServe || game.Run.Clock.Display != "06:00") throw new InvalidOperationException("Mi-rae did not reach the counter at 06:00.");
            ShiftTaskQA.Use(input, markers, "pos");
            if (!game.Ended || game.Speaker != "Chị Mi-rae") throw new InvalidOperationException("Morning handover was not accepted.");
            game.SelectOption(0);
            if (game.Ending != ending) throw new InvalidOperationException("Morning handover selected the wrong ending.");
        }

        [Serializable]
        sealed class StoryResult { public bool passed, freezerRetry, redReceipt, redCleared; public string endingA, endingB; }
    }
}
