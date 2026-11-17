using System.IO;
using NUnit.Framework;
using Treeline.Sim;
using UnityEngine;

namespace Treeline.Tests
{
    // A run of waves for the playtest (Yaroslav, 2026-11-16): wave plan, money between waves, selling, placement zone.
    public class RunTests
    {
        static string Data(string name) => File.ReadAllText(Path.Combine(Application.dataPath, "Data", name));

        static Battle Make(float budget = 2000f)
        {
            var field = new Field { Name = "t", Width = 60f, Height = 300f };
            var obstacles = ObstacleTable.Parse(Data("obstacles.csv"));
            return new Battle(new NavGrid(field, obstacles), UnitTable.Parse(Data("units.csv")), budget,
                new VisionMap(field, obstacles), ArmourTable.Parse(Data("armour.csv")), 1000f, 1, BattleRules.Parse(Data("battle_rules.csv")));
        }

        [Test]
        public void TheWavePlan_ParsesAndEveryUnitInItExists()
        {
            var plan = WavePlan.Parse(Data("waves.csv"));
            var units = UnitTable.Parse(Data("units.csv"));
            Assert.GreaterOrEqual(plan.WaveCount, 3);
            foreach (var g in plan.Groups)
            {
                Assert.IsTrue(units.TryGet(g.UnitId, out var d), "unknown unit " + g.UnitId);
                Assert.IsFalse(d.IsDefence, g.UnitId + " is a defender");
                Assert.Greater(g.Count, 0);
            }
            for (int w = 1; w <= plan.WaveCount; w++) Assert.Greater(plan.WaveGroups(w).Count, 0, "wave " + w + " is empty");
        }

        [Test]
        public void AQueuedWave_SpawnsItsGroups_AndAHeldWaveGoesBackToPreparation()
        {
            var plan = WavePlan.Parse("wave,unit,count,delay_s,interval_s\n1,recruit,2,0,1\n2,regular,3,0,1\n");
            var b = Make();
            Assert.IsTrue(b.TryPlace("machine_gun", 30f, 280f, out _, out _));
            Assert.IsTrue(plan.Queue(b, 1));
            b.StartRound();
            for (float t = 0f; t < 120f && b.Outcome == BattleOutcome.Ongoing; t += 0.05f) b.Step(0.05f);
            Assert.AreEqual(BattleOutcome.Won, b.Outcome);
            Assert.AreEqual(2, b.Stats["recruit"].Spawned);
            Assert.IsTrue(b.BackToPreparation());
            Assert.AreEqual(BattlePhase.Preparation, b.Phase);

            // Placed between waves: stands there at once, no running in.
            Assert.IsTrue(b.TryPlace("rifle_squad", 20f, 270f, out var r, out _));
            Assert.AreEqual(DefenderState.Standing, r.State);
            Assert.AreEqual(270f, r.Y, 0.01f);

            Assert.IsTrue(plan.Queue(b, 2));
            b.StartRound();
            Assert.AreEqual(2, b.WaveNumber);
            for (float t = 0f; t < 120f && b.Outcome == BattleOutcome.Ongoing; t += 0.05f) b.Step(0.05f);
            Assert.AreEqual(3, b.Stats["regular"].Spawned);
        }

        [Test]
        public void Selling_GivesBackTheRefundShare()
        {
            var b = Make(1000f);
            Assert.IsTrue(b.TryPlace("machine_gun", 30f, 280f, out var mg, out _));
            Assert.AreEqual(800f, b.Budget, 0.01f);
            Assert.IsTrue(b.TrySell(mg.Id, 0.5f, out float refund));
            Assert.AreEqual(100f, refund, 0.01f);
            Assert.AreEqual(900f, b.Budget, 0.01f);
            Assert.AreEqual(0, b.Defenders.Count);
            b.AddBudget(300f);
            Assert.AreEqual(1200f, b.Budget, 0.01f);
        }

        [Test]
        public void TheGame_CanKeepDefendersOutOfTheTopOfTheField()
        {
            var b = Make();
            b.PlacementMinY = 0.35f * 300f;
            Assert.IsFalse(b.TryPlace("rifle_squad", 30f, 50f, out _, out var why));
            StringAssert.Contains("enemy", why);
            Assert.IsTrue(b.TryPlace("rifle_squad", 30f, 200f, out _, out _));
            // Yaroslav 2026-11-17: in the data the zone is off, defenders stand anywhere (the mechanism stays).
            Assert.AreEqual(0f, BattleRules.Parse(Data("battle_rules.csv")).PlacementZoneStart, 1e-4f);
        }

        [Test]
        public void KillRewards_AreAShareOfThreatValue_AndArePaidWhenCollected()
        {
            var rules = BattleRules.Parse(Data("battle_rules.csv") + "kill_reward_share,0.5,test\n");
            var field = new Field { Name = "t", Width = 60f, Height = 300f };
            var obstacles = ObstacleTable.Parse(Data("obstacles.csv"));
            var units = UnitTable.Parse(Data("units.csv"));
            var b = new Battle(new NavGrid(field, obstacles), units, 1000f, new VisionMap(field, obstacles),
                ArmourTable.Parse(Data("armour.csv")), 1000f, 1, rules);
            Assert.IsTrue(b.TryPlace("machine_gun", 30f, 280f, out _, out _));
            b.QueueWave("recruit", 3, 0f, 1f);
            b.StartRound();
            for (float t = 0f; t < 120f && b.Outcome == BattleOutcome.Ongoing; t += 0.05f) b.Step(0.05f);
            Assert.AreEqual(BattleOutcome.Won, b.Outcome);
            int killed = b.Stats["recruit"].Killed;
            Assert.Greater(killed, 0);
            float expected = killed * units.Get("recruit").ThreatValue * 0.5f;
            Assert.AreEqual(expected, b.Rewards, 0.01f, "earned, not yet paid");
            float budget = b.Budget;
            Assert.AreEqual(expected, b.CollectRewards(), 0.01f);
            Assert.AreEqual(budget + expected, b.Budget, 0.01f);
            Assert.AreEqual(0f, b.Rewards);
        }

        [Test]
        public void TheSideStrips_AreClosed_NobodyWalksOrStandsThere()
        {
            Assert.AreEqual(10f, BattleRules.Parse(Data("battle_rules.csv")).EdgeMarginM, 1e-4f);
            var b = Make();   // 60 m wide: open from x 10 to 50
            Assert.IsFalse(b.TryPlace("rifle_squad", 4f, 200f, out _, out _), "not in the left strip");
            Assert.IsFalse(b.TryPlace("rifle_squad", 57f, 200f, out _, out _), "not in the right strip");
            Assert.IsTrue(b.TryPlace("rifle_squad", 30f, 200f, out _, out _));
            b.QueueWave("recruit", 10, 0f, 0.5f);
            b.QueueWave("buggy", 3, 2f, 1f);
            b.StartRound();
            for (float t = 0f; t < 120f && b.Outcome == BattleOutcome.Ongoing; t += 0.05f)
            {
                b.Step(0.05f);
                foreach (var a in b.Attackers)
                    Assert.IsTrue(a.X >= 9f && a.X <= 51f, a.Def.Id + " at x " + a.X);
            }
        }
    }
}
