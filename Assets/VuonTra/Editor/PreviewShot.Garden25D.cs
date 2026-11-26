using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using VuonTra.Core;
using VuonTra.Game;
using VuonTra.Game.UI;
using L10n = VuonTra.Game.L10n;
using Motion = VuonTra.Game.UI.Motion;

namespace VuonTra.EditorTools
{
    public static partial class PreviewShot
    {
        public const string IslandFolder = "docs/anh/po-145";

        [MenuItem("VuonTra/Render 2.5D Garden Before And After (#145)")]
        public static void RenderGarden25D()
        {
            Directory.CreateDirectory(IslandFolder);
            bool reduced = Motion.Reduced;
            try
            {
                Motion.Reduced = false;
                ShootGarden(false, "vi", false, false, "145-truoc");
                ShootGarden(true, "vi", false, false, "145-sau");
                ShootGarden(true, "en", true, false, "145-sau-en-chu-to");
                ShootGarden(true, "vi", false, true, "145-sau-ban-do");
            }
            finally
            {
                Features.Garden25D = Features.Garden25DByDefault;
                Motion.Reduced = reduced;
                UiKit.SetLargeText(false);
                L10n.SetLocale("vi");
            }
        }

        static void ShootGarden(bool island, string code, bool large, bool map, string name, string folder = IslandFolder, int near = -1, bool staffed = false, System.Action<GameRunner, GardenScreen, Camera, RenderTexture> film = null)
        {
            EditorSceneManager.OpenScene(ContentSeeder.ScenePath, OpenSceneMode.Single);
            Features.Garden25D = island;
            L10n.SetLocale(code);
            UiKit.SetLargeText(large);
            var runner = Object.FindAnyObjectByType<GameRunner>();
            var screen = Object.FindAnyObjectByType<GardenScreen>();
            GameState state = GameState.NewGame(runner.Catalog.BuildDefs());
            int[] levels = { 27, 12, 9, 4, 2, 0 };
            for (int i = 0; i < levels.Length; i++) state.Levels[i] = levels[i];
            state.Plots = 6;
            if (staffed) Staff(state, runner.Catalog.BuildDefs());
            state.GardenXp = Garden.XpForLevel(5) + 90;
            state.HintSeen = true;
            state.TutorialStep = Tutorial.Steps;
            state.Coins = new BigNum(5.2e6);
            state.Energy = 73;
            state.IntrosSeen.UnionWith(Intros.All);
            state.IntrosTried.UnionWith(Intros.All);
            runner.Init(state);
            screen.Build();
            screen.ClockHours = () => 22.0;
            screen.Refresh();
            screen.Shell.StartGame();
            Step(screen, 1f);

            var rt = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
            Camera eye = island ? runner.Garden25D.Rig.View : screen.Cam;
            Canvas canvas = screen.transform.Find("Canvas").GetComponent<Canvas>();
            eye.targetTexture = rt;
            if (island)
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = eye;
                canvas.planeDistance = 1.5f;
            }
            Canvas.ForceUpdateCanvases();
            if (map) screen.IslandMap.Show();
            if (near >= 0) LookClose(screen, runner, near, island);
            Step(screen, 0.1f);
            if (film != null)
            {
                film(runner, screen, eye, rt);
                eye.targetTexture = null;
                rt.Release();
                Object.DestroyImmediate(rt);
                return;
            }
            Canvas.ForceUpdateCanvases();
            eye.Render();
            eye.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            eye.targetTexture = null;
            File.WriteAllBytes(Path.Combine(folder, name + ".png"), tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            if (map) screen.IslandMap.Close();
            rt.Release();
            Object.DestroyImmediate(rt);
        }
    }
}
