// Balance runs for Treeline. Pure C# over Treeline.Sim; no Unity needed.
// Build and run (Mono):  mcs -out:balance.exe unity/Assets/Scripts/Sim/*.cs tools/balance/Balance.cs && mono balance.exe <out-dir> [repo-root] [shared|own]
// Writes CSV files into <out-dir>: duels.csv, maps.csv, waves.csv, crossing.csv.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Treeline.Sim;

static class Balance
{
    static string root = ".";
    static UnitTable units;
    static ArmourTable armour;
    static ObstacleTable obstacles;
    static BattleRules rules;
    const float Dt = 0.05f;

    static string Data(string n) => File.ReadAllText(Path.Combine(root, "unity/Assets/Data", n));
    static string F(float v) => v.ToString("0.##", CultureInfo.InvariantCulture);

    static readonly string[] Ground = { "recruit", "regular", "special_forces", "old_tank", "modern_tank", "ifv", "mrap", "buggy", "motorbike", "van" };
    static readonly string[] DefenderIds = { "rifle_squad", "machine_gun", "sniper", "mortar", "anti_tank", "fpv_operator", "bomber_operator" };

    static int Main(string[] args)
    {
        string outDir = args.Length > 0 ? args[0] : "balance-out";
        if (args.Length > 1) root = args[1];
        Directory.CreateDirectory(outDir);
        units = UnitTable.Parse(Data("units.csv"));
        armour = ArmourTable.Parse(Data("armour.csv"));
        obstacles = ObstacleTable.Parse(Data("obstacles.csv"));
        rules = BattleRules.Parse(Data("battle_rules.csv"));
        // Optional third argument: "own" = direct fire needs the shooter's own sight, "shared" = shared vision.
        if (args.Length > 2) rules.DirectFireNeedsOwnSight = args[2] == "own";

        Duels(Path.Combine(outDir, "duels.csv"));
        Crossing(Path.Combine(outDir, "crossing.csv"));
        Maps(Path.Combine(outDir, "maps.csv"), Path.Combine(outDir, "waves.csv"));
        return 0;
    }

    sealed class Run
    {
        public NavGrid Grid;
        public VisionMap Vision;
        public Battle Battle;
    }

    static Run Make(Field f, float budget, int seed, float lineHealth = 1000f)
    {
        var r = new Run { Grid = new NavGrid(f, obstacles), Vision = new VisionMap(f, obstacles) };
        r.Battle = new Battle(r.Grid, units, budget, r.Vision, armour, lineHealth, seed, rules);
        return r;
    }

    static void Finish(Battle b, float cap)
    {
        float t0 = b.RoundTime;
        while (b.Outcome == BattleOutcome.Ongoing && b.RoundTime - t0 < cap) b.Step(Dt);
    }

    static UnitStats S(Battle b, string id) => b.Stats.TryGetValue(id, out var s) ? s : new UnitStats();

    // ---- 1. One defender against a stream of one attacker type, open ground, the route passes 10 m away. ----
    static void Duels(string path)
    {
        var sb = new StringBuilder("defender,attacker,count,killed,crossed,line_damage,defender_survived,defender_hp_left,damage_dealt,damage_taken,build_cost,attackers_value_usd_killed\n");
        foreach (var d in DefenderIds)
            foreach (var a in Ground)
            {
                var def = units.Get(d);
                var att = units.Get(a);
                int count = att.Type == "vehicle" ? 3 : 8;
                var field = new Field { Name = "duel", Width = 40f, Height = 300f };
                var r = Make(field, 10000f, 1);
                r.Battle.TryPlace(d, 30f, 200f, out var placed, out _);
                if (d == "mortar") r.Battle.TryPlace("rifle_squad", 30f, 200f, out _, out _);   // mortars need a spotter; counted apart
                if (d == "fpv_operator" || d == "bomber_operator") r.Battle.TryPlace("recon_drone", 30f, 100f, out _, out _);   // drones need something that sees far
                r.Battle.BeginWave();
                r.Battle.QueueWave(a, count, 0f, att.Type == "vehicle" ? 4f : 1.5f);
                r.Battle.StartRound();
                Finish(r.Battle, 400f);
                var st = S(r.Battle, a);
                var ds = S(r.Battle, d);
                bool alive = placed != null && placed.Alive;
                float valueKilled = st.Killed * ValueOf(att);
                if (att.CarriesCount > 0 && units.TryGet(att.CarriesUnit, out var p))
                    valueKilled = S(r.Battle, a).Killed * ValueOf(att) + S(r.Battle, p.Id).Killed * ValueOf(p);
                sb.Append($"{d},{a},{count},{st.Killed},{st.Crossed},{F(r.Battle.LineDamage)},{alive},{F(alive ? placed.Hp : 0f)},{F(ds.DamageDealt)},{F(ds.DamageTaken)},{F(def.BuildCost)},{F(valueKilled)}\n");
            }
        File.WriteAllText(path, sb.ToString());
    }

