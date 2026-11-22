using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using UnityEditor;
using VuonTra.Game;

namespace VuonTra.Core.Tests
{
    /// <summary>
    /// Plays the shipped game with a simple, steady player and checks the pace the design promises:
    /// garden level 5 in about the first ten minutes, level 10 within the first day, and no widening of
    /// the garden held up for want of rabbits. The numbers it finds are written to Logs/garden-pacing.txt.
    /// </summary>
    public class GardenPacingTests
    {
        List<StationDef> defs;
        List<UpgradeDef> ups;
        List<QuestDef> quests;
        GameState s;
        Modifiers mods;
        System.Random rng;
        double clock; // seconds of the player's day
        readonly Dictionary<int, double> levelAt = new Dictionary<int, double>();
        readonly double[] waveLevelAt = new double[4], waveRabbitsAt = new double[4], waveTakenAt = new double[4];
        int summoned;
        /// <summary>How the simulated player plays: taps a second, seconds between purchases, seconds between looking at quests.</summary>
        int tapsPerSecond = 1, buyEvery = 3, buysAtOnce = 1, questEvery = 30;
        /// <summary>
        /// Moon jars (#64): the player opens every jar the moment the purse shows them (garden level 3), the plainest rule.
        /// With <see cref="OldRules"/> the game is played as before jars: the harvest quest pays its clover and no jar is opened.
        /// </summary>
        bool openJars = true;
        long today = 1;
        int jarsHeld, jarsOpened;
        BigNum earnedJars;
        readonly Dictionary<long, int> freeJars = new Dictionary<long, int>();
        /// <summary>
        /// The Frog Rabbit (#62), modelled at its most: every sitting that opens while the day still has a frog opens in
        /// the rain, and the player greets it at once, so the casual player meets both of each day's frogs. Its clover
        /// roll has its own dice, so the rest of the week plays out the same with frogs or without.
        /// </summary>
        bool meetFrogs = true;
        int frogsMet, frogClovers;
        BigNum earnedFrogs;
        System.Random frogRng;
        /// <summary>
        /// The moon pond (#58): every sitting opens with every bait that came back since the last one cast, a fixed share
        /// of them caught (<see cref="FishCatchRate"/>, the rest tapped too late), every fish fed to the rabbit at the
        /// station that earns most, which is also stroked once a day. The casts have their own dice, so the rest of the
        /// week plays out the same with fishing or without.
        /// </summary>
        bool goFishing = true;
        const double FishCatchRate = 0.8;
        int casts, fishCaught, fishPoints;
        System.Random fishRng;

        [SetUp]
        public void SetUp()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<GameCatalog>("Assets/VuonTra/Content/GameCatalog.asset");
            Assert.IsNotNull(catalog, "Run VuonTra > Seed Content first.");
            defs = catalog.BuildDefs();
            ups = catalog.BuildUpgrades();
            quests = catalog.BuildQuests();
            s = GameState.NewGame(defs, ups.Count);
            mods = Modifiers.From(ups, s);
            rng = new System.Random(20261122);
            clock = 0;
            tapsPerSecond = 1; buyEvery = 3; buysAtOnce = 1; questEvery = 30; // NUnit keeps one instance: a tireless test before must not set the pace of the next
            levelAt.Clear();
            summoned = 0;
            for (int i = 0; i < 4; i++) waveLevelAt[i] = waveRabbitsAt[i] = waveTakenAt[i] = -1;
            trace.Clear();
            openJars = true;
            today = 1;
            jarsHeld = jarsOpened = 0;
            earnedJars = BigNum.Zero;
            freeJars.Clear();
            meetFrogs = true;
            frogsMet = frogClovers = 0;
            earnedFrogs = BigNum.Zero;
            frogRng = new System.Random(62);
            goFishing = true;
            casts = fishCaught = fishPoints = 0;
            fishRng = new System.Random(58);
            Quests.RollDaily(quests, defs, s, 1, mods);
        }

        /// <summary>The game as it was before moon jars (#64): the harvest quest's clover, and jars never opened.</summary>
        void OldRules()
        {
            openJars = false;
            foreach (QuestDef quest in quests)
                if (quest.RewardJars > 0) { quest.RewardClovers += quest.RewardJars; quest.RewardJars = 0; }
        }

