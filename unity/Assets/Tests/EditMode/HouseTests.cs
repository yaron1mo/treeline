using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Treeline.Sim;
using UnityEngine;

namespace Treeline.Tests
{
    // Yaroslav, 2026-11-27: villages with one-storey houses; the player sends one unit into a house.
    public class HouseTests
    {
        static string Data(string name) => File.ReadAllText(Path.Combine(Application.dataPath, "Data", name));

        static Pt[] Rect(float x, float y, float w, float h) =>
            new[] { new Pt(x, y), new Pt(x + w, y), new Pt(x + w, y + h), new Pt(x, y + h) };

        static Battle Make(out VisionMap vision)
        {
            var field = new Field { Name = "village", Width = 60f, Height = 300f };
            field.Features.Add(new Feature { Type = "house", Poly = Rect(25f, 240f, 10f, 8f) });
            var obstacles = ObstacleTable.Parse(Data("obstacles.csv"));
            vision = new VisionMap(field, obstacles);
            return new Battle(new NavGrid(field, obstacles), UnitTable.Parse(Data("units.csv")), 2000f,
                vision, ArmourTable.Parse(Data("armour.csv")), 1000f, 1, BattleRules.Parse(Data("battle_rules.csv")));
        }

        [Test]
        public void OneDefender_GoesIntoAHouse_AndASecondOneIsRefused()
        {
            var b = Make(out var vision);
            Assert.AreEqual(1, vision.Houses.Count);
            Assert.IsTrue(b.TryPlace("rifle_squad", 30f, 244f, out var first, out string why), why);
            Assert.AreEqual(0, first.House);
            Assert.AreEqual(30f, first.X, 0.01f);
            Assert.AreEqual(244f, first.Y, 0.01f);
            Assert.AreEqual(first.Id, b.HouseOccupant(0));
            Assert.IsFalse(b.TryPlace("rifle_squad", 31f, 245f, out _, out why));
            Assert.AreEqual("The house is taken", why);
        }

        [Test]
        public void TheDefenderInside_HasTheHouseCover_FromEverySide()
        {
            var b = Make(out var vision);
            Assert.IsTrue(b.TryPlace("rifle_squad", 30f, 244f, out var d, out string why), why);
            foreach (var s in new[] { (30f, 100f), (5f, 244f), (55f, 244f), (30f, 290f) })
            {
                var cover = vision.CoverFor(s.Item1, s.Item2, d.X, d.Y);
                Assert.IsNotNull(cover, $"no cover from {s}");
                Assert.AreEqual("house_inside", cover.Id);
            }
        }

        [Test]
        public void MovingOut_OrSelling_FreesTheHouse()
        {
            var b = Make(out _);
            Assert.IsTrue(b.TryPlace("rifle_squad", 30f, 244f, out var d, out string why), why);
            Assert.IsTrue(b.TryMove(d.Id, 30f, 280f, out why), why);
            Assert.AreEqual(-1, d.House);
            Assert.AreEqual(-1, b.HouseOccupant(0));
            Assert.IsTrue(b.TryPlace("machine_gun", 30f, 244f, out var mg, out why), why);
            Assert.IsTrue(b.TrySell(mg.Id, 0.5f, out _));
            Assert.AreEqual(-1, b.HouseOccupant(0));
        }

        [Test]
        public void SunflowersHide_ButDoNotCover_AndABurnedBeltDoesNotCover()
        {
            var t = ObstacleTable.Parse(Data("obstacles.csv"));
            Assert.IsTrue(t.TryGet("sunflower", out var sf));
            Assert.AreEqual(SightEffect.Soft, sf.Sight);
            Assert.Less(sf.DetectChance, 0.5f);
            Assert.IsTrue(string.IsNullOrEmpty(sf.CoverApplies));
            Assert.IsTrue(t.TryGet("burned_belt", out var bb));
            Assert.IsTrue(string.IsNullOrEmpty(bb.CoverApplies));
        }
    }
}
