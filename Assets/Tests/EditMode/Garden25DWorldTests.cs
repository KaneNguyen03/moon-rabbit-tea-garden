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
                var press = new PointerEventData(null) { position = screen };
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
