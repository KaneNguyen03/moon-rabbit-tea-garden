using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using UnityEditor;
using VuonTra.Game;

namespace VuonTra.Core.Tests
{
    [Category("Slow")]
    [Timeout(600000)]
    public class EconomySimulationTests
    {
        const double Day = 86400d;
        const double SellEvery = 120d;
        const double RabbitLevelSeconds = 10d;

        List<StationDef> defs;
        List<UpgradeDef> ups;
        List<QuestDef> quests;
        GameState s;
        Modifiers mods;
        System.Random rng;
        double clock, sessionClock, lastSale;

        int tapsPerSecond, buyEvery, buysAtOnce, questEvery, gameEvery;
        int poundingScore, leavesScore, firefliesScore;

        BigNum earnedTaps, earnedIdle, earnedBoost, earnedAway, earnedQuests, earnedTray, earnedGames, earnedOrders;
        BigNum earnedJars;
        int jarsOpened, jarsHeld;
        readonly Dictionary<long, int> freeJars = new Dictionary<long, int>();
        BigNum earnedFrogs;
        int frogsMet, cloversFrogs;
        System.Random frogRng;
        bool goFishing;
        const double FishCatchRate = 0.8;
        int casts, fishCaught, fishPoints, goldenHearts;
        System.Random fishRng;
        int cloversQuests, cloversGames, cloversOrders, goldenGift, goldenPhoto, goldenQuests, goldenGames, goldenTray, goldenSpent;
        int goldenNewTiers;
        int summoned, goldenCalls, visitorsBought, gamesPlayed, ordersDelivered, rabbitLevels, boosts;
        bool takeFreeDouble, everyAd;
        bool upgradeAll;
        bool raiseDishes;
        int dishesRaised;
        BigNum earnedDoubles, earnedAds;
        int goldenAds, extraRounds;
        bool newcomer;
        BigNum earnedNewcomer;
        int cloversNewcomer, goldenNewcomer;
        System.Random newcomerRng;
        readonly int[] newcomerDayAt = new int[VuonTra.Core.Newcomer.Days];
        int goldenChests, cloversChests, boxesOpened, chestsOpened;
        int goldenPass, cloversPass, passTiers;
        bool openChests, answerAsks, claimPass;
        long firstDay;
        BigNum earnedAsks;
        System.Random askRng;
        bool welcomeBack;
        int goldenWelcome, cloversWelcome, welcomeStarts, welcomeCheckIns;

        readonly Dictionary<int, double> levelAt = new Dictionary<int, double>();
        readonly Dictionary<int, double> plotsAt = new Dictionary<int, double>();
        readonly double[] waveLevelAt = new double[Expansion.Table.Length], waveRabbitsAt = new double[Expansion.Table.Length], waveTakenAt = new double[Expansion.Table.Length];
        double purpleAt = -1, goldAt = -1;
        readonly List<string> snapshots = new List<string>();
        readonly StringBuilder trace = new StringBuilder();

        [SetUp]
        public void SetUp()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<GameCatalog>("Assets/VuonTra/Content/GameCatalog.asset");
            Assert.IsNotNull(catalog, "Run VuonTra > Seed Content first.");
            defs = catalog.BuildDefs();
            ups = catalog.BuildUpgrades();
            quests = catalog.BuildQuests();
        }

        const int Seed = 20261122;

        static readonly int[] Seeds = { Seed, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 };

        const int Enough = 15;

        void Reset(bool tireless, int seed = Seed)
        {
            s = GameState.NewGame(defs, ups.Count);
            s.Profile.Code = Fixture.CodeFor(seed);
            Rabbits.GiftFirst(s, "R");
            mods = Modifiers.From(ups, s);
            rng = new System.Random(seed);
            clock = sessionClock = lastSale = 0;
            earnedTaps = earnedIdle = earnedBoost = earnedAway = earnedQuests = earnedTray = earnedGames = earnedOrders = earnedJars = BigNum.Zero;
            jarsOpened = jarsHeld = 0;
            freeJars.Clear();
            earnedFrogs = BigNum.Zero;
            frogsMet = cloversFrogs = 0;
            frogRng = new System.Random(unchecked(seed * 31 + 62));
            goFishing = false;
            casts = fishCaught = fishPoints = goldenHearts = 0;
            fishRng = new System.Random(unchecked(seed * 31 + 58));
            cloversQuests = cloversGames = cloversOrders = goldenGift = goldenPhoto = goldenQuests = goldenGames = goldenTray = goldenSpent = 0;
            goldenNewTiers = 0;
            summoned = goldenCalls = visitorsBought = gamesPlayed = ordersDelivered = rabbitLevels = boosts = 0;
            takeFreeDouble = everyAd = upgradeAll = false;
            raiseDishes = true;
            dishesRaised = 0;
            earnedDoubles = earnedAds = BigNum.Zero;
            goldenAds = extraRounds = 0;
            newcomer = false;
            earnedNewcomer = BigNum.Zero;
            cloversNewcomer = goldenNewcomer = 0;
            newcomerRng = new System.Random(unchecked(seed * 31 + 102));
            for (int i = 0; i < newcomerDayAt.Length; i++) newcomerDayAt[i] = 0;
            goldenChests = cloversChests = boxesOpened = chestsOpened = 0;
            goldenPass = cloversPass = passTiers = 0;
            openChests = true;
            claimPass = true;
            answerAsks = false;
            firstDay = 1;
            earnedAsks = BigNum.Zero;
            askRng = new System.Random(unchecked(seed * 31 + 103));
            welcomeBack = false;
            goldenWelcome = cloversWelcome = welcomeStarts = welcomeCheckIns = 0;
            levelAt.Clear();
            plotsAt.Clear();
            for (int i = 0; i < waveLevelAt.Length; i++) waveLevelAt[i] = waveRabbitsAt[i] = waveTakenAt[i] = -1;
            purpleAt = goldAt = -1;
            snapshots.Clear();
            trace.Clear();
            if (tireless)
            {
                tapsPerSecond = 3; buyEvery = 1; buysAtOnce = 4; questEvery = 1; gameEvery = 1;
                poundingScore = Pounding.MaxScore; leavesScore = Leaves.MaxScore; firefliesScore = Fireflies.MaxScore;
            }
            else
            {
                tapsPerSecond = 1; buyEvery = 3; buysAtOnce = 1; questEvery = 30; gameEvery = 240;
                poundingScore = 84; leavesScore = 31; firefliesScore = 21;
            }
        }

        static readonly List<ItemDef> BigGiftHat = new List<ItemDef>
        {
            new ItemDef { Id = Shop.BigGiftHat, Name = "Nón lá", Slot = ItemSlot.Hat, Effect = ItemEffect.None, Price = 20, Tier = 1 }
        };

        long Today => firstDay + (long)(clock / Day);

        BigNum Income => Economy.IncomePerSecond(defs, s) * (mods.IncomeMultiplier * mods.SpeedMultiplier);

        void OpenSession()
        {
            sessionClock = 0;
            lastSale = 0;
            if (welcomeBack) TakeWelcomeBack();
            VuonTra.Core.Seasons.Clamp(s, Today);
            Quests.RollDaily(quests, defs, s, Today, mods);
            goldenGift += Shop.ClaimDailyGift(s, Today, null, BigGiftHat, out _, out _);
            if (Photos.Take(s, Today)) goldenPhoto += Photos.GiftGolden;
            if (everyAd) while (Ads.ClaimShopGolden(s, Today)) goldenAds += Ads.ShopGoldenReward;
            MeetFrog();
            GoFishing();
            if (newcomer) TryTheWeeksTasks();
        }

        void TakeWelcomeBack()
        {
            int golden = s.GoldenCarrots, clovers = s.Clovers, jars = s.MoonJars;
            if (VuonTra.Core.WelcomeBack.Check(s, Today).Ok) welcomeStarts++;
            WelcomeBackGift gift = VuonTra.Core.WelcomeBack.CheckIn(s, Today, "W", VuonTra.Core.Newcomer.ShownKindRoll);
            if (gift.Ok) welcomeCheckIns++;
            if (gift.Rabbit != null) Rabbits.AutoAssign(defs, s);
            goldenWelcome += s.GoldenCarrots - golden;
            cloversWelcome += s.Clovers - clovers;
            jarsHeld += s.MoonJars - jars;
        }

        void TryTheWeeksTasks()
        {
            int petted = 0;
            foreach (Rabbit r in s.Rabbits)
            {
                if (petted >= 3) break;
                if (Rabbits.Pet(s, r, Today).FirstToday) petted++;
            }
            if (s.LayoutMoves == 0 && s.Grid.TryTileOf(GardenGrid.StationId(defs[1]), out Hex a) && s.Grid.TryTileOf(GardenGrid.StationId(defs[2]), out Hex b)
                && Layout.Drop(defs, s, a, b) == LayoutDrop.Swapped)
                Layout.Drop(defs, s, a, b);
        }

        void ClaimTheWeek()
        {
            for (int day = 1; day <= VuonTra.Core.Newcomer.Days; day++)
                for (int slot = 0; slot < VuonTra.Core.Newcomer.TasksPerDay; slot++)
                {
                    NewcomerClaim claim = VuonTra.Core.Newcomer.Claim(defs, s, mods, day, slot, "T", newcomerRng.NextDouble());
                    if (!claim.Ok) continue;
                    earnedNewcomer += claim.Carrots;
                    cloversNewcomer += claim.Clovers;
                    goldenNewcomer += claim.Golden + claim.DayGolden;
                    if (claim.DayPaid) newcomerDayAt[day - 1] = (int)Today;
                    if (claim.Rabbit != null) Rabbits.AutoAssign(defs, s);
                }
        }

        void GoFishing()
        {
            if (!goFishing) return;
            long now = (long)clock;
            FishingCast cast;
            while ((cast = Fishing.Cast(s, now, fishRng.NextDouble)) != null)
            {
                casts++;
                cast.Advance(cast.BobSeconds + (fishRng.NextDouble() < FishCatchRate ? 0.25 : 1d));
                if (cast.Tap() == CastTap.Caught && Fishing.Land(s, cast)) fishCaught++;
            }
            Rabbit r = Rabbits.AtStation(s, BestToTap());
            if (r == null) return;
            int golden = s.GoldenCarrots;
            Rabbits.Pet(s, r, Today);
            for (int k = Fishing.Kinds - 1; k >= 0; k--)
                for (FeedResult fed; s.Fish[k] > 0 && (fed = Fishing.Feed(s, r, k)).Fed;) fishPoints += fed.Points;
            goldenHearts += s.GoldenCarrots - golden;
        }

        int MostHearts()
        {
            int most = 0;
            foreach (Rabbit r in s.Rabbits) most = Math.Max(most, Rabbits.HeartsOf(r));
            return most;
        }

        void MeetFrog()
        {
            long day = Today;
            Tutorial.Advance(s);
            double[] showers = Weather.ShowerStarts(day);
            for (int k = 0; k < showers.Length; k++)
            {
                if ((s.FrogShowers & (1 << k)) != 0 && s.FrogDay == day) continue;
                double hour = showers[k] + 1d / 60d;
                if (FrogRabbit.Advance(s, 1d, day, hour) != FrogEvent.Arrived) return;
                if (FrogRabbit.Greet(defs, s, mods, day, hour, frogRng.NextDouble(), out BigNum paid, out int clovers))
                {
                    frogsMet++;
                    cloversFrogs += clovers;
                    earnedFrogs += paid;
                }
                return;
            }
        }

        int BestToTap()
        {
            int best = -1;
            BigNum bestIncome = BigNum.Zero;
            for (int i = 0; i < defs.Count; i++)
            {
                if (defs[i].Kind != StationKind.Brew || s.Levels[i] <= 0) continue;
                BigNum income = Economy.StationIncomePerSecond(defs, s, i, out _, out _);
                if (best < 0 || income > bestIncome) { best = i; bestIncome = income; }
            }
            return best < 0 ? 0 : best;
        }

        void Second(int taps)
        {
            int target = -1;
            BigNum best = BigNum.Zero, income = BigNum.Zero;
            for (int i = 0; i < defs.Count; i++)
            {
                if (defs[i].Kind != StationKind.Brew || s.Levels[i] <= 0) continue;
                BigNum here = Economy.StationIncomePerSecond(defs, s, i, out _, out _);
                if (target < 0 || here > best) { target = i; best = here; }
                if (defs[i].CycleSeconds > 0d && !Layout.IsStored(defs, s, i)) income += here;
            }
            if (target < 0) target = 0;
            BigNum tapValue = BigNum.Zero;
            for (int tap = 0; tap < taps; tap++)
                if (Simulation.Tap(defs, s, target, mods))
                    tapValue += Economy.StationIncomePerSecond(defs, s, target, out _, out _) * (mods.TapSeconds * mods.IncomeMultiplier);
            double boostedSeconds = Math.Min(1d, Math.Max(0d, s.BoostSecondsLeft));
            BigNum idleValue = income * (mods.IncomeMultiplier * mods.SpeedMultiplier);
            BigNum boostValue = idleValue * (boostedSeconds * (Energy.BoostSpeed - 1d));
            BigNum paid = BigNum.Zero;
            Simulation.Tick(defs, s, 1d, (i, amount) => paid += amount, mods, rng.NextDouble);
            BigNum expected = tapValue + idleValue + boostValue;
            if (!expected.IsZero && !paid.IsZero)
            {
                BigNum scale = paid / expected;
                earnedTaps += tapValue * scale;
                earnedIdle += idleValue * scale;
                earnedBoost += boostValue * scale;
            }
            Orders.Advance(defs, s, 1d, Today, rng.NextDouble);
            clock += 1d;
            sessionClock += 1d;
        }

