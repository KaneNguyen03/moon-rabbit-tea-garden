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
        StationLevel,
        UpgradesOwned,
        PhotosTaken,
        PhotosWithGoldRabbit,
        PhotosWithFullRest,
        PoundingThreeStar,
        PoundingGames,
        RarestKindMet,
        PlotsOwned,
        RabbitsAtCap,
        LeavesThreeStar,
        LeavesGames,
        FirefliesThreeStar,
        FirefliesGames,
        OrdersDelivered,
        SeasonKindMet,
        SeasonKindInTrade,
        PhotosWithSeasonRabbit,
        PhotoAsksAnswered,
        RabbitsResting,
        KindsMet,
        FrogsMet,
        FishCaught,
        MostHearts,
        GardenLevel,
        RabbitsInTrade,
        RabbitsCalled,
        RabbitsFed,
        JarsOpened,
        Pets,
        LayoutMoves,
        Boosts,
        DailyGifts,
        MergesOrGoldenCalls,
        SegmentsOpened,
        BestFish,
        DailyQuestsToday
    }

    public sealed class QuestDef
    {
        public string Id;
        public string Title;
        public QuestMetric Metric;
        public string StationId;
        public double Target;
        public bool Daily;
        public double RewardSeconds;
        public double RewardCarrots;
        public int RewardClovers;
        public int RewardGolden;
        public int RewardJars;
        public Season Season;
    }

    public static class Quests
    {
        public const double DailyCarrotSeconds = 600d;
        public const double DailyCarrotMinimum = 100d;

        public static double TargetOf(QuestDef q, GameState s)
        {
            if (q.Daily && q.Metric == QuestMetric.CarrotsEarned && q.Target <= 0d) return s.DailyCarrotTarget.ToDouble();
            return q.Target;
        }

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
                    return RestSpots.SeatedCount(s);
                case QuestMetric.KindsMet:
                    return AlbumSets.KindsMet(s);
                case QuestMetric.FrogsMet:
                    return s.FrogsMet;
                case QuestMetric.FishCaught:
                    return s.FishCaught;
                case QuestMetric.MostHearts:
                {
                    int most = 0;
                    foreach (Rabbit r in s.Rabbits) most = Math.Max(most, Rabbits.HeartsOf(r));
                    return most;
                }
                case QuestMetric.OrdersDelivered:
                    return q.Daily ? Math.Max(0, s.OrdersDelivered - s.DailyOrdersBase) : s.OrdersDelivered;
                case QuestMetric.GardenLevel:
                    return Garden.Level(s);
                case QuestMetric.RabbitsInTrade:
                {
                    int inTrade = 0;
                    foreach (Rabbit r in s.Rabbits) if (Rabbits.InTrade(defs, r)) inTrade++;
                    return inTrade;
                }
                case QuestMetric.RabbitsCalled:
                    return s.CloverCalls + s.GoldenCalls;
                case QuestMetric.RabbitsFed:
                    return s.FishFed;
                case QuestMetric.JarsOpened:
                    return s.JarsOpened;
                case QuestMetric.Pets:
                    return s.Pets;
                case QuestMetric.LayoutMoves:
                    return s.LayoutMoves;
                case QuestMetric.Boosts:
                    return s.Boosts;
                case QuestMetric.DailyGifts:
                    return s.DailyGifts;
                case QuestMetric.MergesOrGoldenCalls:
                    return s.Merges + s.GoldenCalls;
                case QuestMetric.SegmentsOpened:
                    return Expansion.Segments(s);
                case QuestMetric.BestFish:
                    return s.BestFish;
                case QuestMetric.DailyQuestsToday:
                    return s.DailyQuestsToday;
                default:
                    return 0;
            }
        }

        public static bool IsClaimed(QuestDef q, GameState s) => s.Claimed.Contains(q.Id);

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

        public static int JarsNow(QuestDef q, GameState s) => q.RewardJars > 0 && MoonJars.DailyWaiting(s, s.DailyDay) ? 1 : 0;

        public static int CloversNow(QuestDef q, GameState s) => q.RewardClovers + q.RewardJars - JarsNow(q, s);

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
            if (q.Daily)
            {
                s.DailyQuestsToday++;
                QuestChests.AddPoints(s, s.DailyDay, QuestChests.QuestPoints);
            }
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

        public static bool RollDaily(IReadOnlyList<QuestDef> quests, IReadOnlyList<StationDef> defs, GameState s, long day, Modifiers m)
        {
            if (day <= s.DailyDay) return false;
            s.DailyDay = day;
            QuestChests.Roll(s, day);
            s.DailyTapsBase = s.TotalTaps;
            s.DailyLevelsBase = s.LevelsBought;
            s.DailyEarnedBase = s.TotalEarned;
            s.DailyOrdersBase = s.OrdersDelivered;
            s.DailyPhotoAsksBase = s.PhotoAsksAnswered;
            s.GoldenToday = 0;
            s.DailyQuestsToday = 0;
            BigNum goal = Economy.IncomePerSecond(defs, s) * (m.IncomeMultiplier * m.SpeedMultiplier * DailyCarrotSeconds);
            var floor = new BigNum(DailyCarrotMinimum);
            s.DailyCarrotTarget = goal > floor ? goal : floor;
            for (int i = 0; i < quests.Count; i++)
                if (quests[i].Daily) s.Claimed.Remove(quests[i].Id);
            return true;
        }

        public static void RerollCarrotTarget(IReadOnlyList<StationDef> defs, GameState s, Modifiers m)
        {
            s.DailyEarnedBase = s.TotalEarned;
            BigNum goal = Economy.IncomePerSecond(defs, s) * (m.IncomeMultiplier * m.SpeedMultiplier * DailyCarrotSeconds);
            var floor = new BigNum(DailyCarrotMinimum);
            s.DailyCarrotTarget = goal > floor ? goal : floor;
        }
    }
}
