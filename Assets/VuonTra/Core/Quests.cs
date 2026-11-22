using System;
using System.Collections.Generic;

namespace VuonTra.Core
{
    public enum QuestMetric
    {
        Taps,
        LevelsBought,
        CarrotsEarned,
        StationsOpen,
        /// <summary>The level of the station named by StationId.</summary>
        StationLevel,
        /// <summary>Total levels across all garden-wide upgrades.</summary>
        UpgradesOwned,
        /// <summary>Pictures of the garden taken, in all.</summary>
        PhotosTaken,
        /// <summary>Pictures taken while a gold-or-rarer rabbit lived in the garden.</summary>
        PhotosWithGoldRabbit,
        /// <summary>Pictures taken while every place to rest was open.</summary>
        PhotosWithFullRest,
        /// <summary>1 once a mooncake game has earned three stars.</summary>
        PoundingThreeStar,
        /// <summary>Mooncake games played, in all.</summary>
        PoundingGames,
        /// <summary>One more than the rarest tier in the album (so a target of 4 means "a Purple kind met").</summary>
        RarestKindMet,
        /// <summary>How many plots the garden has.</summary>
        PlotsOwned,
        /// <summary>How many rabbits stand at their tier's level cap.</summary>
        RabbitsAtCap,
        /// <summary>1 once a leaf-catching game has earned three stars.</summary>
        LeavesThreeStar,
        /// <summary>Leaf-catching games played, in all.</summary>
        LeavesGames
    }

    /// <summary>
    /// One goal with a reward. Daily quests count from the start of the day and come back
    /// tomorrow; the others are achievements, earned once.
    /// </summary>
    public sealed class QuestDef
    {
        public string Id;
        public string Title;
        public QuestMetric Metric;
        public string StationId;
        /// <summary>The number to reach. A daily CarrotsEarned quest with 0 here uses the day's own target.</summary>
        public double Target;
        public bool Daily;
        /// <summary>Carrots paid: this many seconds of current income…</summary>
        public double RewardSeconds;
        /// <summary>…but never less than this.</summary>
        public double RewardCarrots;
        public int RewardClovers;
        public int RewardGolden;
    }

    public static class Quests
    {
        /// <summary>A day's carrot goal: ten minutes of the income the day started with, at least this.</summary>
        public const double DailyCarrotSeconds = 600d;
        public const double DailyCarrotMinimum = 100d;

        public static double TargetOf(QuestDef q, GameState s)
        {
            if (q.Daily && q.Metric == QuestMetric.CarrotsEarned && q.Target <= 0d) return s.DailyCarrotTarget.ToDouble();
            return q.Target;
        }

        /// <summary>How far along the quest is, in the same unit as its target (not clamped).</summary>
        public static double Progress(QuestDef q, IReadOnlyList<StationDef> defs, GameState s)
        {
            switch (q.Metric)
            {
                case QuestMetric.Taps:
                    return q.Daily ? Math.Max(0, s.TotalTaps - s.DailyTapsBase) : s.TotalTaps;
                case QuestMetric.LevelsBought:
                    return q.Daily ? Math.Max(0, s.LevelsBought - s.DailyLevelsBase) : s.LevelsBought;
                case QuestMetric.CarrotsEarned:
                    return q.Daily ? (s.TotalEarned - s.DailyEarnedBase).ToDouble() : s.TotalEarned.ToDouble();
                case QuestMetric.StationsOpen:
                {
                    int open = 0;
                    for (int i = 0; i < s.Levels.Length; i++)
                        if (s.Levels[i] > 0) open++;
                    return open;
                }
                case QuestMetric.StationLevel:
                    for (int i = 0; i < defs.Count; i++)
                        if (defs[i].Id == q.StationId) return s.Levels[i];
                    return 0;
                case QuestMetric.UpgradesOwned:
                    return Upgrades.OwnedLevels(s);
                case QuestMetric.PhotosTaken:
                    return s.PhotosTaken;
                case QuestMetric.PhotosWithGoldRabbit:
                    return s.PhotosWithGoldRabbit;
                case QuestMetric.PhotosWithFullRest:
                    return s.PhotosWithFullRest;
                case QuestMetric.RarestKindMet:
                {
                    int rarest = 0;
                    foreach (string kindId in s.SeenKinds)
                    {
                        RabbitKind kind = RabbitKinds.Find(kindId);
                        if (kind != null && kind.Tier + 1 > rarest) rarest = kind.Tier + 1;
                    }
                    return rarest;
                }
                case QuestMetric.PlotsOwned:
                    return s.Plots;
                case QuestMetric.PoundingThreeStar:
                    return s.PoundingBestStars >= 3 ? 1 : 0;
                case QuestMetric.PoundingGames:
                    return s.PoundingGames;
                case QuestMetric.LeavesThreeStar:
                    return s.Leaves.BestStars >= 3 ? 1 : 0;
                case QuestMetric.LeavesGames:
                    return s.Leaves.Games;
                case QuestMetric.RabbitsAtCap:
                {
                    int capped = 0;
                    foreach (Rabbit r in s.Rabbits) if (Rabbits.IsMaxLevel(r)) capped++;
                    return capped;
                }
                default:
                    return 0;
            }
        }

