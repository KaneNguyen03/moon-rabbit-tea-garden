using System.Collections.Generic;
using NUnit.Framework;

namespace VuonTra.Core.Tests
{
    /// <summary>A kind that comes with something on its head or at its neck wears nothing more there.</summary>
    public class WearTests
    {
        List<StationDef> defs;
        GameState s;

        [SetUp]
        public void SetUp()
        {
            defs = Fixture.Defs();
            s = GameState.NewGame(defs);
        }

        Rabbit Add(string kindId)
        {
            RabbitKind kind = RabbitKinds.Find(kindId);
            var r = new Rabbit { Id = ++s.NextRabbitId, Tier = kind.Tier, Kind = kind.Id, Name = "R" + s.NextRabbitId };
            s.Rabbits.Add(r);
            return r;
        }

        Item Grant(ItemSlot slot)
        {
            var def = new ItemDef { Id = slot == ItemSlot.Hat ? "hat-non-la" : "chuong", Name = "x", Slot = slot, Effect = ItemEffect.None };
            return Items.Grant(s, def, 0);
        }

        [TestCase("tho-noel", false, false)]       // its own Santa hat and scarf
        [TestCase("tho-gac", false, true)]         // antlers
        [TestCase("tho-rong", false, true)]        // horns
        [TestCase("tho-mat-troi", false, true)]    // a flame
        [TestCase("tho-hang-nga", false, false)]   // a moon on the brow, a pendant
        [TestCase("tho-ao-dai", true, false)]      // the dress's collar
        [TestCase("tho-bong", true, true)]
        [TestCase("tho-den-ong-sao", true, true)]  // holds a lantern, wears nothing
        public void A_Kind_With_Its_Own_Things_Cannot_Wear_More_There(string kindId, bool hat, bool charm)
        {
            Rabbit r = Add(kindId);
            Assert.AreEqual(hat, Items.CanWear(r, ItemSlot.Hat), "hat");
            Assert.AreEqual(charm, Items.CanWear(r, ItemSlot.Charm), "charm");
            Item h = Grant(ItemSlot.Hat), c = Grant(ItemSlot.Charm);
            Assert.AreEqual(hat, Items.Equip(s, h.Id, r.Id));
            Assert.AreEqual(charm, Items.Equip(s, c.Id, r.Id));
            Assert.AreEqual(hat ? r.Id : 0, h.WornBy);
            Assert.AreEqual(charm ? r.Id : 0, c.WornBy);
        }

        [Test]
        public void An_Old_Save_With_A_Hat_On_A_Kind_That_Has_Its_Own_Puts_It_Back_In_The_Bag()
        {
            Rabbit noel = Add("tho-noel"), bong = Add("tho-bong");
            Item onNoel = Grant(ItemSlot.Hat), scarfed = Grant(ItemSlot.Charm), onBong = Grant(ItemSlot.Hat);
            onNoel.WornBy = noel.Id; scarfed.WornBy = noel.Id; // as an old build allowed
            onBong.WornBy = bong.Id;
            GameState back = SaveCodec.Decode(defs, SaveCodec.Encode(defs, s)).State;
            Assert.AreEqual(3, back.Items.Count, "nothing is lost");
            Assert.AreEqual(0, Items.Find(back, onNoel.Id).WornBy, "the hat comes off the Santa hat");
            Assert.AreEqual(0, Items.Find(back, scarfed.Id).WornBy, "the charm comes off the scarf");
            Assert.AreEqual(bong.Id, Items.Find(back, onBong.Id).WornBy, "everyone else keeps theirs");
        }

        [Test]
        public void Merging_Into_A_Kind_With_Antlers_Takes_The_Hat_Off_And_Keeps_The_Charm()
        {
            IReadOnlyList<RabbitKind> tier3 = RabbitKinds.OfTier(3);
            int antlers = -1;
            for (int i = 0; i < tier3.Count; i++) if (tier3[i].Id == "tho-gac") antlers = i;
            Assert.GreaterOrEqual(antlers, 0);
            var three = new List<Rabbit>();
            for (int i = 0; i < 3; i++) three.Add(Add("tho-suong"));
            Item hat = Grant(ItemSlot.Hat), charm = Grant(ItemSlot.Charm);
            Assert.IsTrue(Items.Equip(s, hat.Id, three[0].Id));
            Assert.IsTrue(Items.Equip(s, charm.Id, three[1].Id));
            Rabbit merged = Rabbits.Merge(s, 2, (antlers + 0.5) / tier3.Count);
            Assert.AreEqual("tho-gac", merged.Kind);
            Assert.AreEqual(0, hat.WornBy, "the hat goes to the bag");
            Assert.AreEqual(merged.Id, charm.WornBy, "the charm moves over");
        }

        [Test]
        public void Every_Kind_Has_A_Head_Inside_Its_Drawing()
        {
            foreach (RabbitKind k in RabbitKinds.All)
            {
                Assert.That(k.HeadX, Is.InRange(0.3f, 0.7f), k.Id + " across");
                Assert.That(k.HeadTop, Is.InRange(0.6f, 0.85f), k.Id + " top of the skull");
                Assert.That(k.HeadWidth, Is.InRange(0.28f, 0.46f), k.Id + " width");
            }
        }
    }
}
