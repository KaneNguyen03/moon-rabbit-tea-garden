using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;
using VuonTra.Game;
using VuonTra.Game.UI;
using Motion = VuonTra.Game.UI.Motion;

namespace VuonTra.Core.Tests
{
    public class Garden25DWorldTests
    {
        Garden25DWorld _world;
        RenderTexture _screen;
        GardenCameraSettings _settings;
        bool _reduced;

        [SetUp]
        public void SetUp()
        {
            _reduced = Motion.Reduced;
            Motion.Reduced = false;
            _settings = ScriptableObject.CreateInstance<GardenCameraSettings>();
            _world = Garden25DWorld.Create(0, _settings, null, null);
            _screen = new RenderTexture(1080, 1920, 24);
            _world.Rig.View.targetTexture = _screen;
            double clock = 0d;
            _world.PanZoom.Clock = () => clock += 1d;
            Physics.SyncTransforms();
        }

        [TearDown]
        public void TearDown()
        {
            Motion.Reduced = _reduced;
            _world.Rig.View.targetTexture = null;
            _world.Map.Release();
            Object.DestroyImmediate(_world.gameObject);
            Object.DestroyImmediate(_screen);
            Object.DestroyImmediate(_settings);
        }

        [Test]
        public void The_Garden_Two_Point_Five_D_Switch_Is_Off_So_Nothing_Is_Built_By_Default()
        {
            Assert.IsFalse(Features.Garden25D);
        }

        [Test]
        public void The_Assets_Load_The_Camera_Settings_And_Both_Island_Materials()
        {
            Assert.IsNotNull(Resources.Load<GardenCameraSettings>(Garden25DWorld.SettingsPath));
            Assert.IsNotNull(Resources.Load<Material>(Garden25DWorld.LawnPath));
            Assert.IsNotNull(Resources.Load<Material>(Garden25DWorld.EarthPath));
        }

        [Test]
        public void The_Two_D_Renderer_Stays_First_And_The_Garden_Camera_Gets_A_Forward_Plus_Renderer()
        {
            var pipeline = UnityEditor.AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/UniversalRP.asset");
            Assert.AreEqual("Renderer2D", pipeline.rendererDataList[0].name);
            var garden = pipeline.rendererDataList[Garden25DWorld.RendererIndex] as UniversalRendererData;
            Assert.IsNotNull(garden);
            Assert.AreEqual(RenderingMode.ForwardPlus, garden.renderingMode);
        }

        [Test]
        public void The_World_Opens_On_The_Teapot_At_The_Default_Distance_With_A_Physics_Raycaster()
        {
            Assert.IsNotNull(_world.Rig.View.GetComponent<PhysicsRaycaster>());
            Assert.IsFalse(_world.Rig.View.orthographic);
            Assert.AreEqual(_world.Rig.Spec.DefaultDistance, _world.Rig.Distance, 1e-9);
            Assert.AreEqual(0d, _world.Rig.Focus.X, 1e-9);
            Assert.AreEqual(0d, _world.Rig.Focus.Z, 1e-9);
            Assert.IsTrue(_world.Ground.GetComponent<MeshCollider>().sharedMesh.bounds.size.x > 0f);
        }

        [Test]
        public void Tapping_A_Tile_Through_The_Physics_Raycaster_Names_That_Tile_And_Not_Its_Neighbour()
        {
            foreach (Hex target in new[] { new Hex(0, 0), new Hex(1, 0), new Hex(-3, 2), new Hex(5, -3) })
            {
                Hex? tapped = null;
                void Heard(Hex h) => tapped = h;
                _world.Ground.TileTapped += Heard;
                GroundPoint g = GardenFraming.Ground(target, _world.Rig.Spec);
                Vector3 screen = _world.Rig.View.WorldToScreenPoint(new Vector3((float)g.X, 0f, (float)g.Z));
                var press = new PointerEventData(null) { position = screen, pressPosition = screen };
                var hits = new List<RaycastResult>();
                _world.Rig.View.GetComponent<PhysicsRaycaster>().Raycast(press, hits);
                Assert.IsNotEmpty(hits, "tile " + target);
                Assert.AreEqual(_world.Ground.gameObject, hits[0].gameObject);
                press.pointerCurrentRaycast = hits[0];
                ExecuteEvents.Execute(hits[0].gameObject, press, ExecuteEvents.pointerClickHandler);
                _world.Ground.TileTapped -= Heard;
                Assert.AreEqual(target, tapped);
            }
        }

