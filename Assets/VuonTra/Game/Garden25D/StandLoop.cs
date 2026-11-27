using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D.Animation;
using VuonTra.Core;

namespace VuonTra.Game
{
    public sealed class StandLoop : MonoBehaviour
    {
        public const string ArtCategory = "Prop", IdleLabel = "idle", DishName = "Dish", DishShadowName = "Dish Shadow";
        public const int LayerOrder = GardenStand.ArtOrder, DishOrder = GardenStand.ArtOrder, DishShadowOrder = GardenStand.ArtOrder - 1, BurstOrder = GardenStand.WorkerOrder + 2;
        static readonly int WorkId = Animator.StringToHash(RabbitActor.WorkParameter);

        readonly List<SpriteResolver> _resolvers = new List<SpriteResolver>();
        readonly List<ParticleSystem> _bursts = new List<ParticleSystem>();
        Animator _animator;
        SpriteRenderer _dish, _dishShadow;

        public Animator Animator => _animator;
        public IReadOnlyList<SpriteResolver> Resolvers => _resolvers;
        public SpriteRenderer Dish => _dish;
        public SpriteRenderer DishShadow => _dishShadow;
        public IReadOnlyList<ParticleSystem> Bursts => _bursts;
        public bool Playing { get; private set; }

        public static string PathOf(PropLayer layer) => layer.Category == ArtCategory ? "Art" : layer.Category;

        public static StandLoop Attach(GardenStand stand, StationStand look, GardenStandSettings settings)
        {
            StandLoop loop = stand.gameObject.AddComponent<StandLoop>();
            loop.Build(stand, look, settings);
            return loop;
        }

        void Build(GardenStand stand, StationStand look, GardenStandSettings settings)
        {
            if (look.Library != null) stand.gameObject.AddComponent<SpriteLibrary>().spriteLibraryAsset = look.Library;
            foreach (PropLayer layer in look.Layers)
            {
                SpriteRenderer target = layer.Category == ArtCategory ? stand.Art : Layer(stand, layer, settings);
                if (look.Library == null) continue;
                SpriteResolver resolver = target.gameObject.AddComponent<SpriteResolver>();
                resolver.SetCategoryAndLabel(layer.Category, IdleLabel);
                _resolvers.Add(resolver);
            }
            if (look.Dish != null)
            {
                _dish = GardenStand.Renderer(DishName, stand.transform, DishOrder);
                float wide = GardenStand.Stand(_dish, look.Dish, look.DishHeight * settings.RabbitMetres);
                _dish.transform.localPosition += look.DishSpot;
                _dish.enabled = false;
                _dishShadow = GardenStand.Renderer(DishShadowName, stand.transform, DishShadowOrder);
                GardenStand.LayShadow(_dishShadow, settings, wide, new Vector2(look.DishSpot.x, look.DishSpot.z), true);
                _dishShadow.transform.localPosition += Vector3.up * look.DishSpot.y;
                _dishShadow.enabled = false;
            }
            _animator = stand.gameObject.AddComponent<Animator>();
            _animator.runtimeAnimatorController = look.Loop;
            foreach (ParticleBeat beat in look.Particles) _bursts.Add(Burst(stand.transform, beat, settings));
        }

        static SpriteRenderer Layer(GardenStand stand, PropLayer layer, GardenStandSettings settings)
        {
            SpriteRenderer renderer = GardenStand.Renderer(layer.Category, stand.transform, LayerOrder);
            Sprite idle = layer.States.Length > 0 ? layer.States[0].Art : null;
            GardenStand.Stand(renderer, idle, layer.Height * settings.RabbitMetres);
            renderer.transform.localPosition += layer.Spot;
            return renderer;
        }

        static ParticleSystem Burst(Transform parent, ParticleBeat beat, GardenStandSettings settings)
        {
            var go = new GameObject("Burst " + (beat.Art != null ? beat.Art.name : "none"), typeof(ParticleSystem));
            go.transform.SetParent(parent, false);
            go.transform.localPosition = beat.From;
            go.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            var system = go.GetComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = system.main;
            main.playOnAwake = false;
            main.loop = true;
            main.duration = settings.LoopSeconds;
            main.startLifetime = beat.Life;
            main.startSpeed = beat.Speed;
            main.startSize = beat.Size;
            main.gravityModifier = beat.Gravity;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.maxParticles = Mathf.Max(8, beat.Count * 2);
            ParticleSystem.EmissionModule emission = system.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(beat.At, (short)beat.Count) });
            ParticleSystem.ShapeModule shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 25f;
            shape.radius = 0.05f;
            ParticleSystem.ColorOverLifetimeModule fade = system.colorOverLifetime;
            fade.enabled = true;
            var ramp = new Gradient();
            ramp.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0f, 1f) });
            fade.color = ramp;
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sharedMaterial = settings.ParticleLookFor(beat.Art);
            renderer.sortingOrder = BurstOrder;
            return system;
        }

        public void Play(RabbitWork work)
        {
            bool working = work != RabbitWork.Idle;
            if (_animator.runtimeAnimatorController != null) _animator.SetInteger(WorkId, (int)work);
            Playing = working;
            foreach (ParticleSystem burst in _bursts)
            {
                if (working && !burst.isPlaying) burst.Play(true);
                else if (!working) burst.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
            if (working || _dish == null) return;
            _dish.enabled = false;
            _dishShadow.enabled = false;
        }
    }
}
