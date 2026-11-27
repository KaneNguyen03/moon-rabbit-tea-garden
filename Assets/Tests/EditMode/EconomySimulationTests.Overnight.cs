using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using VuonTra.Game;

namespace VuonTra.Core.Tests
{
    public partial class EconomySimulationTests
    {
        const long NightEpoch = 20601L * 86400L;
        const double NightSitting = 15 * 60, NightShortSitting = 10 * 60;
        const double NightGapLeast = 4 * 3600, NightGapMost = 10 * 3600;

        bool nights;
        int storiesTold, storiesTaken, cloversNights, goldenNights, levelGoldNights, leavesNights, bondsNights, strangersNights;
        readonly Dictionary<long, int> cloversByWeek = new Dictionary<long, int>();
        readonly List<long> strangerDays = new List<long>();
        System.Random gapRng;

        void ResetNights(int seed)
        {
            nights = false;
            storiesTold = storiesTaken = cloversNights = goldenNights = levelGoldNights = leavesNights = bondsNights = strangersNights = 0;
            cloversByWeek.Clear();
            strangerDays.Clear();
            gapRng = new System.Random(unchecked(seed * 31 + 174));
        }

        void TellStories(long left, long now) => storiesTold += Overnight.Roll(s, left, now).Count;

        void TakeStories()
        {
            int golden = s.GoldenCarrots, rabbits = s.Rabbits.Count, level = Garden.Level(s);
            long week = Overnight.WeekOf(NightEpoch + (long)clock);
            foreach (Story story in new List<Story>(s.Overnight.Waiting))
            {
                StoryClaim claim = Overnight.Claim(s, story, "D");
                if (!claim.Ok) continue;
                storiesTaken++;
                cloversNights += claim.Clovers;
                leavesNights += claim.Leaves;
                if (claim.Bond > 0) bondsNights++;
                cloversByWeek.TryGetValue(week, out int n);
                cloversByWeek[week] = n + claim.Clovers;
                if (claim.Arrived == null) continue;
                strangersNights++;
                strangerDays.Add(Today);
            }
            int levelGold = Shop.GoldenForLevels(Math.Max(level, NewMoon.LevelPassed(s)), Garden.Level(s));
            levelGoldNights += levelGold;
            goldenNights += s.GoldenCarrots - golden - levelGold;
            if (s.Rabbits.Count == rabbits) return;
            Rabbits.AutoAssign(defs, s);
            mods = Modifiers.From(ups, s);
        }

        sealed class NightWeek
        {
            public int Seed;
            public double Level10, Plots12, ShortestGap = double.MaxValue, LongestGap;
            public int Told, Taken, Clovers, Golden, LevelGold, Leaves, Bonds, Strangers, MostCloversInAWeek, LevelEnd, Rabbits, GoldenCalls;
            public long ClosestStrangers = long.MaxValue;
        }

        double NightGap(double least, double most) => least + gapRng.NextDouble() * (most - least);

        NightWeek ThreeVisitWeek(int seed, bool tell)
        {
            Reset(false, seed);
            nights = tell;
            var w = new NightWeek { Seed = seed };
            double awayInADay = Day - NightSitting - 2 * NightShortSitting;
            for (int day = 1; day <= 7; day++)
            {
                double first = NightGap(NightGapLeast, NightGapMost);
                double left = awayInADay - first;
                double second = NightGap(Math.Max(NightGapLeast, left - NightGapMost), Math.Min(NightGapMost, left - NightGapLeast));
                double third = left - second;
                foreach (double gap in new[] { first, second, third })
                {
                    w.ShortestGap = Math.Min(w.ShortestGap, gap);
                    w.LongestGap = Math.Max(w.LongestGap, gap);
                }
                Play(NightSitting);
                Away(first);
                Play(NightShortSitting);
                Away(second);
                Play(NightShortSitting);
                Away(third);
            }
            w.Level10 = At(levelAt, 10);
            w.Plots12 = At(plotsAt, 12);
            w.Told = storiesTold;
            w.Taken = storiesTaken;
            w.Clovers = cloversNights;
            w.Golden = goldenNights;
            w.LevelGold = levelGoldNights;
            w.Leaves = leavesNights;
            w.Bonds = bondsNights;
            w.Strangers = strangersNights;
            foreach (int n in cloversByWeek.Values) w.MostCloversInAWeek = Math.Max(w.MostCloversInAWeek, n);
            for (int i = 1; i < strangerDays.Count; i++) w.ClosestStrangers = Math.Min(w.ClosestStrangers, strangerDays[i] - strangerDays[i - 1]);
            w.LevelEnd = Garden.Level(s);
            w.Rabbits = s.Rabbits.Count;
            w.GoldenCalls = goldenCalls + visitorsBought;
            return w;
        }

