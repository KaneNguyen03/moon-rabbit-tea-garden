using System;
using System.Collections.Generic;
using UnityEngine;

namespace VuonTra.Game
{
    /// <summary>
    /// Every sound in the game, synthesized in code so nothing has to be downloaded. Volumes go
    /// through three gains (music, effects, station sounds) that the settings sliders own.
    /// </summary>
    public static class Sfx
    {
        const int Rate = 44100;
        const string MusicKey = "vol.music", SfxKey = "vol.sfx", StationKey = "vol.station", TuneKey = "music.tune";
        const float BedLevel = 0.35f;
        const float MelodyLevel = 0.5f;
        public const float DefaultStationGain = 0.7f;

        static bool _loaded;
        static float _musicGain = 0.6f, _sfxGain = 0.8f, _stationGain = DefaultStationGain;
        static Tune _tune = Tune.Auto;

        // Read from the saved settings the first time anything asks, so the pause card's sliders, built
        // before the sources are attached, already show what was saved.
        public static float MusicGain { get { Load(); return _musicGain; } }
        public static float SfxGain { get { Load(); return _sfxGain; } }
        /// <summary>The "Tiếng trạm" slider: how loud a station's own loop is up close (#63), apart from the blips.</summary>
        public static float StationGain { get { Load(); return _stationGain; } }

        static void Load(bool again = false)
        {
            if (_loaded && !again) return;
            _loaded = true;
            _musicGain = PlayerPrefs.GetFloat(MusicKey, 0.6f);
            _sfxGain = PlayerPrefs.GetFloat(SfxKey, 0.8f);
            _stationGain = PlayerPrefs.GetFloat(StationKey, DefaultStationGain);
            _tune = (Tune)Mathf.Clamp(PlayerPrefs.GetInt(TuneKey, 0), 0, 3);
        }

        static AudioSource _oneShots, _bed, _music;
        static AudioClip _blip, _click, _sting, _tick, _ambient, _melody, _pet, _milestone, _reward, _sheetUp, _sheetDown, _deny;
        static readonly AudioClip[] _summon = new AudioClip[6];

        /// <summary>The name of the last sound asked for, whether or not it could be heard (for tests).</summary>
        public static string Last { get; private set; } = "";
        static float _lastTick;

        /// <summary>Adds the two audio sources to <paramref name="host"/> and starts the ambient bed.</summary>
        public static void Attach(GameObject host)
        {
            Load(true);
            // Already attached here (the screen was rebuilt): keep the music playing without a gap.
            if (_oneShots != null && _oneShots.gameObject == host) return;

            _oneShots = host.AddComponent<AudioSource>();
            _oneShots.playOnAwake = false;
            _oneShots.spatialBlend = 0f;

            _bed = host.AddComponent<AudioSource>();
            _bed.playOnAwake = false;
            _bed.spatialBlend = 0f;
            _bed.loop = true;
            _bed.clip = Ambient();
            _bed.volume = BedLevel * MusicGain;
            if (Application.isPlaying) _bed.Play();

            _music = host.AddComponent<AudioSource>();
            _music.playOnAwake = false;
            _music.spatialBlend = 0f;
            _music.loop = true;
            if (_lastHour < 0d) _lastHour = DateTime.Now.TimeOfDay.TotalHours;
            PlayingTune = TuneAt(CurrentTune, _lastHour);
            _music.clip = TuneClip(PlayingTune);
            _music.volume = MelodyLevel * MusicGain;
            if (Application.isPlaying) _music.Play();
        }

        public static void SetMusic(float gain)
        {
            Load();
            _musicGain = Mathf.Clamp01(gain);
            PlayerPrefs.SetFloat(MusicKey, _musicGain);
            if (_bed != null) _bed.volume = BedLevel * MusicGain;
            if (_music != null) _music.volume = MelodyLevel * MusicGain;
        }

        public static void SetSfx(float gain)
        {
            Load();
            _sfxGain = Mathf.Clamp01(gain);
            PlayerPrefs.SetFloat(SfxKey, _sfxGain);
        }

        /// <summary>The station sounds' own slider; <see cref="StationLoopPlayer"/> reads it every frame.</summary>
        public static void SetStation(float gain)
        {
            Load();
            _stationGain = Mathf.Clamp01(gain);
            PlayerPrefs.SetFloat(StationKey, _stationGain);
        }

        // ----- the tunes (#63) -----

        /// <summary>The player's choice of tune; Auto lets the clock pick between the moon and daybreak.</summary>
        public enum Tune { Auto, Moon, Dawn, Rain }

        /// <summary>What the player chose on the pause card.</summary>
        public static Tune CurrentTune { get { Load(); return _tune; } }
        /// <summary>What is playing now: never Auto.</summary>
        public static Tune PlayingTune { get; private set; } = Tune.Moon;
        /// <summary>The clip on the music source (for tests).</summary>
        public static AudioClip MusicClip => _music != null ? _music.clip : null;
        static double _lastHour = -1d;
        static AudioClip _dawn, _rain;

