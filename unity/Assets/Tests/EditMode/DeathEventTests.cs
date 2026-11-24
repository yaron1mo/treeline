using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Treeline.Sim;
using UnityEngine;

namespace Treeline.Tests
{
    // Death events for the renderer (Codex request, 2026-11-24): one read-only event per attacker killed, with what
    // killed it. They change nothing in the battle.
    public class DeathEventTests
    {
        static string Data(string name) => File.ReadAllText(Path.Combine(Application.dataPath, "Data", name));

        static Battle Fight(string defender, string attacker, int count, float gap, int seed, bool recon = true)
        {
            var field = new Field { Name = "t", Width = 60f, Height = 300f };
            field.Spawns.Add(30f);
            var t = ObstacleTable.Parse(Data("obstacles.csv"));
            var b = new Battle(new NavGrid(field, t), UnitTable.Parse(Data("units.csv")), 50000f, new VisionMap(field, t),
                ArmourTable.Parse(Data("armour.csv")), 1000f, seed, BattleRules.Parse(Data("battle_rules.csv")));
            if (defender != null) Assert.IsTrue(b.TryPlace(defender, 30f, 250f, out _, out _));
            if (recon) Assert.IsTrue(b.TryPlace("recon_drone", 30f, 200f, out _, out _));
            b.BeginWave();
            b.QueueWave(attacker, count, 0f, gap);
            b.StartRound();
            return b;
        }

        static void Finish(Battle b)
        {
            for (float t = 0f; t < 300f && b.Outcome == BattleOutcome.Ongoing; t += 0.05f) b.Step(0.05f);
        }

        static void AssertUnit(DeathEvent e)
        {
            float len = (float)System.Math.Sqrt(e.DirX * e.DirX + e.DirY * e.DirY);
            Assert.AreEqual(1f, len, 0.01f, "direction is a unit vector");
        }

        [Test]
        public void ABulletDeath_IsABulletEvent_FromTheShooter()
        {
            var b = Fight("rifle_squad", "recruit", 3, 1f, 1, false);
            Finish(b);
            Assert.AreEqual(3, b.DeathEvents.Count);
            for (int i = 0; i < 3; i++)
            {
                var e = b.DeathEvents[i];
                Assert.AreEqual(i + 1, e.EventId);
                Assert.AreEqual("recruit", e.VictimType);
                Assert.AreEqual(DeathCause.Bullet, e.Cause);
                Assert.AreEqual(1, e.Wave);
                Assert.IsTrue(e.HasSource);
                Assert.AreEqual("rifle_squad", e.SourceType);
                Assert.AreEqual(30f, e.SourceX, 0.5f);
                Assert.AreEqual(250f, e.SourceY, 0.5f);
                Assert.IsFalse(e.HasImpact);
                Assert.Less(e.DirY, -0.9f, "the bullet flew up the field, from the shooter to the victim");
                AssertUnit(e);
            }
            Assert.AreEqual(3, b.LastDeathEventId);
        }

        [Test]
        public void AMissileOnATank_IsADirectExplosiveEvent()
        {
            var b = Fight("anti_tank", "old_tank", 1, 1f, 1);
            Finish(b);
            Assert.AreEqual(1, b.DeathEvents.Count);
            var e = b.DeathEvents[0];
            Assert.AreEqual("old_tank", e.VictimType);
            Assert.AreEqual(DeathCause.DirectExplosive, e.Cause);
            Assert.IsTrue(e.HasImpact);
            Assert.AreEqual("anti_tank", e.SourceType);
            Assert.AreEqual(250f, e.SourceY, 0.5f);
            AssertUnit(e);
        }

        [Test]
        public void AMortarShellNearAGroup_KillsWithBlastEvents()
        {
            var b = Fight("mortar", "recruit", 10, 0.3f, 3);
            Finish(b);
            Assert.Greater(b.DeathEvents.Count, 0);
            int blast = 0;
            foreach (var e in b.DeathEvents)
            {
                Assert.IsTrue(e.Cause == DeathCause.Blast || e.Cause == DeathCause.DirectExplosive, e.Cause.ToString());
                Assert.IsTrue(e.HasImpact);
                Assert.AreEqual("mortar", e.SourceType);
                Assert.AreEqual(250f, e.SourceY, 0.5f, "the shell came from the mortar");
                AssertUnit(e);
                if (e.Cause == DeathCause.Blast)
                {
                    blast++;
                    // A blast pushes away from the burst.
                    float dx = e.X - e.ImpactX, dy = e.Y - e.ImpactY;
                    Assert.Greater(dx * e.DirX + dy * e.DirY, 0f);
                }
            }
            Assert.Greater(blast, 0);
        }

