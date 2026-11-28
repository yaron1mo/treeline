// A scripted player runs the playtest waves (waves.csv, start_budget, wave_income, kill_reward_share) on each field
// (2026-11-16). It shows whether the waves can be held with sensible play and where a run usually breaks. Not a real
// player: it buys from a table of how many of each attacker one defender stops (tools/balance, matrix to 20 units).
// Styles:
//   saver    - before each wave buys only what that wave needs, keeps the rest of the money
//   greedy   - the same, then spends everything left on machine gunners and rifle squads
//   reactive - waits for the wave, sees which entry point it uses, then places (defenders run in) as saver does
// Houses near a route and hill tops that see it are scored as candidate spots first (Routes.TerrainSpots, issue #49).
// Compile: mcs -langversion:latest -out:playtest-run.exe unity/Assets/Scripts/Sim/*.cs tools/balance/Routes.cs tools/balance/PlaytestRun.cs
// Run:     mono playtest-run.exe <data root> [seeds] [style] [stop table csv]
//   stop table: defender,attacker,stopped (units of that attacker one defender stops in a run)
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Treeline.Sim;

static class PlaytestRun
{
    static Dictionary<string, Dictionary<string, float>> stop = new Dictionary<string, Dictionary<string, float>>();
    static readonly string[] Defenders = { "rifle_squad", "machine_gun", "sniper", "mortar", "anti_tank", "air_defence", "fpv_operator", "bomber_operator" };
    static readonly HashSet<string> NeedRecon = new HashSet<string> { "mortar", "anti_tank", "air_defence", "fpv_operator", "bomber_operator" };

