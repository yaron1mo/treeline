using System;
using System.Collections.Generic;

namespace Treeline.Sim
{
    /// <summary>
    /// What a unit can see. Obstacles from the field are rasterised into the same 2 m cells as the nav grid
    /// (players never see them). A full obstacle hides everything behind it, like a slice cut from the sight
    /// circle; a soft one (grove, forest belt) only blurs: the chance to recognise something is highest right
    /// behind it and falls with distance. Pure maths, no randomness: the caller rolls against the chance.
    /// </summary>
    public sealed class VisionMap
    {
        public readonly int Cols, Rows;
        public readonly float Width, Height;
        readonly short[] cells;                 // index into 'used', -1 = nothing
        readonly List<Obstacle> used = new List<Obstacle>();

        /// <summary>The obstacle table this map was built from.</summary>
        public readonly ObstacleTable Table;

        public VisionMap(Field field, ObstacleTable table)
        {
            Table = table;
            Width = field.Width;
            Height = field.Height;
            Cols = (int)Math.Round(Width / NavGrid.CellSize);
            Rows = (int)Math.Round(Height / NavGrid.CellSize);
            cells = new short[Cols * Rows];
            for (int i = 0; i < cells.Length; i++) cells[i] = -1;

            foreach (var f in field.Features)
            {
                if (!table.TryGet(f.Type, out var o)) continue;
                if (o.Sight == SightEffect.None && !o.HasOnBonus && string.IsNullOrEmpty(o.CoverApplies)) continue;   // a trench hides nothing but still covers
                var pts = f.Poly ?? f.Line;
                if (pts == null || pts.Length == 0) continue;
                Paint(f, o, pts);
            }
        }

        static int Priority(Obstacle o) => o.Sight == SightEffect.Full ? 2 : (o.Sight == SightEffect.Soft ? 1 : 0);

        static float Half(Feature f) => Math.Max(f.Width / 2f, NavGrid.CellSize * 0.75f);

        void Paint(Feature f, Obstacle o, Pt[] pts)
        {
            int idx = used.IndexOf(o);
            if (idx < 0) { used.Add(o); idx = used.Count - 1; }

            float pad = f.Poly == null ? Half(f) : 0f;
            float x0 = float.MaxValue, y0 = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue;
            foreach (var p in pts)
            {
                if (p.X < x0) x0 = p.X; if (p.Y < y0) y0 = p.Y;
                if (p.X > x1) x1 = p.X; if (p.Y > y1) y1 = p.Y;
            }
            int c0 = Math.Max(0, (int)Math.Floor((x0 - pad) / NavGrid.CellSize));
            int c1 = Math.Min(Cols - 1, (int)Math.Floor((x1 + pad) / NavGrid.CellSize));
            int r0 = Math.Max(0, (int)Math.Floor((y0 - pad) / NavGrid.CellSize));
            int r1 = Math.Min(Rows - 1, (int)Math.Floor((y1 + pad) / NavGrid.CellSize));

            int painted = 0;
            for (int r = r0; r <= r1; r++)
            {
                for (int c = c0; c <= c1; c++)
                {
                    float x = (c + 0.5f) * NavGrid.CellSize, y = (r + 0.5f) * NavGrid.CellSize;
                    bool covered = f.Poly != null ? Geometry.InPoly(x, y, f.Poly) : Geometry.DistLine(x, y, f.Line) <= Half(f);
                    if (covered && Set(r * Cols + c, o, idx)) painted++;
                }
            }

            // A small object (a rock) may miss every cell centre: put it in the cell of its middle.
            if (painted == 0 && f.Poly != null)
            {
                int c = (int)Math.Floor((x0 + x1) / 2f / NavGrid.CellSize);
                int r = (int)Math.Floor((y0 + y1) / 2f / NavGrid.CellSize);
                if (c >= 0 && c < Cols && r >= 0 && r < Rows) Set(r * Cols + c, o, idx);
            }
        }

