using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using VuonTra.Game;
using VuonTra.Game.Platform;
using L10n = VuonTra.Game.L10n;

namespace VuonTra.Core.Tests
{
    /// <summary>
    /// Android sign-in through Google Play Games and the save kept in Saved Games (#40), run against a fake
    /// Play Games so the editor can check every rule. The real plugin is only ever called on a device.
    /// </summary>
    public class PlayGamesTests
    {
        /// <summary>Play Games as the game sees it: answers at once, remembers what was asked of it, can be offline or say no.</summary>
        sealed class FakePlayGames : IPlayGames
        {
            /// <summary>Sign-in, opening and writing all fail while false.</summary>
            public bool Online = true;
            /// <summary>Whether a sign-in attempt succeeds while online.</summary>
            public bool Accept = true;
            public bool SignedIn { get; set; }
            public string DisplayName { get; set; } = "Kha Nguyễn";
            /// <summary>What the one snapshot holds: null for none yet, empty for a freshly created one.</summary>
            public byte[] Data;
            public bool ReadFails;
            public string Description;
            public byte[] Cover;
            public TimeSpan Played;
            public int SignIns, InteractiveSignIns, Opens, Reads, Commits, Deletes;

            public void SignIn(bool interactive, Action<bool> onDone)
            {
                SignIns++;
                if (interactive) InteractiveSignIns++;
                bool ok = Online && Accept;
                if (ok) SignedIn = true;
                onDone(ok);
            }

            public void Open(string file, Action<object> onDone)
            {
                Opens++;
                Assert.AreEqual(SavedGamesCloudSave.FileName, file);
                if (!Online || !SignedIn) { onDone(null); return; }
                if (Data == null) Data = new byte[0];
                onDone(file);
            }

            public void Read(object snapshot, Action<byte[]> onDone)
            {
                Reads++;
                Assert.IsNotNull(snapshot);
                onDone(ReadFails ? null : Data);
            }

            public void Commit(object snapshot, byte[] data, string description, byte[] coverPng, TimeSpan played, Action<bool> onDone)
            {
                Commits++;
                Assert.IsNotNull(snapshot);
                if (!Online) { onDone(false); return; }
                Data = data;
                Description = description;
                Cover = coverPng;
                Played = played;
                onDone(true);
            }

            public void Delete(object snapshot, Action<bool> onDone)
            {
                Deletes++;
                Assert.IsNotNull(snapshot);
                if (!Online) { onDone(false); return; }
                Data = null;
                onDone(true);
            }
        }

        FakePlayGames games;
        bool remembered;
        int changes;

        [SetUp]
        public void SetUp()
        {
            L10n.SetLocale("vi");
            games = new FakePlayGames();
            remembered = false;
            changes = 0;
        }

        PlayGamesAccount Account()
        {
            var a = new PlayGamesAccount(games, () => remembered, v => remembered = v);
            a.Changed += () => changes++;
            return a;
        }

        // ----- the garden's default name -----

        [Test]
        public void A_Nameless_Garden_Takes_The_Play_Games_Name_Cleaned_And_A_Named_One_Keeps_Its_Own()
        {
            var unnamed = new PlayerProfile();
            Assert.AreEqual("Kha Nguyễn", PlayGamesAccount.NameFor(unnamed, "Kha Nguyễn"));
            Assert.AreEqual("Kha Nguyễn", PlayGamesAccount.NameFor(unnamed, "  Kha   Nguyễn "), "cleaned like any garden name");
            Assert.LessOrEqual(PlayGamesAccount.NameFor(unnamed, "Nguyễn Thị Hoàng Anh Thư").Length, Profile.MaxNameLength);
            Assert.AreEqual("", PlayGamesAccount.NameFor(unnamed, "   "), "nothing to take: the garden stays unnamed");
            Assert.AreEqual("", PlayGamesAccount.NameFor(unnamed, null));
            var named = new PlayerProfile { GardenName = "Vườn Mây" };
            Assert.AreEqual("Vườn Mây", PlayGamesAccount.NameFor(named, "Kha Nguyễn"), "a name the player chose is never replaced");
        }

        // ----- sign-in -----

