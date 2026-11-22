using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using VuonTra.Game;
using VuonTra.Game.Platform;
using VuonTra.Game.UI;
using L10n = VuonTra.Game.L10n;

namespace VuonTra.Core.Tests
{
    /// <summary>
    /// The iOS services behind the account and cloud seams (#39): Game Center sign-in and the iCloud key-value store.
    /// The device APIs cannot run here, so what is tested is everything around them: the state machine with a fake
    /// sign-in, the default-name rule, the wiring off iOS, the size of a full garden against iCloud's limit, and the
    /// way a save that is too large is reported rather than sent.
    /// </summary>
    public class PlatformTests
    {
        /// <summary>A Game Center that answers when the test says so.</summary>
        sealed class FakeGameCenter
        {
            public Action<bool, string> Waiting;
            public int Asked;
            public void Authenticate(Action<bool, string> onDone) { Asked++; Waiting = onDone; }
            public void Answer(bool ok, string name) { Action<bool, string> w = Waiting; Waiting = null; w(ok, name); }
        }

        sealed class MemoryPrefs : GameCenterAccount.IPrefs
        {
            public readonly Dictionary<string, string> Map = new Dictionary<string, string>();
            public string Get(string key) => Map.TryGetValue(key, out string v) ? v : null;
            public void Set(string key, string value) => Map[key] = value;
            public void Remove(string key) => Map.Remove(key);
        }

        GameCatalog catalog;

        [SetUp]
        public void SetUp()
        {
            L10n.SetLocale("vi");
            catalog = AssetDatabase.LoadAssetAtPath<GameCatalog>("Assets/VuonTra/Content/GameCatalog.asset");
        }

        GameRunner Runner()
        {
            var go = new GameObject("Runner");
            var runner = go.AddComponent<GameRunner>();
            runner.Catalog = catalog;
            runner.Init(GameState.NewGame(catalog.BuildDefs(), catalog.BuildUpgrades().Count));
            return runner;
        }

        [Test]
        public void Off_IOS_The_Runner_Keeps_The_Local_Only_Account_And_Cloud()
        {
            Assert.IsFalse(PlatformServices.IsIOSDevice, "the editor on a Mac is not an iPhone");
            GameRunner runner = Runner();
            try
            {
                PlatformServices.Install(runner);
                Assert.IsInstanceOf<LocalOnlyAccount>(runner.Account);
                Assert.IsInstanceOf<LocalOnlyCloudSave>(runner.Cloud);
                Assert.IsFalse(runner.CloudStatus.SignedOut, "no iCloud to be signed out of");
                PlatformServices.Tick(runner, 10f);
                Assert.IsInstanceOf<LocalOnlyCloudSave>(runner.Cloud, "ticking off iOS changes nothing");
            }
            finally { UnityEngine.Object.DestroyImmediate(runner.gameObject); }
        }

        [Test]
        public void Off_IOS_The_ICloud_Store_Is_Honest_About_Having_Nothing()
        {
            var kvs = new ICloudKvsCloudSave();
            Assert.IsFalse(kvs.Available);
            Assert.AreEqual(ICloudKvsCloudSave.Limit, kvs.MaxBytes);
            bool? pushed = null;
            kvs.Push("{}", ok => pushed = ok);
            Assert.AreEqual(false, pushed);
            string pulled = "unset";
            kvs.Pull(text => pulled = text);
            Assert.IsNull(pulled);
            bool? deleted = null;
            kvs.Delete(ok => deleted = ok);
            Assert.AreEqual(true, deleted, "nothing up there counts as gone");
            Assert.IsFalse(kvs.TakeExternalChange());
            kvs.Start();
        }