        /// <summary>Auto is "Rạng sáng" from dawn (5 h) until dusk (17 h), and "Đêm trăng" through the evening and night.</summary>
        public static Tune TuneAt(Tune choice, double hour)
        {
            if (choice != Tune.Auto) return choice;
            hour = ((hour % 24d) + 24d) % 24d;
            return hour >= VuonTra.Core.Daylight.DawnStart && hour < VuonTra.Core.Daylight.DuskStart ? Tune.Dawn : Tune.Moon;
        }

        public static void SetTune(Tune choice)
        {
            Load();
            _tune = choice;
            PlayerPrefs.SetInt(TuneKey, (int)choice);
            Retune();
        }

        /// <summary>Called every frame with the garden's clock, so Auto changes tune at five and at seventeen.</summary>
        public static void UpdateTune(double hour)
        {
            _lastHour = hour;
            if (TuneAt(CurrentTune, hour) != PlayingTune) Retune();
        }

        static void Retune()
        {
            PlayingTune = TuneAt(CurrentTune, _lastHour < 0d ? DateTime.Now.TimeOfDay.TotalHours : _lastHour);
            if (_music == null) return;
            AudioClip clip = TuneClip(PlayingTune);
            if (_music.clip == clip) return;
            _music.clip = clip;
            if (Application.isPlaying) _music.Play();
        }

        static AudioClip TuneClip(Tune tune)
        {
            switch (tune)
            {
                case Tune.Dawn: return _dawn ?? (_dawn = Daybreak());
                case Tune.Rain: return _rain ?? (_rain = Rain());
                default: return Melody();
            }
        }

        /// <summary>A small confirmation: something was chosen or put in place.</summary>
        public static void Blip() => Play("blip", _blip ?? (_blip = MakeBlip()), 0.55f, 0.08f);
        public static void Click() => Play("click", _click ?? (_click = MakeClick()), 0.5f, 0.05f);
        /// <summary>Something worth celebrating: a new station, a new rabbit.</summary>
        public static void Sting() => Play("sting", _sting ?? (_sting = MakeSting()), 0.6f, 0f);

        /// <summary>A rabbit arrived: the rarer the tier, the more bells and the higher they climb.</summary>
        public static void Summon(int tier)
        {
            tier = Mathf.Clamp(tier, 0, _summon.Length - 1);
            Play("summon-" + tier, _summon[tier] ?? (_summon[tier] = MakeSummon(tier)), 0.6f, 0f);
        }

        // ----- one sound for each kind of touch -----
        /// <summary>A rabbit was patted: two quick soft chirps.</summary>
        public static void Pet() => Play("pet", _pet ?? (_pet = MakePet()), 0.5f, 0.1f);
        /// <summary>A level was bought.</summary>
        public static void Upgrade() => Play("upgrade", _blip ?? (_blip = MakeBlip()), 0.55f, 0.08f);
        /// <summary>A station crossed a milestone: four rising bells.</summary>
        public static void Milestone() => Play("milestone", _milestone ?? (_milestone = MakeMilestone()), 0.6f, 0f);
        /// <summary>A station or a plot was opened.</summary>
        public static void Unlock() => Play("unlock", _sting ?? (_sting = MakeSting()), 0.6f, 0f);
        /// <summary>A reward was collected: two bright coins.</summary>
        public static void Reward() => Play("reward", _reward ?? (_reward = MakeReward()), 0.55f, 0.03f);
        /// <summary>A sheet slid up, or back down.</summary>
        public static void SheetUp() => Play("sheet-up", _sheetUp ?? (_sheetUp = MakeSwish(true)), 0.4f, 0.04f);
        public static void SheetDown() => Play("sheet-down", _sheetDown ?? (_sheetDown = MakeSwish(false)), 0.34f, 0.04f);
        /// <summary>A picture was taken.</summary>
        public static void Shutter() => Play("shutter", _click ?? (_click = MakeClick()), 0.6f, 0f);
        /// <summary>Not enough to buy this: two low soft notes going down.</summary>
        public static void Deny() => Play("deny", _deny ?? (_deny = MakeDeny()), 0.5f, 0f);

        /// <summary>A station paid out. Quiet and rate-limited so a busy garden never chatters.</summary>
        public static void Tick()
        {
            if (Time.unscaledTime - _lastTick < 0.35f) return;
            _lastTick = Time.unscaledTime;
            Play("tick", _tick ?? (_tick = MakeTick()), 0.22f, 0.1f);
        }

        static void Play(string name, AudioClip clip, float volume, float pitchSpread)
        {
            Last = name;
            if (_oneShots == null || !Application.isPlaying || SfxGain <= 0f) return;
            _oneShots.pitch = 1f + UnityEngine.Random.Range(-pitchSpread, pitchSpread);
            _oneShots.PlayOneShot(clip, volume * SfxGain);
        }

