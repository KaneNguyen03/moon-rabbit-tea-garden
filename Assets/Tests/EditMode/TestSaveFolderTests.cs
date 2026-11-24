using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using VuonTra.Game;

namespace VuonTra.Core.Tests
{
    public class TestSaveFolderTests
    {
        static string Here => TestSaveFolder.For(TestContext.CurrentContext.Test.FullName);

        static string PlayerSave => Path.Combine(Application.persistentDataPath, "save.json");

        static string Stamp(string path) => File.Exists(path) ? File.GetLastWriteTimeUtc(path).Ticks + " " + File.ReadAllText(path).GetHashCode() : "none";

        [Test]
        public void A_Test_Saves_In_A_Cache_Folder_Of_Its_Own_And_Never_In_The_Players_Folder()
        {
            Assert.AreEqual(Here, new SaveStore().Folder);
            StringAssert.StartsWith(TestSaveFolder.Root, Here);
            StringAssert.DoesNotStartWith(Application.persistentDataPath, Here);
            StringAssert.Contains(nameof(A_Test_Saves_In_A_Cache_Folder_Of_Its_Own_And_Never_In_The_Players_Folder), Here);
        }

        [Test]
        public void A_Runner_With_No_Folder_Saves_In_The_Tests_Folder_And_Leaves_The_Players_Save_Alone()
        {
            Assert.AreEqual(Here, SaveStore.DefaultDirectory(), "tests are routed away from the player's folder before any runner starts");
            string before = Stamp(PlayerSave);
            var go = new GameObject("Runner");
            try
            {
                var runner = go.AddComponent<GameRunner>();
                runner.Catalog = AssetDatabase.LoadAssetAtPath<GameCatalog>("Assets/VuonTra/Content/GameCatalog.asset");
                runner.Init();
                runner.State.Levels[0] = 4;
                runner.Save();
                Assert.AreEqual(Here, runner.SaveFolder);
                Assert.IsTrue(File.Exists(Path.Combine(Here, "save.json")));
                Assert.AreEqual(before, Stamp(PlayerSave), "the player's own garden is not touched");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
