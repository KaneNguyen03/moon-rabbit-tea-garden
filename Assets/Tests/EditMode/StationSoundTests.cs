using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using VuonTra.Game;
using VuonTra.Game.Platform;
using VuonTra.Game.UI;
using L10n = VuonTra.Game.L10n;
using Motion = VuonTra.Game.UI.Motion;

namespace VuonTra.Core.Tests
{
    public class StationSoundTests
    {
        const float Frame = 1f / 30f;
        GameObject root;
        GameRunner runner;
        GardenScreen screen;
        float stationBefore;
        Sfx.Tune tuneBefore;
        System.Func<double> clockBefore;

        [SetUp]
        public void SetUp()
        {
            stationBefore = PlayerPrefs.GetFloat("vol.station", Sfx.DefaultStationGain);
            tuneBefore = (Sfx.Tune)PlayerPrefs.GetInt("music.tune", 0);
            L10n.SetLocale("vi");
            var catalog = AssetDatabase.LoadAssetAtPath<GameCatalog>("Assets/VuonTra/Content/GameCatalog.asset");
            root = new GameObject("Game");
            var cam = new GameObject("Cam", typeof(Camera));
            cam.transform.SetParent(root.transform);
            runner = root.AddComponent<GameRunner>();
            runner.Catalog = catalog;
            runner.DayClock = () => GiftCodes.Day(2027, 10, 2);
            runner.Init(GameState.NewGame(catalog.BuildDefs()));
            screen = root.AddComponent<GardenScreen>();
            screen.Runner = runner;
            screen.Cam = cam.GetComponent<Camera>();
            screen.Raining = () => false;
            clockBefore = screen.ClockHours;
            screen.Build();
            Sfx.SetStation(0.7f);
            screen.Refresh();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(root);
            Sfx.SetStation(stationBefore);
            Sfx.SetTune(tuneBefore);
            Sfx.SupplyStationClip("am-tra", null);
            Motion.SetReduced(false);
            L10n.SetLocale("vi");
        }

        void OpenSix()
        {
            screen.Shell.SkipTitle();
            GameState s = runner.State;
            s.Plots = 6;
            for (int i = 0; i < 6; i++) s.Levels[i] = Mathf.Max(1, s.Levels[i]);
            screen.Refresh();
        }

        void Step(float seconds)
        {
            for (float t = 0f; t < seconds - 1e-4f; t += Frame) screen.Advance(Frame);
        }

        int NearestOpen()
        {
            var world = (RectTransform)root.transform.Find("Canvas/Garden/Content");
            var centre = new Vector2(1080f / 2f, 1920f * 0.6f);
            int best = -1;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < 6; i++)
            {
                Vector2 at = screen.StationSpot(i) * world.localScale.x + world.anchoredPosition;
                float d = Vector2.Distance(at, centre);
                if (d < bestDistance) { bestDistance = d; best = i; }
            }
            return best;
        }

        string Id(int i) => runner.Defs[i].Id;

        [Test]
        public void Every_Station_Has_A_Loop_Of_Four_To_Six_Seconds_Made_Only_Once()
        {
            var distinct = new HashSet<AudioClip>();
            for (int i = 0; i < runner.Defs.Count; i++)
            {
                AudioClip clip = Sfx.StationClip(Id(i));
                Assert.IsNotNull(clip, Id(i));
                Assert.That(clip.length, Is.InRange(4f, 6f), Id(i) + " loops every 4–6 s");
                Assert.AreSame(clip, Sfx.StationClip(Id(i)), "made once, then reused");
                Assert.IsNotNull(Sfx.StationLoopOf(Id(i)), Id(i) + " is mapped");
                distinct.Add(clip);
            }
            Assert.GreaterOrEqual(distinct.Count, 8, "the eight sounds the issue names, at least");
            Assert.AreSame(Sfx.StationClip("lo-banh"), Sfx.StationClip("tiem-banh"), "two ovens, one crackle");
            Assert.IsNull(Sfx.StationClip("no-such-station"));
        }

