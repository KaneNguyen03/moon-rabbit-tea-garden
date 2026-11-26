using System.Collections.Generic;
using UnityEngine;

namespace VuonTra.Game.UI
{
    public static class Palette
    {
        static readonly Dictionary<string, Color> Parsed = new Dictionary<string, Color>();

        public static readonly Color Night = Hex("1B1E45");
        public static readonly Color Grass = Hex("3D8369");
        public static readonly Color Paper = Hex("FFF4E0");
        public static readonly Color Ink = Hex("3A2A4D");
        public static readonly Color Blush = Hex("F6A5B8");
        public static readonly Color Moss = Hex("6FBF8A");
        public static readonly Color Lantern = Hex("FFC866");
        public static readonly Color Carrot = Hex("FF9B4A");

        public static readonly Color[] Tiers =
        {
            Hex("C9CEDA"), Moss, Hex("6FA8F0"), Hex("B48BE8"), Lantern, Hex("F0605E")
        };

        public static readonly Color Card = Mix(Paper, Color.white, 0.7f);
        public static readonly Color CardLip = Mix(Paper, Ink, 0.2f);
        public static readonly Color Track = Mix(Paper, Ink, 0.14f);
        public static readonly Color InkSoft = Mix(Ink, Paper, 0.27f);
        public static readonly Color CarrotLip = Mix(Carrot, Ink, 0.3f);
        public static readonly Color OnCarrot = Ink;
        public static readonly Color CarrotInk = Mix(Carrot, Ink, 0.55f);
        public static readonly Color MossInk = Mix(Moss, Ink, 0.5f);
        public static readonly Color OffLip = Mix(Paper, Ink, 0.36f);
        public static readonly Color Scrim = Alpha(Mix(Night, Ink, 0.4f), 0.72f);
        public static readonly Color Asleep = Alpha(Mix(Night, Paper, 0.38f), 0.8f);

        public static Color Hex(string rgb)
        {
            if (rgb != null && Parsed.TryGetValue(rgb, out Color known)) return known;
            ColorUtility.TryParseHtmlString("#" + rgb, out Color c);
            if (rgb != null) Parsed[rgb] = c;
            return c;
        }

        public static Color Mix(Color a, Color b, float t)
        {
            return new Color(Mathf.Lerp(a.r, b.r, t), Mathf.Lerp(a.g, b.g, t), Mathf.Lerp(a.b, b.b, t), Mathf.Lerp(a.a, b.a, t));
        }

        public static Color Alpha(Color c, float a)
        {
            return new Color(c.r, c.g, c.b, a);
        }
    }
}
