using System.IO;
using UnityEditor;
using UnityEngine;

namespace VuonTra.EditorTools
{
    /// <summary>
    /// Draws stand-in sprites as PNG files so the game is playable before real art exists.
    /// Replace the files under Art/Placeholder to reskin; nothing else refers to these shapes.
    /// </summary>
    public static class PlaceholderArt
    {
        public const string Folder = "Assets/VuonTra/Art/Placeholder";

        static readonly Color Outline = Hex("2b2347");
        static readonly Color Fur = Hex("fff6e8");

        public static Color Hex(string rgb, float alpha = 1f)
        {
            ColorUtility.TryParseHtmlString("#" + rgb, out Color c);
            c.a = alpha;
            return c;
        }

        sealed class Painter
        {
            public readonly int W, H;
            readonly Color[] _px;

            public Painter(int w, int h)
            {
                W = w; H = h;
                _px = new Color[w * h];
            }

            void Blend(int x, int y, Color c, float coverage)
            {
                if (coverage <= 0f) return;
                int i = y * W + x;
                Color dst = _px[i];
                float a = c.a * Mathf.Clamp01(coverage);
                float outA = a + dst.a * (1f - a);
                if (outA <= 0f) return;
                _px[i] = new Color(
                    (c.r * a + dst.r * dst.a * (1f - a)) / outA,
                    (c.g * a + dst.g * dst.a * (1f - a)) / outA,
                    (c.b * a + dst.b * dst.a * (1f - a)) / outA,
                    outA);
            }

            public void Ellipse(float cx, float cy, float rx, float ry, float rotDeg, Color c)
            {
                float rad = rotDeg * Mathf.Deg2Rad, cos = Mathf.Cos(rad), sin = Mathf.Sin(rad);
                float reach = Mathf.Max(rx, ry) + 2f;
                int x0 = Mathf.Max(0, (int)(cx - reach)), x1 = Mathf.Min(W - 1, (int)(cx + reach));
                int y0 = Mathf.Max(0, (int)(cy - reach)), y1 = Mathf.Min(H - 1, (int)(cy + reach));
                float edge = Mathf.Min(rx, ry);
                for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                {
                    float dx = x + 0.5f - cx, dy = y + 0.5f - cy;
                    float lx = dx * cos + dy * sin, ly = -dx * sin + dy * cos;
                    float d = Mathf.Sqrt(lx * lx / (rx * rx) + ly * ly / (ry * ry));
                    Blend(x, y, c, (1f - d) * edge + 0.5f);
                }
            }

            public void RoundRect(float cx, float cy, float hw, float hh, float r, Color c)
            {
                int x0 = Mathf.Max(0, (int)(cx - hw - 2)), x1 = Mathf.Min(W - 1, (int)(cx + hw + 2));
                int y0 = Mathf.Max(0, (int)(cy - hh - 2)), y1 = Mathf.Min(H - 1, (int)(cy + hh + 2));
                for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                {
                    float qx = Mathf.Abs(x + 0.5f - cx) - (hw - r), qy = Mathf.Abs(y + 0.5f - cy) - (hh - r);
                    float outside = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude;
                    float dist = outside + Mathf.Min(Mathf.Max(qx, qy), 0f) - r;
                    Blend(x, y, c, 0.5f - dist);
                }
            }

            /// <summary>An ellipse with the dark outline every sprite shares.</summary>
            public void Blob(float cx, float cy, float rx, float ry, float rot, Color fill, float line = 5f)
            {
                Ellipse(cx, cy, rx + line, ry + line, rot, Outline);
                Ellipse(cx, cy, rx, ry, rot, fill);
            }

            public void Box(float cx, float cy, float hw, float hh, float r, Color fill, float line = 5f)
            {
                RoundRect(cx, cy, hw + line, hh + line, r + line, Outline);
                RoundRect(cx, cy, hw, hh, r, fill);
            }

            public void Fill(System.Func<float, float, Color> shader)
            {
                for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                    _px[y * W + x] = shader((x + 0.5f) / W, (y + 0.5f) / H);
            }