        /// <summary>Counts the jars that came since the last look (all free here: this player buys none) and opens them while the purse shows them.</summary>
        void Jars()
        {
            if (s.MoonJars > jarsHeld)
            {
                freeJars.TryGetValue(today, out int n);
                freeJars[today] = n + s.MoonJars - jarsHeld;
            }
            while (openJars && MoonJars.Shown(s) && s.MoonJars > 0)
            {
                earnedJars += MoonJars.Open(defs, s, mods);
                jarsOpened++;
            }
            jarsHeld = s.MoonJars;
        }

        /// <summary>A sitting opens in the next shower of the day that still has its frog, which the player greets at once (#62).</summary>
        void MeetFrog()
        {
            if (!meetFrogs) return;
            Tutorial.Advance(s); // as the game does on every change: the frog waits for the first minute's guidance
            double[] showers = Weather.ShowerStarts(today);
            for (int k = 0; k < showers.Length; k++)
            {
                if ((s.FrogShowers & (1 << k)) != 0 && s.FrogDay == today) continue;
                double hour = showers[k] + 1d / 60d;
                if (FrogRabbit.Advance(s, 1d, today, hour) != FrogEvent.Arrived) return;
                if (FrogRabbit.Greet(defs, s, mods, today, hour, frogRng.NextDouble(), out BigNum paid, out int clovers))
                {
                    frogsMet++;
                    frogClovers += clovers;
                    earnedFrogs += paid;
                }
                return;
            }
        }

        /// <summary>The open brew station that earns most: where the player sends the fish.</summary>
        int BusiestStation()
        {
            int best = -1;
            BigNum bestIncome = BigNum.Zero;
            for (int i = 0; i < defs.Count; i++)
            {
                if (defs[i].Kind != StationKind.Brew || s.Levels[i] <= 0) continue;
                BigNum income = Economy.StationIncomePerSecond(defs, s, i, out _, out _);
                if (best < 0 || income > bestIncome) { best = i; bestIncome = income; }
            }
            return best;
        }

