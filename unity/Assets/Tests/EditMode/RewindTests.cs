using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
using Treeline.Sim;
using UnityEngine;

namespace Treeline.Tests
{
    // Replay earlier rounds (Yaroslav, 2026-11-17): a run records the player's actions and can go back to the start of
    // an earlier wave, exactly as it was then.
    public class RewindTests
    {
        static string Data(string name) => File.ReadAllText(Path.Combine(Application.dataPath, "Data", name));

        static Run MakeRun(float lineHealth = -1f)
        {
            var units = UnitTable.Parse(Data("units.csv"));
            var obstacles = ObstacleTable.Parse(Data("obstacles.csv"));
            var armour = ArmourTable.Parse(Data("armour.csv"));
            var rules = BattleRules.Parse(Data("battle_rules.csv"));
            var plan = WavePlan.Parse("wave,unit,count,delay_s,interval_s\n1,recruit,5,0,1\n2,regular,6,0,1\n2,buggy,2,4,2\n3,regular,8,0,1\n");
            var field = new Field { Name = "t", Width = 120f, Height = 300f };
            field.Spawns.Add(40f);
            field.Spawns.Add(80f);
            return new Run(() => new Battle(new NavGrid(field, obstacles), units, rules.StartBudget + 1000f,
                new VisionMap(field, obstacles), armour, lineHealth, 12345, rules), plan, rules);
        }

        // Everything the player would notice about the state of the run.
        static string Snapshot(Run r)
        {
            var b = r.Battle;
            var sb = new StringBuilder();
            sb.Append($"held {r.WavesHeld} wave {b.WaveNumber} phase {b.Phase} money {b.Budget:0.###} line {b.LineDamage:0.###}");
            foreach (var d in b.Defenders) sb.Append($" | {d.Id} {d.Def.Id} {d.X:0.###},{d.Y:0.###} hp {d.Hp:0.###}");
            foreach (var kv in new SortedDictionary<string, UnitStats>(new Dictionary<string, UnitStats>(Stats(b))))
                sb.Append($" | {kv.Key} k{kv.Value.Killed} s{kv.Value.Spawned} c{kv.Value.Crossed}");
            return sb.ToString();
        }

        static IDictionary<string, UnitStats> Stats(Battle b)
        {
            var d = new Dictionary<string, UnitStats>();
            foreach (var kv in b.Stats) d[kv.Key] = kv.Value;
            return d;
        }

        static void PlayWave(Run r, System.Action midWave = null)
        {
            Assert.IsTrue(r.StartNextWave());
            int ticks = 0;
            while (r.Battle.Phase == BattlePhase.Round && !r.Over && ticks < 60000)
            {
                r.Tick();
                if (++ticks == 150) midWave?.Invoke();   // something done during the wave, 5 s in
            }
        }

        [Test]
        public void RewindingToAWave_GivesExactlyTheStateItStartedWith()
        {
            var r = MakeRun();
            Assert.IsTrue(r.Place("rifle_squad", 40f, 230f, out _, out _));
            Assert.IsTrue(r.Place("machine_gun", 80f, 230f, out var mg, out _));
            PlayWave(r);
            Assert.AreEqual(1, r.WavesHeld, "wave 1 held");
            string beforeWave2 = Snapshot(r);

            // Wave 2: a sniper bought before it, the gunner moved and a rifle squad sent in during it.
            Assert.IsTrue(r.Place("sniper", 60f, 260f, out _, out _));
            PlayWave(r, () =>
            {
                Assert.IsTrue(r.Move(mg.Id, 70f, 220f, out _));
                Assert.IsTrue(r.Place("rifle_squad", 50f, 240f, out _, out _));
            });
            Assert.AreEqual(2, r.WavesHeld, "wave 2 held");
            string beforeWave3 = Snapshot(r);
            int acts = r.Log.Count;

            // Play wave 3 badly (sell everything first), then go back.
            foreach (var d in new List<Defender>(r.Battle.Defenders)) r.Sell(d.Id, out _);
            PlayWave(r);

            Assert.IsTrue(r.RewindTo(3));
            Assert.AreEqual(beforeWave3, Snapshot(r), "back to the start of wave 3, with the mid-wave actions of wave 2 replayed");
            Assert.AreEqual(acts, r.Log.Count, "what came after is forgotten");

            Assert.IsTrue(r.RewindTo(2));
            Assert.AreEqual(beforeWave2, Snapshot(r), "back to the start of wave 2");

            Assert.IsTrue(r.RewindTo(1));
            Assert.AreEqual(0, r.Battle.Defenders.Count, "wave 1: an empty field, as at the start");
            Assert.IsFalse(r.RewindTo(3), "cannot go forward past what was played");
        }

        [Test]
        public void ALostRun_CanRetryTheWaveItLost()
        {
            var r = MakeRun(8f);   // a short line: 5 recruits get through wave 1 unopposed (5 damage), wave 2 breaks it
            PlayWave(r);
            Assert.IsFalse(r.Over, "wave 1: 5 of 8");
            Assert.IsTrue(r.Place("rifle_squad", 40f, 230f, out _, out _));
            foreach (var d in new List<Defender>(r.Battle.Defenders)) r.Sell(d.Id, out _);
            PlayWave(r);
            Assert.IsTrue(r.Over, "wave 2 breaks the line");
            Assert.IsFalse(r.Won);
            Assert.IsTrue(r.RewindTo(2), "retry the lost wave");
            Assert.IsFalse(r.Over);
            Assert.AreEqual(BattlePhase.Preparation, r.Battle.Phase);
            Assert.AreEqual(1, r.WavesHeld);
            Assert.AreEqual(0, r.Battle.Defenders.Count, "the squad was bought after wave 1 was held: not there");
            Assert.AreEqual(5f, r.Battle.LineDamage, 1e-3f, "the damage of wave 1 stays");
        }
    }
}
