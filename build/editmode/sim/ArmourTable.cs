using System;
using System.Collections.Generic;
using System.Globalization;

namespace Treeline.Sim
{
    /// <summary>Damage multiplier by damage type and armour level (unity/Assets/Data/armour.csv).</summary>
    public sealed class ArmourTable
    {
        readonly Dictionary<string, Dictionary<string, float>> factors =
            new Dictionary<string, Dictionary<string, float>>(StringComparer.OrdinalIgnoreCase);

        public static ArmourTable Parse(string csv)
        {
            var rows = Csv.Parse(csv);
            if (rows.Count == 0) throw new FormatException("Armour table is empty");
            var header = rows[0];
            var table = new ArmourTable();
            for (int n = 1; n < rows.Count; n++)
            {
                var r = rows[n];
                if (r.Length == 0 || r[0].Trim().Length == 0) continue;
                var byLevel = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
                for (int c = 2; c < header.Length && c < r.Length; c++)
                {
                    string cell = r[c].Trim();
                    if (cell.Length > 0) byLevel[header[c].Trim()] = float.Parse(cell, CultureInfo.InvariantCulture);
                }
                table.factors[r[0].Trim()] = byLevel;
            }
            return table;
        }

        /// <summary>1 = full damage. Unknown damage types and levels do not reduce damage; empty armour means none.</summary>
        public float Factor(string damageType, string armour)
        {
            if (string.IsNullOrEmpty(armour)) armour = "none";
            if (!factors.TryGetValue(damageType ?? "", out var byLevel)) return 1f;
            return byLevel.TryGetValue(armour, out float f) ? f : 1f;
        }
    }
}
