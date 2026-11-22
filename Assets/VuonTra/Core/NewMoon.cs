using System;
using System.Collections.Generic;

namespace VuonTra.Core
{
    /// <summary>What a new moon would keep, take and give, with the save's own numbers: the compare card reads it.</summary>
    public struct NewMoonPreview
    {
        public int SeasonAfter;
        public int Rabbits, Items, Clovers, GoldenCarrots;
        public BigNum Coins;
        public int StationLevels, StationsOpen, Plots, RestSpots, GardenLevel;
        public double BonusBefore, BonusAfter;
        public int GiftClovers, GiftGolden;
        /// <summary>The moon rabbit comes with the first new moon only.</summary>
        public bool MoonRabbit;
    }

    /// <summary>
    /// The new moon (#1): a garden grown far enough can start over from the first plots, keeping its rabbits,
    /// items and rare currencies, and every season begun adds a lasting share to all income. Each season asks
    /// for a few garden levels more than the last, up to the seventh.
    /// </summary>
    public static class NewMoon
    {
        /// <summary>The garden level the first new moon asks for; each later one asks for this many more; after the last, no more.</summary>
        public const int FirstLevel = 30, LevelStep = 5, MaxSeasons = 7;
        /// <summary>Each season begun multiplies income by one more of this, added up (season 4 is ×2).</summary>
        public const double BonusPerSeason = 0.25;
        public const int GiftClovers = 3, GiftGolden = 5;
        /// <summary>The kind that only ever arrives with a new moon.</summary>
        public const string MoonKind = "tho-trang-non";

        /// <summary>The garden level needed to begin the season after this many.</summary>
        public static int LevelNeeded(int seasonsDone) => FirstLevel + LevelStep * Math.Max(0, seasonsDone);

        public static bool AtCap(GameState s) => s.Season >= MaxSeasons;

        public static bool CanBegin(GameState s) => !AtCap(s) && Garden.Level(s) >= LevelNeeded(s.Season);

        /// <summary>The lasting income multiplier after this many seasons: ×1 for none, ×1.25 after one.</summary>
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

        /// <summary>
        /// Begins the next season: carrots, stations, land, places to rest, the garden level, the tray and the day's
        /// game plays go back to the start; rabbits, items, clovers, golden carrots, the album and the profile stay.
        /// Rabbits whose station closed go to rest and are never lost. Pays the season's gifts; the first season
        /// also brings the moon rabbit, returned in <paramref name="gift"/>. False, and nothing changes, when the
        /// garden is not far enough.
        /// </summary>
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

            // Only the teapot is open: whoever worked elsewhere rests, and the rest places are only for looking.
            foreach (Rabbit r in s.Rabbits)
                if (r.Station > 0) r.Station = -1;

            s.Clovers += GiftClovers;
            s.GoldenCarrots += GiftGolden;
            if (!s.SeenKinds.Contains(MoonKind)) gift = Rabbits.ArriveKind(s, MoonKind, moonRabbitName);
            return true;
        }
    }
}