        [Test]
        public void AnAttackerThatCrossesTheLine_HasNoDeathEvent()
        {
            var b = Fight(null, "recruit", 3, 1f, 1, false);
            Finish(b);
            Assert.Greater(b.LineDamage, 0f);
            Assert.AreEqual(0, b.DeathEvents.Count);
            Assert.AreEqual(0, b.LastDeathEventId);
        }

        [Test]
        public void ReadingWithACursor_ShowsEachDeathOnce_AndANewWaveClearsTheList()
        {
            var b = Fight("rifle_squad", "recruit", 3, 1f, 1, false);
            var seen = new List<DeathEvent>();
            int cursor = 0;
            for (float t = 0f; t < 300f && b.Outcome == BattleOutcome.Ongoing; t += 0.05f)
            {
                b.Step(0.05f);
                b.Step(0.05f);   // two steps in one "frame"
                cursor = b.ReadDeathEvents(cursor, seen);
            }
            cursor = b.ReadDeathEvents(cursor, seen);
            Assert.AreEqual(3, seen.Count);
            Assert.AreEqual("1 2 3", $"{seen[0].EventId} {seen[1].EventId} {seen[2].EventId}");

            Assert.IsTrue(b.BackToPreparation());
            b.BeginWave();
            b.QueueWave("recruit", 1, 0f, 1f);
            b.StartRound();
            Assert.AreEqual(0, b.DeathEvents.Count, "a new wave starts with an empty list");
            Assert.AreEqual(3, b.LastDeathEventId, "ids keep counting");
            Assert.AreEqual(3, b.ReadDeathEvents(cursor, seen));
            Assert.AreEqual(3, seen.Count);
        }

        [Test]
        public void ARewind_RebuildsTheSameDeathEvents()
        {
            var units = UnitTable.Parse(Data("units.csv"));
            var obstacles = ObstacleTable.Parse(Data("obstacles.csv"));
            var armour = ArmourTable.Parse(Data("armour.csv"));
            var rules = BattleRules.Parse(Data("battle_rules.csv"));
            var plan = WavePlan.Parse("wave,unit,count,delay_s,interval_s\n1,recruit,5,0,1\n2,regular,6,0,1\n");
            var field = new Field { Name = "t", Width = 120f, Height = 300f };
            field.Spawns.Add(40f);
            field.Spawns.Add(80f);
            var run = new Run(() => new Battle(new NavGrid(field, obstacles), units, rules.StartBudget + 1000f,
                new VisionMap(field, obstacles), armour, -1f, 12345, rules), plan, rules);
            Assert.IsTrue(run.Place("machine_gun", 40f, 220f, out _, out _));
            Assert.IsTrue(run.Place("machine_gun", 80f, 220f, out _, out _));
            Assert.IsTrue(run.StartNextWave());
            for (int i = 0; i < 60000 && run.Battle.Phase == BattlePhase.Round && !run.Over; i++) run.Tick();
            Assert.AreEqual(1, run.WavesHeld);
            var before = Describe(run.Battle.DeathEvents);
            int last = run.Battle.LastDeathEventId;
            Assert.Greater(last, 0);

            var old = run.Battle;
            Assert.IsTrue(run.RewindTo(2));
            Assert.IsFalse(ReferenceEquals(old, run.Battle), "a rewind builds a new battle: the renderer starts from its LastDeathEventId");
            Assert.AreEqual(before, Describe(run.Battle.DeathEvents));
            Assert.AreEqual(last, run.Battle.LastDeathEventId);
        }

        static string Describe(IReadOnlyList<DeathEvent> list)
        {
            var parts = new List<string>();
            foreach (var e in list)
                parts.Add($"{e.EventId} {e.VictimId} {e.VictimType} {e.X:0.###},{e.Y:0.###} t{e.Time:0.###} w{e.Wave} {e.Cause} " +
                          $"{e.SourceUnitId} {e.SourceX:0.###},{e.SourceY:0.###} {e.DirX:0.###},{e.DirY:0.###}");
            return string.Join(" | ", parts);
        }
    }
}