        [Test]
        public void Opening_The_Game_Signs_In_Quietly_And_Remembers_That_It_Worked()
        {
            PlayGamesAccount a = Account();
            Assert.AreEqual(AccountProvider.PlayGames, a.Provider);
            Assert.IsTrue(a.CanLink);
            Assert.AreEqual(BackupState.LocalOnly, a.State);
            a.SignInAtOpen();
            Assert.AreEqual(BackupState.Linked, a.State);
            Assert.AreEqual("Kha Nguyễn", a.LinkedName);
            Assert.AreEqual(1, games.SignIns);
            Assert.AreEqual(0, games.InteractiveSignIns, "nothing is asked of the player at open");
            Assert.IsTrue(remembered);
            Assert.GreaterOrEqual(changes, 1);
        }

        [Test]
        public void A_Refused_Or_Failed_Quiet_Sign_In_Leaves_A_Guest_Unless_It_Worked_Before()
        {
            games.Accept = false;
            PlayGamesAccount a = Account();
            a.SignInAtOpen();
            Assert.AreEqual(BackupState.LocalOnly, a.State, "never linked: a guest, with the link button to hand");
            Assert.AreEqual("", a.LinkedName);
            Assert.IsFalse(remembered);

            remembered = true;
            games.Online = false;
            PlayGamesAccount b = Account();
            b.SignInAtOpen();
            Assert.AreEqual(BackupState.Failed, b.State, "linked before and could not get back in: said so");
            Assert.IsTrue(remembered, "one bad morning does not forget the link");
        }

        [Test]
        public void The_Link_Button_Asks_Play_Games_Out_Loud_And_Unlinking_Forgets()
        {
            games.Accept = false;
            PlayGamesAccount a = Account();
            a.SignInAtOpen();
            games.Accept = true;
            bool? linked = null;
            a.Link(ok => linked = ok);
            Assert.AreEqual(true, linked);
            Assert.AreEqual(1, games.InteractiveSignIns);
            Assert.AreEqual(BackupState.Linked, a.State);
            Assert.IsTrue(remembered);

            a.Unlink();
            Assert.AreEqual(BackupState.LocalOnly, a.State);
            Assert.AreEqual("", a.LinkedName);
            Assert.IsFalse(remembered);

            games.Online = false;
            linked = null;
            a.Link(ok => linked = ok);
            Assert.AreEqual(false, linked);
            Assert.AreEqual(BackupState.LocalOnly, a.State, "a failed link from a guest leaves a guest");
        }

        // ----- the save in Saved Games -----

        SavedGamesCloudSave Cloud(Func<byte[]> cover = null) => new SavedGamesCloudSave(games, cover);

        static GameState Garden4()
        {
            var s = GameState.NewGame(Fixture.Defs());
            s.Profile.Code = "THO-7K3M9Q";
            s.GardenXp = Garden.XpForLevel(4);
            s.Rabbits.Add(new Rabbit { Id = 1, Tier = 0, Name = "A" });
            s.Rabbits.Add(new Rabbit { Id = 2, Tier = 0, Name = "B" });
            s.PlaySeconds = 3661;
            s.LastSeenUnixSeconds = 4000;
            return s;
        }

        static string Envelope(GameState s, long at = 5000) => CloudSave.Wrap(SaveCodec.Encode(Fixture.Defs(), s), SaveSummary.Of(s), at);

        [Test]
        public void The_Cloud_Is_There_Only_While_Signed_In()
        {
            SavedGamesCloudSave cloud = Cloud();
            Assert.IsFalse(cloud.Available);
            bool? pushed = null;
            cloud.Push(Envelope(Garden4()), ok => pushed = ok);
            Assert.AreEqual(false, pushed);
            Assert.AreEqual(0, games.Opens, "nothing is tried without a sign-in");
            games.SignedIn = true;
            Assert.IsTrue(cloud.Available);
        }

        [Test]
        public void A_Push_Writes_The_Save_With_Its_Description_Play_Time_And_The_Latest_Photo_As_Cover()
        {
            games.SignedIn = true;
            byte[] photo = { 1, 2, 3 };
            SavedGamesCloudSave cloud = Cloud(() => photo);
            GameState s = Garden4();
            string text = Envelope(s);
            bool? pushed = null;
            cloud.Push(text, ok => pushed = ok);
            Assert.AreEqual(true, pushed);
            Assert.AreEqual(text, Encoding.UTF8.GetString(games.Data));
            Assert.AreEqual("Cấp vườn 4 · 2 thỏ", games.Description);
            Assert.AreEqual(TimeSpan.FromSeconds(3661), games.Played);
            Assert.AreSame(photo, games.Cover);

            SavedGamesCloudSave bare = Cloud();
            bare.Push(text, ok => pushed = ok);
            Assert.AreEqual(true, pushed);
            Assert.IsNull(games.Cover, "no photo yet: no cover");
        }

