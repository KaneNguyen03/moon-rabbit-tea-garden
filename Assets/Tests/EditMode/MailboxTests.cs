using System.Collections.Generic;
using NUnit.Framework;

namespace VuonTra.Core.Tests
{
    /// <summary>The mailbox (#68): letters the game writes itself, ten at most, thirty days each, claimed once.</summary>
    public class MailboxTests
    {
        List<StationDef> defs;
        GameState s;

        [SetUp]
        public void SetUp()
        {
            defs = Fixture.Defs();
            s = GameState.NewGame(defs);
        }

        static long D(int y, int m, int d) => GiftCodes.Day(y, m, d);

        [Test]
        public void A_Save_That_Could_Not_Be_Read_Brings_A_Make_Up_Letter_That_Pays_Once()
        {
            long day = D(2026, 11, 23);
            Assert.IsTrue(Mailbox.Compensate(s, day));
            Assert.AreEqual(1, s.Letters.Count);
            Letter letter = s.Letters[0];
            Assert.AreEqual(LetterKind.MakeUp, letter.Kind);
            Assert.AreEqual(Mailbox.MakeUpGolden, letter.Golden);
            Assert.AreEqual(Mailbox.MakeUpClovers, letter.Clovers);
            Assert.IsFalse(Mailbox.Compensate(s, day), "one make-up letter a day, however often the load fails");

            Letter paid = Mailbox.Claim(s, letter.Id, day + 2);
            Assert.IsNotNull(paid);
            Assert.AreEqual(Mailbox.MakeUpGolden, s.GoldenCarrots);
            Assert.AreEqual(Mailbox.MakeUpClovers, s.Clovers);
            Assert.AreEqual(0, s.Letters.Count, "a claimed letter leaves the box");
            Assert.IsNull(Mailbox.Claim(s, letter.Id, day + 2), "and pays nothing twice");
            Assert.AreEqual(Mailbox.MakeUpGolden, s.GoldenCarrots);
        }

        [Test]
        public void A_Letter_Keeps_Thirty_Days_Then_Goes_Unpaid()
        {
            long day = D(2026, 11, 1);
            Mailbox.Compensate(s, day);
            string id = s.Letters[0].Id;
            Assert.AreEqual(30, Mailbox.DaysLeft(s.Letters[0], day));
            Assert.AreEqual(1, Mailbox.Open(s, day + Mailbox.KeepDays).Count, "the thirtieth day still counts");
            Assert.AreEqual(0, Mailbox.Open(s, day + Mailbox.KeepDays + 1).Count);
            Assert.IsNull(Mailbox.Claim(s, id, day + Mailbox.KeepDays + 1));
            Assert.AreEqual(0, s.GoldenCarrots);
        }

        [Test]
        public void The_Box_Holds_Ten_Letters_And_Lets_The_Oldest_Go()
        {
            long day = D(2026, 3, 1);
            for (int i = 0; i < 12; i++) Mailbox.Compensate(s, day + i);
            Assert.AreEqual(Mailbox.MaxLetters, s.Letters.Count);
            Assert.AreEqual(day + 2, s.Letters[0].Day, "the two oldest made way");
            Assert.AreEqual(day + 11, s.Letters[9].Day);
        }

        [Test]
        public void A_Season_Missed_Altogether_Leaves_A_Letter_When_The_Player_Comes_Back()
        {
            // Last seen in August, back in October: Mid-Autumn (all of September) went by unseen.
            s.MailDay = D(2026, 8, 20);
            int added = Mailbox.Check(s, D(2026, 10, 5));
            Assert.AreEqual(1, added);
            Letter letter = s.Letters[0];
            Assert.AreEqual(LetterKind.Season, letter.Kind);
            Assert.AreEqual(Season.MidAutumn, letter.Season);
            Assert.AreEqual(Mailbox.SeasonGolden, letter.Golden);
            Assert.AreEqual(D(2026, 10, 5), s.MailDay);
            Assert.AreEqual(0, Mailbox.Check(s, D(2026, 10, 5)), "the same day again brings nothing");
            Assert.AreEqual(0, Mailbox.Check(s, D(2026, 10, 9)), "nor does a later day without a season");
            Assert.AreEqual(1, s.Letters.Count);
        }

