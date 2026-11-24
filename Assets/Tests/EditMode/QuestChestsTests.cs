using System.Collections.Generic;
using NUnit.Framework;

namespace VuonTra.Core.Tests
{
    public class QuestChestsTests
    {
        List<StationDef> defs;
        List<QuestDef> quests;
        GameState s;

        static readonly long Monday = GiftCodes.Day(2026, 11, 23);

        [SetUp]
        public void SetUp()
        {
            defs = Fixture.Defs();
            s = GameState.NewGame(defs);
            s.Profile.Code = Fixture.CodeFor(7);
            quests = new List<QuestDef>();
            foreach (string id in new[] { "ngay-cham", "ngay-nang", "ngay-thu", "ngay-don", "ngay-anh" })
                quests.Add(new QuestDef { Id = id, Metric = QuestMetric.Taps, Target = 1, Daily = true, RewardCarrots = 1 });
            quests.Add(new QuestDef { Id = "mo-1", Metric = QuestMetric.Taps, Target = 1, RewardCarrots = 1 });
        }

        void Open(long day)
        {
            Seasons.Clamp(s, day);
            Quests.RollDaily(quests, defs, s, s.MaxDaySeen, Modifiers.None);
        }

        int ClaimDailies(int count)
        {
            s.TotalTaps = s.DailyTapsBase + 1;
            int claimed = 0;
            foreach (QuestDef q in quests)
                if (q.Daily && claimed < count && Quests.TryClaim(q, defs, s, Modifiers.None, out _)) claimed++;
            return claimed;
        }

        void PlayFullDay(long day)
        {
            Open(day);
            ClaimDailies(5);
            Assert.IsNotNull(QuestChests.OpenBox(quests, defs, s, Modifiers.None));
        }

        [Test]
        public void The_Week_Starts_On_Monday()
        {
            Assert.AreEqual(QuestChests.WeekOf(Monday), QuestChests.WeekOf(Monday + 6), "Monday to Sunday is one week");
            Assert.AreEqual(QuestChests.WeekOf(Monday) + 1, QuestChests.WeekOf(Monday + 7), "the next Monday starts the next");
            Assert.AreEqual(QuestChests.WeekOf(Monday) - 1, QuestChests.WeekOf(Monday - 1), "Sunday belongs to the week before");
            Assert.AreEqual(Monday, QuestChests.MondayOf(QuestChests.WeekOf(Monday + 3)));
        }

        [Test]
        public void The_Daily_Box_Opens_Once_A_Day_When_All_Five_Dailies_Are_Claimed()
        {
            Open(Monday);
            Assert.AreEqual(5, QuestChests.DailyTotal(quests, s));
            ClaimDailies(3);
            Assert.AreEqual(3, QuestChests.DailyDone(quests, s));
            Assert.IsFalse(QuestChests.BoxReady(quests, s), "three of five is not the whole set");
            Assert.IsNull(QuestChests.OpenBox(quests, defs, s, Modifiers.None));

            ClaimDailies(5);
            Assert.AreEqual(5, QuestChests.DailyDone(quests, s));
            Assert.IsTrue(QuestChests.BoxReady(quests, s));
            BoxPrize[] prizes = QuestChests.OpenBox(quests, defs, s, Modifiers.None);
            Assert.IsNotNull(prizes);
            Assert.AreEqual(QuestChests.BoxPrizes, prizes.Length);
            Assert.IsTrue(QuestChests.BoxOpened(s));
            Assert.IsFalse(QuestChests.BoxReady(quests, s));
            Assert.IsNull(QuestChests.OpenBox(quests, defs, s, Modifiers.None), "once a day");

            Open(Monday + 1);
            Assert.IsFalse(QuestChests.BoxOpened(s), "a new day, a new box");
            Assert.IsFalse(QuestChests.BoxReady(quests, s));
            ClaimDailies(5);
            Assert.IsNotNull(QuestChests.OpenBox(quests, defs, s, Modifiers.None));
        }