        [Test]
        public void The_Description_Follows_The_Language()
        {
            var sum = new SaveSummary { GardenLevel = 7, Rabbits = 12 };
            Assert.AreEqual("Cấp vườn 7 · 12 thỏ", SavedGamesCloudSave.Describe(sum));
            L10n.SetLocale("en");
            Assert.AreEqual("Garden level 7 · 12 rabbits", SavedGamesCloudSave.Describe(sum));
            L10n.SetLocale("vi");
        }

        [Test]
        public void A_Pull_Finds_Nothing_In_A_Fresh_Snapshot_And_The_Save_Otherwise()
        {
            games.SignedIn = true;
            SavedGamesCloudSave cloud = Cloud();
            string pulled = "unset";
            cloud.Pull(t => pulled = t);
            Assert.IsNull(pulled, "a snapshot Play Games has just created is empty, not a save");
            string text = Envelope(Garden4());
            games.Data = Encoding.UTF8.GetBytes(text);
            cloud.Pull(t => pulled = t);
            Assert.AreEqual(text, pulled);
        }

        [Test]
        public void A_Read_That_Fails_Blocks_The_Next_Push_So_A_Copy_That_Could_Not_Be_Seen_Is_Not_Overwritten()
        {
            games.SignedIn = true;
            games.Data = Encoding.UTF8.GetBytes(Envelope(Garden4(), 100));
            games.ReadFails = true;
            SavedGamesCloudSave cloud = Cloud();
            string pulled = "unset";
            cloud.Pull(t => pulled = t);
            Assert.IsNull(pulled);
            bool? pushed = null;
            cloud.Push(Envelope(Garden4()), ok => pushed = ok);
            Assert.AreEqual(false, pushed);
            Assert.AreEqual(0, games.Commits, "nothing was written over the copy we could not read");

            games.ReadFails = false;
            cloud.Pull(t => pulled = t);
            Assert.IsNotNull(pulled);
            cloud.Push(Envelope(Garden4()), ok => pushed = ok);
            Assert.AreEqual(true, pushed, "once the copy has been seen, pushing is allowed again");
        }

        [Test]
        public void A_Push_That_Fails_Comes_Out_Of_The_Sync_As_Offline_Without_Crashing()
        {
            games.SignedIn = true;
            SavedGamesCloudSave cloud = Cloud();
            var sync = new CloudSync(cloud, () => 5000);
            GameState s = Garden4();
            games.Online = false;
            SyncReport r = null;
            sync.Sync(SyncReason.Open, SaveCodec.Encode(Fixture.Defs(), s), SaveSummary.Of(s), x => r = x);
            Assert.IsNotNull(r);
            Assert.AreEqual(SyncOutcome.Offline, r.Outcome);
            Assert.AreEqual(-1, sync.LastPushedAt);
            Assert.AreSame(r, sync.Last, "the settings screen can show it");

            games.Online = true;
            sync.Sync(SyncReason.Manual, SaveCodec.Encode(Fixture.Defs(), s), SaveSummary.Of(s), x => r = x);
            Assert.AreEqual(SyncOutcome.Pushed, r.Outcome);
            Assert.AreEqual(EnvelopeStatus.Ok, CloudSave.Open(Encoding.UTF8.GetString(games.Data), out _));
        }

        [Test]
        public void A_Save_Over_The_Limit_Is_Refused_Before_It_Is_Sent()
        {
            games.SignedIn = true;
            SavedGamesCloudSave cloud = Cloud();
            string huge = new string('x', SavedGamesCloudSave.MaxBytes + 1);
            bool? pushed = null;
            cloud.Push(huge, ok => pushed = ok);
            Assert.AreEqual(false, pushed);
            Assert.AreEqual(0, games.Commits);
            Assert.IsTrue(cloud.LastPushTooLarge);
        }

        [Test]
        public void Deleting_Removes_The_Snapshot_And_Says_When_It_Could_Not()
        {
            games.SignedIn = true;
            games.Data = new byte[] { 1 };
            SavedGamesCloudSave cloud = Cloud();
            bool? gone = null;
            cloud.Delete(ok => gone = ok);
            Assert.AreEqual(true, gone);
            Assert.AreEqual(1, games.Deletes);
            Assert.IsNull(games.Data);

            games.Online = false;
            cloud.Delete(ok => gone = ok);
            Assert.AreEqual(false, gone);

            games.SignedIn = false;
            games.Online = true;
            cloud.Delete(ok => gone = ok);
            Assert.AreEqual(true, gone, "not signed in: there is no copy of ours up there to remove");
        }

