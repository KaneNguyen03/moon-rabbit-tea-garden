using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEditor;
using VuonTra.Game;

namespace VuonTra.Core.Tests
{
    public partial class EconomySimulationTests
    {
        enum Pace { Casual, Keen, Tireless }

        static readonly string[] PaceNames = { "thong thả", "chăm", "không nghỉ tay" };
        const int CadenceDays = 28, CadenceFrom = 8;
        const int QuietMedian = 2, QuietP90 = 3;
        const int CadenceQuestEvery = 30;

        sealed class Cadence
        {
            public int Seed;
            public Pace Pace;
            public bool UpgradeAll;
            public readonly List<string>[] News = new List<string>[CadenceDays + 1];
            public readonly bool[] LevelUp = new bool[CadenceDays + 1];
            public readonly Dictionary<string, int> CardDay = new Dictionary<string, int>();
            public readonly Dictionary<string, int> PlainDay = new Dictionary<string, int>();
            public int Quiet, QuietWithLevels;
            public Seasons Moons;
            public int MoonDay(int n) => Moons.MoonDays.Count >= n ? (int)Moons.MoonDays[n - 1] : CadenceDays + 1;
            public string Key => PaceNames[(int)Pace] + (UpgradeAll ? ", Nâng tất cả" : ", từng cấp");
        }

        static bool OpenWithoutCard(GameState s, string id)
        {
            switch (id)
            {
                case Intros.Aquarium: return Fishing.Unlocked(s);
                case Intros.Trunk:
                case Intros.MonthPass:
                case Intros.WeekChest: return true;
                default: return Intros.IsOpen(s, id);
            }
        }

        int planEvery;

        double[] SittingsOf(Pace pace)
        {
            switch (pace)
            {
                case Pace.Keen:
                    tapsPerSecond = 2; buyEvery = 1; buysAtOnce = 2; questEvery = 5; gameEvery = 60; planEvery = 60;
                    poundingScore = 100; leavesScore = 38; firefliesScore = 26;
                    return new[] { 15 * 60d, 3.75 * 3600, 15 * 60d, 3.75 * 3600, 15 * 60d, 3.75 * 3600, 15 * 60d };
                case Pace.Tireless:
                    planEvery = 10;
                    return new[] { 60 * 60d, 4 * 3600d, 30 * 60d, 4 * 3600d, 30 * 60d };
                default:
                    planEvery = 0;
                    return new[] { 20 * 60d, 8 * 3600d, 10 * 60d };
            }
        }

        bool TakeNewMoonOnDay(Seasons moons, int day)
        {
            int before = moons.MoonDays.Count;
            if (!TakeNewMoon(moons)) return false;
            moons.MoonDays[before] = day;
            return true;
        }

        void CadenceSitting(double seconds, Seasons moons, int day)
        {
            OpenSession();
            TakeNewMoonOnDay(moons, day);
            UpgradeAll();
            for (int i = 0; i < seconds; i++)
            {
                ActiveSecond();
                if (planEvery > 0 && (int)sessionClock % planEvery == 0) UpgradeAll();
                if ((int)sessionClock % 60 == 0) TakeNewMoonOnDay(moons, day);
            }
            UpgradeAll();
            TakeNewMoonOnDay(moons, day);
            SellTray(false);
        }

        int OpenStations()
        {
            int open = 0;
            for (int i = 0; i < defs.Count; i++) if (s.Levels[i] > 0) open++;
            return open;
        }

