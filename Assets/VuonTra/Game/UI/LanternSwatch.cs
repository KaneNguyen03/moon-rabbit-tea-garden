using UnityEngine;
using UnityEngine.UI;

namespace VuonTra.Game.UI
{
    public static class LanternSwatch
    {
        public static Sprite Art(GameCatalog catalog) => catalog.StoneLantern;

        public static Sprite SpriteFor(GameCatalog catalog) => Art(catalog) != null ? Art(catalog) : catalog.Round;

        public static Image Add(RectTransform rt, GameCatalog catalog, Color color)
        {
            Image lantern = rt.AddImage(SpriteFor(catalog), color);
            lantern.preserveAspect = true;
            return lantern;
        }
    }
}
