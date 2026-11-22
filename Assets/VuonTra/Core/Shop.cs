using System;
using System.Collections.Generic;

namespace VuonTra.Core
{
    /// <summary>
    /// The tea shop by the pond: a gift every day that grows with the streak, and calling a rabbit with
    /// golden carrots at better odds. Golden carrots are only ever earned here and in play; nothing turns
    /// them into ordinary carrots or back.
    /// </summary>
    public static class Shop
    {
        /// <summary>The daily gift by streak day: 1, 1, 2, 2, 3, 3, 5, then 3, 3, 5 again and again. A missed day starts over.</summary>
        static readonly int[] GiftChain = { 1, 1, 2, 2, 3, 3, 5 };
        const int GiftLoopFrom = 4; // after day 7 the chain repeats from day 5

        public static int GiftAt(int streakDay)
        {
            if (streakDay < 1) streakDay = 1;
            if (streakDay <= GiftChain.Length) return GiftChain[streakDay - 1];
            int loop = GiftChain.Length - GiftLoopFrom;
            return GiftChain[GiftLoopFrom + (streakDay - GiftChain.Length - 1) % loop];
        }

        /// <summary>True while today's gift is still to be had. A clock set back to a day already paid gets nothing.</summary>
        public static bool GiftWaiting(GameState s, long day) => day > s.GiftDay;

        /// <summary>The streak day today would be: one more when yesterday was paid, else back to one.</summary>
        public static int StreakFor(GameState s, long day) => s.GiftDay >= 0 && day == s.GiftDay + 1 ? s.GiftStreak + 1 : 1;

        /// <summary>Pays today's gift once. How many golden carrots it brought, 0 when today is already paid or the clock went back.</summary>
        public static int ClaimDailyGift(GameState s, long day) => ClaimDailyGift(s, day, null, out _);

        /// <summary>
        /// The same, and on a season's last day the gift also brings the season's kind to a garden that has none (#8),
        /// so that everyone who plays through a season meets its rabbit. <paramref name="seasonRabbit"/> is that rabbit, or null.
        /// </summary>
        public static int ClaimDailyGift(GameState s, long day, string name, out Rabbit seasonRabbit)
        {
            seasonRabbit = null;
            if (!GiftWaiting(s, day)) return 0;
            int streak = StreakFor(s, day);
            int gift = GiftAt(streak);
            s.GoldenCarrots += gift;
            s.GiftDay = day;
            s.GiftStreak = streak;
            Season season = Seasons.SeasonOf(day);
            if (Seasons.IsLastDay(day) && !Seasons.HasSeasonRabbit(s, season))
                seasonRabbit = Rabbits.ArriveKind(s, Seasons.KindOf(season).Id, name ?? Seasons.KindOf(season).NameVi);
            return gift;
        }

        /// <summary>Calling a rabbit with golden carrots: the price, and the odds that favour the rarer tiers.</summary>
        public const int GoldenSummonCost = 10;
        public static readonly double[] GoldenOdds = { 0.25, 0.35, 0.22, 0.12, 0.05, 0.01 };

        /// <summary>Spends golden carrots for one new rabbit at the shop's odds; null when there are not enough.</summary>
        /// <summary>The same, in a season: half of the calls that land on the season kind's tier bring the season kind (#8).</summary>
        public static Rabbit SummonGolden(GameState s, double roll, string name, double kindRoll, long day)
        {
            int tier = Rabbits.RollTier(roll, GoldenOdds);
            RabbitKind seasonKind = Seasons.KindOf(Seasons.SeasonOf(day));
            if (seasonKind != null && tier == seasonKind.Tier && kindRoll < Seasons.GoldenCallShare)
            {
                if (s.GoldenCarrots < GoldenSummonCost) return null;
                s.GoldenCarrots -= GoldenSummonCost;
                return Rabbits.ArriveKind(s, seasonKind.Id, name);
            }
            return SummonGolden(s, roll, name, kindRoll);
        }

        public static Rabbit SummonGolden(GameState s, double roll, string name, double kindRoll = 0d)
        {
            if (s.GoldenCarrots < GoldenSummonCost) return null;
            s.GoldenCarrots -= GoldenSummonCost;
            return Rabbits.Arrive(s, Rabbits.RollTier(roll, GoldenOdds), name, kindRoll);
        }

        // ----- the rabbit that visits the shop -----

        /// <summary>The visitor's tier is Blue or rarer: 60 / 30 / 9 / 1 for Blue, Purple, Gold, Red.</summary>
        public static readonly double[] VisitorTierOdds = { 0.60, 0.30, 0.09, 0.01 };
        public const int VisitorFirstTier = 2;
        /// <summary>What the visitor costs, by tier from Blue up.</summary>
        public static readonly int[] VisitorPrices = { 15, 40, 120, 400 };
        /// <summary>A kind not yet in the album is this many times likelier to come than one that is.</summary>
        public const int UnseenWeight = 3;

        /// <summary>The rabbit at the shop today: who it is, what it costs, and whether it has been bought.</summary>
        public struct Visitor
        {
            public long Day;
            public int Tier;
            public string Kind;
            public int Price;
            public bool Bought;
            public bool Seen;
        }

        /// <summary>A number in [0, 1) from the day and the player code, the same every time it is asked for.</summary>
        static double Roll(long day, string code, int salt)
        {
            ulong h = 1469598103934665603UL;
            unchecked
            {
                h ^= (ulong)day; h *= 1099511628211UL;
                h ^= (ulong)salt; h *= 1099511628211UL;
                foreach (char c in code ?? "") { h ^= c; h *= 1099511628211UL; }
                h ^= h >> 29; h *= 0x9E3779B97F4A7C15UL; h ^= h >> 32;
            }
            return (h >> 11) / (double)(1UL << 53);
        }

