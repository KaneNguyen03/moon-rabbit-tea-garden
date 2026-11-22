using System;
using System.Collections.Generic;

namespace VuonTra.Core
{
    /// <summary>
    /// Places in the garden where resting rabbits sit: a stump, a rug, a hammock. A rabbit with a place is seen
    /// in the garden and lends a hand: each one seated adds a share of the whole garden's income, more the rarer
    /// it is (#60). A garden starts with two, and each widening of the garden (see <see cref="Expansion"/>) opens one more.
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

        /// <summary>The share of the whole garden's income one seated silver rabbit adds (#60).</summary>
        public const double RestBonus = 0.02;

        /// <summary>
        /// How a seated rabbit's share grows with its tier: above silver it is the tier's work multiplier
        /// (<see cref="Rabbits.Multipliers"/>), so green adds 2.5 %, blue 3 %, purple 4 %, gold 6 % and red 10 %;
        /// silver counts as 1, a plain 2 %.
        /// </summary>
        public static readonly double[] TierScale = { 1d, 1.25, 1.5, 2d, 3d, 5d };

        /// <summary>What this rabbit adds to the garden's income while it sits at a place to rest, as a share (0.03 = 3 %).</summary>
        public static double BonusOf(Rabbit r)
        {
            int tier = Math.Max(0, Math.Min(TierScale.Length - 1, r.Tier));
            return RestBonus * TierScale[tier];
        }

        /// <summary>
        /// What the seated rabbits add to the whole garden's income together, as a share. Only the rabbits that have a
        /// place count: the rest of the resting ones are not out. A rabbit that dozes or strolls off keeps its place.
        /// </summary>
        public static double Bonus(GameState s)
        {
            if (s == null || s.Rabbits == null) return 0d;
            double sum = 0d;
            foreach (Rabbit r in Seated(s)) sum += BonusOf(r);
            return sum;
        }

        /// <summary>What every carrot the garden earns is multiplied by for its seated rabbits: 1 with nobody seated.</summary>
        public static double Multiplier(GameState s) => 1d + Bonus(s);

        /// <summary>True when this rabbit sits at one of the places to rest, and so adds its share.</summary>
        public static bool IsSeated(GameState s, Rabbit r)
        {
            if (s == null || s.Rabbits == null || r == null || r.Station >= 0) return false;
            foreach (Rabbit seated in Seated(s))
                if (seated.Id == r.Id) return true;
            return false;
        }
    }
}