            public void Save(string name)
            {
                var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
                tex.SetPixels(_px);
                tex.Apply();
                File.WriteAllBytes(Path.Combine(Folder, name + ".png"), tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
            }
        }

        public static void Generate()
        {
            Directory.CreateDirectory(Folder);
            Rabbit();
            Scarf();
            Shapes();
            Background();
            Props();
            File.WriteAllText(Path.Combine(Folder, "CREDITS.md"),
                "# Hình giữ chỗ\n\nMọi file ảnh trong thư mục này do `PlaceholderArt.cs` của dự án tự vẽ ra, " +
                "không lấy từ nguồn ngoài. Chúng chỉ để game chạy được trước khi có hình thật. " +
                "Thay file cùng tên là đổi được hình trong game.\n");
            AssetDatabase.Refresh();

            foreach (string path in Directory.GetFiles(Folder, "*.png"))
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(path.Replace('\\', '/'));
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.spritePixelsPerUnit = 100;
                importer.spriteBorder = Path.GetFileNameWithoutExtension(path) == "panel" ? new Vector4(28, 28, 28, 28) : Vector4.zero;
                importer.SaveAndReimport();
            }
        }

        public static Sprite Load(string name)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(Folder + "/" + name + ".png");
        }

        static void Rabbit()
        {
            var p = new Painter(256, 256);
            Color pink = Hex("f6b6c2"), ink = Hex("2a2140");
            // outlines first so the body parts merge into one silhouette
            float o = 5f;
            p.Ellipse(96, 16, 24 + o, 13 + o, 0, Outline); p.Ellipse(160, 16, 24 + o, 13 + o, 0, Outline);
            p.Ellipse(128, 54, 54 + o, 46 + o, 0, Outline);
            p.Ellipse(74, 62, 15 + o, 21 + o, 20, Outline); p.Ellipse(182, 62, 15 + o, 21 + o, -20, Outline);
            p.Ellipse(98, 192, 21 + o, 56 + o, 9, Outline); p.Ellipse(158, 192, 21 + o, 56 + o, -9, Outline);
            p.Ellipse(128, 122, 72 + o, 60 + o, 0, Outline);

            p.Ellipse(96, 16, 24, 13, 0, Fur); p.Ellipse(160, 16, 24, 13, 0, Fur);
            p.Ellipse(128, 54, 54, 46, 0, Fur);
            p.Ellipse(128, 46, 30, 26, 0, Hex("ffffff"));
            p.Ellipse(74, 62, 15, 21, 20, Fur); p.Ellipse(182, 62, 15, 21, -20, Fur);
            p.Ellipse(98, 192, 21, 56, 9, Fur); p.Ellipse(158, 192, 21, 56, -9, Fur);
            p.Ellipse(98, 190, 9, 40, 9, pink); p.Ellipse(158, 190, 9, 40, -9, pink);
            p.Ellipse(128, 122, 72, 60, 0, Fur);

            p.Ellipse(86, 104, 13, 8, 0, Hex("f9b4b4", 0.75f)); p.Ellipse(170, 104, 13, 8, 0, Hex("f9b4b4", 0.75f));
            p.Ellipse(102, 124, 8.5f, 11, 0, ink); p.Ellipse(154, 124, 8.5f, 11, 0, ink);
            p.Ellipse(99, 129, 3.2f, 3.6f, 0, Color.white); p.Ellipse(151, 129, 3.2f, 3.6f, 0, Color.white);
            p.Ellipse(128, 108, 6.5f, 4.5f, 0, Hex("f08a9c"));
            p.Ellipse(121, 99, 6, 4, 0, ink); p.Ellipse(135, 99, 6, 4, 0, ink);
            p.Ellipse(121, 101.5f, 6, 4, 0, Fur); p.Ellipse(135, 101.5f, 6, 4, 0, Fur);
            p.Save("rabbit");
        }

