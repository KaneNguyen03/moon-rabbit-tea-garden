using System.Collections.Generic;
using NUnit.Framework;

namespace VuonTra.Core.Tests
{
    /// <summary>Decorating the garden (#11): four things bought at the shop for golden carrots, eight fixed spots to stand them on, and one colour for every paper lantern.</summary>
    public class DecorTests
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
        public void Four_Kinds_At_Their_Prices_Eight_Spots_And_Four_Lantern_Colours()
        {
            Assert.AreEqual(4, Decor.Kinds.Length);
            Assert.AreEqual(8, Decor.Spots);
            Assert.AreEqual(15, Decor.Find("den-da").Price);
            Assert.AreEqual(20, Decor.Find("bon-hoa").Price);
            Assert.AreEqual(30, Decor.Find("ghe-nam").Price);
            Assert.AreEqual(40, Decor.Find("tham-picnic").Price);
            Assert.AreEqual("decor_den-da", Decor.Find("den-da").Art, "drawn from the art already in the game");
            Assert.IsNull(Decor.Find("no-such-thing"));
            Assert.AreEqual(4, Decor.LanternColors.Length);
            foreach (string hex in Decor.LanternColors) Assert.AreEqual(6, hex.Length, "a six-digit colour: " + hex);
            Assert.AreEqual(10, Decor.LanternColorCost);
            Assert.AreEqual(Decor.Kinds.Length, s.DecorOwned.Length);
            Assert.AreEqual(Decor.Spots, s.DecorSpots.Length);
            Assert.AreEqual(0, s.LanternColor, "paper to begin with");
            Assert.AreEqual(0, Decor.TotalOwned(s));
            Assert.IsFalse(Decor.ShowEmptySpots(s), "a newcomer sees no clutter");
        }

        [Test]
        public void Buying_Pays_The_Price_Can_Repeat_And_Stops_At_Eight_In_All()
        {
            s.GoldenCarrots = 14;
            Assert.IsFalse(Decor.Buy(s, "den-da"), "one short");
            Assert.AreEqual(14, s.GoldenCarrots);
            s.GoldenCarrots = 15;
            Assert.IsTrue(Decor.Buy(s, "den-da"));
            Assert.AreEqual(0, s.GoldenCarrots, "the price was paid");
            Assert.AreEqual(1, Decor.Owned(s, "den-da"));
            Assert.AreEqual(1, Decor.Available(s, "den-da"), "owned and not yet placed");
            Assert.IsTrue(Decor.ShowEmptySpots(s), "now the empty spots show");
            s.GoldenCarrots = 100;
            Assert.IsTrue(Decor.Buy(s, "den-da"), "the same kind can be bought again");
            Assert.AreEqual(2, Decor.Owned(s, "den-da"));
            Assert.IsFalse(Decor.Buy(s, "no-such-thing"));
            Assert.AreEqual(85, s.GoldenCarrots);

            s.GoldenCarrots = 1000;
            for (int n = Decor.TotalOwned(s); n < Decor.Spots; n++) Assert.IsTrue(Decor.Buy(s, "bon-hoa"), "piece " + n);
            Assert.AreEqual(Decor.Spots, Decor.TotalOwned(s));
            Assert.IsFalse(Decor.Buy(s, "tham-picnic"), "no more than there are spots");
            Assert.AreEqual(1000 - 6 * 20, s.GoldenCarrots, "nothing paid for nothing");
        }

        [Test]
        public void Placing_Needs_An_Owned_Unplaced_Piece_And_An_Empty_Spot_Storing_Frees_It_And_Swapping_Does_Both()
        {
            Assert.IsFalse(Decor.Place(s, 0, "den-da"), "nothing owned");
            s.GoldenCarrots = 100;
            Decor.Buy(s, "den-da");
            Decor.Buy(s, "ghe-nam");
            Assert.IsFalse(Decor.Place(s, -1, "den-da"));
            Assert.IsFalse(Decor.Place(s, Decor.Spots, "den-da"));
            Assert.IsTrue(Decor.Place(s, 3, "den-da"));
            Assert.AreEqual("den-da", s.DecorSpots[3]);
            Assert.AreEqual(0, Decor.Available(s, "den-da"), "the one owned is out");
            Assert.AreEqual(1, Decor.Placed(s));
            Assert.IsFalse(Decor.Place(s, 5, "den-da"), "no second lantern to place");
            Assert.IsFalse(Decor.Place(s, 3, "ghe-nam"), "the spot is taken");
            Assert.IsNull(s.DecorSpots[5]);
            Assert.AreEqual(1, Decor.Available(s, "ghe-nam"));
            Assert.IsTrue(Decor.ShowEmptySpots(s), "the chair still waits in storage: the empty spots show");
            Assert.IsTrue(Decor.Place(s, 5, "ghe-nam"));
            Assert.IsFalse(Decor.ShowEmptySpots(s), "every owned piece stands somewhere: no '+' to tap");
            Assert.IsTrue(Decor.Store(s, 5));

            Assert.IsTrue(Decor.Swap(s, 3, "ghe-nam"), "the chair takes the lantern's place");
            Assert.AreEqual("ghe-nam", s.DecorSpots[3]);
            Assert.AreEqual(1, Decor.Available(s, "den-da"), "the lantern is back in storage");
            Assert.AreEqual(0, Decor.Available(s, "ghe-nam"));
            Assert.IsFalse(Decor.Swap(s, 3, "ghe-nam"), "already there");
            Assert.IsFalse(Decor.Swap(s, 3, "tham-picnic"), "none owned");
            Assert.AreEqual("ghe-nam", s.DecorSpots[3], "a failed swap leaves the spot as it was");
            Assert.IsTrue(Decor.Swap(s, 6, "den-da"), "on an empty spot a swap is a placing");
            Assert.AreEqual("den-da", s.DecorSpots[6]);

            Assert.IsTrue(Decor.Store(s, 3));
            Assert.IsNull(s.DecorSpots[3]);
            Assert.AreEqual(1, Decor.Available(s, "ghe-nam"));
            Assert.IsFalse(Decor.Store(s, 3), "nothing there now");
            Assert.IsFalse(Decor.Store(s, 99));
            Assert.AreEqual(100 - 15 - 30, s.GoldenCarrots, "placing and storing cost nothing");
        }

