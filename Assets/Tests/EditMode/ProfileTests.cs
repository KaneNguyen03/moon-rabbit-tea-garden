using System.Collections.Generic;
using NUnit.Framework;

namespace VuonTra.Core.Tests
{
    /// <summary>The player's profile (garden name, avatar rabbit, player code) and the account seam behind it.</summary>
    public class ProfileTests
    {
        List<StationDef> defs;
        GameState s;

        [SetUp]
        public void SetUp()
        {
            defs = Fixture.Defs();
            s = GameState.NewGame(defs);
            s.Levels = new[] { 5, 2, 1 };
        }

        [Test]
        public void A_New_Garden_Has_No_Name_No_Avatar_And_A_Code_Of_Its_Own()
        {
            Assert.AreEqual("", s.Profile.GardenName);
            Assert.AreEqual(0, s.Profile.AvatarRabbitId);
            Assert.IsTrue(Profile.IsCode(s.Profile.Code), "a fresh garden already has a player code: " + s.Profile.Code);
            Assert.AreNotEqual(s.Profile.Code, GameState.NewGame(defs).Profile.Code, "every garden gets its own");
        }

        [TestCase("THO-7K3M9Q", true)]
        [TestCase("tho-7k3m9q", false)]
        [TestCase("THO-7K3M9", false)]
        [TestCase("THO-0O1IAB", false)] // 0, O, 1 and I are left out so a code read aloud is never ambiguous
        [TestCase("", false)]
        [TestCase(null, false)]
        public void A_Code_Is_THO_And_Six_Unambiguous_Characters(string code, bool valid)
        {
            Assert.AreEqual(valid, Profile.IsCode(code));
        }

        [Test]
        public void New_Codes_Are_Valid_And_Rarely_Collide()
        {
            var seen = new HashSet<string>();
            for (int i = 0; i < 2000; i++)
            {
                string code = Profile.NewCode();
                Assert.IsTrue(Profile.IsCode(code), code);
                Assert.IsTrue(seen.Add(code), "duplicate code " + code);
            }
        }

        [Test]
        public void The_Profile_Round_Trips_Through_The_Save()
        {
            s.Rabbits.Add(new Rabbit { Id = 7, Tier = 1, Name = "Mây" });
            s.NextRabbitId = 7;
            Profile.Rename(s, "  Vườn   của \"Kha\" ");
            s.Profile.AvatarRabbitId = 7;
            GameState r = SaveCodec.Decode(defs, SaveCodec.Encode(defs, s)).State;
            Assert.AreEqual("Vườn của \"Kha\"", r.Profile.GardenName);
            Assert.AreEqual(7, r.Profile.AvatarRabbitId);
            Assert.AreEqual(s.Profile.Code, r.Profile.Code, "the code never changes once given");
        }

        [Test]
        public void An_Old_Save_Without_A_Profile_Gets_A_Code_And_Keeps_It_Afterwards()
        {
            const string json = "{\"version\":10,\"stations\":[{\"id\":\"tea\",\"level\":3,\"progress\":0}],\"rabbits\":[]}";
            GameState r = SaveCodec.Decode(defs, json).State;
            Assert.AreEqual("", r.Profile.GardenName);
            Assert.IsTrue(Profile.IsCode(r.Profile.Code));
            GameState again = SaveCodec.Decode(defs, SaveCodec.Encode(defs, r)).State;
            Assert.AreEqual(r.Profile.Code, again.Profile.Code);
        }

        [Test]
        public void A_Broken_Code_In_A_Save_Is_Replaced_And_An_Avatar_That_Left_Is_Forgotten()
        {
            const string json = "{\"version\":11,\"stations\":[],\"rabbits\":[{\"id\":2,\"tier\":0,\"name\":\"B\"}],\"profile\":{\"name\":\"X\",\"avatar\":9,\"code\":\"not-a-code\"}}";
            GameState r = SaveCodec.Decode(defs, json).State;
            Assert.AreEqual("X", r.Profile.GardenName);
            Assert.IsTrue(Profile.IsCode(r.Profile.Code));
            Assert.AreEqual(0, r.Profile.AvatarRabbitId, "rabbit 9 is not in this garden");
        }

        [Test]
        public void Renaming_The_Garden_Cleans_The_Name_And_Refuses_An_Empty_One()
        {
            Assert.IsTrue(Profile.Rename(s, "  Trà   Trăng  "));
            Assert.AreEqual("Trà Trăng", s.Profile.GardenName);
            Assert.IsFalse(Profile.Rename(s, "   "));
            Assert.AreEqual("Trà Trăng", s.Profile.GardenName, "the old name stays");
            Assert.IsTrue(Profile.Rename(s, "Một cái tên rất là dài và còn dài nữa"));
            Assert.LessOrEqual(s.Profile.GardenName.Length, Profile.MaxNameLength);
        }

        [Test]
        public void The_Avatar_Is_The_Chosen_Rabbit_Else_The_First_One_Else_Nobody()
        {
            Assert.IsNull(Profile.Avatar(s));
            var a = new Rabbit { Id = 1, Tier = 0, Name = "A" };
            var b = new Rabbit { Id = 2, Tier = 3, Name = "B" };
            s.Rabbits.Add(a);
            s.Rabbits.Add(b);
            Assert.AreSame(a, Profile.Avatar(s), "nothing chosen: the first rabbit");
            s.Profile.AvatarRabbitId = 2;
            Assert.AreSame(b, Profile.Avatar(s));
            s.Rabbits.Remove(b); // merged away
            Assert.AreSame(a, Profile.Avatar(s), "a chosen rabbit that is gone falls back to the first");
        }