        [Test]
        public void The_Game_Center_Name_Becomes_The_Garden_Name_Only_While_The_Garden_Has_None()
        {
            var profile = new PlayerProfile();
            Assert.AreEqual("Kha Nguyễn", GameCenterAccount.NameFor(profile, "  Kha   Nguyễn "), "cleaned like any name");
            Assert.IsNull(GameCenterAccount.NameFor(profile, ""), "no name to take");
            Assert.IsNull(GameCenterAccount.NameFor(profile, "   "));
            Assert.IsNull(GameCenterAccount.NameFor(profile, null));
            string longName = GameCenterAccount.NameFor(profile, "Một cái tên Game Center rất là dài");
            Assert.LessOrEqual(longName.Length, Profile.MaxNameLength);
            profile.GardenName = "Trà Trăng";
            Assert.IsNull(GameCenterAccount.NameFor(profile, "Kha"), "a garden the player named keeps its name");
        }

        [Test]
        public void A_Sign_In_Is_Remembered_So_The_Row_Says_Linked_At_Once_And_Unlink_Forgets_It()
        {
            var gc = new FakeGameCenter();
            var prefs = new MemoryPrefs();
            var account = new GameCenterAccount(gc.Authenticate, prefs);
            int changed = 0;
            account.Changed += () => changed++;
            Assert.AreEqual(AccountProvider.GameCenter, account.Provider);
            Assert.AreEqual(BackupState.LocalOnly, account.State);
            Assert.IsTrue(account.CanLink);

            account.Open();
            Assert.AreEqual(BackupState.Linking, account.State, "asked Game Center as soon as the game opened");
            Assert.AreEqual(1, gc.Asked);
            Assert.IsFalse(account.CanLink, "no second button press while one is in flight");
            gc.Answer(true, "Kha");
            Assert.AreEqual(BackupState.Linked, account.State);
            Assert.AreEqual("Kha", account.LinkedName);
            Assert.IsFalse(account.CanLink);
            Assert.GreaterOrEqual(changed, 2);

            // The next open says linked before Game Center answers, and the answer only confirms it.
            var again = new GameCenterAccount(gc.Authenticate, prefs);
            again.Open();
            Assert.AreEqual(BackupState.Linked, again.State, "remembered");
            Assert.AreEqual("Kha", again.LinkedName);
            Assert.AreEqual(2, gc.Asked);
            gc.Answer(false, "");
            Assert.AreEqual(BackupState.Linked, again.State, "a remembered link is not undone by one failed check");
            again.Link(null);
            Assert.AreEqual(BackupState.Linked, again.State, "and asking again does not flicker through Linking");
            gc.Answer(true, "Kha N.");
            Assert.AreEqual("Kha N.", again.LinkedName, "the name follows Game Center");

            // Game Center has no sign-out: unlinking only forgets here.
            again.Unlink();
            Assert.AreEqual(BackupState.LocalOnly, again.State);
            Assert.AreEqual("", again.LinkedName);
            Assert.IsTrue(again.CanLink);
            var third = new GameCenterAccount(gc.Authenticate, prefs);
            third.Open();
            Assert.AreEqual(BackupState.Linking, third.State, "nothing remembered any more");
        }

        [Test]
        public void A_Refused_Sign_In_Is_Reported_And_Can_Be_Tried_Again()
        {
            var gc = new FakeGameCenter();
            var account = new GameCenterAccount(gc.Authenticate, new MemoryPrefs());
            bool? linked = null;
            account.Link(ok => linked = ok);
            gc.Answer(false, "");
            Assert.AreEqual(false, linked);
            Assert.AreEqual(BackupState.Failed, account.State);
            Assert.IsTrue(account.CanLink, "the button stays, to try again");
            Assert.AreEqual(L10n.T("backup.failed", L10n.T("provider.gamecenter")), ProfileView.BackupLine(account));
            account.Link(ok => linked = ok);
            Assert.AreEqual(L10n.T("backup.linking", L10n.T("provider.gamecenter")), ProfileView.BackupLine(account));
            gc.Answer(true, "Kha");
            Assert.AreEqual(true, linked);
            Assert.AreEqual(L10n.T("backup.linked", "Kha"), ProfileView.BackupLine(account));
            Assert.AreEqual(L10n.T("backup.local"), ProfileView.BackupLine(new LocalOnlyAccount()));
        }

