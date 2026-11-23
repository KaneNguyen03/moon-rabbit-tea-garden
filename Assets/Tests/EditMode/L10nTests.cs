using NUnit.Framework;
using VuonTra.EditorTools;
using VuonTra.Game;

namespace VuonTra.Core.Tests
{
    public class L10nTests
    {
        [TearDown]
        public void TearDown()
        {
            L10n.SetLocale("vi");
        }

        [Test]
        public void Reads_Both_Languages()
        {
            Assert.IsTrue(L10n.SetLocale("vi"));
            Assert.AreEqual("Tạm nghỉ", L10n.T("pause.title"));
            Assert.AreEqual("Ấm trà sương", L10n.Station("am-tra"));
            Assert.IsTrue(L10n.SetLocale("en"));
            Assert.AreEqual("en", L10n.Code);
            Assert.AreEqual("Paused", L10n.T("pause.title"));
            Assert.AreEqual("Dew Teapot", L10n.Station("am-tra"));
        }

        [Test]
        public void Fills_In_Arguments()
        {
            L10n.SetLocale("vi");
            Assert.AreEqual("Nâng +10 · 3,88K", L10n.T("quick.upgrade", 10, "3,88K"));
            L10n.SetLocale("en");
            Assert.AreEqual("Up +10 · 3.88K", L10n.T("quick.upgrade", 10, L10n.Number("3,88K")));
        }

        [Test]
        public void Numbers_Use_The_Languages_Decimal_Mark()
        {
            L10n.SetLocale("vi");
            Assert.AreEqual("12,3K", new BigNum(12345).Short());
            L10n.SetLocale("en");
            Assert.AreEqual("12.3K", new BigNum(12345).Short());
        }

        [Test]
        public void Unknown_Language_Is_Refused()
        {
            Assert.IsFalse(L10n.SetLocale("xx"));
        }

        [Test]
        public void Language_Change_Raises_The_Event()
        {
            L10n.SetLocale("vi");
            int raised = 0;
            System.Action onChange = () => raised++;
            L10n.Changed += onChange;
            L10n.SetLocale("en");
            L10n.Changed -= onChange;
            Assert.AreEqual(1, raised);
        }

        [Test]
        public void English_Reads_A_Count_Of_One_In_The_Singular()
        {
            L10n.SetLocale("en");
            Assert.AreEqual("+1 golden carrot", L10n.T("shop.gift.amount", 1));
            Assert.AreEqual("+0 golden carrots", L10n.T("shop.gift.amount", 0));
            Assert.AreEqual("+2 golden carrots", L10n.T("shop.gift.amount", 2));
            Assert.AreEqual("+5 golden carrots", L10n.T("shop.gift.amount", 5));
            Assert.AreEqual("1 day left", L10n.T("mail.days", 1));
            Assert.AreEqual("2 days left", L10n.T("mail.days", 2));
            Assert.AreEqual("1 rabbit · 1/72 kinds met", L10n.T("rabbits.count", 1, 1, 72));
            Assert.AreEqual("5 rabbits · 1/72 kinds met", L10n.T("rabbits.count", 5, 1, 72), "only the first number decides");
            Assert.AreEqual("+1 clover", L10n.T("moment.garden.sub", 1));
            Assert.AreEqual("+3 clovers", L10n.T("moment.garden.sub", 3), "several garden levels at once");
            Assert.AreEqual("Lv 1", L10n.T("station.level", 1), "a line with no singular twin is read as it is");
        }

        [Test]
        public void Vietnamese_Has_No_Singular_And_Reads_The_Usual_Line()
        {
            L10n.SetLocale("vi");
            Assert.AreEqual("+1 cà rốt vàng", L10n.T("shop.gift.amount", 1));
            Assert.AreEqual("+2 cà rốt vàng", L10n.T("shop.gift.amount", 2));
            Assert.AreEqual("còn 1 ngày", L10n.T("mail.days", 1));
        }

        [Test]
        public void The_First_Number_Among_The_Arguments_Decides()
        {
            Assert.IsTrue(L10n.FirstCountIsOne(new object[] { 1 }));
            Assert.IsTrue(L10n.FirstCountIsOne(new object[] { "Bông", 1, 5 }), "names are passed over");
            Assert.IsTrue(L10n.FirstCountIsOne(new object[] { 1L }));
            Assert.IsFalse(L10n.FirstCountIsOne(new object[] { 0 }));
            Assert.IsFalse(L10n.FirstCountIsOne(new object[] { 2, 1 }));
            Assert.IsFalse(L10n.FirstCountIsOne(new object[] { "1" }), "text is never a count");
            Assert.IsFalse(L10n.FirstCountIsOne(new object[] { 1.5d }));
        }

        [Test]
        public void Every_Singular_Line_Has_A_Plural_Line_With_The_Same_Blanks()
        {
            var plural = new System.Collections.Generic.Dictionary<string, string>();
            for (int i = 0; i < L10nStrings.All.GetLength(0); i++) plural[L10nStrings.All[i, 0]] = L10nStrings.All[i, 2];
            for (int i = 0; i < L10nStrings.EnglishOne.GetLength(0); i++)
            {
                string key = L10nStrings.EnglishOne[i, 0];
                StringAssert.EndsWith(L10n.OneSuffix, key);
                string of = key.Substring(0, key.Length - L10n.OneSuffix.Length);
                Assert.IsTrue(plural.ContainsKey(of), key + " has no plural line");
                CollectionAssert.AreEquivalent(Blanks(plural[of]), Blanks(L10nStrings.EnglishOne[i, 1]), key);
                Assert.AreNotEqual(plural[of], L10nStrings.EnglishOne[i, 1], key + " says the same as its plural");
            }
        }

        static System.Collections.Generic.List<string> Blanks(string text)
        {
            var found = new System.Collections.Generic.List<string>();
            foreach (System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(text, @"\{\d+\}")) found.Add(m.Value);
            return found;
        }
    }
}
