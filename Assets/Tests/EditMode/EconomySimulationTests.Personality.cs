using System;
using System.Linq;
using System.Text;
using NUnit.Framework;
using VuonTra.Game;

namespace VuonTra.Core.Tests
{
    public partial class EconomySimulationTests
    {
        enum StrokeMode { Plain, Asset, AlwaysFavourite }

        const double MostPacingShift = 0.03;

        PersonalityBook likes;
        StrokeMode strokeMode, strokeModeForRuns = StrokeMode.Asset;
        int favouriteStrokes;

        void ResetStrokes()
        {
            strokeMode = strokeModeForRuns;
            favouriteStrokes = 0;
        }

        PetResult Stroke(Rabbit r)
        {
            switch (strokeMode)
            {
                case StrokeMode.Plain:
                    return Rabbits.Pet(s, r, Today);
                case StrokeMode.Asset:
                    bool loved = likes.LovesStation(defs, r);
                    PetResult pet = Personalities.Pet(defs, likes, s, r, Today);
                    if (loved && pet.FirstToday) favouriteStrokes++;
                    return pet;
                default:
                    PetResult always = Rabbits.Pet(s, r, Today);
                    if (!always.FirstToday) return always;
                    favouriteStrokes++;
                    BondStep extra = Rabbits.AddBond(s, r, Personalities.FavouritePoints);
                    always.LineUnlocked |= extra.LineUnlocked;
                    always.GiftGiven |= extra.GiftGiven;
                    always.TenHearts |= extra.TenHearts;
                    return always;
            }
        }

        sealed class LikeWeek
        {
            public int Seed, MostAffection, FavouriteStrokes, LevelEnd;
            public double Income7, Level10;
            public Week Week;
            public double DaysToTenHearts => MostAffection <= 0 ? double.PositiveInfinity : 7d * Rabbits.PointsFor(Rabbits.HeartMax) / MostAffection;
        }

        LikeWeek LikeRun(int seed, StrokeMode mode)
        {
            strokeModeForRuns = mode;
            try
            {
                Week w = CasualWeek(false, false, true, seed);
                return new LikeWeek
                {
                    Seed = seed, Week = w, Income7 = w.Income7, Level10 = w.Level10, LevelEnd = w.LevelEnd,
                    MostAffection = s.Rabbits.Count == 0 ? 0 : s.Rabbits.Max(r => r.Affection), FavouriteStrokes = favouriteStrokes,
                };
            }
            finally
            {
                strokeModeForRuns = StrokeMode.Asset;
            }
        }

        static double Shift(double before, double after) => before == 0 ? 0 : after / before - 1d;

        static string Percent(double shift) => (shift >= 0 ? "+" : "") + Math.Round(shift * 100d, 2) + " %";

