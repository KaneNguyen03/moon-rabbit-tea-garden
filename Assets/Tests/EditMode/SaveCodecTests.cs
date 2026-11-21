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
