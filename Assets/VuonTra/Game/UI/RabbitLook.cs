using UnityEngine;
using UnityEngine.UI;
using VuonTra.Core;

namespace VuonTra.Game.UI
{
    /// <summary>
    /// How a rabbit is dressed wherever it is drawn (owner's review 23 Nov: "the rabbits look odd in their clothes").
    /// A hat is sized and seated from the kind's own head and the hat's own <see cref="HatSeat"/>, leans the way the
    /// rabbits look, has a soft shadow under its brim, and the ears stand behind it, or come through it for a knitted
    /// hat (a copy of the drawing cut at the top of the skull, drawn over the hat). Dye takes the fur's brightness
    /// and keeps the ink. Every view uses these, so a rabbit looks the same at its station, on its seat and in the list.
    /// </summary>
    public static class RabbitLook
    {
        /// <summary>Strength of the hat's shadow on the head.</summary>
        public const float ShadowAlpha = 0.28f;

        /// <summary>
        /// The middle of the top of the skull, from the bottom middle of a box <paramref name="art"/> high in which the
        /// kind's drawing stands, fitted to the height (Image.preserveAspect).
        /// </summary>
        public static Vector2 Head(RabbitKind kind, float art)
        {
            return new Vector2((kind.HeadX - 0.5f) * art * GameCatalog.KindAspect, kind.HeadTop * art);
        }

        /// <summary>Where a charm hangs at the neck, from the same bottom middle.</summary>
        public static Vector2 Neck(RabbitKind kind, float art)
        {
            return new Vector2((kind.HeadX - 0.5f) * art * GameCatalog.KindAspect, (kind.HeadTop - GameCatalog.CharmBelowHead) * art);
        }

        /// <summary>
        /// Sizes, places and leans a worn hat. <paramref name="rt"/> hangs from the bottom middle of the box (or of a
        /// parent whose bottom is <paramref name="foot"/> below it), pivoting about the middle of its bottom edge.
        /// </summary>
        public static void PlaceHat(RectTransform rt, Sprite worn, HatSeat seat, RabbitKind kind, float art, float foot = 0f)
        {
            float head = kind.HeadWidth * art;
            float w = Mathf.Max(1f, seat.Width * head), h = w;
            if (worn != null && worn.rect.width > 0f) h = w * worn.rect.height / worn.rect.width;
            Vector2 top = Head(kind, art);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(top.x + seat.Shift * head, foot + top.y - seat.Sink * head);
            rt.localRotation = Quaternion.Euler(0f, 0f, seat.Tilt);
        }

        /// <summary>The soft shadow a hat casts on the head, under the middle of its brim.</summary>
        public static void PlaceShadow(RectTransform rt, HatSeat seat, RabbitKind kind, float art, float foot = 0f)
        {
            float head = kind.HeadWidth * art;
            float w = seat.Width * head * 0.72f, h = Mathf.Max(6f, head * 0.12f);
            Vector2 top = Head(kind, art);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(top.x + seat.Shift * head, foot + top.y - seat.Sink * head);
            rt.localRotation = Quaternion.Euler(0f, 0f, seat.Tilt);
        }

        /// <summary>
        /// The ears drawn again over a hat they come through: the same picture, filled from the top down to the top of
        /// the skull. <paramref name="ears"/> must cover the drawing exactly (same rect, preserveAspect).
        /// </summary>
        public static void ShowEars(Image ears, Image drawing, RabbitKind kind, bool on)
        {
            if (ears.gameObject.activeSelf != on) ears.gameObject.SetActive(on);
            if (!on) return;
            ears.sprite = drawing.sprite;
            ears.color = drawing.color;
            ears.material = drawing.material;
            ears.preserveAspect = true;
            ears.type = Image.Type.Filled;
            ears.fillMethod = Image.FillMethod.Vertical;
            ears.fillOrigin = (int)Image.OriginVertical.Top;
            ears.fillAmount = Mathf.Clamp01(1f - kind.HeadTop);
            ears.raycastTarget = false;
        }

        /// <summary>The colour a rabbit's picture is given: white for its own coat, else its dye as it is.</summary>
        public static Color Fur(Rabbit r)
        {
            if (r == null || r.Dye < 0 || r.Dye >= Shop.Dyes.Length) return Color.white;
            return Palette.Hex(Shop.Dyes[r.Dye].Hex);
        }

        /// <summary>
        /// Draws <paramref name="image"/> in the rabbit's coat: as painted, or dyed with the dye material (the dye's hue
        /// at the fur's own brightness, ink, eyes and shading kept).
        /// </summary>
        public static void Dye(Image image, Rabbit r, GameCatalog catalog)
        {
            bool dyed = r != null && r.Dye >= 0 && r.Dye < Shop.Dyes.Length && catalog != null && catalog.DyeFur != null;
            image.color = dyed ? Fur(r) : Color.white;
            image.material = dyed ? catalog.DyeFur : null;
        }

        /// <summary>
        /// Puts a rabbit's hat, its shadow and its ears on: hidden when it wears none (or its kind cannot), else seated
        /// as <see cref="PlaceHat"/> says. <paramref name="ears"/> may be null where the ears are never drawn again.
        /// </summary>
        public static void WearHat(Image hat, Image shadow, Image ears, Image drawing, GameCatalog catalog, Item worn, RabbitKind kind, float art, float foot = 0f)
        {
            Sprite sprite = worn != null && kind != null && !kind.NoHat ? catalog.WornHat(worn.DefId) : null;
            bool on = sprite != null;
            if (hat.gameObject.activeSelf != on) hat.gameObject.SetActive(on);
            if (shadow != null && shadow.gameObject.activeSelf != on) shadow.gameObject.SetActive(on);
            HatSeat seat = on ? catalog.HatSeatOf(worn.DefId) : HatSeat.Default;
            if (ears != null) ShowEars(ears, drawing, kind, on && seat.EarsOver);
            if (!on) return;
            hat.sprite = sprite;
            hat.preserveAspect = true;
            PlaceHat(hat.rectTransform, sprite, seat, kind, art, foot);
            if (shadow != null) PlaceShadow(shadow.rectTransform, seat, kind, art, foot);
        }
    }
}
