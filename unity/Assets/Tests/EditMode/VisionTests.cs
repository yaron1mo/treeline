using System.IO;
using NUnit.Framework;
using Treeline.Sim;
using UnityEngine;

namespace Treeline.Tests
{
    public class VisionTests
    {
        const string Csv =
            "id,name,kind,sight,detect_chance,detect_falloff_per_m,on_sight_mult\n" +
            "rock,Rock,object,full,,,\n" +
            "bush,Bush,object,full,,,\n" +
            "belt,Forest belt,terrain,soft,0.5,0.03,\n" +
            "hill,Hill,terrain,full,,,1.3\n";

        static Pt[] Box(float x0, float y0, float x1, float y1) =>
            new[] { new Pt(x0, y0), new Pt(x1, y0), new Pt(x1, y1), new Pt(x0, y1) };

        static VisionMap Map(params Feature[] features)
        {
            var field = new Field { Name = "test", Width = 100f, Height = 60f };
            field.Features.AddRange(features);
            return new VisionMap(field, ObstacleTable.Parse(Csv));
        }

        [Test]
        public void OpenGround_SeesEverythingInsideTheRadiusAndNothingOutside()
        {
            var v = Map();
            Assert.AreEqual(1f, v.SeeChance(10, 30, 38, 30, 30f));
            Assert.AreEqual(0f, v.SeeChance(10, 30, 45, 30, 30f));
        }

        [Test]
        public void Rock_HidesWhatIsBehindItButNotItselfOrWhatIsBesideIt()
        {
            var v = Map(new Feature { Type = "rock", Poly = Box(23, 28, 27, 32) });
            Assert.AreEqual(0f, v.SeeChance(10, 30, 35, 30, 30f), "straight behind the rock");
            Assert.AreEqual(1f, v.SeeChance(10, 30, 25, 30, 30f), "the rock itself");
            Assert.AreEqual(1f, v.SeeChance(10, 30, 35, 45, 30f), "off to the side");
            Assert.AreEqual(1f, v.SeeChance(10, 30, 20, 30, 30f), "in front of the rock");
        }

        [Test]
        public void SmallRock_StillCastsAShadow()
        {
            var v = Map(new Feature { Type = "rock", Poly = Box(24.8f, 30.1f, 25.2f, 30.5f) });
            Assert.AreEqual(0f, v.SeeChance(10, 30, 35, 30, 30f));
        }

        [Test]
        public void Bush_HidesLikeARock()
        {
            var v = Map(new Feature { Type = "bush", Poly = Box(23, 28, 27, 32) });
            Assert.AreEqual(0f, v.SeeChance(10, 30, 35, 30, 30f));
        }

        [Test]
        public void ForestBelt_BlursAndTheChanceFallsWithDistanceBehindIt()
        {
            var v = Map(new Feature { Type = "belt", Line = new[] { new Pt(25, -10), new Pt(25, 70) }, Width = 6f });
            float near = v.SeeChance(5, 30, 31, 30, 30f);
            float far = v.SeeChance(5, 30, 34, 30, 30f);
            float inside = v.SeeChance(5, 30, 25, 30, 30f);
            Assert.Greater(near, 0f);
            Assert.Less(near, 1f);
            Assert.Less(far, near, "farther behind the belt is harder to see");
            Assert.Greater(inside, far, "inside the belt is blurred, and clearer than far behind it");
            Assert.Less(inside, 1f, "inside the belt is blurred");
            Assert.AreEqual(1f, v.SeeChance(5, 30, 20, 30, 30f), "in front of the belt is clear");
        }

        [Test]
        public void ObserverInsideABelt_SeesAnHourglass_ShortAlongTheBeltAndClearTowardsTheOpenGround()
        {
            var v = Map(new Feature { Type = "belt", Line = new[] { new Pt(-10, 30), new Pt(110, 30) }, Width = 6f });
            float along = v.SeeChance(50, 30, 62, 30, 30f);
            float alongFar = v.SeeChance(50, 30, 78, 30, 30f);
            Assert.Greater(along, 0f);
            Assert.Less(along, 1f);
            Assert.AreEqual(0f, alongFar, "trees all the way: nothing recognised that far along the belt");
            Assert.AreEqual(1f, v.SeeChance(50, 30, 50, 48, 30f), "clear towards the field on one side");
            Assert.AreEqual(1f, v.SeeChance(50, 30, 50, 12, 30f), "clear towards the field on the other side");
            Assert.AreEqual(1f, v.SeeChance(50, 30, 60, 42, 30f), "clear at an angle too");
        }

