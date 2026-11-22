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
    /// <summary>The moon pond on the real screen (#58b): its card, a cast from bait to basket, feeding a rabbit, ten hearts.</summary>
    public class PondScreenTests
    {
        GameObject root;
        GameRunner runner;
        GardenScreen screen;
        long now = 1_800_000_000L;

        [SetUp]
        public void SetUp()
        {
            L10n.SetLocale("vi");
            UiKit.SetLargeText(false);
            var catalog = AssetDatabase.LoadAssetAtPath<GameCatalog>("Assets/VuonTra/Content/GameCatalog.asset");
            Assert.IsNotNull(catalog, "Run VuonTra > Seed Content first.");
            root = new GameObject("Game");
            var cam = new GameObject("Cam", typeof(Camera));
            cam.transform.SetParent(root.transform);
            runner = root.AddComponent<GameRunner>();
            runner.Catalog = catalog;
            GameState s = GameState.NewGame(catalog.BuildDefs());
            s.Levels[1] = 12; s.Levels[2] = 9; s.Levels[3] = 4;
            s.GardenXp = Garden.XpForLevel(5); // the firefly jar stands by the pond too
            s.HintSeen = true;
            s.TutorialStep = Tutorial.Steps;
            runner.Init(s);
            runner.FishingClock = () => now;
            screen = root.AddComponent<GardenScreen>();
            screen.Runner = runner;
            screen.Cam = cam.GetComponent<Camera>();
            screen.Raining = () => false; // the real clock may fall in a shower (#62): these tests are about a dry garden
            screen.Build();
            Motion.SetReduced(true);
            screen.Shell.StartGame();
            screen.Refresh();
            Step(1.2f); // the opening glide
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(root);
            Motion.SetReduced(false);
            UiKit.SetLargeText(false);
            PlayerPrefs.DeleteKey("text.large");
            L10n.SetLocale("vi");
        }

        GameState S => runner.State;

        void Step(float seconds)
        {
            for (float t = 0f; t < seconds; t += 1f / 30f) screen.Advance(1f / 30f);
        }

        Button Find(string path)
        {
            Transform t = root.transform.Find(path);
            Assert.IsNotNull(t, "Missing UI object: " + path);
            return t.GetComponent<Button>();
        }

        string TextAt(string path)
        {
            Transform t = root.transform.Find(path);
            Assert.IsNotNull(t, "Missing UI object: " + path);
            return t.GetComponent<Text>().text;
        }

        const string Card = "Canvas/Pond/Card";

        void OpenPond()
        {
            Find("Canvas/Garden/Content/PondTap").onClick.Invoke();
            screen.Refresh();
            Step(0.3f);
            Assert.IsTrue(screen.FishingCard.IsOpen, "the pond's card is up");
        }

        /// <summary>Steps the screen until the float goes under; fails if it never does.</summary>
        FishingCast StepUntilSinking()
        {
            for (int k = 0; k < 400 && runner.Cast != null && runner.Cast.Phase != CastPhase.Sinking; k++) screen.Advance(1f / 30f);
            Assert.IsNotNull(runner.Cast, "the cast is still out");
            Assert.AreEqual(CastPhase.Sinking, runner.Cast.Phase);
            return runner.Cast;
        }

        [Test]
        public void Touching_The_Pond_Opens_Its_Card_With_The_Baits_And_An_Empty_Basket()
        {
            Assert.AreEqual(L10n.T("pond.plate", 5, 5), TextAt("Canvas/Garden/Content/PondTap/Plate/Text"), "the plate on the bank counts the baits");
            OpenPond();
            Assert.IsTrue(root.transform.Find("Canvas/Pond").gameObject.activeSelf);
            Assert.AreEqual(L10n.T("pond.title"), TextAt(Card + "/Title"));
            Assert.AreEqual("Mồi: 5/5", TextAt(Card + "/Baits"));
            Assert.AreEqual("", TextAt(Card + "/Next"), "all five on hand: nothing to wait for");
            Assert.IsTrue(Find(Card + "/Cast").interactable);
            Assert.AreEqual(L10n.T("pond.cast"), TextAt(Card + "/Cast/Face/Text"));
            Assert.AreEqual(L10n.T("pond.hint"), screen.FishingCard.Status);
            for (int i = 0; i < Fishing.Kinds; i++) Assert.AreEqual("×0", TextAt(Card + "/Basket/Fish " + i + "/Count"));
            Assert.IsFalse(root.transform.Find(Card + "/Water/Float").gameObject.activeSelf, "no float before a cast");

            Find(Card + "/Close").onClick.Invoke();
            Step(0.3f);
            Assert.IsFalse(screen.FishingCard.IsOpen);
        }

        [Test]
        public void A_Strike_While_The_Float_Sinks_Lands_The_Fish_Which_Flies_Into_The_Basket()
        {
            Motion.SetReduced(false); // the fish's flight takes its time here
            OpenPond();
            Find(Card + "/Cast").onClick.Invoke();
            FishingCast cast = runner.Cast;
            Assert.IsNotNull(cast, "a bait went on the hook");
            Assert.AreEqual("Mồi: 4/5", TextAt(Card + "/Baits"));
            Assert.AreEqual(L10n.T("fish.next", "24:00"), TextAt(Card + "/Next"), "the next bait's countdown");
            Assert.IsFalse(Find(Card + "/Cast").interactable, "one line in the water at a time");
            Assert.AreEqual(L10n.T("pond.casting"), TextAt(Card + "/Cast/Face/Text"));
            Assert.IsTrue(root.transform.Find(Card + "/Water/Float").gameObject.activeSelf, "the float is on the water");
            Assert.AreEqual(L10n.T("pond.wait"), screen.FishingCard.Status);

            StepUntilSinking();
            Assert.AreEqual(L10n.T("pond.sink"), screen.FishingCard.Status);
            int fish = cast.Fish;
            Find(Card + "/Water").onClick.Invoke();
            Assert.AreEqual(1, S.Fish[fish], "in the basket");
            Assert.AreEqual(1, S.FishCaught);
            Assert.IsNull(runner.Cast, "the cast is over");
            StringAssert.Contains(L10n.T("fish." + fish), screen.FishingCard.Status, "the line names the fish");
            StringAssert.Contains(L10n.T("fish.points", Fishing.Points[fish]), screen.FishingCard.Status);
            Assert.IsTrue(root.transform.Find(Card + "/Catch").gameObject.activeSelf, "the fish shows over the water");
            Assert.AreEqual(1, screen.FishingCard.FishFlying);
            Assert.AreEqual("×0", TextAt(Card + "/Basket/Fish " + fish + "/Count"), "its count waits for it to land");

            Step(FishingView.ShowSeconds + FishingView.FlySeconds + 0.3f);
            Assert.AreEqual(0, screen.FishingCard.FishFlying);
            Assert.IsFalse(root.transform.Find(Card + "/Catch").gameObject.activeSelf);
            Assert.AreEqual("×1", TextAt(Card + "/Basket/Fish " + fish + "/Count"), "the basket shows it");
            Assert.IsTrue(Find(Card + "/Cast").interactable, "the next cast");
        }

        [Test]
        public void A_Tap_Too_Early_Brings_The_Float_Back_Up_And_A_Float_Left_To_Sink_Loses_The_Bait()
        {
            OpenPond();
            Find(Card + "/Cast").onClick.Invoke();
            Step(0.5f);
            Find(Card + "/Water").onClick.Invoke();
            Assert.AreEqual(L10n.T("pond.early"), screen.FishingCard.Status);
            Assert.IsNotNull(runner.Cast, "the bait is still on the hook");
            Assert.AreEqual(1, runner.Cast.EarlyTaps);
            Assert.AreEqual(CastPhase.Bobbing, runner.Cast.Phase);
            Assert.AreEqual(0, Fishing.InBasket(S));

            Step((float)(Fishing.BobMax + Fishing.SinkSeconds) + 0.5f);
            Assert.IsNull(runner.Cast, "nobody struck: the cast is over");
            Assert.AreEqual(L10n.T("pond.lost"), screen.FishingCard.Status);
            Assert.AreEqual(0, Fishing.InBasket(S), "and nothing caught");
            Assert.AreEqual("Mồi: 4/5", TextAt(Card + "/Baits"), "the bait is gone");
            Assert.IsFalse(root.transform.Find(Card + "/Water/Float").gameObject.activeSelf);
            Find(Card + "/Water").onClick.Invoke();
            Assert.AreEqual(L10n.T("pond.lost"), screen.FishingCard.Status, "a touch on still water does nothing");
        }

        [Test]
        public void With_No_Bait_The_Cast_Button_Lies_Flat_And_Counts_Down_To_The_Next()
        {
            S.Baits = 0;
            S.BaitAt = now;
            screen.Refresh();
            Assert.AreEqual(L10n.T("pond.plate", 0, 5), TextAt("Canvas/Garden/Content/PondTap/Plate/Text"));
            OpenPond();
            Assert.AreEqual("Mồi: 0/5", TextAt(Card + "/Baits"));
            Assert.AreEqual(L10n.T("fish.next", "24:00"), TextAt(Card + "/Next"));
            Assert.IsFalse(Find(Card + "/Cast").interactable);
            Assert.AreEqual(L10n.T("pond.nobait"), TextAt(Card + "/Cast/Face/Text"));
            Assert.AreEqual(L10n.T("fish.nobait"), screen.FishingCard.Status);
            screen.FishingCard.Cast();
            Assert.IsNull(runner.Cast, "nothing to cast with");

            now += 60;
            Step(0.1f);
            Assert.AreEqual(L10n.T("fish.next", "23:00"), TextAt(Card + "/Next"), "the countdown runs");
            now += Fishing.BaitSeconds - 60;
            Step(0.1f);
            Assert.AreEqual("Mồi: 1/5", TextAt(Card + "/Baits"));
            Assert.IsTrue(Find(Card + "/Cast").interactable, "a bait came back");
        }

        [Test]
        public void Reduced_Motion_Keeps_The_Float_Still_And_Shows_The_Sink_By_Colour_And_Size()
        {
            OpenPond();
            Find(Card + "/Cast").onClick.Invoke();
            var art = (RectTransform)root.transform.Find(Card + "/Water/Float/Art");
            Color bobbing = screen.FishingCard.FloatColor;
            Vector2 at = art.anchoredPosition;
            for (int k = 0; k < 20; k++)
            {
                screen.Advance(1f / 30f);
                if (runner.Cast.Phase != CastPhase.Bobbing) break;
                Assert.AreEqual(at, art.anchoredPosition, "no bobbing");
                Assert.AreEqual(Vector3.one, screen.FishingCard.FloatScale);
            }
            StepUntilSinking();
            Assert.AreNotEqual(bobbing, screen.FishingCard.FloatColor, "the sink shows as a colour");
            Assert.AreEqual(Palette.Lantern, screen.FishingCard.FloatColor);
            Assert.Less(screen.FishingCard.FloatScale.x, 0.9f, "and as a smaller float");
            Assert.AreEqual(at, art.anchoredPosition, "without moving");

            // The same timing rules with motion on: the float bobs.
            Find(Card + "/Water").onClick.Invoke();
            Motion.SetReduced(false);
            Step(0.2f);
            Find(Card + "/Cast").onClick.Invoke();
            screen.Advance(1f / 30f);
            Vector2 first = art.anchoredPosition;
            Step(0.2f);
            Assert.AreNotEqual(first, art.anchoredPosition, "with motion on it bobs");
        }

        [Test]
        public void The_Pond_Does_Not_Open_While_The_Garden_Is_Being_Arranged()
        {
            screen.EnterLayoutMode();
            Find("Canvas/Garden/Content/PondTap").onClick.Invoke();
            screen.OpenFishing();
            Step(0.3f);
            Assert.IsFalse(screen.FishingCard.IsOpen);
            screen.ExitLayoutMode();
            Step(0.3f);
            screen.OpenFishing();
            Assert.IsTrue(screen.FishingCard.IsOpen, "and opens again once done");
        }

        [Test]
        public void The_Pond_Lies_Under_The_Frog_Rabbit_And_The_Firefly_Jar_So_They_Keep_Their_Touch()
        {
            Transform street = root.transform.Find("Canvas/Garden/Content");
            int pond = street.Find("PondTap").GetSiblingIndex();
            Assert.Less(pond, street.Find("Frog").GetSiblingIndex(), "the Frog Rabbit waits at the water over it");
            Assert.Less(pond, street.Find("Jar").GetSiblingIndex(), "the jar on its stump too");
        }

        Rabbit FeedingRabbit()
        {
            Rabbit r = S.Rabbits.Count > 0 ? S.Rabbits[0] : Rabbits.GiftFirst(S, "Bông");
            screen.Refresh();
            screen.Detail.Open(runner.Defs, S, r.Id);
            screen.Refresh();
            Step(0.3f);
            return r;
        }

        const string Body = "Canvas/RabbitSheet/Card/Body";

        [Test]
        public void Feeding_From_The_Rabbit_Sheet_Raises_Its_Points_And_Hearts()
        {
            S.Fish[0] = 1;
            S.Fish[2] = 2; // two pink koi, five points each: the best in the basket
            Rabbit r = FeedingRabbit();
            r.Affection = 8;
            screen.Refresh();
            Button feed = Find(Body + "/Feed");
            Assert.IsTrue(feed.interactable);
            Assert.AreEqual(L10n.T("feed.button"), TextAt(Body + "/Feed/Face/Top"));
            Assert.AreEqual(L10n.T("feed.with", L10n.T("fish.2"), 2), TextAt(Body + "/Feed/Face/Sub"), "the fish it gives and how many");
            Assert.AreEqual("+5 điểm", TextAt(Body + "/Feed/Face/Points"), "and what it is worth");
            Assert.GreaterOrEqual(((RectTransform)feed.transform).rect.height, UiKit.MinTouch);
            Assert.AreEqual(Rabbits.Hearts(8), TextAt(Body + "/Bond/Text"));

            feed.onClick.Invoke();
            Assert.AreEqual(13, r.Affection, "eight and the koi's five");
            Assert.AreEqual(1, S.Fish[2]);
            Assert.AreEqual(1, S.FishFed);
            Assert.AreEqual(Rabbits.Hearts(13), TextAt(Body + "/Bond/Text"), "the first heart shows at once");
            Assert.AreEqual(1, Rabbits.HeartsOf(r));
            var fill = (RectTransform)root.transform.Find(Body + "/Bond/Bar/Fill");
            Assert.AreEqual((13f - 10f) / (30f - 10f), fill.anchorMax.x, 0.001f, "the bar runs to the next heart");
            Assert.Greater(screen.Feel.Flying, 0, "the fish flies to its hearts");
            Assert.AreEqual(L10n.T("feed.with", L10n.T("fish.2"), 1), TextAt(Body + "/Feed/Face/Sub"));

            Find(Body + "/Feed").onClick.Invoke();
            Find(Body + "/Feed").onClick.Invoke();
            Assert.AreEqual(19, r.Affection, "the last koi, then the silverfish");
            Assert.AreEqual(0, Fishing.InBasket(S));
            Assert.IsFalse(Find(Body + "/Feed").interactable, "an empty basket feeds nobody");
            Assert.AreEqual(L10n.T("feed.empty"), TextAt(Body + "/Feed/Face/Sub"));
            Assert.AreEqual("", TextAt(Body + "/Feed/Face/Points"));
            Step(1f);
        }

        [Test]
        public void Ten_Hearts_Brings_The_Banner_Three_Golden_Carrots_And_The_Badge_On_The_Tile()
        {
            Rabbit r = FeedingRabbit();
            Rabbit other = Rabbits.Arrive(S, 0, "Mây", 0.3);
            r.Affection = Rabbits.AffectionMax - 10;
            S.Fish[5] = 1; // a starry night fish: thirty points
            int golden = S.GoldenCarrots;
            screen.Refresh();
            Assert.IsFalse(Rabbits.FullHearts(r));

            Find(Body + "/Feed").onClick.Invoke();
            Assert.IsTrue(Rabbits.FullHearts(r));
            Assert.AreEqual(golden + Rabbits.TenHeartGolden, S.GoldenCarrots);
            Assert.AreEqual(1, screen.TenHeartMoments);
            Assert.IsTrue(screen.Feel.BannerVisible, "the banner");
            Assert.IsTrue(root.transform.Find("Canvas/Hud/Golden").gameObject.activeSelf, "the carrots' chip is up for them to land on");
            Assert.AreEqual(root.transform.Find("Canvas").childCount - 1, root.transform.Find("Canvas/Feel").GetSiblingIndex(), "shown over the rabbit's sheet");
            Assert.IsFalse(Find(Body + "/Feed").interactable);
            Assert.AreEqual(L10n.T("feed.full"), TextAt(Body + "/Feed/Face/Sub"));
            Step(2.5f);

            screen.Detail.Close();
            screen.OpenSheet(3);
            screen.Refresh();
            Step(0.3f);
            Transform badge = root.transform.Find("Canvas/Sheet/Panel/Rabbits/Content/Row 0/Rabbit " + r.Id + "/Tile/TenHearts");
            Assert.IsNotNull(badge, "the heart badge on its tile");
            Assert.AreEqual("♥10", badge.Find("Text").GetComponent<Text>().text);
            Assert.IsNull(root.transform.Find("Canvas/Sheet/Panel/Rabbits/Content/Row 0/Rabbit " + other.Id + "/Tile/TenHearts"), "not on a rabbit short of ten");
        }

        [Test]
        public void Stroking_A_Rabbit_To_Ten_Hearts_Plays_The_Same_Moment()
        {
            Rabbit r = Rabbits.GiftFirst(S, "Bông");
            Rabbits.Assign(S, r.Id, 0);
            r.Affection = Rabbits.AffectionMax - Rabbits.PetPoints;
            r.PetDay = -1;
            screen.Refresh();
            // The screen's own stroke: a finger dragged across the teapot's rabbit.
            const string rabbit = "Canvas/Garden/Content/Station am-tra/Rabbit";
            var trigger = root.transform.Find(rabbit).GetComponent<UnityEngine.EventSystems.EventTrigger>();
            var data = new UnityEngine.EventSystems.PointerEventData(null) { button = UnityEngine.EventSystems.PointerEventData.InputButton.Left };
            foreach (var type in new[] { UnityEngine.EventSystems.EventTriggerType.BeginDrag, UnityEngine.EventSystems.EventTriggerType.Drag })
                foreach (UnityEngine.EventSystems.EventTrigger.Entry e in trigger.triggers)
                    if (e.eventID == type) e.callback.Invoke(data);
            Assert.IsTrue(Rabbits.FullHearts(r));
            Assert.AreEqual(1, screen.TenHeartMoments);
            Assert.IsTrue(screen.Feel.BannerVisible);
        }
    }
}
