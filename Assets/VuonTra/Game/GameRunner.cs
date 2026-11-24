using System;
using System.Collections.Generic;
using UnityEngine;
using VuonTra.Core;
using VuonTra.Game.Platform;

namespace VuonTra.Game
{
    public sealed partial class GameRunner : MonoBehaviour
    {
        public GameCatalog Catalog;
        public string SaveDirectory;

        const float AutosaveSeconds = 5f;
        const float LongFrameSeconds = 2f;

        public GameState State { get; private set; }
        public IReadOnlyList<StationDef> Defs { get; private set; }
        public IReadOnlyList<UpgradeDef> UpgradeDefs { get; private set; }
        public IReadOnlyList<QuestDef> QuestDefs { get; private set; }
        public IReadOnlyList<ItemDef> ItemDefs { get; private set; }
        public Modifiers Mods { get; private set; } = Modifiers.None;

        public bool Paused;

        public event Action StateChanged;
        public event Action<int, BigNum> Payout;
        public event Action<AwayReport> AwayClaimed;
        public event Action<Rabbit> SeasonArrived;
        public event Action<Season> SeasonBegan;
        public event Action<AlbumSet> AlbumSetCompleted;
        public event Action<int, int> AlbumMilestoneReached;
        public event Action<string, BigNum, int> IntroRewarded;

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
        public AwayReport LastAway { get; private set; }
        public bool LastAwayDoubled { get; private set; }

        static long Now => DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        public long TodayNumber => Seasons.Clamp(State, DayClock());

        public Func<long> DayClock = () => Today;
        public Season SeasonNow => Seasons.SeasonOf(TodayNumber);

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
            Store = new VuonTra.Game.Store.UnityStorefront();
#endif
#if !UNITY_EDITOR
            Application.logMessageReceived += OnLogMessage;
#endif
            OfferCardsOn = true;
            if (!_ready) Init();
        }