        void SellTray(bool keepForGuest)
        {
            for (int slot = s.Tray.Count - 1; slot >= 0; slot--)
            {
                Product p = s.Tray[slot];
                if (keepForGuest && s.Order != null && !p.Golden && Needed(p.Station)) continue;
                earnedTray += Tray.Sell(defs, s, slot, mods, out int golden);
                goldenTray += golden;
            }
            lastSale = sessionClock;
        }

        bool Needed(int station)
        {
            foreach (OrderLine line in s.Order.Lines) if (line.Station == station) return true;
            return false;
        }

        void BuyCheapest()
        {
            if (Expansion.Expand(defs, s))
            {
                if (Expansion.Segments(s) <= waveTakenAt.Length) waveTakenAt[Expansion.Segments(s) - 1] = clock;
                mods = Modifiers.From(ups, s);
                return;
            }
            int best = -1;
            bool bestIsUpgrade = false;
            BigNum bestCost = BigNum.Zero;
            s.BuyMode = 1;
            for (int i = 0; i < defs.Count; i++)
            {
                if (s.Levels[i] == 0 && !Economy.CanUnlock(defs, s, i)) continue;
                if (Economy.LevelsLeft(s.Levels[i]) <= 0) continue;
                BigNum cost = Economy.CostOfNext(defs[i], s.Levels[i], 1);
                if (best < 0 || cost < bestCost) { best = i; bestCost = cost; bestIsUpgrade = false; }
            }
            for (int u = 0; u < ups.Count; u++)
            {
                if (Upgrades.IsMaxed(ups[u], s.UpgradeLevels[u])) continue;
                BigNum cost = Upgrades.CostOfNext(ups[u], s.UpgradeLevels[u]);
                if (best < 0 || cost < bestCost) { best = u; bestCost = cost; bestIsUpgrade = true; }
            }
            if (best < 0 || s.Coins < bestCost) return;
            if (bestIsUpgrade) { Upgrades.TryBuy(ups, s, best); mods = Modifiers.From(ups, s); }
            else Simulation.TryBuy(defs, s, best);
        }

        void LevelRabbits()
        {
            BigNum budget = Income * RabbitLevelSeconds;
            foreach (Rabbit r in s.Rabbits)
            {
                if (r.Station < 0 || Rabbits.IsMaxLevel(r) || Rabbits.LevelCost(r) > budget) continue;
                if (Rabbits.TryLevelUp(s, r.Id)) rabbitLevels++;
            }
        }

        void RaiseDishes()
        {
            while (raiseDishes)
            {
                int best = -1;
                double bestGain = 0d;
                BigNum before = Economy.IncomePerSecond(defs, s);
                if (before.IsZero) return;
                for (int i = 0; i < defs.Count; i++)
                {
                    if (!Recipes.CanRaise(s, i)) continue;
                    int dish = Recipes.CurrentDish(s, i), level = Recipes.Level(s, i);
                    Recipes.SetLevel(s, i, dish, level + 1);
                    double gain = (Economy.IncomePerSecond(defs, s) / before).ToDouble() - 1d;
                    Recipes.SetLevel(s, i, dish, level);
                    gain /= Recipes.CostOfNext(level);
                    if (gain > bestGain) { best = i; bestGain = gain; }
                }
                if (best < 0 || !Recipes.Raise(s, best)) return;
                dishesRaised++;
            }
        }

        void SpendClovers()
        {
            while (s.Clovers >= Rabbits.SummonCost)
            {
                Rabbits.Summon(s, rng.NextDouble(), "R", rng.NextDouble());
                summoned++;
                Rabbits.AutoAssign(defs, s);
            }
        }

        void SpendGolden()
        {
            Shop.Visitor v = Shop.VisitorFor(s, Today);
            if (!v.Bought && s.GoldenCarrots >= v.Price)
            {
                Shop.BuyVisitor(s, Today, "V");
                goldenSpent += v.Price;
                visitorsBought++;
                Rabbits.AutoAssign(defs, s);
            }
            while (s.GoldenCarrots >= Shop.GoldenSummonCost)
            {
                Shop.SummonGolden(s, rng.NextDouble(), "G", rng.NextDouble());
                goldenSpent += Shop.GoldenSummonCost;
                goldenCalls++;
                Rabbits.AutoAssign(defs, s);
            }
        }

        void ClaimQuests()
        {
            if (newcomer) ClaimTheWeek();
            foreach (QuestDef quest in quests)
            {
                int clovers = s.Clovers, golden = s.GoldenCarrots;
                int tierGolden = quest.Chained && !Quests.CurrentTier(quest, s).Merged ? Quests.GoldenNow(quest, s) : 0;
                if (!Quests.TryClaim(quest, defs, s, mods, out BigNum paid)) continue;
                earnedQuests += paid;
                cloversQuests += s.Clovers - clovers;
                goldenQuests += s.GoldenCarrots - golden;
                goldenNewTiers += tierGolden;
            }
            OpenQuestChests();
            ClaimMonthPass();
        }

        void AnswerPhotoAsk()
        {
            if (!answerAsks) return;
            PhotoAsks.Advance(defs, s, 1d, Today, askRng.NextDouble);
            if (PhotoAsks.Waiting(s) && PhotoAsks.Answer(defs, s, mods, Today, out BigNum paid)) earnedAsks += paid;
        }

        void OpenQuestChests()
        {
            if (!openChests) return;
            int clovers = s.Clovers, golden = s.GoldenCarrots, rabbits = s.Rabbits.Count, jars = s.MoonJars;
            BoxPrize[] box = QuestChests.OpenBox(quests, defs, s, mods);
            if (box != null)
            {
                boxesOpened++;
                foreach (BoxPrize prize in box) earnedQuests += prize.Coins;
            }
            for (int i = 0; i < QuestChests.Chests.Length; i++)
                if (QuestChests.ChestReady(s, i, s.DailyDay) && QuestChests.ClaimChest(s, i, s.DailyDay, "W", rng.NextDouble(), out _)) chestsOpened++;
            cloversChests += s.Clovers - clovers;
            goldenChests += s.GoldenCarrots - golden;
            jarsHeld += s.MoonJars - jars;
            if (s.Rabbits.Count != rabbits) Rabbits.AutoAssign(defs, s);
        }

        void ClaimMonthPass()
        {
            if (!claimPass) return;
            int clovers = s.Clovers, golden = s.GoldenCarrots, jars = s.MoonJars;
            for (int t = 1; t <= MonthPass.Tiers; t++)
                if (MonthPass.Claim(defs, s, mods, MonthRow.Free, t, s.DailyDay, null, null, out MonthClaim claim))
                {
                    passTiers++;
                    earnedQuests += claim.Coins;
                }
            cloversPass += s.Clovers - clovers;
            goldenPass += s.GoldenCarrots - golden;
            jarsHeld += s.MoonJars - jars;
        }

        void OpenJars()
        {
            if (s.MoonJars > jarsHeld)
            {
                freeJars.TryGetValue(Today, out int n);
                freeJars[Today] = n + s.MoonJars - jarsHeld;
            }
            while (MoonJars.Shown(s) && s.MoonJars > 0)
            {
                earnedJars += MoonJars.Open(defs, s, mods);
                jarsOpened++;
            }
            jarsHeld = s.MoonJars;
        }

        int MostFreeJarsInADay()
        {
            int most = 0;
            foreach (int n in freeJars.Values) if (n > most) most = n;
            return most;
        }

        int FreeJars()
        {
            int got = 0;
            foreach (int n in freeJars.Values) got += n;
            return got;
        }

        bool PlayAGame()
        {
            long day = Today;
            double seconds;
            BigNum carrots;
            int clovers = s.Clovers, golden = s.GoldenCarrots;
            if (Pounding.CanPlay(s, day) && Pounding.Start(s, day)) { seconds = 25d; Busy(seconds); carrots = Pounding.Finish(defs, s, mods, poundingScore).Carrots; }
            else if (Leaves.CanPlay(s, day) && Leaves.Start(s, day)) { seconds = Leaves.Seconds; Busy(seconds); carrots = Leaves.Finish(defs, s, mods, leavesScore).Carrots; }
            else if (Fireflies.CanPlay(s, day) && Fireflies.Start(s, day)) { seconds = Fireflies.Seconds; Busy(seconds); carrots = Fireflies.Finish(defs, s, mods, firefliesScore).Carrots; }
            else if (everyAd && ExtraRound(day)) return PlayAGame();
            else return false;
            earnedGames += carrots;
            cloversGames += s.Clovers - clovers;
            goldenGames += s.GoldenCarrots - golden;
            gamesPlayed++;
            return true;
        }

        bool ExtraRound(long day)
        {
            foreach (string game in Ads.Games)
                if (Ads.ClaimExtraRound(s, day, game)) { extraRounds++; return true; }
            return false;
        }

        void Busy(double seconds)
        {
            for (int i = 0; i < seconds; i++) Second(0);
        }

        void ActiveSecond()
        {
            if (Energy.CanBoost(s) && Energy.TryBoost(s)) boosts++;
            Second(tapsPerSecond);

            if (s.Order != null && Orders.CanDeliver(s, s.Order))
            {
                int clovers = s.Clovers;
                if (Orders.Deliver(defs, s, mods, out BigNum paid, out _)) { earnedOrders += paid; ordersDelivered++; cloversOrders += s.Clovers - clovers; }
            }
            if (Tray.IsFull(s) || sessionClock - lastSale >= SellEvery) SellTray(true);
            AnswerPhotoAsk();

            if ((int)sessionClock % buyEvery == 0)
            {
                for (int buys = 0; buys < buysAtOnce; buys++) BuyCheapest();
                LevelRabbits();
                RaiseDishes();
            }
            if ((int)sessionClock % questEvery == 0) ClaimQuests();
            int rabbitsBefore = s.Rabbits.Count;
            SpendClovers();
            SpendGolden();
            if (s.Rabbits.Count != rabbitsBefore) mods = Modifiers.From(ups, s);
            if ((int)sessionClock % gameEvery == 0 && PlayAGame()) Note();
            if ((int)sessionClock % 600 == 0) MeetFrog();
            OpenJars();
            Note();
            int t = (int)clock;
            if (t == 600) Snapshot("10 phút");
            if (t == 3600) Snapshot("1 giờ");
            if (t == 30 || t == 60 || t == 120 || t == 300 || t == 600 || t == 1200 || t == 3600 || t == 7200)
            {
                int open = 0, levels = 0;
                for (int k = 0; k < defs.Count; k++) { if (s.Levels[k] > 0) open++; levels += s.Levels[k]; }
                trace.AppendLine("  t=" + t + "s  cà rốt " + s.Coins.ToShortString() + "  thu/giây " + Income.ToShortString()
                    + "  trạm mở " + open + "  tổng cấp trạm " + levels + "  cấp vườn " + Garden.Level(s) + "  thỏ " + s.Rabbits.Count
                    + "  nhiệm vụ " + s.Claimed.Count + "  ván " + gamesPlayed + "  đơn " + ordersDelivered + "  vàng " + s.GoldenCarrots);
            }
        }

        void Note()
        {
            int level = Garden.Level(s);
            for (int l = 2; l <= level; l++)
                if (!levelAt.ContainsKey(l)) levelAt[l] = clock;
            if (!plotsAt.ContainsKey(s.Plots)) plotsAt[s.Plots] = clock;
            for (int w = 0; w < Expansion.Table.Length; w++)
            {
                MistSegment wave = Expansion.Table[w];
                if (waveLevelAt[w] < 0 && level >= wave.NeedLevel) waveLevelAt[w] = clock;
                if (waveRabbitsAt[w] < 0 && s.Rabbits.Count >= wave.NeedRabbits && s.SeenKinds.Count >= wave.NeedKinds) waveRabbitsAt[w] = clock;
            }
            if (purpleAt < 0 || goldAt < 0)
                foreach (Rabbit r in s.Rabbits)
                {
                    if (purpleAt < 0 && r.Tier >= 3) purpleAt = clock;
                    if (goldAt < 0 && r.Tier >= 4) goldAt = clock;
                }
        }

        void Play(double seconds)
        {
            OpenSession();
            UpgradeAll();
            for (int i = 0; i < seconds; i++) ActiveSecond();
            UpgradeAll();
            SellTray(false);
        }

        void UpgradeAll()
        {
            if (!upgradeAll) return;
            List<UpgradeStep> plan = UpgradePlan.Plan(defs, s, Simulation.Budget(s));
            if (plan.Count > 0 && UpgradePlan.Apply(defs, s, plan)) Note();
        }

        void Away(double seconds)
        {
            s.LastSeenUnixSeconds = 0;
            BigNum paid = Simulation.ClaimAway(defs, s, (long)seconds, out _, mods);
            earnedAway += paid;
            clock += seconds;
            if (takeFreeDouble && VuonTra.Core.Away.DoubleIsFree(s, Today) && VuonTra.Core.Away.Double(s, Today, paid)) earnedDoubles += paid;
            else if (everyAd && Ads.ClaimAwayDouble(s, Today, paid)) earnedAds += paid;
            Note();
        }

