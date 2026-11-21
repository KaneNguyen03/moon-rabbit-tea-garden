using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using VuonTra.Core;
using VuonTra.Game;
using VuonTra.Game.UI;
using L10n = VuonTra.Game.L10n;

namespace VuonTra.EditorTools
{
    /// <summary>
    /// Renders the screen to PNGs without entering Play mode: title, the street, the busiest
    /// feedback moment, pause, and each tab of the sheet. These are the frames the look gets
    /// scored on.
    /// </summary>
    public static class PreviewShot
    {
        public const string Folder = "Logs/preview";
        const int Width = 1080, Height = 1920;

        [MenuItem("VuonTra/Render Preview")]
        public static void Render()
        {
            EditorSceneManager.OpenScene(ContentSeeder.ScenePath, OpenSceneMode.Single);
            L10n.SetLocale("vi");
            var runner = Object.FindAnyObjectByType<GameRunner>();
            var screen = Object.FindAnyObjectByType<GardenScreen>();

            // A mid-game state so open, sleeping and not-yet-bought stations all show.
            var defs = runner.Catalog.BuildDefs();
            GameState state = GameState.NewGame(defs);
            int[] levels = { 27, 12, 9, 4, 2, 0 };
            double[] progress = { 0.6, 0, 0.35, 0, 0.8, 0 };
            for (int i = 0; i < levels.Length; i++)
            {
                state.Levels[i] = levels[i];
                state.Progress[i] = progress[i];
            }
            state.Plots = 6;
            state.Coins = new BigNum(5.2e6);
            state.Energy = 73;
            runner.Init(state);

            screen.Build();
            screen.Refresh();
            if (Directory.Exists(Folder)) Directory.Delete(Folder, true);
            Directory.CreateDirectory(Folder);

            Step(screen, 0.4f);
            Capture(screen, "1-title");

            screen.Shell.StartGame();
            Step(screen, 1.2f);
            Capture(screen, "2-garden");

            // Busy frame: open the next station so the milestone moment is on screen.
            screen.FocusStation(5);
            Step(screen, 0.4f);
            Capture(screen, "2b-card");
            Press(screen, "Canvas/Garden/Content/Card/Buy");
            Step(screen, 0.45f);
            Capture(screen, "3-moment");
            Step(screen, 2.5f);

            // Rabbits and items for the remaining frames.
            string[] names = { "Bông", "Mây", "Sữa", "Mochi", "Đậu", "Kem", "Nếp", "Mật" };
            int[] tiers = { 5, 4, 3, 2, 1, 0, 0, 0 };
            for (int i = 0; i < tiers.Length; i++)
                state.Rabbits.Add(new Rabbit { Id = ++state.NextRabbitId, Tier = tiers[i], Name = names[i], Level = 1 + i * 2 });
            state.Clovers = 4;
            Rabbits.AutoAssign(runner.Defs, state);
            var itemDefs = runner.Catalog.BuildItems();
            for (int i = 0; i < itemDefs.Count; i++) Items.Grant(state, itemDefs[i], (i * 2) % Rabbits.TierCount);
            Items.Equip(state, state.Items[0].Id, state.Rabbits[0].Id);
            Items.Equip(state, state.Items[3].Id, state.Rabbits[0].Id);
            state.TotalTaps = 37;
            runner.BuyUpgrade(0); // also refreshes the modifiers

            Press(screen, "Canvas/Hud/Energy");
            Step(screen, 2.4f);
            Capture(screen, "4-boost");

            screen.OpenSheet(0);
            Step(screen, 0.5f);
            Capture(screen, "5-stations");
            screen.SetTab(1);
            Step(screen, 0.2f);
            Capture(screen, "6-upgrades");
            screen.SetTab(2);
            Step(screen, 0.2f);
            Capture(screen, "7-quests");
            screen.SetTab(3);
            Step(screen, 0.2f);
            Capture(screen, "8-rabbits");

            screen.Picker.Open(state, state.Rabbits[0].Id, ItemSlot.Hat);
            Step(screen, 0.4f);
            Capture(screen, "9-items");
            screen.Picker.Close();
            screen.CloseSheet();

            screen.Shell.OpenPause();
            Step(screen, 0.6f);
            Capture(screen, "10-pause");

            // The same screens in English.
            screen.Shell.Resume();
            Step(screen, 0.6f);
            L10n.SetLocale("en");
            screen.RebuildUi();
            Step(screen, 0.8f);
            Capture(screen, "11-garden-en");
            screen.OpenSheet(3);
            Step(screen, 0.5f);
            Capture(screen, "12-rabbits-en");
            screen.SetTab(2);
            Step(screen, 0.2f);
            Capture(screen, "13-quests-en");
            screen.CloseSheet();
            screen.Shell.OpenPause();
            Step(screen, 0.6f);
            Capture(screen, "14-pause-en");
            L10n.SetLocale("vi");

            Debug.Log("[VuonTra] Previews written to " + Folder);
            // The scene now holds preview objects; replace it so nothing gets saved by accident.
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        static void Press(GardenScreen screen, string path)
        {
            Transform t = screen.transform.Find(path);
            if (t == null) throw new System.Exception("Preview: no UI object at " + path);
            t.GetComponent<Button>().onClick.Invoke();
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
