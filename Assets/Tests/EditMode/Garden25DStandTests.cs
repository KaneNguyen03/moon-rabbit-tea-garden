using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using VuonTra.Game;
using VuonTra.Game.UI;
using Motion = VuonTra.Game.UI.Motion;

namespace VuonTra.Core.Tests
{
    public class Garden25DStandTests
    {
        const int Teapot = 0, Leaves = 1, MilkTea = 6;
        const string RiggedBreed = "tho-me", OtherBreed = "tho-ca-phe";

        GameCatalog _catalog;
        GardenStandSettings _settings;
        GardenCameraSettings _camera;
        List<StationDef> _defs;
        GameState _state;
        Garden25DWorld _world;
        GardenStandRow _row;
        RenderTexture _screen;
        bool _reduced;

        [SetUp]
        public void SetUp()
        {
            _reduced = Motion.Reduced;
            Motion.Reduced = false;
            _catalog = AssetDatabase.LoadAssetAtPath<GameCatalog>("Assets/VuonTra/Content/GameCatalog.asset");
            _settings = Object.Instantiate(Resources.Load<GardenStandSettings>(Garden25DWorld.StandsPath));
            var serialized = new SerializedObject(_settings);
            serialized.FindProperty("rigs").GetArrayElementAtIndex(0).FindPropertyRelative("Kind").stringValue = RiggedBreed;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            _defs = _catalog.BuildDefs();
            _state = GameState.NewGame(_defs);
            _state.Plots = _defs.Count;
            for (int i = 0; i < _defs.Count; i++) _state.Levels[i] = 1;
            GardenGrid.Settle(_defs, _state);
            for (int i = 0; i < _defs.Count; i++) _state.Rabbits.Add(new Rabbit { Id = 100 + i, Station = i, Name = "R" + i, Kind = RiggedBreed });
            _camera = ScriptableObject.CreateInstance<GardenCameraSettings>();
            _world = Garden25DWorld.Create(_state.Grid.Segments, _camera, null, null);
            _screen = new RenderTexture(1080, 1920, 24);
            _world.Rig.View.targetTexture = _screen;
            double clock = 0d;
            _world.PanZoom.Clock = () => clock += 1d;
            _row = _world.Raise(_catalog, _settings);
            _row.Refresh(_defs, _state);
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
            Object.DestroyImmediate(_camera);
            Object.DestroyImmediate(_settings);
        }

        CameraSpec Spec => _world.Rig.Spec;

        [Test]
        public void The_Stand_Settings_Load_With_The_Cream_Rig_A_Shadow_And_The_Three_Sample_Stations()
        {
            Assert.IsNotNull(_settings);
            Assert.AreEqual(1, _settings.Rigs.Length, "one rig so far: Cream");
            Assert.IsNotNull(_settings.Rigs[0].Rig);
            Assert.IsNotNull(_settings.ShadowSprite);
            Assert.AreEqual(RabbitWork.TeaBrewing, _settings.Station("am-tra").Work);
            Assert.AreEqual(RabbitWork.TeaPicking, _settings.Station("gio-la").Work);
            Assert.AreEqual(RabbitWork.MilkTea, _settings.Station("tra-sua").Work);
            Assert.AreEqual(StandRole.Counter, _settings.Station("tra-sua").Role);
            Assert.AreEqual(RabbitWork.Idle, _settings.Station("ho-sen").Work);
        }