        static string Clock(double seconds)
        {
            if (seconds < 0 || seconds >= int.MaxValue) return "chưa đạt";
            int total = (int)seconds;
            if (total >= 86400) return "ngày " + (total / 86400 + 1) + " (" + (total / 3600) + " giờ " + (total % 3600 / 60) + " phút)";
            return total >= 3600 ? (total / 3600) + " giờ " + (total % 3600 / 60) + " phút" : (total / 60) + " phút " + (total % 60) + " giây";
        }

        BigNum TotalEarned => earnedTaps + earnedIdle + earnedBoost + earnedAway + earnedQuests + earnedTray + earnedGames + earnedOrders + earnedJars + earnedDoubles + earnedAds + earnedFrogs + earnedAsks + earnedNewcomer;

        double Share(BigNum part)
        {
            BigNum total = TotalEarned;
            return total.IsZero ? 0d : Math.Round((part / total).ToDouble() * 1000d) / 10d;
        }

        int GoldenEarned => goldenGift + goldenPhoto + goldenQuests + goldenGames + goldenTray + goldenAds + goldenHearts + goldenChests + goldenNewcomer + goldenPass + goldenWelcome + GoldenFromLevels;
        int GoldenFromLevels => s.GoldenCarrots + goldenSpent - goldenGift - goldenPhoto - goldenQuests - goldenGames - goldenTray - goldenAds - goldenHearts - goldenChests - goldenNewcomer - goldenPass - goldenWelcome;
        int CloversFromLevels => s.Clovers + summoned - cloversQuests - cloversGames - cloversOrders - cloversFrogs - cloversChests - cloversNewcomer - cloversPass - cloversWelcome;

        void Snapshot(string label)
        {
            int open = 0;
            for (int k = 0; k < defs.Count; k++) if (s.Levels[k] > 0) open++;
            snapshots.Add("| " + label + " | " + Income.ToShortString() + " | " + s.Coins.ToShortString() + " | " + Garden.Level(s) + " (" + (int)s.GardenXp + ") | " + s.Plots + " | " + open + " | " + s.Rabbits.Count
                + " | " + Share(earnedTaps) + " | " + Share(earnedIdle) + " | " + Share(earnedBoost) + " | " + Share(earnedAway) + " | " + Share(earnedQuests) + " | " + Share(earnedTray)
                + " | " + Share(earnedGames) + " | " + Share(earnedOrders) + " | " + Share(earnedJars) + " | " + Share(earnedFrogs) + " | " + GoldenEarned + " / " + goldenSpent + " |");
        }

        const string SnapshotHeader = "| Lúc | Thu/giây | Cà rốt | Cấp vườn (điểm) | Ô đất | Trạm | Thỏ | Chạm % | Trạm tự chạy % | Trà lực % | Vắng mặt % | Nhiệm vụ % | Khay % | Trò chơi % | Đơn khách % | Hũ trăng % | Thỏ Ếch % | Vàng kiếm / tiêu |\n|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|";

        string Milestones()
        {
            var sb = new StringBuilder();
            sb.AppendLine("| Mốc | Lúc đạt |");
            sb.AppendLine("|---|---|");
            foreach (int level in new[] { 3, 5, 10, 15 })
                sb.AppendLine("| Cấp vườn " + level + " | " + (levelAt.ContainsKey(level) ? Clock(levelAt[level]) : "chưa đạt") + " |");
            foreach (int plots in new[] { 6, 8, 10, 12 })
                sb.AppendLine("| " + plots + " ô đất (" + GardenGrid.SegmentsForPlots(plots) + " đoạn) | " + (plotsAt.ContainsKey(plots) ? Clock(plotsAt[plots]) : "chưa đạt") + " |");
            sb.AppendLine("| Thỏ Tím đầu tiên | " + Clock(purpleAt) + " |");
            sb.AppendLine("| Thỏ Vàng đầu tiên | " + Clock(goldAt) + " |");
            for (int w = 0; w < waveLevelAt.Length; w++)
                sb.AppendLine("| Đoạn " + Expansion.Table[w].SegmentsAfter + " | đủ cấp " + Clock(waveLevelAt[w]) + ", đủ thỏ " + Clock(waveRabbitsAt[w]) + ", mở " + Clock(waveTakenAt[w]) + " |");
            return sb.ToString();
        }

        string Extras()
        {
            return "Cỏ: " + cloversQuests + " từ nhiệm vụ, " + CloversFromLevels + " từ cấp vườn, " + cloversGames + " từ trò chơi, " + cloversOrders + " từ đơn khách, " + cloversFrogs + " từ Thỏ Ếch; gọi " + summoned + " thỏ.\n"
                + "Cà rốt vàng: " + goldenGift + " quà ngày, " + goldenPhoto + " ảnh, " + GoldenFromLevels + " cấp vườn, " + goldenQuests + " nhiệm vụ, " + goldenGames + " trò chơi, " + goldenTray + " khay; tiêu " + goldenSpent
                + " (" + goldenCalls + " lượt gọi vàng, " + visitorsBought + " thỏ ghé tiệm); còn " + s.GoldenCarrots + ".\n"
                + "Hũ trăng (#64): " + FreeJars() + " hũ miễn phí (nhiều nhất " + MostFreeJarsInADay() + " trong một ngày), mở " + jarsOpened + " hũ ngay khi ví cho mở, không mua hũ; còn " + s.MoonJars + ".\n"
                + "Câu cá (#58): thả " + casts + " lần, được " + fishCaught + " con (" + (int)(FishCatchRate * 100) + " % chạm đúng lúc), cho thỏ ở trạm thu nhiều nhất ăn " + fishPoints + " điểm, vuốt nó mỗi ngày; tim cao nhất "
                + MostHearts() + " (×" + Rabbits.HeartMultipliers[MostHearts()] + "), " + goldenHearts + " cà rốt vàng từ quà 10 tim.\n"
                + "Hộp quà ngày và rương tuần (#103): mở " + boxesOpened + " hộp, " + chestsOpened + " rương; " + goldenChests + " cà rốt vàng, " + cloversChests + " cỏ.\n"
                + "Vé tháng (#107), hàng miễn phí: nhận " + passTiers + " nấc; " + goldenPass + " cà rốt vàng, " + cloversPass + " cỏ.\n"
                + "Thỏ Ếch (#62): gặp " + frogsMet + " lần (mọi buổi chơi mở đầu trong mưa khi ngày còn ếch), " + earnedFrogs.ToShortString() + " cà rốt, " + cloversFrogs + " cỏ.\n"
                + "Sổ công thức (#110): nâng món " + dishesRaised + " lần, món cao nhất cấp " + TopDish() + ", còn " + s.RecipeLeaves + " lá.\n"
                + "Khác: " + gamesPlayed + " ván chơi, " + ordersDelivered + " đơn giao, " + rabbitLevels + " cấp thỏ mua, " + boosts + " lần trà lực, " + s.Claimed.Count + " nhiệm vụ đang nhận.\n";
        }

        int TopDish()
        {
            int top = 1;
            for (int i = 0; i < defs.Count; i++) top = Math.Max(top, Recipes.Level(s, i));
            return top;
        }

        int LevelsClimbed()
        {
            int climbed = Garden.Level(s) - 1;
            for (int k = 0; k < s.Season; k++) climbed += NewMoon.LevelNeeded(k) - 1;
            return climbed;
        }

        sealed class Week
        {
            public int Seed;
            public double Level5, Level10, Level15, Plots12, R8At, R9At, UpgradeAllOpens;
            public int LevelDayOne, GoldenCalls, Visitors, Summoned, GoldenAds, ExtraRounds, Plots, Rabbits, LevelEnd, GoldenEarnedDayOne;
            public int GoldenEarned, GoldenChests, Boxes, Chests;
            public readonly double[] Wait = new double[3];
            public double DoubleShare, AdShare;
            public double PlayShare, QuestShare;
            public int MostHearts, FishCaught, MostFreeJars, JarsOpened, FrogsMet;
            public int NewcomerClaimed, NewcomerGolden, NewcomerClovers;
            public int[] NewcomerDayAt;
            public string NewcomerMissing;
            public double Income7, Earned7;
            public int DishesRaised, TopDish, LeavesLeft;
            public string Table, Report, Fingerprint;
            public int Golden => GoldenCalls + Visitors;
        }

        Week CasualWeek(bool freeDouble, bool watchEveryAd, bool fishing = false, int seed = Seed, bool pressUpgradeAll = false, bool chests = true, bool wholeSet = false, bool firstWeek = false, bool pass = true, bool dishes = true)
        {
            Reset(false, seed);
            raiseDishes = dishes;
            openChests = chests;
            claimPass = pass;
            answerAsks = wholeSet;
            if (wholeSet) firstDay = QuestChests.MondayOf(QuestChests.WeekOf(1) + 1);
            newcomer = firstWeek;
            goFishing = fishing || firstWeek;
            upgradeAll = pressUpgradeAll;
            takeFreeDouble = freeDouble;
            everyAd = watchEveryAd;
            var w = new Week { Seed = seed };
            for (int day = 1; day <= 7; day++)
            {
                Play(20 * 60);
                if (day == 1) Snapshot("20 phút");
                Away(8 * 3600);
                Play(10 * 60);
                Away(Day - 8 * 3600 - 30 * 60);
                if (day == 1)
                {
                    Snapshot("hết ngày 1");
                    w.LevelDayOne = Garden.Level(s);
                    BigNum active = TotalEarned - earnedAway - earnedJars - earnedFrogs - earnedDoubles - earnedAds;
                    w.PlayShare = active.IsZero ? 0 : ((earnedGames + earnedOrders) / active).ToDouble() * 100d;
                    w.QuestShare = active.IsZero ? 0 : (earnedQuests / active).ToDouble() * 100d;
                    w.GoldenEarnedDayOne = GoldenEarned;
                }
                if (day == 2) Snapshot("hết ngày 2");
                if (day == 3) Snapshot("hết ngày 3");
                if (day == 7) Snapshot("hết ngày 7");
            }
            w.Level5 = At(levelAt, 5);
            w.Level10 = At(levelAt, 10);
            w.Level15 = At(levelAt, 15);
            w.UpgradeAllOpens = At(levelAt, UpgradePlan.UnlockLevel);
            w.Plots12 = At(plotsAt, 12);
            w.R8At = waveTakenAt.Length > 4 ? waveTakenAt[4] : -1;
            w.R9At = waveTakenAt.Length > 5 ? waveTakenAt[5] : -1;
            for (int i = 0; i < 3; i++) w.Wait[i] = waveRabbitsAt[i] < 0 ? double.PositiveInfinity : waveRabbitsAt[i] - waveLevelAt[i];
            w.GoldenCalls = goldenCalls; w.Visitors = visitorsBought; w.Summoned = summoned;
            w.GoldenAds = goldenAds; w.ExtraRounds = extraRounds;
            w.GoldenEarned = GoldenEarned; w.GoldenChests = goldenChests; w.Boxes = boxesOpened; w.Chests = chestsOpened;
            w.Plots = s.Plots; w.Rabbits = s.Rabbits.Count; w.LevelEnd = Garden.Level(s);
            w.DoubleShare = Share(earnedDoubles); w.AdShare = Share(earnedAds);
            w.MostHearts = MostHearts(); w.FishCaught = fishCaught;
            w.MostFreeJars = MostFreeJarsInADay(); w.JarsOpened = jarsOpened; w.FrogsMet = frogsMet;
            w.Income7 = Income.ToDouble();
            w.Earned7 = TotalEarned.ToDouble();
            w.DishesRaised = dishesRaised; w.TopDish = TopDish(); w.LeavesLeft = s.RecipeLeaves;
            w.NewcomerClaimed = 0;
            var missing = new List<string>();
            foreach (NewcomerTask task in VuonTra.Core.Newcomer.All)
                if (VuonTra.Core.Newcomer.IsClaimed(task, s)) w.NewcomerClaimed++;
                else missing.Add(task.Day + "/" + task.Id);
            w.NewcomerMissing = string.Join(" ", missing);
            w.NewcomerGolden = goldenNewcomer;
            w.NewcomerClovers = cloversNewcomer;
            w.NewcomerDayAt = (int[])newcomerDayAt.Clone();
            w.Table = Milestones();
            var report = new StringBuilder();
            report.AppendLine(w.Table);
            report.AppendLine(SnapshotHeader);
            foreach (string row in snapshots) report.AppendLine(row);
            report.AppendLine();
            report.AppendLine("Ngày 1, tính trên cà rốt kiếm được lúc đang chơi (không tính vắng mặt, hũ trăng, Thỏ Ếch, ×2 và video): nhiệm vụ " + Math.Round(w.QuestShare, 1) + " %, trò chơi + đơn khách " + Math.Round(w.PlayShare, 1) + " %; cà rốt vàng kiếm được " + w.GoldenEarnedDayOne + ".");
            report.AppendLine(Extras());
            report.AppendLine("Diễn biến:");
            report.Append(trace);
            w.Report = report.ToString();
            w.Fingerprint = SaveCodec.Encode(defs, s, ups) + "\n" + TotalEarned.Mantissa.ToString("R") + "e" + TotalEarned.Exponent + "\n" + w.Report;
            return w;
        }

