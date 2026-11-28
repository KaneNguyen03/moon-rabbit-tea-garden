using UnityEngine;
using UnityEngine.UI;
using VuonTra.Core;

namespace VuonTra.Game.UI
{
    public static class LanternSwatch
    {
        public static Sprite Art(GameCatalog catalog) => catalog.HangingLantern;

        public static Sprite SpriteFor(GameCatalog catalog) => Art(catalog) != null ? Art(catalog) : catalog.Round;

        public static Color TintFor(GameCatalog catalog, int lanternColor) =>
            Art(catalog) != null ? GardenScreen.LanternTint(lanternColor) : Palette.Hex(MonthPass.LanternHex(lanternColor));

        public static Image Add(RectTransform rt, GameCatalog catalog, int lanternColor)
        {
            Image lantern = rt.AddImage(SpriteFor(catalog), TintFor(catalog, lanternColor));
            lantern.preserveAspect = true;
            return lantern;
        }
    }
}
