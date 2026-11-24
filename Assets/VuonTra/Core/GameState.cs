using System.Collections.Generic;

namespace VuonTra.Core
{
    public sealed class GameState
    {
        public BigNum Coins;
        public int[] Levels;
        public double[] Progress;
        public int BuyMode = 1;
        public double SpendShare = 1d;
        public long LastSeenUnixSeconds;
        public double PlaySeconds;
        public bool HintSeen;

        public int[] UpgradeLevels = new int[0];
        public int Clovers;
        public int GoldenCarrots;
        public long GiftDay = -1;
        public int GiftStreak;
        public long GiftBigDay = -1;
        public bool GiftBigGolden;
        public bool GiftBigClaimed => GiftBigDay >= 0;
        public long VisitorBoughtDay = -1, VisitorSeenDay = -1;

        public long TotalTaps;
        public long LevelsBought;
        public BigNum TotalEarned;

        public long DailyDay;
        public long DailyTapsBase;
        public long DailyLevelsBase;
        public BigNum DailyEarnedBase;
        public BigNum DailyCarrotTarget;

        public int Plots;
        public double Energy = VuonTra.Core.Energy.Max;
        public double BoostSecondsLeft;
        public List<Item> Items = new List<Item>();
        public long NextItemId;

        public List<Rabbit> Rabbits = new List<Rabbit>();
        public long NextRabbitId;

        public HashSet<string> Claimed = new HashSet<string>();
        public HashSet<string> SeenKinds = new HashSet<string>();
        public HashSet<string> FreshKinds = new HashSet<string>();
        public HashSet<int> AlbumMilestonesClaimed = new HashSet<int>();
        public int RestSpots = VuonTra.Core.RestSpots.Starting;
        public double GardenXp;

        public List<Product> Tray = new List<Product>();
        public int TraySlots = VuonTra.Core.Tray.StartingSlots;
        public double[] TrayWork = new double[0];
        public int GoldenToday;

        public long PhotoDay = -1;
        public long PhotosTaken;
        public int TutorialStep;
        public bool TutorialCloverGiven;
        public long PhotosWithGoldRabbit, PhotosWithFullRest;

        public long MaxDaySeen = -1;
        public long FirstDaySeen = -1;
        public long SeasonGreetedDay = -1;
        public long AwayDoubleDay = -1;
        public long PhotosWithSeasonRabbit;
        public long PoundingDay = -1;
        public int PoundingPlays;
        public bool PoundingStarToday, PoundingThreeToday;
        public long PoundingGames;
        public int PoundingBestStars;
        public MiniGameRecord Leaves = new MiniGameRecord();
        public MiniGameRecord Fireflies = new MiniGameRecord();
        public List<string> Album = new List<string>();

        public double ViewX, ViewY, ViewZoom;

        public PlayerProfile Profile = new PlayerProfile { Code = VuonTra.Core.Profile.NewCode() };

        public Order Order;
        public double OrderClock = VuonTra.Core.Orders.StartingClock;
        public int OrdersToday;
        public long OrderDay = -1;
        public long OrdersDelivered, DailyOrdersBase, NextOrderId;

        public long PhotoAskRabbit;
        public double PhotoAskLeft, PhotoAskClock;
        public int PhotoAsksToday;
        public long PhotoAskDay = -1;
        public long PhotoAsksAnswered, DailyPhotoAsksBase;

        public int[] DecorOwned = new int[Decor.Kinds.Length];
        public string[] DecorSpots = new string[Decor.Spots];
        public int LanternColor;

        public int Season;

        public int PacksBought;
        public List<string> PaidTransactions = new List<string>();
        public List<OfferRecord> OfferRecords = new List<OfferRecord>();
        public long OfferClock = -1;
        public double OfferPlayMark;

        public int MoonJars;
        public long MoonJarBuyDay = -1;
        public int MoonJarsBoughtToday;
        public long MoonJarFreeDay = -1;
        public GardenGrid Grid = new GardenGrid();
        public AdsRecord AdsToday = new AdsRecord();
        public long NoAdsUntilDay = -1;
        public long MoonPassFromDay = -1, MoonPassUntilDay = -1, MoonPassMailDay = -1, MoonPassRemindedUntil = -1, MoonPassDoubleDay = -1;

        public HashSet<string> CodesUsed = new HashSet<string>();
        public List<Letter> Letters = new List<Letter>();
        public long MailDay = -1;

        public long BoxDay = -1;
        public long WeekIndex = -1;
        public int WeekPoints;
        public int WeekChestsClaimed;

        public long PassMonth = -1;
        public int PassPoints;
        public int PassFreeClaimed, PassPremiumClaimed;
        public long PassBoughtMonth = -1;
        public int MonthLanterns, MonthFrames;
        public int PhotoFrame = MonthPass.PlainFrame;

        public long FrogDay = -1;
        public int FrogShowers, FrogsToday;
        public long FrogsMet;
        public double FrogLeft;

        public int Baits;
        public long BaitAt = -1;
        public long Casts;
        public int[] Fish = new int[VuonTra.Core.Fishing.Points.Length];
        public long FishCaught, FishFed;

        public long Pets, Boosts, LayoutMoves, Merges, CloverCalls, GoldenCalls, JarsOpened, DailyGifts;
        public int DailyQuestsToday, BestFish;
        public NewcomerRecord Newcomer = new NewcomerRecord();
        public ReviewRecord Review = new ReviewRecord();

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
            GardenGrid.Settle(defs, s);
            return s;
        }
    }
}
