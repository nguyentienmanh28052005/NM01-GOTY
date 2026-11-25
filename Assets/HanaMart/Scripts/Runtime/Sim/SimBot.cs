using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HanaMart.Store;
using UnityEngine;

namespace HanaMart.Sim
{
    public static class SimBot
    {
        sealed class QueuedStep
        {
            public Vector2 Spot;
            public Vector2 Face = new(1f, 0f);
            public double Extra;
            public string Tag;
            public Action Then;
        }

        public static SimRunLog Run(SimOptions options, StoreNavigation navigation = null)
        {
            options ??= new SimOptions();
            var sk = SimSkillProfile.Get(options.Skill);
            var host = new ShiftSimHost(options.Seed, navigation);
            var botRng = new System.Random(unchecked(options.Seed * 1664525 + 1013904223));
            var log = new SimRunLog
            {
                Seed = options.Seed,
                SkillId = sk.Id,
            };

            host.StrikeLogged += (m, why) => log.Strikes.Add(new SimStrikeEntry { minute = m, why = why });

            double RandRange(double a, double b) => a + botRng.NextDouble() * (b - a);
            bool Ok(double mult = 1.0) => botRng.NextDouble() < 1.0 - (1.0 - sk.P) * mult;

            var known = new HashSet<string>();
            bool Safe(string key, double w = 1.0, double mult = 1.0) =>
                known.Contains(key) ? Ok(mult * 0.5) : botRng.NextDouble() > sk.F1 * w;

            float PathDist(Vector2 a, Vector2 b)
            {
                if (navigation != null && navigation.TryPath(a, b, out var pts) && pts.Count > 0)
                {
                    float total = 0f;
                    var cur = a;
                    for (int i = 0; i < pts.Count; ++i)
                    {
                        total += Vector2.Distance(cur, pts[i]);
                        cur = pts[i];
                    }
                    return total;
                }
                return Vector2.Distance(a, b);
            }

            var queue = new List<QueuedStep>();
            var reacted = new HashSet<string>();
            var skippedTasks = new HashSet<string>();
            var seenPhones = new HashSet<string>();
            var botAt = SimSpots.Get("counter");
            double busy = 0;
            double pendingStartedAt = 0;
            double pendingWait = 0;
            bool hasPending = false;
            bool deadHandled = false;
            bool redChecked = false;
            bool greetKey = false;
            int retries = 0;
            double prevMin = 0;
            double stallT = 0;
            bool stallLogged = false;
            double idleRun = 0;
            double[] phaseTime = new double[3];

            void Place(Vector2 pos, Vector2 face)
            {
                host.PlayerPos = pos;
                host.PlayerForward = face.sqrMagnitude > 1e-6f ? face.normalized : new Vector2(1f, 0f);
            }

            void Enqueue(string spotName, Action then, double extra = 0, string tag = null, Vector2? face = null)
            {
                queue.Add(new QueuedStep
                {
                    Spot = SimSpots.Get(spotName),
                    Face = face ?? new Vector2(1f, 0f),
                    Extra = extra,
                    Tag = tag,
                    Then = then,
                });
            }

            host.NewShift();
            Place(SimSpots.Get("counter"), new Vector2(1f, 0f));

            double dt = Math.Max(0.01, options.Dt);
            double t = 0;
            int steps = 0;

            while (t < options.MaxSimSeconds && steps < options.MaxSteps)
            {
                ++steps;
                t += dt;
                log.SimSeconds = t;

                try
                {
                    host.Tick(dt, greetKey);
                }
                catch (Exception e)
                {
                    log.Warnings.Add("update: " + e.Message);
                }
                greetKey = false;

                double m = host.Run.Clock.Minutes;
                int ph = m < 120 ? 0 : m < 240 ? 1 : 2;
                phaseTime[ph] += dt;

                if (host.Ended)
                {
                    if (host.DialogOpen) host.SelectDialogOption(0);
                    log.Ending = host.Ending ?? (host.HasFlag("jiho") ? "B" : "A");
                    break;
                }

                if (host.Dead)
                {
                    if (!deadHandled)
                    {
                        deadHandled = true;
                        int dr = host.LastDeathRule;
                        if (SimTrapMap.ByDeathRule.TryGetValue(dr, out var trapKey)) known.Add(trapKey);
                        if (dr == 12) known.Add("clock444");
                        log.Deaths.Add(new SimDeathEntry { minute = (int)Math.Round(m), rule = dr, why = host.LastDeathReason });
                    }
                    if (++retries > options.MaxRetries)
                    {
                        log.Warnings.Add("quá nhiều lần chết");
                        break;
                    }
                    host.Retry();
                    deadHandled = false;
                    queue.Clear();
                    busy = 0;
                    hasPending = false;
                    reacted.Clear();
                    skippedTasks.Clear();
                    seenPhones.Clear();
                    redChecked = false;
                    Place(SimSpots.Get("counter"), new Vector2(1f, 0f));
                    botAt = SimSpots.Get("counter");
                    continue;
                }

                // 1. Dialogue handling
                if (host.DialogOpen)
                {
                    if (!hasPending)
                    {
                        hasPending = true;
                        pendingStartedAt = t;
                        pendingWait = RandRange(sk.ReadMin, sk.ReadMax);
                        if (host.CurrentDialog.Options.Length > 1)
                            log.Decisions.Add(new SimDecisionEntry { time = t, minute = m, kind = "dialog:" + host.CurrentDialog.Speaker });
                    }
                    if (t - pendingStartedAt > pendingWait)
                    {
                        var opts = host.CurrentDialog.Options;
                        int pick = 0;
                        if (opts.Length > 1)
                        {
                            int good = Array.FindIndex(opts, o => o.Ok);
                            if (good < 0) good = 0;
                            int bad = Array.FindIndex(opts, o => !o.Ok);
                            string trap = opts.FirstOrDefault(o => !string.IsNullOrEmpty(o.Trap))?.Trap;
                            bool chooseGood;
                            if (!string.IsNullOrEmpty(trap))
                            {
                                double w = SimTrapMap.TrapWeight(trap, host.HasFlag("hqTrap"), host.HasFlag("fake13"));
                                chooseGood = Safe(trap, w);
                            }
                            else
                            {
                                chooseGood = Ok();
                            }
                            pick = chooseGood ? good : (bad >= 0 ? bad : good);
                        }
                        hasPending = false;
                        try
                        {
                            host.SelectDialogOption(pick);
                        }
                        catch (Exception e)
                        {
                            log.Warnings.Add("dialog: " + e.Message);
                        }
                    }
                    goto MeasureIdleAndStall;
                }
                else
                {
                    hasPending = false;
                }

                // 2. Greeting
                if (host.GreetWindow > 0)
                {
                    string gKey = "greet" + host.GreetSerial;
                    if (reacted.Add(gKey))
                    {
                        bool ghost = host.GreetGhost;
                        log.Decisions.Add(new SimDecisionEntry { time = t, minute = m, kind = ghost ? "chuông-ma" : "chào" });
                        bool doGreet = ghost ? !Ok(0.8) : Ok(0.6);
                        double delay = RandRange(sk.ReactMin, sk.ReactMax);
                        if (doGreet && delay < 5.5) host.Later(delay, () => greetKey = true);
                    }
                }

                // 3. Hazards
                if (host.HasFlag("blackout"))
                {
                    string bKey = "black" + host.Run.Progress.SavedHour + "_" + (int)Math.Floor(m / 60);
                    if (reacted.Add(bKey))
                    {
                        log.Decisions.Add(new SimDecisionEntry { time = t, minute = m, kind = "mất-điện" });
                        double travel = PathDist(botAt, SimSpots.Get("counter")) / sk.Speed + RandRange(sk.ReactMin, sk.ReactMax) +
                            (known.Contains("blackout") ? 0 : RandRange(0, 4));
                        queue.Clear();
                        busy = t + travel + 40;
                        bool looks = !Safe("blackout", 0.35, 0.6);
                        host.Later(Math.Min(travel, 30), () =>
                        {
                            Place(SimSpots.Get("counter"), looks ? new Vector2(0.2f, 1f) : new Vector2(-1f, 0f));
                            host.PlayerCrouch = 1f;
                            botAt = SimSpots.Get("counter");
                        });
                        host.Later(40, () =>
                        {
                            host.PlayerCrouch = 0f;
                            Place(SimSpots.Get("counter"), new Vector2(1f, 0f));
                        });
                    }
                }

                if (host.HasFlag("double"))
                {
                    string cKey = "cctv" + (int)Math.Floor(m / 10);
                    if (reacted.Add(cKey))
                    {
                        log.Decisions.Add(new SimDecisionEntry { time = t, minute = m, kind = "camera" });
                        bool near = Vector2.Distance(botAt, new Vector2(0.8f, 3f)) < 5f;
                        double notice = (near ? RandRange(1, 4) : RandRange(3, 10)) + (known.Contains("cctv") ? 0 : RandRange(0, 6));
                        double travel = PathDist(botAt, SimSpots.Get("monitor")) / sk.Speed;
                        if (Ok(0.7)) host.Later(notice + travel + 10, () => { if (host.HasFlag("double")) host.CctvOff(); });
                    }
                }

                if (host.PhoneRinging)
                {
                    string k = host.PhoneKind;
                    string pKey = k + "_" + (int)Math.Floor(m);
                    if (seenPhones.Add(pKey))
                    {
                        log.Decisions.Add(new SimDecisionEntry { time = t, minute = m, kind = "điện-thoại-" + k });
                        bool at444 = k == "444" || (int)Math.Floor(m) == 284;
                        bool fake12 = host.HasFlag("fake12");
                        bool answer = k == "self" ? !Safe("selfCall", 0.9) :
                            k == "clock444" ? !Safe("clock444", 0.75) :
                            at444 ? !Safe("phone444", fake12 ? 0.8 : 0.3) :
                            botRng.NextDouble() < 0.85;
                        double travel = PathDist(botAt, SimSpots.Get("phone")) / sk.Speed + RandRange(sk.ReactMin, sk.ReactMax);
                        if (answer && travel < 18)
                        {
                            host.Later(travel, () =>
                            {
                                if (host.PhoneRinging)
                                {
                                    Place(SimSpots.Get("phone"), new Vector2(-1f, 0f));
                                    botAt = SimSpots.Get("phone");
                                    host.AnswerPhone();
                                }
                            });
                        }
                    }
                }

                if (host.HasStarer && reacted.Add("starer" + host.StarerId))
                {
                    log.Decisions.Add(new SimDecisionEntry { time = t, minute = m, kind = "người-ngoài-kính" });
                    if (!Ok(0.7))
                    {
                        host.Later(RandRange(1, 6), () =>
                        {
                            if (host.HasStarer && host.StarerLookAcc >= 0)
                            {
                                host.StarerLookAcc = -999;
                                host.Strike(11, "Bạn đã nhìn lại người đứng ngoài cửa kính quá 3 giây.");
                            }
                        });
                    }
                }

                if (host.HasFlag("knock") && m >= 120 && m < 130 && reacted.Add("knock"))
                {
                    log.Decisions.Add(new SimDecisionEntry { time = t, minute = m, kind = "tủ-đông-gõ" });
                    if (!Safe("freezer", 0.35, 0.4))
                    {
                        host.Later(RandRange(20, 50), () =>
                        {
                            if (host.Run.Clock.Minutes < 130)
                            {
                                botAt = SimSpots.Get("freezer");
                                Place(botAt, new Vector2(0f, -1f));
                                host.OpenFreezer();
                            }
                        });
                    }
                }

                if (host.HasFlag("knock323") && reacted.Add("323"))
                {
                    log.Decisions.Add(new SimDecisionEntry { time = t, minute = m, kind = "gõ-3-2-3" });
                    if (host.Run.Progress.Diaries.Count >= 4 && botRng.NextDouble() < 0.85)
                        Enqueue("freezer", host.OpenFreezer);
                }

                if (host.HasFlag("alleyCall") && reacted.Add("alley" + (int)Math.Floor(m)))
                {
                    log.Decisions.Add(new SimDecisionEntry { time = t, minute = m, kind = "tiếng-gọi-hẻm" });
                    if (!Safe("alley", 0.6, 0.8))
                    {
                        host.Later(RandRange(1.5, 3), () =>
                        {
                            if (host.HasFlag("alleyCall"))
                                Place(host.PlayerPos, host.CallPosition - host.PlayerPos);
                        });
                    }
                }

                if (host.HasFlag("red"))
                {
                    if (reacted.Add("red"))
                    {
                        log.Decisions.Add(new SimDecisionEntry { time = t, minute = m, kind = "giờ-đỏ" });
                        queue.Clear();
                    }
                    if (host.RedDark && !redChecked)
                    {
                        redChecked = true;
                        if (!Safe("red", 0.35, 0.5))
                            host.PlayerPos += new Vector2(0.4f, 0f);
                    }
                    if (!host.RedDark) redChecked = false;
                    goto MeasureIdleAndStall;
                }

                if (host.HasFlag("chase"))
                {
                    string chaseKey = "chase" + host.ChaseDeadline.ToString("F2", CultureInfo.InvariantCulture) + "_" + (int)Math.Floor(m);
                    if (reacted.Add(chaseKey))
                    {
                        log.Decisions.Add(new SimDecisionEntry { time = t, minute = m, kind = "đèn-tắt-dần" });
                        var kp = host.ChaseSafeLight;
                        double travel = PathDist(botAt, kp) / (sk.Speed * 1.4) + RandRange(sk.ReactMin, sk.ReactMax) +
                            (known.Contains("chase") ? 0 : RandRange(1, 5));
                        if (Safe("chase", 0.55) && travel < host.ChaseDeadline)
                        {
                            queue.Clear();
                            busy = t + travel + 2;
                            host.Later(travel, () =>
                            {
                                Place(kp, new Vector2(1f, 0f));
                                botAt = kp;
                            });
                        }
                    }
                }

                // 4. Counter customer & microwave
                if (host.HasCounterCustomer && !host.Scanning && !host.DialogOpen)
                {
                    string sKey = "serve" + host.CounterCustomerId +
                        (host.CounterCustomerHeatAsked ? "h" : "") +
                        (host.Carry == "hot" ? "H" : "") +
                        ((int)Math.Floor(m / 3)).ToString(CultureInfo.InvariantCulture);
                    if (reacted.Add(sKey))
                    {
                        int expectedId = host.CounterCustomerId;
                        queue.Insert(0, new QueuedStep
                        {
                            Spot = SimSpots.Get("counter"),
                            Then = () => { if (host.HasCounterCustomer && host.CounterCustomerId == expectedId) host.ServeAtCounter(); },
                        });
                        if (!host.CounterCustomerHeatAsked || host.Carry == "hot") busy = Math.Min(busy, t);
                    }
                }

                if (host.Carry == "heat" && !queue.Any(q => q.Tag == "heat"))
                    queue.Insert(0, new QueuedStep { Tag = "heat", Spot = SimSpots.Get("micro"), Extra = 1, Then = () => host.Use("micro1") });

                if (host.MicrowaveReady && !queue.Any(q => q.Tag == "heat2"))
                    queue.Insert(0, new QueuedStep
                    {
                        Tag = "heat2",
                        Spot = SimSpots.Get("micro"),
                        Then = () =>
                        {
                            host.Use(host.ActiveMicrowaveOven);
                            if (botRng.NextDouble() < sk.Diary && !host.Run.Progress.Diaries.Contains("micro2"))
                                host.FindDiary("micro2");
                        },
                    });

                // 5. Tasks
                if (queue.Count == 0 && t >= busy && !host.DialogOpen)
                {
                    var nextTask = host.Tasks.Items.FirstOrDefault(item => !item.Done && !skippedTasks.Contains(item.Id));
                    if (nextTask != null) PlanTask(host, nextTask.Id, Enqueue, queue, skippedTasks, botRng, sk, Place);
                }

                // 6. Execute queue
                if (queue.Count > 0 && t >= busy)
                {
                    var q = queue[0];
                    queue.RemoveAt(0);
                    double travel = PathDist(botAt, q.Spot) / sk.Speed + q.Extra;
                    busy = t + travel;
                    botAt = q.Spot;
                    var face = q.Face;
                    host.Later(travel, () =>
                    {
                        Place(q.Spot, face);
                        try
                        {
                            q.Then?.Invoke();
                        }
                        catch (Exception e)
                        {
                            log.Warnings.Add("task: " + e.Message);
                        }
                    });
                }

            MeasureIdleAndStall:
                bool something = host.DialogOpen || queue.Count > 0 || t < busy ||
                    host.Tasks.Items.Any(item => !item.Done) || host.LiveVisitors() > 0 ||
                    host.GreetWindow > 0 || host.PhoneRinging || host.HasFlag("double") || host.HasFlag("blackout");
                if (!something)
                {
                    idleRun += dt;
                }
                else if (idleRun > 0)
                {
                    if (idleRun > 8) log.Idle.Add(new SimIdleEntry { minute = (int)Math.Round(m), seconds = Math.Round(idleRun, 1) });
                    log.MaxIdle = Math.Max(log.MaxIdle, idleRun);
                    idleRun = 0;
                }

                if (!log.FirstDecision.HasValue && log.Decisions.Count > 0)
                    log.FirstDecision = Math.Round(t, 1);

                if (Math.Abs(m - prevMin) > 1e-6)
                {
                    prevMin = m;
                    stallT = 0;
                }
                else if ((stallT += dt) > 40 && !stallLogged)
                {
                    stallLogged = true;
                    log.StallDetected = true;
                    log.Warnings.Add($"ĐỨNG ĐỒNG HỒ @{m:F1} dialog={host.DialogOpen} red={host.HasFlag("red")} dead={host.Dead} ended={host.Ended}");
                }
            }

            if (!host.Ended && retries <= options.MaxRetries && (t >= options.MaxSimSeconds || steps >= options.MaxSteps))
                log.HitStepOrTimeCap = true;

            log.Steps = steps;
            log.TasksDone = host.Run.Progress.Flags.GetValueOrDefault("tDone");
            log.TasksAll = host.Run.Progress.Flags.GetValueOrDefault("tAll");
            log.Retries = retries;
            log.Reached6 = host.Ended;
            log.Diaries = host.Run.Progress.Diaries.Count;
            log.SimMinutes = Math.Round(t / 60.0, 1);
            for (int i = 0; i < 3; ++i) log.PhaseMin[i] = Math.Round(phaseTime[i] / 60.0, 1);

            var byPh = new[] { new List<SimDecisionEntry>(), new List<SimDecisionEntry>(), new List<SimDecisionEntry>() };
            foreach (var d in log.Decisions) byPh[d.minute < 120 ? 0 : d.minute < 240 ? 1 : 2].Add(d);
            for (int i = 0; i < 3; ++i)
            {
                log.DecisionsPerPhase[i] = byPh[i].Count;
                if (byPh[i].Count >= 2)
                {
                    double sum = 0;
                    for (int j = 1; j < byPh[i].Count; ++j) sum += byPh[i][j].time - byPh[i][j - 1].time;
                    log.AvgGapPerPhase[i] = Math.Round(sum / (byPh[i].Count - 1), 1);
                }
            }

            return log;
        }