        static void Scarf()
        {
            // Same canvas as the rabbit so the two line up; white so it can be tinted per station.
            var p = new Painter(256, 256);
            Color shade = new Color(0.55f, 0.55f, 0.6f, 1f);
            p.Ellipse(128, 76, 52, 14, 0, shade);
            p.Ellipse(128, 79, 49, 11, 0, Color.white);
            p.Ellipse(166, 62, 12, 20, -24, shade);
            p.Ellipse(166, 64, 9, 17, -24, Color.white);
            p.Save("scarf");
        }

        static void Shapes()
        {
            var panel = new Painter(64, 64);
            panel.RoundRect(32, 32, 32, 32, 26, Color.white);
            panel.Save("panel");

            var round = new Painter(128, 128);
            round.Ellipse(64, 64, 62, 62, 0, Color.white);
            round.Save("round");

            // Soft light pool: white fading to nothing, tinted where it is used.
            var glow = new Painter(128, 128);
            glow.Fill((u, v) =>
            {
                float d = Mathf.Clamp01(new Vector2(u - 0.5f, v - 0.5f).magnitude * 2f);
                float a = (1f - d) * (1f - d);
                return new Color(1f, 1f, 1f, a);
            });
            glow.Save("glow");

            var carrot = new Painter(128, 128);
            carrot.Blob(88, 100, 7, 20, -20, Hex("6fcf8a"), 4);
            carrot.Blob(72, 106, 7, 20, 12, Hex("8adf9c"), 4);
            carrot.Blob(102, 88, 7, 18, -52, Hex("6fcf8a"), 4);
            carrot.Blob(56, 52, 22, 46, 38, Hex("ff9b4a"), 5);
            carrot.Ellipse(48, 62, 6, 16, 38, Hex("ffc38a"));
            carrot.Ellipse(62, 44, 9, 2.5f, -52, Hex("e07a2c"));
            carrot.Ellipse(50, 30, 7, 2.2f, -52, Hex("e07a2c"));
            carrot.Save("carrot");
        }

        static float Hash(float n)
        {
            float v = Mathf.Sin(n * 12.9898f) * 43758.5453f;
            return v - Mathf.Floor(v);
        }

        static void Background()
        {
            const int w = 540, h = 960;
            var p = new Painter(w, h);
            Color skyTop = Hex("0e1433"), skyMid = Hex("252d68"), skyLow = Hex("6a5a9c");
            Color grassTop = Hex("3d8369"), grassMid = Hex("2b6459"), grassLow = Hex("19394a");
            const float horizon = 0.845f; // garden ground starts here (v runs bottom to top)

            p.Fill((u, v) =>
            {
                float ground = horizon + 0.012f * Mathf.Sin(u * 9f) - 0.02f * Mathf.Sin(u * Mathf.PI);
                if (v < ground)
                {
                    float t = Mathf.Clamp01((ground - v) / ground);
                    Color g = t < 0.35f ? Color.Lerp(grassTop, grassMid, t / 0.35f) : Color.Lerp(grassMid, grassLow, (t - 0.35f) / 0.65f);
                    if (ground - v < 0.004f) g = Color.Lerp(g, Hex("9be0aa"), 0.5f);
                    return g;
                }
                float s = Mathf.Clamp01((v - horizon) / (1f - horizon));
                Color sky = s < 0.5f ? Color.Lerp(skyLow, skyMid, s / 0.5f) : Color.Lerp(skyMid, skyTop, (s - 0.5f) / 0.5f);
                // two rows of soft tree shapes against the sky
                float far = ground + 0.03f + 0.016f * Mathf.Abs(Mathf.Sin(u * 38f)) + 0.012f * Mathf.Sin(u * 11f);
                float near = ground + 0.012f + 0.02f * Mathf.Abs(Mathf.Sin(u * 21f + 1.3f)) + 0.01f * Mathf.Sin(u * 7f);
                if (v < near) return Hex("19254f");
                if (v < far) return Color.Lerp(Hex("262f6b"), sky, 0.25f);
                return sky;
            });

            for (int i = 0; i < 90; i++)
            {
                float x = Hash(i + 1) * w, y = (horizon + 0.035f + Hash(i + 60) * (1f - horizon - 0.035f)) * h;
                p.Ellipse(x, y, 1.1f + Hash(i + 120) * 1.3f, 1.1f + Hash(i + 120) * 1.3f, 0, Hex("fff1d2", 0.35f + 0.6f * Hash(i + 7)));
            }
            float mx = w * 0.50f, my = h * 0.915f;
            for (int k = 6; k >= 1; k--) p.Ellipse(mx, my, 26 + k * 12, 26 + k * 12, 0, Hex("ffe9a8", 0.05f));
            p.Ellipse(mx, my, 26, 26, 0, Hex("fff3c8"));
            p.Ellipse(mx - 8, my + 5, 5, 5, 0, Hex("ecd193", 0.7f));
            p.Ellipse(mx + 9, my - 8, 7, 7, 0, Hex("ecd193", 0.7f));

            for (int i = 0; i < 70; i++)
            {
                float x = Hash(i + 500) * w, y = Hash(i + 510) * horizon * h * 0.96f;
                Color c = i % 3 == 0 ? Hex("ffc866") : (i % 3 == 1 ? Hex("f3a6d6") : Hex("bfe9ff"));
                p.Ellipse(x, y, 6, 6, 0, new Color(c.r, c.g, c.b, 0.22f));
                p.Ellipse(x, y, 2.2f, 2.2f, 0, c);
            }
            for (int i = 0; i < 14; i++)
                p.Ellipse(Hash(i + 400) * w, Hash(i + 410) * horizon * h, 60 + Hash(i + 420) * 70, 14 + Hash(i + 430) * 14, 0,
                    i % 2 == 0 ? new Color(1, 1, 1, 0.035f) : new Color(0.04f, 0.08f, 0.2f, 0.1f));
            p.Save("background");
        }

