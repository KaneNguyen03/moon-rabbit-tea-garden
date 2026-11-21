using System;
using System.Collections.Generic;

namespace VuonTra.Core
{
    /// <summary>
    /// Places in the garden where resting rabbits sit: a stump, a rug, a hammock. They are for looking at,
    /// not for earning: a rabbit with a place is seen in the garden, and nothing else changes. A garden
    /// starts with two, and each widening of the garden (see <see cref="Expansion"/>) opens one more.
    /// </summary>
    public static class RestSpots
    {
        public const int Starting = 2;
        public const int Max = 6;
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