        [Test]
        public void A_Lantern_Colour_Costs_Ten_Each_Time_But_Not_The_Colour_Already_Hung()
        {
            s.GoldenCarrots = 9;
            Assert.IsFalse(Decor.SetLanternColor(s, 2), "one short");
            Assert.AreEqual(0, s.LanternColor);
            s.GoldenCarrots = 10;
            Assert.IsFalse(Decor.SetLanternColor(s, 0), "paper is what hangs already");
            Assert.IsFalse(Decor.SetLanternColor(s, 4), "no such colour");
            Assert.IsFalse(Decor.SetLanternColor(s, -1));
            Assert.AreEqual(10, s.GoldenCarrots);
            Assert.IsTrue(Decor.SetLanternColor(s, 2));
            Assert.AreEqual(2, s.LanternColor);
            Assert.AreEqual(0, s.GoldenCarrots);
            Assert.IsFalse(Decor.SetLanternColor(s, 2), "the colour already hung is not sold again");
            s.GoldenCarrots = 25;
            Assert.IsTrue(Decor.SetLanternColor(s, 0), "back to paper costs like any colour");
            Assert.AreEqual(15, s.GoldenCarrots);
        }

        [Test]
        public void The_Save_Keeps_What_Is_Owned_Where_It_Stands_And_The_Lantern_Colour_And_Old_Saves_Open_Bare()
        {
            s.GoldenCarrots = 200;
            Decor.Buy(s, "den-da"); Decor.Buy(s, "den-da"); Decor.Buy(s, "tham-picnic");
            Decor.Place(s, 1, "den-da");
            Decor.Place(s, 7, "tham-picnic");
            Decor.SetLanternColor(s, 3);
            GameState back = SaveCodec.Decode(defs, SaveCodec.Encode(defs, s)).State;
            Assert.AreEqual(2, Decor.Owned(back, "den-da"));
            Assert.AreEqual(1, Decor.Owned(back, "tham-picnic"));
            Assert.AreEqual(0, Decor.Owned(back, "bon-hoa"));
            Assert.AreEqual("den-da", back.DecorSpots[1]);
            Assert.AreEqual("tham-picnic", back.DecorSpots[7]);
            Assert.IsNull(back.DecorSpots[0]);
            Assert.AreEqual(1, Decor.Available(back, "den-da"));
            Assert.AreEqual(3, back.LanternColor);
            Assert.AreEqual(SaveCodec.CurrentVersion, 14, "an additive block: no new version");

            GameState old = SaveCodec.Decode(defs, "{\"version\":14,\"stations\":[]}").State;
            Assert.AreEqual(0, Decor.TotalOwned(old), "a save from before: nothing owned");
            foreach (string spot in old.DecorSpots) Assert.IsNull(spot);
            Assert.AreEqual(0, old.LanternColor);
            Assert.IsFalse(Decor.ShowEmptySpots(old));

            // A save that was fiddled with: a kind the shop lacks is dropped, more placed than owned is put away, an unknown colour is paper.
            const string odd = "{\"version\":14,\"stations\":[],\"decor\":{\"owned\":{\"den-da\":1,\"mystery\":5,\"bon-hoa\":-2},\"spots\":[\"den-da\",\"den-da\",\"mystery\",null,\"bon-hoa\"],\"lantern\":9}}";
            GameState fixedUp = SaveCodec.Decode(defs, odd).State;
            Assert.AreEqual(1, Decor.Owned(fixedUp, "den-da"));
            Assert.AreEqual(0, Decor.Owned(fixedUp, "bon-hoa"));
            Assert.AreEqual("den-da", fixedUp.DecorSpots[0]);
            Assert.IsNull(fixedUp.DecorSpots[1], "only one lantern is owned");
            Assert.IsNull(fixedUp.DecorSpots[2]);
            Assert.IsNull(fixedUp.DecorSpots[4], "no planter is owned");
            Assert.AreEqual(0, fixedUp.LanternColor);
            Assert.AreEqual(Decor.Spots, fixedUp.DecorSpots.Length);
        }
    }
}
