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
        DailyQuestsToday,
        MiniGames,
        RabbitsFullHearts,
        GardenLevelReached,
        NewMoons
    }

    [Serializable]
    public sealed class QuestTier
    {
        public double Target;
        public double RewardSeconds;
        public double RewardCarrots;
        public int RewardClovers;
        public int RewardGolden;
        public int RewardJars;
        public bool Merged;
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
        public QuestTier[] Tiers;
        public string[] Merged;

        public bool Chained => Tiers != null && Tiers.Length > 0;

        string[] _tierIds;

        public string TierId(int tier)
        {
            if (_tierIds == null || _tierIds.Length != Tiers.Length)
            {
                _tierIds = new string[Tiers.Length];
                for (int i = 0; i < _tierIds.Length; i++) _tierIds[i] = Id + "." + (i + 1);
            }
            return _tierIds[tier];
        }
    }

    public static class Quests
    {
        public const double DailyCarrotSeconds = 600d;
        public const double DailyCarrotMinimum = 100d;

        public static int TiersOf(QuestDef q) => q.Chained ? q.Tiers.Length : 1;

        public static string TierId(QuestDef q, int tier) => q.TierId(tier);

        public static int TiersClaimed(QuestDef q, GameState s)
        {
            if (!q.Chained) return s.Claimed.Contains(q.Id) ? 1 : 0;
            int oldClaimed = 0;
            if (q.Merged != null)
                foreach (string id in q.Merged)
                    if (s.Claimed.Contains(id)) oldClaimed++;
            int n = 0, oldSeen = 0;
            while (n < q.Tiers.Length)
            {
                bool old = q.Tiers[n].Merged && oldSeen++ < oldClaimed;
                if (!old && !s.Claimed.Contains(TierId(q, n))) break;
                n++;
            }
            return n;
        }

        public static QuestTier CurrentTier(QuestDef q, GameState s)
        {
            if (!q.Chained) return null;
            return q.Tiers[Math.Min(TiersClaimed(q, s), q.Tiers.Length - 1)];
        }

        public static int TiersDone(IReadOnlyList<QuestDef> quests, GameState s)
        {
            int n = 0;
            for (int i = 0; i < quests.Count; i++)
                if (quests[i].Chained) n += TiersClaimed(quests[i], s);
            return n;
        }

        public static double TargetOf(QuestDef q, GameState s)
        {
            if (q.Chained) return CurrentTier(q, s).Target;
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
                case QuestMetric.MiniGames:
                    return s.PoundingGames + s.Leaves.Games + s.Fireflies.Games;
                case QuestMetric.RabbitsFullHearts:
                {
                    int full = 0;
                    foreach (Rabbit r in s.Rabbits) if (Rabbits.FullHearts(r)) full++;
                    return full;
                }
                case QuestMetric.GardenLevelReached:
                    return Math.Max(Garden.Level(s), NewMoon.LevelPassed(s));
                case QuestMetric.NewMoons:
                    return s.Season;
                default:
                    return 0;
            }
        }

        public static bool IsClaimed(QuestDef q, GameState s) => q.Chained ? TiersClaimed(q, s) >= q.Tiers.Length : s.Claimed.Contains(q.Id);

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
            QuestTier tier = CurrentTier(q, s);
            double seconds = tier != null ? tier.RewardSeconds : q.RewardSeconds, carrots = tier != null ? tier.RewardCarrots : q.RewardCarrots;
            BigNum bySeconds = Economy.IncomePerSecond(defs, s) * (m.IncomeMultiplier * m.SpeedMultiplier * seconds);
            var flat = new BigNum(carrots);
            return bySeconds > flat ? bySeconds : flat;
        }

        public static int GoldenNow(QuestDef q, GameState s) => q.Chained ? CurrentTier(q, s).RewardGolden : q.RewardGolden;

        public static int JarsNow(QuestDef q, GameState s)
        {
            if (q.Chained) return CurrentTier(q, s).RewardJars > 0 && MoonJars.FreeWaiting(s, s.DailyDay) ? 1 : 0;
            return q.RewardJars > 0 && MoonJars.DailyWaiting(s, s.DailyDay) ? 1 : 0;
        }

        public static int CloversNow(QuestDef q, GameState s)
        {
            if (q.Chained)
            {
                QuestTier tier = CurrentTier(q, s);
                return tier.RewardClovers + tier.RewardJars - JarsNow(q, s);
            }
            return q.RewardClovers + q.RewardJars - JarsNow(q, s);
        }

        public static bool TryClaim(QuestDef q, IReadOnlyList<StationDef> defs, GameState s, Modifiers m, out BigNum paid)
        {
            paid = BigNum.Zero;
            if (!CanClaim(q, defs, s)) return false;
            paid = RewardOf(q, defs, s, m);
            s.Coins += paid;
            int clovers = CloversNow(q, s), jars = JarsNow(q, s), golden = GoldenNow(q, s);
            if (jars > 0) MoonJars.GiveFree(s, s.DailyDay);
            s.Clovers += clovers;
            s.GoldenCarrots += golden;
            s.Claimed.Add(q.Chained ? TierId(q, TiersClaimed(q, s)) : q.Id);
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
