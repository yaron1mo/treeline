using System;
using System.Collections.Generic;
using System.Globalization;

namespace Treeline.Sim
{
    /// <summary>How an obstacle affects what can be seen through it.</summary>
    public enum SightEffect { None, Soft, Full }

    /// <summary>Who is shooting, for the cover multipliers.</summary>
    public enum ShooterClass { Troops = 0, Vehicle = 1, Aviation = 2, Indirect = 3 }

    /// <summary>One row of the obstacles table (unity/Assets/Data/obstacles.csv).</summary>
    public sealed class Obstacle
    {
        public string Id = "", Name = "", Kind = "", CoverApplies = "", Notes = "";
        public SightEffect Sight;
        public float DetectChance = 1f, FalloffPerM;
        public readonly float[] CoverAcc = { 1f, 1f, 1f, 1f };
        public readonly float[] CoverDmg = { 1f, 1f, 1f, 1f };
        public float OnRangeMult = 1f, OnSightMult = 1f, OnCoverMult = 1f;
        /// <summary>Move cost per class; null when the table leaves it empty, PositiveInfinity for "blocked".</summary>
        public float? MoveCostInfantry, MoveCostVehicle;
        /// <summary>True if the player may place it; BuildCost is in budget points.</summary>
        public bool Buildable;
        public float BuildCost;

        public bool HasOnBonus => OnRangeMult != 1f || OnSightMult != 1f || OnCoverMult != 1f;
    }

    public sealed class ObstacleTable
    {
        readonly Dictionary<string, Obstacle> byId = new Dictionary<string, Obstacle>(StringComparer.OrdinalIgnoreCase);
        public readonly List<Obstacle> All = new List<Obstacle>();

        public bool TryGet(string id, out Obstacle o) => byId.TryGetValue(id, out o);

        public Obstacle Get(string id)
        {
            if (!byId.TryGetValue(id, out var o)) throw new KeyNotFoundException("No obstacle '" + id + "' in the table");
            return o;
        }

        sealed class Reader
        {
            readonly Dictionary<string, int> col;
            public Reader(Dictionary<string, int> col) { this.col = col; }

            public string Cell(string[] r, string name) => col.TryGetValue(name, out int i) && i < r.Length ? r[i].Trim() : "";

            public float Num(string[] r, string name, float fallback)
            {
                string s = Cell(r, name);
                if (s.Length == 0) return fallback;
                if (s.Equals("blocked", StringComparison.OrdinalIgnoreCase)) return float.PositiveInfinity;
                return float.Parse(s, CultureInfo.InvariantCulture);
            }

            public float? OptNum(string[] r, string name)
            {
                string s = Cell(r, name);
                if (s.Length == 0) return null;
                return Num(r, name, 0f);
            }
        }

        public static ObstacleTable Parse(string csv)
        {
            var rows = Csv.Parse(csv);
            if (rows.Count == 0) throw new FormatException("Obstacle table is empty");

            var col = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < rows[0].Length; i++) col[rows[0][i].Trim()] = i;
            if (!col.ContainsKey("id") || !col.ContainsKey("sight"))
                throw new FormatException("Obstacle table needs the columns 'id' and 'sight'");

            var rd = new Reader(col);

            var table = new ObstacleTable();
            string[] classes = { "troops", "vehicle", "aviation", "indirect" };
            for (int n = 1; n < rows.Count; n++)
            {
                var r = rows[n];
                string id = rd.Cell(r, "id");
                if (id.Length == 0) continue;

                var o = new Obstacle
                {
                    Id = id,
                    Name = rd.Cell(r, "name"),
                    Kind = rd.Cell(r, "kind"),
                    CoverApplies = rd.Cell(r, "cover_applies"),
                    Notes = rd.Cell(r, "notes"),
                    DetectChance = rd.Num(r, "detect_chance", 1f),
                    FalloffPerM = rd.Num(r, "detect_falloff_per_m", 0f),
                    OnRangeMult = rd.Num(r, "on_range_mult", 1f),
                    OnSightMult = rd.Num(r, "on_sight_mult", 1f),
                    OnCoverMult = rd.Num(r, "on_cover_mult", 1f),
                    MoveCostInfantry = rd.OptNum(r, "move_cost_infantry"),
                    MoveCostVehicle = rd.OptNum(r, "move_cost_vehicle"),
                    Buildable = rd.Cell(r, "buildable").Equals("TRUE", StringComparison.OrdinalIgnoreCase),
                    BuildCost = rd.Num(r, "build_cost", 0f),
                };

                string sight = rd.Cell(r, "sight").ToLowerInvariant();
                if (sight == "full") o.Sight = SightEffect.Full;
                else if (sight == "soft") o.Sight = SightEffect.Soft;
                else if (sight == "none" || sight.Length == 0) o.Sight = SightEffect.None;
                else throw new FormatException("Obstacle '" + id + "': sight must be full, soft or none, not '" + sight + "'");

                for (int k = 0; k < 4; k++)
                {
                    o.CoverAcc[k] = rd.Num(r, "cover_acc_" + classes[k], 1f);
                    o.CoverDmg[k] = rd.Num(r, "cover_dmg_" + classes[k], 1f);
                }

                if (table.byId.ContainsKey(id)) throw new FormatException("Obstacle '" + id + "' appears twice");
                table.byId[id] = o;
                table.All.Add(o);
            }
            return table;
        }
    }
}
