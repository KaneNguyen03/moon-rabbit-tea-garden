using System.Collections.Generic;
using NUnit.Framework;

namespace VuonTra.Core.Tests
{
    /// <summary>The new moon (#1): when it opens, what it keeps and takes, what it gives, and that it lasts.</summary>
    public class NewMoonTests
    {
        List<StationDef> defs;
        GameState s;

        [SetUp]
        public void SetUp()
        {
            defs = Fixture.Defs();
            s = GameState.NewGame(defs);
        }

        static void SetGardenLevel(GameState s, int level) => s.GardenXp = Garden.XpForLevel(level);

        [Test]
        public void Opens_At_Garden_Level_Thirty_And_Asks_Five_More_Each_Season()
        {
            SetGardenLevel(s, 29);
            Assert.IsFalse(NewMoon.CanBegin(s));
            SetGardenLevel(s, 30);
            Assert.IsTrue(NewMoon.CanBegin(s));

            s.Season = 1;
            Assert.AreEqual(35, NewMoon.LevelNeeded(1));
            SetGardenLevel(s, 34);
            Assert.IsFalse(NewMoon.CanBegin(s));
            SetGardenLevel(s, 35);
            Assert.IsTrue(NewMoon.CanBegin(s));
            Assert.AreEqual(60, NewMoon.LevelNeeded(6), "the seventh season asks for level 60");
        }

        [Test]
        public void The_Seventh_Season_Is_The_Last()
        {
            s.Season = NewMoon.MaxSeasons;
            SetGardenLevel(s, 99);
            Assert.IsTrue(NewMoon.AtCap(s));
            Assert.IsFalse(NewMoon.CanBegin(s));
            Assert.IsFalse(NewMoon.Begin(defs, s, "Non", out Rabbit none));
            Assert.IsNull(none);
            Assert.AreEqual(NewMoon.MaxSeasons, s.Season);
        }

        [Test]
        public void Nothing_Changes_When_The_Garden_Is_Not_Far_Enough()
        {
            s.Coins = new BigNum(500);
            s.Levels[1] = 7;
            Assert.IsFalse(NewMoon.Begin(defs, s, "Non", out _));
            Assert.AreEqual(0, s.Season);
            Assert.AreEqual(500d, s.Coins.ToDouble(), 1e-9);
            Assert.AreEqual(7, s.Levels[1]);
        }

        GameState GrownGarden()
        {
            var g = GameState.NewGame(defs);
            SetGardenLevel(g, 31);
            g.Coins = new BigNum(123456);
            g.Levels[0] = 120; g.Levels[1] = 40; g.Levels[2] = 15;
            g.Plots = 3;
            g.RestSpots = 4;
            g.Clovers = 2;
            g.GoldenCarrots = 9;
            Rabbits.GiftFirst(g, "Bông");
            Rabbit worker = Rabbits.Arrive(g, 2, "Lam", 0d);
            Rabbits.Assign(g, worker.Id, 2);
            Rabbit rester = Rabbits.Arrive(g, 4, "Vàng", 0d);
            Rabbits.Assign(g, rester.Id, -1);
            g.Items.Add(new Item { Id = ++g.NextItemId, DefId = "mu", Name = "Mũ", Tier = 1 });
            g.Tray.Add(new Product { Station = 0 });
            g.Leaves.Plays = 3;
            g.PoundingPlays = 2;
            return g;
        }

