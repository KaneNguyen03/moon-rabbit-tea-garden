using NUnit.Framework;
using UnityEngine;
using VuonTra.Game;

namespace VuonTra.Core.Tests
{
    public class SoundLevelTests
    {
        static readonly string[] Loops = { "boil", "simmer", "crackle", "shake", "tinkle", "thump", "drip", "stream", "rustle" };
        static readonly string[] LowFishing = { "sink", "reel", "bait-lost", "float-up" };

        const float MaxLoopCentroidHz = 4500f;
        const float MaxLoopAbove4k = 0.20f;
        const float MaxLoopOverMusicDb = 0f, MinLoopOverMusicDb = -6f;
        const float MinFishingCentroidHz = 700f;
        const float MaxFishingPeakDb = -3f;

        static string StationOf(string loop)
        {
            foreach (string id in new[] { "am-tra", "gio-la", "noi-mut", "coi-hoa", "lo-banh", "to-mat", "tra-sua", "binh-sao", "suoi-hoa" })
                if (Sfx.StationLoopOf(id) == loop) return id;
            return null;
        }

        [Test]
        public void The_Meter_Reads_A_Known_Tone_Right()
        {
            var tone = new float[44100];
            for (int i = 0; i < tone.Length; i++) tone[i] = 0.5f * Mathf.Sin(2f * Mathf.PI * 1000f * i / 44100f);
            SoundMeter m = SoundMeter.Of(tone, 44100);
            Assert.AreEqual(-6.02f, m.PeakDb, 0.05f);
            Assert.AreEqual(-9.03f, m.RmsDb, 0.05f);
            Assert.AreEqual(1000f, m.CentroidHz, 15f);
            Assert.Less(m.Above4k, 0.001f);
            for (int i = 0; i < tone.Length; i++) tone[i] = 0.5f * Mathf.Sin(2f * Mathf.PI * 8000f * i / 44100f);
            Assert.Greater(SoundMeter.Of(tone, 44100).Above4k, 0.999f);
        }

        [Test]
        public void No_Station_Loop_Hisses_Above_Four_Kilohertz([ValueSource(nameof(Loops))] string loop)
        {
            string id = StationOf(loop);
            Assert.IsNotNull(id, loop + " belongs to a station");
            SoundMeter m = SoundMeter.Of(Sfx.StationClip(id));
            Assert.Less(m.CentroidHz, MaxLoopCentroidHz, loop + ": " + m);
            Assert.Less(m.Above4k, MaxLoopAbove4k, loop + ": " + m);
        }

        [Test]
        public void At_The_Default_Sliders_A_Station_Loop_Sits_At_Or_Under_The_Music([ValueSource(nameof(Loops))] string loop)
        {
            float music = float.MaxValue;
            foreach (Sfx.Tune tune in new[] { Sfx.Tune.Moon, Sfx.Tune.Dawn, Sfx.Tune.Rain })
                music = Mathf.Min(music, SoundMeter.Of(Sfx.TuneClipOf(tune), Sfx.MelodyLevel * Sfx.DefaultMusicGain).RmsDb);
            SoundMeter m = SoundMeter.Of(Sfx.StationClip(StationOf(loop)), Sfx.DefaultStationGain);
            float over = m.RmsDb - music;
            Assert.LessOrEqual(over, MaxLoopOverMusicDb, $"{loop} {m.RmsDb:0.0} dBFS against the quietest tune's {music:0.0}");
            Assert.GreaterOrEqual(over, MinLoopOverMusicDb, $"{loop} {m.RmsDb:0.0} dBFS against the quietest tune's {music:0.0}");
        }

        [Test]
        public void The_Low_Pond_Sounds_Reach_Up_Where_A_Phone_Plays([ValueSource(nameof(LowFishing))] string name)
        {
            SoundMeter m = SoundMeter.Of(Sfx.FishingClip(name));
            Assert.GreaterOrEqual(m.CentroidHz, MinFishingCentroidHz, name + ": " + m);
            Assert.LessOrEqual(m.PeakDb, MaxFishingPeakDb, name + ": " + m);
        }
    }
}