        static void Props()
        {
            Painter p;

            p = new Painter(128, 128); // ấm trà sương
            p.Blob(100, 62, 20, 9, 35, Hex("6fb7b0"));
            p.Blob(60, 52, 40, 32, 0, Hex("6fb7b0"));
            p.Ellipse(46, 62, 12, 7, 30, Hex("a6dcd5"));
            p.Box(60, 88, 22, 6, 5, Hex("4f9790"));
            p.Blob(60, 100, 7, 7, 0, Hex("4f9790"));
            p.Ellipse(104, 96, 7, 7, 0, new Color(1, 1, 1, 0.5f)); p.Ellipse(110, 112, 9, 9, 0, new Color(1, 1, 1, 0.3f));
            p.Save("prop_am-tra");

            p = new Painter(128, 128); // giỏ lá trà
            p.Blob(44, 84, 18, 9, 30, Hex("6fcf8a")); p.Blob(66, 92, 18, 9, -10, Hex("8adf9c")); p.Blob(86, 84, 18, 9, -40, Hex("6fcf8a"));
            p.Box(64, 44, 40, 28, 12, Hex("b97a45"));
            p.RoundRect(64, 50, 38, 2.5f, 2, Hex("8a5830")); p.RoundRect(64, 36, 36, 2.5f, 2, Hex("8a5830"));
            p.Box(64, 70, 46, 7, 6, Hex("dba56c"));
            p.Save("prop_gio-la");

            p = new Painter(128, 128); // nồi mứt dâu
            p.Box(14, 60, 8, 6, 5, Hex("b23a58")); p.Box(114, 60, 8, 6, 5, Hex("b23a58"));
            p.Box(64, 48, 44, 32, 16, Hex("e0577a"));
            p.RoundRect(36, 48, 5, 16, 5, Hex("f08aa3"));
            p.Box(64, 80, 48, 7, 6, Hex("f6a3b8"));
            p.Blob(46, 100, 6, 6, 0, Hex("ffc2d1"), 3); p.Blob(70, 108, 8, 8, 0, Hex("ffc2d1"), 3); p.Blob(90, 98, 5, 5, 0, Hex("ffc2d1"), 3);
            p.Save("prop_noi-mut");

            p = new Painter(128, 128); // cối giã hoa
            p.Blob(78, 88, 8, 34, -28, Hex("f0e2ba"));
            p.Box(64, 16, 22, 7, 6, Hex("8088b8"));
            p.Blob(64, 50, 44, 30, 0, Hex("a3abd8"));
            p.Ellipse(64, 68, 36, 10, 0, Hex("7078a8"));
            p.Blob(46, 70, 7, 7, 0, Hex("f3a6d6"), 3); p.Blob(62, 73, 6, 6, 0, Hex("ffd0ea"), 3);
            p.Save("prop_coi-hoa");

            p = new Painter(128, 128); // lò bánh hạt dẻ
            p.Box(92, 98, 9, 18, 4, Hex("7d7399"));
            p.Blob(64, 30, 50, 62, 0, Hex("9d93ad"));
            p.RoundRect(64, 2, 64, 12, 0, new Color(0, 0, 0, 0));
            p.Ellipse(40, 66, 8, 5, -30, Hex("b5abc4")); p.Ellipse(90, 56, 7, 4, 30, Hex("b5abc4"));
            p.Blob(64, 26, 26, 30, 0, Hex("2a2140"), 4);
            p.Ellipse(64, 22, 19, 21, 0, Hex("ff9b4a")); p.Ellipse(64, 18, 10, 12, 0, Hex("ffe08a"));
            p.Save("prop_lo-banh");

            p = new Painter(128, 128); // tổ mật ong
            p.Blob(64, 28, 40, 16, 0, Hex("f0a93a")); p.Blob(64, 52, 46, 17, 0, Hex("f5b94c"));
            p.Blob(64, 76, 40, 16, 0, Hex("f0a93a")); p.Blob(64, 98, 26, 13, 0, Hex("f5b94c"));
            p.Blob(64, 44, 11, 13, 0, Hex("2a2140"), 0);
            p.Ellipse(48, 82, 12, 4, 0, Hex("ffe2a3"));
            p.Save("prop_to-mat");

            p = new Painter(128, 128); // quầy trà sữa trăng
            p.Blob(78, 96, 4, 28, -14, Hex("f27d6b"), 3);
            p.Box(64, 50, 30, 40, 10, new Color(1, 1, 1, 0.35f));
            p.RoundRect(64, 40, 27, 28, 8, Hex("e0b891"));
            p.Ellipse(50, 20, 5, 5, 0, Hex("2a2140")); p.Ellipse(64, 18, 5, 5, 0, Hex("2a2140")); p.Ellipse(78, 20, 5, 5, 0, Hex("2a2140"));
            p.Ellipse(57, 30, 5, 5, 0, Hex("2a2140")); p.Ellipse(72, 30, 5, 5, 0, Hex("2a2140"));
            p.Box(64, 92, 36, 6, 5, Hex("8fb4ff"));
            p.Save("prop_tra-sua");

            p = new Painter(128, 128); // bình ủ sao
            p.Box(64, 12, 28, 7, 6, Hex("6a5a8c")); p.Box(64, 24, 8, 8, 3, Hex("6a5a8c"));
            p.Ellipse(64, 70, 56, 56, 0, Hex("ffd98a", 0.25f));
            p.Blob(64, 70, 40, 40, 0, new Color(1, 1, 1, 0.22f));
            p.Ellipse(64, 70, 24, 7, 0, Hex("ffe9a8")); p.Ellipse(64, 70, 7, 24, 0, Hex("ffe9a8"));
            p.Ellipse(64, 70, 17, 5, 45, Hex("ffe9a8")); p.Ellipse(64, 70, 17, 5, -45, Hex("ffe9a8"));
            p.Ellipse(64, 70, 8, 8, 0, Color.white);
            p.Ellipse(46, 90, 10, 5, 35, new Color(1, 1, 1, 0.6f));
            p.Save("prop_binh-sao");
        }
    }
}
