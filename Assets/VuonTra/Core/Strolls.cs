using System;
using System.Collections.Generic;

namespace VuonTra.Core
{
    /// <summary>A point of the garden, in whatever units the screen uses. Here so the core stays free of the engine.</summary>
    public struct Vec2
    {
        public float X, Y;
        public Vec2(float x, float y) { X = x; Y = y; }
        public static float Distance(Vec2 a, Vec2 b) => (float)Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));
    }

    /// <summary>What a rabbit does at the empty spot it strolled to.</summary>
    public enum StrollActivity { Tea, Firefly, Doze }

    /// <summary>
    /// Resting rabbits stroll to empty spots of the lawn and do small things there (#55). Where one may go is a pure
    /// choice over the spots it is given, so the screen can hand it its stroll points today and the empty tiles of the
    /// hex grid (#52) later.
    /// </summary>
    public static class Strolls
    {
        /// <summary>No spot within this of a blocked point (a station, another stroller's spot) is walked to.</summary>
        public const float Clearance = 200f;
        /// <summary>The straight way to a spot keeps this far from a blocked point.</summary>
        public const float PathClearance = 120f;
        /// <summary>The rest at the spot lasts between these.</summary>
        public const float LingerLeast = 4f, LingerMost = 6f;

        /// <summary>
        /// The spot to stroll to from <paramref name="from"/>: one of <paramref name="spots"/> within <paramref name="radius"/>,
        /// not within <see cref="Clearance"/> of any blocked point, with a straight way there that keeps <see cref="PathClearance"/>
        /// from every blocked point beyond the start; <paramref name="roll"/> in [0, 1] picks among those that qualify, in order.
        /// Returns its index in <paramref name="spots"/>, or -1 when none qualifies.
        /// </summary>
        public static int PickSpot(IReadOnlyList<Vec2> spots, IReadOnlyList<Vec2> blocked, Vec2 from, float radius, double roll)
        {
            var open = new List<int>();
            for (int i = 0; i < spots.Count; i++)
                if (Vec2.Distance(from, spots[i]) <= radius && Clear(spots[i], blocked) && WayClear(from, spots[i], blocked)) open.Add(i);
            if (open.Count == 0) return -1;
            int k = (int)(Math.Max(0d, Math.Min(1d, roll)) * open.Count);
            return open[Math.Min(k, open.Count - 1)];
        }

        static bool Clear(Vec2 spot, IReadOnlyList<Vec2> blocked)
        {
            for (int b = 0; b < blocked.Count; b++) if (Vec2.Distance(spot, blocked[b]) < Clearance) return false;
            return true;
        }

        /// <summary>The segment from <paramref name="a"/> to <paramref name="b"/> keeps clear of every blocked point that does not stand at the start itself.</summary>
        static bool WayClear(Vec2 a, Vec2 b, IReadOnlyList<Vec2> blocked)
        {
            float dx = b.X - a.X, dy = b.Y - a.Y, len2 = dx * dx + dy * dy;
            for (int i = 0; i < blocked.Count; i++)
            {
                Vec2 p = blocked[i];
                if (Vec2.Distance(a, p) < Clearance) continue; // the seat's own neighbours: the rabbit starts among them
                float t = len2 <= 0f ? 0f : Math.Max(0f, Math.Min(1f, ((p.X - a.X) * dx + (p.Y - a.Y) * dy) / len2));
                if (Vec2.Distance(new Vec2(a.X + dx * t, a.Y + dy * t), p) < PathClearance) return false;
            }
            return true;
        }

        /// <summary>A number in [0, 1) for one question about one rabbit's stroll, the same every time it is asked.</summary>
        public static double Roll(int seat, int stroll, int salt)
        {
            ulong h = 1469598103934665603UL;
            unchecked
            {
                h ^= (ulong)(uint)seat; h *= 1099511628211UL;
                h ^= (ulong)(uint)stroll; h *= 1099511628211UL;
                h ^= (ulong)(uint)salt; h *= 1099511628211UL;
                h ^= 0x5374726FUL; h *= 1099511628211UL; // "Stro": not another game's rolls
                h ^= h >> 29; h *= 0x9E3779B97F4A7C15UL; h ^= h >> 32;
            }
            return (h >> 11) / (double)(1UL << 53);
        }

        /// <summary>How many rabbits may be out on the lawn at once: one for every three empty spots.</summary>
        public static int MaxAtOnce(int emptySpots) => Math.Max(0, emptySpots) / 3;

        /// <summary>What the rabbit does at its spot: tea, a firefly to chase (by night only) or a doze, by the roll.</summary>
        public static StrollActivity ActivityFor(double roll, bool night)
        {
            if (night) return roll < 1d / 3d ? StrollActivity.Tea : (roll < 2d / 3d ? StrollActivity.Firefly : StrollActivity.Doze);
            return roll < 0.5 ? StrollActivity.Tea : StrollActivity.Doze;
        }

        /// <summary>Seconds spent at the spot: four to six, by the roll.</summary>
        public static float LingerFor(double roll) => LingerLeast + (LingerMost - LingerLeast) * (float)Math.Max(0d, Math.Min(1d, roll));
    }
}
