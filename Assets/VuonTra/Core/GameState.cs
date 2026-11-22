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
        /// <summary>1, 10, 0 for "Tối đa", -1 for "Tới mốc" (see <see cref="Simulation.BuyOne"/> and friends).</summary>
        public int BuyMode = 1;
        /// <summary>The share of the purse a quick upgrade may spend: 1, 0.5 or 0.25, so some is kept for opening stations.</summary>
        public double SpendShare = 1d;
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

        /// <summary>How many garden plots are owned; station i needs plot i.</summary>
        public int Plots;
        /// <summary>"Trà lực", 0 to Energy.Max.</summary>
        public double Energy = VuonTra.Core.Energy.Max;
        /// <summary>Seconds left of a running double-speed burst.</summary>
        public double BoostSecondsLeft;
        public List<Item> Items = new List<Item>();
        public long NextItemId;

        /// <summary>The rabbits the player has collected.</summary>
        public List<Rabbit> Rabbits = new List<Rabbit>();
        public long NextRabbitId;

        /// <summary>Ids of quests already paid out. Daily ids are removed when a new day starts.</summary>
        public HashSet<string> Claimed = new HashSet<string>();
        /// <summary>Every kind of rabbit the player has ever had, including ones since merged away: the album.</summary>
        public HashSet<string> SeenKinds = new HashSet<string>();
        /// <summary>How many places resting rabbits have to sit in the garden.</summary>
        public int RestSpots = VuonTra.Core.RestSpots.Starting;
        /// <summary>The garden's experience; its level follows from this.</summary>
        public double GardenXp;

        /// <summary>What the stations have made and nobody has sold yet, one entry per slot.</summary>
        public List<Product> Tray = new List<Product>();
        public int TraySlots = VuonTra.Core.Tray.StartingSlots;
        /// <summary>Seconds of work each station has put towards its next special batch; indexed like the stations.</summary>
        public double[] TrayWork = new double[0];
        /// <summary>Golden batches made today; a new day starts the count over.</summary>
        public int GoldenToday;

        /// <summary>The day a photo was last taken (its gift is once a day), and how many have been taken in all.</summary>
        public long PhotoDay = -1;
        public long PhotosTaken;
        /// <summary>Photos taken while a gold-or-rarer rabbit lived here, and while every place to rest was open: what two quests count.</summary>
        public long PhotosWithGoldRabbit, PhotosWithFullRest;
        /// <summary>Where the latest pictures were written, oldest first; at most <see cref="Photos.AlbumSize"/>.</summary>
        public List<string> Album = new List<string>();

        /// <summary>Where the player last looked: the garden point under the middle of the screen and the zoom; zoom 0 means never recorded.</summary>
        public double ViewX, ViewY, ViewZoom;

        /// <summary>The garden's name, its avatar rabbit and the player code.</summary>
        public PlayerProfile Profile = new PlayerProfile { Code = VuonTra.Core.Profile.NewCode() };

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
            s.Plots = System.Math.Min(defs.Count, VuonTra.Core.Plots.Starting);
            s.TrayWork = new double[defs.Count];
            if (defs.Count > 0) s.Levels[0] = 1;
            return s;
        }
    }
}
