using System;
using System.Collections.Generic;

namespace VuonTra.Core
{
    public enum LeafKind { Leaf, Golden, Cone }

    public struct Leaf
    {
        public int Index;
        public double Drop, Lands, X;
        public LeafKind Kind;
    }

    public struct Drop
    {
        public Leaf Leaf;
        public bool Caught;
        public int Points;
    }

    public sealed class LeavesResult
    {
        public int Score, Stars;
        public BigNum Carrots;
        public int Clovers;
    }

    public sealed class MiniGameRecord
    {
        public long Day = -1;
        public int Plays;
        public bool StarToday;
        public int CloversToday, GoldenToday;
        public long Games;
        public int BestStars;

        public int PlaysLeft(long day, int perDay) => day > Day ? perDay : Math.Max(0, perDay - Plays);

        public bool Start(long day, int perDay)
        {
            if (PlaysLeft(day, perDay) <= 0) return false;
            if (day > Day)
            {
                Day = day;
                Plays = 0;
                StarToday = false;
                CloversToday = 0;
                GoldenToday = 0;
            }
            Plays++;
            return true;
        }
    }

    public sealed class LeavesRun
    {
        readonly List<Drop> _settledNow = new List<Drop>();

        public long Seed { get; }
        public IReadOnlyList<Leaf> Schedule { get; }
        public int Score { get; private set; }
        public int Caught { get; private set; }
        public int Golden { get; private set; }
        public int Cones { get; private set; }
        public int Missed { get; private set; }
        public int Settled { get; private set; }
        public double StunnedUntil { get; private set; } = double.NegativeInfinity;
        public bool Finished => Settled >= Schedule.Count;

        public LeavesRun(IReadOnlyList<Leaf> schedule, long seed = -1)
        {
            Schedule = schedule;
            Seed = seed;
        }

        public static LeavesRun FromSeed(long seed) => new LeavesRun(Leaves.ScheduleFor(seed), seed);

        public bool Stunned(double now) => now < StunnedUntil;

        public IReadOnlyList<Drop> Advance(double now, double basketX)
        {
            _settledNow.Clear();
            while (!Finished && Schedule[Settled].Lands <= now)
            {
                Leaf leaf = Schedule[Settled];
                bool caught = Leaves.InReach(leaf.X, basketX) && !Stunned(leaf.Lands);
                int before = Score;
                if (caught)
                {
                    switch (leaf.Kind)
                    {
                        case LeafKind.Leaf:
                            Caught++;
                            Score += Leaves.LeafPoints;
                            break;
                        case LeafKind.Golden:
                            Caught++;
                            Golden++;
                            Score += Leaves.GoldenPoints;
                            break;
                        default:
                            Cones++;
                            Score = Math.Max(0, Score - Leaves.ConePenalty);
                            StunnedUntil = leaf.Lands + Leaves.StunSeconds;
                            break;
                    }
                }
                else Missed++;
                Settled++;
                _settledNow.Add(new Drop { Leaf = leaf, Caught = caught, Points = Score - before });
            }
            return _settledNow;
        }
    }

    public static class Leaves
    {
        public const double Seconds = 30d;
        public const double Lead = 1.0;
        public const int LeafCount = 36, GoldenCount = 4, ConeCount = 8;
        public const int Drops = LeafCount + GoldenCount + ConeCount;
        public const double MaxFall = 3.0, FirstFall = 2.8, MinFall = 2.0;
        public const double BasketHalfWidth = 0.12;
        public const int LeafPoints = 1, GoldenPoints = 2, ConePenalty = 3;
        public const double StunSeconds = 0.5;
        public const int MaxScore = LeafCount * LeafPoints + GoldenCount * GoldenPoints;
        public const int OneStar = 15, TwoStars = 29, ThreeStars = 38;
        public const int PlaysPerDay = 3;
        public const int UnlockLevel = 7;
        public const int KeptFromLevel = 4;
        public const double RewardSeconds = 60d;
        public const string StationKind = "gio-la";

        const ulong StreamApartFromOtherGames = 0x4C656166UL;

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

        public static IReadOnlyList<Leaf> ScheduleFor(long seed)
        {
            var kinds = new LeafKind[Drops];
            for (int i = 0; i < Drops; i++) kinds[i] = i < LeafCount ? LeafKind.Leaf : (i < LeafCount + GoldenCount ? LeafKind.Golden : LeafKind.Cone);
            for (int i = Drops - 1; i > 0; i--)
            {
                int j = (int)(Roll(seed, 1000 + i) * (i + 1));
                LeafKind t = kinds[i]; kinds[i] = kinds[j]; kinds[j] = t;
            }
            if (kinds[0] == LeafKind.Cone)
                for (int i = 1; i < Drops; i++)
                    if (kinds[i] != LeafKind.Cone) { kinds[0] = kinds[i]; kinds[i] = LeafKind.Cone; break; }

            double gap = (Seconds - Lead - MaxFall - 0.3) / Drops;
            var list = new Leaf[Drops];
            for (int i = 0; i < Drops; i++)
            {
                double drop = Lead + i * gap + Roll(seed, 2000 + i) * 0.4 * gap;
                double fall = FirstFall + (MinFall - FirstFall) * i / (Drops - 1);
                list[i] = new Leaf { Index = i, Drop = drop, Lands = drop + fall, X = Roll(seed, 3000 + i), Kind = kinds[i] };
            }
            return list;
        }

        public static bool InReach(double leafX, double basketX) => Math.Abs(leafX - basketX) <= BasketHalfWidth;

        public static int StarsFor(int score) => score >= ThreeStars ? 3 : (score >= TwoStars ? 2 : (score >= OneStar ? 1 : 0));

        public static bool Unlocked(GameState s)
            => Garden.Level(s) >= UnlockLevel || (Garden.Level(s) >= KeptFromLevel && (s.Leaves.Games > 0 || s.IntrosSeen.Contains(Intros.Leaves)));

        public static int StationIndex(IReadOnlyList<StationDef> defs)
        {
            if (defs != null) for (int i = 0; i < defs.Count; i++) if (defs[i].Id == StationKind) return i;
            return 1;
        }

        public static int PlaysLeft(GameState s, long day) => s.Leaves.PlaysLeft(day, PlaysPerDay);

        public static bool CanPlay(GameState s, long day) => Unlocked(s) && PlaysLeft(s, day) > 0;

        public static bool Start(GameState s, long day) => Unlocked(s) && s.Leaves.Start(day, PlaysPerDay);

        public static BigNum Reward(IReadOnlyList<StationDef> defs, GameState s, Modifiers m, int score)
        {
            double share = Math.Max(0, Math.Min(MaxScore, score)) / (double)MaxScore;
            return Economy.IncomePerSecond(defs, s) * (m.IncomeMultiplier * m.SpeedMultiplier * RewardSeconds * share);
        }

        public static LeavesResult Finish(IReadOnlyList<StationDef> defs, GameState s, Modifiers m, LeavesRun run) => Finish(defs, s, m, run.Score);

        public static LeavesResult Finish(IReadOnlyList<StationDef> defs, GameState s, Modifiers m, int score)
        {
            var result = new LeavesResult { Score = score, Stars = StarsFor(score) };
            result.Carrots = Reward(defs, s, m, score);
            s.Coins += result.Carrots;
            if (result.Stars >= 1 && !s.Leaves.StarToday)
            {
                s.Leaves.StarToday = true;
                s.Clovers += 1;
                result.Clovers = 1;
            }
            s.Leaves.Games++;
            if (result.Stars > s.Leaves.BestStars) s.Leaves.BestStars = result.Stars;
            return result;
        }
    }
}
