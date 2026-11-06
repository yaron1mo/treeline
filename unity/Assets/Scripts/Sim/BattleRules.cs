using System;
using System.Collections.Generic;
using System.Globalization;

namespace Treeline.Sim
{
    /// <summary>Battle-wide numbers (unity/Assets/Data/battle_rules.csv). Defaults match the shipped file.</summary>
    public sealed class BattleRules
    {
        public float LineHealth = 20f;
        public float RunSpeedMultiplier = 3f;
        public float EvadeLengthWeight = 0.1f;
        public float DangerWeight = 1000f;
        public float MemoryDecayPerWave = 0.3f;
        public float MemoryForgetBelow = 0.25f;
        public float RouteChoiceSlack = 0.1f;
        public bool DismountBlastKills = true;
        /// <summary>Direct fire hits only what the shooter itself sees; shared vision then serves indirect fire (mortars) only.</summary>
        public bool DirectFireNeedsOwnSight;

        public static BattleRules Parse(string csv)
        {
            var r = new BattleRules();
            var rows = Csv.Parse(csv);
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (int n = 1; n < rows.Count; n++)
                if (rows[n].Length >= 2 && rows[n][0].Trim().Length > 0) values[rows[n][0].Trim()] = rows[n][1].Trim();

            r.LineHealth = Num(values, "line_health", r.LineHealth);
            r.RunSpeedMultiplier = Num(values, "run_speed_multiplier", r.RunSpeedMultiplier);
            r.EvadeLengthWeight = Num(values, "evade_length_weight_s_per_m", r.EvadeLengthWeight);
            r.DangerWeight = Num(values, "danger_weight_m_per_hp", r.DangerWeight);
            r.MemoryDecayPerWave = Num(values, "memory_decay_per_wave", r.MemoryDecayPerWave);
            r.MemoryForgetBelow = Num(values, "memory_forget_below", r.MemoryForgetBelow);
            r.RouteChoiceSlack = Num(values, "route_choice_slack", r.RouteChoiceSlack);
            r.DismountBlastKills = Flag(values, "dismount_blast_kills", r.DismountBlastKills);
            r.DirectFireNeedsOwnSight = Flag(values, "direct_fire_needs_own_sight", r.DirectFireNeedsOwnSight);
            return r;
        }

        static bool Flag(Dictionary<string, string> v, string key, bool fallback) =>
            v.TryGetValue(key, out var s) && s.Length > 0 ? s.Equals("TRUE", StringComparison.OrdinalIgnoreCase) : fallback;

        static float Num(Dictionary<string, string> v, string key, float fallback) =>
            v.TryGetValue(key, out var s) && s.Length > 0 ? float.Parse(s, CultureInfo.InvariantCulture) : fallback;
    }
}
