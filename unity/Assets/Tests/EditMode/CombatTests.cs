using System.IO;
using NUnit.Framework;
using Treeline.Sim;
using UnityEngine;

namespace Treeline.Tests
{
    public class CombatTests
    {
        static string Data(string name) => File.ReadAllText(Path.Combine(Application.dataPath, "Data", name));

        static Pt[] Box(float x0, float y0, float x1, float y1) =>
            new[] { new Pt(x0, y0), new Pt(x1, y0), new Pt(x1, y1), new Pt(x0, y1) };

        static Field Open() => new Field { Name = "test", Width = 40f, Height = 100f };

        static Battle Make(Field field, float lineHealth = 20f, int seed = 1)
        {
            var grid = new NavGrid(field);
            var vision = new VisionMap(field, ObstacleTable.Parse(Data("obstacles.csv")));
            return new Battle(grid, UnitTable.Parse(Data("units.csv")), 5000f, vision,
                ArmourTable.Parse(Data("armour.csv")), lineHealth, seed);
        }

        static void Run(Battle b, float seconds, float dt = 0.05f)
        {
            for (float t = 0f; t < seconds && b.Outcome == BattleOutcome.Ongoing; t += dt) b.Step(dt);
        }

        [Test]
        public void Unopposed_RecruitsReachTheRearEdgeAndHurtTheLine()
        {
            var b = Make(Open());
            b.QueueWave("recruit", 3, 0f, 1f);
            b.StartRound();
            Run(b, 120f);
            Assert.AreEqual(3f, b.LineDamage, 1e-4f);
            Assert.AreEqual(BattleOutcome.Won, b.Outcome);
            Assert.AreEqual(0, b.Attackers.Count);
        }

        [Test]
        public void ASecondWave_CanBeSentAfterTheFirstIsWon()
        {
            var b = Make(Open());
            b.QueueWave("recruit", 2, 0f, 1f);
            b.StartRound();
            Run(b, 120f);
            Assert.AreEqual(BattleOutcome.Won, b.Outcome);
            float t = b.RoundTime;
            Assert.IsTrue(b.QueueWave("recruit", 2, 1f, 1f));
            Assert.AreEqual(BattleOutcome.Ongoing, b.Outcome);
            Run(b, 120f);
            Assert.AreEqual(BattleOutcome.Won, b.Outcome);
            Assert.AreEqual(4f, b.LineDamage, 1e-4f, "both waves reached the edge");
            Assert.Greater(b.RoundTime, t + 1f, "the second wave started from now, not from round start");
        }

        [Test]
        public void TooMuchLineDamage_LosesTheBattle()
        {
            var b = Make(Open(), lineHealth: 2f);
            b.QueueWave("recruit", 3, 0f, 1f);
            b.StartRound();
            Run(b, 120f);
            Assert.AreEqual(BattleOutcome.Lost, b.Outcome);
        }

        [Test]
        public void MachineGunner_StopsAHandfulOfRecruits()
        {
            var b = Make(Open());
            Assert.IsTrue(b.TryPlace("machine_gun", 20f, 70f, out _, out _));
            b.QueueWave("recruit", 4, 0f, 2f);
            b.StartRound();
            Run(b, 120f);
            Assert.AreEqual(BattleOutcome.Won, b.Outcome);
            Assert.AreEqual(0f, b.LineDamage, 1e-4f, "nobody got through");
        }

        [Test]
        public void ARunningDefender_CannotFire()
        {
            var b = Make(Open());
            b.QueueWave("recruit", 1, 0f, 1f);
            b.StartRound();
            b.Step(0.05f);
            Assert.IsTrue(b.TryPlace("machine_gun", 20f, 20f, out var d, out _));
            Assert.AreEqual(DefenderState.Running, d.State);
            var recruit = b.Attackers[0];
            while (d.State == DefenderState.Running && b.RoundTime < 30f)
            {
                b.Step(0.05f);
                if (d.State == DefenderState.Running)
                    Assert.AreEqual(recruit.Def.Hp, recruit.Hp, 1e-4f, "no damage while the gunner is still running in");
            }
            Assert.AreEqual(DefenderState.Standing, d.State);
        }

        [Test]
        public void Rock_ProtectsTheUnitBehindItFromFireFromTheFront()
        {
            var field = Open();
            field.Features.Add(new Feature { Type = "rock", Poly = Box(18, 50, 22, 54) });
            var v = new VisionMap(field, ObstacleTable.Parse(Data("obstacles.csv")));
            Assert.IsTrue(v.CoverFor(20, 40, 20, 57) != null, "target 3 m behind the rock, shooter in front");
            Assert.IsTrue(v.CoverFor(35, 57, 20, 57) == null, "shooter at the side");
            Assert.IsTrue(v.CoverFor(20, 70, 20, 57) == null, "shooter behind the target");
        }

        static Battle Skirmish()
        {
            var b = Make(Open(), seed: 7);
            b.TryPlace("rifle_squad", 12f, 60f, out _, out _);
            b.QueueWave("regular", 5, 0f, 1f);
            b.StartRound();
            Run(b, 14f);
            return b;
        }

        [Test]
        public void SameSeed_GivesTheSameBattle()
        {
            var one = Skirmish();
            var two = Skirmish();
            Assert.AreEqual(one.Attackers.Count, two.Attackers.Count);
            for (int i = 0; i < one.Attackers.Count; i++)
            {
                Assert.AreEqual(one.Attackers[i].X, two.Attackers[i].X);
                Assert.AreEqual(one.Attackers[i].Hp, two.Attackers[i].Hp);
            }
            Assert.AreEqual(one.LineDamage, two.LineDamage);
        }

        [Test]
        public void WeaponClasses_OnlyHitWhatTheyShould()
        {
            var u = UnitTable.Parse(Data("units.csv"));
            Assert.IsTrue(Battle.CanTarget(u.Get("machine_gun"), u.Get("recruit")));
            Assert.IsFalse(Battle.CanTarget(u.Get("air_defence"), u.Get("recruit")), "air defence is for aircraft");
            Assert.IsFalse(Battle.CanTarget(u.Get("anti_tank"), u.Get("recruit")), "anti-tank is for vehicles");
            Assert.IsTrue(Battle.CanTarget(u.Get("anti_tank"), u.Get("old_tank")));
            Assert.IsTrue(Battle.CanTarget(u.Get("air_defence"), u.Get("helicopter")));
            Assert.IsFalse(Battle.CanTarget(u.Get("machine_gun"), u.Get("helicopter")), "helicopter is air_defence / fpv / explosive only");
            Assert.IsTrue(Battle.CanTarget(u.Get("machine_gun"), u.Get("fpv")));
        }

        [Test]
        public void ArmourTable_ReducesDamageByTypeAndLevel()
        {
            var a = ArmourTable.Parse(Data("armour.csv"));
            Assert.AreEqual(1f, a.Factor("small_arms", ""), "empty armour means none");
            Assert.AreEqual(0.5f, a.Factor("small_arms", "low"));
            Assert.AreEqual(0f, a.Factor("small_arms", "high"));
            Assert.Greater(a.Factor("anti_tank", "high"), a.Factor("small_arms", "high"));
            Assert.AreEqual(1f, a.Factor("unknown", "high"), "unknown damage types do not reduce damage");
        }
    }
}