        static double At(Dictionary<int, double> at, int key) => at.TryGetValue(key, out double t) ? t : -1;

        sealed class Hours
        {
            public double Level10, Ring7, Income;
            public int MostFreeJars;
            public string Report, Fingerprint;
        }

        Hours Tireless(int seed, bool fishing = false, double seconds = 2 * 3600)
        {
            Reset(true, seed);
            goFishing = fishing;
            Play(seconds);
            Snapshot(seconds >= 3600 ? (int)(seconds / 3600) + " giờ" : (int)(seconds / 60) + " phút");
            var h = new Hours { Level10 = At(levelAt, 10), Ring7 = waveTakenAt[3], Income = Income.ToDouble(), MostFreeJars = MostFreeJarsInADay() };
            var report = new StringBuilder();
            report.AppendLine(Milestones());
            report.AppendLine(SnapshotHeader);
            foreach (string row in snapshots) report.AppendLine(row);
            report.AppendLine();
            report.AppendLine(Extras());
            report.AppendLine("Diễn biến:");
            report.Append(trace);
            h.Report = report.ToString();
            h.Fingerprint = SaveCodec.Encode(defs, s, ups) + "\n" + h.Report;
            return h;
        }

        readonly Dictionary<string, Week[]> weeksOverSeeds = new Dictionary<string, Week[]>();
        Hours[] hoursOverSeeds;

        Week[] OverSeeds(bool freeDouble, bool watchEveryAd, bool fishing)
        {
            string key = freeDouble + "/" + watchEveryAd + "/" + fishing;
            if (!weeksOverSeeds.TryGetValue(key, out Week[] weeks))
            {
                weeks = new Week[Seeds.Length];
                for (int i = 0; i < Seeds.Length; i++) weeks[i] = CasualWeek(freeDouble, watchEveryAd, fishing, Seeds[i]);
                weeksOverSeeds[key] = weeks;
            }
            return weeks;
        }

        Hours[] TirelessOverSeeds()
        {
            if (hoursOverSeeds == null)
            {
                hoursOverSeeds = new Hours[Seeds.Length];
                for (int i = 0; i < Seeds.Length; i++) hoursOverSeeds[i] = Tireless(Seeds[i]);
            }
            return hoursOverSeeds;
        }

        static double Median(IEnumerable<double> values)
        {
            var sorted = new List<double>(values);
            sorted.Sort();
            int n = sorted.Count;
            return n == 0 ? double.NaN : n % 2 == 1 ? sorted[n / 2] : (sorted[n / 2 - 1] + sorted[n / 2]) / 2d;
        }

        static double Median<T>(IEnumerable<T> runs, Func<T, double> value)
        {
            var values = new List<double>();
            foreach (T run in runs) values.Add(value(run));
            return Median(values);
        }

        static int Count<T>(IEnumerable<T> runs, Func<T, bool> rule)
        {
            int n = 0;
            foreach (T run in runs) if (rule(run)) n++;
            return n;
        }

        static void Holds<T>(IList<T> runs, Func<T, bool> rule, string what, string report)
        {
            int n = Count(runs, rule);
            Assert.GreaterOrEqual(n, Enough, what + ": on " + n + " of " + runs.Count + " seeds, at least " + Enough + " asked\n" + report);
        }

        static void Always<T>(IList<T> runs, Func<T, bool> rule, string what, string report)
        {
            int n = Count(runs, rule);
            Assert.AreEqual(runs.Count, n, what + ": on " + n + " of " + runs.Count + " seeds, every one asked\n" + report);
        }

        static void HardRules(Week[] weeks, string report)
        {
            Holds(weeks, w => w.Level5 >= 4 * 60 && w.Level5 <= 12 * 60, "garden level 5 between minute four and minute twelve", report);
            Assert.LessOrEqual(Median(weeks, w => w.Level5), 9 * 60, "garden level 5 by minute nine on the median (the owner: about seven)\n" + report);
            Holds(weeks, w => w.LevelDayOne >= 10, "garden level 10 within day one", report);
            Holds(weeks, w => w.Level10 >= 2 * 3600, "garden level 10 not in the first sitting", report);
            Holds(weeks, w => w.Plots12 < 0 || w.Plots12 >= 2 * Day, "all twelve plots (R 7) wait for day three", report);
            Holds(weeks, w => w.Wait[0] <= 300 && w.Wait[1] <= 300 && w.Wait[2] <= 300, "no forest ring waits on rabbits", report);
        }

        static string SeedTable(Week[] weeks)
        {
            var sb = new StringBuilder();
            sb.AppendLine("| Hạt | Cấp 5 | Cấp 10 | Cấp hết ngày 1 | Cấp 15 | 12 ô đất (R 7) | R 8 | R 9 | Trò chơi + khách ngày 1 % | Nhiệm vụ ngày 1 % | Gọi vàng + thỏ ghé tiệm | Thu/giây hết ngày 7 | Cấp hết ngày 7 | Thỏ ngày 7 |");
            sb.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|---|---|---|");
            foreach (Week w in weeks)
                sb.AppendLine("| " + w.Seed + " | " + Clock(w.Level5) + " | " + Clock(w.Level10) + " | " + w.LevelDayOne + " | " + Clock(w.Level15) + " | " + Clock(w.Plots12) + " | " + Clock(w.R8At) + " | " + Clock(w.R9At)
                    + " | " + Math.Round(w.PlayShare, 1) + " | " + Math.Round(w.QuestShare, 1) + " | " + w.Golden + " | " + new BigNum(w.Income7).ToShortString() + " | " + w.LevelEnd + " | " + w.Rabbits + " |");
            sb.AppendLine("| **trung vị** | " + Clock(Median(weeks, w => w.Level5)) + " | " + Clock(Median(weeks, w => w.Level10)) + " | " + Median(weeks, w => w.LevelDayOne) + " | " + Clock(Median(weeks, w => Late(w.Level15)))
                + " | " + Clock(Median(weeks, w => Late(w.Plots12))) + " | " + Clock(Median(weeks, w => Late(w.R8At))) + " | " + Clock(Median(weeks, w => Late(w.R9At))) + " | " + Math.Round(Median(weeks, w => w.PlayShare), 1)
                + " | " + Math.Round(Median(weeks, w => w.QuestShare), 1) + " | " + Median(weeks, w => w.Golden) + " | " + new BigNum(Median(weeks, w => w.Income7)).ToShortString() + " | " + Median(weeks, w => w.LevelEnd) + " | " + Median(weeks, w => w.Rabbits) + " |");
            return sb.ToString();
        }

        static double Late(double seconds) => seconds < 0 ? double.MaxValue : seconds;

        const double DaySevenIncomeBeforeFourStations = 6.39e21;

        [Test]
        public void Casual_Week_And_Tireless_Two_Hours_Keep_The_Intended_Pace()
        {
            Week[] weeks = OverSeeds(false, false, false);
            Hours[] hours = TirelessOverSeeds();
            Week shipped = weeks[0];
            Hours tireless = hours[0];

            var report = new StringBuilder();
            report.AppendLine("# Nhịp kinh tế với đủ mọi nguồn thu (EconomySimulationTests)");
            report.AppendLine();
            report.AppendLine("## Người chơi thong thả, hạt " + Seed);
            report.AppendLine("1 chạm/giây vào trạm thu nhiều nhất, 3 giây mua một thứ rẻ nhất, 30 giây xem nhiệm vụ, 4 phút chơi một ván (2 sao), bán khay khi đầy hoặc mỗi 2 phút, giao đơn khi khay có đủ, gọi thỏ ngay khi có cỏ, mua thỏ ghé tiệm khi đủ vàng rồi gọi vàng mỗi 10, bật trà lực khi đủ, nâng cấp thỏ khi giá dưới 10 giây thu nhập.");
            report.AppendLine("Mỗi ngày: chơi 20 phút, vắng 8 giờ, chơi 10 phút, vắng tới hết ngày; 7 ngày.");
            report.AppendLine();
            report.Append(shipped.Report);
            report.AppendLine();
            report.AppendLine("## Người chơi thong thả trên " + Seeds.Length + " hạt (#83)");
            report.AppendLine("Mục tiêu chủ project: cấp 5 khoảng 7 phút, cấp 10 trong ngày 1, đủ 12 ô đất ngày 3, Mùa trăng mới ở cấp 30. Luật: trung vị, hoặc đúng trên ít nhất " + Enough + "/" + Seeds.Length + " hạt.");
            report.AppendLine();
            report.Append(SeedTable(weeks));
            report.AppendLine();
            report.AppendLine("## Người chơi không nghỉ tay, hạt " + Seed);
            report.AppendLine("3 chạm/giây, 4 lần mua mỗi giây, nhận nhiệm vụ ngay, chơi ván 3 sao ngay khi có lượt, còn lại như trên. Chơi liền 2 giờ.");
            report.AppendLine();
            report.Append(tireless.Report);
            report.AppendLine();
            report.AppendLine("Trên " + Seeds.Length + " hạt: cấp 10 trung vị lúc " + Clock(Median(hours, h => Late(h.Level10))) + ", vòng R 7 (12 ô đất) trung vị lúc " + Clock(Median(hours, h => Late(h.Ring7)))
                + ", thu/giây sau 2 giờ trung vị " + new BigNum(Median(hours, h => h.Income)).ToShortString() + " (mục tiêu R 7: không trước ~2 giờ; chưa đạt vì mỗi cấp trạm mua đều cho 1 điểm, 4 lần mua mỗi giây).");

            System.IO.Directory.CreateDirectory("Logs");
            System.IO.File.WriteAllText("Logs/economy-pacing.txt", report.ToString());
            TestContext.WriteLine(report.ToString());

            HardRules(weeks, report.ToString());
            Holds(weeks, w => w.QuestShare < 35, "quests should not dominate day one", report.ToString());
            double share = Median(weeks, w => w.PlayShare);
            Assert.GreaterOrEqual(share, 15, "the games and the guests should matter on day one, on the median\n" + report);
            Assert.LessOrEqual(share, 30, "but not take over\n" + report);
            Holds(weeks, w => w.Golden >= 2 && w.Golden <= 6, "a casual player should call with golden carrots a few times a week, but not every day (at most six)", report.ToString());
            Assert.LessOrEqual(Median(weeks, w => w.Income7), 10 * DaySevenIncomeBeforeFourStations, "day seven's income no more than about ten times what it was before the four new stations\n" + report);
            Always(weeks, w => w.JarsOpened > 0, "the casual player gets and opens jars", report.ToString());
            Always(weeks, w => w.MostFreeJars <= MoonJars.FreePerDay, "at most one free jar a day", report.ToString());
            Always(hours, h => h.MostFreeJars <= MoonJars.FreePerDay, "the tireless player too", report.ToString());
            Always(weeks, w => w.FrogsMet > 0 && w.FrogsMet <= 7 * FrogRabbit.PerDay, "the casual player meets the Frog Rabbit, two a day at most", report.ToString());
        }

        [Test]
        public void The_Same_Seed_Plays_The_Same_Game_Twice()
        {
            string[] names = { "casual", "tireless", "every video", "fishing" };
            Func<string>[] players =
            {
                () => CasualWeek(false, false).Fingerprint,
                () => Tireless(Seed).Fingerprint,
                () => CasualWeek(true, true).Fingerprint,
                () => CasualWeek(false, false, true).Fingerprint,
            };
            var first = new string[players.Length];
            for (int i = 0; i < players.Length; i++) first[i] = players[i]();
            for (int i = 0; i < players.Length; i++)
                Assert.AreEqual(first[i], players[i](), "the " + names[i] + " player's game played again on the same seed");
            Assert.AreNotEqual(first[0], CasualWeek(false, false, false, Seeds[1]).Fingerprint, "another seed plays another game");
        }

