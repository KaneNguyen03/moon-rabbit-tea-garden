using System;
using System.Collections.Generic;

namespace VuonTra.Core
{
    public struct NewMoonPreview
    {
        public int SeasonAfter;
        public int Rabbits, Items, Clovers, GoldenCarrots;
        public BigNum Coins;
        public int StationLevels, StationsOpen, Plots, RestSpots, GardenLevel;
        public double BonusBefore, BonusAfter;
        public int GiftClovers, GiftGolden;
        public bool MoonRabbit;
    }

    public static class NewMoon
    {
        public const int FirstLevel = 30, LevelStep = 3, MaxSeasons = 7;
        public const double BonusPerSeason = 0.25;
        public const int GiftClovers = 3, GiftGolden = 5;
        public const string MoonKind = "tho-trang-non";

        public static int LevelNeeded(int seasonsDone) => FirstLevel + LevelStep * Math.Max(0, seasonsDone);

        public static int LevelPassed(GameState s) => s.Season > 0 ? LevelNeeded(Math.Min(s.Season, MaxSeasons) - 1) : 0;

        public static bool AtCap(GameState s) => s.Season >= MaxSeasons;

        public static bool CanBegin(GameState s) => !AtCap(s) && Garden.Level(s) >= LevelNeeded(s.Season);

        public static double Bonus(int seasons) => 1d + BonusPerSeason * Math.Max(0, Math.Min(MaxSeasons, seasons));

        public static double Bonus(GameState s) => Bonus(s.Season);

        public static NewMoonPreview Compare(IReadOnlyList<StationDef> defs, GameState s)
        {
            var p = new NewMoonPreview
            {
                SeasonAfter = s.Season + 1,
                Rabbits = s.Rabbits.Count,
                Items = s.Items.Count,
                Clovers = s.Clovers,
                GoldenCarrots = s.GoldenCarrots,
                Coins = s.Coins,
                Plots = s.Plots,
                RestSpots = s.RestSpots,
                GardenLevel = Garden.Level(s),
                BonusBefore = Bonus(s.Season),
                BonusAfter = Bonus(s.Season + 1),
                GiftClovers = GiftClovers,
                GiftGolden = GiftGolden,
                MoonRabbit = !s.SeenKinds.Contains(MoonKind)
            };
            for (int i = 0; i < s.Levels.Length; i++)
            {
                p.StationLevels += s.Levels[i];
                if (s.Levels[i] > 0) p.StationsOpen++;
            }
            return p;
        }

        public static bool Begin(IReadOnlyList<StationDef> defs, GameState s, string moonRabbitName, out Rabbit gift)
        {
            gift = null;
            if (!CanBegin(s)) return false;
            s.Season++;

            s.Coins = BigNum.Zero;
            for (int i = 0; i < s.Levels.Length; i++)
            {
                s.Levels[i] = 0;
                if (s.Progress != null && i < s.Progress.Length) s.Progress[i] = 0d;
                if (s.TrayWork != null && i < s.TrayWork.Length) s.TrayWork[i] = 0d;
            }
            if (s.Levels.Length > 0) s.Levels[0] = 1;
            s.Plots = Math.Min(defs.Count, Plots.Starting);
            s.RestSpots = RestSpots.Starting;
            s.Grid.Segments = 0;
            GardenGrid.Settle(defs, s);
            s.GardenXp = 0d;
            s.Tray.Clear();
            s.GoldenToday = 0;
            s.BoostSecondsLeft = 0d;
            s.Energy = Energy.Max;
            s.Order = null;
            s.OrderClock = Orders.StartingClock;
            s.PoundingPlays = 0;
            s.Leaves.Plays = 0;
            s.Fireflies.Plays = 0;

            foreach (Rabbit r in s.Rabbits)
                if (r.Station > 0) r.Station = -1;

            s.Clovers += GiftClovers;
            s.GoldenCarrots += GiftGolden;
            if (!s.SeenKinds.Contains(MoonKind)) gift = Rabbits.ArriveKind(s, MoonKind, moonRabbitName);
            return true;
        }
    }
}
