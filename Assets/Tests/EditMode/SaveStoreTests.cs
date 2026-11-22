using System;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using VuonTra.Game;

namespace VuonTra.Core.Tests
{
    /// <summary>Where the save lives when the scene names no folder (#70).</summary>
    public class SaveStoreTests
    {
        string dir;
        Func<string> before;

        [SetUp]
        public void SetUp()
        {
            dir = Path.Combine(Path.GetTempPath(), "vuontra-save-" + Guid.NewGuid().ToString("N"));
            before = SaveStore.DefaultDirectory;
            // Never the real persistentDataPath: the editor shares it with the Mac build's save.
            SaveStore.DefaultDirectory = () => dir;
        }

        [TearDown]
        public void TearDown()
        {
            SaveStore.DefaultDirectory = before;
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void A_Folder_That_Is_Not_Set_Means_The_Platforms_Data_Folder(string folder)
        {
            Assert.AreEqual(dir, new SaveStore(folder).Folder);
        }

        [Test]
        public void A_Named_Folder_Is_Used_As_It_Is()
        {
            string other = Path.Combine(dir, "named");
            Assert.AreEqual(other, new SaveStore(other).Folder);
        }

        [Test]
        public void A_Store_With_A_Blank_Folder_Saves_And_Reads_Back()
        {
            var defs = Fixture.Defs();
            GameState s = GameState.NewGame(defs);
            s.Levels[0] = 7;
            Assert.IsTrue(new SaveStore("").Save(defs, s), "an empty folder name no longer makes every save fail");
            Assert.IsTrue(File.Exists(Path.Combine(dir, "save.json")), "the save is in the data folder, not the working directory");
            LoadResult back = new SaveStore("").Load(defs);
            Assert.AreEqual(LoadStatus.Ok, back.Status);
            Assert.AreEqual(7, back.State.Levels[0]);
        }

        [Test]
        public void A_Runner_From_The_Scene_With_No_Folder_Keeps_The_Garden_Across_A_Restart()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<GameCatalog>("Assets/VuonTra/Content/GameCatalog.asset");
            var go = new GameObject("First");
            try
            {
                var first = go.AddComponent<GameRunner>();
                first.Catalog = catalog;
                first.SaveDirectory = ""; // what Garden.unity holds
                first.Init();
                Assert.AreEqual(dir, first.SaveFolder);
                first.State.Levels[0] = 9;
                first.Save();
                UnityEngine.Object.DestroyImmediate(go);

                go = new GameObject("Second");
                var second = go.AddComponent<GameRunner>();
                second.Catalog = catalog;
                second.SaveDirectory = "";
                second.Init();
                Assert.AreEqual(9, second.State.Levels[0], "the garden is the one just played");
            }
            finally
            {
                if (go != null) UnityEngine.Object.DestroyImmediate(go);
            }
        }
    }
}
