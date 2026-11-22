using System.Collections.Generic;
using NUnit.Framework;

namespace VuonTra.Core.Tests
{
    /// <summary>The first minute's guidance: six steps read off the garden, a skip, one clover, and the save.</summary>
    public class TutorialTests
    {
        List<StationDef> defs;
        GameState s;

        [SetUp]
        public void SetUp()
        {
            defs = Fixture.Defs();
            s = GameState.NewGame(defs);
            Rabbits.GiftFirst(s, "Bông");
        }

        [Test]
        public void A_New_Garden_Starts_At_The_First_Step_And_Moves_Only_When_The_Garden_Shows_It()
        {
            Assert.AreEqual(0, s.TutorialStep);
            Assert.AreEqual(0, Tutorial.Advance(s));
            s.TotalTaps = 1;
            Assert.AreEqual(1, Tutorial.Advance(s));
            Assert.AreEqual(Tutorial.RaiseTeapot, s.TutorialStep);
            s.Levels[0] = 2;
            Tutorial.Advance(s);
            Assert.AreEqual(Tutorial.OpenSecond, s.TutorialStep);
            s.Levels[1] = 1;
            Tutorial.Advance(s);
            Assert.AreEqual(Tutorial.CollectQuest, s.TutorialStep);
            s.Claimed.Add("mo-2");
            Tutorial.Advance(s);
            Assert.AreEqual(Tutorial.CallRabbit, s.TutorialStep);
            Rabbit called = Rabbits.Summon(s, 0.5, "Mây");
            Assert.IsNull(called, "no clover yet: the step must bring one");
            Assert.IsTrue(Tutorial.GrantCallClover(s));
            Assert.IsFalse(Tutorial.GrantCallClover(s), "once only");
            called = Rabbits.Summon(s, 0.5, "Mây");
            Assert.IsNotNull(called);
            Tutorial.Advance(s);
            Assert.AreEqual(Tutorial.PlaceRabbit, s.TutorialStep);
            Rabbits.Assign(s, called.Id, 1);
            Tutorial.Advance(s);
            Assert.IsTrue(Tutorial.IsDone(s));
        }

        [Test]
        public void Steps_Done_Out_Of_Order_Are_Passed_Together()
        {
            s.TotalTaps = 5;
            s.Levels[0] = 3;
            s.Levels[1] = 2;
            Assert.AreEqual(3, Tutorial.Advance(s));
            Assert.AreEqual(Tutorial.CollectQuest, s.TutorialStep);
        }

        [Test]
        public void The_Clover_Comes_Only_On_The_Call_Step_And_Only_When_The_Purse_Has_None()
        {
            Assert.IsFalse(Tutorial.GrantCallClover(s), "not on the first step");
            s.TutorialStep = Tutorial.CallRabbit;
            s.Clovers = 2;
            Assert.IsFalse(Tutorial.GrantCallClover(s), "already able to call");
            s.Clovers = 0;
            Assert.IsTrue(Tutorial.GrantCallClover(s));
            Assert.AreEqual(Rabbits.SummonCost, s.Clovers);
        }

        [Test]
        public void Skipping_Ends_It_For_Good_And_The_Save_Keeps_The_Step()
        {
            s.TotalTaps = 1;
            Tutorial.Advance(s);
            GameState r = SaveCodec.Decode(defs, SaveCodec.Encode(defs, s)).State;
            Assert.AreEqual(Tutorial.RaiseTeapot, r.TutorialStep);
            Tutorial.Skip(r);
            Assert.IsTrue(Tutorial.IsDone(r));
            GameState again = SaveCodec.Decode(defs, SaveCodec.Encode(defs, r)).State;
            Assert.IsTrue(Tutorial.IsDone(again));
            Assert.AreEqual(0, Tutorial.Advance(again), "nothing left to move");
        }

        [Test]
        public void A_Garden_From_Before_The_Guidance_Is_Not_Shown_It_But_An_Untouched_One_Is()
        {
            GameState played = SaveCodec.Decode(defs, "{\"version\":12,\"stations\":[{\"id\":\"tea\",\"level\":7,\"progress\":0}],\"hintSeen\":true}").State;
            Assert.IsTrue(Tutorial.IsDone(played));
            GameState tapped = SaveCodec.Decode(defs, "{\"version\":12,\"stations\":[{\"id\":\"tea\",\"level\":1,\"progress\":0}],\"taps\":3}").State;
            Assert.IsTrue(Tutorial.IsDone(tapped));
            GameState fresh = SaveCodec.Decode(defs, "{\"version\":12,\"stations\":[{\"id\":\"tea\",\"level\":1,\"progress\":0}]}").State;
            Assert.AreEqual(0, fresh.TutorialStep, "a garden that never did anything still gets the guidance");
        }
    }
}