        [Test]
        public void The_Cover_Is_The_Latest_Album_Photo_When_It_Is_On_Disk_And_Small_Enough()
        {
            string dir = Path.Combine(Path.GetTempPath(), "vt-playgames-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            try
            {
                var album = new List<string>();
                Assert.IsNull(SavedGamesCloudSave.LatestPhoto(album));
                string old = Path.Combine(dir, "vuon-1.png"), latest = Path.Combine(dir, "vuon-2.png"), gone = Path.Combine(dir, "vuon-3.png");
                File.WriteAllBytes(old, new byte[] { 1 });
                File.WriteAllBytes(latest, new byte[] { 2, 2 });
                album.Add(old);
                album.Add(latest);
                album.Add(gone);
                Assert.AreEqual(new byte[] { 2, 2 }, SavedGamesCloudSave.LatestPhoto(album), "the newest picture that is still there");
                File.WriteAllBytes(latest, new byte[SavedGamesCloudSave.MaxCoverBytes + 1]);
                Assert.AreEqual(new byte[] { 1 }, SavedGamesCloudSave.LatestPhoto(album), "a picture too big for a cover is passed over");
            }
            finally { Directory.Delete(dir, true); }
        }

        // ----- the size of a save -----

        [Test]
        public void A_Garden_With_Everything_Maxed_Is_Far_Below_The_Three_Megabyte_Limit()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<GameCatalog>("Assets/VuonTra/Content/GameCatalog.asset");
            IReadOnlyList<StationDef> defs = catalog.BuildDefs();
            IReadOnlyList<UpgradeDef> upgrades = catalog.BuildUpgrades();
            IReadOnlyList<ItemDef> items = catalog.BuildItems();
            IReadOnlyList<QuestDef> quests = catalog.BuildQuests();
            GameState s = GameState.NewGame(defs, upgrades.Count);
            s.Coins = BigNum.FromParts(9.99, 300);
            s.TotalEarned = s.Coins;
            s.DailyEarnedBase = s.Coins;
            s.DailyCarrotTarget = s.Coins;
            for (int i = 0; i < defs.Count; i++) { s.Levels[i] = Economy.MaxLevel; s.Progress[i] = 0.999999; s.TrayWork[i] = 12345.678; }
            for (int i = 0; i < upgrades.Count; i++) s.UpgradeLevels[i] = 99;
            s.Plots = defs.Count;
            s.Clovers = s.GoldenCarrots = int.MaxValue;
            s.TotalTaps = s.LevelsBought = long.MaxValue;
            s.GardenXp = Garden.XpForLevel(Garden.MaxLevel);
            s.RestSpots = RestSpots.Max;
            s.TraySlots = Tray.MaxSlots;
            for (int i = 0; i < Tray.MaxSlots; i++) s.Tray.Add(new Product { Station = i % defs.Count, Count = Tray.StackLimit, Golden = true });
            Profile.Rename(s, "Vườn Trà Thỏ Trăng Rất Dài");
            // Far more rabbits, each with the longest name and everything set, than any garden can hold.
            for (int i = 0; i < 300; i++)
            {
                RabbitKind kind = RabbitKinds.All[i % RabbitKinds.All.Count];
                s.Rabbits.Add(new Rabbit { Id = i + 1, Tier = i % Rabbits.TierCount, Kind = kind.Id, Name = "Thỏ Trăng Mười", Level = 99, Station = i % defs.Count, Affection = Rabbits.AffectionMax, PetDay = 20000, Dye = 3, FirstOfKind = true });
                s.SeenKinds.Add(kind.Id);
                s.FreshKinds.Add(kind.Id);
            }
            s.NextRabbitId = 300;
            s.Profile.AvatarRabbitId = 300;
            for (int i = 0; i < 300; i++)
            {
                Item item = Items.Grant(s, items[i % items.Count], i % Items.TierScale.Length);
                item.WornBy = i + 1;
            }
            foreach (QuestDef q in quests) s.Claimed.Add(q.Id);
            for (int i = 0; i < Photos.AlbumSize; i++)
                s.Album.Add("/data/user/0/com.vuontratho.moonrabbitteagarden/files/Photos/vuon-2026-11-22-23-59-59-" + i + "-with-a-long-suffix.png");
            for (int i = 0; i < Decor.Kinds.Length; i++) s.DecorOwned[i] = 99;
            for (int i = 0; i < Decor.Spots; i++) s.DecorSpots[i] = Decor.Kinds[i % Decor.Kinds.Length].Id;
            s.LanternColor = 3;
            s.Order = new Order { Id = 9, Guest = "Khách Phương Xa", Left = 99, RewardSeconds = 999, RewardClovers = 9 };
            s.PlaySeconds = 123456789.5;
            s.LastSeenUnixSeconds = 1_900_000_000;

            string dir = Path.Combine(Path.GetTempPath(), "vt-save-size-" + Guid.NewGuid().ToString("N"));
            try
            {
                var store = new SaveStore(dir);
                Assert.IsTrue(store.Save(defs, s, upgrades));
                string raw = store.ReadRaw();
                Assert.IsNotNull(raw);
                Assert.AreEqual(LoadStatus.Ok, SaveCodec.Decode(defs, raw, upgrades).Status, "the maximal save still loads");
                string envelope = CloudSave.Wrap(raw, SaveSummary.Of(s), 1_900_000_000);
                int bytes = Encoding.UTF8.GetByteCount(envelope);
                Debug.Log("Saved Games size check: maximal save " + Encoding.UTF8.GetByteCount(raw) + " bytes, as an envelope " + bytes + " bytes, limit " + SavedGamesCloudSave.MaxBytes);
                Assert.Less(bytes, SavedGamesCloudSave.MaxBytes / 10, "a tenth of the 3 MB limit leaves years of growth");
            }
            finally { if (Directory.Exists(dir)) Directory.Delete(dir, true); }
        }