        [Test, Category("Slow"), Timeout(3600000)]
        public void Strokes_At_The_Favourite_Station_Move_Ten_Hearts_And_Day_Seven_Income_By_No_More_Than_Three_Percent()
        {
            var plain = new LikeWeek[Seeds.Length];
            var asset = new LikeWeek[Seeds.Length];
            var always = new LikeWeek[Seeds.Length];
            for (int i = 0; i < Seeds.Length; i++)
            {
                plain[i] = LikeRun(Seeds[i], StrokeMode.Plain);
                asset[i] = LikeRun(Seeds[i], StrokeMode.Asset);
                always[i] = LikeRun(Seeds[i], StrokeMode.AlwaysFavourite);
            }
            double[] incomeShift = Seeds.Select((_, i) => Shift(plain[i].Income7, asset[i].Income7)).ToArray();
            double[] heartShift = Seeds.Select((_, i) => Shift(asset[i].DaysToTenHearts, plain[i].DaysToTenHearts)).ToArray();
            double[] incomeBound = Seeds.Select((_, i) => Shift(plain[i].Income7, always[i].Income7)).ToArray();
            double[] heartBound = Seeds.Select((_, i) => Shift(always[i].DaysToTenHearts, plain[i].DaysToTenHearts)).ToArray();

            var report = new StringBuilder();
            report.AppendLine("# Món thích của từng loài (#176)");
            report.AppendLine();
            report.AppendLine("Tuần chơi thong thả có câu cá của EconomySimulationTests trên " + Seeds.Length + " hạt: mỗi ngày vuốt thỏ ở trạm thu nhiều nhất và cho nó ăn hết cá. "
                + "Không món thích: vuốt lần đầu trong ngày +" + Rabbits.PetPoints + " điểm. Theo asset: +" + (Rabbits.PetPoints + Personalities.FavouritePoints) + " khi thỏ đứng ở trạm món thích của loài. "
                + "Luôn ở món thích: mọi lần vuốt đầu ngày đều được +" + Personalities.FavouritePoints + " (cận trên). 10 tim = " + Rabbits.PointsFor(Rabbits.HeartMax)
                + " điểm; số ngày tới 10 tim = 7 × " + Rabbits.PointsFor(Rabbits.HeartMax) + " / điểm cao nhất sau 7 ngày. Dương = sớm hơn / nhiều hơn.");
            report.AppendLine();
            report.AppendLine("| Hạt | Thu/giây ngày 7: không → asset | Đổi | Điểm thân cao nhất: không → asset → luôn | Ngày tới 10 tim: không → asset | Sớm hơn (asset) | Sớm hơn (luôn) | Lần vuốt được +2 (asset / luôn) |");
            report.AppendLine("|---|---|---|---|---|---|---|---|");
            for (int i = 0; i < Seeds.Length; i++)
                report.AppendLine("| " + Seeds[i] + " | " + new BigNum(plain[i].Income7).ToShortString() + " → " + new BigNum(asset[i].Income7).ToShortString() + " | " + Percent(incomeShift[i])
                    + " | " + plain[i].MostAffection + " → " + asset[i].MostAffection + " → " + always[i].MostAffection
                    + " | " + Math.Round(plain[i].DaysToTenHearts) + " → " + Math.Round(asset[i].DaysToTenHearts) + " | " + Percent(heartShift[i]) + " | " + Percent(heartBound[i])
                    + " | " + asset[i].FavouriteStrokes + " / " + always[i].FavouriteStrokes + " |");
            report.AppendLine("| **trung vị** | " + new BigNum(Median(plain, w => w.Income7)).ToShortString() + " → " + new BigNum(Median(asset, w => w.Income7)).ToShortString() + " | " + Percent(Median(incomeShift))
                + " | " + Median(plain, w => w.MostAffection) + " → " + Median(asset, w => w.MostAffection) + " → " + Median(always, w => w.MostAffection)
                + " | " + Math.Round(Median(plain, w => w.DaysToTenHearts)) + " → " + Math.Round(Median(asset, w => w.DaysToTenHearts)) + " | " + Percent(Median(heartShift)) + " | " + Percent(Median(heartBound))
                + " | " + Median(asset, w => w.FavouriteStrokes) + " / " + Median(always, w => w.FavouriteStrokes) + " |");
            report.AppendLine();
            report.AppendLine("Lớn nhất trên một hạt: thu nhập ngày 7 " + Percent(incomeShift.Max(Math.Abs)) + " (asset), " + Percent(incomeBound.Max(Math.Abs)) + " (luôn); 10 tim sớm hơn "
                + Percent(heartShift.Max()) + " (asset), " + Percent(heartBound.Max()) + " (luôn).");
            report.AppendLine("Chỉ vuốt, không cho ăn cá, luôn ở món thích: 10 tim sớm hơn " + Percent(Shift(Rabbits.PointsFor(Rabbits.HeartMax) / (double)(Rabbits.PetPoints + Personalities.FavouritePoints),
                Rabbits.PointsFor(Rabbits.HeartMax) / (double)Rabbits.PetPoints)) + " (tính tay, không mô phỏng).");
            string text = report.ToString();
            System.IO.Directory.CreateDirectory("Logs");
            System.IO.File.WriteAllText("Logs/economy-personality.txt", text);
            TestContext.WriteLine(text);

            HardRules(asset.Select(w => w.Week).ToArray(), text);
            Assert.LessOrEqual(Math.Abs(Median(incomeShift)), MostPacingShift, "day seven's income moves no more than 3 % on the median\n" + text);
            Assert.LessOrEqual(Math.Abs(Median(heartShift)), MostPacingShift, "ten hearts come no more than 3 % sooner on the median\n" + text);
        }
    }
}