        public void Init(GameState preset = null)
        {
            Defs = Catalog.BuildDefs();
            UpgradeDefs = Catalog.BuildUpgrades();
            QuestDefs = Catalog.BuildQuests();
            ItemDefs = Catalog.BuildItems();

            bool newGarden = false;
            if (preset != null)
            {
                State = preset;
                if (State.UpgradeLevels == null || State.UpgradeLevels.Length != UpgradeDefs.Count)
                    Array.Resize(ref State.UpgradeLevels, UpgradeDefs.Count);
                Mods = Modifiers.From(UpgradeDefs, State);
                foreach (PurchaseResult r in _deliveriesWaiting) if (GrantPaid(r, true)) Analytics.LogPurchase(r, Store.PriceOf(r.PackId), true);
                _deliveriesWaiting.Clear();
            }
            else
            {
                _store = new SaveStore(SaveDirectory);
                LoadResult loaded = _store.Load(Defs, UpgradeDefs);
                State = loaded.State;
                newGarden = loaded.Status != LoadStatus.Ok;
                if (loaded.Status == LoadStatus.Ok)
                {
                    Mods = Modifiers.From(UpgradeDefs, State);
                    _pendingAway = Away.Claim(Defs, State, Now, Mods, TodayNumber);
                }
                else
                {
                    Rabbits.GiftFirst(State, PickName());
                    State.LastSeenUnixSeconds = Now;
                    State.IntrosKnown = true;
                    if (loaded.Status == LoadStatus.Corrupt) Mailbox.Compensate(State, TodayNumber);
                }
                Mods = Modifiers.From(UpgradeDefs, State);
            }

            CheckWelcomeBack();
            CheckFair();
            Intros.Settle(State);
            Quests.RollDaily(QuestDefs, Defs, State, TodayNumber, Mods);
            LoadGiftKey();
            CheckMail();
            _plotsSeen = State.Plots;
            _goldSeen = GoldRabbits();
            NoteAlbum(false);
            _watch.Reset(State);
            _passWatch.Reset(State, TodayNumber);
            _ready = true;
            Analytics.LogGameOpen(newGarden);
            StartSession();
            NoteOffers();
            if (preset == null)
            {
                PlatformServices.Install(this);
                Platform.AndroidServices.Install(this);
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
                State.LastSeenUnixSeconds = Now;
            }
            else if (dt > LongFrameSeconds)
            {
                AwayReport away = Away.Claim(Defs, State, Now, Mods, TodayNumber);
                if (away.Paid >= BigNum.One) TellAway(away);
            }
            else
            {
                Simulation.Tick(Defs, State, dt, Payout, Mods, Roll, ProductMade);
            }

            if (!Paused) State.PlaySeconds += dt;
            (_adProvider as FakeRewardedAds)?.Advance(dt);
            _sinceSync += dt;
            if (_sinceSync >= SyncTimerSeconds) SyncNow(SyncReason.Timer);
            PlatformServices.Tick(this, dt);
            _sinceSave += dt;
            if (_sinceSave >= AutosaveSeconds)
            {
                if (CheckWelcomeBack()) StateChanged?.Invoke();
                if (CheckFair()) StateChanged?.Invoke();
                if (Quests.RollDaily(QuestDefs, Defs, State, TodayNumber, Mods)) StateChanged?.Invoke();
                if (CheckMail()) StateChanged?.Invoke();
                if (NoteOffers()) StateChanged?.Invoke();
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
            if (ok && !Tutorial.IsDone(State) && Tutorial.Advance(State) > 0) { _watch.Note(Defs, State, Analytics); StateChanged?.Invoke(); }
            return ok;
        }

        public void SkipTutorial()
        {
            _watch.SkipTutorial(State, Analytics);
            Changed();
        }

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

        public bool ClaimQuest(int index, out BigNum paid)
        {
            paid = BigNum.Zero;
            if (index < 0 || index >= QuestDefs.Count) return false;
            QuestDef quest = QuestDefs[index];
            LastQuestItem = null;
            int clovers = quest.Chained ? Quests.CurrentTier(quest, State).RewardClovers : quest.RewardClovers;
            bool ok = Quests.TryClaim(quest, Defs, State, Mods, out paid);
            if (!ok) return false;
            if (quest.Daily || clovers >= 2)
            {
                var gifts = new System.Collections.Generic.List<ItemDef>();
                foreach (ItemDef def in ItemDefs) if (def.Price <= 0) gifts.Add(def);
                if (gifts.Count > 0) LastQuestItem = Items.Grant(State, gifts[_rng.Next(gifts.Count)], Rabbits.RollTier(_rng.NextDouble()));
            }
            Changed();
            return true;
        }

        public BoxPrize[] OpenQuestBox()
        {
            BoxPrize[] prizes = QuestChests.OpenBox(QuestDefs, Defs, State, Mods);
            if (prizes != null) Changed();
            return prizes;
        }

        public bool ClaimWeekChest(int chest, out Rabbit rabbit)
        {
            bool ok = QuestChests.ClaimChest(State, chest, TodayNumber, PickName(), _rng.NextDouble(), out rabbit);
            if (!ok) return false;
            if (rabbit != null) Rabbits.AutoAssign(Defs, State);
            Changed();
            return true;
        }

        readonly System.Random _rng = new System.Random();
        Func<double> _roll;
        Func<double> Roll => _roll ??= _rng.NextDouble;

        public Item LastQuestItem { get; private set; }

        public event Action<int> ProductMade;

        public BigNum SellProduct(int slot, out int clovers)
        {
            BigNum paid = Tray.Sell(Defs, State, slot, Mods, out clovers);
            if (paid > BigNum.Zero || clovers > 0) Changed();
            return paid;
        }

        public BigNum SellAllProducts(out int clovers)
        {
            BigNum paid = Tray.SellAll(Defs, State, Mods, out clovers);
            if (paid > BigNum.Zero || clovers > 0) Changed();
            return paid;
        }

        public OrderEvent AdvanceOrders(double dt)
        {
            OrderEvent e = Orders.Advance(Defs, State, dt, TodayNumber, Roll);
            if (e != OrderEvent.None) Save();
            return e;
        }

        public Order PlaceOrder()
        {
            Order order = Orders.Place(Defs, State, TodayNumber, Roll);
            if (order != null) Changed();
            return order;
        }

        public bool DeliverOrder(out BigNum paid, out int clovers)
        {
            bool ok = Orders.Deliver(Defs, State, Mods, out paid, out clovers);
            if (ok)
            {
                EarnFair(Fairs.OrderCoins);
                TryIntro(Intros.Orders);
                Changed();
            }
            return ok;
        }

        public bool RaiseDish(int station)
        {
            if (!Recipes.Raise(State, station)) return false;
            Changed();
            return true;
        }

        public bool GiftWaiting => Shop.GiftWaiting(State, TodayNumber);

        public int ClaimDailyGift() => ClaimDailyGift(out _);

        public int ClaimDailyGift(out Shop.BigGift big)
        {
            int gift = Shop.ClaimDailyGift(State, TodayNumber, PickName(), ItemDefs, out Rabbit seasonRabbit, out big);
            if (seasonRabbit != null) SeasonArrived?.Invoke(seasonRabbit);
            if (gift > 0)
            {
                TryIntro(Intros.Shop);
                Changed();
            }
            return gift;
        }

        public Shop.Visitor VisitorToday => Shop.VisitorFor(State, TodayNumber);

        public bool VisitorUnseen => State.VisitorSeenDay != TodayNumber;

        public void MarkVisitorSeen()
        {
            if (State.VisitorSeenDay == TodayNumber) return;
            State.VisitorSeenDay = TodayNumber;
            Changed();
        }

        public Rabbit BuyVisitor()
        {
            Rabbit rabbit = Shop.BuyVisitor(State, TodayNumber, PickName());
            _shopBought = rabbit != null;
            if (rabbit != null) Changed();
            return rabbit;
        }

        public Rabbit SummonGoldenRabbit()
        {
            Rabbit rabbit = Shop.SummonGolden(State, _rng.NextDouble(), PickName(), _rng.NextDouble(), TodayNumber);
            _shopBought = rabbit != null;
            if (rabbit != null) Analytics.LogRabbitCalled(AnalyticsEvents.ViaGolden, rabbit);
            if (ReviewAsk.IsHappy(rabbit)) NoteReview(ReviewMoment.GoldenRabbit);
            if (rabbit != null) Changed();
            return rabbit;
        }

        public Item BuyItem(string defId)
        {
            Item item = Shop.BuyItem(State, ItemDefs, defId);
            if (item != null) Changed();
            return item;
        }

        public bool BuyDecor(string kindId)
        {
            bool ok = Decor.Buy(State, kindId);
            if (ok) Changed();
            return ok;
        }

        public bool PlaceDecor(int spot, string kindId)
        {
            bool ok = Decor.Swap(State, spot, kindId);
            if (ok) Changed();
            return ok;
        }

        public bool StoreDecor(int spot)
        {
            bool ok = Decor.Store(State, spot);
            if (ok) Changed();
            return ok;
        }

        public bool SetLanternColor(int color)
        {
            bool ok = Decor.SetLanternColor(State, color);
            if (ok) Changed();
            return ok;
        }

        public bool DyeRabbit(long rabbitId, int dye)
        {
            bool ok = Shop.DyeRabbit(State, rabbitId, dye);
            if (ok) Changed();
            return ok;
        }

        public bool WashRabbit(long rabbitId)
        {
            bool ok = Shop.Wash(State, rabbitId);
            if (ok) Changed();
            return ok;
        }

        public bool TakePhoto(string path = null)
        {
            bool gift = Photos.Take(State, TodayNumber, path);
            Changed();
            return gift;
        }

        public bool PhotoGiftWaiting => Photos.GiftWaiting(State, TodayNumber);

        public PhotoAskEvent AdvancePhotoAsks(double dt)
        {
            PhotoAskEvent e = PhotoAsks.Advance(Defs, State, dt, TodayNumber, Roll);
            if (e != PhotoAskEvent.None) Save();
            return e;
        }

        public bool AskForPhoto()
        {
            bool ok = PhotoAsks.Ask(Defs, State, TodayNumber, Roll);
            if (ok) Save();
            return ok;
        }

        public bool AnswerPhotoAsk(out BigNum paid)
        {
            bool ok = PhotoAsks.Answer(Defs, State, Mods, TodayNumber, out paid);
            if (ok) EarnFair(Fairs.PhotoAskCoins);
            if (ok) Changed();
            return ok;
        }

        public bool DeclinePhotoAsk()
        {
            bool ok = PhotoAsks.Decline(State);
            if (ok) Save();
            return ok;
        }

        public void Grant(BigNum carrots)
        {
            State.Coins += carrots;
            State.TotalEarned += carrots;
            Changed();
        }

        public void AddEnergy(double amount)
        {
            State.Energy = Math.Min(Energy.Max, State.Energy + amount);
            StateChanged?.Invoke();
        }

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

        public PetResult PetRabbit(long id, bool wake = false)
        {
            PetResult result = Rabbits.Pet(State, Rabbits.Find(State, id), TodayNumber, wake);
            if (result.TenHearts) NoteReview(ReviewMoment.TenHearts);
            if (result.FirstToday || result.Energy > 0d) Changed();
            return result;
        }

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

        void OnDelivered(PurchaseResult r)
        {
            if (r.Outcome != PurchaseOutcome.Paid) return;
            if (State == null) { _deliveriesWaiting.Add(r); return; }
            if (!GrantPaid(r, true)) return;
            Analytics.LogPurchase(r, Store.PriceOf(r.PackId), true);
            Changed();
        }

        public void BuyGolden(string packId, Action<PurchaseResult> done)
        {
            if (Offers.Find(packId) != null && !Offers.CanBuy(State, packId, OfferNow))
            {
                PurchaseResult gone = PurchaseResult.Of(PurchaseOutcome.Unavailable, packId);
                Analytics.LogPurchase(gone, Store.PriceOf(packId));
                done?.Invoke(gone);
                return;
            }
            Store.Buy(packId, r =>
            {
                if (r.Outcome == PurchaseOutcome.Paid)
                {
                    if (GrantPaid(r, false)) Changed();
                    else r.Outcome = PurchaseOutcome.Duplicate;
                }
                if (r.Outcome == PurchaseOutcome.Failed || r.Outcome == PurchaseOutcome.Unavailable) _review.PurchaseFailed = true;
                Analytics.LogPurchase(r, Store.PriceOf(r.PackId));
                done?.Invoke(r);
            });
        }

        public void RestorePurchases(Action<bool> done) => Store.Restore(done);

        public IAccountService Account { get; set; } = new LocalOnlyAccount();
        ICloudSave _cloud = new LocalOnlyCloudSave();
        public ICloudSave Cloud
        {
            get => _cloud;
            set { _cloud = value ?? new LocalOnlyCloudSave(); _sync = null; }
        }

        CloudSync _sync;
        float _sinceSync;
        const float SyncTimerSeconds = 180f;
        int _plotsSeen, _goldSeen;
        bool _shopBought;
        public event Action<SyncReport> SyncChoiceNeeded;
        public event Action<SyncReport> Synced;

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
            _watch.Reset(State);
            _passWatch.Reset(State, TodayNumber);
            StateChanged?.Invoke();
            Save();
        }

