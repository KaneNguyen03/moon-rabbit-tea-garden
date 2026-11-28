using System;
using System.Collections.Generic;

namespace VuonTra.Core
{
    public enum StoryKind
    {
        Leaves,
        Clover,
        Stranger,
        Bond,
        Doze
    }

    public sealed class Story
    {
        public StoryKind Kind;
        public long Rabbit;
        public long Friend;
        public string Guest;
        public int Amount;
        public int Line;
        public long Night;
    }

    public sealed class OvernightRecord
    {
        public long Mark = -1;
        public long StrangerDay = -1;
        public long Week = -1;
        public int WeekClovers;
        public List<Story> Waiting = new List<Story>();
    }

    public struct OvernightRoll
    {
        public List<Story> Told;
        public List<Story> Dropped;

        public int Count => Told != null ? Told.Count : 0;
    }

    public struct StoryClaim
    {
        public bool Ok;
        public StoryKind Kind;
        public int Clovers;
        public int Leaves;
        public int Bond;
        public Rabbit Arrived;
    }

    public static class Overnight
    {
        public const long MinSeconds = 30 * 60;
        public const long LongNightSeconds = 6 * 3600;
        public const int MaxWaiting = 2;

        public const int LeavesLeast = 2, LeavesMost = 5;
        public const int CloverGift = 1;
        public const int WeekCloverCap = 7;
        public const int WeekGoldenCap = 0;
        public const int StrangerTopTier = 2;
        public const long StrangerEveryDays = 3;
        public const int BondPoints = 10;
        public const int DozeLines = 2;

        public const int LeavesWeight = 30, CloverWeight = 20, StrangerWeight = 10, BondWeight = 15, DozeWeight = 25;

        public const long DaySeconds = 86400;
        public const long WeekDays = 7;

        static readonly StoryKind[] Kinds = { StoryKind.Leaves, StoryKind.Clover, StoryKind.Stranger, StoryKind.Bond, StoryKind.Doze };
        static readonly string[] Ids = { "leaves", "clover", "stranger", "bond", "doze" };

        public static string IdOf(StoryKind kind) => Ids[(int)kind];

        public static bool TryKind(string id, out StoryKind kind)
        {
            int at = Array.IndexOf(Ids, id);
            kind = at >= 0 ? Kinds[at] : StoryKind.Doze;
            return at >= 0;
        }

        public static int WeightOf(StoryKind kind)
        {
            switch (kind)
            {
                case StoryKind.Leaves: return LeavesWeight;
                case StoryKind.Clover: return CloverWeight;
                case StoryKind.Stranger: return StrangerWeight;
                case StoryKind.Bond: return BondWeight;
                default: return DozeWeight;
            }
        }

        public static int Most(StoryKind kind)
        {
            switch (kind)
            {
                case StoryKind.Leaves: return LeavesMost;
                case StoryKind.Clover: return CloverGift;
                case StoryKind.Bond: return BondPoints;
                default: return 0;
            }
        }

        public static int StoriesFor(long seconds) => seconds >= LongNightSeconds ? 2 : seconds >= MinSeconds ? 1 : 0;

        public static long DayOf(long unixSeconds) => FloorDiv(unixSeconds, DaySeconds);

        public static long WeekOf(long unixSeconds) => FloorDiv(DayOf(unixSeconds), WeekDays);

        static long FloorDiv(long a, long b) => a >= 0 ? a / b : -((-a + b - 1) / b);

        public static OvernightRoll Roll(GameState s, long leftAt, long now)
        {
            var roll = new OvernightRoll { Told = new List<Story>(), Dropped = new List<Story>() };
            OvernightRecord r = s.Overnight;
            if (leftAt <= 0) { r.Mark = Math.Max(r.Mark, now); return roll; }
            long from = Math.Max(leftAt, r.Mark);
            r.Mark = Math.Max(r.Mark, Math.Max(leftAt, now));
            int count = StoriesFor(now - from);
            if (count == 0) return roll;

            long week = WeekOf(now);
            if (week > r.Week) { r.Week = week; r.WeekClovers = 0; }
            long day = DayOf(now);
            var rng = new Dice(Seed(s.Profile != null ? s.Profile.Code : null, from));
            for (int i = 0; i < count; i++)
            {
                Story story = Pick(s, rng, day, roll.Told);
                if (story == null) break;
                story.Night = from;
                roll.Told.Add(story);
                r.Waiting.Add(story);
            }
            while (r.Waiting.Count > MaxWaiting)
            {
                roll.Dropped.Add(r.Waiting[0]);
                r.Waiting.RemoveAt(0);
            }
            return roll;
        }

        static Story Pick(GameState s, Dice rng, long day, List<Story> told)
        {
            int total = 0;
            var weights = new int[Kinds.Length];
            for (int k = 0; k < Kinds.Length; k++)
            {
                if (!Open(s, Kinds[k], day, told)) continue;
                weights[k] = WeightOf(Kinds[k]);
                total += weights[k];
            }
            if (total <= 0) return null;
            int at = rng.Next(total);
            StoryKind kind = Kinds[0];
            for (int k = 0; k < Kinds.Length; k++)
            {
                if (at < weights[k]) { kind = Kinds[k]; break; }
                at -= weights[k];
            }
            return Make(s, rng, kind, day);
        }

