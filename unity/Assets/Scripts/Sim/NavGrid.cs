using System;
using System.Collections.Generic;

namespace Treeline.Sim
{
    /// <summary>
    /// Terrain kinds in drawing order: later ones paint over earlier ones.
    /// Open is the default ground; the rest match the web prototype.
    /// </summary>
    public enum TerrainKind : byte
    {
        Open = 0, Hill, Heap, Grove, Belt, Pond, River, Embankment, Balka, Pipe, Road, Ford
    }

    public enum UnitClass { Infantry = 0, Vehicle = 1 }

    public static class TerrainTable
    {
        const float X = float.PositiveInfinity;
        // Move cost per terrain (1 = open ground, higher = slower, X = impassable).
        // Same numbers as the web prototype; these will move into the data tables.
        static readonly float[] Infantry = { 1f, 1.6f, 3f, 2f, 2f, X, 4f, 2f, 1.2f, 2f, 0.8f, 2f };
        static readonly float[] Vehicle  = { 1f, 1.8f, X,  X,  3f, X, X,  X,  X,    X,  0.5f, 2f };

        public static float Cost(TerrainKind t, UnitClass c) => (c == UnitClass.Infantry ? Infantry : Vehicle)[(int)t];

        static readonly Dictionary<string, TerrainKind> Names = BuildNames();
        static Dictionary<string, TerrainKind> BuildNames()
        {
            var d = new Dictionary<string, TerrainKind>();
            foreach (TerrainKind t in Enum.GetValues(typeof(TerrainKind)))
                if (t != TerrainKind.Open) d[t.ToString().ToLowerInvariant()] = t;
            return d;
        }

        /// <summary>False for decoration such as fields, tracks and craters.</summary>
        public static bool TryParse(string name, out TerrainKind t) => Names.TryGetValue(name, out t);
    }

    static class Geometry
    {
        public static float DistSeg(float px, float py, float ax, float ay, float bx, float by)
        {
            float dx = bx - ax, dy = by - ay, l = dx * dx + dy * dy;
            float t = l > 0f ? ((px - ax) * dx + (py - ay) * dy) / l : 0f;
            t = t < 0f ? 0f : (t > 1f ? 1f : t);
            float qx = ax + t * dx - px, qy = ay + t * dy - py;
            return (float)Math.Sqrt(qx * qx + qy * qy);
        }

        public static float DistLine(float px, float py, Pt[] pts)
        {
            float m = float.PositiveInfinity;
            for (int i = 1; i < pts.Length; i++)
            {
                float d = DistSeg(px, py, pts[i - 1].X, pts[i - 1].Y, pts[i].X, pts[i].Y);
                if (d < m) m = d;
            }
            return m;
        }

        public static bool InPoly(float px, float py, Pt[] pts)
        {
            bool c = false;
            for (int i = 0, j = pts.Length - 1; i < pts.Length; j = i++)
            {
                float xi = pts[i].X, yi = pts[i].Y, xj = pts[j].X, yj = pts[j].Y;
                if ((yi > py) != (yj > py) && px < (xj - xi) * (py - yi) / (yj - yi) + xi) c = !c;
            }
            return c;
        }
    }

    /// <summary>
    /// The hidden navigation grid: the free-form terrain rasterised into 2 m cells.
    /// Players never see it; units steer freely, this only feeds the path search.
    /// </summary>
    public sealed class NavGrid
    {
        public const float CellSize = 2f;

        public readonly int Cols, Rows;
        public readonly float Width, Height;
        public readonly TerrainKind[] Cells;
        /// <summary>Attacker entry points along the top edge (x in metres); empty = anywhere.</summary>
        public readonly float[] SpawnXs;
        /// <summary>Goes up every time the ground changes during a battle (a wreck); routes must then be rebuilt.</summary>
        public int Version { get; private set; }

        readonly ObstacleTable table;
        readonly float[][] raw = new float[2][];    // move cost before the vehicle clearance rule
        readonly float[][] cost = new float[2][];   // what routes use
        readonly bool[] hasObject;                   // a rock, wreck, wire... painted over the terrain
        readonly Dictionary<float, float[]> treeProfiles = new Dictionary<float, float[]>();
        int treeProfilesVersion;

        public int Count => Cols * Rows;
        public float[] Cost(UnitClass c) => cost[(int)c];

