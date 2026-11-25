using System;
using System.Collections;
using System.IO;
using System.Linq;
using HanaMart.Actors;
using HanaMart.Player;
using HanaMart.Render;
using HanaMart.Shift;
using UnityEditor;
using UnityEngine;

namespace HanaMart.EditorTools
{
    public static class ShiftDeathQA
    {
        [MenuItem("HanaMart/QA/Run Death Camera")]
        public static void Run()
        {
            if (!Application.isPlaying) throw new InvalidOperationException("Death camera QA runs in Play mode.");
            Application.runInBackground = true;
            var camera = Camera.main;
            camera.GetComponent<PlayerController>().StartCoroutine(Check(camera));
        }

        [MenuItem("HanaMart/QA/Capture Guest Death Cuts")]
        public static void GuestCuts()
        {
            if (!Application.isPlaying) throw new InvalidOperationException("Guest death QA runs in Play mode.");
            Application.runInBackground = true;
            var camera = Camera.main;
            camera.GetComponent<PlayerController>().StartCoroutine(CheckGuestCuts(camera));
        }

        static IEnumerator CheckGuestCuts(Camera view)
        {
            var game = view.GetComponent<ShiftGame>();
            var cuts = new[] { (rule: 3, at: 2.2f), (rule: 18, at: 2.6f), (rule: 9, at: 4.1f), (rule: 13, at: 3.7f) };
            var results = new CutResult[cuts.Length];
            Directory.CreateDirectory("docs/visual/unity");
            Directory.CreateDirectory("Logs/HanaMart");
            const string output = "Logs/HanaMart/death-guests.json";
            File.Delete(output);
            for (int index = 0; index < cuts.Length; ++index)
            {
                var (rule, at) = cuts[index];
                game.NewShift();
                yield return null;
                yield return null;
                game.Die(rule, "Kiểm tra cảnh thua theo nội quy.");
                var cut = game.ActiveDeath;
                double end = Time.realtimeSinceStartupAsDouble + 24;
                for (int frame = 0; frame < 4000 && cut.Time < at && Time.realtimeSinceStartupAsDouble < end; ++frame) yield return null;
                if (cut.Done || cut.Time < at) throw new InvalidOperationException($"Rule {rule} did not reach its capture cue.");
                var actors = UnityEngine.Object.FindObjectsByType<Actor>(FindObjectsSortMode.None)
                    .Where(actor => actor.name.StartsWith("Cảnh thua", StringComparison.Ordinal)).ToArray();
                bool staged = rule switch
                {
                    3 => actors.Length == 1 && Mathf.Abs(actors[0].pos.x - 2.45f) < .01f,
                    18 => GameObject.Find("Kính nứt"),
                    9 => actors.Length == 1 && actors[0].pos.z < 3.4f,
                    13 => actors.Length == 7 && !string.IsNullOrEmpty(game.ClockCaption),
                    _ => false,
                };
                if (!staged) throw new InvalidOperationException($"Rule {rule} did not create its source staging.");
                CharacterPreview.CaptureFrame(view, $"docs/visual/unity/death-{rule}.png");
                for (int frame = 0; frame < 6500 && !cut.Done && Time.realtimeSinceStartupAsDouble < end; ++frame) yield return null;
                if (!cut.Done || cut.Skipped || cut.Time > 19.9f || game.Dialogue == null)
                    throw new InvalidOperationException($"Rule {rule} did not finish its full sequence.");
                results[index] = new CutResult { rule = rule, kind = cut.Kind, seconds = cut.Time };
                game.SelectOption(0);
                yield return null;
                yield return null;
                if (game.Dead || GameObject.Find("Kính nứt") || actors.Any(actor => actor))
                    throw new InvalidOperationException($"Rule {rule} left a scene actor or prop after retry.");
            }
            File.WriteAllText(output, JsonUtility.ToJson(new CutResults { passed = true, scenes = results }, true));
        }

        static IEnumerator Check(Camera view)
        {
            const string output = "Logs/HanaMart/death-camera.json";
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            File.Delete(output);
            var player = view.GetComponent<PlayerController>();
            var game = view.GetComponent<ShiftGame>();
            var film = view.GetComponent<FilmSettings>();
            game.NewShift();
            yield return null;
            Quaternion originalRotation = view.transform.rotation;
            game.Die(99, "Kiểm camera và phục hồi cảnh.");
            if (game.ActiveDeath == null || game.SkipDeath() || player.GameplayEnabled) throw new InvalidOperationException("The cut did not lock control for its opening.");
            double deadline = Time.realtimeSinceStartupAsDouble + 8;
            float rotation = 0;
            for (int frame = 0; frame < 4000 && !game.ActiveDeath.CanSkip && Time.realtimeSinceStartupAsDouble < deadline; ++frame)
            {
                rotation = Mathf.Max(rotation, Quaternion.Angle(originalRotation, view.transform.rotation));
                yield return null;
            }
            bool skipped = game.SkipDeath();
            if (film.cine < .99f || rotation < .8f || !skipped)
                throw new InvalidOperationException($"Death camera: cine={film.cine:F3}, rotation={rotation:F3}, elapsed={game.ActiveDeath.Time:F3}, skipped={skipped}.");
            yield return null;
            yield return null;
            if (film.fade < .99f || game.Options.Length != 1) throw new InvalidOperationException("Skipping the cut did not open the recap.");
            game.SelectOption(0);
            yield return null;
            yield return null;
            bool actorsGone = !UnityEngine.Object.FindObjectsByType<Actor>(FindObjectsSortMode.None).Any(actor => actor.name.StartsWith("Cảnh thua", StringComparison.Ordinal));
            if (!actorsGone || game.ActiveDeath != null || game.Dead || !player.GameplayEnabled || film.cine != 0 || film.fade != 0 || film.red != 0)
                throw new InvalidOperationException("Retry did not restore actor/film/input state.");
            File.WriteAllText(output, JsonUtility.ToJson(new Result { passed = true, cameraDegrees = rotation, restored = actorsGone }, true));
        }

        [Serializable]
        sealed class Result { public bool passed, restored; public float cameraDegrees; }

        [Serializable]
        sealed class CutResult { public int rule; public string kind; public float seconds; }
        [Serializable]
        sealed class CutResults { public bool passed; public CutResult[] scenes; }
    }
}