        static bool Open(GameState s, StoryKind kind, long day, List<Story> told)
        {
            foreach (Story t in told) if (t.Kind == kind) return false;
            switch (kind)
            {
                case StoryKind.Leaves: return s.Rabbits.Count > 0 && Recipes.Unlocked(s);
                case StoryKind.Clover: return s.Overnight.WeekClovers + CloverGift <= WeekCloverCap;
                case StoryKind.Stranger: return StrangerDue(s, day) && UnmetGuests(s).Count > 0;
                case StoryKind.Bond: return Pairs(s).Count > 0;
                default: return s.Rabbits.Count > 0;
            }
        }

        static Story Make(GameState s, Dice rng, StoryKind kind, long day)
        {
            var story = new Story { Kind = kind };
            switch (kind)
            {
                case StoryKind.Leaves:
                    story.Rabbit = s.Rabbits[rng.Next(s.Rabbits.Count)].Id;
                    story.Amount = LeavesLeast + rng.Next(LeavesMost - LeavesLeast + 1);
                    break;
                case StoryKind.Clover:
                    story.Amount = CloverGift;
                    s.Overnight.WeekClovers += CloverGift;
                    break;
                case StoryKind.Stranger:
                    List<RabbitKind> guests = UnmetGuests(s);
                    story.Guest = guests[rng.Next(guests.Count)].Id;
                    s.Overnight.StrangerDay = day;
                    break;
                case StoryKind.Bond:
                    List<long[]> pairs = Pairs(s);
                    long[] pair = pairs[rng.Next(pairs.Count)];
                    story.Rabbit = pair[0];
                    story.Friend = pair[1];
                    story.Amount = BondPoints;
                    break;
                default:
                    story.Rabbit = s.Rabbits[rng.Next(s.Rabbits.Count)].Id;
                    story.Line = rng.Next(DozeLines);
                    break;
            }
            return story;
        }

        public static bool StrangerDue(GameState s, long day)
        {
            foreach (Story w in s.Overnight.Waiting) if (w.Kind == StoryKind.Stranger) return false;
            return s.Overnight.StrangerDay < 0 || day - s.Overnight.StrangerDay >= StrangerEveryDays;
        }

        public static List<RabbitKind> UnmetGuests(GameState s)
        {
            var list = new List<RabbitKind>();
            foreach (RabbitKind kind in RabbitKinds.All)
                if (kind.Tier <= StrangerTopTier && kind.Season == Season.None && !kind.NewMoonOnly && !s.SeenKinds.Contains(kind.Id)) list.Add(kind);
            return list;
        }

        public static List<long[]> Pairs(GameState s)
        {
            var pairs = new List<long[]>();
            for (int i = 0; i < s.Rabbits.Count; i++)
            {
                Rabbit a = s.Rabbits[i];
                if (!BondFits(a)) continue;
                for (int j = i + 1; j < s.Rabbits.Count; j++)
                {
                    Rabbit b = s.Rabbits[j];
                    if (BondFits(b) && (a.Station < 0) == (b.Station < 0)) pairs.Add(new[] { a.Id, b.Id });
                }
            }
            return pairs;
        }

        static bool BondFits(Rabbit r)
        {
            int before = Rabbits.HeartsOf(r.Affection), after = Rabbits.HeartsOf(Math.Min(Rabbits.AffectionMax, Math.Max(0, r.Affection) + BondPoints));
            return !(before < Rabbits.GiftHearts && after >= Rabbits.GiftHearts) && !(before < Rabbits.HeartMax && after >= Rabbits.HeartMax);
        }

        public static StoryClaim Claim(GameState s, Story story, string name)
        {
            var claim = new StoryClaim();
            if (story == null || !s.Overnight.Waiting.Remove(story)) return claim;
            claim.Ok = true;
            claim.Kind = story.Kind;
            switch (story.Kind)
            {
                case StoryKind.Leaves:
                    int before = s.RecipeLeaves;
                    Recipes.Earn(s, story.Amount);
                    claim.Leaves = s.RecipeLeaves - before;
                    break;
                case StoryKind.Clover:
                    s.Clovers += story.Amount;
                    claim.Clovers = story.Amount;
                    break;
                case StoryKind.Stranger:
                    if (RabbitKinds.Find(story.Guest) != null) claim.Arrived = Rabbits.ArriveKind(s, story.Guest, name);
                    break;
                case StoryKind.Bond:
                    Rabbit a = Rabbits.Find(s, story.Rabbit), b = Rabbits.Find(s, story.Friend);
                    if (a != null && b != null && BondFits(a) && BondFits(b))
                    {
                        Rabbits.AddBond(s, a, story.Amount);
                        Rabbits.AddBond(s, b, story.Amount);
                        claim.Bond = story.Amount;
                    }
                    break;
            }
            return claim;
        }

        public static ulong Seed(string code, long night)
        {
            ulong h = 14695981039346656037UL;
            if (code != null)
                foreach (char c in code) { h ^= c; h *= 1099511628211UL; }
            for (int i = 0; i < 8; i++) { h ^= (ulong)((night >> (8 * i)) & 0xff); h *= 1099511628211UL; }
            return h;
        }

        sealed class Dice
        {
            ulong _state;

            public Dice(ulong seed) => _state = seed;

            ulong NextBits()
            {
                _state += 0x9E3779B97F4A7C15UL;
                ulong z = _state;
                z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
                z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
                return z ^ (z >> 31);
            }

            public int Next(int bound) => bound <= 1 ? 0 : (int)(NextBits() % (ulong)bound);
        }
    }
}
