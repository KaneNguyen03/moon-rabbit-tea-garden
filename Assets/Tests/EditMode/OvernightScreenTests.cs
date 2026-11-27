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
    public class OvernightScreenTests
    {
        const long Hour = 3600;

        GameObject root;
        GameRunner runner;
        GardenScreen screen;
        MemoryAnalytics log;

        [SetUp]
        public void SetUp()
        {
            L10n.SetLocale("vi");
            Motion.SetReduced(true);
        }

        [TearDown]
        public void TearDown()
        {
            if (root != null) Object.DestroyImmediate(root);
            UiKit.SetLargeText(false);
            PlayerPrefs.DeleteKey("text.large");
            Motion.SetReduced(false);
            L10n.SetLocale("vi");
        }

        GameState Build()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<GameCatalog>("Assets/VuonTra/Content/GameCatalog.asset");
            Assert.IsNotNull(catalog, "Run VuonTra > Seed Content first.");
            root = new GameObject("Game");
            var cam = new GameObject("Cam", typeof(Camera));
            cam.transform.SetParent(root.transform);
            runner = root.AddComponent<GameRunner>();
            runner.Catalog = catalog;
            log = new MemoryAnalytics();
            runner.Analytics = log;
            GameState s = GameState.NewGame(catalog.BuildDefs());
            Rabbits.GiftFirst(s, "Bông");
            Rabbits.ArriveKind(s, "tho-me", "Mè");
            Rabbits.ArriveKind(s, "tho-cup", "Cụp");
            s.TutorialStep = Tutorial.Steps;
            s.HintSeen = true;
            s.Season = 1;
            s.Levels[0] = 5;
            runner.Init(s);
            runner.State.IntrosSeen.UnionWith(Intros.All);
            runner.State.IntrosTried.UnionWith(Intros.All);
            screen = root.AddComponent<GardenScreen>();
            screen.Runner = runner;
            screen.Cam = cam.GetComponent<Camera>();
            screen.Raining = () => false;
            screen.Build();
            screen.Shell.StartGame();
            screen.Refresh();
            return runner.State;
        }

        AwayReport ComeBack(GameState s, long hours)
        {
            long now = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            s.LastSeenUnixSeconds = now - hours * Hour;
            AwayReport away = Away.Claim(runner.Defs, s, now, Modifiers.From(runner.UpgradeDefs, s));
            Assert.IsTrue(away.WorthTelling);
            runner.ShowAwayForTest(away);
            screen.ShowAway(away);
            Assert.IsTrue(screen.AwayCard.IsOpen);
            return away;
        }

        void Wait(GameState s, params Story[] stories)
        {
            s.Overnight.Mark = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 100 * Hour;
            s.Overnight.Waiting.Clear();
            s.Overnight.Waiting.AddRange(stories);
        }

        RectTransform Row(string id) => (RectTransform)root.transform.Find("Canvas/Away/Card/" + id);

        static float Top(RectTransform rt) => rt.offsetMax.y;

        static float Bottom(RectTransform rt) => rt.offsetMin.y;

        [Test]
        public void A_Long_Night_Puts_The_Last_Night_Card_Right_After_The_Carrots_With_Two_Stories()
        {
            GameState s = Build();
            AwayReport away = ComeBack(s, 7);
            Assert.AreEqual(2, away.Stories.Count);
            RectTransform stories = Row("Stories"), carrots = Row("Carrots"), tray = Row("Tray"), guests = Row("Guests");
            Assert.IsTrue(stories.gameObject.activeSelf);
            Assert.AreEqual(2, screen.AwayCard.StoryRows.Count);
            Assert.LessOrEqual(Top(stories), Bottom(carrots), "the card sits below the carrots");
            Assert.LessOrEqual(Top(tray), Bottom(stories), "and above the tray");
            Assert.Less(Top(guests), Top(tray));
            Assert.AreEqual(L10n.T("overnight.title"), stories.Find("Head").GetComponent<Text>().text);
            Assert.AreEqual(AwayView.Height + AwayView.StoriesShift(2), screen.AwayCard.SheetHeight);
            Assert.AreEqual(screen.AwayCard.SheetHeight, screen.AwayCard.Sheet.Card.sizeDelta.y);
            foreach (StoryRowView row in screen.AwayCard.StoryRows)
            {
                Assert.AreEqual(AwayView.Line(row.Story, s), row.Text.text);
                Assert.GreaterOrEqual(((RectTransform)row.Button.Root).rect.height, UiKit.MinTouch, "a thumb-sized button");
            }
        }

        [Test]
        public void A_Short_Break_Has_No_Card_And_The_Sheet_Keeps_Its_Height()
        {
            GameState s = Build();
            ComeBack(s, 7);
            screen.AwayCard.Close();
            s.Overnight.Waiting.Clear();
            long now = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 20 * 60;
            s.LastSeenUnixSeconds = now - 20 * 60;
            s.Overnight.Mark = s.LastSeenUnixSeconds;
            var away = new AwayReport { Paid = new BigNum(5000), Seconds = 20 * 60 };
            screen.ShowAway(away);
            Assert.IsFalse(Row("Stories").gameObject.activeSelf);
            Assert.AreEqual(0, screen.AwayCard.StoryRows.Count);
            Assert.AreEqual(AwayView.Height, screen.AwayCard.SheetHeight);
            Assert.AreEqual(-500f, (Top(Row("Tray")) + Bottom(Row("Tray"))) / 2f, 0.01f, "the tray line is where it always was");
        }

        [Test]
        public void Taking_Each_Story_Puts_The_Gift_In_The_Right_Purse_Once()
        {
            GameState s = Build();
            Rabbit first = s.Rabbits[0];
            var clover = new Story { Kind = StoryKind.Clover, Amount = 1 };
            var leaves = new Story { Kind = StoryKind.Leaves, Rabbit = first.Id, Amount = 4 };
            Wait(s, clover, leaves);
            ComeBack(s, 2);
            int clovers = s.Clovers, recipeLeaves = s.RecipeLeaves;

            StoryRowView cloverRow = screen.AwayCard.RowOf(clover);
            cloverRow.Button.Button.onClick.Invoke();
            Assert.AreEqual(clovers + 1, s.Clovers, "the clover is in the purse");
            Assert.AreEqual(L10n.T("overnight.done"), cloverRow.Label.text);
            Assert.IsFalse(cloverRow.Button.Button.interactable);
            cloverRow.Button.Button.onClick.Invoke();
            Assert.AreEqual(clovers + 1, s.Clovers, "once");

            screen.AwayCard.RowOf(leaves).Button.Button.onClick.Invoke();
            Assert.AreEqual(recipeLeaves + 4, s.RecipeLeaves, "the leaves are in the recipe book");
            Assert.AreEqual(0, s.Overnight.Waiting.Count);
            Assert.AreEqual(2, log.Count(AnalyticsEvents.OvernightStory));
            Assert.AreEqual("leaves", log.Last(AnalyticsEvents.OvernightStory)[AnalyticsEvents.Kind].Text);
            Assert.AreEqual(1, log.Last(AnalyticsEvents.OvernightStory)[AnalyticsEvents.Claimed].Number);
        }

        [Test]
        public void The_Stranger_At_The_Gate_Is_Welcomed_Into_The_Garden_And_Two_Friends_Grow_Closer()
        {
            GameState s = Build();
            s.Rabbits[1].Station = -1;
            s.Rabbits[2].Station = -1;
            var guest = new Story { Kind = StoryKind.Stranger, Guest = "tho-suong" };
            var bond = new Story { Kind = StoryKind.Bond, Rabbit = s.Rabbits[1].Id, Friend = s.Rabbits[2].Id, Amount = 10 };
            Wait(s, guest, bond);
            ComeBack(s, 2);
            int rabbits = s.Rabbits.Count, a = s.Rabbits[1].Affection, b = s.Rabbits[2].Affection;
            StringAssert.Contains(L10n.T("kind.tho-suong"), screen.AwayCard.RowOf(guest).Text.text);
            Assert.AreEqual(L10n.T("overnight.welcome"), screen.AwayCard.RowOf(guest).Label.text);

            screen.AwayCard.RowOf(bond).Button.Button.onClick.Invoke();
            Assert.AreEqual(a + 10, s.Rabbits[1].Affection);
            Assert.AreEqual(b + 10, s.Rabbits[2].Affection);

            screen.AwayCard.RowOf(guest).Button.Button.onClick.Invoke();
            Assert.AreEqual(rabbits + 1, s.Rabbits.Count);
            Rabbit met = s.Rabbits[s.Rabbits.Count - 1];
            Assert.AreEqual("tho-suong", met.Kind);
            Assert.IsTrue(met.FirstOfKind);
            Assert.AreEqual(1, log.Count(AnalyticsEvents.RabbitCalled));
            Assert.AreEqual(AnalyticsEvents.ViaOvernight, log.Last(AnalyticsEvents.RabbitCalled)[AnalyticsEvents.Via].Text);
        }

        [TestCase("vi", false)]
        [TestCase("en", false)]
        [TestCase("vi", true)]
        [TestCase("en", true)]
        public void Every_Story_Line_And_Button_Fits_In_Both_Languages_And_Large_Text(string code, bool large)
        {
            L10n.SetLocale(code);
            UiKit.SetLargeText(large);
            GameState s = Build();
            s.Rabbits[0].Name = "Bánh Bao Nhỏ";
            s.Rabbits[1].Name = "Mochi Mây";
            s.Rabbits[1].Station = -1;
            s.Rabbits[2].Station = -1;
            Rabbits.ArriveKind(s, "tho-suong", "Sương Sớm");
            var all = new[]
            {
                new Story { Kind = StoryKind.Leaves, Rabbit = s.Rabbits[0].Id, Amount = 5 },
                new Story { Kind = StoryKind.Clover, Amount = 1 },
                new Story { Kind = StoryKind.Stranger, Guest = "tho-o-long" },
                new Story { Kind = StoryKind.Bond, Rabbit = s.Rabbits[1].Id, Friend = s.Rabbits[0].Id, Amount = 10 },
            };
            var dozes = new System.Collections.Generic.List<Story>();
            for (int r = 0; r < s.Rabbits.Count; r++)
                for (int line = 0; line < Overnight.DozeLines; line++) dozes.Add(new Story { Kind = StoryKind.Doze, Rabbit = s.Rabbits[r].Id, Line = line });
            var shown = new System.Collections.Generic.List<Story>(all);
            shown.AddRange(dozes);
            for (int i = 0; i < shown.Count; i += 2)
            {
                Wait(s, shown.GetRange(i, System.Math.Min(2, shown.Count - i)).ToArray());
                ComeBack(s, 2);
                foreach (StoryRowView row in screen.AwayCard.StoryRows)
                {
                    StringAssert.DoesNotContain("overnight.", row.Text.text, "a real line, not a key");
                    AssertFits(row.Text);
                    AssertFits(row.Label);
                    Assert.GreaterOrEqual(Used(row.Text), 16, "\"" + row.Text.text + "\" stays readable");
                }
                AssertFits(Row("Stories").Find("Head").GetComponent<Text>());
                screen.AwayCard.Close();
            }
        }

        static int Used(Text text)
        {
            Canvas.ForceUpdateCanvases();
            var gen = new TextGenerator();
            gen.Populate(text.text, text.GetGenerationSettings(text.rectTransform.rect.size));
            return text.resizeTextForBestFit ? Mathf.RoundToInt(gen.fontSizeUsedForBestFit / text.pixelsPerUnit) : text.fontSize;
        }

        static void AssertFits(Text text)
        {
            Canvas.ForceUpdateCanvases();
            Vector2 size = text.rectTransform.rect.size;
            var gen = new TextGenerator();
            TextGenerationSettings settings = text.GetGenerationSettings(size);
            gen.Populate(text.text, settings);
            settings.fontSize = text.resizeTextForBestFit ? gen.fontSizeUsedForBestFit : settings.fontSize;
            settings.resizeTextForBestFit = false;
            settings.verticalOverflow = VerticalWrapMode.Overflow;
            float h = gen.GetPreferredHeight(text.text, settings) / text.pixelsPerUnit;
            Assert.LessOrEqual(h, size.y + 1f, "\"" + text.text + "\" fits its box");
            settings.horizontalOverflow = HorizontalWrapMode.Overflow;
            string[] words = text.text.Split(' ');
            foreach (string word in words)
                Assert.LessOrEqual(gen.GetPreferredWidth(word, settings) / text.pixelsPerUnit, size.x + 1f, "\"" + word + "\" fits the width");
        }
    }
}
