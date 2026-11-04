using System;
using System.Collections.Generic;

namespace Treeline.Sim
{
    /// <summary>
    /// Cheapest-route map to our line: for every cell, the cost to reach the rear edge
    /// and which neighbouring cell to step to. One per unit class.
    /// </summary>
    public sealed class FlowField
    {
        public const int Exit = -2;   // Next value for cells on the rear edge
        public const int None = -1;   // Next value for cells that cannot reach the rear edge

        public readonly double[] Dist;
        public readonly int[] Next;
        readonly int cols, rows;

        static readonly int[] Dx = { 1, -1, 0, 0, 1, 1, -1, -1 };
        static readonly int[] Dy = { 0, 0, 1, -1, 1, -1, 1, -1 };

        public FlowField(NavGrid grid, UnitClass cls)
        {
            cols = grid.Cols;
            rows = grid.Rows;
            int n = grid.Count;
            Dist = new double[n];
            Next = new int[n];
            for (int i = 0; i < n; i++) { Dist[i] = double.PositiveInfinity; Next[i] = None; }

            float[] k = grid.Cost(cls);
            var heap = new MinHeap(n);

            // Start from every passable cell on the rear edge (the bottom row, nearest our line).
            for (int c = 0; c < cols; c++)
            {
                int i = (rows - 1) * cols + c;
                if (float.IsPositiveInfinity(k[i])) continue;
                Dist[i] = k[i] / 2.0;
                Next[i] = Exit;
                heap.Push(Dist[i], i);
            }

            while (heap.Count > 0)
            {
                heap.Pop(out double d, out int i);
                if (d > Dist[i]) continue;
                int c0 = i % cols, r0 = i / cols;
                for (int m = 0; m < 8; m++)
                {
                    int nc = c0 + Dx[m], nr = r0 + Dy[m];
                    if (nc < 0 || nc >= cols || nr < 0 || nr >= rows) continue;
                    int j = nr * cols + nc;
                    if (float.IsPositiveInfinity(k[j])) continue;
                    bool diagonal = Dx[m] != 0 && Dy[m] != 0;
                    // No squeezing diagonally between two blocked cells.
                    if (diagonal && (float.IsPositiveInfinity(k[r0 * cols + nc]) || float.IsPositiveInfinity(k[nr * cols + c0]))) continue;
                    double nd = d + (k[i] + k[j]) / 2.0 * (diagonal ? Math.Sqrt(2.0) : 1.0);
                    if (nd < Dist[j]) { Dist[j] = nd; Next[j] = i; heap.Push(nd, j); }
                }
            }
        }

        public bool Reachable(int cell) => !double.IsPositiveInfinity(Dist[cell]);

        /// <summary>Cells from <paramref name="start"/> to the rear edge, or an empty list if unreachable.</summary>
        public List<int> Trace(int start)
        {
            var path = new List<int>();
            if (!Reachable(start)) return path;
            int cur = start;
            for (int guard = 0; guard < Dist.Length && cur >= 0; guard++)
            {
                path.Add(cur);
                cur = Next[cur];
            }
            return path;
        }

        sealed class MinHeap
        {
            double[] keys;
            int[] vals;
            int n;

            public MinHeap(int capacity)
            {
                int cap = Math.Max(16, capacity);
                keys = new double[cap];
                vals = new int[cap];
            }

            public int Count => n;

            public void Push(double key, int val)
            {
                if (n == keys.Length) { Array.Resize(ref keys, n * 2); Array.Resize(ref vals, n * 2); }
                int i = n++;
                while (i > 0)
                {
                    int p = (i - 1) >> 1;
                    if (keys[p] <= key) break;
                    keys[i] = keys[p]; vals[i] = vals[p];
                    i = p;
                }
                keys[i] = key; vals[i] = val;
            }

            public void Pop(out double key, out int val)
            {
                key = keys[0]; val = vals[0];
                n--;
                if (n == 0) return;
                double lk = keys[n]; int lv = vals[n];
                int i = 0;
                for (;;)
                {
                    int l = 2 * i + 1;
                    if (l >= n) break;
                    int r = l + 1;
                    int m = (r < n && keys[r] < keys[l]) ? r : l;
                    if (keys[m] >= lk) break;
                    keys[i] = keys[m]; vals[i] = vals[m];
                    i = m;
                }
                keys[i] = lk; vals[i] = lv;
            }
        }
    }
}
