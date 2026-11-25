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
    [MonetizationOn]
    public partial class AccessibilityTests
    {
        long roundTwoClock;

        static long FairSaturday()
        {
            FairRun run = Fairs.RunOn(GiftCodes.Day(2027, 5, Fairs.StartDay)).Value;
            for (long d = run.Start; d <= run.End; d++)
                if (Seasons.DateOf(d).DayOfWeek == System.DayOfWeek.Saturday) return d;
            return run.Start;
        }

        void BuildRoundTwoGarden()
        {
            Object.DestroyImmediate(root);
            long day = FairSaturday();
            roundTwoClock = day * 86400L + 19 * 3600L + 600L;
            var catalog = AssetDatabase.LoadAssetAtPath<GameCatalog>("Assets/VuonTra/Content/GameCatalog.asset");
            root = new GameObject("Game");
            var cam = new GameObject("Cam", typeof(Camera));
            cam.transform.SetParent(root.transform);
            runner = root.AddComponent<GameRunner>();
            runner.Catalog = catalog;
            runner.DayClock = () => day;
            runner.LocalClock = () => roundTwoClock;
            runner.Store = new FakeStorefront();
            runner.Analytics = new MemoryAnalytics();

            GameState s = GameState.NewGame(catalog.BuildDefs());
            int[] levels = { 112, 64, 41, 27, 18, 9 };
            for (int i = 0; i < levels.Length; i++) s.Levels[i] = levels[i];
            s.Plots = 8;
            s.GardenXp = Garden.XpForLevel(Recipes.UnlockLevel + 2);
            s.HintSeen = true;
            s.TutorialStep = Tutorial.Steps;
            s.Coins = new BigNum(3.1e9);
            s.GoldenCarrots = 500;
            s.Clovers = 7;
            s.MoonJars = 2;
            s.RecipeLeaves = 86;
            s.Profile.Code = "VT-121PREV";
            Recipes.SetLevel(s, 0, 0, 4);
            Recipes.SetLevel(s, 0, 1, 6);
            Rabbits.GiftFirst(s, "Bông");
            Rabbit loved = Rabbits.ArriveKind(s, "tho-me", "Thỏ Tím Rất Dài Tên");
            Rabbits.AddBond(s, loved, Rabbits.PointsFor(6));
            s.FreshKinds.Clear();
            s.FirstDaySeen = day - 6;
            s.MaxDaySeen = day;
            s.WelcomeBack.SeenDay = day;
            s.MailDay = day;
            s.GiftDay = day;
            s.Fish[0] = 24; s.Fish[1] = 6; s.Fish[3] = 1;
            for (int n = 0; n < 22; n++) Aquarium.Release(s, 0);
            for (int n = 0; n < 5; n++) Aquarium.Release(s, 1);
            Aquarium.Release(s, 3);
            Aquarium.SeeFresh(s);
            runner.Init(s);
            s = runner.State;
            s.IntrosSeen.UnionWith(Intros.All);
            s.IntrosTried.UnionWith(Intros.All);
            foreach (QuestDef q in runner.QuestDefs) if (q.Daily) s.Claimed.Add(q.Id);
            s.WeekIndex = QuestChests.WeekOf(day);
            s.WeekPoints = QuestChests.MaxPoints;
            s.PassMonth = MonthPass.MonthOf(day);
            s.PassPoints = 12 * MonthPass.PointsPerTier;
            s.Fair.Coins = 265;
            runner.SeeNewDishes();

            screen = root.AddComponent<GardenScreen>();
            screen.Runner = runner;
            screen.Cam = cam.GetComponent<Camera>();
            screen.Raining = () => false;
            screen.Build();
            screen.Shell.StartGame();
            screen.Refresh();
            screen.Advance(1f / 30f);
            Layout();
            screen.Refresh();
        }

        void Press(string path)
        {
            Transform t = root.transform.Find(path);
            Assert.IsNotNull(t, "Missing UI object: " + path);
            t.GetComponent<Button>().onClick.Invoke();
        }

        void Show(string where, System.Action<string, List<Transform>> measure, System.Action open, System.Action close, params string[] paths)
        {
            open();
            screen.Advance(1f / 30f);
            Layout();
            var roots = new List<Transform>();
            foreach (string p in paths)
            {
                Transform t = root.transform.Find("Canvas/" + p);
                Assert.IsNotNull(t, where + ": missing " + p);
                if (t.gameObject.activeInHierarchy) roots.Add(t);
            }
            Assert.IsTrue(roots.Count > 0 && roots[0] == root.transform.Find("Canvas/" + paths[0]), where + " did not open " + paths[0]);
            measure(where, roots);
            close();
            screen.Advance(1f / 30f);
        }

        const string QuestsContent = "Canvas/Sheet/Panel/Quests/Content/";

        void EachRoundTwoCard(System.Action<string, List<Transform>> measure)
        {
            Motion.SetReduced(true);
            BuildRoundTwoGarden();
            GameState s = runner.State;
            long day = runner.TodayNumber;

            Show("first week day 7", measure, () => screen.FirstWeek.Open(), () => screen.FirstWeek.Close(), "NewcomerSheet", "Hud/Newcomer");
            Show("first week day 1", measure, () => { screen.FirstWeek.Open(); screen.FirstWeek.Select(1); }, () => screen.FirstWeek.Close(), "NewcomerSheet");

            screen.OpenSheet(2);
            screen.Refresh();
            Show("quests tab", measure, () => { }, () => { }, "Sheet/Panel/Quests");
            Show("day box", measure, () => Press(QuestsContent + "QuestBoxRow/Box"), () => screen.Prizes.Close(), "QuestPrizes");
            Show("week chest", measure, () => Press(QuestsContent + "QuestWeekRow/Track/Chest " + (QuestChests.Chests.Length - 1)), () => screen.Prizes.Close(), "QuestPrizes");
            Show("month pass last page", measure, () => { Press(QuestsContent + "MonthPassRow/Next"); Press(QuestsContent + "MonthPassRow/Next"); }, () => { },
                "Sheet/Panel/Quests/Content/MonthPassRow");
            MonthPass.Buy(s, day);
            screen.Refresh();
            Show("month pass bought", measure, () => { }, () => { }, "Sheet/Panel/Quests/Content/MonthPassRow");
            screen.CloseSheet();
            Show("month kinds", measure, () => screen.KindChooser.Open(MonthPass.MonthOf(day), null), () => screen.KindChooser.Close(), "MonthKinds");

            foreach (string id in Intros.All)
            {
                string reward = Intros.HasReward(id) ? L10n.T("intro.reward", Intros.Reward(runner.Defs, s, runner.Mods).Short(), Intros.RewardClovers) : "";
                Show("intro " + id, measure, () => screen.IntroCard.Show(id, runner.Catalog.Round, Palette.Lantern, reward), () => screen.IntroCard.Close(), "Intro");
            }
            Show("intro list", measure, () => screen.IntroList.Open(s), () => screen.IntroList.Close(), "Intros");

            foreach (OfferDef offer in Offers.All)
                Show("offer " + offer.Id, measure, () => screen.OpenOffer(offer), () => screen.OfferCard.Close(), "Offer");
            Show("golden packs", measure, () => screen.GoldenPacks.Open(runner.Store), () => screen.GoldenPacks.Close(), "GoldenPacks");
            PurchaseResult moon = default;
            runner.BuyMoonPass(r => moon = r);
            Assert.AreEqual(PurchaseOutcome.Paid, moon.Outcome);
            Show("moon pass bought", measure, () => screen.GoldenPacks.Open(runner.Store), () => { screen.GoldenPacks.Close(); screen.Refresh(); }, "GoldenPacks");
            Show("moon pass pill", measure, () => screen.Refresh(), () => { }, "Hud/MoonPass");

            Show("review ask", measure, () => screen.ReviewCard.Open(s.Profile.Code), () => screen.ReviewCard.Close(), "Review");
            Show("review feedback", measure, () => { screen.ReviewCard.Open(s.Profile.Code); screen.ReviewCard.ShowFeedback(); }, () => screen.ReviewCard.Close(), "Review");

            Show("recipe book", measure, () => screen.Book.Open(runner.Defs, s), () => screen.Book.Close(), "RecipeBook");
            for (int station = 0; station < runner.Defs.Count && station < runner.Catalog.Stations.Length; station++)
                for (int dish = 0; dish < Recipes.Dishes; dish++)
                {
                    StationData data = runner.Catalog.Stations[station];
                    int shown = dish;
                    Show("new dish " + data.Id + " " + dish, measure, () => screen.NewDishCard.Open(data, shown, 2), () => screen.NewDishCard.Close(), "NewDish");
                }

            Assert.IsTrue(runner.FairNow.HasValue, "a fair runs on the day the cards are opened");
            Show("fair", measure, () => screen.FairCard.Open(), () => screen.FairCard.Close(), "FairCard", "Hud/Fair");

            Show("trunk open", measure, () => screen.OpenTrunk(), () => screen.TrunkCard.Close(), "TrunkCard");
            roundTwoClock = day * 86400L + 14 * 3600L;
            screen.Refresh();
            Show("trunk closed", measure, () => screen.OpenTrunk(), () => screen.TrunkCard.Close(), "TrunkCard");

            Show("tank", measure, () => screen.OpenTank(), () => screen.TankSheet.Close(), "Aquarium");
            Show("tank full set", measure, () => screen.TankCard.Open(s), () => screen.TankCard.Close(), "AquariumSetCard");

            s.Newcomer.Closed = true;
            s.WelcomeBack.StartDay = day;
            s.WelcomeBack.AwayDays = 15;
            s.WelcomeBack.CardSeen = true;
            screen.Refresh();
            Show("welcome back", measure, () => screen.WelcomeCard.Open(), () => screen.WelcomeCard.Close(), "WelcomeBackCard", "Hud/WelcomeBack");
        }

        static string Spill(Text text)
        {
            if (string.IsNullOrEmpty(text.text)) return null;
            Rect r = text.rectTransform.rect;
            if (r.width <= 0f || r.height <= 0f) return null;
            bool wide = text.horizontalOverflow == HorizontalWrapMode.Overflow;
            float w = text.preferredWidth, h = text.preferredHeight;
            if (text.resizeTextForBestFit)
            {
                var gen = new TextGenerator();
                TextGenerationSettings settings = text.GetGenerationSettings(r.size);
                gen.Populate(text.text, settings);
                settings.fontSize = gen.fontSizeUsedForBestFit;
                settings.resizeTextForBestFit = false;
                settings.verticalOverflow = VerticalWrapMode.Overflow;
                w = gen.GetPreferredWidth(text.text, settings) / text.pixelsPerUnit;
                h = gen.GetPreferredHeight(text.text, settings) / text.pixelsPerUnit;
            }
            if (wide) return w > r.width + 1f ? Path(text.transform) + " wide " + Mathf.RoundToInt(w) + ">" + Mathf.RoundToInt(r.width) + " \"" + text.text + "\"" : null;
            return h > r.height + 1f ? Path(text.transform) + " tall " + Mathf.RoundToInt(h) + ">" + Mathf.RoundToInt(r.height) + " \"" + text.text + "\"" : null;
        }

        [TestCase("vi", false)]
        [TestCase("en", false)]
        [TestCase("vi", true)]
        [TestCase("en", true)]
        public void No_Label_On_A_Round_Two_Card_Spills_Out_Of_Its_Box(string code, bool large)
        {
            L10n.SetLocale(code);
            UiKit.SetLargeText(large);
            var spills = new List<string>();
            EachRoundTwoCard((where, roots) =>
            {
                foreach (Transform r in roots)
                    foreach (Text text in r.GetComponentsInChildren<Text>(false))
                    {
                        string spill = Spill(text);
                        if (spill != null) spills.Add(where + ": " + spill);
                    }
            });
            Assert.IsEmpty(spills, spills.Count + " labels spill in " + code + (large ? " (large text)" : "") + ":\n" + string.Join("\n", spills));
        }

        [TestCase("vi", false)]
        [TestCase("en", false)]
        [TestCase("vi", true)]
        [TestCase("en", true)]
        public void Every_Button_On_A_Round_Two_Card_Is_At_Least_The_Minimum_Touch_Size(string code, bool large)
        {
            L10n.SetLocale(code);
            UiKit.SetLargeText(large);
            var small = new HashSet<string>();
            EachRoundTwoCard((where, roots) =>
            {
                foreach (Transform r in roots)
                    foreach (Button button in r.GetComponentsInChildren<Button>(false)) Measure(button, small);
            });
            Assert.IsEmpty(small, small.Count + " buttons under " + UiKit.MinTouch + ":\n" + string.Join("\n", small));
        }

        static float GroupAlpha(Transform t)
        {
            float alpha = 1f;
            for (; t != null; t = t.parent)
                foreach (CanvasGroup group in t.GetComponents<CanvasGroup>()) alpha *= group.alpha;
            return alpha;
        }

        static bool Covers(RectTransform rt, Vector3 point)
        {
            Vector3 local = rt.InverseTransformPoint(point);
            return rt.rect.Contains(new Vector2(local.x, local.y));
        }

        bool IsPlate(Image image)
        {
            if (!image.enabled) return false;
            Mask mask = image.GetComponent<Mask>();
            if (mask != null && !mask.showMaskGraphic) return false;
            return image.sprite == null || image.sprite == runner.Catalog.Panel || image.sprite == runner.Catalog.Round;
        }

        bool Backdrop(Graphic[] drawn, int index, Vector3 point, out Color back)
        {
            var layers = new List<Color>();
            back = Color.clear;
            for (int j = index - 1; j >= 0; j--)
            {
                if (!(drawn[j] is Image image) || !IsPlate(image) || !Covers(image.rectTransform, point)) continue;
                Color c = image.color;
                c.a *= GroupAlpha(image.transform);
                if (c.a < 0.05f) continue;
                layers.Add(c);
                if (c.a >= 0.99f) break;
            }
            if (layers.Count == 0 || layers[layers.Count - 1].a < 0.99f) return false;
            back = layers[layers.Count - 1];
            for (int k = layers.Count - 2; k >= 0; k--) back = Color.Lerp(back, layers[k], layers[k].a);
            back.a = 1f;
            return true;
        }

        static readonly string[] ColoursAwaitingTheDesignSession =
        {
        };

        static string KindOf(string path)
        {
            string kind = System.Text.RegularExpressions.Regex.Replace(path, "^Canvas/", "");
            kind = System.Text.RegularExpressions.Regex.Replace(kind, "Content/[a-z-]+/Dish", "Content/*/Dish");
            kind = System.Text.RegularExpressions.Regex.Replace(kind, "Row [a-z]+/", "Row */");
            return System.Text.RegularExpressions.Regex.Replace(kind, "[0-9]+", "#");
        }

        static bool Disabled(Transform t)
        {
            Selectable control = t.GetComponentInParent<Selectable>();
            return control != null && !control.interactable;
        }

        [TestCase("vi", false)]
        [TestCase("en", false)]
        [TestCase("vi", true)]
        [TestCase("en", true)]
        public void Every_Label_On_A_Round_Two_Card_Reads_At_AA_Contrast(string code, bool large)
        {
            L10n.SetLocale(code);
            UiKit.SetLargeText(large);
            var known = new HashSet<string>(ColoursAwaitingTheDesignSession);
            var faint = new HashSet<string>();
            var stillFaint = new HashSet<string>();
            int read = 0;
            EachRoundTwoCard((where, roots) =>
            {
                foreach (Transform r in roots)
                {
                    Graphic[] drawn = r.GetComponentsInChildren<Graphic>(false);
                    for (int i = 0; i < drawn.Length; i++)
                    {
                        if (!(drawn[i] is Text text) || string.IsNullOrEmpty(text.text.Trim()) || GroupAlpha(text.transform) < 0.99f || Disabled(text.transform)) continue;
                        RectTransform rt = text.rectTransform;
                        if (!Backdrop(drawn, i, rt.TransformPoint(rt.rect.center), out Color back)) continue;
                        read++;
                        Color ink = Color.Lerp(back, text.color, text.color.a);
                        bool bold = text.fontStyle == FontStyle.Bold || text.fontStyle == FontStyle.BoldAndItalic;
                        double need = bold ? 3.0 : 4.5, have = Contrast(ink, back);
                        if (have + 0.005 >= need) continue;
                        string kind = KindOf(Path(text.transform));
                        if (known.Contains(kind)) stillFaint.Add(kind);
                        else faint.Add(where + ": " + Path(text.transform) + " " + have.ToString("0.00") + " < " + need.ToString("0.0") + " \"" + text.text + "\"");
                    }
                }
            });
            Assert.Greater(read, 200, "the sweep finds the cards' labels on their plates");
            Assert.IsEmpty(faint, faint.Count + " labels under AA in " + code + (large ? " (large text)" : "") + ":\n" + string.Join("\n", faint));
            known.ExceptWith(stillFaint);
            Assert.IsEmpty(known, "these now read at AA; take them off the design session's list:\n" + string.Join("\n", known));
        }

        [Test]
        public void Under_Reduced_Motion_Every_Round_Two_Card_Holds_Still_After_Its_First_Frame()
        {
            var moved = new List<string>();
            EachRoundTwoCard((where, roots) =>
            {
                var poses = new List<(RectTransform rt, Vector2 at, float angle, Vector3 scale)>();
                foreach (Transform r in roots)
                    foreach (RectTransform rt in r.GetComponentsInChildren<RectTransform>(false))
                        poses.Add((rt, rt.anchoredPosition, rt.localEulerAngles.z, rt.localScale));
                for (int f = 0; f < 90; f++) screen.Advance(1f / 30f);
                foreach ((RectTransform rt, Vector2 at, float angle, Vector3 scale) in poses)
                {
                    if (rt == null) continue;
                    if ((rt.anchoredPosition - at).magnitude > 0.01f || Mathf.Abs(Mathf.DeltaAngle(angle, rt.localEulerAngles.z)) > 0.01f || (rt.localScale - scale).magnitude > 0.0001f)
                        moved.Add(where + ": " + Path(rt) + " at " + at + " " + angle + "° x" + scale + " then " + rt.anchoredPosition + " " + rt.localEulerAngles.z + "° x" + rt.localScale);
                }
            });
            Assert.IsEmpty(moved, moved.Count + " parts move after the first frame under reduced motion:\n" + string.Join("\n", moved));
        }
    }
}