        [Test]
        public void Beginning_Keeps_Rabbits_Items_And_Rare_Currencies_And_Resets_The_Garden()
        {
            GameState g = GrownGarden();
            int rabbits = g.Rabbits.Count, items = g.Items.Count, clovers = g.Clovers, golden = g.GoldenCarrots;
            HashSet<string> album = new HashSet<string>(g.SeenKinds);
            int levelBefore = Garden.Level(g);
            Assert.AreEqual(31, levelBefore);

            Assert.IsTrue(NewMoon.Begin(defs, g, "Non", out Rabbit gift));

            Assert.AreEqual(1, g.Season);
            Assert.IsTrue(g.Coins.IsZero, "carrots go back to nothing");
            Assert.AreEqual(1, g.Levels[0], "only the teapot is open, at level 1");
            Assert.AreEqual(0, g.Levels[1]);
            Assert.AreEqual(0, g.Levels[2]);
            Assert.AreEqual(System.Math.Min(defs.Count, Plots.Starting), g.Plots);
            Assert.AreEqual(RestSpots.Starting, g.RestSpots);
            Assert.AreEqual(1, Garden.Level(g));
            Assert.AreEqual(0, g.Tray.Count);
            Assert.AreEqual(0, g.Leaves.Plays);
            Assert.AreEqual(0, g.PoundingPlays);

            Assert.AreEqual(rabbits + 1, g.Rabbits.Count, "every rabbit stays, and the moon rabbit joins");
            Assert.AreEqual(items, g.Items.Count);
            Assert.AreEqual(clovers + NewMoon.GiftClovers, g.Clovers);
            Assert.AreEqual(golden + NewMoon.GiftGolden, g.GoldenCarrots);
            foreach (string kind in album) Assert.IsTrue(g.SeenKinds.Contains(kind), "the album keeps " + kind);
            Assert.IsNotNull(gift);
            Assert.AreEqual(NewMoon.MoonKind, gift.Kind);
            Assert.AreEqual(3, gift.Tier, "the moon rabbit is purple");
        }

        [Test]
        public void Rabbits_Whose_Station_Closed_Go_To_Rest_And_None_Is_Lost()
        {
            GameState g = GrownGarden();
            Rabbit first = g.Rabbits[0];
            Assert.AreEqual(0, first.Station);
            NewMoon.Begin(defs, g, "Non", out _);
            Assert.AreEqual(0, first.Station, "the teapot's rabbit keeps its place");
            foreach (Rabbit r in g.Rabbits)
                Assert.IsTrue(r.Station <= 0, r.Name + " cannot work at a closed station");
            // More resting rabbits than places: they are all still here, only not all out in the garden.
            Assert.LessOrEqual(RestSpots.Seated(g).Count, RestSpots.Starting);
            Assert.AreEqual(4, g.Rabbits.Count);
        }

        [Test]
        public void The_Moon_Rabbit_Comes_Once_And_Only_By_New_Moon()
        {
            GameState g = GrownGarden();
            NewMoon.Begin(defs, g, "Non", out Rabbit first);
            Assert.IsNotNull(first);
            SetGardenLevel(g, 40);
            Assert.IsTrue(NewMoon.Begin(defs, g, "Non", out Rabbit second));
            Assert.IsNull(second, "the second season gives no second moon rabbit");
            Assert.AreEqual(2, g.Season);

            for (int tier = 0; tier < Rabbits.TierCount; tier++)
                foreach (RabbitKind k in RabbitKinds.OfTier(tier))
                    Assert.AreNotEqual(NewMoon.MoonKind, k.Id, "never from clovers, the shop or a merge");
            for (int i = 0; i < 100; i++)
                Assert.AreNotEqual(NewMoon.MoonKind, RabbitKinds.Pick(3, i / 100d));
            Assert.IsTrue(RabbitKinds.Find(NewMoon.MoonKind).NewMoonOnly);
            Assert.AreEqual(4, RabbitKinds.Special().Count, "three seasons' kinds and the moon rabbit");
        }

        [Test]
        public void Each_Season_Adds_A_Quarter_To_All_Income_Up_To_The_Seventh()
        {
            Assert.AreEqual(1d, NewMoon.Bonus(0), 1e-12);
            Assert.AreEqual(1.25d, NewMoon.Bonus(1), 1e-12);
            Assert.AreEqual(2d, NewMoon.Bonus(4), 1e-12);
            Assert.AreEqual(2.75d, NewMoon.Bonus(7), 1e-12);
            Assert.AreEqual(2.75d, NewMoon.Bonus(9), 1e-12, "nothing past the last season");

            s.Season = 1;
            Modifiers m = Modifiers.From(null, s);
            Assert.AreEqual(1.25d, m.IncomeMultiplier, 1e-12);

            // The bonus pays on a station's cycle like any other multiplier.
            var plain = GameState.NewGame(defs);
            var season = GameState.NewGame(defs);
            season.Season = 1;
            Simulation.Tick(defs, plain, 30d, null, Modifiers.From(null, plain));
            Simulation.Tick(defs, season, 30d, null, Modifiers.From(null, season));
            Assert.AreEqual(plain.Coins.ToDouble() * 1.25d, season.Coins.ToDouble(), 1e-6);
        }

