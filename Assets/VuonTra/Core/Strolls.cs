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

    public readonly struct BreakPost
    {
        public readonly int Station;
        public readonly long Rabbit;
        public readonly Vec2 Home;
        public BreakPost(int station, long rabbit, Vec2 home) { Station = station; Rabbit = rabbit; Home = home; }
    }

    public enum BreakLeg { Home, Out, There, Back }

    public sealed class Break
    {
        public int Station, Number, Spot = -1;
        public long Rabbit;
        public double Start;
        public float Linger;
        public StrollActivity Activity;
        public bool Granted;
        public Vec2 From, To;
        public double Back => Start + 2d * Breaks.Walk + Linger;
        public bool Covers(double clock) => Granted && clock >= Start && clock < Back;

        public BreakLeg LegAt(double clock, out double along)
        {
            along = 0d;
            if (!Covers(clock)) return BreakLeg.Home;
            double e = clock - Start;
            if (e < Breaks.Walk) { along = e / Breaks.Walk; return BreakLeg.Out; }
            if (e < Breaks.Walk + Linger) { along = (e - Breaks.Walk) / Linger; return BreakLeg.There; }
            along = Math.Min(1d, (e - Breaks.Walk - Linger) / Breaks.Walk);
            return BreakLeg.Back;
        }
    }

    public sealed class Breaks
    {
        public const int AtOnce = 2;
        public const float Every = 180f, Reach = 300f, Walk = 3f, Late = Walk;
        public const float Longest = 2f * Walk + Strolls.LingerMost;
        const int SpotSalt = 10, ActivitySalt = 11, LingerSalt = 12, StartSalt = 13;

        readonly Dictionary<int, Break> _last = new Dictionary<int, Break>();

        public static int NumberAt(double clock) => (int)Math.Floor(clock / Every);

        public static double StartOf(int station, int number) => number * (double)Every + Strolls.Roll(station, number, StartSalt) * (Every - Longest);

        public Break Of(int station, long rabbit, double clock) => _last.TryGetValue(station, out Break b) && b.Rabbit == rabbit && b.Covers(clock) ? b : null;

        public int OutAt(double clock)
        {
            int n = 0;
            foreach (Break b in _last.Values) if (b.Covers(clock)) n++;
            return n;
        }

        public void Forget() => _last.Clear();

        public static void PostsOf(IReadOnlyList<StationDef> defs, GameState s, Func<int, Vec2> home, List<BreakPost> into)
        {
            into.Clear();
            for (int i = 0; i < defs.Count && i < s.Levels.Length; i++)
            {
                if (s.Levels[i] <= 0 || i >= s.Plots || Layout.IsStored(defs, s, i)) continue;
                Rabbit worker = Rabbits.AtStation(s, i);
                if (worker != null) into.Add(new BreakPost(i, worker.Id, home(i)));
            }
        }

        public bool Due(double clock, IReadOnlyList<BreakPost> posts)
        {
            int n = NumberAt(clock);
            for (int i = 0; i < posts.Count; i++) if (DueNow(posts[i], n, clock)) return true;
            return false;
        }

        bool DueNow(BreakPost p, int n, double clock) => StartOf(p.Station, n) <= clock && !(_last.TryGetValue(p.Station, out Break was) && was.Number == n);

        public void Plan(double clock, IReadOnlyList<BreakPost> posts, IReadOnlyList<Vec2> spots, IReadOnlyList<Vec2> blocked, bool night, bool still)
        {
            int n = NumberAt(clock);
            List<BreakPost> due = null;
            for (int i = 0; i < posts.Count; i++)
                if (DueNow(posts[i], n, clock)) (due ?? (due = new List<BreakPost>())).Add(posts[i]);
            if (due == null) return;
            due.Sort((a, b) =>
            {
                int byStart = StartOf(a.Station, n).CompareTo(StartOf(b.Station, n));
                return byStart != 0 ? byStart : a.Station.CompareTo(b.Station);
            });
            foreach (BreakPost p in due)
            {
                var b = new Break
                {
                    Station = p.Station, Rabbit = p.Rabbit, Number = n, Start = StartOf(p.Station, n),
                    Linger = Strolls.LingerFor(Strolls.Roll(p.Station, n, LingerSalt)),
                    Activity = Strolls.ActivityFor(Strolls.Roll(p.Station, n, ActivitySalt), night),
                };
                var taken = new List<Vec2>(blocked);
                int away = 0;
                foreach (Break other in _last.Values)
                {
                    if (!other.Covers(b.Start)) continue;
                    away++;
                    taken.Add(other.To);
                }
                if (!still && clock - b.Start <= Late && away < AtOnce && posts.Count - away >= 2)
                    b.Spot = Strolls.PickSpot(spots, taken, p.Home, Reach, Strolls.Roll(p.Station, n, SpotSalt));
                b.Granted = b.Spot >= 0;
                b.From = p.Home;
                if (b.Granted) b.To = spots[b.Spot];
                _last[p.Station] = b;
            }
        }
    }
}
