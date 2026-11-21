using System;
using System.Collections.Generic;

namespace VuonTra.Core
{
    /// <summary>One collected rabbit. Tier 0 is the most common (silver), 5 the rarest (red).</summary>
    public sealed class Rabbit
    {
        public long Id;
        public int Tier;
        public string Name;
        /// <summary>1 and up; each level adds to what the rabbit is worth. The tier sets the cap.</summary>
        public int Level = 1;
        /// <summary>Index of the station it works at, or -1 when resting.</summary>
        public int Station = -1;
    }

    /// <summary>
    /// The rabbit collection: call one with clovers, merge three of a tier into one of the next,
    /// and put them to work. A working rabbit multiplies what its station is worth.
    /// </summary>
    public static class Rabbits
    {
        public const int TierCount = 6;
        public const int SummonCost = 1;
        public const int MergeCount = 3;

        /// <summary>Silver, green, blue, purple, gold, red.</summary>
        public static readonly double[] Multipliers = { 1.1, 1.25, 1.5, 2, 3, 5 };
        /// <summary>Highest level a rabbit of each tier can reach.</summary>
        public static readonly int[] LevelCaps = { 10, 20, 30, 40, 50, 60 };
        /// <summary>Each level above 1 adds this share of the tier's multiplier.</summary>
        public const double LevelBonus = 0.05;

        static int ClampTier(int tier) => Math.Max(0, Math.Min(TierCount - 1, tier));

        /// <summary>What this rabbit multiplies its station by: tier, level, then its own items.</summary>
        public static double Multiplier(GameState s, Rabbit r)
        {
            double fromLevel = Multipliers[ClampTier(r.Tier)] * (1d + LevelBonus * (Math.Max(1, r.Level) - 1));
            return fromLevel * (1d + Items.StationBonus(s, r.Id));
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

        public static bool IsMaxLevel(Rabbit r) => r.Level >= LevelCaps[ClampTier(r.Tier)];

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

        /// <summary>Chance of each tier from one call; they add up to 1.</summary>
        public static readonly double[] Odds = { 0.60, 0.25, 0.10, 0.04, 0.009, 0.001 };

        /// <summary>Turns a number in [0, 1) into a tier using the odds table.</summary>
        public static int RollTier(double roll)
        {
            if (double.IsNaN(roll) || roll < 0d) roll = 0d;
            double edge = 0d;
            for (int tier = 0; tier < TierCount; tier++)
            {
                edge += Odds[tier];
                if (roll < edge) return tier;
            }
            return 0; // a roll of 1 or more: never hand out the rarest by accident
        }

        /// <summary>Spends clovers for one new rabbit; null when there are not enough.</summary>
        public static Rabbit Summon(GameState s, double roll, string name)
        {
            if (s.Clovers < SummonCost) return null;
            s.Clovers -= SummonCost;
            var rabbit = new Rabbit { Id = ++s.NextRabbitId, Tier = RollTier(roll), Name = name };
            s.Rabbits.Add(rabbit);
            return rabbit;
        }

        public static int CountOfTier(GameState s, int tier)
        {
            int n = 0;
            for (int i = 0; i < s.Rabbits.Count; i++)
                if (s.Rabbits[i].Tier == tier) n++;
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
        public static Rabbit Merge(GameState s, int tier)
        {
            if (!CanMerge(s, tier)) return null;
            var used = new List<Rabbit>(MergeCount);
            for (int pass = 0; pass < 2 && used.Count < MergeCount; pass++)
                for (int i = 0; i < s.Rabbits.Count && used.Count < MergeCount; i++)
                {
                    Rabbit r = s.Rabbits[i];
                    bool resting = r.Station < 0;
                    if (r.Tier == tier && resting == (pass == 0)) used.Add(r);
                }

            int station = -1, level = 1;
            foreach (Rabbit r in used)
            {
                if (station < 0 && r.Station >= 0) station = r.Station;
                if (r.Level > level) level = r.Level; // the training is not lost
                s.Rabbits.Remove(r);
            }
            var merged = new Rabbit { Id = ++s.NextRabbitId, Tier = tier + 1, Name = used[0].Name, Station = station, Level = level };
            s.Rabbits.Add(merged);
            // Items move to the new rabbit, first come first served per slot; the rest come off.
            foreach (Rabbit r in used)
                for (int i = 0; i < s.Items.Count; i++)
                {
                    Item item = s.Items[i];
                    if (item.WornBy != r.Id) continue;
                    item.WornBy = Items.Worn(s, merged.Id, item.Slot) == null ? merged.Id : 0;
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

        public static double StationMultiplier(GameState s, int station)
        {
            Rabbit r = AtStation(s, station);
            return r == null ? 1d : Multiplier(s, r);
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
        /// The open station where this rabbit would help most: an empty one if there is one,
        /// otherwise the one whose current rabbit is the weakest and weaker than this one. -1 if none.
        /// Stations that earn more come first.
        /// </summary>
        public static int BestStationFor(IReadOnlyList<StationDef> defs, GameState s, Rabbit rabbit)
        {
            List<int> order = StationsByWorth(defs, s);
            foreach (int i in order)
                if (AtStation(s, i) == null) return i;

            int best = -1, weakest = rabbit.Tier;
            foreach (int i in order)
            {
                Rabbit there = AtStation(s, i);
                if (there != rabbit && there.Tier < weakest)
                {
                    weakest = there.Tier;
                    best = i;
                }
            }
            return best;
        }

        /// <summary>Best rabbits to the stations that earn the most; everyone else rests.</summary>
        public static void AutoAssign(IReadOnlyList<StationDef> defs, GameState s)
        {
            foreach (Rabbit r in s.Rabbits) r.Station = -1;
            List<int> order = StationsByWorth(defs, s);
            var ranked = new List<Rabbit>(s.Rabbits);
            ranked.Sort((a, b) => b.Tier != a.Tier ? b.Tier.CompareTo(a.Tier) : a.Id.CompareTo(b.Id));
            for (int i = 0; i < order.Count && i < ranked.Count; i++) ranked[i].Station = order[i];
        }

        /// <summary>Open stations, biggest earners first, ingredient stations after the brew ones.</summary>
        static List<int> StationsByWorth(IReadOnlyList<StationDef> defs, GameState s)
        {
            var open = new List<int>();
            var worth = new Dictionary<int, double>();
            for (int i = 0; i < defs.Count; i++)
            {
                if (s.Levels[i] <= 0) continue;
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
