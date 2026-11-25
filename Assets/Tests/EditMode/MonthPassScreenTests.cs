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
    public class MonthPassScreenTests
    {
        GameObject root;
        GameRunner runner;
        GardenScreen screen;
        GameState s;
        FakeStorefront store;

        static readonly long Tuesday = GiftCodes.Day(2026, 11, 24);
        const string Pass = "Canvas/Sheet/Panel/Quests/Content/MonthPassRow/";

        [SetUp]
        public void SetUp()
        {
            L10n.SetLocale("vi");
            Motion.SetReduced(true);
            var catalog = AssetDatabase.LoadAssetAtPath<GameCatalog>("Assets/VuonTra/Content/GameCatalog.asset");
            root = new GameObject("Game");
            var cam = new GameObject("Cam", typeof(Camera));
            cam.transform.SetParent(root.transform);
            runner = root.AddComponent<GameRunner>();
            runner.Catalog = catalog;
            runner.DayClock = () => Tuesday;
            store = new FakeStorefront();
            runner.Store = store;
            s = GameState.NewGame(catalog.BuildDefs());
            s.HintSeen = true;
            s.TutorialStep = Tutorial.Steps;
            Rabbits.GiftFirst(s, "Bông");
            runner.Init(s);
            runner.State.IntrosSeen.UnionWith(Intros.All);
            runner.State.IntrosTried.UnionWith(Intros.All);
            screen = root.AddComponent<GardenScreen>();
            screen.Runner = runner;
            screen.Cam = cam.GetComponent<Camera>();
            screen.Raining = () => false;
            screen.Build();
            screen.Shell.StartGame();
            screen.OpenSheet(2);
            screen.Refresh();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(root);
            Motion.SetReduced(false);
            L10n.SetLocale("vi");
        }

        Transform At(string path)
        {
            Transform t = root.transform.Find(Pass + path);
            Assert.IsNotNull(t, "Missing UI object: " + Pass + path);
            return t;
        }

        void Tap(string path) => At(path).GetComponent<Button>().onClick.Invoke();

        void Points(int tiers)
        {
            s.PassPoints = tiers * MonthPass.PointsPerTier;
            screen.Refresh();
        }

        [Test]
        public void The_Month_Pass_Stands_Under_The_Two_Rows_With_A_Locked_Lower_Row()
        {
            Transform content = root.transform.Find("Canvas/Sheet/Panel/Quests/Content");
            Assert.AreEqual("QuestBoxRow", content.GetChild(0).name);
            Assert.AreEqual("QuestWeekRow", content.GetChild(1).name);
            Assert.AreEqual("Ready", content.GetChild(2).name, "finished quests keep their place above it");
            Assert.AreEqual("MonthPassRow", content.GetChild(3).name);
            Assert.Less(content.Find("MonthPassRow").GetSiblingIndex(), content.Find("Heading").GetSiblingIndex(), "and it stands above the quest lists");
            Assert.AreEqual("Vé tháng 11 · nấc 0/30", At("Title").GetComponent<Text>().text);
            Assert.IsTrue(At("Premium/Lock").gameObject.activeSelf, "the lower row is shaded until bought");
            Assert.IsTrue(At("Buy").gameObject.activeSelf);
            StringAssert.Contains("Mở Vé tháng", At("Buy/Face/Text").GetComponent<Text>().text);
            StringAssert.Contains("129.000 ₫", At("Buy/Face/Text").GetComponent<Text>().text);
            Assert.IsNotNull(At("Free/Tier 10/Bonus"));
            Assert.IsTrue(At("Free/Tier 10/Bonus").gameObject.activeSelf, "tier 10 shows its five golden carrots");
            Assert.IsFalse(At("Free/Tier 9/Bonus").gameObject.activeSelf);
        }

        [Test]
        public void A_Reached_Free_Tier_Wears_A_Badge_And_Pays_Once()
        {
            Points(2);
            Assert.AreEqual("Vé tháng 11 · nấc 2/30", At("Title").GetComponent<Text>().text);
            Assert.IsTrue(At("Free/Tier 2/Badge").gameObject.activeSelf);
            Assert.IsFalse(At("Free/Tier 3/Badge").gameObject.activeSelf);
            Assert.IsFalse(At("Premium/Tier 1/Badge").gameObject.activeSelf, "the locked row shows no badge");
            int clovers = s.Clovers;
            Tap("Free/Tier 3");
            Assert.IsFalse(screen.Prizes.IsOpen, "tier three is not reached");
            Tap("Free/Tier 2");
            Assert.IsTrue(screen.Prizes.IsOpen);
            Assert.AreEqual(clovers + 1, s.Clovers);
            Assert.IsTrue(screen.ReviewBusy, "one card at a time");
            screen.Prizes.Close();
            screen.Refresh();
            Assert.IsTrue(At("Free/Tier 2/Tick").gameObject.activeSelf);
            Tap("Free/Tier 2");
            Assert.IsFalse(screen.Prizes.IsOpen, "once");
            Assert.AreEqual(clovers + 1, s.Clovers);
        }

        [Test]
        public void Buying_The_Pass_Opens_The_Lower_Row_And_Pays_The_Tiers_Passed()
        {
            Points(12);
            int golden = s.GoldenCarrots;
            Tap("Buy");
            Assert.AreEqual(1, store.Bought.Count);
            Assert.AreEqual(MonthPass.ProductId, store.Bought[0]);
            Assert.IsTrue(MonthPass.Premium(s, Tuesday));
            Assert.AreEqual(golden + 30, s.GoldenCarrots, "tiers 1, 3, 5, 7, 9 and 11 at once");
            Assert.IsFalse(At("Premium/Lock").gameObject.activeSelf);
            Assert.IsFalse(At("Buy").gameObject.activeSelf);
            Assert.IsTrue(At("Bought").gameObject.activeSelf);
            Assert.AreEqual(MonthPass.LanternIndex(10), s.LanternColor, "November's lantern colour is hung");
            Tap("Buy");
            Assert.AreEqual(1, store.Bought.Count, "a bought month asks the store nothing more");
        }

        [Test]
        public void Tier_Thirty_Asks_Which_Gold_Rabbit_To_Bring()
        {
            Points(30);
            MonthPass.Buy(s, Tuesday);
            screen.Refresh();
            At("Next").GetComponent<Button>().onClick.Invoke();
            At("Next").GetComponent<Button>().onClick.Invoke();
            Assert.AreEqual(2, screen.MonthPassRows.Page);
            Assert.IsTrue(At("Premium/Tier 10/Badge").gameObject.activeSelf, "the tenth cell on the last page is tier 30");
            int rabbits = s.Rabbits.Count;
            Tap("Premium/Tier 10");
            Assert.IsTrue(screen.KindChooser.IsOpen);
            Assert.AreEqual(rabbits, s.Rabbits.Count, "nothing comes before the choice");
            Assert.IsTrue(screen.ReviewBusy, "the review card waits while the choice is open");
            Assert.IsFalse(screen.OfferCardFree, "and so does an offer card");
            screen.KindChooser.Pick(2);
            Assert.IsFalse(screen.KindChooser.IsOpen);
            Assert.AreEqual(rabbits + 1, s.Rabbits.Count);
            Rabbit gold = s.Rabbits[s.Rabbits.Count - 1];
            Assert.AreEqual(Photos.GoldTier, gold.Tier);
            Assert.AreEqual(MonthPass.KindChoices(MonthPass.MonthOf(Tuesday))[2], gold.Kind);
        }

        [Test]
        public void A_Month_Letter_Reads_Its_Month_And_Gifts()
        {
            var letter = new Letter { Id = "month-x-free", Kind = LetterKind.MonthPass, Month = MonthPass.MonthOf(Tuesday), Tiers = 0b111, Coins = new BigNum(240) };
            Assert.AreEqual("Quà Vé tháng 11 chưa nhận · hàng miễn phí", MailView.TitleOf(letter));
            StringAssert.Contains("+1 cỏ bốn lá", MailView.GiftOf(letter));
            StringAssert.Contains("+1 hũ trăng", MailView.GiftOf(letter));
            L10n.SetLocale("en");
            Assert.AreEqual("Unclaimed November pass gifts · free row", MailView.TitleOf(letter));
        }
    }
}