        public static bool IsClaimed(QuestDef q, GameState s) => s.Claimed.Contains(q.Id);

        public static bool IsComplete(QuestDef q, IReadOnlyList<StationDef> defs, GameState s)
        {
            double target = TargetOf(q, s);
            return target > 0d && Progress(q, defs, s) >= target;
        }

        public static bool CanClaim(QuestDef q, IReadOnlyList<StationDef> defs, GameState s)
        {
            return !IsClaimed(q, s) && IsComplete(q, defs, s);
        }

        public static BigNum RewardOf(QuestDef q, IReadOnlyList<StationDef> defs, GameState s, Modifiers m)
        {
            BigNum bySeconds = Economy.IncomePerSecond(defs, s) * (m.IncomeMultiplier * m.SpeedMultiplier * q.RewardSeconds);
            var flat = new BigNum(q.RewardCarrots);
            return bySeconds > flat ? bySeconds : flat;
        }

        /// <summary>Pays a finished quest once. Rewards are a gift, so they do not count as earned carrots.</summary>
        public static bool TryClaim(QuestDef q, IReadOnlyList<StationDef> defs, GameState s, Modifiers m, out BigNum paid)
        {
            paid = BigNum.Zero;
            if (!CanClaim(q, defs, s)) return false;
            paid = RewardOf(q, defs, s, m);
            s.Coins += paid;
            s.Clovers += q.RewardClovers;
            s.GoldenCarrots += q.RewardGolden;
            s.Claimed.Add(q.Id);
            Garden.AddXp(s, Garden.XpQuest);
            return true;
        }

        public static int ClaimableCount(IReadOnlyList<QuestDef> quests, IReadOnlyList<StationDef> defs, GameState s)
        {
            int n = 0;
            for (int i = 0; i < quests.Count; i++)
                if (CanClaim(quests[i], defs, s)) n++;
            return n;
        }

        /// <summary>
        /// Starts a new day when <paramref name="day"/> differs from the one in the state: daily
        /// counters restart from now, daily claims are forgotten, and the carrot goal is set from
        /// the income at this moment. Returns true when a new day began. A day number that goes
        /// backwards (clock changed) also starts a fresh day rather than reviving old claims.
        /// </summary>
        public static bool RollDaily(IReadOnlyList<QuestDef> quests, IReadOnlyList<StationDef> defs, GameState s, long day, Modifiers m)
        {
            if (s.DailyDay == day) return false;
            s.DailyDay = day;
            s.DailyTapsBase = s.TotalTaps;
            s.DailyLevelsBase = s.LevelsBought;
            s.DailyEarnedBase = s.TotalEarned;
            s.GoldenToday = 0;
            BigNum goal = Economy.IncomePerSecond(defs, s) * (m.IncomeMultiplier * m.SpeedMultiplier * DailyCarrotSeconds);
            var floor = new BigNum(DailyCarrotMinimum);
            s.DailyCarrotTarget = goal > floor ? goal : floor;
            for (int i = 0; i < quests.Count; i++)
                if (quests[i].Daily) s.Claimed.Remove(quests[i].Id);
            return true;
        }
    }
}