        /// <summary>Put an obstacle on the map during a battle (a wreck): every cell within the radius, at least one.</summary>
        public void PlaceObject(Obstacle o, float x, float y, float radius)
        {
            int idx = used.IndexOf(o);
            if (idx < 0) { used.Add(o); idx = used.Count - 1; }
            int centre = CellOf(x, y);
            Set(centre, o, idx);
            int reach = (int)Math.Ceiling(radius / NavGrid.CellSize);
            int c0 = centre % Cols, r0 = centre / Cols;
            for (int r = Math.Max(0, r0 - reach); r <= Math.Min(Rows - 1, r0 + reach); r++)
                for (int c = Math.Max(0, c0 - reach); c <= Math.Min(Cols - 1, c0 + reach); c++)
                {
                    float dx = (c + 0.5f) * NavGrid.CellSize - x, dy = (r + 0.5f) * NavGrid.CellSize - y;
                    if (dx * dx + dy * dy <= radius * radius) Set(r * Cols + c, o, idx);
                }
        }

        bool Set(int cell, Obstacle o, int idx)
        {
            short cur = cells[cell];
            if (cur >= 0 && Priority(used[cur]) > Priority(o)) return false;
            cells[cell] = (short)idx;
            return true;
        }

        int CellOf(float x, float y)
        {
            int c = Math.Min(Cols - 1, Math.Max(0, (int)Math.Floor(x / NavGrid.CellSize)));
            int r = Math.Min(Rows - 1, Math.Max(0, (int)Math.Floor(y / NavGrid.CellSize)));
            return r * Cols + c;
        }

        /// <summary>The obstacle painted in a cell, or null.</summary>
        public Obstacle ObstacleInCell(int cell) => cells[cell] >= 0 ? used[cells[cell]] : null;

        public Obstacle ObstacleAt(float x, float y) => ObstacleInCell(CellOf(x, y));

        /// <summary>Sight radius for a unit standing at this point (a hill gives a bonus).</summary>
        public float SightRadius(float x, float y, float baseSight)
        {
            var o = ObstacleAt(x, y);
            return o != null ? baseSight * o.OnSightMult : baseSight;
        }

        struct SoftSegment
        {
            public Obstacle O;
            public float From, To;   // distances along the ray where it enters and leaves
            public bool Inside;      // the observer starts inside it
        }

        /// <summary>
        /// Chance (0 to 1) that the observer recognises something at the target point.
        /// 0 = not seen (out of range or hidden), 1 = clearly seen.
        /// Soft obstacles (grove, forest belt) blur the view along the ray: a target inside one, or behind one
        /// the ray entered from outside, is harder to recognise the farther it is. An observer standing inside
        /// a belt sees clearly past the edge of it, so its circle becomes an hourglass: short along the belt,
        /// full towards the open ground.
        /// </summary>
        public float SeeChance(float ox, float oy, float tx, float ty, float baseSight)
        {
            float dx = tx - ox, dy = ty - oy;
            float dist = (float)Math.Sqrt(dx * dx + dy * dy);
            if (dist > SightRadius(ox, oy, baseSight)) return 0f;
            if (dist < 1e-3f) return 1f;

            int startCell = CellOf(ox, oy), targetCell = CellOf(tx, ty);
            var own = cells[startCell] >= 0 ? used[cells[startCell]] : null;

            // Looking at the body of an obstacle is fine: its own cells do not hide it from you.
            var targetFull = cells[targetCell] >= 0 && used[cells[targetCell]].Sight == SightEffect.Full ? used[cells[targetCell]] : null;
            Obstacle firstFull = null;
            bool leftBody = false;

            List<SoftSegment> segments = null;
            int curId = -1;
            float curFrom = 0f, curTo = 0f;
            bool curInside = false;

            // Walk every cell the ray touches, in order (grid traversal), with the distance where it enters and leaves each.
            const float C = NavGrid.CellSize;
            float dirX = dx / dist, dirY = dy / dist;
            int cx = (int)Math.Floor(ox / C), cy = (int)Math.Floor(oy / C);
            int endX = (int)Math.Floor(tx / C), endY = (int)Math.Floor(ty / C);
            int stepX = dirX > 0 ? 1 : (dirX < 0 ? -1 : 0), stepY = dirY > 0 ? 1 : (dirY < 0 ? -1 : 0);
            float nextX = stepX == 0 ? float.PositiveInfinity : ((stepX > 0 ? (cx + 1) * C : cx * C) - ox) / dirX;
            float nextY = stepY == 0 ? float.PositiveInfinity : ((stepY > 0 ? (cy + 1) * C : cy * C) - oy) / dirY;
            float deltaX = stepX == 0 ? float.PositiveInfinity : C / Math.Abs(dirX);
            float deltaY = stepY == 0 ? float.PositiveInfinity : C / Math.Abs(dirY);

            float enter = 0f;
            int guard = Cols + Rows + 4;
            for (int k = 0; k < guard; k++)
            {
                float leave = Math.Min(Math.Min(nextX, nextY), dist);
                int cell = CellOf((cx + 0.5f) * C, (cy + 0.5f) * C);
                short ci = cells[cell];

                // Track the soft stretches of the ray.
                int sid = ci >= 0 && used[ci].Sight == SightEffect.Soft ? ci : -1;
                if (sid != curId)
                {
                    if (curId >= 0)
                    {
                        if (segments == null) segments = new List<SoftSegment>();
                        segments.Add(new SoftSegment { O = used[curId], From = curFrom, To = curTo, Inside = curInside });
                    }
                    curId = sid;
                    if (sid >= 0) { curFrom = enter; curTo = leave; curInside = k == 0; }
                }
                else if (sid >= 0) curTo = leave;

                if (ci < 0) { if (firstFull != null) leftBody = true; }
                else
                {
                    var o = used[ci];
                    if (o.Sight == SightEffect.Full)
                    {
                        // Standing on it (a hill) or looking at the obstacle itself does not block.
                        bool free = o == own || cell == targetCell
                            || (o == targetFull && !leftBody && (firstFull == null || firstFull == o));
                        if (!free) return 0f;
                        if (o != own && cell != targetCell) firstFull = o;
                    }
                    else if (firstFull != null) leftBody = true;
                }

                if ((cx == endX && cy == endY) || leave >= dist) break;
                enter = leave;
                if (nextX < nextY) { cx += stepX; nextX += deltaX; }
                else { cy += stepY; nextY += deltaY; }
            }
            if (curId >= 0)
            {
                if (segments == null) segments = new List<SoftSegment>();
                segments.Add(new SoftSegment { O = used[curId], From = curFrom, To = curTo, Inside = curInside });
            }

            float chance = 1f;
            if (segments != null)
            {
                foreach (var seg in segments)
                {
                    float c;
                    if (dist <= seg.To + 1e-3f) c = seg.O.DetectChance - seg.O.FalloffPerM * (dist - seg.From); // target inside it
                    else if (seg.Inside) continue;                                                          // clear past the edge of our own belt
                    else c = seg.O.DetectChance - seg.O.FalloffPerM * (dist - seg.To);                      // behind it
                    if (c < chance) chance = c;
                }
            }
            return chance < 0f ? 0f : chance;
        }

