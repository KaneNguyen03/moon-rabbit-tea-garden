using System;
using System.Collections.Generic;

namespace VuonTra.Core
{
    public sealed class OfferDef
    {
        public string Id;
        public int Level;
        public long Seconds;
        public int Golden, Jars, Clovers;
        public string Hat;
        public int RabbitTier = -1;

        public bool IsStarter => Id == Offers.Starter;
    }

    public sealed class OfferRecord
    {
        public string Id;
        public long Start;
        public bool Shown, Bought, Closed;
    }

    public enum OfferState { Unseen, Active, Bought, Expired }

    public sealed class OfferNews
    {
        public readonly List<string> Started = new List<string>();
        public readonly List<string> Expired = new List<string>();
    }

    public struct OfferGift
    {
        public string Id;
        public int Golden, Jars, Clovers;
        public Item Hat;
        public Rabbit Rabbit;
    }

    public static class Offers
    {
        public const string Starter = "vuontra.starter";
        public const string Milestone10 = "vuontra.milestone.10", Milestone20 = "vuontra.milestone.20", Milestone30 = "vuontra.milestone.30";
        public const string StarterHat = "hat-no-trang";
        public const int StarterLevel = 4;
        public const int MilestoneRabbitTier = 3;
        public const long DaySeconds = 86400L;
        public const long StarterSeconds = 72L * 3600L, MilestoneSeconds = 48L * 3600L;

        public static readonly OfferDef[] All =
        {
            new OfferDef { Id = Starter, Level = StarterLevel, Seconds = StarterSeconds, Golden = 80, Jars = 3, Clovers = 3, Hat = StarterHat },
            Milestone(Milestone10, 10),
            Milestone(Milestone20, 20),
            Milestone(Milestone30, 30),
        };

        static OfferDef Milestone(string id, int level) => new OfferDef
        {
            Id = id, Level = level, Seconds = MilestoneSeconds, Golden = 150, Jars = 5, RabbitTier = MilestoneRabbitTier
        };

        public static OfferDef Find(string id)
        {
            for (int i = 0; i < All.Length; i++)
                if (All[i].Id == id) return All[i];
            return null;
        }

        public static OfferRecord RecordOf(GameState s, string id)
        {
            for (int i = 0; i < s.OfferRecords.Count; i++)
                if (s.OfferRecords[i].Id == id) return s.OfferRecords[i];
            return null;
        }

        public static long Clock(GameState s, long wallSeconds)
        {
            long played = (long)Math.Floor(Math.Max(0d, s.PlaySeconds - s.OfferPlayMark));
            long clock = wallSeconds;
            if (s.MaxDaySeen >= 0) clock = Math.Max(clock, s.MaxDaySeen * DaySeconds);
            if (s.OfferClock >= 0) clock = Math.Max(clock, s.OfferClock + played);
            s.OfferPlayMark = s.PlaySeconds < s.OfferPlayMark ? s.PlaySeconds : s.OfferPlayMark + played;
            s.OfferClock = clock;
            return clock;
        }

        public static long EndOf(OfferRecord record, OfferDef def) => record.Start + def.Seconds;

        public static OfferState StateOf(GameState s, string id, long clock)
        {
            OfferDef def = Find(id);
            OfferRecord record = def != null ? RecordOf(s, id) : null;
            if (record == null) return OfferState.Unseen;
            if (record.Bought) return OfferState.Bought;
            return record.Closed || clock >= EndOf(record, def) ? OfferState.Expired : OfferState.Active;
        }

        public static long SecondsLeft(GameState s, string id, long clock)
        {
            if (StateOf(s, id, clock) != OfferState.Active) return 0L;
            return EndOf(RecordOf(s, id), Find(id)) - clock;
        }

        public static bool CanBuy(GameState s, string id, long clock) => StateOf(s, id, clock) == OfferState.Active;

        public static List<OfferDef> Active(GameState s, long clock)
        {
            var list = new List<OfferDef>();
            foreach (OfferDef def in All)
                if (StateOf(s, def.Id, clock) == OfferState.Active) list.Add(def);
            list.Sort((a, b) => SecondsLeft(s, a.Id, clock).CompareTo(SecondsLeft(s, b.Id, clock)));
            return list;
        }

