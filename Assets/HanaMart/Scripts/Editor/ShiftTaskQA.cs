using System;
using System.Collections;
using System.IO;
using System.Linq;
using HanaMart.Interaction;
using HanaMart.Player;
using HanaMart.Shift;
using HanaMart.Store;
using UnityEditor;
using UnityEngine;

namespace HanaMart.EditorTools
{
    public static class ShiftTaskQA
    {
        const string Output = "Logs/HanaMart/jobs-smoke.json";

        [MenuItem("HanaMart/QA/Run Shift Jobs")]
        public static void Run()
        {
            if (!Application.isPlaying) throw new InvalidOperationException("Shift jobs QA runs in Play mode.");
            Application.runInBackground = true;
            var camera = Camera.main;
            camera.GetComponent<PlayerController>().StartCoroutine(Check(camera));
        }

        static IEnumerator Check(Camera camera)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Output));
            File.Delete(Output);
            var game = camera.GetComponent<ShiftGame>();
            var interact = camera.GetComponent<Interactor>();
            var carry = camera.GetComponent<CarryController>();
            var markers = UnityEngine.Object.FindObjectsByType<StoreMarker>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var stock = markers.Where(marker => marker.kind == StoreMarker.Kind.Product && marker.key.EndsWith(":shelfRamen", StringComparison.Ordinal)).ToArray();
            float scale = Time.timeScale;
            Time.timeScale = 4;
            try
            {
                game.NewShift();
                game.Run.Tick(1.3);
                game.Run.Clock.Running = false;
                if (game.Tasks.Items.Count != 3 || stock.Length == 0 || stock.Any(item => item.gameObject.activeSelf))
                    throw new InvalidOperationException("Intro tasks or empty ramen shelf missing.");
                Use(interact, markers, "rules");
                if (game.PaperTitle != "NỘI QUY CA ĐÊM" || game.Tasks.Pending("rules")) throw new InvalidOperationException("Rules paper was not opened.");
                game.ClosePaper();
                Use(interact, markers, "fridgeTemp");
                if (game.Tasks.Pending("temp")) throw new InvalidOperationException("Temperature task was not completed.");
                Use(interact, markers, "boxRamen");
                if (carry.Kind != "boxRamen") throw new InvalidOperationException("Ramen box was not picked up.");
                Use(interact, markers, "shelfRamen");
                if (carry.Kind != null) throw new InvalidOperationException("Ramen box was not accepted at its shelf.");
                for (int frame = 0; frame < 1600 && game.Tasks.Pending("ramen"); ++frame) yield return null;
                if (game.Tasks.Pending("ramen") || stock.Any(item => !item.gameObject.activeSelf)) throw new InvalidOperationException("Ramen flight did not fill the shelf.");

                carry.FoodType = "kimbap";
                carry.SetCarry("heat");
                var doors = UnityEngine.Object.FindFirstObjectByType<StoreDoors>();
                Use(interact, markers, "micro1");
                if (carry.Kind != null || doors.Microwaves[0].motor.target != 1) throw new InvalidOperationException("Microwave did not open for the food.");
                yield return new WaitForSeconds(1.1f);
                if (doors.Microwaves[0].motor.target != 0) throw new InvalidOperationException("Microwave did not close before heating.");
                yield return new WaitForSeconds(6.4f);
                Use(interact, markers, "micro1");
                if (carry.Kind != "hot") throw new InvalidOperationException("Heated item was not available.");

                game.Run.Progress.Checkpoint(1);
                game.Retry();
                if (game.Run.Clock.Minutes != 60 || game.Tasks.Items.Count != 0 || carry.Kind != null ||
                    stock.Any(item => !item.gameObject.activeSelf) || doors.Microwaves[0].motor.target != 0)
                    throw new InvalidOperationException("Retry did not preserve stock and reset the hand and oven.");
                File.WriteAllText(Output, JsonUtility.ToJson(new Result { passed = true, stock = stock.Length, checkpoint = game.Run.Clock.Display }, true));
            }
            finally { Time.timeScale = scale; }
        }

        static void Use(Interactor interact, StoreMarker[] markers, string id)
        {
            var target = markers.First(marker => marker.key == "H:" + id);
            var center = target.transform.position;
            foreach (var direction in new[] { Vector3.up, Vector3.left, Vector3.right, Vector3.back, Vector3.forward })
            {
                var ray = new Ray(center + direction * 1.15f, -direction);
                if (interact.Trace(ray) != target) continue;
                interact.Tick(.016f, ray, true, false);
                return;
            }
            throw new InvalidOperationException("No clear interaction ray for " + id);
        }

        [Serializable]
        sealed class Result { public bool passed; public int stock; public string checkpoint; }
    }
}
