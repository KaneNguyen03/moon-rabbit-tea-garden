using System;
using System.Collections.Generic;

namespace VuonTra.Core
{
    /// <summary>
    /// One firefly of the night: it appears at one side at <see cref="Appears"/>, crosses to the other side in
    /// <see cref="Crossing"/> seconds at height <see cref="Y"/>, wobbling up and down on a sine, and is gone after.
    /// Three of the swarm are golden and worth three.
    /// </summary>
    public struct Firefly
    {
        public int Index;
        public double Appears, Crossing;
        /// <summary>Across the screen, 0 left to 1 right; one of the two is at an edge.</summary>
        public double FromX, ToX;
        /// <summary>Up the screen, 0 bottom to 1 top.</summary>
        public double Y;
        /// <summary>How far the wobble carries it (screen heights), how many waves one crossing holds, and where in the wave it starts (0..1).</summary>
        public double Wobble, Waves, Phase;
        public bool Golden;
    }

    /// <summary>Where a fly is: across and up the screen, both 0 to 1.</summary>
    public struct FlySpot
    {
        public double X, Y;
    }

    /// <summary>What a finished night paid out.</summary>
    public sealed class FirefliesResult
    {
        public int Score, Stars;
        public BigNum Carrots;
        public int Clovers, Golden;
        /// <summary>The moon jar (#64) the day's three-star prize became, in place of the golden carrot: 0 or 1.</summary>
        public int Jars;
        /// <summary>Clovers or the golden carrot were earned but the day's cap held some or all of them back.</summary>
        public bool CloversCapped, GoldenCapped;
    }

    /// <summary>
    /// One night of catching fireflies: twenty seconds of a seeded swarm crossing the garden, each tap taking
    /// the nearest fly within reach. Where every fly is at any moment is a function of the schedule and the
    /// clock, never of frames, so the screen and the tests agree.
    /// </summary>
    public sealed class FirefliesRun
    {
        readonly bool[] _caught;

        public long Seed { get; }
        public IReadOnlyList<Firefly> Schedule { get; }
        public int Score { get; private set; }
        public int Caught { get; private set; }
        public int Golden { get; private set; }
        /// <summary>Taps that took nothing. They cost nothing; counted for the curious.</summary>
        public int Misses { get; private set; }

        public FirefliesRun(IReadOnlyList<Firefly> schedule, long seed = -1)
        {
            Schedule = schedule;
            Seed = seed;
            _caught = new bool[schedule.Count];
        }

        public static FirefliesRun FromSeed(long seed) => new FirefliesRun(Fireflies.ScheduleFor(seed), seed);

        public bool IsCaught(int index) => index >= 0 && index < _caught.Length && _caught[index];

        /// <summary>
        /// A tap at (<paramref name="x"/>, <paramref name="y"/>) at <paramref name="now"/>: the nearest fly that is out,
        /// not yet caught and within <see cref="Fireflies.CatchRadius"/> is caught and scored. Returns its index, or -1 for a miss.
        /// </summary>
        public int Catch(double x, double y, double now)
        {
            int best = -1;
            double bestDistance = Fireflies.CatchRadius * Fireflies.CatchRadius;
            for (int i = 0; i < Schedule.Count; i++)
            {
                if (_caught[i]) continue;
                Firefly fly = Schedule[i];
                if (!Fireflies.Visible(fly, now)) continue;
                FlySpot spot = Fireflies.PositionAt(fly, now);
                double dx = spot.X - x, dy = spot.Y - y, d = dx * dx + dy * dy;
                if (d <= bestDistance && (best < 0 || d < bestDistance))
                {
                    best = i;
                    bestDistance = d;
                }
            }
            if (best < 0)
            {
                Misses++;
                return -1;
            }
            _caught[best] = true;
            Caught++;
            if (Schedule[best].Golden) Golden++;
            Score += Fireflies.Points(Schedule[best]);
            return best;
        }
    }

