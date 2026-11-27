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
        public void The_Two_And_A_Half_D_Billboards_Import_Bilinear_Without_Mipmaps_And_Flat_Pieces_Trilinear_With_Them_Uncompressed()
        {
            int count = 0;
            foreach (string folder in new[] { BillboardArt.StandFolder, BillboardArt.FlatFolder })
                foreach (string file in Files(folder))
                {
                    var importer = (TextureImporter)AssetImporter.GetAtPath(file.Replace('\\', '/'));
                    Assert.AreEqual(TextureImporterType.Sprite, importer.textureType, file);
                    Assert.AreEqual(SpriteImportMode.Single, importer.spriteImportMode, file);
                    bool standing = folder == BillboardArt.StandFolder;
                    Assert.AreEqual(!standing, importer.mipmapEnabled, file + ": art bible §11, standing cards have no mipmaps, flat ground pieces seen at a slant keep them");
                    Assert.AreEqual(standing ? FilterMode.Bilinear : FilterMode.Trilinear, importer.filterMode, file + ": standing cards bilinear (the trilinear render left a sawtooth rim on the stall, 146-vien-loc.png), flat ground pieces trilinear");
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
                Assert.IsNotNull(sprite, file + " imports as a sprite");
                Vector2 foot = BillboardArt.FootPivot(file);
                Assert.AreEqual(foot.x, sprite.pivot.x / sprite.rect.width, 1e-3, file + " pivots across on the middle of its feet");
                Assert.AreEqual(foot.y, sprite.pivot.y / sprite.rect.height, 1e-3, file);
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
        public void A_Lamp_Post_Standing_On_One_Side_Of_Its_Picture_Pivots_Under_The_Post_Not_The_Middle()
        {
            string file = Path.Combine(Application.temporaryCachePath, "post.png");
            var image = new Texture2D(10, 20, TextureFormat.RGBA32, false);
            var px = new Color32[200];
            for (int y = 0; y < 20; y++) px[y * 10 + 2] = new Color32(0, 0, 0, 255);
            for (int y = 12; y < 18; y++)
                for (int x = 2; x < 10; x++) px[y * 10 + x] = new Color32(0, 0, 0, 255);
            image.SetPixels32(px);
            File.WriteAllBytes(file, image.EncodeToPNG());
            Object.DestroyImmediate(image);
            try
            {
                Assert.AreEqual(0.25f, BillboardArt.FootPivot(file).x, 1e-6, "the post's foot, at x = 2.5 of 10, not the lantern that hangs to the right");
                Assert.AreEqual(0f, BillboardArt.FootPivot(file).y, 1e-6);
            }
            finally
            {
                File.Delete(file);
            }
        }

        [Test]
        public void Two_Feet_Of_Different_Widths_Pivot_Midway_Between_Their_Outer_Columns_Not_On_Their_Weighted_Mean()
        {
            string file = Path.Combine(Application.temporaryCachePath, "two-feet.png");
            var image = new Texture2D(10, 20, TextureFormat.RGBA32, false);
            var px = new Color32[200];
            for (int y = 0; y < 20; y++)
            {
                px[y * 10 + 1] = new Color32(0, 0, 0, 255);
                for (int x = 6; x < 9; x++) px[y * 10 + x] = new Color32(0, 0, 0, 255);
            }
            image.SetPixels32(px);
            File.WriteAllBytes(file, image.EncodeToPNG());
            Object.DestroyImmediate(image);
            try
            {
                Assert.AreEqual(0.5f, BillboardArt.FootPivot(file).x, 1e-6, "art bible §10: the middle of the opaque columns in the bottom band, columns 1 to 8 of 10, not the mean 0.575 that a thick leg drags right");
            }
            finally
            {
                File.Delete(file);
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
                Assert.AreEqual(0.5f, BillboardArt.FootPivot(file).x, 1e-6, "a full-width foot row is centred");
            }
            finally
            {
                File.Delete(file);
            }
        }
    }
}
