using System;
using System.Collections.Generic;
using UnityEngine;

namespace HanaMart.Sim
{
    public enum SimSkillKind
    {
        Careful,
        Average,
        Careless
    }

    public sealed class SimSkillProfile
    {
        static readonly SimSkillProfile CarefulProfile = new(
            SimSkillKind.Careful, "careful", "cẩn thận", 0.55, 0.96, 0.6, 1.8, 1.5, 3.0, 2.4, 0.75);

        static readonly SimSkillProfile AverageProfile = new(
            SimSkillKind.Average, "average", "trung bình", 0.7, 0.86, 0.9, 2.8, 2.0, 4.5, 2.2, 0.45);

        static readonly SimSkillProfile CarelessProfile = new(
            SimSkillKind.Careless, "careless", "ẩu", 0.85, 0.7, 1.2, 4.0, 1.0, 2.5, 2.6, 0.2);

        public SimSkillKind Kind { get; }
        public string Id { get; }
        public string Label { get; }
        public double F1 { get; }
        public double P { get; }
        public double ReactMin { get; }
        public double ReactMax { get; }
        public double ReadMin { get; }
        public double ReadMax { get; }
        public double Speed { get; }
        public double Diary { get; }

        public SimSkillProfile(
            SimSkillKind kind, string id, string label,
            double f1, double p,
            double reactMin, double reactMax,
            double readMin, double readMax,
            double speed, double diary)
        {
            Kind = kind;
            Id = id;
            Label = label;
            F1 = f1;
            P = p;
            ReactMin = reactMin;
            ReactMax = reactMax;
            ReadMin = readMin;
            ReadMax = readMax;
            Speed = speed;
            Diary = diary;
        }

        public static SimSkillProfile Get(SimSkillKind kind) => kind switch
        {
            SimSkillKind.Careful => CarefulProfile,
            SimSkillKind.Average => AverageProfile,
            SimSkillKind.Careless => CarelessProfile,
            _ => AverageProfile,
        };
    }

    public static class SimSpots
    {
        static readonly Dictionary<string, Vector2> Spots = new()
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

        public static IReadOnlyDictionary<string, Vector2> All => Spots;

        public static Vector2 Get(string key) => Spots[key];
    }

    public static class SimTrapMap
    {
        static readonly Dictionary<int, string> Map = new()
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

        public static IReadOnlyDictionary<int, string> ByDeathRule => Map;

        public static double TrapWeight(string trap, bool hqTrap, bool fake13) => trap switch
        {
            "manager" => hqTrap ? 0.9 : 0.4,
            "mirae" => fake13 ? 0.8 : 0.3,
            "raincoat" => 0.5,
            "delivery" => 0.6,
            "noShadow" => 1.0,
            "childMom" => 1.0,
            "dbl" => 0.65,
            "scream" => 0.75,
            "late" => 0.7,
            _ => 1.0,
        };
    }

    public sealed class SimOptions
    {
        public SimSkillKind Skill { get; set; } = SimSkillKind.Average;
        public int Seed { get; set; } = 1001;
        public double Dt { get; set; } = 0.1;
        public double MaxSimSeconds { get; set; } = 12000;
        public int MaxSteps { get; set; } = 125000;
        public int MaxRetries { get; set; } = 45;
    }

    [Serializable]
    public sealed class SimDeathEntry
    {
        public int minute;
        public int rule;
        public string why;
    }

    [Serializable]
    public sealed class SimStrikeEntry
    {
        public int minute;
        public string why;
    }

    [Serializable]
    public sealed class SimDecisionEntry
    {
        public double time;
        public double minute;
        public string kind;
    }

    [Serializable]
    public sealed class SimIdleEntry
    {
        public int minute;
        public double seconds;
    }

    [Serializable]
    public sealed class SimRunLog
    {
        public int Seed { get; set; }
        public string SkillId { get; set; }
        public List<SimDeathEntry> Deaths { get; } = new();
        public List<SimStrikeEntry> Strikes { get; } = new();
        public List<SimDecisionEntry> Decisions { get; } = new();
        public List<SimIdleEntry> Idle { get; } = new();
        public string Ending { get; set; }
        public int Retries { get; set; }
        public double SimSeconds { get; set; }
        public double SimMinutes { get; set; }
        public double[] PhaseMin { get; } = new double[3];
        public double MaxIdle { get; set; }
        public double? FirstDecision { get; set; }
        public int TasksDone { get; set; }
        public int TasksAll { get; set; }
        public int Diaries { get; set; }
        public bool Reached6 { get; set; }
        public bool StallDetected { get; set; }
        public bool HitStepOrTimeCap { get; set; }
        public int Steps { get; set; }
        public int[] DecisionsPerPhase { get; } = new int[3];
        public double?[] AvgGapPerPhase { get; } = new double?[3];
        public List<string> Warnings { get; } = new();
    }

    [Serializable]
    public sealed class SimBatchSummary
    {
        public string SkillId { get; set; }
        public string SkillLabel { get; set; }
        public int Runs { get; set; }
        public int Reached6Count { get; set; }
        public int EndingACount { get; set; }
        public int EndingBCount { get; set; }
        public int TotalStalls { get; set; }
        public int TotalWarnings { get; set; }
        public double AvgDeaths { get; set; }
        public double MedianDeaths { get; set; }
        public int MinDeaths { get; set; }
        public int MaxDeaths { get; set; }
        public double AvgStrikes { get; set; }
        public double AvgRetries { get; set; }
        public double AvgSimMinutes { get; set; }
        public List<SimRunLog> Logs { get; } = new();
    }
}
