using NUnit.Framework;
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
    }
}
