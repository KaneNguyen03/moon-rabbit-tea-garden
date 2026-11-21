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
            state.GardenXp = Garden.XpForLevel(5) + 90;
            state.HintSeen = true; // the first-run hint would cover the garden in every frame
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
            screen.SetZoom(GardenScreen.ZoomLevel.Far, false);
            Step(screen, 0.2f);
            Capture(screen, "z1-far");
            screen.SetZoom(GardenScreen.ZoomLevel.Default, false);
            screen.ZoomToStation(0, false);
            Step(screen, 0.2f);
            Capture(screen, "z3-near");
            screen.SetZoom(GardenScreen.ZoomLevel.Default, false);
            screen.LookAt(new Vector2(540f, 1350f));
            Step(screen, 0.2f);
            Capture(screen, "z2-default");
            screen.LookAt(new Vector2(540f, 2900f)); // the top of the garden
            Step(screen, 0.2f);
            Capture(screen, "2c-top");
            screen.LookAt(new Vector2(540f, 300f)); // the far end
            Step(screen, 0.2f);
            Capture(screen, "2d-bottom");

            // Busy frame: open the next station so the milestone moment is on screen.
            screen.FocusStation(5);
            Step(screen, 0.4f);
            Capture(screen, "2b-card");
            Press(screen, "Canvas/Card/Buy");
            Step(screen, 0.45f);
            Capture(screen, "3-moment");
            Step(screen, 2.5f);

            // Rabbits and items for the remaining frames.
            string[] names = { "Bông", "Mây", "Sữa", "Mochi", "Đậu", "Kem", "Nếp", "Mật", "Bánh Trôi", "Su Su", "Khoai", "Tuyết" };
            int[] tiers = { 5, 4, 3, 2, 1, 0, 0, 0, 2, 1, 3, 0 };
            for (int i = 0; i < tiers.Length; i++)
                state.Rabbits.Add(new Rabbit { Id = ++state.NextRabbitId, Tier = tiers[i], Kind = RabbitKinds.OfTier(tiers[i])[RabbitKinds.OfTier(tiers[i]).Count - 1 - i % 3].Id, Name = names[i], Level = 1 + i * 2 });
            foreach (Rabbit seen in state.Rabbits) Rabbits.See(state, Rabbits.KindOf(seen));
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

            // The sheet's slide in, frame by frame (30 frames a second), then at rest.
            Capture(screen, "22-open-0");
            screen.OpenSheet(0);
            for (int f = 1; f <= 5; f++)
            {
                Step(screen, f == 5 ? 0.14f : 0.06f);
                Capture(screen, "22-open-" + f);
            }
            Step(screen, 0.5f);
            Capture(screen, "5-stations");
            // Touching a row brings that station into the strip of garden above the sheet.
            Press(screen, "Canvas/Sheet/Panel/Scroll/Content/Row " + runner.Defs[3].Id);
            Step(screen, 0.5f);
            Capture(screen, "22-row-to-station");
            // Pulled up by the handle it fills the screen up to the HUD.
            screen.DragSheet(500f);
            screen.ReleaseSheet();
            Step(screen, 0.5f);
            Capture(screen, "22-full");
            screen.DragSheet(-300f);
            screen.ReleaseSheet(-2000f);
            Step(screen, 0.5f);
            screen.SetTab(1);
            Step(screen, 0.2f);
            Capture(screen, "6-upgrades");
            screen.SetTab(2);
            Step(screen, 0.2f);
            Capture(screen, "7-quests");
            screen.SetTab(3);
            Step(screen, 0.2f);
            Capture(screen, "8-rabbits");
            // Calling a rabbit: the moon gate, the shadow, the reveal.
            Press(screen, "Canvas/Sheet/Panel/Rabbits/Content/Summon/Button");
            Step(screen, 0.85f);
            Capture(screen, "18a-gate");
            Step(screen, 0.75f);
            Capture(screen, "18b-shadow");
            Step(screen, 1.6f);
            Capture(screen, "18c-arrived");
            screen.Arrival.Close();
            Step(screen, 0.2f);
            screen.Detail.Open(runner.Defs, state, state.Rabbits[0].Id);
            Step(screen, 0.5f);
            Capture(screen, "8b-detail");
            screen.Detail.Open(runner.Defs, state, state.Rabbits[4].Id);
            Step(screen, 0.5f);
            Capture(screen, "8b-detail-2");
            screen.Detail.Close();
            screen.ShowRabbitAlbum(true);
            Step(screen, 0.3f);
            Capture(screen, "8c-album-top");
            var albumScroll = GameObject.Find("Canvas/Sheet/Panel/Rabbits").GetComponent<UnityEngine.UI.ScrollRect>();
            Canvas.ForceUpdateCanvases();
            albumScroll.verticalNormalizedPosition = 0f;
            Step(screen, 0.2f);
            Capture(screen, "8d-album-bottom");
            screen.Detail.OpenKind(runner.Defs, state, state.Rabbits[1].Kind);
            Step(screen, 0.5f);
            Capture(screen, "8e-kind");
            screen.Detail.Close();
            screen.ShowRabbitAlbum(false);
            Step(screen, 0.2f);

            screen.Picker.Open(state, state.Rabbits[0].Id, ItemSlot.Hat);
            Step(screen, 0.4f);
            Capture(screen, "9-items");
            screen.Picker.Close();
            screen.Rename.Open(state, state.Rabbits[0].Id);
            Step(screen, 0.4f);
            Capture(screen, "9b-rename");
            screen.Rename.Close();
            screen.CloseSheet();
            Step(screen, 0.3f);
            screen.FocusStation(4);
            Step(screen, 0.4f);
            Capture(screen, "9c-worn");
            screen.SelectStation(-1);

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
            screen.Detail.Open(runner.Defs, state, state.Rabbits[2].Id);
            Step(screen, 0.5f);
            Capture(screen, "12b-detail-en");
            screen.Detail.Close();
            screen.ShowRabbitAlbum(true);
            Step(screen, 0.3f);
            Capture(screen, "12c-album-en");
            screen.ShowRabbitAlbum(false);
            screen.SetTab(2);
            Step(screen, 0.2f);
            Capture(screen, "13-quests-en");
            screen.CloseSheet();
            screen.Shell.OpenPause();
            Step(screen, 0.6f);
            Capture(screen, "14-pause-en");
            L10n.SetLocale("vi");

            // The whole garden with every station open, to check the layout.
            screen.Shell.Resume();
            for (int i = 0; i < state.Levels.Length; i++) if (state.Levels[i] <= 0) state.Levels[i] = 1 + i;
            state.Plots = state.Levels.Length;
            screen.Refresh();
            screen.SetZoom(GardenScreen.ZoomLevel.Far, false);
            Step(screen, 0.5f);
            Capture(screen, "15-all-far");
            screen.ZoomToStation(4, false);
            Step(screen, 0.3f);
            Capture(screen, "15-all-near");
            screen.SetZoom(GardenScreen.ZoomLevel.Default, false);
            screen.LookAt(new Vector2(540f, 2900f));
            Step(screen, 0.3f);
            Capture(screen, "15-all-top");
            screen.FocusStation(9);
            Step(screen, 0.4f);
            Capture(screen, "15-card-top");
            screen.FocusStation(11);
            Step(screen, 0.4f);
            Capture(screen, "15-card-bottom");
            screen.SelectStation(-1);
            screen.LookAt(new Vector2(540f, 300f));
            Step(screen, 0.3f);
            Capture(screen, "16-all-bottom");
            // Every place to rest open and taken, to see the seats and the strollers.
            state.RestSpots = RestSpots.Max;
            for (int i = 0; i < state.Rabbits.Count && i < 7; i++) state.Rabbits[i].Station = -1;
            screen.Refresh();
            screen.LookAt(new Vector2(540f, 300f));
            Step(screen, 0.4f);
            Capture(screen, "17-rest-bottom");
            screen.LookAt(new Vector2(540f, 2900f));
            Step(screen, 0.3f);
            Capture(screen, "17-rest-top");

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