        [Test]
        public void Every_Station_Stands_On_Its_Grid_Tile_With_Its_Feet_On_The_Ground()
        {
            Assert.AreEqual(_defs.Count, _row.Count);
            for (int i = 0; i < _defs.Count; i++)
            {
                GardenStand stand = _row.Stand(i);
                Assert.IsTrue(stand.gameObject.activeSelf, _defs[i].Id);
                Sprite own = _settings.Station(_defs[i].Id).Art;
                Assert.AreSame(own != null ? own : _catalog.Stations[i].Prop, stand.Art.sprite, _defs[i].Id);
                Assert.IsTrue(_state.Grid.TryTileOf(_defs[i].GridId, out Hex tile));
                GroundPoint foot = GardenFraming.Ground(tile, Spec);
                Assert.AreEqual(foot.X, stand.Foot.x, 1e-4, _defs[i].Id);
                Assert.AreEqual(0f, stand.Foot.y, 1e-4, _defs[i].Id);
                Assert.AreEqual(foot.Z, stand.Foot.z, 1e-4, _defs[i].Id);
                if (GardenStand.FootOf(stand.Art.sprite) == 0f) Assert.AreEqual(0f, stand.Art.transform.position.y, 1e-4, _defs[i].Id + " stands on its pivot, the lowest half-opaque row of its picture");
                else Assert.That(stand.Art.bounds.min.y, Is.InRange(-0.03f, 0.01f), _defs[i].Id + " feet touch the ground");
                Assert.AreEqual(foot.X, stand.Art.bounds.center.x, 0.01f, _defs[i].Id + " stands centred over its tile");
                Assert.AreEqual(GardenStand.FlatLift, stand.Shadow.transform.position.y, 1e-4);
                Assert.Less(Vector3.Angle(Vector3.down, stand.Shadow.transform.forward), 0.01f, _defs[i].Id + " shadow lies flat, its face up to the camera");
            }
        }

        [Test]
        public void Every_Station_And_Rabbit_Is_As_Tall_As_The_Scale_Table_Says_Within_Fifteen_Percent()
        {
            double rabbit = _settings.RabbitMetres;
            for (int i = 0; i < _defs.Count; i++)
            {
                StationStand look = _settings.Station(_defs[i].Id);
                double ratio = (_row.Stand(i).Art.bounds.max.y - _row.Stand(i).Foot.y) / rabbit;
                Assert.IsTrue(GardenStands.Range(look.Role).Holds(ratio), _defs[i].Id + " is " + ratio.ToString("0.00") + " rabbits tall as a " + look.Role);
                Bounds worker = _row.Rig(i) != null ? _row.Rig(i).Extent() : _row.StillRabbit(i).bounds;
                double rabbitRatio = (worker.max.y - _row.Stand(i).Foot.y) / rabbit;
                Assert.IsTrue(GardenStands.Range(StandRole.Rabbit).Holds(rabbitRatio), _defs[i].Id + " rabbit is " + rabbitRatio.ToString("0.00"));
            }
            Assert.IsTrue(GardenStands.Range(StandRole.Counter).Holds(_row.Stand(MilkTea).Art.bounds.size.y / rabbit), "the milk tea counter is a counter, twice a rabbit");
        }

        [Test]
        public void The_Three_Sample_Stations_Use_The_Two_And_A_Half_D_Copies_Whose_Pivot_Is_The_Real_Foot()
        {
            foreach (int station in new[] { Teapot, Leaves, MilkTea })
            {
                Sprite art = _row.Stand(station).Art.sprite;
                StringAssert.StartsWith(VuonTra.EditorTools.BillboardArt.StandFolder, AssetDatabase.GetAssetPath(art), _defs[station].Id);
                Assert.AreEqual(0f, GardenStand.FootOf(art), _defs[station].Id + " stands on its pivot");
                Assert.AreEqual(0f, _row.Stand(station).Art.transform.localPosition.y, 1e-5, _defs[station].Id);
            }
        }

        [Test]
        public void Every_Contact_Shadow_Is_Nine_Tenths_Of_Its_Base_And_Every_Station_Sits_On_A_Flat_Pad_Under_It()
        {
            for (int i = 0; i < _defs.Count; i++)
            {
                GardenStand stand = _row.Stand(i);
                float art = stand.Art.bounds.size.x;
                Assert.AreEqual(Mathf.Max(_settings.ShadowMinWidth, art * _settings.ShadowShare), stand.Shadow.bounds.size.x, 0.02f, _defs[i].Id);
                Assert.AreSame(_settings.ShadowSprite, stand.Shadow.sprite);
                Assert.IsTrue(stand.Pad.enabled, _defs[i].Id + " has its base");
                Assert.AreSame(_settings.PadFor(i), stand.Pad.sprite, _defs[i].Id);
                Assert.Less(stand.Pad.sortingOrder, stand.Shadow.sortingOrder);
                Assert.Less(stand.Shadow.sortingOrder, stand.Art.sortingOrder);
                Assert.Less(Vector3.Angle(Vector3.down, stand.Pad.transform.forward), 0.01f, _defs[i].Id + " base lies flat");
                Assert.AreEqual(GardenStand.PadLift, stand.Pad.transform.position.y, 1e-4);
                Assert.GreaterOrEqual(stand.Pad.bounds.size.x, art, _defs[i].Id + " base is wider than what stands on it");
                Assert.AreEqual(_settings.PadTint, stand.Pad.color, "the 2D floor tint, one step toward Night");
            }
            GameState fresh = GameState.NewGame(_defs);
            fresh.Plots = 2;
            _row.Refresh(_defs, fresh);
            Assert.IsFalse(_row.Stand(2).Pad.enabled, "land waiting for its plot shows the sign and no base");
        }

