// A scripted player runs the playtest waves (waves.csv, start_budget, wave_income) on each field (2026-11-16).
// It shows whether the waves can be held with sensible play and where a run usually breaks. Not a real player:
// before each wave it spends its money by a fixed order of needs (what the coming wave holds) on fixed spots in
// front of the entry points.
// Compile: mcs -langversion:latest -out:playtest-run.exe unity/Assets/Scripts/Sim/*.cs tools/balance/PlaytestRun.cs
// Run:     mono playtest-run.exe <repo root> [seeds] [strategy]
using System;
using System.Collections.Generic;
using System.IO;
using Treeline.Sim;

static class PlaytestRun
{
    static void Main(string[] args)
    {
        string root = args.Length > 0 ? args[0] : ".";
        int seeds = args.Length > 1 ? int.Parse(args[1]) : 20;
        string data = Path.Combine(root, "unity/Assets/Data");
        var units = UnitTable.Parse(File.ReadAllText(Path.Combine(data, "units.csv")));
        var obstacles = ObstacleTable.Parse(File.ReadAllText(Path.Combine(data, "obstacles.csv")));
        var armour = ArmourTable.Parse(File.ReadAllText(Path.Combine(data, "armour.csv")));
        var rules = BattleRules.Parse(File.ReadAllText(Path.Combine(data, "battle_rules.csv")));
        var plan = WavePlan.Parse(File.ReadAllText(Path.Combine(data, "waves.csv")));

        foreach (var file in Directory.GetFiles(Path.Combine(root, "unity/Assets/Resources/Fields"), "*.json"))
        {
            var field = FieldParser.Parse(File.ReadAllText(file));
            if (field.Name == "Vision test") continue;
            var held = new int[plan.WaveCount + 2];
            var line = new float[plan.WaveCount + 2];
            int wins = 0;
            for (int seed = 1; seed <= seeds; seed++)
            {
                var grid = new NavGrid(field, obstacles);
                var b = new Battle(grid, units, rules.StartBudget, new VisionMap(field, obstacles), armour, -1f, seed, rules);
                b.PlacementMinY = rules.PlacementZoneStart * field.Height;
                var spots = Spots(field);
                int spot = 0;
                for (int w = 1; w <= plan.WaveCount; w++)
                {
                    Buy(b, units, plan, w, spots, ref spot);
                    float before = b.LineDamage;
                    plan.Queue(b, w);
                    b.StartRound();
                    while (b.Outcome == BattleOutcome.Ongoing && b.RoundTime < 100000f) b.Step(0.05f);
                    line[w] += b.LineDamage - before;
                    if (Environment.GetEnvironmentVariable("PLAYTEST_VERBOSE") != null && seed == 1)
                    {
                        var have = new SortedDictionary<string, int>();
                        foreach (var d in b.Defenders) { have.TryGetValue(d.Def.Id, out int n); have[d.Def.Id] = n + 1; }
                        var crossed = new List<string>();
                        foreach (var kv in b.Stats) if (kv.Value.Crossed > 0) crossed.Add(kv.Key + " " + kv.Value.Crossed);
                        Console.WriteLine($"    seed 1 wave {w}: {b.Outcome}, line {b.LineDamage:0.#}, money {b.Budget:0}, alive: " +
                            string.Join(", ", ToList(have)) + " | crossed so far: " + string.Join(", ", crossed));
                    }
                    if (b.Outcome != BattleOutcome.Won) break;
                    held[w]++;
                    if (w == plan.WaveCount) wins++;
                    b.AddBudget(rules.WaveIncome);
                    b.BackToPreparation();
                }
            }
            Console.WriteLine($"{field.Name}: runs won {wins}/{seeds}");
            for (int w = 1; w <= plan.WaveCount; w++)
                Console.WriteLine($"  wave {w}: held in {held[w]}/{seeds}, line damage in it {line[w] / seeds:0.0}");
        }
    }

    static List<string> ToList(SortedDictionary<string, int> d)
    {
        var l = new List<string>();
        foreach (var kv in d) l.Add(kv.Key + " " + kv.Value);
        return l;
    }

    // Spots in front of the entry points (or across the field), from the back forward.
    static List<(float x, float y)> Spots(Field f)
    {
        var xs = new List<float>(f.Spawns);
        if (xs.Count == 0) { xs.Add(f.Width * 0.3f); xs.Add(f.Width * 0.7f); }
        var list = new List<(float, float)>();
        float[] depth = { 0.80f, 0.72f, 0.86f, 0.65f, 0.92f, 0.58f };
        float[] side = { 0f, 18f, -18f, 36f, -36f };
        foreach (float d in depth)
            foreach (float s in side)
                foreach (float x in xs)
                    list.Add((Math.Max(4f, Math.Min(f.Width - 4f, x + s)), f.Height * d));
        return list;
    }

    // What the coming wave needs (a target count per defender type); buy towards it, keep the rest of the money.
    static void Buy(Battle b, UnitTable units, WavePlan plan, int wave, List<(float x, float y)> spots, ref int spot)
    {
        int infantry = 0, armoured = 0, air = 0, fpv = 0;
        var groups = plan.WaveGroups(wave);
        // Look one wave ahead for the special threats, so money is there when the tanks come.
        foreach (var g in plan.WaveGroups(wave + 1))
        {
            var d = units.Get(g.UnitId);
            if (d.Type == "vehicle" && d.Armour != "none") armoured = Math.Max(armoured, 1);
            if (g.UnitId == "helicopter" || g.UnitId == "one_way_drone") air = Math.Max(air, 1);
        }
        foreach (var g in groups)
        {
            var d = units.Get(g.UnitId);
            if (d.Type == "troops") infantry += g.Count;
            if (d.Type == "vehicle" && d.Armour != "none") armoured += g.Count;
            if (d.Type == "vehicle" && d.CarriesCount > 0) infantry += g.Count * d.CarriesCount;
            if (g.UnitId == "helicopter") { air += g.Count; infantry += g.Count * d.CarriesCount; }
            if (g.UnitId == "one_way_drone") air += g.Count;
            if (g.UnitId == "fpv") fpv += g.Count;
        }
        var target = new List<(string id, int n)>
        {
            ("recon_drone", armoured > 0 || fpv > 0 || infantry >= 15 ? 1 : 0),
            ("anti_tank", Math.Min(3, armoured)),
            ("air_defence", air > 0 ? 1 : 0),
            ("machine_gun", Math.Min(5, (infantry + 4) / 5)),
            ("fpv_operator", armoured >= 2 ? 1 : 0),
            ("mortar", infantry >= 15 ? 1 : 0),
            ("rifle_squad", fpv > 0 ? 2 : 0),
        };
        foreach (var t in target)
        {
            string id = t.id;
            int n = t.n;
            int have = 0;
            foreach (var d in b.Defenders) if (d.Def.Id == id) have++;
            var def = units.Get(id);
            while (have < n && b.Budget >= def.BuildCost && spot < spots.Count)
            {
                var p = spots[spot++];
                if (b.TryPlace(id, p.x, p.y, out _, out _)) have++;
            }
        }
    }
}
