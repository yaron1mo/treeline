using System.IO;
using NUnit.Framework;
using Treeline.Sim;
using UnityEngine;

namespace Treeline.Tests
{
    public class RouteTests
    {
        static string Data(string name) => File.ReadAllText(Path.Combine(Application.dataPath, "Data", name));

        static Battle Make(Field field)
        {
            var table = ObstacleTable.Parse(Data("obstacles.csv"));
            return new Battle(new NavGrid(field, table), UnitTable.Parse(Data("units.csv")), 5000f, new VisionMap(field, table),
                ArmourTable.Parse(Data("armour.csv")), 100f, 3, BattleRules.Parse(Data("battle_rules.csv")));
        }

        static void RunWave(Battle b, string unit, int count)
        {
            b.BeginWave();
            b.QueueWave(unit, count, 0f, 1f);
            if (b.Phase == BattlePhase.Preparation) b.StartRound();
            for (float t = 0f; t < 120f && b.Outcome == BattleOutcome.Ongoing; t += 0.05f) b.Step(0.05f);
        }

        // Starts the wave and steps until the first attacker of it is on the field.
        static Attacker FirstOf(Battle b, string unit)
        {
            b.BeginWave();
            b.QueueWave(unit, 1, 0f, 1f);
            if (b.Phase == BattlePhase.Preparation) b.StartRound();
            for (int i = 0; i < 10 && b.Attackers.Count == 0; i++) b.Step(0.05f);
            return b.Attackers[0];
        }

        [Test]
        public void FirstWave_TakesTheFastestWay_NextWaveEvadesWhatItLearnt()
        {
            var b = Make(new Field { Name = "t", Width = 120f, Height = 160f });
            Assert.IsTrue(b.TryPlace("machine_gun", 60f, 90f, out var mg, out _));
            var first = FirstOf(b, "special_forces");
            Assert.AreEqual(RouteMode.Fastest, first.Mode, "wave 1 knows nothing");
            for (float t = 0f; t < 120f && b.Outcome == BattleOutcome.Ongoing; t += 0.05f) b.Step(0.05f);

            var second = FirstOf(b, "special_forces");   // lag 1: knows wave 1
            Assert.AreEqual(RouteMode.Evade, second.Mode);
            foreach (int cell in second.Path)
            {
                float dx = (cell % 60 + 0.5f) * 2f - 60f, dy = (cell / 60 + 0.5f) * 2f - 90f;
                Assert.Greater(dx * dx + dy * dy, 30f * 30f, "keeps out of the gunner's sight");
            }
        }

        [Test]
        public void SlowLearners_StillUseTheOldWay()
        {
            var b = Make(new Field { Name = "t", Width = 120f, Height = 160f });
            Assert.IsTrue(b.TryPlace("machine_gun", 60f, 90f, out _, out _));
            RunWave(b, "regular", 3);                                   // wave 1: the gunner is seen
            Assert.AreEqual(RouteMode.Fastest, FirstOf(b, "regular").Mode, "regulars lag 2 waves: wave 2 still knows nothing");
        }

        [Test]
        public void NoWayAround_TakesTheSafestWay()
        {
            var b = Make(new Field { Name = "t", Width = 40f, Height = 160f });
            Assert.IsTrue(b.TryPlace("machine_gun", 20f, 90f, out _, out _));   // its sight covers the whole width
            RunWave(b, "special_forces", 2);
            Assert.AreEqual(RouteMode.Safest, FirstOf(b, "special_forces").Mode);
        }

        [Test]
        public void ArmourIgnoresWeaponsThatCannotHurtIt()
        {
            var b = Make(new Field { Name = "t", Width = 120f, Height = 160f });
            Assert.IsTrue(b.TryPlace("rifle_squad", 60f, 90f, out _, out _));
            RunWave(b, "special_forces", 2);                             // the rifle squad is seen in wave 1
            Assert.AreEqual(RouteMode.Fastest, FirstOf(b, "mrap").Mode, "rifles do nothing to high armour, so no threat for an MRAP");
        }
    }
}
