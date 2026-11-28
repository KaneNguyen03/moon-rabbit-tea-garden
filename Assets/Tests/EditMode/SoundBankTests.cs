using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VuonTra.Game;
using Motion = VuonTra.Game.UI.Motion;
using Object = UnityEngine.Object;

namespace VuonTra.Core.Tests
{
    public class SoundBankTests
    {
        const float MaxPeakDb = -3f;
        const float CommonRmsDb = -20f, RmsToleranceDb = 3f;
        const float MaxOverMusicDb = 6f;
        const float MaxCentroidHz = 4500f;
        const int SummonTier = 2;

        static readonly Dictionary<SoundMoment, string> Synthesized = new Dictionary<SoundMoment, string>
        {
            { SoundMoment.Summon, "summon-" + SummonTier },
            { SoundMoment.GardenLevel, "sting" },
            { SoundMoment.Plot, "sting" },
            { SoundMoment.Gift, "reward" },
            { SoundMoment.Pet, "pet" },
            { SoundMoment.Tray, "reward" },
        };

        static readonly Dictionary<SoundMoment, string> Names = new Dictionary<SoundMoment, string>
        {
            { SoundMoment.Summon, "summon-" + SummonTier },
            { SoundMoment.GardenLevel, "sting" },
            { SoundMoment.Plot, "unlock" },
            { SoundMoment.Gift, "reward" },
            { SoundMoment.Pet, "pet" },
            { SoundMoment.Tray, "reward" },
        };

        static void Play(SoundMoment moment)
        {
            switch (moment)
            {
                case SoundMoment.Summon: Sfx.Summon(SummonTier); break;
                case SoundMoment.GardenLevel: Sfx.GardenLevel(); break;
                case SoundMoment.Plot: Sfx.Plot(); break;
                case SoundMoment.Gift: Sfx.Gift(); break;
                case SoundMoment.Pet: Sfx.Pet(); break;
                case SoundMoment.Tray: Sfx.Collect(); break;
                default: throw new ArgumentOutOfRangeException(nameof(moment));
            }
        }

        static IEnumerable<SoundMoment> Moments => SoundBank.Moments;

        float sfxBefore;
        bool motionBefore;
        SoundBank empty;

        [SetUp]
        public void SetUp()
        {
            sfxBefore = Sfx.SfxGain;
            motionBefore = Motion.Reduced;
            Sfx.SetSfx(Sfx.DefaultSfxGain);
            Sfx.ReloadBank();
        }

        [TearDown]
        public void TearDown()
        {
            if (empty != null) Object.DestroyImmediate(empty);
            Sfx.ReloadBank();
            Sfx.SetSfx(sfxBefore);
            Motion.SetReduced(motionBefore);
        }

        static SoundBank Shipped()
        {
            SoundBank bank = Resources.Load<SoundBank>(SoundBank.ResourcePath);
            Assert.IsNotNull(bank, "Resources/" + SoundBank.ResourcePath + " is the bank the game loads");
            return bank;
        }

        static AudioClip Loaded(SoundMoment moment)
        {
            AudioClip clip = Shipped().Clip(moment);
            Assert.IsNotNull(clip, moment + " has a recorded clip");
            clip.LoadAudioData();
            return clip;
        }

        [Test]
        public void The_Bank_Has_A_Clip_For_Each_Of_The_Six_Moments()
        {
            Assert.AreEqual(6, SoundBank.Moments.Length);
            SoundBank bank = Shipped();
            var files = new HashSet<string>();
            foreach (SoundMoment moment in SoundBank.Moments)
            {
                Assert.IsNotNull(bank.Clip(moment), moment.ToString());
                Assert.AreEqual(SoundBank.FileOf(moment), bank.Clip(moment).name, moment + " is the file the seeder names");
                Assert.IsTrue(files.Add(SoundBank.FileOf(moment)), moment + " has its own file");
            }
        }

        [Test]
        public void A_Recorded_Clip_Plays_For_Its_Moment([ValueSource(nameof(Moments))] SoundMoment moment)
        {
            Play(moment);
            Assert.AreSame(Shipped().Clip(moment), Sfx.LastClip, moment.ToString());
            Assert.AreEqual(moment, Sfx.LastMoment);
            Assert.AreEqual(Names[moment], Sfx.Last, "the name the screens listen for stays the same");
            Assert.AreEqual(SoundBank.Volume * Sfx.DefaultSfxGain, Sfx.LastVolumeOf(Names[moment]), 1e-5f);
        }