        /// <summary>The kind the visitor is, from a roll: kinds the album lacks weigh three times a kind it has.</summary>
        public static string PickKind(GameState s, int tier, double roll)
        {
            IReadOnlyList<RabbitKind> kinds = RabbitKinds.OfTier(tier);
            if (kinds.Count == 0) return RabbitKinds.DefaultFor(tier);
            double total = 0d;
            foreach (RabbitKind kind in kinds) total += s.SeenKinds.Contains(kind.Id) ? 1d : UnseenWeight;
            double at = Math.Max(0d, Math.Min(0.999999d, roll)) * total;
            foreach (RabbitKind kind in kinds)
            {
                at -= s.SeenKinds.Contains(kind.Id) ? 1d : UnseenWeight;
                if (at < 0d) return kind.Id;
            }
            return kinds[kinds.Count - 1].Id;
        }

        /// <summary>Who is at the shop on <paramref name="day"/>: the same answer all day for this garden. In a season, a third of the days bring the season's kind (#8).</summary>
        public static Visitor VisitorFor(GameState s, long day)
        {
            string code = s.Profile != null ? s.Profile.Code : "";
            int tier = VisitorFirstTier + Rabbits.RollTier(Roll(day, code, 1), VisitorTierOdds);
            tier = Math.Min(tier, Rabbits.TierCount - 1);
            RabbitKind seasonKind = Seasons.KindOf(Seasons.SeasonOf(day));
            bool seasonal = seasonKind != null && Roll(day, code, 3) < Seasons.VisitorShare;
            if (seasonal) tier = seasonKind.Tier;
            return new Visitor
            {
                Day = day, Tier = tier, Kind = seasonal ? seasonKind.Id : PickKind(s, tier, Roll(day, code, 2)),
                Price = VisitorPrices[Math.Min(tier - VisitorFirstTier, VisitorPrices.Length - 1)],
                Bought = s.VisitorBoughtDay == day, Seen = s.VisitorSeenDay == day
            };
        }

        /// <summary>Buys today's visitor, once, while it is still today. The rabbit, or null when it cannot be bought.</summary>
        public static Rabbit BuyVisitor(GameState s, long day, string name)
        {
            Visitor v = VisitorFor(s, day);
            if (v.Bought || s.GoldenCarrots < v.Price) return null;
            s.GoldenCarrots -= v.Price;
            s.VisitorBoughtDay = day;
            return Rabbits.ArriveKind(s, v.Kind, name);
        }

        // ----- things for rabbits (#44 round 3): hats for looks, and fur dye -----

        /// <summary>
        /// Buys one item the shop sells, for golden carrots: it goes into the wardrobe, not on a head. One of each:
        /// a kind the player already owns is not sold again. Null when the item is not for sale, already owned,
        /// or the purse is short.
        /// </summary>
        public static Item BuyItem(GameState s, IReadOnlyList<ItemDef> defs, string defId)
        {
            ItemDef def = Items.DefOf(defs, defId);
            if (def == null || def.Price <= 0 || Items.Owns(s, defId) || s.GoldenCarrots < def.Price) return null;
            s.GoldenCarrots -= def.Price;
            return Items.Grant(s, def, def.Tier);
        }

        /// <summary>One colour of the dye palette: its id (for its name) and its colour as six hex digits.</summary>
        public struct Dye
        {
            public string Id;
            public string Hex;
            public Dye(string id, string hex) { Id = id; Hex = hex; }
        }

        /// <summary>What one dyeing costs, every time, in golden carrots.</summary>
        public const int DyeCost = 8;

        /// <summary>The eight colours a rabbit's fur can be dyed: pink, peach, lemon, mint, sky, lavender, cream, charcoal.</summary>
        public static readonly Dye[] Dyes =
        {
            new Dye("hong", "F28BB5"), new Dye("dao", "FFB088"), new Dye("vang-chanh", "F5E36B"), new Dye("bac-ha", "7FD9B0"),
            new Dye("xanh-troi", "7EC2F0"), new Dye("oai-huong", "B49BE0"), new Dye("kem", "FFF0C8"), new Dye("than", "6A6478")
        };

        /// <summary>Dyes one rabbit's fur a colour of the palette for <see cref="DyeCost"/>. False when the colour, the rabbit or the carrots are missing, or it already wears that colour.</summary>
        public static bool DyeRabbit(GameState s, long rabbitId, int dye)
        {
            Rabbit r = Rabbits.Find(s, rabbitId);
            if (r == null || dye < 0 || dye >= Dyes.Length || r.Dye == dye || s.GoldenCarrots < DyeCost) return false;
            s.GoldenCarrots -= DyeCost;
            r.Dye = dye;
            return true;
        }

        /// <summary>Washes the dye out, for nothing. False when there was none.</summary>
        public static bool Wash(GameState s, long rabbitId)
        {
            Rabbit r = Rabbits.Find(s, rabbitId);
            if (r == null || r.Dye < 0) return false;
            r.Dye = -1;
            return true;
        }

        /// <summary>Golden carrots the garden brings at every level that divides by five.</summary>
        public const int GoldenPerFifthLevel = 2;
        public static int GoldenForLevels(int from, int to)
        {
            int n = 0;
            for (int level = from + 1; level <= to; level++) if (level % 5 == 0) n += GoldenPerFifthLevel;
            return n;
        }
    }
}
