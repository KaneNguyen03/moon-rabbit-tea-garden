using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using UnityEditor;
using VuonTra.Game;

namespace VuonTra.Core.Tests
{
    [Category("Slow")]
    [Timeout(600000)]
    public class GardenPacingTests
    {
        List<StationDef> defs;
        List<UpgradeDef> ups;
        List<QuestDef> quests;
        GameState s;
        Modifiers mods;
        System.Random rng;
        double clock;
        readonly Dictionary<int, double> levelAt = new Dictionary<int, double>();
        readonly double[] waveLevelAt = new double[Expansion.Table.Length], waveRabbitsAt = new double[Expansion.Table.Length], waveTakenAt = new double[Expansion.Table.Length];
        readonly long[] waveTakenOn = new long[Expansion.Table.Length];
        int summoned;
        int tapsPerSecond = 1, buyEvery = 3, buysAtOnce = 1, questEvery = 30;
        bool openJars = true;
        long today = 1;
        int jarsHeld, jarsOpened;
        BigNum earnedJars;
        readonly Dictionary<long, int> freeJars = new Dictionary<long, int>();
        bool meetFrogs = true;
        int frogsMet, frogClovers;
        BigNum earnedFrogs;
        System.Random frogRng;
        bool goFishing = true;
        const double FishCatchRate = 0.8;
        int casts, fishCaught, fishPoints;
        System.Random fishRng;
        bool upgradeAll;

        const int Seed = 20261122;

        static readonly int[] Seeds = { Seed, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 };

        const int Enough = 15;

        [SetUp]
        public void SetUp() => Begin(Seed);

        void Begin(int seed)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<GameCatalog>("Assets/VuonTra/Content/GameCatalog.asset");
            Assert.IsNotNull(catalog, "Run VuonTra > Seed Content first.");
            defs = catalog.BuildDefs();
            ups = catalog.BuildUpgrades();
            quests = catalog.BuildQuests();
            s = GameState.NewGame(defs, ups.Count);
            s.Profile.Code = Fixture.CodeFor(seed);
            mods = Modifiers.From(ups, s);
            rng = new System.Random(seed);
            clock = 0;
            tapsPerSecond = 1; buyEvery = 3; buysAtOnce = 1; questEvery = 30;
            levelAt.Clear();
            summoned = 0;
            for (int i = 0; i < waveLevelAt.Length; i++) { waveLevelAt[i] = waveRabbitsAt[i] = waveTakenAt[i] = -1; waveTakenOn[i] = -1; }
            trace.Clear();
            openJars = true;
            today = 1;
            jarsHeld = jarsOpened = 0;
            earnedJars = BigNum.Zero;
            freeJars.Clear();
            newStationOpenedOn.Clear();
            newStationOpenedAt.Clear();
            levelOnDay.Clear();
            meetFrogs = true;
            frogsMet = frogClovers = 0;
            earnedFrogs = BigNum.Zero;
            frogRng = new System.Random(unchecked(seed * 31 + 62));
            goFishing = true;
            casts = fishCaught = fishPoints = 0;
            fishRng = new System.Random(unchecked(seed * 31 + 58));
            upgradeAll = false;
            levelDayOne = 0;
            setsDayOne = null;
            ringSevenDay = -1;
            Seasons.Clamp(s, today);
            Quests.RollDaily(quests, defs, s, 1, mods);
        }

        void OldRules()
        {
            openJars = false;
            foreach (QuestDef quest in quests)
                if (quest.RewardJars > 0) { quest.RewardClovers += quest.RewardJars; quest.RewardJars = 0; }
        }

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

