// Every defender against every attacker, 1 v 1, in the open and hidden (Yaroslav, 2026-11-15): the full matrix.
// Compile: mcs -langversion:latest -out:matchups.exe unity/Assets/Scripts/Sim/*.cs tools/balance/Matchups.cs
// Run:     mono matchups.exe <out.csv> <repo root>
// Same field as ArmourHidden.cs; positions: open (25 m beside the route) and trench_belt (a trench inside a forest belt,
// same spot). 1 to 5 attackers of one type, GapS apart (default 0.3 s: a group arriving together; Yaroslav, 2026-11-15). The mortar, the anti-tank team and the
// drone operators always get a recon drone over the approach (Yaroslav: recon is their base condition); the others
// fight alone. Columns: killed = how many of the attackers were destroyed (a carrier or helicopter: the vehicle itself);
// line_damage = what reached the rear edge (troops let out count).
//
// Field 120 x 300 m, one entry point at x = 40, so the vehicle drives straight down x ~ 40 to the rear edge.
// The defender stands at y = 200, either on that route or 25 m to the side, in one of these positions:
//   route   - on the route, in the open (the setup of ArmourCalibration.cs)
//   open    - 25 m aside, in the open
//   trench  - 25 m aside, in a trench (cover inside, does not hide)
//   belt    - 25 m aside, inside a forest belt running along the route (hides: seen at most 50%, less with depth)
//   rock    - 25 m aside, right behind a rock on the enemy side (hidden from the front until the vehicle is level)
// Each with and without a recon drone that sees the whole approach (shared vision).
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Treeline.Sim;

static class Matchups
{
    const float Dt = 0.05f;
    const int Seeds = 50;
    static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
    static readonly string[] Defenders = { "rifle_squad", "machine_gun", "sniper", "mortar", "anti_tank", "air_defence", "fpv_operator", "bomber_operator" };
    static readonly string[] Vehicles = { "recruit", "regular", "special_forces", "old_tank", "modern_tank", "ifv", "mrap",
        "buggy", "motorbike", "van", "fpv", "helicopter" };
    static readonly string[] Positions = { "open", "trench_belt" };
    static readonly int[] Counts = { 1, 2, 3, 4, 5 };
    static readonly float GapS = string.IsNullOrEmpty(Environment.GetEnvironmentVariable("MATCHUPS_GAP")) ? 0.3f : float.Parse(Environment.GetEnvironmentVariable("MATCHUPS_GAP"), CultureInfo.InvariantCulture);
    static bool NeedsRecon(string d) => d == "mortar" || d == "anti_tank" || d == "air_defence" || d == "fpv_operator" || d == "bomber_operator";

    static Pt[] Box(float x0, float y0, float x1, float y1) =>
        new[] { new Pt(x0, y0), new Pt(x1, y0), new Pt(x1, y1), new Pt(x0, y1) };

    static Field FieldFor(string position, out float dx, out float dy)
    {
        var f = new Field { Name = "armour-hidden-" + position, Width = 120f, Height = 300f };
        f.Spawns.Add(40f);
        dx = 65f; dy = 200f;
        switch (position)
        {
            case "route": dx = 40f; break;
            case "trench": f.Features.Add(new Feature { Type = "trench", Poly = Box(62f, 198f, 68f, 202f) }); break;
            case "belt": f.Features.Add(new Feature { Type = "belt", Poly = Box(59f, 150f, 71f, 250f) }); break;
            case "trench_belt":   // a trench dug inside a forest belt along the route (Yaroslav, 2026-11-15)
                f.Features.Add(new Feature { Type = "belt", Poly = Box(59f, 150f, 71f, 250f) });
                f.Features.Add(new Feature { Type = "trench", Poly = Box(62f, 198f, 68f, 202f) });
                break;
            case "rock": f.Features.Add(new Feature { Type = "rock", Poly = Box(62f, 194f, 68f, 197f) }); dy = 199f; break;
        }
        return f;
    }

    static void Main(string[] args)
    {
        string outPath = args.Length > 0 ? args[0] : "armour-hidden.csv";
        string root = args.Length > 1 ? args[1] : ".";
        string data = Path.Combine(root, "unity/Assets/Data");
        var units = UnitTable.Parse(File.ReadAllText(Path.Combine(data, "units.csv")));
        var obstacles = ObstacleTable.Parse(File.ReadAllText(Path.Combine(data, "obstacles.csv")));
        var armour = ArmourTable.Parse(File.ReadAllText(Path.Combine(data, "armour.csv")));
        var rules = BattleRules.Parse(File.ReadAllText(Path.Combine(data, "battle_rules.csv")));

        var csv = new StringBuilder("defender,vehicle,count,position,recon,seed,killed,defender_alive,line_damage,seconds\n");
        var only = Environment.GetEnvironmentVariable("MATCHUPS_DEFENDERS");   // e.g. "air_defence" to rerun one
        foreach (var def in Defenders)
        if (string.IsNullOrEmpty(only) || Array.IndexOf(only.Split(','), def) >= 0)
        foreach (var veh in Vehicles)
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("MATCHUPS_ATTACKERS")) ||
            Array.IndexOf(Environment.GetEnvironmentVariable("MATCHUPS_ATTACKERS").Split(','), veh) >= 0)
        foreach (var count in Counts)
        foreach (var pos in Positions)
        foreach (var recon in new[] { NeedsRecon(def) })
        for (int seed = 1; seed <= Seeds; seed++)
        {
            var field = FieldFor(pos, out float x, out float y);
            var b = new Battle(new NavGrid(field, obstacles), units, 10000f, new VisionMap(field, obstacles), armour, 1000f, seed, rules);
            if (!b.TryPlace(def, x, y, out var d, out var why)) throw new Exception(def + " at " + pos + ": " + why);
            if (recon && !b.TryPlace("recon_drone", 65f, 100f, out _, out why)) throw new Exception("recon: " + why);
            b.BeginWave();
            b.QueueWave(veh, count, 0f, GapS);
            b.StartRound();
            while (b.Outcome == BattleOutcome.Ongoing && b.RoundTime < 400f) b.Step(Dt);
            b.Stats.TryGetValue(veh, out var vs);
            csv.Append(def).Append(',').Append(veh).Append(',').Append(count).Append(',').Append(pos).Append(',').Append(recon ? 1 : 0).Append(',')
               .Append(seed).Append(',').Append(vs != null ? vs.Killed : 0).Append(',').Append(d.Alive ? 1 : 0).Append(',')
               .Append(b.LineDamage.ToString("0.#", Inv)).Append(',').Append(b.RoundTime.ToString("0.#", Inv)).Append('\n');
        }
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outPath)));
        File.WriteAllText(outPath, csv.ToString());
        Console.WriteLine("Done");
    }
}
