using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;
using VuonTra.Core;
using VuonTra.Game.UI;

namespace VuonTra.Game
{
    public sealed class Garden25DWorld : MonoBehaviour
    {
        public const int RendererIndex = 1;
        public const string SettingsPath = "Garden25D/GardenCameraSettings";
        public const string LawnPath = "Garden25D/IslandLawn";
        public const string EarthPath = "Garden25D/IslandEarth";

        readonly Tweens _tweens = new Tweens();
        GardenCameraSettings _settings;
        Material _lawn, _earth;

        public GardenCameraRig Rig { get; private set; }
        public GardenGround Ground { get; private set; }
        public GardenPanZoom PanZoom { get; private set; }
        public GardenMapCamera Map { get; private set; }
        public int Segments { get; private set; }
        public GroundRect Island => GardenFraming.Island(Segments, Rig.Spec);
        public int PendingMotion => _tweens.Count;

        public static Garden25DWorld Load(int segments) =>
            Create(segments, Resources.Load<GardenCameraSettings>(SettingsPath), Resources.Load<Material>(LawnPath), Resources.Load<Material>(EarthPath));

        public static Garden25DWorld Create(int segments, GardenCameraSettings settings, Material lawn, Material earth)
        {
            var world = new GameObject("Garden 2.5D").AddComponent<Garden25DWorld>();
            world._settings = settings;
            world._lawn = lawn;
            world._earth = earth;
            world.Segments = segments;
            CameraSpec spec = settings.Spec();

            var eye = new GameObject("Garden Camera", typeof(Camera), typeof(PhysicsRaycaster));
            eye.transform.SetParent(world.transform, false);
            Camera view = eye.GetComponent<Camera>();
            view.clearFlags = CameraClearFlags.SolidColor;
            view.backgroundColor = Palette.Night;
            view.nearClipPlane = 1f;
            view.farClipPlane = 200f;
            view.GetUniversalAdditionalCameraData().SetRenderer(RendererIndex);
            world.Rig = eye.AddComponent<GardenCameraRig>();
            world.Rig.Attach(view, spec);

            var land = new GameObject("Island", typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider));
            land.transform.SetParent(world.transform, false);
            world.Ground = land.AddComponent<GardenGround>();
            world.PanZoom = land.AddComponent<GardenPanZoom>();
            world.PanZoom.Attach(world.Rig, world._tweens, world.Island);
            world.BuildGround();

            var mapEye = new GameObject("Map Camera", typeof(Camera));
            mapEye.transform.SetParent(world.transform, false);
            world.Map = mapEye.AddComponent<GardenMapCamera>();
            world.Map.Attach(mapEye.GetComponent<Camera>());

            world.Rig.Frame(new GroundPoint(0d, 0d), spec.DefaultDistance);
            return world;
        }

        public void Grow(int segments)
        {
            if (segments == Segments) return;
            Segments = segments;
            BuildGround();
            PanZoom.SetIsland(Island);
        }

        public void Tick(float seconds) => _tweens.Update(seconds);

        void BuildGround() => Ground.Build(Segments, Rig.Spec, _settings.IslandThickness, _settings.GrassTileMetres, _lawn, _earth);
    }
}
