using NUnit.Framework;

namespace VuonTra.Core.Tests
{
    public class RarityTests
    {
        [TestCase(0, 1)]
        [TestCase(2, 3)]
        [TestCase(5, 6)]
        [TestCase(-3, 1)]
        [TestCase(99, 6)]
        public void Each_Tier_Wears_One_More_Star_Than_The_Last_And_Stays_On_The_Scale(int tier, int stars)
        {
            Assert.AreEqual(stars, Rarity.Stars(tier));
            Assert.AreEqual(new string('★', stars), Rarity.Marks(tier));
        }

        [Test]
        public void Rabbits_And_Items_Use_The_Same_Six_Tiers()
        {
            Assert.AreEqual(Rarity.Count, Rabbits.TierCount);
            Assert.AreEqual(Rarity.Count, Rabbits.Multipliers.Length);
            Assert.AreEqual(Rarity.Count, Rabbits.LevelCaps.Length);
            Assert.AreEqual(Rarity.Count, Items.TierScale.Length);
            Assert.AreEqual(Rarity.Count, RabbitKinds.OfTier(Rarity.Count - 1).Count > 0 ? Rarity.Count : -1, "the rarest tier has kinds");
        }
    }
}
