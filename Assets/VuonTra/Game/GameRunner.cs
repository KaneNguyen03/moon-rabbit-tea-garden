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

        /// <summary>The day number the daily things turn over on, for screens that show "today".</summary>
        public long TodayNumber => Today;

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
            _plotsSeen = State.Plots;
            _goldSeen = GoldRabbits();
            _ready = true;
            if (preset == null) SyncNow(SyncReason.Open);
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

            if (!Paused) State.PlaySeconds += dt;
            _sinceSync += dt;
            if (_sinceSync >= SyncTimerSeconds) SyncNow(SyncReason.Timer);
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

        /// <summary>True while the shop's gift for today is still to be had.</summary>
        public bool GiftWaiting => Shop.GiftWaiting(State, Today);

        /// <summary>Takes today's gift at the shop. How many golden carrots it brought; 0 when there was none to take.</summary>
        public int ClaimDailyGift()
        {
            int gift = Shop.ClaimDailyGift(State, Today);
            if (gift > 0) Changed();
            return gift;
        }

        /// <summary>Who is visiting the shop today.</summary>
        public Shop.Visitor VisitorToday => Shop.VisitorFor(State, Today);

        /// <summary>True while today's visitor has not been looked at yet.</summary>
        public bool VisitorUnseen => State.VisitorSeenDay != Today;

        /// <summary>The shop was opened: today's visitor has been seen.</summary>
        public void MarkVisitorSeen()
        {
            if (State.VisitorSeenDay == Today) return;
            State.VisitorSeenDay = Today;
            Changed();
        }

        /// <summary>Buys today's visitor; null when it cannot be bought.</summary>
        public Rabbit BuyVisitor()
        {
            Rabbit rabbit = Shop.BuyVisitor(State, Today, PickName());
            _shopBought = rabbit != null;
            if (rabbit != null) Changed();
            return rabbit;
        }

        /// <summary>Calls a rabbit with golden carrots at the shop's odds; null when there are not enough.</summary>
        public Rabbit SummonGoldenRabbit()
        {
            Rabbit rabbit = Shop.SummonGolden(State, _rng.NextDouble(), PickName(), _rng.NextDouble());
            _shopBought = rabbit != null;
            if (rabbit != null) Changed();
            return rabbit;
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

        /// <summary>
        /// The player touched or stroked a rabbit (#19): the bond and the tea power follow <see cref="Rabbits.Pet"/>.
        /// Saved only when something changed; a touch that pays nothing is not worth a write.
        /// </summary>
        public PetResult PetRabbit(long id, bool wake = false)
        {
            PetResult result = Rabbits.Pet(State, Rabbits.Find(State, id), Today, wake);
            if (result.FirstToday || result.Energy > 0d) Changed();
            return result;
        }

        /// <summary>Where the save could be backed up to. Local-only until a store sign-in is wired in (#38, #39, #40).</summary>
        public IAccountService Account { get; set; } = new LocalOnlyAccount();
        /// <summary>Where a copy of the save could be kept. Nowhere until the cloud save core is wired in (#38).</summary>
        ICloudSave _cloud = new LocalOnlyCloudSave();
        public ICloudSave Cloud
        {
            get => _cloud;
            set { _cloud = value ?? new LocalOnlyCloudSave(); _sync = null; }
        }

        CloudSync _sync;
        float _sinceSync;
        /// <summary>Besides opening, going to the background and milestones, the save goes up this often at most.</summary>
        const float SyncTimerSeconds = 180f;
        int _plotsSeen, _goldSeen;
        bool _shopBought;
        /// <summary>Two copies of the garden differ and the player has to choose: the screen shows them.</summary>
        public event Action<SyncReport> SyncChoiceNeeded;
        /// <summary>Every sync attempt that finished, for the settings screen.</summary>
        public event Action<SyncReport> Synced;

        /// <summary>The sync core over whatever cloud is wired in; built again when the cloud changes.</summary>
        public CloudSync Sync => _sync ?? (_sync = new CloudSync(Cloud, () => Now));

        public CloudStatus CloudStatus => new CloudStatus
        {
            Available = Cloud.Available, LastPushedAt = Sync.LastPushedAt, Last = Sync.Last,
            HasLocalBackup = _store != null && _store.HasBackup, LocalBackupAt = _store != null ? _store.BackupUnixSeconds : -1
        };

        string LocalSaveText()
        {
            State.LastSeenUnixSeconds = Now;
            return SaveCodec.Encode(Defs, State, UpgradeDefs);
        }

        /// <summary>The cloud's copy becomes the garden: the device's copy is backed up first, and a copy that will not load changes nothing.</summary>
        bool ReplaceWithCloud(string json)
        {
            LoadResult loaded = SaveCodec.Decode(Defs, json, UpgradeDefs);
            if (loaded.Status != LoadStatus.Ok) return false;
            if (_store != null) _store.Backup();
            Adopt(loaded.State);
            return true;
        }

        void Adopt(GameState state)
        {
            State = state;
            if (State.UpgradeLevels == null || State.UpgradeLevels.Length != UpgradeDefs.Count)
                Array.Resize(ref State.UpgradeLevels, UpgradeDefs.Count);
            Mods = Modifiers.From(UpgradeDefs, State);
            Quests.RollDaily(QuestDefs, Defs, State, Today, Mods);
            _plotsSeen = State.Plots;
            _goldSeen = GoldRabbits();
            StateChanged?.Invoke();
            Save();
        }

        int GoldRabbits()
        {
            int n = 0;
            foreach (Rabbit r in State.Rabbits) if (r.Tier >= 4) n++;
            return n;
        }

        /// <summary>One sync attempt now, for the reason given. A choice that comes out of it is raised as an event.</summary>
        public void SyncNow(SyncReason reason, Action<SyncReport> onDone = null)
        {
            _sinceSync = 0f;
            Sync.Sync(reason, LocalSaveText(), SaveSummary.Of(State), r =>
            {
                if (r.Outcome == SyncOutcome.NeedsChoice) SyncChoiceNeeded?.Invoke(r);
                Synced?.Invoke(r);
                onDone?.Invoke(r);
            });
        }

        /// <summary>The player chose between the two copies.</summary>
        public void ResolveSync(bool takeRemote, Action<SyncReport> onDone = null)
        {
            Sync.Resolve(takeRemote, LocalSaveText(), ReplaceWithCloud, r => { Synced?.Invoke(r); onDone?.Invoke(r); });
        }

        /// <summary>"Save now".</summary>
        public void PushToCloud(Action<SyncReport> onDone = null)
        {
            Sync.PushNow(LocalSaveText(), SaveSummary.Of(State), r => { Synced?.Invoke(r); onDone?.Invoke(r); });
        }

        /// <summary>"Restore from the cloud": the device's copy is backed up, then replaced.</summary>
        public void RestoreFromCloud(Action<SyncReport> onDone = null)
        {
            Sync.Restore(SaveSummary.Of(State), ReplaceWithCloud, r => { Synced?.Invoke(r); onDone?.Invoke(r); });
        }

        /// <summary>Brings the backed-up copy on the device back; the copy it replaces becomes the backup, so nothing is lost either way.</summary>
        public bool RestoreLocalBackup()
        {
            if (_store == null || !_store.HasBackup) return false;
            string backup = _store.ReadBackup();
            LoadResult loaded = SaveCodec.Decode(Defs, backup, UpgradeDefs);
            if (loaded.Status != LoadStatus.Ok) return false;
            string current = _store.ReadRaw();
            Adopt(loaded.State);
            if (current != null) _store.WriteBackup(current);
            return true;
        }

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

        /// <summary>Spends one of today's mooncake plays and hands back the game to score; null when none is left.</summary>
        public PoundingRun StartPounding()
        {
            if (!Pounding.Start(State, Today)) return null;
            Changed();
            return new PoundingRun();
        }

        /// <summary>Pays a finished mooncake game.</summary>
        public PoundingResult FinishPounding(PoundingRun run)
        {
            PoundingResult result = Pounding.Finish(Defs, State, Mods, run);
            Changed();
            return result;
        }

        /// <summary>Spends one of today's leaf-catching plays and hands back the game, seeded by the day and the games so far; null when none is left.</summary>
        public LeavesRun StartLeaves()
        {
            if (!Leaves.Start(State, Today)) return null;
            Changed();
            return LeavesRun.FromSeed(Leaves.SeedFor(Today, State.Leaves.Games));
        }

        /// <summary>Pays a finished leaf-catching game.</summary>
        public LeavesResult FinishLeaves(LeavesRun run)
        {
            LeavesResult result = Leaves.Finish(Defs, State, Mods, run);
            Changed();
            return result;
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
            // The big moments go up at once: more land, a gold-or-rarer rabbit, something bought at the shop.
            int gold = GoldRabbits();
            bool milestone = State.Plots > _plotsSeen || gold > _goldSeen || _shopBought;
            _plotsSeen = State.Plots;
            _goldSeen = gold;
            _shopBought = false;
            if (milestone && _ready) SyncNow(SyncReason.Milestone);
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
            if (paused && _ready)
            {
                Save();
                SyncNow(SyncReason.Background);
            }
        }

        void OnApplicationQuit()
        {
            if (_ready) Save();
        }
    }
}