    public static class Fireflies
    {
        public const double Seconds = 20d;
        /// <summary>Seconds before the first fly, so the player sees the dark garden and the jar.</summary>
        public const double Lead = 0.8;
        /// <summary>The swarm, and how many of it are golden.</summary>
        public const int Count = 24, GoldenCount = 3;
        /// <summary>A crossing takes between these; golden flies keep to the quick end.</summary>
        public const double MinCrossing = 4.0, MaxCrossing = 7.0;
        /// <summary>How near a tap must be, in screen widths, to take a fly.</summary>
        public const double CatchRadius = 0.09;
        public const int PlainPoints = 1, GoldenPoints = 3;
        /// <summary>Every fly caught: twenty-one at one and three at three.</summary>
        public const int MaxScore = (Count - GoldenCount) * PlainPoints + GoldenCount * GoldenPoints;
        /// <summary>A third, two thirds and seven eighths of the most.</summary>
        public const int OneStar = 10, TwoStars = 20, ThreeStars = 26;
        public const int PlaysPerDay = 3;
        /// <summary>One level above the basket, so the third game comes after the second.</summary>
        public const int UnlockLevel = 5;
        /// <summary>A full-score game pays this many seconds of income: a shorter game than the others, a smaller purse.</summary>
        public const double RewardSeconds = 20d;
        /// <summary>The prize is clovers: one for every ten points, three at most, and three a day in all.</summary>
        public const int PointsPerClover = 10, CloversMost = 3, CloverCapPerDay = 3;
        /// <summary>Three stars bring a golden carrot, once a day; the day's free moon jar in its place while that can still come, from garden level 15 (#64).</summary>
        public const int GoldenForThreeStars = 1, GoldenCapPerDay = 1;

        /// <summary>A number in [0, 1) from the seed and a salt, the same every time it is asked for.</summary>
        static double Roll(long seed, int salt)
        {
            ulong h = 1469598103934665603UL;
            unchecked
            {
                h ^= (ulong)seed; h *= 1099511628211UL;
                h ^= (ulong)salt; h *= 1099511628211UL;
                h ^= 0x466C7920UL; h *= 1099511628211UL; // "Fly ": not the leaf game's rolls
                h ^= h >> 29; h *= 0x9E3779B97F4A7C15UL; h ^= h >> 32;
            }
            return (h >> 11) / (double)(1UL << 53);
        }

        /// <summary>The seed of a night: the day and how many games came before it, so no two nights are the same.</summary>
        public static long SeedFor(long day, long gamesBefore) => day * 1000003L + gamesBefore;

        /// <summary>
        /// The swarm of one night, in order of appearing. Flies come about every half second with a little jitter,
        /// from either side at random, each at its own height with its own wobble; three of them, never the first,
        /// are golden and cross a little quicker.
        /// </summary>
        public static IReadOnlyList<Firefly> ScheduleFor(long seed)
        {
            var golden = new bool[Count];
            for (int i = 0; i < GoldenCount; i++) golden[i] = true;
            for (int i = Count - 1; i > 0; i--)
            {
                int j = (int)(Roll(seed, 1000 + i) * (i + 1));
                bool t = golden[i]; golden[i] = golden[j]; golden[j] = t;
            }
            if (golden[0])
                for (int i = 1; i < Count; i++)
                    if (!golden[i]) { golden[0] = false; golden[i] = true; break; }

            double gap = (Seconds - Lead - MaxCrossing - 0.2) / Count;
            var list = new Firefly[Count];
            for (int i = 0; i < Count; i++)
            {
                bool left = Roll(seed, 2000 + i) < 0.5;
                double crossing = MinCrossing + Roll(seed, 3000 + i) * (MaxCrossing - MinCrossing) * (golden[i] ? 0.4 : 1.0);
                list[i] = new Firefly
                {
                    Index = i,
                    Appears = Lead + i * gap + Roll(seed, 4000 + i) * 0.4 * gap,
                    Crossing = crossing,
                    FromX = left ? 0.02 : 0.98,
                    ToX = left ? 0.98 : 0.02,
                    Y = 0.15 + Roll(seed, 5000 + i) * 0.7,
                    Wobble = 0.03 + Roll(seed, 6000 + i) * 0.04,
                    Waves = 1.5 + Roll(seed, 7000 + i) * 2.0,
                    Phase = Roll(seed, 8000 + i),
                    Golden = golden[i],
                };
            }
            return list;
        }

        /// <summary>The fly is out: between appearing and reaching the far side.</summary>
        public static bool Visible(Firefly fly, double now) => now >= fly.Appears && now <= fly.Appears + fly.Crossing;

