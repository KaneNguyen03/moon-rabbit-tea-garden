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
            s.PlaySeconds = Intros.EarliestSeconds;
            s.Fish[0] = Intros.TankFish;
            long fair = Fairs.StartOf(Fairs.All[0], 2026);
            Seasons.Clamp(s, fair - Intros.MonthPassDay);
            Seasons.Clamp(s, fair);
        }

        void PlayDay(int day)
        {
            if (s.FirstDaySeen < 0) Seasons.Clamp(s, 1000);
            Seasons.Clamp(s, s.FirstDaySeen + day - 1);
        }

        [Test]
        public void There_Are_Sixteen_Cards_And_All_But_The_New_Moon_Carry_The_First_Try_Gift()
        {
            Assert.AreEqual(16, Intros.All.Length);
            Assert.AreEqual(16, new HashSet<string>(Intros.All).Count);
            CollectionAssert.AreEqual(new[] { Intros.RecipeBook, Intros.Aquarium, Intros.Trunk, Intros.Fair, Intros.MonthPass, Intros.WeekChest }, Intros.Late);
            for (int i = 0; i < Intros.Late.Length; i++) Assert.AreEqual(Intros.Late[i], Intros.All[10 + i], "the round-two cards come after the first ten");
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

        [Test, MonetizationOn]
        public void No_Card_But_The_Shop_Shows_Before_Seven_Minutes_Of_Play()
        {
            OpenEverything();
            Assert.AreEqual(420d, Intros.EarliestSeconds);
            s.PlaySeconds = Intros.EarliestSeconds - 1d;
            foreach (string id in Intros.All)
            {
                Assert.IsTrue(Intros.IsOpen(s, id), id + ": the feature itself is open");
                Assert.AreEqual(id == Intros.Shop, Intros.Due(s, id), id);
            }
            Assert.AreEqual(Intros.Shop, Intros.Next(s), "only the shop comes in the first seven minutes");
            Assert.IsFalse(Intros.Show(s, Intros.Fishing));
            Assert.IsTrue(Intros.Show(s, Intros.Shop));
            Assert.IsNull(Intros.Next(s));
            var queue = new IntroQueue();
            for (int k = 0; k < 30; k++) Assert.IsNull(queue.Tick(s, 1d, false), "second " + k);

            s.PlaySeconds = Intros.EarliestSeconds;
            Assert.AreEqual(Intros.Fishing, Intros.Next(s), "at minute seven the cards start, in the list's order");
            Assert.AreEqual(Intros.Fishing, queue.Tick(s, 1d, false));
        }

        [Test]
        public void The_Fishing_Card_Waits_For_The_First_Band_Of_New_Land()
        {
            s.PlaySeconds = Intros.EarliestSeconds;
            Assert.AreEqual(0, Expansion.Segments(s));
            Assert.IsTrue(Fishing.Unlocked(s), "the pond stays open from the first level");
            Assert.IsTrue(Intros.IsOpen(s, Intros.Fishing));
            Assert.IsFalse(Intros.Due(s, Intros.Fishing));
            Assert.IsNull(Intros.Next(s));
            Assert.IsFalse(Intros.Show(s, Intros.Fishing));
            s.Grid.Segments = Intros.FishingSegments;
            Assert.AreEqual(1, Intros.FishingSegments);
            Assert.AreEqual(Intros.Fishing, Intros.Next(s));
            Assert.IsTrue(Intros.Show(s, Intros.Fishing));
        }

        [Test]
        public void Trying_The_Pond_Before_Its_Card_Still_Pays_The_First_Try_Gift()
        {
            Assert.IsTrue(Intros.Use(defs, s, Modifiers.None, Intros.Fishing, out _, out int clovers), "the gift follows the feature, not the card's clock");
            Assert.AreEqual(Intros.RewardClovers, clovers);
            Assert.IsTrue(s.IntrosSeen.Contains(Intros.Fishing));
        }

        [Test, MonetizationOn]
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
            CollectionAssert.AreEqual(Intros.All, shown, "all sixteen, once each, in the list's order");
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
            s.PlaySeconds = 2d * Intros.EarliestSeconds;
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
                bool open = id != Intros.NewMoon && System.Array.IndexOf(Intros.Late, id) < 0;
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
            Assert.IsNull(Intros.Next(empty.State), "a new garden starts on the main loop, no card yet");

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
        public void The_Queue_Shows_One_Card_Every_Gap_And_Holds_While_Blocked()
        {
            OpenEverything();
            var queue = new IntroQueue();
            Assert.IsNull(queue.Tick(s, 0d, true), "nothing while blocked");
            Assert.AreEqual(Intros.Fishing, queue.Tick(s, 1d, false));
            Assert.IsTrue(s.IntrosSeen.Contains(Intros.Fishing), "a card handed out counts as shown");
            for (int k = 0; k < (int)Intros.GapSeconds - 1; k++) Assert.IsNull(queue.Tick(s, 1d, false), "second " + k);
            Assert.IsNull(queue.Tick(s, 1d, true), "the gap has run but the card still waits while blocked");
            Assert.AreEqual(Intros.Pounding, queue.Tick(s, 0d, false));
            Assert.AreEqual(Intros.GapSeconds, queue.Wait, 1e-9);
        }

        [Test, MonetizationOn]
        public void The_Week_Chest_Opens_On_Day_Two_The_Trunk_On_Day_Three_And_The_Month_Pass_On_Day_Four()
        {
            s.TutorialStep = Tutorial.Steps;
            PlayDay(1);
            Assert.IsFalse(Intros.IsOpen(s, Intros.WeekChest));
            Assert.IsFalse(Intros.IsOpen(s, Intros.Trunk));
            Assert.IsFalse(Intros.IsOpen(s, Intros.MonthPass));
            PlayDay(Intros.WeekChestDay);
            Assert.IsTrue(Intros.IsOpen(s, Intros.WeekChest));
            Assert.IsFalse(Intros.IsOpen(s, Intros.Trunk));
            PlayDay(Intros.TrunkDay);
            Assert.IsTrue(Intros.IsOpen(s, Intros.Trunk));
            Assert.IsFalse(Intros.IsOpen(s, Intros.MonthPass));
            PlayDay(Intros.MonthPassDay);
            Assert.IsTrue(Intros.IsOpen(s, Intros.MonthPass));
            Assert.AreEqual(2, Intros.WeekChestDay);
            Assert.AreEqual(3, Intros.TrunkDay);
            Assert.AreEqual(4, Intros.MonthPassDay);
        }

        [Test, MonetizationOn(false)]
        public void With_Monetization_Off_The_Month_Pass_Card_Never_Opens_On_Any_Day()
        {
            OpenEverything();
            for (int day = 1; day <= 60; day++)
            {
                PlayDay(day);
                Assert.IsFalse(Intros.IsOpen(s, Intros.MonthPass), "day " + day);
                Assert.IsFalse(Intros.Show(s, Intros.MonthPass), "day " + day);
                Assert.IsFalse(Intros.Use(defs, s, Modifiers.None, Intros.MonthPass, out _, out _), "day " + day);
            }
            for (string id = Intros.Next(s); id != null; id = Intros.Next(s))
            {
                Assert.AreNotEqual(Intros.MonthPass, id);
                Intros.Show(s, id);
            }
            Assert.IsFalse(s.IntrosSeen.Contains(Intros.MonthPass));
        }

        [Test, MonetizationOn]
        public void A_Clock_Turned_Back_Does_Not_Close_A_Day_Card_Again()
        {
            PlayDay(Intros.MonthPassDay);
            Assert.IsTrue(Intros.IsOpen(s, Intros.MonthPass));
            Seasons.Clamp(s, s.FirstDaySeen);
            Assert.IsTrue(Intros.IsOpen(s, Intros.MonthPass), "the days played only count up");
        }

        [Test]
        public void The_Tank_Card_Waits_For_Ten_Spare_Fish_And_The_Recipe_Book_For_Its_Level()
        {
            s.Fish[0] = Intros.TankFish - 4;
            s.Fish[2] = 3;
            Assert.IsFalse(Intros.IsOpen(s, Intros.Aquarium), "nine fish are not enough");
            s.Fish[2] = 4;
            Assert.IsTrue(Intros.IsOpen(s, Intros.Aquarium));
            Assert.AreEqual(10, Intros.TankFish);

            s.GardenXp = Garden.XpForLevel(Recipes.UnlockLevel - 1);
            Assert.IsFalse(Intros.IsOpen(s, Intros.RecipeBook));
            s.GardenXp = Garden.XpForLevel(Recipes.UnlockLevel);
            Assert.AreEqual(Recipes.Unlocked(s), Intros.IsOpen(s, Intros.RecipeBook));
            Assert.IsTrue(Intros.IsOpen(s, Intros.RecipeBook));
            s.GardenXp = 0;
            s.Season = 1;
            Assert.IsTrue(Intros.IsOpen(s, Intros.RecipeBook), "after a new moon the book stays open");
        }

        [Test]
        public void The_Fair_Card_Opens_Only_While_A_Fair_Runs()
        {
            FairDef fair = Fairs.All[0];
            long start = Fairs.StartOf(fair, 2026);
            Seasons.Clamp(s, start - 1);
            Assert.IsFalse(Intros.IsOpen(s, Intros.Fair));
            Seasons.Clamp(s, start);
            Assert.IsTrue(Intros.IsOpen(s, Intros.Fair));
            Seasons.Clamp(s, start + fair.Days);
            Assert.IsFalse(Intros.IsOpen(s, Intros.Fair), "between fairs there is nothing to show");
        }

        [Test]
        public void A_Feature_Used_Before_Its_Day_Still_Waits_For_Its_Day_And_Pays_The_Next_Try()
        {
            PlayDay(1);
            s.Trunk.Window = 3;
            s.PassFreeClaimed = 1;
            Assert.IsFalse(Intros.IsOpen(s, Intros.Trunk), "a visit on day one does not bring the card forward");
            Assert.IsFalse(Intros.IsOpen(s, Intros.MonthPass));
            Assert.IsFalse(Intros.Use(defs, s, Modifiers.None, Intros.Trunk, out _, out _), "nor pays before its day");
            Assert.IsFalse(s.IntrosSeen.Contains(Intros.Trunk));
            PlayDay(Intros.TrunkDay);
            s.PlaySeconds = Intros.EarliestSeconds;
            Assert.IsTrue(Intros.IsOpen(s, Intros.Trunk), "on its day the card comes");
            Assert.IsTrue(Intros.Show(s, Intros.Trunk));
            Assert.IsTrue(Intros.Use(defs, s, Modifiers.None, Intros.Trunk, out _, out int clovers));
            Assert.AreEqual(Intros.RewardClovers, clovers);
        }

        [Test]
        public void A_Fish_In_The_Tank_Opens_The_Tank_Card_Without_Ten_Spare_Fish()
        {
            Assert.IsFalse(Intros.IsOpen(s, Intros.Aquarium));
            s.Fish[1] = 1;
            Assert.IsTrue(Aquarium.Release(s, 1).Released);
            Assert.AreEqual(0, Fishing.InBasket(s));
            Assert.IsTrue(Intros.IsOpen(s, Intros.Aquarium), "the first release is its first try");
            Assert.IsTrue(Intros.Use(defs, s, Modifiers.None, Intros.Aquarium, out _, out _));
        }

        [Test, MonetizationOn]
        public void Each_Round_Two_Card_Shows_Once_And_Pays_Its_First_Try_Gift_Once()
        {
            OpenEverything();
            foreach (string id in Intros.All)
                if (System.Array.IndexOf(Intros.Late, id) < 0) Intros.Show(s, id);
            foreach (string id in Intros.Late)
            {
                Assert.IsTrue(Intros.IsOpen(s, id), id);
                Assert.IsTrue(Intros.HasReward(id), id);
                Assert.AreEqual(id, Intros.Next(s), "one at a time, in the list's order");
                Assert.IsTrue(Intros.Show(s, id), id);
                Assert.IsFalse(Intros.Show(s, id), id + " shows once");
                int clovers = s.Clovers;
                BigNum coins = s.Coins;
                Assert.IsTrue(Intros.Use(defs, s, Modifiers.None, id, out BigNum paid, out int got), id);
                Assert.Greater(paid.ToDouble(), 0d, id);
                Assert.AreEqual(Intros.Reward(defs, s, Modifiers.None).ToDouble(), paid.ToDouble(), 1e-6 * paid.ToDouble(), id + ": a minute of income, as for the first ten");
                Assert.AreEqual(Intros.RewardClovers, got, id);
                Assert.AreEqual(clovers + Intros.RewardClovers, s.Clovers, id);
                Assert.AreEqual((coins + paid).ToDouble(), s.Coins.ToDouble(), 1e-6 * s.Coins.ToDouble(), id);
                Assert.IsFalse(Intros.Use(defs, s, Modifiers.None, id, out _, out _), id + " pays once");
            }
            Assert.IsNull(Intros.Next(s));
        }

        GameState Used()
        {
            OpenEverything();
            s.DishLevels[0] = 3;
            s.Fish[1] = 1;
            Aquarium.Release(s, 1);
            s.Trunk.Window = 40;
            s.Fair.Done = 5;
            s.PassBoughtMonth = 2;
            s.WeekChestsClaimed = 1;
            return s;
        }

        static string BeforeRoundTwo(string json)
        {
            Assert.Greater(json.IndexOf(",\"late\":true"), 0, "the save marks that the round-two cards are known");
            return json.Replace(",\"late\":true", "");
        }

        static string WithoutCards(string json)
        {
            int at = json.IndexOf(",\"intros\":");
            Assert.Greater(at, 0);
            int end = json.IndexOf('}', at);
            return json.Substring(0, at) + json.Substring(end + 1);
        }

        [Test]
        public void A_Save_From_Before_The_Round_Two_Cards_That_Used_The_Features_Shows_None_Of_Them_And_Pays_Nothing()
        {
            Used();
            foreach (string id in Intros.All)
                if (System.Array.IndexOf(Intros.Late, id) < 0) { s.IntrosSeen.Add(id); s.IntrosTried.Add(id); }
            int clovers = s.Clovers;
            GameState o = SaveCodec.Decode(defs, BeforeRoundTwo(SaveCodec.Encode(defs, s))).State;
            Assert.IsTrue(o.IntrosKnown);
            Assert.IsTrue(o.IntrosLateKnown, "settled once");
            foreach (string id in Intros.Late)
            {
                Assert.IsTrue(o.IntrosSeen.Contains(id), id + ": already used, no card");
                Assert.IsTrue(o.IntrosTried.Contains(id), id + ": and no gift to claim");
                Assert.IsFalse(Intros.HasDot(o, id), id);
            }
            Assert.IsNull(Intros.Next(o));
            Assert.AreEqual(clovers, o.Clovers, "nothing is paid for it");
        }

        [Test]
        public void A_Save_From_Before_The_Round_Two_Cards_Still_Gets_The_Cards_Of_Features_It_Never_Used()
        {
            OpenEverything();
            foreach (string id in Intros.All)
                if (System.Array.IndexOf(Intros.Late, id) < 0) { s.IntrosSeen.Add(id); s.IntrosTried.Add(id); }
            s.Trunk.Window = 40;
            GameState o = SaveCodec.Decode(defs, BeforeRoundTwo(SaveCodec.Encode(defs, s))).State;
            Assert.IsTrue(o.IntrosSeen.Contains(Intros.Trunk), "the trunk was visited");
            Assert.AreEqual(Intros.RecipeBook, Intros.Next(o), "the rest still come, one at a time");
            Assert.IsTrue(Intros.Show(o, Intros.RecipeBook));
            Assert.AreEqual(Intros.Aquarium, Intros.Next(o));
        }

        [Test, MonetizationOn]
        public void A_Save_From_Before_Any_Card_Marks_Every_Open_Round_Two_Card_Seen()
        {
            Used();
            GameState o = SaveCodec.Decode(defs, WithoutCards(SaveCodec.Encode(defs, s))).State;
            foreach (string id in Intros.All)
            {
                Assert.IsTrue(o.IntrosSeen.Contains(id), id);
                Assert.IsTrue(o.IntrosTried.Contains(id), id);
            }
            Assert.IsNull(Intros.Next(o));
        }

        [Test]
        public void A_Save_Made_After_The_Round_Two_Cards_Is_Not_Settled_Again()
        {
            Used();
            s.IntrosKnown = true;
            GameState o = SaveCodec.Decode(defs, SaveCodec.Encode(defs, s)).State;
            Assert.AreEqual(Intros.Fishing, Intros.Next(o), "a feature used since the cards came is not hidden by a reload");
            Assert.IsFalse(o.IntrosSeen.Contains(Intros.Trunk));
        }

        [Test]
        public void Settling_Twice_Changes_Nothing()
        {
            Used();
            s.IntrosKnown = true;
            s.IntrosLateKnown = false;
            Intros.Settle(s);
            var seen = new HashSet<string>(s.IntrosSeen);
            var tried = new HashSet<string>(s.IntrosTried);
            Intros.Settle(s);
            CollectionAssert.AreEquivalent(seen, s.IntrosSeen);
            CollectionAssert.AreEquivalent(tried, s.IntrosTried);
        }
    }
}
