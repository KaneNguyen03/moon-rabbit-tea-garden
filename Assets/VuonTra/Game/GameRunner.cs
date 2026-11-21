using System;
using System.Collections.Generic;
using UnityEngine;
using VuonTra.Core;

namespace VuonTra.Game
{
    /// <summary>Owns the game state: loads it, moves time forward, saves it, and tells the UI what happened.</summary>
    public sealed class GameRunner : MonoBehaviour
    {
        public GameCatalog Catalog;

        const float AutosaveSeconds = 5f;
        const float LongFrameSeconds = 2f;

        public GameState State { get; private set; }
        public IReadOnlyList<StationDef> Defs { get; private set; }

        public event Action StateChanged;
        public event Action<int, BigNum> Payout;
        /// <summary>Amount earned while away and the seconds that counted.</summary>
        public event Action<BigNum, double> AwayClaimed;

        SaveStore _store;
        float _sinceSave;
        bool _ready;
        BigNum _pendingAway;
        double _pendingAwaySeconds;

        static long Now => DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        void Awake()
        {
            if (!_ready) Init();
        }

        /// <summary>Loads the save, or uses <paramref name="preset"/> without touching disk (previews, tests).</summary>
        public void Init(GameState preset = null)
        {
            Defs = Catalog.BuildDefs();
            if (preset != null)
            {
                State = preset;
            }
            else
            {
                _store = new SaveStore();
                LoadResult loaded = _store.Load(Defs);
                State = loaded.State;
                if (loaded.Status == LoadStatus.Ok)
                {
                    _pendingAway = Simulation.ClaimAway(Defs, State, Now, out _pendingAwaySeconds);
                }
                else
                {
                    State.LastSeenUnixSeconds = Now;
                }
            }
            _ready = true;
        }

        void Start()
        {
            if (_pendingAway >= BigNum.One) AwayClaimed?.Invoke(_pendingAway, _pendingAwaySeconds);
            _pendingAway = BigNum.Zero;
        }

        /// <summary>While true, no time passes for the stations (the pause screen is open).</summary>
        public bool Paused;

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            if (Paused)
            {
                // Paused time is not time away: keep the stamp fresh so it never gets paid out.
                State.LastSeenUnixSeconds = Now;
            }
            else if (dt > LongFrameSeconds)
            {
                // The app was suspended: pay the gap as time away instead of simulating it.
                BigNum paid = Simulation.ClaimAway(Defs, State, Now, out double counted);
                if (paid >= BigNum.One) AwayClaimed?.Invoke(paid, counted);
            }
            else
            {
                Simulation.Tick(Defs, State, dt, Payout);
            }

            _sinceSave += dt;
            if (_sinceSave >= AutosaveSeconds) Save();
        }

        public bool Buy(int index)
        {
            bool ok = Simulation.TryBuy(Defs, State, index);
            if (ok)
            {
                StateChanged?.Invoke();
                Save();
            }
            return ok;
        }

        public bool Tap(int index)
        {
            if (!State.HintSeen) State.HintSeen = true;
            return Simulation.Tap(Defs, State, index);
        }

        public void SetBuyMode(int mode)
        {
            State.BuyMode = mode == 0 ? 0 : (mode == 10 ? 10 : 1);
            StateChanged?.Invoke();
        }

        /// <summary>Throws the progress away and starts a new garden.</summary>
        public void ResetGame()
        {
            int buyMode = State.BuyMode;
            State = GameState.NewGame(Defs);
            State.BuyMode = buyMode;
            State.HintSeen = true;
            State.LastSeenUnixSeconds = Now;
            StateChanged?.Invoke();
            Save();
        }

        public void Save()
        {
            _sinceSave = 0f;
            if (_store == null) return;
            State.LastSeenUnixSeconds = Now;
            _store.Save(Defs, State);
        }

        void OnApplicationPause(bool paused)
        {
            if (paused && _ready) Save();
        }

        void OnApplicationQuit()
        {
            if (_ready) Save();
        }
    }
}
