using System.Collections.Generic;
using NUnit.Framework;

namespace VuonTra.Core.Tests
{
    public class IntrosTests
    {
        List<StationDef> defs;
        GameState s;

        [SetUp]
        public void SetUp()
        {
            defs = Fixture.Defs();
            s = GameState.NewGame(defs);
            s.Levels = new[] { 30, 0, 0 };
        }

        void OpenEverything()
        {
            s.GardenXp = Garden.XpForLevel(NewMoon.FirstLevel);
            s.TutorialStep = Tutorial.Steps;
            s.NextOrderId = 1;
            s.Grid.Segments = 1;
        }

        [Test]
        public void There_Are_Ten_Cards_And_All_But_The_New_Moon_Carry_The_First_Try_Gift()
        {
            Assert.AreEqual(10, Intros.All.Length);
            Assert.AreEqual(10, new HashSet<string>(Intros.All).Count);
            foreach (string id in Intros.All) Assert.AreEqual(id != Intros.NewMoon, Intros.HasReward(id), id);
        }

        [Test]
        public void Each_Card_Opens_By_The_Rule_That_Already_Opens_Its_Feature()
        {
            s.TutorialStep = 0;
            Assert.IsTrue(Intros.IsOpen(s, Intros.Fishing), "the pond is open from the first level");
            Assert.IsFalse(Intros.IsOpen(s, Intros.Shop), "the shop waits for the guidance to end");
            Assert.IsFalse(Intros.IsOpen(s, Intros.Layout));
            Assert.IsFalse(Intros.IsOpen(s, Intros.Orders), "no guest has come yet");
            Assert.IsFalse(Intros.IsOpen(s, Intros.Pounding));
            s.TutorialStep = Tutorial.Steps;
            Assert.IsTrue(Intros.IsOpen(s, Intros.Shop));
            Assert.IsFalse(Intros.IsOpen(s, Intros.Layout), "arranging waits for the first band of new land");
            s.Grid.Segments = 1;
            Assert.IsTrue(Intros.IsOpen(s, Intros.Layout));
            s.NextOrderId = 1;
            Assert.IsTrue(Intros.IsOpen(s, Intros.Orders));

            s.GardenXp = Garden.XpForLevel(Pounding.UnlockLevel);
            Assert.AreEqual(Pounding.Unlocked(s), Intros.IsOpen(s, Intros.Pounding));
            Assert.AreEqual(MoonJars.Shown(s), Intros.IsOpen(s, Intros.MoonJar));
            Assert.IsTrue(Intros.IsOpen(s, Intros.Pounding));
            Assert.IsFalse(Intros.IsOpen(s, Intros.Leaves));
            s.GardenXp = Garden.XpForLevel(Leaves.UnlockLevel);
            Assert.IsTrue(Intros.IsOpen(s, Intros.Leaves));
            Assert.IsFalse(Intros.IsOpen(s, Intros.Fireflies));
            s.GardenXp = Garden.XpForLevel(Fireflies.UnlockLevel);
            Assert.IsTrue(Intros.IsOpen(s, Intros.Fireflies));
            Assert.IsFalse(Intros.IsOpen(s, Intros.Rain));
            s.GardenXp = Garden.XpForLevel(FrogRabbit.FromLevel);
            Assert.AreEqual(FrogRabbit.CanCome(s), Intros.IsOpen(s, Intros.Rain));
            Assert.IsTrue(Intros.IsOpen(s, Intros.Rain));
            Assert.IsFalse(Intros.IsOpen(s, Intros.NewMoon));
            s.GardenXp = Garden.XpForLevel(NewMoon.FirstLevel);
            Assert.IsTrue(Intros.IsOpen(s, Intros.NewMoon));
        }

        [Test]
        public void Every_Card_Shows_Exactly_Once()
        {
            OpenEverything();
            var shown = new List<string>();
            for (int n = 0; n < 30; n++)
            {
                string id = Intros.Next(s);
                if (id == null) break;
                Assert.IsTrue(Intros.Show(s, id));
                Assert.IsFalse(Intros.Show(s, id), "a card shown is not shown again");
                shown.Add(id);
            }
            CollectionAssert.AreEqual(Intros.All, shown, "all ten, once each, in the list's order");
            Assert.IsNull(Intros.Next(s));
            CollectionAssert.AreEqual(Intros.All, Intros.Opened(s), "the read-again list holds every card shown");
        }