        [Test]
        public void A_Pan_Or_A_Pinch_Ending_Where_It_Began_Names_No_Tile_And_A_Quick_Second_Tap_Zooms_Instead()
        {
            int heard = 0;
            void Heard(Hex h) => heard++;
            _world.Ground.TileTapped += Heard;
            Vector2 at = _world.Rig.View.WorldToScreenPoint(Vector3.zero);
            var hits = new List<RaycastResult>();
            _world.Rig.View.GetComponent<PhysicsRaycaster>().Raycast(new PointerEventData(null) { position = at }, hits);
            PointerEventData Finger(int id) => new PointerEventData(null) { pointerId = id, position = at, pressPosition = at, pointerCurrentRaycast = hits[0] };
            void Click(PointerEventData e) => ExecuteEvents.Execute(_world.Ground.gameObject, e, ExecuteEvents.pointerClickHandler);

            PointerEventData pan = Finger(0);
            _world.PanZoom.OnPointerDown(pan);
            pan.position = at + new Vector2(300f, 0f);
            _world.PanZoom.OnDrag(pan);
            pan.position = at;
            _world.PanZoom.OnDrag(pan);
            _world.PanZoom.OnPointerUp(pan);
            Click(pan);
            Assert.AreEqual(0, heard, "a drag never counts as a tap, even one that comes back");

            PointerEventData still = Finger(0), spread = Finger(1);
            spread.position = spread.pressPosition = at + new Vector2(80f, 0f);
            _world.PanZoom.OnPointerDown(still);
            _world.PanZoom.OnPointerDown(spread);
            spread.position += new Vector2(60f, 0f);
            _world.PanZoom.OnDrag(spread);
            _world.PanZoom.OnPointerUp(spread);
            _world.PanZoom.OnPointerUp(still);
            Click(still);
            Assert.AreEqual(0, heard, "lifting the still finger of a pinch is not a tap");

            _world.PanZoom.Clock = () => 5d;
            PointerEventData tap = Finger(0);
            _world.PanZoom.OnPointerDown(tap);
            _world.PanZoom.OnPointerUp(tap);
            Click(tap);
            Assert.AreEqual(1, heard);
            _world.PanZoom.OnPointerDown(tap);
            _world.PanZoom.OnPointerUp(tap);
            Click(tap);
            Assert.AreEqual(1, heard, "the second tap of a double tap zooms instead");
            Assert.Greater(_world.PendingMotion, 0);
            _world.Ground.TileTapped -= Heard;
        }

        [Test]
        public void A_Finger_Resting_Within_The_Tap_Slop_Does_Not_Move_The_Garden()
        {
            double slop = GardenFraming.TapSlopPixels(Screen.width, Screen.height);
            var finger = new PointerEventData(null) { pointerId = 0, position = new Vector2(540f, 960f) };
            _world.PanZoom.OnPointerDown(finger);
            finger.position += new Vector2((float)slop * 0.8f, 0f);
            _world.PanZoom.OnDrag(finger);
            Assert.AreEqual(0d, _world.Rig.Focus.X, 1e-9);
            finger.position += new Vector2((float)slop + 40f, 0f);
            _world.PanZoom.OnDrag(finger);
            Assert.Less(_world.Rig.Focus.X, 0d, "past the slop the drag moves the garden");
        }

        void DoubleTap(Vector2 spot)
        {
            _world.PanZoom.Clock = () => 7d;
            var tap = new PointerEventData(null) { position = spot, pressPosition = spot };
            _world.PanZoom.OnPointerClick(tap);
            _world.PanZoom.OnPointerClick(tap);
        }

