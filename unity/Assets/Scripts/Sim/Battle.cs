using System;
using System.Collections.Generic;

namespace Treeline.Sim
{
    public enum BattlePhase { Preparation, Round }

    public enum DefenderState { Standing, Running }

    /// <summary>A defender on the field. While Running it is on its way in from the bottom edge and cannot fire.</summary>
    public sealed class Defender
    {
        public int Id;
        public UnitDef Def;
        public float X, Y;
        public float TargetX, TargetY;
        public DefenderState State;

        internal List<int> Path;      // cells from the entry point to the target
        internal int PathIndex;       // the cell the defender has reached
        internal float SegmentDone;   // 0..1 along the current step
    }

    /// <summary>
    /// The defence side of a battle: preparation phase, budget, and placing or moving defenders.
    /// In preparation everything is instant. During a round a placed or moved defender appears at the bottom edge
    /// and runs to its spot at three times the speed of a regular soldier, over the real terrain.
    /// </summary>
    public sealed class Battle
    {
        /// <summary>Running speed = this many times the speed of the unit with id <see cref="RunBaseUnitId"/>.</summary>
        public const float RunSpeedMultiplier = 3f;
        public const string RunBaseUnitId = "regular";

        readonly NavGrid grid;
        readonly UnitTable units;
        readonly List<Defender> defenders = new List<Defender>();
        int nextId = 1;

        public BattlePhase Phase { get; private set; } = BattlePhase.Preparation;
        public float Budget { get; private set; }
        public IReadOnlyList<Defender> Defenders => defenders;
        public float RunSpeedMps { get; }

        public Battle(NavGrid grid, UnitTable units, float budget)
        {
            this.grid = grid;
            this.units = units;
            Budget = budget;
            RunSpeedMps = units.TryGet(RunBaseUnitId, out var baseUnit) && baseUnit.SpeedMps > 0f
                ? baseUnit.SpeedMps * RunSpeedMultiplier
                : 18f;
        }

        public void StartRound()
        {
            if (Phase == BattlePhase.Preparation) Phase = BattlePhase.Round;
        }

        public bool TryPlace(string unitId, float x, float y, out Defender placed, out string why)
        {
            placed = null;
            if (!units.TryGet(unitId, out var def)) { why = "No such unit"; return false; }
            if (!def.IsDefence) { why = "Not a defender"; return false; }
            if (def.BuildCost > Budget) { why = "Not enough budget"; return false; }
            if (!CheckSpot(x, y, out var flow, out why)) return false;

            Budget -= def.BuildCost;
            var d = new Defender { Id = nextId++, Def = def };
            defenders.Add(d);
            Send(d, flow, x, y);
            placed = d;
            return true;
        }

        public bool TryMove(int defenderId, float x, float y, out string why)
        {
            var d = defenders.Find(e => e.Id == defenderId);
            if (d == null) { why = "No such defender"; return false; }
            if (!CheckSpot(x, y, out var flow, out why)) return false;
            Send(d, flow, x, y);
            return true;
        }

        public void Remove(int defenderId) => defenders.RemoveAll(e => e.Id == defenderId);

        bool CheckSpot(float x, float y, out FlowField flow, out string why)
        {
            flow = null;
            if (x < 0f || y < 0f || x > grid.Width || y > grid.Height) { why = "Outside the field"; return false; }
            int cell = grid.CellAt(x, y);
            if (float.IsPositiveInfinity(grid.Cost(UnitClass.Infantry)[cell])) { why = "Cannot stand there"; return false; }
            flow = FlowField.To(grid, UnitClass.Infantry, cell);
            if (EntryCell(flow) < 0) { why = "No way to get there from the bottom edge"; return false; }
            why = "";
            return true;
        }

        // The bottom-edge cell with the shortest way to the target.
        int EntryCell(FlowField flow)
        {
            int best = -1;
            double bestDist = double.PositiveInfinity;
            int row = grid.Rows - 1;
            for (int c = 0; c < grid.Cols; c++)
            {
                int i = row * grid.Cols + c;
                if (flow.Dist[i] < bestDist) { bestDist = flow.Dist[i]; best = i; }
            }
            return best;
        }

        void Send(Defender d, FlowField flow, float x, float y)
        {
            d.TargetX = x;
            d.TargetY = y;
            if (Phase == BattlePhase.Preparation)
            {
                d.X = x; d.Y = y;
                d.State = DefenderState.Standing;
                d.Path = null;
                return;
            }
            d.Path = flow.Trace(EntryCell(flow));
            d.PathIndex = 0;
            d.SegmentDone = 0f;
            d.X = grid.CenterX(d.Path[0]);
            d.Y = grid.CenterY(d.Path[0]);
            d.State = DefenderState.Running;
            if (d.Path.Count == 1) Arrive(d);
        }

        /// <summary>Advance the round by dt seconds. Does nothing in preparation.</summary>
        public void Step(float dt)
        {
            if (Phase != BattlePhase.Round) return;
            float[] cost = grid.Cost(UnitClass.Infantry);
            foreach (var d in defenders)
            {
                if (d.State != DefenderState.Running) continue;
                float budget = RunSpeedMps * dt;   // metres of open ground this step is worth
                while (budget > 0f && d.State == DefenderState.Running)
                {
                    int a = d.Path[d.PathIndex], b = d.Path[d.PathIndex + 1];
                    float ax = grid.CenterX(a), ay = grid.CenterY(a), bx = grid.CenterX(b), by = grid.CenterY(b);
                    float len = (float)Math.Sqrt((bx - ax) * (bx - ax) + (by - ay) * (by - ay));
                    float effective = len * (cost[a] + cost[b]) / 2f;   // slower ground makes the step longer
                    float left = effective * (1f - d.SegmentDone);
                    if (budget >= left)
                    {
                        budget -= left;
                        d.PathIndex++;
                        d.SegmentDone = 0f;
                        d.X = bx; d.Y = by;
                        if (d.PathIndex >= d.Path.Count - 1) Arrive(d);
                    }
                    else
                    {
                        d.SegmentDone += budget / effective;
                        budget = 0f;
                        d.X = ax + (bx - ax) * d.SegmentDone;
                        d.Y = ay + (by - ay) * d.SegmentDone;
                    }
                }
            }
        }

        static void Arrive(Defender d)
        {
            d.X = d.TargetX;
            d.Y = d.TargetY;
            d.State = DefenderState.Standing;
            d.Path = null;
        }
    }
}
