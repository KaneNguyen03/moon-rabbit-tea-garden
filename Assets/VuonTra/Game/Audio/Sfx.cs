using System;
using System.Collections.Generic;
using UnityEngine;

namespace VuonTra.Game
{
    public static class Sfx
    {
        const int Rate = 44100;
        const string MusicKey = "vol.music", SfxKey = "vol.sfx", StationKey = "vol.station", TuneKey = "music.tune";
        const float BedLevel = 0.35f;
        const float NoteG4 = 392f, NoteEFlat4 = 311.13f, NoteD4 = 293.66f;
        const float SemitoneRatio = 1.059463f;
        const int SemitonesUpPerTier = 2;
        public const float MelodyLevel = 0.5f;
        public const float DefaultMusicGain = 0.6f;
        public const float DefaultStationGain = 0.5f;

        static bool _loaded;
        static float _musicGain = DefaultMusicGain, _sfxGain = 0.8f, _stationGain = DefaultStationGain;
        static Tune _tune = Tune.Auto;

        public static float MusicGain { get { Load(); return _musicGain; } }
        public static float SfxGain { get { Load(); return _sfxGain; } }
        public static float StationGain { get { Load(); return _stationGain; } }

        static void Load(bool again = false)
        {
            if (_loaded && !again) return;
            _loaded = true;
            _musicGain = PlayerPrefs.GetFloat(MusicKey, DefaultMusicGain);
            _sfxGain = PlayerPrefs.GetFloat(SfxKey, 0.8f);
            _stationGain = PlayerPrefs.GetFloat(StationKey, DefaultStationGain);
            _tune = (Tune)Mathf.Clamp(PlayerPrefs.GetInt(TuneKey, 0), 0, 3);
        }

        static AudioSource _oneShots, _bed, _music;
        static AudioClip _blip, _click, _sting, _tick, _ambient, _melody, _pet, _milestone, _reward, _sheetUp, _sheetDown, _deny;
        static readonly AudioClip[] _summon = new AudioClip[6];

        public static string Last { get; private set; } = "";
        static float _lastTick;

        public static void Attach(GameObject host)
        {
            Load(true);
            if (_oneShots != null && _oneShots.gameObject == host) return;

            _oneShots = host.AddComponent<AudioSource>();
            _oneShots.playOnAwake = false;
            _oneShots.spatialBlend = 0f;

            _bed = host.AddComponent<AudioSource>();
            _bed.playOnAwake = false;
            _bed.spatialBlend = 0f;
            _bed.loop = true;
            _bed.clip = Raining ? RainBed() : Ambient();
            _bed.volume = BedGain;
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
            if (_bed != null) _bed.volume = BedGain;
            if (_music != null) _music.volume = MelodyLevel * MusicGain;
        }

        public static void SetSfx(float gain)
        {
            Load();
            _sfxGain = Mathf.Clamp01(gain);
            PlayerPrefs.SetFloat(SfxKey, _sfxGain);
            if (_bed != null) _bed.volume = BedGain;
        }

        public static void SetStation(float gain)
        {
            Load();
            _stationGain = Mathf.Clamp01(gain);
            PlayerPrefs.SetFloat(StationKey, _stationGain);
        }

        public enum Tune { Auto, Moon, Dawn, Rain }

        public static Tune CurrentTune { get { Load(); return _tune; } }
        public static Tune PlayingTune { get; private set; } = Tune.Moon;
        public static AudioClip MusicClip => _music != null ? _music.clip : null;
        static double _lastHour = -1d;
        static AudioClip _dawn, _rain;

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

        public static AudioClip TuneClipOf(Tune tune) => TuneClip(tune);

        static AudioClip TuneClip(Tune tune)
        {
            switch (tune)
            {
                case Tune.Dawn: return _dawn ?? (_dawn = Daybreak());
                case Tune.Rain: return _rain ?? (_rain = Rain());
                default: return Melody();
            }
        }

        public static void Blip() => Play("blip", _blip ?? (_blip = MakeBlip()), 0.55f, 0.08f);
        public static void Click() => Play("click", _click ?? (_click = MakeClick()), 0.5f, 0.05f);
        public static void Sting() => Play("sting", _sting ?? (_sting = MakeSting()), 0.6f, 0f);