        [Test]
        public void Open_Stations_Are_Dressed_With_Small_Pieces_That_Stand_On_The_Ground_And_Take_No_Taps()
        {
            int standing = 0;
            for (int i = 0; i < _defs.Count; i++)
            {
                IReadOnlyList<GardenStand> dressing = _row.Dressing(i);
                Assert.AreEqual(_settings.Dressing.Length, dressing.Count);
                for (int k = 0; k < dressing.Count; k++)
                {
                    GardenStand piece = dressing[k];
                    float side = i % 2 == 0 ? 1f : -1f;
                    var wanted = new Vector3(_row.Stand(i).Foot.x + _settings.Dressing[k].Offset.x * side, 0f, _row.Stand(i).Foot.z + _settings.Dressing[k].Offset.y);
                    bool crowded = false;
                    for (int j = 0; j < _defs.Count; j++)
                        if (j != i && Vector3.Distance(wanted, _row.Stand(j).Foot) < _settings.KeepClear) crowded = true;
                    Assert.AreEqual(!crowded, piece.gameObject.activeSelf, _defs[i].Id + " piece " + k + " stands unless it would land on another station's base");
                    if (!piece.gameObject.activeSelf) continue;
                    standing++;
                    Assert.IsFalse(piece.Touchable);
                    Assert.IsNull(piece.GetComponent<Collider>());
                    Assert.AreEqual(0f, piece.Foot.y, 1e-5);
                    Assert.AreEqual(_row.Stand(i).Foot.x + _settings.Dressing[k].Offset.x * side, piece.Foot.x, 1e-4);
                    Assert.AreEqual(_row.Stand(i).Foot.z + _settings.Dressing[k].Offset.y, piece.Foot.z, 1e-4);
                    double ratio = piece.Art.bounds.size.y / _settings.RabbitMetres;
                    Assert.IsTrue(GardenStands.Range(_settings.Dressing[k].Role).Holds(ratio), _defs[i].Id + " piece " + k + " is " + ratio.ToString("0.00"));
                    Assert.IsTrue(piece.Shadow.enabled);
                }
            }
            Assert.Greater(standing, _defs.Count, "the garden is dressed, more than one piece a station on average");
            GameState fresh = GameState.NewGame(_defs);
            fresh.Plots = 2;
            _row.Refresh(_defs, fresh);
            bool teapotDressed = false;
            foreach (GardenStand piece in _row.Dressing(0)) teapotDressed |= piece.gameObject.activeSelf;
            Assert.IsTrue(teapotDressed, "the open teapot keeps its dressing");
            foreach (GardenStand piece in _row.Dressing(1)) Assert.IsFalse(piece.gameObject.activeSelf, "a station not built yet has none");
            foreach (GardenStand piece in _row.Dressing(5)) Assert.IsFalse(piece.gameObject.activeSelf, "nor one that is not shown");
        }

