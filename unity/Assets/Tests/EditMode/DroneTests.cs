using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Treeline.Sim;
using UnityEngine;

namespace Treeline.Tests
{
    public class DroneTests
    {
        static string Data(string name) => File.ReadAllText(Path.Combine(Application.dataPath, "Data", name));

        static Battle Make(float height = 200f)
        {
            var field = new Field { Name = "t", Width = 40f, Height = height };
            var table = ObstacleTable.Parse(Data("obstacles.csv"));
            return new Battle(new NavGrid(field, table), UnitTable.Parse(Data("units.csv")), 5000f, new VisionMap(field, table),
                ArmourTable.Parse(Data("armour.csv")), 100f, 1, BattleRules.Parse(Data("battle_rules.csv")));
        }

        // Places the operator at the rear and a recon drone in the middle, so the attackers are seen from the start.
        static Defender Setup(Battle b, string operatorId)
        {
            Assert.IsTrue(b.TryPlace("recon_drone", 20f, 60f, out _, out _));
            Assert.IsTrue(b.TryPlace(operatorId, 20f, 150f, out var op, out _));
            return op;
        }

        // Runs and reports when each drone first appeared and what it flew at.
        static List<KeyValuePair<float, Drone>> Run(Battle b, float seconds)
        {
            var launches = new List<KeyValuePair<float, Drone>>();
            var seen = new HashSet<int>();
            for (float t = 0f; t < seconds && b.Outcome == BattleOutcome.Ongoing; t += 0.05f)
            {
                b.Step(0.05f);
                foreach (var d in b.Drones)
                    if (seen.Add(d.Id)) launches.Add(new KeyValuePair<float, Drone>(b.RoundTime, d));
            }
            return launches;
        }

        [Test]
        public void TheTableHasTheThreeDroneRows()
        {
            var units = UnitTable.Parse(Data("units.csv"));
            Assert.AreEqual("aviation fast heavy infantry", units.Get("fpv_operator").TargetOrder);
            Assert.AreEqual(1, units.Get("fpv_operator").DroneAmmo);
            Assert.AreEqual(6, units.Get("bomber_operator").DroneAmmo);
            Assert.AreEqual(120f, units.Get("recon_drone").SightRangeM);
            Assert.AreEqual("fast", units.Get("motorbike").TargetClass);
            Assert.AreEqual("heavy", units.Get("old_tank").TargetClass);
        }

        [Test]
        public void TheOperatorLaunchesImmediately_AndGoesForTheFastTargetBeforeInfantry()
        {
            var b = Make();
            Setup(b, "fpv_operator");
            b.QueueWave("regular", 1, 0f, 1f);
            b.QueueWave("motorbike", 1, 0f, 1f);
            b.StartRound();
            var launches = Run(b, 20f);
            Assert.IsTrue(launches.Count > 0, "a drone was launched");
            Assert.IsTrue(launches[0].Key < 0.2f, "no separate launch delay: " + launches[0].Key);
            Assert.AreEqual("motorbike", launches[0].Value.Target.Def.Id);
        }

        [Test]
        public void AnFpvStrike_KillsARegular_AndTheDroneIsUsedUp()
        {
            var b = Make();
            Setup(b, "fpv_operator");
            b.QueueWave("regular", 1, 0f, 1f);
            b.StartRound();
            Run(b, 40f);
            Assert.AreEqual(1, b.Stats["regular"].Killed);
            Assert.AreEqual(0, b.Drones.Count);
            // FPV 70 x 0.9 x low-armour multiplier 1.0 exceeds Regular 10 HP.
            Assert.AreEqual(10f, b.Stats["fpv_operator"].DamageDealt, 1e-3f);
        }

        [Test]
        public void FpvCanFlyToAWaypointAndFindTargetWithOwnSensor_WithoutRecon()
        {
            var b = Make();
            Assert.IsTrue(b.TryPlace("fpv_operator", 20f, 150f, out var op, out _));
            Assert.AreEqual(20f, op.Def.SightRangeM, 0.01f);
            Assert.AreEqual(20f, op.Def.DroneSightRangeM, 0.01f);
            Assert.IsTrue(b.TrySetDroneWaypoint(op.Id, 20f, 80f, out _));
            b.QueueWave("recruit", 1, 0f, 1f);
            b.StartRound();

            // Recruit is >100 m from the operator initially; drone must launch without recon.
            var launches = Run(b, 8f);
            Assert.AreEqual(1, launches.Count, "FPV launched to waypoint without a visible target");
            Assert.IsTrue(launches[0].Value.UsesWaypoint);
            Assert.AreEqual(20f, launches[0].Value.WaypointX, 0.01f);
            Assert.AreEqual(80f, launches[0].Value.WaypointY, 0.01f);
            Assert.IsNull(launches[0].Value.Target, "still searching; recruit not in onboard range yet");

            Run(b, 40f);
            Assert.AreEqual(1, b.Stats["recruit"].Killed, "drone located and struck the recruit independently");
        }

