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
            Quests.RollDaily(quests, defs, s, 1, mods);
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
            // Every clover calls a rabbit, and nobody is ever merged away: the plainest way to play.
            while (s.Clovers >= Rabbits.SummonCost)
            {
                Rabbits.Summon(s, rng.NextDouble(), "R", rng.NextDouble());
                summoned++;
                Rabbits.AutoAssign(defs, s);
            }
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

        [Test]
        public void A_Casual_Player_Reaches_Level_Five_In_About_Ten_Minutes_And_Ten_In_A_Day_Without_Waiting_On_Rabbits()
        {
            // The first day: twenty minutes at the start, then four visits of five minutes, two hours apart.
            Play(20 * 60);
            for (int visit = 0; visit < 4; visit++)
            {
                Away(2 * 3600);
                Play(5 * 60);
            }
            int levelDayOne = Garden.Level(s);
            double activeDayOne = (20 + 4 * 5) * 60;
            int rabbitsDayOne = s.Rabbits.Count, kindsDayOne = s.SeenKinds.Count, plotsDayOne = s.Plots;

            // Days two to seven: three visits of five minutes each, dailies renewed each morning.
            for (int day = 2; day <= 7; day++)
            {
                Away(8 * 3600);
                Quests.RollDaily(quests, defs, s, day, mods);
                for (int visit = 0; visit < 3; visit++)
                {
                    Play(5 * 60);
                    Away(2 * 3600);
                }
            }

            var report = new StringBuilder();
            report.AppendLine("Người chơi thong thả (1 chạm/giây, 3 giây mua một lần thứ rẻ nhất, 30 giây xem nhiệm vụ một lần, không ghép thỏ)");
            report.AppendLine("Ngày 1: chơi 20 phút, rồi 4 lần ghé 5 phút cách nhau 2 giờ (tổng " + (int)(activeDayOne / 60) + " phút chơi)");
            foreach (int level in new[] { 2, 3, 5, 6, 10, 15 })
                report.AppendLine("  cấp vườn " + level + ": " + (levelAt.ContainsKey(level) ? Clock(levelAt[level]) : "chưa đạt trong 7 ngày"));
            report.AppendLine("Cuối ngày 1: cấp " + levelDayOne + ", " + rabbitsDayOne + " thỏ, " + kindsDayOne + " loài đã gặp, " + plotsDayOne + " ô đất");
            report.AppendLine("Cuối ngày 7: cấp " + Garden.Level(s) + ", " + s.Rabbits.Count + " thỏ, " + s.SeenKinds.Count + " loài đã gặp, " + s.Plots + " ô đất, đã gọi " + summoned + " lần");
            for (int w = 0; w < 4; w++)
                report.AppendLine("  đợt " + (w + 1) + ": đủ cấp lúc " + Clock(waveLevelAt[w]) + ", đủ thỏ lúc " + Clock(waveRabbitsAt[w]) + ", mở lúc " + Clock(waveTakenAt[w]));
            report.AppendLine("Diễn biến:");
            report.Append(trace);
            System.IO.Directory.CreateDirectory("Logs");
            System.IO.File.WriteAllText("Logs/garden-pacing.txt", report.ToString());
            TestContext.WriteLine(report.ToString());

            Assert.IsTrue(levelAt.ContainsKey(5), "level 5 was never reached");
            Assert.LessOrEqual(levelAt[5], 12 * 60, "level 5 should come in about the first ten minutes");
            Assert.GreaterOrEqual(levelAt[5], 5 * 60, "but not in the first moments either");
            Assert.GreaterOrEqual(levelDayOne, 10, "level 10 should come within the first day");
            for (int w = 0; w < 3; w++)
            {
                Assert.GreaterOrEqual(waveLevelAt[w], 0, "wave " + (w + 1) + " level was never reached");
                Assert.GreaterOrEqual(waveRabbitsAt[w], 0, "wave " + (w + 1) + " never had enough rabbits");
                // Rabbits must be ready by the time the level is; a short lag while a quest is claimed is fine.
                Assert.LessOrEqual(waveRabbitsAt[w] - waveLevelAt[w], 300, "wave " + (w + 1) + " waited on rabbits");
            }
        }
    }
}