        [Test]
        public void The_Runner_Takes_The_Linked_Name_For_An_Unnamed_Garden_Once_And_Never_Over_The_Players_Own()
        {
            var gc = new FakeGameCenter();
            var account = new GameCenterAccount(gc.Authenticate, new MemoryPrefs());
            GameRunner runner = Runner();
            try
            {
                runner.Account = account;
                account.Changed += () => runner.AdoptLinkedName();
                Assert.IsFalse(runner.AdoptLinkedName(), "nothing linked yet");
                Assert.AreEqual("", runner.State.Profile.GardenName);
                account.Open();
                gc.Answer(true, "Kha Nguyễn");
                Assert.AreEqual("Kha Nguyễn", runner.State.Profile.GardenName, "the Game Center name named the garden");
                Assert.IsTrue(runner.RenameGarden("Trà Trăng"));
                account.Link(null);
                gc.Answer(true, "Kha Nguyễn");
                Assert.AreEqual("Trà Trăng", runner.State.Profile.GardenName, "the player's own name stays");
                Assert.IsFalse(runner.AdoptLinkedName());
            }
            finally { UnityEngine.Object.DestroyImmediate(runner.gameObject); }
        }

        [Test]
        public void A_Save_Too_Large_For_The_Cloud_Is_Reported_Not_Sent_And_Nothing_Crashes()
        {
            IReadOnlyList<StationDef> defs = Fixture.Defs();
            GameState local = GameState.NewGame(defs);
            local.Rabbits.Add(new Rabbit { Id = 1, Tier = 0, Name = "A" });
            string save = SaveCodec.Encode(defs, local);
            var cloud = new MemoryCloudSave { MaxBytes = CloudSave.Bytes(save) };
            long now = 5000;
            var sync = new CloudSync(cloud, () => now);
            SyncReport r = null;
            sync.Sync(SyncReason.Open, save, SaveSummary.Of(local), x => r = x);
            Assert.AreEqual(SyncOutcome.TooLarge, r.Outcome, "the envelope round the save tips it over");
            Assert.IsNull(cloud.Text, "nothing went up");
            Assert.AreEqual(0, cloud.Pushes, "it was never even tried");
            Assert.AreEqual(-1, sync.LastPushedAt);
            Assert.IsNull(sync.Pending);
            sync.PushNow(save, SaveSummary.Of(local), x => r = x);
            Assert.AreEqual(SyncOutcome.TooLarge, r.Outcome, "Save now says the same");
            Assert.AreEqual(L10n.T("cloud.status.toolarge"), ProfileView.CloudLine(new CloudStatus { Available = true, Last = r }));

            cloud.MaxBytes = 0;
            sync.PushNow(save, SaveSummary.Of(local), x => r = x);
            Assert.AreEqual(SyncOutcome.Pushed, r.Outcome, "with room it goes up");
            Assert.IsNotNull(cloud.Text);
            // The store itself refuses too, in case a cloud without a cap in the sync is handed something huge.
            cloud.MaxBytes = 10;
            bool? pushed = null;
            cloud.Push(cloud.Text, ok => pushed = ok);
            Assert.AreEqual(false, pushed);
            Assert.AreEqual(L10n.T("cloud.status.signin"), ProfileView.CloudLine(new CloudStatus { Available = false, SignedOut = true }));
            Assert.AreEqual(L10n.T("cloud.status.off"), ProfileView.CloudLine(new CloudStatus { Available = false }));
        }

