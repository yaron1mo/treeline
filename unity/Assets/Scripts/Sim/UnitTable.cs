using System;
using System.Collections.Generic;
using System.Globalization;

namespace Treeline.Sim
{
    /// <summary>One row of the units table (unity/Assets/Data/units.csv). Only the columns the Sim uses so far.</summary>
    public sealed class UnitDef
    {
        public string Id = "", Name = "", Side = "", Type = "", Subtype = "", Behaviour = "", WeaponClass = "";
        public float Hp, SpeedMps, SightRangeM, RangeM, BuildCost;

        public bool IsDefence => Side == "defence";
    }

    public sealed class UnitTable
    {
        readonly Dictionary<string, UnitDef> byId = new Dictionary<string, UnitDef>(StringComparer.OrdinalIgnoreCase);
        public readonly List<UnitDef> All = new List<UnitDef>();

        public bool TryGet(string id, out UnitDef u) => byId.TryGetValue(id, out u);

        public UnitDef Get(string id)
        {
            if (!byId.TryGetValue(id, out var u)) throw new KeyNotFoundException("No unit '" + id + "' in the table");
            return u;
        }

        public static UnitTable Parse(string csv)
        {
            var rows = Csv.Parse(csv);
            if (rows.Count == 0) throw new FormatException("Unit table is empty");

            var col = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < rows[0].Length; i++) col[rows[0][i].Trim()] = i;
            if (!col.ContainsKey("id") || !col.ContainsKey("side"))
                throw new FormatException("Unit table needs the columns 'id' and 'side'");

            var table = new UnitTable();
            for (int n = 1; n < rows.Count; n++)
            {
                var r = rows[n];
                string id = Cell(col, r, "id");
                if (id.Length == 0) continue;
                var u = new UnitDef
                {
                    Id = id,
                    Name = Cell(col, r, "name"),
                    Side = Cell(col, r, "side"),
                    Type = Cell(col, r, "type"),
                    Subtype = Cell(col, r, "subtype"),
                    Behaviour = Cell(col, r, "behaviour"),
                    WeaponClass = Cell(col, r, "weapon_class"),
                    Hp = Num(col, r, "hp"),
                    SpeedMps = Num(col, r, "speed_mps"),
                    SightRangeM = Num(col, r, "sight_range_m"),
                    RangeM = Num(col, r, "range_m"),
                    BuildCost = Num(col, r, "build_cost"),
                };
                if (table.byId.ContainsKey(id)) throw new FormatException("Unit '" + id + "' appears twice");
                table.byId[id] = u;
                table.All.Add(u);
            }
            return table;
        }

        static string Cell(Dictionary<string, int> col, string[] r, string name) =>
            col.TryGetValue(name, out int i) && i < r.Length ? r[i].Trim() : "";

        static float Num(Dictionary<string, int> col, string[] r, string name)
        {
            string s = Cell(col, r, name);
            return s.Length == 0 ? 0f : float.Parse(s, CultureInfo.InvariantCulture);
        }
    }
}