        [Test]
        public void Hill_GivesLongerSightToWhoStandsOnItAndDoesNotBlockThem()
        {
            var v = Map(new Feature { Type = "hill", Poly = Box(5, 20, 20, 40) });
            Assert.AreEqual(39f, v.SightRadius(10, 30, 30f), 0.001f);
            Assert.AreEqual(1f, v.SeeChance(10, 30, 48, 30, 30f), "38 m away, beyond the plain 30 m");
            Assert.AreEqual(0f, v.SeeChance(60, 30, 25, 30, 30f), "behind the hill from the other side");
        }

        [Test]
        public void Compute_MatchesSeeChanceAndStaysInsideTheRadius()
        {
            var v = Map(new Feature { Type = "rock", Poly = Box(23, 28, 27, 32) });
            var grid = v.Compute(10, 30, 30f);
            int behind = (int)(30 / NavGrid.CellSize) * v.Cols + (int)(35 / NavGrid.CellSize);
            int far = (int)(30 / NavGrid.CellSize) * v.Cols + (int)(60 / NavGrid.CellSize);
            Assert.AreEqual(0f, grid[behind]);
            Assert.AreEqual(0f, grid[far]);
            Assert.AreEqual(1f, grid[(int)(30 / NavGrid.CellSize) * v.Cols + (int)(18 / NavGrid.CellSize)]);
        }

        [Test]
        public void ShippedObstacleTable_HasTheAgreedRules()
        {
            string path = Path.Combine(Application.dataPath, "Data", "obstacles.csv");
            var t = ObstacleTable.Parse(File.ReadAllText(path));
            Assert.AreEqual(SightEffect.Full, t.Get("rock").Sight);
            Assert.AreEqual(SightEffect.Full, t.Get("bush").Sight);
            Assert.AreEqual(1f, t.Get("bush").CoverAcc[(int)ShooterClass.Troops], "a bush hides but gives no protection");
            Assert.Less(t.Get("wreck").CoverAcc[(int)ShooterClass.Troops], 1f);
            Assert.AreEqual(SightEffect.Soft, t.Get("belt").Sight);
            Assert.Greater(t.Get("hill").OnSightMult, 1f);
            Assert.IsTrue(float.IsPositiveInfinity(t.Get("pond").MoveCostInfantry.Value));
        }

        [Test]
        public void PlayerBuiltObstacles_FollowTheAgreedRules()
        {
            string path = Path.Combine(Application.dataPath, "Data", "obstacles.csv");
            var t = ObstacleTable.Parse(File.ReadAllText(path));
            var wire = t.Get("wire");
            var hedgehog = t.Get("hedgehog");
            var trench = t.Get("trench");
            Assert.IsTrue(wire.Buildable && hedgehog.Buildable && trench.Buildable);
            Assert.IsFalse(t.Get("rock").Buildable);
            Assert.Greater(wire.BuildCost, 0f);
            Assert.Greater(wire.MoveCostInfantry.Value, 1f, "wire slows infantry");
            Assert.IsFalse(float.IsPositiveInfinity(wire.MoveCostInfantry.Value), "infantry can always cut through wire");
            Assert.IsTrue(float.IsPositiveInfinity(hedgehog.MoveCostVehicle.Value), "a hedgehog stops vehicles");
            Assert.IsFalse(float.IsPositiveInfinity(hedgehog.MoveCostInfantry.Value), "infantry walk around a hedgehog");
            Assert.AreEqual(1f, trench.MoveCostInfantry.Value, "a trench blocks nothing");
        }

        [Test]
        public void CsvReader_HandlesQuotesAndLineEndings()
        {
            var rows = Treeline.Sim.Csv.Parse("a,b\r\n\"x, y\",\"he said \"\"hi\"\"\"\r\n");
            Assert.AreEqual(2, rows.Count);
            Assert.AreEqual("x, y", rows[1][0]);
            Assert.AreEqual("he said \"hi\"", rows[1][1]);
        }
    }
}