        [Test]
        public void A_Double_Tap_Glides_In_To_Near_Around_The_Spot_And_The_Next_Back_To_Default()
        {
            CameraSpec spec = _world.Rig.Spec;
            Vector2 spot = _world.Rig.View.WorldToScreenPoint(new Vector3(1f, 0f, 1f));
            DoubleTap(spot);
            Assert.Greater(_world.PendingMotion, 0, "the zoom travels");
            _world.Tick(Motion.Travel / 2f);
            Assert.Less(_world.Rig.Distance, spec.DefaultDistance);
            Assert.Greater(_world.Rig.Distance, spec.NearDistance);
            _world.Tick(Motion.Travel);
            Assert.AreEqual(spec.NearDistance, _world.Rig.Distance, 1e-6);
            Vector2 stillThere = _world.Rig.View.WorldToScreenPoint(new Vector3(1f, 0f, 1f));
            Assert.AreEqual(spot.x, stillThere.x, 1f, "the ground under the finger stays under it");
            Assert.AreEqual(spot.y, stillThere.y, 1f);
            DoubleTap(spot);
            _world.Tick(Motion.Travel + 0.1f);
            Assert.AreEqual(spec.DefaultDistance, _world.Rig.Distance, 1e-6);
        }

        [Test]
        public void Two_Slow_Taps_Are_Two_Single_Taps_And_Do_Not_Zoom()
        {
            Vector2 middle = _world.Rig.View.WorldToScreenPoint(Vector3.zero);
            var tap = new PointerEventData(null) { position = middle, pressPosition = middle };
            _world.PanZoom.OnPointerClick(tap);
            _world.PanZoom.OnPointerClick(tap);
            Assert.AreEqual(0, _world.PendingMotion);
            Assert.AreEqual(_world.Rig.Spec.DefaultDistance, _world.Rig.Distance, 1e-9);
        }

        [Test]
        public void Under_Reduced_Motion_A_Double_Tap_Zooms_At_Once()
        {
            Motion.Reduced = true;
            DoubleTap(_world.Rig.View.WorldToScreenPoint(Vector3.zero));
            Assert.AreEqual(0, _world.PendingMotion);
            Assert.AreEqual(_world.Rig.Spec.NearDistance, _world.Rig.Distance, 1e-6);
        }

        [Test]
        public void The_Island_Wears_Design_Colours_Grass_Lip_Shadow_Gap_Lit_And_Shaded_Earth_And_Underside()
        {
            Garden25DWorld loaded = Garden25DWorld.Load(0);
            try
            {
                Material[] paints = loaded.Ground.GetComponent<MeshRenderer>().sharedMaterials;
                Assert.AreEqual(SlabMesh.PartCount, paints.Length);
                Assert.AreEqual(SlabMesh.PartCount, loaded.Ground.GetComponent<MeshFilter>().sharedMesh.subMeshCount);
                Assert.AreSame(Resources.Load<Material>(Garden25DWorld.LawnPath), paints[(int)SlabPart.Lawn], "the lawn keeps the #129 grass tile");
                AssertHex("366F62", loaded.Paint(SlabPart.Lip));
                AssertHex("453448", loaded.Paint(SlabPart.Gap));
                AssertHex("85554C", loaded.Paint(SlabPart.Earth));
                AssertHex("60424A", loaded.Paint(SlabPart.EarthShade));
                AssertHex("362C47", loaded.Paint(SlabPart.Underside));
                var settings = Resources.Load<GardenCameraSettings>(Garden25DWorld.SettingsPath);
                Assert.AreSame(loaded.LockedPaint, loaded.LockedLand.GetComponent<MeshRenderer>().sharedMaterial);
                AssertHex("31605C", loaded.LockedPaint);
                Assert.AreEqual(ColorUtility.ToHtmlStringRGB(settings.LockedGrass), ColorUtility.ToHtmlStringRGB(loaded.LockedPaint.color), "design's temporary colour for land not opened yet, no fog");
                Assert.IsNull(loaded.LockedPaint.mainTexture, "a flat colour until design delivers the locked land's shape");
                AssertHex("85554C", Resources.Load<Material>(Garden25DWorld.EarthPath));
                Assert.AreEqual("Universal Render Pipeline/Unlit", Resources.Load<Material>(Garden25DWorld.EarthPath).shader.name, "flat toon steps, not lit by a light the scene does not have yet");
                Assert.AreEqual("Universal Render Pipeline/Unlit", Resources.Load<Material>(Garden25DWorld.LawnPath).shader.name, "the grass reads as bright as the 2D garden until the moon light of #147");
            }
            finally
            {
                for (int part = 1; part < SlabMesh.PartCount; part++) Object.DestroyImmediate(loaded.Paint((SlabPart)part));
                Object.DestroyImmediate(loaded.LockedPaint);
                Object.DestroyImmediate(loaded.gameObject);
            }
        }

