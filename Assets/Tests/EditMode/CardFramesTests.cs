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
    public class CardFramesTests
    {
        const string CatalogPath = "Assets/VuonTra/Content/GameCatalog.asset";

        GameObject root;
        GameRunner runner;
        GardenScreen screen;
        GameCatalog catalog;

        [SetUp]
        public void SetUp()
        {
            L10n.SetLocale("vi");
            UiKit.SetLargeText(false);
            var seeded = AssetDatabase.LoadAssetAtPath<GameCatalog>(CatalogPath);
            Assert.IsNotNull(seeded, "Run VuonTra > Seed Content first.");
            catalog = Object.Instantiate(seeded);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(catalog);
            UiFrames.Use(null);
            Motion.SetReduced(false);
            UiKit.SetLargeText(false);
            PlayerPrefs.DeleteKey("text.large");
            L10n.SetLocale("vi");
        }

        void Build(bool worker = true)
        {
            root = new GameObject("Game");
            var cam = new GameObject("Cam", typeof(Camera));
            cam.transform.SetParent(root.transform);
            runner = root.AddComponent<GameRunner>();
            runner.Catalog = catalog;
            GameState s = GameState.NewGame(catalog.BuildDefs());
            s.Levels[0] = 12; s.Levels[1] = 4;
            s.Plots = 4;
            s.GardenXp = Garden.XpForLevel(5);
            s.HintSeen = true;
            s.TutorialStep = Tutorial.Steps;
            s.Newcomer.Closed = true;
            if (worker) Rabbits.Assign(s, Rabbits.ArriveKind(s, "tho-bong", "Bông").Id, 0);
            s.FreshKinds.Clear();
            runner.Init(s);
            runner.State.IntrosSeen.UnionWith(Intros.All);
            runner.State.IntrosTried.UnionWith(Intros.All);
            screen = root.AddComponent<GardenScreen>();
            screen.Runner = runner;
            screen.Cam = cam.GetComponent<Camera>();
            screen.Raining = () => false;
            screen.Build();
            Motion.SetReduced(true);
            screen.Shell.StartGame();
            screen.Refresh();
            Step(1.2f);
        }

        void Step(float seconds)
        {
            for (float t = 0f; t < seconds; t += 1f / 30f) screen.Advance(1f / 30f);
        }

        Transform At(string path)
        {
            Transform t = root.transform.Find(path);
            Assert.IsNotNull(t, "Missing UI object: " + path);
            return t;
        }

        static Sprite Painted()
        {
            var tex = new Texture2D(96, 96) { hideFlags = HideFlags.HideAndDontSave };
            return Sprite.Create(tex, new Rect(0, 0, 96, 96), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(32, 32, 32, 32));
        }

        static Rect World(Transform t)
        {
            var corners = new Vector3[4];
            ((RectTransform)t).GetWorldCorners(corners);
            return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
        }

        [Test]
        public void EveryFrameKindHasASlotInTheCatalog()
        {
            Sprite painted = Painted();
            catalog.CardFrame = painted;
            catalog.TabFrame = painted;
            Assert.AreSame(painted, catalog.FrameArt(FrameKind.Card));
            Assert.AreSame(painted, catalog.FrameArt(FrameKind.Tab));
            Assert.IsNull(catalog.FrameArt(FrameKind.Button));
            foreach (FrameKind kind in System.Enum.GetValues(typeof(FrameKind)))
                Assert.DoesNotThrow(() => catalog.FrameArt(kind), kind.ToString());
        }

        [Test]
        public void WithoutPaintedFramesTheCardKeepsItsFlatFaceAndLip()
        {
            Build();
            screen.FocusStation(0);
            Step(0.5f);
            Image lip = At("Canvas/Card/Lip").GetComponent<Image>();
            Image face = At("Canvas/Card/Face").GetComponent<Image>();
            Assert.AreSame(catalog.Panel, face.sprite);
            Assert.AreEqual(Palette.Paper, face.color);
            Assert.AreEqual(Palette.CardLip, lip.color);
            Assert.AreEqual(10f, ((RectTransform)face.transform).offsetMin.y, 0.01f, "the face sits above the lip");
        }

        [Test]
        public void APaintedCardFrameReplacesFaceAndLipWithoutTint()
        {
            Sprite painted = Painted();
            catalog.CardFrame = painted;
            Build();
            screen.FocusStation(0);
            Step(0.5f);
            Assert.IsNull(root.transform.Find("Canvas/Card/Lip"), "the painted frame carries its own lip");
            Image face = At("Canvas/Card/Face").GetComponent<Image>();
            Assert.AreSame(painted, face.sprite);
            Assert.AreEqual(Color.white, face.color);
            Assert.AreEqual(Image.Type.Sliced, face.type);
            Assert.IsTrue(face.raycastTarget, "a tap on the card still does not reach the garden");
        }

        [Test]
        public void PaintedHudAndTabFramesReachThePurseAndTheTabBar()
        {
            Sprite hud = Painted(), tab = Painted();
            catalog.HudFrame = hud;
            catalog.TabFrame = tab;
            Build();
            Assert.AreSame(hud, At("Canvas/Hud/Purse/Face").GetComponent<Image>().sprite);
            Assert.AreSame(tab, At("Canvas/TabBar/Plate").GetComponent<Image>().sprite);
            Assert.AreSame(catalog.Panel, At("Canvas/Card/Face").GetComponent<Image>().sprite, "a kind without art stays flat");
        }

        [Test]
        public void APaintedButtonFrameShowsUntintedAndGreysWhenOff()
        {
            catalog.ButtonFrame = Painted();
            UiFrames.Use(catalog);
            root = new GameObject("Game", typeof(RectTransform));
            ChunkyButton button = UiKit.Chunky(UiKit.Rect("Buy", root.transform), catalog.Panel, () => { });
            Assert.AreSame(catalog.ButtonFrame, button.Face.sprite);
            Assert.AreEqual(Color.white, button.Face.color);
            button.SetEnabled(false);
            Assert.AreEqual(Palette.Track, button.Face.color);
            button.SetEnabled(true);
            Assert.AreEqual(Color.white, button.Face.color);
            Assert.AreEqual(0f, button.Lip.color.a, "the painted button carries its own lip");
        }

        [Test]
        public void TheStationCardShowsTheWorkerThePropAndTheDish()
        {
            Build();
            screen.FocusStation(0);
            Step(0.5f);
            StationData data = catalog.Stations[0];
            StationSceneView scene = screen.CardScene;
            Assert.IsTrue(scene.Prop.enabled);
            Assert.AreSame(data.Prop, scene.Prop.sprite);
            Assert.AreEqual(data.Product != null, scene.Dish.enabled);
            if (data.Product != null) Assert.AreSame(data.Product, scene.Dish.sprite);
            Assert.IsTrue(scene.Worker.enabled, "the rabbit working here is in the picture");
            Assert.AreSame(catalog.RabbitArt(Rabbits.AtStation(runner.State, 0)), scene.Worker.sprite);
            Assert.Less(scene.Worker.rectTransform.anchoredPosition.x, scene.Prop.rectTransform.anchoredPosition.x);
        }

        [Test]
        public void AStationWithNobodyWorkingShowsNoRabbit()
        {
            Build(worker: false);
            screen.FocusStation(0);
            Step(0.5f);
            Assert.IsFalse(screen.CardScene.Worker.enabled);
            Assert.IsTrue(screen.CardScene.Prop.enabled);
        }

        [Test]
        public void AClosedStationShowsOnlyItsProp()
        {
            Build();
            screen.FocusStation(2);
            Step(0.5f);
            Assert.AreSame(catalog.Stations[2].Prop, screen.CardScene.Prop.sprite);
            Assert.IsFalse(screen.CardScene.Dish.enabled);
            Assert.IsFalse(screen.CardScene.Worker.enabled);
        }

        [TestCase(false, "vi")]
        [TestCase(true, "vi")]
        [TestCase(false, "en")]
        [TestCase(true, "en")]
        public void TheScenePictureStaysClearOfTheBuyModesAndInsideTheCard(bool large, string locale)
        {
            L10n.SetLocale(locale);
            UiKit.SetLargeText(large);
            Build();
            screen.FocusStation(0);
            Step(0.5f);
            Rect scene = World(At("Canvas/Card/Scene")), modes = World(At("Canvas/Card/Modes/Track")), card = World(At("Canvas/Card"));
            Rect dish = World(At("Canvas/Card/Dish"));
            Assert.Less(scene.xMax, modes.xMin, "left of the buy modes");
            Assert.Greater(scene.xMin, card.xMin);
            Assert.GreaterOrEqual(scene.yMin, dish.yMax - 0.01f, "above the dish row");
            foreach (Image picture in new[] { screen.CardScene.Worker, screen.CardScene.Prop, screen.CardScene.Dish })
            {
                if (!picture.enabled) continue;
                Rect box = World(picture.transform);
                Assert.GreaterOrEqual(box.xMin, scene.xMin, picture.name);
                Assert.LessOrEqual(box.xMax, scene.xMax, picture.name);
            }
        }

        Image HungLantern() => At("Canvas/Garden/Content/Lantern").GetComponent<Image>();

        Image SwatchUnder(Transform holder)
        {
            foreach (Image image in holder.GetComponentsInChildren<Image>(true))
                if (image.name == "Fill") return image;
            Assert.Fail("no lantern swatch under " + holder.name);
            return null;
        }

        void AssertLooksAsHung(Image swatch, int lanternColor)
        {
            runner.State.LanternColor = lanternColor;
            screen.Refresh();
            Image hung = HungLantern();
            Assert.AreSame(catalog.HangingLantern, swatch.sprite, "the hanging lantern, as in the garden");
            Assert.AreNotSame(catalog.StoneLantern, swatch.sprite, "the stone lantern never takes the colour");
            Assert.AreSame(hung.sprite, swatch.sprite);
            Assert.AreEqual(GardenScreen.LanternTint(lanternColor), swatch.color, "the garden's own tint");
            Assert.AreEqual(hung.color, swatch.color, "what the garden shows once this colour hangs");
            Assert.IsTrue(swatch.preserveAspect);
        }

        [Test]
        public void EachShopLanternColourLooksAsTheGardenHangsIt()
        {
            Assert.IsNotNull(catalog.HangingLantern, "the hanging lantern is drawn");
            Build();
            for (int color = 0; color < Decor.LanternColors.Length; color++)
                AssertLooksAsHung(SwatchUnder(At("Canvas/Shop/Card/Decor/Lanterns/Dot " + color)), color);
        }

        [Test]
        public void TheTrunksLanternOfferLooksAsTheGardenHangsIt()
        {
            Build();
            const int cal = 4;
            var offer = new TrunkOffer { Slot = 0, Code = "lantern", Kind = TrunkKind.Lantern, Id = "lantern:" + cal, Amount = 1, FullPrice = 10, Price = 10 };
            screen.TrunkCard.Open(new TrunkShelf { Open = true, Offers = new[] { offer } });
            AssertLooksAsHung(SwatchUnder(screen.TrunkCard.TileOf(0)), MonthPass.LanternIndex(cal));
        }

        [Test]
        public void TheMonthPassLanternLooksAsTheGardenHangsIt()
        {
            Build();
            screen.OpenSheet(2);
            screen.Refresh();
            int cal = MonthPass.CalendarMonth(MonthPass.MonthOf(runner.TodayNumber));
            Image swatch = null;
            foreach (Image image in At("Canvas/Sheet/Panel/Quests/Content/MonthPassRow").GetComponentsInChildren<Image>(true))
                if (image.name == "Icon" && image.sprite == catalog.HangingLantern) swatch = image;
            Assert.IsNotNull(swatch, "the lantern tier shows the hanging lantern");
            AssertLooksAsHung(swatch, MonthPass.LanternIndex(cal));
        }

        [Test]
        public void WithoutLanternArtTheSwatchFallsBackToADotOfTheFullColour()
        {
            catalog.Decor = new Sprite[0];
            Assert.IsNull(catalog.HangingLantern);
            Assert.AreSame(catalog.Round, LanternSwatch.SpriteFor(catalog));
            Assert.AreEqual(Palette.Hex(Decor.LanternColors[2]), LanternSwatch.TintFor(catalog, 2));
        }
    }
}