        [Test]
        public void Fishing_And_Feeding_Keep_The_Hard_Pacing_Rules()
        {
            Week[] before = OverSeeds(false, false, false);
            Week[] after = OverSeeds(false, false, true);
            Hours[] tirelessBefore = TirelessOverSeeds();
            var tirelessAfter = new Hours[Seeds.Length];
            for (int i = 0; i < Seeds.Length; i++) tirelessAfter[i] = Tireless(Seeds[i], true, 20 * 60);

            var report = new StringBuilder();
            report.AppendLine("# Câu cá và cho thỏ ăn (#58)");
            report.AppendLine();
            report.AppendLine("Cùng tuần chơi thong thả với EconomySimulationTests, trên " + Seeds.Length + " hạt. Có câu cá: mỗi buổi chơi thả hết số mồi đã hồi, " + (int)(FishCatchRate * 100) + " % lần chạm đúng lúc, cho hết cá vào thỏ ở trạm thu nhiều nhất và vuốt nó mỗi ngày.");
            report.AppendLine();
            report.AppendLine("| Hạt | Cấp 10 (không câu → câu) | Cấp hết ngày 1 | 12 ô đất | Trò chơi + khách ngày 1 % | Gọi vàng + thỏ ghé tiệm | Cấp hết ngày 7 | Cá câu được | Tim cao nhất |");
            report.AppendLine("|---|---|---|---|---|---|---|---|---|");
            for (int i = 0; i < Seeds.Length; i++)
            {
                Week b = before[i], a = after[i];
                report.AppendLine("| " + Seeds[i] + " | " + Clock(b.Level10) + " → " + Clock(a.Level10) + " | " + b.LevelDayOne + " → " + a.LevelDayOne + " | " + Clock(b.Plots12) + " → " + Clock(a.Plots12)
                    + " | " + Math.Round(b.PlayShare, 1) + " → " + Math.Round(a.PlayShare, 1) + " | " + b.Golden + " → " + a.Golden + " | " + b.LevelEnd + " → " + a.LevelEnd + " | " + a.FishCaught + " | " + a.MostHearts + " |");
            }
            double tenBefore = Median(before, w => w.Level10), tenAfter = Median(after, w => w.Level10);
            report.AppendLine("| **trung vị** | " + Clock(tenBefore) + " → " + Clock(tenAfter) + " | " + Median(before, w => w.LevelDayOne) + " → " + Median(after, w => w.LevelDayOne)
                + " | " + Clock(Median(before, w => Late(w.Plots12))) + " → " + Clock(Median(after, w => Late(w.Plots12))) + " | " + Math.Round(Median(before, w => w.PlayShare), 1) + " → " + Math.Round(Median(after, w => w.PlayShare), 1)
                + " | " + Median(before, w => w.Golden) + " → " + Median(after, w => w.Golden) + " | " + Median(before, w => w.LevelEnd) + " → " + Median(after, w => w.LevelEnd) + " | " + Median(after, w => w.FishCaught) + " | " + Median(after, w => w.MostHearts) + " |");
            report.AppendLine();
            double tirelessTenBefore = Median(tirelessBefore, h => Late(h.Level10)), tirelessTenAfter = Median(tirelessAfter, h => Late(h.Level10));
            report.AppendLine("Người chơi không nghỉ tay, trung vị: cấp 10 lúc " + Clock(tirelessTenBefore) + " không câu, " + Clock(tirelessTenAfter) + " có câu.");
            report.AppendLine();
            report.AppendLine("Mốc khi câu cá, hạt " + Seed + ":");
            report.AppendLine(after[0].Table);
            System.IO.Directory.CreateDirectory("Logs");
            System.IO.File.WriteAllText("Logs/economy-fishing.txt", report.ToString());
            TestContext.WriteLine(report.ToString());

            HardRules(after, report.ToString());
            Assert.GreaterOrEqual(tenAfter, 0.7 * tenBefore, "level 10 no more than 30 % sooner, on the median\n" + report);
            Assert.GreaterOrEqual(Median(after, w => w.PlayShare), 15, "the games and the guests should matter on day one, on the median\n" + report);
            Holds(after, w => w.Golden <= 6, "golden calls stay a few a week, at most six", report.ToString());
            Always(after, w => w.FishCaught > 0, "the casual player does catch fish", report.ToString());
            Assert.GreaterOrEqual(tirelessTenAfter, 0.7 * tirelessTenBefore, "nor for the tireless player\n" + report);
        }

        [Test]
        public void A_Casual_Newcomer_Claims_All_Twenty_Eight_Tasks_Of_The_First_Week_And_Keeps_The_Pace()
        {
            Week[] before = OverSeeds(false, false, false);
            var after = new Week[Seeds.Length];
            for (int i = 0; i < Seeds.Length; i++) after[i] = CasualWeek(false, false, false, Seeds[i], firstWeek: true);

            var report = new StringBuilder();
            report.AppendLine("# Tuần đầu của người mới (#102)");
            report.AppendLine();
            report.AppendLine("Cùng tuần chơi thong thả với EconomySimulationTests, trên " + Seeds.Length + " hạt; người mới thêm: câu cá mỗi buổi như #58, vuốt 3 bé mỗi ngày, đổi chỗ hai trạm một lần ở Bố trí, nhận việc Tuần đầu mỗi 30 giây cùng lúc xem nhiệm vụ.");
            report.AppendLine("Quà mỗi việc: " + VuonTra.Core.Newcomer.TaskSeconds + " giây thu nhập + cỏ " + string.Join("/", VuonTra.Core.Newcomer.TaskClovers) + " + cà rốt vàng " + string.Join("/", VuonTra.Core.Newcomer.TaskGolden)
                + " (ngày 1–7); quà đủ ngày: hũ trăng " + string.Join("/", VuonTra.Core.Newcomer.DayJars) + ", cà rốt vàng " + string.Join("/", VuonTra.Core.Newcomer.DayGolden) + ", thỏ Tím " + string.Join("/", VuonTra.Core.Newcomer.DayPurple) + ".");
            report.AppendLine();
            report.AppendLine("| Hạt | Việc đã nhận | Ngày nhận đủ (ngày 1–7) | Cấp 5 (không → có) | Cấp 10 (không → có) | Cấp hết ngày 1 | 12 ô đất | Gọi vàng + thỏ ghé tiệm | Vàng từ tuần đầu | Cỏ từ tuần đầu | Cấp hết ngày 7 | Chưa nhận |");
            report.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|---|");
            for (int i = 0; i < Seeds.Length; i++)
            {
                Week b = before[i], a = after[i];
                report.AppendLine("| " + Seeds[i] + " | " + a.NewcomerClaimed + "/" + VuonTra.Core.Newcomer.Tasks + " | " + string.Join(" ", a.NewcomerDayAt) + " | " + Clock(b.Level5) + " → " + Clock(a.Level5) + " | " + Clock(b.Level10) + " → " + Clock(a.Level10)
                    + " | " + b.LevelDayOne + " → " + a.LevelDayOne + " | " + Clock(b.Plots12) + " → " + Clock(a.Plots12) + " | " + b.Golden + " → " + a.Golden + " | " + a.NewcomerGolden + " | " + a.NewcomerClovers
                    + " | " + b.LevelEnd + " → " + a.LevelEnd + " | " + a.NewcomerMissing + " |");
            }
            double tenBefore = Median(before, w => w.Level10), tenAfter = Median(after, w => w.Level10);
            report.AppendLine("| **trung vị** | " + Median(after, w => w.NewcomerClaimed) + " | | " + Clock(Median(before, w => w.Level5)) + " → " + Clock(Median(after, w => w.Level5)) + " | " + Clock(tenBefore) + " → " + Clock(tenAfter) + " | " + Median(before, w => w.LevelDayOne) + " → " + Median(after, w => w.LevelDayOne)
                + " | " + Clock(Median(before, w => Late(w.Plots12))) + " → " + Clock(Median(after, w => Late(w.Plots12))) + " | " + Median(before, w => w.Golden) + " → " + Median(after, w => w.Golden)
                + " | " + Median(after, w => w.NewcomerGolden) + " | " + Median(after, w => w.NewcomerClovers) + " | " + Median(before, w => w.LevelEnd) + " → " + Median(after, w => w.LevelEnd) + " | |");
            report.AppendLine();
            report.AppendLine("Người mới, hạt " + Seed + ":");
            report.AppendLine(after[0].Report);
            System.IO.Directory.CreateDirectory("Logs");
            System.IO.File.WriteAllText("Logs/economy-newcomer.txt", report.ToString());
            TestContext.WriteLine(report.ToString());
            string text = report.ToString();

            Always(after, w => w.NewcomerClaimed == VuonTra.Core.Newcomer.Tasks, "the casual newcomer claims all 28 tasks within the seven days", text);
            Holds(after, w => w.Golden <= 6, "golden calls stay a few a week, at most six", text);
            Assert.GreaterOrEqual(tenAfter, 0.85 * tenBefore, "level 10 no more than 15 % sooner, on the median\n" + text);
            HardRules(after, text);
        }

        public const int MostGoldenFromChests = 25;
        public const int MostGoldenCallsWithChests = 9;

        [Test]
        public void The_Daily_Box_And_The_Week_Chests_Add_At_Most_Twenty_Five_Golden_Carrots_A_Week()
        {
            Week[] plain = OverSeeds(false, false, false);
            var without = new Week[Seeds.Length];
            var with = new Week[Seeds.Length];
            for (int i = 0; i < Seeds.Length; i++)
            {
                without[i] = CasualWeek(false, false, false, Seeds[i], false, false, true, pass: false);
                with[i] = CasualWeek(false, false, false, Seeds[i], false, true, true, pass: false);
            }

            var report = new StringBuilder();
            report.AppendLine("# Hộp quà ngày và rương tuần (#103)");
            report.AppendLine();
            report.AppendLine("Người chơi thong thả của EconomySimulationTests không trả lời thỏ xin ảnh, nên không bao giờ xong đủ 5 nhiệm vụ ngày: hộp ngày không mở, tuần của nó (thứ Sáu đến thứ Năm) chỉ tới rương " + QuestChests.Chests[0].Points + " điểm. "
                + "Bảng dưới là người chơi thong thả làm đủ bộ: cùng hai buổi chơi mỗi ngày, trả lời mọi thỏ xin ảnh, tuần bắt đầu thứ Hai, so khi không có và có hộp + rương; cả hai không nhận Vé tháng (#107) để chỉ đo hộp + rương.");
            report.AppendLine();
            report.AppendLine("| Hạt | Cà rốt vàng miễn phí (không → có) | Tăng | Từ hộp + rương | Gọi vàng + thỏ ghé tiệm (không → có) | Hộp | Rương | Người thong thả thường: gọi vàng, rương |");
            report.AppendLine("|---|---|---|---|---|---|---|---|");
            for (int i = 0; i < Seeds.Length; i++)
            {
                Week a = without[i], b = with[i];
                report.AppendLine("| " + Seeds[i] + " | " + a.GoldenEarned + " → " + b.GoldenEarned + " | " + (b.GoldenEarned - a.GoldenEarned) + " | " + b.GoldenChests + " | " + a.Golden + " → " + b.Golden
                    + " | " + b.Boxes + " | " + b.Chests + " | " + plain[i].Golden + ", " + plain[i].Chests + " |");
            }
            double rise = Median(Seeds.Length, i => with[i].GoldenEarned - without[i].GoldenEarned);
            report.AppendLine("| **trung vị** | " + Median(without, w => w.GoldenEarned) + " → " + Median(with, w => w.GoldenEarned) + " | " + rise + " | " + Median(with, w => w.GoldenChests)
                + " | " + Median(without, w => w.Golden) + " → " + Median(with, w => w.Golden) + " | " + Median(with, w => w.Boxes) + " | " + Median(with, w => w.Chests) + " | " + Median(plain, w => w.Golden) + ", " + Median(plain, w => w.Chests) + " |");
            report.AppendLine();
            report.AppendLine("Rương: " + QuestChests.Chests[0].Points + " điểm = " + QuestChests.Chests[0].Jars + " hũ + " + QuestChests.Chests[0].Clovers + " cỏ; " + QuestChests.Chests[1].Points + " điểm = " + QuestChests.Chests[1].Golden + " cà rốt vàng; "
                + QuestChests.Chests[2].Points + " điểm = thỏ bậc " + (QuestChests.Chests[2].RabbitTier + 1) + " + " + QuestChests.Chests[2].Golden + " cà rốt vàng.");
            System.IO.Directory.CreateDirectory("Logs");
            System.IO.File.WriteAllText("Logs/economy-chests.txt", report.ToString());
            TestContext.WriteLine(report.ToString());

            Always(with, w => w.Boxes == 7 && w.Chests == QuestChests.Chests.Length, "the whole-set player opens a box every day and all three chests", report.ToString());
            Assert.LessOrEqual(rise, MostGoldenFromChests, "free golden carrots a week rise by at most " + MostGoldenFromChests + ", on the median\n" + report);
            Assert.LessOrEqual(Median(with, w => w.GoldenChests), MostGoldenFromChests, "nor do the box and chests themselves pay more, on the median\n" + report);
            Holds(with, w => w.Golden <= MostGoldenCallsWithChests, "the whole-set player calls with golden carrots at most " + MostGoldenCallsWithChests + " times a week", report.ToString());
            Holds(plain, w => w.Golden <= 6, "the casual player still calls at most six times a week", report.ToString());
            Always(plain, w => w.Boxes == 0, "the casual player who leaves the photo asks never opens the box", report.ToString());
        }

        sealed class PassMonth
        {
            public int Seed, Tier, Day20 = -1, GoldenPass, CloversPass, Tiers, Boxes;
            public readonly List<int> GoldenByWeek = new List<int>();
            public int MostGoldenInAWeek => GoldenByWeek.Count == 0 ? 0 : GoldenByWeek.Max();
        }

        public const int PassDays = 30;
        public const int PassTierByDay30 = 20;
        public const int WholeSetPassDays = 21;

        PassMonth CasualMonth(int seed, bool wholeSet, int days)
        {
            Reset(false, seed);
            answerAsks = wholeSet;
            firstDay = MonthPass.FirstDayOf(MonthPass.MonthOf(1) + 2);
            var run = new PassMonth { Seed = seed };
            int callsBefore = 0;
            for (int day = 1; day <= days; day++)
            {
                Play(20 * 60);
                Away(8 * 3600);
                Play(10 * 60);
                Away(Day - 8 * 3600 - 30 * 60);
                if (run.Day20 < 0 && MonthPass.Tier(s, s.DailyDay) >= PassTierByDay30) run.Day20 = day;
                if (day % 7 == 0)
                {
                    run.GoldenByWeek.Add(goldenCalls + visitorsBought - callsBefore);
                    callsBefore = goldenCalls + visitorsBought;
                }
            }
            run.Tier = MonthPass.Tier(s, s.DailyDay);
            run.GoldenPass = goldenPass;
            run.CloversPass = cloversPass;
            run.Tiers = passTiers;
            run.Boxes = boxesOpened;
            return run;
        }

