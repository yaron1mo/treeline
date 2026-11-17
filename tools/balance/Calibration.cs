// Controlled Monte Carlo and one-at-a-time sensitivity screening for infantry balance.
// No Unity dependencies. Runner keeps the authoritative units.csv unchanged.
// Compile: mcs -langversion:latest -out:calibration.exe unity/Assets/Scripts/Sim/*.cs tools/balance/Calibration.cs
// Run: mono calibration.exe balance-output/calibration-runs.csv .
// Scenario: one unfortified defender at (20,200), 40x300m open field.
// "spawn_gap_s" is a release interval, NOT guaranteed physical separation; spawns can start at different X positions.
using System;
using System.Globalization;
using System.IO;
using System.Text;
using Treeline.Sim;

static class Calibration
{
    const float Dt = 0.05f;
    const int BaselineSeeds = 100;
    const int SensitivitySeeds = 40;
    static readonly string[] Defenders = { "rifle_squad", "machine_gun", "sniper" };
    static readonly string[] Attackers = { "recruit", "regular", "special_forces" };
    static readonly int[] Counts = { 1, 2, 3, 4, 5 };
    static readonly float[] Gaps = { 0.75f, 1.5f, 3.0f };
    static readonly string[] Parameters = {
        "defender_hp", "defender_damage", "defender_attack_speed",
        "defender_accuracy", "defender_sight_range", "attacker_hp", "attacker_speed"
    };
    static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    static float ReadStat(UnitDef u, string param)
    {
        switch (param)
        {
            case "defender_hp": case "attacker_hp": return u.Hp;
            case "defender_damage": return u.Damage;
            case "defender_attack_speed": return u.AttackSpeed;
            case "defender_accuracy": return u.Accuracy;
            case "defender_sight_range": return u.SightRangeM;
            case "attacker_speed": return u.SpeedMps;
        }
        throw new ArgumentException(param);
    }

    static void SetStat(UnitDef u, string param, float val)
    {
        val = Math.Max(0.01f, val);
        switch (param)
        {
            case "defender_hp": case "attacker_hp": u.Hp = val; return;
            case "defender_damage": u.Damage = val; return;
            case "defender_attack_speed": u.AttackSpeed = val; return;
            case "defender_accuracy": u.Accuracy = Math.Min(1f, val); return;
            case "defender_sight_range": u.SightRangeM = val; return;
            case "attacker_speed": u.SpeedMps = val; return;
        }
        throw new ArgumentException(param);
    }

    static string F(float f) => f.ToString("0.####", Inv);

    static void Simulate(StringBuilder csv, UnitTable units, ObstacleTable obstacles, ArmourTable armour,
        BattleRules rules, string experiment, string defender, string attacker, int count,
        float gap, string parameter, float factor, int seed)
    {
        var field = new Field { Name = "calibration-open-field", Width = 40f, Height = 300f };
        var grid = new NavGrid(field, obstacles);
        var vision = new VisionMap(field, obstacles);
        var battle = new Battle(grid, units, 10000f, vision, armour, 1000f, seed, rules);
        Defender d;
        string why;
        if (!battle.TryPlace(defender, 20f, 200f, out d, out why))
            throw new Exception("Cannot place " + defender + ": " + why);
        battle.BeginWave();
        if (!battle.QueueWave(attacker, count, 0f, gap))
            throw new Exception("Cannot queue " + attacker);
        battle.StartRound();
        while (battle.Outcome == BattleOutcome.Ongoing && battle.RoundTime < 400f)
            battle.Step(Dt);
        if (battle.Outcome == BattleOutcome.Ongoing)
            throw new Exception("Incomplete: " + defender + "/" + attacker + "/" + count + "/" + seed);
        UnitStats att;
        if (!battle.Stats.TryGetValue(attacker, out att)) att = new UnitStats();
        float hpPct = d.Alive ? 100f * Math.Max(0f, d.Hp) / d.Def.Hp : 0f;
        csv.Append(experiment).Append(',').Append(defender).Append(',').Append(attacker).Append(',')
           .Append(count).Append(',').Append(F(gap)).Append(',').Append(parameter).Append(',').Append(F(factor))
           .Append(',').Append(seed).Append(',').Append(att.Killed).Append(',').Append(att.Crossed)
           .Append(',').Append(att.Crossed == 0 ? "1" : "0")
           .Append(',').Append(d.Alive ? "1" : "0")
           .Append(',').Append(F(hpPct)).Append(',').Append(F(battle.RoundTime)).Append('\n');
    }

    static int Main(string[] args)
    {
        var output = args.Length > 0 ? args[0] : "calibration-runs.csv";
        var root = args.Length > 1 ? args[1] : ".";
        var data = Path.Combine(root, "unity/Assets/Data");
        var units = UnitTable.Parse(File.ReadAllText(Path.Combine(data, "units.csv")));
        var armour = ArmourTable.Parse(File.ReadAllText(Path.Combine(data, "armour.csv")));
        var obstacles = ObstacleTable.Parse(File.ReadAllText(Path.Combine(data, "obstacles.csv")));
        var rules = BattleRules.Parse(File.ReadAllText(Path.Combine(data, "battle_rules.csv")));
        var csv = new StringBuilder("experiment,defender,attacker,count,spawn_gap_s,parameter,factor,seed,killed,crossed,held,defender_survived,defender_hp_pct,seconds\n");

        // Full baseline: 1v1, 1v2, 1v3, 1v4, 1v5; three wave release intervals.
        foreach (string defender in Defenders)
        foreach (string attacker in Attackers)
        foreach (int count in Counts)
        foreach (float gap in Gaps)
        for (int seed = 1; seed <= BaselineSeeds; seed++)
            Simulate(csv, units, obstacles, armour, rules, "baseline", defender, attacker, count,
                gap, "none", 1f, seed);

        // OAT: +/-20% of ONE stat at a time; same seeds and fixed 1.5 s gap.
        // Includes defender stats, attacker HP and speed; no balance source files changed.
        foreach (string defender in Defenders)
        foreach (string attacker in Attackers)
        foreach (int count in new[] { 3, 4, 5 })
        foreach (string param in Parameters)
        foreach (float factor in new[] { 0.8f, 1.2f })
        {
            var affected = units.Get(param.StartsWith("defender_") ? defender : attacker);
            var original = ReadStat(affected, param);
            SetStat(affected, param, original * factor);
            try
            {
                for (int seed = 1; seed <= SensitivitySeeds; seed++)
                    Simulate(csv, units, obstacles, armour, rules, "sensitivity", defender, attacker, count,
                        1.5f, param, factor, seed);
            }
            finally { SetStat(affected, param, original); }
        }

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output)));
        File.WriteAllText(output, csv.ToString());
        Console.WriteLine("Monte Carlo baseline: " + (Defenders.Length*Attackers.Length*Counts.Length*Gaps.Length*BaselineSeeds));
        Console.WriteLine("OAT sensitivity: " + (Defenders.Length*Attackers.Length*3*Parameters.Length*2*SensitivitySeeds));
        return 0;
    }
}
