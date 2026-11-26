using UnityEngine;
using UnityEngine.Rendering;
using VuonTra.Core;

namespace VuonTra.Game
{
    [RequireComponent(typeof(SortingGroup))]
    public sealed class GardenStand : MonoBehaviour
    {
        public const float FlatLift = 0.01f, PadLift = 0.005f;
        public const int PadOrder = -20, ShadowOrder = -10, ArtOrder = 0, WorkerOrder = 1;
        public const float FootPivotShare = 0.25f;

        GardenStandSettings _settings;
        SpriteRenderer _art, _shadow, _pad;
        BoxCollider _box;
        Transform _flat;

        public int Index { get; private set; }
        public SpriteRenderer Art => _art;
        public SpriteRenderer Shadow => _shadow;
        public SpriteRenderer Pad => _pad;
        public Transform Flat => _flat;
        public double Height { get; private set; }
        public Vector3 Foot => transform.position;
        public bool Touchable => _box != null;

        public static GardenStand Create(string name, Transform parent, int index, GardenPanZoom touch, float yawDegrees, GardenStandSettings settings)
        {
            var go = new GameObject(name, typeof(SortingGroup));
            go.transform.SetParent(parent, false);
            go.transform.localRotation = Quaternion.Euler(0f, yawDegrees, 0f);
            GardenStand stand = go.AddComponent<GardenStand>();
            stand.Index = index;
            stand._settings = settings;
            if (touch != null)
            {
                stand._box = go.AddComponent<BoxCollider>();
                go.AddComponent<GardenTouch>().Attach(touch);
            }
            stand._flat = new GameObject(name + " Flat").transform;
            stand._flat.SetParent(parent, false);
            stand._flat.localRotation = go.transform.localRotation;
            stand._pad = Renderer("Pad", stand._flat, PadOrder);
            stand._pad.enabled = false;
            stand._shadow = Renderer("Shadow", stand._flat, ShadowOrder);
            stand._art = Renderer("Art", go.transform, ArtOrder);
            return stand;
        }

        public static SpriteRenderer Renderer(string name, Transform parent, int order)
        {
            var go = new GameObject(name, typeof(SpriteRenderer));
            go.transform.SetParent(parent, false);
            var renderer = go.GetComponent<SpriteRenderer>();
            renderer.sortingOrder = order;
            return renderer;
        }

        public static float FootOf(Sprite sprite) => sprite.pivot.y <= sprite.rect.height * FootPivotShare ? 0f : sprite.bounds.min.y;

        public static float Stand(SpriteRenderer renderer, Sprite sprite, double metres)
        {
            renderer.sprite = sprite;
            if (sprite == null) return 0f;
            Bounds shape = sprite.bounds;
            float foot = FootOf(sprite), tall = shape.max.y - foot;
            float scale = tall > 0f ? (float)metres / tall : 1f;
            renderer.transform.localScale = new Vector3(scale, scale, 1f);
            renderer.transform.localPosition = new Vector3(-shape.center.x * scale, -foot * scale, 0f);
            return shape.size.x * scale;
        }

        public static void LayFlat(SpriteRenderer renderer, Sprite sprite, Color tint, float wide, float deep, Vector2 at, float lift)
        {
            renderer.sprite = sprite;
            renderer.color = tint;
            renderer.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            renderer.transform.localPosition = new Vector3(at.x, lift, at.y);
            if (sprite == null) return;
            Bounds shape = sprite.bounds;
            renderer.transform.localScale = new Vector3(wide / shape.size.x, deep / shape.size.y, 1f);
        }

        public static void LayShadow(SpriteRenderer shadow, GardenStandSettings settings, float baseWidth, Vector2 foot)
        {
            float wide = Mathf.Max(settings.ShadowMinWidth, baseWidth * settings.ShadowShare);
            LayFlat(shadow, settings.ShadowSprite, settings.ShadowTint, wide, wide * settings.ShadowDepth, foot + settings.ShadowFall, FlatLift);
        }

        public void PlaceAt(GroundPoint foot)
        {
            var at = new Vector3((float)foot.X, 0f, (float)foot.Z);
            transform.localPosition = at;
            _flat.localPosition = at;
        }

        void OnEnable()
        {
            if (_flat != null) _flat.gameObject.SetActive(true);
        }

        void OnDisable()
        {
            if (_flat != null) _flat.gameObject.SetActive(false);
        }

        public void Show(Sprite sprite, double metres, Color tint, Sprite pad)
        {
            Height = sprite != null ? metres : 0d;
            float baseWidth = Stand(_art, sprite, metres);
            _art.color = tint;
            _art.enabled = sprite != null;
            _shadow.enabled = sprite != null && _settings.ShadowSprite != null;
            if (sprite != null) LayShadow(_shadow, _settings, baseWidth, Vector2.zero);
            _pad.enabled = sprite != null && pad != null;
            if (_pad.enabled)
            {
                float wide = Mathf.Max(_settings.PadMinWidth, baseWidth * _settings.PadShare);
                LayFlat(_pad, pad, _settings.PadTint, wide, wide * _settings.PadDepth, Vector2.zero, PadLift);
            }
            Fit();
        }

        public void Fit()
        {
            if (_box == null) return;
            bool any = false;
            Bounds local = default;
            foreach (SpriteRenderer part in GetComponentsInChildren<SpriteRenderer>())
            {
                if (!part.enabled || part.sprite == null || part.sortingOrder < ArtOrder) continue;
                Bounds world = part.bounds;
                Vector3 low = transform.InverseTransformPoint(world.min), high = transform.InverseTransformPoint(world.max);
                var piece = new Bounds((low + high) / 2f, new Vector3(Mathf.Abs(high.x - low.x), Mathf.Abs(high.y - low.y), 0.2f));
                if (any) local.Encapsulate(piece);
                else local = piece;
                any = true;
            }
            _box.enabled = any;
            if (!any) return;
            _box.center = local.center;
            _box.size = new Vector3(local.size.x, local.size.y, 0.2f);
        }
    }
}
