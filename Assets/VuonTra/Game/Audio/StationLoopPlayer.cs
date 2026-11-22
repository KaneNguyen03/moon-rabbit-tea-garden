using UnityEngine;

namespace VuonTra.Game
{
    /// <summary>
    /// The station heard up close (#63): two sources so one loop can fade out under the next. A new loop
    /// rises over <see cref="FadeSeconds"/>, the one it replaces falls over the same half second, and a third
    /// change before that is over cuts the quietest, so two loops never sound together for longer.
    /// Volume follows <see cref="Sfx.StationGain"/> every frame.
    /// </summary>
    public sealed class StationLoopPlayer
    {
        public const float FadeSeconds = 0.5f;
        public const float BoostPitch = 1.1f;

        sealed class Voice
        {
            public AudioSource Source;
            public string Id;
            public float Level;
        }

        Voice _current, _outgoing;
        float _overlap;

        public StationLoopPlayer(GameObject host)
        {
            _current = new Voice { Source = MakeSource(host) };
            _outgoing = new Voice { Source = MakeSource(host) };
        }

        static AudioSource MakeSource(GameObject host)
        {
            var source = host.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.loop = true;
            source.volume = 0f;
            return source;
        }

        /// <summary>The station being faded in or held, or null when nothing is wanted.</summary>
        public string StationId => _current.Id;
        /// <summary>The station fading out under it, or null.</summary>
        public string OutgoingId => _outgoing.Id;
        public AudioClip Clip => _current.Source != null ? _current.Source.clip : null;
        public float Volume => _current.Source != null ? _current.Source.volume : 0f;
        public float Pitch => _current.Source != null ? _current.Source.pitch : 1f;
        /// <summary>Something is heard or rising: a station is wanted, or the last one has not finished fading.</summary>
        public bool Playing => _current.Id != null;
        /// <summary>How many loops are sounding right now: 0, 1 or 2.</summary>
        public int Audible => (_current.Id != null && _current.Level > 0f ? 1 : 0) + (_outgoing.Id != null && _outgoing.Level > 0f ? 1 : 0);
        /// <summary>The longest any two loops have sounded together, in seconds.</summary>
        public float LongestOverlap { get; private set; }

        /// <summary>One frame: <paramref name="stationId"/> is the station to hear (null for none).</summary>
        public void Update(string stationId, bool boosted, float dt)
        {
            if (_current.Source == null) return; // the host is gone
            if (stationId != null && Sfx.StationClip(stationId) == null) stationId = null; // a station with no loop is quiet
            if (stationId != _current.Id) Switch(stationId);

            float rate = dt / FadeSeconds;
            if (_current.Id != null) _current.Level = Mathf.Min(1f, _current.Level + rate);
            if (_outgoing.Id != null)
            {
                _outgoing.Level -= rate;
                if (_outgoing.Level <= 1e-4f) Silence(_outgoing);
            }

            if (_current.Id != null && _outgoing.Id != null)
            {
                _overlap += dt;
                LongestOverlap = Mathf.Max(LongestOverlap, _overlap);
            }

            float gain = Sfx.StationGain;
            _current.Source.volume = _current.Level * gain;
            _current.Source.pitch = boosted ? BoostPitch : 1f;
            if (_outgoing.Id != null) _outgoing.Source.volume = _outgoing.Level * gain;
        }

        void Switch(string stationId)
        {
            if (stationId != null && stationId == _outgoing.Id)
            {
                // Back to the one still fading out: it rises again from where it is.
                Voice back = _outgoing;
                _outgoing = _current;
                _current = back;
            }
            else
            {
                Silence(_outgoing); // a third change mid-fade: the older one goes at once
                Voice free = _outgoing;
                _outgoing = _current;
                _current = free;
                if (stationId != null) Start(_current, stationId);
            }
            _overlap = 0f;
            if (_outgoing.Id == null || _outgoing.Level <= 0f) Silence(_outgoing);
        }

        void Start(Voice voice, string stationId)
        {
            AudioClip clip = Sfx.StationClip(stationId);
            if (clip == null) return;
            voice.Id = stationId;
            voice.Level = 0f;
            voice.Source.clip = clip;
            voice.Source.volume = 0f;
            voice.Source.time = 0f;
            if (Application.isPlaying) voice.Source.Play();
        }

        static void Silence(Voice voice)
        {
            voice.Id = null;
            voice.Level = 0f;
            if (voice.Source == null) return;
            voice.Source.volume = 0f;
            if (voice.Source.isPlaying) voice.Source.Stop();
        }
    }
}
