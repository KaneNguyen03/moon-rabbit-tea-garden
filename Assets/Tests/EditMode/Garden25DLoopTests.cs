using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.U2D.Animation;
using VuonTra.Game;
using VuonTra.Game.UI;
using Motion = VuonTra.Game.UI.Motion;

namespace VuonTra.Core.Tests
{
    public class Garden25DLoopTests
    {
        const string RiggedBreed = "tho-bong";
        static readonly string[] Samples = { "am-tra", "gio-la", "tra-sua" };

        GameCatalog _catalog;
        GardenStandSettings _settings;
        List<StationDef> _defs;
        GameState _state;
        GardenCameraSettings _camera;
        Garden25DWorld _world;
        GardenStandRow _row;
        bool _reduced;

        [SetUp]
        public void SetUp()
        {
            _reduced = Motion.Reduced;
            Motion.Reduced = false;
            _catalog = AssetDatabase.LoadAssetAtPath<GameCatalog>("Assets/VuonTra/Content/GameCatalog.asset");
            _settings = Object.Instantiate(Resources.Load<GardenStandSettings>(Garden25DWorld.StandsPath));
            _defs = _catalog.BuildDefs();
            _state = GameState.NewGame(_defs);
            _state.Plots = _defs.Count;
            for (int i = 0; i < _defs.Count; i++) _state.Levels[i] = 1;
            GardenGrid.Settle(_defs, _state);
            for (int i = 0; i < _defs.Count; i++) _state.Rabbits.Add(new Rabbit { Id = 100 + i, Station = i, Name = "R" + i, Kind = RiggedBreed });
            _camera = ScriptableObject.CreateInstance<GardenCameraSettings>();
            _world = Garden25DWorld.Create(_state.Grid.Segments, _camera, null, null);
            _row = _world.Raise(_catalog, _settings);
            _row.Refresh(_defs, _state);
        }

        [TearDown]
        public void TearDown()
        {
            Motion.Reduced = _reduced;
            _world.Map.Release();
            Object.DestroyImmediate(_world.gameObject);
            Object.DestroyImmediate(_camera);
            Object.DestroyImmediate(_settings);
        }

        int IndexOf(string id) => _defs.FindIndex(d => d.Id == id);

        StandLoop LoopOf(string id) => _row.Loop(IndexOf(id));

        AnimationClip ClipOf(string id, string state)
        {
            foreach (AnimationClip clip in _settings.Station(id).Loop.animationClips)
                if (clip.name == id + "_" + state) return clip;
            return null;
        }

        static string LabelAt(StandLoop loop, AnimationClip clip, float time, string category)
        {
            clip.SampleAnimation(loop.gameObject, time);
            foreach (SpriteResolver resolver in loop.Resolvers)
                if (resolver.GetCategory() == category) return resolver.GetLabel();
            return null;
        }

        [Test]
        public void The_Three_Sample_Stations_Carry_A_Sprite_Library_A_Resolver_On_Their_Art_And_A_Loop_Controller()
        {
            foreach (string id in Samples)
            {
                StationStand look = _settings.Station(id);
                Assert.IsNotNull(look.Library, id + " has its SpriteLibraryAsset");
                Assert.IsNotNull(look.Loop, id + " has its Animator controller");
                StandLoop loop = LoopOf(id);
                Assert.IsNotNull(loop, id);
                Assert.AreSame(look.Library, loop.GetComponent<SpriteLibrary>().spriteLibraryAsset);
                Assert.AreSame(look.Loop, loop.Animator.runtimeAnimatorController);
                Assert.IsNotNull(_row.Stand(IndexOf(id)).Art.GetComponent<SpriteResolver>(), id + " swaps its own picture through a SpriteResolver, not from code");
                Assert.AreEqual(2, look.Loop.animationClips.Length, id + " has an idle clip and a work clip");
            }
        }

