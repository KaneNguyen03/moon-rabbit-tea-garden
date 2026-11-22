using System;
using System.Collections.Generic;

namespace VuonTra.Core
{
    /// <summary>One set of the album (#61): the every-day kinds of one tier that share one trade.</summary>
    public sealed class AlbumSet
    {
        public int Tier;
        public RabbitTrade Trade;
        public IReadOnlyList<RabbitKind> Kinds;
        /// <summary>"3-Brewer": the same for a set across runs, for comparing which sets are complete.</summary>
        public string Key => Tier + "-" + Trade;
    }

    /// <summary>
    /// The album pays (#61). Each tier's every-day kinds of one trade make a set; meeting every kind of a set
    /// (it only has to be in <see cref="GameState.SeenKinds"/>, so a rabbit merged away still counts) adds 3 % to
    /// the income of that trade's stations for good. Seasonal kinds and the new moon's are left out, so a set never
    /// waits on a season. Meeting 10, 20, 40 and 72 every-day kinds are milestones: the quests of
    /// <see cref="QuestMetric.KindsMet"/> pay their golden carrots and clover, and <see cref="ClaimMilestones"/>
    /// gives the two things a quest cannot: the Starry Hat at 40 and the gold "complete" frame at 72.
    /// </summary>
    public static class AlbumSets
    {
        /// <summary>What one complete set adds to its trade's stations.</summary>
        public const double BonusPerSet = 0.03;
        /// <summary>How many every-day kinds make each milestone.</summary>
        public static readonly int[] Milestones = { 10, 20, 40, 72 };
        public const int HatMilestone = 40, FrameMilestone = 72;
        /// <summary>The hat given at <see cref="HatMilestone"/>; a player who has one gets its price in golden carrots instead.</summary>
        public const string MilestoneHat = "hat-mu-sao";

        static readonly RabbitTrade[] Trades = { RabbitTrade.Brewer, RabbitTrade.Baker, RabbitTrade.Gatherer, RabbitTrade.Stargazer };
        static readonly AlbumSet[] Table = Build();

        static AlbumSet[] Build()
        {
            var sets = new List<AlbumSet>();
            for (int tier = 0; tier < Rabbits.TierCount; tier++)
            {
                IReadOnlyList<RabbitKind> everyDay = RabbitKinds.OfTier(tier); // no season: every-day kinds, no new moon
                foreach (RabbitTrade trade in Trades)
                {
                    var kinds = new List<RabbitKind>();
                    foreach (RabbitKind k in everyDay) if (k.Trade == trade) kinds.Add(k);
                    if (kinds.Count > 0) sets.Add(new AlbumSet { Tier = tier, Trade = trade, Kinds = kinds });
                }
            }
            return sets.ToArray();
        }

        /// <summary>Every set, tier by tier, trades in the order Brewer, Baker, Gatherer, Stargazer.</summary>
        public static IReadOnlyList<AlbumSet> All => Table;

        public static List<AlbumSet> OfTier(int tier)
        {
            var list = new List<AlbumSet>(4);
            foreach (AlbumSet set in Table) if (set.Tier == tier) list.Add(set);
            return list;
        }

        public static AlbumSet Find(int tier, RabbitTrade trade)
        {
            foreach (AlbumSet set in Table) if (set.Tier == tier && set.Trade == trade) return set;
            return null;
        }

        /// <summary>How many of the set's kinds the album holds.</summary>
        public static int Met(GameState s, AlbumSet set)
        {
            int n = 0;
            foreach (RabbitKind k in set.Kinds) if (s.SeenKinds.Contains(k.Id)) n++;
            return n;
        }

        public static bool IsComplete(GameState s, AlbumSet set) => Met(s, set) == set.Kinds.Count;

        /// <summary>The complete sets of one trade.</summary>
        public static int CompleteCount(GameState s, RabbitTrade trade)
        {
            int n = 0;
            if (s == null) return 0;
            foreach (AlbumSet set in Table) if (set.Trade == trade && IsComplete(s, set)) n++;
            return n;
        }

        public static List<AlbumSet> Completed(GameState s)
        {
            var list = new List<AlbumSet>();
            foreach (AlbumSet set in Table) if (IsComplete(s, set)) list.Add(set);
            return list;
        }

        /// <summary>What a trade's stations earn by: 1, plus 3 % for each complete set of that trade (six sets: +18 %).</summary>
        public static double TradeFactor(GameState s, RabbitTrade trade)
        {
            return trade == RabbitTrade.None ? 1d : 1d + BonusPerSet * CompleteCount(s, trade);
        }

        /// <summary>The album's factor on one station, by the station's trade.</summary>
        public static double StationFactor(IReadOnlyList<StationDef> defs, GameState s, int station)
        {
            if (defs == null || station < 0 || station >= defs.Count) return 1d;
            return TradeFactor(s, defs[station].Trade);
        }

        /// <summary>How many every-day kinds the album holds: what the milestones count.</summary>
        public static int KindsMet(GameState s)
        {
            int n = 0;
            foreach (RabbitKind k in RabbitKinds.All)
                if (k.Season == Season.None && !k.NewMoonOnly && s.SeenKinds.Contains(k.Id)) n++;
            return n;
        }

        /// <summary>The number of every-day kinds there are to meet (72).</summary>
        public static int EveryDayKinds
        {
            get
            {
                int n = 0;
                foreach (RabbitKind k in RabbitKinds.All) if (k.Season == Season.None && !k.NewMoonOnly) n++;
                return n;
            }
        }

        /// <summary>True once the album has reached its last milestone: the gold "complete" frame is the player's.</summary>
        public static bool HasFullFrame(GameState s) => s.AlbumMilestonesClaimed.Contains(FrameMilestone);

        /// <summary>
        /// Notes every milestone reached and not yet noted, and gives what it carries beyond the quest's reward:
        /// the Starry Hat at 40 (its price in golden carrots when the player already has one), the gold frame at 72.
        /// Each milestone is noted once, so nothing is given twice. Returns the milestones noted now, smallest first.
        /// </summary>
        public static List<int> ClaimMilestones(GameState s, IReadOnlyList<ItemDef> items)
        {
            var now = new List<int>();
            int met = KindsMet(s);
            foreach (int milestone in Milestones)
            {
                if (met < milestone || s.AlbumMilestonesClaimed.Contains(milestone)) continue;
                s.AlbumMilestonesClaimed.Add(milestone);
                now.Add(milestone);
                if (milestone == HatMilestone)
                {
                    ItemDef hat = Items.DefOf(items, MilestoneHat);
                    if (hat == null) continue;
                    if (Items.Owns(s, MilestoneHat)) s.GoldenCarrots += Math.Max(0, hat.Price);
                    else Items.Grant(s, hat, hat.Tier);
                }
            }
            return now;
        }
    }
}
