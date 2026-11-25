using System;
using NUnit.Framework;

namespace VuonTra.Core.Tests
{
    public class BigNumTests
    {
        [Test]
        public void Normalizes()
        {
            var n = BigNum.FromParts(123, 0);
            Assert.AreEqual(1.23, n.Mantissa, 1e-9);
            Assert.AreEqual(2, n.Exponent);
        }

        [Test]
        public void Adds_Across_Magnitudes()
        {
            Assert.AreEqual(1001, (new BigNum(1000) + new BigNum(1)).ToDouble(), 1e-6);
        }

        [Test]
        public void Tiny_Addend_Is_Dropped()
        {
            var big = BigNum.FromParts(1, 400);
            Assert.IsTrue((big + BigNum.One).Equals(big));
        }

        [Test]
        public void Subtract_Clamps_To_Zero()
        {
            Assert.IsTrue((new BigNum(5) - new BigNum(9)).Equals(BigNum.Zero));
            Assert.IsTrue((new BigNum(5) - new BigNum(5)).Equals(BigNum.Zero));
        }

        [Test]
        public void Subtracts()
        {
            Assert.AreEqual(89.1, (new BigNum(100) - new BigNum(10.9)).ToDouble(), 1e-9);
        }

        [Test]
        public void Multiplies_Past_Double_Range()
        {
            var n = BigNum.FromParts(5, 300) * BigNum.FromParts(4, 300);
            Assert.AreEqual(2, n.Mantissa, 1e-9);
            Assert.AreEqual(601, n.Exponent);
        }

        [Test]
        public void Divides()
        {
            Assert.AreEqual(2.5, (new BigNum(10) / 4d).ToDouble(), 1e-9);
            Assert.IsTrue((new BigNum(10) / BigNum.Zero).Equals(BigNum.Zero));
        }

        [Test]
        public void Pow_Matches_Double()
        {
            Assert.AreEqual(Math.Pow(1.09, 50), new BigNum(1.09).Pow(50).ToDouble(), 1e-6);
            Assert.IsTrue(new BigNum(7).Pow(0).Equals(BigNum.One));
        }

        [Test]
        public void Compare_Orders()
        {
            Assert.IsTrue(BigNum.FromParts(9, 5) < BigNum.FromParts(1, 6));
            Assert.IsTrue(BigNum.Zero < new BigNum(0.001));
            Assert.IsTrue(new BigNum(3) >= new BigNum(3));
        }

        [TestCase(0, "0")]
        [TestCase(0.4, "0")]
        [TestCase(7, "7")]
        [TestCase(999.9, "999")]
        [TestCase(1000, "1,00K")]
        [TestCase(12345, "12,3K")]
        [TestCase(123456, "123K")]
        [TestCase(999999, "999K")]
        [TestCase(2.5e6, "2,50M")]
        [TestCase(1e9, "1,00B")]
        [TestCase(1e12, "1,00T")]
        [TestCase(1e15, "1,00aa")]
        [TestCase(1e18, "1,00ab")]
        public void ShortString(double value, string expected)
        {
            Assert.AreEqual(expected, new BigNum(value).ToShortString());
        }

        [Test]
        public void ShortString_Huge_Has_No_Infinity()
        {
            string s = BigNum.FromParts(3, 400).ToShortString();
            StringAssert.DoesNotContain("Infinity", s);
            StringAssert.DoesNotContain("NaN", s);
            StringAssert.StartsWith("30,0", s);
        }

        [Test]
        public void ShortString_Keeps_Going_Past_Two_Letters()
        {
            long exponent = (5L + 676L) * 3L;
            Assert.AreEqual("1,00aaa", BigNum.FromParts(1, exponent).ToShortString());
        }

        [Test]
        public void NaN_Infinity_And_Negatives_Become_Zero()
        {
            Assert.IsTrue(new BigNum(double.NaN).Equals(BigNum.Zero));
            Assert.IsTrue(new BigNum(double.PositiveInfinity).Equals(BigNum.Zero));
            Assert.IsTrue(new BigNum(-4).Equals(BigNum.Zero));
        }

        [Test]
        public void ToDouble_Clamps()
        {
            Assert.AreEqual(double.MaxValue, BigNum.FromParts(1, 5000).ToDouble());
        }

        [TestCase(0d, "0")]
        [TestCase(999d, "999")]
        [TestCase(13912345d, "13.912.345")]
        [TestCase(1000d, "1.000")]
        public void The_Full_Form_Groups_Every_Digit(double value, string text)
        {
            Assert.AreEqual(text, new BigNum(value).ToFullString('.'));
            Assert.AreEqual(text.Replace('.', ','), new BigNum(value).ToFullString(','));
        }

        [Test]
        public void Past_Fifteen_Digits_The_Full_Form_Is_The_Short_One()
        {
            var huge = new BigNum(1e30);
            Assert.AreEqual(huge.ToShortString(), huge.ToFullString('.'));
        }
        [Test]
        public void The_Short_Key_Changes_Exactly_When_The_Short_Form_Does()
        {
            var rng = new System.Random(123);
            var seen = new System.Collections.Generic.Dictionary<long, string>();
            var keys = new System.Collections.Generic.Dictionary<string, long>();
            for (int n = 0; n < 200000; n++)
            {
                BigNum value = n < 3000 ? new BigNum(n * 0.37) : BigNum.FromParts(1d + 9d * rng.NextDouble(), rng.Next(-2, 120));
                if (n % 7 == 0) value = BigNum.FromParts(System.Math.Round(value.Mantissa, 2), value.Exponent);
                string text = value.ToShortString();
                long key = value.ShortKey();
                if (seen.TryGetValue(key, out string before)) Assert.AreEqual(before, text, "one key, one text: " + key);
                else seen[key] = text;
                if (keys.TryGetValue(text, out long had)) Assert.AreEqual(had, key, "one text, one key: " + text);
                else keys[text] = key;
            }
            Assert.AreEqual(BigNum.Zero.ShortKey(), new BigNum(0.4).ShortKey(), "both read 0");
            Assert.AreNotEqual(new BigNum(100d).ShortKey(), new BigNum(10d).ShortKey());
            Assert.AreNotEqual(BigNum.FromParts(1d, 5).ShortKey(), BigNum.FromParts(1d, 4).ShortKey(), "100K and 10,0K");
        }
    }
}
