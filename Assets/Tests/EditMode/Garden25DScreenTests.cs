using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using VuonTra.Game;
using VuonTra.Game.UI;
using L10n = VuonTra.Game.L10n;
using Motion = VuonTra.Game.UI.Motion;

namespace VuonTra.Core.Tests
{
    public class Garden25DScreenTests
    {
        GameObject _root;
        GameRunner _runner;
        GardenScreen _screen;
        Camera _cam;

        void Open(bool island, string locale = "vi")
        {
            Features.Garden25D = island;
            L10n.SetLocale(locale);
            var catalog = AssetDatabase.LoadAssetAtPath<GameCatalog>("Assets/VuonTra/Content/GameCatalog.asset");
            _root = new GameObject("Game");
            _cam = new GameObject("Cam", typeof(Camera)).GetComponent<Camera>();
            _cam.transform.SetParent(_root.transform);
            _runner = _root.AddComponent<GameRunner>();
            _runner.Catalog = catalog;
            _runner.Init(GameState.NewGame(catalog.BuildDefs()));
            _runner.State.IntrosSeen.UnionWith(Intros.All);
            _runner.State.IntrosTried.UnionWith(Intros.All);
            _screen = _root.AddComponent<GardenScreen>();
            _screen.Runner = _runner;
            _screen.Cam = _cam;
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
                Object.DestroyImmediate(_runner.Garden25D.LockedPaint);
                Object.DestroyImmediate(_runner.Garden25D.gameObject);
            }
            Object.DestroyImmediate(_root);
            Features.Garden25D = Features.Garden25DByDefault;
            Motion.SetReduced(false);
            L10n.SetLocale("vi");
        }

        Garden25DWorld World => _runner.Garden25D;
        Canvas ScreenCanvas => _root.transform.Find("Canvas").GetComponent<Canvas>();
        GameObject Layer(string name) => _root.transform.Find("Canvas/" + name).gameObject;

        [Test]
        public void With_The_Switch_Off_The_Two_D_Garden_Draws_And_Takes_The_Taps_As_Before()
        {
            Open(false);
            Assert.IsNull(World);
            Assert.IsNull(_screen.IslandMap);
            Assert.AreEqual(RenderMode.ScreenSpaceCamera, ScreenCanvas.renderMode);
            Assert.AreSame(_cam, ScreenCanvas.worldCamera);
            Assert.IsTrue(_cam.enabled);
            Assert.IsTrue(Layer("Ground").activeSelf);
            Assert.IsTrue(Layer("Garden").activeSelf);
            Assert.IsTrue(Layer("Garden").GetComponent<Image>().raycastTarget, "the 2D garden catches drags and taps");
        }

        [Test]
        public void With_The_Switch_On_The_Two_D_Garden_Steps_Aside_For_The_Island_Camera()
        {
            Open(true);
            Assert.IsNotNull(World);
            Assert.AreEqual(RenderMode.ScreenSpaceOverlay, ScreenCanvas.renderMode, "the HUD draws over the island instead of under a second camera");
            Assert.IsNull(ScreenCanvas.worldCamera);
            Assert.IsFalse(_cam.enabled, "the 2D camera no longer clears over the island");
            Assert.IsTrue(World.Rig.View.enabled);
            Assert.IsFalse(Layer("Ground").activeSelf);
            Assert.IsFalse(Layer("Garden").activeSelf);
            Assert.IsTrue(Layer("Hud").activeSelf);
        }

        [Test]
        public void With_The_Switch_On_Nothing_On_The_Canvas_Covers_The_Middle_Of_The_Screen_So_Taps_Reach_The_Island()
        {
            Open(true);
            RectTransform canvas = (RectTransform)ScreenCanvas.transform;
            Vector2 middle = canvas.rect.center;
            var corners = new Vector3[4];
            foreach (Graphic g in canvas.GetComponentsInChildren<Graphic>(false))
            {
                if (!g.raycastTarget || !g.enabled) continue;
                g.rectTransform.GetWorldCorners(corners);
                Vector2 a = canvas.InverseTransformPoint(corners[0]), b = canvas.InverseTransformPoint(corners[2]);
                Rect area = Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
                Assert.IsFalse(area.Contains(middle), g.name + " under " + g.transform.parent.name + " would take the taps meant for the island");
            }
        }

        [Test]
        public void With_The_Switch_On_The_Stations_Stand_On_The_Island_And_A_Tap_On_One_Opens_Its_Card()
        {
            Open(true);
            Assert.IsNotNull(World.Stands);
            Assert.IsTrue(World.Stands.Stand(0).gameObject.activeSelf, "the teapot stands on the island");
            World.Stands.Tap(0);
            Assert.AreEqual(0, _screen.Selected);
            World.Ground.Tap(new PointerEventData(null) { pointerCurrentRaycast = new RaycastResult { worldPosition = Vector3.zero } });
            Assert.AreEqual(-1, _screen.Selected, "a tap on open ground puts the card away");
        }

        [Test]
        public void With_The_Switch_On_The_Edge_Pills_And_The_Way_Home_Of_The_Two_D_View_Stay_Away()
        {
            Open(true);
            for (int i = 0; i < 5; i++) _screen.Advance(0.1f);
            foreach (string pill in new[] { "EdgeUp", "EdgeDown", "EdgeLeft", "EdgeRight", "Home" })
                Assert.IsFalse(Layer(pill).activeSelf, pill);
        }

