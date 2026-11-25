using System;
using System.Collections.Generic;

namespace VuonTra.Core
{
    /// <summary>One collected rabbit. Tier 0 is the most common (silver), 5 the rarest (red).</summary>
    public sealed class Rabbit
    {
        public long Id;
        public int Tier;
        /// <summary>Which of the tier's kinds it is (a <see cref="RabbitKind"/> id). Use <see cref="Rabbits.KindOf"/> to read it safely.</summary>
        public string Kind;
        /// <summary>True on the rabbit that first brought its kind into the album. Not saved: it is for the moment it arrives.</summary>
        public bool FirstOfKind;
        public string Name;
        /// <summary>1 and up; each level adds to what the rabbit is worth. The tier sets the cap.</summary>
        public int Level = 1;
        /// <summary>Index of the station it works at, or -1 when resting.</summary>
        public int Station = -1;
        /// <summary>
        /// How close the rabbit and the player are, in bond points from 0 to <see cref="Rabbits.AffectionMax"/> (#58): ten for
        /// each day it is stroked, more for each fish it is fed. Its hearts follow from them (<see cref="Rabbits.HeartsOf(int)"/>).
        /// </summary>
        public int Affection;
        /// <summary>The day it was last stroked (see <see cref="Rabbits.Pet"/>), or -1 when never.</summary>
        public long PetDay = -1;
        /// <summary>Index into <see cref="Shop.Dyes"/> of the colour its fur is dyed, or -1 for its own coat (#44).</summary>
        public int Dye = -1;
    }

    /// <summary>What one touch of a rabbit came to.</summary>
    public struct PetResult
    {
        /// <summary>False when there was no rabbit to touch; nothing else is set then.</summary>
        public bool Found;
        /// <summary>The first stroke of this day: the bond grew (unless already at its cap) and tea power was paid.</summary>
        public bool FirstToday;
        /// <summary>Tea power paid for this touch; zero after the first stroke of the day unless it was a wake.</summary>
        public double Energy;
        /// <summary>The bond just reached <see cref="Rabbits.LineHearts"/>: the rabbit has something to say.</summary>
        public bool LineUnlocked;
        /// <summary>The bond just reached <see cref="Rabbits.GiftHearts"/>: the rabbit gave its clover, already counted in the purse.</summary>
        public bool GiftGiven;
        /// <summary>The bond just reached ten hearts (#58): the rabbit gave its golden carrots, already counted in the purse.</summary>
        public bool TenHearts;
    }

    /// <summary>Which of the bond's moments one gain of points crossed (#19, #58); the gifts are already paid.</summary>
    public struct BondStep
    {
        /// <summary>The bond reached <see cref="Rabbits.LineHearts"/>: the rabbit has something to say.</summary>
        public bool LineUnlocked;
        /// <summary>The bond reached <see cref="Rabbits.GiftHearts"/>: one clover.</summary>
        public bool GiftGiven;
        /// <summary>The bond reached ten hearts: <see cref="Rabbits.TenHeartGolden"/> golden carrots, and the heart badge from now on.</summary>
        public bool TenHearts;
    }

    /// <summary>
    /// The rabbit collection: call one with clovers, merge three of a tier into one of the next,
    /// and put them to work. A working rabbit multiplies what its station is worth.
    /// </summary>
    public static class Rabbits
    {
        public const int TierCount = Rarity.Count;
        public const int SummonCost = 1;
        public const int MergeCount = 3;

        /// <summary>Silver, green, blue, purple, gold, red.</summary>
        public static readonly double[] Multipliers = { 1.1, 1.25, 1.5, 2, 3, 5 };
        /// <summary>Highest level a rabbit of each tier can reach.</summary>
        public static readonly int[] LevelCaps = { 10, 20, 30, 40, 50, 60 };
        /// <summary>Each level above 1 adds this share of the tier's multiplier.</summary>
        public const double LevelBonus = 0.05;

        static int ClampTier(int tier) => Math.Max(0, Math.Min(TierCount - 1, tier));

