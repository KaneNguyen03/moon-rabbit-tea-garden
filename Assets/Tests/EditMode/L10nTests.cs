using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
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
            Assert.AreEqual("3,88K cà rốt / 10 giây", L10n.T("row.income", "3,88K", 10));
            L10n.SetLocale("en");
            Assert.AreEqual("3.88K carrots / 10 s", L10n.T("row.income", L10n.Number("3,88K"), 10));
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

        [Test]
        public void Every_Key_In_The_Table_Has_A_Caller()
        {
            var literals = new HashSet<string>();
            var families = new List<Regex>();
            foreach (string folder in new[] { "VuonTra/Game", "VuonTra/Core" })
                foreach (string file in Directory.GetFiles(Path.Combine(Application.dataPath, folder), "*.cs", SearchOption.AllDirectories))
                    foreach (string line in File.ReadAllLines(file))
                        CollectKeysAskedFor(line, literals, families);
            Assert.Greater(families.Count, 20, "keys built from a prefix, like \"quest.\" + id, were read too");
            var uncalled = new List<string>();
            for (int i = 0; i < L10nStrings.All.GetLength(0); i++)
            {
                string key = L10nStrings.All[i, 0];
                if (!literals.Contains(key) && !families.Exists(family => family.IsMatch(key))) uncalled.Add(key);
            }
            Assert.IsEmpty(uncalled, uncalled.Count + " keys nothing in Game or Core asks for:\n" + string.Join("\n", uncalled));
        }

        static readonly Regex StringOrLineComment = new Regex(@"""(?:[^""\\]|\\.)*""|//.*");
        static readonly Regex JoinedSuffix = new Regex(@"\G\s*\+\s*[\w.()\[\]\s+-]*?\+\s*""(\.[^""\\]*)""");
        static readonly Regex KeyPrefix = new Regex(@"^[a-z][\w.-]*\.$");
        const string OneSegment = "[^.]+";

        static void CollectKeysAskedFor(string line, HashSet<string> literals, List<Regex> families)
        {
            foreach (Match token in StringOrLineComment.Matches(line))
            {
                if (token.Value.StartsWith("//")) return;
                string literal = token.Value.Substring(1, token.Value.Length - 2);
                literals.Add(literal);
                if (!KeyPrefix.IsMatch(literal)) continue;
                var pattern = new StringBuilder("^").Append(Regex.Escape(literal)).Append(OneSegment);
                int at = token.Index + token.Length;
                for (Match piece = JoinedSuffix.Match(line, at); piece.Success; piece = JoinedSuffix.Match(line, at))
                {
                    string suffix = piece.Groups[1].Value;
                    pattern.Append(Regex.Escape(suffix));
                    at = piece.Index + piece.Length;
                    if (!suffix.EndsWith(".")) break;
                    pattern.Append(OneSegment);
                }
                families.Add(new Regex(pattern.Append('$').ToString()));
            }
        }

        static readonly string[] DeletedKeys =
        {
            "pause.profile", "profile.close", "playgames.name", "playgames.saved", "quick.plot", "quick.open", "quick.upgrade",
            "quick.plot.short", "quick.open.short", "land.expand", "moment.plot.title", "moment.summon.title", "moment.summon.sub",
            "moment.merge.title", "moment.merge.sub", "away.body", "plot.title", "plot.sub", "upgrade.level", "quest.reward.clovers",
            "rabbits.summon.title", "rabbits.clovers", "rabbits.auto", "rabbits.tier", "rabbits.where.working", "rabbits.where.resting",
            "rabbits.work", "rabbits.full", "rename.button", "quest.season.note", "moment.golden.title", "moment.golden.sub",
            "offer.jars.float", "forest.expand", "forest.title", "moment.forest.title", "fair.began.title", "fair.began.sub",
            "quest.cham-100", "quest.cham-1000", "quest.thu-1k", "quest.thu-1m", "quest.thu-1b", "rest.buy", "rest.full", "quest.anh-1",
            "quest.tho-max", "quest.gia-30", "quest.la-30", "quest.dom-30", "quest.don-20", "quest.loai-10", "quest.loai-20",
            "quest.loai-40", "quest.loai-72", "quest.ca-50", "quest.tim-10", "offer.shop", "offer.jars.float.one"
        };

        [Test]
        public void No_Deleted_Key_Is_Used_Anywhere_In_Assets()
        {
            var alternatives = new List<string>();
            foreach (string key in DeletedKeys) alternatives.Add(Regex.Escape(key));
            var anyDeletedKey = new Regex(@"(?<![\w.-])(" + string.Join("|", alternatives) + @")(?![\w-]|\.\w)");
            string tests = Path.Combine(Application.dataPath, "Tests");
            var used = new List<string>();
            int searched = 0;
            foreach (string file in Directory.GetFiles(Application.dataPath, "*", SearchOption.AllDirectories))
            {
                if (file.StartsWith(tests) || !SearchedForKeys(file)) continue;
                searched++;
                foreach (Match hit in anyDeletedKey.Matches(File.ReadAllText(file)))
                    used.Add(hit.Value + " in " + file.Substring(Application.dataPath.Length - "Assets".Length));
            }
            Assert.Greater(searched, 100, "code, prefabs, scenes and assets were all searched");
            Assert.IsEmpty(used, string.Join("\n", used));
        }

        static bool SearchedForKeys(string file)
        {
            foreach (string kind in new[] { ".cs", ".prefab", ".unity", ".asset", ".json", ".txt" })
                if (file.EndsWith(kind)) return true;
            return false;
        }

        [TestCase("vi", "lần đầu mỗi 5 cấp vườn")]
        [TestCase("en", "the first time you reach every 5th garden level")]
        public void The_Shop_Says_Garden_Level_Gold_Comes_The_First_Time_Only_And_Fits_In_Large_Text(string code, string says)
        {
            L10n.SetLocale(code);
            UiKit.SetLargeText(true);
            var root = new GameObject("Game");
            try
            {
                var catalog = AssetDatabase.LoadAssetAtPath<GameCatalog>("Assets/VuonTra/Content/GameCatalog.asset");
                var cam = new GameObject("Cam", typeof(Camera));
                cam.transform.SetParent(root.transform);
                var runner = root.AddComponent<GameRunner>();
                runner.Catalog = catalog;
                runner.Init(GameState.NewGame(catalog.BuildDefs()));
                var screen = root.AddComponent<GardenScreen>();
                screen.Runner = runner;
                screen.Cam = cam.GetComponent<Camera>();
                screen.Raining = () => false;
                screen.Build();
                screen.Shell.StartGame();
                screen.OpenShop();
                screen.Refresh();
                LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)root.transform.Find("Canvas"));
                Canvas.ForceUpdateCanvases();
                Text earn = null;
                foreach (Text text in root.transform.Find("Canvas/Shop").GetComponentsInChildren<Text>(true))
                    if (text.name == "Earn") earn = text;
                Assert.IsNotNull(earn, "the shop has its \"Kiếm ở\" line");
                StringAssert.Contains(says, earn.text);
                Assert.LessOrEqual(earn.preferredHeight, earn.rectTransform.rect.height + 1f, "\"" + earn.text + "\" at " + earn.fontSize + "px in a box " + Mathf.RoundToInt(earn.rectTransform.rect.width) + " wide");
            }
            finally
            {
                Object.DestroyImmediate(root);
                UiKit.SetLargeText(false);
                PlayerPrefs.DeleteKey("text.large");
            }
        }

        static System.Collections.Generic.List<string> Blanks(string text)
        {
            var found = new System.Collections.Generic.List<string>();
            foreach (System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(text, @"\{\d+\}")) found.Add(m.Value);
            return found;
        }

        [Test]
        public void English_Lines_Use_A_Decimal_Point_And_One_Name_Per_Thing()
        {
            var bad = new System.Collections.Generic.List<string>();
            for (int i = 0; i < L10nStrings.All.GetLength(0); i++)
            {
                string key = L10nStrings.All[i, 0], en = L10nStrings.All[i, 2];
                if (System.Text.RegularExpressions.Regex.IsMatch(en, @"\d,\d(?!\d\d)")) bad.Add(key + ": decimal comma in " + en);
                if (System.Text.RegularExpressions.Regex.IsMatch(en, @"(?i)\bseason \{")) bad.Add(key + ": a new moon is \"Moon {n}\": " + en);
                if (System.Text.RegularExpressions.Regex.IsMatch(en, @"(?i)\bpress\b")) bad.Add(key + ": \"Tap\" everywhere: " + en);
                if (System.Text.RegularExpressions.Regex.IsMatch(en, @"(?i)places? to rest|resting places")) bad.Add(key + ": \"rest spots\": " + en);
            }
            Assert.IsEmpty(bad, string.Join("\n", bad));
            L10n.SetLocale("en");
            Assert.AreEqual("Choose a trinket for Bông", L10n.T("picker.title", L10n.T("slot.charm"), "Bông"), "the tab is Charms; a rabbit wears a trinket");
            Assert.AreNotEqual(L10n.T("photo.album"), L10n.T("rabbits.tab.album"), "the rabbit book is not the photo album");
            Assert.AreEqual("Moon 2", L10n.T("hud.season", 2));
            Assert.AreEqual("1.5", VuonTra.Game.UI.UiKit.Times(1.5), "times signs read with a point");
        }

        [Test]
        public void Vietnamese_Lines_Keep_One_Spelling()
        {
            var banned = new (string pattern, string why)[]
            {
                (@"[óòỏõọÓÒỎÕỌ][aeAE](?![\wâăêôơư])", "tone on the o: write xoá, khoá, hoà"),
                (@"(?<![qQ])[úùủũụÚÙỦŨỤ][yY](?!\w)", "tone on the u: write huỷ, thuỷ"),
                (@"\S %", "no space before %: +5%"),
                (@"(?i)con thỏ", "a rabbit is \"bé thỏ\""),
                (@"(?i)(?<!\w)nó(?!\w)", "a rabbit is \"bé\", not \"nó\""),
                (@"(?i)(?<!\w)bấm(?!\w)", "\"Chạm\" everywhere"),
                (@"\+\{\d+\} cỏ(?! bốn lá)", "a reward of clovers says \"cỏ bốn lá\""),
            };
            var bad = new System.Collections.Generic.List<string>();
            for (int i = 0; i < L10nStrings.All.GetLength(0); i++)
                foreach (var (pattern, why) in banned)
                    if (System.Text.RegularExpressions.Regex.IsMatch(L10nStrings.All[i, 1], pattern))
                        bad.Add(L10nStrings.All[i, 0] + ": " + L10nStrings.All[i, 1] + " (" + why + ")");
            Assert.IsEmpty(bad, string.Join("\n", bad));
        }
    }
}
