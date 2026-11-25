using System.Collections.Generic;
using NUnit.Framework;

namespace VuonTra.Core.Tests
{
    public class FirstSessionTests
    {
        const long Start = 20_800L * Offers.DaySeconds + 9 * 3600L;

        GameState s;

        [SetUp]
        public void SetUp()
        {
            List<StationDef> defs = Fixture.Defs();
            s = GameState.NewGame(defs);
            s.MaxDaySeen = s.FirstDaySeen = 20_800L;
        }

        [Test]
        public void A_Card_That_Comes_By_Itself_Waits_Twenty_Seconds_After_The_Last_Card_Closed()
        {
            var pacer = new CardPacer();
            Assert.IsTrue(pacer.Rested(0d), "the first card of the day comes at once");
            pacer.Note(10d, true);
            pacer.Note(14d, true);
            pacer.Note(15d, false);
            Assert.IsFalse(pacer.Rested(15d));
            Assert.IsFalse(pacer.Rested(33.9d), "twenty seconds of garden after the card that was up until second 14");
            Assert.AreEqual(0.1d, pacer.Wait(33.9d), 1e-9);
            Assert.IsTrue(pacer.Rested(34d));
            Assert.AreEqual(0d, pacer.Wait(40d));
            pacer.Reset();
            Assert.IsTrue(pacer.Rested(15d));
        }

        [Test]
        public void No_Purchase_Card_Comes_Before_The_Guidance_Is_Done_And_The_Starter_Pack_Waits_For_Minute_Three()
        {
            s.GardenXp = Garden.XpForLevel(Offers.StarterLevel);
            Offers.Tick(s, Start);
            Assert.AreEqual(OfferState.Active, Offers.StateOf(s, Offers.Starter, Start));
            s.PlaySeconds = 600d;
            Assert.IsNull(Offers.ToShow(s, Start), "the guidance is not done yet");

            s.TutorialStep = Tutorial.Steps;
            s.PlaySeconds = Offers.StarterQuietSeconds - 1d;
            Assert.IsNull(Offers.ToShow(s, Start), "not before minute three");

            s.PlaySeconds = Offers.StarterQuietSeconds;
            Assert.AreEqual(Offers.Starter, Offers.ToShow(s, Start).Id);
        }

        [Test]
        public void A_Milestone_Pack_Needs_Only_The_Guidance_Done()
        {
            s.GardenXp = Garden.XpForLevel(10);
            Offers.Tick(s, Start);
            Offers.MarkShown(s, Offers.Starter);
            Assert.IsNull(Offers.ToShow(s, Start), "never during the guidance");
            s.TutorialStep = Tutorial.Steps;
            Assert.AreEqual(Offers.Milestone10, Offers.ToShow(s, Start).Id, "a level-ten garden has played past minute three anyway");
        }
    }
}
