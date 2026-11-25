using System;
using System.Collections.Generic;

namespace VuonTra.Core
{
    public static class RestSpots
    {
        public const int Starting = 2;
        public const int Max = 6;
        public static List<Rabbit> Seated(GameState s)
        {
            var resting = new List<Rabbit>();
            Seated(s, resting);
            return resting;
        }

        public static void Seated(GameState s, List<Rabbit> into)
        {
            into.Clear();
            int places = Math.Max(0, Math.Min(Max, s.RestSpots));
            for (int i = 0; i < s.Rabbits.Count; i++)
            {
                Rabbit r = s.Rabbits[i];
                if (r.Station >= 0) continue;
                int at = into.Count;
                while (at > 0 && SitsBefore(r, into[at - 1])) at--;
                if (at >= places) continue;
                if (into.Count == places) into.RemoveAt(places - 1);
                into.Insert(at, r);
            }
        }

        static bool SitsBefore(Rabbit a, Rabbit b) => a.Tier != b.Tier ? a.Tier > b.Tier : a.Id < b.Id;

        public static int SeatedCount(GameState s)
        {
            int resting = 0;
            for (int i = 0; i < s.Rabbits.Count; i++) if (s.Rabbits[i].Station < 0) resting++;
            return Math.Min(resting, Math.Max(0, Math.Min(Max, s.RestSpots)));
        }

        public const double RestBonus = 0.02;

        public static readonly double[] TierScale = { 1d, 1.25, 1.5, 2d, 3d, 5d };

        public static double BonusOf(Rabbit r)
        {
            int tier = Math.Max(0, Math.Min(TierScale.Length - 1, r.Tier));
            return RestBonus * TierScale[tier];
        }

        public static double Bonus(GameState s)
        {
            if (s == null || s.Rabbits == null) return 0d;
            double sum = 0d;
            foreach (Rabbit r in Seated(s)) sum += BonusOf(r);
            return sum;
        }

        public static double Multiplier(GameState s) => 1d + Bonus(s);

        public static bool IsSeated(GameState s, Rabbit r)
        {
            if (s == null || s.Rabbits == null || r == null || r.Station >= 0) return false;
            foreach (Rabbit seated in Seated(s))
                if (seated.Id == r.Id) return true;
            return false;
        }
    }
}