        public static void Summon(int tier)
        {
            tier = Mathf.Clamp(tier, 0, _summon.Length - 1);
            Play("summon-" + tier, _summon[tier] ?? (_summon[tier] = MakeSummon(tier)), 0.6f, 0f);
        }

        public static void Pet() => Play("pet", _pet ?? (_pet = MakePet()), 0.5f, 0.1f);
        public static void Upgrade() => Play("upgrade", _blip ?? (_blip = MakeBlip()), 0.55f, 0.08f);
        public static void Milestone() => Play("milestone", _milestone ?? (_milestone = MakeMilestone()), 0.6f, 0f);
        public static void Unlock() => Play("unlock", _sting ?? (_sting = MakeSting()), 0.6f, 0f);
        public static void Reward() => Play("reward", _reward ?? (_reward = MakeReward()), 0.55f, 0.03f);
        public static void SheetUp() => Play("sheet-up", _sheetUp ?? (_sheetUp = MakeSwish(true)), 0.4f, 0.04f);
        public static void SheetDown() => Play("sheet-down", _sheetDown ?? (_sheetDown = MakeSwish(false)), 0.34f, 0.04f);
        public static void Shutter() => Play("shutter", _click ?? (_click = MakeClick()), 0.6f, 0f);
        public static void Deny() => Play("deny", _deny ?? (_deny = MakeDeny()), 0.5f, 0f);

        public static void Tick()
        {
            if (Time.unscaledTime - _lastTick < 0.35f) return;
            _lastTick = Time.unscaledTime;
            Play("tick", _tick ?? (_tick = MakeTick()), 0.22f, 0.1f);
        }

        static readonly List<string> _recent = new List<string>();
        static readonly Dictionary<string, float> _volumes = new Dictionary<string, float>();

        public static IReadOnlyList<string> Recent => _recent;
        public static void ClearRecent() => _recent.Clear();
        public static float LastVolumeOf(string name) => _volumes.TryGetValue(name, out float v) ? v : 0f;

        static void Play(string name, AudioClip clip, float volume, float pitchSpread)
        {
            Last = name;
            _recent.Add(name);
            if (_recent.Count > 64) _recent.RemoveAt(0);
            _volumes[name] = volume * SfxGain;
            if (_oneShots == null || !Application.isPlaying || SfxGain <= 0f) return;
            _oneShots.pitch = 1f + UnityEngine.Random.Range(-pitchSpread, pitchSpread);
            _oneShots.PlayOneShot(clip, volume * SfxGain);
        }

        static readonly Dictionary<string, AudioClip> _fishing = new Dictionary<string, AudioClip>();

        public static void Cast() => Play("cast", FishingClip("cast"), 0.45f, 0.03f);
        public static void Sink() => Play("sink", FishingClip("sink"), 0.5f, 0.03f);
        public static void Strike() => Play("strike", FishingClip("strike"), 0.45f, 0.04f);
        public static void Reel() => Play("reel", FishingClip("reel"), 0.42f, 0f);
        public static void BaitLost() => Play("bait-lost", FishingClip("bait-lost"), 0.42f, 0f);
        public static void FloatUp() => Play("float-up", FishingClip("float-up"), 0.3f, 0.05f);

        public static AudioClip FishingClip(string name)
        {
            if (name == null) return null;
            if (_fishing.TryGetValue(name, out AudioClip made) && made != null) return made;
            float[] data;
            var r = new System.Random(58);
            switch (name)
            {
                case "cast": data = DrawCast(r); break;
                case "sink": data = DrawSink(r); break;
                case "strike": data = DrawStrike(r); break;
                case "reel": data = DrawReel(r); break;
                case "bait-lost": data = DrawBaitLost(); break;
                case "float-up": data = DrawFloatUp(); break;
                default: return null;
            }
            made = Shape(name, data);
            _fishing[name] = made;
            return made;
        }

