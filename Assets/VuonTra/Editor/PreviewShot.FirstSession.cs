using System.IO;
using UnityEditor.SceneManagement;
using UnityEngine;
using VuonTra.Core;
using VuonTra.Game;
using VuonTra.Game.UI;
using L10n = VuonTra.Game.L10n;

namespace VuonTra.EditorTools
{
    public static partial class PreviewShot
    {
        const int FirstSessionSeconds = 30 * 60;
        const float FirstSessionReadSeconds = 4f;
        static readonly int[] FirstSessionMarks = { 60, 5 * 60, 15 * 60, 30 * 60 };

        public static void RenderFirstSessionOnly()
        {
            EditorSceneManager.OpenScene(ContentSeeder.ScenePath, OpenSceneMode.Single);
            L10n.SetLocale("vi");
            Features.Monetization = true;
            var runner = Object.FindAnyObjectByType<GameRunner>();
            var screen = Object.FindAnyObjectByType<GardenScreen>();
            runner.Init(GameState.NewGame(runner.Catalog.BuildDefs()));
            screen.Build();
            screen.ClockHours = () => 22.0;
            Directory.CreateDirectory(Folder);
            RenderFirstSession(runner, screen);
        }

        static void RenderFirstSession(GameRunner runner, GardenScreen screen)
        {
            System.Func<long> dayBefore = runner.DayClock, offerBefore = runner.OfferWallClock;
            bool cardsBefore = runner.OfferCardsOn;
            foreach (string locale in new[] { "vi", "en" })
            {
                long[] wall = { (runner.TodayNumber + 1) * Offers.DaySeconds + 9 * 3600L };
                runner.DayClock = () => wall[0] / Offers.DaySeconds;
                runner.OfferWallClock = () => wall[0];
                runner.Store = new FakeStorefront();
                GameState s = GameState.NewGame(runner.Catalog.BuildDefs());
                Rabbits.GiftFirst(s, "Bông");
                s.IntrosKnown = true;
                runner.Init(s);
                runner.OfferCardsOn = true;
                runner.SetBuyMode(1);
                L10n.SetLocale(locale);
                screen.Raining = () => false;
                screen.RebuildUi();
                if (screen.Shell.State == ShellState.Title) screen.Shell.StartGame();
                if (screen.Shell.State != ShellState.Playing) screen.Shell.Resume();
                screen.SelectStation(-1);
                screen.LookAt(screen.StationSpot(0) + new Vector2(0f, -120f));
                screen.Refresh();
                Step(screen, 0.4f);
                Capture(screen, "115-giay-0-" + locale);

                var rng = new System.Random(20261122);
                float openedAt = -1f;
                for (int second = 1; second <= FirstSessionSeconds; second++)
                {
                    PlayFirstSecond(runner, s, second, rng);
                    wall[0] += 1;
                    screen.Advance(1f);
                    if (screen.CardShowing && openedAt < 0f) openedAt = screen.Clock;
                    if (System.Array.IndexOf(FirstSessionMarks, second) >= 0)
                    {
                        Step(screen, 0.4f);
                        Capture(screen, "115-" + (second < 300 ? "giay-" + second : "phut-" + second / 60) + "-" + locale);
                    }
                    if (openedAt >= 0f && screen.Clock - openedAt >= FirstSessionReadSeconds)
                    {
                        CloseFirstSessionCard(screen);
                        openedAt = -1f;
                    }
                }
                CloseFirstSessionCard(screen);
                ShowCardWaiting(runner, screen, s, locale);
            }
            runner.OfferCardsOn = cardsBefore;
            runner.DayClock = dayBefore;
            runner.OfferWallClock = offerBefore;
            screen.Raining = null;
            L10n.SetLocale("vi");
            screen.RebuildUi();
            Step(screen, 0.3f);
        }

        static void ShowCardWaiting(GameRunner runner, GardenScreen screen, GameState s, string locale)
        {
            Step(screen, (float)CardPacer.GapSeconds + 1f);
            s.IntrosSeen.Remove(Intros.Fishing);
            s.IntrosTried.Remove(Intros.Fishing);
            s.IntrosLater.Remove(Intros.Fishing);
            screen.IntroQueue.Reset();
            screen.OpenOffer(Offers.Find(Offers.Starter));
            Step(screen, 0.6f);
            Capture(screen, "115-the-truoc-" + locale);
            screen.OfferCard.Close();
            Step(screen, 3f);
            if (screen.IntroShowing) throw new System.Exception("Preview: the intro card did not wait its twenty seconds");
            Capture(screen, "115-the-dang-cho-" + locale);
            Step(screen, (float)CardPacer.GapSeconds - 2.5f);
            if (!screen.IntroShowing) throw new System.Exception("Preview: the waiting intro card did not come");
            Capture(screen, "115-the-den-sau-20-giay-" + locale);
            Press(screen, "Canvas/Intro/Later");
            Step(screen, 0.4f);
        }

        static void PlayFirstSecond(GameRunner runner, GameState s, int second, System.Random rng)
        {
            int best = 0;
            for (int i = 0; i < runner.Defs.Count; i++)
                if (runner.Defs[i].Kind == StationKind.Brew && s.Levels[i] > 0 && runner.Defs[i].BaseIncome > runner.Defs[best].BaseIncome) best = i;
            runner.Tap(best);
            if (second % 3 == 0 && !runner.BuyPlot())
            {
                int cheapest = -1;
                BigNum cost = BigNum.Zero;
                for (int i = 0; i < runner.Defs.Count; i++)
                {
                    if (s.Levels[i] == 0 && !Economy.CanUnlock(runner.Defs, s, i)) continue;
                    if (Economy.LevelsLeft(s.Levels[i]) <= 0) continue;
                    BigNum next = Economy.CostOfNext(runner.Defs[i], s.Levels[i], 1);
                    if (cheapest < 0 || next < cost) { cheapest = i; cost = next; }
                }
                if (cheapest >= 0 && s.Coins >= cost) runner.Buy(cheapest);
            }
            if (second % 30 == 0)
                for (int q = 0; q < runner.QuestDefs.Count; q++) runner.ClaimQuest(q, out _);
            runner.GrantTutorialClover();
            while (s.Clovers >= Rabbits.SummonCost && runner.SummonRabbit() != null) { }
            runner.AutoAssignRabbits();
            Simulation.Tick(runner.Defs, s, 1d, null, runner.Mods, rng.NextDouble);
            s.PlaySeconds += 1d;
        }

        static void CloseFirstSessionCard(GardenScreen screen)
        {
            if (screen.IntroShowing) Press(screen, "Canvas/Intro/Later");
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
    }
}
