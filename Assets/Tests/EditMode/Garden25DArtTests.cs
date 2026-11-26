using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using VuonTra.EditorTools;
using VuonTra.Game;

namespace VuonTra.Core.Tests
{
    public class Garden25DArtTests
    {
        static string[] Files(string folder) => Directory.GetFiles(folder, "*.png");

        [Test]
        public void The_Two_And_A_Half_D_Billboards_Import_With_Mipmaps_And_Trilinear_Filtering_Uncompressed()
        {
            int count = 0;
            foreach (string folder in new[] { BillboardArt.StandFolder, BillboardArt.FlatFolder })
                foreach (string file in Files(folder))
                {
                    var importer = (TextureImporter)AssetImporter.GetAtPath(file.Replace('\\', '/'));
                    Assert.AreEqual(TextureImporterType.Sprite, importer.textureType, file);
                    Assert.AreEqual(SpriteImportMode.Single, importer.spriteImportMode, file);
                    Assert.IsTrue(importer.mipmapEnabled, file + " shrinks about three times at the usual zoom, so it needs mipmaps");
                    Assert.AreEqual(FilterMode.Trilinear, importer.filterMode, file);
                    Assert.AreEqual(TextureImporterCompression.Uncompressed, importer.textureCompression, file);
                    Assert.IsTrue(importer.alphaIsTransparency, file);
                    var settings = new TextureImporterSettings();
                    importer.ReadTextureSettings(settings);
                    Assert.AreEqual(SpriteMeshType.Tight, settings.spriteMeshType, file + " draws only its painted pixels, so its bounds start at its feet");
                    count++;
                }
            Assert.Greater(count, 10);
        }

        [Test]
        public void A_Standing_Billboard_Pivots_On_Its_Lowest_Row_That_Is_At_Least_Half_Opaque_And_A_Flat_One_On_Its_Centre()
        {
            foreach (string file in Files(BillboardArt.StandFolder))
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(file.Replace('\\', '/'));
                Assert.AreEqual(0.5f, sprite.pivot.x / sprite.rect.width, 1e-3, file);
                Assert.AreEqual(BillboardArt.FootShare(file), sprite.pivot.y / sprite.rect.height, 1e-3, file);
                Assert.AreEqual(0f, GardenStand.FootOf(sprite), file + " is placed by its pivot");
            }
            foreach (string file in Files(BillboardArt.FlatFolder))
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(file.Replace('\\', '/'));
                Assert.AreEqual(0.5f, sprite.pivot.y / sprite.rect.height, 1e-3, file);
            }
        }

        [Test]
        public void The_Two_D_Garden_Keeps_Its_Own_Import_Without_Mipmaps()
        {
            foreach (string name in new[] { "prop_tra-sua", "prop_am-tra", "pad_san-go", "decor_bon-hoa" })
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(FinalArt.Folder + "/" + name + ".png");
                Assert.IsFalse(importer.mipmapEnabled, name);
                Assert.AreNotEqual(FilterMode.Trilinear, importer.filterMode, name);
            }
        }

        [Test]
        public void The_Foot_Row_Is_The_Lowest_Row_With_Alpha_Of_At_Least_Half()
        {
            string file = Path.Combine(Application.temporaryCachePath, "foot-row.png");
            var image = new Texture2D(4, 10, TextureFormat.RGBA32, false);
            var px = new Color32[40];
            for (int x = 0; x < 4; x++)
            {
                px[1 * 4 + x] = new Color32(0, 0, 0, 100);
                px[3 * 4 + x] = new Color32(0, 0, 0, 200);
            }
            image.SetPixels32(px);
            File.WriteAllBytes(file, image.EncodeToPNG());
            Object.DestroyImmediate(image);
            try
            {
                Assert.AreEqual(0.3f, BillboardArt.FootShare(file), 1e-6, "a faint row under the feet does not count");
            }
            finally
            {
                File.Delete(file);
            }
        }
    }
}