        [Test]
        public void Dark_Grass_Spreads_Past_The_Island_Edge_Below_Its_Earth_So_The_Frame_Has_No_Empty_Night()
        {
            GardenApron land = _world.LockedLand;
            var slab = _settings.Slab();
            Assert.AreEqual(-(float)slab.Thickness - GardenApron.Below, land.Top, 1e-5);
            GroundRect island = _world.Island;
            Assert.LessOrEqual(land.Area.MinX, island.MinX - _settings.LockedReach + 1e-6);
            Assert.GreaterOrEqual(land.Area.MaxZ, island.MaxZ + _settings.LockedReach - 1e-6);
            var plane = new Plane(Vector3.up, new Vector3(0f, land.Top, 0f));
            Camera view = _world.Rig.View;
            foreach (GroundPoint corner in new[] { new GroundPoint(island.MinX, island.MinZ), new GroundPoint(island.MaxX, island.MaxZ) })
            {
                _world.Rig.Frame(corner, _world.Rig.Spec.FarDistance);
                foreach (Vector2 screen in new[] { Vector2.zero, new Vector2(view.pixelWidth, 0f), new Vector2(0f, view.pixelHeight), new Vector2(view.pixelWidth, view.pixelHeight) })
                {
                    Ray ray = view.ScreenPointToRay(screen);
                    Assert.IsTrue(plane.Raycast(ray, out float along), "every corner of the screen looks down onto the ground");
                    Vector3 hit = ray.GetPoint(along);
                    Assert.IsTrue(land.Area.Contains(new GroundPoint(hit.x, hit.z)), "zoomed all the way out at the island's corner, the screen corner " + screen + " still shows grass, not night");
                }
            }
            Mesh islandMesh = _world.Ground.GetComponent<MeshFilter>().sharedMesh, lockedMesh = _world.LockedLand.GetComponent<MeshFilter>().sharedMesh;
            _world.Grow(3);
            Assert.AreSame(islandMesh, _world.Ground.GetComponent<MeshFilter>().sharedMesh, "growing refills the island mesh instead of destroying it, which edit mode forbids");
            Assert.AreSame(lockedMesh, _world.LockedLand.GetComponent<MeshFilter>().sharedMesh);
            Physics.SyncTransforms();
            Assert.AreEqual((float)_world.Island.MaxX, _world.Ground.GetComponent<MeshCollider>().bounds.max.x, 0.05f, "the tap collider grows with the island");
            Assert.LessOrEqual(_world.LockedLand.Area.MaxX, _world.Island.MaxX + _settings.LockedReach + 1e-6);
            Assert.GreaterOrEqual(_world.LockedLand.Area.MaxX, _world.Island.MaxX + _settings.LockedReach - 1e-6, "it grows with the island");
        }

        [Test]
        public void A_Drag_Starting_On_The_Dark_Grass_Moves_The_Garden_And_A_Tap_There_Names_No_Tile()
        {
            int tiles = 0, touches = 0;
            _world.Ground.TileTapped += h => tiles++;
            _world.Ground.Touched += () => touches++;
            GroundRect island = _world.Island;
            var outside = new Vector3((float)island.MaxX + 3f, _world.LockedLand.Top, (float)island.Centre.Z);
            _world.Rig.Frame(new GroundPoint(island.MaxX, island.Centre.Z), _world.Rig.Spec.DefaultDistance);
            Physics.SyncTransforms();
            Vector2 at = _world.Rig.View.WorldToScreenPoint(outside);
            var press = new PointerEventData(null) { position = at, pressPosition = at };
            var hits = new List<RaycastResult>();
            _world.Rig.View.GetComponent<PhysicsRaycaster>().Raycast(press, hits);
            hits.Sort((a, b) => a.distance.CompareTo(b.distance));
            Assert.AreSame(_world.LockedLand.gameObject, hits[0].gameObject);
            press.pointerCurrentRaycast = hits[0];
            GameObject handler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject);
            ExecuteEvents.Execute(handler, press, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(handler, press, ExecuteEvents.pointerUpHandler);
            press.pointerPress = handler;
            ExecuteEvents.Execute(handler, press, ExecuteEvents.pointerClickHandler);
            Assert.AreEqual(0, tiles, "land not opened yet has no tile to name");
            Assert.AreEqual(1, touches, "but the tap still reaches the ground, so an open card can close");
            GroundPoint before = _world.Rig.Focus;
            var drag = new PointerEventData(null) { position = at, pressPosition = at, pointerCurrentRaycast = hits[0] };
            ExecuteEvents.Execute(handler, drag, ExecuteEvents.pointerDownHandler);
            drag.position = at + new Vector2(0f, -300f);
            ExecuteEvents.Execute(handler, drag, ExecuteEvents.dragHandler);
            Assert.AreNotEqual(before.Z, _world.Rig.Focus.Z, "dragging on the dark grass moves the view like dragging on the lawn");
            ExecuteEvents.Execute(handler, drag, ExecuteEvents.pointerUpHandler);
        }

