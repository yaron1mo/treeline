using System.IO;
using NUnit.Framework;
using Treeline.Sim;
using UnityEngine;

namespace Treeline.Tests
{
    // Enemy aircraft (2026-11-15): helicopter air assault, FPV strikes, fly-to-rear drones.
    public class AviationTests
    {
        static string Data(string name) => File.ReadAllText(Path.Combine(Application.dataPath, "Data", name));

        static Battle Make(int seed = 1, float height = 300f)
        {
            var field = new Field { Name = "t", Width = 60f, Height = height };
            var obstacles = ObstacleTable.Parse(Data("obstacles.csv"));
            return new Battle(new NavGrid(field, obstacles), UnitTable.Parse(Data("units.csv")), 5000f,
                new VisionMap(field, obstacles), ArmourTable.Parse(Data("armour.csv")), 1000f, seed,
                BattleRules.Parse(Data("battle_rules.csv")));
        }

        static void Run(Battle b, float seconds)
        {
            for (float t = 0f; t < seconds && b.Outcome == BattleOutcome.Ongoing; t += 0.05f) b.Step(0.05f);
        }

        [Test]
        public void AHelicopter_DropsItsTroopsPartWayDown_ThenFliesOut()
        {
            var b = Make();
            b.QueueWave("helicopter", 1, 0f, 1f);
            b.StartRound();
            float dropY = -1f;
            for (float t = 0f; t < 20f; t += 0.05f)
            {
                b.Step(0.05f);
                foreach (var a in b.Attackers)
                    if (a.Def.Id == "regular" && dropY < 0f) dropY = a.Y;
            }
            Assert.AreEqual(300f * 0.55f, dropY, 1f, "troops come down 55% of the way");
            Assert.AreEqual(20, b.Stats["regular"].Spawned);
            Run(b, 200f);
            Assert.AreEqual(0, b.Stats["helicopter"].Killed, "it flew out, it was not shot down");
            Assert.AreEqual(0, b.Stats["helicopter"].Crossed, "and it did not cross the line itself");
        }

        [Test]
        public void AnEnemyFpv_UsuallyKillsARifleSquadInTheOpen()
        {
            int dead = 0;
            for (int seed = 1; seed <= 10; seed++)
            {
                var b = Make(seed);
                Assert.IsTrue(b.TryPlace("rifle_squad", 30f, 150f, out var r, out _));
                b.QueueWave("fpv", 1, 0f, 1f);
                b.StartRound();
                Run(b, 30f);
                if (!r.Alive) dead++;
            }
            Assert.GreaterOrEqual(dead, 6, "dead in " + dead + " of 10");
        }

        [Test]
        public void AirDefence_WithRecon_ShootsDownAHelicopter_AndItsTroopsDieWithIt()
        {
            int downed = 0;
            for (int seed = 1; seed <= 10; seed++)
            {
                var b = Make(seed);
                Assert.IsTrue(b.TryPlace("recon_drone", 30f, 160f, out _, out _));
                Assert.IsTrue(b.TryPlace("air_defence", 30f, 290f, out _, out _));
                b.QueueWave("helicopter", 1, 0f, 1f);
                b.StartRound();
                Run(b, 60f);
                if (b.Stats["helicopter"].Killed > 0)
                {
                    downed++;
                    Assert.AreEqual(b.Stats["regular"].Spawned, b.Stats["regular"].Killed + CountAlive(b, "regular") + b.Stats["regular"].Crossed);
                }
            }
            Assert.GreaterOrEqual(downed, 5, "downed in " + downed + " of 10");
        }

        static int CountAlive(Battle b, string id)
        {
            int n = 0;
            foreach (var a in b.Attackers) if (a.Def.Id == id) n++;
            return n;
        }

        [Test]
        public void AOneWayDrone_FliesToTheRearAndHurtsTheLine()
        {
            var b = Make();
            b.QueueWave("one_way_drone", 1, 0f, 1f);
            b.StartRound();
            Run(b, 60f);
            Assert.AreEqual(10f, b.LineDamage, 1e-3f);
        }

        [Test]
        public void AHelicopter_IsInTheAir_ExceptWhileItSitsLettingItsTroopsOut()
        {
            // Two states (Yaroslav, 2026-11-16): flying, misses go into the sky; on the ground, it is a ground target.
            var b = Make();
            b.QueueWave("helicopter", 1, 0f, 1f);
            b.StartRound();
            bool flew = false, landed = false, flewAfter = false;
            for (float t = 0f; t < 60f && b.Outcome == BattleOutcome.Ongoing; t += 0.05f)
            {
                b.Step(0.05f);
                foreach (var a in b.Attackers)
                {
                    if (a.Def.Id != "helicopter") continue;
                    if (a.Landed) { landed = true; Assert.IsFalse(a.InAir); Assert.IsTrue(a.Dismounting); }
                    else if (!landed) { flew = true; Assert.IsTrue(a.InAir); }
                    else { flewAfter = true; Assert.IsTrue(a.InAir); }
                }
            }
            Assert.IsTrue(flew && landed && flewAfter, "flies in, lands, flies out");
        }
    }
}