        Cadence PlayCadence(Pace pace, bool pressUpgradeAll, int seed)
        {
            Reset(pace == Pace.Tireless, seed);
            double[] sittings = SittingsOf(pace);
            questEvery = CadenceQuestEvery;
            upgradeAll = pressUpgradeAll;
            goFishing = true;
            tank = Tank.KeepFish;
            var run = new Cadence { Seed = seed, Pace = pace, UpgradeAll = pressUpgradeAll };
            var moons = new Seasons { Seed = seed };
            run.Moons = moons;
            int season = s.Season, open = OpenStations(), segments = Expansion.Segments(s), level = Garden.Level(s), tiers = Quests.TiersDone(quests, s);
            long fair = -1;
            int quiet = 0, quietWithLevels = 0;
            for (int day = 1; day <= CadenceDays; day++)
            {
                double dayStart = (day - 1) * Day;
                for (int k = 0; k < sittings.Length; k++)
                {
                    if (k % 2 == 0) CadenceSitting(sittings[k], moons, day);
                    else Away(sittings[k]);
                }
                Away(dayStart + Day - clock);
                long today = firstDay + day - 1;

                var news = new List<string>();
                if (s.Season > season) news.Add("Mùa trăng mới " + s.Season);
                int openNow = OpenStations();
                if (openNow > open) news.Add("trạm " + openNow);
                int segmentsNow = Expansion.Segments(s);
                if (segmentsNow > segments) news.Add("đoạn đất " + segmentsNow);
                int fresh = Recipes.NewDishes(s);
                if (fresh > 0) { news.Add(fresh + " món mới"); Recipes.SeeDishes(s); }
                int tiersNow = Quests.TiersDone(quests, s);
                if (tiersNow > tiers) news.Add((tiersNow - tiers) + " nấc thành tựu");
                if (Fairs.Open(s, today) is FairRun run0 && run0.Start != fair) { fair = run0.Start; news.Add("hội " + run0.Def.Id); }
                foreach (string id in Intros.All)
                {
                    if (!run.PlainDay.ContainsKey(id) && OpenWithoutCard(s, id)) run.PlainDay[id] = day;
                    if (run.CardDay.ContainsKey(id) || !Intros.IsOpen(s, id)) continue;
                    run.CardDay[id] = day;
                    Intros.Show(s, id);
                    news.Add("thẻ " + id);
                }
                int levelNow = Garden.Level(s);
                run.LevelUp[day] = levelNow > level || s.Season > season;
                run.News[day] = news;
                if (day >= CadenceFrom)
                {
                    quiet = news.Count > 0 ? 0 : quiet + 1;
                    quietWithLevels = news.Count > 0 || run.LevelUp[day] ? 0 : quietWithLevels + 1;
                    run.Quiet = Math.Max(run.Quiet, quiet);
                    run.QuietWithLevels = Math.Max(run.QuietWithLevels, quietWithLevels);
                }
                season = s.Season; open = openNow; segments = segmentsNow; level = levelNow; tiers = tiersNow;
            }
            return run;
        }

        static double Percentile(IEnumerable<double> values, double p)
        {
            var sorted = new List<double>(values);
            sorted.Sort();
            if (sorted.Count == 0) return double.NaN;
            double rank = p * (sorted.Count - 1);
            int below = (int)Math.Floor(rank);
            int above = Math.Min(sorted.Count - 1, below + 1);
            return sorted[below] + (sorted[above] - sorted[below]) * (rank - below);
        }

        static string Spread(IEnumerable<Cadence> runs, Func<Cadence, double> value)
        {
            var values = runs.Select(value).ToList();
            return Median(values) + " [" + Math.Round(Percentile(values, 0.1), 1) + "–" + Math.Round(Percentile(values, 0.9), 1) + "]";
        }

