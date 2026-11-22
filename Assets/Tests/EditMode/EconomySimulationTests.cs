using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using UnityEditor;
using VuonTra.Game;

namespace VuonTra.Core.Tests
{
    /// <summary>
    /// Plays the shipped game with every source of carrots the garden has (taps, idle stations, time away, the
    /// boost, quests, the tray, the three games, guests' orders) and the shop's golden carrots, for a casual
    /// player over a week and a tireless one over two hours, and writes where each stands at every milestone
    /// and where the carrots came from, to Logs/economy-pacing.txt. The asserts hold the pace the owner asked
    /// for in #6; the table is what the numbers get tuned against.
    /// </summary>
    public class EconomySimulationTests
    {
        const double Day = 86400d;
        const double SellEvery = 120d;
        /// <summary>A rabbit is levelled when its next level costs no more than this many seconds of income.</summary>
        const double RabbitLevelSeconds = 10d;

        List<StationDef> defs;
        List<UpgradeDef> ups;
        List<QuestDef> quests;
        GameState s;
        Modifiers mods;
        System.Random rng;
        double clock, sessionClock, lastSale;

        // How the simulated player plays.
        int tapsPerSecond, buyEvery, buysAtOnce, questEvery, gameEvery;
        int poundingScore, leavesScore, firefliesScore;

        // Where the carrots came from.
        BigNum earnedTaps, earnedIdle, earnedBoost, earnedAway, earnedQuests, earnedTray, earnedGames, earnedOrders;
        /// <summary>Moon jars (#64): carrots from opened jars, jars opened, and the free jars that came on each day. The player opens every jar the moment the purse shows them and buys none.</summary>
        BigNum earnedJars;
        int jarsOpened, jarsHeld;
        readonly Dictionary<long, int> freeJars = new Dictionary<long, int>();
        // Where the clovers and golden carrots came from.
        int cloversQuests, cloversGames, cloversOrders, goldenGift, goldenPhoto, goldenQuests, goldenGames, goldenTray, goldenSpent;
        int summoned, goldenCalls, visitorsBought, gamesPlayed, ordersDelivered, rabbitLevels, boosts;

        // When things happened (seconds on the clock; -1 = not yet).
        readonly Dictionary<int, double> levelAt = new Dictionary<int, double>();
        readonly Dictionary<int, double> plotsAt = new Dictionary<int, double>();
        readonly double[] waveLevelAt = new double[4], waveRabbitsAt = new double[4], waveTakenAt = new double[4];
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

        void Reset(bool tireless)
        {
            s = GameState.NewGame(defs, ups.Count);
            Rabbits.GiftFirst(s, "R"); // every new garden starts with its cotton rabbit at the teapot
            mods = Modifiers.From(ups, s);
            rng = new System.Random(20261122);
            clock = sessionClock = lastSale = 0;
            earnedTaps = earnedIdle = earnedBoost = earnedAway = earnedQuests = earnedTray = earnedGames = earnedOrders = earnedJars = BigNum.Zero;
            jarsOpened = jarsHeld = 0;
            freeJars.Clear();
            cloversQuests = cloversGames = cloversOrders = goldenGift = goldenPhoto = goldenQuests = goldenGames = goldenTray = goldenSpent = 0;
            summoned = goldenCalls = visitorsBought = gamesPlayed = ordersDelivered = rabbitLevels = boosts = 0;
            levelAt.Clear();
            plotsAt.Clear();
            for (int i = 0; i < 4; i++) waveLevelAt[i] = waveRabbitsAt[i] = waveTakenAt[i] = -1;
            purpleAt = goldAt = -1;
            snapshots.Clear();
            trace.Clear();
            if (tireless)
            {
                tapsPerSecond = 3; buyEvery = 1; buysAtOnce = 4; questEvery = 1; gameEvery = 1;
                poundingScore = Pounding.MaxScore; leavesScore = Leaves.MaxScore; firefliesScore = Fireflies.MaxScore; // three stars every time
            }
            else
            {
                tapsPerSecond = 1; buyEvery = 3; buysAtOnce = 1; questEvery = 30; gameEvery = 240;
                poundingScore = 84; leavesScore = 31; firefliesScore = 21; // two stars, about 70 % of the most
            }
        }

        /// <summary>The hat the first week's big gift brings (#57), as the shop sells it.</summary>
        static readonly List<ItemDef> BigGiftHat = new List<ItemDef>
        {
            new ItemDef { Id = Shop.BigGiftHat, Name = "Nón lá", Slot = ItemSlot.Hat, Effect = ItemEffect.None, Price = 20, Tier = 1 }
        };