        /// <summary>What this rabbit multiplies its station by: tier, level, its own items, then its hearts (#58).</summary>
        public static double Multiplier(GameState s, Rabbit r)
        {
            double fromLevel = Multipliers[ClampTier(r.Tier)] * (1d + LevelBonus * (Math.Max(1, r.Level) - 1));
            return fromLevel * (1d + Items.StationBonus(s, r.Id)) * HeartMultiplier(r);
        }

        /// <summary>What a rabbit's multiplier is multiplied by at a station of its own trade.</summary>
        public const double TradeBonus = 1.5;

        public static RabbitTrade TradeOf(Rabbit r) => RabbitKinds.Find(KindOf(r)).Trade;

        /// <summary>True when the rabbit is working at a station of its trade.</summary>
        public static bool InTrade(IReadOnlyList<StationDef> defs, Rabbit r) => r.Station >= 0 && Suits(defs, r, r.Station);

        /// <summary>True when this station belongs to the rabbit's trade.</summary>
        public static bool Suits(IReadOnlyList<StationDef> defs, Rabbit r, int station)
        {
            if (defs == null || station < 0 || station >= defs.Count) return false;
            RabbitTrade trade = defs[station].Trade;
            return trade != RabbitTrade.None && trade == TradeOf(r);
        }

        /// <summary>The open stations of this rabbit's trade, in the order they open.</summary>
        public static List<int> SuitedStations(IReadOnlyList<StationDef> defs, GameState s, Rabbit r)
        {
            var list = new List<int>();
            for (int i = 0; i < defs.Count; i++)
                if (s.Levels[i] > 0 && Suits(defs, r, i) && !Layout.IsStored(defs, s, i)) list.Add(i);
            return list;
        }

        /// <summary>What the rabbit would be worth at a station: its own multiplier, half again in its trade.</summary>
        public static double MultiplierAt(IReadOnlyList<StationDef> defs, GameState s, Rabbit r, int station)
        {
            return Multiplier(s, r) * (Suits(defs, r, station) ? TradeBonus : 1d);
        }

        /// <summary>What the rabbit is worth where it is now (resting counts as no bonus to a station; a seated rabbit adds to the whole garden instead, see <see cref="RestSpots.BonusOf"/>).</summary>
        public static double WorkMultiplier(IReadOnlyList<StationDef> defs, GameState s, Rabbit r) => MultiplierAt(defs, s, r, r.Station);

        // ----- touching and stroking (#19), the bond in points (#58) -----

        /// <summary>The most hearts a rabbit can hold for the player.</summary>
        public const int HeartMax = 10;
        /// <summary>
        /// Bond points each heart starts at, from none to ten (#58): the steps of the issue (10, 30, 80 ... 8 880) and one
        /// more of ten thousand for the tenth. An old save's 0 to 5 hearts become exactly the points those hearts start at,
        /// and a day's stroke (<see cref="PetPoints"/>) still brings the first heart.
        /// </summary>
        public static readonly int[] HeartPoints = { 0, 10, 30, 80, 180, 380, 880, 1880, 3880, 8880, 18880 };
        /// <summary>The most bond points a rabbit can hold: ten hearts.</summary>
        public const int AffectionMax = 18880;
        /// <summary>What a rabbit's hearts multiply its worth by, from none to ten (#58).</summary>
        public static readonly double[] HeartMultipliers = { 1d, 1.05, 1.1, 1.2, 1.3, 1.45, 1.6, 1.8, 2d, 2.3, 2.6 };
        /// <summary>At this many hearts the rabbit says a line of its own.</summary>
        public const int LineHearts = 3;
        /// <summary>At this many hearts the rabbit gives <see cref="AffectionGiftClovers"/>, once: the old full bond.</summary>
        public const int GiftHearts = 5;
        /// <summary>The clovers given at <see cref="GiftHearts"/>.</summary>
        public const int AffectionGiftClovers = 1;
        /// <summary>Golden carrots a rabbit gives once, the day its bond reaches ten hearts (#58).</summary>
        public const int TenHeartGolden = 3;
        /// <summary>Bond points for the first stroke of a day.</summary>
        public const int PetPoints = 10;
        /// <summary>Tea power for the first stroke of a day.</summary>
        public const double PetEnergy = 5d;
        /// <summary>Tea power for waking a rabbit that nodded off at its post, every time.</summary>
        public const double DozeEnergy = 8d;

