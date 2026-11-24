using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using VuonTra.EditorTools;

namespace VuonTra.Core.Tests
{
    public class PreviewLanguageCheckTests
    {
        static PreviewLanguageCheck Check() => new PreviewLanguageCheck(L10nStrings.All, L10nStrings.EnglishOne);

        [TestCase("Tea Shop")]
        [TestCase("Buy more")]
        [TestCase("Golden call")]
        [TestCase("Day 7")]
        [TestCase("Garden level 5 · Rabbits: 3 · Stations: 4")]
        [TestCase("Linking for the first time brings 3 clovers")]
        public void An_English_Line_From_The_Table_Is_Caught(string text)
        {
            Assert.IsTrue(Check().IsEnglish(text), text);
        }

        [TestCase("Tiệm")]
        [TestCase("Mua thêm")]
        [TestCase("Ngày 7")]
        [TestCase("Cấp vườn 5 · 3 bé thỏ · 4 trạm")]
        [TestCase("Mochi")]
        [TestCase("+5")]
        [TestCase("1,00af")]
        public void A_Vietnamese_Line_A_Name_Or_A_Number_Passes(string text)
        {
            Assert.IsFalse(Check().IsEnglish(text), text);
        }

        [Test]
        public void A_Frame_Lists_Each_Visible_English_Label_With_Its_Place()
        {
            var root = new GameObject("Screen");
            try
            {
                var title = new GameObject("Title", typeof(RectTransform), typeof(Text));
                title.transform.SetParent(root.transform, false);
                title.GetComponent<Text>().text = "Tea Shop";
                var body = new GameObject("Body", typeof(RectTransform), typeof(Text));
                body.transform.SetParent(root.transform, false);
                body.GetComponent<Text>().text = "Mua thêm";
                var hidden = new GameObject("Hidden", typeof(RectTransform), typeof(Text));
                hidden.transform.SetParent(root.transform, false);
                hidden.GetComponent<Text>().text = "Golden call";
                hidden.SetActive(false);

                PreviewLanguageCheck check = Check();
                check.Look("57-ngay-7", root.transform);
                Assert.AreEqual(1, check.Found.Count);
                Assert.AreEqual("57-ngay-7: Title = Tea Shop", check.Found[0]);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