        [Test, Category("Slow"), Timeout(3600000)]
        public void Three_Visits_A_Day_With_Overnight_Stories_Add_At_Most_Seven_Clovers_A_Week_No_Gold_And_Bring_Level_Ten_And_Twelve_Plots_No_More_Than_A_Tenth_Sooner()
        {
            var without = new NightWeek[Seeds.Length];
            var with = new NightWeek[Seeds.Length];
            for (int i = 0; i < Seeds.Length; i++)
            {
                without[i] = ThreeVisitWeek(Seeds[i], false);
                with[i] = ThreeVisitWeek(Seeds[i], true);
            }
            double tenWithout = Median(without, w => Late(w.Level10)), tenWith = Median(with, w => Late(w.Level10));
            double plotsWithout = Median(without, w => Late(w.Plots12)), plotsWith = Median(with, w => Late(w.Plots12));

            var report = new StringBuilder();
            report.AppendLine("# Chuyện đêm qua (#174)");
            report.AppendLine();
            report.AppendLine("Người chơi thong thả của EconomySimulationTests, 7 ngày, mỗi ngày 3 lần ghé (15, 10 và 10 phút) cách nhau " + NightGapLeast / 3600 + "–" + NightGapMost / 3600
                + " giờ (ngẫu nhiên theo hạt), trên " + Seeds.Length + " hạt; không và có chuyện \"Đêm qua…\", chạm nhận hết chuyện ở đầu mỗi lần ghé. Vắng ≥ " + Overnight.MinSeconds / 60 + " phút: 1 chuyện, ≥ "
                + Overnight.LongNightSeconds / 3600 + " giờ: 2. Trọng số: lá " + Overnight.LeavesWeight + ", cỏ " + Overnight.CloverWeight + ", thỏ lạ " + Overnight.StrangerWeight + ", thân nhau " + Overnight.BondWeight
                + ", ngủ gật " + Overnight.DozeWeight + "; trần cỏ " + Overnight.WeekCloverCap + "/tuần, thỏ lạ bậc ≤ " + Overnight.StrangerTopTier + " mỗi " + Overnight.StrangerEveryDays + " ngày.");
            report.AppendLine();
            report.AppendLine("| Hạt | Cấp 10 (không → có) | 12 ô đất (không → có) | Cấp hết ngày 7 | Thỏ ngày 7 | Gọi vàng + thỏ ghé tiệm | Chuyện kể / nhận | Cỏ | Cỏ nhiều nhất 1 tuần | Vàng từ chuyện | Vàng cấp vườn tới sớm lúc nhận | Lá | Thân nhau | Thỏ lạ | Khoảng vắng ngắn / dài nhất |");
            report.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|");
            for (int i = 0; i < Seeds.Length; i++)
            {
                NightWeek a = without[i], b = with[i];
                report.AppendLine("| " + Seeds[i] + " | " + Clock(a.Level10) + " → " + Clock(b.Level10) + " | " + Clock(a.Plots12) + " → " + Clock(b.Plots12) + " | " + a.LevelEnd + " → " + b.LevelEnd
                    + " | " + a.Rabbits + " → " + b.Rabbits + " | " + a.GoldenCalls + " → " + b.GoldenCalls + " | " + b.Told + " / " + b.Taken + " | " + b.Clovers + " | " + b.MostCloversInAWeek + " | " + b.Golden + " | " + b.LevelGold
                    + " | " + b.Leaves + " | " + b.Bonds + " | " + b.Strangers + " | " + Math.Round(b.ShortestGap / 3600, 1) + " / " + Math.Round(b.LongestGap / 3600, 1) + " giờ |");
            }
            report.AppendLine("| **trung vị** | " + Clock(tenWithout) + " → " + Clock(tenWith) + " | " + Clock(plotsWithout) + " → " + Clock(plotsWith) + " | " + Median(without, w => w.LevelEnd) + " → " + Median(with, w => w.LevelEnd)
                + " | " + Median(without, w => w.Rabbits) + " → " + Median(with, w => w.Rabbits) + " | " + Median(without, w => w.GoldenCalls) + " → " + Median(with, w => w.GoldenCalls)
                + " | " + Median(with, w => w.Told) + " / " + Median(with, w => w.Taken) + " | " + Median(with, w => w.Clovers) + " | " + Median(with, w => w.MostCloversInAWeek) + " | " + Median(with, w => w.Golden) + " | " + Median(with, w => w.LevelGold)
                + " | " + Median(with, w => w.Leaves) + " | " + Median(with, w => w.Bonds) + " | " + Median(with, w => w.Strangers) + " | |");
            report.AppendLine();
            report.AppendLine("Vàng từ chuyện là vàng do chính chuyện trả. Cột kế bên là vàng mốc cấp vườn (mỗi mốc 5 cấp, một lần) rơi đúng lúc nhận chuyện vì thỏ lạ cho điểm vườn: vàng đó vẫn đến khi vườn lên cấp, chuyện chỉ làm nó tới sớm. Gọi vàng + thỏ ghé tiệm cả tuần (không → có): trung vị "
                + Median(without, w => w.GoldenCalls) + " → " + Median(with, w => w.GoldenCalls) + ".");
            report.AppendLine("Cấp vườn 10: " + Math.Round(100d * (1d - tenWith / tenWithout), 1) + " % sớm hơn (trần 10 %). 12 ô đất: " + (plotsWithout >= double.MaxValue ? "không đạt trong tuần" : Math.Round(100d * (1d - plotsWith / plotsWithout), 1) + " % sớm hơn (trần 10 %)."));
            System.IO.Directory.CreateDirectory("Logs");
            System.IO.File.WriteAllText("Logs/economy-overnight.txt", report.ToString());
            TestContext.WriteLine(report.ToString());
            string text = report.ToString();

            Always(with, w => w.ShortestGap >= NightGapLeast - 1 && w.LongestGap <= NightGapMost + 1, "every gap between visits is four to ten hours", text);
            Always(with, w => w.Told > 0 && w.Taken > 0, "stories are told and taken", text);
            Always(with, w => w.Clovers <= Overnight.WeekCloverCap && w.MostCloversInAWeek <= Overnight.WeekCloverCap, "at most seven clovers a week from the stories", text);
            Always(with, w => w.Golden == 0, "no golden carrots from the stories themselves", text);
            Assert.LessOrEqual(Median(with, w => w.GoldenCalls), Median(without, w => w.GoldenCalls), "nor more golden calls in the week, on the median\n" + text);
            Always(with, w => w.ClosestStrangers >= Overnight.StrangerEveryDays, "a stranger at the gate at most once in three days", text);
            Always(without, w => w.Told == 0 && w.Clovers == 0, "nothing without the stories", text);
            Assert.GreaterOrEqual(tenWith, 0.9 * tenWithout, "garden level 10 no more than 10 % sooner, on the median\n" + text);
            Assert.GreaterOrEqual(plotsWith, 0.9 * plotsWithout, "twelve plots no more than 10 % sooner, on the median\n" + text);
        }

