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
    /// <summary>
    /// The pond's own sounds (#58 follow-up): the cast, the float going under, the strike, the reel to the basket, the
    /// lost bait and the float popping back up, each at its moment of a cast, short, not silent, under the Sound slider.
    /// </summary>
    public class FishingSoundTests
    {
        static readonly string[] Sounds = { "cast", "sink", "strike", "reel", "bait-lost", "float-up" };

        GameObject root;
        GameRunner runner;
        GardenScreen screen;
        long now = 1_800_000_000L;
        float musicBefore, sfxBefore, stationBefore;

        [SetUp]
        public void SetUp()
        {
            musicBefore = Sfx.MusicGain;
            sfxBefore = Sfx.SfxGain;
            stationBefore = Sfx.StationGain;
            Sfx.SetSfx(0.8f);
            Build();
        }

        void Build()
        {
            L10n.SetLocale("vi");
            UiKit.SetLargeText(false);
            var catalog = AssetDatabase.LoadAssetAtPath<GameCatalog>("Assets/VuonTra/Content/GameCatalog.asset");
            Assert.IsNotNull(catalog, "Run VuonTra > Seed Content first.");
            root = new GameObject("Game");
            var cam = new GameObject("Cam", typeof(Camera));
            cam.transform.SetParent(root.transform);
            runner = root.AddComponent<GameRunner>();
            runner.Catalog = catalog;
            GameState s = GameState.NewGame(catalog.BuildDefs());
            s.Levels[1] = 12; s.Levels[2] = 9; s.Levels[3] = 4;
            s.GardenXp = Garden.XpForLevel(5);
            s.HintSeen = true;
            s.TutorialStep = Tutorial.Steps;
            runner.Init(s);
            runner.FishingClock = () => now;
            screen = root.AddComponent<GardenScreen>();
            screen.Runner = runner;
            screen.Cam = cam.GetComponent<Camera>();
            screen.Raining = () => false;
            screen.Build();
            Motion.SetReduced(true);
            screen.Shell.StartGame();
            screen.Refresh();
            Step(1.2f);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(root);
            Motion.SetReduced(false);
            Sfx.SetMusic(musicBefore);
            Sfx.SetSfx(sfxBefore);
            Sfx.SetStation(stationBefore);
            L10n.SetLocale("vi");
        }

        void Step(float seconds)
        {
            for (float t = 0f; t < seconds; t += 1f / 30f) screen.Advance(1f / 30f);
        }

        const string Card = "Canvas/Pond/Card";

        Button Find(string path)
        {
            Transform t = root.transform.Find(path);
            Assert.IsNotNull(t, "Missing UI object: " + path);
            return t.GetComponent<Button>();
        }

        void OpenPond()
        {
            Find("Canvas/Garden/Content/PondTap").onClick.Invoke();
            screen.Refresh();
            Step(0.3f);
            Assert.IsTrue(screen.FishingCard.IsOpen);
        }

        void StepUntilSinking()
        {
            for (int k = 0; k < 400 && runner.Cast != null && runner.Cast.Phase != CastPhase.Sinking; k++)
            {
                screen.Advance(1f / 30f);
                if (runner.Cast != null && runner.Cast.Phase != CastPhase.Sinking)
                    CollectionAssert.DoesNotContain(Sfx.Recent, "sink", "no bloop while the float still bobs");
            }
            Assert.IsNotNull(runner.Cast);
            Assert.AreEqual(CastPhase.Sinking, runner.Cast.Phase);
        }

        /// <summary>One cast that is caught, one let go, one tapped too early: what plays when.</summary>
        void PlayACast(bool checkTiming)
        {
            OpenPond();
            Sfx.ClearRecent();
            Find(Card + "/Cast").onClick.Invoke();
            Assert.AreEqual("cast", Sfx.Last, "the line swishes out and the float plops in");

            StepUntilSinking();
            Assert.AreEqual("sink", Sfx.Last, "the bloop the moment the float goes under");

            Sfx.ClearRecent();
            Find(Card + "/Water").onClick.Invoke();
            Assert.AreEqual("strike", Sfx.Last, "the splash of a strike in the window");
            CollectionAssert.DoesNotContain(Sfx.Recent, "reel", "the fish holds over the water first");
            if (checkTiming)
            {
                Step(FishingView.ShowSeconds - 0.1f);
                CollectionAssert.DoesNotContain(Sfx.Recent, "reel", "not while it shows over the water");
            }
            Step(FishingView.ShowSeconds + FishingView.FlySeconds + 0.3f);
            Assert.AreEqual(1, Count("reel"), "once, as it flies to the basket");
            Assert.AreEqual(0, screen.FishingCard.FishFlying);

            Find(Card + "/Cast").onClick.Invoke();
            Step(0.4f);
            Sfx.ClearRecent();
            Find(Card + "/Water").onClick.Invoke();
            Assert.AreEqual(CastPhase.Bobbing, runner.Cast.Phase);
            Assert.AreEqual("float-up", Sfx.Last, "too early: the float bobs back up");

            Sfx.ClearRecent();
            Step((float)(Fishing.BobMax + Fishing.SinkSeconds) + 0.5f);
            Assert.IsNull(runner.Cast, "left to sink: the bait is gone");
            Assert.AreEqual(1, Count("bait-lost"));
            CollectionAssert.DoesNotContain(Sfx.Recent, "deny", "a gentle aw, not the shop's no");
        }

        static int Count(string name)
        {
            int n = 0;
            foreach (string s in Sfx.Recent) if (s == name) n++;
            return n;
        }

        [Test]
        public void Each_Fishing_Sound_Plays_At_Its_Moment_Of_A_Cast()
        {
            PlayACast(false);
        }

        [Test]
        public void The_Reel_Waits_For_The_Fish_To_Leave_The_Water()
        {
            Motion.SetReduced(false);
            PlayACast(true);
        }

        [Test]
        public void Each_Fishing_Sound_Is_Made_Once_Short_And_Not_Silent()
        {
            foreach (string name in Sounds)
            {
                AudioClip clip = Sfx.FishingClip(name);
                Assert.IsNotNull(clip, name);
                Assert.AreSame(clip, Sfx.FishingClip(name), name + " is made once, then reused");
                float most = name == "reel" ? 2f : 1.5f;
                Assert.That(clip.length, Is.GreaterThan(0.05f).And.LessThanOrEqualTo(most), name + " is short");
                var data = new float[clip.samples * clip.channels];
                Assert.IsTrue(clip.GetData(data, 0), name);
                float peak = 0f;
                double sum = 0d;
                foreach (float v in data) { peak = Mathf.Max(peak, Mathf.Abs(v)); sum += v * v; }
                Assert.Greater(peak, 0.1f, name + " can be heard");
                Assert.LessOrEqual(peak, 0.95f, name + " never clips");
                Assert.Greater(System.Math.Sqrt(sum / data.Length), 0.005, name + " is more than a click");
                Assert.Less(Mathf.Abs(data[0]), 0.01f, name + " starts softly");
                Assert.Less(Mathf.Abs(data[data.Length - 1]), 0.01f, name + " ends softly");
            }
            Assert.IsNull(Sfx.FishingClip("no-such-sound"));
        }

        [Test]
        public void The_Fishing_Sounds_Follow_The_Sound_Slider_Alone()
        {
            Sfx.SetSfx(0.8f);
            PlayACast(false);
            foreach (string name in Sounds)
            {
                Assert.Greater(Sfx.LastVolumeOf(name), 0f, name + " is heard");
                Assert.LessOrEqual(Sfx.LastVolumeOf(name), 0.5f * 0.8f, name + " sits low, like the other small sounds");
            }

            // The Sound slider at nought: silent, whatever Music and the stations are set to.
            Object.DestroyImmediate(root);
            Build();
            Sfx.SetSfx(0f);
            Sfx.SetMusic(1f);
            Sfx.SetStation(1f);
            PlayACast(false);
            foreach (string name in Sounds) Assert.AreEqual(0f, Sfx.LastVolumeOf(name), name + " is silent");

            // And Music or the stations at nought do not quiet them.
            Sfx.SetSfx(1f);
            Sfx.SetMusic(0f);
            Sfx.SetStation(0f);
            Sfx.Cast(); Sfx.Sink(); Sfx.Strike(); Sfx.Reel(); Sfx.BaitLost(); Sfx.FloatUp();
            foreach (string name in Sounds) Assert.Greater(Sfx.LastVolumeOf(name), 0f, name + " follows Sound, not Music or Tiếng trạm");
        }
    }
}