        int GoldRabbits()
        {
            int n = 0;
            foreach (Rabbit r in State.Rabbits) if (r.Tier >= 4) n++;
            return n;
        }

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

        public void ResolveSync(bool takeRemote, Action<SyncReport> onDone = null)
        {
            Sync.Resolve(takeRemote, LocalSaveText(), ReplaceWithCloud, r => { Synced?.Invoke(r); onDone?.Invoke(r); });
        }

        public void PushToCloud(Action<SyncReport> onDone = null)
        {
            Sync.PushNow(LocalSaveText(), SaveSummary.Of(State), r => { Synced?.Invoke(r); onDone?.Invoke(r); });
        }

        public void RestoreFromCloud(Action<SyncReport> onDone = null)
        {
            Sync.Restore(SaveSummary.Of(State), ReplaceWithCloud, r => { Synced?.Invoke(r); onDone?.Invoke(r); });
        }

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

        public bool AdoptLinkedName()
        {
            if (Account.State != BackupState.Linked) return false;
            string name = GameCenterAccount.NameFor(State.Profile, Account.LinkedName);
            if (name == null) return false;
            State.Profile.GardenName = name;
            Changed();
            return true;
        }

        public void LookAtKind(string kindId)
        {
            if (Rabbits.LookAt(State, kindId)) Changed();
        }

