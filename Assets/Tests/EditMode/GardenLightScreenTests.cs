using System.Collections.Generic;
using System.Linq;
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
    public class GardenLightScreenTests
    {
        GameObject root;
        GameRunner runner;
        GardenScreen screen;
        GameCatalog catalog;
        bool raining;

        [SetUp]
        public void SetUp()
        {
            L10n.SetLocale("vi");
            catalog = AssetDatabase.LoadAssetAtPath<GameCatalog>("Assets/VuonTra/Content/GameCatalog.asset");
            root = new GameObject("Game");
            var cam = new GameObject("Cam", typeof(Camera));
            cam.transform.SetParent(root.transform);
            runner = root.AddComponent<GameRunner>();
            runner.Catalog = catalog;
            runner.Init(GameState.NewGame(catalog.BuildDefs()));
            runner.State.IntrosSeen.UnionWith(Intros.All);
            runner.State.IntrosTried.UnionWith(Intros.All);
            screen = root.AddComponent<GardenScreen>();
            screen.Runner = runner;
            screen.Cam = cam.GetComponent<Camera>();
            raining = false;
            screen.Raining = () => raining;
            screen.Build();
            screen.Shell.StartGame();
            Motion.SetReduced(true);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(root);
            Motion.SetReduced(false);
            Sfx.SetRain(false);
        }

        RectTransform Canvas => (RectTransform)root.transform.Find("Canvas");

        List<string> OverlaysAt(double hour, bool rain)
        {
            screen.ClockHours = () => hour;
            raining = rain;
            screen.Refresh();
            screen.Advance(0.1f);
            return SceneOverlays.Over(Canvas, Canvas.Find("Garden"), Canvas.Find("Hud")).Select(g => g.name).ToList();
        }

        [TestCase(22.0, false)]
        [TestCase(22.0, true)]
        [TestCase(13.0, false)]
        [TestCase(13.0, true)]
        [TestCase(18.0, false)]
        [TestCase(6.0, true)]
        public void No_More_Than_Two_Layers_Lie_Over_The_Whole_Garden(double hour, bool rain)
        {
            List<string> over = OverlaysAt(hour, rain);
            Assert.LessOrEqual(over.Count, SceneOverlays.Budget, string.Join(", ", over));
            CollectionAssert.IsSubsetOf(over, new[] { "Vignette", "PaperGrain" }, "only the vignette and the paper's grain");
        }

        [Test]
        public void The_Night_Garden_Has_Its_Vignette_And_Grain_And_Nothing_Else()
        {
            List<string> over = OverlaysAt(22.0, false);
            CollectionAssert.AreEquivalent(new[] { "Vignette", "PaperGrain" }, over);
            Assert.IsNull(Canvas.Find("MoonGlow"), "the moon's glow is painted into the vignette");
        }

        [Test]
        public void The_Washes_Of_The_Hour_And_The_Rain_Lie_On_The_Grass_Under_Everything_That_Stands()
        {
            OverlaysAt(13.0, true);
            int garden = Canvas.Find("Garden").GetSiblingIndex();
            Image daylight = Canvas.Find("Daylight").GetComponent<Image>();
            Image rain = Canvas.Find("RainTint").GetComponent<Image>();
            Assert.Greater(daylight.color.a, 0.1f);
            Assert.Greater(screen.RainTintAlpha, 0.1f);
            Assert.Greater(daylight.transform.GetSiblingIndex(), Canvas.Find("Ground").GetSiblingIndex());
            Assert.Less(daylight.transform.GetSiblingIndex(), garden);
            Assert.Less(rain.transform.GetSiblingIndex(), garden);
            Assert.IsFalse(daylight.raycastTarget);
            Assert.IsFalse(rain.raycastTarget);

            OverlaysAt(22.0, false);
            Assert.IsFalse(rain.gameObject.activeSelf, "dry: no rain colour");
            Assert.IsFalse(daylight.enabled, "night: no wash at all");
        }

        [Test]
        public void The_Grass_Is_The_Art_Bible_Green_With_The_Painted_Tile_Laid_Softly_Over_It()
        {
            Transform ground = Canvas.Find("Ground");
            Assert.AreEqual(Palette.Grass, ground.GetComponent<Image>().color);
            RawImage tile = ground.Find("Tile").GetComponent<RawImage>();
            Assert.AreSame(catalog.BgGrass, tile.texture);
            Assert.AreEqual(SceneOverlays.GrassTileStrength, tile.color.a, 1e-6f);
        }

        [Test]
        public void Every_Lantern_Pours_A_Warm_Pool_Of_Light_On_The_Grass_Below_What_Stands()
        {
            Transform world = Canvas.Find("Garden/Content");
            var pools = world.GetComponentsInChildren<Image>(true).Where(i => i.name == "LanternPool").ToList();
            var standing = world.GetComponentsInChildren<Image>(true)
                .Where(i => i.transform.parent == world && i.gameObject.activeSelf && (i.name == "Lantern" || i.name == "StoneLantern" || i.name == "Rim Lantern")).ToList();
            int lanterns = standing.Count;
            Assert.Greater(lanterns, 0);
            Assert.AreEqual(lanterns, pools.Count, "one pool for every lantern");
            int firstStanding = standing.Min(i => i.transform.GetSiblingIndex());
            foreach (Image pool in pools)
            {
                Assert.AreEqual(Palette.Alpha(Palette.Lantern, SceneOverlays.LanternPoolAlpha), pool.color);
                Assert.Less(pool.transform.GetSiblingIndex(), firstStanding, "flat on the grass, under every lantern");
                Assert.IsFalse(pool.raycastTarget);
                Assert.Greater(pool.rectTransform.sizeDelta.x, pool.rectTransform.sizeDelta.y, "lies flat");
            }
        }

        [Test]
        public void Every_Stone_Lantern_Of_The_Forest_Rim_Stands_In_Its_Own_Pool_Of_Light()
        {
            Transform world = Canvas.Find("Garden/Content");
            var rim = world.GetComponentsInChildren<Image>(true).Where(i => i.transform.parent == world && i.name == "Rim Lantern").ToList();
            var pools = world.GetComponentsInChildren<Image>(true).Where(i => i.name == "LanternPool").ToList();
            Assert.Greater(rim.Count, 0, "the rim has stone lanterns");
            foreach (Image lantern in rim)
            {
                Vector2 foot = lantern.rectTransform.anchoredPosition;
                Image pool = pools.FirstOrDefault(p => Vector2.Distance(p.rectTransform.anchoredPosition, foot) < 1f);
                Assert.IsNotNull(pool, "a pool at the foot of the rim lantern at " + foot);
                Assert.AreEqual(300f, pool.rectTransform.sizeDelta.x, 1e-3f, "the size of the other stone lanterns' pools");
                Assert.AreEqual(SceneOverlays.LanternPoolAlpha * lantern.color.a, pool.color.a, 1e-4f, "it fades with its lantern");
            }
        }

        List<Image> FlatGround()
        {
            Transform world = Canvas.Find("Garden/Content");
            return world.GetComponentsInChildren<Image>(true).Where(i =>
                (i.transform.parent == world && (i.name == "Path" || i.name == "Stone" || i.name == "Flagstone"))
                || (i.name == "Dash" && i.transform.parent.name.StartsWith("Ring "))
                || i.transform.parent.name.StartsWith("Segment Mist ")).ToList();
        }

        [TestCase(13.0, false)]
        [TestCase(13.0, true)]
        [TestCase(18.0, false)]
        [TestCase(6.0, true)]
        [TestCase(22.0, true)]
        public void Paths_And_All_Flat_Ground_Take_The_Same_Washes_As_The_Grass(double hour, bool rain)
        {
            OverlaysAt(22.0, false);
            List<Image> flat = FlatGround();
            foreach (string kind in new[] { "Path", "Stone", "Flagstone", "Dash", "Veil" })
                Assert.Greater(flat.Count(i => i.name == kind), 0, "the garden has " + kind);
            List<Color> night = flat.Select(i => i.color).ToList();

            OverlaysAt(hour, rain);
            Image daylight = Canvas.Find("Daylight").GetComponent<Image>();
            Image tint = Canvas.Find("RainTint").GetComponent<Image>();
            Color day = daylight.enabled ? daylight.color : Color.clear;
            Color wet = tint.gameObject.activeSelf ? tint.color : Color.clear;
            Assert.Greater(day.a + wet.a, 0.1f, "a wash lies on the grass");
            for (int i = 0; i < flat.Count; i++)
            {
                Color want = SceneOverlays.Washed(SceneOverlays.Washed(night[i], day), wet);
                Color got = flat[i].color;
                string what = flat[i].name + " " + i;
                Assert.AreEqual(want.r, got.r, 1e-4f, what);
                Assert.AreEqual(want.g, got.g, 1e-4f, what);
                Assert.AreEqual(want.b, got.b, 1e-4f, what);
                Assert.AreEqual(night[i].a, got.a, 1e-6f, what);
            }

            OverlaysAt(22.0, false);
            for (int i = 0; i < flat.Count; i++)
                Assert.AreEqual(night[i], flat[i].color, flat[i].name + " " + i + " is its own colour again at night");
        }

        [Test]
        public void A_Wash_Over_A_Piece_Of_Ground_Is_The_Wash_Over_The_Grass()
        {
            Color earth = new Color(0.3f, 0.35f, 0.25f, 1f), wash = new Color(0.9f, 0.85f, 0.7f, 0.2f);
            Assert.AreEqual(earth, SceneOverlays.Washed(earth, Color.clear));
            Color washed = SceneOverlays.Washed(earth, wash);
            Assert.Greater(washed.grayscale, earth.grayscale, "a pale wash lifts the earth");
            Assert.AreEqual(1f, washed.a);
            Color half = SceneOverlays.Washed(Palette.Alpha(earth, 0.5f), wash);
            Assert.AreEqual(0.5f, half.a, "the piece keeps its own alpha");
            Assert.AreEqual(washed.r, half.r, 1e-6f);
            Color linear = Palette.Mix(earth.linear, wash.linear, wash.a);
            Assert.AreEqual(linear.g, washed.linear.g, 1e-4f, "blended in linear light, as the screen blends the wash over the grass");
        }

        [Test]
        public void The_Vignette_Is_Soft_Night_Blue_And_Never_Darkens_The_Middle()
        {
            Assert.LessOrEqual(SceneOverlays.VignetteStrength, 0.25f);
            Assert.GreaterOrEqual(SceneOverlays.VignetteStrength, 0.15f);
            Texture2D vignette = catalog.BgVignette;
            Assert.IsNotNull(vignette);
            Assert.Less(vignette.width, vignette.height, "drawn for the portrait screen");
            string path = AssetDatabase.GetAssetPath(vignette);
            var pixels = new Texture2D(2, 2);
            pixels.LoadImage(System.IO.File.ReadAllBytes(path));
            try
            {
                float darkest = 0f, most = 0f;
                for (int y = 0; y < pixels.height; y++)
                    for (int x = 0; x < pixels.width; x++)
                    {
                        Color c = pixels.GetPixel(x, y);
                        most = Mathf.Max(most, c.a);
                        if (c.b < 0.3f) darkest = Mathf.Max(darkest, c.a);
                    }
                Assert.LessOrEqual(darkest, SceneOverlays.VignetteStrength + 0.01f, "the night edge never goes past its strength");
                Assert.LessOrEqual(most, SceneOverlays.VignetteStrength + SceneOverlays.MoonlightStrength + 0.01f);
                Assert.AreEqual(0f, pixels.GetPixel(pixels.width / 2, pixels.height / 2).a, 0.005f, "clear in the middle");
                Color corner = pixels.GetPixel(0, 0);
                Assert.AreEqual(Palette.Night.r, corner.r, 0.02f);
                Assert.AreEqual(Palette.Night.g, corner.g, 0.02f);
                Assert.AreEqual(Palette.Night.b, corner.b, 0.02f);
            }
            finally
            {
                Object.DestroyImmediate(pixels);
            }
        }
    }
}
