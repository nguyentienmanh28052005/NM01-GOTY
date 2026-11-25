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
    public static class ShiftVisitQA
    {
        const string Output = "Logs/HanaMart/visit-smoke.json";

        [MenuItem("HanaMart/QA/Run Checkout Visit")]
        public static void Run()
        {
            if (!Application.isPlaying) throw new InvalidOperationException("Checkout QA runs in Play mode.");
            Application.runInBackground = true;
            var camera = Camera.main;
            var game = camera.GetComponent<ShiftGame>();
            var player = camera.GetComponent<PlayerController>();
            if (!game) throw new InvalidOperationException("Run Setup Shift before testing checkout.");
            player.StartCoroutine(Check(game, camera));
        }

        static IEnumerator Check(ShiftGame game, Camera camera)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Output));
            File.Delete(Output);
            float previousScale = Time.timeScale;
            Time.timeScale = 4;
            try
            {
                game.NewShift();
                game.Run.Clock.Running = false;
                game.SpawnVisit("raincoatOk");
                bool entranceChime = false;
                double cutoff = Time.realtimeSinceStartupAsDouble + 35;
                for (int frame = 0; frame < 10000 && !game.CanServe && Time.realtimeSinceStartupAsDouble < cutoff; ++frame)
                {
                    entranceChime |= game.GreetingTime > 0;
                    yield return null;
                }
                if (!game.CanServe || !entranceChime) throw new InvalidOperationException("Guest did not enter and reach the register.");
                var pos = UnityEngine.Object.FindObjectsByType<StoreMarker>(FindObjectsSortMode.None).First(m => m.key == "H:pos");
                var ray = new Ray(camera.transform.position, pos.transform.position - camera.transform.position);
                var input = camera.GetComponent<Interactor>();
                if (input.Trace(ray) != pos) throw new InvalidOperationException("The checkout trigger is outside ray reach.");
                input.Tick(.016f, ray, true, true);

                cutoff = Time.realtimeSinceStartupAsDouble + 6;
                for (int frame = 0; frame < 2000 && game.Dialogue == null && Time.realtimeSinceStartupAsDouble < cutoff; ++frame)
                    yield return null;
                if (game.Speaker != "Người áo mưa vàng" || game.Options.Length != 1 || !game.Options[0].Label.Contains("3,000"))
                    throw new InvalidOperationException("Raincoat checkout did not offer the expected payment.");
                if (!GameObject.Find("Túi hàng") || !GameObject.Find("Hóa đơn"))
                    throw new InvalidOperationException("Scan did not produce its bag and receipt.");
                string screen = game.PosDisplay;
                game.SelectOption(0);
                cutoff = Time.realtimeSinceStartupAsDouble + 15;
                for (int frame = 0; frame < 5000 && game.LiveGuests > 0 && Time.realtimeSinceStartupAsDouble < cutoff; ++frame)
                    yield return null;
                if (game.LiveGuests != 0 || game.TotalSold != 1 || game.Run.Progress.Strikes != 0 || GameObject.Find("Túi hàng"))
                    throw new InvalidOperationException("Checkout did not release the guest and clean its props.");
                File.WriteAllText(Output, JsonUtility.ToJson(new Result { passed = true, entranceChime = entranceChime, totalSold = game.TotalSold, screen = screen }, true));
            }
            finally { Time.timeScale = previousScale; }
        }

        [Serializable]
        sealed class Result
        {
            public bool passed, entranceChime;
            public int totalSold;
            public string screen;
        }
    }
}