    static float ValueOf(UnitDef u) => u.Id == "" ? 0f : LossValue(u);

    static float LossValue(UnitDef u)
    {
        // loss_value_usd is not in UnitDef; read it straight from the CSV once.
        if (lossValues == null)
        {
            lossValues = new Dictionary<string, float>();
            var rows = Csv.Parse(Data("units.csv"));
            int id = Array.IndexOf(rows[0], "id"), lv = Array.IndexOf(rows[0], "loss_value_usd");
            for (int i = 1; i < rows.Count; i++)
                if (rows[i].Length > lv && rows[i][lv].Trim().Length > 0)
                    lossValues[rows[i][id]] = float.Parse(rows[i][lv], CultureInfo.InvariantCulture);
        }
        return lossValues.TryGetValue(u.Id, out var v) ? v : 0f;
    }
    static Dictionary<string, float> lossValues;

    static IEnumerable<KeyValuePair<string, Field>> Fields()
    {
        foreach (var file in Directory.GetFiles(Path.Combine(root, "unity/Assets/Resources/Fields"), "*.json"))
        {
            string name = Path.GetFileNameWithoutExtension(file);
            if (name == "vision-test") continue;
            yield return new KeyValuePair<string, Field>(name, FieldParser.Parse(File.ReadAllText(file)));
        }
    }

    // ---- 2. Unopposed: how long each attacker needs to cross each map, and whether it can. ----
    static void Crossing(string path)
    {
        var sb = new StringBuilder("map,attacker,crossed,seconds,line_damage\n");
        foreach (var kv in Fields())
            foreach (var a in Ground)
            {
                var r = Make(kv.Value, 0f, 1);
                r.Battle.BeginWave();
                r.Battle.QueueWave(a, 1, 0f, 1f);
                r.Battle.StartRound();
                Finish(r.Battle, 600f);
                var st = S(r.Battle, a);
                sb.Append($"{kv.Key},{a},{st.Crossed},{F(r.Battle.RoundTime)},{F(r.Battle.LineDamage)}\n");
            }
        File.WriteAllText(path, sb.ToString());
    }

    // ---- 3. A standard defence on each map against five mixed waves, many seeds. ----
    // The defence is placed the way a sensible player would: next to where the attackers' routes run.
    static string[] Defence;
    static float[] Depth;

    // Same six defenders as the first run, then the same plus drone operators. Budgets are exactly what the units cost.
    sealed class Loadout { public string Name; public float Budget; public string[] Units; public float[] Depth; }
    static readonly Loadout[] Loadouts =
    {
        new Loadout { Name = "base", Budget = 1500f,
            Units = new[] { "machine_gun", "anti_tank", "sniper", "rifle_squad", "rifle_squad", "mortar" },
            Depth = new[] { 0.62f, 0.70f, 0.55f, 0.66f, 0.74f, 0.92f } },
        new Loadout { Name = "base+fpv", Budget = 1900f,
            Units = new[] { "machine_gun", "anti_tank", "sniper", "rifle_squad", "rifle_squad", "mortar", "fpv_operator", "recon_drone" },
            Depth = new[] { 0.62f, 0.70f, 0.55f, 0.66f, 0.74f, 0.92f, 0.90f, 0.45f } },
        new Loadout { Name = "base+fpv+bomber", Budget = 2350f,
            Units = new[] { "machine_gun", "anti_tank", "sniper", "rifle_squad", "rifle_squad", "mortar", "fpv_operator", "bomber_operator", "recon_drone" },
            Depth = new[] { 0.62f, 0.70f, 0.55f, 0.66f, 0.74f, 0.92f, 0.90f, 0.86f, 0.45f } },
    };

    static void MixedWave(Battle b)
    {
        b.BeginWave();
        b.QueueWave("recruit", 6, 0f, 1.5f);
        b.QueueWave("regular", 4, 4f, 2f);
        b.QueueWave("motorbike", 2, 2f, 1f);
        b.QueueWave("mrap", 1, 6f, 1f);
        b.QueueWave("old_tank", 1, 10f, 1f);
        b.QueueWave("van", 1, 14f, 1f);
    }

