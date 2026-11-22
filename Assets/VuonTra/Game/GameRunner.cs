using System;
using System.Collections.Generic;
using UnityEngine;
using VuonTra.Core;
using VuonTra.Game.Platform;

namespace VuonTra.Game
{
    /// <summary>Owns the game state: loads it, moves time forward, saves it, and tells the UI what happened.</summary>
    public sealed class GameRunner : MonoBehaviour
    {
        public GameCatalog Catalog;
        /// <summary>Where the save file lives; null is the player's own save folder. Tests point it at a folder of their own.</summary>
        /// <summary>Where the save lives; leave empty for the platform's data folder (the scene stores "" here, #70).</summary>
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
        /// <summary>What the garden did while the player was away (#69): carrots, the tray, guests, the sleepiest rabbit.</summary>
        public event Action<AwayReport> AwayClaimed;
        /// <summary>A season's last-day gift brought the season's rabbit (#8).</summary>
        public event Action<Rabbit> SeasonArrived;
        /// <summary>The first day the game sees of a season, once a year: the screen greets it.</summary>
        public event Action<Season> SeasonBegan;
        /// <summary>A set of the album was just completed (#61): the screen raises its banner.</summary>
        public event Action<AlbumSet> AlbumSetCompleted;
        /// <summary>
        /// An album milestone with a gift of its own was just reached (#61): 40 kinds (the hat) or 72 (the frame),
        /// with the golden carrots given instead of a hat the player already had (0 otherwise).
        /// </summary>
        public event Action<int, int> AlbumMilestoneReached;

        /// <summary>Greets the running season if it has not been greeted this year; true when it was greeted now.</summary>
        public bool GreetSeason()
        {
            if (!Seasons.ShouldGreet(State, TodayNumber)) return false;
            SeasonBegan?.Invoke(SeasonNow);
            Save();
            return true;
        }

        SaveStore _store;
        float _sinceSave;
        bool _ready;
        AwayReport _pendingAway;
        /// <summary>The latest time away and whether it has been doubled yet: the sheet's ×2 button acts on it once.</summary>
        public AwayReport LastAway { get; private set; }
        public bool LastAwayDoubled { get; private set; }

        static long Now => DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        /// <summary>The day number the daily things turn over on, for screens that show "today".</summary>
        /// <summary>The day the game goes by: the local calendar day, never earlier than the latest day this save has seen (#8).</summary>
        public long TodayNumber => Seasons.Clamp(State, Today);
        public Season SeasonNow => Seasons.SeasonOf(TodayNumber);

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
#if UNITY_PURCHASING && (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
            // A phone build talks to its store (#9); the editor, the tests and the Mac build keep the store in memory.
            Store = new VuonTra.Game.Store.UnityStorefront();
#endif
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
                // Purchases the store delivered before the save was here.
                foreach (PurchaseResult r in _deliveriesWaiting) Storefront.Grant(State, r.PackId, r.TransactionId);
                _deliveriesWaiting.Clear();
            }
            else
            {
                _store = new SaveStore(SaveDirectory);
                LoadResult loaded = _store.Load(Defs, UpgradeDefs);
                State = loaded.State;
                if (loaded.Status == LoadStatus.Ok)
                {
                    Mods = Modifiers.From(UpgradeDefs, State);
                    _pendingAway = Away.Claim(Defs, State, Now, Mods);
                }
                else
                {
                    // A garden that starts from nothing starts with one rabbit; a save we could load keeps what it had.
                    Rabbits.GiftFirst(State, PickName());
                    State.LastSeenUnixSeconds = Now;
                    // A save that could not be read: a make-up letter waits in the mailbox (#68).
                    if (loaded.Status == LoadStatus.Corrupt) Mailbox.Compensate(State, TodayNumber);
                }
                Mods = Modifiers.From(UpgradeDefs, State);
            }

