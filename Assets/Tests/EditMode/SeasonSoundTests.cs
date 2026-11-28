using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using VuonTra.EditorTools;
using VuonTra.Game;
using VuonTra.Game.UI;
using L10n = VuonTra.Game.L10n;
using Motion = VuonTra.Game.UI.Motion;

namespace VuonTra.Core.Tests
{
    public class SeasonSoundTests
    {
        const float Frame = 1f / 30f;
        GameObject root;
        GameRunner runner;
        GardenScreen screen;
        long today;
        Sfx.Tune tuneBefore;
        float sfxBefore, musicBefore;

        static long MidAutumnDay => GiftCodes.Day(2027, 9, 15);
        static long OutOfSeasonDay => GiftCodes.Day(2027, 10, 2);
        static long ChristmasDay => GiftCodes.Day(2027, 12, 20);
        static long TetDay => GiftCodes.Day(2028, 1, 25);
        static FairRun May => Fairs.RunOn(GiftCodes.Day(2027, 5, Fairs.StartDay)).Value;

        [SetUp]
        public void SetUp()
        {
            tuneBefore = (Sfx.Tune)Prefs.GetInt("music.tune", 0);
            sfxBefore = Sfx.SfxGain;
            musicBefore = Sfx.MusicGain;
            L10n.SetLocale("vi");
        }

        [TearDown]
        public void TearDown()
        {
            if (root != null) Object.DestroyImmediate(root);
            Sfx.SetTune(tuneBefore);
            Sfx.SetSfx(sfxBefore);
            Sfx.SetMusic(musicBefore);
            Sfx.UpdateTune(12d, Season.None, 10f);
            Sfx.SetFair(false);
            Motion.SetReduced(false);
            L10n.SetLocale("vi");
        }

        GameState Open(long day)
        {
            today = day;
            var catalog = AssetDatabase.LoadAssetAtPath<GameCatalog>("Assets/VuonTra/Content/GameCatalog.asset");
            Assert.IsNotNull(catalog, "Run VuonTra > Seed Content first.");
            root = new GameObject("Game");
            var cam = new GameObject("Cam", typeof(Camera));
            cam.transform.SetParent(root.transform);
            runner = root.AddComponent<GameRunner>();
            runner.Catalog = catalog;
            runner.Analytics = new MemoryAnalytics();
            runner.DayClock = () => today;
            GameState s = GameState.NewGame(catalog.BuildDefs());
            s.FirstDaySeen = today - 20;
            s.MaxDaySeen = today;
            runner.Init(s);
            screen = root.AddComponent<GardenScreen>();
            screen.Runner = runner;
            screen.Cam = cam.GetComponent<Camera>();
            screen.Raining = () => false;
            screen.ClockHours = () => 10d;
            screen.Build();
            return s;
        }

        void Settle()
        {
            for (int i = 0; i < 4; i++) screen.Advance(1f);
        }

        float Full => Sfx.MelodyLevel * Sfx.MusicGain;

        [TestCase(Season.MidAutumn, Sfx.Tune.MidAutumn)]
        [TestCase(Season.Tet, Sfx.Tune.Tet)]
        [TestCase(Season.Christmas, Sfx.Tune.Christmas)]
        public void In_A_Holiday_Season_Auto_Plays_The_Season_Tune_At_Any_Hour(Season season, Sfx.Tune heard)
        {
            foreach (double hour in new[] { 0d, 4.99d, 5d, 12d, 16.99d, 17d, 23.5d })
                Assert.AreEqual(heard, Sfx.TuneAt(Sfx.Tune.Auto, hour, season), "hour " + hour);
        }

        [TestCase(0d, Sfx.Tune.Moon)]
        [TestCase(5d, Sfx.Tune.Dawn)]
        [TestCase(16.99d, Sfx.Tune.Dawn)]
        [TestCase(17d, Sfx.Tune.Moon)]
        public void Out_Of_Season_Auto_Follows_The_Hour_As_Before(double hour, Sfx.Tune heard)
        {
            Assert.AreEqual(heard, Sfx.TuneAt(Sfx.Tune.Auto, hour, Season.None));
            Assert.AreEqual(heard, Sfx.TuneAt(Sfx.Tune.Auto, hour));
        }

        [Test]
        public void A_Fixed_Choice_Is_Kept_In_Every_Season([Values(Sfx.Tune.Moon, Sfx.Tune.Dawn, Sfx.Tune.Rain)] Sfx.Tune choice)
        {
            foreach (Season season in new[] { Season.None, Season.MidAutumn, Season.Tet, Season.Christmas })
                foreach (double hour in new[] { 2d, 10d, 20d })
                    Assert.AreEqual(choice, Sfx.TuneAt(choice, hour, season), season + " at " + hour);
        }