        static float[] DrawCast(System.Random r)
        {
            var buf = new float[Samples(0.62f)];
            int swish = Samples(0.3f);
            float low = 0f, lower = 0f;
            for (int i = 0; i < swish; i++)
            {
                float k = i / (float)swish;
                low += ((float)r.NextDouble() * 2f - 1f - low) * Mathf.Lerp(0.04f, 0.22f, Mathf.Sin(k * Mathf.PI));
                lower += (low - lower) * 0.02f;
                buf[i] += (low - lower) * Mathf.Sin(k * Mathf.PI) * 0.9f;
            }
            int at = Samples(0.33f);
            Glide(buf, at, 620f, 240f, 0.05f, 30f, 0.5f);
            Burst(buf, at, 0.03f, 0.12f, 0.35f, r);
            return buf;
        }

        static float[] DrawSink(System.Random r)
        {
            var buf = new float[Samples(0.42f)];
            Glide(buf, 0, 360f, 150f, 0.12f, 13f, 0.3f, 0.008f, SinkVoice);
            Bubble(buf, Samples(0.13f), 420f, 2.5f, 0.12f, 40f);
            Bubble(buf, Samples(0.13f), 1260f, 2.5f, 0.1f, 55f);
            return buf;
        }

        static float[] DrawStrike(System.Random r)
        {
            var buf = new float[Samples(0.5f)];
            Burst(buf, 0, 0.26f, 0.5f, 0.3f, r);
            Glide(buf, 0, 480f, 200f, 0.05f, 26f, 0.3f);
            for (int k = 0; k < 6; k++)
                Bubble(buf, Samples(0.02f + (float)r.NextDouble() * 0.16f), 700f + (float)r.NextDouble() * 600f, 1.8f, 0.07f, 70f);
            return buf;
        }

        static float[] DrawReel(System.Random r)
        {
            var buf = new float[Samples(0.9f)];
            for (int k = 0; k < 9; k++)
            {
                int at = Samples(k * 0.058f);
                float pitch = Mathf.Lerp(700f, 1400f, k / 8f);
                Ping(buf, at, pitch, 0.38f, 80f, 0.06f);
                Burst(buf, at, 0.006f, 0.08f, 0.25f, r);
            }
            int land = Samples(0.55f);
            Glide(buf, land, 150f, 85f, 0.08f, 17f, 0.2f, 0.004f, ThunkVoice);
            Burst(buf, land, 0.05f, 0.3f, 0.5f, r);
            return buf;
        }

        static float[] DrawBaitLost()
        {
            var buf = new float[Samples(0.75f)];
            void Note(float start, float from, float to, float gain)
            {
                int at = Samples(start), length = Mathf.Min(Samples(0.55f), buf.Length - at);
                float phase = 0f;
                for (int i = 0; i < length; i++)
                {
                    float t = i / (float)Rate;
                    phase += 2f * Mathf.PI * Mathf.Lerp(from, to, Mathf.Clamp01(t / 0.35f)) / Rate;
                    float env = Mathf.Min(1f, t / 0.025f) * Mathf.Exp(-t * 6f) * Mathf.Clamp01((0.55f - t) / 0.08f);
                    buf[at + i] += Partials(phase, AwVoice) * env * gain;
                }
            }
            Note(0f, NoteG4, NoteG4, 0.3f);
            Note(0.2f, NoteEFlat4, NoteD4, 0.3f);
            return buf;
        }

        static float[] DrawFloatUp()
        {
            var buf = new float[Samples(0.18f)];
            Bubble(buf, 0, 380f, 3f, 0.4f, 32f);
            Bubble(buf, 0, 1140f, 3f, 0.45f, 40f);
            Bubble(buf, 0, 1900f, 3f, 0.35f, 48f);
            return buf;
        }

        static void Glide(float[] buf, int at, float from, float to, float glide, float decay, float gain, float attack = 0.003f, float[] overtones = null)
        {
            overtones = overtones ?? PlainVoice;
            int length = Mathf.Min(Samples(6f / decay), buf.Length - at);
            float phase = 0f;
            for (int i = 0; i < length; i++)
            {
                float t = i / (float)Rate;
                phase += 2f * Mathf.PI * Mathf.Lerp(from, to, Mathf.Clamp01(t / glide)) / Rate;
                buf[at + i] += Partials(phase, overtones) * Mathf.Min(1f, t / attack) * Mathf.Exp(-t * decay) * gain;
            }
        }