        static void PlanTask(
            ShiftSimHost host,
            string id,
            Action<string, Action, double, string, Vector2?> enqueue,
            List<QueuedStep> queue,
            HashSet<string> skipped,
            System.Random botRng,
            SimSkillProfile sk,
            Action<Vector2, Vector2> place)
        {
            switch (id)
            {
                case "rules":
                    enqueue("rules", () => host.Use("rules"), 6, null, null);
                    break;
                case "temp":
                case "temp2":
                    enqueue("temp", () => host.Use("fridgeTemp"), 1, null, null);
                    break;
                case "ramen":
                    enqueue("boxRamen", () =>
                    {
                        host.Use("boxRamen");
                        if (botRng.NextDouble() < sk.Diary * 0.5 && !host.Run.Progress.Diaries.Contains("locker"))
                            host.FindDiary("locker");
                    }, 1.5, null, null);
                    enqueue("shelfRamen", () => host.Use("shelfRamen"), 4, null, null);
                    break;
                case "milk":
                    enqueue("boxMilk", () => host.Use("boxMilk"), 1.5, null, null);
                    enqueue("shelfMilk", () => host.Use("shelfMilk"), 4, null, null);
                    break;
                case "mop":
                    enqueue("mop", () => host.Use("mop"), 1, null, null);
                    enqueue("spill", host.CleanSpill, 3, null, null);
                    enqueue("mop", () => host.Use("mop"), 1, null, null);
                    break;
                case "prints":
                    enqueue("mop", () => host.Use("mop"), 1, null, null);
                    enqueue("glass", host.CleanPrints, 3, null, null);
                    enqueue("mop", () => host.Use("mop"), 1, null, null);
                    break;
                case "trash":
                    enqueue("trash", () => host.Use("trash"), 1, null, null);
                    enqueue("backDoor", () => { if (!host.BackDoorOpen) host.Use("backDoor"); }, 1, null, null);
                    enqueue("dumpster", () =>
                    {
                        host.Use("dumpster");
                        if (botRng.NextDouble() < sk.Diary)
                            host.Later(4, () => { if (!host.Run.Progress.Diaries.Contains("dumpster")) host.FindDiary("dumpster"); });
                    }, 1, null, new Vector2(1f, -0.6f));
                    queue.Add(new QueuedStep { Spot = new Vector2(4.0f, -7.0f), Face = new Vector2(-0.6f, 0.8f), Extra = 1.5 });
                    enqueue("backDoor", () => place(new Vector2(2.0f, -3.6f), new Vector2(0f, 1f)), 1, null, new Vector2(0f, 1f));
                    break;
                case "fallen":
                    enqueue("aisle", () => host.Use("fallen"), 1, null, null);
                    break;
                case "fridgeClose":
                    enqueue("fridge", host.CloseFridge, 1, null, null);
                    break;
                case "storage":
                    enqueue("storage", null, 2, null, null);
                    break;
                case "aisle":
                    enqueue("aisle", null, 1, null, null);
                    break;
                case "door":
                    enqueue("door", null, 1, null, null);
                    break;
                case "delivery":
                    enqueue("backDoor", () => { if (!host.BackDoorOpen) host.Use("backDoor"); }, 1, null, null);
                    queue.Add(new QueuedStep { Spot = new Vector2(2.4f, -5.6f), Extra = 6, Then = host.DeliveryDialog });
                    enqueue("counter", null, 0, null, null);
                    break;
                default:
                    skipped.Add(id);
                    break;
            }

            if (botRng.NextDouble() < sk.Diary * 0.25)
            {
                if (!host.Run.Progress.Diaries.Contains("drawer"))
                    enqueue("drawer", () => host.FindDiary("drawer"), 2, null, null);
                else if (!host.Run.Progress.Diaries.Contains("rulesBack") && host.RulesRead)
                    enqueue("rules", () => host.FindDiary("rulesBack"), 3, null, null);
            }
        }

