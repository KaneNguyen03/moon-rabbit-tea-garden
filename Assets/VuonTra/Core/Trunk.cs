using System;
using System.Collections.Generic;

namespace VuonTra.Core
{
    public enum TrunkKind { SeasonRabbit, Hat, Jars, Clovers, Lantern, GoldenCall }

    public sealed class TrunkRecord
    {
        public long Window = -1;
        public List<string> Codes = new List<string>();
        public int Sale = -1;
        public int Bought;
    }

    public struct TrunkOffer
    {
        public int Slot;
        public string Code;
        public TrunkKind Kind;
        public string Id;
        public int Amount, FullPrice, Price;
        public bool Sale, Bought, Owned;
        public bool OldSeason => Kind == TrunkKind.SeasonRabbit;
        public bool CanBuy => !Bought && !Owned;
    }

    public struct TrunkShelf
    {
        public bool Open;
        public int Slot;
        public long Window;
        public int CloseHour, NextHour;
        public TrunkOffer[] Offers;
    }

    public struct TrunkBuy
    {
        public bool Ok;
        public TrunkOffer Offer;
        public Rabbit Rabbit, Second;
        public Item Hat;
        public int Jars, JarClovers;
    }

    public sealed class TrunkStock
    {
        public readonly TrunkKind Kind;
        public readonly string Prefix;
        public readonly double Weight;
        public readonly int Price, Amount;

        public TrunkStock(TrunkKind kind, string prefix, double weight, int price, int amount = 1)
        {
            Kind = kind;
            Prefix = prefix;
            Weight = weight;
            Price = price;
            Amount = amount;
        }
    }

    public sealed class TrunkLanterns
    {
        public string Id;
        public int Count;
        public Func<long, int> Current;
        public Func<GameState, int, bool> Owns;
        public Action<GameState, int> Grant;
    }

    public static class Trunk
    {
        public static readonly int[] OpenHours = { 11, 19 };
        public static readonly int[] CloseHours = { 13, 22 };
        public static readonly string[] WindowNames = { "noon", "evening" };
        public const int Windows = 2;
        public const int Shown = 4;
        public const int SalePercent = 30;

        public const int RabbitPrice = 60;
        public const int JarsPrice = 30, JarsAmount = 3;
        public const int CloversPrice = 25, CloversAmount = 5;
        public const int LanternPrice = 40;
        public const int GoldenCallPrice = 18, GoldenCallAmount = 2;

        const string KindPrefix = "kind:", HatPrefix = "hat:", LanternPrefix = "lantern:";
        const int PickSalt = 1130, SaleSalt = 1190;

        public static readonly TrunkStock[] Stock =
        {
            new TrunkStock(TrunkKind.SeasonRabbit, KindPrefix, 2d, RabbitPrice),
            new TrunkStock(TrunkKind.Hat, HatPrefix, 3d, 0),
            new TrunkStock(TrunkKind.Jars, "jars", 3d, JarsPrice, JarsAmount),
            new TrunkStock(TrunkKind.Clovers, "clovers", 3d, CloversPrice, CloversAmount),
            new TrunkStock(TrunkKind.Lantern, LanternPrefix, 2d, LanternPrice),
            new TrunkStock(TrunkKind.GoldenCall, "golden", 2d, GoldenCallPrice, GoldenCallAmount),
        };

        public static readonly TrunkLanterns[] Lanterns =
        {
            new TrunkLanterns
            {
                Id = "month", Count = MonthPass.Months,
                Current = day => MonthPass.CalendarMonth(MonthPass.MonthOf(day)),
                Owns = MonthPass.OwnsLantern,
                Grant = (s, i) => { s.MonthLanterns |= 1 << i; s.LanternColor = MonthPass.LanternIndex(i); }
            },
        };

        public readonly struct Candidate
        {
            public readonly string Code;
            public readonly double Weight;

            public Candidate(string code, double weight)
            {
                Code = code;
                Weight = weight;
            }
        }

        public static long LocalClock(DateTimeOffset now) => now.ToUnixTimeSeconds() + (long)now.Offset.TotalSeconds;

        public static long DayOf(long clock) => (long)Math.Floor(clock / 86400d);

        static int MinuteOf(long clock) => (int)((clock - DayOf(clock) * 86400L) / 60L);

        public static int SlotAt(long clock)
        {
            int minute = MinuteOf(clock);
            for (int i = 0; i < Windows; i++)
                if (minute >= OpenHours[i] * 60 && minute < CloseHours[i] * 60) return i;
            return -1;
        }

        public static long WindowAt(long clock)
        {
            int slot = SlotAt(clock);
            return slot < 0 ? -1 : DayOf(clock) * Windows + slot;
        }

