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
            screen.Raining = () => false; // the real clock may fall in a shower (#62): these tests are about a dry garden
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

        /// <summary>
        /// Null when the label's words sit on one line inside its box at the size it is drawn (after best fit),
        /// else what went wrong. A best-fit label is skipped by <see cref="Spills"/>, so this asks the generator itself.
        /// </summary>
        static string OneLine(Text text)
        {
            Vector2 size = text.rectTransform.rect.size;
            var gen = new TextGenerator();
            TextGenerationSettings settings = text.GetGenerationSettings(size);
            gen.Populate(text.text, settings);
            settings.fontSize = text.resizeTextForBestFit ? gen.fontSizeUsedForBestFit : settings.fontSize;
            settings.resizeTextForBestFit = false;
            settings.horizontalOverflow = HorizontalWrapMode.Overflow;
            settings.verticalOverflow = VerticalWrapMode.Overflow;
            float ppu = text.pixelsPerUnit;
            float w = gen.GetPreferredWidth(text.text, settings) / ppu, h = gen.GetPreferredHeight(text.text, settings) / ppu;
            if (w <= size.x + 1f && h <= size.y + 1f) return null;
            return Path(text.transform) + " \"" + text.text + "\" at " + Mathf.RoundToInt(settings.fontSize / ppu) + "px is " + Mathf.RoundToInt(w) + "x" + Mathf.RoundToInt(h) + " in " + Mathf.RoundToInt(size.x) + "x" + Mathf.RoundToInt(size.y);
        }

        [TestCase("vi", false)]
        [TestCase("en", false)]
        [TestCase("vi", true)]
        [TestCase("en", true)]
        public void The_Level_500_Label_Fits_On_One_Line_In_Its_Pill_On_The_Card_And_The_Row(string code, bool large)
        {
            L10n.SetLocale(code);
            UiKit.SetLargeText(large);
            runner.State.Levels[0] = Economy.MaxLevel;
            screen.RebuildUi();
            screen.Shell.StartGame();
            screen.FocusStation(0);
            screen.Refresh();
            Layout();
            Text card = root.transform.Find("Canvas/Card/Level/Text").GetComponent<Text>();
            Assert.AreEqual(L10n.StationLevel(Economy.MaxLevel), card.text);
            Assert.IsNull(OneLine(card));
            screen.SelectStation(-1);
            screen.OpenSheet(0);
            screen.Refresh();
            Layout();
            Text row = root.transform.Find("Canvas/Sheet/Panel/Scroll/Content/Row am-tra/Level/Text").GetComponent<Text>();
            Assert.AreEqual(L10n.StationLevel(Economy.MaxLevel), row.text);
            Assert.IsNull(OneLine(row));
        }

        [TestCase("vi", false)]
        [TestCase("en", false)]
        [TestCase("vi", true)]
        [TestCase("en", true)]
        public void Every_Kind_And_Tier_Pill_Fits_On_One_Line_On_The_Rabbits_Sheet_And_The_Album_Page(string code, bool large)
        {
            // #81: "Thỏ Đèn Lồng · bậc Vàng ★★★★★" wrapped in its pill and the stars fell out below it.
            L10n.SetLocale(code);
            UiKit.SetLargeText(large);
            screen.RebuildUi();
            screen.Shell.StartGame();
            GameState s = runner.State;
            var wrong = new List<string>();
            int pills = 0;
            foreach (RabbitKind kind in RabbitKinds.All)
            {
                Rabbit r = Rabbits.ArriveKind(s, kind.Id, "Bông");
                screen.Detail.Open(runner.Defs, s, r.Id);
                Layout();
                Text own = root.transform.Find("Canvas/RabbitSheet/Card/Body/Kind/Text").GetComponent<Text>();
                Assert.AreEqual(L10n.T("detail.kind", L10n.Kind(r), L10n.Rarity(r.Tier)), own.text);
                string line = OneLine(own);
                if (line != null) wrong.Add(line);
                screen.Detail.OpenKind(runner.Defs, s, kind.Id);
                Layout();
                line = OneLine(root.transform.Find("Canvas/RabbitSheet/Card/Body/Tier/Text").GetComponent<Text>());
                if (line != null) wrong.Add(line);
                pills += 2;
            }
            screen.Detail.Close();
            Assert.AreEqual(2 * RabbitKinds.All.Count, pills);
            Assert.IsEmpty(wrong, wrong.Count + " pills run onto a second line in " + code + (large ? " (large text)" : "") + ":\n" + string.Join("\n", wrong));
        }

        [Test]
        public void The_Photo_Bar_Leaves_The_Frames_Name_And_Date_In_View()
        {
            bool frameBefore = GardenScreen.PhotoFrameOn;
            GardenScreen.PhotoFrameOn = true;
            try
            {
                runner.State.Profile.GardenName = "Vườn Trà Mây Trắng";
                screen.Shell.StartGame();
                screen.Refresh();
                screen.EnterPhotoMode();
                Layout();
                Transform frame = root.transform.Find("Canvas/PhotoFrame");
                Assert.IsTrue(frame.gameObject.activeSelf, "the frame is on");
                var words = new List<RectTransform> { (RectTransform)frame.Find("GardenName"), (RectTransform)frame.Find("Date"), (RectTransform)frame.Find("Logo") };
                var covered = new List<string>();
                foreach (RectTransform part in root.transform.Find("Canvas/PhotoBar").GetComponentsInChildren<RectTransform>(false))
                    foreach (RectTransform word in words)
                        if (WorldRect(part).Overlaps(WorldRect(word))) covered.Add(Path(part) + " over " + word.name);
                Assert.IsEmpty(covered, string.Join("\n", covered));
            }
            finally { screen.ExitPhotoMode(); GardenScreen.PhotoFrameOn = frameBefore; }
        }

        [TestCase("vi", false)]
        [TestCase("en", false)]
        [TestCase("vi", true)]
        [TestCase("en", true)]
        public void Every_Shop_Tile_Name_Fits_On_One_Line_Clear_Of_Its_Price(string code, bool large)
        {
            L10n.SetLocale(code);
            UiKit.SetLargeText(large);
            screen.RebuildUi();
            screen.Shell.StartGame();
            screen.OpenShop();
            screen.Refresh();
            Layout();
            var wrong = new List<string>();
            int tiles = 0;
            foreach (Text text in root.transform.Find("Canvas/Shop").GetComponentsInChildren<Text>(true))
            {
                Transform tile = text.transform.parent;
                if (text.name != "Name" || !(tile.name.StartsWith("Decor ") || tile.name.StartsWith("Hat ") || tile.name == "Jar")) continue; // the moon jar (#64) too
                tiles++;
                string line = OneLine(text);
                if (line != null) wrong.Add(line);
                Rect name = WorldRect(text.rectTransform);
                foreach (string chip in new[] { "Price", "Owned", "Icon" })
                {
                    Transform c = tile.Find(chip);
                    if (c != null && WorldRect((RectTransform)c).Overlaps(name)) wrong.Add(Path(text.transform) + " \"" + text.text + "\" runs into its " + chip);
                }
            }
            Assert.Greater(tiles, Decor.Kinds.Length, "every decor tile and the hats were measured");
            Assert.IsEmpty(wrong, string.Join("\n", wrong));
        }

        /// <summary>
        /// Null when the label's words, wrapped, fit its box at the size best fit draws them, and that size is no
        /// smaller than <paramref name="least"/>; else what went wrong.
        /// </summary>
        static string Wrapped(Text text, int least)
        {
            Vector2 size = text.rectTransform.rect.size;
            var gen = new TextGenerator();
            TextGenerationSettings settings = text.GetGenerationSettings(size);
            gen.Populate(text.text, settings);
            settings.fontSize = text.resizeTextForBestFit ? gen.fontSizeUsedForBestFit : settings.fontSize;
            settings.resizeTextForBestFit = false;
            settings.verticalOverflow = VerticalWrapMode.Overflow;
            float ppu = text.pixelsPerUnit;
            float h = gen.GetPreferredHeight(text.text, settings) / ppu, drawn = settings.fontSize / ppu;
            if (h <= size.y + 1f && drawn >= least - 0.01f) return null;
            return Path(text.transform) + " \"" + text.text + "\" at " + Mathf.RoundToInt(drawn) + "px is " + Mathf.RoundToInt(h) + " tall in " + Mathf.RoundToInt(size.x) + "x" + Mathf.RoundToInt(size.y);
        }

        [TestCase("vi", false)]
        [TestCase("en", false)]
        [TestCase("vi", true)]
        [TestCase("en", true)]
        public void Every_Idle_Line_A_Rabbit_Says_Fits_Its_Bubble(string code, bool large)
        {
            L10n.SetLocale(code);
            UiKit.SetLargeText(large);
            screen.RebuildUi();
            screen.Shell.StartGame();
            runner.State.Rabbits.Add(new Rabbit { Id = 1, Tier = 0, Kind = "tho-bong", Name = "Bông", Station = 0 });
            runner.State.NextRabbitId = 1;
            screen.Refresh();
            Assert.IsTrue(screen.ChatterNow());
            Layout();
            Text text = root.transform.Find("Canvas/Garden/Content/ChatLine/Text").GetComponent<Text>();
            var wrong = new List<string>();
            int lines = 0;
            // The longest station name in either language stands in for the booster's.
            string longest = "";
            foreach (StationDef d in runner.Defs) if (L10n.Station(d.Id).Length > longest.Length) longest = L10n.Station(d.Id);
            foreach (string key in Chatter.AllKeys)
            {
                lines++;
                text.text = L10n.T(key, longest);
                string line = Wrapped(text, 18);
                if (line != null) wrong.Add(line);
            }
            Assert.AreEqual(33, lines);
            Assert.IsEmpty(wrong, string.Join("\n", wrong));
        }

        [TestCase("vi", false)]
        [TestCase("en", false)]
        [TestCase("vi", true)]
        [TestCase("en", true)]
        public void Every_Gift_Day_Label_Fits_On_One_Line_Clear_Of_Its_Pictures(string code, bool large)
        {
            L10n.SetLocale(code);
            UiKit.SetLargeText(large);
            screen.RebuildUi();
            screen.Shell.StartGame();
            // Day seven waiting (the big tile with the hat and clover), the rest taken with ticks: every look at once.
            runner.State.GiftDay = runner.TodayNumber - 1;
            runner.State.GiftStreak = 6;
            screen.OpenShop();
            screen.Refresh();
            Layout();
            Transform days = root.transform.Find("Canvas/Shop/Card/Today/Days");
            var wrong = new List<string>();
            Rect row = WorldRect((RectTransform)days.parent);
            for (int i = 1; i <= VuonTra.Core.Shop.WeekDays; i++)
            {
                Transform tile = days.Find("Day " + i);
                Rect tileRect = WorldRect((RectTransform)tile);
                if (!row.Contains(tileRect.min) || !row.Contains(tileRect.max)) wrong.Add(Path(tile) + " runs out of today's panel");
                foreach (string label in new[] { "Day", "Amount" })
                {
                    Text text = tile.Find(label).GetComponent<Text>();
                    string line = OneLine(text);
                    if (line != null) wrong.Add(line);
                    Rect box = WorldRect(text.rectTransform);
                    foreach (string part in new[] { "Golden", "Hat", "Clover", "Tick" })
                    {
                        Transform p = tile.Find(part);
                        if (p.gameObject.activeSelf && WorldRect((RectTransform)p).Overlaps(box)) wrong.Add(Path(text.transform) + " \"" + text.text + "\" runs into its " + part);
                    }
                }
            }
            Assert.IsTrue(days.Find("Day 7/Hat").gameObject.activeSelf, "the big tile was measured");
            Assert.IsEmpty(wrong, string.Join("\n", wrong));
        }

        [TestCase("Canvas/Fireflies", "Timer")]
        [TestCase("Canvas/Leaves", "Timer")]
        [TestCase("Canvas/Pound", "Beats")]
        public void The_Minigame_Score_And_Count_Keep_24_From_The_Screen_Edges_And_Below_The_Notch(string game, string count)
        {
            GardenScreen.SafeInsets = () => new Vector2(90f, 120f); // a phone with a notch and a home bar
            screen.Advance(0.1f);
            Layout();
            var overlay = (RectTransform)root.transform.Find(game);
            Assert.IsNotNull(overlay, game);
            Rect screenRect = overlay.rect;
            float notch = GardenScreen.SafeInsets().y;
            foreach (string name in new[] { "Score", count })
            {
                Rect r = RectIn((RectTransform)overlay.Find(name), overlay);
                Assert.GreaterOrEqual(r.xMin, screenRect.xMin + 24f, game + "/" + name + " keeps off the left edge");
                Assert.LessOrEqual(r.xMax, screenRect.xMax - 24f, game + "/" + name + " keeps off the right edge");
                Assert.LessOrEqual(r.yMax, screenRect.yMax - notch, game + "/" + name + " sits below the notch");
            }
            Transform quit = overlay.Find("Quit");
            if (quit != null) Assert.IsFalse(RectIn((RectTransform)quit, overlay).Overlaps(RectIn((RectTransform)overlay.Find(count), overlay)), "the count is clear of the quit button");
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

        /// <summary>A rect's corners in another rect's own units.</summary>
        static Rect RectIn(RectTransform rt, RectTransform space)
        {
            var c = new Vector3[4];
            rt.GetWorldCorners(c);
            Vector3 a = space.InverseTransformPoint(c[0]), b = space.InverseTransformPoint(c[2]);
            return Rect.MinMaxRect(a.x, a.y, b.x, b.y);
        }

        /// <summary>
        /// Where a rect lands on the 1080x1920 phone the layout is drawn for, measured from the corner of
        /// <paramref name="frame"/> it hangs from. The test canvas has the batch screen's shape, so the HUD (hung
        /// from the top) and the garden (placed from the bottom) only line up again in phone units.
        /// </summary>
        static Rect OnPhone(RectTransform rt, RectTransform frame, Vector2 corner)
        {
            Rect r = RectIn(rt, frame), f = frame.rect;
            var from = new Vector2(Mathf.Lerp(f.xMin, f.xMax, corner.x), Mathf.Lerp(f.yMin, f.yMax, corner.y));
            var to = new Vector2(corner.x * 1080f, corner.y * 1920f);
            return new Rect(r.position - from + to, r.size);
        }

        /// <summary>The plates the garden hangs on its stations and its games, as they are on screen now.</summary>
        List<RectTransform> GardenPlates()
        {
            var plates = new List<RectTransform>();
            Transform street = root.transform.Find("Canvas/Garden/Content");
            foreach (Transform child in street)
            {
                if (!child.gameObject.activeInHierarchy) continue;
                Transform plate = child.name.StartsWith("Station ") ? child.Find("Level") : child.Find("Plate");
                if (plate != null && plate.gameObject.activeInHierarchy) plates.Add((RectTransform)plate);
            }
            return plates;
        }

        [Test]
        public void The_Guest_Pill_Covers_No_Garden_Plate_As_The_View_Rests_On_The_Teapot()
        {
            screen.Shell.StartGame();
            runner.State.GardenXp = Garden.XpForLevel(5); // the firefly jar and its plate are out
            runner.PlaceOrder();
            screen.Refresh();
            screen.LookAt(screen.StationSpot(0)); // where the teapot button takes the view
            Layout();
            var pill = (RectTransform)root.transform.Find("Canvas/Hud/Order");
            Assert.IsTrue(pill.gameObject.activeSelf, "a guest waits, so the pill is up");
            Assert.IsTrue(root.transform.Find("Canvas/Garden/Content/Jar").gameObject.activeSelf, "the jar is out");
            var hud = (RectTransform)root.transform.Find("Canvas/Hud");
            var view = (RectTransform)root.transform.Find("Canvas/Garden");
            Rect p = OnPhone(pill, hud, Vector2.one);
            var covered = new List<string>();
            foreach (RectTransform plate in GardenPlates())
            {
                Rect r = OnPhone(plate, view, Vector2.zero);
                if (r.Overlaps(p)) covered.Add(Path(plate) + " " + r);
            }
            Assert.IsEmpty(covered, "the guest pill " + p + " covers: " + string.Join(", ", covered));
        }

        [TestCase(GardenScreen.ZoomLevel.Default)]
        [TestCase(GardenScreen.ZoomLevel.Near)]
        public void The_Top_Edge_Pill_Covers_No_Garden_Plate_As_The_View_Rests_On_The_Teapot(GardenScreen.ZoomLevel zoom)
        {
            // #71's last point: the "▲ N" pill at the top edge sat on the mortar's "Giã bánh" plate.
            screen.Shell.StartGame();
            runner.State.GardenXp = Garden.XpForLevel(5); // the mortar, the basket and the jar are all out
            screen.Refresh();
            screen.SetZoom(zoom, false);
            screen.LookAt(screen.StationSpot(0));
            Layout();
            var pill = (RectTransform)root.transform.Find("Canvas/EdgeUp");
            pill.gameObject.SetActive(true); // as when a station waits above the view
            Layout();
            var canvas = (RectTransform)root.transform.Find("Canvas");
            var view = (RectTransform)root.transform.Find("Canvas/Garden");
            Rect p = OnPhone(pill, canvas, Vector2.zero);
            var covered = new List<string>();
            foreach (RectTransform plate in GardenPlates())
            {
                Rect r = OnPhone(plate, view, Vector2.zero);
                if (r.Overlaps(p)) covered.Add(Path(plate) + " " + r);
            }
            Assert.IsEmpty(covered, "the top edge pill " + p + " covers: " + string.Join(", ", covered));
        }

        [TestCase("vi", false)]
        [TestCase("en", false)]
        [TestCase("vi", true)]
        [TestCase("en", true)]
        public void The_Album_Set_Lines_And_The_Gold_Frame_Fit(string code, bool large)
        {
            // #61: every set line, half met or complete, and the frame at the top, in both languages and large text.
            L10n.SetLocale(code);
            UiKit.SetLargeText(large);
            foreach (AlbumSet set in AlbumSets.All)
                for (int i = 0; i < set.Kinds.Count; i++)
                    if (set.Tier % 2 == 0 || i == 0) Rabbits.See(runner.State, set.Kinds[i].Id);
            runner.State.AlbumMilestonesClaimed.Add(AlbumSets.FrameMilestone);
            screen.RebuildUi();
            screen.Shell.StartGame();
            screen.OpenSheet(3);
            screen.ShowRabbitAlbum(true);
            screen.Refresh();
            Layout();
            var spills = new List<string>();
            Spills("album", spills);
            var lines = new List<string>();
            foreach (Text text in root.transform.Find("Canvas/Sheet/Panel/Rabbits/Content").GetComponentsInChildren<Text>(false))
                if (text.name == "Line" || text.transform.parent.name == "FullFrame") { string bad = OneLine(text); if (bad != null) lines.Add(bad); }
            Assert.IsEmpty(spills, string.Join("\n", spills));
            Assert.IsEmpty(lines, string.Join("\n", lines));
            Assert.Greater(root.transform.Find("Canvas/Sheet/Panel/Rabbits/Content").GetComponentsInChildren<Text>(false).Length, 24);
        }
    }
}