        [Test]
        public void The_Box_Pays_What_It_Shows()
        {
            Open(Monday);
            ClaimDailies(5);
            s.GardenXp = Garden.XpForLevel(MoonJars.ShowLevel);
            int golden = s.GoldenCarrots, clovers = s.Clovers, jars = s.MoonJars;
            BigNum coins = s.Coins;
            BoxPrize[] prizes = QuestChests.OpenBox(quests, defs, s, Modifiers.None);
            int wantGolden = 0, wantClovers = 0, wantJars = 0;
            BigNum wantCoins = BigNum.Zero;
            foreach (BoxPrize p in prizes)
                switch (p.Kind)
                {
                    case BoxPrizeKind.Golden: wantGolden += p.Amount; break;
                    case BoxPrizeKind.Clover: wantClovers += p.Amount; break;
                    case BoxPrizeKind.Jar: wantJars += p.Amount; break;
                    case BoxPrizeKind.Income: wantCoins += p.Coins; Assert.IsTrue(p.Coins > BigNum.Zero); break;
                }
            Assert.AreEqual(golden + wantGolden, s.GoldenCarrots);
            Assert.AreEqual(clovers + wantClovers, s.Clovers);
            Assert.AreEqual(jars + wantJars, s.MoonJars);
            Assert.AreEqual((coins + wantCoins).ToDouble(), s.Coins.ToDouble(), 1e-6);
        }

        [Test]
        public void The_Draw_Repeats_For_The_Same_Seed_Never_Holds_Two_Golden_And_Follows_The_Weights()
        {
            string code = Fixture.CodeFor(3);
            CollectionAssert.AreEqual(QuestChests.Draw(Monday, code), QuestChests.Draw(Monday, code), "the same day and garden draw the same box");
            bool differs = false;
            for (int d = 1; d < 30 && !differs; d++) differs = !Same(QuestChests.Draw(Monday + d, code), QuestChests.Draw(Monday, code));
            Assert.IsTrue(differs, "another day draws another box");

            var counts = new Dictionary<BoxSlot, int>();
            const int Boxes = 20000;
            for (int d = 0; d < Boxes; d++)
            {
                BoxSlot[] box = QuestChests.Draw(Monday + d, code);
                Assert.AreEqual(QuestChests.BoxPrizes, box.Length);
                int goldenSlots = 0;
                foreach (BoxSlot slot in box)
                {
                    if (slot.Kind == BoxPrizeKind.Golden) goldenSlots++;
                    counts.TryGetValue(slot, out int n);
                    counts[slot] = n + 1;
                }
                Assert.LessOrEqual(goldenSlots, 1, "never two golden carrot prizes in one box");
            }
            counts.TryGetValue(QuestChests.Table[0], out int income);
            counts.TryGetValue(QuestChests.Table[1], out int clover);
            Assert.Greater(income, clover, "300 seconds of income is the commonest prize");
            counts.TryGetValue(QuestChests.Table[3], out int golden2);
            counts.TryGetValue(QuestChests.Table[4], out int golden5);
            Assert.AreEqual(3d, golden2 / (double)golden5, 0.4, "two golden carrots come three times as often as five");
            double firstIncome = 0;
            for (int d = 0; d < Boxes; d++) if (QuestChests.Draw(Monday + d, code)[0] == QuestChests.Table[0]) firstIncome++;
            Assert.AreEqual(0.40, firstIncome / Boxes, 0.02, "the first prize follows the table's weights");
        }

        static bool Same(BoxSlot[] a, BoxSlot[] b)
        {
            for (int i = 0; i < a.Length; i++) if (a[i] != b[i]) return false;
            return true;
        }

        [Test]
        public void Points_Are_Ten_Per_Daily_And_Ten_For_The_Box_And_Go_Back_To_Zero_On_Monday()
        {
            Open(Monday + 5);
            ClaimDailies(5);
            Assert.AreEqual(5 * QuestChests.QuestPoints, QuestChests.Points(s, s.DailyDay));
            QuestChests.OpenBox(quests, defs, s, Modifiers.None);
            Assert.AreEqual(5 * QuestChests.QuestPoints + QuestChests.BoxPoints, QuestChests.Points(s, s.DailyDay));
            s.TotalTaps = s.DailyTapsBase + 1;
            Assert.IsTrue(Quests.TryClaim(quests[5], defs, s, Modifiers.None, out _));
            Assert.AreEqual(60, QuestChests.Points(s, s.DailyDay), "achievements give no week points");

            PlayFullDay(Monday + 6);
            Assert.AreEqual(120, QuestChests.Points(s, s.DailyDay), "Sunday is still the same week");
            Open(Monday + 7);
            Assert.AreEqual(0, QuestChests.Points(s, s.DailyDay), "the new week starts from nothing");
            Assert.AreEqual(0, s.WeekPoints);
        }