        [Test]
        public void A_Card_Whose_Feature_Is_Shut_Cannot_Be_Shown()
        {
            Assert.IsFalse(Intros.Show(s, Intros.NewMoon));
            Assert.IsFalse(Intros.Show(s, "unknown"));
            Assert.AreEqual(0, Intros.Opened(s).Count);
        }

        [Test]
        public void An_Old_Save_Past_The_Thresholds_Has_Its_Open_Cards_Marked_Seen_And_No_Gift_To_Claim()
        {
            s.GardenXp = Garden.XpForLevel(16);
            s.TutorialStep = Tutorial.Steps;
            s.NextOrderId = 4;
            s.Grid.Segments = 1;
            string json = SaveCodec.Encode(defs, s);
            int at = json.IndexOf(",\"intros\":");
            Assert.Greater(at, 0, "the save writes the cards' block");
            int end = json.IndexOf('}', at);
            string old = json.Substring(0, at) + json.Substring(end + 1);
            Assert.AreEqual(-1, old.IndexOf("intros"));

            LoadResult loaded = SaveCodec.Decode(defs, old);
            Assert.AreEqual(LoadStatus.Ok, loaded.Status);
            GameState o = loaded.State;
            Assert.IsNull(Intros.Next(o), "nothing pops up in a heap after the upgrade");
            foreach (string id in Intros.All)
            {
                bool open = id != Intros.NewMoon;
                Assert.AreEqual(open, o.IntrosSeen.Contains(id), id);
                Assert.AreEqual(open, o.IntrosTried.Contains(id), id);
            }
            Assert.IsFalse(Intros.Use(defs, o, Modifiers.None, Intros.Fishing, out BigNum paid, out int clovers));
            Assert.IsTrue(paid.IsZero);
            Assert.AreEqual(0, clovers);

            o.GardenXp = Garden.XpForLevel(NewMoon.FirstLevel);
            Assert.AreEqual(Intros.NewMoon, Intros.Next(o), "a feature opened after the upgrade still gets its card");
        }

        [Test]
        public void A_New_Garden_Is_Not_Marked_And_A_Saved_One_Keeps_Its_Cards()
        {
            LoadResult empty = SaveCodec.Decode(defs, "");
            Assert.AreEqual(0, empty.State.IntrosSeen.Count);
            Assert.AreEqual(Intros.Fishing, Intros.Next(empty.State));

            OpenEverything();
            Intros.Show(s, Intros.Fishing);
            Intros.Show(s, Intros.Shop);
            Intros.Later(s, Intros.Shop);
            Intros.Use(defs, s, Modifiers.None, Intros.Fishing, out _, out _);
            GameState back = SaveCodec.Decode(defs, SaveCodec.Encode(defs, s)).State;
            CollectionAssert.AreEquivalent(new[] { Intros.Fishing, Intros.Shop }, back.IntrosSeen);
            CollectionAssert.AreEquivalent(new[] { Intros.Fishing }, back.IntrosTried);
            CollectionAssert.AreEquivalent(new[] { Intros.Shop }, back.IntrosLater);
            Assert.AreEqual(Intros.Pounding, Intros.Next(back));
        }