        static void AssertHex(string hex, Material paint)
        {
            Assert.IsNotNull(paint);
            Assert.AreEqual(hex, ColorUtility.ToHtmlStringRGB(paint.color), paint.name);
        }

        [Test]
        public void Dragging_Far_Off_The_Edge_Gives_A_Little_Then_Settles_On_The_Island()
        {
            GroundRect island = _world.Island;
            var finger = new PointerEventData(null) { pointerId = 0, position = new Vector2(540f, 960f) };
            _world.PanZoom.OnPointerDown(finger);
            for (int i = 0; i < 40; i++)
            {
                finger.delta = new Vector2(-200f, 0f);
                finger.position += finger.delta;
                _world.PanZoom.OnDrag(finger);
            }
            Assert.Greater(_world.Rig.Focus.X, island.MaxX, "the edge gives under the finger");
            Assert.LessOrEqual(_world.Rig.Focus.X, island.MaxX + _world.Rig.Spec.EdgeGive + 1e-6);
            _world.PanZoom.OnPointerUp(finger);
            _world.Tick(2f);
            Assert.IsTrue(island.Contains(_world.Rig.Focus), "after the glide the view centre is back on the island");
        }

        [Test]
        public void Under_Reduced_Motion_Letting_Go_Stops_At_Once()
        {
            Motion.Reduced = true;
            var finger = new PointerEventData(null) { pointerId = 0, position = new Vector2(540f, 960f) };
            _world.PanZoom.OnPointerDown(finger);
            finger.delta = new Vector2(-30f, 0f);
            finger.position += finger.delta;
            _world.PanZoom.OnDrag(finger);
            GroundPoint held = _world.Rig.Focus;
            _world.PanZoom.OnPointerUp(finger);
            Assert.AreEqual(held.X, _world.Rig.Focus.X, 1e-9);
            Assert.AreEqual(0, _world.PendingMotion);
        }

        [Test]
        public void Spreading_Two_Fingers_Zooms_In_And_Stops_At_Near()
        {
            var a = new PointerEventData(null) { pointerId = 0, position = new Vector2(500f, 960f) };
            var b = new PointerEventData(null) { pointerId = 1, position = new Vector2(580f, 960f) };
            _world.PanZoom.OnPointerDown(a);
            _world.PanZoom.OnPointerDown(b);
            b.delta = new Vector2(80f, 0f);
            b.position += b.delta;
            _world.PanZoom.OnDrag(b);
            Assert.Less(_world.Rig.Distance, _world.Rig.Spec.DefaultDistance);
            for (int i = 0; i < 20; i++)
            {
                b.delta = new Vector2(100f, 0f);
                b.position += b.delta;
                _world.PanZoom.OnDrag(b);
            }
            Assert.AreEqual(_world.Rig.Spec.NearDistance, _world.Rig.Distance, 1e-6);
        }

        [Test]
        public void The_Map_Is_A_Top_Down_Shot_Of_The_Screen_Size_Even_When_Opened_Twice()
        {
            RenderTexture first = _world.Map.Shoot(_world.Island, 720, 1280);
            Assert.IsNotNull(first);
            RenderTexture second = _world.Map.Shoot(_world.Island, 1080, 1920);
            Assert.IsNotNull(second);
            Assert.AreEqual(1080, second.width);
            Assert.AreEqual(1920, second.height);
            Assert.AreSame(second, _world.Map.Shot);
            Camera map = _world.Map.View;
            Assert.IsTrue(map.orthographic);
            Assert.IsFalse(map.enabled, "the map camera only renders on request");
            Assert.AreEqual(Palette.Night, map.backgroundColor, "the sea around the island on the map is the night sky");
            Assert.AreEqual(90f, map.transform.eulerAngles.x, 1e-3f);
        }
    }
}
