using System;
using System.Collections.Generic;
using System.Globalization;

namespace Treeline.Sim
{
    /// <summary>One row of the units table (unity/Assets/Data/units.csv). Only the columns the Sim uses so far.</summary>
    public sealed class UnitDef
    {
        public string Id = "", Name = "", Side = "", Type = "", Subtype = "", Behaviour = "", WeaponClass = "";
        public string DamageType = "", Armour = "", TargetableBy = "";
        public float Hp, SpeedMps, SightRangeM, RangeM, BuildCost;
        // AttackSpeed is shots per second; Damage is raw damage per successful shot.
        public float Damage, AttackSpeed, Accuracy, MoveAccuracyMult = 1f, LineDamage = 1f;
        public bool FireOnMove;
        public string CarriesUnit = "";
        public int CarriesCount;
        public float DismountAtHp, DismountIntervalS, BlastRadiusM, WreckRadiusM, TreeMoveCost;
        public int AdaptationLagWaves;
        // Drones (operators launch them; see Battle.LaunchDrones).
        public float DroneSpeedMps, DropIntervalS, DroneSightRangeM;
        public int DroneAmmo;
        /// <summary>Drone operators: their damage and attack_speed belong to the drones they launch, not to a gun of
        /// their own, so they never fire directly and are not a direct-fire threat on attackers' routes.</summary>
        public bool LaunchesDrones => DroneAmmo > 0;
        /// <summary>Attackers: what kind of target this is for a drone operator (aviation, fast, heavy, infantry).</summary>
        public string TargetClass = "";
        /// <summary>Operators: target classes in order of priority; a class that is not listed is never chosen.</summary>
        public string TargetOrder = "";

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
                    DamageType = Cell(col, r, "damage_type"),
                    Armour = Cell(col, r, "armour"),
                    TargetableBy = Cell(col, r, "targetable_by"),
                    FireOnMove = Cell(col, r, "fire_on_move").Equals("TRUE", StringComparison.OrdinalIgnoreCase),
                    Damage = Num(col, r, "damage"),
                    AttackSpeed = Num(col, r, "attack_speed"),
                    Accuracy = Num(col, r, "accuracy"),
                    MoveAccuracyMult = NumOr(col, r, "move_accuracy_mult", 1f),
                    LineDamage = NumOr(col, r, "line_damage", 1f),
                    CarriesUnit = Cell(col, r, "carries_unit"),
                    CarriesCount = (int)Num(col, r, "carries_count"),
                    DismountAtHp = Num(col, r, "dismount_at_hp"),
                    DismountIntervalS = Num(col, r, "dismount_interval_s"),
                    BlastRadiusM = Num(col, r, "blast_radius_m"),
                    WreckRadiusM = Num(col, r, "wreck_radius_m"),
                    TreeMoveCost = Num(col, r, "tree_move_cost"),
                    AdaptationLagWaves = (int)Num(col, r, "adaptation_lag_waves"),
                    DroneSpeedMps = Num(col, r, "drone_speed_mps"),
                    DroneSightRangeM = Num(col, r, "drone_sight_range_m"),
                    DropIntervalS = Num(col, r, "drop_interval_s"),
                    DroneAmmo = (int)Num(col, r, "drone_ammo"),
                    TargetClass = Cell(col, r, "target_class"),
                    TargetOrder = Cell(col, r, "target_order"),
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

        static float Num(Dictionary<string, int> col, string[] r, string name) => NumOr(col, r, name, 0f);

        static float NumOr(Dictionary<string, int> col, string[] r, string name, float fallback)
        {
            string s = Cell(col, r, name);
            return s.Length == 0 ? fallback : float.Parse(s, CultureInfo.InvariantCulture);
        }
    }
}
