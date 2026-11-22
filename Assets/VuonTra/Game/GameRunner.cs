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
        /// <summary>Where the save file lives; null is the player's own save folder. Tests point it at a folder of their own.</summary>
        public string SaveDirectory;

        const float AutosaveSeconds = 5f;
        const float LongFrameSeconds = 2f;

        public GameState State { get; private set; }
        public IReadOnlyList<StationDef> Defs { get; private set; }
        public IReadOnlyList<UpgradeDef> UpgradeDefs { get; private set; }
        public IReadOnlyList<QuestDef> QuestDefs { get; private set; }
        public IReadOnlyList<ItemDef> ItemDefs { get; private set; }
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
            ItemDefs = Catalog.BuildItems();

            if (preset != null)
            {
                State = preset;
                if (State.UpgradeLevels == null || State.UpgradeLevels.Length != UpgradeDefs.Count)
                    Array.Resize(ref State.UpgradeLevels, UpgradeDefs.Count);
                Mods = Modifiers.From(UpgradeDefs, State);
            }
            else
            {
                _store = new SaveStore(SaveDirectory);
                LoadResult loaded = _store.Load(Defs, UpgradeDefs);
                State = loaded.State;
                if (loaded.Status == LoadStatus.Ok)
                {
                    Mods = Modifiers.From(UpgradeDefs, State);
                    _pendingAway = Simulation.ClaimAway(Defs, State, Now, out _pendingAwaySeconds, Mods);
                }
                else
                {
                    // A garden that starts from nothing starts with one rabbit; a save we could load keeps what it had.
                    Rabbits.GiftFirst(State, PickName());
                    State.LastSeenUnixSeconds = Now;
                }
                Mods = Modifiers.From(UpgradeDefs, State);
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
                Simulation.Tick(Defs, State, dt, Payout, Mods, _rng.NextDouble, ProductMade);
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
            bool ok = Simulation.Tap(Defs, State, index, Mods);
            // Taps do not save or refresh by themselves; the guidance's first step is the one thing that watches them.
            if (ok && !Tutorial.IsDone(State) && Tutorial.Advance(State) > 0) StateChanged?.Invoke();
            return ok;
        }

        /// <summary>The player skips the first minute's guidance.</summary>
        public void SkipTutorial()
        {
            Tutorial.Skip(State);
            Changed();
        }

        /// <summary>The call step's clover, once, when the purse has none.</summary>
        public bool GrantTutorialClover()
        {
            bool given = Tutorial.GrantCallClover(State);
            if (given) Changed();
            return given;
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
            QuestDef quest = QuestDefs[index];
            LastQuestItem = null;
            bool ok = Quests.TryClaim(quest, Defs, State, Mods, out paid);
            if (!ok) return false;
            // Daily quests and the bigger achievements also hand out something to wear.
            if ((quest.Daily || quest.RewardClovers >= 2) && ItemDefs.Count > 0)
                LastQuestItem = Items.Grant(State, ItemDefs[_rng.Next(ItemDefs.Count)], Rabbits.RollTier(_rng.NextDouble()));
            Changed();
            return true;
        }

        readonly System.Random _rng = new System.Random();

        /// <summary>The item the most recent quest claim gave, if any.</summary>
        public Item LastQuestItem { get; private set; }

        /// <summary>A station finished a special batch for the tray: the station's index.</summary>
        public event Action<int> ProductMade;

        /// <summary>Sells one slot of the tray. The carrots paid, and any clovers a golden stack brought.</summary>
        public BigNum SellProduct(int slot, out int clovers)
        {
            BigNum paid = Tray.Sell(Defs, State, slot, Mods, out clovers);
            if (paid > BigNum.Zero || clovers > 0) Changed();
            return paid;
        }

        /// <summary>Sells everything in the tray.</summary>
        public BigNum SellAllProducts(out int clovers)
        {
            BigNum paid = Tray.SellAll(Defs, State, Mods, out clovers);
            if (paid > BigNum.Zero || clovers > 0) Changed();
            return paid;
        }

        /// <summary>A photo of the garden was taken. True when it brought today's gift.</summary>
        public bool TakePhoto(string path = null)
        {
            bool gift = Photos.Take(State, Today, path);
            Changed();
            return gift;
        }

        /// <summary>True while today's photo gift is still to be had.</summary>
        public bool PhotoGiftWaiting => Photos.GiftWaiting(State, Today);

        /// <summary>A gift of carrots (a caught firefly, a visitor). Counts as earned.</summary>
        public void Grant(BigNum carrots)
        {
            State.Coins += carrots;
            State.TotalEarned += carrots;
            Changed();
        }

        /// <summary>Tops the tea power bar up a little, never past full.</summary>
        public void AddEnergy(double amount)
        {
            State.Energy = Math.Min(Energy.Max, State.Energy + amount);
            StateChanged?.Invoke();
        }

        /// <summary>Spends "trà lực" on a burst of double speed.</summary>
        public bool Boost()
        {
            bool ok = Energy.TryBoost(State);
            if (ok) Changed();
            return ok;
        }

        public bool LevelUpRabbit(long id)
        {
            bool ok = Rabbits.TryLevelUp(State, id);
            if (ok) Changed();
            return ok;
        }

        public bool RenameRabbit(long id, string name)
        {
            bool ok = Rabbits.Rename(State, id, name);
            if (ok) Changed();
            return ok;
        }

        /// <summary>Where the save could be backed up to. Local-only until a store sign-in is wired in (#38, #39, #40).</summary>
        public IAccountService Account { get; set; } = new LocalOnlyAccount();
        /// <summary>Where a copy of the save could be kept. Nowhere until the cloud save core is wired in (#38).</summary>
        public ICloudSave Cloud { get; set; } = new LocalOnlyCloudSave();

        /// <summary>
        /// Asks the account service to link. When it does, the garden's one-time gift is paid and
        /// <paramref name="onDone"/> hears whether the link worked and whether the gift came with it.
        /// </summary>
        public void LinkAccount(Action<bool, bool> onDone = null)
        {
            Account.Link(ok =>
            {
                bool gift = ok && Profile.OnLinked(State);
                if (ok) Changed();
                onDone?.Invoke(ok, gift);
            });
        }

        public bool RenameGarden(string name)
        {
            bool ok = Profile.Rename(State, name);
            if (ok) Changed();
            return ok;
        }

        /// <summary>A kind's page in the album was opened: its "new" mark comes off and stays off.</summary>
        public void LookAtKind(string kindId)
        {
            if (Rabbits.LookAt(State, kindId)) Changed();
        }

        /// <summary>Makes the next rabbit the player's avatar; null when there are none.</summary>
        public Rabbit NextAvatar()
        {
            Rabbit next = Profile.NextAvatar(State);
            if (next != null) Changed();
            return next;
        }

        public bool EquipItem(long itemId, long rabbitId)
        {
            bool ok = Items.Equip(State, itemId, rabbitId);
            if (ok) Changed();
            return ok;
        }

        public bool UnequipItem(long itemId)
        {
            bool ok = Items.Unequip(State, itemId);
            if (ok) Changed();
            return ok;
        }

        /// <summary>Calls one new rabbit for clovers; null when there are not enough.</summary>
        public Rabbit SummonRabbit()
        {
            Rabbit rabbit = Rabbits.Summon(State, _rng.NextDouble(), PickName(), _rng.NextDouble());
            if (rabbit != null) Changed();
            return rabbit;
        }

        /// <summary>A given name for a rabbit that has just arrived, from the catalog's list.</summary>
        string PickName()
        {
            string[] names = Catalog.RabbitNames;
            return names != null && names.Length > 0 ? names[_rng.Next(names.Length)] : L10n.T("rabbit.default");
        }

        public Rabbit MergeRabbits(int tier)
        {
            Rabbit merged = Rabbits.Merge(State, tier, _rng.NextDouble());
            if (merged != null) Changed();
            return merged;
        }

        /// <summary>A resting rabbit goes to the station where it helps most; a working one rests.</summary>
        /// <summary>Sends a rabbit to a station (whoever was there rests), or to rest with -1.</summary>
        public bool AssignRabbit(long id, int station)
        {
            bool ok = Rabbits.Assign(State, id, station);
            if (ok) Changed();
            return ok;
        }

        public bool ToggleRabbitWork(long id)
        {
            Rabbit rabbit = Rabbits.Find(State, id);
            if (rabbit == null) return false;
            int station = rabbit.Station >= 0 ? -1 : Rabbits.BestStationFor(Defs, State, rabbit);
            if (rabbit.Station < 0 && station < 0) return false;
            bool ok = Rabbits.Assign(State, id, station);
            if (ok) Changed();
            return ok;
        }

        public void AutoAssignRabbits()
        {
            Rabbits.AutoAssign(Defs, State);
            Changed();
        }

        public void SetBuyMode(int mode)
        {
            State.BuyMode = mode == Simulation.BuyMax || mode == Simulation.BuyTen || mode == Simulation.BuyToMilestone ? mode : Simulation.BuyOne;
            StateChanged?.Invoke();
        }

        /// <summary>How much of the purse a quick upgrade may spend: 1, 0.5 or 0.25.</summary>
        public void SetSpendShare(double share)
        {
            State.SpendShare = share == 0.5d || share == 0.25d ? share : 1d;
            StateChanged?.Invoke();
        }

        /// <summary>Upgrades every open station as far as the budget goes, where the carrots earn most. Null when nothing could be bought.</summary>
        public List<UpgradeStep> UpgradeAll()
        {
            List<UpgradeStep> plan = UpgradePlan.Plan(Defs, State, Simulation.Budget(State));
            if (!UpgradePlan.Apply(Defs, State, plan)) return null;
            Changed();
            return plan;
        }

        /// <summary>Levels a rabbit up as far as the budget goes, never past its cap. Returns how many levels.</summary>
        public int LevelUpRabbitMax(long id)
        {
            int n = Rabbits.LevelUpMax(State, id, Simulation.Budget(State));
            if (n > 0) Changed();
            return n;
        }

        /// <summary>Throws the progress away and starts a new garden.</summary>
        public void ResetGame()
        {
            int buyMode = State.BuyMode;
            State = GameState.NewGame(Defs, UpgradeDefs.Count);
            Rabbits.GiftFirst(State, PickName());
            State.BuyMode = buyMode;
            State.HintSeen = true;
            State.LastSeenUnixSeconds = Now;
            Mods = Modifiers.From(UpgradeDefs, State);
            Quests.RollDaily(QuestDefs, Defs, State, Today, Mods);
            Changed();
        }

        /// <summary>Buys the next garden plot, which lets the next station be opened.</summary>
        public bool BuyPlot()
        {
            bool ok = Plots.TryBuy(Defs, State);
            if (ok) Changed();
            return ok;
        }

        void Changed()
        {
            if (!Tutorial.IsDone(State)) Tutorial.Advance(State);
            // Upgrades, working rabbits and their items all feed the modifiers.
            Mods = Modifiers.From(UpgradeDefs, State);
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
