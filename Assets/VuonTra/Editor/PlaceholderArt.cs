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

        static readonly Color Outline = Hex("3a2a4d"); // the same plum the drawn rabbits are outlined in
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
            readonly float _s;

            /// <param name="scale">Draw at this multiple of the given size; coordinates stay in the given size.</param>
            public Painter(int w, int h, int scale = 1)
            {
                _s = scale;
                W = w * scale; H = h * scale;
                _px = new Color[W * H];
            }

            /// <summary>
            /// Soft shading for a filled shape, to sit beside hand-painted art: lighter towards the
            /// top-left light, darker and slightly plum towards the bottom-right and at the rim.
            /// </summary>
            static Color Lit(Color c, float light, float rim)
            {
                Color dark = Color.Lerp(c, Outline, 0.42f), bright = Color.Lerp(c, Color.white, 0.30f);
                Color o = light >= 0f ? Color.Lerp(c, bright, light) : Color.Lerp(c, dark, -light);
                o = Color.Lerp(o, dark, rim);
                o.a = c.a;
                return o;
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

            public void Ellipse(float cx, float cy, float rx, float ry, float rotDeg, Color c, bool shaded = false)
            {
                cx *= _s; cy *= _s; rx *= _s; ry *= _s;
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
                    Color fill = c;
                    if (shaded)
                    {
                        // light comes from the upper left in screen space, whatever the shape's rotation
                        float light = Mathf.Clamp((-dx / rx * 0.45f + dy / ry * 0.8f), -1f, 1f) * 0.8f;
                        fill = Lit(c, light, Mathf.Clamp01((d - 0.72f) / 0.28f) * 0.35f);
                    }
                    Blend(x, y, fill, (1f - d) * edge + 0.5f);
                }
            }

            public void RoundRect(float cx, float cy, float hw, float hh, float r, Color c, bool shaded = false)
            {
                cx *= _s; cy *= _s; hw *= _s; hh *= _s; r *= _s;
                int x0 = Mathf.Max(0, (int)(cx - hw - 2)), x1 = Mathf.Min(W - 1, (int)(cx + hw + 2));
                int y0 = Mathf.Max(0, (int)(cy - hh - 2)), y1 = Mathf.Min(H - 1, (int)(cy + hh + 2));
                for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                {
                    float qx = Mathf.Abs(x + 0.5f - cx) - (hw - r), qy = Mathf.Abs(y + 0.5f - cy) - (hh - r);
                    float outside = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude;
                    float dist = outside + Mathf.Min(Mathf.Max(qx, qy), 0f) - r;
                    Color fill = c;
                    if (shaded)
                    {
                        float light = Mathf.Clamp(-(x + 0.5f - cx) / Mathf.Max(1f, hw) * 0.4f + (y + 0.5f - cy) / Mathf.Max(1f, hh) * 0.75f, -1f, 1f) * 0.75f;
                        fill = Lit(c, light, Mathf.Clamp01(1f + dist / (Mathf.Min(hw, hh) * 0.45f)) * 0.3f);
                    }
                    Blend(x, y, fill, 0.5f - dist);
                }
            }

            /// <summary>An ellipse with the dark outline every sprite shares.</summary>
            public void Blob(float cx, float cy, float rx, float ry, float rot, Color fill, float line = 3.4f)
            {
                if (line > 0f) Ellipse(cx, cy, rx + line, ry + line, rot, new Color(Outline.r, Outline.g, Outline.b, Mathf.Max(fill.a, 0.85f)));
                Ellipse(cx, cy, rx, ry, rot, fill, true);
            }

            public void Box(float cx, float cy, float hw, float hh, float r, Color fill, float line = 3.4f)
            {
                if (line > 0f) RoundRect(cx, cy, hw + line, hh + line, r + line, new Color(Outline.r, Outline.g, Outline.b, Mathf.Max(fill.a, 0.85f)));
                RoundRect(cx, cy, hw, hh, r, fill, true);
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
            Decor();
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
                string file = Path.GetFileNameWithoutExtension(path);
                importer.spriteBorder = file == "panel" ? new Vector4(28, 28, 28, 28) : (file == "shadow" ? new Vector4(40, 40, 40, 40) : Vector4.zero);
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

            var carrot = new Painter(128, 128, 2);
            carrot.Blob(88, 100, 7, 20, -20, Hex("6fcf8a"), 4);
            carrot.Blob(72, 106, 7, 20, 12, Hex("8adf9c"), 4);
            carrot.Blob(102, 88, 7, 18, -52, Hex("6fcf8a"), 4);
            carrot.Blob(56, 52, 22, 46, 38, Hex("ff9b4a"), 5);
            carrot.Ellipse(48, 62, 6, 16, 38, Hex("ffc38a"));
            carrot.Ellipse(62, 44, 9, 2.5f, -52, Hex("e07a2c"));
            carrot.Ellipse(50, 30, 7, 2.2f, -52, Hex("e07a2c"));
            carrot.Save("carrot");

            // Soft drop shadow for panels and buttons: a blurred rounded rectangle, 9-sliced.
            var shadow = new Painter(96, 96);
            shadow.Fill((u, v) =>
            {
                float qx = Mathf.Abs(u - 0.5f) * 96f - 20f, qy = Mathf.Abs(v - 0.5f) * 96f - 20f;
                float dist = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude + Mathf.Min(Mathf.Max(qx, qy), 0f) - 10f;
                float a = Mathf.SmoothStep(1f, 0f, Mathf.Clamp01((dist + 6f) / 22f));
                return new Color(1f, 1f, 1f, a);
            });
            shadow.Save("shadow");

            var up = new Painter(96, 96, 2); // "upgrade" arrow
            up.Box(48, 30, 11, 22, 8, Hex("6fbf8a"));
            up.Blob(48, 62, 30, 9, 0, Hex("6fbf8a"), 0);
            up.Box(34, 58, 6, 20, 6, Hex("6fbf8a")); up.Box(62, 58, 6, 20, 6, Hex("6fbf8a"));
            up.Blob(48, 70, 24, 20, 0, Hex("6fbf8a"));
            up.Box(48, 40, 10, 24, 7, Hex("6fbf8a"), 0);
            up.Save("icon_up");

            var charm = new Painter(128, 128, 2); // charms: a moon with a star
            charm.Blob(60, 64, 44, 44, 0, Hex("ffd98a"));
            charm.Ellipse(80, 76, 36, 36, 0, Hex("fff4e0"));
            charm.Ellipse(80, 76, 33, 33, 0, new Color(0, 0, 0, 0));
            charm.Blob(92, 84, 13, 5, 0, Hex("b48be8"), 2.6f); charm.Blob(92, 84, 5, 13, 0, Hex("b48be8"), 0);
            charm.Blob(92, 84, 9, 4, 45, Hex("b48be8"), 0); charm.Blob(92, 84, 9, 4, -45, Hex("b48be8"), 0);
            charm.Ellipse(92, 84, 4, 4, 0, Color.white);
            charm.Save("icon_charm");

            var quest = new Painter(128, 128, 2); // quests: a note with ticks
            quest.Box(64, 62, 36, 46, 12, Hex("fff4e0"));
            quest.Box(64, 104, 20, 8, 6, Hex("f27d6b"));
            for (int k = 0; k < 3; k++)
            {
                float y = 82 - k * 24;
                quest.Blob(44, y, 7, 7, 0, k < 2 ? Hex("6fbf8a") : Hex("d9cdb4"), 2.4f);
                quest.Box(74, y, 18, 4, 3, Hex("c9b99a"), 0);
            }
            quest.Save("icon_quest");

            // White so it can be tinted: a ring of teeth around a hub.
            var gear = new Painter(128, 128);
            for (int k = 0; k < 8; k++)
            {
                float a = k * Mathf.PI / 4f;
                gear.Ellipse(64 + Mathf.Cos(a) * 46, 64 + Mathf.Sin(a) * 46, 12, 12, 0, Color.white);
            }
            gear.Ellipse(64, 64, 44, 44, 0, Color.white);
            gear.Save("gear");
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

            p = new Painter(128, 128, 2); // ấm trà sương
            p.Blob(100, 62, 20, 9, 35, Hex("6fb7b0"));
            p.Blob(60, 52, 40, 32, 0, Hex("6fb7b0"));
            p.Ellipse(46, 62, 12, 7, 30, Hex("a6dcd5"));
            p.Box(60, 88, 22, 6, 5, Hex("4f9790"));
            p.Blob(60, 100, 7, 7, 0, Hex("4f9790"));
            p.Ellipse(104, 96, 7, 7, 0, new Color(1, 1, 1, 0.5f)); p.Ellipse(110, 112, 9, 9, 0, new Color(1, 1, 1, 0.3f));
            p.Save("prop_am-tra");

            p = new Painter(128, 128, 2); // giỏ lá trà
            p.Blob(44, 84, 18, 9, 30, Hex("6fcf8a")); p.Blob(66, 92, 18, 9, -10, Hex("8adf9c")); p.Blob(86, 84, 18, 9, -40, Hex("6fcf8a"));
            p.Box(64, 44, 40, 28, 12, Hex("b97a45"));
            p.RoundRect(64, 50, 38, 2.5f, 2, Hex("8a5830")); p.RoundRect(64, 36, 36, 2.5f, 2, Hex("8a5830"));
            p.Box(64, 70, 46, 7, 6, Hex("dba56c"));
            p.Save("prop_gio-la");

            p = new Painter(128, 128, 2); // nồi mứt dâu
            p.Box(14, 60, 8, 6, 5, Hex("b23a58")); p.Box(114, 60, 8, 6, 5, Hex("b23a58"));
            p.Box(64, 48, 44, 32, 16, Hex("e0577a"));
            p.RoundRect(36, 48, 5, 16, 5, Hex("f08aa3"));
            p.Box(64, 80, 48, 7, 6, Hex("f6a3b8"));
            p.Blob(46, 100, 6, 6, 0, Hex("ffc2d1"), 3); p.Blob(70, 108, 8, 8, 0, Hex("ffc2d1"), 3); p.Blob(90, 98, 5, 5, 0, Hex("ffc2d1"), 3);
            p.Save("prop_noi-mut");

            p = new Painter(128, 128, 2); // cối giã hoa
            p.Blob(78, 88, 8, 34, -28, Hex("f0e2ba"));
            p.Box(64, 16, 22, 7, 6, Hex("8088b8"));
            p.Blob(64, 50, 44, 30, 0, Hex("a3abd8"));
            p.Ellipse(64, 68, 36, 10, 0, Hex("7078a8"));
            p.Blob(46, 70, 7, 7, 0, Hex("f3a6d6"), 3); p.Blob(62, 73, 6, 6, 0, Hex("ffd0ea"), 3);
            p.Save("prop_coi-hoa");

            p = new Painter(128, 128, 2); // lò bánh hạt dẻ
            p.Box(92, 98, 9, 18, 4, Hex("7d7399"));
            p.Blob(64, 30, 50, 62, 0, Hex("9d93ad"));
            p.RoundRect(64, 2, 64, 12, 0, new Color(0, 0, 0, 0));
            p.Ellipse(40, 66, 8, 5, -30, Hex("b5abc4")); p.Ellipse(90, 56, 7, 4, 30, Hex("b5abc4"));
            p.Blob(64, 26, 26, 30, 0, Hex("2a2140"), 4);
            p.Ellipse(64, 22, 19, 21, 0, Hex("ff9b4a")); p.Ellipse(64, 18, 10, 12, 0, Hex("ffe08a"));
            p.Save("prop_lo-banh");

            p = new Painter(128, 128, 2); // tổ mật ong
            p.Blob(64, 28, 40, 16, 0, Hex("f0a93a")); p.Blob(64, 52, 46, 17, 0, Hex("f5b94c"));
            p.Blob(64, 76, 40, 16, 0, Hex("f0a93a")); p.Blob(64, 98, 26, 13, 0, Hex("f5b94c"));
            p.Blob(64, 44, 11, 13, 0, Hex("2a2140"), 0);
            p.Ellipse(48, 82, 12, 4, 0, Hex("ffe2a3"));
            p.Save("prop_to-mat");

            p = new Painter(128, 128, 2); // quầy trà sữa trăng
            p.Blob(78, 96, 4, 28, -14, Hex("f27d6b"), 3);
            p.Box(64, 50, 30, 40, 10, new Color(1, 1, 1, 0.35f));
            p.RoundRect(64, 40, 27, 28, 8, Hex("e0b891"));
            p.Ellipse(50, 20, 5, 5, 0, Hex("2a2140")); p.Ellipse(64, 18, 5, 5, 0, Hex("2a2140")); p.Ellipse(78, 20, 5, 5, 0, Hex("2a2140"));
            p.Ellipse(57, 30, 5, 5, 0, Hex("2a2140")); p.Ellipse(72, 30, 5, 5, 0, Hex("2a2140"));
            p.Box(64, 92, 36, 6, 5, Hex("8fb4ff"));
            p.Save("prop_tra-sua");

            p = new Painter(128, 128, 2); // bình ủ sao
            p.Box(64, 12, 28, 7, 6, Hex("6a5a8c")); p.Box(64, 24, 8, 8, 3, Hex("6a5a8c"));
            p.Ellipse(64, 70, 56, 56, 0, Hex("ffd98a", 0.25f));
            p.Blob(64, 70, 40, 40, 0, new Color(1, 1, 1, 0.22f));
            p.Ellipse(64, 70, 24, 7, 0, Hex("ffe9a8")); p.Ellipse(64, 70, 7, 24, 0, Hex("ffe9a8"));
            p.Ellipse(64, 70, 17, 5, 45, Hex("ffe9a8")); p.Ellipse(64, 70, 17, 5, -45, Hex("ffe9a8"));
            p.Ellipse(64, 70, 8, 8, 0, Color.white);
            p.Ellipse(46, 90, 10, 5, 35, new Color(1, 1, 1, 0.6f));
            p.Save("prop_binh-sao");

            p = new Painter(128, 128, 2); // giàn nho trăng
            p.Box(30, 56, 5, 50, 4, Hex("8a5830")); p.Box(98, 56, 5, 50, 4, Hex("8a5830")); p.Box(64, 104, 46, 5, 4, Hex("a8703f"));
            p.Blob(52, 84, 10, 10, 0, Hex("b48be8"), 3); p.Blob(66, 78, 10, 10, 0, Hex("9a6fd6"), 3); p.Blob(78, 86, 10, 10, 0, Hex("b48be8"), 3);
            p.Blob(58, 68, 10, 10, 0, Hex("9a6fd6"), 3); p.Blob(72, 66, 10, 10, 0, Hex("b48be8"), 3); p.Blob(65, 54, 10, 10, 0, Hex("9a6fd6"), 3);
            p.Blob(84, 100, 12, 6, -30, Hex("6fcf8a"), 3);
            p.Save("prop_gian-nho");

            p = new Painter(128, 128, 2); // tiệm bánh trung thu
            p.Box(64, 20, 50, 8, 6, Hex("b97a45"));
            p.Blob(64, 58, 40, 32, 0, Hex("d9963f"));
            p.Ellipse(64, 60, 28, 21, 0, Hex("f0b860"));
            p.Ellipse(64, 60, 13, 3, 0, Hex("b9742c")); p.Ellipse(64, 60, 3, 13, 0, Hex("b9742c"));
            p.Ellipse(64, 60, 10, 3, 45, Hex("b9742c")); p.Ellipse(64, 60, 10, 3, -45, Hex("b9742c"));
            p.Save("prop_tiem-banh");

            p = new Painter(128, 128, 2); // suối khoáng hoa
            p.Blob(64, 30, 54, 22, 0, Hex("8a93b8"));
            p.Ellipse(64, 34, 44, 14, 0, Hex("7fc4f0")); p.Ellipse(52, 37, 16, 4, 0, Hex("c8ecff"));
            p.Blob(40, 44, 7, 7, 0, Hex("f3a6d6"), 3); p.Blob(86, 40, 6, 6, 0, Hex("ffd0ea"), 3);
            p.Ellipse(64, 70, 5, 5, 0, new Color(1, 1, 1, 0.55f)); p.Ellipse(74, 88, 7, 7, 0, new Color(1, 1, 1, 0.4f)); p.Ellipse(58, 106, 9, 9, 0, new Color(1, 1, 1, 0.25f));
            p.Save("prop_suoi-hoa");

            p = new Painter(128, 128, 2); // đài ngắm sao
            p.Box(64, 16, 34, 8, 6, Hex("6a5a8c")); p.Box(50, 44, 4, 26, 3, Hex("6a5a8c")); p.Box(78, 44, 4, 26, 3, Hex("6a5a8c"));
            p.Blob(72, 82, 14, 36, -40, Hex("8fb4ff"));
            p.Blob(96, 104, 12, 12, 0, Hex("c8dcff"), 4);
            p.Ellipse(98, 106, 5, 5, 0, Color.white);
            p.Ellipse(28, 108, 5, 5, 0, Hex("ffe9a8")); p.Ellipse(42, 94, 3, 3, 0, Hex("ffe9a8"));
            p.Save("prop_dai-sao");
        }

        /// <summary>Things that stand along the street between stations.</summary>
        static void Decor()
        {
            Painter p;

            p = new Painter(200, 240, 2); // round storybook tree
            p.Box(100, 46, 13, 46, 8, Hex("8a5a3a"));
            p.Blob(64, 130, 50, 44, 0, Hex("3f8a6c")); p.Blob(138, 126, 50, 46, 0, Hex("3f8a6c"));
            p.Blob(100, 176, 58, 50, 0, Hex("57a37f"));
            p.Ellipse(82, 192, 24, 14, -20, Hex("7cc49a")); p.Ellipse(124, 150, 16, 9, 20, Hex("7cc49a"));
            p.Ellipse(60, 120, 5, 5, 0, Hex("ffc866")); p.Ellipse(140, 108, 5, 5, 0, Hex("f3a6d6")); p.Ellipse(112, 198, 5, 5, 0, Hex("ffc866"));
            p.Save("decor_tree");

            p = new Painter(200, 120, 2); // flowering bush
            p.Blob(60, 44, 46, 36, 0, Hex("3f8a6c")); p.Blob(140, 42, 46, 34, 0, Hex("3f8a6c")); p.Blob(100, 60, 54, 44, 0, Hex("57a37f"));
            p.Ellipse(86, 76, 20, 11, -15, Hex("7cc49a"));
            foreach (var f in new[] { new Vector2(50, 56), new Vector2(104, 86), new Vector2(150, 54), new Vector2(124, 40), new Vector2(72, 30) })
            {
                p.Blob(f.x, f.y, 7, 7, 0, Hex("f3a6d6"), 2);
                p.Ellipse(f.x, f.y, 2.5f, 2.5f, 0, Hex("fff3c8"));
            }
            p.Save("decor_bush");

            p = new Painter(120, 240, 2); // paper lantern on a post
            p.Box(46, 100, 6, 100, 5, Hex("5a4660"));
            p.Box(66, 196, 26, 5, 4, Hex("5a4660"));
            p.Ellipse(86, 150, 40, 44, 0, Hex("ffc866", 0.28f));
            p.Blob(86, 150, 22, 30, 0, Hex("ffd98a"));
            p.Ellipse(86, 150, 22, 3, 0, Hex("e0a54a")); p.Ellipse(86, 164, 19, 3, 0, Hex("e0a54a")); p.Ellipse(86, 136, 19, 3, 0, Hex("e0a54a"));
            p.Ellipse(80, 158, 6, 9, 0, Hex("fff8dc"));
            p.Box(86, 184, 10, 4, 3, Hex("5a4660"), 3); p.Box(86, 117, 8, 4, 3, Hex("5a4660"), 3);
            p.Save("decor_lantern");

            p = new Painter(240, 110, 2); // low picket fence
            for (int k = 0; k < 5; k++) p.Box(30 + k * 45, 48, 11, 40, 9, Hex("e6d2a8"));
            p.Box(120, 62, 112, 6, 5, Hex("d3b98a")); p.Box(120, 30, 112, 6, 5, Hex("d3b98a"));
            p.Save("decor_fence");

            p = new Painter(150, 120, 2); // mushroom cluster
            p.Box(52, 30, 11, 26, 9, Hex("fff1d2")); p.Blob(52, 62, 40, 26, 0, Hex("f27d6b"));
            p.Ellipse(38, 68, 7, 6, 0, Hex("fff1d2")); p.Ellipse(62, 74, 6, 5, 0, Hex("fff1d2")); p.Ellipse(72, 58, 5, 4, 0, Hex("fff1d2"));
            p.Box(108, 20, 8, 17, 7, Hex("fff1d2")); p.Blob(108, 42, 26, 17, 0, Hex("c58be0"));
            p.Ellipse(100, 46, 5, 4, 0, Hex("fff1d2")); p.Ellipse(116, 44, 4, 3, 0, Hex("fff1d2"));
            p.Save("decor_mushrooms");

            p = new Painter(160, 90, 2); // mossy stones
            p.Blob(58, 32, 44, 26, 0, Hex("8a93b8")); p.Blob(112, 24, 30, 18, 0, Hex("7880a8"));
            p.Ellipse(46, 44, 18, 8, -10, Hex("a9b2d4")); p.Ellipse(62, 54, 22, 6, 0, Hex("6fbf8a"));
            p.Save("decor_stones");
        }
    }
}
