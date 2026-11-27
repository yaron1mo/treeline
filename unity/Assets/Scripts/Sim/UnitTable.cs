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
        public float Hp, SpeedMps, SightRangeM, RangeM, BuildCost, DisplayScale = 1f;
        /// <summary>Price rise after each purchase (units.csv cost_increase): "25" adds 25 per unit bought, "25%"
        /// multiplies the price by 1.25 per unit bought. Empty = the price never changes (Yaroslav 2026-11-17, recon drone).</summary>
        public float CostIncrease;
        public bool CostIncreaseIsShare;

        /// <summary>The price of the next one after <paramref name="bought"/> have been bought, rounded to whole points.</summary>
        public float CostAfter(int bought)
        {
            if (bought <= 0 || CostIncrease <= 0f) return BuildCost;
            double c = CostIncreaseIsShare ? BuildCost * Math.Pow(1.0 + CostIncrease, bought) : BuildCost + CostIncrease * bought;
            return (float)Math.Round(c);
        }
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
        // Shots (Ballistics.cs).
        /// <summary>How fast its rounds fly; 0 = the default for its kind of fire.</summary>
        public float ProjectileSpeedMps;
        /// <summary>A guided missile (ATGM): flies to the target and follows it; accuracy is the chance the guidance works.</summary>
        public bool Guided;
        /// <summary>How big it is as a target: a round passing this close hits it. 0 = the default for its type.</summary>
        public float HitRadiusM;
        /// <summary>Cannot fire at anything closer than this (mortar).</summary>
        public float MinRangeM;
        /// <summary>Area fire: rounds land in a zone of this radius around the aim point (rings: battle_rules
        /// spread_ring_shares). 0 = spread from accuracy instead.</summary>
        public float SpreadM;
        /// <summary>Indirect fire: shells fly over everything and land with a spread around the aim point.</summary>
        public bool IndirectFire => Behaviour == "indirect_fire";
        /// <summary>Attackers: what kind of target this is for a drone operator (aviation, fast, heavy, infantry).</summary>
        public string TargetClass = "";
        /// <summary>Operators: target classes in order of priority; a class that is not listed is never chosen.</summary>
        public string TargetOrder = "";
        /// <summary>Drone operators: the unit row of the drone they launch (its hp, hit radius, targetable_by), so the
        /// enemy can shoot it down by the same rules we shoot theirs (Yaroslav, 2026-11-16).</summary>
        public string DroneUnit = "";
        /// <summary>Attackers: what it costs the cheapest fitting defence to stop one (rifle squad = 100), measured by
        /// tools/balance; kill rewards are a share of it (Yaroslav, 2026-11-16).</summary>
        public float ThreatValue;
        /// <summary>Distance at which accuracy is quoted (chance to hit a standing soldier); empty = half of range_m.</summary>
        public float AccuracyRefM;
        /// <summary>Attackers: what killing one is worth for the kill reward; empty = threat_value. An enemy FPV is a
        /// round, worth nothing by itself; its spotter (recon UAV) is what is worth shooting down (Yaroslav, 2026-11-16).</summary>
        public float RewardValue;

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
                    DroneUnit = Cell(col, r, "drone_unit"),
                    ThreatValue = Num(col, r, "threat_value"),
                    AccuracyRefM = Num(col, r, "accuracy_ref_m"),
                    RewardValue = NumOr(col, r, "reward_value", float.NaN),
                    DisplayScale = NumOr(col, r, "display_scale", 1f),
                    Hp = Num(col, r, "hp"),
                    SpeedMps = Num(col, r, "speed_mps"),
                    SightRangeM = Num(col, r, "sight_range_m"),
                    RangeM = Num(col, r, "range_m"),
                    BuildCost = Num(col, r, "build_cost"),
                    ProjectileSpeedMps = Num(col, r, "projectile_speed_mps"),
                    Guided = Cell(col, r, "guided").Equals("TRUE", StringComparison.OrdinalIgnoreCase),
                    HitRadiusM = Num(col, r, "hit_radius_m"),
                    MinRangeM = Num(col, r, "min_range_m"),
                    SpreadM = Num(col, r, "spread_m"),
                };
                if (table.byId.ContainsKey(id)) throw new FormatException("Unit '" + id + "' appears twice");
                string inc = Cell(col, r, "cost_increase");
                if (inc.EndsWith("%"))
                {
                    u.CostIncreaseIsShare = true;
                    u.CostIncrease = float.Parse(inc.TrimEnd('%').Trim(), CultureInfo.InvariantCulture) / 100f;
                }
                else if (inc.Length > 0) u.CostIncrease = float.Parse(inc, CultureInfo.InvariantCulture);
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
