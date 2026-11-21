using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace VuonTra.EditorTools
{
    /// <summary>
    /// Backdrop layers for the side-scrolling street: sky, far hills, tree line and ground. The
    /// last three tile horizontally so they can slide at different speeds (parallax). Stand-ins
    /// like the rest of the placeholder art: a file of the same name in Art/Final replaces one.
    /// </summary>
    public static class StreetArt
    {
        public const string Folder = "Assets/VuonTra/Art/Placeholder";

        static Color Hex(string rgb) => PlaceholderArt.Hex(rgb);

        static float Hash(float a, float b)
        {
            float v = Mathf.Sin(a * 127.1f + b * 311.7f) * 43758.5453f;
            return v - Mathf.Floor(v);
        }

        static void Write(string name, int w, int h, Func<float, float, Color> shader)
        {
            var px = new Color[w * h];
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                px[y * w + x] = shader((x + 0.5f) / w, (y + 0.5f) / h);
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.SetPixels(px);
            tex.Apply();
            File.WriteAllBytes(Path.Combine(Folder, name + ".png"), tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
        }

        public static void Generate()
        {
            Directory.CreateDirectory(Folder);
            Color skyLow = Hex("7a66a8"), skyMid = Hex("2a3272"), skyTop = Hex("0e1433");

            Write("bg_sky", 960, 540, (u, v) =>
            {
                Color c = v < 0.5f ? Color.Lerp(skyLow, skyMid, v / 0.5f) : Color.Lerp(skyMid, skyTop, (v - 0.5f) / 0.5f);
                // moon with a soft halo
                float dx = (u - 0.74f) * (960f / 540f), dy = v - 0.78f;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                c = Color.Lerp(c, Hex("ffe9a8"), Mathf.Clamp01(1f - d / 0.42f) * 0.22f);
                if (d < 0.062f) c = Color.Lerp(Hex("fff3c8"), Hex("ecd193"), Mathf.Clamp01((Mathf.Sin(dx * 90f) * Mathf.Sin(dy * 70f) - 0.55f) * 3f));
                else if (d < 0.066f) c = Color.Lerp(Hex("fff3c8"), c, (d - 0.062f) / 0.004f);
                // stars: one per cell, most cells empty
                float cx = Mathf.Floor(u * 120f), cy = Mathf.Floor(v * 68f);
                if (v > 0.3f && Hash(cx, cy) > 0.93f)
                {
                    float sx = (u * 120f - cx) - 0.2f - 0.6f * Hash(cx + 5, cy), sy = (v * 68f - cy) - 0.2f - 0.6f * Hash(cx, cy + 9);
                    float star = Mathf.Clamp01(1f - Mathf.Sqrt(sx * sx + sy * sy) / (0.16f + 0.2f * Hash(cx + 2, cy + 3)));
                    c = Color.Lerp(c, Hex("fff1d2"), star * (0.5f + 0.5f * Hash(cx + 1, cy + 1)));
                }
                return c;
            });

            Write("bg_hills", 1024, 256, (u, v) =>
            {
                float h = 0.52f + 0.20f * Mathf.Sin(u * Mathf.PI * 4f) + 0.10f * Mathf.Sin(u * Mathf.PI * 10f + 1.3f) + 0.04f * Mathf.Sin(u * Mathf.PI * 26f);
                float a = Mathf.Clamp01((h - v) * 256f);
                Color c = Color.Lerp(Hex("3a4488"), Hex("2a3272"), Mathf.Clamp01((h - v) * 3f));
                return new Color(c.r, c.g, c.b, a);
            });

            Write("bg_trees", 1024, 256, (u, v) =>
            {
                float far = 0.50f + 0.22f * Mathf.Pow(Mathf.Abs(Mathf.Sin(u * Mathf.PI * 22f + 0.7f)), 0.7f) + 0.05f * Mathf.Sin(u * Mathf.PI * 6f);
                float near = 0.30f + 0.30f * Mathf.Pow(Mathf.Abs(Mathf.Sin(u * Mathf.PI * 13f)), 0.6f) + 0.06f * Mathf.Sin(u * Mathf.PI * 4f + 2f);
                float aNear = Mathf.Clamp01((near - v) * 256f), aFar = Mathf.Clamp01((far - v) * 256f);
                Color c = Color.Lerp(Hex("262f6b"), Hex("19254f"), aNear);
                return new Color(c.r, c.g, c.b, Mathf.Max(aNear, aFar));
            });

            Color grassTop = Hex("4a9474"), grassMid = Hex("2f6b5c"), grassLow = Hex("1a3d4a");
            Color[] flowers = { Hex("ffc866"), Hex("f3a6d6"), Hex("bfe9ff") };
            Write("bg_ground", 1024, 512, (u, v) =>
            {
                float t = 1f - v; // 0 at the horizon, 1 at the bottom
                Color c = t < 0.3f ? Color.Lerp(grassTop, grassMid, t / 0.3f) : Color.Lerp(grassMid, grassLow, (t - 0.3f) / 0.7f);
                if (t < 0.012f) c = Color.Lerp(Hex("a6e6b4"), c, t / 0.012f);
                // soft bands of lighter grass, wider towards the viewer
                c = Color.Lerp(c, Color.white, 0.035f * Mathf.Clamp01(Mathf.Sin(u * Mathf.PI * 6f + t * 14f) * Mathf.Sin(t * 22f)));
                // a worn path the stations stand along
                float path = Mathf.Abs(t - 0.52f - 0.03f * Mathf.Sin(u * Mathf.PI * 8f));
                c = Color.Lerp(c, Hex("7fb39c"), Mathf.Clamp01(1f - path / 0.05f) * 0.22f);
                // glowing flowers, one per cell in most cells; cells tile across the texture
                float cx = Mathf.Floor(u * 16f), cy = Mathf.Floor(v * 8f);
                if (Hash(cx + 40, cy) > 0.35f)
                {
                    float fx = (u * 16f - cx) - 0.15f - 0.7f * Hash(cx, cy + 20), fy = ((v * 8f - cy) - 0.15f - 0.7f * Hash(cx + 30, cy)) ;
                    float d = Mathf.Sqrt(fx * fx + fy * fy);
                    Color f = flowers[(int)(Hash(cx + 3, cy + 7) * 2.999f)];
                    c = Color.Lerp(c, f, Mathf.Clamp01(1f - d / 0.11f) * 0.28f);
                    if (d < 0.035f) c = f;
                }
                return c;
            });

            AssetDatabase.Refresh();
            foreach (string name in new[] { "bg_sky", "bg_hills", "bg_trees", "bg_ground" })
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(Folder + "/" + name + ".png");
                importer.textureType = TextureImporterType.Default;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.wrapMode = name == "bg_sky" ? TextureWrapMode.Clamp : TextureWrapMode.Repeat;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.SaveAndReimport();
            }
        }

        /// <summary>A drawn backdrop layer in Art/Final wins over the generated one.</summary>
        public static Texture2D Load(string name)
        {
            var drawn = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/VuonTra/Art/Final/" + name + ".png");
            return drawn != null ? drawn : AssetDatabase.LoadAssetAtPath<Texture2D>(Folder + "/" + name + ".png");
        }
    }
}
