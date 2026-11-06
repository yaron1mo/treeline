using System;
using System.Collections.Generic;

namespace Treeline.Sim
{
    public enum BattlePhase { Preparation, Round }

    public enum DefenderState { Standing, Running }

    public enum BattleOutcome { Ongoing, Won, Lost }

    /// <summary>Anything with a position and hit points on the field.</summary>
    public abstract class Unit
    {
        public int Id;
        public UnitDef Def;
        public float X, Y;
        public float Hp;
        public bool Alive = true;
        /// <summary>0..1: how well the other side currently sees this unit (shared vision).</summary>
        public float SeenChance;

        internal List<int> Path;      // cells to walk
        internal int PathIndex;       // the cell the unit has reached
        internal float SegmentDone;   // 0..1 along the current step
    }

    /// <summary>A defender on the field. While Running it is on its way in from the bottom edge and cannot fire.</summary>
    public sealed class Defender : Unit
    {
        public float TargetX, TargetY;
        public DefenderState State;
    }

    /// <summary>An attacker walking or driving to the rear edge.</summary>
    public sealed class Attacker : Unit
    {
        public UnitClass Class;
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

        const float VisionInterval = 0.25f;

        struct Spawn { public float Time; public string UnitId; }

        sealed class SpawnOrder : IComparer<Spawn>
        {
            public int Compare(Spawn a, Spawn b) => a.Time.CompareTo(b.Time);
        }

        readonly NavGrid grid;
        readonly UnitTable units;
        readonly VisionMap vision;
        readonly ArmourTable armour;
        readonly Random rng;
        readonly List<Defender> defenders = new List<Defender>();
        readonly List<Attacker> attackers = new List<Attacker>();
        readonly List<Spawn> spawns = new List<Spawn>();
        readonly FlowField[] flows = new FlowField[2];
        readonly int[][] startCells = new int[2][];
        int nextId = 1;
        int nextSpawn;
        bool waveQueued;
        float visionTimer = VisionInterval;

        public BattlePhase Phase { get; private set; } = BattlePhase.Preparation;
        public float Budget { get; private set; }
        public IReadOnlyList<Defender> Defenders => defenders;
        public IReadOnlyList<Attacker> Attackers => attackers;
        public float RunSpeedMps { get; }
        public BattleOutcome Outcome { get; private set; } = BattleOutcome.Ongoing;
        public float RoundTime { get; private set; }
        /// <summary>Damage the attackers have done to our line by reaching the rear edge.</summary>
        public float LineDamage { get; private set; }
        public float LineHealth { get; }
        /// <summary>Attackers still waiting to enter the field.</summary>
        public int Pending => spawns.Count - nextSpawn;

