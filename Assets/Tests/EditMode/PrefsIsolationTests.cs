using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using VuonTra.Game;
using VuonTra.Game.UI;
using Motion = VuonTra.Game.UI.Motion;
using Object = UnityEngine.Object;

namespace VuonTra.Core.Tests
{
    public class PrefsIsolationTests
    {
        sealed class ForeignCheckout : IDisposable
        {
            readonly List<Action> _undo = new List<Action>();

            public ForeignCheckout Int(string key, int value)
            {
                bool had = PlayerPrefs.HasKey(key);
                int before = PlayerPrefs.GetInt(key, 0);
                _undo.Add(() => { if (had) PlayerPrefs.SetInt(key, before); else PlayerPrefs.DeleteKey(key); });
                PlayerPrefs.SetInt(key, value);
                return this;
            }

            public ForeignCheckout Float(string key, float value)
            {
                bool had = PlayerPrefs.HasKey(key);
                float before = PlayerPrefs.GetFloat(key, 0f);
                _undo.Add(() => { if (had) PlayerPrefs.SetFloat(key, before); else PlayerPrefs.DeleteKey(key); });
                PlayerPrefs.SetFloat(key, value);
                return this;
            }

            public void Dispose()
            {
                for (int i = _undo.Count - 1; i >= 0; i--) _undo[i]();
            }
        }

        GameObject host;

        [TearDown]
        public void TearDown()
        {
            if (host != null) Object.DestroyImmediate(host);
            Prefs.Store = TestPrefs.Store;
            Motion.SetReduced(false);
            UiKit.SetLargeText(false);
            Haptics.SetOn(true);
            Sfx.SetMusic(Sfx.DefaultMusicGain);
            Sfx.SetSfx(Sfx.DefaultSfxGain);
            Sfx.SetStation(Sfx.DefaultStationGain);
            Sfx.SetTune(Sfx.Tune.Auto);
        }

        [Test]
        public void Tests_Run_On_A_Store_Of_Their_Own_That_Starts_Empty()
        {
            Assert.AreSame(TestPrefs.Store, Prefs.Store);
            Assert.AreNotSame(Prefs.Device, Prefs.Store);
            foreach (string key in new[] { "motion.reduced", "text.large", "vol.station", "vol.music", "vol.sfx", "music.tune", "haptics.on" })
                Assert.IsFalse(Prefs.HasKey(key), key + " starts unset in every test");
        }

        [Test]
        public void A_Foreign_Reduced_Motion_Is_Not_Read()
        {
            Motion.SetReduced(false);
            using (new ForeignCheckout().Int("motion.reduced", 1)) Motion.Load();
            Assert.IsFalse(Motion.Reduced);
        }

        [Test]
        public void A_Foreign_Large_Text_Is_Not_Read()
        {
            using (new ForeignCheckout().Int("text.large", 1)) UiKit.LoadTextSize();
            Assert.IsFalse(UiKit.LargeText);
        }

        [Test]
        public void A_Foreign_Haptics_Switch_Is_Not_Read()
        {
            using (new ForeignCheckout().Int("haptics.on", 0)) Haptics.Load();
            Assert.IsTrue(Haptics.On);
        }

        [Test]
        public void A_Foreign_Station_Volume_Is_Not_Read()
        {
            Sfx.SetStation(0.7f);
            host = new GameObject("Host");
            using (new ForeignCheckout().Float("vol.station", 0.5f)) Sfx.Attach(host);
            Assert.AreEqual(0.7f, Sfx.StationGain, 1e-4f);
        }

        [Test]
        public void A_Foreign_Music_Volume_Is_Not_Read()
        {
            Sfx.SetMusic(0.6f);
            host = new GameObject("Host");
            using (new ForeignCheckout().Float("vol.music", 0.1f)) Sfx.Attach(host);
            Assert.AreEqual(0.6f, Sfx.MusicGain, 1e-4f);
        }

        [Test]
        public void A_Foreign_Sound_Volume_Is_Not_Read()
        {
            Sfx.SetSfx(0.6f);
            host = new GameObject("Host");
            using (new ForeignCheckout().Float("vol.sfx", 0.1f)) Sfx.Attach(host);
            Assert.AreEqual(0.6f, Sfx.SfxGain, 1e-4f);
        }