        [Test]
        public void The_Season_Tunes_Are_Their_Own_Loops_As_Loud_As_The_Hour_Tunes()
        {
            float quietest = float.MaxValue, loudest = float.MinValue;
            foreach (Sfx.Tune tune in new[] { Sfx.Tune.Moon, Sfx.Tune.Dawn, Sfx.Tune.Rain })
            {
                float db = SoundMeter.Of(Sfx.TuneClipOf(tune)).RmsDb;
                quietest = Mathf.Min(quietest, db);
                loudest = Mathf.Max(loudest, db);
            }
            var names = new System.Collections.Generic.HashSet<string>();
            foreach (Sfx.Tune tune in new[] { Sfx.Tune.MidAutumn, Sfx.Tune.Tet, Sfx.Tune.Christmas })
            {
                AudioClip clip = Sfx.TuneClipOf(tune);
                Assert.IsNotNull(clip, tune.ToString());
                Assert.IsTrue(names.Add(clip.name), tune + " has a clip of its own");
                Assert.Greater(clip.length, 15f, tune + ": a tune, not a jingle");
                SoundMeter m = SoundMeter.Of(clip);
                Assert.GreaterOrEqual(m.RmsDb, quietest - 3f, tune + ": " + m);
                Assert.LessOrEqual(m.RmsDb, loudest + 3f, tune + ": " + m);
                Assert.LessOrEqual(m.PeakDb, 0f, tune + ": " + m);
            }
            Assert.AreNotEqual(Sfx.TuneClipOf(Sfx.Tune.Moon).name, Sfx.TuneClipOf(Sfx.Tune.MidAutumn).name);
        }

        [Test]
        public void The_Garden_Plays_The_Season_Tune_In_Season_And_The_Hour_Tune_After()
        {
            Sfx.SetTune(Sfx.Tune.Auto);
            Open(MidAutumnDay);
            Assert.AreSame(Sfx.TuneClipOf(Sfx.Tune.MidAutumn), Sfx.MusicClip, "a garden opened in season starts on the season tune");
            Settle();
            Assert.AreEqual(Sfx.Tune.MidAutumn, Sfx.PlayingTune);
            Assert.AreSame(Sfx.TuneClipOf(Sfx.Tune.MidAutumn), Sfx.MusicClip);
            Assert.AreEqual(Full, Sfx.MusicVolume, 1e-4f);

            today = ChristmasDay;
            Settle();
            Assert.AreSame(Sfx.TuneClipOf(Sfx.Tune.Christmas), Sfx.MusicClip);

            today = TetDay;
            Settle();
            Assert.AreSame(Sfx.TuneClipOf(Sfx.Tune.Tet), Sfx.MusicClip);

            today = TetDay + 40;
            Settle();
            Assert.AreEqual(Sfx.Tune.Dawn, Sfx.PlayingTune, "back to the tune of the hour");
            Assert.AreSame(Sfx.TuneClipOf(Sfx.Tune.Dawn), Sfx.MusicClip);
        }

        [Test]
        public void A_Fixed_Choice_Is_Heard_Through_A_Season()
        {
            Sfx.SetTune(Sfx.Tune.Rain);
            Open(ChristmasDay);
            Settle();
            Assert.AreEqual(Sfx.Tune.Rain, Sfx.PlayingTune);
            Assert.AreSame(Sfx.TuneClipOf(Sfx.Tune.Rain), Sfx.MusicClip);
        }

        [Test]
        public void A_Tune_Change_Fades_Out_For_A_Second_And_In_For_A_Second_Even_With_Reduced_Motion([Values(false, true)] bool reduced)
        {
            Motion.SetReduced(reduced);
            Sfx.SetTune(Sfx.Tune.Auto);
            Open(OutOfSeasonDay);
            Settle();
            AudioClip hour = Sfx.MusicClip;
            Assert.AreSame(Sfx.TuneClipOf(Sfx.Tune.Dawn), hour);
            Assert.AreEqual(Full, Sfx.MusicVolume, 1e-4f);

            today = ChristmasDay;
            screen.Advance(0.5f);
            Assert.AreEqual(Sfx.Tune.Christmas, Sfx.PlayingTune, "the new tune is chosen at once");
            Assert.AreSame(hour, Sfx.MusicClip, "the old tune is still fading");
            Assert.AreEqual(Full * 0.5f, Sfx.MusicVolume, 0.02f);
            screen.Advance(0.5f);
            Assert.AreSame(Sfx.TuneClipOf(Sfx.Tune.Christmas), Sfx.MusicClip, "after a second the new tune takes over");
            Assert.AreEqual(0f, Sfx.MusicVolume, 0.02f);
            screen.Advance(0.5f);
            Assert.AreEqual(Full * 0.5f, Sfx.MusicVolume, 0.02f);
            screen.Advance(0.5f);
            Assert.AreEqual(Full, Sfx.MusicVolume, 1e-4f, "and is back at the Music slider a second later");
        }