        /// <summary>A sitting opens at the pond (#58): every bait cast, most caught, every fish fed to the busiest station's rabbit, stroked once a day.</summary>
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
            Rabbit r = Rabbits.AtStation(s, BusiestStation());
            if (r == null) return;
            Rabbits.Pet(s, r, today);
            for (int k = Fishing.Kinds - 1; k >= 0; k--)
                while (s.Fish[k] > 0 && Fishing.Feed(s, r, k).Fed) fishPoints += Fishing.Points[k];
        }

        int MostHearts()
        {
            int most = 0;
            foreach (Rabbit r in s.Rabbits) most = System.Math.Max(most, Rabbits.HeartsOf(r));
            return most;
        }

        string FishLine() => "Câu cá (#58): thả " + casts + " lần, được " + fishCaught + " con (" + (int)(FishCatchRate * 100) + " % chạm đúng lúc), cho thỏ ở trạm thu nhiều nhất ăn " + fishPoints
            + " điểm, vuốt nó mỗi ngày; tim cao nhất " + MostHearts() + " (×" + Rabbits.HeartMultipliers[MostHearts()] + ")";

        string FrogLine()
        {
            double total = s.TotalEarned.ToDouble() + earnedJars.ToDouble() + earnedFrogs.ToDouble();
            return "Thỏ Ếch (#62): gặp " + frogsMet + " lần (mọi buổi chơi mở đầu trong mưa khi ngày còn ếch), " + earnedFrogs.ToShortString() + " cà rốt ("
                + (total > 0 ? System.Math.Round(earnedFrogs.ToDouble() / total * 1000d) / 10d : 0d) + " % so với cà rốt kiếm được), " + frogClovers + " cỏ";
        }

        int MostFreeJarsInADay()
        {
            int most = 0;
            foreach (int n in freeJars.Values) if (n > most) most = n;
            return most;
        }

        string JarLine()
        {
            int got = 0;
            foreach (int n in freeJars.Values) got += n;
            double total = s.TotalEarned.ToDouble() + earnedJars.ToDouble();
            return "Hũ trăng (#64): nhận " + got + " hũ miễn phí (nhiều nhất " + MostFreeJarsInADay() + " trong một ngày), mở " + jarsOpened + " hũ = "
                + earnedJars.ToShortString() + " cà rốt (" + (total > 0 ? System.Math.Round(earnedJars.ToDouble() / total * 1000d) / 10d : 0d) + " % so với cà rốt kiếm được), còn " + s.MoonJars + " hũ";
        }

        /// <summary>One second of an attentive player: a few taps, then spend on the cheapest useful thing.</summary>
        void ActiveSecond()
        {
            for (int tap = 0; tap < tapsPerSecond; tap++) Simulation.Tap(defs, s, 0, mods);
            Simulation.Tick(defs, s, 1d, null, mods);
            clock += 1d;

            for (int buys = 0; buys < buysAtOnce && (int)clock % buyEvery == 0; buys++)
            {
                if (Expansion.TryExpand(defs, s))
                {
                    waveTakenAt[Expansion.WavesTaken(s.Plots) - 1] = clock;
                    mods = Modifiers.From(ups, s); // one more place to rest (#60)
                    continue;
                }
                // The cheapest thing on offer: a level, the next station, or a charm.
                int best = -1;
                bool bestIsUpgrade = false;
                BigNum bestCost = BigNum.Zero;
                s.BuyMode = 1;
                for (int i = 0; i < defs.Count; i++)
                {
                    if (s.Levels[i] == 0 && !Economy.CanUnlock(s, i)) continue;
                    BigNum cost = Economy.CostOfNext(defs[i], s.Levels[i], 1);
                    if (best < 0 || cost < bestCost) { best = i; bestCost = cost; bestIsUpgrade = false; }
                }
                for (int u = 0; u < ups.Count; u++)
                {
                    if (Upgrades.IsMaxed(ups[u], s.UpgradeLevels[u])) continue;
                    BigNum cost = Upgrades.CostOfNext(ups[u], s.UpgradeLevels[u]);
                    if (best < 0 || cost < bestCost) { best = u; bestCost = cost; bestIsUpgrade = true; }
                }
                if (best < 0 || s.Coins < bestCost) break;
                if (bestIsUpgrade) { Upgrades.TryBuy(ups, s, best); mods = Modifiers.From(ups, s); }
                else Simulation.TryBuy(defs, s, best);
            }

            if ((int)clock % questEvery == 0)
                foreach (QuestDef quest in quests) Quests.TryClaim(quest, defs, s, mods, out _);
            Jars();
            // Every clover calls a rabbit, and nobody is ever merged away: the plainest way to play.
            bool called = false;
            while (s.Clovers >= Rabbits.SummonCost)
            {
                Rabbits.Summon(s, rng.NextDouble(), "R", rng.NextDouble());
                summoned++;
                Rabbits.AutoAssign(defs, s);
                called = true;
            }
            // As in the game, who works and who rests feeds the modifiers (#60).
            if (called) mods = Modifiers.From(ups, s);
            Note();
        }

        void Note()
        {
            int level = Garden.Level(s);
            for (int l = 2; l <= level; l++)
                if (!levelAt.ContainsKey(l)) levelAt[l] = clock;
            for (int w = 0; w < Expansion.Waves.Length; w++)
            {
                ExpansionWave wave = Expansion.Waves[w];
                if (waveLevelAt[w] < 0 && level >= wave.NeedLevel) waveLevelAt[w] = clock;
                if (waveRabbitsAt[w] < 0 && s.Rabbits.Count >= wave.NeedRabbits && s.SeenKinds.Count >= wave.NeedKinds) waveRabbitsAt[w] = clock;
            }
        }

        readonly StringBuilder trace = new StringBuilder();

        void Play(double seconds)
        {
            MeetFrog();
            GoFishing();
            for (int i = 0; i < seconds; i++)
            {
                ActiveSecond();
                int t = (int)clock;
                if (t == 10 || t == 20 || t == 30 || t == 60 || t == 120 || t == 300 || t == 600 || t == 1200)
                {
                    int open = 0, levels = 0;
                    for (int k = 0; k < defs.Count; k++) { if (s.Levels[k] > 0) open++; levels += s.Levels[k]; }
                    trace.AppendLine("  t=" + t + "s  cà rốt " + s.Coins.ToShortString() + "  thu/giây " + Economy.IncomePerSecond(defs, s).ToShortString()
                        + "  trạm mở " + open + "  tổng cấp trạm " + levels + "  xp " + (int)s.GardenXp + "  thỏ " + s.Rabbits.Count + "  nhiệm vụ đã nhận " + s.Claimed.Count + "  cỏ đã dùng " + summoned);
                }
            }
        }

        void Away(double seconds)
        {
            s.LastSeenUnixSeconds = 0;
            Simulation.ClaimAway(defs, s, (long)seconds, out _, mods);
            clock += seconds;
        }

        static string Clock(double seconds)
        {
            if (seconds < 0) return "chưa đạt";
            int total = (int)seconds;
            return total >= 3600 ? (total / 3600) + " giờ " + (total % 3600 / 60) + " phút" : (total / 60) + " phút " + (total % 60) + " giây";
        }

        /// <summary>A garden on its second season (#1) earns a quarter more, so the same casual player climbs faster, and still climbs.</summary>
        [Test]
        public void The_Second_Season_Climbs_Faster_Than_The_First_And_Still_Passes_The_Marks()
        {
            Play(10 * 60);
            double firstXp = s.GardenXp;
            double firstCoins = s.TotalEarned.ToDouble();
            int firstLevel = Garden.Level(s);

            SetUp();
            s.Season = 1;
            mods = Modifiers.From(ups, s);
            Assert.AreEqual(1.25d, mods.IncomeMultiplier, 1e-9);
            Play(10 * 60);

            Assert.Greater(s.TotalEarned.ToDouble(), firstCoins, "a quarter more on every carrot");
            Assert.GreaterOrEqual(s.GardenXp, firstXp, "more carrots buy more levels");
            Assert.GreaterOrEqual(Garden.Level(s), firstLevel);
            Assert.GreaterOrEqual(Garden.Level(s), 5, "the ten-minute mark still holds on the second season");
        }

        /// <summary>Not a pass or fail: writes how a tireless, perfect player fares, to show the fastest the game can go.</summary>
        [Test]
        public void Report_For_A_Tireless_Player()
        {
            tapsPerSecond = 3; buyEvery = 1; buysAtOnce = 4; questEvery = 1;
            Play(20 * 60);
            int at20 = Garden.Level(s);
            double xp20 = s.GardenXp;
            for (int visit = 0; visit < 4; visit++) { Away(2 * 3600); Play(5 * 60); }
            var report = new StringBuilder();
            report.AppendLine("Người chơi không nghỉ tay (3 chạm/giây, 4 lần mua mỗi giây, nhận nhiệm vụ ngay)");
            report.AppendLine(FrogLine());
            report.AppendLine(FishLine());
            report.AppendLine("  sau 20 phút: xp " + (int)xp20 + " (cấp " + at20 + "); cuối ngày 1: xp " + (int)s.GardenXp + " (cấp " + Garden.Level(s) + "), " + s.Rabbits.Count + " thỏ, " + s.Plots + " ô đất");
            foreach (int level in new[] { 3, 5, 6, 10, 15 })
                report.AppendLine("  cấp vườn " + level + ": " + (levelAt.ContainsKey(level) ? Clock(levelAt[level]) : "chưa đạt"));
            report.AppendLine("Diễn biến:");
            report.Append(trace);
            System.IO.Directory.CreateDirectory("Logs");
            System.IO.File.WriteAllText("Logs/garden-pacing-tireless.txt", report.ToString());
            Assert.Pass(report.ToString());
        }

        /// <summary>
        /// The casual player's week: on the first day twenty minutes at the start, then four visits of five minutes, two
        /// hours apart; on days two to seven three visits of five minutes each, dailies renewed each morning.
        /// </summary>
        void CasualWeek(System.Action endOfDayOne = null)
        {
            Play(20 * 60);
            for (int visit = 0; visit < 4; visit++)
            {
                Away(2 * 3600);
                Play(5 * 60);
            }
            endOfDayOne?.Invoke();
            for (int day = 2; day <= 7; day++)
            {
                Away(8 * 3600);
                today = day;
                Quests.RollDaily(quests, defs, s, day, mods);
                for (int visit = 0; visit < 3; visit++)
                {
                    Play(5 * 60);
                    Away(2 * 3600);
                }
            }
        }

        [Test]
        public void A_Casual_Player_Reaches_Level_Five_In_About_Ten_Minutes_And_Ten_In_A_Day_Without_Waiting_On_Rabbits()
        {
            int levelDayOne = 0, rabbitsDayOne = 0, kindsDayOne = 0, plotsDayOne = 0;
            double activeDayOne = (20 + 4 * 5) * 60;
            List<AlbumSet> setsDayOne = null;
            CasualWeek(() =>
            {
                levelDayOne = Garden.Level(s);
                rabbitsDayOne = s.Rabbits.Count; kindsDayOne = s.SeenKinds.Count; plotsDayOne = s.Plots;
                setsDayOne = AlbumSets.Completed(s); // the album's sets (#61)
            });

            var report = new StringBuilder();
            report.AppendLine("Người chơi thong thả (1 chạm/giây, 3 giây mua một lần thứ rẻ nhất, 30 giây xem nhiệm vụ một lần, không ghép thỏ)");
            report.AppendLine("Ngày 1: chơi 20 phút, rồi 4 lần ghé 5 phút cách nhau 2 giờ (tổng " + (int)(activeDayOne / 60) + " phút chơi)");
            foreach (int level in new[] { 2, 3, 5, 6, 10, 15 })
                report.AppendLine("  cấp vườn " + level + ": " + (levelAt.ContainsKey(level) ? Clock(levelAt[level]) : "chưa đạt trong 7 ngày"));
            report.AppendLine("Cuối ngày 1: cấp " + levelDayOne + ", " + rabbitsDayOne + " thỏ, " + kindsDayOne + " loài đã gặp, " + plotsDayOne + " ô đất");
            report.AppendLine("Cuối ngày 7: cấp " + Garden.Level(s) + ", " + s.Rabbits.Count + " thỏ, " + s.SeenKinds.Count + " loài đã gặp, " + s.Plots + " ô đất, đã gọi " + summoned + " lần");
            report.AppendLine("Sổ thỏ: cuối ngày 1 đủ " + setsDayOne.Count + " bộ (" + string.Join(", ", setsDayOne.ConvertAll(x => x.Key)) + "), cuối ngày 7 đủ " + AlbumSets.Completed(s).Count + " bộ (" + string.Join(", ", AlbumSets.Completed(s).ConvertAll(x => x.Key)) + ")");
            for (int w = 0; w < 4; w++)
                report.AppendLine("  đợt " + (w + 1) + ": đủ cấp lúc " + Clock(waveLevelAt[w]) + ", đủ thỏ lúc " + Clock(waveRabbitsAt[w]) + ", mở lúc " + Clock(waveTakenAt[w]));
            report.AppendLine(JarLine() + "; mở ngay khi ví cho mở (cấp vườn 3)");
            report.AppendLine(FrogLine());
            report.AppendLine(FishLine());
            report.AppendLine("Diễn biến:");
            report.Append(trace);
            System.IO.Directory.CreateDirectory("Logs");
            System.IO.File.WriteAllText("Logs/garden-pacing.txt", report.ToString());
            TestContext.WriteLine(report.ToString());

            Assert.IsTrue(levelAt.ContainsKey(5), "level 5 was never reached");
            Assert.LessOrEqual(levelAt[5], 12 * 60, "level 5 should come in about the first ten minutes");
            Assert.GreaterOrEqual(levelAt[5], 5 * 60, "but not in the first moments either");
            Assert.GreaterOrEqual(levelDayOne, 10, "level 10 should come within the first day");
            Assert.LessOrEqual(setsDayOne.Count, 2, "the album's sets come slowly: at most one or two on the first day (#61)");
            for (int w = 0; w < 3; w++)
            {
                Assert.GreaterOrEqual(waveLevelAt[w], 0, "wave " + (w + 1) + " level was never reached");
                Assert.GreaterOrEqual(waveRabbitsAt[w], 0, "wave " + (w + 1) + " never had enough rabbits");
                // Rabbits must be ready by the time the level is; a short lag while a quest is claimed is fine.
                Assert.LessOrEqual(waveRabbitsAt[w] - waveLevelAt[w], 300, "wave " + (w + 1) + " waited on rabbits");
            }
        }

        /// <summary>
        /// Moon jars (#64): the casual week played twice, as before jars and with them opened the moment the purse shows
        /// them. The free sources bring at most one a day, and level 10 comes no more than a fifth sooner.
        /// </summary>
        [Test]
        public void Moon_Jars_Come_Free_At_Most_One_A_Day_And_Bring_Level_Ten_No_More_Than_A_Fifth_Sooner()
        {
            OldRules();
            CasualWeek();
            double tenBefore = levelAt.ContainsKey(10) ? levelAt[10] : -1;
            double fiveBefore = levelAt.ContainsKey(5) ? levelAt[5] : -1;
            int summonedBefore = summoned;
            Assert.AreEqual(0, jarsOpened);

            SetUp();
            CasualWeek();
            double tenAfter = levelAt.ContainsKey(10) ? levelAt[10] : -1;
            double fiveAfter = levelAt.ContainsKey(5) ? levelAt[5] : -1;
            string report = "Trước hũ: cấp 5 lúc " + Clock(fiveBefore) + ", cấp 10 lúc " + Clock(tenBefore) + ", gọi " + summonedBefore + " thỏ.\n"
                + "Có hũ: cấp 5 lúc " + Clock(fiveAfter) + ", cấp 10 lúc " + Clock(tenAfter) + ", gọi " + summoned + " thỏ.\n" + JarLine();
            TestContext.WriteLine(report);
            Assert.GreaterOrEqual(tenBefore, 0, report);
            Assert.GreaterOrEqual(tenAfter, 0, report);
            Assert.GreaterOrEqual(tenAfter, 0.8 * tenBefore, "level 10 no more than a fifth sooner\n" + report);
            Assert.Greater(jarsOpened, 0, "the casual player does get jars\n" + report);
            Assert.LessOrEqual(MostFreeJarsInADay(), MoonJars.FreePerDay, "at most one free jar a day\n" + report);

            // The tireless player's day, too.
            SetUp();
            tapsPerSecond = 3; buyEvery = 1; buysAtOnce = 4; questEvery = 1;
            Play(20 * 60);
            for (int visit = 0; visit < 4; visit++) { Away(2 * 3600); Play(5 * 60); }
            Assert.LessOrEqual(MostFreeJarsInADay(), MoonJars.FreePerDay, "the tireless player too\n" + JarLine());
        }

        /// <summary>
        /// The Frog Rabbit (#62): the casual week played without it and with both of each day's frogs greeted. The frogs
        /// bring level 10 no more than a fifth sooner, the ten-minute mark still holds, and nobody meets more than two a day.
        /// </summary>
        [Test]
        public void Frog_Rabbits_Greeted_Twice_A_Day_Bring_Level_Ten_No_More_Than_A_Fifth_Sooner()
        {
            meetFrogs = false;
            CasualWeek();
            double tenBefore = levelAt.ContainsKey(10) ? levelAt[10] : -1;
            double fiveBefore = levelAt.ContainsKey(5) ? levelAt[5] : -1;
            int summonedBefore = summoned;
            Assert.AreEqual(0, frogsMet);

            SetUp();
            CasualWeek();
            double tenAfter = levelAt.ContainsKey(10) ? levelAt[10] : -1;
            double fiveAfter = levelAt.ContainsKey(5) ? levelAt[5] : -1;
            string report = "Không Thỏ Ếch: cấp 5 lúc " + Clock(fiveBefore) + ", cấp 10 lúc " + Clock(tenBefore) + ", gọi " + summonedBefore + " thỏ.\n"
                + "Có Thỏ Ếch: cấp 5 lúc " + Clock(fiveAfter) + ", cấp 10 lúc " + Clock(tenAfter) + ", gọi " + summoned + " thỏ.\n" + FrogLine();
            TestContext.WriteLine(report);
            Assert.GreaterOrEqual(tenBefore, 0, report);
            Assert.GreaterOrEqual(tenAfter, 0, report);
            Assert.GreaterOrEqual(tenAfter, 0.8 * tenBefore, "level 10 no more than a fifth sooner\n" + report);
            Assert.GreaterOrEqual(fiveAfter, 5 * 60, "level 5 still not in the first five minutes\n" + report);
            Assert.Greater(frogsMet, 0, "the casual player does meet frogs\n" + report);
            Assert.LessOrEqual(frogsMet, 7 * FrogRabbit.PerDay, "two a day at most\n" + report);
        }

        /// <summary>
        /// The moon pond and the hearts it feeds (#58): the casual week played without fishing and with every bait cast
        /// each sitting, four in five caught and every fish fed to the busiest station's rabbit. The hearts bring level 10
        /// no more than 30 % sooner (the issue's rule), the ten-minute mark still holds, and the tireless player's day is
        /// written beside it.
        /// </summary>
        [Test]
        public void Fishing_And_Feeding_Every_Sitting_Bring_Level_Ten_No_More_Than_Thirty_Percent_Sooner()
        {
            goFishing = false;
            CasualWeek();
            double tenBefore = levelAt.ContainsKey(10) ? levelAt[10] : -1;
            double fiveBefore = levelAt.ContainsKey(5) ? levelAt[5] : -1;
            double fifteenBefore = levelAt.ContainsKey(15) ? levelAt[15] : -1;
            int endBefore = Garden.Level(s);
            Assert.AreEqual(0, casts);

            SetUp();
            CasualWeek();
            double tenAfter = levelAt.ContainsKey(10) ? levelAt[10] : -1;
            double fiveAfter = levelAt.ContainsKey(5) ? levelAt[5] : -1;
            double fifteenAfter = levelAt.ContainsKey(15) ? levelAt[15] : -1;
            string report = "Không câu cá: cấp 5 lúc " + Clock(fiveBefore) + ", cấp 10 lúc " + Clock(tenBefore) + ", cấp 15 lúc " + Clock(fifteenBefore) + ", hết ngày 7 cấp " + endBefore + ".\n"
                + "Câu cá mỗi buổi: cấp 5 lúc " + Clock(fiveAfter) + ", cấp 10 lúc " + Clock(tenAfter) + ", cấp 15 lúc " + Clock(fifteenAfter) + ", hết ngày 7 cấp " + Garden.Level(s) + ".\n" + FishLine();

            SetUp();
            tapsPerSecond = 3; buyEvery = 1; buysAtOnce = 4; questEvery = 1;
            goFishing = false;
            Play(20 * 60);
            for (int visit = 0; visit < 4; visit++) { Away(2 * 3600); Play(5 * 60); }
            double tirelessTenBefore = levelAt.ContainsKey(10) ? levelAt[10] : -1;
            SetUp();
            tapsPerSecond = 3; buyEvery = 1; buysAtOnce = 4; questEvery = 1;
            Play(20 * 60);
            for (int visit = 0; visit < 4; visit++) { Away(2 * 3600); Play(5 * 60); }
            double tirelessTenAfter = levelAt.ContainsKey(10) ? levelAt[10] : -1;
            report += "\nNgười chơi không nghỉ tay: cấp 10 lúc " + Clock(tirelessTenBefore) + " không câu, " + Clock(tirelessTenAfter) + " có câu.";
            System.IO.Directory.CreateDirectory("Logs");
            System.IO.File.WriteAllText("Logs/garden-pacing-fishing.txt", report);
            TestContext.WriteLine(report);

            Assert.GreaterOrEqual(tenBefore, 0, report);
            Assert.GreaterOrEqual(tenAfter, 0, report);
            Assert.GreaterOrEqual(tenAfter, 0.7 * tenBefore, "level 10 no more than 30 % sooner\n" + report);
            Assert.GreaterOrEqual(fiveAfter, 5 * 60, "level 5 still not in the first five minutes\n" + report);
            Assert.LessOrEqual(fiveAfter, 12 * 60, "and still within twelve\n" + report);
            Assert.Greater(fishCaught, 0, "the casual player does catch fish\n" + report);
        }
    }
}
