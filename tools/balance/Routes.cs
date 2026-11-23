// Where the attackers really go (2026-11-23), for the scripted players in WaveCost and PlaytestRun.
// Fields without a spawns list let attackers enter anywhere along the top edge. Their routes join into a few lanes.
// This helper traces infantry and vehicle routes from many start points and groups them into lanes where they cross the
// placing zone. A scripted player then stands near a lane, not in the middle of the field.
using System;
using System.Collections.Generic;
using Treeline.Sim;

static class Routes
{
    public sealed class Lane
    {
        /// <summary>Where the lane crosses the middle of the placing zone.</summary>
        public float X;
        /// <summary>How many traced routes use this lane (a larger lane gets more defenders).</summary>
        public int Weight;
        /// <summary>Route points inside the placing zone.</summary>
        public readonly List<float[]> Points = new List<float[]>();
    }

    static readonly Dictionary<string, List<Lane>> cache = new Dictionary<string, List<Lane>>();

    /// <summary>Lanes on this grid, the busiest first. Call after the battle is built (it closes the side strips).</summary>
    public static List<Lane> Of(string key, NavGrid grid, float zoneTop, float zoneBottom)
    {
        if (cache.TryGetValue(key, out var have)) return have;
        var starts = new List<float>();
        if (grid.SpawnXs != null && grid.SpawnXs.Length > 0) starts.AddRange(grid.SpawnXs);
        else for (int i = 0; i < 15; i++) starts.Add(grid.Width * (i + 0.5f) / 15f);

        float midY = (zoneTop + zoneBottom) / 2f;
        var lanes = new List<Lane>();
        foreach (var cls in new[] { UnitClass.Infantry, UnitClass.Vehicle })
        {
            var flow = new FlowField(grid, cls);
            foreach (float sx in starts)
            {
                var path = flow.Trace(grid.CellAt(sx, NavGrid.CellSize * 0.5f));
                if (path.Count == 0) continue;
                float crossX = float.NaN;
                var pts = new List<float[]>();
                foreach (int cell in path)
                {
                    float x = grid.CenterX(cell), y = grid.CenterY(cell);
                    if (y < zoneTop || y > zoneBottom) continue;
                    pts.Add(new[] { x, y });
                    if (float.IsNaN(crossX) && y >= midY) crossX = x;
                }
                if (float.IsNaN(crossX)) continue;
                Lane lane = null;
                foreach (var l in lanes) if (Math.Abs(l.X - crossX) < 25f) { lane = l; break; }
                if (lane == null) { lane = new Lane { X = crossX }; lanes.Add(lane); }
                lane.Weight++;
                lane.Points.AddRange(pts);
            }
        }
        lanes.Sort((a, b) => b.Weight.CompareTo(a.Weight));
        if (lanes.Count > 3) lanes.RemoveRange(3, lanes.Count - 3);
        if (lanes.Count == 0) lanes.Add(new Lane { X = grid.Width / 2f, Weight = 1 });
        cache[key] = lanes;
        return lanes;
    }

    /// <summary>A point beside the lane, at a random place on it inside the placing zone.</summary>
    public static void Near(Lane lane, Random rng, float width, float zoneTop, float zoneBottom, out float x, out float y)
    {
        if (lane.Points.Count > 0)
        {
            var p = lane.Points[rng.Next(lane.Points.Count)];
            x = p[0] + (float)(rng.NextDouble() * 2 - 1) * 15f;
            y = p[1];
        }
        else
        {
            x = lane.X + (float)(rng.NextDouble() * 2 - 1) * 20f;
            y = zoneTop + (zoneBottom - zoneTop) * (float)rng.NextDouble();
        }
        x = Math.Max(12f, Math.Min(width - 12f, x));
    }
}