        [Test]
        public void With_The_Switch_On_The_Zoom_Button_Steps_The_Island_Camera_And_Sheets_Still_Open()
        {
            Open(true);
            Motion.SetReduced(true);
            CameraSpec spec = World.Rig.Spec;
            _screen.CycleZoom();
            Assert.AreEqual(spec.NearDistance, World.Rig.Distance, 1e-6);
            _screen.CycleZoom();
            Assert.AreEqual(spec.FarDistance, World.Rig.Distance, 1e-6);
            _screen.OpenSheet(0);
            Assert.IsTrue(_screen.SheetOpen);
            _screen.Advance(0f);
            Assert.IsFalse(_screen.IslandMap.Pill.gameObject.activeSelf, "no map pill over an open sheet");
            _screen.CloseSheet();
            _screen.Advance(0f);
            Assert.IsTrue(_screen.IslandMap.Pill.gameObject.activeSelf);
        }

        [Test]
        public void The_Map_Pill_Opens_A_Top_Down_Shot_Under_The_Hud_That_Fits_Its_Frame_Even_Twice()
        {
            Open(true);
            GardenMapView map = _screen.IslandMap;
            Assert.IsTrue(map.Pill.gameObject.activeSelf);
            foreach (RectTransform button in new[] { map.Pill, map.CloseButton })
            {
                Rect hit = ((RectTransform)button.Find("Hit")).rect;
                Assert.GreaterOrEqual(Mathf.Min(hit.width, hit.height), 132f, button.name + " can be hit with a thumb: 44 pt on a 1080-wide canvas");
                Assert.IsTrue(button.Find("Hit").GetComponent<Image>().raycastTarget);
            }
            Assert.AreEqual("Bản đồ", map.Pill.GetComponentInChildren<Text>().text);
            for (int round = 0; round < 2; round++)
            {
                map.Pill.GetComponent<Button>().onClick.Invoke();
                Assert.IsTrue(map.Open);
                Assert.IsFalse(map.Pill.gameObject.activeSelf);
                Texture shot = map.Shot.texture;
                Assert.IsNotNull(shot);
                Assert.AreSame(World.Map.Shot, shot);
                Rect frame = map.Shot.rectTransform.rect;
                Assert.AreEqual(frame.width / frame.height, (float)shot.width / shot.height, 0.01f, "the shot is not stretched");
                Assert.Less(map.Root.GetSiblingIndex(), Layer("Hud").transform.GetSiblingIndex(), "the HUD stays over the map");
                map.CloseButton.GetComponent<Button>().onClick.Invoke();
                Assert.IsFalse(map.Open);
                Assert.IsNull(World.Map.Shot, "closing the map hands its texture back");
            }
        }

        [Test]
        public void Tapping_A_Spot_On_The_Map_Closes_It_And_Travels_The_Garden_There()
        {
            Open(true);
            GardenMapView map = _screen.IslandMap;
            map.Show();
            var target = new Hex(-2, 1);
            GroundPoint ground = GardenFraming.Ground(target, World.Rig.Spec);
            Rect frame = map.Shot.rectTransform.rect;
            double aspect = (double)World.Map.Shot.width / World.Map.Shot.height;
            MapFrame shot = GardenFraming.Map(GardenFraming.Pad(World.Island, GardenMapView.SeaAround), aspect);
            float u = (float)(0.5d + (ground.X - shot.Centre.X) / (2d * shot.HalfHeight * aspect));
            float v = (float)(0.5d + (ground.Z - shot.Centre.Z) / (2d * shot.HalfHeight));
            Vector3 world = map.Shot.rectTransform.TransformPoint(new Vector3(frame.xMin + u * frame.width, frame.yMin + v * frame.height, 0f));
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(null, world);
            var tap = new PointerEventData(null) { position = screen, pressPosition = screen };
            ExecuteEvents.Execute(map.Shot.gameObject, tap, ExecuteEvents.pointerClickHandler);
            Assert.IsFalse(map.Open);
            Assert.IsNull(World.Map.Shot);
            Assert.Greater(World.PendingMotion, 0, "the garden glides to the spot");
            World.Tick(Motion.Travel + 0.1f);
            Assert.AreEqual(ground.X, World.Rig.Focus.X, 0.02d);
            Assert.AreEqual(ground.Z, World.Rig.Focus.Z, 0.02d);
        }

        [Test]
        public void Under_Reduced_Motion_The_Map_Jumps_Straight_To_The_Spot()
        {
            Open(true);
            Motion.SetReduced(true);
            GardenMapView map = _screen.IslandMap;
            map.Show();
            map.TapAt(0.5d, 0.5d);
            Assert.AreEqual(0, World.PendingMotion);
            Assert.AreEqual(World.Island.Centre.X, World.Rig.Focus.X, 1e-6);
            Assert.AreEqual(World.Island.Centre.Z, World.Rig.Focus.Z, 1e-6);
        }

        [Test]
        public void In_English_The_Map_Pill_Says_Map()
        {
            Open(true, "en");
            Assert.AreEqual("Map", _screen.IslandMap.Pill.GetComponentInChildren<Text>().text);
        }
    }
}
