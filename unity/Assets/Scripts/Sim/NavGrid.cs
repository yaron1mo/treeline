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
        readonly float[][] cost = new float[2][];

        public int Count => Cols * Rows;
        public float[] Cost(UnitClass c) => cost[(int)c];

        public NavGrid(Field field)
        {
            Width = field.Width;
            Height = field.Height;
            Cols = (int)Math.Round(Width / CellSize);
            Rows = (int)Math.Round(Height / CellSize);
            Cells = new TerrainKind[Cols * Rows];
            cost[0] = new float[Cells.Length];
            cost[1] = new float[Cells.Length];
            Rasterize(field);
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
                    cost[0][i] = TerrainTable.Cost(t, UnitClass.Infantry);
                    cost[1][i] = TerrainTable.Cost(t, UnitClass.Vehicle);
                }
            }

            // Vehicles keep one cell clear of anything impassable, except on roads and fords.
            var veh = cost[1];
            var blocked = new bool[veh.Length];
            for (int i = 0; i < veh.Length; i++) blocked[i] = float.IsPositiveInfinity(veh[i]);
            for (int r = 0; r < Rows; r++)
            {
                for (int c = 0; c < Cols; c++)
                {
                    int i = r * Cols + c;
                    if (blocked[i] || Cells[i] == TerrainKind.Road || Cells[i] == TerrainKind.Ford) continue;
                    if ((c > 0 && blocked[i - 1]) || (c < Cols - 1 && blocked[i + 1]) ||
                        (r > 0 && blocked[i - Cols]) || (r < Rows - 1 && blocked[i + Cols]))
                        veh[i] = float.PositiveInfinity;
                }
            }
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
