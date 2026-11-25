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
    public class AwaySheetFeelTests
    {
        GameObject root;
        GameRunner runner;
        GardenScreen screen;
        FakeRewardedAds fake;

        [SetUp]
        public void SetUp()
        {
            L10n.SetLocale("vi");
            Motion.SetReduced(false);
            Haptics.SetOn(true);
            var catalog = AssetDatabase.LoadAssetAtPath<GameCatalog>("Assets/VuonTra/Content/GameCatalog.asset");
            root = new GameObject("Game");
            var cam = new GameObject("Cam", typeof(Camera));
            cam.transform.SetParent(root.transform);
            runner = root.AddComponent<GameRunner>();
            runner.Catalog = catalog;
            runner.Init(GameState.NewGame(catalog.BuildDefs()));
            fake = new FakeRewardedAds();
            runner.AdProvider = fake;
            screen = root.AddComponent<GardenScreen>();
            screen.Runner = runner;
            screen.Cam = cam.GetComponent<Camera>();
            screen.Raining = () => false;
            screen.Build();
            screen.Shell.StartGame();
            screen.Refresh();
            Step(1.5f);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(root);
            UiKit.SetLargeText(false);
            PlayerPrefs.DeleteKey("text.large");
            Motion.SetReduced(false);
            L10n.SetLocale("vi");
        }

        void Step(float seconds)
        {
            for (float t = 0f; t < seconds; t += 1f / 30f) screen.Advance(1f / 30f);
        }

        Button Find(string path)
        {
            Transform t = root.transform.Find(path);
            Assert.IsNotNull(t, "Missing UI object: " + path);
            return t.GetComponent<Button>();
        }

        Text Amount => root.transform.Find("Canvas/Away/Card/Carrots/Amount").GetComponent<Text>();
        Text TimeLine => root.transform.Find("Canvas/Away/Card/Carrots/Time").GetComponent<Text>();

        string LastFloat()
        {
            Text[] gains = root.transform.Find("Canvas/Feel").GetComponentsInChildren<Transform>(true)
                .Where(t => t.name == "Gain").Select(t => t.GetComponent<Text>()).ToArray();
            Assert.IsNotEmpty(gains, "a number floats up");
            return gains[gains.Length - 1].text;
        }

        AwayReport Show(double paid, double seconds = 2 * 3600, bool capped = false)
        {
            var away = new AwayReport { Paid = new BigNum(paid), Seconds = seconds, Capped = capped };
            runner.ShowAwayForTest(away);
            screen.ShowAway(away);
            Assert.IsTrue(screen.AwayCard.IsOpen);
            return away;
        }

        [Test]
        public void The_Carrots_Count_Up_From_Zero_In_Under_A_Second_With_A_Few_Ticks()
        {
            AwayReport away = Show(5000);
            string final = "+" + away.Paid.Short();
            Assert.AreEqual("+0", Amount.text, "the count starts at nothing");
            Step(AwayView.CountSeconds / 2f);
            Assert.AreNotEqual("+0", Amount.text);
            Assert.AreNotEqual(final, Amount.text, "still counting halfway");
            Step(AwayView.CountSeconds / 2f + 0.1f);
            Assert.AreEqual(final, Amount.text);
            Assert.Greater(screen.AwayCard.Ticks, 0, "the count ticks");
            Assert.LessOrEqual(screen.AwayCard.Ticks, AwayView.MaxTicks);
        }

        [Test]
        public void Collect_Flies_The_Carrots_From_The_Sheet_To_The_Purse_With_A_Buzz_And_The_Reward_Sound()
        {
            Show(5000);
            Step(1f);
            Sfx.ClearRecent();
            int buzzes = Haptics.Count;
            BigNum before = runner.State.Coins;
            Find("Canvas/Away/Card/Ok").onClick.Invoke();
            Assert.Greater(screen.Feel.Flying, 0, "the carrots fly to the purse");
            Assert.LessOrEqual(screen.Feel.Flying, GardenScreen.AwayFlyCount);
            Assert.Greater(Haptics.Count, buzzes, "a light buzz");
            CollectionAssert.Contains(Sfx.Recent, "reward");
            CollectionAssert.DoesNotContain(Sfx.Recent, "milestone");
            Assert.IsTrue(before.Equals(runner.State.Coins), "Collect pays nothing more: the carrots were in the purse already");
            Step(2f);
            Assert.AreEqual(0, screen.Feel.Flying, "all landed");
        }

        [Test]
        public void Times_Two_Sends_A_Second_Wave_With_The_Milestone_Sound_And_Floats_What_It_Paid()
        {
            AwayReport away = Show(5000);
            Step(1f);
            Sfx.ClearRecent();
            int buzzes = Haptics.Count;
            BigNum before = runner.State.Coins;
            Find("Canvas/Away/Card/Double").onClick.Invoke();
            Assert.AreEqual((before + away.Paid).ToDouble(), runner.State.Coins.ToDouble(), 1e-6);
            Assert.AreEqual("+" + away.Paid.Short(), LastFloat());
            Assert.Greater(screen.Feel.Flying, 0);
            Assert.Greater(Haptics.Count, buzzes);
            CollectionAssert.Contains(Sfx.Recent, "reward");
            CollectionAssert.Contains(Sfx.Recent, "milestone");
            Step(FeelLayer.FlyTime + 0.6f);
            Assert.Greater(screen.Feel.Flying, 0, "a second wave follows the first");
            Step(2f);
            Assert.AreEqual(0, screen.Feel.Flying);
        }

        [Test]
        public void A_Second_Time_Away_Paid_While_The_Video_Plays_Floats_The_Amount_Actually_Paid()
        {
            GameState s = runner.State;
            s.AwayDoubleDay = runner.TodayNumber;
            Show(5000);
            Assert.IsTrue(screen.AwayCard.Ad.Shown);
            Find("Canvas/Away/Card/Ad").onClick.Invoke();
            Assert.IsTrue(runner.AdShowing);
            AwayReport second = Show(91000, 3 * 3600, true);
            BigNum before = s.Coins;
            fake.Finish();
            Assert.AreEqual(second.Paid.ToDouble(), runner.LastAway.Paid.ToDouble(), 1e-6);
            Assert.AreEqual((before + runner.LastAway.Paid).ToDouble(), s.Coins.ToDouble(), 1e-6, "the video pays the report that came last");
            Assert.AreEqual("+" + runner.LastAway.Paid.Short(), LastFloat(), "and the number that floats up is that one");
        }

        [Test]
        public void Under_Reduced_Motion_The_Number_Is_There_At_Once_And_Nothing_Flies_But_Sound_And_Buzz_Stay()
        {
            Motion.SetReduced(true);
            AwayReport away = Show(5000);
            Assert.AreEqual("+" + away.Paid.Short(), Amount.text, "the final number on the first frame");
            Sfx.ClearRecent();
            int buzzes = Haptics.Count;
            Find("Canvas/Away/Card/Ok").onClick.Invoke();
            Assert.AreEqual(0, screen.Feel.Flying);
            Assert.Greater(Haptics.Count, buzzes);
            CollectionAssert.Contains(Sfx.Recent, "reward");

            Show(5000);
            Sfx.ClearRecent();
            Find("Canvas/Away/Card/Double").onClick.Invoke();
            Assert.AreEqual(0, screen.Feel.Flying);
            CollectionAssert.Contains(Sfx.Recent, "milestone");
        }

        [TestCase("vi", false)]
        [TestCase("en", false)]
        [TestCase("vi", true)]
        [TestCase("en", true)]
        public void The_Cap_Line_Says_Three_Hours_And_Four_With_The_Moon_Pass(string code, bool large)
        {
            L10n.SetLocale(code);
            UiKit.SetLargeText(large);
            screen.RebuildUi();
            GameState s = runner.State;

            ShowClaimed(s);
            Assert.AreEqual(L10n.T("away.time.capped", 3), TimeLine.text);
            AssertFits(TimeLine);
            screen.AwayCard.Close();

            MoonPass.Grant(s, runner.TodayNumber);
            ShowClaimed(s);
            Assert.AreEqual(L10n.T("away.time.capped.moon", 4), TimeLine.text);
            StringAssert.Contains("4", TimeLine.text);
            AssertFits(TimeLine);
        }

        void ShowClaimed(GameState s)
        {
            long now = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            s.Levels[0] = System.Math.Max(s.Levels[0], 5);
            s.LastSeenUnixSeconds = now - 10 * 3600;
            AwayReport away = Away.Claim(runner.Defs, s, now, Modifiers.From(runner.UpgradeDefs, s));
            Assert.IsTrue(away.Capped);
            Assert.IsTrue(away.WorthTelling);
            runner.ShowAwayForTest(away);
            screen.ShowAway(away);
            Assert.IsTrue(screen.AwayCard.IsOpen);
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
            Assert.LessOrEqual(h, size.y + 1f, "\"" + text.text + "\" fits its line");
        }
    }
}
