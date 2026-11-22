using System.Collections.Generic;
using NUnit.Framework;

namespace VuonTra.Core.Tests
{
    /// <summary>The time away, told and doubled (#69).</summary>
    public class AwayTests
    {
        List<StationDef> defs;
        GameState s;

        [SetUp]
        public void SetUp()
        {
            defs = Fixture.Defs();
            s = GameState.NewGame(defs);
            s.Levels[0] = 10;
            s.LastSeenUnixSeconds = 0;
        }

        [Test]
        public void Time_Away_Is_Paid_For_Up_To_Three_Hours()
        {
            Assert.AreEqual(3 * 3600d, Simulation.AwayCapSeconds);
            AwayReport r = Away.Claim(defs, s, 10 * 3600);
            Assert.AreEqual(3 * 3600d, r.Seconds, 1e-9);
            Assert.IsTrue(r.Capped);
            Assert.IsTrue(r.WorthTelling);
            AwayReport shorter = Away.Claim(defs, s, 10 * 3600 + 2 * 3600);
            Assert.AreEqual(2 * 3600d, shorter.Seconds, 1e-9);
            Assert.IsFalse(shorter.Capped);
        }

        [Test]
        public void Under_Five_Minutes_Is_Paid_But_Not_Worth_A_Sheet()
        {
            AwayReport r = Away.Claim(defs, s, 120);
            Assert.Greater(r.Paid.ToDouble(), 0d);
            Assert.IsFalse(r.WorthTelling);
        }

        [Test]
        public void The_Report_Counts_The_Tray_The_Guests_And_Finds_The_Sleepiest_Rabbit()
        {
            Rabbits.Arrive(s, 1, "Lá", 0d).Station = 0;
            Rabbit resting = Rabbits.Arrive(s, 0, "Mây", 0d);
            AwayReport r = Away.Claim(defs, s, 3 * 3600);
            Assert.AreEqual(Tray.Count(s), r.TrayAdded, "everything on the tray came while away");
            Assert.AreEqual(Away.GuestsWhileAway(3 * 3600), r.GuestsMissed);
            Assert.AreEqual(resting.Id, r.SleepiestId, "a resting rabbit before a working one, even of a lower tier");
            Assert.AreEqual(0, Away.GuestsWhileAway(0));
            Assert.AreEqual(Orders.PerDay, Away.GuestsWhileAway(48 * 3600), "at most a day's guests");
        }

        [Test]
        public void Doubling_Is_Free_Once_A_Day_And_A_Turned_Back_Clock_Does_Not_Bring_It_Back()
        {
            var paid = new BigNum(1000);
            BigNum before = s.Coins;
            Assert.IsTrue(Away.DoubleIsFree(s, 50));
            Assert.IsTrue(Away.Double(s, 50, paid));
            Assert.AreEqual((before + paid).ToDouble(), s.Coins.ToDouble(), 1e-9);
            Assert.AreEqual(50, s.AwayDoubleDay);
            Assert.IsFalse(Away.DoubleIsFree(s, 50), "the second time the same day is not free");
            Assert.IsFalse(Away.DoubleIsFree(s, 49), "nor on a day the clock went back to");
            Assert.AreEqual(Away.DoubleCost, Away.DoubleCostToday(s, 50));
            Assert.IsTrue(Away.DoubleIsFree(s, 51), "a new day brings it back");
        }

        [Test]
        public void A_Paid_Doubling_Takes_Golden_Carrots_And_Is_Refused_When_Short()
        {
            s.AwayDoubleDay = 50;
            s.GoldenCarrots = Away.DoubleCost - 1;
            BigNum before = s.Coins;
            Assert.IsFalse(Away.Double(s, 50, new BigNum(1000)));
            Assert.AreEqual(before.ToDouble(), s.Coins.ToDouble(), 1e-9, "nothing paid when short");
            s.GoldenCarrots = Away.DoubleCost + 2;
            Assert.IsTrue(Away.Double(s, 50, new BigNum(1000)));
            Assert.AreEqual(2, s.GoldenCarrots);
            Assert.IsFalse(Away.Double(s, 50, BigNum.Zero), "nothing to double");
        }

        [Test]
        public void The_Free_Doubling_Day_Is_Saved_And_An_Old_Save_Has_None()
        {
            s.AwayDoubleDay = 77;
            GameState back = SaveCodec.Decode(defs, SaveCodec.Encode(defs, s)).State;
            Assert.AreEqual(77, back.AwayDoubleDay);
            string old = SaveCodec.Encode(defs, s).Replace(",\"awayDouble\":77", "");
            Assert.AreEqual(-1, SaveCodec.Decode(defs, old).State.AwayDoubleDay);
        }
    }
}