        /// <summary>
        /// The obstacle protecting the target from this shooter, or null. Standing inside one (trench, forest)
        /// counts; so does having a full obstacle (rock, wreck) within a few metres on the shooter's side.
        /// </summary>
        public Obstacle CoverFor(float sx, float sy, float tx, float ty)
        {
            var here = ObstacleAt(tx, ty);
            if (here != null && here.CoverApplies == "inside") return here;

            float dx = sx - tx, dy = sy - ty;
            float dist = (float)Math.Sqrt(dx * dx + dy * dy);
            if (dist < 1e-3f) return null;
            int targetCell = CellOf(tx, ty);
            for (float d = 0.5f; d <= Math.Min(6f, dist); d += 0.5f)
            {
                int cell = CellOf(tx + dx / dist * d, ty + dy / dist * d);
                if (cell == targetCell) continue;
                var o = ObstacleInCell(cell);
                if (o != null && o != here && o.Sight == SightEffect.Full && o.CoverApplies == "behind") return o;
            }
            return null;
        }

        /// <summary>The chance for every cell centre around the observer, indexed like the nav grid (row * Cols + col).</summary>
        public float[] Compute(float ox, float oy, float baseSight)
        {
            var result = new float[Cols * Rows];
            float radius = SightRadius(ox, oy, baseSight);
            int c0 = Math.Max(0, (int)Math.Floor((ox - radius) / NavGrid.CellSize));
            int c1 = Math.Min(Cols - 1, (int)Math.Floor((ox + radius) / NavGrid.CellSize));
            int r0 = Math.Max(0, (int)Math.Floor((oy - radius) / NavGrid.CellSize));
            int r1 = Math.Min(Rows - 1, (int)Math.Floor((oy + radius) / NavGrid.CellSize));
            for (int r = r0; r <= r1; r++)
                for (int c = c0; c <= c1; c++)
                    result[r * Cols + c] = SeeChance(ox, oy, (c + 0.5f) * NavGrid.CellSize, (r + 0.5f) * NavGrid.CellSize, baseSight);
            return result;
        }
    }
}
