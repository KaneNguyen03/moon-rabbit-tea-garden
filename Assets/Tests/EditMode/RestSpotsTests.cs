using System.Collections.Generic;
using NUnit.Framework;

namespace VuonTra.Core.Tests
{
    public class RestSpotsTests
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

        Rabbit Add(int tier, int station = -1)
        {
            var r = new Rabbit { Id = ++s.NextRabbitId, Tier = tier, Name = "R" + s.NextRabbitId, Station = station };
            s.Rabbits.Add(r);
            return r;
        }

        [Test]
        public void A_New_Garden_Has_Two_Places_To_Rest()
        {
            Assert.AreEqual(2, RestSpots.Starting);
            Assert.AreEqual(RestSpots.Starting, s.RestSpots);
            Assert.GreaterOrEqual(RestSpots.Max, 4);
        }

        [Test]
        public void Only_Resting_Rabbits_Take_A_Place_Best_First_And_No_More_Than_There_Are_Places()
        {
            Rabbit worker = Add(5, 0);
            Rabbit silver = Add(0), gold = Add(4), green = Add(1);
            List<Rabbit> seated = RestSpots.Seated(s);
            Assert.AreEqual(2, seated.Count);
            Assert.AreSame(gold, seated[0]);
            Assert.AreSame(green, seated[1]);
            CollectionAssert.DoesNotContain(seated, worker);
            CollectionAssert.DoesNotContain(seated, silver);

            s.RestSpots = 3;
            Assert.AreEqual(3, RestSpots.Seated(s).Count);
            Assert.AreSame(silver, RestSpots.Seated(s)[2]);
        }

        [Test]
        public void Nobody_Resting_Means_Nobody_Seated()
        {
            Add(2, 0);
            Assert.IsEmpty(RestSpots.Seated(s));
        }

        [Test]
        public void Another_Place_Costs_Carrots_And_Each_One_Costs_More()
        {
            BigNum first = RestSpots.CostOfNext(s);
            Assert.IsTrue(first > BigNum.Zero);
            s.Coins = first - BigNum.One;
            Assert.IsFalse(RestSpots.TryBuy(s), "one carrot short");
            Assert.AreEqual(2, s.RestSpots);

            s.Coins = first;
            Assert.IsTrue(RestSpots.TryBuy(s));
            Assert.AreEqual(3, s.RestSpots);
            Assert.IsTrue(s.Coins.Equals(BigNum.Zero));
            Assert.IsTrue(RestSpots.CostOfNext(s) > first);
        }

        [Test]
        public void There_Is_A_Last_Place()
        {
            s.RestSpots = RestSpots.Max;
            s.Coins = BigNum.FromParts(1, 50);
            Assert.IsFalse(RestSpots.CanBuy(s));
            Assert.IsFalse(RestSpots.TryBuy(s));
            Assert.AreEqual(RestSpots.Max, s.RestSpots);
            Assert.IsTrue(RestSpots.CostOfNext(s).Equals(BigNum.Zero));
        }

        [Test]
        public void Places_To_Rest_Never_Change_What_The_Garden_Earns()
        {
            Add(3, 0); Add(1);
            double before = Economy.IncomePerSecond(defs, s).ToDouble();
            s.Coins = RestSpots.CostOfNext(s);
            Assert.IsTrue(RestSpots.TryBuy(s));
            Assert.AreEqual(before, Economy.IncomePerSecond(defs, s).ToDouble(), 1e-12);
        }

        [Test]
        public void Places_Round_Trip_And_Old_Saves_Start_With_Two()
        {
            s.RestSpots = 4;
            Assert.AreEqual(4, SaveCodec.Decode(defs, SaveCodec.Encode(defs, s)).State.RestSpots);
            Assert.AreEqual(2, SaveCodec.Decode(defs, "{\"version\":6,\"stations\":[]}").State.RestSpots);
            Assert.AreEqual(RestSpots.Max, SaveCodec.Decode(defs, "{\"version\":7,\"stations\":[],\"restSpots\":999}").State.RestSpots);
            Assert.AreEqual(2, SaveCodec.Decode(defs, "{\"version\":7,\"stations\":[],\"restSpots\":-5}").State.RestSpots);
        }
    }
}
