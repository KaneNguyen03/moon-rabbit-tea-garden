using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using VuonTra.Core;
using VuonTra.Game;
using VuonTra.Game.UI;

namespace VuonTra.EditorTools
{
    /// <summary>
    /// Renders the screen to PNGs without entering Play mode: the title, normal play, the busiest
    /// feedback moment and the pause card. These are the frames the look gets scored on.
    /// </summary>
    public static class PreviewShot
    {
        public const string Folder = "Logs/preview";
        const int Width = 1080, Height = 1920;

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
            state.Coins = new BigNum(5.2e6);
            runner.Init(state);

            screen.Build();
            screen.Refresh();
            Directory.CreateDirectory(Folder);

            Step(screen, 0.4f);
            Capture(screen, "1-title");

            screen.Shell.StartGame();
            Step(screen, 1.2f);
            Capture(screen, "2-play");

            // Busy frame: unlock the next station so the milestone moment is on screen.
            screen.transform.Find("Canvas/Panel/Scroll/Content/Row to-mat/Buy").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            Step(screen, 0.45f);
            Capture(screen, "3-moment");

            Step(screen, 2.5f);
            screen.Shell.OpenPause();
            Step(screen, 0.6f);
            Capture(screen, "4-pause");

            Debug.Log("[VuonTra] Previews written to " + Folder);
            // The scene now holds preview objects; replace it so nothing gets saved by accident.
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        static void Step(GardenScreen screen, float seconds)
        {
            for (float t = 0f; t < seconds; t += 1f / 30f) screen.Advance(1f / 30f);
        }

        static void Capture(GardenScreen screen, string name)
        {
            var rt = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
            Camera cam = screen.Cam;
            cam.targetTexture = rt;
            Canvas.ForceUpdateCanvases();
            cam.Render(); // warm-up frame
            cam.Render();

            RenderTexture.active = rt;
            var tex = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            cam.targetTexture = null;
            File.WriteAllBytes(Path.Combine(Folder, name + ".png"), tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            rt.Release();
        }
    }
}
