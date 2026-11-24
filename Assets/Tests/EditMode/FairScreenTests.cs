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
    public class FairScreenTests
    {
        GameObject root;
        GameRunner runner;
        GardenScreen screen;
        long today;

        static FairRun May => Fairs.RunOn(GiftCodes.Day(2027, 5, Fairs.StartDay)).Value;

        static long DayOf(bool weekend)
        {
            for (long d = May.Start; d <= May.End; d++)
                if (Fairs.Weekend(d) == weekend) return d;
            return May.Start;
        }

        [SetUp]
        public void SetUp()
        {
            L10n.SetLocale("vi");
            Motion.SetReduced(true);
        }

        [TearDown]
        public void TearDown()
        {
            if (root != null) Object.DestroyImmediate(root);
            Motion.SetReduced(false);
            L10n.SetLocale("vi");
        }

        GameState Open(long day)
        {
            today = day;
            var catalog = AssetDatabase.LoadAssetAtPath<GameCatalog>("Assets/VuonTra/Content/GameCatalog.asset");
            Assert.IsNotNull(catalog, "Run VuonTra > Seed Content first.");
            root = new GameObject("Game");
            var cam = new GameObject("Cam", typeof(Camera));
            cam.transform.SetParent(root.transform);
            runner = root.AddComponent<GameRunner>();
            runner.Catalog = catalog;
            runner.Analytics = new MemoryAnalytics();
            runner.DayClock = () => today;
            runner.OfferCardsOn = true;
            GameState s = GameState.NewGame(catalog.BuildDefs());
            Rabbits.GiftFirst(s, "Bông");
            s.TutorialStep = Tutorial.Steps;
            s.HintSeen = true;
            s.GardenXp = Garden.XpForLevel(6);
            s.FirstDaySeen = today - 20;
            s.MaxDaySeen = today;
            s.WelcomeBack.SeenDay = today;
            s.MailDay = today;
            s.GiftDay = today;
            s.Newcomer.Closed = true;
            runner.Init(s);
            screen = root.AddComponent<GardenScreen>();
            screen.Runner = runner;
            screen.Cam = cam.GetComponent<Camera>();
            screen.Raining = () => false;
            screen.Build();
            screen.Shell.StartGame();
            return s;
        }

        void Press(string path)
        {
            Transform t = root.transform.Find(path);
            Assert.IsNotNull(t, "Missing UI object: " + path);
            t.GetComponent<Button>().onClick.Invoke();
        }

        [Test]
        public void On_A_Fair_Day_The_Card_Opens_Once_And_The_Chip_Sits_Beside_The_Purse()
        {
            GameState s = Open(DayOf(false));
            Assert.AreEqual(May.Start, s.Fair.Start, "opening the garden joins the fair");
            screen.Refresh();
            Assert.IsTrue(screen.FairCard.IsOpen, "the fair's card greets the player");
            Assert.AreEqual(L10n.T("fair.hoi-tra-sen"), screen.FairCard.Title);
            Assert.AreEqual(L10n.T("fair.weekday", L10n.T("fair.coin.hoi-tra-sen"), 2), screen.FairCard.Weekend);
            Assert.IsFalse(screen.OfferCardFree, "one card at a time");
            Assert.IsTrue(screen.ReviewBusy);
            screen.FairCard.Close();
            screen.Refresh();
            Assert.IsFalse(screen.FairCard.IsOpen, "once seen it does not come back by itself");
            Assert.IsTrue(screen.FairCard.ChipShown);
            Assert.AreEqual("0", screen.FairCard.ChipText);
            Assert.IsFalse(screen.FairCard.ChipDouble);
            Assert.IsNotNull(root.transform.Find("Canvas/Hud/Fair"));

            s.Fair.Coins = 45;
            screen.Refresh();
            Assert.AreEqual("45", screen.FairCard.ChipText);
            Press("Canvas/Hud/Fair");
            Assert.IsTrue(screen.FairCard.IsOpen, "a tap on the chip opens the stall");
            Assert.AreEqual(L10n.T("fair.have", 45, L10n.T("fair.coin.hoi-tra-sen")), screen.FairCard.Have);
        }

        [Test]
        public void On_A_Weekend_The_Chip_And_The_Banner_Say_Double()
        {
            Open(DayOf(true));
            screen.Refresh();
            Assert.IsTrue(screen.FairCard.ChipDouble);
            Assert.AreEqual(L10n.T("fair.weekend", L10n.T("fair.coin.hoi-tra-sen"), 2), screen.FairCard.Weekend);
        }

        [Test]
        public void Two_Taps_Buy_At_The_Stall_And_The_Fair_Rabbit_Steps_Out_Of_The_Tinted_Gate()
        {
            GameState s = Open(DayOf(false));
            screen.Refresh();
            s.Fair.Coins = 480;
            int jars = s.MoonJars + s.Clovers;
            Press("Canvas/FairCard/Card/Stall 0");
            Assert.AreEqual(480, s.Fair.Coins, "the first tap only asks");
            Assert.AreEqual(L10n.T("shop.hats.confirm"), screen.FairCard.PriceOf(0));
            Press("Canvas/FairCard/Card/Stall 0");
            Assert.AreEqual(440, s.Fair.Coins);
            Assert.AreEqual(jars + 1, s.MoonJars + s.Clovers);
            Assert.AreEqual(L10n.T("fair.left", 4, 5), screen.FairCard.LeftOf(0));

            int rabbits = s.Rabbits.Count;
            Press("Canvas/FairCard/Card/Stall 5");
            Press("Canvas/FairCard/Card/Stall 5");
            Assert.AreEqual(rabbits + 1, s.Rabbits.Count);
            Rabbit fair = s.Rabbits[s.Rabbits.Count - 1];
            Assert.AreEqual(3, fair.Tier);
            Assert.AreEqual(Fairs.Dye(May.Index), fair.Dye);
            Assert.AreNotEqual(Color.white, RabbitGridView.Fur(fair), "the fair rabbit wears the fair's tint");
            Assert.IsFalse(screen.FairCard.IsOpen);
            Assert.IsTrue(screen.Arrival.IsOpen);
            Image gate = root.transform.Find("Canvas/SummonMoment/Gate/GateDecor").GetComponent<Image>();
            Assert.IsTrue(gate.gameObject.activeSelf, "the festival decor hangs on the gate");
            Assert.AreNotEqual(Color.white, gate.color, "tinted in the fair's colour");
        }

        [Test]
        public void The_Shop_Has_The_Fair_Button_Only_During_A_Fair()
        {
            Open(DayOf(false));
            screen.Refresh();
            screen.FairCard.Close();
            screen.OpenShop();
            Assert.IsTrue(screen.Shop.FairShown);
            Press("Canvas/Shop/Card/Fair");
            Assert.IsTrue(screen.FairCard.IsOpen);
            Assert.IsFalse(screen.Shop.IsOpen);
            Object.DestroyImmediate(root);

            Open(GiftCodes.Day(2027, 5, 25));
            screen.Refresh();
            Assert.IsFalse(screen.FairCard.ChipShown);
            Assert.IsFalse(screen.FairCard.IsOpen);
            screen.OpenShop();
            Assert.IsFalse(screen.Shop.FairShown);
        }

        [Test]
        public void The_Fair_Letter_Names_The_Fair_And_Its_Carrots()
        {
            var letter = new Letter { Id = "fair-" + May.Start, Kind = LetterKind.FairEnd, Fair = May.Start, FairCoins = 12, Coins = new BigNum(3600) };
            Assert.AreEqual(L10n.T("mail.fair", L10n.T("fair.hoi-tra-sen"), L10n.T("fair.coin.hoi-tra-sen")), MailView.TitleOf(letter));
            Assert.AreEqual(L10n.T("mail.coins", new BigNum(3600).ToShortString()), MailView.GiftOf(letter));
        }

        [Test]
        public void Every_Fair_Has_Its_Name_And_Coin_In_Both_Languages()
        {
            foreach (string locale in new[] { "vi", "en" })
            {
                L10n.SetLocale(locale);
                foreach (FairDef def in Fairs.All)
                {
                    Assert.AreNotEqual(def.NameKey, L10n.T(def.NameKey), locale + " " + def.NameKey);
                    Assert.AreNotEqual(def.CoinKey, L10n.T(def.CoinKey), locale + " " + def.CoinKey);
                }
                foreach (FairWare ware in System.Enum.GetValues(typeof(FairWare)))
                    if (ware != FairWare.Rabbit) Assert.AreNotEqual("fair.ware." + ware, FairView.WareName(ware));
            }
        }
    }
}