        /// <param name="table">Obstacle table: move costs for terrain and objects. Without it the built-in
        /// prototype numbers are used and objects (rocks, wrecks) do not affect movement.</param>
        public NavGrid(Field field, ObstacleTable table = null)
        {
            this.table = table;
            Width = field.Width;
            Height = field.Height;
            Cols = (int)Math.Round(Width / CellSize);
            Rows = (int)Math.Round(Height / CellSize);
            SpawnXs = field.Spawns.ToArray();
            Cells = new TerrainKind[Cols * Rows];
            hasObject = new bool[Cells.Length];
            for (int k = 0; k < 2; k++) { raw[k] = new float[Cells.Length]; cost[k] = new float[Cells.Length]; }
            Rasterize(field);
            if (table != null) PaintObjects(field);
            Rebuild();
        }

        float TerrainCost(TerrainKind t, UnitClass c)
        {
            if (table != null && t != TerrainKind.Open && table.TryGet(t.ToString().ToLowerInvariant(), out var o))
            {
                var v = o.MoveCost(c);
                if (v.HasValue) return v.Value;
            }
            return TerrainTable.Cost(t, c);
        }

        struct Shape
        {
            public Feature F;
            public TerrainKind T;
            public float X0, Y0, X1, Y1;
        }

        static float Half(Feature f) => Math.Max(f.Width / 2f, CellSize * 0.75f);

        static bool Covers(Feature f, float x, float y) =>
            f.Poly != null ? Geometry.InPoly(x, y, f.Poly) : Geometry.DistLine(x, y, f.Line) <= Half(f);

        void Rasterize(Field field)
        {
            var shapes = new List<Shape>();
            foreach (var f in field.Features)
            {
                if (!TerrainTable.TryParse(f.Type, out var t)) continue;
                var pts = f.Poly ?? f.Line;
                if (pts == null || pts.Length == 0) continue;
                float pad = f.Poly == null ? Half(f) : 0f;
                float x0 = float.MaxValue, y0 = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue;
                foreach (var p in pts)
                {
                    if (p.X < x0) x0 = p.X; if (p.Y < y0) y0 = p.Y;
                    if (p.X > x1) x1 = p.X; if (p.Y > y1) y1 = p.Y;
                }
                shapes.Add(new Shape { F = f, T = t, X0 = x0 - pad, Y0 = y0 - pad, X1 = x1 + pad, Y1 = y1 + pad });
            }
            // Sort by terrain order so later kinds paint over earlier ones.
            // Order among shapes of the same kind does not matter: they paint the same value.
            var order = shapes;
            order.Sort((a, b) => ((int)a.T).CompareTo((int)b.T));

            for (int r = 0; r < Rows; r++)
            {
                for (int c = 0; c < Cols; c++)
                {
                    float x = (c + 0.5f) * CellSize, y = (r + 0.5f) * CellSize;
                    var t = TerrainKind.Open;
                    foreach (var s in order)
                    {
                        if (x < s.X0 || x > s.X1 || y < s.Y0 || y > s.Y1) continue;
                        if (Covers(s.F, x, y)) t = s.T;
                    }
                    int i = r * Cols + c;
                    Cells[i] = t;
                    raw[0][i] = TerrainCost(t, UnitClass.Infantry);
                    raw[1][i] = TerrainCost(t, UnitClass.Vehicle);
                }
            }
        }

        // Objects from the obstacle table (rock, wreck, bush, wire, hedgehog, trench) paint their move cost over the terrain.
        void PaintObjects(Field field)
        {
            foreach (var f in field.Features)
            {
                if (TerrainTable.TryParse(f.Type, out _)) continue;
                if (!table.TryGet(f.Type, out var o)) continue;
                if (!o.MoveCostInfantry.HasValue && !o.MoveCostVehicle.HasValue) continue;
                var pts = f.Poly ?? f.Line;
                if (pts == null || pts.Length == 0) continue;
                float x0 = float.MaxValue, y0 = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue;
                foreach (var p in pts)
                {
                    if (p.X < x0) x0 = p.X; if (p.Y < y0) y0 = p.Y;
                    if (p.X > x1) x1 = p.X; if (p.Y > y1) y1 = p.Y;
                }
                float pad = f.Poly == null ? Half(f) : 0f;
                int painted = 0;
                for (int r = Math.Max(0, (int)((y0 - pad) / CellSize)); r <= Math.Min(Rows - 1, (int)((y1 + pad) / CellSize)); r++)
                    for (int c = Math.Max(0, (int)((x0 - pad) / CellSize)); c <= Math.Min(Cols - 1, (int)((x1 + pad) / CellSize)); c++)
                        if (Covers(f, (c + 0.5f) * CellSize, (r + 0.5f) * CellSize)) { SetObject(r * Cols + c, o); painted++; }
                // A small object may miss every cell centre: put it in the cell of its middle.
                if (painted == 0) SetObject(CellAt((x0 + x1) / 2f, (y0 + y1) / 2f), o);
            }
        }

