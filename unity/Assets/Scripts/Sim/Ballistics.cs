using System;

namespace Treeline.Sim
{
    /// <summary>
    /// Turns the designer's accuracy into a spread, and the maths of a shot (Yaroslav, 2026-11-15).
    ///
    /// accuracy in units.csv means:
    /// - direct fire (bullets, tank rounds): the chance to hit a standing soldier (hit radius
    ///   <see cref="ReferenceHitRadiusM"/>) at half the weapon's range, standing still. From it comes an angular spread;
    ///   further away, against smaller or moving targets, or while moving yourself, the chance falls by itself.
    /// - area fire (mortar shells, bomber grenades): the chance that a round lands within its blast radius of the aim
    ///   point (mortar: at half its range; grenades: always, they fall from the same height). A weapon with spread_m
    ///   ignores accuracy: its rounds land in a zone of that radius, by rings (battle_rules spread_ring_shares).
    /// - guided (ATGM, FPV): the chance the guidance works; if not, the round lands a few metres off.
    /// </summary>
    public static class Ballistics
    {
        /// <summary>A standing soldier, the target accuracy is quoted against.</summary>
        public const float ReferenceHitRadiusM = 0.5f;

        static float Clamp(float p) => Math.Max(0.01f, Math.Min(0.999f, p));

        /// <summary>Inverse error function (Winitzki's approximation, good to about 0.2%).</summary>
        public static double ErfInv(double x)
        {
            const double a = 0.147;
            double ln = Math.Log(1.0 - x * x);
            double t = 2.0 / (Math.PI * a) + ln / 2.0;
            double r = Math.Sqrt(Math.Sqrt(t * t - ln / a) - t);
            return x < 0 ? -r : r;
        }

        /// <summary>Standard deviation of the shot's direction, in radians, so that a still shooter hits a standing
        /// soldier at half range with chance <paramref name="accuracy"/>.</summary>
        public static float AngularSpread(float accuracy, float rangeM)
        {
            float dRef = Math.Max(1f, rangeM / 2f);
            return (float)(ReferenceHitRadiusM / (dRef * Math.Sqrt(2.0) * ErfInv(Clamp(accuracy))));
        }

        /// <summary>Chance that one shot with this spread hits a still target of this size at this distance.</summary>
        public static float HitChance(float sigmaRad, float distanceM, float hitRadiusM)
        {
            if (distanceM <= hitRadiusM) return 1f;
            double s = sigmaRad * distanceM;
            if (s <= 0) return 1f;
            return (float)Erf(hitRadiusM / (s * Math.Sqrt(2.0)));
        }

        /// <summary>Standard deviation per axis of where an area round lands, so that it lands within
        /// <paramref name="radiusM"/> of the aim point with chance <paramref name="accuracy"/>.</summary>
        public static float GroundSpread(float accuracy, float radiusM)
        {
            float r = radiusM > 0f ? radiusM : ReferenceHitRadiusM;
            return (float)(r / Math.Sqrt(-2.0 * Math.Log(1.0 - Clamp(accuracy))));
        }

        /// <summary>
        /// Where an area round lands within a zone of radius <paramref name="radiusM"/> around the aim point, split into
        /// equally wide rings (inner to outer) with the given shares of rounds; uniform inside a ring. Returns the offset.
        /// </summary>
        public static void SampleZone(Random rng, float radiusM, float[] ringShares, out float dx, out float dy)
        {
            int n = ringShares != null && ringShares.Length > 0 ? ringShares.Length : 1;
            float total = 0f;
            for (int i = 0; i < n; i++) total += ringShares != null && ringShares.Length > 0 ? ringShares[i] : 1f;
            double pick = rng.NextDouble() * total;
            int ring = n - 1;
            for (int i = 0; i < n; i++)
            {
                float share = ringShares != null && ringShares.Length > 0 ? ringShares[i] : 1f;
                if (pick < share) { ring = i; break; }
                pick -= share;
            }
            double r0 = radiusM * ring / n, r1 = radiusM * (ring + 1) / n;
            double r = Math.Sqrt(rng.NextDouble() * (r1 * r1 - r0 * r0) + r0 * r0);   // uniform over the ring's area
            double a = rng.NextDouble() * 2.0 * Math.PI;
            dx = (float)(Math.Cos(a) * r);
            dy = (float)(Math.Sin(a) * r);
        }

        /// <summary>Error function (Abramowitz and Stegun 7.1.26).</summary>
        public static double Erf(double x)
        {
            double sign = x < 0 ? -1 : 1;
            x = Math.Abs(x);
            double t = 1.0 / (1.0 + 0.3275911 * x);
            double y = 1.0 - (((((1.061405429 * t - 1.453152027) * t) + 1.421413741) * t - 0.284496736) * t + 0.254829592) * t * Math.Exp(-x * x);
            return sign * y;
        }

        /// <summary>A normal random number (Box-Muller).</summary>
        public static double Gauss(Random rng)
        {
            double u1 = 1.0 - rng.NextDouble(), u2 = rng.NextDouble();
            return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
        }
    }
}
