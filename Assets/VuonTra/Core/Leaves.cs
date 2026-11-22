using System;
using System.Collections.Generic;

namespace VuonTra.Core
{
    /// <summary>What falls from the canopy: a tea leaf, a golden leaf worth two, or a pine cone that costs three.</summary>
    public enum LeafKind { Leaf, Golden, Cone }

    /// <summary>One thing that falls: when it leaves the canopy, when it reaches the basket line, and where across the screen (0 left, 1 right).</summary>
    public struct Leaf
    {
        public int Index;
        public double Drop, Lands, X;
        public LeafKind Kind;
    }

    /// <summary>One drop settled: caught in the basket or gone past it.</summary>
    public struct Drop
    {
        public Leaf Leaf;
        public bool Caught;
        /// <summary>What it did to the score, after the floor at zero.</summary>
        public int Points;
    }

    /// <summary>What a finished game paid out.</summary>
    public sealed class LeavesResult
    {
        public int Score, Stars;
        public BigNum Carrots;
        public int Clovers;
    }

    /// <summary>
    /// A day's plays and the lifetime counts of one small game. Each game keeps its own block, so a
    /// third game only needs another field on <see cref="GameState"/> and a block in the save.
    /// </summary>
    public sealed class MiniGameRecord
    {
        public long Day = -1;
        public int Plays;
        /// <summary>The day's first starred game has given its clover.</summary>
        public bool StarToday;
        /// <summary>What the day's capped gifts have paid so far, for a game that caps them (the firefly night).</summary>
        public int CloversToday, GoldenToday;
        public long Games;
        public int BestStars;

        /// <summary>Games left today. A clock turned back does not bring plays back.</summary>
        public int PlaysLeft(long day, int perDay) => day > Day ? perDay : Math.Max(0, perDay - Plays);

        /// <summary>Spends one of today's plays; a new day starts the count and the day's gifts over. False when none is left.</summary>
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

    /// <summary>
    /// One game of catching tea leaves: thirty seconds of leaves falling from the canopy on a fixed schedule,
    /// settled in landing order against where the basket stood when each one reached the basket line.
    /// The schedule is a function of the seed, never of frames, so the screen and the tests agree.
    /// </summary>
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
        /// <summary>How many drops have reached the basket line, in schedule order.</summary>
        public int Settled { get; private set; }
        /// <summary>Until when a caught pine cone keeps the basket from catching anything.</summary>
        public double StunnedUntil { get; private set; } = double.NegativeInfinity;
        public bool Finished => Settled >= Schedule.Count;

        public LeavesRun(IReadOnlyList<Leaf> schedule, long seed = -1)
        {
            Schedule = schedule;
            Seed = seed;
        }

        public static LeavesRun FromSeed(long seed) => new LeavesRun(Leaves.ScheduleFor(seed), seed);

        public bool Stunned(double now) => now < StunnedUntil;

        /// <summary>
        /// Time passed: every drop whose landing time has come is settled against <paramref name="basketX"/>.
        /// Returns what settled in this call, oldest first (the list is reused: read it before the next call).
        /// </summary>
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
        /// <summary>Seconds before the first drop, so the player sees the canopy and the basket.</summary>
        public const double Lead = 1.0;
        /// <summary>Everything that falls in one game, and how many of each.</summary>
        public const int LeafCount = 36, GoldenCount = 4, ConeCount = 8;
        public const int Drops = LeafCount + GoldenCount + ConeCount;
        /// <summary>A fall takes this long at the start and quickens to the shorter time by the end.</summary>
        public const double MaxFall = 3.0, FirstFall = 2.8, MinFall = 2.0;
        /// <summary>Half the basket's reach, in screen widths: a leaf this far from its middle is still caught.</summary>
        public const double BasketHalfWidth = 0.12;
        public const int LeafPoints = 1, GoldenPoints = 2, ConePenalty = 3;
        /// <summary>A caught pine cone: nothing is caught for this long after it lands.</summary>
        public const double StunSeconds = 0.5;
        /// <summary>Every leaf caught, every cone dodged: thirty-six at one and four at two.</summary>
        public const int MaxScore = LeafCount * LeafPoints + GoldenCount * GoldenPoints;
        /// <summary>About a third, two thirds and seven eighths of the most.</summary>
        public const int OneStar = 15, TwoStars = 29, ThreeStars = 38;
        public const int PlaysPerDay = 3;
        /// <summary>One level above the mortar, so the second game comes after the first.</summary>
        public const int UnlockLevel = 4;
        /// <summary>A full-score game pays this many seconds of income.</summary>
        public const double RewardSeconds = 60d;

        /// <summary>A number in [0, 1) from the seed and a salt, the same every time it is asked for.</summary>
        static double Roll(long seed, int salt)
        {
            ulong h = 1469598103934665603UL;
            unchecked
            {
                h ^= (ulong)seed; h *= 1099511628211UL;
                h ^= (ulong)salt; h *= 1099511628211UL;
                h ^= 0x4C656166UL; h *= 1099511628211UL; // "Leaf": not the shop's rolls
                h ^= h >> 29; h *= 0x9E3779B97F4A7C15UL; h ^= h >> 32;
            }
            return (h >> 11) / (double)(1UL << 53);
        }

        /// <summary>The seed of a game: the day and how many games came before it, so no two games are the same.</summary>
        public static long SeedFor(long day, long gamesBefore) => day * 1000003L + gamesBefore;

        /// <summary>
        /// The drops of one game, in drop order (which is also landing order). Drops come about every half second
        /// with a little jitter; falls quicken from <see cref="FirstFall"/> to <see cref="MinFall"/>; the kinds are
        /// a fixed mix shuffled by the seed, and the first drop is never a pine cone.
        /// </summary>
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

        public static bool Unlocked(GameState s) => Garden.Level(s) >= UnlockLevel;

        public static int PlaysLeft(GameState s, long day) => s.Leaves.PlaysLeft(day, PlaysPerDay);

        public static bool CanPlay(GameState s, long day) => Unlocked(s) && PlaysLeft(s, day) > 0;

        /// <summary>Spends one of today's plays. False when none is left or the basket is not out yet.</summary>
        public static bool Start(GameState s, long day) => Unlocked(s) && s.Leaves.Start(day, PlaysPerDay);

        /// <summary>Carrots for a score: a minute of income at full score, by the same rule quests and the mooncake game use.</summary>
        public static BigNum Reward(IReadOnlyList<StationDef> defs, GameState s, Modifiers m, int score)
        {
            double share = Math.Max(0, Math.Min(MaxScore, score)) / (double)MaxScore;
            return Economy.IncomePerSecond(defs, s) * (m.IncomeMultiplier * m.SpeedMultiplier * RewardSeconds * share);
        }

        /// <summary>Pays a finished game and keeps the day's and the garden's counts. Carrots are the prize; no golden carrot here.</summary>
        public static LeavesResult Finish(IReadOnlyList<StationDef> defs, GameState s, Modifiers m, LeavesRun run) => Finish(defs, s, m, run.Score);

        /// <summary>Pays a game that ended on this score; the run is only its score once it is over.</summary>
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