        [Test, Category("Slow"), Timeout(1800000)]
        public void A_Free_Player_Reaches_Month_Pass_Tier_Twenty_Within_Thirty_Days()
        {
            var casual = new PassMonth[Seeds.Length];
            var whole = new PassMonth[Seeds.Length];
            for (int i = 0; i < Seeds.Length; i++)
            {
                casual[i] = CasualMonth(Seeds[i], false, PassDays);
                whole[i] = CasualMonth(Seeds[i], true, WholeSetPassDays);
            }

            var report = new StringBuilder();
            report.AppendLine("# Vé tháng, hàng miễn phí (#107)");
            report.AppendLine();
            report.AppendLine("Tháng 31 ngày, hai buổi mỗi ngày (20 và 10 phút) như người chơi thong thả, nhận mọi nấc miễn phí ngay khi tới. Mỗi nấc " + MonthPass.PointsPerTier + " điểm, cùng điểm nhiệm vụ ngày của rương tuần (#103). "
                + "\"Thong thả\" (chơi " + PassDays + " ngày) không trả lời thỏ xin ảnh nên không bao giờ xong đủ 5 nhiệm vụ ngày; \"đủ bộ\" (chơi " + WholeSetPassDays + " ngày) là người chơi thong thả trả lời mọi thỏ xin ảnh, xong đủ 5 và mở hộp khi được (người chơi \"làm đủ mọi nhiệm vụ ngày\" của #103).");
            report.AppendLine();
            report.AppendLine("| Hạt | Thong thả: nấc ngày " + PassDays + " | ngày tới nấc " + PassTierByDay30 + " | gọi vàng + thỏ ghé tiệm mỗi tuần | vàng, cỏ từ vé | Đủ bộ: nấc ngày " + WholeSetPassDays + " | ngày tới nấc " + PassTierByDay30 + " | gọi vàng mỗi tuần | hộp |");
            report.AppendLine("|---|---|---|---|---|---|---|---|---|");
            for (int i = 0; i < Seeds.Length; i++)
            {
                PassMonth c = casual[i], w = whole[i];
                report.AppendLine("| " + Seeds[i] + " | " + c.Tier + " | " + c.Day20 + " | " + string.Join("/", c.GoldenByWeek) + " | " + c.GoldenPass + ", " + c.CloversPass
                    + " | " + w.Tier + " | " + w.Day20 + " | " + string.Join("/", w.GoldenByWeek) + " | " + w.Boxes + " |");
            }
            report.AppendLine("| **trung vị** | " + Median(casual, r => r.Tier) + " | " + Median(casual, r => r.Day20) + " | " + string.Join("/", WeekMedians(casual))
                + " | " + Median(casual, r => r.GoldenPass) + ", " + Median(casual, r => r.CloversPass) + " | " + Median(whole, r => r.Tier) + " | " + Median(whole, r => r.Day20) + " | " + string.Join("/", WeekMedians(whole)) + " | " + Median(whole, r => r.Boxes) + " |");
            report.AppendLine();
            report.AppendLine("Gọi vàng nhiều nhất trong một tuần, trung vị: thong thả " + Median(casual, r => r.MostGoldenInAWeek) + " (luật cũ: tối đa 6), đủ bộ " + Median(whole, r => r.MostGoldenInAWeek) + " (luật #103: tối đa " + MostGoldenCallsWithChests + ").");
            System.IO.Directory.CreateDirectory("Logs");
            System.IO.File.WriteAllText("Logs/economy-monthpass.txt", report.ToString());
            TestContext.WriteLine(report.ToString());

            Holds(casual, r => r.Day20 > 0 && r.Day20 <= PassDays, "the casual free player reaches tier " + PassTierByDay30 + " within " + PassDays + " days", report.ToString());
            Always(whole, r => r.Day20 > 0 && r.Day20 <= WholeSetPassDays, "the whole-set free player always does, within " + WholeSetPassDays + " days", report.ToString());
            Holds(casual, r => r.MostGoldenInAWeek <= 6, "with the free row the casual player still calls with golden carrots at most six times a week", report.ToString());
            Holds(whole, r => r.MostGoldenInAWeek <= MostGoldenCallsWithChests, "and the whole-set player at most " + MostGoldenCallsWithChests, report.ToString());
        }

        static List<string> WeekMedians(PassMonth[] runs)
        {
            var medians = new List<string>();
            for (int w = 0; w < runs[0].GoldenByWeek.Count; w++) { int week = w; medians.Add(Median(runs, r => r.GoldenByWeek[week]).ToString()); }
            return medians;
        }

        sealed class Return
        {
            public int AwayDays, Calls, CallsBefore, GoldenWelcome, CloversWelcome, Starts, CheckIns, LevelBefore, LevelEnd, PurpleRabbits;
        }

        Return ReturningPlayer(int seed, int awayDays, bool welcome)
        {
            CasualWeek(false, false, false, seed);
            var r = new Return { AwayDays = awayDays, CallsBefore = goldenCalls + visitorsBought, LevelBefore = Garden.Level(s) };
            int purple = 0;
            foreach (Rabbit rabbit in s.Rabbits) if (rabbit.Tier == VuonTra.Core.WelcomeBack.PurpleTier) purple++;
            welcomeBack = welcome;
            s.WelcomeBack.SeenDay = s.MaxDaySeen;
            Away(awayDays * Day);
            for (int day = 1; day <= 7; day++)
            {
                Play(20 * 60);
                Away(8 * 3600);
                Play(10 * 60);
                Away(Day - 8 * 3600 - 30 * 60);
            }
            r.Calls = goldenCalls + visitorsBought - r.CallsBefore;
            r.GoldenWelcome = goldenWelcome;
            r.CloversWelcome = cloversWelcome;
            r.Starts = welcomeStarts;
            r.CheckIns = welcomeCheckIns;
            r.LevelEnd = Garden.Level(s);
            foreach (Rabbit rabbit in s.Rabbits) if (rabbit.Tier == VuonTra.Core.WelcomeBack.PurpleTier) r.PurpleRabbits++;
            r.PurpleRabbits -= purple;
            return r;
        }

        [Test]
        public void A_Casual_Player_Back_After_Ten_Or_Twenty_Days_Gets_One_Welcome_Back_And_At_Most_Nine_Golden_Calls_That_Week()
        {
            int[] gaps = { 10, 20 };
            var without = new Return[gaps.Length][];
            var with = new Return[gaps.Length][];
            for (int g = 0; g < gaps.Length; g++)
            {
                without[g] = new Return[Seeds.Length];
                with[g] = new Return[Seeds.Length];
                for (int i = 0; i < Seeds.Length; i++)
                {
                    without[g][i] = ReturningPlayer(Seeds[i], gaps[g], false);
                    with[g][i] = ReturningPlayer(Seeds[i], gaps[g], true);
                }
            }

            var report = new StringBuilder();
            report.AppendLine("# Vườn nhớ bạn (#108)");
            report.AppendLine();
            report.AppendLine("Người chơi thong thả của EconomySimulationTests chơi tuần đầu, rồi vắng " + string.Join(" hoặc ", gaps) + " ngày (tuần đầu đã hết, viên Tuần đầu không còn), rồi chơi lại 7 ngày như cũ, trên " + Seeds.Length + " hạt. "
                + "Có Vườn nhớ bạn: quà ngay " + VuonTra.Core.WelcomeBack.GiftJars + " hũ + " + VuonTra.Core.WelcomeBack.GiftClovers + " cỏ + " + VuonTra.Core.WelcomeBack.GiftGolden + " cà rốt vàng (×" + VuonTra.Core.WelcomeBack.BigAwayTimes + " khi vắng ≥ " + VuonTra.Core.WelcomeBack.BigAwayDays + " ngày), điểm danh mỗi buổi đầu ngày, nhiệm vụ ngày ×" + VuonTra.Core.WelcomeBack.QuestTimes + ".");
            report.AppendLine();
            report.AppendLine("| Hạt | Vắng | Gọi vàng + thỏ ghé tiệm tuần đầu | Tuần quay lại (không → có) | Vàng từ Vườn nhớ bạn | Cỏ từ Vườn nhớ bạn | Lần bật | Điểm danh | Thỏ Tím | Cấp vườn (trước → hết tuần quay lại, có) |");
            report.AppendLine("|---|---|---|---|---|---|---|---|---|---|");
            for (int g = 0; g < gaps.Length; g++)
            {
                for (int i = 0; i < Seeds.Length; i++)
                {
                    Return a = without[g][i], b = with[g][i];
                    report.AppendLine("| " + Seeds[i] + " | " + gaps[g] + " | " + b.CallsBefore + " | " + a.Calls + " → " + b.Calls + " | " + b.GoldenWelcome + " | " + b.CloversWelcome + " | " + b.Starts + " | " + b.CheckIns + " | " + b.PurpleRabbits + " | " + b.LevelBefore + " → " + b.LevelEnd + " |");
                }
                report.AppendLine("| **trung vị** | " + gaps[g] + " | " + Median(with[g], r => r.CallsBefore) + " | " + Median(without[g], r => r.Calls) + " → " + Median(with[g], r => r.Calls) + " | " + Median(with[g], r => r.GoldenWelcome)
                    + " | " + Median(with[g], r => r.CloversWelcome) + " | | | | " + Median(with[g], r => r.LevelBefore) + " → " + Median(with[g], r => r.LevelEnd) + " |");
            }
            System.IO.Directory.CreateDirectory("Logs");
            System.IO.File.WriteAllText("Logs/economy-welcome-back.txt", report.ToString());
            TestContext.WriteLine(report.ToString());
            string text = report.ToString();

            for (int g = 0; g < gaps.Length; g++)
            {
                Always(with[g], r => r.Starts == 1 && r.CheckIns == VuonTra.Core.WelcomeBack.Days && r.PurpleRabbits >= 1, "one welcome back, seven check-ins and the Purple rabbit after " + gaps[g] + " days away", text);
                Always(without[g], r => r.Starts == 0 && r.GoldenWelcome == 0, "nothing without it", text);
                Holds(with[g], r => r.Calls <= MostGoldenCallsWithChests, "the week back calls with golden carrots at most " + MostGoldenCallsWithChests + " times, like the whole-set player", text);
            }
            Assert.AreEqual(VuonTra.Core.WelcomeBack.GiftGolden + 8, Median(with[0], r => r.GoldenWelcome), "ten days away: 5 at once and 3 + 5 from the check-in\n" + text);
            Assert.AreEqual(2 * VuonTra.Core.WelcomeBack.GiftGolden + 8, Median(with[1], r => r.GoldenWelcome), "twenty days away: the instant gifts doubled\n" + text);
        }

        static double Median(int n, Func<int, double> value)
        {
            var values = new List<double>();
            for (int i = 0; i < n; i++) values.Add(value(i));
            return Median(values);
        }

        static string Row(string label, Week[] weeks) =>
            "| " + label + " | " + Clock(Median(weeks, w => w.Level5)) + " | " + Clock(Median(weeks, w => w.Level10)) + " | " + Median(weeks, w => w.LevelDayOne) + " | " + Clock(Median(weeks, w => Late(w.Plots12)))
            + " | " + Median(weeks, w => w.LevelEnd) + " | " + Median(weeks, w => w.Plots) + " | " + Median(weeks, w => w.Rabbits) + " | " + Median(weeks, w => w.Golden) + " (" + Count(weeks, w => w.Golden <= 6) + "/" + weeks.Length + " ≤ 6)"
            + " | " + Median(weeks, w => w.GoldenAds) + " | " + Median(weeks, w => w.ExtraRounds) + " | " + Median(weeks, w => w.DoubleShare) + " | " + Median(weeks, w => w.AdShare) + " |";

