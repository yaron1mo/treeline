// One defender against one armoured vehicle, with the defender hidden in different ways (Yaroslav, 2026-11-13).
// Compile: mcs -langversion:latest -out:armour-hidden.exe unity/Assets/Scripts/Sim/*.cs tools/balance/ArmourHidden.cs
// Run:     mono armour-hidden.exe <out.csv> <repo root>
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

static class ArmourHidden
{
    const float Dt = 0.05f;
    const int Seeds = 200;
    static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
    static readonly string[] Defenders = { "anti_tank", "fpv_operator", "bomber_operator", "mortar", "sniper" };
    static readonly string[] Vehicles = { "old_tank", "modern_tank", "ifv", "mrap" };
    static readonly string[] Positions = { "route", "open", "trench", "belt", "rock" };

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

        var csv = new StringBuilder("defender,vehicle,position,recon,seed,stopped,defender_alive,vehicle_damage_pct,line_damage,seconds\n");
        foreach (var def in Defenders)
        foreach (var veh in Vehicles)
        foreach (var pos in Positions)
        foreach (var recon in new[] { false, true })
        for (int seed = 1; seed <= Seeds; seed++)
        {
            var field = FieldFor(pos, out float x, out float y);
            var b = new Battle(new NavGrid(field, obstacles), units, 10000f, new VisionMap(field, obstacles), armour, 1000f, seed, rules);
            if (!b.TryPlace(def, x, y, out var d, out var why)) throw new Exception(def + " at " + pos + ": " + why);
            if (recon && !b.TryPlace("recon_drone", 65f, 100f, out _, out why)) throw new Exception("recon: " + why);
            if (def == "fpv_operator" && !b.TrySetDroneWaypoint(d.Id, 40f, 120f, out why)) throw new Exception("waypoint: " + why);
            b.BeginWave();
            b.QueueWave(veh, 1, 0f, 1f);
            b.StartRound();
            while (b.Outcome == BattleOutcome.Ongoing && b.RoundTime < 400f) b.Step(Dt);
            if (b.Outcome == BattleOutcome.Ongoing) throw new Exception("unfinished " + def + "/" + veh + "/" + pos + "/" + seed);
            b.Stats.TryGetValue(veh, out var vs);
            float dmg = vs != null ? 100f * vs.DamageTaken / units.Get(veh).Hp : 0f;
            csv.Append(def).Append(',').Append(veh).Append(',').Append(pos).Append(',').Append(recon ? 1 : 0).Append(',')
               .Append(seed).Append(',').Append(vs != null && vs.Killed > 0 ? 1 : 0).Append(',').Append(d.Alive ? 1 : 0).Append(',')
               .Append(dmg.ToString("0.#", Inv)).Append(',').Append(b.LineDamage.ToString("0.#", Inv)).Append(',')
               .Append(b.RoundTime.ToString("0.#", Inv)).Append('\n');
        }
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outPath)));
        File.WriteAllText(outPath, csv.ToString());
        Console.WriteLine("Runs: " + (Defenders.Length * Vehicles.Length * Positions.Length * 2 * Seeds));
    }
}
