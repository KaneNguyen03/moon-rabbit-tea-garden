using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using VuonTra.Game;

namespace VuonTra.Core.Tests
{
    /// <summary>Checks the shipped content, so a bad edit to an asset fails here instead of in play.</summary>
    public class CatalogTests
    {
        GameCatalog catalog;

        [SetUp]
        public void SetUp()
        {
            catalog = AssetDatabase.LoadAssetAtPath<GameCatalog>("Assets/VuonTra/Content/GameCatalog.asset");
            Assert.IsNotNull(catalog, "Run VuonTra > Seed Content first.");
        }

        [Test]
        public void Has_Twelve_Stations_In_Unlock_Order()
        {
            var ids = new List<string>();
            foreach (StationData s in catalog.Stations) ids.Add(s.Id);
            CollectionAssert.AreEqual(
                new[] { "am-tra", "gio-la", "noi-mut", "coi-hoa", "lo-banh", "to-mat", "tra-sua", "binh-sao", "gian-nho", "tiem-banh", "suoi-hoa", "dai-sao" }, ids);
        }

        [Test]
        public void Ids_Are_Unique_And_First_Station_Earns()
        {
            var seen = new HashSet<string>();
            foreach (StationData s in catalog.Stations) Assert.IsTrue(seen.Add(s.Id), "Duplicate id " + s.Id);
            Assert.AreEqual(StationKind.Brew, catalog.Stations[0].Kind);
        }

        [Test]
        public void Numbers_Are_Playable()
        {
            foreach (StationData s in catalog.Stations)
            {
                Assert.Greater(s.CostGrowth, 1d, s.Id);
                Assert.Greater(s.BaseCost, 0d, s.Id);
                if (s.Kind == StationKind.Brew)
                {
                    Assert.Greater(s.BaseIncome, 0d, s.Id);
                    Assert.Greater(s.CycleSeconds, 0d, s.Id);
                }
            }
        }

        [Test]
        public void Ingredients_Boost_Real_Brew_Stations()
        {
            var brew = new HashSet<StationData>();
            foreach (StationData s in catalog.Stations)
                if (s.Kind == StationKind.Brew) brew.Add(s);
            foreach (StationData s in catalog.Stations)
            {
                if (s.Kind != StationKind.Ingredient) continue;
                Assert.IsNotEmpty(s.Boosts, s.Id);
                foreach (StationData target in s.Boosts) Assert.IsTrue(brew.Contains(target), s.Id + " boosts a non-brew station");
            }
        }

        [Test]
        public void Every_Station_And_Shared_Slot_Has_Art()
        {
            foreach (StationData s in catalog.Stations) Assert.IsNotNull(s.Prop, s.Id);
            Assert.IsNotNull(catalog.Rabbit);
            // A station either has its own dressed rabbit or wears the shared tinted scarf.
            // The four newest stations have no costume drawn yet and show the plain shared rabbit.
            // Remove an id from this list when its rabbit_<id>.png lands in Art/Final.
            var awaitingArt = new HashSet<string> { "gian-nho", "tiem-banh", "suoi-hoa", "dai-sao" };
            foreach (StationData s in catalog.Stations)
                if (s.Rabbit == null && !awaitingArt.Contains(s.Id))
                    Assert.IsNotNull(catalog.Scarf, s.Id + " has neither its own rabbit nor a scarf");
            Assert.IsNotNull(catalog.Panel);
            Assert.IsNotNull(catalog.Round);
            Assert.IsNotNull(catalog.Background);
        }

        [Test]
        public void Defs_Carry_The_Boost_Links()
        {
            List<StationDef> defs = catalog.BuildDefs();
            Assert.AreEqual(12, defs.Count);
            Assert.IsTrue(defs[1].Boosts("am-tra"));
            Assert.IsFalse(defs[1].Boosts("noi-mut"));
        }
    }
}