        public static int NextOpenHour(long clock)
        {
            int minute = MinuteOf(clock);
            for (int i = 0; i < Windows; i++)
                if (OpenHours[i] * 60 > minute) return OpenHours[i];
            return OpenHours[0];
        }

        public static int NextHour(GameState s, long clock)
        {
            long window = WindowAt(clock);
            return window >= 0 && window < s.Trunk.Window ? OpenHours[(int)(s.Trunk.Window % Windows)] : NextOpenHour(clock);
        }

        public static bool IsOpen(GameState s, long clock)
        {
            long window = WindowAt(clock);
            return window >= 0 && window >= s.Trunk.Window;
        }

        public static int SalePrice(int price) => Math.Max(1, (int)Math.Round(price * (100 - SalePercent) / 100d, MidpointRounding.AwayFromZero));

        public static IReadOnlyList<Candidate> Pool(GameState s, IReadOnlyList<ItemDef> items, long day)
        {
            var pool = new List<Candidate>();
            foreach (TrunkStock stock in Stock)
            {
                List<string> codes = CodesOf(stock, s, items, day);
                foreach (string code in codes) pool.Add(new Candidate(code, stock.Weight / codes.Count));
            }
            return pool;
        }

        static List<string> CodesOf(TrunkStock stock, GameState s, IReadOnlyList<ItemDef> items, long day)
        {
            var codes = new List<string>();
            switch (stock.Kind)
            {
                case TrunkKind.SeasonRabbit:
                    Season now = Seasons.SeasonOf(day);
                    foreach (RabbitKind kind in RabbitKinds.Seasonal())
                        if (kind.Season != now && !OwnsKind(s, kind.Id)) codes.Add(KindPrefix + kind.Id);
                    break;
                case TrunkKind.Hat:
                    if (items != null)
                        foreach (ItemDef def in items)
                            if (def.Slot == ItemSlot.Hat && def.Price > 0 && !Items.Owns(s, def.Id)) codes.Add(HatPrefix + def.Id);
                    break;
                case TrunkKind.Lantern:
                    foreach (TrunkLanterns source in Lanterns)
                    {
                        int current = source.Current(day);
                        for (int i = 0; i < source.Count; i++)
                            if (i != current && !source.Owns(s, i)) codes.Add(LanternPrefix + source.Id + ":" + i);
                    }
                    break;
                case TrunkKind.Jars:
                    if (MoonJars.Shown(s)) codes.Add(stock.Prefix);
                    break;
                default:
                    codes.Add(stock.Prefix);
                    break;
            }
            return codes;
        }

        static bool OwnsKind(GameState s, string kindId)
        {
            foreach (Rabbit r in s.Rabbits) if (Rabbits.KindOf(r) == kindId) return true;
            return false;
        }

        static string Seed(GameState s) => "trunk" + (s.Profile != null ? s.Profile.Code : "");

        static void Roll(GameState s, IReadOnlyList<ItemDef> items, long window)
        {
            var pool = new List<Candidate>(Pool(s, items, window / Windows));
            TrunkRecord r = s.Trunk;
            r.Window = window;
            r.Codes = new List<string>();
            r.Bought = 0;
            string seed = Seed(s);
            for (int pick = 0; pick < Shown && pool.Count > 0; pick++)
            {
                double total = 0d;
                foreach (Candidate c in pool) total += c.Weight;
                double at = Shop.Roll(window, seed, PickSalt + pick) * total;
                int chosen = pool.Count - 1;
                for (int i = 0; i < pool.Count; i++)
                {
                    at -= pool[i].Weight;
                    if (at < 0d) { chosen = i; break; }
                }
                r.Codes.Add(pool[chosen].Code);
                pool.RemoveAt(chosen);
            }
            r.Sale = r.Codes.Count == 0 ? -1 : Math.Min(r.Codes.Count - 1, (int)(Shop.Roll(window, seed, SaleSalt) * r.Codes.Count));
        }

        public static TrunkShelf Visit(GameState s, IReadOnlyList<ItemDef> items, long clock)
        {
            int slot = SlotAt(clock);
            var shelf = new TrunkShelf { Slot = slot, NextHour = NextHour(s, clock), Offers = Array.Empty<TrunkOffer>() };
            if (!IsOpen(s, clock)) return shelf;
            shelf.Open = true;
            shelf.Window = WindowAt(clock);
            shelf.CloseHour = CloseHours[slot];
            if (shelf.Window > s.Trunk.Window) Roll(s, items, shelf.Window);
            var offers = new List<TrunkOffer>();
            for (int i = 0; i < s.Trunk.Codes.Count; i++)
                if (TryOffer(s, items, i, out TrunkOffer offer)) offers.Add(offer);
            shelf.Offers = offers.ToArray();
            return shelf;
        }