        [Test]
        public void A_Recorded_Clip_Can_Stand_In_For_A_Made_One()
        {
            AudioClip made = Sfx.StationClip("am-tra");
            AudioClip recorded = AudioClip.Create("sfx_am-tra", 44100 * 5, 1, 44100, false);
            Sfx.SupplyStationClip("am-tra", recorded);
            Assert.AreSame(recorded, Sfx.StationClip("am-tra"));
            Sfx.SupplyStationClip("am-tra", null);
            Assert.AreSame(made, Sfx.StationClip("am-tra"));
            Object.DestroyImmediate(recorded);
        }

        [Test]
        public void Up_Close_The_Station_In_The_Middle_Plays_Its_Loop_And_The_Usual_View_Stops_It()
        {
            OpenSix();
            Step(0.2f);
            Assert.IsFalse(screen.StationSound.Playing, "the usual view is quiet");

            screen.SetZoom(GardenScreen.ZoomLevel.Near, false);
            screen.LookAt(screen.StationSpot(2));
            int nearest = NearestOpen();
            Assert.AreEqual(2, nearest, "looking at the jam pot puts it in the middle");
            Step(Frame);
            Assert.AreEqual(Id(2), screen.StationSound.StationId);
            Assert.AreSame(Sfx.StationClip(Id(2)), screen.StationSound.Clip, "the jam pot's own loop");
            Assert.IsTrue(screen.StationSound.Playing);
            Assert.Less(screen.StationSound.Volume, 0.7f * 0.2f, "it rises, it does not start loud");
            Step(0.25f);
            Assert.That(screen.StationSound.Volume, Is.InRange(0.7f * 0.35f, 0.7f * 0.7f), "halfway up after a quarter second");
            Step(0.3f);
            Assert.AreEqual(0.7f, screen.StationSound.Volume, 1e-4f, "up to the slider's level after half a second");
            Assert.AreEqual(1f, screen.StationSound.Pitch, 1e-4f);

            screen.SetZoom(GardenScreen.ZoomLevel.Default, false);
            Step(0.6f);
            Assert.IsFalse(screen.StationSound.Playing, "back to the usual view: stopped");
            Assert.IsNull(screen.StationSound.StationId);
            Assert.AreEqual(0f, screen.StationSound.Volume, 1e-4f);

            screen.SetZoom(GardenScreen.ZoomLevel.Far, false);
            Step(0.2f);
            Assert.IsFalse(screen.StationSound.Playing, "from far away too");
        }

        [Test]
        public void The_Station_Just_Tapped_Is_The_One_Heard()
        {
            OpenSix();
            screen.SetZoom(GardenScreen.ZoomLevel.Near, false);
            screen.LookAt(screen.StationSpot(0));
            Step(0.6f);
            Assert.AreEqual(Id(0), screen.StationSound.StationId);
            screen.SelectStation(5);
            Step(Frame);
            Assert.AreEqual(Id(5), screen.StationSound.StationId, "the card that is open wins over the middle of the screen");
            Assert.AreSame(Sfx.StationClip(Id(5)), screen.StationSound.Clip);
        }

        [Test]
        public void Changing_Station_Crossfades_In_Half_A_Second_And_Two_Loops_Never_Overlap_Longer()
        {
            OpenSix();
            screen.SetZoom(GardenScreen.ZoomLevel.Near, false);
            screen.LookAt(screen.StationSpot(2));
            Step(0.6f);
            screen.LookAt(screen.StationSpot(4));
            Assert.AreEqual(4, NearestOpen());
            Step(Frame);
            Assert.AreEqual(Id(4), screen.StationSound.StationId);
            Assert.AreEqual(Id(2), screen.StationSound.OutgoingId, "the jam pot fades out under the oven");
            Assert.AreEqual(2, screen.StationSound.Audible);
            Step(0.5f);
            Assert.IsNull(screen.StationSound.OutgoingId, "gone within half a second");
            Assert.AreEqual(1, screen.StationSound.Audible);
            Assert.AreEqual(0.7f, screen.StationSound.Volume, 1e-4f);

            int[] path = { 0, 2, 5, 4, 1, 2, 0 };
            int most = 0;
            foreach (int i in path)
            {
                screen.LookAt(screen.StationSpot(i));
                for (int f = 0; f < 5; f++)
                {
                    screen.Advance(Frame);
                    most = Mathf.Max(most, screen.StationSound.Audible);
                }
            }
            Step(1f);
            Assert.LessOrEqual(most, 2);
            Assert.LessOrEqual(screen.StationSound.LongestOverlap, 0.5f + 1e-4f);
            Assert.Greater(screen.StationSound.LongestOverlap, 0.3f, "but they do cross, not cut");
        }

