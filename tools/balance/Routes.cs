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
        /// <summary>The whole traced route, every third cell (houses and hill tops are scored against it).</summary>
        public readonly List<float[]> All = new List<float[]>();
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
                var all = new List<float[]>();
                int step = 0;
                foreach (int cell in path)
                {
                    float x = grid.CenterX(cell), y = grid.CenterY(cell);
                    if (step++ % 3 == 0) all.Add(new[] { x, y });
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
                lane.All.AddRange(all);
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

    // ---- terrain spots (2026-11-28, issue #49): houses and hill tops as candidate spots ---------------------
    // The scripted players score houses and hill tops next to the routes, per defender type: a house near the
    // route scores high for short-range (infantry) types, a hill top that sees the route for long-range types
    // and recon. A spot above LaneScore (1) is tried before the plain lane points. Deterministic: the same
    // field and defender type always give the same list in the same order.

    public sealed class Spot
    {
        public float X, Y;
        /// <summary>Index into VisionMap.Houses when this spot is a house, else -1.</summary>
        public int House = -1;
        public float Score;
        /// <summary>Feature order, the tie-break that keeps the sort stable.</summary>
        public int Order;
    }

    public const float LaneScore = 1f;   // the score of a plain lane point
    const float HouseNearRouteM = 60f;   // a house this far from a route counts as "near the route"
    const float HillSeesRouteChance = 0.25f;

    static readonly Dictionary<string, List<Spot>> spotCache = new Dictionary<string, List<Spot>>();

    /// <summary>House and hill-top spots near the routes, scored for this defender type, best first.
    /// Only spots inside the placing band count: the scripted players defend from the band, and a house or hill
    /// on the enemy side of the field is not a spot for them (they would be shot there before they can help).
    /// Empty on fields without houses or hills in the band (the players then place on the lanes as before).</summary>
    public static List<Spot> TerrainSpots(string key, Field field, VisionMap vision, List<Lane> lanes, UnitDef def, float zoneTop, float zoneBottom)
    {
        string ck = key + "|" + def.Id + "|" + zoneTop + "|" + zoneBottom;
        if (spotCache.TryGetValue(ck, out var have)) return have;
        var route = new List<float[]>();
        foreach (var l in lanes) route.AddRange(l.All);
        bool recon = def.Type == "aviation" || def.Id == "recon_drone";
        bool longRange = def.RangeM >= 200f;   // sniper, mortar, anti-tank, air defence: the hill gives them range
        bool infantry = !recon && !longRange;  // rifle squad, machine gun: the house gives them cover
        var spots = new List<Spot>();
        int order = 0;
        for (int i = 0; i < vision.Houses.Count; i++)
        {
            var h = vision.Houses[i];
            if (h.Y < zoneTop || h.Y > zoneBottom) continue;
            float d = DistToRoute(h.X, h.Y, route);
            if (d > HouseNearRouteM) continue;
            float score = infantry ? 3f + 0.5f * (1f - d / HouseNearRouteM) : 0.8f;
            spots.Add(new Spot { X = h.X, Y = h.Y, House = i, Score = score, Order = order++ });
        }
        foreach (var f in field.Features)
        {
            if (!vision.Table.TryGet(f.Type, out var o) || !o.HasOnBonus) continue;   // only hills give an on-top bonus
            var pts = f.Poly ?? f.Line;
            if (pts == null || pts.Length == 0) continue;
            float x = 0f, y = 0f;
            foreach (var p in pts) { x += p.X; y += p.Y; }
            x /= pts.Length; y /= pts.Length;
            if (y < zoneTop || y > zoneBottom) continue;
            var top = vision.ObstacleAt(x, y);
            if (top == null || !top.HasOnBonus) continue;   // the top is not painted (too small for a cell)
            bool sees = SeesRoute(vision, x, y, def, route);
            float score = (longRange || recon) && sees ? 3f : 0.8f;
            spots.Add(new Spot { X = x, Y = y, Score = score, Order = order++ });
        }
        spots.Sort((a, b) => b.Score != a.Score ? b.Score.CompareTo(a.Score) : a.Order.CompareTo(b.Order));
        spotCache[ck] = spots;
        return spots;
    }

    static float DistToRoute(float x, float y, List<float[]> route)
    {
        float best = float.MaxValue;
        foreach (var p in route)
        {
            float dx = p[0] - x, dy = p[1] - y, d = dx * dx + dy * dy;
            if (d < best) best = d;
        }
        return (float)Math.Sqrt(best);
    }

    // Does a unit of this type standing at (x, y) (on the hill top, with its sight bonus) see any route point?
    static bool SeesRoute(VisionMap vision, float x, float y, UnitDef def, List<float[]> route)
    {
        float sight = def.SightRangeM > 0f ? def.SightRangeM : 30f;
        float radius = vision.SightRadius(x, y, sight);
        foreach (var p in route)
        {
            float dx = p[0] - x, dy = p[1] - y;
            if (dx * dx + dy * dy > radius * radius) continue;
            if (vision.SeeChance(x, y, sight, p[0], p[1]) >= HillSeesRouteChance) return true;
        }
        return false;
    }
}