        // ----- wiring into the runner -----

        [Test]
        public void Off_Android_Installing_Does_Nothing_And_The_Runner_Keeps_Its_Local_Only_Services()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<GameCatalog>("Assets/VuonTra/Content/GameCatalog.asset");
            var root = new GameObject("Game");
            try
            {
                var runner = root.AddComponent<GameRunner>();
                runner.Catalog = catalog;
                runner.Init(GameState.NewGame(catalog.BuildDefs()));
                Assert.IsFalse(AndroidServices.Install(runner));
                Assert.IsInstanceOf<LocalOnlyAccount>(runner.Account);
                Assert.IsInstanceOf<LocalOnlyCloudSave>(runner.Cloud);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        [Test]
        public void Installed_With_Play_Games_The_Runner_Signs_In_Names_The_Garden_And_Sends_It_Up()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<GameCatalog>("Assets/VuonTra/Content/GameCatalog.asset");
            var root = new GameObject("Game");
            try
            {
                var runner = root.AddComponent<GameRunner>();
                runner.Catalog = catalog;
                runner.Init(GameState.NewGame(catalog.BuildDefs()));
                var reports = new List<SyncReport>();
                runner.Synced += reports.Add;
                AndroidServices.Install(runner, games, () => remembered, v => remembered = v);
                Assert.IsInstanceOf<PlayGamesAccount>(runner.Account);
                Assert.IsInstanceOf<SavedGamesCloudSave>(runner.Cloud);
                Assert.AreEqual(BackupState.Linked, runner.Account.State);
                Assert.AreEqual("Kha Nguyễn", runner.State.Profile.GardenName, "the garden takes the player's Play Games name");
                Assert.AreEqual(1, games.Commits, "the open sync went up once signed in");
                Assert.AreEqual(1, reports.Count);
                Assert.AreEqual(SyncOutcome.Pushed, reports[0].Outcome);
                Assert.AreEqual(EnvelopeStatus.Ok, CloudSave.Open(Encoding.UTF8.GetString(games.Data), out CloudEnvelope env));
                Assert.AreEqual(runner.State.Profile.Code, env.Summary.Code);
                Assert.IsFalse(runner.State.Profile.LinkRewarded, "signing in quietly is not the link the gift is for");

                runner.State.Profile.GardenName = "Vườn Mây";
                runner.Account.Unlink();
                bool? ok = null; bool gift = false;
                runner.LinkAccount((o, g) => { ok = o; gift = g; });
                Assert.AreEqual(true, ok);
                Assert.IsTrue(gift, "the link button pays the one-time gift");
                Assert.AreEqual("Vườn Mây", runner.State.Profile.GardenName, "a chosen name stays");
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
    }
}