        /// <summary>Hearts for so many bond points, 0 to <see cref="HeartMax"/>.</summary>
        public static int HeartsOf(int points)
        {
            int hearts = 0;
            while (hearts < HeartMax && points >= HeartPoints[hearts + 1]) hearts++;
            return hearts;
        }

        public static int HeartsOf(Rabbit r) => r == null ? 0 : HeartsOf(r.Affection);

        /// <summary>The bond points a number of hearts starts at: how an old save's hearts become points.</summary>
        public static int PointsFor(int hearts) => HeartPoints[Math.Max(0, Math.Min(HeartMax, hearts))];

        /// <summary>Points still to go to the next heart; 0 at ten hearts.</summary>
        public static int PointsToNext(Rabbit r)
        {
            int hearts = HeartsOf(r);
            return hearts >= HeartMax ? 0 : HeartPoints[hearts + 1] - Math.Max(0, r.Affection);
        }

        /// <summary>What the rabbit's hearts multiply its worth by.</summary>
        public static double HeartMultiplier(Rabbit r) => HeartMultipliers[HeartsOf(r)];

        /// <summary>True for a rabbit with all ten hearts: it wears the heart badge.</summary>
        public static bool FullHearts(Rabbit r) => HeartsOf(r) >= HeartMax;

        /// <summary>
        /// Adds bond points, never past <see cref="AffectionMax"/>, and pays what the hearts it crosses bring: the clover at
        /// <see cref="GiftHearts"/> and the golden carrots at ten, each exactly once, at the step that reaches it.
        /// </summary>
        public static BondStep AddBond(GameState s, Rabbit r, int points)
        {
            var step = new BondStep();
            if (r == null || points <= 0) return step;
            int before = HeartsOf(r);
            r.Affection = Math.Min(AffectionMax, Math.Max(0, r.Affection) + points);
            int after = HeartsOf(r);
            step.LineUnlocked = before < LineHearts && after >= LineHearts;
            step.GiftGiven = before < GiftHearts && after >= GiftHearts;
            step.TenHearts = before < HeartMax && after >= HeartMax;
            if (step.GiftGiven) s.Clovers += AffectionGiftClovers;
            if (step.TenHearts) s.GoldenCarrots += TenHeartGolden;
            return step;
        }

        /// <summary>
        /// The player touched or stroked a rabbit on <paramref name="day"/>. The first time on a day later than the
        /// last one the bond grows by <see cref="PetPoints"/> up to its cap and <see cref="PetEnergy"/> is paid; every
        /// later touch that day, and every touch on a day the clock was turned back to, is for love alone (#78).
        /// Waking a dozing rabbit (<paramref name="wake"/>) pays <see cref="DozeEnergy"/> instead, every time.
        /// The clover and the ten-heart gift are paid exactly once, at the step that reaches them.
        /// </summary>
        public static PetResult Pet(GameState s, Rabbit r, long day, bool wake = false)
        {
            var result = new PetResult();
            if (r == null) return result;
            result.Found = true;
            result.FirstToday = day > r.PetDay;
            if (result.FirstToday)
            {
                r.PetDay = day;
                BondStep step = AddBond(s, r, PetPoints);
                result.LineUnlocked = step.LineUnlocked;
                result.GiftGiven = step.GiftGiven;
                result.TenHearts = step.TenHearts;
            }
            result.Energy = wake ? DozeEnergy : (result.FirstToday ? PetEnergy : 0d);
            if (result.Energy > 0d) s.Energy = Math.Min(Energy.Max, Math.Max(0d, s.Energy) + result.Energy);
            return result;
        }

        /// <summary>The hearts a rabbit holds for so many bond points, as marks: full ones for the bond so far, hollow for the rest.</summary>
        public static string Hearts(int points)
        {
            int full = HeartsOf(points);
            return new string('♥', full) + new string('♡', HeartMax - full);
        }

