using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using VuonTra.Game;
using VuonTra.Game.UI;
using L10n = VuonTra.Game.L10n;
using Motion = VuonTra.Game.UI.Motion;

namespace VuonTra.Core.Tests
{
    [MonetizationOn]
    public class FirstSessionScreenTests
    {
        const int HalfHour = 30 * 60;
        const float ReadSeconds = 4f;

        GameObject root;
        GameRunner runner;
        GardenScreen screen;
        long wall;
        System.Random rng;

        sealed class Seen
        {
            public string What;
            public float Open, Close = -1f;
            public bool GuidanceDone;
            public double Played;
        }

        [SetUp]
        public void SetUp()
        {
            L10n.SetLocale("vi");
            UiKit.SetLargeText(false);
            Motion.SetReduced(true);
            var catalog = AssetDatabase.LoadAssetAtPath<GameCatalog>("Assets/VuonTra/Content/GameCatalog.asset");
            Assert.IsNotNull(catalog, "Run VuonTra > Seed Content first.");
            root = new GameObject("Game");
            var cam = new GameObject("Cam", typeof(Camera));
            cam.transform.SetParent(root.transform);
            runner = root.AddComponent<GameRunner>();
            runner.Catalog = catalog;
            wall = 20_800L * Offers.DaySeconds + 10 * 3600L;
            runner.DayClock = () => wall / Offers.DaySeconds;
            runner.OfferWallClock = () => wall;
            runner.OfferCardsOn = true;
            runner.Store = new FakeStorefront();
            GameState s = GameState.NewGame(catalog.BuildDefs());
            Rabbits.GiftFirst(s, "Bông");
            s.IntrosKnown = true;
            runner.Init(s);
            runner.SetBuyMode(1);
            screen = root.AddComponent<GardenScreen>();
            screen.Runner = runner;
            screen.Cam = cam.GetComponent<Camera>();
            screen.Raining = () => false;
            screen.Build();
            screen.Shell.StartGame();
            screen.Refresh();
            rng = new System.Random(20261122);
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(root);
            Motion.SetReduced(false);
            UiKit.SetLargeText(false);
            PlayerPrefs.DeleteKey("text.large");
            L10n.SetLocale("vi");
        }

        GameState S => runner.State;

        void PlayOneSecond(int second)
        {
            runner.Tap(BestStation());
            if (second % 3 == 0) BuyCheapest();
            if (second % 30 == 0)
                for (int q = 0; q < runner.QuestDefs.Count; q++) runner.ClaimQuest(q, out _);
            runner.GrantTutorialClover();
            while (S.Clovers >= Rabbits.SummonCost && runner.SummonRabbit() != null) { }
            runner.AutoAssignRabbits();
            Simulation.Tick(runner.Defs, S, 1d, null, runner.Mods, rng.NextDouble);
            S.PlaySeconds += 1d;
            wall += 1;
            screen.Advance(1f);
        }

        int BestStation()
        {
            int best = 0;
            for (int i = 0; i < runner.Defs.Count; i++)
                if (runner.Defs[i].Kind == StationKind.Brew && S.Levels[i] > 0 && runner.Defs[i].BaseIncome > runner.Defs[best].BaseIncome) best = i;
            return best;
        }

        void BuyCheapest()
        {
            if (runner.BuyPlot()) return;
            int best = -1;
            BigNum bestCost = BigNum.Zero;
            for (int i = 0; i < runner.Defs.Count; i++)
            {
                if (S.Levels[i] == 0 && !Economy.CanUnlock(runner.Defs, S, i)) continue;
                if (Economy.LevelsLeft(S.Levels[i]) <= 0) continue;
                BigNum cost = Economy.CostOfNext(runner.Defs[i], S.Levels[i], 1);
                if (best < 0 || cost < bestCost) { best = i; bestCost = cost; }
            }
            if (best >= 0 && S.Coins >= bestCost) runner.Buy(best);
        }

        string OpenCard()
        {
            if (screen.IntroCard.IsOpen) return "intro " + screen.IntroCard.Showing;
            if (screen.OfferCard.IsOpen) return "offer " + screen.OfferCard.Offer.Id;
            if (screen.NewDishCard.IsOpen) return "dish";
            if (screen.Arrival.IsOpen) return "arrival";
            if (screen.ReviewCard.IsOpen) return "review";
            if (screen.TankCard.IsOpen) return "tank";
            if (screen.FairCard.IsOpen) return "fair";
            if (screen.WelcomeCard.IsOpen) return "welcome";
            if (screen.TrunkCard.IsOpen) return "trunk";
            if (screen.AwayCard.IsOpen) return "away";
            return screen.CardShowing ? "card" : null;
        }