        [Test]
        public void FpvWaypointRejectsOutOfBoundsAndWrongUnitType()
        {
            var b = Make();
            Assert.IsTrue(b.TryPlace("fpv_operator", 20f, 150f, out var op, out _));
            Assert.IsFalse(b.TrySetDroneWaypoint(op.Id, 50f, 80f, out _));
            Assert.IsFalse(b.TrySetDroneWaypoint(op.Id, 20f, -1f, out _));
            Assert.IsTrue(b.TryPlace("rifle_squad", 10f, 150f, out var soldier, out _));
            Assert.IsFalse(b.TrySetDroneWaypoint(soldier.Id, 20f, 80f, out _));
        }

        [Test]
        public void TheOperatorReloads_AfterDroneIsGone_NotWhileInFlight()
        {
            var b = Make();
            Setup(b, "fpv_operator");
            b.QueueWave("regular", 4, 0f, 1f);
            b.StartRound();
            var launches = Run(b, 60f);
            Assert.IsTrue(launches.Count >= 2, "launched " + launches.Count);
            for (int i = 1; i < launches.Count; i++)
                Assert.IsTrue(launches[i].Key - launches[i - 1].Key >= 3f - 0.05f,
                    "at least 3s between launch times: " + (launches[i].Key - launches[i - 1].Key));
        }

        [Test]
        public void FpvWeaponState_ReloadBeginsAfterShootdown()
        {
            var b = Make();
            Assert.IsTrue(b.TryPlace("fpv_operator", 20f, 150f, out var op, out _));
            Assert.IsTrue(b.TrySetDroneWaypoint(op.Id, 20f, 80f, out _));
            b.QueueWave("regular", 1, 0f, 1f);
            b.StartRound();
            b.Step(0.05f);
            Assert.AreEqual(DroneWeaponState.InFlight, op.DroneState);
            Assert.AreEqual(0f, op.ReloadLeft);
            Assert.AreEqual(1, op.ShotsFired);

            Assert.IsTrue(b.TryDestroyDrone(b.Drones[0].Id));
            Assert.AreEqual(DroneWeaponState.Reloading, op.DroneState);
            Assert.AreEqual(3f, op.ReloadLeft, 1e-4f);
            for (int i = 0; i < 55; i++) b.Step(0.05f);
            Assert.AreEqual(DroneWeaponState.Reloading, op.DroneState);
            for (int i = 0; i < 10; i++) b.Step(0.05f);
            Assert.AreEqual(DroneWeaponState.InFlight, op.DroneState);
            Assert.AreEqual(2, op.ShotsFired);
        }

        [Test]
        public void ADroneInTheAir_IsLostWhenItsOperatorDies()
        {
            var b = Make();
            var op = Setup(b, "fpv_operator");
            b.QueueWave("old_tank", 1, 0f, 1f);
            b.StartRound();
            for (float t = 0f; t < 10f && b.Drones.Count == 0; t += 0.05f) b.Step(0.05f);
            Assert.AreEqual(1, b.Drones.Count, "a drone is in the air");
            op.Alive = false;
            b.Step(0.05f);
            Assert.AreEqual(0, b.Drones.Count);
        }

        [Test]
        public void ABomber_DropsItsLoadOnATank()
        {
            var b = Make();
            Setup(b, "bomber_operator");
            b.QueueWave("old_tank", 1, 0f, 1f);
            b.StartRound();
            Run(b, 60f);
            // 25 x 0.5 x 0.6 (medium armour) = 7.5 per grenade, six grenades.
            Assert.IsTrue(b.Stats["old_tank"].DamageTaken >= 45f - 1e-2f, "tank took " + b.Stats["old_tank"].DamageTaken);
        }

        [Test]
        public void ARecon_ShowsTheAttackersFarAway_AndNothingShootsIt()
        {
            var b = Make();
            Assert.IsTrue(b.TryPlace("recon_drone", 20f, 100f, out _, out _));
            b.QueueWave("regular", 3, 0f, 1f);
            b.StartRound();
            bool seenFar = false;
            for (float t = 0f; t < 25f; t += 0.05f)
            {
                b.Step(0.05f);
                foreach (var a in b.Attackers) if (a.SeenChance > 0f && a.Y < 40f) seenFar = true;
            }
            Assert.IsTrue(seenFar, "seen from 60+ m away");
            Assert.IsTrue(b.Defenders.Count == 1 && b.Defenders[0].Hp == 8f, "the recon drone is untouched");
        }
    }
}
