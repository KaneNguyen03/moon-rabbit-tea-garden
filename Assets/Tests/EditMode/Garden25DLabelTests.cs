using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using VuonTra.Game;
using VuonTra.Game.UI;
using Motion = VuonTra.Game.UI.Motion;

namespace VuonTra.Core.Tests
{
    public class Garden25DLabelTests
    {
        GameCatalog _catalog;
        GardenStandSettings _settings;
        List<StationDef> _defs;
        GameState _state;
        GardenCameraSettings _camera;
        Garden25DWorld _world;
        GardenStandRow _row;
        RenderTexture _screen;
        bool _reduced;

        [SetUp]
        public void SetUp()
        {
            _reduced = Motion.Reduced;
            Motion.Reduced = true;
            _catalog = AssetDatabase.LoadAssetAtPath<GameCatalog>("Assets/VuonTra/Content/GameCatalog.asset");
            _settings = Object.Instantiate(Resources.Load<GardenStandSettings>(Garden25DWorld.StandsPath));
            _defs = _catalog.BuildDefs();
            _state = GameState.NewGame(_defs);
            _state.Plots = _defs.Count;
            for (int i = 0; i < _defs.Count; i++) _state.Levels[i] = 1 + i;
            GardenGrid.Settle(_defs, _state);
            _state.Rabbits.Add(new Rabbit { Id = 100, Station = 0, Name = "R0" });
            _camera = ScriptableObject.CreateInstance<GardenCameraSettings>();
            _world = Garden25DWorld.Create(_state.Grid.Segments, _camera, null, null);
            _screen = new RenderTexture(1080, 1920, 24);
            _world.Rig.View.targetTexture = _screen;
            _row = _world.Raise(_catalog, _settings);
            _row.Refresh(_defs, _state);
            Canvas.ForceUpdateCanvases();
        }

        [TearDown]
        public void TearDown()
        {
            Motion.Reduced = _reduced;
            _world.Rig.View.targetTexture = null;
            _world.Map.Release();
            Object.DestroyImmediate(_world.gameObject);
            Object.DestroyImmediate(_screen);
            Object.DestroyImmediate(_camera);
            Object.DestroyImmediate(_settings);
        }

        CameraSpec Spec => _world.Rig.Spec;

        void Zoom(double distance)
        {
            _world.Rig.Frame(_world.Rig.Focus, distance);
            Canvas.ForceUpdateCanvases();
        }

        List<StandLabel> Shown(out int texts)
        {
            var shown = new List<StandLabel>();
            texts = 0;
            for (int i = 0; i < _row.Count; i++)
            {
                StandLabel label = _row.Label(i);
                if (!label.gameObject.activeInHierarchy || (!label.PadShown && !label.LevelShown)) continue;
                shown.Add(label);
                texts += (label.PadShown ? 1 : 0) + (label.LevelShown ? 1 : 0);
            }
            return shown;
        }

