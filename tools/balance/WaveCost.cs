// Wave cost (Yaroslav, 2026-11-17): the smallest budget with which a scripted player holds one wave of waves.csv.
// Each wave is fought alone, on an empty field, with no memory of earlier waves.
// The player splits the budget by need: for each attacker group it takes the cheapest defender per attacker stopped
// (stop table), then buys those defenders in proportion until the money ends. Each entry point gets one machine gunner
// first, and a recon drone when a long gun needs one. Defenders go to the lanes in turn (tools/balance/Routes.cs:
// where the traced routes cross the placing zone, so fields without a spawns list work too).
// Houses near a route and hill tops that see it are scored as candidate spots first, per defender type
// (Routes.TerrainSpots, 2026-11-28, issue #49).
// A wave is held when the line takes no more than line_health / number of waves damage.
// The cost is the smallest budget (to 25 points) that holds the wave on at least 8 of 10 seeds.
// The result is an upper bound: the scripted player plays worse than a person.
// Compile: mcs -langversion:latest -out:wave-cost.exe unity/Assets/Scripts/Sim/*.cs tools/balance/Routes.cs tools/balance/WaveCost.cs
// Squads (2026-11-25): SQUADS_FILE = csv "squad,defenders" (defenders split by "|"). For each squad the player may buy
// only the defender types of that squad (recon_drone too); the output gets a "squad" column. Without SQUADS_FILE all types
// are allowed and the output is as before.
// Run:     mono wave-cost.exe <repo root> <stop table csv> [field name filter] [first wave] [last wave] [line damage allowed]
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Treeline.Sim;

static class WaveCost
{
    const int Seeds = 10, MustHold = 8;
    const float Step = 25f;
    static float MaxBudget = 12000f;
    static readonly string[] AllDefenders = { "rifle_squad", "machine_gun", "sniper", "mortar", "anti_tank", "air_defence", "fpv_operator", "bomber_operator" };
    static readonly HashSet<string> NeedRecon = new HashSet<string> { "mortar", "anti_tank", "air_defence", "fpv_operator", "bomber_operator" };
    static readonly Dictionary<string, Dictionary<string, float>> stop = new Dictionary<string, Dictionary<string, float>>();

    static string[] Defenders;
    static HashSet<string> Allowed;   // null = every type
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
        // WAVES_FILE: another wave list (for example probe waves from make_probe_waves.py). MAX_BUDGET: search limit.
        string wavesFile = Environment.GetEnvironmentVariable("WAVES_FILE");
        plan = WavePlan.Parse(File.ReadAllText(string.IsNullOrEmpty(wavesFile) ? Path.Combine(data, "waves.csv") : Path.Combine(root, wavesFile)));
        string mb = Environment.GetEnvironmentVariable("MAX_BUDGET");
        if (!string.IsNullOrEmpty(mb)) MaxBudget = float.Parse(mb, CultureInfo.InvariantCulture);
        int first = args.Length > 3 ? int.Parse(args[3]) : 1;
        int last = args.Length > 4 ? int.Parse(args[4]) : plan.WaveCount;
        // Line damage allowed per wave: line_health / 20 (a 20-wave run), or the 6th argument.
        float allowance = args.Length > 5 ? float.Parse(args[5], CultureInfo.InvariantCulture) : rules.LineHealth / 20f;

        var squads = new List<(string name, HashSet<string> types)>();
        string squadsFile = Environment.GetEnvironmentVariable("SQUADS_FILE");
        if (!string.IsNullOrEmpty(squadsFile))
            foreach (var line in File.ReadAllLines(Path.Combine(root, squadsFile)))
            {
                var c = line.Split(',');
                if (c.Length < 2 || c[0] == "squad" || c[0].Trim().Length == 0) continue;
                squads.Add((c[0].Trim(), new HashSet<string>(c[1].Trim().Split('|'))));
            }
        bool squadMode = squads.Count > 0;
        if (!squadMode) squads.Add(("", null));