        [Test]
        public void Next_Avatar_Walks_Round_The_Rabbits()
        {
            s.Rabbits.Add(new Rabbit { Id = 1, Tier = 0, Name = "A" });
            s.Rabbits.Add(new Rabbit { Id = 2, Tier = 1, Name = "B" });
            s.Rabbits.Add(new Rabbit { Id = 3, Tier = 2, Name = "C" });
            Assert.AreEqual(2, Profile.NextAvatar(s).Id);
            Assert.AreEqual(3, Profile.NextAvatar(s).Id);
            Assert.AreEqual(1, Profile.NextAvatar(s).Id, "and round again");
            Assert.AreEqual(1, s.Profile.AvatarRabbitId);
            s.Rabbits.Clear();
            Assert.IsNull(Profile.NextAvatar(s));
        }

        [Test]
        public void Linking_An_Account_Pays_Its_Gift_Once_And_The_Save_Remembers()
        {
            Assert.IsTrue(Profile.OnLinked(s), "the first link brings the gift");
            Assert.AreEqual(Profile.LinkRewardClovers, s.Clovers);
            Assert.IsFalse(Profile.OnLinked(s), "linking again, or on another device, brings nothing more");
            Assert.AreEqual(Profile.LinkRewardClovers, s.Clovers);
            GameState r = SaveCodec.Decode(defs, SaveCodec.Encode(defs, s)).State;
            Assert.IsTrue(r.Profile.LinkRewarded);
            Assert.IsFalse(Profile.OnLinked(r));
            GameState old = SaveCodec.Decode(defs, "{\"version\":11,\"stations\":[],\"profile\":{\"name\":\"A\",\"avatar\":0,\"code\":\"THO-7K3M9Q\"}}").State;
            Assert.IsFalse(old.Profile.LinkRewarded, "a save from before the gift still has it to come");
        }

        [Test]
        public void With_Nowhere_To_Back_Up_To_Every_Push_Fails_And_Every_Pull_Finds_Nothing()
        {
            ICloudSave cloud = new LocalOnlyCloudSave();
            Assert.IsFalse(cloud.Available);
            bool? pushed = null;
            cloud.Push("{}", ok => pushed = ok);
            Assert.AreEqual(false, pushed);
            string pulled = "unset";
            cloud.Pull(json => pulled = json);
            Assert.IsNull(pulled);
        }

        [Test]
        public void Two_Copies_Of_A_Garden_Are_Chosen_Between_By_Level_Rabbits_And_Time()
        {
            var a = new SaveSummary { GardenLevel = 5, Rabbits = 3, LastSeenUnixSeconds = 1000, Code = "THO-7K3M9Q" };
            Assert.AreEqual(SaveChoice.Same, SaveSync.Choose(a, a));
            var ahead = a; ahead.GardenLevel = 6; ahead.LastSeenUnixSeconds = 2000;
            Assert.AreEqual(SaveChoice.KeepRemote, SaveSync.Choose(a, ahead), "the other copy is ahead in every way");
            Assert.AreEqual(SaveChoice.KeepLocal, SaveSync.Choose(ahead, a));
            var elsewhere = a; elsewhere.Rabbits = 5; elsewhere.LastSeenUnixSeconds = 500;
            Assert.AreEqual(SaveChoice.Ask, SaveSync.Choose(a, elsewhere), "more rabbits but older: the player decides");
            Assert.AreEqual(SaveChoice.Ask, SaveSync.Choose(elsewhere, a));
            var later = a; later.LastSeenUnixSeconds = 3000;
            Assert.AreEqual(SaveChoice.KeepRemote, SaveSync.Choose(a, later), "same garden, seen later elsewhere");
        }

        [Test]
        public void A_Summary_Reads_The_Garden_Level_Rabbits_And_Time_Off_The_State()
        {
            s.Rabbits.Add(new Rabbit { Id = 1, Tier = 0, Name = "R" });
            s.LastSeenUnixSeconds = 777;
            SaveSummary sum = SaveSummary.Of(s);
            Assert.AreEqual(Garden.Level(s), sum.GardenLevel);
            Assert.AreEqual(1, sum.Rabbits);
            Assert.AreEqual(777, sum.LastSeenUnixSeconds);
            Assert.AreEqual(s.Profile.Code, sum.Code);
        }

        [Test]
        public void With_No_Service_Linked_The_Account_Is_Local_Only_And_Cannot_Link()
        {
            IAccountService account = new LocalOnlyAccount();
            Assert.AreEqual(AccountProvider.None, account.Provider);
            Assert.AreEqual(BackupState.LocalOnly, account.State);
            Assert.IsFalse(account.CanLink);
            bool? result = null;
            account.Link(ok => result = ok);
            Assert.AreEqual(false, result, "says no straight away instead of pretending");
            Assert.AreEqual(BackupState.LocalOnly, account.State);
        }
    }
}