        [Test]
        public void Watching_Every_Ad_Each_Day_Keeps_The_Hard_Pacing_Rules()
        {
            Week[] plain = OverSeeds(false, false, false);
            Week[] free = { CasualWeek(true, false) };
            Week[] ads = OverSeeds(true, true, false);

            var report = new StringBuilder();
            report.AppendLine("# Người chơi thong thả xem mọi quảng cáo (#66)");
            report.AppendLine();
            report.AppendLine("Cùng tuần chơi với EconomySimulationTests, trung vị trên " + Seeds.Length + " hạt. Mỗi ngày: ×2 vắng mặt miễn phí ở lần về đầu tiên trong ngày (#69); xem video ×2 ở lần về sau khi lượt miễn phí đã dùng (1/ngày); 2 video ở Tiệm (+1 cà rốt vàng mỗi lần); thêm 1 lượt mỗi trò khi đã chơi hết lượt (1/trò/ngày).");
            report.AppendLine();
            report.AppendLine("| Cách chơi | Cấp vườn 5 | Cấp vườn 10 | Cấp hết ngày 1 | 12 ô đất | Cấp hết ngày 7 | Ô đất ngày 7 | Thỏ ngày 7 | Lượt gọi vàng + thỏ ghé tiệm | Vàng từ video | Lượt thêm từ video | ×2 miễn phí % | ×2 video % |");
            report.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|---|---|");
            report.AppendLine(Row("Như hiện nay (không ×2, không quảng cáo)", plain));
            report.AppendLine(Row("Dùng ×2 miễn phí mỗi ngày (chỉ hạt " + Seed + ")", free));
            report.AppendLine(Row("×2 miễn phí + xem mọi quảng cáo", ads));
            report.AppendLine();
            report.AppendLine("Xem mọi quảng cáo, từng hạt:");
            report.Append(SeedTable(ads));
            report.AppendLine();
            report.AppendLine("Mốc khi xem mọi quảng cáo, hạt " + Seed + ":");
            report.AppendLine(ads[0].Table);
            int calls = Count(ads, w => w.Golden <= 6);
            report.AppendLine(calls >= Enough ? "Lượt gọi bằng cà rốt vàng vẫn trong mức \"vài lần một tuần, không phải mỗi ngày\" (≤ 6) trên " + calls + "/" + ads.Length + " hạt."
                : "Lượt gọi bằng cà rốt vàng vượt mức \"không phải mỗi ngày\" (> 6) trên " + (ads.Length - calls) + "/" + ads.Length + " hạt: cần chủ project quyết.");
            System.IO.Directory.CreateDirectory("Logs");
            System.IO.File.WriteAllText("Logs/economy-ads.txt", report.ToString());
            TestContext.WriteLine(report.ToString());

            HardRules(ads, report.ToString());
        }

        Week[] upgradeAllOverSeeds;

        Week[] UpgradeAllOverSeeds()
        {
            if (upgradeAllOverSeeds == null)
            {
                upgradeAllOverSeeds = new Week[Seeds.Length];
                for (int i = 0; i < Seeds.Length; i++) upgradeAllOverSeeds[i] = CasualWeek(false, false, false, Seeds[i], true);
            }
            return upgradeAllOverSeeds;
        }

        [Test, Category("Slow")]
        public void A_Casual_Week_With_Upgrade_All_Keeps_The_First_Minutes_And_Level_Ten_On_Day_One()
        {
            Week[] single = OverSeeds(false, false, false);
            Week[] all = UpgradeAllOverSeeds();

            var report = new StringBuilder();
            report.AppendLine("# Người chơi thong thả bấm Nâng tất cả (#99)");
            report.AppendLine();
            report.AppendLine("Cùng tuần chơi với EconomySimulationTests; người thứ hai bấm \"Nâng tất cả\" (UpgradePlan, cả ví) lúc mở mỗi buổi và trước khi rời. Trung vị trên " + Seeds.Length + " hạt.");
            report.AppendLine();
            report.AppendLine("| Luật (#6, #83) | Mua từng cấp | Bấm Nâng tất cả |");
            report.AppendLine("|---|---|---|");
            System.Action<string, Func<Week, bool>> rule = (label, holds) =>
                report.AppendLine("| " + label + " | " + Count(single, holds) + "/" + single.Length + " | " + Count(all, holds) + "/" + all.Length + " |");
            rule("Cấp 5 trong phút 4–12", w => w.Level5 >= 4 * 60 && w.Level5 <= 12 * 60);
            rule("Cấp 10 trong ngày 1", w => w.LevelDayOne >= 10);
            rule("Cấp 10 không trong buổi đầu (≥ 2 giờ)", w => w.Level10 >= 2 * 3600);
            rule("12 ô đất không trước ngày 3", w => w.Plots12 < 0 || w.Plots12 >= 2 * Day);
            rule("Ba đoạn đầu không chờ thỏ", w => w.Wait[0] <= 300 && w.Wait[1] <= 300 && w.Wait[2] <= 300);
            rule("Nhiệm vụ < 35 % ngày 1", w => w.QuestShare < 35);
            rule("Gọi vàng + thỏ ghé tiệm 2–6 trong tuần", w => w.Golden >= 2 && w.Golden <= 6);
            rule("Đoạn 5 (đất trạm 13) không trước ngày 4 (#99)", w => w.R8At < 0 || w.R8At >= 3 * Day);
            report.AppendLine("| Thu/giây hết ngày 7 không quá ×10 người mua từng cấp cùng hạt (#101) | – | " + Count(Pairs(single, all), p => p.All <= 10 * p.Single) + "/" + all.Length + " |");
            report.AppendLine("| Nâng tất cả mở ở cấp vườn " + UpgradePlan.UnlockLevel + " (trung vị, #101) | – | " + Clock(Median(all, w => Late(w.UpgradeAllOpens))) + " |");
            report.AppendLine("| Đoạn 5 mở (trung vị, #99) | " + Clock(Median(single, w => Late(w.R8At))) + " | " + Clock(Median(all, w => Late(w.R8At))) + " |");
            report.AppendLine("| Đoạn 6 mở (trung vị, #99; tuần chỉ có 7 ngày, đoạn 6 chờ tới ngày 8) | " + Clock(Median(single, w => Late(w.R9At))) + " | " + Clock(Median(all, w => Late(w.R9At))) + " |");
            report.AppendLine("| Trò chơi + khách ngày 1 (trung vị, luật 15–30 %) | " + Math.Round(Median(single, w => w.PlayShare), 1) + " % | " + Math.Round(Median(all, w => w.PlayShare), 1) + " % |");
            report.AppendLine("| Thu/giây hết ngày 7 (trung vị, luật ≤ " + new BigNum(10 * DaySevenIncomeBeforeFourStations).ToShortString() + ") | " + new BigNum(Median(single, w => w.Income7)).ToShortString() + " | " + new BigNum(Median(all, w => w.Income7)).ToShortString() + " |");
            report.AppendLine();
            report.AppendLine("Bấm Nâng tất cả, từng hạt:");
            report.Append(SeedTable(all));
            System.IO.Directory.CreateDirectory("Logs");
            System.IO.File.WriteAllText("Logs/economy-upgrade-all.txt", report.ToString());
            TestContext.WriteLine(report.ToString());
            string text = report.ToString();

            Holds(all, w => w.Level5 >= 4 * 60 && w.Level5 <= 12 * 60, "garden level 5 between minute four and minute twelve", text);
            Assert.LessOrEqual(Median(all, w => w.Level5), 9 * 60, "garden level 5 by minute nine on the median\n" + text);
            Holds(all, w => w.LevelDayOne >= 10, "garden level 10 within day one", text);
            Holds(all, w => w.Wait[0] <= 300 && w.Wait[1] <= 300 && w.Wait[2] <= 300, "no segment waits on rabbits", text);
            Holds(all, w => w.QuestShare < 35, "quests should not dominate day one", text);
            Holds(all, w => w.Golden >= 2 && w.Golden <= 6, "golden calls a few times a week, at most six", text);
            Always(all, w => w.JarsOpened > 0 && w.MostFreeJars <= MoonJars.FreePerDay, "jars come, at most one free a day", text);
            Always(all, w => w.FrogsMet > 0 && w.FrogsMet <= 7 * FrogRabbit.PerDay, "the Frog Rabbit, two a day at most", text);
            Always(all, w => w.R8At < 0 || w.R8At >= 3 * Day, "the land of station 13 (segment 5) not before day four (#99)", text);
            Always(single, w => w.R8At < 0 || w.R8At >= 3 * Day, "nor for the player buying a level at a time", text);

            Holds(all, w => w.Level10 >= 2 * 3600, "garden level 10 not in the first sitting (#101)", text);
            Holds(all, w => w.Plots12 < 0 || w.Plots12 >= 2 * Day, "all twelve plots wait for day three (#101)", text);
            double share = Median(all, w => w.PlayShare);
            Assert.GreaterOrEqual(share, 15, "the games and the guests matter on day one for the upgrade-all player too, on the median (#101)\n" + text);
            Assert.LessOrEqual(share, 30, "but do not take over (#101)\n" + text);
            Holds(Pairs(single, all), p => p.All <= 10 * p.Single, "day seven's income no more than ten times the one-level buyer's on the same seed (#101)", text);
            Assert.Greater(Median(all, w => w.Income7), Median(single, w => w.Income7), "the button still pays: more income by day seven than buying a level at a time (#101)\n" + text);
        }

        sealed class IncomePair
        {
            public double Single, All;
        }

        static IncomePair[] Pairs(Week[] single, Week[] all)
        {
            var pairs = new IncomePair[all.Length];
            for (int i = 0; i < all.Length; i++) pairs[i] = new IncomePair { Single = single[i].Income7, All = all[i].Income7 };
            return pairs;
        }

        sealed class Seasons
        {
            public int Seed;
            public readonly List<long> MoonDays = new List<long>();
            public readonly List<int> GoldenByWeek = new List<int>();
            public readonly List<int> TiersByWeek = new List<int>();
            public readonly List<int> AchievementGoldenByWeek = new List<int>();
            public readonly List<int> ClimbByWeek = new List<int>();
            public int DishesRaised, TopDish;
            public readonly List<int> NewTierGoldenByWeek = new List<int>();
            public int LongestStill, Tiers;
            public long MoonDay(int n) => MoonDays.Count >= n ? MoonDays[n - 1] : 99;
        }

        static List<string> WeeklyMedians(Seasons[] runs, int weeks)
        {
            var medians = new List<string>();
            for (int w = 0; w < weeks; w++) { int week = w; medians.Add(Median(runs, r => r.GoldenByWeek[week]).ToString()); }
            return medians;
        }

        bool TakeNewMoon(Seasons run)
        {
            if (!NewMoon.Begin(defs, s, "M", out _)) return false;
            Quests.RerollCarrotTarget(defs, s, Modifiers.From(ups, s));
            mods = Modifiers.From(ups, s);
            run.MoonDays.Add(Today);
            return true;
        }

        void PlayTakingNewMoons(double seconds, Seasons run)
        {
            OpenSession();
            TakeNewMoon(run);
            UpgradeAll();
            for (int i = 0; i < seconds; i++)
            {
                ActiveSecond();
                if ((int)sessionClock % 60 == 0) TakeNewMoon(run);
            }
            UpgradeAll();
            TakeNewMoon(run);
            SellTray(false);
        }

        Seasons CasualSeasons(int seed, int days, bool dishes = true)
        {
            Reset(false, seed);
            raiseDishes = dishes;
            var run = new Seasons { Seed = seed };
            int callsBefore = 0, levelBefore = 0, openBefore = 0, segmentsBefore = 0, seasonBefore = 0, still = 0, tiersBefore = 0, achievementGoldenBefore = 0, newTierGoldenBefore = 0;
            for (int day = 1; day <= days; day++)
            {
                PlayTakingNewMoons(20 * 60, run);
                Away(8 * 3600);
                PlayTakingNewMoons(10 * 60, run);
                Away(Day - 8 * 3600 - 30 * 60);
                if (day % 7 == 0)
                {
                    run.GoldenByWeek.Add(goldenCalls + visitorsBought - callsBefore);
                    run.ClimbByWeek.Add(LevelsClimbed());
                    callsBefore = goldenCalls + visitorsBought;
                    int tiers = Quests.TiersDone(quests, s);
                    run.TiersByWeek.Add(tiers - tiersBefore);
                    tiersBefore = run.Tiers = tiers;
                    run.AchievementGoldenByWeek.Add(goldenQuests - achievementGoldenBefore);
                    achievementGoldenBefore = goldenQuests;
                    run.NewTierGoldenByWeek.Add(goldenNewTiers - newTierGoldenBefore);
                    newTierGoldenBefore = goldenNewTiers;
                }
                int open = 0;
                for (int i = 0; i < defs.Count; i++) if (s.Levels[i] > 0) open++;
                int level = Garden.Level(s), segments = Expansion.Segments(s);
                bool news = s.Season > seasonBefore || level > levelBefore || open > openBefore || segments > segmentsBefore;
                if (day >= 8) { still = news ? 0 : still + 1; run.LongestStill = Math.Max(run.LongestStill, still); }
                levelBefore = level; openBefore = open; segmentsBefore = segments; seasonBefore = s.Season;
            }
            run.DishesRaised = dishesRaised;
            run.TopDish = TopDish();
            return run;
        }

        const int SeasonDays = 42;
        readonly Dictionary<bool, Seasons[]> seasonsOverSeeds = new Dictionary<bool, Seasons[]>();

        Seasons[] SeasonsOverSeeds(bool dishes)
        {
            if (!seasonsOverSeeds.TryGetValue(dishes, out Seasons[] runs))
            {
                runs = new Seasons[Seeds.Length];
                for (int i = 0; i < Seeds.Length; i++) runs[i] = CasualSeasons(Seeds[i], SeasonDays, dishes);
                seasonsOverSeeds[dishes] = runs;
            }
            return runs;
        }

        Week[] WithoutDishesOverSeeds()
        {
            const string key = "no-dishes";
            if (!weeksOverSeeds.TryGetValue(key, out Week[] weeks))
            {
                weeks = new Week[Seeds.Length];
                for (int i = 0; i < Seeds.Length; i++) weeks[i] = CasualWeek(false, false, false, Seeds[i], dishes: false);
                weeksOverSeeds[key] = weeks;
            }
            return weeks;
        }