        [Test]
        public void Stepping_Stones_And_A_Lamp_Post_Mark_Every_Path_Between_Two_Open_Stations()
        {
            var expected = new List<GroundPoint>();
            int paths = 0;
            foreach (StationPair pair in GardenStands.Paths)
            {
                if (pair.From >= _defs.Count || pair.To >= _defs.Count) continue;
                paths++;
                Vector3 a = _row.Stand(pair.From).Foot, b = _row.Stand(pair.To).Foot;
                GardenStands.Stones(new GroundPoint(a.x, a.z), new GroundPoint(b.x, b.z), _settings.StoneStep, _settings.StoneClear, expected);
                GroundPoint post = GardenStands.Aside(new GroundPoint(a.x, a.z), new GroundPoint(b.x, b.z), _settings.LampPostAside);
                if (!Clear(post)) paths--;
            }
            expected.RemoveAll(spot => !Clear(spot));
            int shown = 0;
            foreach (SpriteRenderer stone in _row.Stones)
            {
                if (!stone.gameObject.activeSelf) continue;
                Assert.AreEqual(expected[shown].X, stone.transform.position.x, 1e-4);
                Assert.AreEqual(expected[shown].Z, stone.transform.position.z, 1e-4);
                Assert.Less(Vector3.Angle(Vector3.down, stone.transform.forward), 0.01f, "stones lie flat");
                Assert.Less(stone.sortingOrder, 0, "under everything that stands");
                shown++;
            }
            Assert.AreEqual(expected.Count, shown);
            Assert.Greater(shown, 0);
            int lamps = 0;
            foreach (GardenStand lamp in _row.Lamps)
            {
                if (!lamp.gameObject.activeSelf) continue;
                lamps++;
                Assert.IsTrue(GardenStands.Range(StandRole.LampPost).Holds(lamp.Art.bounds.size.y / _settings.RabbitMetres), "a lamp post is 1.4 rabbits");
                Assert.IsFalse(lamp.Touchable);
            }
            Assert.AreEqual(paths, lamps, "a lamp for every path whose midpoint is clear of the bases");
            foreach (GardenStand lamp in _row.Lamps)
                if (lamp.gameObject.activeSelf) Assert.IsTrue(Clear(new GroundPoint(lamp.Foot.x, lamp.Foot.z)), "no lamp post on a base");
            GameState fresh = GameState.NewGame(_defs);
            fresh.Plots = 2;
            _row.Refresh(_defs, fresh);
            foreach (SpriteRenderer stone in _row.Stones) Assert.IsFalse(stone.gameObject.activeSelf, "no path runs to a station not built");
            foreach (GardenStand lamp in _row.Lamps) Assert.IsFalse(lamp.gameObject.activeSelf);
        }

        [Test]
        public void At_The_Default_Zoom_A_Working_Rabbit_Stands_About_A_Hundred_And_Ten_Pixels_Tall()
        {
            _world.Rig.Frame(new GroundPoint(_row.Stand(Teapot).Foot.x, _row.Stand(Teapot).Foot.z), Spec.DefaultDistance);
            Bounds body = _row.Rig(Teapot).Extent();
            Vector3 foot = _row.Rig(Teapot).transform.position;
            float top = _world.Rig.View.WorldToScreenPoint(new Vector3(foot.x, body.max.y, foot.z)).y;
            float bottom = _world.Rig.View.WorldToScreenPoint(foot).y;
            Assert.That(top - bottom, Is.InRange(110f * 0.85f, 110f * 1.15f));
        }

        [Test]
        public void Billboards_Turn_Only_About_The_Vertical_Axis_To_Face_The_Camera()
        {
            float yaw = _world.Rig.View.transform.eulerAngles.y;
            for (int i = 0; i < _defs.Count; i++)
            {
                Transform stand = _row.Stand(i).transform;
                Assert.AreEqual(Vector3.up, stand.up, _defs[i].Id + " stands upright, not leaning back with the camera");
                Assert.AreEqual(yaw, stand.eulerAngles.y, 1e-3, _defs[i].Id);
                Vector3 facing = _world.Rig.View.transform.forward;
                facing.y = 0f;
                Assert.AreEqual(0f, stand.forward.y, 1e-5);
                Assert.AreEqual(1f, Vector3.Dot(stand.forward, facing.normalized), 1e-5, _defs[i].Id + " faces the way the camera looks");
            }
            Assert.AreEqual(TransparencySortMode.CustomAxis, _world.Rig.View.transparencySortMode);
            Assert.AreEqual(Garden25DWorld.DepthAxis, _world.Rig.View.transparencySortAxis);
        }

