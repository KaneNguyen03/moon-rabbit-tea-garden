using System.Collections.Generic;

namespace VuonTra.Core
{
    /// <summary>
    /// Garden plots: a station can only be opened on a plot the player owns. The garden starts
    /// with a few and grows one plot at a time, paid in carrots.
    /// </summary>
    public static class Plots
    {
        public const int Starting = 4;
        /// <summary>A plot costs this share of what the station that will stand on it costs.</summary>
        public const double CostShare = 0.25;

        public static bool CanBuy(IReadOnlyList<StationDef> defs, GameState s) => s.Plots < defs.Count;

        public static BigNum CostOfNext(IReadOnlyList<StationDef> defs, GameState s)
        {
            return CanBuy(defs, s) ? new BigNum(defs[s.Plots].BaseCost * CostShare) : BigNum.Zero;
        }

        public static bool TryBuy(IReadOnlyList<StationDef> defs, GameState s)
        {
            if (!CanBuy(defs, s)) return false;
            BigNum cost = CostOfNext(defs, s);
            if (s.Coins < cost) return false;
            s.Coins -= cost;
            s.Plots++;
            return true;
        }
    }
}
