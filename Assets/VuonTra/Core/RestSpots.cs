using System;
using System.Collections.Generic;

namespace VuonTra.Core
{
    /// <summary>
    /// Places in the garden where resting rabbits sit: a stump, a seat by the tea table. They are for looking
    /// at, not for earning: a rabbit with a place is seen in the garden, and nothing else changes.
    /// </summary>
    public static class RestSpots
    {
        public const int Starting = 2;
        public const int Max = 6;
        /// <summary>The third place costs this many carrots; each one after costs ten times the last.</summary>
        public const double FirstCost = 5000;

        public static bool CanBuy(GameState s) => s.RestSpots < Max;

        public static BigNum CostOfNext(GameState s)
        {
            if (!CanBuy(s)) return BigNum.Zero;
            return new BigNum(10d).Pow(Math.Max(0, s.RestSpots - Starting)) * FirstCost;
        }

        public static bool TryBuy(GameState s)
        {
            if (!CanBuy(s)) return false;
            BigNum cost = CostOfNext(s);
            if (s.Coins < cost) return false;
            s.Coins -= cost;
            s.RestSpots++;
            return true;
        }

        /// <summary>
        /// The resting rabbits that have a place, in seat order: the rarest first, then the longest here.
        /// Rabbits resting beyond the number of places are simply not out in the garden.
        /// </summary>
        public static List<Rabbit> Seated(GameState s)
        {
            var resting = new List<Rabbit>();
            foreach (Rabbit r in s.Rabbits)
                if (r.Station < 0) resting.Add(r);
            resting.Sort((a, b) => b.Tier != a.Tier ? b.Tier.CompareTo(a.Tier) : a.Id.CompareTo(b.Id));
            int places = Math.Max(0, Math.Min(Max, s.RestSpots));
            if (resting.Count > places) resting.RemoveRange(places, resting.Count - places);
            return resting;
        }
    }
}
