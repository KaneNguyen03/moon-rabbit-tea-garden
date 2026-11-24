using System;
using System.Collections.Generic;

namespace VuonTra.Core
{
    public enum MonthRow { Free, Premium }

    public enum MonthGiftKind { Income, Clover, Jar, Golden, Lantern, Frame, Rabbit }

    public readonly struct MonthGift
    {
        public readonly MonthGiftKind Kind;
        public readonly int Amount;
        public readonly int Golden;

        public MonthGift(MonthGiftKind kind, int amount, int golden = 0)
        {
            Kind = kind;
            Amount = amount;
            Golden = golden;
        }
    }

    public struct MonthClaim
    {
        public MonthGift Gift;
        public BigNum Coins;
        public int Jars, JarClovers;
        public Rabbit Rabbit;
    }

    public struct MonthBundle
    {
        public int Golden, Clovers, Jars, IncomeTiers;
        public bool Lantern, Frame, Rabbit;
    }

    public static class MonthPass
    {
        public const int Tiers = 30;
        public const int PointsPerTier = 40;
        public const int MaxPoints = Tiers * PointsPerTier;
        public const int AllTiers = (1 << Tiers) - 1;
        public const int KeepDays = 7;
        public const string ProductId = "vuontra.monthpass";
        public const string ReferencePrice = "129.000 ₫";

        public const int IncomeSeconds = 120;
        public const int FreeClovers = 1, FreeJars = 1;
        public const int BonusEvery = 10, BonusGolden = 5;
        public const int PremiumGolden = 5, PremiumJars = 2;
        public const int LanternTier = 10, FrameTier = 20, RabbitTier = 30;
        public const int RabbitRarity = Photos.GoldTier;
        public const int Choices = 3;
        public const int Months = 12;
        public const int AllMonths = (1 << Months) - 1;
        public const int PlainFrame = -1;

        public static readonly string[] LanternColors =
        {
            "F2C14E", "E87A8C", "A8D672", "9C8FE0", "F4A259", "6EC5E9",
            "F7E07A", "D97BB4", "FFB347", "D9824B", "B7C77A", "E0605A",
        };

        public static readonly string[] FrameColors =
        {
            "F8E3A3", "F6C9CF", "D6EBC0", "D9D2F2", "F9D3B0", "CDE9F4",
            "FBF0C2", "EFCDE2", "FFDDB0", "EFCBB0", "DEE5C2", "F2C1BC",
        };

        public static long MonthOf(long day)
        {
            DateTime d = Seasons.DateOf(day);
            return d.Year * 12L + d.Month - 1;
        }

        public static int CalendarMonth(long month) => (int)(((month % Months) + Months) % Months);

        public static int MonthNumber(long month) => CalendarMonth(month) + 1;

        public static long FirstDayOf(long month)
        {
            long year = month >= 0 ? month / Months : (month - Months + 1) / Months;
            return GiftCodes.Day((int)year, CalendarMonth(month) + 1, 1);
        }

        public static int DaysLeft(long day) => (int)(FirstDayOf(MonthOf(day) + 1) - day);

        public static MonthGift GiftOf(MonthRow row, int tier)
        {
            if (row == MonthRow.Premium)
            {
                switch (tier)
                {
                    case LanternTier: return new MonthGift(MonthGiftKind.Lantern, 1);
                    case FrameTier: return new MonthGift(MonthGiftKind.Frame, 1);
                    case RabbitTier: return new MonthGift(MonthGiftKind.Rabbit, RabbitRarity);
                    default: return tier % 2 == 1 ? new MonthGift(MonthGiftKind.Golden, PremiumGolden) : new MonthGift(MonthGiftKind.Jar, PremiumJars);
                }
            }
            int bonus = tier % BonusEvery == 0 ? BonusGolden : 0;
            switch ((tier - 1) % 3)
            {
                case 0: return new MonthGift(MonthGiftKind.Income, IncomeSeconds, bonus);
                case 1: return new MonthGift(MonthGiftKind.Clover, FreeClovers, bonus);
                default: return new MonthGift(MonthGiftKind.Jar, FreeJars, bonus);
            }
        }

        static bool Valid(int tier) => tier >= 1 && tier <= Tiers;

        static int Bit(int tier) => 1 << (tier - 1);

        public static int TierCount(int tiers)
        {
            int n = 0;
            for (int t = 1; t <= Tiers; t++) if ((tiers & Bit(t)) != 0) n++;
            return n;
        }

        public static void Roll(GameState s, long day, IReadOnlyList<StationDef> defs, Modifiers m)
        {
            long month = MonthOf(day);
            if (month <= s.PassMonth) return;
            if (s.PassMonth >= 0) MailLeftovers(s, day, defs, m);
            s.PassMonth = month;
            s.PassPoints = 0;
            s.PassFreeClaimed = 0;
            s.PassPremiumClaimed = 0;
        }

