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
    public partial class AccessibilityTests
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
            screen.Raining = () => false;
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
            screen.RebuildUi();
            heading = root.transform.Find("Canvas/Pause/Card/Heading").GetComponent<Text>();
            Assert.AreEqual(72, heading.fontSize);
            Assert.AreEqual(UiKit.Scaled(UiKit.TextBody), root.transform.Find("Canvas/Sheet/Panel/Scroll/Content/Row am-tra/Name").GetComponent<Text>().fontSize);
            Assert.AreEqual(ShellState.Paused, screen.Shell.State, "still on the card with the switch");
            Assert.AreEqual(L10n.T("toggle.on"), root.transform.Find("Canvas/Pause/Card/Text/Text").GetComponent<Text>().text);

            UiKit.SetLargeText(false);
            Assert.AreEqual(26, UiKit.Scaled(UiKit.TextBody));
        }

        [Test]
        public void The_Hud_And_The_Tab_Bar_Keep_Out_Of_The_Notch_And_The_Home_Bar()
        {
            GardenScreen.SafeInsets = () => new Vector2(90f, 120f);
            screen.Advance(0.1f);
            var hud = (RectTransform)root.transform.Find("Canvas/Hud");
            var tabBar = (RectTransform)root.transform.Find("Canvas/TabBar");
            Assert.AreEqual(-120f, hud.anchoredPosition.y, 0.01f, "the HUD hangs below the notch");
            Assert.AreEqual(90f + 16f, tabBar.anchoredPosition.y, 0.01f, "the tab bar stands above the home bar");

            GardenScreen.SafeInsets = () => Vector2.zero;
            screen.Advance(0.1f);
            Assert.AreEqual(0f, hud.anchoredPosition.y, 0.01f);
            Assert.AreEqual(16f, tabBar.anchoredPosition.y, 0.01f);
        }

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

        void Layout()
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)root.transform.Find("Canvas"));
            Canvas.ForceUpdateCanvases();
        }

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
            screen.OpenFishing();
            screen.Refresh();
            Layout();
            measure("pond");
            screen.FishingCard.Close();
            screen.Shell.OpenPause();
            Layout();
            measure("pause");
        }

        [Test]
        public void Every_Button_Is_At_Least_The_Minimum_Touch_Size()
        {
            var small = new HashSet<string>();
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
            if (r.width <= 0f || r.height <= 0f) return;
            if (Mathf.Min(r.width, r.height) + 0.01f < UiKit.MinTouch)
                small.Add(Path(button.transform) + " " + Mathf.RoundToInt(r.width) + "x" + Mathf.RoundToInt(r.height));
        }

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

        [TestCase("vi", false)]
        [TestCase("en", false)]
        [TestCase("vi", true)]
        [TestCase("en", true)]
        public void The_Pond_Card_The_Feed_Button_And_The_Heart_Badge_Read_On_One_Line_And_Are_Big_Enough(string code, bool large)
        {
            L10n.SetLocale(code);
            UiKit.SetLargeText(large);
            const long now = 1_800_000_000L;
            runner.FishingClock = () => now;
            runner.State.Baits = 0;
            runner.State.BaitAt = now;
            Rabbit r = Rabbits.Arrive(runner.State, 3, "Thỏ Tím Rất Dài Tên", 0.5);
            Rabbit full = Rabbits.Arrive(runner.State, 0, "Mây", 0.2);
            full.Affection = Rabbits.AffectionMax;
            for (int i = 0; i < Fishing.Kinds; i++) runner.State.Fish[i] = 12;
            screen.RebuildUi();
            screen.Shell.StartGame();
            screen.OpenFishing();
            screen.Refresh();
            Layout();
            var wrong = new List<string>();
            var small = new HashSet<string>();
            void Check(Text text) { string bad = OneLine(text); if (bad != null) wrong.Add(bad); }
            Transform card = root.transform.Find("Canvas/Pond/Card");
            foreach (Text text in card.GetComponentsInChildren<Text>(false)) if (!string.IsNullOrEmpty(text.text)) Check(text);
            foreach (Button button in card.GetComponentsInChildren<Button>(false)) Measure(button, small);
            Text status = card.Find("Status").GetComponent<Text>();
            foreach (string key in new[] { "pond.hint", "pond.wait", "pond.sink", "pond.early", "pond.lost", "fish.nobait" })
            {
                status.text = L10n.T(key);
                Check(status);
            }
            for (int i = 0; i < Fishing.Kinds; i++)
            {
                status.text = L10n.T("pond.caught", L10n.T("fish." + i)) + " · " + L10n.T("fish.points", Fishing.Points[i]);
                Check(status);
            }
            status.text = L10n.T("pond.lost") + " · " + L10n.T("fish.nobait");
            Check(status);
            Text cast = card.Find("Cast/Face/Text").GetComponent<Text>();
            foreach (string key in new[] { "pond.cast", "pond.casting", "pond.nobait" })
            {
                cast.text = L10n.T(key);
                Check(cast);
            }
            Text plate = root.transform.Find("Canvas/Garden/Content/PondTap/Plate/Text").GetComponent<Text>();
            plate.text = L10n.T("pond.plate", 5, 5);
            Check(plate);
            screen.FishingCard.Close();

            screen.Detail.Open(runner.Defs, runner.State, r.Id);
            screen.Refresh();
            Layout();
            Transform feed = root.transform.Find("Canvas/RabbitSheet/Card/Body/Feed");
            Measure(feed.GetComponent<Button>(), small);
            Check(feed.Find("Face/Top").GetComponent<Text>());
            Text sub = feed.Find("Face/Sub").GetComponent<Text>(), points = feed.Find("Face/Points").GetComponent<Text>();
            for (int i = 0; i < Fishing.Kinds; i++)
            {
                sub.text = L10n.T("feed.with", L10n.T("fish." + i), 12);
                Check(sub);
                points.text = L10n.T("feed.points", Fishing.Points[i]);
                Check(points);
            }
            Rect top = WorldRect(feed.Find("Face/Top").GetComponent<RectTransform>()), pts = WorldRect(points.rectTransform);
            if (top.Overlaps(pts)) wrong.Add("the Feed button's word runs into its points");
            foreach (string key in new[] { "feed.empty", "feed.full", "feed.leaf" })
            {
                sub.text = L10n.T(key);
                Check(sub);
            }
            screen.Detail.Close();

            screen.OpenSheet(3);
            screen.Refresh();
            Layout();
            Transform badge = root.transform.Find("Canvas/Sheet/Panel/Rabbits/Content/Row 0/Rabbit " + full.Id + "/Tile/TenHearts");
            Assert.IsNotNull(badge, "the full rabbit wears its heart badge");
            Check(badge.Find("Text").GetComponent<Text>());
            Assert.IsEmpty(wrong, string.Join("\n", wrong));
            Assert.IsEmpty(small, string.Join("\n", small));
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
                if (text.name != "Name" || !(tile.name.StartsWith("Decor ") || tile.name.StartsWith("Hat ") || tile.name == "Jar")) continue;
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
            GardenScreen.SafeInsets = () => new Vector2(90f, 120f);
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

        static double Luminance(Color c)
        {
            double f(double v) => v <= 0.03928 ? v / 12.92 : System.Math.Pow((v + 0.055) / 1.055, 2.4);
            return 0.2126 * f(c.r) + 0.7152 * f(c.g) + 0.0722 * f(c.b);
        }

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
            Assert.AreEqual(Palette.Ink, Palette.OnCarrot);
            Assert.GreaterOrEqual(Contrast(Palette.OnCarrot, Palette.Carrot), 4.5, "words on a primary button");
            Assert.AreEqual(Palette.OnCarrot, ChunkyButton.InkFor(true), "every raised button writes in it");
            Assert.GreaterOrEqual(Contrast(Palette.CarrotInk, Palette.Card), 4.5, "carrot-coloured words on a card");
            Assert.GreaterOrEqual(Contrast(Palette.MossInk, Palette.Card), 4.5, "moss-coloured words on a card");
        }

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

        static Rect RectIn(RectTransform rt, RectTransform space)
        {
            var c = new Vector3[4];
            rt.GetWorldCorners(c);
            Vector3 a = space.InverseTransformPoint(c[0]), b = space.InverseTransformPoint(c[2]);
            return Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
        }

        static Rect OnPhone(RectTransform rt, RectTransform frame, Vector2 corner)
        {
            Rect r = RectIn(rt, frame), f = frame.rect;
            var from = new Vector2(Mathf.Lerp(f.xMin, f.xMax, corner.x), Mathf.Lerp(f.yMin, f.yMax, corner.y));
            var to = new Vector2(corner.x * 1080f, corner.y * 1920f);
            return new Rect(r.position - from + to, r.size);
        }

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

        static Transform OnStreet(Transform t, Transform street)
        {
            while (t != null && t.parent != street) t = t.parent;
            return t;
        }

        [Category("Slow")]
        [Test]
        public void No_Decoration_On_Any_Tile_By_A_Station_Covers_Its_Level_Plate_Or_Its_Pad()
        {
            GameState s = runner.State;
            for (int i = 0; i < runner.Defs.Count; i++) s.Levels[i] = 1;
            s.Plots = runner.Defs.Count;
            s.GardenXp = Garden.XpForLevel(9);
            GardenGrid.Settle(runner.Defs, s);
            screen.RebuildUi();
            screen.Shell.StartGame();
            screen.Refresh();
            GardenGrid grid = s.Grid;
            var near = new List<Hex>();
            for (int i = 0; i < runner.Defs.Count; i++)
                foreach (Hex h in GardenGrid.Neighbours(grid.TileOf(GardenGrid.StationId(runner.Defs[i])).Value))
                    if (grid.OccupantAt(h.Q, h.R) == null && grid.InLawn(h.Q, h.R) && !GardenGrid.IsReserved(h) && !near.Contains(h)) near.Add(h);
            Assert.Greater(near.Count, Decor.Spots, "free tiles by the stations");

            var covered = new List<string>();
            int reaching = 0;
            foreach (DecorKind kind in Decor.Kinds)
                for (int start = 0; start < near.Count; start += Decor.Spots)
                {
                    for (int n = 0; n < Decor.Spots; n++) { s.DecorSpots[n] = null; grid.Remove(GardenGrid.DecorId(n)); }
                    for (int n = 0; n < Decor.Spots && start + n < near.Count; n++)
                    {
                        s.DecorSpots[n] = kind.Id;
                        Assert.IsTrue(grid.Place(GardenGrid.DecorId(n), near[start + n].Q, near[start + n].R));
                    }
                    screen.RebuildUi();
                    screen.Refresh();
                    Layout();
                    Transform street = root.transform.Find("Canvas/Garden/Content");
                    var plates = new List<RectTransform>();
                    foreach (Transform child in street)
                    {
                        if (!child.name.StartsWith("Station ") || !child.gameObject.activeInHierarchy) continue;
                        foreach (string part in new[] { "Level", "Pad" })
                        {
                            Transform t = child.Find(part);
                            if (t != null && t.gameObject.activeInHierarchy) plates.Add((RectTransform)t);
                        }
                    }
                    Assert.GreaterOrEqual(plates.Count, runner.Defs.Count, "every station's plate was measured");
                    for (int n = 0; n < Decor.Spots && start + n < near.Count; n++)
                    {
                        var item = (RectTransform)street.Find("Spot " + n + "/Item");
                        Assert.IsTrue(item.gameObject.activeInHierarchy, kind.Id + " stands on spot " + n);
                        Rect art = WorldRect(item);
                        foreach (RectTransform plate in plates)
                        {
                            if (!art.Overlaps(WorldRect(plate))) continue;
                            reaching++;
                            if (OnStreet(item, street).GetSiblingIndex() > OnStreet(plate, street).GetSiblingIndex())
                                covered.Add(kind.Id + " at " + near[start + n].Q + "," + near[start + n].R + " covers " + Path(plate));
                        }
                    }
                }
            Assert.Greater(reaching, 0, "some decoration reaches a plate or a pad, so the drawing order is what keeps them readable");
            Assert.IsEmpty(covered, covered.Count + " covered:\n" + string.Join("\n", covered));
        }

        [Category("Slow")]
        [Test]
        public void After_Any_Station_Move_The_Arranging_Mode_Takes_No_Two_Station_Plates_Overlap()
        {
            Motion.SetReduced(true);
            GameState s = runner.State;
            for (int i = 0; i < 4; i++) s.Levels[i] = 1 + i;
            GardenGrid.Settle(runner.Defs, s);
            screen.RebuildUi();
            screen.Shell.StartGame();
            screen.Refresh();
            GardenGrid grid = s.Grid;
            int placed = 0, refused = 0;
            var overlaps = new List<string>();
            for (int i = 0; i < 4; i++)
            {
                string id = GardenGrid.StationId(runner.Defs[i]);
                Hex home = grid.TileOf(id).Value;
                foreach (Hex to in grid.EmptyTiles())
                {
                    bool close = false;
                    for (int j = 0; j < 4; j++)
                        if (j != i && GardenGrid.Distance(to, grid.TileOf(GardenGrid.StationId(runner.Defs[j])).Value) <= Expansion.StationSpacing) close = true;
                    if (!close) continue;
                    if (VuonTra.Core.Layout.Drop(runner.Defs, s, home, to) != LayoutDrop.Placed) { refused++; continue; }
                    placed++;
                    screen.RebuildUi();
                    screen.Refresh();
                    Layout();
                    var plates = GardenPlates().FindAll(p => p.parent.name.StartsWith("Station "));
                    Assert.GreaterOrEqual(plates.Count, 4, "every station's level plate is out");
                    for (int a = 0; a < plates.Count; a++)
                        for (int b = a + 1; b < plates.Count; b++)
                            if (WorldRect(plates[a]).Overlaps(WorldRect(plates[b])))
                                overlaps.Add(id + " on " + to + ": " + Path(plates[a]) + " over " + Path(plates[b]));
                    Assert.AreEqual(LayoutDrop.Placed, VuonTra.Core.Layout.Drop(runner.Defs, s, to, home), "and back home");
                }
            }
            Assert.IsEmpty(overlaps, overlaps.Count + " overlapping:\n" + string.Join("\n", overlaps));
            Assert.Greater(placed, 0, "some tile near a station takes it");
            Assert.Greater(refused, 0, "and the ones next to a station do not");
        }

        static readonly string[] OffGridProps = { "Jar", "Jar/Plate", "Basket", "Basket/Plate", "Mortar", "Mortar/Plate", "PondTap", "PondTap/Plate", "Tank" };

        static Rect GardenRect(RectTransform rt, RectTransform street)
        {
            Rect r = RectIn(rt, street);
            r.position -= street.rect.min;
            return r;
        }

        [Test]
        public void Every_Tile_Under_The_Jar_The_Basket_Or_The_Mortar_Or_Their_Plates_Is_Reserved_And_No_Other()
        {
            GameState s = runner.State;
            s.GardenXp = Garden.XpForLevel(9);
            screen.Shell.StartGame();
            screen.Refresh();
            Layout();
            var street = (RectTransform)root.transform.Find("Canvas/Garden/Content");
            Rect jar = GardenRect((RectTransform)street.Find("Jar"), street);
            Assert.AreEqual(GardenScreen.FireflyJarSpot.x, jar.center.x, 0.5f, "garden units: the jar stands on its spot");
            Assert.AreEqual(GardenScreen.FireflyJarSpot.y + 80f, jar.center.y, 0.5f);

            const float Hair = 16f;
            var depth = new Dictionary<Hex, float>();
            var under = new Dictionary<Hex, string>();
            var basketBefore = new Rect();
            foreach (string path in OffGridProps)
            {
                var rt = (RectTransform)street.Find(path);
                Assert.IsTrue(rt.gameObject.activeInHierarchy, path + " is out");
                Rect r = GardenRect(rt, street);
                if (path == "Basket") basketBefore = r;
                for (float x = r.xMin; x <= r.xMax; x += 2f)
                    for (float y = r.yMin; y <= r.yMax; y += 2f)
                    {
                        float d = Mathf.Min(Mathf.Min(x - r.xMin, r.xMax - x), Mathf.Min(y - r.yMin, r.yMax - y));
                        Hex h = GardenScreen.WorldToCell(new Vector2(x, y));
                        if (!depth.TryGetValue(h, out float was) || d > was) { depth[h] = d; under[h] = path; }
                    }
            }
            var missing = new List<string>();
            foreach (KeyValuePair<Hex, float> cell in depth)
            {
                Hex h = cell.Key;
                if (cell.Value <= Hair || GardenGrid.IsReserved(h)) continue;
                string holder = s.Grid.OccupantAt(h.Q, h.R);
                if (holder == GardenGrid.MortarId || holder == GardenGrid.CounterId || holder == GardenGrid.ShopId) continue;
                if ((holder == GardenGrid.BasketId || holder == GardenGrid.BasketPlateId) && under[h].StartsWith("Basket")) continue;
                missing.Add(h + " under " + under[h] + " by " + cell.Value);
            }
            Assert.IsEmpty(missing, "not reserved:\n" + string.Join("\n", missing));
            foreach (Hex h in GardenGrid.ReservedTiles)
                Assert.IsTrue(depth.ContainsKey(h), h + " is reserved, but no prop or plate reaches its cell");
            Assert.AreEqual(GardenGrid.MortarId, s.Grid.OccupantAt(1, -1), "the mortar holds its own tile");

            int leaf = Leaves.StationIndex(runner.Defs);
            Hex home = s.Grid.TileOf(GardenGrid.StationId(runner.Defs[leaf])).Value, basketHome = s.Grid.TileOf(GardenGrid.BasketId).Value;
            Assert.IsTrue(s.Grid.NearestEmptyApart(Expansion.StationSpacing, out Hex elsewhere));
            Assert.AreEqual(LayoutDrop.Placed, VuonTra.Core.Layout.Drop(runner.Defs, s, home, elsewhere));
            screen.RebuildUi();
            screen.Refresh();
            Layout();
            street = (RectTransform)root.transform.Find("Canvas/Garden/Content");
            Hex basketNow = s.Grid.TileOf(GardenGrid.BasketId).Value;
            Assert.AreEqual(1, GardenGrid.Distance(basketNow, elsewhere), "the basket went with its station (#119)");
            Assert.IsNull(s.Grid.OccupantAt(basketHome.Q, basketHome.R), "and gave its old tile back");
            Rect basketAfter = GardenRect((RectTransform)street.Find("Basket"), street);
            Vector2 shift = GardenScreen.CellToWorld(basketNow) - GardenScreen.CellToWorld(basketHome);
            Assert.Less(Vector2.Distance(basketBefore.center + shift, basketAfter.center), 0.5f, "drawn on its new tile as it was on the old: " + basketBefore + " then " + basketAfter);
            Rect body = GardenRect((RectTransform)street.Find("Basket"), street);
            for (float x = body.xMin + Hair; x <= body.xMax - Hair; x += 2f)
                for (float y = body.yMin + Hair; y <= body.yMax - Hair; y += 2f)
                {
                    Hex h = GardenScreen.WorldToCell(new Vector2(x, y));
                    Assert.AreEqual(GardenGrid.BasketId, s.Grid.OccupantAt(h.Q, h.R), "the basket reaches " + h);
                }
            Hex plate = GardenScreen.WorldToCell(GardenRect((RectTransform)street.Find("Basket/Plate"), street).center);
            Assert.AreEqual(GardenGrid.BasketPlateId, s.Grid.OccupantAt(plate.Q, plate.R), "its plate hangs over the tile it holds");
        }

        [Category("Slow")]
        [Test]
        public void No_Decoration_On_Any_Droppable_Tile_Or_Spot_Covers_The_Jar_The_Basket_Or_The_Mortar_s_Plate()
        {
            GameState s = runner.State;
            for (int i = 0; i < runner.Defs.Count; i++) s.Levels[i] = 1;
            s.Plots = runner.Defs.Count;
            s.GardenXp = Garden.XpForLevel(9);
            GardenGrid.Settle(runner.Defs, s);
            screen.RebuildUi();
            screen.Shell.StartGame();
            screen.Refresh();
            GardenGrid grid = s.Grid;
            var tiles = new List<Hex>();
            foreach (Hex h in screen.LayoutEmptyTiles())
                foreach (Hex kept in GardenGrid.ReservedTiles)
                    if (GardenGrid.Distance(h, kept) <= 2 && !tiles.Contains(h)) { tiles.Add(h); break; }
            for (int n = 0; n < Decor.Spots; n++)
            {
                Hex spot = GardenGrid.DefaultDecorTile(n);
                if (grid.OccupantAt(spot.Q, spot.R) == null && !tiles.Contains(spot)) tiles.Add(spot);
            }
            Assert.Greater(tiles.Count, Decor.Spots, "free tiles by the props");
            foreach (Hex h in tiles) Assert.IsFalse(GardenGrid.IsReserved(h), h + " is offered");

            List<string> Covered(List<Hex> on, bool beyondRules, int used, IEnumerable<string> platePaths)
            {
                var covered = new List<string>();
                foreach (DecorKind kind in Decor.Kinds)
                    for (int start = 0; start < on.Count; start += used)
                    {
                        for (int n = 0; n < used; n++) { s.DecorSpots[n] = null; grid.Remove(GardenGrid.DecorId(n)); }
                        for (int n = 0; n < used && start + n < on.Count; n++)
                        {
                            s.DecorSpots[n] = kind.Id;
                            Assert.IsTrue(grid.Place(GardenGrid.DecorId(n), on[start + n].Q, on[start + n].R, beyondRules), "placed on " + on[start + n]);
                        }
                        screen.RebuildUi();
                        screen.Refresh();
                        Layout();
                        Transform street = root.transform.Find("Canvas/Garden/Content");
                        var plates = new List<RectTransform>();
                        foreach (string path in platePaths)
                        {
                            var rt = (RectTransform)street.Find(path);
                            Assert.IsTrue(rt.gameObject.activeInHierarchy, path + " is out");
                            plates.Add(rt);
                        }
                        for (int n = 0; n < used && start + n < on.Count; n++)
                        {
                            var item = (RectTransform)street.Find("Spot " + n + "/Item");
                            Assert.IsTrue(item.gameObject.activeInHierarchy, kind.Id + " stands on spot " + n);
                            Rect art = WorldRect(item);
                            foreach (RectTransform plate in plates)
                                if (art.Overlaps(WorldRect(plate)) && OnStreet(item, street).GetSiblingIndex() > OnStreet(plate, street).GetSiblingIndex())
                                    covered.Add(kind.Id + " at " + on[start + n] + " covers " + Path(plate));
                        }
                    }
                return covered;
            }

            var plates = new[] { "Jar", "Jar/Plate", "Basket/Plate", "Mortar/Plate", "PondTap/Plate" };
            List<string> onPlates = Covered(tiles, false, Decor.Spots, plates);
            Assert.IsEmpty(onPlates, onPlates.Count + " covered:\n" + string.Join("\n", onPlates));
            var reserved = new List<Hex>(GardenGrid.ReservedTiles);
            Assert.IsNotEmpty(Covered(reserved, true, Decor.Spots, plates), "a decoration on (4,-3) covers the jar's plate");

            for (int n = 0; n < Decor.Spots; n++) { s.DecorSpots[n] = null; grid.Remove(GardenGrid.DecorId(n)); }
            int leaf = Leaves.StationIndex(runner.Defs);
            Hex station = grid.TileOf(GardenGrid.StationId(runner.Defs[leaf])).Value;
            Assert.IsTrue(grid.NearestEmptyApart(Expansion.StationSpacing, out Hex elsewhere));
            Assert.AreEqual(LayoutDrop.Placed, VuonTra.Core.Layout.Drop(runner.Defs, s, station, elsewhere), "the leaf station moves and its basket with it");
            const int Empty = Decor.Spots - 1;
            s.DecorOwned[0] = Decor.Spots;
            GardenGrid.SettleDecor(s);
            Hex plus = grid.TileOf(GardenGrid.DecorId(Empty)).Value, basket = grid.TileOf(GardenGrid.BasketId).Value;
            Assert.AreEqual(GardenGrid.DefaultDecorTile(Empty), plus, "the empty spot shows its plus on its own tile");
            var beside = new List<Hex>();
            foreach (Hex h in screen.LayoutEmptyTiles())
            {
                Assert.AreNotEqual(plus, h, "the plus's tile is not offered");
                Assert.AreNotEqual(basket, h, "nor the basket's");
                if ((GardenGrid.Distance(h, plus) <= 2 || GardenGrid.Distance(h, basket) <= 2) && !beside.Contains(h)) beside.Add(h);
            }
            Assert.Greater(beside.Count, Empty, "free tiles by the plus and the basket");
            List<string> onHeld = Covered(beside, false, Empty, new[] { "Basket/Plate", "Spot " + Empty + "/Plate" });
            Assert.IsEmpty(onHeld, onHeld.Count + " covered:\n" + string.Join("\n", onHeld));
        }

        [Test]
        public void The_Guest_Pill_Covers_No_Garden_Plate_As_The_View_Rests_On_The_Teapot()
        {
            screen.Shell.StartGame();
            runner.State.GardenXp = Garden.XpForLevel(5);
            runner.PlaceOrder();
            screen.Refresh();
            screen.LookAt(screen.StationSpot(0));
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

        [TestCase(GardenScreen.ZoomLevel.Default, "EdgeUp")]
        [TestCase(GardenScreen.ZoomLevel.Near, "EdgeUp")]
        [TestCase(GardenScreen.ZoomLevel.Default, "EdgeLeft")]
        [TestCase(GardenScreen.ZoomLevel.Near, "EdgeLeft")]
        [TestCase(GardenScreen.ZoomLevel.Default, "EdgeRight")]
        [TestCase(GardenScreen.ZoomLevel.Near, "EdgeRight")]
        public void The_Edge_Pills_Cover_No_Garden_Plate_As_The_View_Rests_On_The_Teapot(GardenScreen.ZoomLevel zoom, string edge)
        {
            screen.Shell.StartGame();
            runner.State.GardenXp = Garden.XpForLevel(5);
            screen.Refresh();
            screen.SetZoom(zoom, false);
            screen.LookAt(screen.StationSpot(0));
            Layout();
            var pill = (RectTransform)root.transform.Find("Canvas/" + edge);
            pill.gameObject.SetActive(true);
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
            Assert.IsEmpty(covered, edge + " " + p + " covers: " + string.Join(", ", covered));
        }

        [TestCase("vi", false, GardenScreen.ZoomLevel.Default)]
        [TestCase("en", false, GardenScreen.ZoomLevel.Default)]
        [TestCase("vi", true, GardenScreen.ZoomLevel.Default)]
        [TestCase("en", true, GardenScreen.ZoomLevel.Default)]
        [TestCase("vi", false, GardenScreen.ZoomLevel.Near)]
        [TestCase("en", false, GardenScreen.ZoomLevel.Near)]
        [TestCase("vi", true, GardenScreen.ZoomLevel.Near)]
        [TestCase("en", true, GardenScreen.ZoomLevel.Near)]
        public void The_Guest_s_Order_Bubble_Covers_No_Plate_Nor_The_Shop_Nor_The_Guest(string code, bool large, GardenScreen.ZoomLevel zoom)
        {
            Motion.SetReduced(true);
            L10n.SetLocale(code);
            UiKit.SetLargeText(large);
            runner.State.GardenXp = Garden.XpForLevel(5);
            runner.State.Order = new Order { Id = 1, Guest = RabbitKinds.All[0].Id, Left = 40, RewardSeconds = Orders.RewardSeconds };
            runner.State.Order.Lines.Add(new OrderLine { Station = 0, Count = 3 });
            runner.State.Order.Lines.Add(new OrderLine { Station = 2, Count = 3 });
            screen.RebuildUi();
            screen.Shell.StartGame();
            screen.Refresh();
            screen.SetZoom(zoom, false);
            screen.LookAt(screen.GuestSpot);
            Layout();
            Assert.IsTrue(screen.GuestAtCounter, "the guest has waited a while, so it stands at the counter");
            var street = (RectTransform)root.transform.Find("Canvas/Garden/Content");
            var view = (RectTransform)root.transform.Find("Canvas/Garden");
            var bubble = (RectTransform)street.Find("Guest/Bubble");
            Assert.IsTrue(bubble.gameObject.activeInHierarchy, "the order is up in its bubble");
            Assert.IsTrue(street.Find("PondTap/Plate").gameObject.activeInHierarchy, "the pond's plate is out");
            Assert.IsTrue(street.Find("Jar/Plate").gameObject.activeInHierarchy, "the jar's plate is out");
            var shop = (RectTransform)street.Find("Shop");
            Assert.IsNotNull(shop, "the tea shop stands by the counter");

            Rect b = OnPhone(bubble, view, Vector2.zero);
            var kept = new List<RectTransform>(GardenPlates()) { shop, (RectTransform)street.Find("Guest/Art") };
            var covered = new List<string>();
            foreach (RectTransform other in kept)
            {
                Rect r = OnPhone(other, view, Vector2.zero);
                if (r.Overlaps(b)) covered.Add(Path(other) + " " + r);
            }
            Assert.IsEmpty(covered, "the bubble " + b + " covers: " + string.Join(", ", covered));
            Rect g = GardenRect(bubble, street);
            Assert.LessOrEqual(g.xMax, GardenScreen.ReadableRight, "the bubble " + g + " stays short of the trees at the edge");
            Assert.GreaterOrEqual(g.xMin, 24f, "and inside the left edge");
            Rect guest = GardenRect((RectTransform)street.Find("Guest/Art"), street), tail = GardenRect((RectTransform)bubble.Find("Tail"), street);
            Assert.That(tail.center.x, Is.InRange(guest.xMin, guest.xMax), "the bubble's tail " + tail + " still points at the guest " + guest);
        }

        [TestCase("vi", false)]
        [TestCase("en", false)]
        [TestCase("vi", true)]
        [TestCase("en", true)]
        public void The_Album_Set_Lines_And_The_Gold_Frame_Fit(string code, bool large)
        {
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

        [TestCase("vi", false, 0)]
        [TestCase("en", false, 0)]
        [TestCase("vi", true, 0)]
        [TestCase("en", true, 0)]
        [TestCase("vi", false, 1)]
        [TestCase("en", false, 1)]
        [TestCase("vi", true, 1)]
        [TestCase("en", true, 1)]
        public void Every_Widen_The_Garden_Sign_In_The_Opening_View_Reads_Whole_24_Inside_The_Screen(string code, bool large, int segments)
        {
            Motion.SetReduced(true);
            L10n.SetLocale(code);
            UiKit.SetLargeText(large);
            GameState s = runner.State;
            s.Plots = Expansion.PlotsForSegments(runner.Defs, segments);
            for (int i = 0; i < s.Plots; i++) s.Levels[i] = 1;
            MistSegment next = Expansion.Next(runner.Defs, s);
            s.GardenXp = Garden.XpForLevel(next.NeedLevel);
            while (s.Rabbits.Count < next.NeedRabbits) s.Rabbits.Add(new Rabbit { Id = ++s.NextRabbitId, Tier = 0, Name = "R" + s.NextRabbitId });
            foreach (RabbitKind kind in RabbitKinds.All) if (s.SeenKinds.Count < next.NeedKinds) s.SeenKinds.Add(kind.Id);
            s.Coins = Expansion.CostOfNext(runner.Defs, s) + new BigNum(1);
            GardenGrid.Settle(runner.Defs, s);
            screen.RebuildUi();
            screen.Shell.StartGame();
            for (int i = 0; i < 45; i++) screen.Advance(1f / 30f);
            Assert.AreEqual(GardenScreen.ZoomLevel.Default, screen.Zoom);
            Layout();
            var street = (RectTransform)root.transform.Find("Canvas/Garden/Content");
            var view = (RectTransform)root.transform.Find("Canvas/Garden");
            Vector2 pot = (Vector2)view.InverseTransformPoint(((RectTransform)street.Find("Station " + runner.Defs[0].Id)).position) - view.rect.min;
            Assert.AreEqual(540f, pot.x, 1f, "the opening view: the teapot in the middle");

            string expand = L10n.T("garden.expand");
            var signs = new List<RectTransform>();
            Transform sign = street.Find("Forest Sign");
            Assert.IsTrue(sign.gameObject.activeSelf, "the garden can widen, so the sign is up");
            signs.Add((RectTransform)sign.Find("Board"));
            for (int i = 0; i < runner.Defs.Count; i++)
            {
                Transform front = street.Find("Station " + runner.Defs[i].Id + " Front");
                if (front == null || !front.gameObject.activeInHierarchy) continue;
                if (front.Find("Level/Text").GetComponent<Text>().text != expand) continue;
                float x = OnPhone((RectTransform)street.Find("Station " + runner.Defs[i].Id), view, Vector2.zero).center.x;
                if (x >= 0f && x <= 1080f) signs.Add((RectTransform)front.Find("Level"));
            }
            Assert.GreaterOrEqual(signs.Count, segments == 1 ? 2 : 1, "the land plate on the first segment counts too");
            var cut = new List<string>();
            foreach (RectTransform rt in signs)
            {
                Rect r = OnPhone(rt, view, Vector2.zero);
                if (r.xMin < 24f || r.xMax > 1080f - 24f || r.yMin < 24f || r.yMax > 1920f - 24f) cut.Add(Path(rt) + " " + r);
            }
            Assert.IsEmpty(cut, "cut off in the opening view:\n" + string.Join("\n", cut));
            Text words = sign.Find("Board/Face/Text").GetComponent<Text>();
            Assert.IsNull(Wrapped(words, 16), "the sign's words fit its board");
        }

        [TestCase("vi", false)]
        [TestCase("en", false)]
        [TestCase("vi", true)]
        [TestCase("en", true)]
        public void The_Home_Button_Says_Where_It_Goes_Is_Big_Enough_And_Its_Words_Fit(string code, bool large)
        {
            L10n.SetLocale(code);
            UiKit.SetLargeText(large);
            GameState s = runner.State;
            s.Plots = runner.Defs.Count;
            for (int i = 0; i < runner.Defs.Count; i++) s.Levels[i] = 1;
            GardenGrid.Settle(runner.Defs, s);
            screen.RebuildUi();
            screen.Shell.StartGame();
            for (int i = 0; i < 45; i++) screen.Advance(1f / 30f);
            screen.LookAt(screen.StationSpot(0) + new Vector2(1700f, 0f));
            screen.Advance(1f / 30f);
            Transform home = root.transform.Find("Canvas/Home");
            Assert.IsTrue(home.gameObject.activeSelf, "far to the right the way home shows");
            Layout();
            Text words = home.Find("Text") != null ? home.Find("Text").GetComponent<Text>() : null;
            Assert.IsNotNull(words, "the button says where it goes");
            Assert.AreEqual(L10n.T("edge.home"), words.text);
            Rect touch = TouchArea(home.GetComponent<Button>());
            Assert.GreaterOrEqual(Mathf.Min(touch.width, touch.height) + 0.01f, UiKit.MinTouch, "big enough to press: " + touch);
            Assert.IsNull(Wrapped(words, 18), "its words fit at a size that reads");
            var canvas = (RectTransform)root.transform.Find("Canvas");
            Rect r = OnPhone((RectTransform)home, canvas, new Vector2(1f, 0f));
            Assert.That(r.xMin, Is.GreaterThanOrEqualTo(24f), "inside the left edge: " + r);
            Assert.That(r.xMax, Is.LessThanOrEqualTo(1080f - 24f), "and keeps 24 from the right: " + r);
            Rect tray = OnPhone((RectTransform)root.transform.Find("Canvas/Tray"), canvas, Vector2.zero);
            Assert.GreaterOrEqual(r.yMin, tray.yMax, "above the tray " + tray + ": " + r);
            Rect pill = OnPhone((RectTransform)root.transform.Find("Canvas/EdgeRight"), canvas, new Vector2(1f, 0f));
            Assert.LessOrEqual(r.yMax, pill.yMin, "and under the side pill " + pill + ": " + r);
        }

        [TestCase("vi", false)]
        [TestCase("en", false)]
        [TestCase("vi", true)]
        [TestCase("en", true)]
        public void An_Idle_Line_At_Any_Station_Or_Seat_Of_A_Full_Garden_Covers_No_Plate(string code, bool large)
        {
            Motion.SetReduced(true);
            L10n.SetLocale(code);
            UiKit.SetLargeText(large);
            GameState s = runner.State;
            s.Plots = runner.Defs.Count;
            for (int i = 0; i < runner.Defs.Count; i++) s.Levels[i] = 1;
            s.GardenXp = Garden.XpForLevel(9);
            s.RestSpots = RestSpots.Max;
            GardenGrid.Settle(runner.Defs, s);
            for (int i = 0; i < runner.Defs.Count; i++) Rabbits.Assign(s, Rabbits.Arrive(s, 0, "T" + i, 0.1 * (i % 10)).Id, i);
            for (int k = 0; k < RestSpots.Max; k++) Rabbits.Arrive(s, 0, "N" + k, 0.1 * k);
            PhotoAsks.Decline(s);
            screen.RebuildUi();
            screen.Shell.StartGame();
            screen.Refresh();
            Layout();
            var street = (RectTransform)root.transform.Find("Canvas/Garden/Content");
            var covered = new List<string>();
            int spoke = 0;
            foreach (Rabbit r in new List<Rabbit>(s.Rabbits))
            {
                var looks = new List<Vector2>();
                if (r.Station >= 0) looks.Add(screen.StationSpot(r.Station));
                else for (int k = 0; k < RestSpots.Max; k++) looks.Add(screen.RestSpot(k));
                bool said = false;
                foreach (Vector2 look in looks)
                {
                    screen.LookAt(look);
                    said = screen.ChatterBy(r.Id);
                    if (said) break;
                }
                if (!said) continue;
                spoke++;
                Layout();
                var line = (RectTransform)street.Find("ChatLine");
                Assert.IsTrue(line.gameObject.activeSelf);
                Rect b = GardenRect(line, street);
                foreach (RectTransform plate in GardenPlates())
                {
                    if (plate.parent.name.StartsWith("Spot ")) continue;
                    Rect p = GardenRect(plate, street);
                    if (p.Overlaps(b)) covered.Add(r.Name + (r.Station >= 0 ? " at " + runner.Defs[r.Station].Id : " resting") + ": the line " + b + " covers " + Path(plate) + " " + p);
                }
            }
            Assert.GreaterOrEqual(spoke, (runner.Defs.Count + RestSpots.Max) * 2 / 3, "most rabbits still find room for a line");
            Assert.IsEmpty(covered, covered.Count + " covered:\n" + string.Join("\n", covered));
        }
    }
}
