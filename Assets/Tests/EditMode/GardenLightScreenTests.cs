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
                .Where(i => i.transform.parent == world && i.gameObject.activeSelf && (i.name == "Lantern" || i.name == "StoneLantern")).ToList();
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