        static void MailLeftovers(GameState s, long day, IReadOnlyList<StationDef> defs, Modifiers m)
        {
            int reached = ReachedMask(TierOf(s.PassPoints));
            int free = reached & ~s.PassFreeClaimed;
            if (free != 0)
            {
                int incomeTiers = Bundle(false, free).IncomeTiers;
                Mailbox.AddMonth(s, new Letter
                {
                    Id = "month-" + s.PassMonth + "-free", Kind = LetterKind.MonthPass, Day = day, Month = s.PassMonth, Tiers = free,
                    Coins = IncomeOf(defs, s, m) * incomeTiers,
                });
            }
            int premium = s.PassBoughtMonth == s.PassMonth ? reached & ~s.PassPremiumClaimed : 0;
            if (premium != 0)
                Mailbox.AddMonth(s, new Letter { Id = "month-" + s.PassMonth + "-premium", Kind = LetterKind.MonthPass, Day = day, Month = s.PassMonth, Premium = true, Tiers = premium });
        }

        static int ReachedMask(int tier) => tier <= 0 ? 0 : tier >= Tiers ? AllTiers : (1 << tier) - 1;

        static int TierOf(int points) => Math.Max(0, Math.Min(Tiers, points / PointsPerTier));

        public static void AddPoints(GameState s, long day, int points)
        {
            if (MonthOf(day) > s.PassMonth) Roll(s, day, null, Modifiers.None);
            if (MonthOf(day) != s.PassMonth || points <= 0) return;
            s.PassPoints = Math.Min(MaxPoints, s.PassPoints + points);
        }

        public static int Points(GameState s, long day) => MonthOf(day) == s.PassMonth ? s.PassPoints : 0;

        public static int Tier(GameState s, long day) => TierOf(Points(s, day));

        public static bool Premium(GameState s, long day) => s.PassBoughtMonth >= 0 && s.PassBoughtMonth == MonthOf(day) && s.PassMonth == s.PassBoughtMonth;

        public static bool CanBuy(GameState s, long day) => MonthOf(day) >= s.PassMonth && s.PassBoughtMonth != Math.Max(MonthOf(day), s.PassMonth);

        public static bool Claimed(GameState s, MonthRow row, int tier)
            => Valid(tier) && ((row == MonthRow.Free ? s.PassFreeClaimed : s.PassPremiumClaimed) & Bit(tier)) != 0;

        public static bool Ready(GameState s, MonthRow row, int tier, long day)
            => Valid(tier) && tier <= Tier(s, day) && !Claimed(s, row, tier) && (row == MonthRow.Free || Premium(s, day));

        public static int ReadyCount(GameState s, long day)
        {
            int n = 0;
            for (int t = 1; t <= Tiers; t++)
            {
                if (Ready(s, MonthRow.Free, t, day)) n++;
                if (Ready(s, MonthRow.Premium, t, day)) n++;
            }
            return n;
        }

        public static bool Buy(GameState s, long day)
        {
            Roll(s, day, null, Modifiers.None);
            if (!CanBuy(s, day)) return false;
            s.PassBoughtMonth = s.PassMonth;
            for (int t = 1; t <= Tier(s, day); t++)
                if (t != RabbitTier) Claim(null, s, Modifiers.None, MonthRow.Premium, t, day, null, null, out _);
            return true;
        }

        public static bool Claim(IReadOnlyList<StationDef> defs, GameState s, Modifiers m, MonthRow row, int tier, long day, string kindId, string name, out MonthClaim claim)
        {
            claim = default;
            if (!Ready(s, row, tier, day)) return false;
            MonthGift gift = GiftOf(row, tier);
            if (gift.Kind == MonthGiftKind.Rabbit && Array.IndexOf(KindChoices(s.PassMonth), kindId) < 0) return false;
            if (row == MonthRow.Free) s.PassFreeClaimed |= Bit(tier);
            else s.PassPremiumClaimed |= Bit(tier);
            claim.Gift = gift;
            int cal = CalendarMonth(s.PassMonth);
            switch (gift.Kind)
            {
                case MonthGiftKind.Income:
                    claim.Coins = IncomeOf(defs, s, m);
                    s.Coins += claim.Coins;
                    break;
                case MonthGiftKind.Clover:
                    s.Clovers += gift.Amount;
                    break;
                case MonthGiftKind.Jar:
                    claim.Jars = QuestChests.GiveJars(s, gift.Amount);
                    claim.JarClovers = gift.Amount - claim.Jars;
                    break;
                case MonthGiftKind.Golden:
                    s.GoldenCarrots += gift.Amount;
                    break;
                case MonthGiftKind.Lantern:
                    s.MonthLanterns |= 1 << cal;
                    s.LanternColor = LanternIndex(cal);
                    break;
                case MonthGiftKind.Frame:
                    s.MonthFrames |= 1 << cal;
                    s.PhotoFrame = cal;
                    break;
                case MonthGiftKind.Rabbit:
                    claim.Rabbit = Rabbits.ArriveKind(s, kindId, name);
                    break;
            }
            s.GoldenCarrots += gift.Golden;
            return true;
        }