        [Test]
        public void A_Whole_Week_Reaches_Every_Chest_And_Each_Pays_Once()
        {
            for (int d = 0; d < 7; d++) PlayFullDay(Monday + d);
            Assert.AreEqual(QuestChests.MaxPoints, QuestChests.Points(s, s.DailyDay));
            Assert.AreEqual(420, QuestChests.MaxPoints);
            for (int i = 0; i < QuestChests.Chests.Length; i++) Assert.IsTrue(QuestChests.ChestReady(s, i, s.DailyDay));

            s.GardenXp = Garden.XpForLevel(MoonJars.ShowLevel);
            s.MoonJars = 0;
            int jars = s.MoonJars, clovers = s.Clovers, golden = s.GoldenCarrots, rabbits = s.Rabbits.Count;
            Assert.IsTrue(QuestChests.ClaimChest(s, 0, s.DailyDay, "A", 0.1, out Rabbit none));
            Assert.IsNull(none);
            Assert.AreEqual(jars + 2, s.MoonJars);
            Assert.AreEqual(clovers + 2, s.Clovers);
            Assert.IsFalse(QuestChests.ClaimChest(s, 0, s.DailyDay, "A", 0.1, out _), "a chest pays once");
            Assert.IsTrue(QuestChests.ClaimChest(s, 1, s.DailyDay, "B", 0.1, out _));
            Assert.AreEqual(golden + 5, s.GoldenCarrots, "the 250 chest pays five golden carrots");
            Assert.IsTrue(QuestChests.ClaimChest(s, 2, s.DailyDay, "C", 0.1, out Rabbit blue));
            Assert.AreEqual(golden + 15, s.GoldenCarrots, "and the 400 chest ten more");
            Assert.IsNotNull(blue);
            Assert.AreEqual(2, blue.Tier, "a Blue rabbit, the third tier");
            Assert.AreEqual(rabbits + 1, s.Rabbits.Count);
            Assert.IsFalse(QuestChests.ChestReady(s, 2, s.DailyDay));
        }

        [Test]
        public void A_Chest_Waits_For_Its_Points()
        {
            Open(Monday);
            ClaimDailies(5);
            Assert.IsFalse(QuestChests.ChestReady(s, 0, s.DailyDay));
            Assert.IsFalse(QuestChests.ClaimChest(s, 0, s.DailyDay, "A", 0d, out _));
            PlayFullDay(Monday + 1);
            Assert.IsTrue(QuestChests.ChestReady(s, 0, s.DailyDay));
            Assert.IsFalse(QuestChests.ChestReady(s, 1, s.DailyDay));
        }

        [Test]
        public void A_Clock_Set_Back_Reopens_Neither_The_Box_Nor_A_Chest()
        {
            PlayFullDay(Monday + 6);
            PlayFullDay(Monday + 5 + 7);
            Assert.AreEqual(60, QuestChests.Points(s, s.DailyDay));
            PlayFullDay(Monday + 6 + 7);
            Assert.IsTrue(QuestChests.ClaimChest(s, 0, s.DailyDay, "A", 0d, out _));

            Open(Monday + 6);
            Assert.AreEqual(Monday + 6 + 7, s.DailyDay, "the day is held at the latest seen");
            Assert.IsFalse(QuestChests.BoxReady(quests, s));
            Assert.IsNull(QuestChests.OpenBox(quests, defs, s, Modifiers.None));
            Assert.IsFalse(QuestChests.ChestReady(s, 0, s.DailyDay));
            Assert.AreEqual(120, QuestChests.Points(s, s.DailyDay));
            Assert.AreEqual(0, ClaimDailies(5), "the dailies stay claimed");
            Assert.IsFalse(QuestChests.ClaimChest(s, 0, Monday, "A", 0d, out _), "an old day asks about a week already gone");
        }

        [Test]
        public void Chests_Left_At_The_Week_Change_Become_Letters_Kept_Seven_Days()
        {
            for (int d = 0; d < 5; d++) PlayFullDay(Monday + d);
            Assert.AreEqual(300, QuestChests.Points(s, s.DailyDay));
            Assert.IsTrue(QuestChests.ClaimChest(s, 0, s.DailyDay, "A", 0d, out _));

            Open(Monday + 9);
            Assert.AreEqual(1, s.Letters.Count, "the 250 chest was left; the 400 was never reached");
            Letter letter = s.Letters[0];
            Assert.AreEqual(LetterKind.WeekChest, letter.Kind);
            Assert.AreEqual(1, letter.Chest);
            Assert.AreEqual(QuestChests.Chests[1].Golden, letter.Golden);
            Assert.AreEqual(QuestChests.ChestKeepDays, Mailbox.DaysLeft(letter, Monday + 9));
            Assert.AreEqual(0, QuestChests.Points(s, s.DailyDay));

            Open(Monday + 10);
            Assert.AreEqual(1, s.Letters.Count, "one letter per chest");
            Assert.AreEqual(1, Mailbox.Open(s, Monday + 9 + QuestChests.ChestKeepDays).Count, "the seventh day still counts");
            Assert.AreEqual(0, Mailbox.Open(s, Monday + 9 + QuestChests.ChestKeepDays + 1).Count);
        }