        /// <param name="vision">Obstacles for sight and cover. Without it everything within sight radius is seen and nothing gives cover.</param>
        /// <param name="armour">Armour table. Without it armour does not reduce damage.</param>
        /// <param name="lineHealth">How much line damage the defence can take (placeholder number until balanced).</param>
        public Battle(NavGrid grid, UnitTable units, float budget, VisionMap vision = null, ArmourTable armour = null,
            float lineHealth = 20f, int seed = 1)
        {
            this.grid = grid;
            this.units = units;
            this.vision = vision;
            this.armour = armour;
            LineHealth = lineHealth;
            rng = new Random(seed);
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
            var d = new Defender { Id = nextId++, Def = def, Hp = def.Hp };
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

        /// <summary>
        /// Queue attackers: <paramref name="count"/> units of <paramref name="unitId"/>, the first after
        /// <paramref name="delay"/> seconds from now, then one every <paramref name="interval"/> seconds.
        /// Aircraft are not simulated yet and are refused. Returns false if the unit cannot be queued.
        /// </summary>
        public bool QueueWave(string unitId, int count, float delay, float interval)
        {
            if (!units.TryGet(unitId, out var def) || def.IsDefence || def.Type == "aviation") return false;
            // Times count from now, so a second wave can be sent while the round runs (or after a won wave).
            for (int i = 0; i < count; i++) spawns.Add(new Spawn { Time = RoundTime + delay + i * interval, UnitId = unitId });
            // Sort only what has not spawned yet.
            spawns.Sort(nextSpawn, spawns.Count - nextSpawn, new SpawnOrder());
            waveQueued = true;
            if (Outcome == BattleOutcome.Won) Outcome = BattleOutcome.Ongoing;
            return true;
        }

        /// <summary>Advance the round by dt seconds. Does nothing in preparation or after the battle is decided.</summary>
        public void Step(float dt)
        {
            if (Phase != BattlePhase.Round || Outcome != BattleOutcome.Ongoing) return;
            RoundTime += dt;

            SpawnDue();
            MoveDefenders(dt);
            MoveAttackers(dt);

            visionTimer += dt;
            if (visionTimer >= VisionInterval) { RefreshVision(); visionTimer = 0f; }

            FireDefenders(dt);
            FireAttackers(dt);
            Cleanup();

            if (LineDamage >= LineHealth) Outcome = BattleOutcome.Lost;
            else if (waveQueued && Pending == 0 && attackers.Count == 0) Outcome = BattleOutcome.Won;
        }

        // ---- movement -------------------------------------------------------------------------------------------

        // Walk along the unit's path; true when it has reached the end. Slow ground makes a step longer.
        bool Walk(Unit u, float[] cost, float speed, float dt)
        {
            float left = speed * dt;
            while (left > 0f)
            {
                if (u.PathIndex >= u.Path.Count - 1) return true;
                int a = u.Path[u.PathIndex], b = u.Path[u.PathIndex + 1];
                float ax = grid.CenterX(a), ay = grid.CenterY(a), bx = grid.CenterX(b), by = grid.CenterY(b);
                float len = (float)Math.Sqrt((bx - ax) * (bx - ax) + (by - ay) * (by - ay));
                float effective = len * (cost[a] + cost[b]) / 2f;
                float rest = effective * (1f - u.SegmentDone);
                if (left >= rest)
                {
                    left -= rest;
                    u.PathIndex++;
                    u.SegmentDone = 0f;
                    u.X = bx; u.Y = by;
                }
                else
                {
                    u.SegmentDone += left / effective;
                    left = 0f;
                    u.X = ax + (bx - ax) * u.SegmentDone;
                    u.Y = ay + (by - ay) * u.SegmentDone;
                }
            }
            return u.PathIndex >= u.Path.Count - 1;
        }

        void MoveDefenders(float dt)
        {
            float[] cost = grid.Cost(UnitClass.Infantry);
            foreach (var d in defenders)
                if (d.State == DefenderState.Running && Walk(d, cost, RunSpeedMps, dt)) Arrive(d);
        }

        static void Arrive(Defender d)
        {
            d.X = d.TargetX;
            d.Y = d.TargetY;
            d.State = DefenderState.Standing;
            d.Path = null;
        }

        void MoveAttackers(float dt)
        {
            foreach (var a in attackers)
            {
                if (!a.Alive || a.Def.SpeedMps <= 0f) continue;
                if (Walk(a, grid.Cost(a.Class), a.Def.SpeedMps, dt))
                {
                    LineDamage += a.Def.LineDamage;
                    a.Alive = false;   // it has crossed the line and is gone
                }
            }
        }

        void SpawnDue()
        {
            while (nextSpawn < spawns.Count && spawns[nextSpawn].Time <= RoundTime)
            {
                var def = units.Get(spawns[nextSpawn++].UnitId);
                var cls = def.Type == "vehicle" ? UnitClass.Vehicle : UnitClass.Infantry;
                int k = (int)cls;
                if (flows[k] == null)
                {
                    flows[k] = new FlowField(grid, cls);
                    var list = new List<int>();
                    float[] cost = grid.Cost(cls);
                    for (int c = 0; c < grid.Cols; c++)
                        if (!float.IsPositiveInfinity(cost[c]) && flows[k].Reachable(c)) list.Add(c);
                    startCells[k] = list.ToArray();
                }
                if (startCells[k].Length == 0) continue;   // no way across this field for this class
                int start = startCells[k][rng.Next(startCells[k].Length)];
                var a = new Attacker { Id = nextId++, Def = def, Hp = def.Hp, Class = cls };
                a.Path = flows[k].Trace(start);
                a.X = grid.CenterX(start);
                a.Y = grid.CenterY(start);
                attackers.Add(a);
            }
        }

        // ---- sight ----------------------------------------------------------------------------------------------

        static float SightOf(UnitDef d) => d.SightRangeM > 0f ? d.SightRangeM : 30f;

        float Seen(float ox, float oy, float sight, float tx, float ty)
        {
            if (vision != null) return vision.SeeChance(ox, oy, tx, ty, sight);
            float dx = tx - ox, dy = ty - oy;
            return dx * dx + dy * dy <= sight * sight ? 1f : 0f;
        }

        // Shared vision: a unit is seen as well as its best observer on the other side sees it.
        void RefreshVision()
        {
            foreach (var a in attackers)
            {
                a.SeenChance = 0f;
                if (!a.Alive) continue;
                foreach (var d in defenders)
                {
                    if (!d.Alive) continue;
                    float c = Seen(d.X, d.Y, SightOf(d.Def), a.X, a.Y);
                    if (c > a.SeenChance) a.SeenChance = c;
                }
            }
            foreach (var d in defenders)
            {
                d.SeenChance = 0f;
                if (!d.Alive) continue;
                foreach (var a in attackers)
                {
                    if (!a.Alive) continue;
                    float c = Seen(a.X, a.Y, SightOf(a.Def), d.X, d.Y);
                    if (c > d.SeenChance) d.SeenChance = c;
                }
            }
        }

        // ---- firing ---------------------------------------------------------------------------------------------

        static bool HasWord(string list, string word)
        {
            if (string.IsNullOrEmpty(list) || string.IsNullOrEmpty(word)) return false;
            int from = 0;
            while (true)
            {
                int i = list.IndexOf(word, from, StringComparison.OrdinalIgnoreCase);
                if (i < 0) return false;
                bool startOk = i == 0 || list[i - 1] == ' ';
                bool endOk = i + word.Length == list.Length || list[i + word.Length] == ' ';
                if (startOk && endOk) return true;
                from = i + 1;
            }
        }

        /// <summary>Can a defender with this weapon class shoot at this attacker?</summary>
        public static bool CanTarget(UnitDef shooter, UnitDef target)
        {
            string w = shooter.WeaponClass;
            if (target.Type == "aviation") return HasWord(target.TargetableBy, w);
            if (w == "air_defence") return false;
            if (w == "anti_tank") return target.Type == "vehicle";
            return true;
        }

        static ShooterClass ClassOf(UnitDef d)
        {
            switch (d.Type)
            {
                case "vehicle": return ShooterClass.Vehicle;
                case "aviation": return ShooterClass.Aviation;
                case "indirect": return ShooterClass.Indirect;
                default: return ShooterClass.Troops;
            }
        }

        // Expected damage this step: no dice, so a run is repeatable and balance numbers are easy to read.
        float Damage(Unit shooter, Unit target, bool moving, float dt)
        {
            var s = shooter.Def;
            float dmg = s.Dps * s.Accuracy * target.SeenChance * dt;
            if (moving) dmg *= s.MoveAccuracyMult;
            if (armour != null) dmg *= armour.Factor(s.DamageType, target.Def.Armour);
            if (vision != null)
            {
                var cls = ClassOf(s);
                var cover = vision.CoverFor(shooter.X, shooter.Y, target.X, target.Y);
                if (cover != null) dmg *= cover.CoverAcc[(int)cls] * cover.CoverDmg[(int)cls];
                var under = vision.ObstacleAt(target.X, target.Y);
                if (under != null && cls != ShooterClass.Indirect) dmg *= under.OnCoverMult;
            }
            return dmg;
        }

        float RangeOf(Unit u)
        {
            float r = u.Def.RangeM;
            if (vision != null) { var o = vision.ObstacleAt(u.X, u.Y); if (o != null) r *= o.OnRangeMult; }
            return r;
        }

        void Hit(Unit target, float dmg)
        {
            target.Hp -= dmg;
            if (target.Hp <= 0f) target.Alive = false;
        }

        void FireDefenders(float dt)
        {
            foreach (var d in defenders)
            {
                if (!d.Alive || d.State != DefenderState.Standing || d.Def.Dps <= 0f) continue;
                float range = RangeOf(d), best = float.MaxValue;
                Attacker target = null;
                foreach (var a in attackers)
                {
                    if (!a.Alive || a.SeenChance <= 0f || !CanTarget(d.Def, a.Def)) continue;
                    float dx = a.X - d.X, dy = a.Y - d.Y, dist = dx * dx + dy * dy;
                    if (dist <= range * range && dist < best) { best = dist; target = a; }
                }
                if (target != null) Hit(target, Damage(d, target, false, dt));
            }
        }

        void FireAttackers(float dt)
        {
            foreach (var a in attackers)
            {
                if (!a.Alive || a.Def.Dps <= 0f || !a.Def.FireOnMove && a.Def.SpeedMps > 0f) continue;
                float range = RangeOf(a), best = float.MaxValue;
                Defender target = null;
                foreach (var d in defenders)
                {
                    if (!d.Alive || d.SeenChance <= 0f) continue;
                    float dx = a.X - d.X, dy = a.Y - d.Y, dist = dx * dx + dy * dy;
                    if (dist <= range * range && dist < best) { best = dist; target = d; }
                }
                if (target != null) Hit(target, Damage(a, target, a.Def.SpeedMps > 0f, dt));
            }
        }

        void Cleanup()
        {
            defenders.RemoveAll(e => !e.Alive);
            attackers.RemoveAll(e => !e.Alive);
        }
    }
}