        Console.WriteLine(squadMode ? "field,wave,squad,threat,cost,held_at_cost" : "field,wave,threat,cost,held_at_cost");
        foreach (var file in Directory.GetFiles(Path.Combine(root, "unity/Assets/Resources/Fields"), "*.json"))
        {
            var field = FieldParser.Parse(File.ReadAllText(file));
            if (field.Name == "Vision test" || (filter.Length > 0 && !field.Name.Contains(filter))) continue;
            foreach (var sq in squads)
            for (int w = first; w <= last; w++)
            {
                string squadName = sq.name;
                Allowed = sq.types;
                Defenders = Array.FindAll(AllDefenders, d => Ok(d));
                squad = squadMode ? squadName : null;
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
    static bool Ok(string id) => Allowed == null || Allowed.Contains(id);
    static string squad;

    static void Out(Field f, int w, float threat, float cost, int held) =>
        Console.WriteLine(squad == null
            ? string.Format(CultureInfo.InvariantCulture, "{0},{1},{2:0},{3:0},{4}", f.Name, w, threat, cost, held)
            : string.Format(CultureInfo.InvariantCulture, "{0},{1},{2},{3:0},{4:0},{5}", f.Name, w, squad, threat, cost, held));

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
        var grid = new NavGrid(field, obstacles);
        var vision = new VisionMap(field, obstacles);
        var b = new Battle(grid, units, budget, vision, armour, -1f, seed, rules);
        b.PlacementMinY = rules.PlacementZoneStart * field.Height;
        Buy(b, field, grid, vision, wave, new Random(seed));
        plan.Queue(b, wave);
        b.StartRound();
        bool debug = Environment.GetEnvironmentVariable("WAVECOST_DEBUG") != null;
        var seen = new HashSet<int>();
        float nextLog = 0f;
        while (b.Outcome == BattleOutcome.Ongoing && b.RoundTime < 3000f)
        {
            b.Step(0.05f);
            if (debug && b.RoundTime >= nextLog)
            {
                nextLog = b.RoundTime + 10f;
                var parts = new List<string>();
                foreach (var a in b.Attackers) parts.Add($"{a.Def.Id}@{a.X:0},{a.Y:0} hp{a.Hp:0}");
                Console.Error.WriteLine($"  t{b.RoundTime:0} line {b.LineDamage:0.#} | {string.Join(" ", parts)}");
            }
        }
        if (debug)
        {
            var ds = new List<string>();
            foreach (var d in b.Defenders) ds.Add($"{d.Def.Id}@{d.X:0},{d.Y:0}");
            Console.Error.WriteLine($"budget {budget} seed {seed}: {b.Outcome} line {b.LineDamage:0.#} money left {b.Budget:0} | {string.Join(" ", ds)}");
        }
        return b.Outcome == BattleOutcome.Won && b.LineDamage <= allowance + 0.001f;
    }

    static Battle cb;
    static Field cf;
    static VisionMap cvision;
    static Random crng;
    static List<Routes.Lane> lanes;
    static int next;
    static Dictionary<string, int> have;

    static bool Put(string id)
    {
        var b = cb; var f = cf; var rng = crng;
        if (!Ok(id)) return false;
        if (b.Budget < b.CostOf(id)) return false;
        // Houses and hill tops first when they suit this type (Routes.TerrainSpots, issue #49), then the lanes.
        var spots = Routes.TerrainSpots(f.Name, f, cvision, lanes, units.Get(id), f.Height * 0.6f, f.Height * 0.9f);
        foreach (var s in spots)
        {
            if (s.Score <= Routes.LaneScore) break;   // not worth it for this type: use the lane points below
            if (s.House >= 0 && b.HouseOccupant(s.House) >= 0) continue;
            if (b.TryPlace(id, s.X, s.Y, out _, out _))
            {
                next++;
                have.TryGetValue(id, out int h);
                have[id] = h + 1;
                return true;
            }
        }
        for (int tries = 0; tries < 20; tries++)
        {
            Routes.Near(lanes[next % lanes.Count], rng, f.Width, f.Height * 0.6f, f.Height * 0.9f, out float x, out float y);
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

    static void Buy(Battle b, Field f, NavGrid grid, VisionMap vision, int wave, Random rng)
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
        cb = b; cf = f; cvision = vision; crng = rng; next = 0; have = new Dictionary<string, int>();
        lanes = Routes.Of(f.Name, grid, f.Height * 0.6f, f.Height * 0.9f);
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
