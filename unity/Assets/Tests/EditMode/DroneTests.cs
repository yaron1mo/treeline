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

        // Places the operator at the rear and a recon drone near the enemy edge, so the attackers are seen from the
        // start (recon sight 40 m since 2026-11-17).
        static Defender Setup(Battle b, string operatorId)
        {
            Assert.IsTrue(b.TryPlace("recon_drone", 20f, 30f, out _, out _));
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
            Assert.AreEqual(40f, units.Get("recon_drone").SightRangeM);   // 120 until 2026-11-17 (Yaroslav: a third)
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
            // A long field with the operator far back: the tank must not reach and shoot it before all six grenades.
            var b = Make(400f);
            Assert.IsTrue(b.TryPlace("recon_drone", 20f, 190f, out _, out _));   // within the bomber's 133 m reach
            Assert.IsTrue(b.TryPlace("bomber_operator", 20f, 330f, out _, out _));
            b.QueueWave("old_tank", 1, 0f, 1f);
            b.StartRound();
            Run(b, 100f);   // the bomber reaches 133 m since 2026-11-17: the tank needs ~45 s to come into it
            // Six grenades of 25 x 0.2 (explosive vs medium armour) = at most 5 each; they land with a spread, so
            // most but not all hit the hull in full. A scratch, as intended (2026-11-15).
            float took = b.Stats["old_tank"].DamageTaken;
            Assert.IsTrue(took > 15f && took <= 30f + 1e-3f, "tank took " + took);
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
                foreach (var a in b.Attackers) if (a.SeenChance > 0f && a.Y < 70f) seenFar = true;
            }
            Assert.IsTrue(seenFar, "seen from 30+ m away (recon sight 40 m)");
            Assert.IsTrue(b.Defenders.Count == 1 && b.Defenders[0].Hp == 8f, "the recon drone is untouched");
        }

        [Test]
        public void AnOperator_NeverFiresDirectly_OnlyItsDronesHit()
        {
            // Regression 2026-11-13: with damage and attack_speed on its row the operator used to fire like a gun,
            // 600 m with blast, at anything the side saw, before its drone had even arrived.
            var b = Make();
            Setup(b, "fpv_operator");
            b.QueueWave("motorbike", 1, 0f, 1f);
            b.StartRound();
            for (float t = 0f; t < 1f; t += 0.05f) b.Step(0.05f);
            Assert.AreEqual(1, b.Attackers.Count, "the motorbike is still there");
            Assert.AreEqual(15f, b.Attackers[0].Hp, 1e-3f, "and untouched while the drone is on its way");
            Assert.AreEqual(1, b.Drones.Count, "a drone is in the air");
        }

        [Test]
        public void AWaypointFpv_AlsoGoesForWhatTheSideSees()
        {
            // The drone flies to a point far from the motorbike, so its own 20 m camera never sees it;
            // the recon drone does, and the FPV goes for it anyway.
            var b = Make();
            Assert.IsTrue(b.TryPlace("recon_drone", 20f, 60f, out _, out _));
            Assert.IsTrue(b.TryPlace("fpv_operator", 20f, 190f, out var op, out _));
            Assert.IsTrue(b.TrySetDroneWaypoint(op.Id, 20f, 170f, out _));
            b.QueueWave("motorbike", 1, 0f, 1f);
            b.StartRound();
            for (float t = 0f; t < 10f && b.Outcome == BattleOutcome.Ongoing; t += 0.05f) b.Step(0.05f);
            Assert.AreEqual(1, b.Stats["motorbike"].Killed, "killed well before it reached the waypoint");
        }

        // Attackers shoot our drones by the same rules we shoot theirs: the fpv row's targetable_by (Yaroslav, 2026-11-16).
        static int DronesLostAgainst(string attacker, int count)
        {
            int lost = 0;
            for (int seed = 1; seed <= 20; seed++)
            {
                var field = new Field { Name = "t", Width = 40f, Height = 300f };
                var table = ObstacleTable.Parse(Data("obstacles.csv"));
                var b = new Battle(new NavGrid(field, table), UnitTable.Parse(Data("units.csv")), 5000f, new VisionMap(field, table),
                    ArmourTable.Parse(Data("armour.csv")), 1000f, seed, BattleRules.Parse(Data("battle_rules.csv")));
                Assert.IsTrue(b.TryPlace("recon_drone", 20f, 60f, out _, out _));
                Assert.IsTrue(b.TryPlace("fpv_operator", 20f, 290f, out _, out _));
                b.QueueWave(attacker, count, 0f, 0.3f);
                b.StartRound();
                for (float t = 0f; t < 60f && b.Outcome == BattleOutcome.Ongoing; t += 0.05f) b.Step(0.05f);
                lost += b.Stats["fpv_operator"].DronesLost;
            }
            return lost;
        }

        [Test]
        public void TheirInfantry_CanShootDownOurFpv()
        {
            Assert.AreEqual("fpv", UnitTable.Parse(Data("units.csv")).Get("fpv_operator").DroneUnit);
            Assert.Greater(DronesLostAgainst("special_forces", 5), 0, "five special forces bring down at least one FPV in 20 runs");
        }

        [Test]
        public void ATank_CannotShootDownOurFpv()
        {
            Assert.AreEqual(0, DronesLostAgainst("modern_tank", 1), "explosive is not in the fpv row's targetable_by");
        }

        [Test]
        public void AShotDownDrone_StartsTheOperatorsReload()
        {
            var b = Make();
            var op = Setup(b, "fpv_operator");
            b.QueueWave("regular", 1, 0f, 1f);
            b.StartRound();
            for (float t = 0f; t < 10f && b.Drones.Count == 0; t += 0.05f) b.Step(0.05f);
            Assert.AreEqual(1, b.Drones.Count, "a drone is up");
            Assert.IsTrue(b.TryDestroyDrone(b.Drones[0].Id));
            Assert.IsFalse(op.DroneInFlight);
            Assert.AreEqual(3f, op.ReloadLeft, 0.01f, "next launch 1/attack_speed after it is lost");
        }

        [Test]
        public void AMissAtOurDrone_GoesIntoTheSky_NotIntoTheOperatorBehindIt()
        {
            // The operator 70 m off the route: no attacker ever sees it, its drones fly straight at them from it.
            float taken = 0f;
            int lost = 0;
            for (int seed = 1; seed <= 20; seed++)
            {
                var field = new Field { Name = "t", Width = 120f, Height = 300f };
                field.Spawns.Add(40f);
                var table = ObstacleTable.Parse(Data("obstacles.csv"));
                var b = new Battle(new NavGrid(field, table), UnitTable.Parse(Data("units.csv")), 10000f, new VisionMap(field, table),
                    ArmourTable.Parse(Data("armour.csv")), 1000f, seed, BattleRules.Parse(Data("battle_rules.csv")));
                Assert.IsTrue(b.TryPlace("fpv_operator", 110f, 280f, out _, out _));
                Assert.IsTrue(b.TryPlace("recon_drone", 65f, 100f, out _, out _));
                b.BeginWave();
                b.QueueWave("special_forces", 5, 0f, 0.3f);
                b.StartRound();
                while (b.Outcome == BattleOutcome.Ongoing && b.RoundTime < 400f) b.Step(0.05f);
                taken += b.Stats["fpv_operator"].DamageTaken;
                lost += b.Stats["fpv_operator"].DronesLost;
            }
            Assert.Greater(lost, 0, "they did shoot at the drones");
            Assert.AreEqual(0f, taken, "and never hit the operator");
        }
    }
}
