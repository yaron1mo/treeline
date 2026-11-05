using System.IO;
using NUnit.Framework;
using Treeline.Sim;
using UnityEngine;

namespace Treeline.Tests
{
    public class BattleTests
    {
        const string Units =
            "id,name,side,speed_mps,build_cost\n" +
            "regular,Regular soldier,attack,6,\n" +
            "machine_gun,Machine gunner,defence,0,200\n" +
            "rifle_squad,Rifle squad,defence,0,100\n";

        static Battle Open(float budget = 1000f)
        {
            var grid = new NavGrid(new Field { Name = "test", Width = 40f, Height = 100f });
            return new Battle(grid, UnitTable.Parse(Units), budget);
        }

        static Battle WithRiver(float budget = 1000f)
        {
            var field = new Field { Name = "test", Width = 40f, Height = 100f };
            field.Features.Add(new Feature { Type = "river", Width = 8f, Line = new[] { new Pt(-5, 70), new Pt(45, 70) } });
            return new Battle(new NavGrid(field), UnitTable.Parse(Units), budget);
        }

        static float SecondsToArrive(Battle b, Defender d, float x, float y)
        {
            float t = 0f;
            while (d.State == DefenderState.Running && t < 60f) { b.Step(0.05f); t += 0.05f; }
            return t;
        }

        [Test]
        public void Preparation_PlacingAndMovingAreInstant()
        {
            var b = Open();
            Assert.AreEqual(BattlePhase.Preparation, b.Phase);
            Assert.IsTrue(b.TryPlace("machine_gun", 20, 40, out var d, out _));
            Assert.AreEqual(DefenderState.Standing, d.State);
            Assert.AreEqual(20f, d.X);
            Assert.AreEqual(40f, d.Y);
            Assert.AreEqual(800f, b.Budget);
            Assert.IsTrue(b.TryMove(d.Id, 10, 30, out _));
            Assert.AreEqual(DefenderState.Standing, d.State);
            Assert.AreEqual(10f, d.X);
            Assert.AreEqual(800f, b.Budget, "moving is free");
        }

        [Test]
        public void RunSpeedIsThreeTimesARegularSoldier()
        {
            Assert.AreEqual(18f, Open().RunSpeedMps, 0.001f);
        }

        [Test]
        public void DuringARound_ANewDefenderEntersAtTheBottomAndRunsToItsSpot()
        {
            var b = Open();
            b.StartRound();
            Assert.IsTrue(b.TryPlace("machine_gun", 20, 40, out var d, out _));
            Assert.AreEqual(DefenderState.Running, d.State);
            Assert.Greater(d.Y, 95f, "starts at the bottom edge");
            // About 60 m at 18 m/s is a bit over 3 seconds.
            b.Step(1f);
            Assert.AreEqual(DefenderState.Running, d.State);
            b.Step(1f);
            Assert.AreEqual(DefenderState.Running, d.State);
            float t = 2f + SecondsToArrive(b, d, 20, 40);
            Assert.AreEqual(DefenderState.Standing, d.State);
            Assert.AreEqual(20f, d.X);
            Assert.AreEqual(40f, d.Y);
            Assert.Greater(t, 3f);
            Assert.Less(t, 4.5f);
        }

        [Test]
        public void DuringARound_AMovedDefenderRunsInAgainFromTheBottom()
        {
            var b = Open();
            Assert.IsTrue(b.TryPlace("rifle_squad", 20, 40, out var d, out _));
            b.StartRound();
            Assert.AreEqual(DefenderState.Standing, d.State);
            Assert.IsTrue(b.TryMove(d.Id, 30, 20, out _));
            Assert.AreEqual(DefenderState.Running, d.State);
            Assert.Greater(d.Y, 95f);
            SecondsToArrive(b, d, 30, 20);
            Assert.AreEqual(30f, d.X);
            Assert.AreEqual(20f, d.Y);
        }

        [Test]
        public void TheRunIsSlowedByTerrain()
        {
            var open = Open();
            open.StartRound();
            open.TryPlace("rifle_squad", 20, 30, out var a, out _);
            float tOpen = SecondsToArrive(open, a, 20, 30);

            var river = WithRiver();
            river.StartRound();
            river.TryPlace("rifle_squad", 20, 30, out var c, out _);
            float tRiver = SecondsToArrive(river, c, 20, 30);

            Assert.Greater(tRiver, tOpen + 0.3f, "wading the river costs time");
        }

        [Test]
        public void BadPlacementsAreRefused()
        {
            var field = new Field { Name = "test", Width = 40f, Height = 100f };
            field.Features.Add(new Feature { Type = "pond", Poly = new[] { new Pt(10, 60), new Pt(30, 60), new Pt(30, 80), new Pt(10, 80) } });
            var b = new Battle(new NavGrid(field), UnitTable.Parse(Units), 150f);
            Assert.IsFalse(b.TryPlace("machine_gun", 20, 40, out _, out string why1), "too expensive");
            StringAssert.Contains("budget", why1);
            Assert.IsFalse(b.TryPlace("regular", 20, 40, out _, out _), "an attacker is not a defender");
            Assert.IsFalse(b.TryPlace("nobody", 20, 40, out _, out _), "unknown unit");
            Assert.IsFalse(b.TryPlace("rifle_squad", 99, 40, out _, out _), "outside the field");
            Assert.IsFalse(b.TryPlace("rifle_squad", 20, 70, out _, out string why2), "in a pond");
            StringAssert.Contains("stand", why2);
            Assert.AreEqual(150f, b.Budget, "nothing was charged");
        }

        [Test]
        public void ShippedUnitTable_HasTheDefendersAndTheRunBaseUnit()
        {
            string path = Path.Combine(Application.dataPath, "Data", "units.csv");
            var t = UnitTable.Parse(File.ReadAllText(path));
            Assert.AreEqual(6f, t.Get("regular").SpeedMps);
            Assert.IsTrue(t.Get("machine_gun").IsDefence);
            Assert.Greater(t.Get("machine_gun").BuildCost, 0f);
            Assert.AreEqual("Machine gunner", t.Get("machine_gun").Name);
        }
    }
}