        public const int MaxNameLength = 12;

        /// <summary>
        /// Tidies a name the player typed: no line breaks or control characters, no leading or
        /// trailing spaces, at most MaxNameLength characters. Returns null when nothing is left.
        /// </summary>
        public static string CleanName(string raw)
        {
            if (raw == null) return null;
            var sb = new System.Text.StringBuilder(raw.Length);
            foreach (char c in raw)
                sb.Append(char.IsControl(c) ? ' ' : c);
            string name = sb.ToString().Trim();
            while (name.Contains("  ")) name = name.Replace("  ", " ");
            if (name.Length > MaxNameLength) name = name.Substring(0, MaxNameLength).TrimEnd();
            return name.Length == 0 ? null : name;
        }

        /// <summary>Renames a rabbit. An empty or all-space name is refused and the old one stays.</summary>
        public static bool Rename(GameState s, long id, string raw)
        {
            Rabbit r = Find(s, id);
            string name = CleanName(raw);
            if (r == null || name == null) return false;
            r.Name = name;
            return true;
        }

        /// <summary>Puts a kind in the album, marked new until it is looked at. True when it was not there before.</summary>
        public static bool See(GameState s, string kindId)
        {
            bool first = RabbitKinds.Find(kindId) != null && s.SeenKinds.Add(kindId);
            if (first) s.FreshKinds.Add(kindId);
            return first;
        }

        /// <summary>The player looked at a kind in the album: its "new" mark comes off. True when there was one.</summary>
        public static bool LookAt(GameState s, string kindId) => s.FreshKinds.Remove(kindId);

        /// <summary>The rabbit's kind, or its tier's default when it has none or one that belongs to another tier.</summary>
        public static string KindOf(Rabbit r)
        {
            RabbitKind kind = RabbitKinds.Find(r.Kind);
            return kind != null && kind.Tier == ClampTier(r.Tier) ? kind.Id : RabbitKinds.DefaultFor(r.Tier);
        }

        public static bool IsMaxLevel(Rabbit r) => r.Level >= LevelCaps[ClampTier(r.Tier)];

        /// <summary>Every ten levels a rabbit earns a star: a mark of how far it has come, nothing more.</summary>
        public const int LevelsPerStar = 10;
        public static int Stars(int level) => Math.Max(0, level) / LevelsPerStar;
        public static int Stars(Rabbit r) => Stars(r.Level);
        /// <summary>How many stars a climb from one level to another earns; never negative.</summary>
        public static int StarsEarned(int before, int after) => Math.Max(0, Stars(after) - Stars(before));

        public static BigNum LevelCost(Rabbit r)
        {
            return new BigNum(1.3).Pow(Math.Max(1, r.Level) - 1) * (100d * Math.Pow(2d, ClampTier(r.Tier)));
        }

        public static bool TryLevelUp(GameState s, long id)
        {
            Rabbit r = Find(s, id);
            if (r == null || IsMaxLevel(r)) return false;
            BigNum cost = LevelCost(r);
            if (s.Coins < cost) return false;
            s.Coins -= cost;
            r.Level++;
            return true;
        }

        /// <summary>
        /// How many levels this rabbit could gain for <paramref name="budget"/> carrots, stopping at its tier's
        /// cap, and what they cost together. Zero when not even one can be paid for.
        /// </summary>
        public static int LevelsAffordable(Rabbit r, BigNum budget, out BigNum cost)
        {
            cost = BigNum.Zero;
            int level = Math.Max(1, r.Level), cap = LevelCaps[ClampTier(r.Tier)], n = 0;
            var probe = new Rabbit { Tier = r.Tier, Level = level };
            while (probe.Level < cap)
            {
                BigNum next = LevelCost(probe);
                if (cost + next > budget) break;
                cost += next;
                probe.Level++;
                n++;
            }
            return n;
        }