        static bool TryOffer(GameState s, IReadOnlyList<ItemDef> items, int slot, out TrunkOffer offer)
        {
            string code = s.Trunk.Codes[slot];
            offer = new TrunkOffer { Slot = slot, Code = code, Sale = slot == s.Trunk.Sale, Bought = (s.Trunk.Bought & (1 << slot)) != 0 };
            TrunkStock stock = StockOf(code);
            if (stock == null) return false;
            offer.Kind = stock.Kind;
            offer.Id = code.Length > stock.Prefix.Length ? code.Substring(stock.Prefix.Length) : "";
            offer.Amount = stock.Amount;
            offer.FullPrice = stock.Price;
            switch (stock.Kind)
            {
                case TrunkKind.SeasonRabbit:
                    RabbitKind kind = RabbitKinds.Find(offer.Id);
                    if (kind == null || kind.Id != offer.Id || kind.Season == Season.None) return false;
                    offer.Owned = OwnsKind(s, offer.Id);
                    break;
                case TrunkKind.Hat:
                    ItemDef def = Items.DefOf(items, offer.Id);
                    if (def == null || def.Price <= 0) return false;
                    offer.FullPrice = def.Price;
                    offer.Owned = Items.Owns(s, offer.Id);
                    break;
                case TrunkKind.Lantern:
                    if (!LanternOf(offer.Id, out TrunkLanterns source, out int index)) return false;
                    offer.Owned = source.Owns(s, index);
                    break;
            }
            offer.Price = offer.Sale ? SalePrice(offer.FullPrice) : offer.FullPrice;
            return true;
        }

        static TrunkStock StockOf(string code)
        {
            if (string.IsNullOrEmpty(code)) return null;
            foreach (TrunkStock stock in Stock)
            {
                bool prefixed = stock.Prefix.EndsWith(":");
                if (prefixed ? code.StartsWith(stock.Prefix, StringComparison.Ordinal) && code.Length > stock.Prefix.Length : code == stock.Prefix) return stock;
            }
            return null;
        }

        static bool LanternOf(string id, out TrunkLanterns source, out int index)
        {
            source = null;
            index = -1;
            int colon = id.IndexOf(':');
            if (colon <= 0) return false;
            string sourceId = id.Substring(0, colon);
            foreach (TrunkLanterns l in Lanterns)
                if (l.Id == sourceId && int.TryParse(id.Substring(colon + 1), System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out index) && index >= 0 && index < l.Count)
                {
                    source = l;
                    return true;
                }
            index = -1;
            return false;
        }

        public static bool IsCode(string code) => StockOf(code) != null;

        public static TrunkBuy Buy(GameState s, IReadOnlyList<ItemDef> items, long clock, int slot, Func<string> name, Func<double> roll)
        {
            var buy = new TrunkBuy();
            TrunkShelf shelf = Visit(s, items, clock);
            if (!shelf.Open) return buy;
            TrunkOffer offer = default;
            bool found = false;
            foreach (TrunkOffer o in shelf.Offers)
                if (o.Slot == slot) { offer = o; found = true; }
            if (!found || !offer.CanBuy || s.GoldenCarrots < offer.Price) return buy;
            s.GoldenCarrots -= offer.Price;
            s.Trunk.Bought |= 1 << slot;
            buy.Ok = true;
            buy.Offer = offer;
            long day = DayOf(clock);
            switch (offer.Kind)
            {
                case TrunkKind.SeasonRabbit:
                    buy.Rabbit = Rabbits.ArriveKind(s, offer.Id, name());
                    break;
                case TrunkKind.Hat:
                    ItemDef def = Items.DefOf(items, offer.Id);
                    buy.Hat = Items.Grant(s, def, def.Tier);
                    break;
                case TrunkKind.Jars:
                    buy.Jars = QuestChests.GiveJars(s, offer.Amount);
                    buy.JarClovers = offer.Amount - buy.Jars;
                    break;
                case TrunkKind.Clovers:
                    s.Clovers += offer.Amount;
                    break;
                case TrunkKind.Lantern:
                    LanternOf(offer.Id, out TrunkLanterns source, out int index);
                    source.Grant(s, index);
                    break;
                case TrunkKind.GoldenCall:
                    buy.Rabbit = Shop.CallGolden(s, roll(), name(), roll(), day);
                    buy.Second = Shop.CallGolden(s, roll(), name(), roll(), day);
                    break;
            }
            return buy;
        }
    }
}