        void CloseCard()
        {
            if (screen.IntroCard.IsOpen) root.transform.Find("Canvas/Intro/Later").GetComponent<Button>().onClick.Invoke();
            if (screen.OfferCard.IsOpen) screen.OfferCard.Close();
            if (screen.NewDishCard.IsOpen) screen.NewDishCard.Close();
            if (screen.Arrival.IsOpen) screen.Arrival.Close();
            if (screen.ReviewCard.IsOpen) screen.ReviewCard.Close();
            if (screen.TankCard.IsOpen) screen.TankCard.Close();
            if (screen.FairCard.IsOpen) screen.FairCard.Close();
            if (screen.WelcomeCard.IsOpen) screen.WelcomeCard.Close();
            if (screen.TrunkCard.IsOpen) screen.TrunkCard.Close();
            if (screen.AwayCard.IsOpen) screen.AwayCard.Close();
        }

        List<Seen> PlayHalfHour()
        {
            var seen = new List<Seen>();
            Seen up = null;
            for (int second = 1; second <= HalfHour; second++)
            {
                PlayOneSecond(second);
                string card = OpenCard();
                if (up == null && card != null)
                {
                    up = new Seen { What = card, Open = screen.Clock, GuidanceDone = Tutorial.IsDone(S), Played = S.PlaySeconds };
                    seen.Add(up);
                }
                if (up != null && screen.Clock - up.Open >= ReadSeconds)
                {
                    CloseCard();
                    up.Close = screen.Clock;
                    up = null;
                }
            }
            return seen;
        }

        static string Timeline(List<Seen> seen) =>
            string.Join("\n", seen.Select(c => c.What + " " + c.Open.ToString("0") + "–" + c.Close.ToString("0") + " s" + (c.GuidanceDone ? "" : " (guidance not done)")));

        [Test]
        public void In_A_Played_First_Half_Hour_Two_Cards_Never_Come_Within_Twenty_Seconds_Of_Each_Other()
        {
            List<Seen> seen = PlayHalfHour();
            string timeline = Timeline(seen);
            TestContext.WriteLine(timeline);
            Assert.IsTrue(Tutorial.IsDone(S), "the half hour gets through the guidance\n" + timeline);
            Assert.GreaterOrEqual(seen.Count, 3, "the half hour shows cards\n" + timeline);
            Assert.IsTrue(seen.Any(c => c.What.StartsWith("intro")), "intro cards among them\n" + timeline);
            Assert.IsTrue(seen.Any(c => c.What == "offer " + Offers.Starter), "and the starter pack\n" + timeline);
            for (int i = 1; i < seen.Count; i++)
                Assert.GreaterOrEqual(seen[i].Open - seen[i - 1].Close, (float)CardPacer.GapSeconds - 0.01f,
                    "twenty seconds of garden between " + seen[i - 1].What + " and " + seen[i].What + "\n" + timeline);
        }

        [Test]
        public void No_Purchase_Card_Comes_Before_The_Guidance_Is_Done_Nor_The_Starter_Pack_Before_Minute_Three()
        {
            List<Seen> seen = PlayHalfHour();
            string timeline = Timeline(seen);
            foreach (Seen c in seen.Where(c => c.What.StartsWith("offer")))
            {
                Assert.IsTrue(c.GuidanceDone, "no pack card during the guidance\n" + timeline);
                Assert.GreaterOrEqual(c.Played, Offers.StarterQuietSeconds, "nor before minute three\n" + timeline);
            }
        }

        [Test]
        public void A_Card_That_Wants_To_Come_Right_After_Another_Waits_Its_Twenty_Seconds()
        {
            S.TutorialStep = Tutorial.Steps;
            S.PlaySeconds = Offers.StarterQuietSeconds;
            S.GardenXp = Garden.XpForLevel(Offers.StarterLevel);
            S.IntrosSeen.UnionWith(Intros.All);
            S.IntrosTried.UnionWith(Intros.All);
            typeof(GameRunner).GetMethod("NoteOffers", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(runner, null);
            screen.Refresh();
            screen.Advance(0.1f);
            Assert.IsTrue(screen.OfferCard.IsOpen, "the pack card comes on a quiet garden");
            screen.Advance(1f);
            screen.OfferCard.Close();
            S.IntrosSeen.Remove(Intros.Fishing);
            S.IntrosTried.Remove(Intros.Fishing);
            for (int k = 0; k < 19; k++) screen.Advance(1f);
            Assert.IsFalse(screen.IntroCard.IsOpen, "the intro card waits");
            Assert.IsFalse(screen.CardsRested);
            Assert.Greater(screen.CardWait, 0f);
            screen.Advance(1.1f);
            Assert.IsTrue(screen.IntroCard.IsOpen, "twenty seconds of garden later it comes");
            Assert.AreEqual(Intros.Fishing, screen.IntroCard.Showing);
        }
    }
}