        /// <summary>Levels the rabbit up as many times as the budget allows, never past its cap. Returns how many.</summary>
        public static int LevelUpMax(GameState s, long id, BigNum budget)
        {
            Rabbit r = Find(s, id);
            if (r == null) return 0;
            int n = LevelsAffordable(r, budget, out BigNum cost);
            if (n <= 0 || s.Coins < cost) return 0;
            s.Coins -= cost;
            r.Level += n;
            return n;
        }

        /// <summary>Chance of each tier from one call; they add up to 1.</summary>
        public static readonly double[] Odds = { 0.60, 0.25, 0.10, 0.04, 0.009, 0.001 };

        /// <summary>Turns a number in [0, 1) into a tier using the odds table.</summary>
        public static int RollTier(double roll) => RollTier(roll, Odds);

        /// <summary>Turns a number in [0, 1) into a tier using any odds table that adds up to 1.</summary>
        public static int RollTier(double roll, double[] odds)
        {
            if (double.IsNaN(roll) || roll < 0d) roll = 0d;
            double edge = 0d;
            for (int tier = 0; tier < TierCount && tier < odds.Length; tier++)
            {
                edge += odds[tier];
                if (roll < edge) return tier;
            }
            return 0; // a roll of 1 or more: never hand out the rarest by accident
        }

        /// <summary>Spends clovers for one new rabbit; null when there are not enough.</summary>
        /// <param name="kindRoll">A second number in [0, 1) that picks the kind within the tier.</param>
        public static Rabbit Summon(GameState s, double roll, string name, double kindRoll = 0d)
        {
            if (s.Clovers < SummonCost) return null;
            s.Clovers -= SummonCost;
            return Arrive(s, RollTier(roll), name, kindRoll);
        }

        /// <summary>A new rabbit of a tier joins the garden, whatever paid for it.</summary>
        public static Rabbit Arrive(GameState s, int tier, string name, double kindRoll)
        {
            tier = ClampTier(tier);
            return ArriveKind(s, RabbitKinds.Pick(tier, kindRoll), name);
        }

        /// <summary>A new rabbit of a known kind joins the garden.</summary>
        public static Rabbit ArriveKind(GameState s, string kindId, string name)
        {
            RabbitKind kind = RabbitKinds.Find(kindId);
            int tier = kind != null ? kind.Tier : 0;
            var rabbit = new Rabbit { Id = ++s.NextRabbitId, Tier = tier, Kind = kind != null ? kind.Id : RabbitKinds.DefaultFor(tier), Name = name };
            rabbit.FirstOfKind = See(s, rabbit.Kind);
            s.Rabbits.Add(rabbit);
            Garden.AddXp(s, Garden.XpSummon + (rabbit.FirstOfKind ? Garden.XpNewKind : 0));
            return rabbit;
        }

        /// <summary>The kind every new player's first rabbit is: a cotton rabbit, a brewer, so the teapot has company.</summary>
        public const string FirstKind = "tho-bong";

        /// <summary>
        /// A new garden's one rabbit, already at work at the first station. Nothing happens, and null comes
        /// back, when the garden has any rabbit at all: an older save keeps the stations it left empty.
        /// </summary>
        public static Rabbit GiftFirst(GameState s, string name)
        {
            if (s.Rabbits.Count > 0) return null;
            var rabbit = new Rabbit { Id = ++s.NextRabbitId, Tier = 0, Kind = FirstKind, Name = name, Station = s.Levels.Length > 0 && s.Levels[0] > 0 ? 0 : -1 };
            See(s, rabbit.Kind);
            s.Rabbits.Add(rabbit);
            return rabbit;
        }

        /// <summary>A rabbit a merge may use up: any but a kind that only ever comes once (the new moon's, #1).</summary>
        public static bool Mergeable(Rabbit r)
        {
            RabbitKind kind = RabbitKinds.Find(r.Kind);
            return kind == null || !kind.NewMoonOnly;
        }

        /// <summary>How many of a tier a merge could use: the one-time kinds are not counted.</summary>
        public static int CountOfTier(GameState s, int tier)
        {
            int n = 0;
            for (int i = 0; i < s.Rabbits.Count; i++)
                if (s.Rabbits[i].Tier == tier && Mergeable(s.Rabbits[i])) n++;
            return n;
        }