        void SetObject(int i, Obstacle o)
        {
            hasObject[i] = true;
            if (o.MoveCostInfantry.HasValue) raw[0][i] = o.MoveCostInfantry.Value;
            if (o.MoveCostVehicle.HasValue) raw[1][i] = o.MoveCostVehicle.Value;
        }

        /// <summary>
        /// Put an object on the ground during a battle (a destroyed vehicle leaves a wreck): every cell whose
        /// centre is within <paramref name="radius"/> takes the object's move cost. Routes must be rebuilt after.
        /// </summary>
        public void PlaceObject(Obstacle o, float x, float y, float radius)
        {
            int centre = CellAt(x, y);
            SetObject(centre, o);
            int reach = (int)Math.Ceiling(radius / CellSize);
            int c0 = centre % Cols, r0 = centre / Cols;
            for (int r = Math.Max(0, r0 - reach); r <= Math.Min(Rows - 1, r0 + reach); r++)
                for (int c = Math.Max(0, c0 - reach); c <= Math.Min(Cols - 1, c0 + reach); c++)
                {
                    float dx = (c + 0.5f) * CellSize - x, dy = (r + 0.5f) * CellSize - y;
                    if (dx * dx + dy * dy <= radius * radius) SetObject(r * Cols + c, o);
                }
            Rebuild();
            Version++;
        }

        void Rebuild()
        {
            Array.Copy(raw[0], cost[0], raw[0].Length);
            Clearance(raw[1], cost[1]);
        }

        // Vehicles keep one cell clear of anything impassable, except on roads and fords.
        void Clearance(float[] from, float[] to)
        {
            for (int r = 0; r < Rows; r++)
            {
                for (int c = 0; c < Cols; c++)
                {
                    int i = r * Cols + c;
                    float v = from[i];
                    if (!float.IsPositiveInfinity(v) && Cells[i] != TerrainKind.Road && Cells[i] != TerrainKind.Ford &&
                        ((c > 0 && float.IsPositiveInfinity(from[i - 1])) || (c < Cols - 1 && float.IsPositiveInfinity(from[i + 1])) ||
                         (r > 0 && float.IsPositiveInfinity(from[i - Cols])) || (r < Rows - 1 && float.IsPositiveInfinity(from[i + Cols]))))
                        v = float.PositiveInfinity;
                    to[i] = v;
                }
            }
        }

        /// <summary>
        /// Move costs for one unit. A vehicle with its own <c>tree_move_cost</c> (tanks push through groves and
        /// forest belts) uses that number in tree cells instead of the table's vehicle cost.
        /// </summary>
        public float[] CostFor(UnitClass c, float treeMoveCost)
        {
            if (c == UnitClass.Infantry || treeMoveCost <= 0f) return Cost(c);
            if (treeProfilesVersion != Version) { treeProfiles.Clear(); treeProfilesVersion = Version; }
            if (treeProfiles.TryGetValue(treeMoveCost, out var have)) return have;
            var src = (float[])raw[1].Clone();
            for (int i = 0; i < src.Length; i++)
                if (!hasObject[i] && (Cells[i] == TerrainKind.Grove || Cells[i] == TerrainKind.Belt)) src[i] = treeMoveCost;
            var result = new float[src.Length];
            Clearance(src, result);
            treeProfiles[treeMoveCost] = result;
            return result;
        }

        public int CellAt(float x, float y)
        {
            int c = Math.Min(Cols - 1, Math.Max(0, (int)Math.Floor(x / CellSize)));
            int r = Math.Min(Rows - 1, Math.Max(0, (int)Math.Floor(y / CellSize)));
            return r * Cols + c;
        }

        public float CenterX(int cell) => (cell % Cols + 0.5f) * CellSize;
        public float CenterY(int cell) => (cell / Cols + 0.5f) * CellSize;
    }
}
