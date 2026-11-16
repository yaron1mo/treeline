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
        /// <summary>Drones in the air are lost when their operator dies.</summary>
        public bool DroneLostWithOperator = true;
        /// <summary>How wrongly a shooter judges a moving target's speed when leading it (standard deviation, share of the speed).</summary>
        public float LeadError = 0.15f;
        /// <summary>Height bomber drones drop their grenades from.</summary>
        public float DroneAltitudeM = 30f;
        /// <summary>When guidance fails, the round lands about this far from the target (0.5x to 1.5x).</summary>
        public float GuidedMissM = 6f;
        /// <summary>A shooter who does not clearly see its target (seen chance below 1) aims this far off, on average.</summary>
        public float BlindAimErrorM = 4f;
        /// <summary>Area fire with spread_m: share of rounds landing in each equally wide ring, inner to outer.</summary>
        public float[] SpreadRingShares = { 0.4f, 0.35f, 0.25f };
        /// <summary>How long an enemy spotter (recon UAV) stays over its spot before flying out.</summary>
        public float AviationLoiterS = 60f;
        /// <summary>Aircraft weave: a shooter aiming at one is off by about this much (standard deviation).</summary>
        public float AircraftEvasionM = 1f;
        /// <summary>Budget at the start of a run (playtest economy, 2026-11-16).</summary>
        public float StartBudget = 800f;
        /// <summary>Budget added after each wave the defence holds.</summary>
        public float WaveIncome = 300f;
        /// <summary>Share of a defender's build_cost given back when it is removed.</summary>
        public float SellRefund = 0.5f;
        /// <summary>Defenders may stand only from this share of the field's depth down to our line (0 = anywhere).</summary>
        public float PlacementZoneStart = 0.35f;

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
            r.DroneLostWithOperator = Flag(values, "drone_lost_with_operator", r.DroneLostWithOperator);
            r.LeadError = Num(values, "lead_error", r.LeadError);
            r.DroneAltitudeM = Num(values, "drone_altitude_m", r.DroneAltitudeM);
            r.GuidedMissM = Num(values, "guided_miss_m", r.GuidedMissM);
            r.BlindAimErrorM = Num(values, "blind_aim_error_m", r.BlindAimErrorM);
            r.AviationLoiterS = Num(values, "aviation_loiter_s", r.AviationLoiterS);
            r.AircraftEvasionM = Num(values, "aircraft_evasion_m", r.AircraftEvasionM);
            r.StartBudget = Num(values, "start_budget", r.StartBudget);
            r.WaveIncome = Num(values, "wave_income", r.WaveIncome);
            r.SellRefund = Num(values, "sell_refund", r.SellRefund);
            r.PlacementZoneStart = Num(values, "placement_zone_start", r.PlacementZoneStart);
            if (values.TryGetValue("spread_ring_shares", out var rings) && rings.Length > 0)
            {
                var parts = rings.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                r.SpreadRingShares = new float[parts.Length];
                for (int i = 0; i < parts.Length; i++) r.SpreadRingShares[i] = float.Parse(parts[i], CultureInfo.InvariantCulture);
            }
            return r;
        }

        static bool Flag(Dictionary<string, string> v, string key, bool fallback) =>
            v.TryGetValue(key, out var s) && s.Length > 0 ? s.Equals("TRUE", StringComparison.OrdinalIgnoreCase) : fallback;

        static float Num(Dictionary<string, string> v, string key, float fallback) =>
            v.TryGetValue(key, out var s) && s.Length > 0 ? float.Parse(s, CultureInfo.InvariantCulture) : fallback;
    }
}