        long Today => 1 + (long)(clock / Day);

        BigNum Income => Economy.IncomePerSecond(defs, s) * (mods.IncomeMultiplier * mods.SpeedMultiplier);

        /// <summary>The player opens the garden: the day's chores first.</summary>
        void OpenSession()
        {
            sessionClock = 0;
            lastSale = 0;
            Quests.RollDaily(quests, defs, s, Today, mods);
            goldenGift += Shop.ClaimDailyGift(s, Today, null, BigGiftHat, out _, out _); // the first seventh day brings the hat and a clover (#57)
            if (Photos.Take(s, Today)) goldenPhoto += Photos.GiftGolden;
        }

        /// <summary>The open brew station that earns most: where a tap is worth most.</summary>
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

        /// <summary>One second of the garden running, with so many taps; the carrots it pays are split by what made them.</summary>
        void Second(int taps)
        {
            int target = BestToTap();
            BigNum tapValue = BigNum.Zero;
            for (int tap = 0; tap < taps; tap++)
                if (Simulation.Tap(defs, s, target, mods))
                    tapValue += Economy.StationIncomePerSecond(defs, s, target, out _, out _) * (mods.TapSeconds * mods.IncomeMultiplier);
            double boostedSeconds = Math.Min(1d, Math.Max(0d, s.BoostSecondsLeft));
            BigNum idleValue = Income;
            BigNum boostValue = idleValue * (boostedSeconds * (Energy.BoostSpeed - 1d));
            BigNum paid = BigNum.Zero;
            Simulation.Tick(defs, s, 1d, (i, amount) => paid += amount, mods, rng.NextDouble);
            // Cycles pay in whole pieces; what each source would have paid this second shares out what was really paid.
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

        /// <summary>Sells the tray, keeping what a waiting guest asked for. Golden stacks always go: guests do not take them.</summary>
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
            if (Expansion.TryExpand(defs, s))
            {
                waveTakenAt[Expansion.WavesTaken(s.Plots) - 1] = clock;
                mods = Modifiers.From(ups, s); // one more place to rest (#60)
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

        /// <summary>A working rabbit whose next level is cheap next to the income gets it.</summary>
        void LevelRabbits()
        {
            BigNum budget = Income * RabbitLevelSeconds;
            foreach (Rabbit r in s.Rabbits)
            {
                if (r.Station < 0 || Rabbits.IsMaxLevel(r) || Rabbits.LevelCost(r) > budget) continue;
                if (Rabbits.TryLevelUp(s, r.Id)) rabbitLevels++;
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
            foreach (QuestDef quest in quests)
            {
                int clovers = s.Clovers, golden = s.GoldenCarrots;
                if (!Quests.TryClaim(quest, defs, s, mods, out BigNum paid)) continue;
                earnedQuests += paid;
                cloversQuests += s.Clovers - clovers;
                goldenQuests += s.GoldenCarrots - golden;
            }
        }

        /// <summary>Counts the jars that came since the last look (all free: this player buys none) and opens them while the purse shows them.</summary>
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

        /// <summary>Plays the first game that is open today: the garden runs on while the player is busy with it.</summary>
        bool PlayAGame()
        {
            long day = Today;
            double seconds;
            BigNum carrots;
            int clovers = s.Clovers, golden = s.GoldenCarrots;
            if (Pounding.CanPlay(s, day) && Pounding.Start(s, day)) { seconds = 25d; Busy(seconds); carrots = Pounding.Finish(defs, s, mods, poundingScore).Carrots; }
            else if (Leaves.CanPlay(s, day) && Leaves.Start(s, day)) { seconds = Leaves.Seconds; Busy(seconds); carrots = Leaves.Finish(defs, s, mods, leavesScore).Carrots; }
            else if (Fireflies.CanPlay(s, day) && Fireflies.Start(s, day)) { seconds = Fireflies.Seconds; Busy(seconds); carrots = Fireflies.Finish(defs, s, mods, firefliesScore).Carrots; }
            else return false;
            earnedGames += carrots;
            cloversGames += s.Clovers - clovers;
            goldenGames += s.GoldenCarrots - golden;
            gamesPlayed++;
            return true;
        }

        void Busy(double seconds)
        {
            for (int i = 0; i < seconds; i++) Second(0);
        }

        /// <summary>One second of an attentive player.</summary>
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

            if ((int)sessionClock % buyEvery == 0)
            {
                for (int buys = 0; buys < buysAtOnce; buys++) BuyCheapest();
                LevelRabbits();
            }
            if ((int)sessionClock % questEvery == 0) ClaimQuests();
            int rabbitsBefore = s.Rabbits.Count;
            SpendClovers();
            SpendGolden();
            // As in the game, who works and who rests feeds the modifiers (#60).
            if (s.Rabbits.Count != rabbitsBefore) mods = Modifiers.From(ups, s);
            if ((int)sessionClock % gameEvery == 0 && PlayAGame()) Note();
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
            for (int w = 0; w < Expansion.Waves.Length; w++)
            {
                ExpansionWave wave = Expansion.Waves[w];
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

        /// <summary>A sitting: the day's chores, so many seconds of play, and the tray sold before leaving.</summary>
        void Play(double seconds)
        {
            OpenSession();
            for (int i = 0; i < seconds; i++) ActiveSecond();
            SellTray(false);
        }

        void Away(double seconds)
        {
            s.LastSeenUnixSeconds = 0;
            earnedAway += Simulation.ClaimAway(defs, s, (long)seconds, out _, mods);
            clock += seconds;
            Note();
        }

        static string Clock(double seconds)
        {
            if (seconds < 0) return "chưa đạt";
            int total = (int)seconds;
            if (total >= 86400) return "ngày " + (total / 86400 + 1) + " (" + (total / 3600) + " giờ " + (total % 3600 / 60) + " phút)";
            return total >= 3600 ? (total / 3600) + " giờ " + (total % 3600 / 60) + " phút" : (total / 60) + " phút " + (total % 60) + " giây";
        }

        BigNum TotalEarned => earnedTaps + earnedIdle + earnedBoost + earnedAway + earnedQuests + earnedTray + earnedGames + earnedOrders + earnedJars;

        double Share(BigNum part)
        {
            BigNum total = TotalEarned;
            return total.IsZero ? 0d : Math.Round((part / total).ToDouble() * 1000d) / 10d;
        }

        int GoldenEarned => goldenGift + goldenPhoto + goldenQuests + goldenGames + goldenTray + GoldenFromLevels;
        int GoldenFromLevels => s.GoldenCarrots + goldenSpent - goldenGift - goldenPhoto - goldenQuests - goldenGames - goldenTray;
        int CloversFromLevels => s.Clovers + summoned - cloversQuests - cloversGames - cloversOrders;

        /// <summary>One row of the table: where the garden stands and where its carrots came from, so far.</summary>
        void Snapshot(string label)
        {
            int open = 0;
            for (int k = 0; k < defs.Count; k++) if (s.Levels[k] > 0) open++;
            snapshots.Add("| " + label + " | " + Income.ToShortString() + " | " + s.Coins.ToShortString() + " | " + Garden.Level(s) + " (" + (int)s.GardenXp + ") | " + s.Plots + " | " + open + " | " + s.Rabbits.Count
                + " | " + Share(earnedTaps) + " | " + Share(earnedIdle) + " | " + Share(earnedBoost) + " | " + Share(earnedAway) + " | " + Share(earnedQuests) + " | " + Share(earnedTray)
                + " | " + Share(earnedGames) + " | " + Share(earnedOrders) + " | " + Share(earnedJars) + " | " + GoldenEarned + " / " + goldenSpent + " |");
        }

        const string SnapshotHeader = "| Lúc | Thu/giây | Cà rốt | Cấp vườn (điểm) | Ô đất | Trạm | Thỏ | Chạm % | Trạm tự chạy % | Trà lực % | Vắng mặt % | Nhiệm vụ % | Khay % | Trò chơi % | Đơn khách % | Hũ trăng % | Vàng kiếm / tiêu |\n|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|";

        string Milestones()
        {
            var sb = new StringBuilder();
            sb.AppendLine("| Mốc | Lúc đạt |");
            sb.AppendLine("|---|---|");
            foreach (int level in new[] { 3, 5, 10, 15 })
                sb.AppendLine("| Cấp vườn " + level + " | " + (levelAt.ContainsKey(level) ? Clock(levelAt[level]) : "chưa đạt") + " |");
            foreach (int plots in new[] { 6, 8, 10, 12 })
                sb.AppendLine("| " + plots + " ô đất | " + (plotsAt.ContainsKey(plots) ? Clock(plotsAt[plots]) : "chưa đạt") + " |");
            sb.AppendLine("| Thỏ Tím đầu tiên | " + Clock(purpleAt) + " |");
            sb.AppendLine("| Thỏ Vàng đầu tiên | " + Clock(goldAt) + " |");
            for (int w = 0; w < 4; w++)
                sb.AppendLine("| Đợt đất " + (w + 1) + " | đủ cấp " + Clock(waveLevelAt[w]) + ", đủ thỏ " + Clock(waveRabbitsAt[w]) + ", mở " + Clock(waveTakenAt[w]) + " |");
            return sb.ToString();
        }

        string Extras()
        {
            return "Cỏ: " + cloversQuests + " từ nhiệm vụ, " + CloversFromLevels + " từ cấp vườn, " + cloversGames + " từ trò chơi, " + cloversOrders + " từ đơn khách; gọi " + summoned + " thỏ.\n"
                + "Cà rốt vàng: " + goldenGift + " quà ngày, " + goldenPhoto + " ảnh, " + GoldenFromLevels + " cấp vườn, " + goldenQuests + " nhiệm vụ, " + goldenGames + " trò chơi, " + goldenTray + " khay; tiêu " + goldenSpent
                + " (" + goldenCalls + " lượt gọi vàng, " + visitorsBought + " thỏ ghé tiệm); còn " + s.GoldenCarrots + ".\n"
                + "Hũ trăng (#64): " + FreeJars() + " hũ miễn phí (nhiều nhất " + MostFreeJarsInADay() + " trong một ngày), mở " + jarsOpened + " hũ ngay khi ví cho mở, không mua hũ; còn " + s.MoonJars + ".\n"
                + "Khác: " + gamesPlayed + " ván chơi, " + ordersDelivered + " đơn giao, " + rabbitLevels + " cấp thỏ mua, " + boosts + " lần trà lực, " + s.Claimed.Count + " nhiệm vụ đang nhận.\n";
        }

        [Test]
        public void Casual_Week_And_Tireless_Two_Hours_Keep_The_Intended_Pace()
        {
            var report = new StringBuilder();
            report.AppendLine("# Nhịp kinh tế với đủ mọi nguồn thu (EconomySimulationTests)");
            report.AppendLine();

            // ----- the casual player: a week of 20 minutes in the morning and 10 in the evening -----
            Reset(false);
            report.AppendLine("## Người chơi thong thả");
            report.AppendLine("1 chạm/giây vào trạm thu nhiều nhất, 3 giây mua một thứ rẻ nhất, 30 giây xem nhiệm vụ, 4 phút chơi một ván (2 sao), bán khay khi đầy hoặc mỗi 2 phút, giao đơn khi khay có đủ, gọi thỏ ngay khi có cỏ, mua thỏ ghé tiệm khi đủ vàng rồi gọi vàng mỗi 10, bật trà lực khi đủ, nâng cấp thỏ khi giá dưới 10 giây thu nhập.");
            report.AppendLine("Mỗi ngày: chơi 20 phút, vắng 8 giờ, chơi 10 phút, vắng tới hết ngày; 7 ngày.");
            report.AppendLine();
            double level5 = -1, level10 = -1;
            int levelDayOne = 0;
            BigNum quests1 = BigNum.Zero, games1 = BigNum.Zero, orders1 = BigNum.Zero, active1 = BigNum.Zero;
            int goldenEarnedDayOne = 0;
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
                    levelDayOne = Garden.Level(s);
                    // Moon jars (#64) are an hour of income at a stretch, like time away, so they are left out of what play earned too.
                    quests1 = earnedQuests; games1 = earnedGames; orders1 = earnedOrders; active1 = TotalEarned - earnedAway - earnedJars;
                    goldenEarnedDayOne = GoldenEarned;
                }
                if (day == 2) Snapshot("hết ngày 2");
                if (day == 3) Snapshot("hết ngày 3");
                if (day == 7) Snapshot("hết ngày 7");
            }
            level5 = levelAt.ContainsKey(5) ? levelAt[5] : -1;
            level10 = levelAt.ContainsKey(10) ? levelAt[10] : -1;
            double plots12Casual = plotsAt.ContainsKey(12) ? plotsAt[12] : -1;
            double[] waitCasual = new double[3];
            for (int w = 0; w < 3; w++) waitCasual[w] = waveRabbitsAt[w] < 0 ? double.PositiveInfinity : waveRabbitsAt[w] - waveLevelAt[w];
            // Time away pays for hours at a stretch and swamps a casual day, so what the player does is measured against what play earned.
            double questShare1 = active1.IsZero ? 0 : (quests1 / active1).ToDouble() * 100d;
            double playShare1 = active1.IsZero ? 0 : ((games1 + orders1) / active1).ToDouble() * 100d;
            int goldenCallsCasual = goldenCalls, visitorsCasual = visitorsBought;
            int mostJarsCasual = MostFreeJarsInADay(), jarsOpenedCasual = jarsOpened;
            report.AppendLine(Milestones());
            report.AppendLine(SnapshotHeader);
            foreach (string row in snapshots) report.AppendLine(row);
            report.AppendLine();
            report.AppendLine("Ngày 1, tính trên cà rốt kiếm được lúc đang chơi (không tính vắng mặt và hũ trăng): nhiệm vụ " + Math.Round(questShare1, 1) + " %, trò chơi + đơn khách " + Math.Round(playShare1, 1) + " %; cà rốt vàng kiếm được " + goldenEarnedDayOne + ".");
            report.AppendLine(Extras());
            report.AppendLine("Diễn biến:");
            report.Append(trace);
            report.AppendLine();

            // ----- the tireless player: two hours without a pause -----
            Reset(true);
            report.AppendLine("## Người chơi không nghỉ tay");
            report.AppendLine("3 chạm/giây, 4 lần mua mỗi giây, nhận nhiệm vụ ngay, chơi ván 3 sao ngay khi có lượt, còn lại như trên. Chơi liền 2 giờ.");
            report.AppendLine();
            Play(2 * 3600);
            Snapshot("2 giờ");
            double plots12Tireless = plotsAt.ContainsKey(12) ? plotsAt[12] : -1;
            int mostJarsTireless = MostFreeJarsInADay();
            report.AppendLine(Milestones());
            report.AppendLine(SnapshotHeader);
            foreach (string row in snapshots) report.AppendLine(row);
            report.AppendLine();
            report.AppendLine(Extras());
            report.AppendLine("Diễn biến:");
            report.Append(trace);
            report.AppendLine();
            // Not asserted: a player who buys four levels a second earns a point for each, so the level that opens the last
            // plots comes in minutes however the carrots are priced. Whether that experience should be capped is the owner's call.
            report.AppendLine("Người chơi không nghỉ tay mở đủ 12 ô đất lúc " + Clock(plots12Tireless) + " (mục tiêu: không trước ~2 giờ; chưa đạt vì mỗi cấp trạm mua đều cho 1 điểm, 4 lần mua mỗi giây).");

            System.IO.Directory.CreateDirectory("Logs");
            System.IO.File.WriteAllText("Logs/economy-pacing.txt", report.ToString());
            TestContext.WriteLine(report.ToString());

            // The pace the owner asked for (#6), on the casual player.
            Assert.GreaterOrEqual(level5, 5 * 60, "garden level 5 should not come in the first five minutes\n" + report);
            Assert.LessOrEqual(level5, 12 * 60, "garden level 5 should come within twelve minutes\n" + report);
            Assert.GreaterOrEqual(levelDayOne, 10, "garden level 10 should come within day one\n" + report);
            Assert.GreaterOrEqual(level10, 2 * 3600, "garden level 10 should not come in the first sitting\n" + report);
            Assert.IsTrue(plots12Casual < 0 || plots12Casual >= 2 * Day, "all twelve plots should wait for day three\n" + report);
            for (int w = 0; w < 3; w++) Assert.LessOrEqual(waitCasual[w], 300, "wave " + (w + 1) + " waited on rabbits\n" + report);
            Assert.Less(questShare1, 35, "quests should not dominate day one\n" + report);
            Assert.GreaterOrEqual(playShare1, 15, "the games and the guests should matter on day one\n" + report);
            Assert.LessOrEqual(playShare1, 30, "but not take over\n" + report);
            Assert.GreaterOrEqual(goldenCallsCasual + visitorsCasual, 2, "a casual player should call with golden carrots a few times a week\n" + report);
            Assert.LessOrEqual(goldenCallsCasual + visitorsCasual, 6, "but not every day\n" + report);
            // Moon jars (#64): the free sources together bring at most one a day, to the casual player and the tireless one.
            Assert.Greater(jarsOpenedCasual, 0, "the casual player gets and opens jars\n" + report);
            Assert.LessOrEqual(mostJarsCasual, MoonJars.FreePerDay, "at most one free jar a day\n" + report);
            Assert.LessOrEqual(mostJarsTireless, MoonJars.FreePerDay, "the tireless player too\n" + report);
            Assert.Pass(report.ToString());
        }
    }
}
