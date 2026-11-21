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

        /// <summary>Levels of the garden-wide upgrades, indexed like the upgrade list.</summary>
        public int[] UpgradeLevels = new int[0];
        /// <summary>Rare currency from quests; spent on new rabbits in a later milestone.</summary>
        public int Clovers;

        // Lifetime counters that quests read.
        public long TotalTaps;
        public long LevelsBought;
        public BigNum TotalEarned;

        // Daily quests count from these snapshots, taken when the day began.
        public long DailyDay;
        public long DailyTapsBase;
        public long DailyLevelsBase;
        public BigNum DailyEarnedBase;
        public BigNum DailyCarrotTarget;

        /// <summary>The rabbits the player has collected.</summary>
        public List<Rabbit> Rabbits = new List<Rabbit>();
        public long NextRabbitId;

        /// <summary>Ids of quests already paid out. Daily ids are removed when a new day starts.</summary>
        public HashSet<string> Claimed = new HashSet<string>();

        public static GameState NewGame(IReadOnlyList<StationDef> defs, int upgradeCount = 0)
        {
            var s = new GameState
            {
                Coins = BigNum.Zero,
                Levels = new int[defs.Count],
                Progress = new double[defs.Count],
                BuyMode = 1,
                UpgradeLevels = new int[upgradeCount]
            };
            if (defs.Count > 0) s.Levels[0] = 1;
            return s;
        }
    }
}
