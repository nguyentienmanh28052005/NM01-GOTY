using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HanaMart.Actors;
using HanaMart.Interaction;
using HanaMart.Player;
using HanaMart.Shift;
using HanaMart.Store;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace HanaMart.EditorTools
{
    public static class ShiftFullPlayQA
    {
        const string Output = "Logs/HanaMart/full-shift-play.json";

        [MenuItem("HanaMart/QA/Run Full Shift Acceptance")]
        public static void Run()
        {
            if (!Application.isPlaying) throw new InvalidOperationException("Full Shift QA runs in Play mode.");
            Application.runInBackground = true;
            var cam = Camera.main;
            cam.GetComponent<PlayerController>().StartCoroutine(RunRoutine(cam));
        }

        static IEnumerator RunRoutine(Camera cam)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Output));
            File.Delete(Output);

            var game = cam.GetComponent<ShiftGame>();
            var player = cam.GetComponent<PlayerController>();
            var interactor = cam.GetComponent<Interactor>();
            var carry = cam.GetComponent<CarryController>();
            var markers = UnityEngine.Object.FindObjectsByType<StoreMarker>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            float prevScale = Time.timeScale;
            float prevMaxDt = Time.maximumDeltaTime;
            var prevBg = InputSystem.settings.backgroundBehavior;
            var prevEd = InputSystem.settings.editorInputBehaviorInPlayMode;
            var virtualKb = InputSystem.AddDevice<Keyboard>("FullShiftVirtualKeyboard");

            var milestones = new List<object>();
            var papersSeen = new HashSet<string>();
            var unexpectedDeaths = new List<object>();
            var targetedTraps = new List<object>();
            bool phase1Visited = false, phase2Visited = false, phase3Visited = false;
            bool retryVerified = false;
            bool redHourSeen = false;
            string partAEnding = null;
            string partBEnding = null;
            double realStart = Time.realtimeSinceStartupAsDouble;

            try
            {
                InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
                InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                InputSystem.EnableDevice(virtualKb);
                virtualKb.MakeCurrent();

                Time.timeScale = 30f;
                Time.maximumDeltaTime = 0.5f;

                // ===== PART A: Continuous 00:00 -> 06:00 Play shift (NO timeline skipping) =====
                game.NewShift(7001);
                yield return null;
                milestones.Add(new { eventName = "shift_start", gameClock = game.Run.Clock.Display, minute = Math.Round(game.Run.Clock.Minutes, 2), realSec = Math.Round(Time.realtimeSinceStartupAsDouble - realStart, 2) });

                int lastSavedHour = game.Run.Progress.SavedHour;
                double nextTaskTryAt = 0;
                int unexpectedRetryCount = 0;
                double deadline = realStart + 600.0;

                for (int frame = 0; frame < 40000 && !game.Ended && Time.realtimeSinceStartupAsDouble < deadline; frame++)
                {
                    double m = game.Run.Clock.Minutes;
                    if (m < 120) phase1Visited = true;
                    else if (m < 240) phase2Visited = true;
                    else phase3Visited = true;

                    if (game.Run.Progress.SavedHour > lastSavedHour)
                    {
                        lastSavedHour = game.Run.Progress.SavedHour;
                        milestones.Add(new
                        {
                            eventName = "checkpoint_" + lastSavedHour,
                            gameClock = game.Run.Clock.Display,
                            minute = Math.Round(m, 2),
                            realSec = Math.Round(Time.realtimeSinceStartupAsDouble - realStart, 2),
                        });
                    }

                    if (game.HasFlag("red") && !redHourSeen)
                    {
                        redHourSeen = true;
                        milestones.Add(new { eventName = "red_hour_start", gameClock = game.Run.Clock.Display, minute = Math.Round(m, 2), realSec = Math.Round(Time.realtimeSinceStartupAsDouble - realStart, 2) });
                    }

                    // Close any paper overlay after recording it
                    if (game.PaperTitle != null)
                    {
                        papersSeen.Add(game.PaperTitle);
                        game.ClosePaper();
                        yield return null;
                        continue;
                    }

                    // Handle Death & Recap
                    if (game.Dead)
                    {
                        if (game.ActiveDeath?.CanSkip == true) game.SkipDeath();
                        if (game.Dialogue != null)
                        {
                            unexpectedDeaths.Add(new { rule = game.LastDeathRule, minute = Math.Round(m, 1) });
                            if (++unexpectedRetryCount > 15)
                                throw new InvalidOperationException("Too many unexpected deaths in full shift: " + JsonConvert.SerializeObject(unexpectedDeaths));
                            game.SelectOption(0);
                        }
                        yield return null;
                        continue;
                    }

                    // Intentional 02:00 checkpoint retry check
                    if (!retryVerified && game.Run.Progress.SavedHour == 2 && m >= 120.2 && !game.HasFlag("red") && !game.HasFlag("blackout"))
                    {
                        ShiftTaskQA.Use(interactor, markers, "freezer");
                        if (!game.Dead || game.LastDeathRule != 6)
                            throw new InvalidOperationException("Opening freezer at 02:00 during full shift did not trigger rule 6.");
                        double recapEnd = Time.realtimeSinceStartupAsDouble + 10.0;
                        for (int rf = 0; rf < 2000 && game.Dialogue == null && Time.realtimeSinceStartupAsDouble < recapEnd; rf++)
                        {
                            if (game.ActiveDeath?.CanSkip == true) game.SkipDeath();
                            yield return null;
                        }
                        if (game.Dialogue == null) throw new InvalidOperationException("Death recap did not open after 02:00 freezer death.");
                        game.SelectOption(0);
                        if (game.Dead || Math.Abs(game.Run.Clock.Minutes - 120.0) > 0.01 || !interactor.Enabled)
                            throw new InvalidOperationException("Retry at 02:00 did not restore checkpoint and input.");
                        retryVerified = true;
                        milestones.Add(new { eventName = "retry_0200_verified", gameClock = game.Run.Clock.Display, minute = Math.Round(game.Run.Clock.Minutes, 2), realSec = Math.Round(Time.realtimeSinceStartupAsDouble - realStart, 2) });
                        yield return null;
                        continue;
                    }

                    // Handle Dialogue
                    if (game.Dialogue != null)
                    {
                        int opt = PickSafeDialogOption(game.Speaker ?? "", game.Dialogue, game.Options);
                        game.SelectOption(opt);
                        yield return null;
                        continue;
                    }

                    // Handle Hazards
                    if (game.HasFlag("red"))
                    {
                        player.Motor.vel = Vector2.zero;
                        yield return null;
                        continue;
                    }

                    if (game.HasFlag("blackout"))
                    {
                        player.Motor.frozen = true;
                        player.Motor.pos = new Vector2(0.85f, 4.4f);
                        player.Motor.vel = Vector2.zero;
                        player.Motor.crouch = 1f;
                        player.Motor.SetYawFacing(-1f, 0f);
                        player.ApplyPose();
                        if (game.GreetingTime > 0.1f && VisitorNearDoor())
                        {
                            InputSystem.QueueStateEvent(virtualKb, new KeyboardState(Key.G));
                            InputSystem.Update();
                        }
                        yield return null;
                        continue;
                    }
                    else if (player.Motor.crouch > 0f || player.Motor.frozen)
                    {
                        player.Motor.frozen = false;
                        player.Motor.crouch = 0f;
                        player.ApplyPose();
                    }

                    if (game.HasFlag("double"))
                    {
                        var mon = markers.First(mk => mk.key == "H:monitor");
                        var monThree = ThreeCoords.ToUnity(mon.transform.position);
                        player.Motor.pos = new Vector2(0.75f, monThree.z);
                        var dir = new Vector2(monThree.x - player.Motor.pos.x, monThree.z - player.Motor.pos.y);
                        if (dir.sqrMagnitude > 1e-4f) player.Motor.SetYawFacing(dir.x, dir.y);
                        player.Motor.pitch = Mathf.Atan2(monThree.y - 1.62f, dir.magnitude);
                        player.ApplyPose();
                        InputSystem.QueueStateEvent(virtualKb, new KeyboardState(Key.E));
                        InputSystem.Update();
                        yield return null;
                        continue;
                    }
                    else
                    {
                        InputSystem.QueueStateEvent(virtualKb, new KeyboardState());
                        InputSystem.Update();
                    }

                    if (game.HasFlag("chase") && game.ChaseSafeThreePos.HasValue)
                    {
                        player.Motor.pos = game.ChaseSafeThreePos.Value;
                        player.Motor.vel = Vector2.zero;
                        player.ApplyPose();
                        yield return null;
                        continue;
                    }

                    // Greeting
                    if (game.GreetingTime > 0.1f && VisitorNearDoor())
                    {
                        InputSystem.QueueStateEvent(virtualKb, new KeyboardState(Key.G));
                        InputSystem.Update();
                    }

                    // Counter & Microwave & Tasks
                    if (game.CanServe)
                    {
                        ShiftTaskQA.Use(interactor, markers, "pos");
                        yield return null;
                        continue;
                    }
                    if (carry.Kind == "heat")
                    {
                        ShiftTaskQA.Use(interactor, markers, "micro1");
                        yield return null;
                        continue;
                    }
                    if (game.MicrowaveReady && !string.IsNullOrEmpty(game.ActiveMicrowaveOven))
                    {
                        ShiftTaskQA.Use(interactor, markers, game.ActiveMicrowaveOven);
                        yield return null;
                        continue;
                    }

                    double now = Time.realtimeSinceStartupAsDouble;
                    if (now >= nextTaskTryAt)
                    {
                        nextTaskTryAt = now + 0.15;
                        TryAdvanceOneTask(game, interactor, carry, markers);
                    }

                    yield return null;
                }

                if (!game.Ended)
                    throw new InvalidOperationException($"Full shift did not reach 06:00 ending. clock={game.Run.Clock.Display} m={game.Run.Clock.Minutes:F1} dead={game.Dead} diag={game.Dialogue}");

                if (game.Dialogue != null) game.SelectOption(0);
                partAEnding = game.Ending;
                milestones.Add(new { eventName = "shift_end_" + partAEnding, gameClock = game.Run.Clock.Display, minute = Math.Round(game.Run.Clock.Minutes, 2), realSec = Math.Round(Time.realtimeSinceStartupAsDouble - realStart, 2) });

                // ===== PART B: Targeted verification of remaining ending & traps =====
                // Ending B with 5 diaries + Ji-ho rescue at 05:00 -> 06:00
                game.NewShift(7002);
                game.Run.Progress.Diaries.UnionWith(new[] { "drawer", "locker", "micro2", "dumpster", "rulesBack" });
                ReachClock(game, 5, 300);
                ShiftTaskQA.Use(interactor, markers, "freezer");
                if (game.Speaker != "Ji-ho" || !game.HasFlag("jiho"))
                    throw new InvalidOperationException("Targeted Ending B: 5 diaries did not unlock Ji-ho.");
                game.SelectOption(0);
                ReachClock(game, 5, 355);
                game.Run.Clock.Running = true;
                game.Run.Tick(40);
                double cutoff = Time.realtimeSinceStartupAsDouble + 15.0;
                for (int f = 0; f < 5000 && !game.CanServe && Time.realtimeSinceStartupAsDouble < cutoff; f++) yield return null;
                if (!game.CanServe) throw new InvalidOperationException("Targeted Ending B: Mi-rae did not reach counter.");
                ShiftTaskQA.Use(interactor, markers, "pos");
                if (game.Dialogue != null) game.SelectOption(0);
                partBEnding = game.Ending;
                if (partBEnding != "B") throw new InvalidOperationException($"Expected Ending B, got {partBEnding}");

                // Trap check: Rule 12 phone at 04:44 + retry
                game.NewShift(7003);
                ReachClock(game, 4, 284);
                game.Run.Clock.Running = true;
                game.Run.Tick(1);
                ShiftTaskQA.Use(interactor, markers, "phone");
                if (!game.Dead || game.LastDeathRule != 12)
                    throw new InvalidOperationException("Targeted Trap: Answering phone at 04:44 did not trigger rule 12.");
                yield return SkipAndRetry(game);
                targetedTraps.Add(new { rule = 12, trap = "phone444", recovered = !game.Dead });

                // Trap check: Rule 10 blackout shelter fail + retry
                game.NewShift(7004);
                ReachClock(game, 3, 222);
                game.Run.Clock.Running = true;
                for (int i = 0; i < 20 && !game.HasFlag("blackout"); i++)
                    game.Run.Tick(7);
                if (!game.HasFlag("blackout"))
                    throw new InvalidOperationException("Targeted Trap: Blackout did not trigger in 222..232 window.");
                double blackWait = Time.realtimeSinceStartupAsDouble + 10.0;
                for (int f = 0; f < 3000 && !game.Dead && Time.realtimeSinceStartupAsDouble < blackWait; f++)
                {
                    player.Motor.pos = new Vector2(6f, 4f);
                    player.Motor.crouch = 0f;
                    player.ApplyPose();
                    yield return null;
                }
                if (!game.Dead || game.LastDeathRule != 10)
                    throw new InvalidOperationException("Targeted Trap: Standing outside counter during blackout did not trigger rule 10.");
                yield return SkipAndRetry(game);
                targetedTraps.Add(new { rule = 10, trap = "blackout", recovered = !game.Dead });

                double totalRealSec = Math.Round(Time.realtimeSinceStartupAsDouble - realStart, 2);
                var result = new
                {
                    passed = true,
                    seedPartA = 7001,
                    timeScale = 30f,
                    totalRealSec,
                    phase1Visited,
                    phase2Visited,
                    phase3Visited,
                    retryVerified,
                    redHourSeen,
                    partAEnding,
                    partBEnding,
                    tasksDone = game.Run.Progress.Flags.GetValueOrDefault("tDone"),
                    tasksAll = game.Run.Progress.Flags.GetValueOrDefault("tAll"),
                    papersSeen = papersSeen.ToArray(),
                    unexpectedDeaths,
                    targetedTraps,
                    milestones,
                };
                File.WriteAllText(Output, JsonConvert.SerializeObject(result, Formatting.Indented));
            }
            finally
            {
                if (player != null) { player.Motor.frozen = false; player.Motor.crouch = 0f; player.ApplyPose(); }
                Time.timeScale = prevScale;
                Time.maximumDeltaTime = prevMaxDt;
                InputSystem.settings.backgroundBehavior = prevBg;
                InputSystem.settings.editorInputBehaviorInPlayMode = prevEd;
                if (virtualKb != null && virtualKb.added) InputSystem.RemoveDevice(virtualKb);
            }
        }

        static IEnumerator SkipAndRetry(ShiftGame game)
        {
            double recapEnd = Time.realtimeSinceStartupAsDouble + 8.0;
            for (int f = 0; f < 2000 && game.Dialogue == null && Time.realtimeSinceStartupAsDouble < recapEnd; f++)
            {
                if (game.ActiveDeath?.CanSkip == true) game.SkipDeath();
                yield return null;
            }
            if (game.Dialogue != null) game.SelectOption(0);
        }

        static void ReachClock(ShiftGame game, int hour, double minute)
        {
            var run = game.Run;
            run.Clock.Reset(hour);
            run.Timeline.SkipBefore(minute);
            run.Tick((minute - hour * 60) * (5 + run.Clock.Phase));
            run.Clock.Running = false;
        }

        static bool VisitorNearDoor()
        {
            var actors = UnityEngine.Object.FindObjectsByType<Actor>(FindObjectsSortMode.None);
            foreach (var a in actors)
            {
                if (!a.alive) continue;
                if (Mathf.Abs(a.pos.x - 3.1f) < 1.5f && a.pos.z > 5.0f && a.pos.z < 8.5f) return true;
            }
            return false;
        }

        static int PickSafeDialogOption(string speaker, string text, ShiftGame.Choice[] options)
        {
            if (options == null || options.Length <= 1) return 0;
            int FindOpt(string sub)
            {
                for (int i = 0; i < options.Length; i++)
                    if (options[i].Label.Contains(sub, StringComparison.OrdinalIgnoreCase)) return i;
                return -1;
            }

            if (speaker.Contains("quản lý", StringComparison.OrdinalIgnoreCase)) return Math.Max(0, FindOpt("nội quy"));
            if (speaker.Contains("Nhân viên HANA", StringComparison.OrdinalIgnoreCase)) return Math.Max(0, FindOpt("6 giờ"));
            if (speaker.Contains("Mi-rae?", StringComparison.OrdinalIgnoreCase)) return Math.Max(0, FindOpt("6 giờ"));
            if (speaker.Contains("ngoài kính", StringComparison.OrdinalIgnoreCase)) return Math.Max(0, FindOpt("cảnh sát"));
            if (speaker.Contains("cửa sau", StringComparison.OrdinalIgnoreCase)) return Math.Max(0, FindOpt("3 giờ"));
            if (speaker.Contains("giao hàng", StringComparison.OrdinalIgnoreCase))
            {
                bool twelve = text != null && text.Contains("mười hai", StringComparison.OrdinalIgnoreCase);
                return Math.Max(0, twelve ? FindOpt("Ký nhận") : FindOpt("Không ký"));
            }
            if (speaker.Contains("vãng lai", StringComparison.OrdinalIgnoreCase)) return Math.Max(0, FindOpt("không có nhà vệ sinh"));
            if (speaker.Contains("áo mưa vàng", StringComparison.OrdinalIgnoreCase))
            {
                if (text != null && text.Contains("mì ly", StringComparison.OrdinalIgnoreCase)) return Math.Max(0, FindOpt("Tính tiền"));
                int broken = FindOpt("hỏng");
                return broken >= 0 ? broken : 0;
            }
            if (speaker.Contains("Khách", StringComparison.OrdinalIgnoreCase))
            {
                int broken = FindOpt("hỏng");
                if (broken >= 0) return broken;
            }
            if (speaker.Contains("Em bé", StringComparison.OrdinalIgnoreCase))
            {
                if (speaker.Contains("cùng mẹ", StringComparison.OrdinalIgnoreCase)) return Math.Max(0, FindOpt("Tính tiền"));
                int home = FindOpt("về nhà");
                if (home >= 0) return home;
            }
            return 0;
        }

        static void TryAdvanceOneTask(ShiftGame game, Interactor interactor, CarryController carry, StoreMarker[] markers)
        {
            if (game.Tasks.Pending("rules")) { ShiftTaskQA.Use(interactor, markers, "rules"); return; }
            if (game.Tasks.Pending("temp") || game.Tasks.Pending("temp2")) { ShiftTaskQA.Use(interactor, markers, "fridgeTemp"); return; }
            if (game.Tasks.Pending("ramen") && !game.Restocking)
            {
                ShiftTaskQA.Use(interactor, markers, carry.Kind == "boxRamen" ? "shelfRamen" : "boxRamen");
                return;
            }
            if (game.Tasks.Pending("milk") && !game.Restocking)
            {
                ShiftTaskQA.Use(interactor, markers, carry.Kind == "boxMilk" ? "shelfMilk" : "boxMilk");
                return;
            }
            if (game.Tasks.Pending("delivery") && game.HasFlag("delivery"))
            {
                var liveMarkers = UnityEngine.Object.FindObjectsByType<StoreMarker>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                ShiftTaskQA.Use(interactor, liveMarkers, "deliveryStack");
                return;
            }
            if (game.Tasks.Pending("trash"))
            {
                if (carry.Kind != "trash") ShiftTaskQA.Use(interactor, markers, "trash");
                else ShiftTaskQA.Use(interactor, markers, "dumpster");
            }
        }

        static void HoldUse(Interactor interactor, StoreMarker[] markers, string id, float dt)
        {
            var target = markers.First(marker => marker.key == "H:" + id);
            var center = target.transform.position;
            foreach (var direction in new[] { Vector3.up, Vector3.left, Vector3.right, Vector3.back, Vector3.forward })
            {
                var ray = new Ray(center + direction * 1.15f, -direction);
                if (interactor.Trace(ray) != target) continue;
                interactor.Tick(dt, ray, false, true);
                return;
            }
        }
    }
}
