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
            state.TutorialStep = Tutorial.Steps; // and so would the guidance; it gets frames of its own at the end
            state.Coins = new BigNum(5.2e6);
            state.Energy = 73;
            // A few things waiting in the tray, one of them golden.
            Tray.Add(state, 0, false); Tray.Add(state, 0, false); Tray.Add(state, 2, false); Tray.Add(state, 4, true);
            runner.Init(state);

            screen.Build();
            screen.Refresh();
            if (Directory.Exists(Folder)) Directory.Delete(Folder, true);
            Directory.CreateDirectory(Folder);

            Step(screen, 0.4f);
            Capture(screen, "1-title");

            // The opening glide from the moon pond to the teapot, five frames 0.2 s apart.
            screen.Shell.StartGame();
            for (int f = 1; f <= 5; f++)
            {
                Step(screen, 0.2f);
                Capture(screen, "28-glide-" + f);
            }
            Step(screen, 0.4f);
            Capture(screen, "2-garden");
            // Photo mode: the garden alone with the snap bar, then the picture just taken.
            screen.PhotoFolder = Folder;
            screen.EnterPhotoMode();
            Step(screen, 0.3f);
            Capture(screen, "18-chup-khung");
            screen.Snap();
            Step(screen, 0.4f);
            Capture(screen, "18-chup-xem");
            GameObject.Find("Canvas/PhotoReview/Card/Close").GetComponent<Button>().onClick.Invoke();
            screen.Snap();
            GameObject.Find("Canvas/PhotoReview/Card/Close").GetComponent<Button>().onClick.Invoke();
            screen.OpenAlbum();
            Step(screen, 0.3f);
            Capture(screen, "18-album");
            screen.CloseAlbum();
            screen.ExitPhotoMode();
            Step(screen, 0.2f);
            // Selling a stack from the tray: the carrots fly up to the purse, two frames.
            Press(screen, "Canvas/Tray/Slot 0");
            for (int f = 1; f <= 2; f++)
            {
                Step(screen, 0.18f);
                Capture(screen, "37-ban-" + f);
            }
            Step(screen, 1f);
            // Nobody works anywhere yet: every open station shows its empty pad, the unbuilt plot its price sign.
            screen.FocusStation(1);
            Step(screen, 0.4f);
            Capture(screen, "43-tram-trong");
            screen.SelectStation(-1);
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
            // The purse explains itself: a row per brewing station, the factors, the boost, the total.
            Press(screen, "Canvas/Hud/Purse");
            Step(screen, 0.4f);
            Capture(screen, "32-income");
            screen.Income.Close();

            // The sheet's slide in, frame by frame (30 frames a second), then at rest.
            Capture(screen, "22-open-0");
            screen.OpenSheet(0);
            for (int f = 1; f <= 5; f++)
            {
                Step(screen, 0.06f);
                Capture(screen, "22-open-" + f);
            }
            Step(screen, 0.5f);
            Capture(screen, "5-stations");
            // Upgrading everything: the summary with the share to spend and what each station gains.
            screen.UpgradeAll.Open(runner.Defs, state);
            Step(screen, 0.4f);
            Capture(screen, "36-nang-tat-ca");
            screen.UpgradeAll.Close();
            Step(screen, 0.1f);
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
            // Collecting a quest: the carrots fly from the row to the purse, three frames 0.15 s apart.
            Press(screen, "Canvas/Sheet/Panel/Quests/Content/Quest mo-4/Claim");
            for (int f = 1; f <= 3; f++)
            {
                Step(screen, 0.15f);
                Capture(screen, "27-bay-" + f);
            }
            Step(screen, 1.5f);
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
            // A rabbit past twenty levels wears its level stars on its sheet and in the grid.
            screen.Detail.Open(runner.Defs, state, state.Rabbits[11].Id);
            Step(screen, 0.5f);
            Capture(screen, "48-detail-stars");
            screen.Detail.Close();
            var gridScroll = GameObject.Find("Canvas/Sheet/Panel/Rabbits").GetComponent<UnityEngine.UI.ScrollRect>();
            Canvas.ForceUpdateCanvases();
            gridScroll.verticalNormalizedPosition = 0f;
            Step(screen, 0.6f);
            Capture(screen, "48-grid-stars");
            gridScroll.verticalNormalizedPosition = 1f;
            Step(screen, 0.2f);
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
            // Choosing a rabbit for a station: its own trade first, marked ×1,5.
            screen.Workers.Open(runner.Defs, state, 2);
            Step(screen, 0.4f);
            Capture(screen, "43-chon-tho");
            screen.Workers.Close();
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

            // Not enough carrots: the button shakes and the purse flashes red (three frames).
            var purseBefore = state.Coins;
            state.Coins = VuonTra.Core.BigNum.Zero;
            screen.OpenSheet(0);
            Step(screen, 0.6f);
            GameObject.Find("Canvas/Sheet/Panel/Scroll/Content/Row " + runner.Defs[0].Id + "/Buy").GetComponent<PressDip>().Click();
            for (int f = 1; f <= 3; f++)
            {
                Step(screen, 0.07f);
                Capture(screen, "29-deny-" + f);
            }
            state.Coins = purseBefore;
            screen.CloseSheet();
            Step(screen, 0.6f);

            screen.Shell.OpenPause();
            Step(screen, 0.6f);
            Capture(screen, "10-pause");
            // The profile behind the pause card's first row: avatar, garden name, code, backup.
            Press(screen, "Canvas/Pause/Card/Profile");
            Step(screen, 0.4f);
            Capture(screen, "17-profile");
            screen.Profile.Close();

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
            Press(screen, "Canvas/Pause/Card/Profile");
            Step(screen, 0.4f);
            Capture(screen, "17-profile-en");
            screen.Profile.Close();
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
            // With a full purse every station can be raised: the pill at the top counts the ones out of view,
            // and close in at the foot of the garden the teapot button shows the way home.
            var purseWas = state.Coins;
            state.Coins = new BigNum(1e30);
            screen.Refresh();
            Step(screen, 0.2f);
            Capture(screen, "31-edges");
            screen.SetZoom(GardenScreen.ZoomLevel.Near, false);
            screen.LookAt(new Vector2(540f, 300f));
            Step(screen, 0.2f);
            Capture(screen, "31-home");
            screen.SetZoom(GardenScreen.ZoomLevel.Default, false);
            screen.LookAt(new Vector2(540f, 300f));
            state.Coins = purseWas;
            screen.Refresh();
            Step(screen, 0.2f);
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

            // The teapot at its cap: the card and the row say "max level" and offer nothing.
            state.Levels[0] = Economy.MaxLevel;
            state.Coins = new BigNum(1e30);
            screen.Refresh();
            screen.FocusStation(0);
            Step(screen, 0.4f);
            Capture(screen, "47-cap-card");
            screen.SelectStation(-1);
            screen.OpenSheet(0);
            Step(screen, 0.6f);
            Capture(screen, "47-cap-row");
            screen.CloseSheet();
            Step(screen, 0.4f);

            RenderGuidance(runner, screen);

            Debug.Log("[VuonTra] Previews written to " + Folder);
            // The scene now holds preview objects; replace it so nothing gets saved by accident.
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        /// <summary>A brand-new garden for the guidance frames: the first step, then the card step, then the sheet step.</summary>
        static void RenderGuidance(GameRunner runner, GardenScreen screen)
        {
            GameState fresh = GameState.NewGame(runner.Catalog.BuildDefs());
            Rabbits.GiftFirst(fresh, "Bông");
            runner.Init(fresh);
            screen.SelectStation(-1);
            screen.Refresh();
            screen.LookAt(GardenScreen.StationSpot(0));
            Step(screen, 0.6f);
            Capture(screen, "30-buoc-1");
            Press(screen, "Canvas/Garden/Content/Station am-tra");
            fresh.Coins = new BigNum(1000);
            Step(screen, 0.4f);
            Capture(screen, "30-buoc-2");
            Press(screen, "Canvas/Card/Buy");
            screen.OpenSheet(0);
            Step(screen, 0.5f);
            Capture(screen, "30-buoc-3");
            screen.CloseSheet();
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