    static void Main(string[] args)
    {
        string root = args.Length > 0 ? args[0] : ".";
        int seeds = args.Length > 1 ? int.Parse(args[1]) : 10;
        string style = args.Length > 2 ? args[2] : "saver";
        string data = Path.Combine(root, "unity/Assets/Data");
        var units = UnitTable.Parse(File.ReadAllText(Path.Combine(data, "units.csv")));
        var obstacles = ObstacleTable.Parse(File.ReadAllText(Path.Combine(data, "obstacles.csv")));
        var armour = ArmourTable.Parse(File.ReadAllText(Path.Combine(data, "armour.csv")));
        var rules = BattleRules.Parse(File.ReadAllText(Path.Combine(data, "battle_rules.csv")));
        var plan = WavePlan.Parse(File.ReadAllText(Path.Combine(data, "waves.csv")));
        string stopPath = args.Length > 3 ? args[3] : Path.Combine(root, "stop.csv");
        foreach (var line in File.ReadAllLines(stopPath))
        {
            var c = line.Split(',');
            if (c[0] == "defender") continue;
            if (!stop.ContainsKey(c[1])) stop[c[1]] = new Dictionary<string, float>();
            stop[c[1]][c[0]] = float.Parse(c[2], CultureInfo.InvariantCulture);
        }
        string fieldsDir = Path.Combine(root, "unity/Assets/Resources/Fields");
        if (!Directory.Exists(fieldsDir)) fieldsDir = Path.Combine(Environment.GetEnvironmentVariable("REPO") ?? ".", "unity/Assets/Resources/Fields");

        Console.WriteLine("field,style,seed,waves_held,line_damage,money_left,defenders,spent");
        foreach (var file in Directory.GetFiles(fieldsDir, "*.json"))
        {
            var field = FieldParser.Parse(File.ReadAllText(file));
            if (field.Name == "Vision test") continue;
            for (int seed = 1; seed <= seeds; seed++)
            {
                var grid = new NavGrid(field, obstacles);
                var vision = new VisionMap(field, obstacles);
                var b = new Battle(grid, units, rules.StartBudget, vision, armour,
                    Environment.GetEnvironmentVariable("PLAYTEST_LINE") != null ? float.Parse(Environment.GetEnvironmentVariable("PLAYTEST_LINE"), CultureInfo.InvariantCulture) : -1f, seed, rules);
                b.PlacementMinY = rules.PlacementZoneStart * field.Height;
                pvision = vision;
                lanes = Routes.Of(field.Name, grid, field.Height * 0.55f, field.Height * 0.9f);
                var rng = new Random(seed);
                int heldWaves = 0;
                float spent = 0f;
                for (int w = 1; w <= plan.WaveCount; w++)
                {
                    float before = b.Budget;
                    if (style != "reactive") spent += Buy(b, units, plan, w, field, null, rng, style == "greedy");
                    plan.Queue(b, w);
                    b.StartRound();
                    if (style == "reactive")
                    {
                        // Wait until the first attacker shows which way the wave comes.
                        while (b.Attackers.Count == 0 && b.Outcome == BattleOutcome.Ongoing && b.RoundTime < 100000f) b.Step(0.05f);
                        float? lane = b.Attackers.Count > 0 ? b.Attackers[0].X : (float?)null;
                        spent += Buy(b, units, plan, w, field, lane, rng, false);
                    }
                    float nextLook = b.RoundTime + 1f;
                    while (b.Outcome == BattleOutcome.Ongoing && b.RoundTime < 100000f)
                    {
                        b.Step(0.05f);
                        if (style == "reactive" && b.RoundTime >= nextLook)
                        {
                            nextLook = b.RoundTime + 1f;
                            foreach (var a in b.Attackers) if (a.Y < f0(field)) spent += Cover(b, units, a.X, field, rng);
                        }
                    }
                    if (Environment.GetEnvironmentVariable("PLAYTEST_VERBOSE") != null)
                    {
                        var ids = new List<string>();
                        foreach (var d in b.Defenders) ids.Add(d.Def.Id + "@" + d.X.ToString("0") + "," + d.Y.ToString("0"));
                        Console.Error.WriteLine($"  {field.Name} s{seed} w{w}: {b.Outcome} line {b.LineDamage:0.#} money {b.Budget:0} | {string.Join(" ", ids)}");
                    }
                    if (b.Outcome != BattleOutcome.Won) break;
                    heldWaves = w;
                    b.CollectRewards();
                    b.AddBudget(rules.WaveIncome);
                    b.BackToPreparation();
                }
                Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "{0},{1},{2},{3},{4:0.#},{5:0},{6},{7:0}",
                    field.Name, style, seed, heldWaves, b.LineDamage, b.Budget, b.Defenders.Count, spent));
            }
        }
    }

    static List<Routes.Lane> lanes;
    static VisionMap pvision;

    static float f0(Field f) => f.Height * 0.3f;   // still far up the field: time to run in

    static float Stop(string defender, string attacker) =>
        stop.TryGetValue(attacker, out var m) && m.TryGetValue(defender, out var v) ? v : 0f;

    // How many of each defender the wave needs: for every group, the cheapest defender per unit stopped, then sum.
    static float Buy(Battle b, UnitTable units, WavePlan plan, int wave, Field f, float? lane, Random rng, bool spendAll)
    {
        var need = new Dictionary<string, float>();
        // This wave and a look at the next one (a player saves for the tanks they can see coming), the larger need.
        var groups = new List<WavePlan.Group>(plan.WaveGroups(wave));
        var nextNeed = Need(units, plan.WaveGroups(wave + 1));
        foreach (var g in groups)
        {
            if (g.UnitId == "recon_uav") continue;
            string best = null;
            float bestCost = float.MaxValue;
            foreach (var d in Defenders)
            {
                float s = Stop(d, g.UnitId);
                if (s < 0.5f) continue;
                float c = units.Get(d).BuildCost / s;
                if (c < bestCost) { bestCost = c; best = d; }
            }
            if (best == null) continue;
            need.TryGetValue(best, out float n);
            need[best] = n + g.Count / Math.Max(0.5f, Stop(best, g.UnitId));
        }
        foreach (var kv in nextNeed)
        {
            need.TryGetValue(kv.Key, out float n);
            need[kv.Key] = Math.Max(n, kv.Value);
        }
        float spent = 0f;
        // Every entry point needs someone who can see it (no recon: sight is only 30-65 m).
        var laneXs = new List<float>();
        if (lane.HasValue) laneXs.Add(lane.Value); else foreach (var l in lanes) laneXs.Add(l.X);
        foreach (var x in laneXs) spent += Cover(b, units, x, f, rng);
        var have = new Dictionary<string, int>();
        foreach (var d in b.Defenders) { have.TryGetValue(d.Def.Id, out int h); have[d.Def.Id] = h + 1; }
        bool wantRecon = wave >= 3 && b.Budget >= 450f;   // recon lets long guns cover both lanes
        if (have.ContainsKey("recon_drone")) wantRecon = false;
        foreach (var kv in need) if (NeedRecon.Contains(kv.Key)) wantRecon = true;
        if (wantRecon && !have.ContainsKey("recon_drone")) spent += Place(b, units, "recon_drone", f, lane, rng);
        // Most needed first.
        var order = new List<KeyValuePair<string, float>>(need);
        order.Sort((x, y) => y.Value.CompareTo(x.Value));
        foreach (var kv in order)
        {
            have.TryGetValue(kv.Key, out int h);
            // Safety: twice what 1 v 1 says (two lanes, no trench, mixed waves); with a known lane, 1.5 times.
            float safety = lane.HasValue ? 1.5f : 2f;
            int want = (int)Math.Ceiling(kv.Value * safety - 0.15f);
            if (!lane.HasValue && lanes.Count > 1) want = Math.Max(want, lanes.Count);
            for (int i = h; i < want; i++)
            {
                float c = Place(b, units, kv.Key, f, lane, rng);
                if (c <= 0f) break;
                spent += c;
            }
        }
        if (spendAll)
            foreach (var id in new[] { "machine_gun", "rifle_squad" })
                while (b.Budget >= units.Get(id).BuildCost) { float c = Place(b, units, id, f, lane, rng); if (c <= 0f) break; spent += c; }
        return spent;
    }

    // Someone near this lane already? If not, the best infantry killer the money allows.
    static float Cover(Battle b, UnitTable units, float x, Field f, Random rng)
    {
        foreach (var d in b.Defenders) if (d.Def.Id != "recon_drone" && Math.Abs(d.X - x) < 45f) return 0f;
        foreach (var id in new[] { "machine_gun", "rifle_squad" })
            if (b.Budget >= units.Get(id).BuildCost) return Place(b, units, id, f, x, rng);
        return 0f;
    }

    static Dictionary<string, float> Need(UnitTable units, List<WavePlan.Group> groups)
    {
        var need = new Dictionary<string, float>();
        foreach (var g in groups)
        {
            if (g.UnitId == "recon_uav") continue;
            string best = null;
            float bestCost = float.MaxValue;
            foreach (var d in Defenders)
            {
                float s = Stop(d, g.UnitId);
                if (s < 0.5f) continue;
                float c = units.Get(d).BuildCost / s;
                if (c < bestCost) { bestCost = c; best = d; }
            }
            if (best == null) continue;
            need.TryGetValue(best, out float n);
            need[best] = n + g.Count / Math.Max(0.5f, Stop(best, g.UnitId));
        }
        return need;
    }

    // Somewhere in front of an entry point (the seen lane if known), at a random depth in the placing zone.
    // Houses and hill tops that suit this type go first (Routes.TerrainSpots, issue #49).
    static float Place(Battle b, UnitTable units, string id, Field f, float? lane, Random rng)
    {
        var def = units.Get(id);
        float cost = b.CostOf(id);
        if (b.Budget < cost) return 0f;
        var spots = Routes.TerrainSpots(f.Name, f, pvision, lanes, def, f.Height * 0.55f, f.Height * 0.9f);
        foreach (var s in spots)
        {
            if (s.Score <= Routes.LaneScore) break;   // not worth it for this type: use the lane points below
            if (s.House >= 0 && b.HouseOccupant(s.House) >= 0) continue;
            if (b.TryPlace(id, s.X, s.Y, out _, out _)) return cost;
        }
        for (int tries = 0; tries < 20; tries++)
        {
            float x, y;
            if (lane.HasValue)
            {
                x = Math.Max(12f, Math.Min(f.Width - 12f, lane.Value + (float)(rng.NextDouble() * 2 - 1) * 30f));
                y = f.Height * (0.55f + 0.35f * (float)rng.NextDouble());
            }
            else Routes.Near(lanes[rng.Next(lanes.Count)], rng, f.Width, f.Height * 0.55f, f.Height * 0.9f, out x, out y);
            if (b.TryPlace(id, x, y, out _, out _)) return cost;
        }
        return 0f;
    }
}
