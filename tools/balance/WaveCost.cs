// Wave cost (Yaroslav, 2026-11-17): the smallest budget with which a scripted player holds one wave of waves.csv.
// Each wave is fought alone, on an empty field, with no memory of earlier waves.
// The player splits the budget by need: for each attacker group it takes the cheapest defender per attacker stopped
// (stop table), then buys those defenders in proportion until the money ends. Each entry point gets one machine gunner
// first, and a recon drone when a long gun needs one. Defenders go to the entry points in turn.
// A wave is held when the line takes no more than line_health / number of waves damage.
// The cost is the smallest budget (to 25 points) that holds the wave on at least 8 of 10 seeds.
// The result is an upper bound: the scripted player plays worse than a person.
// Compile: mcs -langversion:latest -out:wave-cost.exe unity/Assets/Scripts/Sim/*.cs tools/balance/WaveCost.cs
// Run:     mono wave-cost.exe <repo root> <stop table csv> [field name filter] [first wave] [last wave] [line damage allowed]
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Treeline.Sim;

static class WaveCost
{
    const int Seeds = 10, MustHold = 8;
    const float Step = 25f, MaxBudget = 12000f;
    static readonly string[] Defenders = { "rifle_squad", "machine_gun", "sniper", "mortar", "anti_tank", "air_defence", "fpv_operator", "bomber_operator" };
    static readonly HashSet<string> NeedRecon = new HashSet<string> { "mortar", "anti_tank", "air_defence", "fpv_operator", "bomber_operator" };
    static readonly Dictionary<string, Dictionary<string, float>> stop = new Dictionary<string, Dictionary<string, float>>();

    static UnitTable units;
    static ObstacleTable obstacles;
    static ArmourTable armour;
    static BattleRules rules;
    static WavePlan plan;

    static void Main(string[] args)
    {
        string root = args[0];
        foreach (var line in File.ReadAllLines(args[1]))
        {
            var c = line.Split(',');
            if (c[0] == "defender") continue;
            if (!stop.ContainsKey(c[1])) stop[c[1]] = new Dictionary<string, float>();
            stop[c[1]][c[0]] = float.Parse(c[2], CultureInfo.InvariantCulture);
        }
        string filter = args.Length > 2 ? args[2] : "";
        string data = Path.Combine(root, "unity/Assets/Data");
        units = UnitTable.Parse(File.ReadAllText(Path.Combine(data, "units.csv")));
        obstacles = ObstacleTable.Parse(File.ReadAllText(Path.Combine(data, "obstacles.csv")));
        armour = ArmourTable.Parse(File.ReadAllText(Path.Combine(data, "armour.csv")));
        rules = BattleRules.Parse(File.ReadAllText(Path.Combine(data, "battle_rules.csv")));
        plan = WavePlan.Parse(File.ReadAllText(Path.Combine(data, "waves.csv")));
        int first = args.Length > 3 ? int.Parse(args[3]) : 1;
        int last = args.Length > 4 ? int.Parse(args[4]) : plan.WaveCount;
        float allowance = args.Length > 5 ? float.Parse(args[5], CultureInfo.InvariantCulture) : rules.LineHealth / plan.WaveCount;

        Console.WriteLine("field,wave,threat,cost,held_at_cost");
        foreach (var file in Directory.GetFiles(Path.Combine(root, "unity/Assets/Resources/Fields"), "*.json"))
        {
            var field = FieldParser.Parse(File.ReadAllText(file));
            if (field.Name == "Vision test" || (filter.Length > 0 && !field.Name.Contains(filter))) continue;
            for (int w = first; w <= last; w++)
            {
                float threat = 0f;
                foreach (var g in plan.WaveGroups(w)) threat += g.Count * Num(units.Get(g.UnitId).ThreatValue);
                // Smallest budget that holds: binary search on steps of 25.
                int lo = 0, hi = (int)(MaxBudget / Step);
                if (Held(field, w, hi * Step, allowance) < MustHold) { Out(field, w, threat, float.NaN, 0); continue; }
                while (hi - lo > 1)
                {
                    int mid = (lo + hi) / 2;
                    if (Held(field, w, mid * Step, allowance) >= MustHold) hi = mid; else lo = mid;
                }
                Out(field, w, threat, hi * Step, Held(field, w, hi * Step, allowance));
            }
        }
    }

