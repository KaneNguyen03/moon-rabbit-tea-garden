using System;
using UnityEngine;

namespace VuonTra.Game
{
    /// <summary>
    /// Every sound in the game, synthesized in code so nothing has to be downloaded. Volumes go
    /// through two gains (music, effects) that the settings sliders own.
    /// </summary>
    public static class Sfx
    {
        const int Rate = 44100;
        const string MusicKey = "vol.music", SfxKey = "vol.sfx";
        const float BedLevel = 0.5f;

        public static float MusicGain { get; private set; } = 0.6f;
        public static float SfxGain { get; private set; } = 0.8f;

        static AudioSource _oneShots, _bed;
        static AudioClip _blip, _click, _sting, _tick, _ambient;
        static float _lastTick;

        /// <summary>Adds the two audio sources to <paramref name="host"/> and starts the ambient bed.</summary>
        public static void Attach(GameObject host)
        {
            MusicGain = PlayerPrefs.GetFloat(MusicKey, 0.6f);
            SfxGain = PlayerPrefs.GetFloat(SfxKey, 0.8f);

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
        }

        public static void SetMusic(float gain)
        {
            MusicGain = Mathf.Clamp01(gain);
            PlayerPrefs.SetFloat(MusicKey, MusicGain);
            if (_bed != null) _bed.volume = BedLevel * MusicGain;
        }

        public static void SetSfx(float gain)
        {
            SfxGain = Mathf.Clamp01(gain);
            PlayerPrefs.SetFloat(SfxKey, SfxGain);
        }

        /// <summary>A tap or purchase landed.</summary>
        public static void Blip() => Play(_blip ?? (_blip = MakeBlip()), 0.55f, 0.08f);
        public static void Click() => Play(_click ?? (_click = MakeClick()), 0.5f, 0.05f);
        /// <summary>Something worth celebrating: a new station, a milestone.</summary>
        public static void Sting() => Play(_sting ?? (_sting = MakeSting()), 0.6f, 0f);

        /// <summary>A station paid out. Quiet and rate-limited so a busy garden never chatters.</summary>
        public static void Tick()
        {
            if (Time.unscaledTime - _lastTick < 0.35f) return;
            _lastTick = Time.unscaledTime;
            Play(_tick ?? (_tick = MakeTick()), 0.22f, 0.1f);
        }

        static void Play(AudioClip clip, float volume, float pitchSpread)
        {
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