        [Test]
        public void The_Three_Sample_Stations_Set_Their_Four_Beat_Loop_On_The_Cream_Rig_And_The_Rest_Show_Their_Rabbit()
        {
            Assert.AreEqual(RabbitWork.TeaBrewing, _row.Rig(Teapot).Work);
            Assert.AreEqual(RabbitWork.TeaPicking, _row.Rig(Leaves).Work);
            Assert.AreEqual(RabbitWork.MilkTea, _row.Rig(MilkTea).Work);
            Assert.IsFalse(_row.Rig(Teapot).Shadow.GetComponent<SpriteRenderer>().enabled, "the rig's small animated shadow gives way to the stand's contact shadow");
            for (int i = 0; i < _defs.Count; i++)
            {
                SpriteRenderer shadow = _row.RabbitShadow(i);
                Assert.IsTrue(shadow.gameObject.activeInHierarchy, _defs[i].Id);
                Assert.Less(Vector3.Angle(Vector3.down, shadow.transform.forward), 0.01f, _defs[i].Id + " rabbit shadow lies on the ground");
                Assert.AreEqual(GardenStand.FlatLift, shadow.transform.position.y, 1e-4, "just above the lawn, so it does not flicker in it");
                Assert.AreSame(_settings.ShadowSprite, shadow.sprite, "the soft Ink ellipse of #147, never black");
            }
            Assert.AreEqual(RabbitActor.WorkParameter, _settings.Rigs[0].Rig.Animator.runtimeAnimatorController is UnityEditor.Animations.AnimatorController controller ? controller.parameters[0].name : null);
            for (int i = 0; i < _defs.Count; i++)
            {
                if (i == Teapot || i == Leaves || i == MilkTea) continue;
                Assert.IsNull(_row.Rig(i), _defs[i].Id);
                Assert.AreSame(_catalog.RabbitArt(_state.Rabbits[i]), _row.StillRabbit(i).sprite, _defs[i].Id);
            }
        }

        [Test]
        public void A_Sample_Station_Worked_By_A_Breed_Without_A_Rig_Shows_That_Breed_And_Never_The_Rigged_One()
        {
            var coffee = new Rabbit { Id = 900, Station = Teapot, Name = "Coffee", Kind = OtherBreed };
            Assert.AreEqual(OtherBreed, Rabbits.KindOf(coffee));
            _state.Rabbits[Teapot].Station = -1;
            _state.Rabbits.Add(coffee);
            _row.Refresh(_defs, _state);
            Assert.IsNull(_row.Rig(Teapot), "the rig is drawn for its own breed only");
            Assert.IsFalse(_row.RigsAt(Teapot)[0].gameObject.activeSelf);
            Assert.IsTrue(_row.StillRabbit(Teapot).enabled);
            Assert.AreSame(_catalog.RabbitArt(coffee), _row.StillRabbit(Teapot).sprite, "the player's own rabbit, standing still: no work loop until its breed has a rig");
            coffee.Station = -1;
            _state.Rabbits[Teapot].Station = Teapot;
            _row.Refresh(_defs, _state);
            Assert.IsTrue(_row.Rig(Teapot).gameObject.activeSelf);
            Assert.AreEqual(1, _row.RigsAt(Teapot).Count, "the same rig comes back, not a second copy");
            Assert.IsFalse(_row.StillRabbit(Teapot).enabled);
        }

        [Test]
        public void The_Shipped_Breed_To_Rig_Table_Gives_The_Cream_Rig_To_Tho_Bong_Only_And_Never_A_Lop_Or_The_Moon_Rabbit()
        {
            var shipped = Resources.Load<GardenStandSettings>(Garden25DWorld.StandsPath);
            foreach (BreedRig rig in shipped.Rigs)
            {
                Assert.IsNotNull(rig.Rig);
                if (string.IsNullOrEmpty(rig.Kind)) continue;
                Assert.IsNotNull(RabbitKinds.Find(rig.Kind), "a mapped breed is a real breed");
                Assert.AreNotEqual("tho-trang", rig.Kind);
                Assert.AreNotEqual("tho-cup-kem", rig.Kind, "the Cream rig has upright ears");
            }
            foreach (RabbitKind kind in RabbitKinds.All)
                if (shipped.RigFor(kind.Id) != null) Assert.AreEqual(1, System.Array.FindAll(shipped.Rigs, r => r.Kind == kind.Id).Length, kind.Id);
            Assert.IsNull(shipped.RigFor(null));
            Assert.IsNull(shipped.RigFor(""), "an empty kind maps to no rig");
            Assert.IsNotNull(shipped.RigFor("tho-bong"), "Thỏ Bông: PO's default, design's visual OK, owner not yet answered");
            Assert.IsNull(shipped.RigFor("tho-me"), "every other breed keeps its own sprite");
        }