        [Test, Category("Slow"), Timeout(3600000)]
        public void The_Shipped_Casual_Week_Keeps_The_Hard_Pacing_Rules_With_Overnight_Stories()
        {
            var weeks = new Week[Seeds.Length];
            var clovers = new int[Seeds.Length];
            var golden = new int[Seeds.Length];
            for (int i = 0; i < Seeds.Length; i++)
            {
                weeks[i] = CasualWeek(false, false, false, Seeds[i], tellNights: true);
                clovers[i] = cloversNights;
                golden[i] = goldenNights;
            }
            Week[] before = OverSeeds(false, false, false);
            var report = new StringBuilder();
            report.AppendLine("# Chuyện đêm qua trong tuần chơi thong thả có sẵn (#174)");
            report.AppendLine();
            report.AppendLine("Cùng tuần chơi với EconomySimulationTests (chơi 20 phút, vắng 8 giờ, chơi 10 phút, vắng tới hết ngày), có chuyện \"Đêm qua…\", chạm nhận ở đầu mỗi buổi.");
            report.AppendLine();
            report.AppendLine("| Cách chơi | Cấp vườn 5 | Cấp vườn 10 | Cấp hết ngày 1 | 12 ô đất | Cấp hết ngày 7 | Ô đất ngày 7 | Thỏ ngày 7 | Lượt gọi vàng + thỏ ghé tiệm | Vàng từ video | Lượt thêm từ video | ×2 miễn phí % | ×2 video % |");
            report.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|---|---|");
            report.AppendLine(Row("Không có chuyện", before));
            report.AppendLine(Row("Có chuyện", weeks));
            report.AppendLine();
            report.Append(SeedTable(weeks));
            report.AppendLine("Cỏ từ chuyện, từng hạt: " + string.Join(", ", clovers) + "; vàng: " + string.Join(", ", golden) + ".");
            System.IO.Directory.CreateDirectory("Logs");
            System.IO.File.WriteAllText("Logs/economy-overnight-casual.txt", report.ToString());
            TestContext.WriteLine(report.ToString());

            HardRules(weeks, report.ToString());
            Holds(weeks, w => w.Golden >= 2 && w.Golden <= 6, "a casual player should call with golden carrots a few times a week, but not every day (at most six)", report.ToString());
            foreach (int n in clovers) Assert.LessOrEqual(n, Overnight.WeekCloverCap, report.ToString());
            foreach (int n in golden) Assert.AreEqual(0, n, report.ToString());
        }
    }
}