            Quests.RollDaily(QuestDefs, Defs, State, TodayNumber, Mods);
            LoadGiftKey();
            CheckMail();
            _plotsSeen = State.Plots;
            _goldSeen = GoldRabbits();
            NoteAlbum(false);
            _ready = true;
            if (preset == null)
            {
                // The real game on a real device: the platform's sign-in and cloud go in before the first sync
                // (Game Center and iCloud on iOS, #39; Play Games and Saved Games on Android, #40; nothing elsewhere).
                PlatformServices.Install(this);
                Platform.AndroidServices.Install(this);
                // Reminders left for the time away are not needed once the game is open (#65).
                Notifications.CameBack();
                SyncNow(SyncReason.Open);
            }
        }

        void Start()
        {
            if (_pendingAway.Paid >= BigNum.One) TellAway(_pendingAway);
            _pendingAway = default;
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
                AwayReport away = Away.Claim(Defs, State, Now, Mods);
                if (away.Paid >= BigNum.One) TellAway(away);
            }
            else
            {
                Simulation.Tick(Defs, State, dt, Payout, Mods, _rng.NextDouble, ProductMade);
            }

            if (!Paused) State.PlaySeconds += dt;
            _sinceSync += dt;
            if (_sinceSync >= SyncTimerSeconds) SyncNow(SyncReason.Timer);
            PlatformServices.Tick(this, dt);
            _sinceSave += dt;
            if (_sinceSave >= AutosaveSeconds)
            {
                if (Quests.RollDaily(QuestDefs, Defs, State, TodayNumber, Mods)) StateChanged?.Invoke();
                if (CheckMail()) StateChanged?.Invoke();
                Save();
                GreetSeason();
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
            // Daily quests and the bigger achievements also hand out something to wear: one of the quest items,
            // never a hat the shop sells (#44: those are bought, one of each).
            if (quest.Daily || quest.RewardClovers >= 2)
            {
                var gifts = new System.Collections.Generic.List<ItemDef>();
                foreach (ItemDef def in ItemDefs) if (def.Price <= 0) gifts.Add(def);
                if (gifts.Count > 0) LastQuestItem = Items.Grant(State, gifts[_rng.Next(gifts.Count)], Rabbits.RollTier(_rng.NextDouble()));
            }
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

        // ----- guests' orders (#20): the screen counts open-play time, so nothing runs at the title or while paused -----

        /// <summary>Counts seconds of open play towards the next guest and the waiting guest's patience. The save keeps up with comings and goings.</summary>
        public OrderEvent AdvanceOrders(double dt)
        {
            OrderEvent e = Orders.Advance(Defs, State, dt, TodayNumber, _rng.NextDouble);
            if (e != OrderEvent.None) Save();
            return e;
        }

        /// <summary>Brings a guest to the counter now (previews, tests); null when one is waiting already or the day's guests have all been.</summary>
        public Order PlaceOrder()
        {
            Order order = Orders.Place(Defs, State, TodayNumber, _rng.NextDouble);
            if (order != null) Changed();
            return order;
        }

        /// <summary>Serves the waiting guest from the tray. The carrots paid and the clovers, or false when a line is short.</summary>
        public bool DeliverOrder(out BigNum paid, out int clovers)
        {
            bool ok = Orders.Deliver(Defs, State, Mods, out paid, out clovers);
            if (ok) Changed();
            return ok;
        }

        /// <summary>True while the shop's gift for today is still to be had.</summary>
        public bool GiftWaiting => Shop.GiftWaiting(State, TodayNumber);

        /// <summary>
        /// Takes today's gift at the shop. How many golden carrots it brought; 0 when there was none to take. On the first
        /// week's seventh day it also brings the conical hat (or its price) and a clover (#57). The day is the clamped
        /// <see cref="TodayNumber"/>, so a clock set back gets nothing.
        /// </summary>
        public int ClaimDailyGift() => ClaimDailyGift(out _);

        public int ClaimDailyGift(out Shop.BigGift big)
        {
            int gift = Shop.ClaimDailyGift(State, TodayNumber, PickName(), ItemDefs, out Rabbit seasonRabbit, out big);
            if (seasonRabbit != null) SeasonArrived?.Invoke(seasonRabbit);
            if (gift > 0) Changed();
            return gift;
        }

        /// <summary>Who is visiting the shop today.</summary>
        public Shop.Visitor VisitorToday => Shop.VisitorFor(State, TodayNumber);

        /// <summary>True while today's visitor has not been looked at yet.</summary>
        public bool VisitorUnseen => State.VisitorSeenDay != TodayNumber;

        /// <summary>The shop was opened: today's visitor has been seen.</summary>
        public void MarkVisitorSeen()
        {
            if (State.VisitorSeenDay == TodayNumber) return;
            State.VisitorSeenDay = TodayNumber;
            Changed();
        }

        /// <summary>Buys today's visitor; null when it cannot be bought.</summary>
        public Rabbit BuyVisitor()
        {
            Rabbit rabbit = Shop.BuyVisitor(State, TodayNumber, PickName());
            _shopBought = rabbit != null;
            if (rabbit != null) Changed();
            return rabbit;
        }

        /// <summary>Calls a rabbit with golden carrots at the shop's odds; null when there are not enough.</summary>
        public Rabbit SummonGoldenRabbit()
        {
            Rabbit rabbit = Shop.SummonGolden(State, _rng.NextDouble(), PickName(), _rng.NextDouble(), TodayNumber);
            _shopBought = rabbit != null;
            if (rabbit != null) Changed();
            return rabbit;
        }

        /// <summary>Buys one of the things the shop sells for golden carrots (#44); null when it cannot be bought.</summary>
        public Item BuyItem(string defId)
        {
            Item item = Shop.BuyItem(State, ItemDefs, defId);
            if (item != null) Changed();
            return item;
        }

        /// <summary>Buys one decoration for the garden (#11) into storage; false when it cannot be bought.</summary>
        public bool BuyDecor(string kindId)
        {
            bool ok = Decor.Buy(State, kindId);
            if (ok) Changed();
            return ok;
        }

        /// <summary>Stands a decoration from storage on a spot, or in place of what stands there; false when nothing changes.</summary>
        public bool PlaceDecor(int spot, string kindId)
        {
            bool ok = Decor.Swap(State, spot, kindId);
            if (ok) Changed();
            return ok;
        }

        /// <summary>Takes a spot's decoration back into storage; false when the spot is empty.</summary>
        public bool StoreDecor(int spot)
        {
            bool ok = Decor.Store(State, spot);
            if (ok) Changed();
            return ok;
        }

        /// <summary>Hangs a colour on every paper lantern for golden carrots; false when it cannot be.</summary>
        public bool SetLanternColor(int color)
        {
            bool ok = Decor.SetLanternColor(State, color);
            if (ok) Changed();
            return ok;
        }

        /// <summary>Dyes a rabbit's fur one of the palette's colours for golden carrots.</summary>
        public bool DyeRabbit(long rabbitId, int dye)
        {
            bool ok = Shop.DyeRabbit(State, rabbitId, dye);
            if (ok) Changed();
            return ok;
        }

        /// <summary>Washes a rabbit's dye out, for nothing.</summary>
        public bool WashRabbit(long rabbitId)
        {
            bool ok = Shop.Wash(State, rabbitId);
            if (ok) Changed();
            return ok;
        }

        /// <summary>A photo of the garden was taken. True when it brought today's gift.</summary>
        public bool TakePhoto(string path = null)
        {
            bool gift = Photos.Take(State, TodayNumber, path);
            Changed();
            return gift;
        }

        /// <summary>True while today's photo gift is still to be had.</summary>
        public bool PhotoGiftWaiting => Photos.GiftWaiting(State, TodayNumber);

        // ----- a rabbit asks for its picture (#56): the screen counts open-play time, like the guests' clock -----

        /// <summary>Counts seconds of open play towards the next ask and the bubble's stay. The save keeps up when a bubble comes or goes.</summary>
        public PhotoAskEvent AdvancePhotoAsks(double dt)
        {
            PhotoAskEvent e = PhotoAsks.Advance(Defs, State, dt, TodayNumber, _rng.NextDouble);
            if (e != PhotoAskEvent.None) Save();
            return e;
        }

        /// <summary>A rabbit asks now (previews, tests): false when one asks already, the day's asks are answered, or nobody can.</summary>
        public bool AskForPhoto()
        {
            bool ok = PhotoAsks.Ask(Defs, State, TodayNumber, _rng.NextDouble);
            if (ok) Save();
            return ok;
        }

        /// <summary>The picture was taken for the asking rabbit: two minutes of income, counted for the day. False when nobody asks.</summary>
        public bool AnswerPhotoAsk(out BigNum paid)
        {
            bool ok = PhotoAsks.Answer(Defs, State, Mods, TodayNumber, out paid);
            if (ok) Changed();
            return ok;
        }

        /// <summary>The camera closed without the asked-for picture: that ask goes, nothing else.</summary>
        public bool DeclinePhotoAsk()
        {
            bool ok = PhotoAsks.Decline(State);
            if (ok) Save();
            return ok;
        }

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
            PetResult result = Rabbits.Pet(State, Rabbits.Find(State, id), TodayNumber, wake);
            if (result.FirstToday || result.Energy > 0d) Changed();
            return result;
        }

        /// <summary>The platform store for golden packs (#9): in memory everywhere but a phone build with Unity IAP.</summary>
        IStorefront _storefront;
        readonly List<PurchaseResult> _deliveriesWaiting = new List<PurchaseResult>();
        public IStorefront Store
        {
            get { if (_storefront == null) Store = new FakeStorefront(); return _storefront; }
            set
            {
                if (_storefront != null) _storefront.Delivered -= OnDelivered;
                _storefront = value ?? new FakeStorefront();
                _storefront.Delivered += OnDelivered;
            }
        }

        /// <summary>A purchase the store hands over by itself (one left unfinished before the app died): paid out once, now or once the save is loaded.</summary>
        void OnDelivered(PurchaseResult r)
        {
            if (r.Outcome != PurchaseOutcome.Paid) return;
            if (State == null) { _deliveriesWaiting.Add(r); return; }
            if (!Storefront.Grant(State, r.PackId, r.TransactionId)) return;
            _shopBought = true;
            Changed();
        }

        /// <summary>Buys a golden pack through the store and pays it into the purse when the store says it was paid.</summary>
        public void BuyGolden(string packId, Action<PurchaseResult> done)
        {
            Store.Buy(packId, r =>
            {
                if (r.Outcome == PurchaseOutcome.Paid)
                {
                    if (Storefront.Grant(State, r.PackId, r.TransactionId)) { _shopBought = true; Changed(); }
                    else r.Outcome = PurchaseOutcome.Duplicate;
                }
                done?.Invoke(r);
            });
        }

        public void RestorePurchases(Action<bool> done) => Store.Restore(done);

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
            Available = Cloud.Available, SignedOut = Cloud is ICloudKvsCloudSave && !Cloud.Available,
            LastPushedAt = Sync.LastPushedAt, Last = Sync.Last,
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
            Quests.RollDaily(QuestDefs, Defs, State, TodayNumber, Mods);
            _plotsSeen = State.Plots;
            _goldSeen = GoldRabbits();
            NoteAlbum(false);
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

        /// <summary>
        /// "Delete my data" (#42): the copy in the cloud, the save on the device, its backup and the kept corrupt copy all go,
        /// and the garden starts over as a new game. <paramref name="onDone"/> gets whether everything could be removed.
        /// </summary>
        public void DeleteMyData(Action<bool> onDone = null)
        {
            Cloud.Delete(cloudGone =>
            {
                bool diskGone = _store == null || _store.DeleteAll();
                _sync = null;
                ResetGame();
                onDone?.Invoke(cloudGone && diskGone);
            });
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

        /// <summary>
        /// A garden with no name yet takes the name the linked account knows the player by (#39), once; a garden
        /// the player named, or that took a name before, keeps it. True when the garden was named now.
        /// </summary>
        public bool AdoptLinkedName()
        {
            if (Account.State != BackupState.Linked) return false;
            string name = GameCenterAccount.NameFor(State.Profile, Account.LinkedName);
            if (name == null) return false;
            State.Profile.GardenName = name;
            Changed();
            return true;
        }

        /// <summary>A kind's page in the album was opened: its "new" mark comes off and stays off.</summary>
        public void LookAtKind(string kindId)
        {
            if (Rabbits.LookAt(State, kindId)) Changed();
        }

        /// <summary>Spends one of today's mooncake plays and hands back the game to score; null when none is left.</summary>
        public PoundingRun StartPounding()
        {
            if (!Pounding.Start(State, TodayNumber)) return null;
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
            if (!Leaves.Start(State, TodayNumber)) return null;
            Changed();
            return LeavesRun.FromSeed(Leaves.SeedFor(TodayNumber, State.Leaves.Games));
        }

        /// <summary>Pays a finished leaf-catching game.</summary>
        public LeavesResult FinishLeaves(LeavesRun run)
        {
            LeavesResult result = Leaves.Finish(Defs, State, Mods, run);
            Changed();
            return result;
        }

        /// <summary>Spends one of today's firefly plays and hands back the night, seeded by the day and the games so far; null when none is left.</summary>
        public FirefliesRun StartFireflies()
        {
            if (!Fireflies.Start(State, TodayNumber)) return null;
            Changed();
            return FirefliesRun.FromSeed(Fireflies.SeedFor(TodayNumber, State.Fireflies.Games));
        }

        /// <summary>Pays a finished firefly night.</summary>
        public FirefliesResult FinishFireflies(FirefliesRun run)
        {
            FirefliesResult result = Fireflies.Finish(Defs, State, Mods, run);
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
            _setsDone.Clear(); // a new garden fills its album again, banners and all
            Rabbits.GiftFirst(State, PickName());
            State.BuyMode = buyMode;
            State.HintSeen = true;
            State.LastSeenUnixSeconds = Now;
            Mods = Modifiers.From(UpgradeDefs, State);
            Quests.RollDaily(QuestDefs, Defs, State, TodayNumber, Mods);
            Changed();
        }

        /// <summary>
        /// Begins the next season (#1): the garden goes back to its first plots, keeping its rabbits and items, and
        /// income grows for good. The moon rabbit comes out in <paramref name="gift"/> the first time. False, and
        /// nothing changes, when the garden is not far enough.
        /// </summary>
        public bool BeginNewMoon(out Rabbit gift)
        {
            if (!NewMoon.Begin(Defs, State, PickName(), out gift)) return false;
            // The day's carrot goal was set from the big garden's income; only that goal is set again, so the
            // day's claims stay claimed and nothing can be collected twice.
            Quests.RerollCarrotTarget(Defs, State, Modifiers.From(UpgradeDefs, State));
            _plotsSeen = State.Plots;
            Changed();
            return true;
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
            NoteAlbum(true);
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

        /// <summary>The album's sets already complete, so only a set completed now raises a banner (#61).</summary>
        readonly HashSet<string> _setsDone = new HashSet<string>();

        /// <summary>
        /// Gives the album's milestone gifts not yet given and notes which sets are complete (#61). With
        /// <paramref name="tell"/> the screen hears of a set or a gift that is new; without, nothing is announced
        /// (a save just loaded or restored).
        /// </summary>
        void NoteAlbum(bool tell)
        {
            if (!tell) _setsDone.Clear();
            foreach (AlbumSet set in AlbumSets.Completed(State))
                if (_setsDone.Add(set.Key) && tell) AlbumSetCompleted?.Invoke(set);
            // A milestone's gift goes up last: the rarer moment is the one left on screen.
            int goldenBefore = State.GoldenCarrots;
            List<int> reached = AlbumSets.ClaimMilestones(State, ItemDefs);
            int golden = State.GoldenCarrots - goldenBefore;
            foreach (int milestone in reached)
                if (tell && (milestone == AlbumSets.HatMilestone || milestone == AlbumSets.FrameMilestone))
                    AlbumMilestoneReached?.Invoke(milestone, milestone == AlbumSets.HatMilestone ? golden : 0);
        }

        /// <summary>Remembers a time away for the ×2 button and tells the screen.</summary>
        void TellAway(AwayReport away)
        {
            LastAway = away;
            LastAwayDoubled = false;
            AwayClaimed?.Invoke(away);
        }

        /// <summary>What doubling the latest time away costs right now in golden carrots (0 for the day's first).</summary>
        public int AwayDoubleCost => Away.DoubleCostToday(State, TodayNumber);

        /// <summary>
        /// Pays the latest time away once more (#69): free once a day, then for golden carrots. False when it was
        /// doubled already, there is nothing to double, or the golden carrots are short.
        /// </summary>
        public bool DoubleAway()
        {
            if (LastAwayDoubled || !Away.Double(State, TodayNumber, LastAway.Paid)) return false;
            LastAwayDoubled = true;
            Changed();
            return true;
        }

        // ----- moon jars (#64) -----

        /// <summary>What one moon jar would pay if opened now.</summary>
        public BigNum MoonJarValue => MoonJars.Value(Defs, State, Mods);

        /// <summary>Opens one moon jar into the purse: the carrots it paid, nothing when there is none.</summary>
        public BigNum OpenMoonJar()
        {
            BigNum paid = MoonJars.Open(Defs, State, Mods);
            if (paid > BigNum.Zero) Changed();
            return paid;
        }

        /// <summary>Buys a moon jar at the shop, by the clamped day (#78); false when the shelf is full, the day's three are sold or the purse is short.</summary>
        public bool BuyMoonJar()
        {
            bool ok = MoonJars.Buy(State, TodayNumber);
            if (ok) Changed();
            return ok;
        }

        /// <summary>For tests and previews: as if the player had just come back with this report.</summary>
        public void ShowAwayForTest(AwayReport away) => TellAway(away);

        /// <summary>The folder the save is read from and written to, once the runner has started; null before.</summary>
        public string SaveFolder => _store?.Folder;

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
                PlanReminders();
            }
            else if (!paused && _ready) Notifications.CameBack();
        }

        void OnApplicationQuit()
        {
            if (!_ready) return;
            Save();
            PlanReminders();
        }

        /// <summary>The device reminders (#65), planned again from the garden as it is left; an empty list while switched off.</summary>
        void PlanReminders() => Notifications.GoingAway(State, Mods, DateTimeOffset.Now);

        #region Gift codes and mailbox (#68)

        /// <summary>
        /// The key signed gift codes are checked with, read once from Resources/keys.json (git ignores it; see
        /// tools/make-code.sh). A build without it refuses every signed code; the public codes still work.
        /// </summary>
        static void LoadGiftKey()
        {
            if (GiftCodes.HasKey) return;
            var file = Resources.Load<TextAsset>("keys");
            if (file != null) GiftCodes.SetKey(GiftCodes.KeyFromJson(file.text));
        }

        /// <summary>Tries a gift code the player typed, on the clamped day. Saves when it paid.</summary>
        public GiftCodes.Redeemed RedeemCode(string raw)
        {
            GiftCodes.Redeemed r = GiftCodes.Redeem(State, raw, TodayNumber, ItemDefs);
            if (r.Result == GiftCodes.Result.Ok) Changed();
            return r;
        }

        /// <summary>The letters that can be claimed today, oldest first.</summary>
        public IReadOnlyList<Letter> Letters => Mailbox.Open(State, TodayNumber);

        /// <summary>Claims one letter; null when it is gone.</summary>
        public Letter ClaimLetter(string id)
        {
            Letter letter = Mailbox.Claim(State, id, TodayNumber);
            if (letter != null) Changed();
            return letter;
        }

        /// <summary>Looks for seasons missed since the box last looked; true when a letter came.</summary>
        bool CheckMail() => Mailbox.Check(State, TodayNumber) > 0;

        #endregion
    }
}
