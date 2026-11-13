using System.IO;
using NUnit.Framework;
using Treeline.Sim;
using UnityEngine;

namespace Treeline.Tests
{
    public class VehicleTests
    {
        static string Data(string name) => File.ReadAllText(Path.Combine(Application.dataPath, "Data", name));

        static Pt[] Box(float x0, float y0, float x1, float y1) =>
            new[] { new Pt(x0, y0), new Pt(x1, y0), new Pt(x1, y1), new Pt(x0, y1) };

        sealed class Setup
        {
            public NavGrid Grid;
            public VisionMap Vision;
            public Battle Battle;
        }

        static Setup Make(Field field, float lineHealth = 100f)
        {
            var table = ObstacleTable.Parse(Data("obstacles.csv"));
            var s = new Setup { Grid = new NavGrid(field, table), Vision = new VisionMap(field, table) };
            s.Battle = new Battle(s.Grid, UnitTable.Parse(Data("units.csv")), 5000f, s.Vision,
                ArmourTable.Parse(Data("armour.csv")), lineHealth, 1, BattleRules.Parse(Data("battle_rules.csv")));
            return s;
        }

        static void Run(Battle b, float seconds)
        {
            for (float t = 0f; t < seconds && b.Outcome == BattleOutcome.Ongoing; t += 0.05f) b.Step(0.05f);
        }

        [Test]
        public void LoadedVan_ReachingTheLine_HurtsItForTheVanAndEveryoneInside()
        {
            var s = Make(new Field { Name = "t", Width = 40f, Height = 100f });
            s.Battle.QueueWave("van", 1, 0f, 1f);
            s.Battle.StartRound();
            Run(s.Battle, 60f);
            Assert.AreEqual(2f + 12f, s.Battle.LineDamage, 1e-4f, "van 2 + 12 recruits x 1");
        }

        [Test]
        public void BadlyHitCarrier_StopsAndLetsItsTroopsOut()
        {
            var s = Make(new Field { Name = "t", Width = 40f, Height = 200f });
            Assert.IsTrue(s.Battle.TryPlace("machine_gun", 20f, 150f, out _, out _));
            s.Battle.QueueWave("van", 1, 0f, 1f);
            s.Battle.StartRound();
            bool sawDismounting = false, sawRecruit = false;
            for (float t = 0f; t < 60f && s.Battle.Outcome == BattleOutcome.Ongoing; t += 0.05f)
            {
                s.Battle.Step(0.05f);
                foreach (var a in s.Battle.Attackers)
                {
                    if (a.Def.Id == "van" && a.Dismounting) sawDismounting = true;
                    if (a.Def.Id == "recruit") sawRecruit = true;
                }
            }
            Assert.IsTrue(sawDismounting, "the van stopped to unload");
            Assert.IsTrue(sawRecruit, "troops got out");
            Assert.AreEqual(12, s.Battle.Stats["recruit"].Spawned, "every passenger is counted, out or dead inside");
            // Whether the van also dies is balance (since 2026-11-12 the unloaded recruits can kill the gunner first).
        }

        [Test]
        public void DestroyedTank_LeavesAWreckThatHidesAndBlocksVehicles()
        {
            var s = Make(new Field { Name = "t", Width = 40f, Height = 200f });
            Assert.IsTrue(s.Battle.TryPlace("anti_tank", 14f, 150f, out _, out _));
            Assert.IsTrue(s.Battle.TryPlace("anti_tank", 26f, 150f, out _, out _));
            s.Battle.QueueWave("old_tank", 1, 0f, 1f);
            s.Battle.StartRound();
            float x = 0f, y = 0f;
            int version = s.Grid.Version;
            for (float t = 0f; t < 80f && s.Battle.Outcome == BattleOutcome.Ongoing; t += 0.05f)
            {
                foreach (var a in s.Battle.Attackers) { x = a.X; y = a.Y; }
                s.Battle.Step(0.05f);
            }
            Assert.AreEqual(1, s.Battle.Stats["old_tank"].Killed, "two anti-tank teams kill an old tank");
            Assert.AreEqual(0f, s.Battle.LineDamage, 1e-4f);
            Assert.Greater(s.Grid.Version, version, "the ground changed");
            Assert.AreEqual("wreck", s.Vision.ObstacleAt(x, y).Id);
            Assert.IsTrue(float.IsPositiveInfinity(s.Grid.Cost(UnitClass.Vehicle)[s.Grid.CellAt(x, y)]), "vehicles cannot drive through it");
            Assert.IsFalse(float.IsPositiveInfinity(s.Grid.Cost(UnitClass.Infantry)[s.Grid.CellAt(x, y)]), "infantry can climb past it");
        }

