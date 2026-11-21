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
        public IReadOnlyList<UpgradeDef> UpgradeDefs { get; private set; }
        public IReadOnlyList<QuestDef> QuestDefs { get; private set; }
        /// <summary>What the owned upgrades add up to; refreshed whenever one is bought.</summary>
        public Modifiers Mods { get; private set; } = Modifiers.None;

        /// <summary>While true, no time passes for the stations (the pause screen is open).</summary>
        public bool Paused;

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

        /// <summary>The player's local calendar day as a number, so daily quests turn over at their midnight.</summary>
        static long Today
        {
            get
            {
                DateTimeOffset local = DateTimeOffset.Now;
                return (long)Math.Floor((local.ToUnixTimeSeconds() + local.Offset.TotalSeconds) / 86400d);
            }
        }

        void Awake()
        {
            if (!_ready) Init();
        }

        /// <summary>Loads the save, or uses <paramref name="preset"/> without touching disk (previews, tests).</summary>
        public void Init(GameState preset = null)
        {
            Defs = Catalog.BuildDefs();
            UpgradeDefs = Catalog.BuildUpgrades();
            QuestDefs = Catalog.BuildQuests();

            if (preset != null)
            {
                State = preset;
                if (State.UpgradeLevels == null || State.UpgradeLevels.Length != UpgradeDefs.Count)
                    Array.Resize(ref State.UpgradeLevels, UpgradeDefs.Count);
                Mods = Modifiers.From(UpgradeDefs, State);
            }
            else
            {
                _store = new SaveStore();
                LoadResult loaded = _store.Load(Defs, UpgradeDefs);
                State = loaded.State;
                Mods = Modifiers.From(UpgradeDefs, State);
                if (loaded.Status == LoadStatus.Ok)
                {
                    _pendingAway = Simulation.ClaimAway(Defs, State, Now, out _pendingAwaySeconds, Mods);
                }
                else
                {
                    State.LastSeenUnixSeconds = Now;
                }
            }

            Quests.RollDaily(QuestDefs, Defs, State, Today, Mods);
            _ready = true;
        }

        void Start()
        {
            if (_pendingAway >= BigNum.One) AwayClaimed?.Invoke(_pendingAway, _pendingAwaySeconds);
            _pendingAway = BigNum.Zero;
        }

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
                BigNum paid = Simulation.ClaimAway(Defs, State, Now, out double counted, Mods);
                if (paid >= BigNum.One) AwayClaimed?.Invoke(paid, counted);
            }
            else
            {
                Simulation.Tick(Defs, State, dt, Payout, Mods);
            }

            _sinceSave += dt;
            if (_sinceSave >= AutosaveSeconds)
            {
                if (Quests.RollDaily(QuestDefs, Defs, State, Today, Mods)) StateChanged?.Invoke();
                Save();
            }
        }

        public bool Buy(int index)
        {
            bool ok = Simulation.TryBuy(Defs, State, index);
            if (ok) Changed();
            return ok;
        }

        public bool Tap(int index)
        {
            if (!State.HintSeen) State.HintSeen = true;
            return Simulation.Tap(Defs, State, index, Mods);
        }

        public bool BuyUpgrade(int index)
        {
            bool ok = Upgrades.TryBuy(UpgradeDefs, State, index);
            if (ok)
            {
                Mods = Modifiers.From(UpgradeDefs, State);
                Changed();
            }
            return ok;
        }

        /// <summary>Pays out a finished quest. <paramref name="paid"/> is the carrots it gave.</summary>
        public bool ClaimQuest(int index, out BigNum paid)
        {
            paid = BigNum.Zero;
            if (index < 0 || index >= QuestDefs.Count) return false;
            bool ok = Quests.TryClaim(QuestDefs[index], Defs, State, Mods, out paid);
            if (ok) Changed();
            return ok;
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
            State = GameState.NewGame(Defs, UpgradeDefs.Count);
            State.BuyMode = buyMode;
            State.HintSeen = true;
            State.LastSeenUnixSeconds = Now;
            Mods = Modifiers.From(UpgradeDefs, State);
            Quests.RollDaily(QuestDefs, Defs, State, Today, Mods);
            Changed();
        }

        void Changed()
        {
            StateChanged?.Invoke();
            Save();
        }

        public void Save()
        {
            _sinceSave = 0f;
            if (_store == null) return;
            State.LastSeenUnixSeconds = Now;
            _store.Save(Defs, State, UpgradeDefs);
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