        Rect ScreenRect(RectTransform plate)
        {
            var corners = new Vector3[4];
            plate.GetWorldCorners(corners);
            Camera view = _world.Rig.View;
            Vector2 min = new Vector2(float.MaxValue, float.MaxValue), max = new Vector2(float.MinValue, float.MinValue);
            foreach (Vector3 corner in corners)
            {
                Vector3 p = view.WorldToScreenPoint(corner);
                min = Vector2.Min(min, p);
                max = Vector2.Max(max, p);
            }
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        IEnumerable<RectTransform> Plates(StandLabel label)
        {
            if (label.PadShown) yield return (RectTransform)label.PadText.transform.parent;
            if (label.LevelShown) yield return (RectTransform)label.LevelText.transform.parent;
        }

        [Test]
        public void The_Three_Label_Tiers_Follow_The_Hashtag_135_Zoom_Thresholds()
        {
            Assert.AreEqual(LabelTier.Default, GardenFraming.TierOf(Spec.DefaultDistance, Spec));
            Assert.AreEqual(LabelTier.Near, GardenFraming.TierOf(Spec.NearDistance, Spec));
            Assert.AreEqual(LabelTier.Far, GardenFraming.TierOf(Spec.FarDistance, Spec));
            Assert.AreEqual(LabelTier.Near, GardenFraming.TierOf(GardenFraming.DistanceOf(CameraSpec.DetailZoom + 0.01d, Spec), Spec), "full labels from DetailZoom up, as in the 2D garden");
            Assert.AreEqual(LabelTier.Default, GardenFraming.TierOf(GardenFraming.DistanceOf(CameraSpec.DetailZoom - 0.01d, Spec), Spec));
            Zoom(Spec.NearDistance);
            Assert.AreEqual(LabelTier.Near, _row.Tier, "the camera tells the stations when it crosses a threshold");
            Zoom(Spec.FarDistance);
            Assert.AreEqual(LabelTier.Far, _row.Tier);
            Zoom(Spec.DefaultDistance);
            Assert.AreEqual(LabelTier.Default, _row.Tier);
        }

        [Test]
        public void At_The_Default_Zoom_At_Most_Four_Words_Show_And_Only_Stations_Waiting_For_A_Rabbit_Ask_For_One()
        {
            Zoom(Spec.DefaultDistance);
            List<StandLabel> shown = Shown(out int texts);
            Assert.Greater(texts, 0, "open stations without a rabbit ask for one");
            Assert.LessOrEqual(texts, GardenStands.LabelsAtDefault, "hud.md §7: at most four words in the world at the default zoom");
            foreach (StandLabel label in shown)
            {
                Assert.IsFalse(label.LevelShown, "Cấp N waits for the near zoom");
                Assert.IsTrue(label.PadShown);
                StringAssert.EndsWith("+", label.PadText.text);
            }
            Assert.IsFalse(_row.Label(0).PadShown, "the staffed station asks for nothing");
        }

        [Test]
        public void Zoomed_Near_Every_Open_Station_Shows_Its_Level_And_Zoomed_Far_None_Show()
        {
            Zoom(Spec.NearDistance);
            List<StandLabel> shown = Shown(out _);
            int levels = 0;
            foreach (StandLabel label in shown)
                if (label.LevelShown)
                {
                    levels++;
                    StringAssert.Contains(L10n.T("station.level", 1).Split(' ')[0], label.LevelText.text);
                }
            Assert.Greater(levels, 2, "Cấp N on the stations at the near zoom");
            Assert.IsTrue(_row.Label(0).LevelShown || !_row.Stand(0).gameObject.activeSelf);
            Zoom(Spec.FarDistance);
            Assert.AreEqual(0, Shown(out int far).Count);
            Assert.AreEqual(0, far, "zoomed out, the stations carry no labels");
        }

        [TestCase(true)]
        [TestCase(false)]
        public void No_Two_Labels_Overlap_On_Screen(bool near)
        {
            Zoom(near ? Spec.NearDistance : Spec.DefaultDistance);
            var rects = new List<Rect>();
            foreach (StandLabel label in Shown(out _))
                foreach (RectTransform plate in Plates(label)) rects.Add(ScreenRect(plate));
            Assert.Greater(rects.Count, 0);
            for (int a = 0; a < rects.Count; a++)
                for (int b = a + 1; b < rects.Count; b++)
                    Assert.IsFalse(rects[a].Overlaps(rects[b]), "label " + a + " " + rects[a] + " overlaps label " + b + " " + rects[b]);
        }

        [Test]
        public void At_The_Default_Zoom_Label_Text_Is_At_Least_22_Canvas_Units_Tall_On_Screen()
        {
            Zoom(Spec.DefaultDistance);
            float units = (float)GardenFraming.CanvasHeight / _world.Rig.View.pixelHeight;
            int checkedLabels = 0;
            foreach (StandLabel label in Shown(out _))
                foreach (RectTransform plate in Plates(label))
                {
                    Rect onScreen = ScreenRect(plate);
                    float text = StandLabel.TextUnits * onScreen.height * units / StandLabel.PlateHeight;
                    Assert.GreaterOrEqual(text, 22f, plate.name + " text reads " + text.ToString("0.0") + " canvas units");
                    checkedLabels++;
                }
            Assert.Greater(checkedLabels, 0);
        }

        [Test]
        public void A_Label_Faces_The_Camera_And_Sits_Above_Its_Station_Taking_No_Taps()
        {
            for (int i = 0; i < _row.Count; i++)
            {
                StandLabel label = _row.Label(i);
                Assert.AreEqual(RenderMode.WorldSpace, label.Canvas.renderMode);
                Assert.Less(Vector3.Angle(label.transform.forward, _world.Rig.View.transform.forward), 0.5f, "the label plane faces the camera, so it is not squashed by the tilt");
                if (!_row.Stand(i).gameObject.activeSelf || _row.Stand(i).Height <= 0d) continue;
                Assert.GreaterOrEqual(label.transform.position.y, (float)_row.Stand(i).Height, "above the art, not over it");
                Assert.IsFalse(label.PadText.raycastTarget);
                Assert.IsNull(label.GetComponent<UnityEngine.UI.GraphicRaycaster>(), "a tap goes through to the station");
            }
        }

        [Test]
        public void A_Refresh_With_Nothing_Changed_Allocates_Nothing_With_Labels_Up()
        {
            Zoom(Spec.NearDistance);
            _row.Refresh(_defs, _state);
            Assert.That(() => _row.Refresh(_defs, _state), UnityEngine.TestTools.Constraints.ConstraintExtensions.AllocatingGCMemory(UnityEngine.TestTools.Constraints.Is.Not));
        }
    }
}
