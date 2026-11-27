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
    public class FeelLaneScreenTests
    {
        const float LeastWorldText = 24f, ScreenHalf = 960f;

        GameObject root;
        GameRunner runner;
        GardenScreen screen;

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
            screen.Shell.StartGame();
            screen.Refresh();
            Step(1.5f);
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
            Assert.Less(card.yMax - Frame.rect.yMin, ScreenHalf, "the banner keeps out of the middle of the garden, where the rabbits work");
            Assert.AreEqual(banner.parent.childCount - 1, banner.GetSiblingIndex(), "a floating number passes under the banner, never over its words");
            StringAssert.StartsWith(L10n.T("moment.gift.title", ""), root.transform.Find("Canvas/Feel/Banner/Title").GetComponent<Text>().text);
        }

        [Test]
        public void With_A_Sheet_Open_The_Banner_Rests_On_The_Sheet()
        {
            screen.OpenSheet(2);
            Step(0.6f);
            screen.Feel.Moment("Title", "Sub");
            Step(0.5f);
            Canvas.ForceUpdateCanvases();
            Rect card = InCanvas((RectTransform)root.transform.Find("Canvas/Feel/Banner"));
            Assert.GreaterOrEqual(card.yMin - Frame.rect.yMin, screen.SheetHeight, "the banner never hides the sheet's rows");
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
            screen.RebuildUi();
            if (screen.Shell.State != ShellState.Playing) screen.Shell.Resume();
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