        [Test]
        public void A_Chest_Letter_Pays_The_Chest_Including_Its_Rabbit()
        {
            for (int d = 0; d < 7; d++) PlayFullDay(Monday + d);
            Open(Monday + 7);
            Assert.AreEqual(3, s.Letters.Count);
            s.GardenXp = Garden.XpForLevel(MoonJars.ShowLevel);
            s.MoonJars = 0;
            int golden = s.GoldenCarrots, rabbits = s.Rabbits.Count, jars = s.MoonJars, clovers = s.Clovers;
            Letter last = s.Letters.Find(l => l.Chest == 2);
            Assert.AreEqual(2, last.RabbitTier);
            Assert.IsNotNull(Mailbox.Claim(s, last.Id, Monday + 8, "Lam", 0.3, out Rabbit rabbit));
            Assert.IsNotNull(rabbit);
            Assert.AreEqual(2, rabbit.Tier);
            Assert.AreEqual(rabbits + 1, s.Rabbits.Count);
            Assert.AreEqual(golden + 10, s.GoldenCarrots);
            Letter first = s.Letters.Find(l => l.Chest == 0);
            Assert.AreEqual(2, first.Jars);
            Assert.IsNotNull(Mailbox.Claim(s, first.Id, Monday + 8));
            Assert.AreEqual(jars + 2, s.MoonJars);
            Assert.AreEqual(clovers + 2, s.Clovers);
        }

        [Test]
        public void Jars_That_Do_Not_Fit_Come_As_Clovers()
        {
            s.GardenXp = Garden.XpForLevel(MoonJars.ShowLevel);
            s.MoonJars = MoonJars.Cap - 1;
            Assert.AreEqual(1, QuestChests.GiveJars(s, 2));
            Assert.AreEqual(MoonJars.Cap, s.MoonJars);
            Assert.AreEqual(1, s.Clovers);
            s.GardenXp = 0;
            s.MoonJars = 0;
            Assert.AreEqual(0, QuestChests.GiveJars(s, 2), "before the jars are shown they come as clovers");
            Assert.AreEqual(3, s.Clovers);
        }

        [Test]
        public void The_Week_Round_Trips_And_An_Old_Save_Starts_With_No_Points()
        {
            for (int d = 0; d < 2; d++) PlayFullDay(Monday + d);
            Assert.IsTrue(QuestChests.ClaimChest(s, 0, s.DailyDay, "A", 0d, out _));
            Mailbox.AddChest(s, 1, 99, Monday);
            GameState r = SaveCodec.Decode(defs, SaveCodec.Encode(defs, s)).State;
            Assert.AreEqual(s.WeekIndex, r.WeekIndex);
            Assert.AreEqual(120, r.WeekPoints);
            Assert.AreEqual(s.WeekChestsClaimed, r.WeekChestsClaimed);
            Assert.AreEqual(Monday + 1, r.BoxDay);
            Letter letter = r.Letters.Find(l => l.Kind == LetterKind.WeekChest);
            Assert.IsNotNull(letter);
            Assert.AreEqual(1, letter.Chest);

            GameState old = SaveCodec.Decode(defs, "{\"version\":14,\"stations\":[]}").State;
            Assert.AreEqual(0, old.WeekPoints);
            Assert.AreEqual(-1, old.WeekIndex);
            Assert.AreEqual(0, old.WeekChestsClaimed);
            Assert.AreEqual(-1, old.BoxDay);

            GameState bad = SaveCodec.Decode(defs, "{\"version\":14,\"stations\":[],\"week\":{\"index\":5,\"points\":-40,\"chests\":99,\"boxDay\":\"x\"},"
                + "\"mail\":{\"day\":1,\"letters\":[{\"id\":\"chest-1-9\",\"kind\":2,\"chest\":9,\"day\":1}]}}").State;
            Assert.AreEqual(0, bad.WeekPoints, "points below nothing are nothing");
            Assert.AreEqual(99 & 7, bad.WeekChestsClaimed, "only the three chests' marks");
            Assert.AreEqual(-1, bad.BoxDay);
            Assert.AreEqual(0, bad.Letters.Count, "a chest the game does not have is no letter");
        }
    }
}