    static void Maps(string mapsPath, string wavesPath)
    {
        var maps = new StringBuilder("loadout,budget,map,seed,waves_survived,line_damage,defenders_lost,attackers_killed,attackers_crossed\n");
        var waves = new StringBuilder("loadout,map,seed,wave,line_damage_this_wave,crossed_this_wave,killed_this_wave,mode_fastest,mode_evade,mode_safest,defenders_left\n");
        var perUnit = new Dictionary<string, float[]>();   // spawned, killed, crossed, line damage, damage dealt
        foreach (var lo in Loadouts)
        foreach (var kv in Fields())
        {
            Defence = lo.Units; Depth = lo.Depth;
            for (int seed = 1; seed <= 20; seed++)
            {
                var r = Make(kv.Value, lo.Budget, seed, rules.LineHealth);
                PlaceDefence(r);
                r.Battle.StartRound();
                int survived = 0;
                for (int w = 1; w <= 5; w++)
                {
                    float line0 = r.Battle.LineDamage;
                    int crossed0 = TotalCrossed(r.Battle), killed0 = TotalKilledAttackers(r.Battle);
                    MixedWave(r.Battle);
                    int f = 0, e = 0, s = 0;
                    bool counted = false;
                    float t0 = r.Battle.RoundTime;
                    while (r.Battle.Outcome == BattleOutcome.Ongoing && r.Battle.RoundTime - t0 < 400f)
                    {
                        r.Battle.Step(Dt);
                        if (!counted && r.Battle.RoundTime - t0 > 16f)
                        {
                            counted = true;
                            foreach (var a in r.Battle.Attackers)
                            {
                                if (a.Mode == RouteMode.Evade) e++;
                                else if (a.Mode == RouteMode.Safest) s++;
                                else f++;
                            }
                        }
                    }
                    waves.Append($"{lo.Name},{kv.Key},{seed},{w},{F(r.Battle.LineDamage - line0)},{TotalCrossed(r.Battle) - crossed0},{TotalKilledAttackers(r.Battle) - killed0},{f},{e},{s},{r.Battle.Defenders.Count}\n");
                    if (r.Battle.Outcome == BattleOutcome.Lost) break;
                    survived = w;
                }
                maps.Append($"{lo.Name},{F(lo.Budget)},{kv.Key},{seed},{survived},{F(r.Battle.LineDamage)},{Defence.Length - r.Battle.Defenders.Count},{TotalKilledAttackers(r.Battle)},{TotalCrossed(r.Battle)}\n");
                foreach (var st in r.Battle.Stats)
                {
                    if (!perUnit.TryGetValue(lo.Name + "," + st.Key, out var acc)) { acc = new float[5]; perUnit[lo.Name + "," + st.Key] = acc; }
                    acc[0] += st.Value.Spawned; acc[1] += st.Value.Killed; acc[2] += st.Value.Crossed; acc[3] += st.Value.LineDamage; acc[4] += st.Value.DamageDealt;
                }
            }
        }
        File.WriteAllText(mapsPath, maps.ToString());
        File.WriteAllText(wavesPath, waves.ToString());
        var u = new StringBuilder("loadout,unit,spawned_or_placed,killed_or_lost,crossed,line_damage,damage_dealt\n");
        foreach (var kv in perUnit) u.Append($"{kv.Key},{kv.Value[0]},{kv.Value[1]},{kv.Value[2]},{F(kv.Value[3])},{F(kv.Value[4])}\n");
        File.WriteAllText(Path.Combine(Path.GetDirectoryName(mapsPath), "units_totals.csv"), u.ToString());
    }

    static int TotalCrossed(Battle b) { int n = 0; foreach (var s in b.Stats) n += s.Value.Crossed; return n; }

    static int TotalKilledAttackers(Battle b)
    {
        int n = 0;
        foreach (var s in b.Stats) if (units.TryGet(s.Key, out var u) && !u.IsDefence) n += s.Value.Killed;
        return n;
    }

    // Where do the first-wave routes run? Count route cells per column at each depth and put each defender
    // 8 m to the side of the busiest crossing, so it sees the route but does not stand on it.
    static void PlaceDefence(Run r)
    {
        var grid = r.Grid;
        var flows = new[] { new FlowField(grid, grid.Cost(UnitClass.Infantry)), new FlowField(grid, grid.Cost(UnitClass.Vehicle)) };
        var use = new int[grid.Count];
        var starts = new List<int>();
        if (grid.SpawnXs.Length > 0) foreach (var x in grid.SpawnXs) starts.Add(grid.CellAt(x, 0f));
        else for (int c = 0; c < grid.Cols; c += 2) starts.Add(c);
        foreach (var flow in flows)
            foreach (int s in starts)
                foreach (int cell in flow.Trace(s)) use[cell]++;

        for (int k = 0; k < Defence.Length; k++)
        {
            int row = Math.Min(grid.Rows - 1, (int)(grid.Rows * Depth[k]));
            int best = -1, bestUse = 0;
            for (int c = 0; c < grid.Cols; c++)
            {
                int i = row * grid.Cols + c;
                if (use[i] > bestUse) { bestUse = use[i]; best = i; }
            }
            if (best < 0) continue;
            float y = grid.CenterY(best);
            float side = (k % 2 == 0) ? 8f : -8f;
            float x = Math.Max(2f, Math.Min(grid.Width - 2f, grid.CenterX(best) + side));
            if (!r.Battle.TryPlace(Defence[k], x, y, out _, out _))
                r.Battle.TryPlace(Defence[k], grid.CenterX(best), y, out _, out _);
        }
    }
}