        [Test]
        public void A_Missing_Clip_Falls_Back_To_The_Synthesized_Sound([ValueSource(nameof(Moments))] SoundMoment moment)
        {
            empty = ScriptableObject.CreateInstance<SoundBank>();
            Sfx.UseBank(empty);
            Assert.DoesNotThrow(() => Play(moment));
            Assert.IsNotNull(Sfx.LastClip, moment + " still sounds");
            Assert.AreEqual(Synthesized[moment], Sfx.LastClip.name, moment + " plays the synthesized sound");
            Assert.AreEqual(Names[moment], Sfx.Last);
            Assert.Greater(Sfx.LastVolumeOf(Names[moment]), 0f);

            Sfx.UseBank(null);
            Assert.DoesNotThrow(() => Play(moment));
            Assert.AreEqual(Synthesized[moment], Sfx.LastClip.name, moment + " without any bank");
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void The_Chest_Shares_The_Gift_Clip_And_Keeps_Its_Own_Fallback()
        {
            Sfx.Chest();
            Assert.AreSame(Shipped().Clip(SoundMoment.Gift), Sfx.LastClip);
            Assert.AreEqual("unlock", Sfx.Last);
            empty = ScriptableObject.CreateInstance<SoundBank>();
            Sfx.UseBank(empty);
            Sfx.Chest();
            Assert.AreEqual("sting", Sfx.LastClip.name);
        }

        [Test]
        public void A_Higher_Tier_Is_Called_A_Little_Higher()
        {
            float before = 0f;
            for (int tier = 0; tier < 6; tier++)
            {
                Sfx.Summon(tier);
                Assert.AreSame(Shipped().Clip(SoundMoment.Summon), Sfx.LastClip);
                Assert.AreEqual(SoundBank.TierPitch(tier), Sfx.LastPitch, 1e-5f);
                Assert.Greater(Sfx.LastPitch, before, "tier " + tier);
                before = Sfx.LastPitch;
            }
            Assert.AreEqual(1f, SoundBank.TierPitch(0), 1e-5f);
            Assert.Less(SoundBank.TierPitch(5), 1.5f, "the top tier stays the same sound, only brighter");
        }

        [Test]
        public void Each_Clip_Peaks_Under_Minus_Three_Sits_At_The_Common_Level_And_Is_Short([ValueSource(nameof(Moments))] SoundMoment moment)
        {
            AudioClip clip = Loaded(moment);
            SoundMeter m = SoundMeter.Of(clip);
            TestContext.WriteLine(moment + " " + clip.name + ": " + m + $", {clip.length:0.000} s");
            Assert.LessOrEqual(m.PeakDb, MaxPeakDb, moment + ": " + m);
            Assert.AreEqual(CommonRmsDb, m.RmsDb, RmsToleranceDb, moment + ": " + m);
            Assert.LessOrEqual(clip.length, SoundBank.MaxSeconds, moment + " is " + clip.length + " s");
        }

        [Test]
        public void No_Clip_Is_Shrill([ValueSource(nameof(Moments))] SoundMoment moment)
        {
            SoundMeter m = SoundMeter.Of(Loaded(moment));
            Assert.Less(m.CentroidHz, MaxCentroidHz, moment + ": " + m);
        }

        [Test]
        public void At_The_Default_Sliders_No_Clip_Is_More_Than_Six_Decibels_Over_The_Music([ValueSource(nameof(Moments))] SoundMoment moment)
        {
            float music = float.MaxValue;
            foreach (Sfx.Tune tune in new[] { Sfx.Tune.Moon, Sfx.Tune.Dawn, Sfx.Tune.Rain })
                music = Mathf.Min(music, SoundMeter.Of(Sfx.TuneClipOf(tune), Sfx.MelodyLevel * Sfx.DefaultMusicGain).RmsDb);
            SoundMeter m = SoundMeter.Of(Loaded(moment), SoundBank.Volume * Sfx.DefaultSfxGain);
            TestContext.WriteLine($"{moment}: {m.RmsDb:0.0} dBFS played, quietest tune {music:0.0} dBFS, {m.RmsDb - music:+0.0;-0.0} dB");
            Assert.LessOrEqual(m.RmsDb - music, MaxOverMusicDb, $"{moment} {m.RmsDb:0.0} dBFS against the quietest tune's {music:0.0}");
        }

        [Test]
        public void The_Sound_Slider_At_Zero_Silences_Every_Moment([ValueSource(nameof(Moments))] SoundMoment moment)
        {
            Sfx.SetSfx(0f);
            Play(moment);
            Assert.AreEqual(moment, Sfx.LastMoment);
            Assert.AreEqual(0f, Sfx.LastVolumeOf(Names[moment]), moment + " is silent");
            Sfx.SetSfx(Sfx.DefaultSfxGain);
            Play(moment);
            Assert.Greater(Sfx.LastVolumeOf(Names[moment]), 0f, moment + " is heard again");
        }

        [Test]
        public void Reduced_Motion_Leaves_Every_Moment_Sounding_The_Same([ValueSource(nameof(Moments))] SoundMoment moment)
        {
            Motion.SetReduced(false);
            Play(moment);
            AudioClip clip = Sfx.LastClip;
            float volume = Sfx.LastVolumeOf(Names[moment]), pitch = Sfx.LastPitch;

            Motion.SetReduced(true);
            Play(moment);
            Assert.AreSame(clip, Sfx.LastClip, moment.ToString());
            Assert.AreEqual(volume, Sfx.LastVolumeOf(Names[moment]), 1e-6f, moment.ToString());
            Assert.AreEqual(pitch, Sfx.LastPitch, 1e-6f, moment.ToString());
            Assert.Greater(volume, 0f);
        }
    }
}
