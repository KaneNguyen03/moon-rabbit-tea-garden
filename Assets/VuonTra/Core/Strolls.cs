using System;
using System.Collections.Generic;

namespace VuonTra.Core
{
    public struct Vec2
    {
        public float X, Y;
        public Vec2(float x, float y) { X = x; Y = y; }
        public static float Distance(Vec2 a, Vec2 b) => (float)Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));
    }

    public enum StrollActivity { Tea, Firefly, Doze }

    public static class Strolls
    {
        public const float Clearance = 200f;
        public const float PathClearance = 120f;
        public const float LingerLeast = 4f, LingerMost = 6f;

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

        static bool WayClear(Vec2 a, Vec2 b, IReadOnlyList<Vec2> blocked)
        {
            float dx = b.X - a.X, dy = b.Y - a.Y, len2 = dx * dx + dy * dy;
            for (int i = 0; i < blocked.Count; i++)
            {
                Vec2 p = blocked[i];
                if (Vec2.Distance(a, p) < Clearance) continue;
                float t = len2 <= 0f ? 0f : Math.Max(0f, Math.Min(1f, ((p.X - a.X) * dx + (p.Y - a.Y) * dy) / len2));
                if (Vec2.Distance(new Vec2(a.X + dx * t, a.Y + dy * t), p) < PathClearance) return false;
            }
            return true;
        }

        const ulong StreamApartFromOtherGames = 0x5374726FUL;

        public static double Roll(int seat, int stroll, int salt)
        {
            ulong h = 1469598103934665603UL;
            unchecked
            {
                h ^= (ulong)(uint)seat; h *= 1099511628211UL;
                h ^= (ulong)(uint)stroll; h *= 1099511628211UL;
                h ^= (ulong)(uint)salt; h *= 1099511628211UL;
                h ^= StreamApartFromOtherGames; h *= 1099511628211UL;
                h ^= h >> 29; h *= 0x9E3779B97F4A7C15UL; h ^= h >> 32;
            }
            return (h >> 11) / (double)(1UL << 53);
        }

        public static int MaxAtOnce(int emptySpots) => Math.Max(0, emptySpots) / 3;

        public static StrollActivity ActivityFor(double roll, bool night)
        {
            if (night) return roll < 1d / 3d ? StrollActivity.Tea : (roll < 2d / 3d ? StrollActivity.Firefly : StrollActivity.Doze);
            return roll < 0.5 ? StrollActivity.Tea : StrollActivity.Doze;
        }

        public static float LingerFor(double roll) => LingerLeast + (LingerMost - LingerLeast) * (float)Math.Max(0d, Math.Min(1d, roll));
    }
}
