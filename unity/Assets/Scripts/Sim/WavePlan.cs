using System;
using System.Collections.Generic;
using System.Globalization;

namespace Treeline.Sim
{
    /// <summary>
    /// The attack waves of a run (unity/Assets/Data/waves.csv, 2026-11-16): one row per group,
    /// <c>wave,unit,count,delay_s,interval_s</c>. Groups of one wave start together; delay_s counts from the wave start.
    /// </summary>
    public sealed class WavePlan
    {
        public sealed class Group
        {
            public int Wave;
            public string UnitId;
            public int Count;
            public float DelayS, IntervalS;
        }

        readonly List<Group> groups = new List<Group>();
        public IReadOnlyList<Group> Groups => groups;

        /// <summary>Number of waves (the highest wave number).</summary>
        public int WaveCount { get; private set; }

        public static WavePlan Parse(string csv)
        {
            var plan = new WavePlan();
            var rows = Csv.Parse(csv);
            if (rows.Count == 0) return plan;
            var head = rows[0];
            int cw = Col(head, "wave"), cu = Col(head, "unit"), cc = Col(head, "count"), cd = Col(head, "delay_s"), ci = Col(head, "interval_s");
            for (int n = 1; n < rows.Count; n++)
            {
                var r = rows[n];
                if (r.Length <= ci || r[cw].Trim().Length == 0) continue;
                var g = new Group
                {
                    Wave = int.Parse(r[cw].Trim(), CultureInfo.InvariantCulture),
                    UnitId = r[cu].Trim(),
                    Count = int.Parse(r[cc].Trim(), CultureInfo.InvariantCulture),
                    DelayS = r[cd].Trim().Length > 0 ? float.Parse(r[cd].Trim(), CultureInfo.InvariantCulture) : 0f,
                    IntervalS = r[ci].Trim().Length > 0 ? float.Parse(r[ci].Trim(), CultureInfo.InvariantCulture) : 1f,
                };
                plan.groups.Add(g);
                if (g.Wave > plan.WaveCount) plan.WaveCount = g.Wave;
            }
            return plan;
        }

        static int Col(string[] head, string name)
        {
            for (int i = 0; i < head.Length; i++) if (string.Equals(head[i].Trim(), name, StringComparison.OrdinalIgnoreCase)) return i;
            throw new FormatException("waves.csv: no column " + name);
        }

        /// <summary>The groups of one wave.</summary>
        public List<Group> WaveGroups(int wave)
        {
            var list = new List<Group>();
            foreach (var g in groups) if (g.Wave == wave) list.Add(g);
            return list;
        }

        /// <summary>Begin the next wave in the battle and queue this wave's groups into it. False if a unit is unknown.</summary>
        public bool Queue(Battle battle, int wave)
        {
            battle.BeginWave();
            bool ok = true;
            foreach (var g in groups)
                if (g.Wave == wave && !battle.QueueWave(g.UnitId, g.Count, g.DelayS, g.IntervalS)) ok = false;
            return ok;
        }
    }
}
