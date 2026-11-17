// Open-field infantry wave sweep. Uses the real Treeline.Sim battle engine, no UnityEngine.
// Compile: mcs -out:open-field.exe unity/Assets/Scripts/Sim/*.cs tools/balance/OpenField.cs
// Run: mono open-field.exe <output.csv> [repo-root]
// The field is empty: no cover, obstacles, trenches or terrain modifiers.
using System;
using System.Globalization;
using System.IO;
using System.Text;
using Treeline.Sim;

static class OpenField
{
    const float Dt = 0.05f;
    static readonly string[] Defenders = { "rifle_squad", "machine_gun", "sniper" };
    static readonly string[] Attackers = { "recruit", "regular", "special_forces" };
    static readonly int[] Counts = { 1, 2, 4, 8, 12 };

    static int Main(string[] args)
    {
        string output = args.Length > 0 ? args[0] : "open-field.csv";
        string root = args.Length > 1 ? args[1] : ".";
        string dir = Path.GetDirectoryName(Path.GetFullPath(output));
        Directory.CreateDirectory(dir);
        string data = Path.Combine(root, "unity/Assets/Data");
        var units = UnitTable.Parse(File.ReadAllText(Path.Combine(data, "units.csv")));
        var armour = ArmourTable.Parse(File.ReadAllText(Path.Combine(data, "armour.csv")));
        var obstacles = ObstacleTable.Parse(File.ReadAllText(Path.Combine(data, "obstacles.csv")));
        var rules = BattleRules.Parse(File.ReadAllText(Path.Combine(data, "battle_rules.csv")));

        var csv = new StringBuilder("defender,attacker,count,seed,killed,crossed,penetration_rate,line_damage,defender_survived,defender_hp_left,defender_damage_dealt,attacker_damage_dealt,seconds,completed\n");
        foreach (string defender in Defenders)
        foreach (string attacker in Attackers)
        foreach (int count in Counts)
        for (int seed = 1; seed <= 20; seed++)
        {
            var field = new Field { Name = "open-field", Width = 40f, Height = 300f };
            var grid = new NavGrid(field, obstacles);
            var vision = new VisionMap(field, obstacles);
            // Very high line health: count every crossing, not only crossings before the line breaks.
            var battle = new Battle(grid, units, 10000f, vision, armour, 1000f, seed, rules);
            Defender placed;
            string why;
            if (!battle.TryPlace(defender, 20f, 200f, out placed, out why))
                throw new Exception("Could not place " + defender + ": " + why);
            battle.BeginWave();
            if (!battle.QueueWave(attacker, count, 0f, 1.5f))
                throw new Exception("Could not queue " + attacker);
            battle.StartRound();
            while (battle.Outcome == BattleOutcome.Ongoing && battle.RoundTime < 400f)
                battle.Step(Dt);

            UnitStats attStats;
            if (!battle.Stats.TryGetValue(attacker, out attStats)) attStats = new UnitStats();
            UnitStats defStats;
            if (!battle.Stats.TryGetValue(defender, out defStats)) defStats = new UnitStats();
            bool completed = battle.Outcome != BattleOutcome.Ongoing;
            csv.Append(defender).Append(',').Append(attacker).Append(',').Append(count).Append(',').Append(seed)
               .Append(',').Append(attStats.Killed).Append(',').Append(attStats.Crossed)
               .Append(',').Append(F((float)attStats.Crossed / count))
               .Append(',').Append(F(battle.LineDamage))
               .Append(',').Append(placed.Alive ? "TRUE" : "FALSE")
               .Append(',').Append(F(placed.Alive ? placed.Hp : 0f))
               .Append(',').Append(F(defStats.DamageDealt))
               .Append(',').Append(F(attStats.DamageDealt))
               .Append(',').Append(F(battle.RoundTime))
               .Append(',').Append(completed ? "TRUE" : "FALSE").Append('\n');
        }
        File.WriteAllText(output, csv.ToString());
        return 0;
    }
    static string F(float x) => x.ToString("0.####", CultureInfo.InvariantCulture);
}
