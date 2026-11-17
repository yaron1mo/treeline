using System.IO;
using NUnit.Framework;
using Treeline.Sim;
using UnityEngine;

namespace Treeline.Tests
{
    // Yaroslav, 2026-11-17: the recon drone starts cheap and gets dearer with every one bought; the player can dig trenches.
    public class EconomyTests
    {
        static string Data(string name) => File.ReadAllText(Path.Combine(Application.dataPath, "Data", name));

        static Battle Make(out VisionMap vision, float budget = 1000f)
        {
            var field = new Field { Name = "t", Width = 60f, Height = 300f };
            var obstacles = ObstacleTable.Parse(Data("obstacles.csv"));
            vision = new VisionMap(field, obstacles);
            return new Battle(new NavGrid(field, obstacles), UnitTable.Parse(Data("units.csv")), budget,
                vision, ArmourTable.Parse(Data("armour.csv")), 1000f, 1, BattleRules.Parse(Data("battle_rules.csv")));
        }

        [Test]
        public void TheReconDrone_GetsDearerWithEveryOneBought_AndSellsForHalfWhatWasPaid()
        {
            var b = Make(out _);
            Assert.AreEqual(75f, b.CostOf("recon_drone"), 0.01f);
            Assert.IsTrue(b.TryPlace("recon_drone", 30f, 280f, out var first, out _));
            Assert.AreEqual(925f, b.Budget, 0.01f);
            Assert.AreEqual(100f, b.CostOf("recon_drone"), 0.01f);
            Assert.IsTrue(b.TryPlace("recon_drone", 20f, 280f, out var second, out _));
            Assert.AreEqual(825f, b.Budget, 0.01f);
            Assert.AreEqual(125f, b.CostOf("recon_drone"), 0.01f);

            // Selling gives back half of what that one cost; the price does not fall back.
            Assert.IsTrue(b.TrySell(second.Id, 0.5f, out float refund));
            Assert.AreEqual(50f, refund, 0.01f);
            Assert.AreEqual(125f, b.CostOf("recon_drone"), 0.01f);
            // Other units keep their price.
            Assert.AreEqual(b.CostOf("rifle_squad"), UnitTable.Parse(Data("units.csv")).Get("rifle_squad").BuildCost, 0.01f);
        }

        [Test]
        public void APercentIncrease_Compounds()
        {
            var u = new UnitDef { BuildCost = 80f, CostIncrease = 0.25f, CostIncreaseIsShare = true };
            Assert.AreEqual(80f, u.CostAfter(0), 0.01f);
            Assert.AreEqual(100f, u.CostAfter(1), 0.01f);
            Assert.AreEqual(125f, u.CostAfter(2), 0.01f);
            var t = UnitTable.Parse("id,side,build_cost,cost_increase\nx,defence,100,10%\ny,defence,100,30\n");
            Assert.AreEqual(121f, t.Get("x").CostAfter(2), 0.01f);
            Assert.AreEqual(160f, t.Get("y").CostAfter(2), 0.01f);
        }

        [Test]
        public void ADugTrench_CostsItsPrice_AndCoversWhoeverStandsInIt()
        {
            var b = Make(out var vision);
            Assert.AreEqual(70f, b.TrenchCost, 0.01f);
            Assert.IsNull(vision.CoverFor(30f, 100f, 30f, 250f), "open ground before digging");
            Assert.IsTrue(b.TryDig(30f, 250f, out string why), why);
            Assert.AreEqual(930f, b.Budget, 0.01f);
            Assert.AreEqual(1, b.Trenches.Count);
            var cover = vision.CoverFor(30f, 100f, 30f, 250f);
            Assert.IsNotNull(cover);
            Assert.AreEqual("trench", cover.Id);
            Assert.IsNull(vision.CoverFor(30f, 100f, 30f, 240f), "ten metres away is open ground again");
            Assert.IsFalse(b.TryDig(-5f, 250f, out _));
            var poor = Make(out _, 50f);
            Assert.IsFalse(poor.TryDig(30f, 250f, out why));
            Assert.AreEqual("Not enough budget", why);
        }
    }
}
