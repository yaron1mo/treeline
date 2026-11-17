using System;
using System.Collections.Generic;

namespace Treeline.Sim
{
    /// <summary>
    /// A whole run of waves on one field, played in fixed steps, with every player action recorded so the run can be
    /// rewound to the start of an earlier wave (Yaroslav, 2026-11-17: "a button to replay earlier rounds").
    ///
    /// The battle is deterministic for a given seed, step length and order of actions, so rewinding builds a fresh
    /// battle and plays the recorded actions again, fast, up to the moment the wave before the chosen one was held.
    /// Between waves the run pays the kill rewards and the wave income and goes back to preparation.
    /// </summary>
    public sealed class Run
    {
        /// <summary>Length of one simulation step, seconds.</summary>
        public const float Dt = 1f / 30f;

        public enum ActKind { Place, Move, Sell, Waypoint, StartWave }

        public struct Act
        {
            public long Step;
            public ActKind Kind;
            public string UnitId;
            public int DefenderId;
            public float X, Y;
        }

        readonly Func<Battle> make;
        readonly WavePlan plan;
        readonly BattleRules rules;
        readonly List<Act> log = new List<Act>();
        long step;

        public Battle Battle { get; private set; }
        /// <summary>Waves held so far.</summary>
        public int WavesHeld { get; private set; }
        /// <summary>The run has ended: the line broke or the last wave was held.</summary>
        public bool Over { get; private set; }
        public bool Won { get; private set; }
        /// <summary>Kill rewards paid when the last wave was held.</summary>
        public float LastReward { get; private set; }
        /// <summary>True while a rewind is playing the record back (the game should not log or show messages).</summary>
        public bool Replaying { get; private set; }
        public IReadOnlyList<Act> Log => log;

        /// <summary>Called when a wave ends: the wave number and "held" or "lost".</summary>
        public event Action<int, string> WaveEnded;

        /// <param name="make">Builds a fresh battle for this run: same field, data and seed every time.</param>
        public Run(Func<Battle> make, WavePlan plan, BattleRules rules)
        {
            this.make = make;
            this.plan = plan;
            this.rules = rules ?? new BattleRules();
            Battle = make();
        }

        public int WaveCount => plan.WaveCount;

        // ---- player actions (recorded) ---------------------------------------------------------------------------

        public bool Place(string unitId, float x, float y, out Defender placed, out string why)
        {
            if (Over) { placed = null; why = "The run is over"; return false; }
            bool ok = Battle.TryPlace(unitId, x, y, out placed, out why);
            if (ok) Record(new Act { Kind = ActKind.Place, UnitId = unitId, X = x, Y = y });
            return ok;
        }

        public bool Move(int defenderId, float x, float y, out string why)
        {
            if (Over) { why = "The run is over"; return false; }
            bool ok = Battle.TryMove(defenderId, x, y, out why);
            if (ok) Record(new Act { Kind = ActKind.Move, DefenderId = defenderId, X = x, Y = y });
            return ok;
        }

        public bool Sell(int defenderId, out float refund)
        {
            refund = 0f;
            if (Over) return false;
            bool ok = Battle.TrySell(defenderId, rules.SellRefund, out refund);
            if (ok) Record(new Act { Kind = ActKind.Sell, DefenderId = defenderId });
            return ok;
        }

        public bool SetWaypoint(int defenderId, float x, float y, out string why)
        {
            if (Over) { why = "The run is over"; return false; }
            bool ok = Battle.TrySetDroneWaypoint(defenderId, x, y, out why);
            if (ok) Record(new Act { Kind = ActKind.Waypoint, DefenderId = defenderId, X = x, Y = y });
            return ok;
        }

        /// <summary>Start the next wave. False when there is nothing to start (a wave running, the run over).</summary>
        public bool StartNextWave()
        {
            int next = Battle.WaveNumber + 1;
            if (Over || Battle.Phase != BattlePhase.Preparation || next > plan.WaveCount) return false;
            Apply(new Act { Kind = ActKind.StartWave });
            Record(new Act { Kind = ActKind.StartWave });
            return true;
        }

        void Record(Act a)
        {
            if (Replaying) return;
            a.Step = step;
            log.Add(a);
        }

        void Apply(Act a)
        {
            switch (a.Kind)
            {
                case ActKind.Place: Battle.TryPlace(a.UnitId, a.X, a.Y, out _, out _); break;
                case ActKind.Move: Battle.TryMove(a.DefenderId, a.X, a.Y, out _); break;
                case ActKind.Sell: Battle.TrySell(a.DefenderId, rules.SellRefund, out _); break;
                case ActKind.Waypoint: Battle.TrySetDroneWaypoint(a.DefenderId, a.X, a.Y, out _); break;
                case ActKind.StartWave:
                    plan.Queue(Battle, Battle.WaveNumber + 1);
                    Battle.StartRound();
                    break;
            }
        }

        // ---- time ------------------------------------------------------------------------------------------------

        /// <summary>One fixed step of the battle, and the end of the wave when it comes.</summary>
        public void Tick()
        {
            if (Over || Battle.Phase != BattlePhase.Round) return;
            if (Battle.Outcome == BattleOutcome.Ongoing)
            {
                Battle.Step(Dt);
                step++;
            }
            if (Battle.Outcome == BattleOutcome.Lost)
            {
                Over = true;
                WaveEnded?.Invoke(Battle.WaveNumber, "lost");
            }
            else if (Battle.Outcome == BattleOutcome.Won && Battle.Attackers.Count == 0 && Battle.Pending == 0)
            {
                WavesHeld = Battle.WaveNumber;
                if (WavesHeld >= plan.WaveCount)
                {
                    Over = true;
                    Won = true;
                    WaveEnded?.Invoke(WavesHeld, "held");
                    return;
                }
                LastReward = Battle.CollectRewards();
                Battle.AddBudget(rules.WaveIncome);
                Battle.BackToPreparation();
                WaveEnded?.Invoke(WavesHeld, "held");
            }
        }

        // ---- rewind ----------------------------------------------------------------------------------------------

        /// <summary>
        /// Go back to just before <paramref name="wave"/> started: money, defenders and the enemy's memory as they were
        /// when the wave before it was held. Everything recorded after that point is forgotten.
        /// </summary>
        public bool RewindTo(int wave)
        {
            if (wave < 1 || wave > WavesHeld + 1 || wave > plan.WaveCount) return false;
            var old = new List<Act>(log);
            Battle = make();
            step = 0;
            WavesHeld = 0;
            Over = false;
            Won = false;
            LastReward = 0f;
            log.Clear();
            Replaying = true;
            try
            {
                int i = 0;
                while (!AtStartOf(wave))
                {
                    if (i < old.Count && old[i].Step <= step)
                    {
                        Apply(old[i]);
                        log.Add(old[i]);
                        i++;
                        continue;
                    }
                    if (Battle.Phase != BattlePhase.Round || Over) break;   // the record ends here
                    Tick();
                }
            }
            finally { Replaying = false; }
            return AtStartOf(wave);
        }

        bool AtStartOf(int wave) => !Over && WavesHeld == wave - 1 && Battle.Phase == BattlePhase.Preparation;
    }
}
