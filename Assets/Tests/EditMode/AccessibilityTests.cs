using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using VuonTra.Game;
using VuonTra.Game.UI;
using L10n = VuonTra.Game.L10n;
using Motion = VuonTra.Game.UI.Motion;

namespace VuonTra.Core.Tests
{
    /// <summary>Easy to read and easy to press: text size, contrast, touch targets, safe areas, overflow (#33).</summary>
    public class AccessibilityTests
    {
        GameObject root;
        GameRunner runner;
        GardenScreen screen;
        System.Func<Vector2> insetsBefore;

        [SetUp]
        public void SetUp()
        {
            L10n.SetLocale("vi");
            UiKit.SetLargeText(false);
            insetsBefore = GardenScreen.SafeInsets;
            var catalog = AssetDatabase.LoadAssetAtPath<GameCatalog>("Assets/VuonTra/Content/GameCatalog.asset");
            root = new GameObject("Game");
            var cam = new GameObject("Cam", typeof(Camera));
            cam.transform.SetParent(root.transform);
            runner = root.AddComponent<GameRunner>();
            runner.Catalog = catalog;
            runner.Init(GameState.NewGame(catalog.BuildDefs()));
            screen = root.AddComponent<GardenScreen>();
            screen.Runner = runner;
            screen.Cam = cam.GetComponent<Camera>();
            screen.Build();
            screen.Refresh();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(root);
            GardenScreen.SafeInsets = insetsBefore;
            UiKit.SetLargeText(false);
            PlayerPrefs.DeleteKey("text.large");
            Motion.SetReduced(false);
            L10n.SetLocale("vi");
        }

        static string Path(Transform t)
        {
            var sb = new StringBuilder(t.name);
            for (Transform p = t.parent; p != null && p.parent != null; p = p.parent) sb.Insert(0, p.name + "/");
            return sb.ToString();
        }

        // ----- large text -----

        [Test]
        public void Large_Text_Makes_Every_Label_A_Fifth_Bigger_And_Is_Remembered()
        {
            screen.Shell.StartGame();
            screen.Shell.OpenPause();
            Text heading = root.transform.Find("Canvas/Pause/Card/Heading").GetComponent<Text>();
            Assert.AreEqual(60, heading.fontSize);
            Transform pill = root.transform.Find("Canvas/Pause/Card/Text");
            Assert.IsNotNull(pill, "the large-text row on the pause card");
            Assert.AreEqual(L10n.T("toggle.off"), pill.Find("Text").GetComponent<Text>().text);

            pill.GetComponent<Button>().onClick.Invoke();
            Assert.IsTrue(UiKit.LargeText);
            Assert.AreEqual(1, PlayerPrefs.GetInt("text.large"), "kept for next time");
            screen.RebuildUi(); // in play the text-changed event calls this
            heading = root.transform.Find("Canvas/Pause/Card/Heading").GetComponent<Text>();
            Assert.AreEqual(72, heading.fontSize);
            Assert.AreEqual(UiKit.Scaled(UiKit.TextBody), root.transform.Find("Canvas/Sheet/Panel/Scroll/Content/Row am-tra/Name").GetComponent<Text>().fontSize);
            Assert.AreEqual(ShellState.Paused, screen.Shell.State, "still on the card with the switch");
            Assert.AreEqual(L10n.T("toggle.on"), root.transform.Find("Canvas/Pause/Card/Text/Text").GetComponent<Text>().text);

            UiKit.SetLargeText(false);
            Assert.AreEqual(26, UiKit.Scaled(UiKit.TextBody));
        }

        // ----- safe areas -----

        [Test]
        public void The_Hud_And_The_Tab_Bar_Keep_Out_Of_The_Notch_And_The_Home_Bar()
        {
            GardenScreen.SafeInsets = () => new Vector2(90f, 120f);
            screen.Advance(0.1f); // the frame step is where the edges are measured
            var hud = (RectTransform)root.transform.Find("Canvas/Hud");
            var tabBar = (RectTransform)root.transform.Find("Canvas/TabBar");
            Assert.AreEqual(-120f, hud.anchoredPosition.y, 0.01f, "the HUD hangs below the notch");
            Assert.AreEqual(90f + 16f, tabBar.anchoredPosition.y, 0.01f, "the tab bar stands above the home bar");

            GardenScreen.SafeInsets = () => Vector2.zero;
            screen.Advance(0.1f);
            Assert.AreEqual(0f, hud.anchoredPosition.y, 0.01f);
            Assert.AreEqual(16f, tabBar.anchoredPosition.y, 0.01f);
        }

