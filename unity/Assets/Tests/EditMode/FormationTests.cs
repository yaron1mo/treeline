using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Treeline.Sim;
using UnityEngine;

namespace Treeline.Tests
{
    // Formations (Yaroslav, 2026-11-17): a column while the way is free, an assault line when every way crosses a known
    // defence; a column cannot shoot over its own men's backs.
    public class FormationTests
    {
        static string Data(string name) => File.ReadAllText(Path.Combine(Application.dataPath, "Data", name));

        static Battle Make(Field field, string extraRules = "")
        {
            var table = ObstacleTable.Parse(Data("obstacles.csv"));
            return new Battle(new NavGrid(field, table), UnitTable.Parse(Data("units.csv")), 50000f, new VisionMap(field, table),
                ArmourTable.Parse(Data("armour.csv")), 1000f, 3, BattleRules.Parse(Data("battle_rules.csv") + extraRules));
        }

        static void RunWave(Battle b, string unit, int count, float gap)
        {
            b.BeginWave();
            b.QueueWave(unit, count, 0f, gap);
            if (b.Phase == BattlePhase.Preparation) b.StartRound();
            for (float t = 0f; t < 200f && b.Outcome == BattleOutcome.Ongoing; t += 0.05f) b.Step(0.05f);
        }

        static float SpreadX(List<Attacker> list)
        {
            float min = float.MaxValue, max = float.MinValue;
            foreach (var a in list) { min = System.Math.Min(min, a.X); max = System.Math.Max(max, a.X); }
            return max - min;
        }

        [Test]
        public void FreeWay_IsAColumn_ADefenceTheyCannotAvoid_IsMetWithALine()
        {
            var field = new Field { Name = "t", Width = 160f, Height = 300f };
            field.Spawns.Add(80f);
            var b = Make(field);
            // Gunners across the whole width: no way round them.
            foreach (float x in new[] { 20f, 50f, 80f, 110f, 140f }) Assert.IsTrue(b.TryPlace("machine_gun", x, 200f, out _, out _));

            // Wave 1 knows nothing: a column.
            b.BeginWave();
            b.QueueWave("special_forces", 8, 0f, 0.5f);
            b.StartRound();
            for (float t = 0f; t < 12f; t += 0.05f) b.Step(0.05f);
            var first = new List<Attacker>(b.Attackers);
            Assert.Greater(first.Count, 4);
            foreach (var a in first) Assert.AreEqual(Formation.Column, a.Formation);
            Assert.Less(SpreadX(first), 5f, "one behind the other: " + SpreadX(first));
            for (float t = 0f; t < 200f && b.Outcome == BattleOutcome.Ongoing; t += 0.05f) b.Step(0.05f);

            RunWave(b, "special_forces", 2, 1f);   // wave 2: special forces lag 2, still nothing known
            // Wave 3 knows what wave 1 saw: every way crosses a gunner, so an assault line.
            b.BeginWave();
            b.QueueWave("special_forces", 8, 0f, 0.5f);
            for (float t = 0f; t < 20f; t += 0.05f) b.Step(0.05f);
            var third = new List<Attacker>(b.Attackers);
            Assert.Greater(third.Count, 4);
            foreach (var a in third)
            {
                Assert.AreEqual(RouteMode.Safest, a.Mode);
                Assert.AreEqual(Formation.Line, a.Formation);
            }
            Assert.Greater(SpreadX(third), 15f, "spread across the route: " + SpreadX(third));
        }

        [Test]
        public void TheLine_NeverStepsOntoGroundItCannotWalk()
        {
            var field = new Field { Name = "t", Width = 60f, Height = 300f };   // open 10..50 (side strips)
            field.Spawns.Add(30f);
            var b = Make(field, "column_jitter_m,25,test\n");   // force a wide spread even in a column
            b.QueueWave("regular", 10, 0f, 0.5f);
            b.StartRound();
            for (float t = 0f; t < 80f && b.Outcome == BattleOutcome.Ongoing; t += 0.05f)
            {
                b.Step(0.05f);
                foreach (var a in b.Attackers) Assert.IsTrue(a.X > 9f && a.X < 51f, "at x " + a.X);
            }
        }

        // How many shots a column of regulars fires at a gunner straight ahead in its first 10 s of fighting.
        static int ColumnShots(bool block)
        {
            var field = new Field { Name = "t", Width = 60f, Height = 300f };
            field.Spawns.Add(30f);
            var b = Make(field, block ? "" : "friendly_block_m,0,test\n");
            Assert.IsTrue(b.TryPlace("machine_gun", 30f, 250f, out var mg, out _));
            mg.Hp = 100000f;   // it must outlive the test
            b.QueueWave("regular", 6, 0f, 1f);
            b.StartRound();
            float firstShot = -1f;
            for (float t = 0f; t < 120f; t += 0.05f)
            {
                b.Step(0.05f);
                int shots = b.Stats.TryGetValue("regular", out var s) ? s.ShotsFired : 0;
                if (shots > 0 && firstShot < 0f) firstShot = b.RoundTime;
                if (firstShot >= 0f && b.RoundTime - firstShot >= 10f) return shots;
            }
            return b.Stats.TryGetValue("regular", out var st) ? st.ShotsFired : 0;
        }

        [Test]
        public void AColumn_CannotShootOverItsOwnMensBacks()
        {
            int blocked = ColumnShots(true), free = ColumnShots(false);
            Assert.Greater(blocked, 0, "the front ones shoot");
            Assert.Less(blocked, free * 0.7f, "blocked " + blocked + " vs free " + free);
        }
    }
}