        [Test]
        public void A_Station_Without_A_Rabbit_Shows_None_And_A_New_Rabbit_Appears_When_One_Is_Assigned()
        {
            _state.Rabbits[Teapot].Station = -1;
            _state.Rabbits[2].Station = -1;
            _row.Refresh(_defs, _state);
            Assert.IsNull(_row.Rig(Teapot));
            Assert.IsFalse(_row.RigsAt(Teapot)[0].gameObject.activeSelf);
            Assert.IsFalse(_row.StillRabbit(2).gameObject.activeInHierarchy);
            _state.Rabbits[2].Station = Teapot;
            _row.Refresh(_defs, _state);
            Assert.IsTrue(_row.Rig(Teapot).gameObject.activeInHierarchy);
        }

        [Test]
        public void Stations_Not_Shown_Or_Stored_Stand_Down_And_Land_Waiting_For_A_Plot_Shows_The_Sign()
        {
            GameState fresh = GameState.NewGame(_defs);
            fresh.Plots = 2;
            _row.Refresh(_defs, fresh);
            Assert.IsTrue(_row.Stand(0).gameObject.activeSelf);
            Assert.IsTrue(_row.Stand(1).gameObject.activeSelf);
            Assert.IsTrue(_row.Stand(2).gameObject.activeSelf);
            Assert.IsFalse(_row.Stand(3).gameObject.activeSelf);
            Assert.AreSame(_settings.Station(_defs[1].Id).Art, _row.Stand(1).Art.sprite, "the tea bush shows its 2.5D copy");
            Assert.AreEqual(_settings.UnbuiltTint, _row.Stand(1).Art.color);
            Assert.AreSame(_catalog.PlotSign, _row.Stand(2).Art.sprite);
            Assert.IsTrue(_state.Grid.Store(_defs[5].GridId));
            _row.Refresh(_defs, _state);
            Assert.IsFalse(_row.Stand(5).gameObject.activeSelf);
            Assert.AreEqual(Color.white, _row.Stand(1).Art.color);
        }

        [Test]
        public void A_Station_Hidden_And_Shown_Again_Sets_Its_Rabbit_To_Work_Again()
        {
            Assert.IsTrue(_state.Grid.Store(_defs[Teapot].GridId));
            _row.Refresh(_defs, _state);
            Assert.IsFalse(_row.Stand(Teapot).gameObject.activeSelf);
            _row.Rig(Teapot).SetWork(RabbitWork.Idle);
            Assert.IsTrue(_state.Grid.TakeOut(_defs[Teapot].GridId));
            _row.Refresh(_defs, _state);
            Assert.IsTrue(_row.Stand(Teapot).gameObject.activeSelf);
            Assert.AreEqual(RabbitWork.TeaBrewing, _row.Rig(Teapot).Work, "an Animator that was switched off forgets its parameters");
        }

        [Test]
        public void Every_Contact_Shadow_Is_Centred_Under_The_Feet_So_Nothing_Hovers()
        {
            for (int i = 0; i < _defs.Count; i++)
            {
                Vector3 foot = _row.Stand(i).Foot + new Vector3(_settings.ShadowFall.x, 0f, _settings.ShadowFall.y), shadow = _row.Stand(i).Shadow.bounds.center;
                Assert.Less(Mathf.Abs(shadow.z - foot.z), 0.05f, _defs[i].Id + " station shadow");
                Assert.Less(_row.Stand(i).Shadow.bounds.min.z, foot.z, _defs[i].Id + " shadow reaches in front of the feet");
                Vector3 rabbitFoot = _row.StillRabbit(i).transform.parent.position + new Vector3(_settings.ShadowFall.x, 0f, _settings.ShadowFall.y);
                Assert.Less(Mathf.Abs(_row.RabbitShadow(i).bounds.center.z - rabbitFoot.z), 0.05f, _defs[i].Id + " rabbit shadow");
            }
        }

        [Test]
        public void A_Refresh_With_Nothing_Changed_Allocates_Nothing()
        {
            _row.Refresh(_defs, _state);
            Assert.That(() => new byte[64], UnityEngine.TestTools.Constraints.Is.AllocatingGCMemory(), "the check can see an allocation");
            Assert.That(() => _row.Refresh(_defs, _state), UnityEngine.TestTools.Constraints.Is.Not.AllocatingGCMemory());
        }