        [Test]
        public void A_Full_Garden_Wrapped_For_The_Cloud_Stays_Far_Under_The_iCloud_Limit()
        {
            IReadOnlyList<StationDef> defs = catalog.BuildDefs();
            List<UpgradeDef> upgrades = catalog.BuildUpgrades();
            string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "vuontra-size-" + Guid.NewGuid().ToString("N"));
            System.IO.Directory.CreateDirectory(dir);
            try
            {
                var store = new SaveStore(dir);

                // A typical garden: a week in, a handful of rabbits, a few photos.
                GameState typical = GameState.NewGame(defs, upgrades.Count);
                typical.Plots = 6;
                for (int i = 0; i < 6; i++) typical.Levels[i] = 40 - i * 5;
                for (int i = 0; i < 6; i++) typical.Rabbits.Add(new Rabbit { Id = i + 1, Tier = i % 3, Kind = RabbitKinds.All[i].Id, Name = "Thỏ " + i, Level = 5, Station = i });
                for (int i = 0; i < 3; i++) typical.Album.Add(PhotoPath(i));
                Profile.Rename(typical, "Vườn của Kha");
                Assert.IsTrue(store.Save(defs, typical, upgrades));
                string typicalSave = store.ReadRaw();
                int typicalBytes = CloudSave.Bytes(CloudSave.Wrap(typicalSave, SaveSummary.Of(typical), 1));

                GameState full = Maximal(defs, upgrades);
                Assert.IsTrue(store.Save(defs, full, upgrades));
                string fullSave = store.ReadRaw();
                Assert.AreEqual(LoadStatus.Ok, SaveCodec.Decode(defs, fullSave, upgrades).Status, "the full garden loads back");
                int fullBytes = CloudSave.Bytes(CloudSave.Wrap(fullSave, SaveSummary.Of(full), 1));

                TestContext.Out.WriteLine("cloud envelope: typical " + typicalBytes + " bytes, maximal " + fullBytes + " bytes (save alone " + CloudSave.Bytes(fullSave) + ")");
                Assert.Less(fullBytes, 512 * 1024, "a full garden wrapped for the cloud must stay under 512 KB; it is " + fullBytes);
                Assert.Less(fullBytes, ICloudKvsCloudSave.Limit / 2, "and under half of what iCloud allows per key");
                Assert.Less(typicalBytes, 64 * 1024, "a typical garden is small: " + typicalBytes);
            }
            finally { System.IO.Directory.Delete(dir, true); }
        }

        /// <summary>A path like the one the photo button writes on an iPhone: the app's own folder and a stamped file name.</summary>
        static string PhotoPath(int i) =>
            "/var/mobile/Containers/Data/Application/0F2B6C8A-1D3E-4F5A-9B7C-8D9E0F1A2B3C/Documents/VuonTra/vuon-2026-11-22-23-59-" + (10 + i) + "-" + (100 + i) + ".png";