        [Test]
        public void A_Boosted_Station_Plays_A_Tenth_Faster()
        {
            OpenSix();
            screen.SetZoom(GardenScreen.ZoomLevel.Near, false);
            screen.LookAt(screen.StationSpot(0));
            Step(0.2f);
            Assert.AreEqual(1f, screen.StationSound.Pitch, 1e-4f);
            runner.State.BoostSecondsLeft = 30d;
            Step(Frame);
            Assert.AreEqual(1.1f, screen.StationSound.Pitch, 1e-4f);
            runner.State.BoostSecondsLeft = 0d;
            Step(Frame);
            Assert.AreEqual(1f, screen.StationSound.Pitch, 1e-4f);
        }

        [Test]
        public void The_Station_Slider_At_Zero_Is_Silent_And_Is_Its_Own_Setting()
        {
            OpenSix();
            screen.SetZoom(GardenScreen.ZoomLevel.Near, false);
            screen.LookAt(screen.StationSpot(0));
            Step(0.6f);
            float effects = Sfx.SfxGain;

            screen.Shell.OpenPause();
            Slider slider = root.transform.Find("Canvas/Pause/Card/Station").GetComponent<Slider>();
            Assert.AreEqual(0.7f, slider.value, 1e-4f);
            Assert.AreEqual("Tiếng trạm", root.transform.Find("Canvas/Pause/Card/Station Label").GetComponent<Text>().text);
            slider.value = 0f;
            Step(Frame);
            Assert.AreEqual(0f, Sfx.StationGain);
            Assert.AreEqual(0f, screen.StationSound.Volume, "silent");
            Assert.AreEqual(effects, Sfx.SfxGain, "the Sound slider is not touched");
            Assert.AreEqual(0f, PlayerPrefs.GetFloat("vol.station", -1f), "kept");

            slider.value = 0.4f;
            Step(Frame);
            Assert.AreEqual(0.4f, screen.StationSound.Volume, 1e-4f, "still up close behind the card, so the slider can be heard");
        }

        [Test]
        public void The_Station_Volume_Starts_At_One_Half_And_A_Saved_Level_Is_Kept()
        {
            PlayerPrefs.DeleteKey("vol.station");
            var host = new GameObject("Host");
            Sfx.Attach(host);
            Assert.AreEqual(0.5f, Sfx.DefaultStationGain);
            Assert.AreEqual(0.5f, Sfx.StationGain, 1e-4f, "a new player starts at the default");
            Object.DestroyImmediate(host);

            PlayerPrefs.SetFloat("vol.station", 0.7f);
            host = new GameObject("Host");
            Sfx.Attach(host);
            Assert.AreEqual(0.7f, Sfx.StationGain, 1e-4f, "a saved level is the player's own, not the default");
            Object.DestroyImmediate(host);
        }

        [TestCase(Sfx.Tune.Auto, 0.0, Sfx.Tune.Moon)]
        [TestCase(Sfx.Tune.Auto, 4.99, Sfx.Tune.Moon)]
        [TestCase(Sfx.Tune.Auto, 5.0, Sfx.Tune.Dawn)]
        [TestCase(Sfx.Tune.Auto, 12.0, Sfx.Tune.Dawn)]
        [TestCase(Sfx.Tune.Auto, 16.99, Sfx.Tune.Dawn)]
        [TestCase(Sfx.Tune.Auto, 17.0, Sfx.Tune.Moon)]
        [TestCase(Sfx.Tune.Auto, 22.0, Sfx.Tune.Moon)]
        [TestCase(Sfx.Tune.Moon, 12.0, Sfx.Tune.Moon)]
        [TestCase(Sfx.Tune.Dawn, 23.0, Sfx.Tune.Dawn)]
        [TestCase(Sfx.Tune.Rain, 9.0, Sfx.Tune.Rain)]
        public void Auto_Plays_Daybreak_From_Five_To_Five_And_The_Moon_The_Rest(Sfx.Tune choice, double hour, Sfx.Tune heard)
        {
            Assert.AreEqual(heard, Sfx.TuneAt(choice, hour));
        }