        [Test]
        public void Tapping_A_Station_Through_The_Physics_Raycaster_Names_That_Station_And_Not_The_Ground_Under_It()
        {
            var heard = new List<int>();
            int tiles = 0;
            _row.StationTapped += heard.Add;
            _world.Ground.TileTapped += h => tiles++;
            foreach (int station in new[] { Teapot, Leaves, MilkTea, 2 })
            {
                _world.Rig.Frame(new GroundPoint(_row.Stand(station).Foot.x, _row.Stand(station).Foot.z), Spec.DefaultDistance);
                Physics.SyncTransforms();
                Vector2 at = _world.Rig.View.WorldToScreenPoint(_row.Stand(station).Art.bounds.center);
                GameObject hit = Press(at, out PointerEventData press);
                Assert.AreSame(_row.Stand(station).gameObject, hit, _defs[station].Id);
                ExecuteEvents.Execute(hit, press, ExecuteEvents.pointerDownHandler);
                ExecuteEvents.Execute(hit, press, ExecuteEvents.pointerUpHandler);
                press.pointerPress = hit;
                ExecuteEvents.Execute(hit, press, ExecuteEvents.pointerClickHandler);
            }
            CollectionAssert.AreEqual(new[] { Teapot, Leaves, MilkTea, 2 }, heard);
            Assert.AreEqual(0, tiles);
        }

        [Test]
        public void A_Drag_That_Starts_On_A_Station_Moves_The_Garden_And_Taps_Nothing()
        {
            int heard = 0;
            _row.StationTapped += i => heard++;
            Physics.SyncTransforms();
            Vector2 at = _world.Rig.View.WorldToScreenPoint(_row.Stand(Teapot).Art.bounds.center);
            GameObject hit = Press(at, out PointerEventData drag);
            Assert.AreSame(_row.Stand(Teapot).gameObject, hit);
            GroundPoint before = _world.Rig.Focus;
            ExecuteEvents.Execute(hit, drag, ExecuteEvents.pointerDownHandler);
            drag.position = at + new Vector2(-200f, 0f);
            ExecuteEvents.Execute(hit, drag, ExecuteEvents.dragHandler);
            ExecuteEvents.Execute(hit, drag, ExecuteEvents.pointerUpHandler);
            drag.pointerPress = hit;
            ExecuteEvents.Execute(hit, drag, ExecuteEvents.pointerClickHandler);
            Assert.Greater(_world.Rig.Focus.X, before.X, "dragging left moves the view right");
            Assert.AreEqual(0, heard);
        }

        [Test]
        public void A_Tap_On_Open_Ground_Beside_A_Station_Still_Names_The_Tile()
        {
            Hex? tapped = null;
            _world.Ground.TileTapped += h => tapped = h;
            Hex empty = new Hex(0, 3);
            Assert.IsNull(_state.Grid.OccupantAt(empty.Q, empty.R));
            GroundPoint g = GardenFraming.Ground(empty, Spec);
            _world.Rig.Frame(g, Spec.DefaultDistance);
            Physics.SyncTransforms();
            Vector2 at = _world.Rig.View.WorldToScreenPoint(new Vector3((float)g.X, 0f, (float)g.Z));
            GameObject hit = Press(at, out PointerEventData press);
            Assert.AreSame(_world.Ground.gameObject, hit);
            ExecuteEvents.Execute(hit, press, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(hit, press, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(hit, press, ExecuteEvents.pointerClickHandler);
            Assert.AreEqual(empty, tapped);
        }

        bool Clear(GroundPoint at)
        {
            for (int j = 0; j < _defs.Count; j++)
            {
                Vector3 foot = _row.Stand(j).Foot;
                double dx = at.X - foot.x, dz = at.Z - foot.z;
                if (dx * dx + dz * dz < _settings.KeepClear * _settings.KeepClear) return false;
            }
            return true;
        }

        GameObject Press(Vector2 at, out PointerEventData press)
        {
            press = new PointerEventData(null) { position = at, pressPosition = at };
            var hits = new List<RaycastResult>();
            _world.Rig.View.GetComponent<PhysicsRaycaster>().Raycast(press, hits);
            Assert.IsNotEmpty(hits);
            hits.Sort((a, b) => a.distance.CompareTo(b.distance));
            press.pointerCurrentRaycast = hits[0];
            return ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject);
        }
    }
}