        public static bool CanMerge(GameState s, int tier)
        {
            return tier >= 0 && tier < TierCount - 1 && CountOfTier(s, tier) >= MergeCount;
        }

        /// <summary>
        /// Three rabbits of a tier become one of the next. Resting ones are used up first; the new
        /// rabbit keeps the first one's name and takes over a station if any of the three had one.
        /// </summary>
        /// <param name="kindRoll">A number in [0, 1) that picks which kind of the next tier comes out.</param>
        public static Rabbit Merge(GameState s, int tier, double kindRoll = 0d)
        {
            if (!CanMerge(s, tier)) return null;
            var used = new List<Rabbit>(MergeCount);
            for (int pass = 0; pass < 2 && used.Count < MergeCount; pass++)
                for (int i = 0; i < s.Rabbits.Count && used.Count < MergeCount; i++)
                {
                    Rabbit r = s.Rabbits[i];
                    bool resting = r.Station < 0;
                    if (r.Tier == tier && resting == (pass == 0) && Mergeable(r)) used.Add(r);
                }

            int station = -1, level = 1, affection = 0;
            long petDay = -1;
            foreach (Rabbit r in used)
            {
                if (station < 0 && r.Station >= 0) station = r.Station;
                if (r.Level > level) level = r.Level; // the training is not lost
                if (r.Affection > affection) affection = r.Affection; // nor is the love
                if (r.PetDay > petDay) petDay = r.PetDay;
                s.Rabbits.Remove(r);
            }
            var merged = new Rabbit { Id = ++s.NextRabbitId, Tier = tier + 1, Kind = RabbitKinds.Pick(tier + 1, kindRoll), Name = used[0].Name, Station = station, Level = level, Affection = affection, PetDay = petDay };
            merged.FirstOfKind = See(s, merged.Kind);
            s.Rabbits.Add(merged);
            if (merged.FirstOfKind) Garden.AddXp(s, Garden.XpNewKind);
            // Items move to the new rabbit, first come first served per slot; the rest come off, and so does
            // whatever the new kind cannot wear (it came with its own hat, antlers or scarf).
            foreach (Rabbit r in used)
                for (int i = 0; i < s.Items.Count; i++)
                {
                    Item item = s.Items[i];
                    if (item.WornBy != r.Id) continue;
                    item.WornBy = Items.CanWear(merged, item.Slot) && Items.Worn(s, merged.Id, item.Slot) == null ? merged.Id : 0;
                }
            return merged;
        }

        public static Rabbit Find(GameState s, long id)
        {
            for (int i = 0; i < s.Rabbits.Count; i++)
                if (s.Rabbits[i].Id == id) return s.Rabbits[i];
            return null;
        }

        public static Rabbit AtStation(GameState s, int station)
        {
            if (station < 0) return null;
            for (int i = 0; i < s.Rabbits.Count; i++)
                if (s.Rabbits[i].Station == station) return s.Rabbits[i];
            return null;
        }

        /// <summary>How many open stations have nobody working at them.</summary>
        public static int StationsWithoutWorker(GameState s)
        {
            int n = 0;
            for (int i = 0; i < s.Levels.Length; i++)
                if (s.Levels[i] > 0 && AtStation(s, i) == null) n++;
            return n;
        }

        /// <summary>Like <see cref="StationsWithoutWorker(GameState)"/>, leaving out the stations put away in storage (#54).</summary>
        public static int StationsWithoutWorker(IReadOnlyList<StationDef> defs, GameState s)
        {
            int n = 0;
            for (int i = 0; i < s.Levels.Length; i++)
                if (s.Levels[i] > 0 && AtStation(s, i) == null && !Layout.IsStored(defs, s, i)) n++;
            return n;
        }

        /// <summary>The rabbit's own multiplier at a station, without the trade bonus. Income uses the overload with the stations.</summary>
        public static double StationMultiplier(GameState s, int station)
        {
            Rabbit r = AtStation(s, station);
            return r == null ? 1d : Multiplier(s, r);
        }

