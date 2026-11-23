using System.IO;
using UnityEditor;
using UnityEngine;

namespace VuonTra.EditorTools
{
    public static class FinalArt
    {
        public const string Folder = "Assets/VuonTra/Art/Final";

        public const string Generated = "Assets/VuonTra/Art/Generated";

        static readonly Color32 Ink = new Color32(0x3A, 0x2A, 0x4D, 255);

        public static void Import()
        {
            if (!Directory.Exists(Folder)) return;
            AssetDatabase.Refresh();
            OutlineKinds();
            ImportFolder(Folder);
            ImportFolder(Generated);
        }

        static void OutlineKinds()
        {
            Directory.CreateDirectory(Generated);
            const float radius = 2.2f;
            int reach = Mathf.CeilToInt(radius);
            foreach (string file in Directory.GetFiles(Folder, "kind_*.png"))
            {
                string id = Path.GetFileNameWithoutExtension(file).Substring("kind_".Length);
                VuonTra.Core.RabbitKind kind = VuonTra.Core.RabbitKinds.Find(id);
                if (kind != null && (kind.OwnGlow || kind.OwnOutline))
                {
                    AssetDatabase.DeleteAsset(Generated + "/" + Path.GetFileName(file));
                    continue;
                }
                var source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                source.LoadImage(File.ReadAllBytes(file));
                int w = source.width, h = source.height;
                Color32[] px = source.GetPixels32();
                var result = new Color32[px.Length];
                for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    int near = 0;
                    for (int dy = -reach; dy <= reach; dy++)
                    for (int dx = -reach; dx <= reach; dx++)
                    {
                        if (dx * dx + dy * dy > radius * radius) continue;
                        int nx = x + dx, ny = y + dy;
                        if (nx < 0 || ny < 0 || nx >= w || ny >= h) continue;
                        int a = px[ny * w + nx].a;
                        if (a > near) near = a;
                    }
                    Color32 own = px[y * w + x];
                    float t = own.a / 255f;
                    result[y * w + x] = new Color32(
                        (byte)(own.r * t + Ink.r * (1f - t)), (byte)(own.g * t + Ink.g * (1f - t)), (byte)(own.b * t + Ink.b * (1f - t)),
                        (byte)Mathf.Max(own.a, near));
                }
                var outlined = new Texture2D(w, h, TextureFormat.RGBA32, false);
                outlined.SetPixels32(result);
                outlined.Apply();
                File.WriteAllBytes(Path.Combine(Generated, Path.GetFileName(file)), outlined.EncodeToPNG());
                Object.DestroyImmediate(source);
                Object.DestroyImmediate(outlined);
            }
            AssetDatabase.Refresh();
        }

        public static Sprite Kind(string id)
        {
            Sprite outlined = AssetDatabase.LoadAssetAtPath<Sprite>(Generated + "/kind_" + id + ".png");
            return outlined != null ? outlined : Find("kind_" + id);
        }

        static void ImportFolder(string folder)
        {
            if (!Directory.Exists(folder)) return;
            foreach (string file in Directory.GetFiles(folder, "*.png"))
            {
                var importer = AssetImporter.GetAtPath(file.Replace('\\', '/')) as TextureImporter;
                if (importer == null) continue;
                bool ok = importer.textureType == TextureImporterType.Sprite
                    && importer.spriteImportMode == SpriteImportMode.Single
                    && importer.alphaIsTransparency
                    && !importer.mipmapEnabled
                    && importer.textureCompression == TextureImporterCompression.Uncompressed;
                if (ok) continue;
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.spritePixelsPerUnit = 100;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
        }

        public static Sprite Find(string name)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(Folder + "/" + name + ".png");
        }

        public static Sprite Load(string name)
        {
            Sprite final = Find(name);
            return final != null ? final : PlaceholderArt.Load(name);
        }
    }
}
