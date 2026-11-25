using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HanaMart.Player;
using HanaMart.Shift;
using HanaMart.Store;
using HanaMart.UI;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace HanaMart.EditorTools
{
    public static class ShiftPerfQA
    {
        const string OutVisualDir = "docs/visual/unity";
        const string OutPerfPath = "Logs/HanaMart/perf-report.json";

        [MenuItem("HanaMart/QA/Capture Visual Acceptance")]
        public static void CaptureVisualAcceptance()
        {
            if (!Application.isPlaying) throw new InvalidOperationException("CaptureVisualAcceptance runs in Play mode.");
            Application.runInBackground = true;
            var cam = Camera.main;
            cam.GetComponent<PlayerController>().StartCoroutine(CaptureDynamicRoutine(cam));
        }

        [MenuItem("HanaMart/QA/Run Perf Benchmark")]
        public static void RunPerfBenchmark()
        {
            if (!Application.isPlaying) throw new InvalidOperationException("RunPerfBenchmark runs in Play mode.");
            Application.runInBackground = true;
            var cam = Camera.main;
            cam.GetComponent<PlayerController>().StartCoroutine(PerfBenchmarkRoutine(cam));
        }

        static IEnumerator CaptureDynamicRoutine(Camera cam)
        {
            Directory.CreateDirectory(OutVisualDir);
            var game = cam.GetComponent<ShiftGame>();
            var player = cam.GetComponent<PlayerController>();
            var iface = UnityEngine.Object.FindFirstObjectByType<ShiftInterface>();

            float prevScale = Time.timeScale;
            Time.timeScale = 2f;
            try
            {
                // 1. Dynamic guest at counter
                game.NewShift(1001);
                yield return null;
                if (game.PaperTitle != null) game.ClosePaper();
                game.SpawnVisit("raincoatOk");
                double deadline = Time.realtimeSinceStartupAsDouble + 20;
                for (int f = 0; f < 4000 && !game.CanServe && Time.realtimeSinceStartupAsDouble < deadline; f++)
                    yield return null;
                player.Motor.pos = new Vector2(0.85f, 4.4f);
                player.Motor.SetYawFacing(1f, 0f);
                player.Motor.pitch = -0.05f;
                player.ApplyPose();
                yield return new WaitForEndOfFrame();
                CaptureScreen(Path.Combine(OutVisualDir, "dynamic-guest.png"));

                // 2. Dynamic Red Hour
                game.NewShift(1002);
                var run = game.Run;
                run.Clock.Reset(5);
                run.Timeline.SkipBefore(315);
                run.Tick((315 - 300) * (5 + run.Clock.Phase));
                yield return null;
                for (int f = 0; f < 120 && game.PaperTitle == null; f++) yield return null;
                if (game.PaperTitle != null) game.ClosePaper();
                player.Motor.pos = new Vector2(0.85f, 4.4f);
                player.Motor.SetYawFacing(1f, 0.25f);
                player.Motor.pitch = 0f;
                player.ApplyPose();
                yield return new WaitForEndOfFrame();
                CaptureScreen(Path.Combine(OutVisualDir, "dynamic-redhour.png"));
            }
            finally
            {
                Time.timeScale = prevScale;
            }
        }

        static IEnumerator PerfBenchmarkRoutine(Camera cam)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(OutPerfPath));
            var game = cam.GetComponent<ShiftGame>();
            var player = cam.GetComponent<PlayerController>();
            var iface = UnityEngine.Object.FindFirstObjectByType<ShiftInterface>();
            var weather = UnityEngine.Object.FindFirstObjectByType<StoreWeather>();

            var savedJson = iface != null && iface.Save != null ? JsonConvert.SerializeObject(iface.Save) : null;
            try
            {
                game.NewShift(2026);
                yield return null;
                if (game.PaperTitle != null) game.ClosePaper();
                game.SpawnVisit("normal");
                game.SpawnVisit("raincoatOk");
                game.SpawnVisit("noShadow");
                player.Motor.pos = new Vector2(3.2f, 6.2f);
                player.Motor.SetYawFacing(0.15f, 1f);
                player.ApplyPose();
                yield return null;

                var presets = new (string quality, bool dynRes, string label)[]
                {
                    ("low", false, "low"),
                    ("medium", false, "medium"),
                    ("medium", true, "medium_dynres"),
                    ("high", false, "high"),
                };
                var results = new List<object>();

                foreach (var (quality, dynRes, label) in presets)
                {
                    if (iface != null && iface.Save != null)
                    {
                        iface.Save.Settings.quality = quality;
                        iface.Save.Settings.dynRes = dynRes;
                        iface.ApplyAllSettings();
                    }
                    for (int w = 0; w < 30; w++) yield return null;

                    var frameTimesMs = new double[180];
                    for (int i = 0; i < frameTimesMs.Length; i++)
                    {
                        frameTimesMs[i] = Time.unscaledDeltaTime * 1000.0;
                        yield return null;
                    }

                    double avgMs = frameTimesMs.Average();
                    var sorted = frameTimesMs.OrderBy(x => x).ToArray();
                    double p95Ms = sorted[(int)(sorted.Length * 0.95)];
                    double p99Ms = sorted[(int)(sorted.Length * 0.99)];
                    double avgFps = avgMs > 0.001 ? 1000.0 / avgMs : 0.0;
                    double low1PctFps = p99Ms > 0.001 ? 1000.0 / p99Ms : 0.0;
                    var urp = UniversalRenderPipeline.asset;

                    results.Add(new
                    {
                        preset = label,
                        quality,
                        dynRes,
                        frames = frameTimesMs.Length,
                        avgFps = Math.Round(avgFps, 1),
                        low1PctFps = Math.Round(low1PctFps, 1),
                        avgFrameMs = Math.Round(avgMs, 2),
                        p95FrameMs = Math.Round(p95Ms, 2),
                        renderScale = Math.Round(urp ? urp.renderScale : 1f, 3),
                        resolution = $"{Screen.width}x{Screen.height}",
                        liveGuests = game.LiveGuests,
                        rainCount = weather ? weather.RainCount : 0,
                    });
                }

                var report = new
                {
                    generatedAt = DateTime.UtcNow.ToString("o"),
                    unityVersion = Application.unityVersion,
                    gpu = SystemInfo.graphicsDeviceName,
                    cpu = SystemInfo.processorType,
                    results,
                };
                File.WriteAllText(OutPerfPath, JsonConvert.SerializeObject(report, Formatting.Indented));
            }
            finally
            {
                if (iface != null && savedJson != null)
                {
                    JsonConvert.PopulateObject(savedJson, iface.Save);
                    SaveStore.Save(iface.Save, iface.SavePathOverride);
                    iface.ApplyAllSettings();
                }
            }
        }

        static void CaptureScreen(string path)
        {
            var tex = ScreenCapture.CaptureScreenshotAsTexture();
            try
            {
                File.WriteAllBytes(path, tex.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(tex);
            }
        }
    }
}