        static readonly float[] PlainVoice = { 0.15f };
        static readonly float[] SinkVoice = { 0.3f, 0.5f, 0.6f, 0.6f, 0.55f, 0.5f, 0.45f, 0.4f, 0.3f };
        static readonly float[] ThunkVoice = { 0.5f, 0.7f, 0.7f, 0.65f, 0.6f, 0.5f, 0.45f, 0.4f };
        static readonly float[] AwVoice = { 0.5f, 0.75f, 0.6f, 0.5f };

        static float Partials(float phase, float[] overtones)
        {
            float sum = Mathf.Sin(phase);
            for (int k = 0; k < overtones.Length; k++) sum += overtones[k] * Mathf.Sin((k + 2) * phase);
            return sum;
        }

        static AudioClip Shape(string name, float[] data)
        {
            float peak = 0f;
            foreach (float v in data) peak = Mathf.Max(peak, Mathf.Abs(v));
            float gain = peak > 0.7f ? 0.7f / peak : 1f;
            int fade = Samples(0.005f);
            for (int i = 0; i < data.Length; i++)
            {
                float edge = Mathf.Min(1f, Mathf.Min(i, data.Length - 1 - i) / (float)fade);
                data[i] *= gain * edge;
            }
            AudioClip clip = AudioClip.Create(name, data.Length, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static AudioClip Make(string name, float seconds, Func<float, float> sample)
        {
            int n = Mathf.Max(1, (int)(Rate * seconds));
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
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

        static AudioClip MakeSummon(int tier)
        {
            int count = 2 + tier;
            float gap = tier >= 4 ? 0.085f : 0.11f;
            float root = 523.25f * Mathf.Pow(SemitoneRatio, tier * SemitonesUpPerTier);
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

        static AudioClip Melody()
        {
            if (_melody != null) return _melody;
            int[] lead =
            {
                 4, -99,  7, -99,  9,  7,  4, -99,    2, -99,  4, -99,  0, -99, -99, -99,
                 4, -99,  7, -99, 12,  9,  7, -99,    9, -99,  7, -99,  4, -99, -99, -99,
                12, -99,  9, -99,  7,  9, 12, -99,   14, -99, 12, -99,  9, -99, -99, -99,
                 7, -99,  4, -99,  2,  4,  7, -99,    4, -99,  2, -99,  0, -99, -99, -99,
            };
            int[] bass = { -12, -5, -10, -5, -12, -5, -7, -5, -8, -3, -10, -5, -12, -5, -7, -12 };
            _melody = Compose("lullaby", 76f, 261.63f, lead, bass, new Voice(0.16f, 2.6f, 0.35f), new Voice(0.13f, 1.1f, 0.05f));
            return _melody;
        }

        static AudioClip Daybreak()
        {
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

        static AudioClip Rain()
        {
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
                float[] bed = Patter(data.Length, new System.Random(23));
                for (int i = 0; i < data.Length; i++) data[i] += bed[i];
            });
        }

        static float[] Patter(int n, System.Random rng)
        {
            return Seamless(n, (buf, r) =>
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
        }

        const float RainBedLevel = 0.45f;
        static AudioClip _rainBed;

        public static bool Raining { get; private set; }
        public static AudioClip BedClip => _bed != null ? _bed.clip : null;
        public static float BedVolume => _bed != null ? _bed.volume : 0f;
        public static AudioClip AmbientClip => Ambient();
        public static AudioClip RainClip => RainBed();

        static float BedGain => Raining ? RainBedLevel * SfxGain : BedLevel * MusicGain;

        public static void SetRain(bool raining)
        {
            if (raining == Raining) return;
            Raining = raining;
            if (_bed == null) return;
            _bed.clip = raining ? RainBed() : Ambient();
            _bed.volume = BedGain;
            if (Application.isPlaying) _bed.Play();
        }

        static AudioClip RainBed()
        {
            if (_rainBed != null) return _rainBed;
            float[] data = Patter(Samples(6f), new System.Random(62));
            Level(data, 0.12f);
            _rainBed = AudioClip.Create("rain-bed", data.Length, 1, Rate, false);
            _rainBed.SetData(data, 0);
            return _rainBed;
        }

        readonly struct Voice
        {
            public readonly float Gain, Decay, Shimmer;
            public Voice(float gain, float decay, float shimmer) { Gain = gain; Decay = decay; Shimmer = shimmer; }
        }

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
                    data[(from + i) % n] += v * env * voice.Gain;
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

        static readonly Dictionary<string, AudioClip> _loops = new Dictionary<string, AudioClip>();
        static readonly Dictionary<string, AudioClip> _supplied = new Dictionary<string, AudioClip>();

        public static string StationLoopOf(string stationId)
        {
            switch (stationId)
            {
                case "am-tra": return "boil";
                case "gio-la": return "rustle";
                case "noi-mut": return "simmer";
                case "coi-hoa": return "thump";
                case "lo-banh": return "crackle";
                case "to-mat": return "drip";
                case "tra-sua": return "shake";
                case "binh-sao": return "tinkle";
                case "gian-nho": return "rustle";
                case "tiem-banh": return "crackle";
                case "suoi-hoa": return "stream";
                case "dai-sao": return "tinkle";
                case "ho-sen": return "stream";
                case "lo-keo": return "simmer";
                case "ruong-nep": return "rustle";
                case "thuyen-tra": return "boil";
                default: return null;
            }
        }

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

        public static void SupplyStationClip(string stationId, AudioClip clip)
        {
            if (clip == null) _supplied.Remove(stationId);
            else _supplied[stationId] = clip;
        }

        static AudioClip MakeStationLoop(string loop)
        {
            int seed = 0;
            foreach (char c in loop) seed = seed * 31 + c;
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
            Level(data, LoopLevel);
            AudioClip clip = AudioClip.Create("station-" + loop, data.Length, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        const float LoopLevel = 0.035f;

        static int Samples(float seconds) => (int)(Rate * seconds);

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

        static void LowPass(float[] buf, float cutoff)
        {
            double w = Math.Tan(Math.PI * cutoff / Rate), q = Math.Sqrt(2d);
            double norm = 1d / (1d + q * w + w * w);
            double b0 = w * w * norm, b1 = 2d * b0, a1 = 2d * (w * w - 1d) * norm, a2 = (1d - q * w + w * w) * norm;
            double x1 = 0d, x2 = 0d, y1 = 0d, y2 = 0d;
            for (int i = 0; i < buf.Length; i++)
            {
                double x = buf[i];
                double y = b0 * x + b1 * x1 + b0 * x2 - a1 * y1 - a2 * y2;
                x2 = x1; x1 = x; y2 = y1; y1 = y;
                buf[i] = (float)y;
            }
        }

        static void Ping(float[] buf, int at, float freq, float gain, float decay, float seconds)
        {
            int length = Mathf.Min(Samples(seconds), buf.Length - at);
            for (int i = 0; i < length; i++)
            {
                float t = i / (float)Rate;
                buf[at + i] += Mathf.Sin(2f * Mathf.PI * freq * t) * Mathf.Min(1f, t / 0.002f) * Mathf.Exp(-t * decay) * gain;
            }
        }

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

        const float ShakeTop = 4000f, RustleTop = 3500f, CrackleTop = 4500f, StreamTop = 5000f;

        static void DrawBoil(float[] buf, System.Random r)
        {
            Noise(buf, 0.5f, 0.05f, 0.004f, r, t => 0.8f + 0.2f * Mathf.Sin(t * 5.1f));
            int bubbles = (int)(buf.Length / (float)Rate * 16f);
            for (int k = 0; k < bubbles; k++)
                Bubble(buf, r.Next(buf.Length), 260f + (float)r.NextDouble() * 420f, 2.5f, 0.12f + (float)r.NextDouble() * 0.12f, 70f);
        }

        static void DrawSimmer(float[] buf, System.Random r)
        {
            Noise(buf, 0.25f, 0.02f, 0.003f, r);
            int bubbles = (int)(buf.Length / (float)Rate * 3.5f);
            for (int k = 0; k < bubbles; k++)
                Bubble(buf, r.Next(buf.Length), 110f + (float)r.NextDouble() * 120f, 1.2f, 0.35f + (float)r.NextDouble() * 0.2f, 22f);
        }

        static void DrawCrackle(float[] buf, System.Random r)
        {
            Noise(buf, 0.35f, 0.03f, 0.002f, r, t => 0.7f + 0.3f * Mathf.Sin(t * 1.3f) * Mathf.Sin(t * 0.7f));
            int cracks = (int)(buf.Length / (float)Rate * 11f);
            for (int k = 0; k < cracks; k++)
            {
                int at = r.Next(buf.Length);
                bool pop = r.NextDouble() < 0.12;
                Burst(buf, at, pop ? 0.02f : 0.004f + (float)r.NextDouble() * 0.006f, pop ? 0.9f : 0.3f + (float)r.NextDouble() * 0.4f, 0.6f, r);
                if (r.NextDouble() < 0.4) Burst(buf, Mathf.Min(buf.Length - 1, at + Samples(0.02f + (float)r.NextDouble() * 0.05f)), 0.004f, 0.25f, 0.6f, r);
            }
            LowPass(buf, CrackleTop);
        }

        static void DrawShake(float[] buf, System.Random r)
        {
            float period = 5f / 12f;
            for (int beat = 0; beat * period * Rate < buf.Length; beat++)
            {
                int start = Samples(beat * period + (beat % 2 == 1 ? 0.02f : 0f));
                for (int k = 0; k < 9; k++)
                {
                    int at = start + Samples((float)(r.NextDouble() * r.NextDouble()) * 0.12f);
                    if (at >= buf.Length) continue;
                    Burst(buf, at, 0.012f, 0.35f + (float)r.NextDouble() * 0.3f, 0.75f, r);
                    Ping(buf, at, 2400f + (float)r.NextDouble() * 1800f, 0.05f, 120f, 0.04f);
                }
                Burst(buf, Mathf.Min(buf.Length - 1, start), 0.09f, 0.12f, 0.15f, r);
            }
            LowPass(buf, ShakeTop);
        }

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
                Burst(buf, Mathf.Min(buf.Length - 1, at + Samples(0.05f)), 0.14f, 0.15f, 0.08f, r);
            }
        }

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

        static void DrawStream(float[] buf, System.Random r)
        {
            Noise(buf, 0.6f, 0.3f, 0.06f, r, t => 0.75f + 0.15f * Mathf.Sin(t * 2.3f) + 0.1f * Mathf.Sin(t * 5.7f));
            int bubbles = (int)(buf.Length / (float)Rate * 30f);
            for (int k = 0; k < bubbles; k++)
                Bubble(buf, r.Next(buf.Length), 700f + (float)r.NextDouble() * 1100f, 1.5f, 0.05f + (float)r.NextDouble() * 0.06f, 110f);
            LowPass(buf, StreamTop);
        }

        static void DrawRustle(float[] buf, System.Random r)
        {
            Noise(buf, 0.5f, 0.45f, 0.12f, r, t => Mathf.Pow(Mathf.Max(0f, Mathf.Sin(t * 1.9f) * 0.6f + Mathf.Sin(t * 0.8f + 1f) * 0.4f), 2f) + 0.1f);
            int crisps = (int)(buf.Length / (float)Rate * 8f);
            for (int k = 0; k < crisps; k++)
                Burst(buf, r.Next(buf.Length), 0.006f + (float)r.NextDouble() * 0.01f, 0.2f, 0.8f, r);
            LowPass(buf, RustleTop);
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
                float swell = 0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * t / seconds);
                float pad = Mathf.Sin(2f * Mathf.PI * 110f * t) * 0.05f + Mathf.Sin(2f * Mathf.PI * 164.75f * t) * 0.035f;
                return brown * 0.22f + pad * swell;
            });
            return _ambient;
        }
    }
}
