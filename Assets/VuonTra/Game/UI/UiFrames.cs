using UnityEngine;
using UnityEngine.UI;

namespace VuonTra.Game.UI
{
    public static class UiFrames
    {
        static GameCatalog _catalog;

        public static void Use(GameCatalog catalog) => _catalog = catalog;

        public static bool Painted(FrameKind kind) => _catalog != null && _catalog.FrameArt(kind) != null;

        public static Sprite Sprite(FrameKind kind, Sprite plain) => Painted(kind) ? _catalog.FrameArt(kind) : plain;

        public static Color Tint(FrameKind kind, Color flat) => Painted(kind) ? Color.white : flat;

        public static Image AddFrame(this RectTransform rt, FrameKind kind, Sprite plain, Color flat)
        {
            return rt.AddImage(Sprite(kind, plain), Tint(kind, flat), true);
        }

        public static Image AddCard(RectTransform card, FrameKind kind, Sprite plain, float lip, string face = "Face", float scale = 1f)
        {
            bool painted = Painted(kind);
            if (!painted)
                UiKit.Rect("Lip", card).Stretch(0, 0, 1, 1).AddImage(plain, Palette.CardLip, true).pixelsPerUnitMultiplier = scale;
            Image front = UiKit.Rect(face, card).Stretch(0, 0, 1, 1, 0, painted ? 0f : lip, 0, 0).AddFrame(kind, plain, Palette.Paper);
            front.pixelsPerUnitMultiplier = painted ? 1f : scale;
            return front;
        }
    }
}