        [Test]
        public void The_Teapot_Opens_Its_Lid_Then_Steeps_Then_Closes_Again_Across_The_Four_Beats()
        {
            StationStand look = _settings.Station("am-tra");
            Assert.AreEqual("prop_am-tra", look.Library.GetSprite(StandLoop.ArtCategory, "idle").name);
            Assert.AreEqual("prop_am-tra_nap-mo", look.Library.GetSprite(StandLoop.ArtCategory, "b1").name);
            Assert.AreEqual("prop_am-tra_ngam", look.Library.GetSprite(StandLoop.ArtCategory, "b3").name);
            StandLoop loop = LoopOf("am-tra");
            AnimationClip work = ClipOf("am-tra", "Work");
            Assert.AreEqual(_settings.LoopSeconds, work.length, 1e-3, "one loop is the rig's 3.28 s, not squeezed to the station's cycle");
            Assert.IsTrue(work.isLooping);
            Assert.AreEqual("b1", LabelAt(loop, work, 0.1f, StandLoop.ArtCategory), "beat 1: lid open for the leaves");
            Assert.AreEqual("b2", LabelAt(loop, work, 1.0f, StandLoop.ArtCategory), "beat 2: still open for the water");
            Assert.AreEqual("b3", LabelAt(loop, work, 1.8f, StandLoop.ArtCategory), "beat 3: closed and steeping");
            Assert.AreEqual("idle", LabelAt(loop, work, 2.6f, StandLoop.ArtCategory), "beat 4: back on the stove");
            Assert.AreEqual("idle", LabelAt(loop, ClipOf("am-tra", "Idle"), 1.8f, StandLoop.ArtCategory), "without a rabbit the teapot rests");
        }

        [Test]
        public void The_Tea_Basket_Fills_Empty_Half_Full_While_The_Row_Is_Picked()
        {
            StandLoop loop = LoopOf("gio-la");
            AnimationClip work = ClipOf("gio-la", "Work");
            Assert.AreEqual(2, loop.Resolvers.Count, "the tea row and the basket beside it");
            Assert.AreEqual("b1", LabelAt(loop, work, 0.1f, "Basket"));
            Assert.AreEqual("item_gio-la_voi", _settings.Station("gio-la").Library.GetSprite("Basket", "b1").name);
            Assert.AreEqual("item_gio-la_nua", _settings.Station("gio-la").Library.GetSprite("Basket", "b2").name);
            Assert.AreEqual("item_gio-la_day", _settings.Station("gio-la").Library.GetSprite("Basket", "b3").name);
            Assert.AreEqual("b2", LabelAt(loop, work, 1.0f, "Basket"));
            Assert.AreEqual("b3", LabelAt(loop, work, 1.8f, "Basket"));
            Assert.AreEqual("b3", LabelAt(loop, work, 3.0f, "Basket"), "the full basket waits on the deck until the loop starts again");
            Assert.AreEqual("prop_gio-la_hai", _settings.Station("gio-la").Library.GetSprite(StandLoop.ArtCategory, "b3").name);
            Assert.AreEqual("b3", LabelAt(loop, work, 1.8f, StandLoop.ArtCategory), "the row shows picked");
            Assert.AreEqual("idle", LabelAt(loop, work, 2.6f, StandLoop.ArtCategory), "and grows its buds back");
        }

        [Test]
        public void The_Dish_Appears_On_Beat_Four_At_Its_Product_Spawn_In_Front_Of_The_Station()
        {
            foreach (string id in Samples)
            {
                StationStand look = _settings.Station(id);
                StandLoop loop = LoopOf(id);
                Assert.IsNotNull(look.Dish, id);
                Assert.IsNotNull(loop.Dish, id);
                Assert.AreSame(look.Dish, loop.Dish.sprite);
                Assert.IsFalse(loop.Dish.enabled, id + " starts with an empty tray");
                Assert.Less(look.DishSpot.z, 0f, id + " ProductSpawn is on the tray in front of the station");
                Vector3 spot = loop.Dish.transform.localPosition;
                Assert.AreEqual(look.DishSpot.x, spot.x, 1e-4, id);
                Assert.AreEqual(look.DishSpot.z, spot.z, 1e-4, id);
                AnimationClip work = ClipOf(id, "Work");
                work.SampleAnimation(loop.gameObject, look.DishFrom - 0.1f);
                Assert.IsFalse(loop.Dish.enabled, id + " no dish before beat four");
                Assert.IsFalse(loop.DishShadow.enabled, id + " nor its shadow");
                work.SampleAnimation(loop.gameObject, look.DishFrom + 0.1f);
                Assert.IsTrue(loop.Dish.enabled, id + " the dish sits at ProductSpawn from beat four");
                Assert.IsTrue(loop.DishShadow.enabled, id + " with its contact shadow");
                Assert.AreSame(_settings.ShadowSprite, loop.DishShadow.sprite);
                Bounds shadow = loop.DishShadow.bounds;
                Vector3 foot = loop.Dish.transform.position;
                Assert.AreEqual(foot.x, shadow.center.x, 1e-3, id + " shadow centred under the dish");
                Assert.AreEqual(foot.z - shadow.size.z * _settings.ShadowInFront, shadow.min.z, 1e-3, id + " a third of it in front of the dish, the rest under it");
                Assert.AreEqual(look.DishSpot.y, shadow.center.y, 0.02f, id + " on the surface the dish stands on");
                Assert.LessOrEqual(loop.DishShadow.color.a * 0.6f, 0.5f, id + " soft enough on a dark base");
                Assert.Less(loop.DishShadow.sortingOrder, loop.Dish.sortingOrder);
                ClipOf(id, "Idle").SampleAnimation(loop.gameObject, 1f);
                Assert.IsFalse(loop.Dish.enabled, id + " no dish without a rabbit");
                Assert.IsFalse(loop.DishShadow.enabled);
                Assert.AreEqual(look.DishHeight * _settings.RabbitMetres, loop.Dish.bounds.max.y - look.DishSpot.y, 0.02f, id + " the dish is small beside the rabbit");
            }
        }