        static AudioClip Make(string name, float seconds, Func<float, float> sample)
        {
            int n = Mathf.Max(1, (int)(Rate * seconds));
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                // 5 ms fades at both ends: raw edges click.
                float edge = Mathf.Min(1f, Mathf.Min(t / 0.005f, (seconds - t) / 0.005f));
                data[i] = Mathf.Clamp(sample(t), -1f, 1f) * Mathf.Max(0f, edge);
            }
            AudioClip clip = AudioClip.Create(name, n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static AudioClip MakeBlip()
        {
            float phase = 0f;
            return Make("blip", 0.12f, t =>
            {
                float freq = Mathf.Lerp(880f, 1320f, Mathf.Clamp01(t / 0.09f));
                phase += 2f * Mathf.PI * freq / Rate;
                return Mathf.Sin(phase) * Mathf.Exp(-t * 26f) * 0.8f;
            });
        }

        static AudioClip MakePet()
        {
            float phase = 0f;
            return Make("pet", 0.16f, t =>
            {
                float local = t < 0.07f ? t : t - 0.07f;
                float freq = (t < 0.07f ? 1250f : 1560f) + local * 2400f;
                phase += 2f * Mathf.PI * freq / Rate;
                return Mathf.Sin(phase) * Mathf.Min(1f, local / 0.006f) * Mathf.Exp(-local * 42f) * 0.6f;
            });
        }

        static AudioClip MakeMilestone()
        {
            float[] notes = { 523.25f, 659.25f, 783.99f, 1046.5f };
            return Make("milestone", 0.85f, t =>
            {
                float sum = 0f;
                for (int k = 0; k < notes.Length; k++)
                {
                    float local = t - k * 0.09f;
                    if (local < 0f) continue;
                    float env = Mathf.Min(1f, local / 0.006f) * Mathf.Exp(-local * (k == notes.Length - 1 ? 4f : 8f));
                    float w = 2f * Mathf.PI * notes[k] * local;
                    sum += (Mathf.Sin(w) + 0.3f * Mathf.Sin(2f * w) + 0.12f * Mathf.Sin(3f * w)) * env;
                }
                return sum * 0.28f;
            });
        }

        static AudioClip MakeReward()
        {
            float[] notes = { 987.77f, 1318.5f };
            return Make("reward", 0.3f, t =>
            {
                float sum = 0f;
                for (int k = 0; k < notes.Length; k++)
                {
                    float local = t - k * 0.075f;
                    if (local < 0f) continue;
                    float w = 2f * Mathf.PI * notes[k] * local;
                    sum += (Mathf.Sin(w) + 0.2f * Mathf.Sin(2.76f * w)) * Mathf.Min(1f, local / 0.004f) * Mathf.Exp(-local * 16f);
                }
                return sum * 0.4f;
            });
        }

        /// <summary>Soft air: filtered noise whose brightness rises (a sheet coming up) or falls (going down).</summary>
        static AudioClip MakeSwish(bool up)
        {
            var rng = new System.Random(up ? 11 : 13);
            float low = 0f;
            const float length = 0.16f;
            return Make(up ? "sheet-up" : "sheet-down", length, t =>
            {
                float k = Mathf.Clamp01(t / length);
                float open = Mathf.Lerp(0.04f, 0.3f, up ? k : 1f - k);
                low += ((float)rng.NextDouble() * 2f - 1f - low) * open;
                return low * Mathf.Sin(k * Mathf.PI) * 0.9f;
            });
        }

        static AudioClip MakeDeny()
        {
            return Make("deny", 0.22f, t =>
            {
                float local = t < 0.09f ? t : t - 0.09f;
                float freq = t < 0.09f ? 233.08f : 174.61f;
                float w = 2f * Mathf.PI * freq * local;
                return (Mathf.Sin(w) + 0.35f * Mathf.Sin(3f * w)) * Mathf.Min(1f, local / 0.005f) * Mathf.Exp(-local * 18f) * 0.55f;
            });
        }

        static AudioClip MakeTick()
        {
            float phase = 0f;
            return Make("tick", 0.07f, t =>
            {
                phase += 2f * Mathf.PI * 660f / Rate;
                return Mathf.Sin(phase) * Mathf.Exp(-t * 60f) * 0.7f;
            });
        }

        static AudioClip MakeClick()
        {
            var rng = new System.Random(7);
            float low = 0f;
            return Make("click", 0.02f, t =>
            {
                low += ((float)rng.NextDouble() * 2f - 1f - low) * 0.35f;
                return low * 0.9f;
            });
        }

        /// <summary>
        /// Tier 0 is two soft bells; each tier adds a bell and starts a little higher, so tier 5 is a
        /// seven-note climb with a shimmer on top.
        /// </summary>
        static AudioClip MakeSummon(int tier)
        {
            int count = 2 + tier;
            float gap = tier >= 4 ? 0.085f : 0.11f;
            float root = 523.25f * Mathf.Pow(1.059463f, tier * 2); // a whole tone up per tier
            float[] ratios = { 1f, 1.25f, 1.5f, 2f, 2.5f, 3f, 4f };
            float length = 0.45f + count * gap;
            return Make("summon-" + tier, length, t =>
            {
                float sum = 0f;
                for (int k = 0; k < count; k++)
                {
                    float local = t - k * gap;
                    if (local < 0f) continue;
                    float env = Mathf.Min(1f, local / 0.008f) * Mathf.Exp(-local * (tier >= 4 ? 5f : 7f));
                    float w = 2f * Mathf.PI * root * ratios[k] * local;
                    sum += (Mathf.Sin(w) + 0.25f * Mathf.Sin(2f * w) + (tier >= 4 ? 0.12f * Mathf.Sin(3f * w) : 0f)) * env;
                }
                return sum * (0.34f - tier * 0.02f);
            });
        }

        static AudioClip MakeSting()
        {
            float[] notes = { 523.25f, 659.25f, 783.99f };
            return Make("sting", 0.5f, t =>
            {
                float sum = 0f;
                for (int k = 0; k < notes.Length; k++)
                {
                    float local = t - k * 0.11f;
                    if (local < 0f) continue;
                    float env = Mathf.Min(1f, local / 0.008f) * Mathf.Exp(-local * 7f);
                    float w = 2f * Mathf.PI * notes[k] * local;
                    sum += (Mathf.Sin(w) + 0.25f * Mathf.Sin(2f * w)) * env;
                }
                return sum * 0.32f;
            });
        }

        /// <summary>
        /// "Đêm trăng": a slow music-box lullaby in C major pentatonic, a bell-like lead over soft low notes.
        /// Sixteen bars of four beats at 76 bpm, written so the last bar leads back into the first.
        /// </summary>
        static AudioClip Melody()
        {
            if (_melody != null) return _melody;
            // Semitones above middle C; -99 is a rest. One entry per half beat.
            int[] lead =
            {
                 4, -99,  7, -99,  9,  7,  4, -99,    2, -99,  4, -99,  0, -99, -99, -99,
                 4, -99,  7, -99, 12,  9,  7, -99,    9, -99,  7, -99,  4, -99, -99, -99,
                12, -99,  9, -99,  7,  9, 12, -99,   14, -99, 12, -99,  9, -99, -99, -99,
                 7, -99,  4, -99,  2,  4,  7, -99,    4, -99,  2, -99,  0, -99, -99, -99,
            };
            // One low note per two beats.
            int[] bass = { -12, -5, -10, -5, -12, -5, -7, -5, -8, -3, -10, -5, -12, -5, -7, -12 };
            _melody = Compose("lullaby", 76f, 261.63f, lead, bass, new Voice(0.16f, 2.6f, 0.35f), new Voice(0.13f, 1.1f, 0.05f));
            return _melody;
        }

        /// <summary>
        /// "Rạng sáng": the same music box woken up, a little quicker (88 bpm) in G major pentatonic, the lead
        /// climbing where the lullaby settles, over a walking I–IV–V–vi.
        /// </summary>
        static AudioClip Daybreak()
        {
            // Semitones above G; -99 is a rest. One entry per half beat.
            int[] lead =
            {
                 0, -99,  4,  7,  9, -99,  7, -99,   12, -99,  9,  7,  4, -99, -99, -99,
                 2, -99,  4,  7,  9, 12, 14, -99,   12, -99,  9, -99,  7, -99, -99, -99,
                 9, -99, 12, -99, 14, 12,  9, -99,    7, -99,  9, -99, 12, -99, -99, -99,
                 7,  4,  2, -99,  4, -99,  7, -99,    2, -99, -99,  4,  0, -99, -99, -99,
            };
            int[] bass = { -12, -5, -7, -5, -12, -3, -7, -5, -3, -7, -12, -5, -7, -5, -12, -12 };
            return Compose("daybreak", 88f, 392f, lead, bass, new Voice(0.15f, 3.2f, 0.25f), new Voice(0.12f, 1.4f, 0.05f));
        }

        /// <summary>
        /// "Mưa": slow (64 bpm) and sparse in A minor pentatonic, long soft notes over Am–F–C–G, with a quiet
        /// patter of rain under it. There is no rain in the garden yet; the tune is there for whoever wants it.
        /// </summary>
        static AudioClip Rain()
        {
            // Semitones above A; -99 is a rest.
            int[] lead =
            {
                 7, -99, -99,  5,  3, -99, -99, -99,    0, -99,  3, -99,  5, -99, -99, -99,
                 7, -99, -99, 10, 12, -99, 10, -99,    7, -99, -99, -99, -99, -99, -99, -99,
                 3, -99, -99,  5,  7, -99, -99, -99,   10, -99,  7, -99,  5, -99, -99, -99,
                 3, -99, -99,  0,  3, -99,  5, -99,    0, -99, -99, -99, -99, -99, -99, -99,
            };
            int[] bass = { -12, -12, -16, -16, -9, -9, -14, -14, -12, -12, -16, -16, -9, -14, -12, -12 };
            return Compose("rain", 64f, 220f, lead, bass, new Voice(0.17f, 1.5f, 0.12f), new Voice(0.11f, 0.8f, 0.02f), data =>
            {
                // The patter: soft filtered noise and the odd drop, folded so the loop has no seam.
                var rng = new System.Random(23);
                float[] bed = Seamless(data.Length, (buf, r) =>
                {
                    float low = 0f, lower = 0f;
                    for (int i = 0; i < buf.Length; i++)
                    {
                        float white = (float)r.NextDouble() * 2f - 1f;
                        low += (white - low) * 0.25f;
                        lower += (low - lower) * 0.02f;
                        buf[i] += (low - lower) * 0.05f;
                    }
                    int drops = (int)(buf.Length / (float)Rate * 7f);
                    for (int k = 0; k < drops; k++)
                        Ping(buf, r.Next(buf.Length), 1800f + (float)r.NextDouble() * 1600f, 0.04f + (float)r.NextDouble() * 0.05f, 90f, 0.03f);
                }, rng);
                for (int i = 0; i < data.Length; i++) data[i] += bed[i];
            });
        }

        /// <summary>How one line of a tune sounds: how loud, how fast it dies away, how much bell is on top.</summary>
        readonly struct Voice
        {
            public readonly float Gain, Decay, Shimmer;
            public Voice(float gain, float decay, float shimmer) { Gain = gain; Decay = decay; Shimmer = shimmer; }
        }

        /// <summary>
        /// Writes a looping tune: one lead note per entry (half a beat each), one bass note per two beats,
        /// semitones above <paramref name="root"/> (the lead an octave up). Tails wrap round, so there is no seam.
        /// </summary>
        static AudioClip Compose(string name, float bpm, float root, int[] lead, int[] bass, Voice leadVoice, Voice bassVoice, Action<float[]> under = null)
        {
            float beat = 60f / bpm;
            float step = beat / 2f;
            float seconds = lead.Length * step;
            int n = (int)(Rate * seconds);
            var data = new float[n];

            void Add(float start, int semitone, Voice voice)
            {
                float freq = root * Mathf.Pow(2f, semitone / 12f);
                int from = (int)(start * Rate), length = (int)(Rate * 2.4f);
                for (int i = 0; i < length; i++)
                {
                    float t = i / (float)Rate;
                    float env = Mathf.Min(1f, t / 0.006f) * Mathf.Exp(-t * voice.Decay);
                    float w = 2f * Mathf.PI * freq * t;
                    float v = Mathf.Sin(w) + voice.Shimmer * Mathf.Sin(3f * w) * Mathf.Exp(-t * 9f) + 0.18f * Mathf.Sin(2f * w);
                    data[(from + i) % n] += v * env * voice.Gain; // tails wrap around so the loop has no seam
                }
            }

            for (int i = 0; i < lead.Length; i++)
                if (lead[i] > -99) Add(i * step, lead[i] + 12, leadVoice);
            for (int i = 0; i < bass.Length; i++)
                Add(i * beat * 2f, bass[i], bassVoice);
            under?.Invoke(data);

            for (int i = 0; i < n; i++) data[i] = Mathf.Clamp(data[i], -1f, 1f);
            AudioClip clip = AudioClip.Create(name, n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        // ----- station loops up close (#63) -----

        static readonly Dictionary<string, AudioClip> _loops = new Dictionary<string, AudioClip>();
        static readonly Dictionary<string, AudioClip> _supplied = new Dictionary<string, AudioClip>();

        /// <summary>
        /// Which loop each station plays. Stations of a kind share one: both ovens crackle, both starry
        /// stations tinkle, the leaf basket and the grape vine rustle.
        /// </summary>
        public static string StationLoopOf(string stationId)
        {
            switch (stationId)
            {
                case "am-tra": return "boil";       // teapot on the boil
                case "gio-la": return "rustle";     // tea leaves turned in the basket
                case "noi-mut": return "simmer";    // jam thickening in the pot
                case "coi-hoa": return "thump";     // petals pounded in the mortar
                case "lo-banh": return "crackle";   // chestnut oven
                case "to-mat": return "drip";       // honey dripping, a bee or two
                case "tra-sua": return "shake";     // ice shaken in the milk tea
                case "binh-sao": return "tinkle";   // stars in the brewing jar
                case "gian-nho": return "rustle";   // the vine's leaves
                case "tiem-banh": return "crackle"; // the mooncake oven
                case "suoi-hoa": return "stream";   // the flower spring
                case "dai-sao": return "tinkle";    // the observatory's star chimes
                case "ho-sen": return "stream";     // water round the lotus leaves (#67)
                case "lo-keo": return "simmer";     // malt syrup bubbling down to candy
                case "ruong-nep": return "rustle";  // rice stalks in the paddy
                case "thuyen-tra": return "boil";   // a kettle on the night boat
                default: return null;
            }
        }

        /// <summary>
        /// A station's loop: a supplied recording if there is one (the hook for <c>Art/Final/sfx_&lt;id&gt;.ogg</c>,
        /// which a seeder can hand over with <see cref="SupplyStationClip"/>), else the one made here, made once.
        /// Null for an unknown station.
        /// </summary>
        public static AudioClip StationClip(string stationId)
        {
            if (stationId == null) return null;
            if (_supplied.TryGetValue(stationId, out AudioClip recorded) && recorded != null) return recorded;
            string loop = StationLoopOf(stationId);
            if (loop == null) return null;
            if (_loops.TryGetValue(loop, out AudioClip made) && made != null) return made;
            made = MakeStationLoop(loop);
            _loops[loop] = made;
            return made;
        }

        /// <summary>Puts a recorded loop in place of the made one for this station; null goes back to the made one.</summary>
        public static void SupplyStationClip(string stationId, AudioClip clip)
        {
            if (clip == null) _supplied.Remove(stationId);
            else _supplied[stationId] = clip;
        }

        static AudioClip MakeStationLoop(string loop)
        {
            int seed = 0;
            foreach (char c in loop) seed = seed * 31 + c; // the same loop every run
            var rng = new System.Random(seed & 0x7fffffff);
            float[] data;
            switch (loop)
            {
                case "boil": data = Seamless(Samples(5f), DrawBoil, rng); break;
                case "simmer": data = Seamless(Samples(5f), DrawSimmer, rng); break;
                case "crackle": data = Seamless(Samples(5f), DrawCrackle, rng); break;
                case "shake": data = Seamless(Samples(5f), DrawShake, rng); break;
                case "tinkle": data = Seamless(Samples(6f), DrawTinkle, rng); break;
                case "thump": data = Seamless(Samples(5f), DrawThump, rng); break;
                case "drip": data = Seamless(Samples(5f), DrawDrip, rng); break;
                case "stream": data = Seamless(Samples(5f), DrawStream, rng); break;
                default: data = Seamless(Samples(5f), DrawRustle, rng); break;
            }
            Level(data, 0.12f);
            AudioClip clip = AudioClip.Create("station-" + loop, data.Length, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static int Samples(float seconds) => (int)(Rate * seconds);

        /// <summary>
        /// Draws a little more than <paramref name="n"/> samples and folds the extra over the start, so the
        /// end runs on into the beginning: any noise or event can loop without a click.
        /// </summary>
        static float[] Seamless(int n, Action<float[], System.Random> draw, System.Random rng)
        {
            int fold = Rate / 4;
            var buf = new float[n + fold];
            draw(buf, rng);
            var data = new float[n];
            Array.Copy(buf, data, n);
            for (int i = 0; i < fold; i++)
            {
                float k = i / (float)fold;
                data[i] = buf[i] * k + buf[n + i] * (1f - k);
            }
            return data;
        }

        /// <summary>Evens the loops out: each to about the same loudness, none past 0.9.</summary>
        static void Level(float[] data, float rms)
        {
            double sum = 0d;
            float peak = 0f;
            for (int i = 0; i < data.Length; i++) { sum += data[i] * data[i]; peak = Mathf.Max(peak, Mathf.Abs(data[i])); }
            float now = (float)Math.Sqrt(sum / data.Length);
            if (now <= 1e-6f) return;
            float gain = Mathf.Min(rms / now, 0.9f / peak);
            for (int i = 0; i < data.Length; i++) data[i] *= gain;
        }

        /// <summary>A short tone that dies away, added at <paramref name="at"/>.</summary>
        static void Ping(float[] buf, int at, float freq, float gain, float decay, float seconds)
        {
            int length = Mathf.Min(Samples(seconds), buf.Length - at);
            for (int i = 0; i < length; i++)
            {
                float t = i / (float)Rate;
                buf[at + i] += Mathf.Sin(2f * Mathf.PI * freq * t) * Mathf.Min(1f, t / 0.002f) * Mathf.Exp(-t * decay) * gain;
            }
        }

        /// <summary>A bubble: a tone that rises as it bursts.</summary>
        static void Bubble(float[] buf, int at, float freq, float rise, float gain, float decay)
        {
            int length = Mathf.Min(Samples(6f / decay), buf.Length - at);
            float phase = 0f;
            for (int i = 0; i < length; i++)
            {
                float t = i / (float)Rate;
                phase += 2f * Mathf.PI * freq * (1f + rise * t) / Rate;
                buf[at + i] += Mathf.Sin(phase) * Mathf.Min(1f, t / 0.003f) * Mathf.Exp(-t * decay) * gain;
            }
        }

        /// <summary>A burst of noise through a soft filter: a tick, a crack, an ice cube, a leaf.</summary>
        static void Burst(float[] buf, int at, float seconds, float gain, float brightness, System.Random r)
        {
            int length = Mathf.Min(Samples(seconds), buf.Length - at);
            float low = 0f;
            for (int i = 0; i < length; i++)
            {
                float k = i / (float)length;
                low += ((float)r.NextDouble() * 2f - 1f - low) * brightness;
                buf[at + i] += low * Mathf.Min(1f, k * 12f) * (1f - k) * (1f - k) * gain;
            }
        }

        /// <summary>Noise between two one-pole filters: <paramref name="high"/> lets the top through, <paramref name="low"/> takes the rumble out.</summary>
        static void Noise(float[] buf, float gain, float high, float low, System.Random r, Func<float, float> swell = null)
        {
            float a = 0f, b = 0f;
            for (int i = 0; i < buf.Length; i++)
            {
                a += ((float)r.NextDouble() * 2f - 1f - a) * high;
                b += (a - b) * low;
                float t = i / (float)Rate;
                buf[i] += (a - b) * gain * (swell != null ? swell(t) : 1f);
            }
        }

        /// <summary>Ấm trà: a low roll of water and quick small bubbles bursting at the top.</summary>
        static void DrawBoil(float[] buf, System.Random r)
        {
            Noise(buf, 0.5f, 0.05f, 0.004f, r, t => 0.8f + 0.2f * Mathf.Sin(t * 5.1f));
            int bubbles = (int)(buf.Length / (float)Rate * 16f);
            for (int k = 0; k < bubbles; k++)
                Bubble(buf, r.Next(buf.Length), 260f + (float)r.NextDouble() * 420f, 2.5f, 0.12f + (float)r.NextDouble() * 0.12f, 70f);
        }

        /// <summary>Nồi mứt: slow thick bubbles, low and round, a few a second.</summary>
        static void DrawSimmer(float[] buf, System.Random r)
        {
            Noise(buf, 0.25f, 0.02f, 0.003f, r);
            int bubbles = (int)(buf.Length / (float)Rate * 3.5f);
            for (int k = 0; k < bubbles; k++)
                Bubble(buf, r.Next(buf.Length), 110f + (float)r.NextDouble() * 120f, 1.2f, 0.35f + (float)r.NextDouble() * 0.2f, 22f);
        }

        /// <summary>Lò bánh: a soft roar under the snap of the fire, the odd louder pop.</summary>
        static void DrawCrackle(float[] buf, System.Random r)
        {
            Noise(buf, 0.35f, 0.03f, 0.002f, r, t => 0.7f + 0.3f * Mathf.Sin(t * 1.3f) * Mathf.Sin(t * 0.7f));
            int cracks = (int)(buf.Length / (float)Rate * 11f);
            for (int k = 0; k < cracks; k++)
            {
                int at = r.Next(buf.Length);
                bool pop = r.NextDouble() < 0.12;
                Burst(buf, at, pop ? 0.02f : 0.004f + (float)r.NextDouble() * 0.006f, pop ? 0.9f : 0.3f + (float)r.NextDouble() * 0.4f, 0.6f, r);
                // Cracks come in twos and threes.
                if (r.NextDouble() < 0.4) Burst(buf, Mathf.Min(buf.Length - 1, at + Samples(0.02f + (float)r.NextDouble() * 0.05f)), 0.004f, 0.25f, 0.6f, r);
            }
        }

        /// <summary>Trà sữa: ice in a shaker, back and forth, twelve times in five seconds.</summary>
        static void DrawShake(float[] buf, System.Random r)
        {
            float period = 5f / 12f;
            for (int beat = 0; beat * period * Rate < buf.Length; beat++)
            {
                int start = Samples(beat * period + (beat % 2 == 1 ? 0.02f : 0f));
                // The cubes all hit the end of the shaker at once, then rattle down.
                for (int k = 0; k < 9; k++)
                {
                    int at = start + Samples((float)(r.NextDouble() * r.NextDouble()) * 0.12f);
                    if (at >= buf.Length) continue;
                    Burst(buf, at, 0.012f, 0.35f + (float)r.NextDouble() * 0.3f, 0.75f, r);
                    Ping(buf, at, 2400f + (float)r.NextDouble() * 1800f, 0.05f, 120f, 0.04f);
                }
                Burst(buf, Mathf.Min(buf.Length - 1, start), 0.09f, 0.12f, 0.15f, r); // the swoosh of the drink
            }
        }

        /// <summary>Bình ủ sao: small glass chimes, high and far apart, in a pentatonic so they never clash.</summary>
        static void DrawTinkle(float[] buf, System.Random r)
        {
            float[] notes = { 1046.5f, 1174.66f, 1318.51f, 1567.98f, 1760f, 2093f };
            Noise(buf, 0.03f, 0.5f, 0.2f, r);
            int count = (int)(buf.Length / (float)Rate * 2.4f);
            for (int k = 0; k < count; k++)
            {
                int at = r.Next(buf.Length);
                float f = notes[r.Next(notes.Length)], g = 0.12f + (float)r.NextDouble() * 0.1f;
                Ping(buf, at, f, g, 4f, 1.2f);
                Ping(buf, at, f * 2.76f, g * 0.25f, 9f, 0.5f);
                Ping(buf, at, f * 5.4f, g * 0.08f, 16f, 0.25f);
            }
        }

        /// <summary>Cối giã hoa: the pestle comes down six times in five seconds, a soft thud and a squash of petals.</summary>
        static void DrawThump(float[] buf, System.Random r)
        {
            float period = 5f / 6f;
            for (int beat = 0; beat * period * Rate < buf.Length; beat++)
            {
                int at = Samples(beat * period);
                int length = Mathf.Min(Samples(0.35f), buf.Length - at);
                float phase = 0f;
                for (int i = 0; i < length; i++)
                {
                    float t = i / (float)Rate;
                    phase += 2f * Mathf.PI * Mathf.Lerp(95f, 55f, Mathf.Clamp01(t / 0.12f)) / Rate;
                    buf[at + i] += Mathf.Sin(phase) * Mathf.Min(1f, t / 0.003f) * Mathf.Exp(-t * 16f) * 0.8f;
                }
                Burst(buf, at, 0.03f, 0.5f, 0.3f, r);
                Burst(buf, Mathf.Min(buf.Length - 1, at + Samples(0.05f)), 0.14f, 0.15f, 0.08f, r); // petals squashed
            }
        }

        /// <summary>Tổ mật: slow honey drops, each a plip that rises, and a bee humming somewhere near.</summary>
        static void DrawDrip(float[] buf, System.Random r)
        {
            for (int i = 0; i < buf.Length; i++)
            {
                float t = i / (float)Rate;
                float wing = 0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * 0.4f * t) * Mathf.Sin(2f * Mathf.PI * 0.6f * t);
                float hum = Mathf.Sin(2f * Mathf.PI * 220f * t) + 0.5f * Mathf.Sin(2f * Mathf.PI * 440f * t) + 0.25f * Mathf.Sin(2f * Mathf.PI * 660f * t);
                buf[i] += hum * wing * (1f + 0.2f * Mathf.Sin(2f * Mathf.PI * 24f * t)) * 0.03f;
            }
            float at = 0.2f;
            while (at * Rate < buf.Length)
            {
                Bubble(buf, Samples(at), 520f + (float)r.NextDouble() * 200f, 3.5f, 0.45f, 38f);
                at += 0.7f + (float)r.NextDouble() * 0.5f;
            }
        }

        /// <summary>Suối hoa: running water, a wash that rises and falls with many small bright bubbles in it.</summary>
        static void DrawStream(float[] buf, System.Random r)
        {
            Noise(buf, 0.6f, 0.3f, 0.06f, r, t => 0.75f + 0.15f * Mathf.Sin(t * 2.3f) + 0.1f * Mathf.Sin(t * 5.7f));
            int bubbles = (int)(buf.Length / (float)Rate * 30f);
            for (int k = 0; k < bubbles; k++)
                Bubble(buf, r.Next(buf.Length), 700f + (float)r.NextDouble() * 1100f, 1.5f, 0.05f + (float)r.NextDouble() * 0.06f, 110f);
        }

        /// <summary>Giỏ lá, giàn nho: leaves stirred, in soft gusts with a few dry crisps.</summary>
        static void DrawRustle(float[] buf, System.Random r)
        {
            Noise(buf, 0.5f, 0.45f, 0.12f, r, t => Mathf.Pow(Mathf.Max(0f, Mathf.Sin(t * 1.9f) * 0.6f + Mathf.Sin(t * 0.8f + 1f) * 0.4f), 2f) + 0.1f);
            int crisps = (int)(buf.Length / (float)Rate * 8f);
            for (int k = 0; k < crisps; k++)
                Burst(buf, r.Next(buf.Length), 0.006f + (float)r.NextDouble() * 0.01f, 0.2f, 0.8f, r);
        }

        static AudioClip Ambient()
        {
            if (_ambient != null) return _ambient;
            var rng = new System.Random(11);
            float brown = 0f;
            const float seconds = 8f;
            _ambient = Make("night", seconds, t =>
            {
                brown = Mathf.Clamp(brown + ((float)rng.NextDouble() * 2f - 1f) * 0.02f, -1f, 1f);
                brown *= 0.999f;
                // whole numbers of cycles in the loop so the pad has no seam
                float swell = 0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * t / seconds);
                float pad = Mathf.Sin(2f * Mathf.PI * 110f * t) * 0.05f + Mathf.Sin(2f * Mathf.PI * 164.75f * t) * 0.035f;
                return brown * 0.22f + pad * swell;
            });
            return _ambient;
        }
    }
}
