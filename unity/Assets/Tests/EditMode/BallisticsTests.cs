using System.IO;
using NUnit.Framework;
using Treeline.Sim;
using UnityEngine;

namespace Treeline.Tests
{
    // Shots as lines, spread from accuracy, rounds that fly (Yaroslav, 2026-11-15).
    public class BallisticsTests
    {
        static string Data(string name) => File.ReadAllText(Path.Combine(Application.dataPath, "Data", name));

        // A small table: a gun with a chosen accuracy, targets that barely move and do not shoot back.
        static UnitTable Table(float accuracy) => UnitTable.Parse(
            "id,side,type,hp,armour,speed_mps,damage_type,damage,attack_speed,range_m,accuracy,sight_range_m,behaviour,weapon_class,build_cost,blast_radius_m,projectile_speed_mps,hit_radius_m\n" +
            "regular,attack,troops,1000,none,0.2,,,,,,30,,,,,,0.5\n" +
            "gun,defence,troops,10,none,0,small_arms,1,5,400,"
            + accuracy.ToString(System.Globalization.CultureInfo.InvariantCulture) + ",400,static,small_arms,10,,800,0.5\n");

        static Battle Make(UnitTable units, float height = 300f, int seed = 1)
        {
            var field = new Field { Name = "t", Width = 60f, Height = height };
            var obstacles = ObstacleTable.Parse(Data("obstacles.csv"));
            return new Battle(new NavGrid(field, obstacles), units, 5000f, new VisionMap(field, obstacles), null, 1000f, seed);
        }

        [Test]
        public void Accuracy_IsTheChanceToHitAStandingSoldierAtHalfRange()
        {
            foreach (var p in new[] { 0.4f, 0.7f, 0.95f })
            {
                float sigma = Ballistics.AngularSpread(p, 700f);
                Assert.AreEqual(p, Ballistics.HitChance(sigma, 350f, 0.5f), 0.01f, "accuracy " + p);
                Assert.Less(Ballistics.HitChance(sigma, 700f, 0.5f), p, "further away it is lower");
                Assert.Greater(Ballistics.HitChance(sigma, 350f, 2.5f), p, "a vehicle is easier to hit");
            }
            float g = Ballistics.GroundSpread(0.4f, 10f);
            float within = 1f - (float)System.Math.Exp(-100.0 / (2.0 * g * g));
            Assert.AreEqual(0.4f, within, 0.01f, "area fire: 40% land within the blast radius");
        }

        static float HitsPerShot(float accuracy, int count)
        {
            int hits = 0, shots = 0;
            for (int seed = 1; seed <= 8; seed++)
            {
                var b = Make(Table(accuracy), 300f, seed);
                Assert.IsTrue(b.TryPlace("gun", 30f, 290f, out _, out _));
                b.QueueWave("regular", count, 0f, 0.5f);   // 0.2 m/s, 0.5 s apart: a tight column coming straight on
                b.StartRound();
                for (float t = 0f; t < 20f; t += 0.05f) b.Step(0.05f);
                var s = b.Stats["gun"];
                hits += s.ShotsHit; shots += s.ShotsFired;
            }
            return (float)hits / shots;
        }

        [Test]
        public void AMissCanHitTheOneBehind_SoAnInaccurateGunDoesBetterAgainstAGroup()
        {
            float single = HitsPerShot(0.2f, 1);
            float group = HitsPerShot(0.2f, 10);
            Assert.Greater(group, single * 1.3f, "single " + single + ", group " + group);
        }

        [Test]
        public void ABullet_HitsWhenItArrives_NotWhenItIsFired()
        {
            var b = Make(Table(0.99f));
            Assert.IsTrue(b.TryPlace("gun", 30f, 290f, out _, out _));
            b.QueueWave("regular", 1, 0f, 1f);
            b.StartRound();
            bool checkedOne = false;
            for (float t = 0f; t < 10f && !checkedOne; t += 0.05f)
            {
                b.Step(0.05f);
                foreach (var tr in b.ShotTraces)
                {
                    float d = (float)System.Math.Sqrt((tr.ToX - tr.FromX) * (tr.ToX - tr.FromX) + (tr.ToY - tr.FromY) * (tr.ToY - tr.FromY));
                    Assert.AreEqual(d / 800f, tr.ArrivesAt - tr.FiredAt, 1e-3f);
                    checkedOne = true;
                }
            }
            Assert.IsTrue(checkedOne, "a shot was fired");
        }

        [Test]
        public void AMortarShell_FliesAndLandsLater_AheadOfAMovingTarget()
        {
            var units = UnitTable.Parse(Data("units.csv"));
            var field = new Field { Name = "t", Width = 60f, Height = 400f };
            var obstacles = ObstacleTable.Parse(Data("obstacles.csv"));
            var b = new Battle(new NavGrid(field, obstacles), units, 5000f, new VisionMap(field, obstacles),
                ArmourTable.Parse(Data("armour.csv")), 1000f, 3);
            Assert.IsTrue(b.TryPlace("recon_drone", 30f, 100f, out _, out _));
            Assert.IsTrue(b.TryPlace("mortar", 30f, 390f, out _, out _));
            b.QueueWave("old_tank", 1, 0f, 1f);
            b.StartRound();
            Projectile shell = null;
            float targetYAtFire = 0f;
            for (float t = 0f; t < 30f && shell == null; t += 0.05f)
            {
                b.Step(0.05f);
                if (b.Projectiles.Count > 0) { shell = b.Projectiles[0]; targetYAtFire = b.Attackers[0].Y; }
            }
            Assert.IsTrue(shell != null, "a shell is in the air");
            Assert.Greater(shell.LandsAt - shell.LaunchedAt, 1f, "it flies for seconds");
            Assert.AreEqual(ProjectileKind.Shell, shell.Kind);
            int lands = 0;
            float ahead = 0f;
            for (int seed = 1; seed <= 30; seed++)
            {
                var c = new Battle(new NavGrid(field, obstacles), units, 5000f, new VisionMap(field, obstacles),
                    ArmourTable.Parse(Data("armour.csv")), 1000f, seed);
                c.TryPlace("recon_drone", 30f, 100f, out _, out _);
                c.TryPlace("mortar", 30f, 390f, out _, out _);
                c.QueueWave("old_tank", 1, 0f, 1f);
                c.StartRound();
                for (float t = 0f; t < 30f; t += 0.05f)
                {
                    c.Step(0.05f);
                    if (c.Projectiles.Count > 0 && c.Attackers.Count > 0)
                    {
                        ahead += c.Projectiles[0].ToY - c.Attackers[0].Y;
                        lands++;
                        break;
                    }
                }
            }
            Assert.Greater(ahead / lands, 3f, "on average it lands ahead of where the tank was when fired (it leads)");
        }
    }
}