    static float Num(float v) => float.IsNaN(v) ? 0f : v;

    static void Out(Field f, int w, float threat, float cost, int held) =>
        Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "{0},{1},{2:0},{3:0},{4}", f.Name, w, threat, cost, held));

    // How many of the seeds hold the wave with this budget (stops early when the answer is known).
    static int Held(Field field, int wave, float budget, float allowance)
    {
        int held = 0, lost = 0;
        for (int seed = 1; seed <= Seeds; seed++)
        {
            if (Fight(field, wave, budget, seed, allowance)) held++; else lost++;
            if (held >= MustHold || lost > Seeds - MustHold) break;
        }
        return held;
    }

    static bool Fight(Field field, int wave, float budget, int seed, float allowance)
    {
        var b = new Battle(new NavGrid(field, obstacles), units, budget, new VisionMap(field, obstacles), armour, -1f, seed, rules);
        b.PlacementMinY = rules.PlacementZoneStart * field.Height;
        Buy(b, field, wave, new Random(seed));
        plan.Queue(b, wave);
        b.StartRound();
        while (b.Outcome == BattleOutcome.Ongoing && b.RoundTime < 3000f) b.Step(0.05f);
        return b.Outcome == BattleOutcome.Won && b.LineDamage <= allowance + 0.001f;
    }

    static Battle cb;
    static Field cf;
    static Random crng;
    static List<float> lanes;
    static int next;
    static Dictionary<string, int> have;

    static bool Put(string id)
    {
        var b = cb; var f = cf; var rng = crng;
        if (b.Budget < b.CostOf(id)) return false;
        for (int tries = 0; tries < 20; tries++)
        {
            float x = lanes[next % lanes.Count] + (float)(rng.NextDouble() * 2 - 1) * 20f;
            x = Math.Max(12f, Math.Min(f.Width - 12f, x));
            float y = f.Height * (0.6f + 0.3f * (float)rng.NextDouble());
            if (b.TryPlace(id, x, y, out _, out _))
            {
                next++;
                have.TryGetValue(id, out int h);
                have[id] = h + 1;
                return true;
            }
        }
        return false;
    }

    static float Stop(string defender, string attacker) =>
        stop.TryGetValue(attacker, out var m) && m.TryGetValue(defender, out var v) ? v : 0f;

    static void Buy(Battle b, Field f, int wave, Random rng)
    {
        // Need: for each group, the cheapest defender per attacker stopped; how many of it the group needs.
        var need = new Dictionary<string, float>();
        foreach (var g in plan.WaveGroups(wave))
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
            need[best] = n + g.Count / Stop(best, g.UnitId);
        }
        cb = b; cf = f; crng = rng; next = 0; have = new Dictionary<string, int>();
        lanes = f.Spawns.Count > 0 ? new List<float>(f.Spawns) : new List<float> { f.Width / 2f };
        // Every entry point first gets someone who sees it.
        for (int i = 0; i < lanes.Count; i++) if (!Put("machine_gun")) Put("rifle_squad");
        bool recon = false;
        foreach (var k in need.Keys) if (NeedRecon.Contains(k)) recon = true;
        if (recon) { next = 0; for (int i = 0; i < lanes.Count; i++) Put("recon_drone"); }
        // Then the needed defenders in proportion: always the one furthest below its share.
        while (need.Count > 0)
        {
            string pick = null;
            float lowest = float.MaxValue;
            foreach (var kv in need)
            {
                if (b.Budget < b.CostOf(kv.Key)) continue;
                have.TryGetValue(kv.Key, out int h);
                float r = h / kv.Value;
                if (r < lowest) { lowest = r; pick = kv.Key; }
            }
            if (pick == null || !Put(pick)) break;
        }
        // Change left over: more infantry killers.
        while (Put("machine_gun") || Put("rifle_squad")) { }
    }
}