        public static OfferDef Leading(GameState s, long clock)
        {
            OfferDef best = null;
            long bestLeft = long.MaxValue;
            for (int i = 0; i < All.Length; i++)
            {
                long left = SecondsLeft(s, All[i].Id, clock);
                if (left > 0L && left < bestLeft) { best = All[i]; bestLeft = left; }
            }
            return best;
        }

        public static OfferDef ToShow(GameState s, long clock)
        {
            foreach (OfferDef def in Active(s, clock))
                if (!RecordOf(s, def.Id).Shown) return def;
            return null;
        }

        public static bool MarkShown(GameState s, string id)
        {
            OfferRecord record = RecordOf(s, id);
            if (record == null || record.Shown) return false;
            record.Shown = true;
            return true;
        }

        public static OfferNews Tick(GameState s, long clock)
        {
            var news = new OfferNews();
            foreach (OfferDef def in All)
            {
                OfferRecord record = RecordOf(s, def.Id);
                if (record == null || record.Bought || record.Closed || clock < EndOf(record, def)) continue;
                record.Closed = true;
                news.Expired.Add(def.Id);
            }

            int level = Garden.Level(s);
            OfferDef highest = null;
            foreach (OfferDef def in All)
            {
                if (def.IsStarter || level < def.Level || RecordOf(s, def.Id) != null) continue;
                if (highest == null || def.Level > highest.Level) highest = def;
            }
            foreach (OfferDef def in All)
            {
                if (level < def.Level || RecordOf(s, def.Id) != null) continue;
                bool open = def.IsStarter || def == highest;
                s.OfferRecords.Add(new OfferRecord { Id = def.Id, Start = clock, Closed = !open });
                if (open) news.Started.Add(def.Id);
            }
            return news;
        }

        public static bool Bought(GameState s, string id)
        {
            OfferRecord record = RecordOf(s, id);
            return record != null && record.Bought;
        }

        public static OfferGift Deliver(GameState s, OfferDef def, IReadOnlyList<ItemDef> items, string rabbitName, double roll)
        {
            OfferRecord record = RecordOf(s, def.Id);
            if (record == null)
            {
                record = new OfferRecord { Id = def.Id, Start = Math.Max(0L, s.OfferClock), Shown = true };
                s.OfferRecords.Add(record);
            }
            record.Bought = true;

            var gift = new OfferGift { Id = def.Id, Golden = def.Golden, Clovers = def.Clovers };
            int room = Math.Max(0, MoonJars.Cap - s.MoonJars);
            gift.Jars = Math.Min(room, def.Jars);
            s.MoonJars += gift.Jars;
            gift.Golden += (def.Jars - gift.Jars) * MoonJars.Price;

            if (def.Hat != null)
            {
                ItemDef hat = Items.DefOf(items, def.Hat);
                if (hat != null && !Items.Owns(s, def.Hat)) gift.Hat = Items.Grant(s, hat, hat.Tier);
                else gift.Golden += hat != null && hat.Price > 0 ? hat.Price : GiftCodes.HatGoldenFallback;
            }
            if (def.RabbitTier >= 0) gift.Rabbit = Rabbits.ArriveKind(s, UnmetKind(s, def.RabbitTier, roll), rabbitName);

            s.GoldenCarrots += gift.Golden;
            s.Clovers += gift.Clovers;
            return gift;
        }

        public static string UnmetKind(GameState s, int tier, double roll)
        {
            var unmet = new List<RabbitKind>();
            foreach (RabbitKind kind in RabbitKinds.OfTier(tier))
                if (!s.SeenKinds.Contains(kind.Id)) unmet.Add(kind);
            if (unmet.Count == 0) return RabbitKinds.Pick(tier, roll);
            if (double.IsNaN(roll) || roll < 0d) roll = 0d;
            return unmet[Math.Min(unmet.Count - 1, (int)Math.Floor(roll * unmet.Count))].Id;
        }
    }
}