        public static BigNum IncomeOf(IReadOnlyList<StationDef> defs, GameState s, Modifiers m)
        {
            var floor = new BigNum(IncomeSeconds);
            if (defs == null) return floor;
            BigNum bySeconds = Economy.IncomePerSecond(defs, s) * (m.IncomeMultiplier * m.SpeedMultiplier * IncomeSeconds);
            return bySeconds > floor ? bySeconds : floor;
        }

        public static string[] KindChoices(long month)
        {
            IReadOnlyList<RabbitKind> kinds = RabbitKinds.OfTier(RabbitRarity);
            var picked = new string[Math.Min(Choices, kinds.Count)];
            int start = (int)(((month * Choices) % kinds.Count + kinds.Count) % kinds.Count);
            for (int i = 0; i < picked.Length; i++) picked[i] = kinds[(start + i) % kinds.Count].Id;
            return picked;
        }

        public static int LanternIndex(int cal) => Decor.LanternColors.Length + cal;

        public static int LanternMonth(int color) => color >= Decor.LanternColors.Length && color < Decor.LanternColors.Length + Months ? color - Decor.LanternColors.Length : -1;

        public static string LanternHex(int color)
        {
            int cal = LanternMonth(color);
            if (cal >= 0) return LanternColors[cal];
            int fair = Fairs.LanternFair(color);
            if (fair >= 0) return Fairs.All[fair].Hex;
            return Decor.LanternColors[Math.Max(0, Math.Min(Decor.LanternColors.Length - 1, color))];
        }

        public static bool OwnsLantern(GameState s, int cal) => cal >= 0 && cal < Months && (s.MonthLanterns & (1 << cal)) != 0;

        public static bool OwnsFrame(GameState s, int cal) => cal >= 0 && cal < Months && (s.MonthFrames & (1 << cal)) != 0;

        public static bool HangLantern(GameState s, int cal)
        {
            if (!OwnsLantern(s, cal)) return false;
            s.LanternColor = LanternIndex(cal);
            return true;
        }

        public static bool WearFrame(GameState s, int cal)
        {
            if (cal != PlainFrame && !OwnsFrame(s, cal)) return false;
            s.PhotoFrame = cal;
            return true;
        }

        public static MonthBundle Bundle(bool premium, int tiers)
        {
            var b = new MonthBundle();
            MonthRow row = premium ? MonthRow.Premium : MonthRow.Free;
            for (int t = 1; t <= Tiers; t++)
            {
                if ((tiers & Bit(t)) == 0) continue;
                MonthGift gift = GiftOf(row, t);
                b.Golden += gift.Golden;
                switch (gift.Kind)
                {
                    case MonthGiftKind.Income: b.IncomeTiers++; break;
                    case MonthGiftKind.Clover: b.Clovers += gift.Amount; break;
                    case MonthGiftKind.Jar: b.Jars += gift.Amount; break;
                    case MonthGiftKind.Golden: b.Golden += gift.Amount; break;
                    case MonthGiftKind.Lantern: b.Lantern = true; break;
                    case MonthGiftKind.Frame: b.Frame = true; break;
                    case MonthGiftKind.Rabbit: b.Rabbit = true; break;
                }
            }
            return b;
        }

        public static MonthBundle BundleOf(Letter letter) => Bundle(letter.Premium, letter.Tiers);

        public static Rabbit GrantLetter(GameState s, Letter letter, string name, double kindRoll)
        {
            MonthBundle b = BundleOf(letter);
            int cal = CalendarMonth(letter.Month);
            s.Coins += letter.Coins;
            s.Clovers += b.Clovers;
            s.GoldenCarrots += b.Golden;
            QuestChests.GiveJars(s, b.Jars);
            if (b.Lantern) s.MonthLanterns |= 1 << cal;
            if (b.Frame) s.MonthFrames |= 1 << cal;
            if (!b.Rabbit) return null;
            string[] kinds = KindChoices(letter.Month);
            if (double.IsNaN(kindRoll) || kindRoll < 0d) kindRoll = 0d;
            return Rabbits.ArriveKind(s, kinds[Math.Min(kinds.Length - 1, (int)Math.Floor(kindRoll * kinds.Length))], name);
        }
    }

    public sealed class MonthPassWatch
    {
        long _month = -1, _bought = -1;
        int _tier;

        public void Reset(GameState s, long day)
        {
            _month = s.PassMonth;
            _tier = MonthPass.Tier(s, day);
            _bought = s.PassBoughtMonth;
        }

        public void Note(GameState s, long day, IAnalytics a)
        {
            if (s.PassMonth != _month)
            {
                _month = s.PassMonth;
                _tier = 0;
            }
            int tier = MonthPass.Tier(s, day);
            bool premium = MonthPass.Premium(s, day);
            if (s.PassBoughtMonth != _bought)
            {
                _bought = s.PassBoughtMonth;
                if (premium) a.LogMonthPassBought(MonthPass.MonthNumber(s.PassMonth), tier);
            }
            for (int t = _tier + 1; t <= tier; t++) a.LogMonthPassTier(t, premium, MonthPass.MonthNumber(s.PassMonth));
            _tier = Math.Max(_tier, tier);
        }
    }
}
