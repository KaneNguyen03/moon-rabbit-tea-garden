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
            levelAt.Clear();
            summoned = 0;
            for (int i = 0; i < 4; i++) waveLevelAt[i] = waveRabbitsAt[i] = waveTakenAt[i] = -1;
            trace.Clear();
            openJars = true;
            today = 1;
            jarsHeld = jarsOpened = 0;
            earnedJars = BigNum.Zero;
            freeJars.Clear();
            landForNewStations = false;
            newStationOpenedOn.Clear();
            newStationOpenedAt.Clear();
            levelOnDay.Clear();
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
                if (landForNewStations && LandForANewStation()) continue;
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
                    if (s.Levels[i] == 0 && !Economy.CanUnlock(defs, s, i)) continue;
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
            NoteNewStations();
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
        void CasualWeek(System.Action endOfDayOne = null, int lastDay = 7)
        {
            Play(20 * 60);
            for (int visit = 0; visit < 4; visit++)
            {
                Away(2 * 3600);
                Play(5 * 60);
            }
            endOfDayOne?.Invoke();
            for (int day = 2; day <= lastDay; day++)
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
            #region Four new stations (#67a)

        /// <summary>
        /// Sim-only, until #53 brings the rings of land: a plot for each station after the twelfth comes as soon as the garden
        /// has the level that station asks for, at the plot rule's price (a quarter of the station's). Off, the game as it ships.
        /// </summary>
        bool landForNewStations;
        /// <summary>The day each station after the twelfth was opened, by its index.</summary>
        readonly Dictionary<int, long> newStationOpenedOn = new Dictionary<int, long>();
        readonly Dictionary<int, double> newStationOpenedAt = new Dictionary<int, double>();
        /// <summary>The garden level at the end of each day of play.</summary>
        readonly SortedDictionary<long, int> levelOnDay = new SortedDictionary<long, int>();

        bool LandForANewStation()
        {
            int next = s.Plots;
            if (next < 12 || next >= defs.Count || !Economy.HasGardenLevel(defs, s, next)) return false;
            var price = new BigNum(defs[next].BaseCost * Plots.CostShare);
            if (s.Coins < price) return false;
            s.Coins -= price;
            s.Plots++;
            GardenGrid.Settle(defs, s);
            return true;
        }

        void NoteNewStations()
        {
            levelOnDay[today] = Garden.Level(s);
            for (int i = 12; i < defs.Count; i++)
                if (s.Levels[i] > 0 && !newStationOpenedOn.ContainsKey(i)) { newStationOpenedOn[i] = today; newStationOpenedAt[i] = clock; }
        }

        string NewStationLine(int i) => "  trạm " + (i + 1) + " (" + defs[i].Name + ", cấp vườn " + defs[i].NeedGardenLevel + "): "
            + (newStationOpenedOn.ContainsKey(i) ? "mở ngày " + newStationOpenedOn[i] + " (" + Clock(newStationOpenedAt[i]) + " trên đồng hồ chơi)" : "chưa mở");

        /// <summary>
        /// The four stations after the star observatory (#67): the casual player of the week above, played on to day fourteen,
        /// opens the thirteenth on day three to five and the sixteenth on day eight to twelve. The land is a sim-only
        /// assumption (see <see cref="landForNewStations"/>) until #53; the garden level each station asks for is the pace.
        /// </summary>
        [Test]
        public void A_Casual_Player_Opens_Station_Thirteen_On_Day_Three_To_Five_And_Sixteen_On_Day_Eight_To_Twelve()
        {
            Assert.AreEqual(16, defs.Count, "four stations after the star observatory");
            landForNewStations = true;
            CasualWeek(null, 14);
            var report = new StringBuilder();
            report.AppendLine("Bốn trạm mới (#67), người chơi thong thả như trên, chơi tới ngày 14; đất cho trạm 13–16 là giả định trong mô phỏng (chờ #53): có ngay khi đủ cấp vườn, giá một phần tư giá trạm.");
            for (int i = 12; i < defs.Count; i++) report.AppendLine(NewStationLine(i));
            foreach (int level in new[] { 10, 15, 16, 18, 20, 22, 23 })
                report.AppendLine("  cấp vườn " + level + ": " + (levelAt.ContainsKey(level) ? Clock(levelAt[level]) : "chưa đạt"));
            var days = new List<string>();
            foreach (KeyValuePair<long, int> d in levelOnDay) days.Add("ngày " + d.Key + ": " + d.Value);
            report.AppendLine("  cấp vườn cuối mỗi ngày: " + string.Join(", ", days));
            report.AppendLine("Cuối ngày 14: cấp " + Garden.Level(s) + ", thu/giây " + Economy.IncomePerSecond(defs, s).ToShortString() + ", " + s.Plots + " ô đất");
            System.IO.Directory.CreateDirectory("Logs");
            System.IO.File.WriteAllText("Logs/garden-pacing-67.txt", report.ToString());
            TestContext.WriteLine(report.ToString());

            Assert.IsTrue(newStationOpenedOn.ContainsKey(12), "station 13 never opened\n" + report);
            Assert.GreaterOrEqual(newStationOpenedOn[12], 3, "station 13 not before day three\n" + report);
            Assert.LessOrEqual(newStationOpenedOn[12], 5, "station 13 by day five\n" + report);
            Assert.IsTrue(newStationOpenedOn.ContainsKey(15), "station 16 never opened\n" + report);
            Assert.GreaterOrEqual(newStationOpenedOn[15], 8, "station 16 not before day eight\n" + report);
            Assert.LessOrEqual(newStationOpenedOn[15], 12, "station 16 by day twelve\n" + report);
            for (int i = 13; i < defs.Count; i++)
            {
                Assert.IsTrue(newStationOpenedOn.ContainsKey(i), "station " + (i + 1) + " never opened\n" + report);
                Assert.GreaterOrEqual(newStationOpenedOn[i], newStationOpenedOn[i - 1], "in order\n" + report);
            }
        }

        /// <summary>As the game ships before #53, no plot exists past the twelfth: the four stations wait.</summary>
        [Test]
        public void Without_The_New_Land_The_Four_Stations_Wait()
        {
            CasualWeek(null, 9);
            Assert.AreEqual(12, s.Plots, "the land waves end at twelve plots until #53");
            for (int i = 12; i < defs.Count; i++) Assert.AreEqual(0, s.Levels[i], defs[i].Id);
        }

        #endregion
    }
}