        Cadence[] CadenceOverSeeds()
        {
            var jobs = new List<(Pace pace, bool all, int seed)>();
            foreach (bool all in new[] { false, true })
                foreach (Pace pace in new[] { Pace.Casual, Pace.Keen, Pace.Tireless })
                    foreach (int seed in Seeds) jobs.Add((pace, all, seed));
            var catalog = AssetDatabase.LoadAssetAtPath<GameCatalog>("Assets/VuonTra/Content/GameCatalog.asset");
            var players = new EconomySimulationTests[jobs.Count];
            for (int i = 0; i < jobs.Count; i++) players[i] = new EconomySimulationTests { defs = catalog.BuildDefs(), ups = catalog.BuildUpgrades(), quests = catalog.BuildQuests() };
            var runs = new Cadence[jobs.Count];
            Parallel.For(0, jobs.Count, new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount - 1) },
                i => runs[i] = players[i].PlayCadence(jobs[i].pace, jobs[i].all, jobs[i].seed));
            return runs;
        }

        static string DayLine(Cadence run, int day)
        {
            List<string> news = run.News[day];
            string text = news.Count > 0 ? string.Join(", ", news) : run.LevelUp[day] ? "(chỉ lên cấp vườn)" : "–";
            return "| " + day + " | " + text + " |";
        }

        [Test, Category("Slow"), Timeout(3600000), MonetizationOn(false)]
        public void Weeks_Two_To_Four_Bring_Something_New_At_Least_Every_Two_To_Three_Days_And_The_Round_Two_Cards_Spread_Over_Days_Two_To_Four()
        {
            Cadence[] runs = CadenceOverSeeds();
            var groups = runs.GroupBy(r => r.Key).ToList();

            var report = new StringBuilder();
            report.AppendLine("# Nhịp thứ mới tuần 2–4 (#122)");
            report.AppendLine();
            report.AppendLine("Ba kiểu người chơi như báo cáo 23/11 (thong thả 30 phút/ngày, chăm 4 × 15 phút, không nghỉ tay 2 giờ/ngày) × hai cách mua (từng cấp, Nâng tất cả đầu/cuối buổi và mỗi " +
                "60 giây với người chăm, 10 giây với người không nghỉ tay) × " + Seeds.Length + " hạt, ngày 1–" + CadenceDays + ", bắt đầu Mùa trăng mới ngay khi được, câu " + Fishing.BaitCap + " mồi mỗi ngày và giữ cá, nhận nhiệm vụ mỗi " + CadenceQuestEvery + " giây (đo theo ngày nên chậm vài giây không đổi gì; người không nghỉ tay của test 2 giờ nhận mỗi giây). " +
                "Chạy với kiếm tiền tắt (#144, mặc định): thẻ vé tháng không hiện. Lịch bắt đầu ngày 2/1/1970, không có hội trong 28 ngày ấy (hội chạy ngày 10–19 của tháng 3, 4, 5, 6, 7, 8, 10, 11), nên số đo là trường hợp không có hội.");
            report.AppendLine();
            report.AppendLine("Thứ mới trong ngày: trạm mở (cả mở lại sau trăng mới), đoạn đất, Mùa trăng mới, thẻ giới thiệu tới lượt (tính năng mới), món mới trong sổ công thức, nấc thành tựu mới, hội. Lên cấp vườn không tính; cột cuối tính thêm để so với báo cáo 23/11.");
            report.AppendLine("\"Chuỗi không có gì mới\" là số ngày liền nhau trong ngày " + CadenceFrom + "–" + CadenceDays + " không có thứ mới nào; mỗi ô là trung vị [p10–p90] trên " + Seeds.Length + " hạt.");
            report.AppendLine();
            report.AppendLine("| Kiểu, cách mua | Chuỗi dài nhất không có gì mới | Hạt có chuỗi > " + QuietMedian + " ngày | Kể cả lên cấp vườn (như 23/11) | Trăng mới 1 / 2 / 3 / 7 (ngày) |");
            report.AppendLine("|---|---|---|---|---|");
            foreach (var group in groups)
                report.AppendLine("| " + group.Key + " | " + Spread(group, r => r.Quiet) + " | " + group.Count(r => r.Quiet > QuietMedian) + "/" + group.Count() + " | " + Spread(group, r => r.QuietWithLevels)
                    + " | " + string.Join(" / ", new[] { 1, 2, 3, NewMoon.MaxSeasons }.Select(n => Median(group, r => r.MoonDay(n)).ToString())) + " |");
            report.AppendLine();
            report.AppendLine("Test kiểm luật ≤ " + QuietMedian + " ngày (trung vị), ≤ " + QuietP90 + " ngày (p90) ở cột \"Kể cả lên cấp vườn\" cho cả sáu nhóm, và ở cột đầu cho người thong thả. Người chăm và không nghỉ tay tới trăng mới thứ " + NewMoon.MaxSeasons
                + " (trần) trước ngày " + CadenceDays + ", sau đó chỉ còn nấc thành tựu và cấp vườn: cột đầu của họ là số để chủ project quyết, không phải luật của test.");
            report.AppendLine();

            report.AppendLine("## Ngày thẻ tới lượt (trung vị [p10–p90]; > " + CadenceDays + " là chưa tới trong " + CadenceDays + " ngày)");
            report.AppendLine();
            report.AppendLine("Trước: ngày tính năng hiện ra trong vườn (rương xe đẩy, bể cá, rương tuần, vé tháng có từ ngày 1). Sau: cổng mở của thẻ (#122).");
            report.AppendLine();
            report.AppendLine("| Thẻ | " + string.Join(" | ", groups.Select(g => g.Key + " trước → sau")) + " |");
            report.AppendLine("|---" + string.Concat(groups.Select(g => "|---")) + "|");
            foreach (string id in Intros.All)
                report.AppendLine("| " + id + " | " + string.Join(" | ", groups.Select(g =>
                    Spread(g, r => r.PlainDay.TryGetValue(id, out int d) ? d : CadenceDays + 1) + " → " + Spread(g, r => r.CardDay.TryGetValue(id, out int d) ? d : CadenceDays + 1))) + " |");
            report.AppendLine();
            report.AppendLine("Số thẻ tới lượt trong ngày 1–4 (trung vị, trước → sau):");
            report.AppendLine();
            report.AppendLine("| Kiểu, cách mua | Ngày 1 | Ngày 2 | Ngày 3 | Ngày 4 |");
            report.AppendLine("|---|---|---|---|---|");
            foreach (var group in groups)
                report.AppendLine("| " + group.Key + " | " + string.Join(" | ", Enumerable(4, d => Median(group, r => r.PlainDay.Values.Count(v => v == d + 1)) + " → " + Median(group, r => r.CardDay.Values.Count(v => v == d + 1)))) + " |");
            report.AppendLine();

            report.AppendLine("## Ngày → thứ mới, hạt " + Seed);
            foreach (var group in groups)
            {
                Cadence first = group.First(r => r.Seed == Seed);
                report.AppendLine();
                report.AppendLine("### " + group.Key);
                report.AppendLine();
                report.AppendLine("| Ngày | Thứ mới |");
                report.AppendLine("|---|---|");
                for (int day = 1; day <= CadenceDays; day++) report.AppendLine(DayLine(first, day));
            }
            report.AppendLine();
            report.AppendLine("## Mỗi ngày, số hạt có thứ mới (trên " + Seeds.Length + ")");
            report.AppendLine();
            report.AppendLine("| Kiểu, cách mua | " + string.Join(" | ", Enumerable(CadenceDays - CadenceFrom + 1, d => (d + CadenceFrom).ToString())) + " |");
            report.AppendLine("|---" + string.Concat(Enumerable(CadenceDays - CadenceFrom + 2, d => "|---")) + "|");
            foreach (var group in groups)
                report.AppendLine("| " + group.Key + " | " + string.Join(" | ", Enumerable(CadenceDays - CadenceFrom + 1, d => group.Count(r => r.News[d + CadenceFrom].Count > 0).ToString())) + " |");

            System.IO.Directory.CreateDirectory("Logs");
            System.IO.File.WriteAllText("Logs/unlock-cadence.txt", report.ToString());
            TestContext.WriteLine(report.ToString());
            string text = report.ToString();

            foreach (var group in groups)
            {
                var withLevels = group.Select(r => (double)r.QuietWithLevels).ToList();
                Assert.LessOrEqual(Median(withLevels), QuietMedian, group.Key + ": at most two days in a row without a garden level or anything new, on the median\n" + text);
                Assert.LessOrEqual(Percentile(withLevels, 0.9), QuietP90, group.Key + ": and at most three at the ninetieth percentile\n" + text);
                if (group.First().Pace == Pace.Casual)
                {
                    var values = group.Select(r => (double)r.Quiet).ToList();
                    Assert.LessOrEqual(Median(values), QuietMedian, group.Key + ": at most two days in a row with nothing new even without the levels, on the median\n" + text);
                    Assert.LessOrEqual(Percentile(values, 0.9), QuietP90, group.Key + ": and at most three at the ninetieth percentile\n" + text);
                }
                foreach (Cadence run in group)
                {
                    Assert.AreEqual(Intros.WeekChestDay, run.CardDay[Intros.WeekChest], group.Key + ", seed " + run.Seed + ": the week chest card on day two\n" + text);
                    Assert.AreEqual(Intros.TrunkDay, run.CardDay[Intros.Trunk], "the trunk card on day three\n" + text);
                    Assert.IsFalse(run.CardDay.ContainsKey(Intros.MonthPass), "with monetization off (#144) the month pass card never comes\n" + text);
                    Assert.IsFalse(run.CardDay.ContainsKey(Intros.Fair), "no fair in these 28 days\n" + text);
                }
            }
        }
    }
}
