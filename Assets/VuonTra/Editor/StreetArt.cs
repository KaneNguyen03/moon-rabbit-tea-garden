using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using VuonTra.Game.UI;

namespace VuonTra.EditorTools
{
    public static class StreetArt
    {
        public const string Folder = "Assets/VuonTra/Art/Placeholder";
        public const string Ground = "Assets/VuonTra/Art/Ground";

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
                float dx = (u - 0.74f) * (960f / 540f), dy = v - 0.78f;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                c = Color.Lerp(c, Hex("ffe9a8"), Mathf.Clamp01(1f - d / 0.42f) * 0.22f);
                if (d < 0.062f) c = Color.Lerp(Hex("fff3c8"), Hex("ecd193"), Mathf.Clamp01((Mathf.Sin(dx * 90f) * Mathf.Sin(dy * 70f) - 0.55f) * 3f));
                else if (d < 0.066f) c = Color.Lerp(Hex("fff3c8"), c, (d - 0.062f) / 0.004f);
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
                float t = 1f - v;
                Color c = t < 0.3f ? Color.Lerp(grassTop, grassMid, t / 0.3f) : Color.Lerp(grassMid, grassLow, (t - 0.3f) / 0.7f);
                if (t < 0.012f) c = Color.Lerp(Hex("a6e6b4"), c, t / 0.012f);
                c = Color.Lerp(c, Color.white, 0.035f * Mathf.Clamp01(Mathf.Sin(u * Mathf.PI * 6f + t * 14f) * Mathf.Sin(t * 22f)));
                float path = Mathf.Abs(t - 0.52f - 0.03f * Mathf.Sin(u * Mathf.PI * 8f));
                c = Color.Lerp(c, Hex("7fb39c"), Mathf.Clamp01(1f - path / 0.05f) * 0.22f);
                float gx = Mathf.Floor(u * 96f), gy = Mathf.Floor(v * 40f);
                if (Hash(gx + 11, gy + 5) > 0.78f)
                {
                    float tx = (u * 96f - gx) - 0.5f, ty = (v * 40f - gy) - 0.3f;
                    float blade = Mathf.Clamp01(1f - Mathf.Abs(tx) * 5f - Mathf.Abs(ty) * 1.6f);
                    c = Color.Lerp(c, Hex("5fb58a"), blade * 0.5f);
                }
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

            Write("bg_grass", 512, 512, (u, v) =>
            {
                float patch = 0.5f + 0.25f * Mathf.Sin(u * Mathf.PI * 2f + Mathf.Sin(v * Mathf.PI * 4f) * 1.3f)
                                   + 0.25f * Mathf.Sin(v * Mathf.PI * 4f + Mathf.Sin(u * Mathf.PI * 6f) * 0.9f);
                Color c = Color.Lerp(Hex("1f5249"), Hex("3f9070"), Mathf.SmoothStep(0f, 1f, patch));
                float gx = Mathf.Floor(u * 64f), gy = Mathf.Floor(v * 64f);
                if (Hash(gx + 11, gy + 5) > 0.74f)
                {
                    float tx = (u * 64f - gx) - 0.5f, ty = (v * 64f - gy) - 0.25f;
                    float blade = Mathf.Clamp01(1f - Mathf.Abs(tx) * 4.5f - Mathf.Abs(ty) * 1.5f);
                    c = Color.Lerp(c, Hash(gx, gy) > 0.5f ? Hex("56a882") : Hex("1c4a44"), blade * 0.4f);
                }
                float cx = Mathf.Floor(u * 6f), cy = Mathf.Floor(v * 6f);
                if (Hash(cx + 40, cy) > 0.7f)
                {
                    float fx = (u * 6f - cx) - 0.2f - 0.6f * Hash(cx, cy + 20), fy = (v * 6f - cy) - 0.2f - 0.6f * Hash(cx + 30, cy);
                    if (Mathf.Sqrt(fx * fx + fy * fy) < 0.022f) c = Color.Lerp(c, Hex("e9f2d8"), 0.7f);
                }
                return c;
            });

            Write("bg_moonbeam", 512, 512, (u, v) =>
            {
                float across = u + (v - 0.5f) * 0.3f;
                float shafts = Mathf.Pow(Mathf.Max(0f, Mathf.Sin((across - 0.08f) * Mathf.PI * 3.6f)), 5f);
                float Rise(float t, float over) => Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / over));
                float sides = Rise(u, 0.22f) * Rise(1f - u, 0.22f);
                float fade = Rise(v, 0.55f) * Rise(1f - v, 0.12f);
                return new Color(0.80f, 0.90f, 1f, shafts * sides * fade * 0.25f);
            });

            Color edge = Palette.Night, moonlight = new Color(0.80f, 0.90f, 1f);
            Write("bg_vignette", 144, 256, (u, v) =>
            {
                float dx = (u - 0.5f) * 2f, dy = (v - 0.5f) * 2f;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float dark = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((d - 0.75f) / 0.65f)) * SceneOverlays.VignetteStrength;
                float mx = (u - 0.87f) / 0.6f, my = (v - 0.9f) / 0.34f;
                float spill = 1f - Mathf.Clamp01(Mathf.Sqrt(mx * mx + my * my));
                float light = spill * spill * SceneOverlays.MoonlightStrength * (1f - dark);
                float a = dark + light;
                if (a <= 0f) return new Color(edge.r, edge.g, edge.b, 0f);
                Color c = (edge * dark + moonlight * light) / a;
                return new Color(c.r, c.g, c.b, a);
            });

            AssetDatabase.Refresh();
            foreach (string name in new[] { "bg_sky", "bg_hills", "bg_trees", "bg_ground", "bg_grass", "bg_moonbeam", "bg_vignette" })
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(Folder + "/" + name + ".png");
                importer.textureType = TextureImporterType.Default;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.wrapMode = name == "bg_sky" || name == "bg_vignette" || name == "bg_moonbeam" ? TextureWrapMode.Clamp : TextureWrapMode.Repeat;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.SaveAndReimport();
            }
        }

        public static void ImportGround()
        {
            if (!Directory.Exists(Ground)) return;
            foreach (string file in Directory.GetFiles(Ground, "*.png"))
            {
                var importer = AssetImporter.GetAtPath(file.Replace('\\', '/')) as TextureImporter;
                if (importer == null) continue;
                bool ok = importer.textureType == TextureImporterType.Default
                    && importer.wrapMode == TextureWrapMode.Repeat
                    && importer.mipmapEnabled
                    && importer.filterMode == FilterMode.Trilinear
                    && importer.npotScale == TextureImporterNPOTScale.None
                    && importer.textureCompression == TextureImporterCompression.Uncompressed;
                if (ok) continue;
                importer.textureType = TextureImporterType.Default;
                importer.alphaIsTransparency = false;
                importer.wrapMode = TextureWrapMode.Repeat;
                importer.mipmapEnabled = true;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.filterMode = FilterMode.Trilinear;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
        }

        public static Texture2D Load(string name)
        {
            var ground = AssetDatabase.LoadAssetAtPath<Texture2D>(Ground + "/" + name + ".png");
            if (ground != null) return ground;
            var drawn = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/VuonTra/Art/Final/" + name + ".png");
            return drawn != null ? drawn : AssetDatabase.LoadAssetAtPath<Texture2D>(Folder + "/" + name + ".png");
        }
    }
}