        [Test]
        public void A_Running_Fair_Adds_The_Market_Layer_At_A_Quarter_Of_The_Effects_Slider_And_Its_End_Takes_It_Away()
        {
            Sfx.SetSfx(0.8f);
            Open(GiftCodes.Day(2027, 5, 1));
            Settle();
            Assert.IsFalse(Sfx.Market, "no fair, no market");
            Assert.AreEqual(0f, Sfx.MarketVolume);

            today = May.Start + 2;
            Settle();
            Assert.IsNotNull(runner.FairNow);
            Assert.IsTrue(Sfx.Market, "the fair is on: the market murmurs");
            Assert.AreSame(Sfx.MarketClip, Sfx.MarketSourceClip);
            Assert.AreEqual(Sfx.MarketLevel * 0.8f, Sfx.MarketVolume, 1e-4f);
            Assert.AreSame(Sfx.AmbientClip, Sfx.BedClip, "on top of the garden sounds, not in their place");
            Sfx.SetSfx(0.4f);
            Assert.AreEqual(Sfx.MarketLevel * 0.4f, Sfx.MarketVolume, 1e-4f, "it follows the Effects slider");

            today = May.End + 1;
            Settle();
            Assert.IsNull(runner.FairNow);
            Assert.IsFalse(Sfx.Market, "the fair is over: the market is quiet");
            Assert.AreEqual(0f, Sfx.MarketVolume);
        }

        [Test]
        public void The_Market_Layer_Is_A_Soft_Loop_Under_The_Music()
        {
            Assert.AreEqual(0.25f, Sfx.MarketLevel);
            AudioClip clip = Sfx.MarketClip;
            Assert.Greater(clip.length, 5f);
            float music = float.MaxValue;
            foreach (Sfx.Tune tune in new[] { Sfx.Tune.Moon, Sfx.Tune.Dawn, Sfx.Tune.Rain })
                music = Mathf.Min(music, SoundMeter.Of(Sfx.TuneClipOf(tune), Sfx.MelodyLevel * Sfx.DefaultMusicGain).RmsDb);
            SoundMeter m = SoundMeter.Of(clip, Sfx.MarketLevel * 0.8f);
            Assert.LessOrEqual(m.RmsDb, music - 6f, "market " + m + " against the quietest tune's " + music);
            Assert.GreaterOrEqual(m.RmsDb, music - 20f, "and still heard: " + m);
            Assert.Less(SoundMeter.Of(clip).CentroidHz, 4500f, m.ToString());
        }

        [Test]
        public void The_Tune_Pill_Says_Auto_Follows_The_Season()
        {
            Sfx.SetTune(Sfx.Tune.Auto);
            Open(OutOfSeasonDay);
            screen.Shell.SkipTitle();
            screen.Shell.OpenPause();
            Text label = root.transform.Find("Canvas/Pause/Card/Tune/Text").GetComponent<Text>();
            Assert.AreEqual("Tự động · Theo mùa", label.text);
            L10n.SetLocale("en");
            Assert.AreEqual("Auto · Seasonal", L10n.T("tune.auto"));
        }

        [Test]
        public void A_Recording_Is_A_Mono_Sixteen_Bit_Wave_Of_Twenty_Seconds()
        {
            AudioClip clip = Sfx.TuneClipOf(Sfx.Tune.Tet);
            byte[] wav = TuneRecorder.Wave(clip, TuneRecorder.Seconds);
            int samples = Mathf.RoundToInt(clip.frequency * TuneRecorder.Seconds);
            Assert.AreEqual(44 + samples * 2, wav.Length);
            Assert.AreEqual("RIFF", System.Text.Encoding.ASCII.GetString(wav, 0, 4));
            Assert.AreEqual("WAVE", System.Text.Encoding.ASCII.GetString(wav, 8, 4));
            Assert.AreEqual(1, System.BitConverter.ToInt16(wav, 22), "mono");
            Assert.AreEqual(clip.frequency, System.BitConverter.ToInt32(wav, 24));
            Assert.AreEqual(16, System.BitConverter.ToInt16(wav, 34));
            Assert.AreEqual(samples * 2, System.BitConverter.ToInt32(wav, 40));
            Assert.AreEqual(4, TuneRecorder.Files.Length, "three season tunes and the market");
            foreach (string file in TuneRecorder.Files) StringAssert.StartsWith(TuneRecorder.Folder, Path.GetDirectoryName(file).Replace('\\', '/'));
        }
    }
}
