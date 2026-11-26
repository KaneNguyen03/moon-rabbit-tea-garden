using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using VuonTra.EditorTools;
using VuonTra.Game;
using VuonTra.Game.UI;
using L10n = VuonTra.Game.L10n;

namespace VuonTra.Core.Tests
{
    public partial class AccessibilityTests
    {
        const double NormalText = 4.5, LargeBoldText = 3.0;
        const int LargeBoldFrom = 24;

        static IEnumerable<TestCaseData> TextOnBackground()
        {
            TestCaseData Pair(string name, Color ink, Color back, double need) => new TestCaseData(ink, back, need).SetName("Contrast_" + name);
            yield return Pair("Ink_On_Paper", Palette.Ink, Palette.Paper, NormalText);
            yield return Pair("Ink_On_Card", Palette.Ink, Palette.Card, NormalText);
            yield return Pair("Ink_On_CardLip", Palette.Ink, Palette.CardLip, NormalText);
            yield return Pair("Ink_On_Track", Palette.Ink, Palette.Track, NormalText);
            yield return Pair("Ink_On_OffLip", Palette.Ink, Palette.OffLip, NormalText);
            yield return Pair("Ink_On_Carrot", Palette.Ink, Palette.Carrot, NormalText);
            yield return Pair("Ink_On_Lantern", Palette.Ink, Palette.Lantern, NormalText);
            yield return Pair("Ink_On_Moss", Palette.Ink, Palette.Moss, NormalText);
            yield return Pair("Ink_On_Blush", Palette.Ink, Palette.Blush, NormalText);
            yield return Pair("OnCarrot_On_Carrot", Palette.OnCarrot, Palette.Carrot, NormalText);
            yield return Pair("InkSoft_On_Paper", Palette.InkSoft, Palette.Paper, NormalText);
            yield return Pair("InkSoft_On_Card", Palette.InkSoft, Palette.Card, NormalText);
            yield return Pair("CarrotInk_On_Paper", Palette.CarrotInk, Palette.Paper, NormalText);
            yield return Pair("CarrotInk_On_Card", Palette.CarrotInk, Palette.Card, NormalText);
            yield return Pair("MossInk_On_Paper", Palette.MossInk, Palette.Paper, NormalText);
            yield return Pair("MossInk_On_Card", Palette.MossInk, Palette.Card, NormalText);
            yield return Pair("Paper_On_Night", Palette.Paper, Palette.Night, NormalText);
            yield return Pair("Card_On_Night", Palette.Card, Palette.Night, NormalText);
            yield return Pair("Lantern_On_Night", Palette.Lantern, Palette.Night, NormalText);
            yield return Pair("Carrot_On_Night", Palette.Carrot, Palette.Night, NormalText);
            yield return Pair("LargeBold_InkSoft_On_Track", Palette.InkSoft, Palette.Track, LargeBoldText);
            yield return Pair("LargeBold_CarrotInk_On_Track", Palette.CarrotInk, Palette.Track, LargeBoldText);
            yield return Pair("LargeBold_MossInk_On_Track", Palette.MossInk, Palette.Track, LargeBoldText);
        }

        [TestCaseSource(nameof(TextOnBackground))]
        public void Palette_Text_Reads_On_Its_Background(Color ink, Color back, double need)
        {
            double have = Contrast(ink, back);
            TestContext.WriteLine(have.ToString("0.00") + ":1, needs " + need.ToString("0.0") + ":1");
            Assert.GreaterOrEqual(have, need);
        }

        [Test]
        public void The_Accent_Colours_Are_Too_Pale_To_Be_Words_On_Cream()
        {
            foreach (Color accent in new[] { Palette.Carrot, Palette.CarrotLip, Palette.Moss, Palette.Lantern, Palette.Blush })
                Assert.Less(Contrast(accent, Palette.Card), NormalText, "an accent that passed would not need its ink twin");
            Assert.AreEqual(Palette.Ink, ChunkyButton.InkFor(false), "a button that cannot be pressed still writes in full ink");
        }

        static double Needed(Text text)
        {
            bool bold = text.fontStyle == FontStyle.Bold || text.fontStyle == FontStyle.BoldAndItalic;
            return bold && text.fontSize >= LargeBoldFrom ? LargeBoldText : NormalText;
        }

        List<string> FaintLabels(Transform under)
        {
            var faint = new List<string>();
            Graphic[] drawn = root.GetComponentsInChildren<Graphic>(false);
            for (int i = 0; i < drawn.Length; i++)
            {
                if (!(drawn[i] is Text text) || !text.transform.IsChildOf(under) || string.IsNullOrEmpty(text.text.Trim()) || GroupAlpha(text.transform) < 0.99f) continue;
                RectTransform rt = text.rectTransform;
                if (!Backdrop(drawn, i, rt.TransformPoint(rt.rect.center), out Color back)) continue;
                double need = Needed(text), have = Contrast(Color.Lerp(back, text.color, text.color.a), back);
                if (have + 0.005 < need) faint.Add(Path(text.transform) + " " + have.ToString("0.00") + " < " + need.ToString("0.0") + " \"" + text.text + "\"");
            }
            return faint;
        }