        public PoundingRun StartPounding()
        {
            if (!Pounding.Start(State, TodayNumber)) return null;
            Changed();
            return new PoundingRun();
        }

        public PoundingResult FinishPounding(PoundingRun run)
        {
            PoundingResult result = Pounding.Finish(Defs, State, Mods, run);
            EarnFair(Fairs.ForStars(result.Stars));
            TryIntro(Intros.Pounding);
            if (ReviewAsk.IsHappy(result.Stars)) NoteReview(ReviewMoment.ThreeStars);
            Changed();
            return result;
        }

        public LeavesRun StartLeaves()
        {
            if (!Leaves.Start(State, TodayNumber)) return null;
            Changed();
            return LeavesRun.FromSeed(Leaves.SeedFor(TodayNumber, State.Leaves.Games));
        }

        public LeavesResult FinishLeaves(LeavesRun run)
        {
            LeavesResult result = Leaves.Finish(Defs, State, Mods, run);
            EarnFair(Fairs.ForStars(result.Stars));
            TryIntro(Intros.Leaves);
            if (ReviewAsk.IsHappy(result.Stars)) NoteReview(ReviewMoment.ThreeStars);
            Changed();
            return result;
        }

        public FirefliesRun StartFireflies()
        {
            if (!Fireflies.Start(State, TodayNumber)) return null;
            Changed();
            return FirefliesRun.FromSeed(Fireflies.SeedFor(TodayNumber, State.Fireflies.Games));
        }

        public FirefliesResult FinishFireflies(FirefliesRun run)
        {
            FirefliesResult result = Fireflies.Finish(Defs, State, Mods, run);
            EarnFair(Fairs.ForStars(result.Stars));
            TryIntro(Intros.Fireflies);
            if (ReviewAsk.IsHappy(result.Stars)) NoteReview(ReviewMoment.ThreeStars);
            Changed();
            return result;
        }

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

        public Rabbit SummonRabbit()
        {
            Rabbit rabbit = Rabbits.Summon(State, _rng.NextDouble(), PickName(), _rng.NextDouble());
            if (rabbit != null) Analytics.LogRabbitCalled(AnalyticsEvents.ViaClover, rabbit);
            if (ReviewAsk.IsHappy(rabbit)) NoteReview(ReviewMoment.GoldenRabbit);
            if (rabbit != null) Changed();
            return rabbit;
        }

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

