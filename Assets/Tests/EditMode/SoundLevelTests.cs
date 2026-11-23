using NUnit.Framework;
using UnityEngine;
using VuonTra.Game;

namespace VuonTra.Core.Tests
{
    /// <summary>
    /// The levels and colour QA measured on 22 Nov (docs/qa-22-11.md on qa-test), held as numbers: the station loops
    /// no longer hiss above 4 kHz or sit over the music, and the pond's low sounds reach up where a phone speaker plays.
    /// Edit mode cannot listen, so these read the made clips' samples through <see cref="SoundMeter"/>.
    /// </summary>
    public class SoundLevelTests
    {
        static readonly string[] Loops = { "boil", "simmer", "crackle", "shake", "tinkle", "thump", "drip", "stream", "rustle" };
        static readonly string[] LowFishing = { "sink", "reel", "bait-lost", "float-up" };

        // Before 23 Nov shake, rustle and crackle had 48–70 % of their energy above 4 kHz (centroids 5.9–8.6 kHz).
        // A loop's centre may sit up to 4.5 kHz, and at most a fifth of its energy may lie above 4 kHz.
        const float MaxLoopCentroidHz = 4500f;
        const float MaxLoopAbove4k = 0.20f;
        // At the default sliders a loop up close is no louder than the quietest tune, and not lost under it either:
        // QA had them 7–12 dB over; now 0 to 6 dB under.
        const float MaxLoopOverMusicDb = 0f, MinLoopOverMusicDb = -6f;
        // Phone speakers give little below 400 Hz: the pond's low sounds keep their centre at 0.7 kHz or above
        // (they sat at 0.2–0.4 kHz) and their peaks at −3 dBFS or under, so the added partials never clip.
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
