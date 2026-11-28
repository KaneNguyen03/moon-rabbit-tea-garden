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
    public class RainScreenTests
    {
        GameObject root;
        GameRunner runner;
        GardenScreen screen;
        float musicBefore, sfxBefore;

        [SetUp]
        public void SetUp()
        {
            musicBefore = Sfx.MusicGain;
            sfxBefore = Sfx.SfxGain;
            L10n.SetLocale("vi");
            var catalog = AssetDatabase.LoadAssetAtPath<GameCatalog>("Assets/VuonTra/Content/GameCatalog.asset");
            root = new GameObject("Game");
            var cam = new GameObject("Cam", typeof(Camera));
            cam.transform.SetParent(root.transform);
            runner = root.AddComponent<GameRunner>();
            runner.Catalog = catalog;
            runner.Init(GameState.NewGame(catalog.BuildDefs()));
            runner.State.IntrosSeen.UnionWith(Intros.All);
            runner.State.IntrosTried.UnionWith(Intros.All);
            screen = root.AddComponent<GardenScreen>();
            screen.Runner = runner;
            screen.Cam = cam.GetComponent<Camera>();
            screen.Raining = () => false;
            screen.Build();
            screen.Refresh();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(root);
            Motion.SetReduced(false);
            Sfx.SetRain(false);
            Sfx.SetMusic(musicBefore);
            Sfx.SetSfx(sfxBefore);
        }

        void Step(float seconds)
        {
            for (float t = 0f; t < seconds; t += 0.1f) screen.Advance(0.1f);
        }

        Image Glow => root.transform.Find("Canvas/Garden/Content/LanternGlow").GetComponent<Image>();

        [Test]
        public void Rain_Lays_Its_Colour_And_Streaks_Over_The_Garden_And_Lights_The_Lanterns_A_Fifth_Brighter()
        {
            Motion.SetReduced(true);
            screen.Advance(0.1f);
            Assert.AreEqual(0f, screen.RainLevel);
            Assert.IsFalse(root.transform.Find("Canvas/Rain").gameObject.activeSelf, "dry: no rain layer");
            Assert.AreEqual(0.6f, Glow.color.a, 0.001f);

            Motion.SetReduced(false);
            screen.Raining = () => true;
            Step(2f);
            Assert.AreEqual(1f, screen.RainLevel, 1e-4f, "the rain comes on in a second and a half");
            Assert.IsTrue(screen.RainStreaksShown, "streaks fall");
            Assert.Greater(screen.RainTintAlpha, 0.1f, "a colour over the garden");
            Assert.LessOrEqual(Glow.color.a, 0.6f * GardenScreen.RainLanternBoost + 0.001f, "brighter, a fifth at most");
            Transform streak = root.transform.Find("Canvas/Rain/Streaks/Streak");
            Vector2 before = ((RectTransform)streak).anchoredPosition;
            screen.Advance(0.1f);
            Assert.AreNotEqual(before, ((RectTransform)streak).anchoredPosition, "the streaks fall");
            Assert.IsFalse(streak.GetComponent<Image>().raycastTarget, "the rain never takes a touch");
            Assert.IsFalse(root.transform.Find("Canvas/RainTint").GetComponent<Image>().raycastTarget);

            Motion.SetReduced(true);
            screen.Advance(0.1f);
            Assert.IsFalse(screen.RainStreaksShown, "reduced motion takes the streaks away");
            Assert.Greater(screen.RainTintAlpha, 0.1f, "and keeps the colour");
            Assert.AreEqual(0.6f * GardenScreen.RainLanternBoost, Glow.color.a, 0.001f);
            Assert.AreEqual(0f, root.transform.Find("Canvas/Garden/Content/RainRipple").GetComponent<Image>().color.a, "still water");

            screen.Raining = () => false;
            screen.Advance(0.1f);
            Assert.AreEqual(0f, screen.RainLevel);
            Assert.IsFalse(root.transform.Find("Canvas/Rain").gameObject.activeSelf);
            Assert.AreEqual(0.6f, Glow.color.a, 0.001f, "dry again, the lanterns as before");
        }

        [Test]
        public void The_Pond_Ripples_With_Drops_While_It_Rains()
        {
            screen.Raining = () => true;
            float most = 0f;
            for (int k = 0; k < 30; k++)
            {
                screen.Advance(0.1f);
                most = Mathf.Max(most, root.transform.Find("Canvas/Garden/Content/RainRipple").GetComponent<Image>().color.a);
            }
            Assert.Greater(most, 0.05f);
        }

        [Test]
        public void The_Rains_Patter_Replaces_The_Ambient_Bed_And_Follows_The_Sound_Slider()
        {
            Sfx.SetMusic(0.6f);
            Sfx.SetSfx(0.8f);
            screen.Advance(0.1f);
            Assert.IsFalse(Sfx.Raining);
            Assert.AreSame(Sfx.AmbientClip, Sfx.BedClip, "dry: the ambient bed");

            screen.Raining = () => true;
            screen.Advance(0.1f);
            Assert.IsTrue(Sfx.Raining);
            Assert.AreSame(Sfx.RainClip, Sfx.BedClip, "the patter in its place");
            Assert.AreNotSame(Sfx.AmbientClip, Sfx.RainClip);
            Assert.Greater(Sfx.BedVolume, 0f);
            float loud = Sfx.BedVolume;

            Sfx.SetSfx(0f);
            Assert.AreEqual(0f, Sfx.BedVolume, 1e-6f, "the Sound slider at nought silences the rain");
            Sfx.SetMusic(1f);
            Assert.AreEqual(0f, Sfx.BedVolume, 1e-6f, "the Music slider does not bring it back");
            Sfx.SetSfx(0.8f);
            Assert.AreEqual(loud, Sfx.BedVolume, 1e-6f);

            screen.Raining = () => false;
            screen.Advance(0.1f);
            Assert.AreSame(Sfx.AmbientClip, Sfx.BedClip, "dry again: the ambient bed, under the Music slider");
            Sfx.SetSfx(0f);
            Assert.Greater(Sfx.BedVolume, 0f);
        }

        [Test]
        public void The_Rabbits_Resting_On_The_Rug_And_In_The_Hammock_Run_Under_A_Tree_And_Back_And_Nothing_Is_Saved()
        {
            Motion.SetReduced(true);
            GameState s = runner.State;
            s.RestSpots = 6;
            for (int i = 0; i < 6; i++) Rabbits.Arrive(s, 0, "R" + i, 0.1 * i);
            screen.Refresh();
            screen.Advance(0.1f);
            RectTransform Seat(int k) => (RectTransform)root.transform.Find("Canvas/Garden/Content/Seat " + k);
            Vector2 rug = Seat(2).anchoredPosition, hammock = Seat(4).anchoredPosition, stump = Seat(0).anchoredPosition;
            string save = SaveCodec.Encode(runner.Defs, s);

            screen.Raining = () => true;
            screen.Advance(0.1f);
            Assert.Greater(Vector2.Distance(rug, Seat(2).anchoredPosition), 100f, "the rug's rabbit runs for cover");
            Assert.Greater(Vector2.Distance(hammock, Seat(4).anchoredPosition), 40f, "and the hammock's steps off");
            Assert.Less(Seat(4).anchoredPosition.x, hammock.x, "towards the trees at the wood's edge");
            Assert.AreEqual(stump, Seat(0).anchoredPosition, "the stumps stay where they are");
            Assert.AreEqual(0f, Mathf.DeltaAngle(0f, Seat(4).Find("Art").localEulerAngles.z), 0.01f, "under a tree it sits up");
            screen.Refresh();
            screen.Advance(0.1f);
            Assert.AreEqual(0f, Mathf.DeltaAngle(0f, Seat(4).Find("Art").localEulerAngles.z), 0.01f, "and stays up through a refresh");
            Assert.AreEqual(save, SaveCodec.Encode(runner.Defs, s), "where a rabbit shelters is not saved");

            screen.Raining = () => false;
            screen.Advance(0.1f);
            Assert.AreEqual(rug, Seat(2).anchoredPosition);
            Assert.AreEqual(hammock, Seat(4).anchoredPosition);
            Assert.AreEqual(-72f, Mathf.DeltaAngle(0f, Seat(4).Find("Art").localEulerAngles.z), 0.01f, "back in the hammock, lying down");
        }

        double ShowerHour() => Weather.ShowerStarts(runner.TodayNumber)[0] + 1d / 60d;

        void FrogGarden()
        {
            screen.Shell.SkipTitle();
            GameState s = runner.State;
            s.TutorialStep = Tutorial.Steps;
            s.GardenXp = Garden.XpForLevel(FrogRabbit.FromLevel);
            s.Levels[0] = 40;
            screen.Refresh();
        }

        [Test]
        public void The_Frog_Rabbit_Comes_Only_In_The_Rain_Walks_From_Its_Gate_To_The_Pond_And_A_Tap_Pays_Ten_Minutes()
        {
            FrogGarden();
            GameState s = runner.State;
            double start = Weather.ShowerStarts(runner.TodayNumber)[0];
            screen.ClockHours = () => start - 0.5d;
            Step(1f);
            Assert.IsFalse(screen.FrogShown, "dry: no frog");
            Assert.IsFalse(FrogRabbit.Here(s));

            screen.ClockHours = ShowerHour;
            screen.Raining = () => true;
            screen.Advance(0.1f);
            Assert.IsTrue(FrogRabbit.Here(s));
            Assert.IsTrue(screen.FrogShown, "it steps out of its gate");
            Assert.IsTrue(root.transform.Find("Canvas/Garden/Content/FrogGate").gameObject.activeSelf, "the little moon gate opens");
            Assert.Less(Vector2.Distance(screen.FrogRect.anchoredPosition, GardenScreen.FrogGateSpot), 40f);
            Assert.IsFalse(screen.FrogWaiting);
            Step(GardenScreen.FrogWalkSeconds + 0.6f);
            Assert.IsTrue(screen.FrogWaiting, "at the pond");
            Assert.Less(Vector2.Distance(screen.FrogRect.anchoredPosition, GardenScreen.FrogPondSpot), 20f);
            Assert.AreEqual("Thỏ Ếch", screen.FrogRect.Find("Name/Text").GetComponent<Text>().text);
            Image hat = screen.FrogRect.Find("Art/Hat").GetComponent<Image>();
            Assert.AreEqual(GardenScreen.FrogGreen, hat.color, "the wool hat, dyed frog green");

            BigNum reward = FrogRabbit.Reward(runner.Defs, s, runner.Mods);
            Assert.Greater(reward.ToDouble(), 0d);
            BigNum coins = s.Coins;
            int clovers = s.Clovers;
            root.transform.Find("Canvas/Garden/Content/Frog").GetComponent<Button>().onClick.Invoke();
            Assert.AreEqual((coins + reward).ToDouble(), s.Coins.ToDouble(), reward.ToDouble() * 1e-6, "ten minutes of income");
            Assert.That(s.Clovers - clovers, Is.InRange(0, 1));
            Assert.AreEqual(1, s.FrogsMet);
            Assert.IsFalse(FrogRabbit.Here(s));
            Step(GardenScreen.FrogWalkSeconds + 0.8f);
            Assert.IsFalse(screen.FrogShown, "it hops home");

            Step(2f);
            Assert.IsFalse(screen.FrogShown);
            Assert.AreEqual(1, s.FrogsToday);
        }

        [Test]
        public void The_Frog_Rabbit_Goes_Home_Ungreeted_When_Its_Minute_Is_Up_And_Reduced_Motion_Makes_The_Walk_Instant()
        {
            Motion.SetReduced(true);
            FrogGarden();
            GameState s = runner.State;
            screen.ClockHours = ShowerHour;
            screen.Advance(0.1f);
            screen.Advance(0.1f);
            Assert.IsTrue(screen.FrogWaiting, "under reduced motion it is simply there");
            for (int k = 0; k < 62; k++) screen.Advance(1f);
            Assert.IsFalse(FrogRabbit.Here(s));
            Assert.IsFalse(screen.FrogShown, "a minute, then home");
            Assert.AreEqual(0, s.FrogsMet);
        }

        [Test]
        public void A_New_Garden_Meets_No_Frog_And_The_Achievement_Is_In_The_List()
        {
            screen.Shell.SkipTitle();
            runner.State.TutorialStep = Tutorial.Steps;
            screen.ClockHours = ShowerHour;
            Step(1f);
            Assert.IsFalse(screen.FrogShown, "not before garden level " + FrogRabbit.FromLevel);

            QuestDef quest = null;
            foreach (QuestDef q in runner.QuestDefs) if (q.Id == "ech-5") quest = q;
            Assert.IsNotNull(quest, "the achievement is seeded");
            Assert.AreEqual(QuestMetric.FrogsMet, quest.Metric);
            Assert.AreEqual(5d, quest.Target);
            Assert.AreEqual(2, quest.RewardClovers);
            Assert.IsFalse(quest.Daily);
            Assert.AreEqual("Gặp Thỏ Ếch 5 lần", L10n.T("quest.ech-5"));
        }
    }
}
