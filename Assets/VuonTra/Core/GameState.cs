using System.Collections.Generic;

namespace VuonTra.Core
{
    /// <summary>Everything the player has earned. Arrays are indexed like the station list.</summary>
    public sealed class GameState
    {
        public BigNum Coins;
        public int[] Levels;
        /// <summary>Fraction of the current cycle done, 0 ≤ p &lt; 1 between ticks.</summary>
        public double[] Progress;
        /// <summary>1, 10, or 0 for "Tối đa".</summary>
        public int BuyMode = 1;
        public long LastSeenUnixSeconds;
        public bool HintSeen;

        public static GameState NewGame(IReadOnlyList<StationDef> defs)
        {
            var s = new GameState
            {
                Coins = BigNum.Zero,
                Levels = new int[defs.Count],
                Progress = new double[defs.Count],
                BuyMode = 1
            };
            if (defs.Count > 0) s.Levels[0] = 1;
            return s;
        }
    }
}