        [Test]
        public void Each_Sample_Station_Bursts_Shuriken_Particles_On_Its_Beats_With_The_Painted_Particle_Art()
        {
            foreach (string id in Samples)
            {
                StationStand look = _settings.Station(id);
                StandLoop loop = LoopOf(id);
                Assert.Greater(look.Particles.Length, 0, id);
                Assert.AreEqual(look.Particles.Length, loop.Bursts.Count, id);
                for (int n = 0; n < loop.Bursts.Count; n++)
                {
                    ParticleSystem burst = loop.Bursts[n];
                    ParticleBeat beat = look.Particles[n];
                    Assert.IsFalse(burst.main.playOnAwake);
                    Assert.AreEqual(_settings.LoopSeconds, burst.main.duration, 1e-4, id + " bursts repeat with the loop");
                    Assert.AreEqual(1, burst.emission.burstCount);
                    Assert.AreEqual(beat.At, burst.emission.GetBurst(0).time, 1e-4);
                    Assert.AreEqual(0f, burst.emission.rateOverTime.constant, "only bursts, no stream");
                    Material art = burst.GetComponent<ParticleSystemRenderer>().sharedMaterial;
                    Assert.IsNotNull(art, id + " particle " + n + " has its material");
                    Assert.AreSame(beat.Art.texture, art.GetTexture("_BaseMap"), id + " draws the painted " + beat.Art.name + ", not a default square");
                    Assert.IsTrue(burst.transform.IsChildOf(loop.transform), "inside the station's sorting group");
                }
            }
        }

        [Test]
        public void A_Working_Rabbit_Starts_The_Loop_And_An_Empty_Station_Rests_With_No_Particles_Or_Dish()
        {
            foreach (string id in Samples)
            {
                StandLoop loop = LoopOf(id);
                Assert.IsTrue(loop.Playing, id + " is staffed in the set-up");
                Assert.AreEqual((int)_settings.Station(id).Work, loop.Animator.GetInteger(RabbitActor.WorkParameter), id);
                foreach (ParticleSystem burst in loop.Bursts) Assert.IsTrue(burst.isPlaying, id);
            }
            int teapot = IndexOf("am-tra");
            _state.Rabbits.RemoveAll(r => r.Station == teapot);
            _row.Refresh(_defs, _state);
            StandLoop rest = LoopOf("am-tra");
            Assert.IsFalse(rest.Playing);
            Assert.AreEqual(0, rest.Animator.GetInteger(RabbitActor.WorkParameter));
            Assert.IsFalse(rest.Dish.enabled);
            foreach (ParticleSystem burst in rest.Bursts) Assert.IsFalse(burst.isEmitting);
        }

        [Test]
        public void A_Station_Hidden_And_Shown_Again_Starts_Its_Loop_Again()
        {
            int teapot = IndexOf("am-tra");
            Assert.IsTrue(_state.Grid.Store(_defs[teapot].GridId));
            _row.Refresh(_defs, _state);
            Assert.IsTrue(_state.Grid.TakeOut(_defs[teapot].GridId));
            _row.Refresh(_defs, _state);
            StandLoop loop = LoopOf("am-tra");
            Assert.AreEqual((int)RabbitWork.TeaBrewing, loop.Animator.GetInteger(RabbitActor.WorkParameter), "an Animator that was switched off forgets its parameters");
            foreach (ParticleSystem burst in loop.Bursts) Assert.IsTrue(burst.isPlaying);
        }

        [Test]
        public void Stations_Without_A_Loop_Get_No_Loop_Parts()
        {
            for (int i = 0; i < _defs.Count; i++)
            {
                if (System.Array.IndexOf(Samples, _defs[i].Id) >= 0) continue;
                StationStand look = _settings.Station(_defs[i].Id);
                if (look.Loop != null) continue;
                Assert.IsNull(_row.Loop(i), _defs[i].Id);
                Assert.IsNull(_row.Stand(i).GetComponentInChildren<StandLoop>(true), _defs[i].Id);
            }
        }
    }
}
