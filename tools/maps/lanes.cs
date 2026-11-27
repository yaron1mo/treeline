// Lanes check for the real maps: how many lanes do the attackers use on each field?
// Same logic the balance tools use (tools/balance/Routes.cs), so the CI Balance run
// sees the same lanes. A map needs at least 2 different lanes (issue #25).
//
// Build and run (Mono, like the balance tools):
//   mcs -langversion:latest -out:lanes.exe unity/Assets/Scripts/Sim/*.cs \
//       tools/balance/Routes.cs tools/maps/lanes.cs
//   mono lanes.exe . [name filter]
// Output: one TSV line per field:  name <TAB> lane count <TAB> lane x positions <TAB> spawns
// tools/maps/preview.py reads this file to draw the lanes into the previews.
using System;
using System.IO;
using Treeline.Sim;

static class Lanes
{
    static int Main(string[] args)
    {
        string root = args.Length > 0 ? args[0] : ".";
        string filter = args.Length > 1 ? args[1] : "";
        string data = Path.Combine(root, "unity/Assets/Data");
        var obstacles = ObstacleTable.Parse(File.ReadAllText(Path.Combine(data, "obstacles.csv")));
        var rules = BattleRules.Parse(File.ReadAllText(Path.Combine(data, "battle_rules.csv")));
        string fieldsDir = Path.Combine(root, "unity/Assets/Resources/Fields");
        foreach (var file in Directory.GetFiles(fieldsDir, "*.json"))
        {
            var field = FieldParser.Parse(File.ReadAllText(file));
            if (field.Name == "Vision test" || (filter.Length > 0 && !field.Name.Contains(filter))) continue;
            var grid = new NavGrid(field, obstacles);
            grid.BlockEdges(rules.EdgeMarginM);
            var lanes = Routes.Of(field.Name, grid, field.Height * 0.6f, field.Height * 0.9f);
            var xs = new System.Collections.Generic.List<string>();
            foreach (var l in lanes) xs.Add(l.X.ToString("0"));
            var sp = new System.Collections.Generic.List<string>();
            foreach (var s in field.Spawns) sp.Add(s.ToString("0"));
            Console.WriteLine($"{field.Name}\t{lanes.Count}\t{string.Join(",", xs)}\t{string.Join(",", sp)}");
        }
        return 0;
    }
}