        [Test]
        public void A_Foreign_Tune_Is_Not_Read()
        {
            Sfx.SetTune(Sfx.Tune.Moon);
            host = new GameObject("Host");
            using (new ForeignCheckout().Int("music.tune", (int)Sfx.Tune.Rain)) Sfx.Attach(host);
            Assert.AreEqual(Sfx.Tune.Moon, Sfx.CurrentTune);
        }

        [Test]
        public void The_Garden_Builds_From_The_Test_Store_Whatever_Another_Checkout_Wrote()
        {
            Motion.SetReduced(false);
            UiKit.SetLargeText(false);
            Sfx.SetStation(0.7f);
            Sfx.SetMusic(0.8f);
            Sfx.SetSfx(0.9f);
            Sfx.SetTune(Sfx.Tune.Dawn);
            var catalog = AssetDatabase.LoadAssetAtPath<GameCatalog>("Assets/VuonTra/Content/GameCatalog.asset");
            host = new GameObject("Game");
            var cam = new GameObject("Cam", typeof(Camera));
            cam.transform.SetParent(host.transform);
            var runner = host.AddComponent<GameRunner>();
            runner.Catalog = catalog;
            runner.Init(GameState.NewGame(catalog.BuildDefs()));
            var screen = host.AddComponent<GardenScreen>();
            screen.Runner = runner;
            screen.Cam = cam.GetComponent<Camera>();
            screen.Raining = () => false;
            using (new ForeignCheckout()
                .Int("motion.reduced", 1).Int("text.large", 1).Int("haptics.on", 0).Int("music.tune", (int)Sfx.Tune.Rain)
                .Float("vol.station", 0.5f).Float("vol.music", 0.1f).Float("vol.sfx", 0.2f))
                screen.Build();
            Assert.IsFalse(Motion.Reduced, "motion.reduced");
            Assert.IsFalse(UiKit.LargeText, "text.large");
            Assert.IsTrue(Haptics.On, "haptics.on");
            Assert.AreEqual(0.7f, Sfx.StationGain, 1e-4f, "vol.station");
            Assert.AreEqual(0.8f, Sfx.MusicGain, 1e-4f, "vol.music");
            Assert.AreEqual(0.9f, Sfx.SfxGain, 1e-4f, "vol.sfx");
            Assert.AreEqual(Sfx.Tune.Dawn, Sfx.CurrentTune, "music.tune");
        }

        [Test]
        public void What_A_Test_Saves_Never_Reaches_The_Players_Store()
        {
            float station = PlayerPrefs.GetFloat("vol.station", -1f);
            int reduced = PlayerPrefs.GetInt("motion.reduced", -1);
            int large = PlayerPrefs.GetInt("text.large", -1);
            Sfx.SetStation(0.123f);
            Motion.SetReduced(reduced != 1);
            UiKit.SetLargeText(large != 1);
            Assert.AreEqual(0.123f, TestPrefs.Store.GetFloat("vol.station", -1f), 1e-6f);
            Assert.AreEqual(station, PlayerPrefs.GetFloat("vol.station", -1f));
            Assert.AreEqual(reduced, PlayerPrefs.GetInt("motion.reduced", -1));
            Assert.AreEqual(large, PlayerPrefs.GetInt("text.large", -1));
        }

        [Test]
        public void On_The_Device_Store_A_Setting_Round_Trips_Through_PlayerPrefs_As_Before()
        {
            Prefs.Store = Prefs.Device;
            using (new ForeignCheckout().Int("motion.reduced", 0).Float("vol.station", 0f))
            {
                Motion.SetReduced(true);
                Assert.AreEqual(1, PlayerPrefs.GetInt("motion.reduced", -1), "written where the player's settings live");
                Motion.Reduced = false;
                Motion.Load();
                Assert.IsTrue(Motion.Reduced, "and read back from there on the next launch");

                Sfx.SetStation(0.3f);
                Assert.AreEqual(0.3f, PlayerPrefs.GetFloat("vol.station", -1f), 1e-6f);
                host = new GameObject("Host");
                Sfx.Attach(host);
                Assert.AreEqual(0.3f, Sfx.StationGain, 1e-6f);
            }
            Prefs.Store = TestPrefs.Store;
        }

        [Test]
        public void Setting_The_Store_To_Nothing_Falls_Back_To_The_Device()
        {
            Prefs.Store = null;
            Assert.AreSame(Prefs.Device, Prefs.Store);
        }
    }
}