        Transform StationRow(int index) => root.transform.Find("Canvas/Sheet/Panel/Scroll/Content/Row " + runner.Defs[index].Id);

        [TestCase("vi", false)]
        [TestCase("en", false)]
        [TestCase("vi", true)]
        [TestCase("en", true)]
        public void Every_Label_On_The_Stations_Sheet_Reads_At_AA_Contrast(string code, bool large)
        {
            L10n.SetLocale(code);
            UiKit.SetLargeText(large);
            screen.RebuildUi();
            screen.OpenSheet(0);
            screen.Refresh(-1f);
            Layout();
            Transform sheet = root.transform.Find("Canvas/Sheet");
            Assert.Greater(sheet.GetComponentsInChildren<Text>(false).Length, 20, "the sheet is open with its rows");
            List<string> faint = FaintLabels(sheet);
            Assert.IsEmpty(faint, faint.Count + " labels under AA in " + code + (large ? " (large text)" : "") + ":\n" + string.Join("\n", faint));
        }

        [Test]
        public void An_Income_Number_Is_Ink_Beside_A_Carrot()
        {
            screen.OpenSheet(0);
            screen.Refresh(-1f);
            Layout();
            Transform row = StationRow(0);
            Text perSecond = row.Find("PerSec").GetComponent<Text>();
            StringAssert.Contains(L10n.T("persec", "").Replace("+", "").Trim(), perSecond.text);
            Assert.AreEqual(Palette.Ink, perSecond.color, "not orange on cream");
            Image carrot = row.Find("Carrot").GetComponent<Image>();
            Assert.AreSame(runner.Catalog.Currency, carrot.sprite, "the carrot says what the number counts");
            Assert.LessOrEqual(RectIn(carrot.rectTransform, (RectTransform)row).xMax, RectIn(perSecond.rectTransform, (RectTransform)row).xMin, "the carrot sits before the number, not under it");
        }

        [Test]
        public void A_Button_Short_Of_Carrots_Shows_A_Lock_And_Full_Ink_And_Loses_The_Lock_When_It_Can_Be_Paid()
        {
            runner.State.Coins = BigNum.Zero;
            screen.OpenSheet(0);
            screen.Refresh(-1f);
            Transform buy = StationRow(0).Find("Buy");
            Text top = buy.Find("Face/Top").GetComponent<Text>();
            Assert.AreEqual(L10n.T("need.more"), top.text);
            Assert.IsFalse(buy.GetComponent<Button>().interactable);
            Assert.GreaterOrEqual(Contrast(top.color, buy.Find("Face").GetComponent<Image>().color), NormalText, "the words that say what is missing read at AA");
            Transform padlock = buy.Find("Lock");
            Assert.IsNotNull(padlock, "short of carrots is drawn, not only tinted");
            Assert.IsTrue(padlock.gameObject.activeSelf);
            Assert.GreaterOrEqual(Contrast(padlock.GetComponent<Image>().color, buy.Find("Face").GetComponent<Image>().color), 3.0, "the lock is a shape the eye can find");

            runner.State.Coins = new BigNum(1e30);
            screen.Refresh(-1f);
            Assert.IsTrue(buy.GetComponent<Button>().interactable);
            Assert.IsFalse(padlock.gameObject.activeSelf, "nothing is locked once it can be paid for");
        }

        [Test]
        public void The_Catalog_Carries_The_Bundled_Fonts_With_Their_Bold_And_Symbol_Faces_Behind_Them()
        {
            GameCatalog catalog = runner.Catalog;
            Assert.AreEqual(ContentSeeder.BodyFontPath, AssetDatabase.GetAssetPath(catalog.BodyFont));
            Assert.AreEqual(ContentSeeder.TitleFontPath, AssetDatabase.GetAssetPath(catalog.TitleFont));
            foreach (string path in new[] { ContentSeeder.BodyFontPath, ContentSeeder.TitleFontPath })
            {
                var importer = (TrueTypeFontImporter)AssetImporter.GetAtPath(path);
                Assert.IsTrue(importer.includeFontData, path + " ships its own outlines");
                var behind = new List<string>();
                foreach (Font font in importer.fontReferences) behind.Add(AssetDatabase.GetAssetPath(font));
                CollectionAssert.Contains(behind, path.Replace("-Regular", "-Bold"), "bold is a drawn face, not a smeared one");
                foreach (string symbols in ContentSeeder.SymbolFontPaths) CollectionAssert.Contains(behind, symbols);
            }
        }

        [Test]
        public void Every_Label_On_Screen_Is_Set_In_A_Bundled_Font()
        {
            GameCatalog catalog = runner.Catalog;
            Assert.AreSame(catalog.BodyFont, UiKit.Font);
            Assert.AreSame(catalog.TitleFont, UiKit.TitleFont);
            int titles = 0, bodies = 0;
            foreach (Text text in root.GetComponentsInChildren<Text>(true))
            {
                if (text.font == catalog.TitleFont) titles++;
                else if (text.font == catalog.BodyFont) bodies++;
                else Assert.Fail(Path(text.transform) + " is set in " + (text.font != null ? text.font.name : "no font"));
            }
            Assert.Greater(titles, 10, "titles and large numbers wear the title face");
            Assert.Greater(bodies, 100, "body text wears the body face");
        }
    }
}
