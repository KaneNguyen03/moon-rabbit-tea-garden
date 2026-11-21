using System.IO;
using UnityEditor;
using UnityEngine;

namespace VuonTra.EditorTools
{
    /// <summary>
    /// Hand-made or generated art that replaces a placeholder. Drop a PNG with the placeholder's
    /// name into <see cref="Folder"/> and the next seed uses it; nothing here is ever overwritten.
    /// </summary>
    public static class FinalArt
    {
        public const string Folder = "Assets/VuonTra/Art/Final";

        /// <summary>Gives every PNG in the folder the same import settings the placeholders get.</summary>
        public static void Import()
        {
            if (!Directory.Exists(Folder)) return;
            AssetDatabase.Refresh();
            foreach (string file in Directory.GetFiles(Folder, "*.png"))
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
                importer.textureCompression = TextureImporterCompression.Uncompressed; // keeps soft outlines clean
                importer.SaveAndReimport();
            }
        }

        /// <summary>The final sprite with this name, or null when only a placeholder exists.</summary>
        public static Sprite Find(string name)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(Folder + "/" + name + ".png");
        }

        /// <summary>The final sprite when there is one, the placeholder otherwise.</summary>
        public static Sprite Load(string name)
        {
            Sprite final = Find(name);
            return final != null ? final : PlaceholderArt.Load(name);
        }
    }
}