        public static SimBatchSummary RunBatch(
            SimSkillKind skill, int runs, int startSeed, StoreNavigation navigation = null, SimOptions template = null)
        {
            var profile = SimSkillProfile.Get(skill);
            var summary = new SimBatchSummary
            {
                SkillId = profile.Id,
                SkillLabel = profile.Label,
                Runs = runs,
                MinDeaths = int.MaxValue,
            };

            for (int i = 0; i < runs; ++i)
            {
                var opt = new SimOptions
                {
                    Skill = skill,
                    Seed = startSeed + i,
                    Dt = template?.Dt ?? 0.1,
                    MaxSimSeconds = template?.MaxSimSeconds ?? 12000,
                    MaxSteps = template?.MaxSteps ?? 125000,
                    MaxRetries = template?.MaxRetries ?? 45,
                };
                var log = Run(opt, navigation);
                summary.Logs.Add(log);
                if (log.Reached6) ++summary.Reached6Count;
                if (log.Ending == "A") ++summary.EndingACount;
                else if (log.Ending == "B") ++summary.EndingBCount;
                if (log.StallDetected) ++summary.TotalStalls;
                summary.TotalWarnings += log.Warnings.Count;
                summary.MinDeaths = Math.Min(summary.MinDeaths, log.Deaths.Count);
                summary.MaxDeaths = Math.Max(summary.MaxDeaths, log.Deaths.Count);
            }

            if (runs > 0)
            {
                summary.AvgDeaths = Math.Round(summary.Logs.Average(l => l.Deaths.Count), 2);
                summary.AvgStrikes = Math.Round(summary.Logs.Average(l => l.Strikes.Count), 2);
                summary.AvgRetries = Math.Round(summary.Logs.Average(l => l.Retries), 2);
                summary.AvgSimMinutes = Math.Round(summary.Logs.Average(l => l.SimMinutes), 1);
                var sorted = summary.Logs.Select(l => l.Deaths.Count).OrderBy(x => x).ToArray();
                summary.MedianDeaths = sorted.Length % 2 == 1
                    ? sorted[sorted.Length / 2]
                    : (sorted[sorted.Length / 2 - 1] + sorted[sorted.Length / 2]) / 2.0;
            }
            else
            {
                summary.MinDeaths = 0;
            }

            return summary;
        }
    }
}