        public bool AssignRabbit(long id, int station)
        {
            if (Layout.IsStored(Defs, State, station)) return false;
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

        public void SetSpendShare(double share)
        {
            State.SpendShare = share == 0.5d || share == 0.25d ? share : 1d;
            StateChanged?.Invoke();
        }

        public List<UpgradeStep> UpgradeAll()
        {
            List<UpgradeStep> plan = UpgradePlan.Plan(Defs, State, Simulation.Budget(State));
            if (!UpgradePlan.Apply(Defs, State, plan)) return null;
            Changed();
            return plan;
        }

        public int LevelUpRabbitMax(long id)
        {
            int n = Rabbits.LevelUpMax(State, id, Simulation.Budget(State));
            if (n > 0) Changed();
            return n;
        }

        public void ResetGame()
        {
            int buyMode = State.BuyMode;
            State = GameState.NewGame(Defs, UpgradeDefs.Count);
            _setsDone.Clear();
            Rabbits.GiftFirst(State, PickName());
            State.BuyMode = buyMode;
            State.HintSeen = true;
            State.IntrosKnown = true;
            State.LastSeenUnixSeconds = Now;
            Mods = Modifiers.From(UpgradeDefs, State);
            Quests.RollDaily(QuestDefs, Defs, State, TodayNumber, Mods);
            Changed();
        }

        public bool BeginNewMoon(out Rabbit gift)
        {
            if (!NewMoon.Begin(Defs, State, PickName(), out gift)) return false;
            TryIntro(Intros.NewMoon);
            Quests.RerollCarrotTarget(Defs, State, Modifiers.From(UpgradeDefs, State));
            _plotsSeen = State.Plots;
            Analytics.LogNewMoon(State);
            Changed();
            return true;
        }

        public bool BuyPlot()
        {
            int segments = Expansion.Segments(State);
            bool ok = Plots.TryBuy(Defs, State);
            if (ok && Expansion.Segments(State) > segments) NoteReview(ReviewMoment.LandOpened);
            if (ok) Changed();
            return ok;
        }

        void Changed()
        {
            if (!Tutorial.IsDone(State)) Tutorial.Advance(State);
            _watch.Note(Defs, State, Analytics);
            _passWatch.Note(State, TodayNumber, Analytics);
            NoteAlbum(true);
            NoteOffers();
            Mods = Modifiers.From(UpgradeDefs, State);
            StateChanged?.Invoke();
            Save();
            int gold = GoldRabbits();
            bool milestone = State.Plots > _plotsSeen || gold > _goldSeen || _shopBought;
            _plotsSeen = State.Plots;
            _goldSeen = gold;
            _shopBought = false;
            if (milestone && _ready) SyncNow(SyncReason.Milestone);
        }

        readonly HashSet<string> _setsDone = new HashSet<string>();

        void NoteAlbum(bool tell)
        {
            if (!tell) _setsDone.Clear();
            foreach (AlbumSet set in AlbumSets.Completed(State))
                if (_setsDone.Add(set.Key) && tell) AlbumSetCompleted?.Invoke(set);
            int goldenBefore = State.GoldenCarrots;
            List<int> reached = AlbumSets.ClaimMilestones(State, ItemDefs);
            int golden = State.GoldenCarrots - goldenBefore;
            foreach (int milestone in reached)
                if (tell && (milestone == AlbumSets.HatMilestone || milestone == AlbumSets.FrameMilestone))
                    AlbumMilestoneReached?.Invoke(milestone, milestone == AlbumSets.HatMilestone ? golden : 0);
        }

        void TellAway(AwayReport away)
        {
            if (away.WorthTelling)
            {
                LastAway = away;
                LastAwayDoubled = false;
            }
            AwayClaimed?.Invoke(away);
        }

        public int AwayDoubleCost => Away.DoubleCostToday(State, TodayNumber);

        public bool DoubleAway()
        {
            if (LastAwayDoubled || !Away.Double(State, TodayNumber, LastAway.Paid)) return false;
            LastAwayDoubled = true;
            Changed();
            return true;
        }

        public BigNum MoonJarValue => MoonJars.Value(Defs, State, Mods);

        public BigNum OpenMoonJar()
        {
            BigNum paid = MoonJars.Open(Defs, State, Mods);
            if (paid > BigNum.Zero)
            {
                TryIntro(Intros.MoonJar);
                Changed();
            }
            return paid;
        }

        public bool BuyMoonJar()
        {
            bool ok = MoonJars.Buy(State, TodayNumber);
            if (ok) Changed();
            return ok;
        }

        public static bool AdsInPlayerBuilds = false;

        IRewardedAds _adProvider;
        public IRewardedAds AdProvider
        {
            get { if (_adProvider == null) _adProvider = DefaultAds(); return _adProvider; }
            set => _adProvider = value ?? new NoAds();
        }

        static IRewardedAds DefaultAds()
        {
#if UNITY_EDITOR
            return new FakeRewardedAds();
#else
            return AdsInPlayerBuilds ? (IRewardedAds)new FakeRewardedAds() : new NoAds();
#endif
        }

        public bool AdsOn => !(AdProvider is NoAds);
        public bool AdShowing { get; private set; }

        public AdOffer AdOfferFor(string placement, string game = null)
        {
            if (placement == Ads.AwayDouble && (LastAwayDoubled || LastAway.Paid < BigNum.One)) return AdOffer.None;
            return Ads.Offer(State, TodayNumber, AdProvider, placement, game);
        }

        public void WatchAd(string placement, string game, Action<bool> done)
        {
            AdOffer offer = AdShowing ? AdOffer.None : AdOfferFor(placement, game);
            if (offer == AdOffer.None) { done?.Invoke(false); return; }
            if (offer == AdOffer.Instant) { Analytics.LogRewardedAd(placement, game, AnalyticsEvents.AdPass); done?.Invoke(ClaimAd(placement, game)); return; }
            AdShowing = true;
            AdProvider.Show(placement, watched =>
            {
                AdShowing = false;
                Analytics.LogRewardedAd(placement, game, watched ? AnalyticsEvents.AdWatched : AnalyticsEvents.AdClosed);
                done?.Invoke(watched && ClaimAd(placement, game));
            });
        }

        bool ClaimAd(string placement, string game)
        {
            long day = TodayNumber;
            bool ok;
            switch (placement)
            {
                case Ads.AwayDouble:
                    ok = !LastAwayDoubled && Ads.ClaimAwayDouble(State, day, LastAway.Paid);
                    if (ok) LastAwayDoubled = true;
                    break;
                case Ads.ExtraRound: ok = Ads.ClaimExtraRound(State, day, game); break;
                case Ads.ShopGolden: ok = Ads.ClaimShopGolden(State, day); break;
                default: ok = false; break;
            }
            if (ok) Changed();
            return ok;
        }

        public static bool AnalyticsInPlayerBuilds = false;

        IAnalytics _analytics;
        public IAnalytics Analytics
        {
            get { if (_analytics == null) _analytics = DefaultAnalytics(); return _analytics; }
            set => _analytics = value ?? new NoAnalytics();
        }

        static IAnalytics DefaultAnalytics()
        {
#if UNITY_EDITOR
            return AnalyticsFor(false);
#else
            return AnalyticsFor(true);
#endif
        }

        public static IAnalytics AnalyticsFor(bool playerBuild)
        {
            if (!playerBuild) return new MemoryAnalytics();
            return AnalyticsInPlayerBuilds ? Platform.FirebaseAnalyticsAdapter.Create() ?? new NoAnalytics() : new NoAnalytics();
        }

        public Func<long> SessionClock = () => Now;
        readonly AnalyticsWatch _watch = new AnalyticsWatch();
        long _sessionStart = -1;

        void StartSession()
        {
            if (_sessionStart >= 0) return;
            _sessionStart = SessionClock();
            _review.NewSession();
            Analytics.LogSessionStart(State);
        }

        void EndSession()
        {
            if (_sessionStart < 0) return;
            Analytics.LogSessionEnd(SessionClock() - _sessionStart);
            _sessionStart = -1;
        }

        public void RecordException(Exception e)
        {
            if (e != null) _review.Crashed = true;
            Analytics.RecordException(e);
        }

        public void OnLogMessage(string condition, string stackTrace, LogType type)
        {
            if (type == LogType.Exception) RecordException(new LoggedException(condition, stackTrace));
        }

        public sealed class LoggedException : Exception
        {
            readonly string _stack;
            public LoggedException(string message, string stackTrace) : base(message) { _stack = stackTrace; }
            public override string StackTrace => _stack;
        }

        IReviewPrompt _reviewPrompt;
        public IReviewPrompt ReviewPrompt
        {
            get { if (_reviewPrompt == null) _reviewPrompt = ReviewPromptFor(NativeReviewPrompt.Create()); return _reviewPrompt; }
            set => _reviewPrompt = value ?? ReviewPromptFor(null);
        }

        public static IReviewPrompt ReviewPromptFor(IReviewPrompt native) => native ?? new FakeReviewPrompt { Said = Debug.Log };

        readonly ReviewWatch _review = new ReviewWatch();
        public ReviewWatch Review => _review;

        void NoteReview(ReviewMoment moment) => _review.Note(State, TodayNumber, moment);

        public ReviewMoment TakeReviewMoment(bool busy)
        {
            if (_review.Waiting == ReviewMoment.None) return ReviewMoment.None;
            ReviewMoment moment = _review.Take(State, TodayNumber, busy);
            if (moment == ReviewMoment.None) return moment;
            Analytics.LogReviewAsked(moment, State.Review.Asks);
            Save();
            return moment;
        }

        public void LikeReview()
        {
            ReviewAsk.Like(State);
            Analytics.LogReviewLiked(_review.LastAsked);
            Save();
            ReviewPrompt.RequestReview();
        }

        public string AnswerReviewFeedback(string typed)
        {
            string line = ReviewAsk.OneLine(typed);
            Analytics.LogReviewFeedback(_review.LastAsked, line.Length > 0, line.Length);
            if (line.Length == 0) return null;
            string code = State.Profile.Code;
            return ReviewAsk.MailTo(AppInfo.SupportEmail, L10n.T("review.mail.subject", code), L10n.T("review.mail.body", line, code));
        }

        public void OpenStorePage() => ReviewPrompt.OpenStorePage();

        public bool RainingAt(double hour) => Weather.IsRaining(TodayNumber, hour);

        public FrogEvent AdvanceFrog(double dt, double hour)
        {
            FrogEvent e = FrogRabbit.Advance(State, dt, TodayNumber, hour);
            if (e != FrogEvent.None) Save();
            return e;
        }

        public bool GreetFrog(double hour, out BigNum paid, out int clovers)
        {
            bool ok = FrogRabbit.Greet(Defs, State, Mods, TodayNumber, hour, _rng.NextDouble(), out paid, out clovers);
            if (ok)
            {
                TryIntro(Intros.Rain);
                Changed();
            }
            return ok;
        }

        public int NoAdsDaysLeft => Ads.PassDaysLeft(State, TodayNumber);

        public void BuyNoAdsPass(Action<PurchaseResult> done)
        {
            Seasons.Clamp(State, DayClock());
            BuyGolden(Storefront.NoAdsPass, done);
        }

        public Func<long> FishingClock = () => Now;

        public FishingCast Cast { get; private set; }

        public bool PondOpen => Fishing.Unlocked(State);

        public int Baits => Fishing.Baits(State, FishingClock());

        public long NextBaitIn => Fishing.NextBaitIn(State, FishingClock());

        public IReadOnlyList<int> Basket => State.Fish;

        public FishingCast StartCast()
        {
            if (Cast != null && !Cast.Over) return null;
            Cast = Fishing.Cast(State, FishingClock(), Roll);
            if (Cast != null) Changed();
            return Cast;
        }

        public CastPhase AdvanceCast(double dt)
        {
            if (Cast == null) return CastPhase.Lost;
            CastPhase phase = Cast.Advance(dt);
            if (phase == CastPhase.Lost)
            {
                Cast = null;
                if (TryIntro(Intros.Fishing)) Changed();
            }
            return phase;
        }

        public CastTap TapCast()
        {
            if (Cast == null) return CastTap.None;
            CastTap tap = Cast.Tap();
            bool landed = tap == CastTap.Caught && Fishing.Land(State, Cast);
            if (landed) EarnFair(Fairs.ForFish(Cast.Fish));
            if (Cast.Over)
            {
                Cast = null;
                landed |= TryIntro(Intros.Fishing);
            }
            if (landed) Changed();
            return tap;
        }

        public FeedResult Feed(long rabbitId, int fish)
        {
            FeedResult result = Fishing.Feed(State, Rabbits.Find(State, rabbitId), fish);
            if (result.Bond.TenHearts) NoteReview(ReviewMoment.TenHearts);
            if (result.Fed) Changed();
            return result;
        }

        public FeedResult FeedBest(long rabbitId) => Feed(rabbitId, Fishing.BestInBasket(State));

        public void ShowAwayForTest(AwayReport away) => TellAway(away);

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
                EndSession();
                Save();
                SyncNow(SyncReason.Background);
                PlanReminders();
            }
            else if (!paused && _ready) { StartSession(); Notifications.CameBack(); }
        }

        void OnDestroy() => Application.logMessageReceived -= OnLogMessage;

        void OnApplicationQuit()
        {
            if (!_ready) return;
            EndSession();
            Save();
            PlanReminders();
        }

        void PlanReminders() => Notifications.GoingAway(State, Mods, DateTimeOffset.Now);

        #region Gift codes and mailbox (#68)

        static void LoadGiftKey()
        {
            if (GiftCodes.HasKey) return;
            var file = Resources.Load<TextAsset>("keys");
            if (file != null) GiftCodes.SetKey(GiftCodes.KeyFromJson(file.text));
        }

        public GiftCodes.Redeemed RedeemCode(string raw)
        {
            GiftCodes.Redeemed r = GiftCodes.Redeem(State, raw, TodayNumber, ItemDefs);
            if (r.Result == GiftCodes.Result.Ok) Changed();
            return r;
        }

        public IReadOnlyList<Letter> Letters => Mailbox.Open(State, TodayNumber);

        public Letter ClaimLetter(string id) => ClaimLetter(id, out _);

        public Letter ClaimLetter(string id, out Rabbit rabbit)
        {
            Letter pending = State.Letters.Find(l => l.Id == id);
            double kindRoll = pending != null && pending.Kind == LetterKind.Newcomer ? Newcomer.ShownKindRoll : _rng.NextDouble();
            Letter letter = Mailbox.Claim(State, id, TodayNumber, PickName(), kindRoll, out rabbit);
            if (letter == null) return null;
            if (rabbit != null) Rabbits.AutoAssign(Defs, State);
            Changed();
            return letter;
        }

        bool CheckMail()
        {
            bool closed = Newcomer.Expire(Defs, State, TodayNumber) != null;
            return Mailbox.Check(State, TodayNumber) > 0 || closed;
        }

        public NewcomerClaim ClaimNewcomer(int day, int slot)
        {
            if (day < 1 || day > Newcomer.Days || slot < 0 || slot >= Newcomer.TasksPerDay) return default;
            bool fallback = Newcomer.IsFallback(Newcomer.At(day, slot), State);
            NewcomerClaim claim = Newcomer.Claim(Defs, State, Mods, day, slot, PickName(), Newcomer.ShownKindRoll);
            if (!claim.Ok) return claim;
            Analytics.LogNewcomer(claim, fallback);
            Changed();
            return claim;
        }

        #endregion

        public bool TryIntro(string id)
        {
            bool tried = !State.IntrosTried.Contains(id);
            if (Intros.Use(Defs, State, Mods, id, out BigNum paid, out int clovers)) IntroRewarded?.Invoke(id, paid, clovers);
            return tried && State.IntrosTried.Contains(id);
        }

        public bool TouchIntro(string id)
        {
            if (!Intros.Touch(State, id)) return false;
            Changed();
            return true;
        }

        public string ShowNextIntro(IntroQueue queue, double dt, bool blocked)
        {
            string id = queue.Tick(State, dt, blocked);
            if (id != null) Changed();
            return id;
        }

        public bool LeaveIntroForLater(string id)
        {
            if (!Intros.Later(State, id)) return false;
            Changed();
            return true;
        }

        #region Month pass (#107)

        readonly MonthPassWatch _passWatch = new MonthPassWatch();

        public bool ClaimMonthTier(MonthRow row, int tier, string kindId, out MonthClaim claim)
        {
            if (!MonthPass.Claim(Defs, State, Mods, row, tier, TodayNumber, kindId, PickName(), out claim)) return false;
            if (claim.Rabbit != null) Rabbits.AutoAssign(Defs, State);
            Changed();
            return true;
        }

        public void BuyMonthPass(Action<PurchaseResult> done)
        {
            if (!MonthPass.CanBuy(State, TodayNumber))
            {
                PurchaseResult refused = PurchaseResult.Of(PurchaseOutcome.Duplicate, MonthPass.ProductId);
                Analytics.LogPurchase(refused, Store.PriceOf(MonthPass.ProductId));
                done?.Invoke(refused);
                return;
            }
            MonthPass.Roll(State, TodayNumber, Defs, Mods);
            BuyGolden(MonthPass.ProductId, done);
        }

        public bool HangMonthLantern(int cal)
        {
            bool ok = MonthPass.HangLantern(State, cal);
            if (ok) Changed();
            return ok;
        }

        public bool PaperLanterns()
        {
            if (MonthPass.LanternMonth(State.LanternColor) < 0) return false;
            State.LanternColor = 0;
            Changed();
            return true;
        }

        public bool WearMonthFrame(int cal)
        {
            bool ok = MonthPass.WearFrame(State, cal);
            if (ok) Changed();
            return ok;
        }

        #endregion

        public LayoutDrop DropInLayout(Hex from, Hex to)
        {
            LayoutDrop done = Layout.Drop(Defs, State, from, to);
            if (done != VuonTra.Core.LayoutDrop.Back)
            {
                TryIntro(Intros.Layout);
                Changed();
            }
            return done;
        }

        public bool StoreAway(string id)
        {
            bool ok = Layout.Store(Defs, State, id);
            if (ok) Changed();
            return ok;
        }

        public bool TakeOutStation(int index)
        {
            bool ok = Layout.TakeOut(Defs, State, index);
            if (ok) Changed();
            return ok;
        }
    }
}