        void MeetFrog()
        {
            if (!meetFrogs) return;
            Tutorial.Advance(s);
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

        void ActiveSecond()
        {
            for (int tap = 0; tap < tapsPerSecond; tap++) Simulation.Tap(defs, s, 0, mods);
            Simulation.Tick(defs, s, 1d, null, mods);
            clock += 1d;

            for (int buys = 0; buys < buysAtOnce && (int)clock % buyEvery == 0; buys++)
            {
                if (Expansion.Expand(defs, s))
                {
                    if (Expansion.Segments(s) <= waveTakenAt.Length) { waveTakenAt[Expansion.Segments(s) - 1] = clock; waveTakenOn[Expansion.Segments(s) - 1] = today; }
                    if (Expansion.Segments(s) == 4) ringSevenDay = today;
                    mods = Modifiers.From(ups, s);
                    continue;
                }
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
            bool called = false;
            while (s.Clovers >= Rabbits.SummonCost)
            {
                Rabbits.Summon(s, rng.NextDouble(), "R", rng.NextDouble());
                summoned++;
                Rabbits.AutoAssign(defs, s);
                called = true;
            }
            if (called) mods = Modifiers.From(ups, s);
            Note();
        }

        void Note()
        {
            NoteNewStations();
            int level = Garden.Level(s);
            for (int l = 2; l <= level; l++)
                if (!levelAt.ContainsKey(l)) levelAt[l] = clock;
            for (int w = 0; w < Expansion.Table.Length; w++)
            {
                MistSegment wave = Expansion.Table[w];
                if (waveLevelAt[w] < 0 && level >= wave.NeedLevel) waveLevelAt[w] = clock;
                if (waveRabbitsAt[w] < 0 && s.Rabbits.Count >= wave.NeedRabbits && s.SeenKinds.Count >= wave.NeedKinds) waveRabbitsAt[w] = clock;
            }
        }

        readonly StringBuilder trace = new StringBuilder();

        void UpgradeAll()
        {
            if (!upgradeAll) return;
            List<UpgradeStep> plan = UpgradePlan.Plan(defs, s, Simulation.Budget(s));
            if (plan.Count > 0 && UpgradePlan.Apply(defs, s, plan)) Note();
        }

        void Play(double seconds)
        {
            MeetFrog();
            GoFishing();
            UpgradeAll();
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
            UpgradeAll();
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

        int levelDayOne;
        List<AlbumSet> setsDayOne;
        long ringSevenDay;

        static double Median(IEnumerable<double> values)
        {
            var sorted = new List<double>(values);
            sorted.Sort();
            int n = sorted.Count;
            return n == 0 ? double.NaN : n % 2 == 1 ? sorted[n / 2] : (sorted[n / 2 - 1] + sorted[n / 2]) / 2d;
        }

        static double Median(IEnumerable<Run> runs, System.Func<Run, double> value)
        {
            var values = new List<double>();
            foreach (Run run in runs) values.Add(value(run));
            return Median(values);
        }

        static int Count(IEnumerable<Run> runs, System.Func<Run, bool> rule)
        {
            int n = 0;
            foreach (Run run in runs) if (rule(run)) n++;
            return n;
        }

        static void Holds(IList<Run> runs, System.Func<Run, bool> rule, string what, string report)
        {
            int n = Count(runs, rule);
            Assert.GreaterOrEqual(n, Enough, what + ": on " + n + " of " + runs.Count + " seeds, at least " + Enough + " asked\n" + report);
        }

        static void Always(IList<Run> runs, System.Func<Run, bool> rule, string what, string report)
        {
            int n = Count(runs, rule);
            Assert.AreEqual(runs.Count, n, what + ": on " + n + " of " + runs.Count + " seeds, every one asked\n" + report);
        }

        sealed class Run
        {
            public int Seed;
            public double Level5, Level10, Level15, UpgradeAllOpens;
            public int LevelDayOne, SetsDayOne, LevelEnd, Plots, JarsOpened, MostFreeJars, FrogsMet, FishCaught, Summoned;
            public long RingSevenDay, Station13Day = -1, Station16Day = -1, Segment5Day = -1, Segment6Day = -1;
            public readonly long[] StationDay = { -1, -1, -1, -1 };
            public bool NewStationsInOrder = true;
            public readonly double[] Wait = new double[3];
            public double Coins, Xp;
            public string Fingerprint;
        }

        Run Result(int seed)
        {
            var r = new Run
            {
                Seed = seed, Level5 = LevelAt(5), Level10 = LevelAt(10), Level15 = LevelAt(15), UpgradeAllOpens = LevelAt(UpgradePlan.UnlockLevel), LevelDayOne = levelDayOne,
                SetsDayOne = setsDayOne == null ? 0 : setsDayOne.Count, LevelEnd = Garden.Level(s), Plots = s.Plots, JarsOpened = jarsOpened,
                MostFreeJars = MostFreeJarsInADay(), FrogsMet = frogsMet, FishCaught = fishCaught, Summoned = summoned, RingSevenDay = ringSevenDay,
                Coins = s.TotalEarned.ToDouble(), Xp = s.GardenXp
            };
            for (int w = 0; w < 3; w++) r.Wait[w] = waveRabbitsAt[w] < 0 ? double.PositiveInfinity : waveRabbitsAt[w] - waveLevelAt[w];
            if (newStationOpenedOn.TryGetValue(12, out long on13)) r.Station13Day = on13;
            for (int k = 0; k < 4; k++) if (newStationOpenedOn.TryGetValue(12 + k, out long on)) r.StationDay[k] = on;
            if (waveTakenOn.Length > 5) { r.Segment5Day = waveTakenOn[4]; r.Segment6Day = waveTakenOn[5]; }
            if (newStationOpenedOn.TryGetValue(15, out long on16)) r.Station16Day = on16;
            for (int i = 13; i < defs.Count; i++)
                if (!newStationOpenedOn.ContainsKey(i) || !newStationOpenedOn.ContainsKey(i - 1) || newStationOpenedOn[i] < newStationOpenedOn[i - 1]) r.NewStationsInOrder = false;
            var levels = new StringBuilder();
            foreach (KeyValuePair<int, double> l in levelAt) levels.Append(l.Key).Append('@').Append(l.Value).Append(' ');
            r.Fingerprint = SaveCodec.Encode(defs, s, ups) + "\n" + levels + "\n" + summoned + "/" + casts + "/" + fishCaught + "/" + fishPoints + "/" + frogsMet + "/" + frogClovers + "/" + jarsOpened;
            return r;
        }

        double LevelAt(int level) => levelAt.TryGetValue(level, out double t) ? t : -1;

        Run[] OverSeeds(System.Action<int> play)
        {
            var runs = new Run[Seeds.Length];
            for (int i = 0; i < Seeds.Length; i++)
            {
                Begin(Seeds[i]);
                play(Seeds[i]);
                runs[i] = Result(Seeds[i]);
            }
            return runs;
        }

        void TirelessDay()
        {
            tapsPerSecond = 3; buyEvery = 1; buysAtOnce = 4; questEvery = 1;
            Play(20 * 60);
            for (int visit = 0; visit < 4; visit++) { Away(2 * 3600); Play(5 * 60); }
        }

        static string SeedTable(Run[] runs)
        {
            var sb = new StringBuilder();
            sb.AppendLine("| Hạt | Cấp 5 | Cấp 10 | Cấp hết ngày 1 | Cấp 15 | 12 ô đất (R 7) | Bộ sổ thỏ ngày 1 | Cấp hết tuần | Thỏ đã gọi |");
            sb.AppendLine("|---|---|---|---|---|---|---|---|---|");
            foreach (Run r in runs)
                sb.AppendLine("| " + r.Seed + " | " + Clock(r.Level5) + " | " + Clock(r.Level10) + " | " + r.LevelDayOne + " | " + Clock(r.Level15) + " | " + (r.RingSevenDay < 0 ? "chưa" : "ngày " + r.RingSevenDay)
                    + " | " + r.SetsDayOne + " | " + r.LevelEnd + " | " + r.Summoned + " |");
            sb.AppendLine("| **trung vị** | " + Clock(Median(runs, r => r.Level5)) + " | " + Clock(Median(runs, r => r.Level10)) + " | " + Median(runs, r => r.LevelDayOne) + " | " + Clock(Median(runs, r => r.Level15 < 0 ? double.MaxValue : r.Level15))
                + " | ngày " + Median(runs, r => r.RingSevenDay < 0 ? 99 : r.RingSevenDay) + " | " + Median(runs, r => r.SetsDayOne) + " | " + Median(runs, r => r.LevelEnd) + " | " + Median(runs, r => r.Summoned) + " |");
            return sb.ToString();
        }

        [Test]
        public void The_Second_Season_Climbs_Faster_Than_The_First_And_Still_Passes_The_Marks()
        {
            Run[] first = OverSeeds(seed => Play(10 * 60));
            Run[] second = OverSeeds(seed =>
            {
                s.Season = 1;
                mods = Modifiers.From(ups, s);
                Assert.AreEqual(1.25d, mods.IncomeMultiplier, 1e-9);
                Play(10 * 60);
            });
            var report = new StringBuilder("10 phút đầu, mùa 1 → mùa 2, theo hạt:\n");
            for (int i = 0; i < Seeds.Length; i++)
                report.AppendLine("  hạt " + Seeds[i] + ": cấp " + Garden.LevelOf(first[i].Xp) + " → " + Garden.LevelOf(second[i].Xp) + ", cà rốt " + new BigNum(first[i].Coins).ToShortString() + " → " + new BigNum(second[i].Coins).ToShortString());
            string text = report.ToString();
            var both = new List<Run>();
            for (int i = 0; i < Seeds.Length; i++) both.Add(new Run { Seed = Seeds[i], Coins = second[i].Coins - first[i].Coins, Xp = second[i].Xp - first[i].Xp, LevelEnd = second[i].LevelEnd - first[i].LevelEnd });
            Holds(both, r => r.Coins > 0, "a quarter more on every carrot", text);
            Holds(both, r => r.Xp >= 0 && r.LevelEnd >= 0, "more carrots buy more levels", text);
            Holds(second, r => r.LevelEnd >= 5, "the ten-minute mark still holds on the second season", text);
        }

        [Test]
        public void Report_For_A_Tireless_Player()
        {
            tapsPerSecond = 3; buyEvery = 1; buysAtOnce = 4; questEvery = 1;
            Play(20 * 60);
            int at20 = Garden.Level(s);
            double xp20 = s.GardenXp;
            for (int visit = 0; visit < 4; visit++) { Away(2 * 3600); Play(5 * 60); }
            var report = new StringBuilder();
            report.AppendLine("Người chơi không nghỉ tay (3 chạm/giây, 4 lần mua mỗi giây, nhận nhiệm vụ ngay), hạt " + Seed);
            report.AppendLine(FrogLine());
            report.AppendLine(FishLine());
            report.AppendLine("  sau 20 phút: xp " + (int)xp20 + " (cấp " + at20 + "); cuối ngày 1: xp " + (int)s.GardenXp + " (cấp " + Garden.Level(s) + "), " + s.Rabbits.Count + " thỏ, " + s.Plots + " ô đất (" + Expansion.Segments(s) + " đoạn)");
            foreach (int level in new[] { 3, 5, 6, 10, 15 })
                report.AppendLine("  cấp vườn " + level + ": " + (levelAt.ContainsKey(level) ? Clock(levelAt[level]) : "chưa đạt"));
            report.AppendLine("Diễn biến:");
            report.Append(trace);
            System.IO.Directory.CreateDirectory("Logs");
            System.IO.File.WriteAllText("Logs/garden-pacing-tireless.txt", report.ToString());
            Assert.Pass(report.ToString());
        }

        void CasualWeek(int lastDay = 7)
        {
            Play(20 * 60);
            for (int visit = 0; visit < 4; visit++)
            {
                Away(2 * 3600);
                Play(5 * 60);
            }
            levelDayOne = Garden.Level(s);
            setsDayOne = AlbumSets.Completed(s);
            for (int day = 2; day <= lastDay; day++)
            {
                Away(8 * 3600);
                today = day;
                Seasons.Clamp(s, today);
                Quests.RollDaily(quests, defs, s, day, mods);
                for (int visit = 0; visit < 3; visit++)
                {
                    Play(5 * 60);
                    Away(2 * 3600);
                }
            }
        }

        [Test]
        public void The_Same_Seed_Plays_The_Same_Game_Twice()
        {
            Begin(Seed); CasualWeek(); string casual = Result(Seed).Fingerprint;
            Begin(Seed); TirelessDay(); string tireless = Result(Seed).Fingerprint;
            Begin(Seed); CasualWeek(); Assert.AreEqual(casual, Result(Seed).Fingerprint, "the casual week played again on the same seed");
            Begin(Seed); TirelessDay(); Assert.AreEqual(tireless, Result(Seed).Fingerprint, "the tireless day played again on the same seed");
            Begin(Seeds[1]); CasualWeek(); Assert.AreNotEqual(casual, Result(Seeds[1]).Fingerprint, "another seed plays another week");
        }

        [Test]
        public void A_Casual_Player_Reaches_Level_Five_In_About_Ten_Minutes_And_Ten_In_A_Day_Without_Waiting_On_Rabbits()
        {
            Run[] runs = OverSeeds(seed => CasualWeek());

            Begin(Seed);
            CasualWeek();
            double activeDayOne = (20 + 4 * 5) * 60;
            var report = new StringBuilder();
            report.AppendLine("Người chơi thong thả (1 chạm/giây, 3 giây mua một lần thứ rẻ nhất, 30 giây xem nhiệm vụ một lần, không ghép thỏ), hạt " + Seed);
            report.AppendLine("Ngày 1: chơi 20 phút, rồi 4 lần ghé 5 phút cách nhau 2 giờ (tổng " + (int)(activeDayOne / 60) + " phút chơi)");
            foreach (int level in new[] { 2, 3, 5, 6, 10, 15, 16 })
                report.AppendLine("  cấp vườn " + level + ": " + (levelAt.ContainsKey(level) ? Clock(levelAt[level]) : "chưa đạt trong 7 ngày"));
            report.AppendLine("Cuối ngày 1: cấp " + levelDayOne + "; cuối ngày 7: cấp " + Garden.Level(s) + ", " + s.Rabbits.Count + " thỏ, " + s.SeenKinds.Count + " loài đã gặp, " + s.Plots + " ô đất (" + Expansion.Segments(s) + " đoạn), đã gọi " + summoned + " lần");
            report.AppendLine("Sổ thỏ: cuối ngày 1 đủ " + setsDayOne.Count + " bộ (" + string.Join(", ", setsDayOne.ConvertAll(x => x.Key)) + "), cuối ngày 7 đủ " + AlbumSets.Completed(s).Count + " bộ (" + string.Join(", ", AlbumSets.Completed(s).ConvertAll(x => x.Key)) + ")");
            for (int w = 0; w < waveLevelAt.Length; w++)
                report.AppendLine("  đoạn " + Expansion.Table[w].SegmentsAfter + ": đủ cấp lúc " + Clock(waveLevelAt[w]) + ", đủ thỏ lúc " + Clock(waveRabbitsAt[w]) + ", mở lúc " + Clock(waveTakenAt[w]));
            report.AppendLine(JarLine() + "; mở ngay khi ví cho mở (cấp vườn 3)");
            report.AppendLine(FrogLine());
            report.AppendLine(FishLine());
            report.AppendLine();
            report.AppendLine("Trên " + Seeds.Length + " hạt (#83); luật: trung vị, hoặc đúng trên ít nhất " + Enough + "/" + Seeds.Length + " hạt:");
            report.Append(SeedTable(runs));
            report.AppendLine("Diễn biến (hạt " + Seed + "):");
            report.Append(trace);
            System.IO.Directory.CreateDirectory("Logs");
            System.IO.File.WriteAllText("Logs/garden-pacing.txt", report.ToString());
            TestContext.WriteLine(report.ToString());
            string text = report.ToString();

            Holds(runs, r => r.Level5 >= 5 * 60 && r.Level5 <= 12 * 60, "level 5 in about the first ten minutes: between minute five and minute twelve", text);
            Assert.LessOrEqual(Median(runs, r => r.Level5), 9 * 60, "level 5 by minute nine on the median (the owner: about seven)\n" + text);
            Holds(runs, r => r.LevelDayOne >= 10, "level 10 within the first day", text);
            Assert.LessOrEqual(Median(runs, r => r.SetsDayOne), 2, "the album's sets come slowly: one or two on the first day on the median (#61)\n" + text);
            Holds(runs, r => r.Wait[0] <= 300 && r.Wait[1] <= 300 && r.Wait[2] <= 300, "the first three rings never wait on rabbits", text);
            Holds(runs, r => r.RingSevenDay < 0 || r.RingSevenDay >= 3, "all twelve plots not before day three", text);
        }

        [Test]
        public void Moon_Jars_Come_Free_At_Most_One_A_Day_And_Bring_Level_Ten_No_More_Than_A_Fifth_Sooner()
        {
            Run[] before = OverSeeds(seed => { OldRules(); CasualWeek(); });
            Run[] after = OverSeeds(seed => CasualWeek());
            Run[] tireless = OverSeeds(seed => TirelessDay());
            double tenBefore = Median(before, r => r.Level10 < 0 ? double.MaxValue : r.Level10), tenAfter = Median(after, r => r.Level10 < 0 ? double.MaxValue : r.Level10);
            string report = "Trung vị trên " + Seeds.Length + " hạt — trước hũ: cấp 5 lúc " + Clock(Median(before, r => r.Level5)) + ", cấp 10 lúc " + Clock(tenBefore) + ", gọi " + Median(before, r => r.Summoned) + " thỏ.\n"
                + "Có hũ: cấp 5 lúc " + Clock(Median(after, r => r.Level5)) + ", cấp 10 lúc " + Clock(tenAfter) + ", gọi " + Median(after, r => r.Summoned) + " thỏ, mở " + Median(after, r => r.JarsOpened) + " hũ.";
            TestContext.WriteLine(report);
            Always(before, r => r.JarsOpened == 0, "no jar is opened under the old rules", report);
            Holds(after, r => r.Level10 >= 0, "level 10 is reached", report);
            Assert.GreaterOrEqual(tenAfter, 0.8 * tenBefore, "level 10 no more than a fifth sooner, on the median\n" + report);
            Always(after, r => r.JarsOpened > 0, "the casual player does get jars", report);
            Always(after, r => r.MostFreeJars <= MoonJars.FreePerDay, "at most one free jar a day", report);
            Always(tireless, r => r.MostFreeJars <= MoonJars.FreePerDay, "the tireless player too", report);
        }

        [Test]
        public void Frog_Rabbits_Greeted_Twice_A_Day_Bring_Level_Ten_No_More_Than_A_Fifth_Sooner()
        {
            Run[] before = OverSeeds(seed => { meetFrogs = false; CasualWeek(); });
            Run[] after = OverSeeds(seed => CasualWeek());
            double tenBefore = Median(before, r => r.Level10 < 0 ? double.MaxValue : r.Level10), tenAfter = Median(after, r => r.Level10 < 0 ? double.MaxValue : r.Level10);
            string report = "Trung vị trên " + Seeds.Length + " hạt — không Thỏ Ếch: cấp 5 lúc " + Clock(Median(before, r => r.Level5)) + ", cấp 10 lúc " + Clock(tenBefore) + ", gọi " + Median(before, r => r.Summoned) + " thỏ.\n"
                + "Có Thỏ Ếch: cấp 5 lúc " + Clock(Median(after, r => r.Level5)) + ", cấp 10 lúc " + Clock(tenAfter) + ", gọi " + Median(after, r => r.Summoned) + " thỏ, gặp " + Median(after, r => r.FrogsMet) + " lần.";
            TestContext.WriteLine(report);
            Always(before, r => r.FrogsMet == 0, "no frog without frogs", report);
            Assert.GreaterOrEqual(tenAfter, 0.8 * tenBefore, "level 10 no more than a fifth sooner, on the median\n" + report);
            Holds(after, r => r.Level5 >= 5 * 60, "level 5 still not in the first five minutes", report);
            Always(after, r => r.FrogsMet > 0 && r.FrogsMet <= 7 * FrogRabbit.PerDay, "the casual player meets frogs, two a day at most", report);
        }

        [Test]
        public void Fishing_And_Feeding_Every_Sitting_Bring_Level_Ten_No_More_Than_Thirty_Percent_Sooner()
        {
            Run[] before = OverSeeds(seed => { goFishing = false; CasualWeek(); });
            Run[] after = OverSeeds(seed => CasualWeek());
            Run[] tirelessBefore = OverSeeds(seed => { goFishing = false; TirelessDay(); });
            Run[] tirelessAfter = OverSeeds(seed => TirelessDay());
            System.Func<Run, double> ten = r => r.Level10 < 0 ? double.MaxValue : r.Level10;
            System.Func<Run, double> fifteen = r => r.Level15 < 0 ? double.MaxValue : r.Level15;
            string report = "Trung vị trên " + Seeds.Length + " hạt — không câu cá: cấp 5 lúc " + Clock(Median(before, r => r.Level5)) + ", cấp 10 lúc " + Clock(Median(before, ten)) + ", cấp 15 lúc " + Clock(Median(before, fifteen)) + ", hết ngày 7 cấp " + Median(before, r => r.LevelEnd) + ".\n"
                + "Câu cá mỗi buổi: cấp 5 lúc " + Clock(Median(after, r => r.Level5)) + ", cấp 10 lúc " + Clock(Median(after, ten)) + ", cấp 15 lúc " + Clock(Median(after, fifteen)) + ", hết ngày 7 cấp " + Median(after, r => r.LevelEnd) + ", câu được " + Median(after, r => r.FishCaught) + " con.\n"
                + "Người chơi không nghỉ tay: cấp 10 lúc " + Clock(Median(tirelessBefore, ten)) + " không câu, " + Clock(Median(tirelessAfter, ten)) + " có câu.";
            System.IO.Directory.CreateDirectory("Logs");
            System.IO.File.WriteAllText("Logs/garden-pacing-fishing.txt", report);
            TestContext.WriteLine(report);

            Holds(after, r => r.Level10 >= 0, "level 10 is reached", report);
            Assert.GreaterOrEqual(Median(after, ten), 0.7 * Median(before, ten), "level 10 no more than 30 % sooner, on the median\n" + report);
            Holds(after, r => r.Level5 >= 5 * 60 && r.Level5 <= 12 * 60, "level 5 still between minute five and minute twelve", report);
            Always(after, r => r.FishCaught > 0, "the casual player does catch fish", report);
        }

        #region Four new stations (#67a, #67b)

        readonly Dictionary<int, long> newStationOpenedOn = new Dictionary<int, long>();
        readonly Dictionary<int, double> newStationOpenedAt = new Dictionary<int, double>();
        readonly SortedDictionary<long, int> levelOnDay = new SortedDictionary<long, int>();

        void NoteNewStations()
        {
            levelOnDay[today] = Garden.Level(s);
            for (int i = 12; i < defs.Count; i++)
                if (s.Levels[i] > 0 && !newStationOpenedOn.ContainsKey(i)) { newStationOpenedOn[i] = today; newStationOpenedAt[i] = clock; }
        }

        string NewStationLine(int i) => "  trạm " + (i + 1) + " (" + defs[i].Name + ", cấp vườn " + defs[i].NeedGardenLevel + "): "
            + (newStationOpenedOn.ContainsKey(i) ? "mở ngày " + newStationOpenedOn[i] + " (" + Clock(newStationOpenedAt[i]) + " trên đồng hồ chơi)" : "chưa mở");

        [Test]
        public void A_Casual_Player_Opens_Station_Thirteen_On_Day_Three_To_Five_And_Sixteen_On_Day_Eight_To_Twelve()
        {
            Assert.AreEqual(16, defs.Count, "four stations after the star observatory");
            Run[] runs = OverSeeds(seed => CasualWeek(14));

            Begin(Seed);
            CasualWeek(14);
            var report = new StringBuilder();
            report.AppendLine("Bốn trạm mới (#67), người chơi thong thả như trên, chơi tới ngày 14, hạt " + Seed + "; đất cho trạm 13–16 là đoạn 5 (cấp " + Expansion.Segment(5).NeedLevel + ", chơi " + Expansion.Segment(5).NeedDays + " ngày) và đoạn 6 (cấp " + Expansion.Segment(6).NeedLevel + ", chơi " + Expansion.Segment(6).NeedDays
                + " ngày), giá một phần tư giá hai trạm ×" + Expansion.Segment56CostTimes + " (#99).");
            for (int i = 12; i < defs.Count; i++) report.AppendLine(NewStationLine(i));
            for (int w = 0; w < waveLevelAt.Length; w++)
                report.AppendLine("  đoạn " + Expansion.Table[w].SegmentsAfter + " (cấp " + Expansion.Table[w].NeedLevel + "): đủ cấp lúc " + Clock(waveLevelAt[w]) + ", mở lúc " + Clock(waveTakenAt[w]));
            foreach (int level in new[] { 10, 15, 16, 18, 20, 22, 23 })
                report.AppendLine("  cấp vườn " + level + ": " + (levelAt.ContainsKey(level) ? Clock(levelAt[level]) : "chưa đạt"));
            var days = new List<string>();
            foreach (KeyValuePair<long, int> d in levelOnDay) days.Add("ngày " + d.Key + ": " + d.Value);
            report.AppendLine("  cấp vườn cuối mỗi ngày: " + string.Join(", ", days));
            report.AppendLine("Cuối ngày 14: cấp " + Garden.Level(s) + ", thu/giây " + Economy.IncomePerSecond(defs, s).ToShortString() + ", " + s.Plots + " ô đất, " + Expansion.Segments(s) + " đoạn");
            report.AppendLine("Trên " + Seeds.Length + " hạt: trạm 13 mở ngày " + Median(runs, r => r.Station13Day < 0 ? 99 : r.Station13Day) + " (trung vị; " + Count(runs, r => r.Station13Day >= 3 && r.Station13Day <= 5) + " hạt trong ngày 3–5), trạm 16 ngày "
                + Median(runs, r => r.Station16Day < 0 ? 99 : r.Station16Day) + " (" + Count(runs, r => r.Station16Day >= 8 && r.Station16Day <= 12) + " hạt trong ngày 8–12), cấp ngày 14 trung vị " + Median(runs, r => r.LevelEnd) + " (Mùa trăng mới ở cấp " + NewMoon.FirstLevel + ": " + Count(runs, r => r.LevelEnd >= NewMoon.FirstLevel) + " hạt tới).");
            System.IO.Directory.CreateDirectory("Logs");
            System.IO.File.WriteAllText("Logs/garden-pacing-67.txt", report.ToString());
            TestContext.WriteLine(report.ToString());
            string text = report.ToString();

            Always(runs, r => r.Plots == defs.Count, "every station has its land by day fourteen", text);
            Holds(runs, r => r.Station13Day >= 3 && r.Station13Day <= 5, "station 13 opens on day three to five", text);
            Holds(runs, r => r.Station16Day >= 8 && r.Station16Day <= 12, "station 16 opens on day eight to twelve", text);
            Holds(runs, r => r.Station13Day >= 4 && r.Station13Day <= 6, "station 13 opens on day five, give or take one (#99)", text);
            Holds(runs, r => r.Station16Day >= 7 && r.Station16Day <= 11, "station 16 opens on day nine, give or take two (#99)", text);
            Always(runs, r => r.NewStationsInOrder, "all four open, in order", text);
            Holds(runs, r => r.LevelEnd < NewMoon.FirstLevel, "the casual player does not reach the new moon in fourteen days (#1)", text);
        }

        static string Spread(Run[] runs, System.Func<Run, double> value, System.Func<double, string> show)
        {
            var values = new List<double>();
            foreach (Run r in runs) values.Add(value(r) < 0 ? double.MaxValue : value(r));
            values.Sort();
            System.Func<double, string> one = v => v == double.MaxValue ? "–" : show(v);
            return one(Median(values)) + " [" + one(values[0]) + "–" + one(values[values.Count - 1]) + "]";
        }

        string UpgradeAllTable(Run[] single, Run[] all)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Nâng tất cả (#99): người chơi thong thả như trên, chơi tới ngày 14 trên " + Seeds.Length + " hạt; trung vị [thấp nhất–cao nhất]. Ngưỡng: đoạn 4 cấp " + Expansion.Segment(4).NeedLevel
                + ", đoạn 5 cấp " + Expansion.Segment(5).NeedLevel + " + chơi " + Expansion.Segment(5).NeedDays + " ngày, đoạn 6 cấp " + Expansion.Segment(6).NeedLevel + " + chơi " + Expansion.Segment(6).NeedDays
                + " ngày, giá hai đoạn ×" + Expansion.Segment56CostTimes + " (#99), trạm 13–16 cấp " + defs[12].NeedGardenLevel + "/" + defs[13].NeedGardenLevel + "/" + defs[14].NeedGardenLevel + "/" + defs[15].NeedGardenLevel + ".");
            sb.AppendLine();
            sb.AppendLine("| Mốc | Mua từng cấp | Bấm Nâng tất cả đầu và cuối mỗi buổi |");
            sb.AppendLine("|---|---|---|");
            System.Action<string, System.Func<Run, double>, System.Func<double, string>> row = (label, value, show) =>
                sb.AppendLine("| " + label + " | " + Spread(single, value, show) + " | " + Spread(all, value, show) + " |");
            System.Func<double, string> day = v => "ngày " + v;
            row("Cấp 5", r => r.Level5, v => Clock(v));
            row("Cấp 10", r => r.Level10, v => Clock(v));
            row("Cấp hết ngày 1", r => r.LevelDayOne, v => v.ToString());
            row("12 ô đất (đoạn 4)", r => r.RingSevenDay, day);
            row("Đoạn 5", r => r.Segment5Day, day);
            row("Đoạn 6", r => r.Segment6Day, day);
            for (int k = 0; k < 4; k++) { int kk = k; row("Trạm " + (13 + k) + " (" + defs[12 + k].Name + ")", r => r.StationDay[kk], day); }
            row("Cấp ngày 14", r => r.LevelEnd, v => v.ToString());
            sb.AppendLine("| Trạm 13 trong ngày 3–5 | " + Count(single, r => r.Station13Day >= 3 && r.Station13Day <= 5) + "/" + single.Length + " | " + Count(all, r => r.Station13Day >= 3 && r.Station13Day <= 5) + "/" + all.Length + " |");
            sb.AppendLine("| Trạm 16 trong ngày 8–12 | " + Count(single, r => r.Station16Day >= 8 && r.Station16Day <= 12) + "/" + single.Length + " | " + Count(all, r => r.Station16Day >= 8 && r.Station16Day <= 12) + "/" + all.Length + " |");
            sb.AppendLine("| Trạm 13 trong ngày 4–6 (#99) | " + Count(single, r => r.Station13Day >= 4 && r.Station13Day <= 6) + "/" + single.Length + " | " + Count(all, r => r.Station13Day >= 4 && r.Station13Day <= 6) + "/" + all.Length + " |");
            sb.AppendLine("| Trạm 16 trong ngày 7–11 (#99) | " + Count(single, r => r.Station16Day >= 7 && r.Station16Day <= 11) + "/" + single.Length + " | " + Count(all, r => r.Station16Day >= 7 && r.Station16Day <= 11) + "/" + all.Length + " |");
            sb.AppendLine("| Trạm 13 không trước ngày 4 (#99) | " + Count(single, r => r.Station13Day >= 4) + "/" + single.Length + " | " + Count(all, r => r.Station13Day >= 4) + "/" + all.Length + " |");
            sb.AppendLine("| 12 ô đất không trước ngày 3 | " + Count(single, r => r.RingSevenDay < 0 || r.RingSevenDay >= 3) + "/" + single.Length + " | " + Count(all, r => r.RingSevenDay < 0 || r.RingSevenDay >= 3) + "/" + all.Length + " |");
            sb.AppendLine("| Cấp 10 không trong buổi đầu, ≥ 2 giờ (#101) | " + Count(single, r => r.Level10 >= 2 * 3600) + "/" + single.Length + " | " + Count(all, r => r.Level10 >= 2 * 3600) + "/" + all.Length + " |");
            sb.AppendLine("| Nâng tất cả mở ở cấp vườn " + UpgradePlan.UnlockLevel + " (#101) | – | " + Spread(all, r => r.UpgradeAllOpens, v => Clock(v)) + " |");
            return sb.ToString();
        }

        [Test, Category("Slow")]
        public void A_Casual_Player_Who_Presses_Upgrade_All_Keeps_The_First_Hour_And_Opens_All_Four_Stations_In_Order()
        {
            Run[] single = OverSeeds(seed => CasualWeek(14));
            Run[] all = OverSeeds(seed => { upgradeAll = true; CasualWeek(14); });
            string text = UpgradeAllTable(single, all);
            System.IO.Directory.CreateDirectory("Logs");
            System.IO.File.WriteAllText("Logs/garden-pacing-upgrade-all.txt", text);
            TestContext.WriteLine(text);

            Holds(all, r => r.Level5 >= 5 * 60 && r.Level5 <= 12 * 60, "level 5 still between minute five and minute twelve", text);
            Holds(all, r => r.LevelDayOne >= 10, "level 10 within the first day", text);
            Always(all, r => r.Plots == defs.Count && r.NewStationsInOrder, "every station has its land by day fourteen, the four new ones opened in order", text);
            Always(all, r => r.MostFreeJars <= MoonJars.FreePerDay, "at most one free jar a day", text);
            Holds(all, r => r.LevelEnd < NewMoon.FirstLevel, "no new moon in fourteen days", text);
            Always(all, r => r.Station13Day >= 4, "station 13 not before day four (#99)", text);
            Always(all, r => r.Segment5Day > Expansion.Segment(5).NeedDays && r.Segment6Day > Expansion.Segment(6).NeedDays, "segments 5 and 6 wait for their days played (#99)", text);
            Holds(all, r => r.Level10 >= 2 * 3600, "garden level 10 not in the first sitting (#101)", text);
            Holds(all, r => r.RingSevenDay < 0 || r.RingSevenDay >= 3, "all twelve plots not before day three (#101)", text);
        }

        #endregion
    }
}
