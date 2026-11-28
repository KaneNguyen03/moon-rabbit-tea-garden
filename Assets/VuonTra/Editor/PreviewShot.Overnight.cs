using UnityEditor.SceneManagement;
using VuonTra.Core;
using VuonTra.Game;
using VuonTra.Game.UI;
using L10n = VuonTra.Game.L10n;
using Motion = VuonTra.Game.UI.Motion;
using Object = UnityEngine.Object;

namespace VuonTra.EditorTools
{
    public static partial class PreviewShot
    {
        static void RenderOvernight()
        {
            foreach (string locale in BothLocales) RenderOvernightIn(locale);
        }

        static void RenderOvernightIn(string locale)
        {
            EditorSceneManager.OpenScene(ContentSeeder.ScenePath, OpenSceneMode.Single);
            L10n.SetLocale(locale);
            bool reduced = Motion.Reduced;
            Motion.Reduced = false;
            var runner = Object.FindAnyObjectByType<GameRunner>();
            var screen = Object.FindAnyObjectByType<GardenScreen>();
            var defs = runner.Catalog.BuildDefs();
            GameState s = GameState.NewGame(defs);
            int[] levels = { 27, 12, 9, 4 };
            for (int i = 0; i < levels.Length; i++) s.Levels[i] = levels[i];
            s.GardenXp = Garden.XpForLevel(31);
            s.HintSeen = true;
            s.TutorialStep = Tutorial.Steps;
            s.Coins = new BigNum(84000);
            Rabbit bong = Rabbits.ArriveKind(s, "tho-bong", "Bông");
            Rabbit me = Rabbits.ArriveKind(s, "tho-me", "Mè");
            Rabbit suong = Rabbits.ArriveKind(s, "tho-suong", "Sương");
            Rabbit lam = Rabbits.ArriveKind(s, "tho-cup", "Lam");
            Rabbits.Assign(s, bong.Id, 0);
            Rabbits.Assign(s, me.Id, 1);
            s.FreshKinds.Clear();
            runner.Init(s);
            screen.Build();
            screen.ClockHours = () => 7.0;
            screen.Raining = () => false;
            screen.Refresh();
            screen.Shell.StartGame();
            screen.SelectStation(-1);
            Step(screen, 1.4f);

            Night(runner, screen, s, new Story { Kind = StoryKind.Clover, Amount = 1 }, new Story { Kind = StoryKind.Leaves, Rabbit = bong.Id, Amount = 4 });
            Capture(screen, "174-co-va-la-" + locale);
            Press(screen, "Canvas/Away/Card/Stories/Story0/Take");
            Step(screen, 0.4f);
            Capture(screen, "174-dang-bay-" + locale);
            Step(screen, 2f);
            Capture(screen, "174-da-nhan-" + locale);
            screen.AwayCard.Close();
            Step(screen, 0.5f);

            Night(runner, screen, s, new Story { Kind = StoryKind.Stranger, Guest = "tho-o-long" }, new Story { Kind = StoryKind.Bond, Rabbit = suong.Id, Friend = lam.Id, Amount = 10 });
            Capture(screen, "174-tho-la-va-than-" + locale);
            screen.AwayCard.Close();
            Step(screen, 0.5f);

            Night(runner, screen, s, new Story { Kind = StoryKind.Doze, Rabbit = me.Id, Line = 1 });
            Capture(screen, "174-ngu-gat-" + locale);
            screen.AwayCard.Close();
            Step(screen, 0.5f);

            UiKit.SetLargeText(true);
            screen.RebuildUi();
            Night(runner, screen, s, new Story { Kind = StoryKind.Stranger, Guest = "tho-o-long" }, new Story { Kind = StoryKind.Doze, Rabbit = suong.Id, Line = 0 });
            Capture(screen, "174-chu-to-" + locale);
            screen.AwayCard.Close();
            UiKit.SetLargeText(false);
            Motion.Reduced = reduced;
        }

        static void Night(GameRunner runner, GardenScreen screen, GameState s, params Story[] stories)
        {
            long now = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            s.Overnight.Mark = now + 100 * 3600;
            s.Overnight.Waiting.Clear();
            s.Overnight.Waiting.AddRange(stories);
            s.LastSeenUnixSeconds = now - 9 * 3600;
            AwayReport away = Away.Claim(runner.Defs, s, now, Modifiers.From(runner.UpgradeDefs, s), runner.TodayNumber);
            runner.ShowAwayForTest(away);
            screen.ShowAway(away);
            Step(screen, 1.2f);
        }
    }
}