        // ----- touch targets -----

        /// <summary>What a button answers to: its own plate plus any unseen pane under it, in its own units.</summary>
        static Rect TouchArea(Button button)
        {
            var rt = (RectTransform)button.transform;
            var corners = new Vector3[4];
            bool any = false;
            float x0 = 0f, y0 = 0f, x1 = 0f, y1 = 0f;
            foreach (Graphic g in button.GetComponentsInChildren<Graphic>(true))
            {
                if (!g.raycastTarget) continue;
                g.rectTransform.GetWorldCorners(corners);
                for (int i = 0; i < 4; i++)
                {
                    Vector3 local = rt.InverseTransformPoint(corners[i]);
                    if (!any) { x0 = x1 = local.x; y0 = y1 = local.y; any = true; }
                    x0 = Mathf.Min(x0, local.x); x1 = Mathf.Max(x1, local.x);
                    y0 = Mathf.Min(y0, local.y); y1 = Mathf.Max(y1, local.y);
                }
            }
            return any ? Rect.MinMaxRect(x0, y0, x1, y1) : rt.rect;
        }

        /// <summary>Lays the open parts of the screen out, so stretched buttons and labels have their real size.</summary>
        void Layout()
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)root.transform.Find("Canvas"));
            Canvas.ForceUpdateCanvases();
        }

        /// <summary>Opens each part of the screen in turn and measures what is on it.</summary>
        void EachState(System.Action<string> measure)
        {
            screen.Shell.StartGame();
            screen.Refresh();
            Layout();
            measure("garden");
            for (int tab = 0; tab < 4; tab++)
            {
                screen.OpenSheet(tab);
                screen.Refresh();
                Layout();
                measure("tab " + tab);
            }
            screen.CloseSheet();
            screen.OpenShop();
            screen.Refresh();
            Layout();
            measure("shop");
            screen.Shop.Close();
            screen.Shell.OpenPause();
            Layout();
            measure("pause");
        }

        [Test]
        public void Every_Button_Is_At_Least_The_Minimum_Touch_Size()
        {
            var small = new HashSet<string>();
            // Buttons on parts that are open get measured laid out; the rest (pickers, the photo bar) as built.
            EachState(where =>
            {
                foreach (Button button in root.GetComponentsInChildren<Button>(false)) Measure(button, small);
            });
            foreach (Button button in root.GetComponentsInChildren<Button>(true))
                if (!button.gameObject.activeInHierarchy) Measure(button, small);
            Assert.IsEmpty(small, small.Count + " buttons under " + UiKit.MinTouch + ":\n" + string.Join("\n", small));
        }

        static void Measure(Button button, HashSet<string> small)
        {
            Rect r = TouchArea(button);
            if (r.width <= 0f || r.height <= 0f) return; // not laid out: nothing to measure
            if (Mathf.Min(r.width, r.height) + 0.01f < UiKit.MinTouch)
                small.Add(Path(button.transform) + " " + Mathf.RoundToInt(r.width) + "x" + Mathf.RoundToInt(r.height));
        }

        // ----- overflow in both languages -----

        /// <summary>Every label on screen right now whose words do not fit its box.</summary>
        void Spills(string where, List<string> spills)
        {
            foreach (Text text in root.GetComponentsInChildren<Text>(false))
            {
                if (string.IsNullOrEmpty(text.text) || text.resizeTextForBestFit) continue;
                var rt = text.rectTransform;
                Rect r = rt.rect;
                if (r.width <= 0f || r.height <= 0f) continue;
                if (text.horizontalOverflow == HorizontalWrapMode.Overflow)
                {
                    if (text.preferredWidth > r.width + 1f) spills.Add(where + ": " + Path(rt) + " wide " + Mathf.RoundToInt(text.preferredWidth) + ">" + Mathf.RoundToInt(r.width) + " \"" + text.text + "\"");
                }
                else if (text.preferredHeight > r.height + 1f)
                    spills.Add(where + ": " + Path(rt) + " tall " + Mathf.RoundToInt(text.preferredHeight) + ">" + Mathf.RoundToInt(r.height) + " \"" + text.text + "\"");
            }
        }

        [TestCase("vi", false)]
        [TestCase("en", false)]
        [TestCase("vi", true)]
        [TestCase("en", true)]
        public void No_Label_Spills_Out_Of_Its_Box(string code, bool large)
        {
            L10n.SetLocale(code);
            UiKit.SetLargeText(large);
            runner.State.Coins = new BigNum(123456789);
            runner.State.Clovers = 7;
            runner.State.GoldenCarrots = 12;
            Rabbits.Arrive(runner.State, 3, "Thỏ Tím Rất Dài Tên", 0.5);
            screen.RebuildUi();
            var spills = new List<string>();
            EachState(where => Spills(where, spills));
            Assert.IsEmpty(spills, spills.Count + " labels spill in " + code + (large ? " (large text)" : "") + ":\n" + string.Join("\n", spills));
        }

        // ----- contrast -----

        static double Luminance(Color c)
        {
            double f(double v) => v <= 0.03928 ? v / 12.92 : System.Math.Pow((v + 0.055) / 1.055, 2.4);
            return 0.2126 * f(c.r) + 0.7152 * f(c.g) + 0.0722 * f(c.b);
        }

        /// <summary>WCAG contrast ratio: 4.5 for small text, 3 for large or bold text.</summary>
        static double Contrast(Color a, Color b)
        {
            double la = Luminance(a), lb = Luminance(b);
            return (System.Math.Max(la, lb) + 0.05) / (System.Math.Min(la, lb) + 0.05);
        }

        [Test]
        public void Text_On_Cream_Reads_At_AA_Contrast()
        {
            Assert.GreaterOrEqual(Contrast(Palette.Ink, Palette.Paper), 7.0, "ink on paper");
            Assert.GreaterOrEqual(Contrast(Palette.Ink, Palette.Card), 7.0, "ink on a card");
            Assert.GreaterOrEqual(Contrast(Palette.InkSoft, Palette.Card), 4.5, "secondary text on a card");
            Assert.GreaterOrEqual(Contrast(Palette.InkSoft, Palette.Paper), 4.5, "secondary text on paper");
            Assert.GreaterOrEqual(Contrast(Palette.InkSoft, Palette.Track), 3.0, "secondary bold text on a track");
            Assert.GreaterOrEqual(Contrast(Palette.Ink, Palette.Lantern), 4.5, "ink on the lantern colour");
            // Primary buttons: the owner chose ink on carrot (6.2:1) over cream (2.0:1) in #33.
            Assert.AreEqual(Palette.Ink, Palette.OnCarrot);
            Assert.GreaterOrEqual(Contrast(Palette.OnCarrot, Palette.Carrot), 4.5, "words on a primary button");
            Assert.AreEqual(Palette.OnCarrot, ChunkyButton.InkFor(true), "every raised button writes in it");
        }

        // ----- the HUD's chips keep apart -----

        static Rect WorldRect(RectTransform rt)
        {
            var c = new Vector3[4];
            rt.GetWorldCorners(c);
            return Rect.MinMaxRect(c[0].x, c[0].y, c[2].x, c[2].y);
        }

        [TestCase("vi", false)]
        [TestCase("en", false)]
        [TestCase("vi", true)]
        [TestCase("en", true)]
        public void The_Season_Chip_And_The_Golden_Chip_Do_Not_Overlap(string code, bool large)
        {
            L10n.SetLocale(code);
            UiKit.SetLargeText(large);
            runner.State.Season = 1;
            runner.State.GoldenCarrots = 5;
            screen.RebuildUi();
            screen.Shell.StartGame();
            screen.Refresh();
            Layout();
            var season = (RectTransform)root.transform.Find("Canvas/Hud/Season");
            var golden = (RectTransform)root.transform.Find("Canvas/Hud/Golden");
            Assert.IsTrue(season.gameObject.activeSelf && golden.gameObject.activeSelf, "both chips are up");
            Rect a = WorldRect(season), b = WorldRect(golden);
            Assert.IsFalse(a.Overlaps(b), "season " + a + " against golden " + b);
        }

    }
}