        [Test, Category("Slow"), Timeout(3600000)]
        public void Recipe_Leaves_Lift_Week_One_At_Most_Fifteen_Percent_And_Speed_Up_Weeks_Two_To_Six()
        {
            Week[] before = WithoutDishesOverSeeds();
            Week[] after = OverSeeds(false, false, false);
            Seasons[] slow = SeasonsOverSeeds(false), fast = SeasonsOverSeeds(true);
            int weeks = SeasonDays / 7;

            var report = new StringBuilder();
            report.AppendLine("# Sổ công thức: lá công thức, cấp món và món 2–3 (#110a, #110b)");
            report.AppendLine();
            report.AppendLine("Người chơi thong thả như trên, trên " + Seeds.Length + " hạt; sổ mở ở cấp vườn " + Recipes.UnlockLevel + " (cấp của Mùa trăng mới đầu tiên) và từ đó luôn mở; người có sổ nâng món ngay khi đủ lá, chọn món cho nhiều thu nhập nhất trên mỗi lá. Lá: mỗi thỏ ở chỗ nghỉ 1 lá / "
                + (int)(Recipes.RestSecondsPerLeaf / 60) + " phút (cả lúc vắng, theo trần vắng mặt), mỗi đơn khách +" + Recipes.LeavesPerOrder + ". Cấp món k → k+1 giá " + Recipes.CostPerLevelSquared + "·k² lá, cấp 10 ×" + Recipes.TopLevelMultiplier + " thu nhập của riêng trạm đó. Khi sổ mở, trạm cấp " + Economy.Ranks[0] + " và " + Economy.Ranks[1] + " mở món 2 và 3, mỗi món ×" + Recipes.ExtraDishMultiplier
                + "; chỉ món đang bán (món cao nhất) tính cấp, món mới bắt đầu từ cấp món trước của trạm.");
            report.AppendLine();
            report.AppendLine("| Hạt | Thu/giây hết ngày 7: không sổ → có sổ | Tỉ lệ | Cà rốt kiếm tuần 1: tỉ lệ | Nâng món tuần 1 | Món cao nhất | Lá còn |");
            report.AppendLine("|---|---|---|---|---|---|---|");
            var ratios = new double[Seeds.Length];
            for (int i = 0; i < Seeds.Length; i++)
            {
                Week b = before[i], a = after[i];
                ratios[i] = a.Income7 / b.Income7;
                report.AppendLine("| " + Seeds[i] + " | " + new BigNum(b.Income7).ToShortString() + " → " + new BigNum(a.Income7).ToShortString() + " | " + Math.Round(ratios[i], 3) + " | " + Math.Round(a.Earned7 / b.Earned7, 3)
                    + " | " + a.DishesRaised + " | " + a.TopDish + " | " + a.LeavesLeft + " |");
            }
            Array.Sort(ratios);
            double medianRatio = ratios[ratios.Length / 2];
            report.AppendLine("| **trung vị** | | " + Math.Round(medianRatio, 3) + " | | " + Median(after, w => w.DishesRaised) + " | " + Median(after, w => w.TopDish) + " | |");
            report.AppendLine();
            report.AppendLine("Tuần 2–6 (chơi tới ngày " + SeasonDays + ", bắt đầu Mùa trăng mới ngay khi được; cấp món giữ qua mùa trăng mới). Số cấp vườn đã leo, cộng dồn qua các mùa, hết mỗi tuần (trung vị):");
            report.AppendLine();
            report.AppendLine("| | " + string.Join(" | ", Enumerable(weeks, w => "Tuần " + (w + 1))) + " | Trăng mới 1 | 2 | 3 | Nâng món | Món cao nhất |");
            report.AppendLine("|---" + string.Concat(Enumerable(weeks + 5, w => "|---")) + "|");
            report.AppendLine("| Không sổ (#98, số đo ở #110a) | 26 | 52 | 72 | 94 | 125 | 159 | 9 | 20 | 30 | – | 1 |");
            report.AppendLine("| Có sổ, chỉ món 1 (#110a) | 26 | 52 | 79 | 106 | 132 | 168 | 9 | 19 | 28 | 144 | 10 |");
            report.AppendLine("| Món 2–3 mở, không nâng món | " + string.Join(" | ", Enumerable(weeks, w => Median(slow, r => r.ClimbByWeek[w]).ToString())) + " | " + Median(slow, r => r.MoonDay(1)) + " | " + Median(slow, r => r.MoonDay(2)) + " | " + Median(slow, r => r.MoonDay(3)) + " | – | 1 |");
            report.AppendLine("| Có sổ, món 1–3 (#110b) | " + string.Join(" | ", Enumerable(weeks, w => Median(fast, r => r.ClimbByWeek[w]).ToString())) + " | " + Median(fast, r => r.MoonDay(1)) + " | " + Median(fast, r => r.MoonDay(2)) + " | " + Median(fast, r => r.MoonDay(3))
                + " | " + Median(fast, r => r.DishesRaised) + " | " + Median(fast, r => r.TopDish) + " |");
            report.AppendLine();
            report.AppendLine("| Hạt | Trăng mới 2: không nâng → có sổ | Trăng mới 3 | Cấp đã leo hết tuần 5 |");
            report.AppendLine("|---|---|---|---|");
            for (int i = 0; i < Seeds.Length; i++)
                report.AppendLine("| " + Seeds[i] + " | " + slow[i].MoonDay(2) + " → " + fast[i].MoonDay(2) + " | " + slow[i].MoonDay(3) + " → " + fast[i].MoonDay(3) + " | " + slow[i].ClimbByWeek[weeks - 1] + " → " + fast[i].ClimbByWeek[weeks - 1] + " |");
            System.IO.Directory.CreateDirectory("Logs");
            System.IO.File.WriteAllText("Logs/economy-recipes.txt", report.ToString());
            TestContext.WriteLine(report.ToString());
            string text = report.ToString();

            Assert.LessOrEqual(medianRatio, 1.15, "week one's income at most 15 % higher with the recipe book, on the median\n" + text);
            Holds(Pairs(before, after), p => p.All <= 1.25 * p.Single, "and no seed more than a quarter higher", text);
            for (int w = 1; w < weeks; w++)
            {
                int week = w;
                Assert.GreaterOrEqual(Median(fast, r => r.ClimbByWeek[week]), Median(slow, r => r.ClimbByWeek[week]), "raising dishes never climbs slower than leaving them, by the end of week " + (week + 1) + "\n" + text);
            }
            double raisedClimb = 0d, leftClimb = 0d;
            for (int w = 2; w < weeks; w++)
            {
                int week = w;
                raisedClimb += Median(fast, r => r.ClimbByWeek[week]);
                leftClimb += Median(slow, r => r.ClimbByWeek[week]);
            }
            Assert.Greater(raisedClimb, leftClimb, "and climbs more over weeks three to six\n" + text);
            Assert.LessOrEqual(Median(fast, r => r.MoonDay(2)), Median(slow, r => r.MoonDay(2)), "the second new moon no later\n" + text);
            Assert.LessOrEqual(Median(fast, r => r.MoonDay(3)), Median(slow, r => r.MoonDay(3)), "nor the third\n" + text);
            Assert.Greater(Median(fast, r => r.DishesRaised), 0, "dishes are raised\n" + text);
            Holds(fast, r => r.LongestStill <= 3, "never more than three days without a garden level or something opening", text);
        }

        static IEnumerable<string> Enumerable(int count, Func<int, string> make)
        {
            for (int i = 0; i < count; i++) yield return make(i);
        }

        [Test, Category("Slow"), Timeout(1800000)]
        public void A_Casual_Player_Reaches_The_Second_New_Moon_Before_Day_25_And_The_Third_Before_Day_35()
        {
            const int days = 35;
            Seasons[] runs = SeasonsOverSeeds(true);

            var report = new StringBuilder();
            report.AppendLine("# Các mùa sau mùa trăng mới đầu tiên (#98)");
            report.AppendLine();
            report.AppendLine("Người chơi thong thả như trên, chơi tới ngày " + SeasonDays + ", bắt đầu Mùa trăng mới ngay khi được. Mùa trăng mới cần cấp " + NewMoon.LevelNeeded(0) + ", rồi mỗi mùa thêm " + NewMoon.LevelStep
                + " cấp; điểm vườn mùa n nhân " + NewMoon.BonusPerSeason + "·n như thu nhập.");
            report.AppendLine();
            report.AppendLine("| Hạt | Trăng mới 1 | 2 | 3 | Gọi vàng + thỏ ghé tiệm mỗi tuần | Chuỗi ngày dài nhất không lên cấp, không mở gì |");
            report.AppendLine("|---|---|---|---|---|---|");
            foreach (Seasons r in runs)
                report.AppendLine("| " + r.Seed + " | " + r.MoonDay(1) + " | " + r.MoonDay(2) + " | " + r.MoonDay(3) + " | " + string.Join(" / ", r.GoldenByWeek) + " | " + r.LongestStill + " |");
            report.AppendLine("| **trung vị** | " + Median(runs, r => r.MoonDay(1)) + " | " + Median(runs, r => r.MoonDay(2)) + " | " + Median(runs, r => r.MoonDay(3)) + " | "
                + string.Join(" / ", WeeklyMedians(runs, days / 7)) + " | " + Median(runs, r => r.LongestStill) + " |");
            System.IO.Directory.CreateDirectory("Logs");
            System.IO.File.WriteAllText("Logs/economy-seasons.txt", report.ToString());
            TestContext.WriteLine(report.ToString());
            string text = report.ToString();

            Holds(runs, r => r.MoonDay(1) >= 7, "the first new moon not before day seven", text);
            Holds(runs, r => r.MoonDay(2) < 25, "the second new moon before day 25", text);
            Holds(runs, r => r.MoonDay(3) < 35, "the third new moon before day 35", text);
            Holds(runs, r => r.LongestStill <= 3, "never more than three days without a garden level or something opening", text);
            for (int w = 0; w < days / 7; w++)
            {
                int week = w;
                Holds(runs, r => r.GoldenByWeek[week] <= 6, "golden calls at most six in week " + (week + 1), text);
            }
        }

        [Test, Category("Slow"), Timeout(1800000)]
        public void Tiered_Achievements_Add_At_Most_Twenty_Golden_Carrots_In_Week_One_And_Three_Tiers_A_Week_After()
        {
            const int days = 42;
            var runs = new Seasons[Seeds.Length];
            for (int i = 0; i < Seeds.Length; i++) runs[i] = CasualSeasons(Seeds[i], days);

            var report = new StringBuilder();
            report.AppendLine("# Thành tựu nhiều nấc (#111)");
            report.AppendLine();
            report.AppendLine("Người chơi thong thả như trên, chơi tới ngày " + days + ", bắt đầu Mùa trăng mới ngay khi được. Thành tựu một lần cũ là các nấc giữ đúng mốc và quà cũ, nên phần vàng thêm so với trước #111 là vàng từ các nấc mới. Mỗi tuần: nấc mới nhận, vàng từ các nấc mới, vàng từ mọi nhiệm vụ (cả thành tựu cũ và nhiệm vụ mùa), gọi vàng + thỏ ghé tiệm.");
            report.AppendLine();
            report.AppendLine("| Hạt | Nấc nhận mỗi tuần | Vàng từ nấc mới mỗi tuần | Vàng từ mọi nhiệm vụ mỗi tuần | Gọi vàng + thỏ ghé tiệm mỗi tuần | Tổng nấc ngày " + days + " |");
            report.AppendLine("|---|---|---|---|---|---|");
            foreach (Seasons r in runs)
                report.AppendLine("| " + r.Seed + " | " + string.Join(" / ", r.TiersByWeek) + " | " + string.Join(" / ", r.NewTierGoldenByWeek) + " | " + string.Join(" / ", r.AchievementGoldenByWeek)
                    + " | " + string.Join(" / ", r.GoldenByWeek) + " | " + r.Tiers + " |");
            var tierMedians = new List<string>();
            var newMedians = new List<string>();
            var goldenMedians = new List<string>();
            for (int w = 0; w < days / 7; w++)
            {
                int week = w;
                tierMedians.Add(Median(runs, r => r.TiersByWeek[week]).ToString());
                newMedians.Add(Median(runs, r => r.NewTierGoldenByWeek[week]).ToString());
                goldenMedians.Add(Median(runs, r => r.AchievementGoldenByWeek[week]).ToString());
            }
            report.AppendLine("| **trung vị** | " + string.Join(" / ", tierMedians) + " | " + string.Join(" / ", newMedians) + " | " + string.Join(" / ", goldenMedians)
                + " | " + string.Join(" / ", WeeklyMedians(runs, days / 7)) + " | " + Median(runs, r => r.Tiers) + " |");
            System.IO.Directory.CreateDirectory("Logs");
            System.IO.File.WriteAllText("Logs/economy-achievements.txt", report.ToString());
            TestContext.WriteLine(report.ToString());
            string text = report.ToString();

            Always(runs, r => r.NewTierGoldenByWeek[0] <= 20, "golden carrots from achievements in week one rise by at most twenty", text);
            for (int w = 1; w < days / 7; w++)
            {
                int week = w;
                Holds(runs, r => r.TiersByWeek[week] >= 3, "at least three new tiers in week " + (week + 1), text);
            }
        }
    }
}
