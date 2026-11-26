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

        public static float FootShare(string file)
        {
            var source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            source.LoadImage(File.ReadAllBytes(file));
            int w = source.width, h = source.height;
            Color32[] px = source.GetPixels32();
            Object.DestroyImmediate(source);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    if (px[y * w + x].a >= FootAlpha) return (float)y / h;
            return 0f;
        }

        static void ImportFolder(string folder, bool standing)
        {
            if (!Directory.Exists(folder)) return;
            foreach (string path in Directory.GetFiles(folder, "*.png"))
            {
                string file = path.Replace('\\', '/');
                var importer = AssetImporter.GetAtPath(file) as TextureImporter;
                if (importer == null) continue;
                var pivot = new Vector2(0.5f, standing ? FootShare(file) : 0.5f);
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                bool ok = importer.textureType == TextureImporterType.Sprite
                    && importer.spriteImportMode == SpriteImportMode.Single
                    && importer.alphaIsTransparency
                    && importer.mipmapEnabled
                    && importer.filterMode == FilterMode.Trilinear
                    && importer.textureCompression == TextureImporterCompression.Uncompressed
                    && settings.spriteMeshType == SpriteMeshType.Tight
                    && settings.spriteAlignment == (int)SpriteAlignment.Custom
                    && Vector2.Distance(settings.spritePivot, pivot) < 1e-4f;
                if (ok) continue;
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = true;
                importer.filterMode = FilterMode.Trilinear;
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
