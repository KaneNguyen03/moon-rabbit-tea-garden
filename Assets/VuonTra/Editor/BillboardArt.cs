using System.IO;
using UnityEditor;
using UnityEngine;

namespace VuonTra.EditorTools
{
    public static class BillboardArt
    {
        public const string Folder = "Assets/VuonTra/Art/Garden25D";
        public const string StandFolder = Folder + "/Stand", FlatFolder = Folder + "/Flat";
        public const byte FootAlpha = 128;

        public static void Import()
        {
            ImportFolder(StandFolder, true);
            ImportFolder(FlatFolder, false);
        }

        public const float FootBandShare = 0.03f;

        public static float FootShare(string file) => FootPivot(file).y;

        public static Vector2 FootPivot(string file)
        {
            var source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            source.LoadImage(File.ReadAllBytes(file));
            int w = source.width, h = source.height;
            Color32[] px = source.GetPixels32();
            Object.DestroyImmediate(source);
            int foot = -1;
            for (int y = 0; y < h && foot < 0; y++)
                for (int x = 0; x < w; x++)
                    if (px[y * w + x].a >= FootAlpha) { foot = y; break; }
            if (foot < 0) return new Vector2(0.5f, 0f);
            int band = Mathf.Max(2, Mathf.RoundToInt(h * FootBandShare));
            int left = w, right = -1;
            for (int y = foot; y < Mathf.Min(h, foot + band); y++)
                for (int x = 0; x < w; x++)
                    if (px[y * w + x].a >= FootAlpha)
                    {
                        left = Mathf.Min(left, x);
                        right = Mathf.Max(right, x);
                    }
            return new Vector2((left + right + 1) / 2f / w, (float)foot / h);
        }

        public static FilterMode FilterOf(bool standing) => standing ? FilterMode.Bilinear : FilterMode.Trilinear;

        public static bool MipmapsOf(bool standing) => !standing;

        static void ImportFolder(string folder, bool standing)
        {
            if (!Directory.Exists(folder)) return;
            foreach (string path in Directory.GetFiles(folder, "*.png"))
            {
                string file = path.Replace('\\', '/');
                var importer = AssetImporter.GetAtPath(file) as TextureImporter;
                if (importer == null) continue;
                Vector2 pivot = standing ? FootPivot(file) : new Vector2(0.5f, 0.5f);
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                bool ok = importer.textureType == TextureImporterType.Sprite
                    && importer.spriteImportMode == SpriteImportMode.Single
                    && importer.alphaIsTransparency
                    && importer.mipmapEnabled == MipmapsOf(standing)
                    && importer.filterMode == FilterOf(standing)
                    && importer.textureCompression == TextureImporterCompression.Uncompressed
                    && settings.spriteMeshType == SpriteMeshType.Tight
                    && settings.spriteAlignment == (int)SpriteAlignment.Custom
                    && Vector2.Distance(settings.spritePivot, pivot) < 1e-4f;
                if (ok) continue;
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = MipmapsOf(standing);
                importer.filterMode = FilterOf(standing);
                importer.spritePixelsPerUnit = 100;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.Tight;
                settings.spriteAlignment = (int)SpriteAlignment.Custom;
                settings.spritePivot = pivot;
                importer.SetTextureSettings(settings);
                importer.SaveAndReimport();
            }
        }
    }
}
