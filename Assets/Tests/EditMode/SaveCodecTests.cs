using System.Collections.Generic;
using NUnit.Framework;

namespace VuonTra.Core.Tests
{
    public class SaveCodecTests
    {
        List<StationDef> defs;
        GameState s;

        [SetUp]
        public void SetUp()
        {
            defs = Fixture.Defs();
            s = GameState.NewGame(defs);
        }

        [Test]
        public void RoundTrip()
        {
            s.Coins = BigNum.FromParts(4.2, 320);
            s.Levels = new[] { 12, 3, 0 };
            s.Progress[0] = 0.25;
            s.BuyMode = 10;
            s.LastSeenUnixSeconds = 1795272000;
            s.HintSeen = true;

            LoadResult r = SaveCodec.Decode(defs, SaveCodec.Encode(defs, s));

            Assert.AreEqual(LoadStatus.Ok, r.Status);
            Assert.IsTrue(r.State.Coins.Equals(s.Coins));
            CollectionAssert.AreEqual(s.Levels, r.State.Levels);
            Assert.AreEqual(0.25, r.State.Progress[0], 1e-12);
            Assert.AreEqual(10, r.State.BuyMode);
            Assert.AreEqual(1795272000, r.State.LastSeenUnixSeconds);
            Assert.IsTrue(r.State.HintSeen);
        }

