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
            Assert.AreEqual(Photos.GiftGolden, s.GoldenCarrots);
            Assert.IsFalse(Photos.GiftWaiting(s, 100));
            Assert.IsFalse(Photos.Take(s, 100), "the same day again");
            Assert.AreEqual(Photos.GiftGolden, s.GoldenCarrots);
            Assert.IsTrue(Photos.Take(s, 101), "a new day, a new gift");
            Assert.AreEqual(2 * Photos.GiftGolden, s.GoldenCarrots);
            Assert.AreEqual(3, s.PhotosTaken);
        }

        [Test]
        public void A_Clock_Turned_Back_Never_Pays_The_Photo_Gift_Again()
        {
            // #78: switching the device between two days used to pay a golden carrot every time.
            GameState s = GameState.NewGame(Fixture.Defs());
            Assert.IsTrue(Photos.Take(s, 101));
            Assert.IsFalse(Photos.GiftWaiting(s, 100), "an earlier day has no gift waiting");
            Assert.IsFalse(Photos.Take(s, 100), "nor does it pay one");
            Assert.IsFalse(Photos.Take(s, 101));
            Assert.AreEqual(Photos.GiftGolden, s.GoldenCarrots, "one golden carrot in all");
            Assert.AreEqual(101, s.PhotoDay);
        }

        [Test]
        public void A_Photo_Counts_Gold_Rabbits_And_Full_Rest_And_Fills_The_Album_Newest_Last()
        {
            GameState s = GameState.NewGame(Fixture.Defs());
            Photos.Take(s, 1, "/p/a.png");
            Assert.AreEqual(1, s.PhotosTaken);
            Assert.AreEqual(0, s.PhotosWithGoldRabbit);
            Assert.AreEqual(0, s.PhotosWithFullRest);
            CollectionAssert.AreEqual(new[] { "/p/a.png" }, s.Album);

            s.Rabbits.Add(new Rabbit { Id = 1, Tier = Photos.GoldTier, Name = "G" });
            s.RestSpots = RestSpots.Max;
            Photos.Take(s, 1, "/p/b.png");
            Assert.AreEqual(1, s.PhotosWithGoldRabbit);
            Assert.AreEqual(1, s.PhotosWithFullRest);
            CollectionAssert.AreEqual(new[] { "/p/a.png", "/p/b.png" }, s.Album);
            Photos.Take(s, 1, null);
            Assert.AreEqual(3, s.PhotosTaken);
            Assert.AreEqual(2, s.Album.Count, "a picture that was not written is not in the album");

            for (int i = 0; i < 20; i++) Photos.Take(s, 1, "/p/" + i + ".png");
            Assert.AreEqual(Photos.AlbumSize, s.Album.Count, "only the latest are kept");
            Assert.AreEqual("/p/19.png", s.Album[s.Album.Count - 1]);
            Assert.IsFalse(s.Album.Contains("/p/a.png"));
            Photos.Take(s, 1, "/p/19.png");
            Assert.AreEqual(Photos.AlbumSize, s.Album.Count, "the same file twice is one entry");

            Assert.AreEqual(1, Photos.Tidy(s, path => path != "/p/19.png"));
            Assert.IsFalse(s.Album.Contains("/p/19.png"));
        }

        [Test]
        public void The_Three_Photo_Quests_Read_The_Counters()
        {
            var defs = Fixture.Defs();
            GameState s = GameState.NewGame(defs);
            var first = new QuestDef { Id = "anh-1", Metric = QuestMetric.PhotosTaken, Target = 1 };
            var gold = new QuestDef { Id = "anh-vang", Metric = QuestMetric.PhotosWithGoldRabbit, Target = 1 };
            var rest = new QuestDef { Id = "anh-nghi", Metric = QuestMetric.PhotosWithFullRest, Target = 1 };
            Assert.IsFalse(Quests.IsComplete(first, defs, s));
            Photos.Take(s, 1, null);
            Assert.IsTrue(Quests.IsComplete(first, defs, s));
            Assert.IsFalse(Quests.IsComplete(gold, defs, s));
            Assert.IsFalse(Quests.IsComplete(rest, defs, s));
            s.Rabbits.Add(new Rabbit { Id = 1, Tier = 5, Name = "R" });
            Photos.Take(s, 1, null);
            Assert.IsTrue(Quests.IsComplete(gold, defs, s), "a red rabbit is gold or better");
            Assert.IsFalse(Quests.IsComplete(rest, defs, s));
            s.RestSpots = RestSpots.Max;
            Photos.Take(s, 1, null);
            Assert.IsTrue(Quests.IsComplete(rest, defs, s));
        }

        [Test]
        public void The_Save_Keeps_The_Album_And_The_Photo_Counters()
        {
            var defs = Fixture.Defs();
            GameState s = GameState.NewGame(defs);
            s.Rabbits.Add(new Rabbit { Id = 1, Tier = 4, Name = "G" });
            s.RestSpots = RestSpots.Max;
            Photos.Take(s, 1, "/p/one.png");
            Photos.Take(s, 1, "/p/two with space.png");
            GameState r = SaveCodec.Decode(defs, SaveCodec.Encode(defs, s)).State;
            CollectionAssert.AreEqual(s.Album, r.Album);
            Assert.AreEqual(2, r.PhotosWithGoldRabbit);
            Assert.AreEqual(2, r.PhotosWithFullRest);
            GameState old = SaveCodec.Decode(defs, "{\"version\":12,\"stations\":[],\"photosTaken\":3}").State;
            Assert.AreEqual(0, old.Album.Count);
            Assert.AreEqual(0, old.PhotosWithGoldRabbit);
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