        [Test]
        public void The_Three_Tunes_Are_Different_Loops_And_The_Clock_Picks_In_Auto()
        {
            Sfx.SetTune(Sfx.Tune.Auto);
            screen.ClockHours = () => 10d;
            screen.Advance(Frame);
            Assert.AreEqual(Sfx.Tune.Dawn, Sfx.PlayingTune);
            screen.Advance(Sfx.TuneFadeSeconds);
            string day = Sfx.MusicClip.name;
            screen.ClockHours = () => 22d;
            screen.Advance(Frame);
            Assert.AreEqual(Sfx.Tune.Moon, Sfx.PlayingTune);
            screen.Advance(Sfx.TuneFadeSeconds);
            string night = Sfx.MusicClip.name;
            Sfx.SetTune(Sfx.Tune.Rain);
            Assert.AreEqual(Sfx.Tune.Rain, Sfx.PlayingTune, "a choice is taken at once");
            screen.Advance(Sfx.TuneFadeSeconds);
            Assert.AreEqual(Sfx.Tune.Rain, Sfx.PlayingTune, "and heard after the fade");
            string rain = Sfx.MusicClip.name;
            CollectionAssert.AllItemsAreUnique(new[] { day, night, rain });
            Assert.Greater(Sfx.MusicClip.length, 10f, "a tune, not a jingle");
            screen.ClockHours = clockBefore;
        }

        [Test]
        public void The_Tune_Pill_Cycles_Through_The_Four_Choices_And_Is_Kept()
        {
            Sfx.SetTune(Sfx.Tune.Auto);
            screen.Shell.SkipTitle();
            screen.Shell.OpenPause();
            Transform pill = root.transform.Find("Canvas/Pause/Card/Tune");
            Assert.IsNotNull(pill);
            Text label = pill.Find("Text").GetComponent<Text>();
            Assert.AreEqual("Tự động · Theo mùa", label.text);
            Assert.AreEqual("Nhạc", root.transform.Find("Canvas/Pause/Card/Tune Label").GetComponent<Text>().text);
            string[] next = { "Đêm trăng", "Rạng sáng", "Mưa", "Tự động · Theo mùa" };
            for (int i = 0; i < next.Length; i++)
            {
                pill.GetComponent<Button>().onClick.Invoke();
                Assert.AreEqual(next[i], label.text);
                Assert.AreEqual((i + 1) % 4, PlayerPrefs.GetInt("music.tune", -1), "kept for the next launch");
            }
            Assert.AreEqual(Sfx.Tune.Auto, Sfx.CurrentTune);
        }

        [Test]
        public void The_Card_Grows_So_The_New_Rows_Sit_Above_The_Buttons_And_On_The_Screen()
        {
            screen.Shell.SkipTitle();
            screen.Shell.OpenPause();
            screen.Advance(0.5f);
            var card = (RectTransform)root.transform.Find("Canvas/Pause/Card");
            var tune = (RectTransform)card.Find("Tune");
            var newMoon = (RectTransform)card.Find("NewMoonLocked");
            var remind = (RectTransform)card.Find("Remind");
            var station = (RectTransform)card.Find("Station");
            float Bottom(RectTransform rt) => card.InverseTransformPoint(rt.TransformPoint(rt.rect.min)).y;
            float Top(RectTransform rt) => card.InverseTransformPoint(rt.TransformPoint(rt.rect.max)).y;
            Assert.Less(Top(station), Bottom(remind), "the station slider under the last row");
            Assert.Less(Top(tune), Bottom(station));
            Assert.Greater(Bottom(tune), Top(newMoon) + 4f, "and clear of the new-moon line");
            Assert.AreEqual(0.5f, card.anchorMin.y, 1e-4f);
            Assert.AreEqual(0f, card.anchoredPosition.y, 0.5f);
            Assert.LessOrEqual(card.rect.height, 1920f - 2f * 80f, "the card stays on the screen");
        }
    }
}