        [Test]
        public void RoundTrip_Keeps_Max_Buy_Mode()
        {
            s.BuyMode = 0;
            Assert.AreEqual(0, SaveCodec.Decode(defs, SaveCodec.Encode(defs, s)).State.BuyMode);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void Empty_Starts_Fresh(string json)
        {
            LoadResult r = SaveCodec.Decode(defs, json);
            Assert.AreEqual(LoadStatus.Empty, r.Status);
            Assert.AreEqual(1, r.State.Levels[0]);
        }

        [TestCase("{")]
        [TestCase("not json")]
        [TestCase("{\"version\":1,\"stations\":[{\"id\":\"tea\",\"level\":")]
        [TestCase("[1,2,3]")]
        [TestCase("{\"stations\":[]}")]
        [TestCase("{\"version\":1} trailing")]
        public void Corrupt_Starts_Fresh(string json)
        {
            LoadResult r = SaveCodec.Decode(defs, json);
            Assert.AreEqual(LoadStatus.Corrupt, r.Status);
            CollectionAssert.AreEqual(new[] { 1, 0, 0 }, r.State.Levels);
        }

        [Test]
        public void Newer_Version_Is_Refused()
        {
            Assert.AreEqual(LoadStatus.NewerVersion, SaveCodec.Decode(defs, "{\"version\":99}").Status);
        }

        [Test]
        public void Matches_Stations_By_Id()
        {
            s.Levels = new[] { 5, 4, 3 };
            string json = SaveCodec.Encode(defs, s);
            var reordered = new List<StationDef> { defs[2], defs[0], defs[1] };
            CollectionAssert.AreEqual(new[] { 3, 5, 4 }, SaveCodec.Decode(reordered, json).State.Levels);
        }

        [Test]
        public void Unknown_And_Missing_Ids_Are_Tolerated()
        {
            const string json = "{\"version\":1,\"coinsMantissa\":5,\"coinsExponent\":0,\"stations\":[{\"id\":\"ghost\",\"level\":9,\"progress\":0}],\"buyMode\":1,\"lastSeen\":0,\"hintSeen\":false}";
            LoadResult r = SaveCodec.Decode(defs, json);
            Assert.AreEqual(LoadStatus.Ok, r.Status);
            CollectionAssert.AreEqual(new[] { 1, 0, 0 }, r.State.Levels);
            Assert.AreEqual(5, r.State.Coins.ToDouble(), 1e-9);
        }

        [Test]
        public void Bad_Values_Are_Sanitized()
        {
            const string json = "{\"version\":1,\"coinsMantissa\":-3,\"coinsExponent\":0,\"stations\":[{\"id\":\"tea\",\"level\":-7,\"progress\":55}],\"buyMode\":7,\"lastSeen\":0,\"hintSeen\":false}";
            LoadResult r = SaveCodec.Decode(defs, json);
            Assert.AreEqual(LoadStatus.Ok, r.Status);
            Assert.IsTrue(r.State.Coins.Equals(BigNum.Zero));
            Assert.AreEqual(1, r.State.Levels[0]);
            Assert.AreEqual(0, r.State.Progress[0]);
            Assert.AreEqual(1, r.State.BuyMode);
        }

        static List<UpgradeDef> Ups() => new List<UpgradeDef>
        {
            new UpgradeDef { Id = "hand", MaxLevel = 10 },
            new UpgradeDef { Id = "moon", MaxLevel = 3 },
        };

        [Test]
        public void RoundTrip_Version_Two_Fields()
        {
            var ups = Ups();
            s = GameState.NewGame(defs, ups.Count);
            s.UpgradeLevels = new[] { 4, 2 };
            s.Clovers = 7;
            s.TotalTaps = 123;
            s.LevelsBought = 45;
            s.TotalEarned = BigNum.FromParts(6.5, 30);
            s.DailyDay = 20500;
            s.DailyTapsBase = 100;
            s.DailyLevelsBase = 40;
            s.DailyEarnedBase = new BigNum(5000);
            s.DailyCarrotTarget = new BigNum(1200);
            s.Claimed.Add("mo-2");
            s.Claimed.Add("ngay-cham");

            GameState r = SaveCodec.Decode(defs, SaveCodec.Encode(defs, s, ups), ups).State;

            CollectionAssert.AreEqual(new[] { 4, 2 }, r.UpgradeLevels);
            Assert.AreEqual(7, r.Clovers);
            Assert.AreEqual(123, r.TotalTaps);
            Assert.AreEqual(45, r.LevelsBought);
            Assert.IsTrue(r.TotalEarned.Equals(s.TotalEarned));
            Assert.AreEqual(20500, r.DailyDay);
            Assert.AreEqual(100, r.DailyTapsBase);
            Assert.AreEqual(40, r.DailyLevelsBase);
            Assert.AreEqual(5000, r.DailyEarnedBase.ToDouble(), 1e-6);
            Assert.AreEqual(1200, r.DailyCarrotTarget.ToDouble(), 1e-6);
            CollectionAssert.AreEquivalent(new[] { "mo-2", "ngay-cham" }, r.Claimed);
        }

        [Test]
        public void Version_One_Save_Loads_With_Progress_Intact()
        {
            const string v1 = "{\"version\":1,\"coinsMantissa\":1.5,\"coinsExponent\":3,\"stations\":[{\"id\":\"tea\",\"level\":12,\"progress\":0.5},{\"id\":\"leaf\",\"level\":3,\"progress\":0}],\"buyMode\":10,\"lastSeen\":1795272000,\"hintSeen\":true}";
            LoadResult r = SaveCodec.Decode(defs, v1, Ups());
            Assert.AreEqual(LoadStatus.Ok, r.Status);
            CollectionAssert.AreEqual(new[] { 12, 3, 0 }, r.State.Levels);
            Assert.AreEqual(1500, r.State.Coins.ToDouble(), 1e-6);
            Assert.AreEqual(10, r.State.BuyMode);
            CollectionAssert.AreEqual(new[] { 0, 0 }, r.State.UpgradeLevels);
            Assert.AreEqual(0, r.State.Clovers);
            Assert.IsEmpty(r.State.Claimed);
        }

        [Test]
        public void Upgrade_Levels_Are_Matched_By_Id_And_Capped()
        {
            var ups = Ups();
            const string json = "{\"version\":2,\"stations\":[],\"upgrades\":[{\"id\":\"moon\",\"level\":99},{\"id\":\"gone\",\"level\":5},{\"id\":\"hand\",\"level\":-4}],\"claimed\":[\"a\",7,null]}";
            GameState r = SaveCodec.Decode(defs, json, ups).State;
            CollectionAssert.AreEqual(new[] { 0, 3 }, r.UpgradeLevels);
            CollectionAssert.AreEquivalent(new[] { "a" }, r.Claimed);
        }

        [Test]
        public void Wrong_Field_Types_Do_Not_Throw()
        {
            const string json = "{\"version\":1,\"coinsMantissa\":\"lots\",\"stations\":{\"id\":\"tea\"},\"buyMode\":null,\"hintSeen\":3}";
            LoadResult r = SaveCodec.Decode(defs, json);
            Assert.AreEqual(LoadStatus.Ok, r.Status);
            CollectionAssert.AreEqual(new[] { 1, 0, 0 }, r.State.Levels);
            Assert.IsFalse(r.State.HintSeen);
        }
    }
}
