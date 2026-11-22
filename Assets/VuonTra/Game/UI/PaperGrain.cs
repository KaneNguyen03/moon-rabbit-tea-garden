using UnityEngine;
using UnityEngine.UI;

namespace VuonTra.Game.UI
{
    /// <summary>
    /// A faint watercolor-paper grain laid over the garden (art bible, cozy-watercolor-art): light paper fibres and
    /// ink speckles so the painted ground never reads as a flat gradient. Made once in code, seamless, and it never
    /// catches a touch.
    /// </summary>
    public static class PaperGrain
    {
        public const int Size = 256;
        /// <summary>Strongest opacity a grain pixel reaches; most stay well under it (the skill asks for 10–15%).</summary>
        public const float Strength = 0.14f;
        /// <summary>How wide one tile of grain is on the canvas.</summary>
        public const float TileWidth = 360f;

        static Texture2D _texture;

        public static Texture2D Texture
        {
            get
            {
                if (_texture == null) _texture = Make();
                return _texture;
            }
        }

        /// <summary>Lays the grain over everything already under <paramref name="parent"/>, stretched to it.</summary>
        public static RawImage Add(RectTransform parent, float width, float height)
        {
            var rt = UiKit.Rect("PaperGrain", parent).Stretch(0, 0, 1, 1);
            var raw = rt.gameObject.AddComponent<RawImage>();
            raw.texture = Texture;
            raw.raycastTarget = false;
            raw.uvRect = new Rect(0, 0, width / TileWidth, height / TileWidth);
            return raw;
        }

        static Texture2D Make()
        {
            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
            {
                name = "PaperGrain",
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.DontSave
            };
            var px = new Color32[Size * Size];
            Color paper = Palette.Paper, ink = Palette.Ink;
            const float Tau = Mathf.PI * 2f;
            for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                {
                    float u = x / (float)Size, v = y / (float)Size;
                    // Soft blotches where the paint pooled: whole periods only, so the tile meets itself.
                    float blotch = 0.5f * Mathf.Sin(Tau * (2f * u + 1f * v) + 1.3f) * Mathf.Sin(Tau * (1f * u - 3f * v) + 0.4f)
                                 + 0.3f * Mathf.Sin(Tau * (5f * u + 4f * v) + 2.1f);
                    // Paper tooth: every pixel its own, which tiles by itself.
                    float tooth = Hash(x, y) * 2f - 1f;
                    // Fibres: short streaks that run mostly one way.
                    float fibre = Hash(x / 6, y) > 0.985f ? 1f : 0f;
                    float n = Mathf.Clamp(0.55f * tooth + 0.35f * blotch + 0.6f * fibre, -1f, 1f);
                    Color c = n >= 0f ? paper : ink;
                    c.a = Mathf.Abs(n) * Strength;
                    px[y * Size + x] = c;
                }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return tex;
        }

        static float Hash(int x, int y)
        {
            unchecked
            {
                uint h = (uint)(x * 374761393 + y * 668265263);
                h = (h ^ (h >> 13)) * 1274126177u;
                h ^= h >> 16;
                return (h & 0xFFFFFF) / (float)0xFFFFFF;
            }
        }
    }
}
