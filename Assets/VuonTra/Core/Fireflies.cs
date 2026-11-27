using System;
using System.Collections.Generic;

namespace VuonTra.Core
{
    public struct Firefly
    {
        public int Index;
        public double Appears, Crossing;
        public double FromX, ToX;
        public double Y;
        public double Wobble, Waves, Phase;
        public bool Golden;
    }

    public struct FlySpot
    {
        public double X, Y;
    }

    public sealed class FirefliesResult
    {
        public int Score, Stars;
        public BigNum Carrots;
        public int Clovers, Golden;
        public int Jars;
        public bool CloversCapped, GoldenCapped;
    }

    public sealed class FirefliesRun
    {
        readonly bool[] _caught;

        public long Seed { get; }
        public IReadOnlyList<Firefly> Schedule { get; }
        public int Score { get; private set; }
        public int Caught { get; private set; }
        public int Golden { get; private set; }
        public int Misses { get; private set; }

        public FirefliesRun(IReadOnlyList<Firefly> schedule, long seed = -1)
        {
            Schedule = schedule;
            Seed = seed;
            _caught = new bool[schedule.Count];
        }

        public static FirefliesRun FromSeed(long seed) => new FirefliesRun(Fireflies.ScheduleFor(seed), seed);

        public bool IsCaught(int index) => index >= 0 && index < _caught.Length && _caught[index];

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
        public const double Lead = 0.8;
        public const int Count = 24, GoldenCount = 3;
        public const double MinCrossing = 4.0, MaxCrossing = 7.0;
        public const double CatchRadius = 0.09;
        public const int PlainPoints = 1, GoldenPoints = 3;
        public const int MaxScore = (Count - GoldenCount) * PlainPoints + GoldenCount * GoldenPoints;
        public const int OneStar = 10, TwoStars = 20, ThreeStars = 26;
        public const int PlaysPerDay = 3;
        public const int UnlockLevel = 9;
        public const int KeptFromLevel = 5;
        public const double RewardSeconds = 20d;
        public const int PointsPerClover = 10, CloversMost = 3, CloverCapPerDay = 3;
        public const int GoldenForThreeStars = 1, GoldenCapPerDay = 1;

        const ulong StreamApartFromOtherGames = 0x466C7920UL;

        static double Roll(long seed, int salt)
        {
            ulong h = 1469598103934665603UL;
            unchecked
            {
                h ^= (ulong)seed; h *= 1099511628211UL;
                h ^= (ulong)salt; h *= 1099511628211UL;
                h ^= StreamApartFromOtherGames; h *= 1099511628211UL;
                h ^= h >> 29; h *= 0x9E3779B97F4A7C15UL; h ^= h >> 32;
            }
            return (h >> 11) / (double)(1UL << 53);
        }

        public static long SeedFor(long day, long gamesBefore) => day * 1000003L + gamesBefore;

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

        public static bool Visible(Firefly fly, double now) => now >= fly.Appears && now <= fly.Appears + fly.Crossing;

        public static FlySpot PositionAt(Firefly fly, double now)
        {
            double k = fly.Crossing <= 0d ? 1d : Math.Max(0d, Math.Min(1d, (now - fly.Appears) / fly.Crossing));
            double x = fly.FromX + (fly.ToX - fly.FromX) * k;
            double y = fly.Y + fly.Wobble * Math.Sin(2d * Math.PI * (fly.Waves * k + fly.Phase));
            return new FlySpot { X = Math.Max(0d, Math.Min(1d, x)), Y = Math.Max(0d, Math.Min(1d, y)) };
        }

        public static int Points(Firefly fly) => fly.Golden ? GoldenPoints : PlainPoints;

        public static int StarsFor(int score) => score >= ThreeStars ? 3 : (score >= TwoStars ? 2 : (score >= OneStar ? 1 : 0));

        public static bool Unlocked(GameState s)
            => Garden.Level(s) >= UnlockLevel || (Garden.Level(s) >= KeptFromLevel && (s.Fireflies.Games > 0 || s.IntrosSeen.Contains(Intros.Fireflies)));

        public static int PlaysLeft(GameState s, long day) => s.Fireflies.PlaysLeft(day, PlaysPerDay);

        public static bool CanPlay(GameState s, long day) => Unlocked(s) && PlaysLeft(s, day) > 0;

        public static bool Start(GameState s, long day) => Unlocked(s) && s.Fireflies.Start(day, PlaysPerDay);

        public static BigNum Reward(IReadOnlyList<StationDef> defs, GameState s, Modifiers m, int score)
        {
            double share = Math.Max(0, Math.Min(MaxScore, score)) / (double)MaxScore;
            return Economy.IncomePerSecond(defs, s) * (m.IncomeMultiplier * m.SpeedMultiplier * RewardSeconds * share);
        }

        public static int CloversFor(int score) => Math.Min(CloversMost, Math.Max(0, score) / PointsPerClover);

        public static FirefliesResult Finish(IReadOnlyList<StationDef> defs, GameState s, Modifiers m, FirefliesRun run) => Finish(defs, s, m, run.Score);

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
