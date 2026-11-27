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
    public class Garden25DMapPillTouchTests
    {
        const float AndroidDensity = 3f;
        const float AndroidMinimumDp = 48f;
        const float PillWidth = 270f, PillHeight = 92f;

        GameObject _root;
        GameRunner _runner;
        GardenScreen _screen;

        [SetUp]
        public void SetUp()
        {
            Features.Garden25D = true;
            L10n.SetLocale("vi");
            var catalog = AssetDatabase.LoadAssetAtPath<GameCatalog>("Assets/VuonTra/Content/GameCatalog.asset");
            _root = new GameObject("Game");
            var cam = new GameObject("Cam", typeof(Camera)).GetComponent<Camera>();
            cam.transform.SetParent(_root.transform);
            _runner = _root.AddComponent<GameRunner>();
            _runner.Catalog = catalog;
            _runner.Init(GameState.NewGame(catalog.BuildDefs()));
            _runner.State.IntrosSeen.UnionWith(Intros.All);
            _runner.State.IntrosTried.UnionWith(Intros.All);
            _screen = _root.AddComponent<GardenScreen>();
            _screen.Runner = _runner;
            _screen.Cam = cam;
            _screen.Raining = () => false;
            _screen.Build();
            _screen.Refresh();
            _screen.Shell.StartGame();
            _screen.Tweens.Update(5f);
            _screen.Advance(0f);
        }

        [TearDown]
        public void TearDown()
        {
            if (_runner != null && _runner.Garden25D != null)
            {
                _runner.Garden25D.Map.Release();
                for (int part = 0; part < SlabMesh.PartCount; part++)
                    if ((SlabPart)part != SlabPart.Lawn) Object.DestroyImmediate(_runner.Garden25D.Paint((SlabPart)part));
                Object.DestroyImmediate(_runner.Garden25D.gameObject);
            }
            Object.DestroyImmediate(_root);
            Features.Garden25D = Features.Garden25DByDefault;
            Motion.SetReduced(false);
            L10n.SetLocale("vi");
        }

        [Test]
        public void The_Map_Pill_And_Its_Close_Button_Reach_48_Dp_On_Both_Axes_At_Android_Density_3()
        {
            GardenMapView map = _screen.IslandMap;
            Assert.IsTrue(map.Pill.gameObject.activeSelf);
            float least = AndroidMinimumDp * AndroidDensity;
            foreach (RectTransform button in new[] { map.Pill, map.CloseButton })
            {
                var hit = (RectTransform)button.Find("Hit");
                Assert.IsNotNull(hit, button.name + " has a touch area");
                Assert.GreaterOrEqual(hit.rect.width, least, button.name + " touch width in canvas units at 1080x1920");
                Assert.GreaterOrEqual(hit.rect.height, least, button.name + " touch height in canvas units at 1080x1920");
                Assert.IsTrue(hit.GetComponent<Image>().raycastTarget, button.name + " touch area takes taps");
            }
        }

        [Test]
        public void The_Drawn_Map_Pill_Keeps_Its_Size_While_Its_Touch_Area_Grows()
        {
            RectTransform pill = _screen.IslandMap.Pill;
            Assert.AreEqual(PillWidth, pill.rect.width, 0.01f);
            Assert.AreEqual(PillHeight, pill.rect.height, 0.01f);
            Rect hit = ((RectTransform)pill.Find("Hit")).rect;
            Assert.AreEqual(PillWidth, hit.width, 0.01f, "the pill is already wider than the minimum, so its touch area grows only in height");
            Assert.AreEqual(pill.rect.center.y, hit.center.y, 0.01f, "the touch area grows evenly above and below the pill");
        }
    }
}