        /// <summary>What the rabbit at a station multiplies it by, trade bonus included; 1 when nobody is there.</summary>
        public static double StationMultiplier(IReadOnlyList<StationDef> defs, GameState s, int station)
        {
            Rabbit r = AtStation(s, station);
            return r == null ? 1d : MultiplierAt(defs, s, r, station);
        }

        /// <summary>
        /// Sends a rabbit to an open station (whoever was there goes to rest), or to rest with -1.
        /// </summary>
        public static bool Assign(GameState s, long id, int station)
        {
            Rabbit rabbit = Find(s, id);
            if (rabbit == null) return false;
            if (station < 0)
            {
                rabbit.Station = -1;
                return true;
            }
            if (station >= s.Levels.Length || s.Levels[station] <= 0) return false;
            Rabbit there = AtStation(s, station);
            if (there != null && there != rabbit) there.Station = -1;
            rabbit.Station = station;
            return true;
        }

        /// <summary>
        /// The open station where this rabbit would help most: an empty one (one of its own trade first),
        /// otherwise the station where it beats the rabbit already there by the most. -1 if it beats nobody.
        /// Stations that earn more come first.
        /// </summary>
        public static int BestStationFor(IReadOnlyList<StationDef> defs, GameState s, Rabbit rabbit)
        {
            List<int> order = StationsByWorth(defs, s);
            int empty = -1;
            foreach (int i in order)
            {
                if (AtStation(s, i) != null) continue;
                if (Suits(defs, rabbit, i)) return i;
                if (empty < 0) empty = i;
            }
            if (empty >= 0) return empty;

            int best = -1;
            double bestGain = 1e-9;
            foreach (int i in order)
            {
                Rabbit there = AtStation(s, i);
                if (there == rabbit) continue;
                double gain = MultiplierAt(defs, s, rabbit, i) - MultiplierAt(defs, s, there, i);
                if (gain > bestGain)
                {
                    bestGain = gain;
                    best = i;
                }
            }
            return best;
        }

        /// <summary>
        /// Fills the stations from the biggest earner down, each with the free rabbit worth most there
        /// (trade bonus counted); everyone left over rests.
        /// </summary>
        public static void AutoAssign(IReadOnlyList<StationDef> defs, GameState s)
        {
            foreach (Rabbit r in s.Rabbits) r.Station = -1;
            var free = new List<Rabbit>(s.Rabbits);
            foreach (int station in StationsByWorth(defs, s))
            {
                Rabbit pick = null;
                double worth = 0d;
                foreach (Rabbit r in free)
                {
                    double here = MultiplierAt(defs, s, r, station);
                    bool better = pick == null || here > worth + 1e-12
                        || (Math.Abs(here - worth) <= 1e-12 && (r.Tier > pick.Tier || (r.Tier == pick.Tier && r.Id < pick.Id)));
                    if (better) { pick = r; worth = here; }
                }
                if (pick == null) break;
                pick.Station = station;
                free.Remove(pick);
            }
        }

        /// <summary>Open stations, biggest earners first, ingredient stations after the brew ones.</summary>
        static List<int> StationsByWorth(IReadOnlyList<StationDef> defs, GameState s)
        {
            var open = new List<int>();
            var worth = new Dictionary<int, double>();
            for (int i = 0; i < defs.Count; i++)
            {
                if (s.Levels[i] <= 0 || Layout.IsStored(defs, s, i)) continue; // a stored station has no post (#54)
                open.Add(i);
                StationDef d = defs[i];
                // Compare on a log scale so huge incomes still order correctly; rabbits excluded.
                worth[i] = d.Kind == StationKind.Brew && d.CycleSeconds > 0d
                    ? Math.Log10(Math.Max(1e-9, d.BaseIncome * s.Levels[i] * Economy.MilestoneMultiplier(s.Levels[i]) / d.CycleSeconds))
                    : -1e9 + i;
            }
            open.Sort((a, b) => worth[b].CompareTo(worth[a]));
            return open;
        }
    }
}
