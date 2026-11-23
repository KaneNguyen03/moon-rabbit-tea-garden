using System;
using System.Collections.Generic;

namespace VuonTra.Core
{
    public sealed class AlbumSet
    {
        public int Tier;
        public RabbitTrade Trade;
        public IReadOnlyList<RabbitKind> Kinds;
        public string Key => Tier + "-" + Trade;
    }

    public readonly struct AlbumFactors
    {
        readonly double _brewer, _baker, _gatherer, _stargazer;

        public AlbumFactors(GameState s)
        {
            _brewer = AlbumSets.TradeFactor(s, RabbitTrade.Brewer);
            _baker = AlbumSets.TradeFactor(s, RabbitTrade.Baker);
            _gatherer = AlbumSets.TradeFactor(s, RabbitTrade.Gatherer);
            _stargazer = AlbumSets.TradeFactor(s, RabbitTrade.Stargazer);
        }

        public double Of(RabbitTrade trade)
        {
            switch (trade)
            {
                case RabbitTrade.Brewer: return _brewer;
                case RabbitTrade.Baker: return _baker;
                case RabbitTrade.Gatherer: return _gatherer;
                case RabbitTrade.Stargazer: return _stargazer;
                default: return 1d;
            }
        }

        public double At(IReadOnlyList<StationDef> defs, int station)
        {
            if (defs == null || station < 0 || station >= defs.Count) return 1d;
            return Of(defs[station].Trade);
        }
    }

    public static class AlbumSets
    {
        public const double BonusPerSet = 0.03;
        public static readonly int[] Milestones = { 10, 20, 40, 72 };
        public const int HatMilestone = 40, FrameMilestone = 72;
        public const string MilestoneHat = "hat-mu-sao";

        static readonly RabbitTrade[] Trades = { RabbitTrade.Brewer, RabbitTrade.Baker, RabbitTrade.Gatherer, RabbitTrade.Stargazer };
        static readonly AlbumSet[] Table = Build();

        static AlbumSet[] Build()
        {
            var sets = new List<AlbumSet>();
            for (int tier = 0; tier < Rabbits.TierCount; tier++)
            {
                IReadOnlyList<RabbitKind> everyDay = RabbitKinds.OfTier(tier);
                foreach (RabbitTrade trade in Trades)
                {
                    var kinds = new List<RabbitKind>();
                    foreach (RabbitKind k in everyDay) if (k.Trade == trade) kinds.Add(k);
                    if (kinds.Count > 0) sets.Add(new AlbumSet { Tier = tier, Trade = trade, Kinds = kinds });
                }
            }
            return sets.ToArray();
        }

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

        public static int Met(GameState s, AlbumSet set)
        {
            int n = 0;
            IReadOnlyList<RabbitKind> kinds = set.Kinds;
            for (int i = 0; i < kinds.Count; i++) if (s.SeenKinds.Contains(kinds[i].Id)) n++;
            return n;
        }

        public static bool IsComplete(GameState s, AlbumSet set) => Met(s, set) == set.Kinds.Count;

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

        public static double TradeFactor(GameState s, RabbitTrade trade)
        {
            return trade == RabbitTrade.None ? 1d : 1d + BonusPerSet * CompleteCount(s, trade);
        }

        public static double StationFactor(IReadOnlyList<StationDef> defs, GameState s, int station)
        {
            if (defs == null || station < 0 || station >= defs.Count) return 1d;
            return TradeFactor(s, defs[station].Trade);
        }

        public static AlbumFactors Factors(GameState s) => new AlbumFactors(s);

        public static int KindsMet(GameState s)
        {
            int n = 0;
            IReadOnlyList<RabbitKind> all = RabbitKinds.All;
            for (int i = 0; i < all.Count; i++)
            {
                RabbitKind k = all[i];
                if (k.Season == Season.None && !k.NewMoonOnly && s.SeenKinds.Contains(k.Id)) n++;
            }
            return n;
        }

        public static int EveryDayKinds
        {
            get
            {
                int n = 0;
                IReadOnlyList<RabbitKind> all = RabbitKinds.All;
                for (int i = 0; i < all.Count; i++) if (all[i].Season == Season.None && !all[i].NewMoonOnly) n++;
                return n;
            }
        }

        public static bool HasFullFrame(GameState s) => s.AlbumMilestonesClaimed.Contains(FrameMilestone);

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
