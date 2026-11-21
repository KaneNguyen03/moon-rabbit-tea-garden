using NUnit.Framework;

namespace VuonTra.Core.Tests
{
    public class PhotosTests
    {
        [Test]
        public void The_First_Photo_Of_A_Day_Gives_A_Clover_And_The_Rest_Do_Not()
        {
            GameState s = GameState.NewGame(Fixture.Defs());
            Assert.IsTrue(Photos.GiftWaiting(s, 100));
            Assert.IsTrue(Photos.Take(s, 100));
            Assert.AreEqual(Photos.GiftClovers, s.Clovers);
            Assert.IsFalse(Photos.GiftWaiting(s, 100));
            Assert.IsFalse(Photos.Take(s, 100), "the same day again");
            Assert.AreEqual(Photos.GiftClovers, s.Clovers);
            Assert.IsTrue(Photos.Take(s, 101), "a new day, a new gift");
            Assert.AreEqual(2 * Photos.GiftClovers, s.Clovers);
            Assert.AreEqual(3, s.PhotosTaken);
        }

        [Test]
        public void The_Save_Remembers_The_Photo_Day()
        {
            var defs = Fixture.Defs();
            GameState s = GameState.NewGame(defs);
            Photos.Take(s, 100);
            GameState r = SaveCodec.Decode(defs, SaveCodec.Encode(defs, s)).State;
            Assert.AreEqual(100, r.PhotoDay);
            Assert.AreEqual(1, r.PhotosTaken);
            Assert.IsFalse(Photos.GiftWaiting(r, 100));
            GameState old = SaveCodec.Decode(defs, "{\"version\":10,\"stations\":[]}").State;
            Assert.IsTrue(Photos.GiftWaiting(old, 100), "an older save has its first gift to come");
        }
    }
}