        [Test]
        public void A_Season_The_Player_Was_Greeted_In_Brings_No_Letter()
        {
            s.MailDay = D(2026, 9, 2);
            s.SeasonGreetedDay = D(2026, 9, 2);
            Assert.AreEqual(0, Mailbox.Check(s, D(2026, 10, 20)));
            Assert.AreEqual(0, s.Letters.Count);
        }

        [Test]
        public void A_Season_Still_Running_Today_Brings_No_Letter_Its_Banner_Is_Still_Coming()
        {
            s.MailDay = D(2026, 11, 20);
            Assert.AreEqual(0, Mailbox.Check(s, D(2026, 12, 10)));
            Assert.AreEqual(0, s.Letters.Count);
        }

        [Test]
        public void A_Clock_Set_Back_Brings_Nothing()
        {
            s.MailDay = D(2026, 10, 5);
            Assert.AreEqual(0, Mailbox.Check(s, D(2026, 8, 20)));
            Assert.AreEqual(D(2026, 10, 5), s.MailDay, "the stored day never goes back");
            Assert.AreEqual(0, s.Letters.Count);
        }

        [Test]
        public void A_New_Garden_Starts_Its_Mail_Today_With_Nothing_Missed()
        {
            Assert.AreEqual(-1, s.MailDay);
            Assert.AreEqual(0, Mailbox.Check(s, D(2026, 10, 5)));
            Assert.AreEqual(D(2026, 10, 5), s.MailDay);
        }

        [Test]
        public void A_Long_Time_Away_Looks_Back_One_Year_At_Most()
        {
            s.MailDay = D(2022, 1, 1);
            int added = Mailbox.Check(s, D(2026, 11, 23));
            // Within the year before: Tết, Mid-Autumn 2026 and Christmas 2025 (from 23 Nov 2025).
            Assert.AreEqual(3, added);
            var seasons = new HashSet<Season>();
            foreach (Letter l in s.Letters) seasons.Add(l.Season);
            CollectionAssert.AreEquivalent(new[] { Season.Christmas, Season.Tet, Season.MidAutumn }, seasons);
        }

        [Test]
        public void Letters_And_The_Mail_Day_Round_Trip_Through_The_Save()
        {
            long day = D(2026, 10, 5);
            s.MailDay = D(2026, 8, 20);
            Mailbox.Check(s, day);
            Mailbox.Compensate(s, day);
            GameState r = SaveCodec.Decode(defs, SaveCodec.Encode(defs, s)).State;
            Assert.AreEqual(day, r.MailDay);
            Assert.AreEqual(2, r.Letters.Count);
            for (int i = 0; i < 2; i++)
            {
                Assert.AreEqual(s.Letters[i].Id, r.Letters[i].Id);
                Assert.AreEqual(s.Letters[i].Kind, r.Letters[i].Kind);
                Assert.AreEqual(s.Letters[i].Season, r.Letters[i].Season);
                Assert.AreEqual(s.Letters[i].Day, r.Letters[i].Day);
                Assert.AreEqual(s.Letters[i].Golden, r.Letters[i].Golden);
                Assert.AreEqual(s.Letters[i].Clovers, r.Letters[i].Clovers);
            }
        }

        [Test]
        public void A_Save_Cannot_Grow_Its_Letters_Past_The_Fixed_Gifts()
        {
            Mailbox.Compensate(s, D(2026, 10, 5));
            string json = SaveCodec.Encode(defs, s).Replace("\"golden\":" + Mailbox.MakeUpGolden + ",\"clovers\":" + Mailbox.MakeUpClovers, "\"golden\":9999,\"clovers\":9999");
            StringAssert.Contains("9999", json);
            GameState r = SaveCodec.Decode(defs, json).State;
            Assert.AreEqual(1, r.Letters.Count);
            Assert.AreEqual(Mailbox.MakeUpGolden, r.Letters[0].Golden, "a letter pays what its kind pays, whatever the file says");
            Assert.AreEqual(Mailbox.MakeUpClovers, r.Letters[0].Clovers);
        }
    }
}