        /// <summary>Everything at its most: all plots and stations at the cap, sixty rabbits dressed and named, the album full, every quest paid, an order waiting, every decoration out.</summary>
        static GameState Maximal(IReadOnlyList<StationDef> defs, List<UpgradeDef> upgrades)
        {
            var s = GameState.NewGame(defs, upgrades.Count);
            s.Coins = new BigNum(9.99e300);
            s.TotalEarned = new BigNum(9.99e305);
            s.DailyEarnedBase = new BigNum(1.23e299);
            s.DailyCarrotTarget = new BigNum(4.56e299);
            s.Plots = defs.Count;
            for (int i = 0; i < defs.Count; i++) { s.Levels[i] = 500; s.Progress[i] = 0.987654321; s.TrayWork[i] = 123.456789; }
            for (int i = 0; i < upgrades.Count; i++) s.UpgradeLevels[i] = Math.Max(1, upgrades[i].MaxLevel);
            s.Clovers = 9999; s.GoldenCarrots = 99999; s.GiftDay = 20780; s.GiftStreak = 365;
            s.VisitorBoughtDay = 20780; s.VisitorSeenDay = 20780;
            s.TotalTaps = 123456789; s.LevelsBought = 6000; s.DailyDay = 20780; s.DailyTapsBase = 123456000; s.DailyLevelsBase = 5990;
            s.Energy = 100; s.BoostSecondsLeft = 59.5; s.RestSpots = RestSpots.Max; s.GardenXp = 1e12;
            s.TraySlots = Tray.MaxSlots;
            for (int i = 0; i < Tray.MaxSlots; i++) s.Tray.Add(new Product { Station = i, Count = Tray.StackLimit, Golden = i % 2 == 0 });
            s.GoldenToday = 2; s.PhotoDay = 20780; s.PhotosTaken = 9999; s.TutorialStep = 99; s.TutorialCloverGiven = true;
            s.PhotosWithGoldRabbit = 9000; s.PhotosWithFullRest = 8000; s.PhotosWithSeasonRabbit = 7000;
            s.MaxDaySeen = 20780; s.SeasonGreetedDay = 20780;
            s.PoundingDay = 20780; s.PoundingPlays = 3; s.PoundingStarToday = true; s.PoundingThreeToday = true; s.PoundingGames = 9999; s.PoundingBestStars = 3;
            s.Leaves = new MiniGameRecord { Day = 20780, Plays = 3, StarToday = true, CloversToday = 3, GoldenToday = 1, Games = 9999 };
            s.Fireflies = new MiniGameRecord { Day = 20780, Plays = 3, StarToday = true, CloversToday = 3, GoldenToday = 1, Games = 9999 };
            for (int i = 0; i < Photos.AlbumSize; i++) s.Album.Add(PhotoPath(i));
            s.ViewX = 1234.5678; s.ViewY = -987.6543; s.ViewZoom = 1.2345;
            Profile.Rename(s, "Vườn Trà Thỏ Trăng Mây");
            s.Profile.AvatarRabbitId = 60;
            s.Profile.LinkRewarded = true;

            IReadOnlyList<RabbitKind> kinds = RabbitKinds.All;
            for (int i = 0; i < 60; i++)
            {
                RabbitKind kind = kinds[i % kinds.Count];
                s.Rabbits.Add(new Rabbit { Id = i + 1, Tier = kind.Tier, Kind = kind.Id, Name = "Thỏ Trăng Mây", Level = 50, Station = i % defs.Count, Affection = 30, PetDay = 20780, Dye = i % 4 });
            }
            s.NextRabbitId = 61;
            List<ItemDef> itemDefs = catalogItems;
            for (int i = 0; i < 100; i++)
            {
                ItemDef def = itemDefs[i % itemDefs.Count];
                s.Items.Add(new Item { Id = i + 1, DefId = def.Id, Name = def.Name, Slot = def.Slot, Effect = def.Effect, Tier = 5, Amount = 0.123456789, WornBy = i < 60 ? i + 1 : 0 });
            }
            s.NextItemId = 101;
            foreach (RabbitKind kind in kinds) { s.SeenKinds.Add(kind.Id); s.FreshKinds.Add(kind.Id); }
            foreach (QuestDef q in catalogQuests) s.Claimed.Add(q.Id);
            s.Order = new Order { Id = 9999, Guest = kinds[kinds.Count - 1].Id, Left = 123.456, RewardSeconds = 600, RewardClovers = 3 };
            for (int i = 0; i < 4; i++) s.Order.Lines.Add(new OrderLine { Station = i, Count = 5 });
            s.OrderClock = 123.456; s.OrdersToday = 20; s.OrderDay = 20780; s.OrdersDelivered = 9999; s.DailyOrdersBase = 9979; s.NextOrderId = 10000;
            for (int i = 0; i < s.DecorOwned.Length; i++) s.DecorOwned[i] = 2;
            for (int i = 0; i < Decor.Spots; i++) s.DecorSpots[i] = Decor.Kinds[i % Decor.Kinds.Length].Id;
            s.LanternColor = 3;
            return s;
        }

        static List<ItemDef> catalogItems;
        static List<QuestDef> catalogQuests;

        [OneTimeSetUp]
        public void LoadContent()
        {
            var c = AssetDatabase.LoadAssetAtPath<GameCatalog>("Assets/VuonTra/Content/GameCatalog.asset");
            catalogItems = c.BuildItems();
            catalogQuests = c.BuildQuests();
        }
    }
}
