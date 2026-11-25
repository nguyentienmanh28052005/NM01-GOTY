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
    }
}