        [Test]
        public void TreeMoveCost_LetsTanksPushThroughGroves()
        {
            var field = new Field { Name = "t", Width = 60f, Height = 60f };
            field.Features.Add(new Feature { Type = "grove", Poly = Box(10, 10, 50, 50) });
            var grid = new NavGrid(field, ObstacleTable.Parse(Data("obstacles.csv")));
            int mid = grid.CellAt(30f, 30f);
            Assert.IsTrue(float.IsPositiveInfinity(grid.Cost(UnitClass.Vehicle)[mid]), "ordinary vehicles cannot enter a grove");
            Assert.AreEqual(5f, grid.CostFor(UnitClass.Vehicle, 5f)[mid], "a tank pushes through at its tree_move_cost");
            Assert.IsTrue(ReferenceEquals(grid.Cost(UnitClass.Infantry), grid.CostFor(UnitClass.Infantry, 5f)), "infantry use their own cost");
        }

        [Test]
        public void ObstacleTable_DrivesMoveCosts_RocksBlockAndWireSlows()
        {
            var field = new Field { Name = "t", Width = 60f, Height = 60f };
            field.Features.Add(new Feature { Type = "rock", Poly = Box(10, 10, 14, 14) });
            field.Features.Add(new Feature { Type = "wire", Line = new[] { new Pt(30, 40), new Pt(50, 40) }, Width = 2f });
            field.Features.Add(new Feature { Type = "river", Line = new[] { new Pt(0, 25), new Pt(60, 25) }, Width = 4f });
            var plain = new NavGrid(field);
            var withTable = new NavGrid(field, ObstacleTable.Parse(Data("obstacles.csv")));
            int rock = withTable.CellAt(12f, 12f), wire = withTable.CellAt(40f, 40f), river = withTable.CellAt(30f, 25f);
            Assert.AreEqual(1f, plain.Cost(UnitClass.Infantry)[rock], "without the table objects do not affect movement");
            Assert.IsTrue(float.IsPositiveInfinity(withTable.Cost(UnitClass.Infantry)[rock]));
            Assert.AreEqual(6f, withTable.Cost(UnitClass.Infantry)[wire]);
            Assert.AreEqual(2f, withTable.Cost(UnitClass.Vehicle)[wire]);
            Assert.AreEqual(plain.Cost(UnitClass.Infantry)[river], withTable.Cost(UnitClass.Infantry)[river], "table and old numbers agree for terrain");
        }

        [Test]
        public void Mortar_HitsEveryoneAroundTheAimingPoint()
        {
            var s = Make(new Field { Name = "t", Width = 40f, Height = 100f });
            Assert.IsTrue(s.Battle.TryPlace("rifle_squad", 20f, 60f, out _, out _));   // spotter
            Assert.IsTrue(s.Battle.TryPlace("mortar", 20f, 95f, out _, out _));
            s.Battle.QueueWave("recruit", 6, 0f, 0.1f);
            s.Battle.StartRound();
            Run(s.Battle, 60f);
            Assert.Greater(s.Battle.Stats["mortar"].DamageDealt, 0f, "the mortar fired at what the rifle squad saw");
        }
    }
}