        [Test]
        public void Save_Keeps_The_Season_And_Old_Saves_Are_Season_Zero()
        {
            GameState g = GrownGarden();
            NewMoon.Begin(defs, g, "Non", out _);
            string json = SaveCodec.Encode(defs, g);
            StringAssert.Contains("\"season\":1", json);
            LoadResult back = SaveCodec.Decode(defs, json);
            Assert.AreEqual(LoadStatus.Ok, back.Status);
            Assert.AreEqual(1, back.State.Season);
            Assert.AreEqual(1.25d * RestSpots.Multiplier(back.State), Modifiers.From(null, back.State).IncomeMultiplier, 1e-12, "the season's quarter, times the seated rabbits' share (#60)");

            LoadResult old = SaveCodec.Decode(defs, json.Replace(",\"season\":1", ""));
            Assert.AreEqual(LoadStatus.Ok, old.Status);
            Assert.AreEqual(0, old.State.Season, "a save from before the new moon is on its first season");
        }

        [Test]
        public void The_Compare_Card_Reads_The_Saves_Own_Numbers()
        {
            GameState g = GrownGarden();
            NewMoonPreview p = NewMoon.Compare(defs, g);
            Assert.AreEqual(1, p.SeasonAfter);
            Assert.AreEqual(3, p.Rabbits);
            Assert.AreEqual(1, p.Items);
            Assert.AreEqual(2, p.Clovers);
            Assert.AreEqual(9, p.GoldenCarrots);
            Assert.AreEqual(123456d, p.Coins.ToDouble(), 1e-9);
            Assert.AreEqual(175, p.StationLevels);
            Assert.AreEqual(3, p.StationsOpen);
            Assert.AreEqual(3, p.Plots);
            Assert.AreEqual(4, p.RestSpots);
            Assert.AreEqual(31, p.GardenLevel);
            Assert.AreEqual(1d, p.BonusBefore, 1e-12);
            Assert.AreEqual(1.25d, p.BonusAfter, 1e-12);
            Assert.IsTrue(p.MoonRabbit);
            NewMoon.Begin(defs, g, "Non", out _);
            Assert.IsFalse(NewMoon.Compare(defs, g).MoonRabbit);
        }

        [Test]
        public void A_Merge_Never_Uses_Up_The_Moon_Rabbit()
        {
            GameState g = GrownGarden();
            NewMoon.Begin(defs, g, "Non", out Rabbit moon);
            Assert.IsNotNull(moon);
            int tier = moon.Tier;
            // Two plain rabbits of the moon rabbit's tier: with the moon rabbit that would be three, but it does not count.
            Rabbits.ArriveKind(g, RabbitKinds.OfTier(tier)[0].Id, "A");
            Rabbits.ArriveKind(g, RabbitKinds.OfTier(tier)[0].Id, "B");
            Assert.AreEqual(2, Rabbits.CountOfTier(g, tier), "the moon rabbit is not merge material");
            Assert.IsFalse(Rabbits.CanMerge(g, tier));

            Rabbits.ArriveKind(g, RabbitKinds.OfTier(tier)[0].Id, "C");
            Assert.IsTrue(Rabbits.CanMerge(g, tier));
            Rabbit merged = Rabbits.Merge(g, tier, 0.5);
            Assert.IsNotNull(merged);
            Assert.IsTrue(g.Rabbits.Contains(moon), "the one-time rabbit stays");
            Assert.AreEqual(tier + 1, merged.Tier);
        }
    }
}
