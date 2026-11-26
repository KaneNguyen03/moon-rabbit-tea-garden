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
        public const string StandsPath = "Garden25D/GardenStands";
        public static readonly Vector3 DepthAxis = new Vector3(0f, 0f, 1f);

        readonly Tweens _tweens = new Tweens();
        GardenCameraSettings _settings;
        Material[] _paints;
        Material _lockedPaint;

        public GardenCameraRig Rig { get; private set; }
        public GardenGround Ground { get; private set; }
        public GardenApron LockedLand { get; private set; }
        public Material LockedPaint => _lockedPaint;
        public GardenPanZoom PanZoom { get; private set; }
        public GardenMapCamera Map { get; private set; }
        public GardenStandRow Stands { get; private set; }
        public int Segments { get; private set; }
        public GroundRect Island => GardenFraming.Island(Segments, Rig.Spec);
        public int PendingMotion => _tweens.Count;

        public static Garden25DWorld Load(int segments) =>
            Create(segments, Resources.Load<GardenCameraSettings>(SettingsPath), Resources.Load<Material>(LawnPath), Resources.Load<Material>(EarthPath));

        public static Garden25DWorld Create(int segments, GardenCameraSettings settings, Material lawn, Material earth)
        {
            var world = new GameObject("Garden 2.5D").AddComponent<Garden25DWorld>();
            world._settings = settings;
            world._paints = Paints(settings, lawn, earth);
            world.Segments = segments;
            CameraSpec spec = settings.Spec();

            var eye = new GameObject("Garden Camera", typeof(Camera), typeof(PhysicsRaycaster));
            eye.transform.SetParent(world.transform, false);
            Camera view = eye.GetComponent<Camera>();
            view.clearFlags = CameraClearFlags.SolidColor;
            view.backgroundColor = Palette.Night;
            view.nearClipPlane = 1f;
            view.farClipPlane = 200f;
            view.transparencySortMode = TransparencySortMode.CustomAxis;
            view.transparencySortAxis = DepthAxis;
            view.GetUniversalAdditionalCameraData().SetRenderer(RendererIndex);
            world.Rig = eye.AddComponent<GardenCameraRig>();
            world.Rig.Attach(view, spec);

            var land = new GameObject("Island", typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider));
            land.transform.SetParent(world.transform, false);
            world.Ground = land.AddComponent<GardenGround>();
            world.PanZoom = land.AddComponent<GardenPanZoom>();
            world.PanZoom.Attach(world.Rig, world._tweens, world.Island);
            world.PanZoom.Tapped += world.Touch;
            var locked = new GameObject("Locked Land", typeof(MeshFilter), typeof(MeshRenderer), typeof(BoxCollider));
            locked.transform.SetParent(world.transform, false);
            locked.AddComponent<GardenTouch>().Attach(world.PanZoom);
            world.LockedLand = locked.AddComponent<GardenApron>();
            if (earth != null) world._lockedPaint = new Material(earth) { name = "Locked Land", color = settings.LockedGrass };
            world.BuildGround();

            var mapEye = new GameObject("Map Camera", typeof(Camera));
            mapEye.transform.SetParent(world.transform, false);
            world.Map = mapEye.AddComponent<GardenMapCamera>();
            world.Map.Attach(mapEye.GetComponent<Camera>());
            mapEye.GetComponent<Camera>().GetUniversalAdditionalCameraData().SetRenderer(RendererIndex);

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

        public GardenStandRow Raise(GameCatalog catalog) => Raise(catalog, Resources.Load<GardenStandSettings>(StandsPath));

        public GardenStandRow Raise(GameCatalog catalog, GardenStandSettings settings)
        {
            if (Stands != null) Destroy(Stands.gameObject);
            var row = new GameObject("Stands");
            row.transform.SetParent(transform, false);
            Stands = row.AddComponent<GardenStandRow>();
            Stands.Build(catalog, settings, PanZoom, Rig.Spec, Rig.View.transform.eulerAngles.y);
            return Stands;
        }

        public void Touch(PointerEventData eventData)
        {
            GardenStand stand = eventData.pointerPress != null ? eventData.pointerPress.GetComponent<GardenStand>() : null;
            if (stand != null && Stands != null) Stands.Tap(stand.Index);
            else Ground.Tap(eventData);
        }

        public Material Paint(SlabPart part) => _paints[(int)part];

        void BuildGround()
        {
            Ground.Build(Segments, Rig.Spec, _settings.Slab(), _paints);
            LockedLand.Build(Island, _settings.Slab(), _settings.LockedReach, _lockedPaint);
        }

        static Material[] Paints(GardenCameraSettings settings, Material lawn, Material earth)
        {
            var paints = new Material[SlabMesh.PartCount];
            for (int part = 0; part < paints.Length; part++)
            {
                if ((SlabPart)part == SlabPart.Lawn) paints[part] = lawn;
                else if (earth != null) paints[part] = new Material(earth) { name = "Island " + (SlabPart)part, color = settings.Tint((SlabPart)part) };
            }
            return paints;
        }

        void OnDestroy()
        {
            for (int part = 0; part < _paints.Length; part++)
                if ((SlabPart)part != SlabPart.Lawn && _paints[part] != null) Destroy(_paints[part]);
            if (_lockedPaint != null) Destroy(_lockedPaint);
        }
    }
}
