// Monte Carlo screen for anti-armour defenders; input CSV remains immutable.
// Compile: mcs -langversion:latest -out:armour-calibration.exe unity/Assets/Scripts/Sim/*.cs tools/balance/ArmourCalibration.cs
// Run: mono armour-calibration.exe balance-output/armour-runs.csv .
using System;
using System.Globalization;
using System.IO;
using System.Text;
using Treeline.Sim;

static class ArmourCalibration
{
    const float Dt = 0.05f;
    static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
    static readonly string[] Defenders = { "mortar", "anti_tank", "fpv_operator" };
    static readonly string[] Vehicles = { "old_tank", "modern_tank", "ifv", "mrap" };
    static readonly int[] Counts = { 1, 2, 3 };
    static readonly float[] Gaps = { 1.5f, 3f };
    const int BaselineSeeds = 100;
    const int SensitivitySeeds = 40;
    static string F(float x) { return x.ToString("0.####", Inv); }

    static void Run(StringBuilder csv, UnitTable units, ObstacleTable obstacles, ArmourTable armour,
        BattleRules rules, string defenderId, string vehicleId, int count, float gap,
        string experiment, float factor, int seed)
    {
        var field = new Field { Name = "anti-armour-open-field", Width = 40f, Height = 300f };
        var battle = new Battle(new NavGrid(field, obstacles), units, 10000f,
            new VisionMap(field, obstacles), armour, 1000f, seed, rules);
        Defender defender;
        string why;
        if (!battle.TryPlace(defenderId, 20f, 200f, out defender, out why))
            throw new Exception("Cannot place " + defenderId + ": " + why);
        // FPV pilot launches to a map point, scouts using the drone sensor; no recon required.
        if (defenderId == "fpv_operator" && !battle.TrySetDroneWaypoint(defender.Id, 20f, 80f, out why))
            throw new Exception("Cannot set FPV waypoint: " + why);
        battle.BeginWave();
        if (!battle.QueueWave(vehicleId, count, 0f, gap))
            throw new Exception("Cannot queue " + vehicleId);
        battle.StartRound();
        while (battle.Outcome == BattleOutcome.Ongoing && battle.RoundTime < 400f)
            battle.Step(Dt);
        if (battle.Outcome == BattleOutcome.Ongoing)
            throw new Exception("Incomplete " + defenderId + "/" + vehicleId + "/" + count + "/" + seed);

        UnitStats v, d;
        if (!battle.Stats.TryGetValue(vehicleId, out v)) v = new UnitStats();
        if (!battle.Stats.TryGetValue(defenderId, out d)) d = new UnitStats();
        // DamageTaken is capped to available HP per hit, including vehicles destroyed before crossing.
        // Carriers may unload troops; vehicle stats and total line damage are therefore separate metrics.
        float totalMaxHp = count * units.Get(vehicleId).Hp;
        float damagePct = totalMaxHp > 0f ? 100f * v.DamageTaken / totalMaxHp : 0f;
        float defenderHpPct = defender.Alive ? 100f * Math.Max(0f, defender.Hp) / defender.Def.Hp : 0f;
        csv.Append(defenderId).Append(',').Append(vehicleId).Append(',').Append(count).Append(',')
           .Append(F(gap)).Append(',').Append(experiment).Append(',').Append(F(factor)).Append(',')
           .Append(seed).Append(',').Append(v.Killed).Append(',').Append(v.Crossed).Append(',')
           .Append(v.Crossed == 0 ? "1" : "0").Append(',')
           .Append(battle.LineDamage <= 0f ? "1" : "0").Append(',')
           .Append(defender.Alive ? "1" : "0").Append(',')
           .Append(F(defenderHpPct)).Append(',').Append(F(damagePct)).Append(',')
           .Append(F(v.DamageTaken)).Append(',').Append(F(battle.LineDamage)).Append(',')
           .Append(F(d.DamageDealt)).Append(',').Append(F(battle.RoundTime)).Append('\n');
    }

    static void Main(string[] args)
    {
        var output = args.Length > 0 ? args[0] : "armour-runs.csv";
        var root = args.Length > 1 ? args[1] : ".";
        var data = Path.Combine(root, "unity/Assets/Data");
        var units = UnitTable.Parse(File.ReadAllText(Path.Combine(data, "units.csv")));
        var armour = ArmourTable.Parse(File.ReadAllText(Path.Combine(data, "armour.csv")));
        var obstacles = ObstacleTable.Parse(File.ReadAllText(Path.Combine(data, "obstacles.csv")));
        var rules = BattleRules.Parse(File.ReadAllText(Path.Combine(data, "battle_rules.csv")));
        var csv = new StringBuilder("defender,vehicle,count,spawn_gap_s,experiment,factor,seed,vehicles_killed,vehicles_crossed,stopped_all_vehicles,held_line,defender_survived,defender_hp_pct,vehicle_damage_pct,vehicle_damage_hp,line_damage,defender_damage_hp,seconds\n");

        foreach (var defender in Defenders)
        foreach (var vehicle in Vehicles)
        foreach (var count in Counts)
        foreach (var gap in Gaps)
        for (int seed = 1; seed <= BaselineSeeds; seed++)
            Run(csv, units, obstacles, armour, rules, defender, vehicle, count, gap, "baseline", 1f, seed);

        // Matched-seed sensitivity: double sight OR double per-hit damage.
        // These are hypothetical variants; neither the Sheets data nor repository CSV is modified.
        foreach (var defender in Defenders)
        foreach (var vehicle in Vehicles)
        foreach (int count in new[] { 1, 2 })
        foreach (var variant in new[] { "sight_x2", "hit_damage_x2" })
        {
            var weapon = units.Get(defender);
            float saved = variant == "sight_x2" ?
                (defender == "fpv_operator" ? weapon.DroneSightRangeM : weapon.SightRangeM) :
                (defender == "fpv_operator" ? weapon.HitDamage : weapon.Damage);
            if (variant == "sight_x2")
            {
                if (defender == "fpv_operator") weapon.DroneSightRangeM = saved * 2f;
                else weapon.SightRangeM = saved * 2f;
            }
            else if (defender == "fpv_operator") weapon.HitDamage = saved * 2f;
            else weapon.Damage = saved * 2f;
            try
            {
                for (int seed = 1; seed <= SensitivitySeeds; seed++)
                    Run(csv, units, obstacles, armour, rules, defender, vehicle, count, 1.5f, variant, 2f, seed);
            }
            finally
            {
                if (variant == "sight_x2")
                {
                    if (defender == "fpv_operator") weapon.DroneSightRangeM = saved;
                    else weapon.SightRangeM = saved;
                }
                else if (defender == "fpv_operator") weapon.HitDamage = saved;
                else weapon.Damage = saved;
            }
        }
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output)));
        File.WriteAllText(output, csv.ToString());
        Console.WriteLine("Armour baseline: 7200, sensitivity: 1920, total: 9120");
    }
}
