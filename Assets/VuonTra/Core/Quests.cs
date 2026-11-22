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
        LeavesGames,
        /// <summary>1 once a firefly night has earned three stars.</summary>
        FirefliesThreeStar,
        /// <summary>Firefly nights played, in all.</summary>
        FirefliesGames,
        /// <summary>Guests' tea orders delivered in full (#20).</summary>
        OrdersDelivered,
        /// <summary>1 once a rabbit of the running season lives in the garden (#8).</summary>
        SeasonKindMet,
        /// <summary>1 while a rabbit of the running season works at a station of its trade.</summary>
        SeasonKindInTrade,
        /// <summary>Pictures taken with a rabbit of the running season in the garden.</summary>
        PhotosWithSeasonRabbit,
        /// <summary>Pictures taken for a rabbit that asked for one (#56).</summary>
        PhotoAsksAnswered,
        /// <summary>How many rabbits sit at the places to rest right now (#60).</summary>
        RabbitsResting,
        /// <summary>How many every-day kinds the album holds (#61): the album's milestones.</summary>
        KindsMet
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
        /// <summary>Moon jars (#64), each in place of a clover: the day's free jar when it can still come, the clover when it cannot.</summary>
        public int RewardJars;
        /// <summary>A quest of a season shows only while that season runs (#8); None is every day.</summary>
        public Season Season;
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
                case QuestMetric.FirefliesThreeStar:
                    return s.Fireflies.BestStars >= 3 ? 1 : 0;
                case QuestMetric.FirefliesGames:
                    return s.Fireflies.Games;
                case QuestMetric.RabbitsAtCap:
                {
                    int capped = 0;
                    foreach (Rabbit r in s.Rabbits) if (Rabbits.IsMaxLevel(r)) capped++;
                    return capped;
                }
                case QuestMetric.SeasonKindMet:
                    return Seasons.HasSeasonRabbit(s, Seasons.SeasonOf(s.MaxDaySeen)) ? 1 : 0;
                case QuestMetric.SeasonKindInTrade:
                    return Seasons.SeasonRabbitInTrade(defs, s, Seasons.SeasonOf(s.MaxDaySeen)) ? 1 : 0;
                case QuestMetric.PhotosWithSeasonRabbit:
                    return s.PhotosWithSeasonRabbit;
                case QuestMetric.PhotoAsksAnswered:
                    return q.Daily ? Math.Max(0, s.PhotoAsksAnswered - s.DailyPhotoAsksBase) : s.PhotoAsksAnswered;
                case QuestMetric.RabbitsResting:
                    return RestSpots.Seated(s).Count;
                case QuestMetric.KindsMet:
                    return AlbumSets.KindsMet(s);
                case QuestMetric.OrdersDelivered:
                    return q.Daily ? Math.Max(0, s.OrdersDelivered - s.DailyOrdersBase) : s.OrdersDelivered;
                default:
                    return 0;
            }
        }

        public static bool IsClaimed(QuestDef q, GameState s) => s.Claimed.Contains(q.Id);

        /// <summary>A quest of a season is only there while the season runs; the others always are.</summary>
        public static bool IsAvailable(QuestDef q, long day) => q.Season == Season.None || Seasons.SeasonOf(day) == q.Season;

        public static bool IsComplete(QuestDef q, IReadOnlyList<StationDef> defs, GameState s)
        {
            double target = TargetOf(q, s);
            return target > 0d && Progress(q, defs, s) >= target;
        }

        public static bool CanClaim(QuestDef q, IReadOnlyList<StationDef> defs, GameState s)
        {
            return !IsClaimed(q, s) && IsComplete(q, defs, s) && IsAvailable(q, s.MaxDaySeen);
        }

        public static BigNum RewardOf(QuestDef q, IReadOnlyList<StationDef> defs, GameState s, Modifiers m)
        {
            BigNum bySeconds = Economy.IncomePerSecond(defs, s) * (m.IncomeMultiplier * m.SpeedMultiplier * q.RewardSeconds);
            var flat = new BigNum(q.RewardCarrots);
            return bySeconds > flat ? bySeconds : flat;
        }

        /// <summary>Moon jars a claim would pay now (#64): one at most, the day's free jar, while it can still come (from garden level 15).</summary>
        public static int JarsNow(QuestDef q, GameState s) => q.RewardJars > 0 && MoonJars.DailyWaiting(s, s.DailyDay) ? 1 : 0;

        /// <summary>Clovers a claim would pay now: its own, and one for each of its jars that cannot come.</summary>
        public static int CloversNow(QuestDef q, GameState s) => q.RewardClovers + q.RewardJars - JarsNow(q, s);

        /// <summary>Pays a finished quest once. Rewards are a gift, so they do not count as earned carrots.</summary>
        public static bool TryClaim(QuestDef q, IReadOnlyList<StationDef> defs, GameState s, Modifiers m, out BigNum paid)
        {
            paid = BigNum.Zero;
            if (!CanClaim(q, defs, s)) return false;
            paid = RewardOf(q, defs, s, m);
            s.Coins += paid;
            int clovers = CloversNow(q, s);
            if (JarsNow(q, s) > 0) MoonJars.GiveFree(s, s.DailyDay);
            s.Clovers += clovers;
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
        /// Starts a new day when <paramref name="day"/> is later than the one in the state: daily counters restart
        /// from now, daily claims are forgotten, and the carrot goal is set from the income at this moment. Returns
        /// true when a new day began. A day number that goes backwards (clock turned back) starts nothing: the
        /// day's claims stay claimed (#78).
        /// </summary>
        public static bool RollDaily(IReadOnlyList<QuestDef> quests, IReadOnlyList<StationDef> defs, GameState s, long day, Modifiers m)
        {
            if (day <= s.DailyDay) return false;
            s.DailyDay = day;
            s.DailyTapsBase = s.TotalTaps;
            s.DailyLevelsBase = s.LevelsBought;
            s.DailyEarnedBase = s.TotalEarned;
            s.DailyOrdersBase = s.OrdersDelivered;
            s.DailyPhotoAsksBase = s.PhotoAsksAnswered;
            s.GoldenToday = 0;
            BigNum goal = Economy.IncomePerSecond(defs, s) * (m.IncomeMultiplier * m.SpeedMultiplier * DailyCarrotSeconds);
            var floor = new BigNum(DailyCarrotMinimum);
            s.DailyCarrotTarget = goal > floor ? goal : floor;
            for (int i = 0; i < quests.Count; i++)
                if (quests[i].Daily) s.Claimed.Remove(quests[i].Id);
            return true;
        }

        /// <summary>
        /// Sets the day's carrot goal again from the income of this moment, counting from now, and nothing else:
        /// the day, its claims and its other counters stay. For a garden that just began a new moon (#1), whose
        /// goal was set from the big garden's income.
        /// </summary>
        public static void RerollCarrotTarget(IReadOnlyList<StationDef> defs, GameState s, Modifiers m)
        {
            s.DailyEarnedBase = s.TotalEarned;
            BigNum goal = Economy.IncomePerSecond(defs, s) * (m.IncomeMultiplier * m.SpeedMultiplier * DailyCarrotSeconds);
            var floor = new BigNum(DailyCarrotMinimum);
            s.DailyCarrotTarget = goal > floor ? goal : floor;
        }
    }
}
