using UnityEngine;
using UnityEngine.UI;

namespace VuonTra.Game.UI
{
    public sealed class DishIcon
    {
        static readonly string[] Numerals = { "", "II", "III" };

        public RectTransform Root;
        public Image Plate, Art, Silhouette;
        public Text Numeral;
        Color _plate;

        public GameObject Shadow => Silhouette.gameObject;

        public static Color Tint(int dish) => dish == 1 ? Palette.Mix(Color.white, Palette.Blush, 0.6f) : dish == 2 ? Palette.Mix(Color.white, Palette.Lantern, 0.7f) : Color.white;

        public static string NumeralOf(int dish) => dish >= 0 && dish < Numerals.Length ? Numerals[dish] : "";

        public static DishIcon Build(RectTransform parent, GameCatalog catalog, float size)
        {
            var icon = new DishIcon { Root = UiKit.Rect("Icon", parent) };
            icon.Root.sizeDelta = new Vector2(size, size);
            icon.Plate = icon.Root.AddImage(catalog.Round, Palette.Track);
            float pad = size * 0.14f;
            icon.Art = UiKit.Rect("Art", icon.Root).Stretch(0, 0, 1, 1, pad, pad, pad, pad).AddImage(null, Color.white);
            icon.Art.preserveAspect = true;
            icon.Silhouette = UiKit.Rect("Shadow", icon.Root).Stretch(0, 0, 1, 1, pad, pad, pad, pad).AddImage(null, Palette.Alpha(Palette.Ink, 0.5f));
            icon.Silhouette.preserveAspect = true;
            icon.Numeral = UiKit.Rect("Numeral", icon.Root).Place(1f, 0f, -size * 0.18f, size * 0.16f, size * 0.6f, size * 0.36f)
                .AddText("", Mathf.RoundToInt(size * 0.26f), Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            var outline = icon.Numeral.gameObject.AddComponent<Outline>();
            outline.effectColor = Palette.Paper;
            outline.effectDistance = new Vector2(2f, -2f);
            return icon;
        }

        public void Show(StationData data, int dish)
        {
            Sprite art = data.Product != null ? data.Product : data.Prop;
            Art.sprite = art;
            Art.color = Tint(dish);
            Silhouette.sprite = art;
            _plate = dish == 0 ? Palette.Mix(data.Accent, Palette.Paper, 0.62f) : Palette.Mix(Tint(dish), Palette.Paper, 0.35f);
            Numeral.text = NumeralOf(dish);
            SetHidden(false);
        }

        public void SetHidden(bool hidden)
        {
            Silhouette.gameObject.SetActive(hidden);
            Art.enabled = !hidden;
            Plate.color = hidden ? Palette.Track : _plate;
            Numeral.color = hidden ? Palette.OffInk : Palette.Ink;
        }
    }
}