        [Test]
        public void The_First_Try_Gift_Is_A_Minute_Of_Income_And_A_Clover_Paid_Once()
        {
            OpenEverything();
            Intros.Show(s, Intros.Fishing);
            var m = Modifiers.None;
            m.IncomeMultiplier = 2d;
            m.SpeedMultiplier = 1.5d;
            double minute = 80d * 60d * 2d * 1.5d * Recipes.ExtraDishMultiplier;
            Assert.AreEqual(minute, Intros.Reward(defs, s, m).ToDouble(), 1e-6 * minute);
            Assert.AreEqual(Economy.IncomePerSecond(defs, s).ToDouble() * 60d * 3d, Intros.Reward(defs, s, m).ToDouble(), 1e-6 * minute);

            int cloversBefore = s.Clovers;
            Assert.IsTrue(Intros.Use(defs, s, m, Intros.Fishing, out BigNum paid, out int clovers));
            Assert.AreEqual(Intros.Reward(defs, s, m).ToDouble(), paid.ToDouble(), 1e-6);
            Assert.AreEqual(paid.ToDouble(), s.Coins.ToDouble(), 1e-6);
            Assert.AreEqual(Intros.RewardClovers, clovers);
            Assert.AreEqual(cloversBefore + 1, s.Clovers);

            Assert.IsFalse(Intros.Use(defs, s, m, Intros.Fishing, out BigNum again, out int againClovers), "the gift is paid once");
            Assert.IsTrue(again.IsZero);
            Assert.AreEqual(0, againClovers);
            Assert.AreEqual(paid.ToDouble(), s.Coins.ToDouble(), 1e-6);
            Assert.AreEqual(cloversBefore + 1, s.Clovers);
        }

        [Test]
        public void The_New_Moon_Card_Pays_Nothing_On_Its_First_Use()
        {
            OpenEverything();
            Intros.Show(s, Intros.NewMoon);
            Assert.IsFalse(Intros.Use(defs, s, Modifiers.None, Intros.NewMoon, out BigNum paid, out int clovers));
            Assert.IsTrue(paid.IsZero);
            Assert.AreEqual(0, clovers);
            Assert.IsTrue(s.IntrosTried.Contains(Intros.NewMoon));
            Assert.IsTrue(s.Coins.IsZero);
        }

        [Test]
        public void Trying_A_Feature_Before_Its_Card_Pays_The_Gift_And_Spares_The_Card()
        {
            OpenEverything();
            Assert.IsTrue(Intros.Use(defs, s, Modifiers.None, Intros.Pounding, out _, out _));
            Assert.IsTrue(s.IntrosSeen.Contains(Intros.Pounding));
            Assert.IsFalse(Intros.Show(s, Intros.Pounding));
            Assert.AreNotEqual(Intros.Pounding, Intros.Next(s));
        }

        [Test]
        public void Later_Leaves_A_Dot_Until_The_Spot_Is_Touched_Or_The_Feature_Tried()
        {
            OpenEverything();
            Assert.IsFalse(Intros.Later(s, Intros.Fishing), "no dot for a card never shown");
            Intros.Show(s, Intros.Fishing);
            Assert.IsTrue(Intros.Later(s, Intros.Fishing));
            Assert.IsTrue(Intros.HasDot(s, Intros.Fishing));
            Assert.IsTrue(Intros.Touch(s, Intros.Fishing));
            Assert.IsFalse(Intros.HasDot(s, Intros.Fishing));
            Assert.IsFalse(Intros.Touch(s, Intros.Fishing));

            Intros.Later(s, Intros.Fishing);
            Intros.Use(defs, s, Modifiers.None, Intros.Fishing, out _, out _);
            Assert.IsFalse(Intros.HasDot(s, Intros.Fishing), "trying it takes the dot away");
            Assert.IsFalse(Intros.Later(s, Intros.Fishing), "and a tried feature gets no new dot");
        }

        [Test]
        public void The_Queue_Shows_One_Card_Every_Ninety_Seconds_And_Holds_While_Blocked()
        {
            OpenEverything();
            var queue = new IntroQueue();
            Assert.IsNull(queue.Tick(s, 0d, true), "nothing while blocked");
            Assert.AreEqual(Intros.Fishing, queue.Tick(s, 1d, false));
            Assert.IsTrue(s.IntrosSeen.Contains(Intros.Fishing), "a card handed out counts as shown");
            for (int k = 0; k < 89; k++) Assert.IsNull(queue.Tick(s, 1d, false), "second " + k);
            Assert.IsNull(queue.Tick(s, 1d, true), "the gap has run but the card still waits while blocked");
            Assert.AreEqual(Intros.Pounding, queue.Tick(s, 0d, false));
            Assert.AreEqual(Intros.GapSeconds, queue.Wait, 1e-9);
        }
    }
}