        /// <summary>Where the fly is at <paramref name="now"/>: straight across with a sine wobble up and down. Before and after its crossing it holds its end.</summary>
        public static FlySpot PositionAt(Firefly fly, double now)
        {
            double k = fly.Crossing <= 0d ? 1d : Math.Max(0d, Math.Min(1d, (now - fly.Appears) / fly.Crossing));
            double x = fly.FromX + (fly.ToX - fly.FromX) * k;
            double y = fly.Y + fly.Wobble * Math.Sin(2d * Math.PI * (fly.Waves * k + fly.Phase));
            return new FlySpot { X = Math.Max(0d, Math.Min(1d, x)), Y = Math.Max(0d, Math.Min(1d, y)) };
        }

        public static int Points(Firefly fly) => fly.Golden ? GoldenPoints : PlainPoints;

        public static int StarsFor(int score) => score >= ThreeStars ? 3 : (score >= TwoStars ? 2 : (score >= OneStar ? 1 : 0));

        public static bool Unlocked(GameState s) => Garden.Level(s) >= UnlockLevel;

        public static int PlaysLeft(GameState s, long day) => s.Fireflies.PlaysLeft(day, PlaysPerDay);

        public static bool CanPlay(GameState s, long day) => Unlocked(s) && PlaysLeft(s, day) > 0;

        /// <summary>Spends one of today's plays. False when none is left or the jar is not out yet.</summary>
        public static bool Start(GameState s, long day) => Unlocked(s) && s.Fireflies.Start(day, PlaysPerDay);

        /// <summary>Carrots for a score: twenty seconds of income at full score, by the same rule the other games use.</summary>
        public static BigNum Reward(IReadOnlyList<StationDef> defs, GameState s, Modifiers m, int score)
        {
            double share = Math.Max(0, Math.Min(MaxScore, score)) / (double)MaxScore;
            return Economy.IncomePerSecond(defs, s) * (m.IncomeMultiplier * m.SpeedMultiplier * RewardSeconds * share);
        }

        /// <summary>Clovers a score earns before the day's cap: one per ten points, three at most.</summary>
        public static int CloversFor(int score) => Math.Min(CloversMost, Math.Max(0, score) / PointsPerClover);

        /// <summary>
        /// Pays a finished night: carrots by the score, clovers by the score and a golden carrot for three stars,
        /// both held to the day's caps, and keeps the day's and the garden's counts.
        /// </summary>
        public static FirefliesResult Finish(IReadOnlyList<StationDef> defs, GameState s, Modifiers m, FirefliesRun run) => Finish(defs, s, m, run.Score);

        /// <summary>Pays a night that ended on this score; the run is only its score once it is over.</summary>
        public static FirefliesResult Finish(IReadOnlyList<StationDef> defs, GameState s, Modifiers m, int score)
        {
            MiniGameRecord record = s.Fireflies;
            var result = new FirefliesResult { Score = score, Stars = StarsFor(score) };
            result.Carrots = Reward(defs, s, m, score);
            s.Coins += result.Carrots;

            int clovers = CloversFor(score);
            result.Clovers = Math.Min(clovers, Math.Max(0, CloverCapPerDay - record.CloversToday));
            result.CloversCapped = clovers > result.Clovers;
            record.CloversToday += result.Clovers;
            s.Clovers += result.Clovers;

            int golden = result.Stars >= 3 ? GoldenForThreeStars : 0;
            int prize = Math.Min(golden, Math.Max(0, GoldenCapPerDay - record.GoldenToday));
            result.GoldenCapped = golden > prize;
            record.GoldenToday += prize;
            // The day's three-star prize is the day's free moon jar while that can still come (#64, from garden level 15); otherwise the golden carrot as before.
            if (prize > 0 && MoonJars.DailyWaiting(s, record.Day) && MoonJars.GiveFree(s, record.Day)) result.Jars = 1;
            else result.Golden = prize;
            s.GoldenCarrots += result.Golden;

            if (result.Stars >= 1) record.StarToday = true;
            record.Games++;
            if (result.Stars > record.BestStars) record.BestStars = result.Stars;
            return result;
        }
    }
}
