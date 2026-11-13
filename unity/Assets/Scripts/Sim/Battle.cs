using System;
using System.Collections.Generic;

namespace Treeline.Sim
{
    public enum BattlePhase { Preparation, Round }

    public enum DefenderState { Standing, Running }

    public enum BattleOutcome { Ongoing, Won, Lost }
    public enum DroneWeaponState { Ready, InFlight, Reloading }

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

        /// <summary>The cells of the route it is walking (read-only, for debug drawing).</summary>
        public IReadOnlyList<int> PathCells => Path;
        /// <summary>How far along <see cref="PathCells"/> it is.</summary>
        public int PathPosition => PathIndex;

        public int ShotsFired;         // renderer can detect new shots by a rising counter
        public float LastShotAt = -1f; // simulation time of most recent shot
        internal float ShotCooldown;  // seconds until the next shot
        internal Unit Target;         // keeps firing at it while it stays valid
        internal List<int> Path;      // cells to walk
        internal int PathIndex;       // the cell the unit has reached
        internal float SegmentDone;   // 0..1 along the current step
    }

    /// <summary>A defender on the field. While Running it is on its way in from the bottom edge and cannot fire.</summary>
    public sealed class Defender : Unit
    {
        public float TargetX, TargetY;
        public DefenderState State;
        /// <summary>Drone operators: seconds until the next drone may be launched.</summary>
        public float ReloadLeft;
        /// <summary>True while this operator's projectile drone is airborne (firing state).</summary>
        public bool DroneInFlight;
        public DroneWeaponState DroneState => DroneInFlight ? DroneWeaponState.InFlight :
            ReloadLeft > 0f ? DroneWeaponState.Reloading : DroneWeaponState.Ready;
        /// <summary>Optional point for the next FPV launch to search; no prior enemy spotting is required.</summary>
        public bool HasDroneWaypoint;
        public float DroneWaypointX, DroneWaypointY;
    }

    /// <summary>A drone in the air: flies from its operator to an attacker, then strikes (FPV) or drops grenades (bomber).</summary>
    public sealed class Drone
    {
        public int Id;
        public Defender Owner;
        public UnitDef Def;      // the operator's row: damage, speed, ammo
        public float X, Y;
        public Attacker Target;
        /// <summary>FPV mission: fly to this point and search using the drone's own sight radius.</summary>
        public bool UsesWaypoint;
        public float WaypointX, WaypointY;
        public int AmmoLeft;
        public bool Alive = true;
        internal float DropTimer;
    }

    /// <summary>How an attacker chose its route (rules/attackers.json, pathfinding).</summary>
    public enum RouteMode
    {
        /// <summary>No known threat: the quickest way.</summary>
        Fastest,
        /// <summary>A way around every known threat zone exists: take it, preferring short ones.</summary>
        Evade,
        /// <summary>Every way crosses a threat zone: least expected damage, then the shortest.</summary>
        Safest
    }

    /// <summary>An attacker walking or driving to the rear edge.</summary>
    public sealed class Attacker : Unit
    {
        public UnitClass Class;
        /// <summary>The wave it came with (1 = first).</summary>
        public int Wave;
        public RouteMode Mode;
        /// <summary>Troops still inside (carriers only).</summary>
        public int Passengers;
        /// <summary>A carrier that has stopped to let its troops out.</summary>
        public bool Dismounting;
        /// <summary>No way to the rear edge from where it stands.</summary>
        public bool Stuck;

        internal bool Crossed, Handled;
        internal float DismountTimer;
        internal float[] MoveCost;
    }

    /// <summary>Counts per unit id, for balance runs.</summary>
    public sealed class UnitStats
    {
        /// <summary>Attackers: entered the field (dismounted troops and troops dying inside included). Defenders: placed.</summary>
        public int Spawned;
        /// <summary>Attackers: destroyed (troops dying inside a carrier included). Defenders: lost.</summary>
        public int Killed;
        /// <summary>Attackers that reached the rear edge.</summary>
        public int Crossed;
        public float LineDamage, DamageDealt, DamageTaken;
        public int ShotsFired, ShotsHit;
    }

    /// <summary>
    /// The defence side of a battle: preparation phase, budget, and placing or moving defenders.
    /// In preparation everything is instant. During a round a placed or moved defender appears at the bottom edge
    /// and runs to its spot at three times the speed of a regular soldier, over the real terrain.
    /// </summary>
    public sealed class Battle
    {
        /// <summary>Defenders run in at <see cref="BattleRules.RunSpeedMultiplier"/> times the speed of this unit.</summary>
        public const string RunBaseUnitId = "regular";

        const float VisionInterval = 0.25f;

        struct Spawn { public float Time; public string UnitId; public int Wave; }

        sealed class SpawnOrder : IComparer<Spawn>
        {
            public int Compare(Spawn a, Spawn b) => a.Time.CompareTo(b.Time);
        }

        // Where a defender was seen or fired from, and in which wave.
        struct Sighting { public int DefenderId; public UnitDef Def; public float X, Y; public int Wave; }

        // A known defender as one attacker group remembers it.
        struct Threat { public UnitDef Def; public float X, Y, Weight; }

        // Routes for one unit type with one state of knowledge.
        sealed class Route
        {
            public RouteMode Mode;
            public FlowField Flow;       // the chosen mode
            public FlowField Fallback;   // used from a cell the chosen mode cannot reach
            public int[] Starts;         // entry cells worth taking
            public float[] MoveCost;     // real terrain cost for walking speed
        }

        readonly NavGrid grid;
        readonly UnitTable units;
        readonly VisionMap vision;
        readonly ArmourTable armour;
        readonly BattleRules rules;
        readonly Random rng;
        readonly List<Defender> defenders = new List<Defender>();
        readonly List<Attacker> attackers = new List<Attacker>();
        readonly List<Drone> drones = new List<Drone>();
        readonly List<Spawn> spawns = new List<Spawn>();
        readonly List<Sighting> sightings = new List<Sighting>();
        readonly Dictionary<string, Route> routes = new Dictionary<string, Route>();
        readonly Dictionary<string, UnitStats> stats = new Dictionary<string, UnitStats>();
        int routesVersion;
        int nextId = 1;
        int nextSpawn;
        bool waveQueued;
        float visionTimer = VisionInterval;

        public BattlePhase Phase { get; private set; } = BattlePhase.Preparation;
        public float Budget { get; private set; }
        public IReadOnlyList<Defender> Defenders => defenders;
        public IReadOnlyList<Attacker> Attackers => attackers;
        /// <summary>Drones in the air now.</summary>
        public IReadOnlyList<Drone> Drones => drones;
        public float RunSpeedMps { get; }
        public BattleOutcome Outcome { get; private set; } = BattleOutcome.Ongoing;
        public float RoundTime { get; private set; }
        /// <summary>Damage the attackers have done to our line by reaching the rear edge.</summary>
        public float LineDamage { get; private set; }
        public float LineHealth { get; }
        /// <summary>Attackers still waiting to enter the field.</summary>
        public int Pending => spawns.Count - nextSpawn;
        /// <summary>The current wave (0 before the first).</summary>
        public int WaveNumber { get; private set; }
        public IReadOnlyDictionary<string, UnitStats> Stats => stats;

        /// <param name="vision">Obstacles for sight and cover. Without it everything within sight radius is seen and nothing gives cover.</param>
        /// <param name="armour">Armour table. Without it armour does not reduce damage.</param>
        /// <param name="lineHealth">Overrides the rules' line health when given.</param>
        /// <param name="rules">Battle rules (battle_rules.csv). Without them the defaults are used.</param>
        public Battle(NavGrid grid, UnitTable units, float budget, VisionMap vision = null, ArmourTable armour = null,
            float lineHealth = -1f, int seed = 1, BattleRules rules = null)
        {
            this.grid = grid;
            this.units = units;
            this.vision = vision;
            this.armour = armour;
            this.rules = rules ?? new BattleRules();
            LineHealth = lineHealth > 0f ? lineHealth : this.rules.LineHealth;
            rng = new Random(seed);
            Budget = budget;
            routesVersion = grid.Version;
            RunSpeedMps = units.TryGet(RunBaseUnitId, out var baseUnit) && baseUnit.SpeedMps > 0f
                ? baseUnit.SpeedMps * this.rules.RunSpeedMultiplier
                : 18f;
        }

        UnitStats StatsFor(UnitDef d)
        {
            UnitStats s;
            if (!stats.TryGetValue(d.Id, out s)) { s = new UnitStats(); stats[d.Id] = s; }
            return s;
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
            StatsFor(def).Spawned++;
            Send(d, flow, x, y);
            placed = d;
            return true;
        }

        /// <summary>Order an FPV launcher to search at this field point on future launches.
        /// No external detection is needed. RangeM bounds launch waypoints, DroneSightRangeM governs onboard search.</summary>
        public bool TrySetDroneWaypoint(int defenderId, float x, float y, out string why)
        {
            var op = defenders.Find(d => d.Id == defenderId);
            if (op == null || op.Def.Behaviour != "fpv_launcher") { why = "Not an FPV operator"; return false; }
            if (x < 0f || x > grid.Width || y < 0f || y > grid.Height)
            { why = "Waypoint outside field"; return false; }
            float dx = x - op.X, dy = y - op.Y;
            if (dx * dx + dy * dy > op.Def.RangeM * op.Def.RangeM)
            { why = "Waypoint beyond FPV flight range"; return false; }
            op.HasDroneWaypoint = true;
            op.DroneWaypointX = x;
            op.DroneWaypointY = y;
            why = "";
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
        /// Start a new wave: attackers queued from now on belong to it. Attackers plan their routes from what was
        /// learnt about the defence in earlier waves (each unit type with its own lag, <c>adaptation_lag_waves</c>).
        /// </summary>
        public int BeginWave()
        {
            WaveNumber++;
            return WaveNumber;
        }

        /// <summary>
        /// Queue attackers into the current wave: <paramref name="count"/> units of <paramref name="unitId"/>, the first
        /// after <paramref name="delay"/> seconds from now, then one every <paramref name="interval"/> seconds.
        /// Starts wave 1 if no wave was begun. Aircraft are not simulated yet and are refused.
        /// </summary>
        public bool QueueWave(string unitId, int count, float delay, float interval)
        {
            if (!units.TryGet(unitId, out var def) || def.IsDefence || def.Type == "aviation") return false;
            if (WaveNumber == 0) BeginWave();
            // Times count from now, so a second wave can be sent while the round runs (or after a won wave).
            for (int i = 0; i < count; i++)
                spawns.Add(new Spawn { Time = RoundTime + delay + i * interval, UnitId = unitId, Wave = WaveNumber });
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
            LaunchDrones(dt);
            FlyDrones(dt);
            FireAttackers(dt);
            Cleanup();
            if (grid.Version != routesVersion) Reroute();

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
                float ca = float.IsPositiveInfinity(cost[a]) ? 1f : cost[a], cb = float.IsPositiveInfinity(cost[b]) ? 1f : cost[b];
                float effective = len * (ca + cb) / 2f;
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
            // By index: troops getting out of a carrier are added to the list while it is walked.
            for (int n = 0; n < attackers.Count; n++)
            {
                var a = attackers[n];
                if (!a.Alive) continue;

                if (a.Passengers > 0 && !a.Dismounting && a.Def.DismountAtHp > 0f && a.Hp <= a.Def.DismountAtHp * a.Def.Hp)
                {
                    a.Dismounting = true;
                    a.DismountTimer = 0f;   // the first one jumps out at once
                }
                if (a.Dismounting)
                {
                    a.DismountTimer -= dt;
                    while (a.DismountTimer <= 0f && a.Passengers > 0)
                    {
                        Dismount(a);
                        a.DismountTimer += Math.Max(0.05f, a.Def.DismountIntervalS);
                    }
                    if (a.Passengers == 0) a.Dismounting = false;   // empty: drives on
                    continue;
                }

                if (a.Stuck || a.Def.SpeedMps <= 0f) continue;
                if (Walk(a, a.MoveCost, a.Def.SpeedMps, dt))
                {
                    float dmg = a.Def.LineDamage;
                    if (a.Passengers > 0 && units.TryGet(a.Def.CarriesUnit, out var p)) dmg += a.Passengers * p.LineDamage;
                    LineDamage += dmg;
                    var st = StatsFor(a.Def);
                    st.Crossed++;
                    st.LineDamage += dmg;
                    a.Crossed = true;
                    a.Alive = false;   // it has crossed the line and is gone
                }
            }
        }

        void Dismount(Attacker carrier)
        {
            carrier.Passengers--;
            if (!units.TryGet(carrier.Def.CarriesUnit, out var def)) return;
            var t = new Attacker
            {
                Id = nextId++, Def = def, Hp = def.Hp, Class = ClassOfUnit(def), Wave = carrier.Wave,
                X = carrier.X, Y = carrier.Y,
            };
            attackers.Add(t);
            StatsFor(def).Spawned++;
            PathFromHere(t);
        }

        static UnitClass ClassOfUnit(UnitDef d) => d.Type == "vehicle" ? UnitClass.Vehicle : UnitClass.Infantry;

        void SpawnDue()
        {
            while (nextSpawn < spawns.Count && spawns[nextSpawn].Time <= RoundTime)
            {
                var sp = spawns[nextSpawn++];
                var def = units.Get(sp.UnitId);
                var route = RouteFor(def, sp.Wave);
                if (route.Starts.Length == 0) continue;   // no way across this field for this unit
                int start = route.Starts[rng.Next(route.Starts.Length)];
                var a = new Attacker
                {
                    Id = nextId++, Def = def, Hp = def.Hp, Class = ClassOfUnit(def), Wave = sp.Wave,
                    Mode = route.Mode, MoveCost = route.MoveCost,
                    Passengers = def.CarriesCount > 0 && units.TryGet(def.CarriesUnit, out _) ? def.CarriesCount : 0,
                };
                a.Path = route.Flow.Trace(start);
                a.X = grid.CenterX(start);
                a.Y = grid.CenterY(start);
                attackers.Add(a);
                StatsFor(def).Spawned++;
            }
        }

        // A new path from where the unit stands (after dismounting, or when a wreck changed the ground).
        void PathFromHere(Attacker a)
        {
            var route = RouteFor(a.Def, a.Wave);
            a.MoveCost = route.MoveCost;
            a.Mode = route.Mode;
            int cell = grid.CellAt(a.X, a.Y);
            FlowField flow = route.Flow;
            if (!flow.Reachable(cell)) { flow = route.Fallback; a.Mode = RouteMode.Safest; }
            if (!flow.Reachable(cell)) cell = NearestReachable(flow, cell);
            a.Stuck = cell < 0;
            a.Path = a.Stuck ? null : flow.Trace(cell);
            a.PathIndex = 0;
            a.SegmentDone = 0f;
        }

        int NearestReachable(FlowField flow, int cell)
        {
            int c0 = cell % grid.Cols, r0 = cell / grid.Cols;
            for (int ring = 1; ring <= 4; ring++)
                for (int r = r0 - ring; r <= r0 + ring; r++)
                    for (int c = c0 - ring; c <= c0 + ring; c++)
                    {
                        if (r < 0 || c < 0 || r >= grid.Rows || c >= grid.Cols) continue;
                        if (Math.Abs(r - r0) != ring && Math.Abs(c - c0) != ring) continue;
                        int i = r * grid.Cols + c;
                        if (flow.Reachable(i)) return i;
                    }
            return -1;
        }

        void Reroute()
        {
            routes.Clear();
            routesVersion = grid.Version;
            foreach (var a in attackers)
                if (a.Alive && !a.Dismounting) PathFromHere(a);
        }

        // ---- routes and threat memory ---------------------------------------------------------------------------

        void Record(Defender d)
        {
            for (int i = sightings.Count - 1; i >= 0; i--)
            {
                var s = sightings[i];
                if (s.Wave != WaveNumber) break;   // records are in wave order
                if (s.DefenderId != d.Id) continue;
                s.X = d.X; s.Y = d.Y;
                sightings[i] = s;
                return;
            }
            sightings.Add(new Sighting { DefenderId = d.Id, Def = d.Def, X = d.X, Y = d.Y, Wave = WaveNumber });
        }

        // What attackers of a wave with this lag remember: the latest sighting of each defender up to wave
        // (wave - lag), fading with every wave it was not seen again.
        List<Threat> Known(int wave, int lag)
        {
            var list = new List<Threat>();
            int asOf = wave - Math.Max(1, lag);
            if (asOf < 1) return list;
            var latest = new Dictionary<int, Sighting>();
            foreach (var s in sightings)
                if (s.Wave <= asOf) latest[s.DefenderId] = s;
            foreach (var s in latest.Values)
            {
                float w = (float)Math.Pow(1f - rules.MemoryDecayPerWave, asOf - s.Wave);
                if (w >= rules.MemoryForgetBelow) list.Add(new Threat { Def = s.Def, X = s.X, Y = s.Y, Weight = w });
            }
            return list;
        }

        float ArmourFactor(string damageType, string armourLevel) => armour != null ? armour.Factor(damageType, armourLevel) : 1f;

        Route RouteFor(UnitDef def, int wave)
        {
            int lag = Math.Max(1, def.AdaptationLagWaves);
            int asOf = Math.Max(0, wave - lag);
            string key = def.Id + "|" + asOf;
            if (routes.TryGetValue(key, out var have)) return have;

            var cls = ClassOfUnit(def);
            float[] move = grid.CostFor(cls, def.TreeMoveCost);
            var route = new Route { MoveCost = move };
            var baseFlow = new FlowField(grid, move);

            // Known defenders that can hurt this unit, and how hard (per second, before cover and sight).
            var threats = Known(wave, lag);
            var hurt = new List<Threat>();
            var rate = new List<float>();
            foreach (var t in threats)
            {
                if (t.Def.Damage <= 0f || t.Def.AttackSpeed <= 0f || !CanTarget(t.Def, def)) continue;
                float f = ArmourFactor(t.Def.DamageType, def.Armour);
                if (f <= 0f) continue;
                hurt.Add(t);
                rate.Add(t.Def.Damage * t.Def.AttackSpeed * t.Def.Accuracy * f * t.Weight);
            }

            if (hurt.Count == 0)
            {
                route.Mode = RouteMode.Fastest;
                route.Flow = route.Fallback = baseFlow;
                route.Starts = AllStarts(baseFlow, move);
                routes[key] = route;
                return route;
            }

            // Expected damage per second in every cell. A defender hurts only where it can hit: inside its range
            // and where it sees (sight radius, obstacles, belts). Indirect fire hits wherever any known defender sees.
            int n = grid.Count;
            var danger = new float[n];
            var seenByAny = new float[n];
            var see = new float[hurt.Count][];
            foreach (var t in threats) SeeFrom(t, seenByAny, null);
            for (int k = 0; k < hurt.Count; k++) { see[k] = new float[n]; SeeFrom(hurt[k], null, see[k]); }
            for (int i = 0; i < n; i++)
            {
                float x = grid.CenterX(i), y = grid.CenterY(i);
                for (int k = 0; k < hurt.Count; k++)
                {
                    float range = RangeAt(hurt[k].Def, hurt[k].X, hurt[k].Y);
                    float dx = x - hurt[k].X, dy = y - hurt[k].Y;
                    if (dx * dx + dy * dy > range * range) continue;
                    float seen = hurt[k].Def.Behaviour == "indirect_fire" ? seenByAny[i] : see[k][i];
                    danger[i] += rate[k] * seen;
                }
            }
            float speed = Math.Max(0.1f, def.SpeedMps);

            // Safest: one point of expected damage weighs as much as DangerWeight metres; speed does not matter.
            var safeCost = new float[n];
            for (int i = 0; i < n; i++)
                safeCost[i] = float.IsPositiveInfinity(move[i]) ? float.PositiveInfinity
                    : 1f + rules.DangerWeight * danger[i] * move[i] / speed;
            var safest = new FlowField(grid, safeCost);

            // Evade: travel time plus a little per metre, threat zones closed.
            var evadeCost = new float[n];
            for (int i = 0; i < n; i++)
                evadeCost[i] = float.IsPositiveInfinity(move[i]) || danger[i] > 0f ? float.PositiveInfinity
                    : move[i] / speed + rules.EvadeLengthWeight;
            var evade = new FlowField(grid, evadeCost);

            var evadeStarts = BestStarts(evade, move);
            if (evadeStarts.Length > 0)
            {
                route.Mode = RouteMode.Evade;
                route.Flow = evade;
                route.Starts = evadeStarts;
            }
            else
            {
                route.Mode = RouteMode.Safest;
                route.Flow = safest;
                route.Starts = BestStarts(safest, move);
            }
            route.Fallback = safest;
            routes[key] = route;
            return route;
        }

        // How well a known defender sees each cell: max into 'max', or written into 'into'.
        void SeeFrom(Threat t, float[] max, float[] into)
        {
            float sight = SightOf(t.Def);
            float radius = vision != null ? vision.SightRadius(t.X, t.Y, sight) : sight;
            int c0 = Math.Max(0, (int)((t.X - radius) / NavGrid.CellSize)), c1 = Math.Min(grid.Cols - 1, (int)((t.X + radius) / NavGrid.CellSize));
            int r0 = Math.Max(0, (int)((t.Y - radius) / NavGrid.CellSize)), r1 = Math.Min(grid.Rows - 1, (int)((t.Y + radius) / NavGrid.CellSize));
            for (int r = r0; r <= r1; r++)
                for (int c = c0; c <= c1; c++)
                {
                    int i = r * grid.Cols + c;
                    float v = Seen(t.X, t.Y, sight, grid.CenterX(i), grid.CenterY(i));
                    if (into != null) into[i] = v;
                    if (max != null && v > max[i]) max[i] = v;
                }
        }

        float RangeAt(UnitDef d, float x, float y)
        {
            float r = d.RangeM;
            if (vision != null) { var o = vision.ObstacleAt(x, y); if (o != null) r *= o.OnRangeMult; }
            return r;
        }

        // Entry cells: the field's entry points, or the whole top edge.
        List<int> EntryCandidates(FlowField flow, float[] move)
        {
            var list = new List<int>();
            if (grid.SpawnXs.Length > 0)
            {
                foreach (float x in grid.SpawnXs)
                {
                    int c = grid.CellAt(x, 0f);
                    if (!float.IsPositiveInfinity(move[c]) && flow.Reachable(c)) list.Add(c);
                }
            }
            else
            {
                for (int c = 0; c < grid.Cols; c++)
                    if (!float.IsPositiveInfinity(move[c]) && flow.Reachable(c)) list.Add(c);
            }
            return list;
        }

        int[] AllStarts(FlowField flow, float[] move) => EntryCandidates(flow, move).ToArray();

        // With threats known, attackers enter where the route is best, give or take RouteChoiceSlack.
        int[] BestStarts(FlowField flow, float[] move)
        {
            var all = EntryCandidates(flow, move);
            if (all.Count == 0) return all.ToArray();
            double best = double.PositiveInfinity;
            foreach (int c in all) if (flow.Dist[c] < best) best = flow.Dist[c];
            var good = new List<int>();
            foreach (int c in all) if (flow.Dist[c] <= best * (1.0 + rules.RouteChoiceSlack) + 1e-9) good.Add(c);
            return good.ToArray();
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
                // Airborne drones contribute their own (short) onboard sensor range to shared vision.
                foreach (var dr in drones)
                {
                    if (!dr.Alive || dr.Def.DroneSightRangeM <= 0f) continue;
                    float c = Seen(dr.X, dr.Y, dr.Def.DroneSightRangeM, a.X, a.Y);
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
                if (d.SeenChance > 0f) Record(d);   // the attackers will remember it
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

        // One discrete shot. Accuracy and detection determine a seeded hit roll;
        // armour and protection scale damage after a hit. No damage per frame.
        // The projectile/animation can track Unit.ShotsFired and Unit.LastShotAt.
        float ShotDamage(Unit shooter, Unit target, float aimSeen, bool moving)
        {
            var weapon = shooter.Def;
            float hitChance = weapon.Accuracy * aimSeen;
            if (moving) hitChance *= weapon.MoveAccuracyMult;
            var cls = ClassOf(weapon);
            var cover = vision != null ? vision.CoverFor(shooter.X, shooter.Y, target.X, target.Y) : null;
            if (cover != null) hitChance *= cover.CoverAcc[(int)cls];
            hitChance = Math.Max(0f, Math.Min(1f, hitChance));
            if (rng.NextDouble() >= hitChance) return 0f;
            StatsFor(shooter.Def).ShotsHit++;
            float damage = weapon.Damage * ArmourFactor(weapon.DamageType, target.Def.Armour);
            if (cover != null) damage *= cover.CoverDmg[(int)cls];
            if (vision != null)
            {
                var under = vision.ObstacleAt(target.X, target.Y);
                if (under != null && cls != ShooterClass.Indirect) damage *= under.OnCoverMult;
            }
            return damage;
        }

        bool ReadyToShoot(Unit shooter, float dt)
        {
            shooter.ShotCooldown = Math.Max(0f, shooter.ShotCooldown - dt);
            return shooter.ShotCooldown <= 0f && shooter.Def.AttackSpeed > 0f && shooter.Def.Damage > 0f;
        }

        void BeginShot(Unit shooter)
        {
            shooter.ShotCooldown = 1f / shooter.Def.AttackSpeed;
            shooter.ShotsFired++;
            shooter.LastShotAt = RoundTime;
            StatsFor(shooter.Def).ShotsFired++;
        }

        float RangeOf(Unit u) => RangeAt(u.Def, u.X, u.Y);

        // Direct fire with the own-sight rule needs the shooter's own view; indirect fire always uses shared vision.
        bool OwnSightOnly(UnitDef d) => rules.DirectFireNeedsOwnSight && d.Behaviour != "indirect_fire";

        float AimAt(Unit shooter, Unit target, bool ownSight) =>
            ownSight ? Seen(shooter.X, shooter.Y, SightOf(shooter.Def), target.X, target.Y) : target.SeenChance;

        static bool InRange(Unit a, Unit b, float range)
        {
            float dx = a.X - b.X, dy = a.Y - b.Y;
            return dx * dx + dy * dy <= range * range;
        }

        void Hit(Unit shooter, Unit target, float dmg)
        {
            if (dmg <= 0f || !target.Alive) return;
            float done = Math.Min(dmg, target.Hp);
            StatsFor(shooter.Def).DamageDealt += done;
            StatsFor(target.Def).DamageTaken += done;
            target.Hp -= dmg;
            if (target.Hp <= 0f) target.Alive = false;
        }

        // Radial linear falloff: at impact centre 100% damage, at radius 0%.
        // Armour applies per victim. Do not roll accuracy again for each unit caught in the blast.
        public static float BlastFalloff(float distance, float radius)
        {
            if (radius <= 0f) return distance <= 0f ? 1f : 0f;
            return Math.Max(0f, Math.Min(1f, 1f - Math.Max(0f, distance) / radius));
        }

        void Explode(Unit shooter, float cx, float cy, float baseDamage,
            float radius, bool fromAbove)
        {
            var weapon = shooter.Def;
            foreach (var a in attackers)
            {
                if (!a.Alive) continue;
                float dx = a.X - cx, dy = a.Y - cy;
                float dist = (float)Math.Sqrt(dx * dx + dy * dy);
                float scale = BlastFalloff(dist, radius);
                if (scale <= 0f) continue;
                float dmg = baseDamage * ArmourFactor(weapon.DamageType, a.Def.Armour) * scale;
                if (!fromAbove && vision != null)
                {
                    var cover = vision.CoverFor(shooter.X, shooter.Y, a.X, a.Y);
                    var cls = ClassOf(weapon);
                    if (cover != null) dmg *= cover.CoverDmg[(int)cls];
                    var obstacle = vision.ObstacleAt(a.X, a.Y);
                    if (obstacle != null && cls != ShooterClass.Indirect) dmg *= obstacle.OnCoverMult;
                }
                Hit(shooter, a, dmg);
            }
        }

        void FireDefenders(float dt)
        {
            foreach (var d in defenders)
            {
                if (!d.Alive || d.State != DefenderState.Standing || !ReadyToShoot(d, dt)) continue;
                float range = RangeOf(d);
                // Retain a live visible target, otherwise pick nearest. Indirect fire uses shared sight.
                bool own = OwnSightOnly(d.Def);
                var target = d.Target as Attacker;
                if (target == null || !target.Alive || AimAt(d, target, own) <= 0f || !InRange(d, target, range))
                {
                    target = null;
                    float best = float.MaxValue;
                    foreach (var a in attackers)
                    {
                        if (!a.Alive || a.SeenChance <= 0f || !CanTarget(d.Def, a.Def)) continue;
                        float dx = a.X - d.X, dy = a.Y - d.Y, dist = dx * dx + dy * dy;
                        if (dist > range * range || dist >= best) continue;
                        if (own && AimAt(d, a, true) <= 0f) continue;
                        best = dist; target = a;
                    }
                    d.Target = target;
                }
                if (target == null) continue;
                Record(d);
                BeginShot(d);
                float aim = AimAt(d, target, own);
                if (d.Def.BlastRadiusM <= 0f)
                {
                    Hit(d, target, ShotDamage(d, target, aim, false));
                    continue;
                }
                // One successful hit creates ONE blast. A miss does not damage other targets.
                float centerX = target.X, centerY = target.Y;
                if (ShotDamage(d, target, aim, false) <= 0f) continue;
                Explode(d, centerX, centerY, d.Def.Damage, d.Def.BlastRadiusM, false);
            }
        }

        void FireAttackers(float dt)
        {
            foreach (var a in attackers)
            {
                if (!a.Alive || !ReadyToShoot(a, dt)) continue;
                bool moving = !a.Dismounting && !a.Stuck && a.Def.SpeedMps > 0f;
                if (moving && !a.Def.FireOnMove) continue;
                float range = RangeOf(a);
                bool own = OwnSightOnly(a.Def);
                var target = a.Target as Defender;
                if (target == null || !target.Alive || AimAt(a, target, own) <= 0f || !InRange(a, target, range))
                {
                    target = null;
                    float best = float.MaxValue;
                    foreach (var d in defenders)
                    {
                        if (!d.Alive || d.SeenChance <= 0f || d.Def.Type == "aviation") continue;   // nothing of theirs shoots up yet
                        float dx = a.X - d.X, dy = a.Y - d.Y, dist = dx * dx + dy * dy;
                        if (dist > range * range || dist >= best) continue;
                        if (own && AimAt(a, d, true) <= 0f) continue;
                        best = dist; target = d;
                    }
                    a.Target = target;
                }
                if (target != null)
                {
                    BeginShot(a);
                    Hit(a, target, ShotDamage(a, target, AimAt(a, target, own), moving));
                }
            }
        }

        // ---- drones ---------------------------------------------------------------------------------------------

        static string ClassOfTarget(UnitDef d)
        {
            if (!string.IsNullOrEmpty(d.TargetClass)) return d.TargetClass;
            return d.Type == "aviation" ? "aviation" : d.Type == "vehicle" ? "heavy" : "infantry";
        }

        // Position of a class in the operator's list; -1 when the operator never goes for it.
        static int PriorityOf(UnitDef op, UnitDef target)
        {
            string cls = ClassOfTarget(target);
            if (string.IsNullOrEmpty(op.TargetOrder)) return 0;
            int rank = 0, from = 0;
            while (from < op.TargetOrder.Length)
            {
                int space = op.TargetOrder.IndexOf(' ', from);
                if (space < 0) space = op.TargetOrder.Length;
                if (string.Equals(op.TargetOrder.Substring(from, space - from), cls, StringComparison.OrdinalIgnoreCase)) return rank;
                rank++;
                from = space + 1;
            }
            return -1;
        }

        // The best target for this operator right now: seen by anyone on our side, inside the operator's range, of a
        // class it goes for. Highest priority first; among equals the one furthest along (closest to the rear edge).
        Attacker PickDroneTarget(Defender op)
        {
            float range = RangeOf(op);
            bool single = op.Def.DroneAmmo == 1;
            Attacker best = null;
            int bestRank = int.MaxValue;
            foreach (var a in attackers)
            {
                if (!a.Alive || a.SeenChance <= 0f || !CanTarget(op.Def, a.Def) || !InRange(op, a, range)) continue;
                int rank = PriorityOf(op.Def, a.Def);
                if (rank < 0) continue;
                if (single && TargetedByDrone(a)) continue;   // one single-use drone per target at a time
                if (rank < bestRank || (rank == bestRank && (a.Y > best.Y || (a.Y == best.Y && a.Id < best.Id))))
                {
                    best = a; bestRank = rank;
                }
            }
            return best;
        }

        bool TargetedByDrone(Attacker a)
        {
            foreach (var dr in drones) if (dr.Alive && dr.Target == a && dr.Def.DroneAmmo == 1) return true;
            return false;
        }

        bool ActiveDroneFor(Defender op)
        {
            foreach (var dr in drones) if (dr.Alive && dr.Owner == op) return true;
            return false;
        }

        Attacker PickNearbyFpvTarget(Drone dr)
        {
            if (dr.Def.DroneSightRangeM <= 0f) return null;
            Attacker best = null;
            int rank = int.MaxValue;
            foreach (var a in attackers)
            {
                if (!a.Alive || !CanTarget(dr.Def, a.Def) ||
                    Seen(dr.X, dr.Y, dr.Def.DroneSightRangeM, a.X, a.Y) <= 0f ||
                    TargetedByDrone(a)) continue;
                int candidate = PriorityOf(dr.Def, a.Def);
                if (candidate < 0) continue;
                if (candidate < rank || (candidate == rank && (best == null || a.Y > best.Y ||
                    (a.Y == best.Y && a.Id < best.Id))))
                {
                    best = a; rank = candidate;
                }
            }
            return best;
        }

        static void MoveDroneTowards(Drone dr, float x, float y, float dt)
        {
            float dx = x - dr.X, dy = y - dr.Y;
            float dist = (float)Math.Sqrt(dx * dx + dy * dy);
            float step = Math.Max(0f, dr.Def.DroneSpeedMps) * dt;
            if (dist <= step) { dr.X = x; dr.Y = y; }
            else if (dist > 0f) { dr.X += dx / dist * step; dr.Y += dy / dist * step; }
        }

        // Launch uses attack_speed in shots/second. A drone in flight IS the active shot.
        // No cooldown starts until it detonates or is destroyed.
        void LaunchDrones(float dt)
        {
            foreach (var op in defenders)
            {
                if (!op.Alive || op.State != DefenderState.Standing || op.Def.DroneAmmo <= 0 ||
                    op.Def.AttackSpeed <= 0f) continue;
                if (op.ReloadLeft > 0f)
                {
                    op.ReloadLeft = Math.Max(0f, op.ReloadLeft - dt);
                    if (op.ReloadLeft > 0f) continue;
                }
                if (op.DroneInFlight || ActiveDroneFor(op)) continue;
                bool waypoint = op.Def.Behaviour == "fpv_launcher" && op.HasDroneWaypoint;
                var target = waypoint ? null : PickDroneTarget(op);
                if (!waypoint && target == null) continue;
                drones.Add(new Drone
                {
                    Id = nextId++, Owner = op, Def = op.Def, X = op.X, Y = op.Y,
                    Target = target, AmmoLeft = op.Def.DroneAmmo,
                    UsesWaypoint = waypoint, WaypointX = op.DroneWaypointX, WaypointY = op.DroneWaypointY
                });
                op.DroneInFlight = true;
                op.ShotsFired++;
                op.LastShotAt = RoundTime;
                StatsFor(op.Def).ShotsFired++;
            }
        }

        // Single lifecycle hook for either a successful explosion or the drone being shot down.
        void EndDroneFlight(Drone dr)
        {
            if (!dr.Alive) return;
            dr.Alive = false;
            dr.Owner.DroneInFlight = false;
            dr.Owner.ReloadLeft = dr.Def.AttackSpeed > 0f ? 1f / dr.Def.AttackSpeed : 0f;
        }

        /// <summary>Destroy an airborne drone (e.g., when it is shot down).
        /// Hostile AA interception is not yet simulated; this API is its future integration hook.</summary>
        public bool TryDestroyDrone(int droneId)
        {
            foreach (var dr in drones)
            {
                if (!dr.Alive || dr.Id != droneId) continue;
                EndDroneFlight(dr);
                return true;
            }
            return false;
        }

        void FlyDrones(float dt)
        {
            for (int i = 0; i < drones.Count; i++)
            {
                var dr = drones[i];
                if (!dr.Alive) continue;
                if (rules.DroneLostWithOperator && !dr.Owner.Alive) { EndDroneFlight(dr); continue; }

                if (dr.UsesWaypoint)
                {
                    if (dr.Target == null || !dr.Target.Alive)
                        dr.Target = PickNearbyFpvTarget(dr);
                    if (dr.Target == null)
                    {
                        MoveDroneTowards(dr, dr.WaypointX, dr.WaypointY, dt);
                        dr.Target = PickNearbyFpvTarget(dr);
                        if (dr.Target == null) continue;
                    }
                }
                else if (dr.Target == null || !dr.Target.Alive)
                {
                    dr.Target = PickDroneTarget(dr.Owner);
                    if (dr.Target == null) { EndDroneFlight(dr); continue; }
                }

                MoveDroneTowards(dr, dr.Target.X, dr.Target.Y, dt);
                if (!InRangeDrone(dr, dr.Target)) continue;

                if (dr.Def.DropIntervalS <= 0f)
                {
                    // FPV: detonate once. Core damage is Damage * Accuracy; armour/falloff per victim.
                    Explode(dr.Owner, dr.Target.X, dr.Target.Y,
                        dr.Def.Damage * dr.Def.Accuracy, dr.Def.BlastRadiusM, true);
                    EndDroneFlight(dr); // starts 3-second reload AFTER the blast
                    continue;
                }

                // Bomber: six successive grenade drops before the drone returns / is consumed.
                dr.DropTimer -= dt;
                if (dr.DropTimer > 0f) continue;
                dr.DropTimer = dr.Def.DropIntervalS;
                Explode(dr.Owner, dr.Target.X, dr.Target.Y,
                    dr.Def.Damage * dr.Def.Accuracy, dr.Def.BlastRadiusM, true);
                if (--dr.AmmoLeft <= 0) EndDroneFlight(dr);
            }
            drones.RemoveAll(d => !d.Alive);
        }

        static bool InRangeDrone(Drone dr, Attacker target)
        {
            float dx = target.X - dr.X, dy = target.Y - dr.Y;
            return dx * dx + dy * dy < 0.0001f;
        }

        // ---- deaths ---------------------------------------------------------------------------------------------

        void Cleanup()
        {
            // Repeat: a carrier's blast can kill troops that were already checked.
            bool again = true;
            while (again)
            {
                again = false;
                foreach (var a in attackers)
                {
                    if (a.Alive || a.Crossed || a.Handled) continue;
                    a.Handled = true;
                    Destroyed(a);
                    again = true;
                }
            }
            foreach (var d in defenders) if (!d.Alive) StatsFor(d.Def).Killed++;
            defenders.RemoveAll(e => !e.Alive);
            attackers.RemoveAll(e => !e.Alive);
        }

        void Destroyed(Attacker a)
        {
            StatsFor(a.Def).Killed++;
            bool loaded = a.Passengers > 0;
            bool dismounting = a.Dismounting;

            // Troops still inside die with the vehicle.
            if (loaded && units.TryGet(a.Def.CarriesUnit, out var p))
            {
                var ps = StatsFor(p);
                ps.Spawned += a.Passengers;
                ps.Killed += a.Passengers;
            }
            a.Passengers = 0;

            // A carrier blowing up with troops inside or getting out kills attacking troops next to it.
            if (rules.DismountBlastKills && a.Def.BlastRadiusM > 0f && (loaded || dismounting))
            {
                float r2 = a.Def.BlastRadiusM * a.Def.BlastRadiusM;
                foreach (var o in attackers)
                {
                    if (!o.Alive || o.Class != UnitClass.Infantry) continue;
                    float dx = o.X - a.X, dy = o.Y - a.Y;
                    if (dx * dx + dy * dy <= r2) { o.Hp = 0f; o.Alive = false; }
                }
            }

            // A destroyed vehicle stays as a wreck: it hides, gives cover and blocks vehicles.
            if (a.Class == UnitClass.Vehicle && a.Def.WreckRadiusM > 0f && vision != null &&
                vision.Table.TryGet("wreck", out var wreck))
            {
                vision.PlaceObject(wreck, a.X, a.Y, a.Def.WreckRadiusM);
                grid.PlaceObject(wreck, a.X, a.Y, a.Def.WreckRadiusM);
            }
        }
    }
}
