using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using VuonTra.Core;
using VuonTra.Game;
using VuonTra.Game.UI;

namespace VuonTra.EditorTools
{
    /// <summary>Renders the Garden screen to a PNG without entering Play mode, for a quick look.</summary>
    public static class PreviewShot
    {
        public const string OutputPath = "Logs/preview.png";

        [MenuItem("VuonTra/Render Preview")]
        public static void Render()
        {
            EditorSceneManager.OpenScene(ContentSeeder.ScenePath, OpenSceneMode.Single);
            var runner = Object.FindAnyObjectByType<GameRunner>();
            var screen = Object.FindAnyObjectByType<GardenScreen>();

            // A mid-game state so both open and sleeping stations show.
            var defs = runner.Catalog.BuildDefs();
            GameState state = GameState.NewGame(defs);
            state.Levels = new[] { 27, 12, 9, 4, 2, 0, 0, 0 };
            state.Progress = new[] { 0.6, 0, 0.35, 0, 0.8, 0, 0, 0 };
            state.Coins = new BigNum(48250);
            state.HintSeen = false;
            runner.Init(state);

            screen.Build();
            screen.Refresh();
            Canvas.ForceUpdateCanvases();

            const int w = 1080, h = 1920;
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
            Camera cam = screen.Cam;
            cam.targetTexture = rt;
            Canvas.ForceUpdateCanvases();
            cam.Render();
            cam.Render();

            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            cam.targetTexture = null;

            Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));
            File.WriteAllBytes(OutputPath, tex.EncodeToPNG());
            Debug.Log("[VuonTra] Preview written to " + OutputPath);
            // The scene now holds preview objects; reopen it so nothing gets saved by accident.
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }
    }
}
