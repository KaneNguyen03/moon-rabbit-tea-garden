using System.Collections.Generic;
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
    [TestFixture(true)]
    [TestFixture(false)]
    public class FeelLaneScreenTests
    {
        const float LeastWorldText = 24f, PhoneW = 1080f, PhoneH = 1920f;

        readonly bool portrait;
        GameObject root;
        GameRunner runner;
        GardenScreen screen;

        public FeelLaneScreenTests(bool portrait)
        {
            this.portrait = portrait;
        }

        [SetUp]
        public void SetUp()
        {
            L10n.SetLocale("en");
            Motion.SetReduced(false);
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
            Phone();
            screen.Shell.StartGame();
            screen.Refresh();
            Step(1.5f);
        }

        void Phone()
        {
            if (!portrait) return;
            var canvas = root.transform.Find("Canvas").GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var rt = (RectTransform)canvas.transform;
            rt.localScale = Vector3.one;
            rt.sizeDelta = new Vector2(PhoneW, PhoneH);
            Canvas.ForceUpdateCanvases();
            Assert.AreEqual(PhoneH, rt.rect.height, 0.01f);
        }

        void Rebuild()
        {
            screen.RebuildUi();
            Phone();
            if (screen.Shell.State != ShellState.Playing) screen.Shell.Resume();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(root);
            UiKit.SetLargeText(false);
            PlayerPrefs.DeleteKey("text.large");
            Motion.SetReduced(false);
            L10n.SetLocale("vi");
        }

        void Step(float seconds)
        {
            for (float t = 0f; t < seconds; t += 1f / 30f) screen.Advance(1f / 30f);
        }

        RectTransform Frame => (RectTransform)root.transform.Find("Canvas");

        Rect InCanvas(RectTransform rt)
        {
            var c = new Vector3[4];
            rt.GetWorldCorners(c);
            Vector3 a = Frame.InverseTransformPoint(c[0]), b = Frame.InverseTransformPoint(c[2]);
            return Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
        }

        Rect Inked(Text text)
        {
            Rect box = InCanvas(text.rectTransform);
            float scale = box.width / Mathf.Max(1f, text.rectTransform.rect.width);
            float w = Mathf.Min(box.width, text.preferredWidth * scale), h = Mathf.Min(box.height, text.preferredHeight * scale);
            return new Rect(box.center.x - w / 2f, box.center.y - h / 2f, w, h);
        }

        List<Text> Gains()
        {
            var gains = new List<Text>();
            foreach (Transform t in root.transform.Find("Canvas/Feel"))
                if (t.name == "Gain" && t.gameObject.activeInHierarchy) gains.Add(t.GetComponent<Text>());
            return gains;
        }

        List<KeyValuePair<string, Rect>> HudWords()
        {
            var words = new List<KeyValuePair<string, Rect>>();
            foreach (Text text in root.transform.Find("Canvas/Hud").GetComponentsInChildren<Text>(false))
                if (!string.IsNullOrEmpty(text.text)) words.Add(new KeyValuePair<string, Rect>(text.name + " \"" + text.text + "\"", Inked(text)));
            return words;
        }

        void ClaimDailyQuest()
        {
            for (int i = 0; i < 50; i++) root.transform.Find("Canvas/Garden/Content/Station am-tra").GetComponent<Button>().onClick.Invoke();
            screen.Refresh();
            Button claim = root.transform.Find("Canvas/Sheet/Panel/Quests/Content/Quest ngay-cham/Claim").GetComponent<Button>();
            Assert.IsTrue(claim.interactable);
            claim.onClick.Invoke();
        }

        [Test]
        public void A_Claimed_Quest_s_Number_Floats_Under_The_Hud_And_Never_Over_Its_Words()
        {
            ClaimDailyQuest();
            Assert.IsNotNull(runner.LastQuestItem, "a daily quest gives a gift");
            var over = new List<string>();
            bool seen = false;
            for (int frame = 0; frame < 60; frame++)
            {
                screen.Advance(1f / 30f);
                Canvas.ForceUpdateCanvases();
                foreach (Text gain in Gains())
                {
                    if (gain.color.a < 0.05f) continue;
                    seen = true;
                    Rect inked = Inked(gain);
                    foreach (KeyValuePair<string, Rect> word in HudWords())
                        if (inked.Overlaps(word.Value)) over.Add("frame " + frame + ": \"" + gain.text + "\" " + inked + " over " + word.Key + " " + word.Value);
                }
            }
            Assert.IsTrue(seen, "the carrots' number floats up");
            Assert.IsEmpty(over, string.Join("\n", over));
        }

        [Test]
        public void The_Gift_Banner_Rests_Above_The_Tray_Clear_Of_The_Middle_Of_The_Garden_And_Over_Every_Number()
        {
            ClaimDailyQuest();
            Step(0.9f);
            Assert.IsTrue(screen.Feel.BannerVisible);
            Canvas.ForceUpdateCanvases();
            var banner = (RectTransform)root.transform.Find("Canvas/Feel/Banner");
            Rect card = InCanvas(banner), tray = InCanvas(screen.ProductTray.Root);
            Assert.GreaterOrEqual(card.yMin, tray.yMax, "the banner stands on the tray, not over it");
            Assert.Less(card.yMax - Frame.rect.yMin, Frame.rect.height / 2f, "the banner keeps out of the middle of the garden, where the rabbits work");
            Assert.AreEqual(banner.parent.childCount - 1, banner.GetSiblingIndex(), "a floating number passes under the banner, never over its words");
            StringAssert.StartsWith(L10n.T("moment.gift.title", ""), root.transform.Find("Canvas/Feel/Banner/Title").GetComponent<Text>().text);
        }

        string CoversTheSheet()
        {
            Canvas.ForceUpdateCanvases();
            Rect card = InCanvas((RectTransform)root.transform.Find("Canvas/Feel/Banner"));
            Rect panel = InCanvas((RectTransform)root.transform.Find("Canvas/Sheet/Panel")), rows = InCanvas((RectTransform)root.transform.Find("Canvas/Sheet/Panel/Quests"));
            var sheet = Rect.MinMaxRect(panel.xMin, rows.yMin, panel.xMax, panel.yMax);
            if (card.Overlaps(sheet)) return "the card " + card + " covers the sheet's header and rows " + sheet + " at height " + screen.SheetHeight;
            Text sub = root.transform.Find("Canvas/Feel/Banner/Sub").GetComponent<Text>();
            float onScreen = sub.fontSize * InCanvas(sub.rectTransform).height / sub.rectTransform.rect.height;
            if (screen.Feel.BannerFit > 0.99f && onScreen < LeastWorldText - 0.5f) return "the card's words read at " + onScreen;
            return null;
        }

        [Test]
        public void With_A_Sheet_Open_The_Banner_Never_Covers_Its_Header_Or_Rows()
        {
            screen.OpenSheet(2);
            Step(0.6f);
            screen.Feel.Moment("Title", "Sub");
            Step(0.5f);
            Assert.IsNull(CoversTheSheet());
        }

        [Test]
        public void With_The_Sheet_Pulled_To_Full_Height_The_Banner_Rests_Over_The_Tab_Bar_And_Still_Reads()
        {
            screen.OpenSheet(2);
            Step(0.6f);
            screen.DragSheet(GardenScreen.SheetFull);
            screen.ReleaseSheet();
            Step(0.6f);
            Assert.AreEqual(GardenScreen.SheetFull, screen.SheetHeight, 0.5f, "the sheet stands at full height");
            screen.Feel.Moment(L10n.T("moment.gift.title", "Silver Bell"), "Silver ★ tier · +10% income at its station · equip it in the Rabbits tab");
            Step(0.5f);
            Assert.IsNull(CoversTheSheet());
            Rect card = InCanvas((RectTransform)root.transform.Find("Canvas/Feel/Banner"));
            Rect bar = InCanvas((RectTransform)root.transform.Find("Canvas/TabBar"));
            Assert.GreaterOrEqual(card.yMin, bar.yMin - 0.5f, "the card lies on the tab bar");
            foreach (CanvasGroup lift in root.transform.Find("Canvas/TabBar").GetComponentsInChildren<CanvasGroup>())
                if (lift.name == "Lift" && lift.alpha > 0.5f) Assert.GreaterOrEqual(card.yMax, InCanvas((RectTransform)lift.transform).yMax - 0.5f, "and over the chosen tab's raised disc, which would poke out above it");
            Text sub = root.transform.Find("Canvas/Feel/Banner/Sub").GetComponent<Text>();
            Assert.GreaterOrEqual(sub.fontSize * InCanvas(sub.rectTransform).height / sub.rectTransform.rect.height, LeastWorldText - 0.5f, "its smaller words still read");
            Step(1.5f);
            Assert.IsFalse(screen.Feel.BannerVisible, "and it leaves on time");
        }

        [Test]
        public void A_Card_Already_Up_Moves_Off_A_Sheet_Pulled_Up_Under_It()
        {
            screen.OpenSheet(2);
            Step(0.6f);
            screen.Feel.Moment("Title", "Sub");
            var faults = new List<string>();
            for (int frame = 0; frame < 40; frame++)
            {
                if (frame == 10) { screen.DragSheet(GardenScreen.SheetFull); screen.ReleaseSheet(); }
                screen.Advance(1f / 30f);
                if (frame < 12 || !screen.Feel.BannerVisible) continue;
                string fault = CoversTheSheet();
                if (fault != null) faults.Add("frame " + frame + ": " + fault);
            }
            Assert.IsEmpty(faults, string.Join("\n", faults));
        }

        [Test]
        public void The_Banner_Still_Leaves_On_Its_Own_In_Two_Seconds()
        {
            screen.Feel.Moment("Title", "Sub");
            Step(0.5f);
            Assert.IsTrue(screen.Feel.BannerVisible);
            Step(1.5f);
            Assert.IsFalse(screen.Feel.BannerVisible);
        }

        [Test]
        public void An_Ingredient_On_Its_Way_Between_Stations_Passes_Under_Every_Station_s_Sign()
        {
            GameState s = runner.State;
            for (int i = 0; i < 4; i++) s.Levels[i] = 5;
            screen.Refresh();
            var street = root.transform.Find("Canvas/Garden/Content");
            int couriers = 0;
            for (int frame = 0; frame < 30 * 20; frame++)
            {
                screen.Advance(1f / 30f);
                int lowestFront = int.MaxValue;
                foreach (Transform child in street)
                    if (child.name.EndsWith(" Front")) lowestFront = Mathf.Min(lowestFront, child.GetSiblingIndex());
                foreach (Transform child in street)
                {
                    if (child.name != "Courier" || !child.gameObject.activeSelf) continue;
                    couriers++;
                    Assert.Less(child.GetSiblingIndex(), lowestFront, "a courier is drawn under the signs, at frame " + frame);
                }
            }
            Assert.Greater(couriers, 0, "ingredients set off between the open stations");
        }

        [TestCase("vi", false)]
        [TestCase("en", false)]
        [TestCase("vi", true)]
        [TestCase("en", true)]
        public void Add_A_Rabbit_Reads_At_Least_24_On_Screen_At_The_Default_Zoom_Within_Its_Pad(string code, bool large)
        {
            L10n.SetLocale(code);
            UiKit.SetLargeText(large);
            Rebuild();
            Step(0.5f);
            Assert.AreEqual(GardenScreen.ZoomLevel.Default, screen.Zoom);
            Canvas.ForceUpdateCanvases();
            int checkedSigns = 0;
            for (int i = 0; i < runner.Defs.Count; i++)
            {
                Transform front = root.transform.Find("Canvas/Garden/Content/Station " + runner.Defs[i].Id + " Front");
                if (front == null) continue;
                Transform tag = front.Find("Pad/Tag");
                Text text = tag.Find("Text").GetComponent<Text>();
                StringAssert.StartsWith(L10n.T("pad.hint"), text.text);
                float onScreen = text.fontSize * InCanvas(text.rectTransform).height / text.rectTransform.rect.height;
                Assert.GreaterOrEqual(onScreen, LeastWorldText - 0.5f, runner.Defs[i].Id + ": the sign's words on screen");
                Assert.LessOrEqual(text.preferredHeight, text.rectTransform.rect.height + 0.5f, runner.Defs[i].Id + ": the words fit the sign");
                Assert.LessOrEqual(InCanvas((RectTransform)tag).width, InCanvas((RectTransform)tag.parent).width + 0.5f, runner.Defs[i].Id + ": the sign stays within its pad, clear of the station's prop");
                checkedSigns++;
            }
            Assert.Greater(checkedSigns, 0);
        }
    }
}
