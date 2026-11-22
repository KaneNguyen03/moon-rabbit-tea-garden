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
            screen.ClockHours = () => 22.0; // every frame is the night garden unless a frame says otherwise
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
            // The ground's layers around the teapot, and the canopy drifting when the view is at the top.
            screen.LookAt(screen.StationSpot(0) + new Vector2(0, -120f));
            Step(screen, 0.3f);
            Capture(screen, "34-nen-tam");
            screen.LookAt(new Vector2(540f, 2800f));
            Step(screen, 0.3f);
            Capture(screen, "34-nen-tren");
            // Station ranks (#49): the basket at rank 2 and the jam pot at rank 3 beside the rank-1 teapot.
            state.Levels[1] = 100;
            state.Levels[2] = 300;
            screen.Refresh();
            screen.LookAt(screen.StationSpot(0) + new Vector2(0, -120f));
            Step(screen, 0.3f);
            Capture(screen, "49-bac-tram");
            state.Levels[1] = 12;
            state.Levels[2] = 9;
            screen.Refresh();
            screen.LookAt(screen.StationSpot(0));
            Step(screen, 0.3f);
            // A payout: two carrots fly from the teapot to the purse, three frames 0.15 s apart.
            screen.PlayPayout(0, new BigNum(1200));
            for (int f = 1; f <= 3; f++)
            {
                Step(screen, 0.15f);
                Capture(screen, "28-fly-" + f);
            }
            Step(screen, 0.6f);
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
            for (int i = 0; i < itemDefs.Count; i++) if (itemDefs[i].Price <= 0) Items.Grant(state, itemDefs[i], (i * 2) % Rabbits.TierCount); // the shop's hats stay on sale
            Items.Equip(state, state.Items[0].Id, state.Rabbits[0].Id);
            Items.Equip(state, state.Items[3].Id, state.Rabbits[0].Id);
            state.TotalTaps = 37;
            runner.BuyUpgrade(0); // also refreshes the modifiers

            // The working rabbits with their trade tools (#2), close up, once they have walked to their posts.
            screen.Refresh();
            Step(screen, 3f);
            screen.SetZoom(GardenScreen.ZoomLevel.Near, false);
            screen.LookAt(screen.StationSpot(0) + new Vector2(-60f, 0f));
            Step(screen, 0.6f);
            Capture(screen, "2-dung-cu-nghe");
            screen.SetZoom(GardenScreen.ZoomLevel.Default, false);
            screen.LookAt(screen.StationSpot(0));
            Step(screen, 0.3f);
            Press(screen, "Canvas/Hud/Energy");
            Step(screen, 2.4f);
            Capture(screen, "4-boost");
            // The purse explains itself: a row per brewing station, the factors, the boost, the total.
            Press(screen, "Canvas/Hud/Purse");
            Step(screen, 0.4f);
            Capture(screen, "32-income");
            screen.Income.Close();
            // The mooncake game: the ring closing on the cake, a perfect pound, the dough gone chewy, and the result.
            screen.OpenPounding();
            Step(screen, 1.2f);
            Capture(screen, "45-gia-1");
            for (int b = 0; b < 6; b++)
            {
                Step(screen, (float)(Pounding.BeatTime(b) - screen.Pound.Time));
                screen.Pound.Tap();
            }
            Step(screen, 0.12f);
            Capture(screen, "45-gia-2");
            Step(screen, 30f);
            Capture(screen, "45-gia-ket-qua");
            screen.Pound.Close();
            screen.Refresh();
            // The leaf-catching game: leaves in the air with the basket under one, then the result.
            screen.OpenLeaves();
            if (screen.Basket.IsOpen)
            {
                LeavesRun leafRun = screen.Basket.Run;
                Step(screen, 3.2f);
                foreach (Leaf leaf in leafRun.Schedule)
                    if (leaf.Drop <= screen.Basket.Time && leaf.Lands > screen.Basket.Time && leaf.Kind != LeafKind.Cone) { screen.Basket.SetBasket((float)leaf.X); break; }
                Step(screen, 0.4f);
                Capture(screen, "10-hung-la");
                foreach (Leaf leaf in leafRun.Schedule)
                {
                    if (leaf.Lands <= screen.Basket.Time) continue;
                    screen.Basket.SetBasket(leaf.Kind == LeafKind.Cone ? (leaf.X > 0.5 ? (float)leaf.X - 0.4f : (float)leaf.X + 0.4f) : (float)leaf.X);
                    Step(screen, (float)(leaf.Lands - screen.Basket.Time) + 0.02f);
                }
                Step(screen, 4f);
                Capture(screen, "10-hung-la-ket-qua");
                screen.Basket.Close();
                screen.Refresh();
            }
            // The firefly night: the swarm over the dark garden with the jar below, then the result.
            screen.OpenFireflies();
            if (screen.Jar.IsOpen)
            {
                FirefliesRun flyRun = screen.Jar.Run;
                Step(screen, 4.2f);
                foreach (Firefly fly in flyRun.Schedule)
                {
                    if (fly.Appears + 0.5 > screen.Jar.Time || !Fireflies.Visible(fly, screen.Jar.Time)) continue;
                    FlySpot spot = Fireflies.PositionAt(fly, screen.Jar.Time);
                    screen.Jar.Tap((float)spot.X, (float)spot.Y);
                    break; // one just caught, its "+1" still up
                }
                Step(screen, 0.1f);
                Capture(screen, "10-dom-dom");
                foreach (Firefly fly in flyRun.Schedule)
                {
                    if (flyRun.IsCaught(fly.Index)) continue;
                    double at = System.Math.Max(screen.Jar.Time, fly.Appears + 0.2);
                    if (at > screen.Jar.Time) Step(screen, (float)(at - screen.Jar.Time));
                    FlySpot spot = Fireflies.PositionAt(fly, screen.Jar.Time);
                    screen.Jar.Tap((float)spot.X, (float)spot.Y);
                }
                Step(screen, 21f);
                Capture(screen, "10-dom-dom-ket-qua");
                screen.Jar.Close();
                screen.Refresh();
            }

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
            // The moon gate dressed for each season (#8): the same moment with the season's hanging on the arch.
            foreach (var season in new[] { Season.MidAutumn, Season.Tet, Season.Christmas })
            {
                screen.Arrival.SetSeasonDecor(runner.Catalog.GateDecor(season));
                Step(screen, 0.3f);
                Capture(screen, "8-cong-" + season.ToString().ToLowerInvariant());
            }
            screen.Arrival.SetSeasonDecor(null);
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
            // #63: the station-sound slider and the tune pill, turned to "Rạng sáng" for the picture and back after.
            {
                Sfx.Tune tuneBefore = Sfx.CurrentTune;
                for (int i = 0; i < 4 && Sfx.CurrentTune != Sfx.Tune.Dawn; i++) Press(screen, "Canvas/Pause/Card/Tune");
                Step(screen, 0.1f);
                Capture(screen, "63-tieng-tram");
                for (int i = 0; i < 4 && Sfx.CurrentTune != tuneBefore; i++) Press(screen, "Canvas/Pause/Card/Tune");
            }
            // The profile behind the pause card's first row: avatar, garden name, code, backup.
            Press(screen, "Canvas/Pause/Card/Profile");
            Step(screen, 0.4f);
            Capture(screen, "17-profile");
            screen.Profile.Close();

            // The cloud: a copy up there that is further along raises the choice card; then the profile with the save sent up.
            {
                GameState ahead = SaveCodec.Decode(runner.Defs, SaveCodec.Encode(runner.Defs, state, runner.UpgradeDefs), runner.UpgradeDefs).State;
                ahead.GardenXp = Garden.XpForLevel(Garden.Level(state) + 3);
                ahead.Rabbits.Add(new Rabbit { Id = ++ahead.NextRabbitId, Tier = 2, Kind = RabbitKinds.OfTier(2)[0].Id, Name = "Mộc", Level = 4 });
                ahead.LastSeenUnixSeconds = state.LastSeenUnixSeconds + 3600;
                ahead.PlaySeconds = state.PlaySeconds + 5400;
                var cloud = new MemoryCloudSave { Text = CloudSave.Wrap(SaveCodec.Encode(runner.Defs, ahead, runner.UpgradeDefs), SaveSummary.Of(ahead), ahead.LastSeenUnixSeconds) };
                runner.Cloud = cloud;
                runner.SyncNow(SyncReason.Open);
                Step(screen, 0.4f);
                Capture(screen, "38-chon-ban");
                screen.CloudChoice.Close();
                runner.Sync.Resolve(false, "{}", _ => false, null); // keep this copy; the push refreshes the status line
                runner.PushToCloud();
                screen.Profile.Open(runner.Defs, state, runner.Account, runner.CloudStatus);
                Step(screen, 0.4f);
                Capture(screen, "38-ho-so-may");
                screen.Profile.Close();
                runner.Cloud = new LocalOnlyCloudSave();
            }

            // Large text: every label a fifth bigger, the sheet and the garden rebuilt around it.
            screen.Shell.Resume();
            Step(screen, 0.6f);
            UiKit.SetLargeText(true);
            screen.RebuildUi();
            Step(screen, 0.8f);
            screen.OpenSheet(0);
            Step(screen, 0.5f);
            Capture(screen, "33-chu-to");
            screen.CloseSheet();
            UiKit.SetLargeText(false);
            screen.RebuildUi();
            Step(screen, 0.8f);

            // The same screens in English.
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
            // #55 Resting rabbits stroll to an empty spot of the lawn: tea by day, a firefly to chase by night.
            {
                screen.ClockHours = () => 12.0;
                screen.Refresh();
                Step(screen, screen.StageStroll(0, StrollActivity.Tea));
                int spot = screen.StrollSpotOf(0);
                if (spot >= 0) screen.LookAt(GardenScreen.LawnSpots[spot]);
                Step(screen, 0.4f);
                Capture(screen, "55-uong-tra");
                screen.ClockHours = () => 22.0;
                screen.Refresh();
                Step(screen, screen.StageStroll(1, StrollActivity.Firefly));
                spot = screen.StrollSpotOf(1);
                if (spot >= 0) screen.LookAt(GardenScreen.LawnSpots[spot]);
                Step(screen, 0.4f);
                Capture(screen, "55-duoi-dom-dom");
            }
            // A resting rabbit given the teapot walks over from its seat: caught halfway.
            {
                Rabbit mover = null;
                foreach (Rabbit r in state.Rabbits) if (r.Station < 0) { mover = r; break; }
                if (mover != null && runner.AssignRabbit(mover.Id, 0))
                {
                    screen.Refresh();
                    screen.LookAt(new Vector2(540f, 1200f));
                    Step(screen, 0.7f);
                    Capture(screen, "28-tho-di-tram");
                    Step(screen, 3f);
                    // Stroked at its new post on the day its bond reaches three hearts: hearts, tea power and its line (#19).
                    mover.Affection = Rabbits.PointsFor(Rabbits.LineHearts) - Rabbits.PetPoints;
                    mover.PetDay = -1;
                    screen.FocusStation(0); // settle the view on the teapot first, so the hearts rise where the rabbit stands
                    Step(screen, 0.5f);
                    Press(screen, "Canvas/Garden/Content/Station am-tra");
                    Step(screen, 0.25f);
                    Capture(screen, "19-vuot-tho");
                    Step(screen, 1f);
                    screen.SelectStation(-1);
                    screen.Detail.Open(runner.Defs, state, mover.Id);
                    Step(screen, 0.5f);
                    Capture(screen, "19-tim-the");
                    screen.Detail.Close();
                    Step(screen, 3f);
                }
            }

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

            // The gift calendar (#57): day one waiting, day four with three days ticked, and day seven's big gift with the hat and clover.
            long giftDayBefore = state.GiftDay;
            int giftStreakBefore = state.GiftStreak;
            state.GoldenCarrots = 12;
            state.GiftDay = -1;
            state.GiftStreak = 0;
            screen.OpenShop();
            Step(screen, 0.4f);
            Capture(screen, "57-ngay-1");
            state.GiftDay = runner.TodayNumber - 1;
            state.GiftStreak = 3;
            screen.Refresh();
            Step(screen, 0.2f);
            Capture(screen, "57-ngay-4");
            state.GiftStreak = 6;
            screen.Refresh();
            Step(screen, 0.2f);
            Capture(screen, "57-ngay-7");
            screen.Shop.Close();
            Step(screen, 0.3f);
            state.GiftDay = giftDayBefore;
            state.GiftStreak = giftStreakBefore;

            // The tea shop by the pond: today's gift and the golden call.
            state.GoldenCarrots = 12;
            screen.OpenShop();
            Step(screen, 0.4f);
            Capture(screen, "44-tiem");
            // Things for rabbits (#44 round 3): the crown bought and owned, the conical hat still affordable, the rest not.
            state.GoldenCarrots = 75;
            Item crown = Shop.BuyItem(state, itemDefs, "hat-vuong-mien");
            screen.Refresh();
            Step(screen, 0.3f);
            Capture(screen, "44-do-cho-tho");
            screen.Shop.Close();
            Step(screen, 0.3f);
            // A rabbit dyed pink wearing the crown, on its own sheet with the dye row.
            if (crown != null) Items.Equip(state, crown.Id, state.Rabbits[0].Id);
            state.GoldenCarrots = 20;
            Shop.DyeRabbit(state, state.Rabbits[0].Id, 0);
            screen.Refresh();
            screen.Detail.Open(runner.Defs, state, state.Rabbits[0].Id);
            Step(screen, 0.5f);
            Capture(screen, "44-nhuom");
            screen.Detail.Close();
            Step(screen, 0.3f);
            // Decorating the garden (#11): four pieces standing, one more waiting in storage so the empty spots show their "+",
            // and mint lanterns; then the shelf in the shop with the colour row.
            state.GoldenCarrots = 200;
            Decor.Buy(state, "den-da"); Decor.Buy(state, "bon-hoa"); Decor.Buy(state, "ghe-nam"); Decor.Buy(state, "tham-picnic"); Decor.Buy(state, "den-da");
            Decor.Place(state, 2, "den-da"); Decor.Place(state, 3, "bon-hoa"); Decor.Place(state, 4, "ghe-nam"); Decor.Place(state, 5, "tham-picnic");
            Decor.SetLanternColor(state, 2);
            screen.Refresh();
            screen.LookAt(new Vector2(540f, 1300f)); // the four pieces and, under the HUD, the "+" of the spot by the beehive
            Step(screen, 0.5f);
            Capture(screen, "11-trang-tri");
            screen.OpenShop();
            Step(screen, 0.4f);
            Capture(screen, "11-ke-trang-tri");
            screen.Shop.Close();
            Step(screen, 0.3f);
            // Back to a bare garden for the frames that follow.
            for (int i = 0; i < Decor.Spots; i++) Decor.Store(state, i);
            for (int i = 0; i < state.DecorOwned.Length; i++) state.DecorOwned[i] = 0;
            state.LanternColor = 0;
            screen.Refresh();
            // A guest at the counter with its order in a bubble (#20), then the order card. One may have come on its
            // own by now: the frames above step through more than the minute the first guest waits for.
            if (runner.State.Order == null) runner.PlaceOrder();
            if (runner.State.Order != null)
            {
                screen.LookAt(screen.GuestSpot);
                Step(screen, GardenScreen.GuestWalkSeconds + 0.6f);
                Capture(screen, "20-khach");
                screen.OpenOrder();
                Step(screen, 0.4f);
                Capture(screen, "20-don");
                screen.OrderCard.Close();
                Step(screen, 0.3f);
            }
            // Coming back after three hours (#69): four lines and the day's free ×2.
            var away = new AwayReport { Paid = new BigNum(1.2e9), Seconds = 3 * 3600, Capped = true, TrayAdded = 7, TrayFull = true, GuestsMissed = 3, SleepiestId = Away.Sleepiest(state) };
            runner.ShowAwayForTest(away);
            screen.ShowAway(away);
            Step(screen, 0.5f);
            Capture(screen, "69-di-vang");
            screen.AwayCard.Close();
            Step(screen, 0.4f);
            // The same garden at noon and at dusk; night is every other frame.
            screen.SelectStation(-1);
            screen.LookAt(screen.StationSpot(0));
            screen.ClockHours = () => 13.0;
            Step(screen, 0.3f);
            Capture(screen, "21-ban-ngay");
            screen.ClockHours = () => 18.0;
            Step(screen, 0.3f);
            Capture(screen, "21-hoang-hon");
            screen.ClockHours = () => 22.0;
            Step(screen, 0.2f);

            RenderGuidance(runner, screen);
            RenderLandWaves(runner, screen);
            RenderPhotoAsk(runner, screen);
            RenderRestShare(runner, screen);
            RenderChatter(runner, screen);
            RenderAlbumSets(runner, screen);
            RenderMoonJars(runner, screen);
            RenderGiftCodes(runner, screen);
            RenderFourStations(runner, screen);
            RenderAdSeams(runner, screen);
            RenderForestRings(runner, screen); // #53
            RenderLayout(runner, screen);
            RenderQaNits2(); // #79–#82, on a freshly opened scene so the title shows again
            RenderReservedProps(); // #54 follow-up

            Debug.Log("[VuonTra] Previews written to " + Folder);
            // The scene now holds preview objects; replace it so nothing gets saved by accident.
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        /// <summary>
        /// #51: a new garden is clear grass with dashed rings on the land not bought and the forest rim at its edge; buying the
        /// first wave steps the rim back. The opening view in Vietnamese, English and large text, the wave before, halfway
        /// and after, and the whole garden from far away at each of the five land stages (4, 6, 8, 10 and 12 plots).
        /// </summary>
        static void RenderLandWaves(GameRunner runner, GardenScreen screen)
        {
            GameState s = GameState.NewGame(runner.Catalog.BuildDefs());
            s.HintSeen = true;
            s.TutorialStep = Tutorial.Steps;
            for (int i = 0; i < Plots.Starting; i++) s.Levels[i] = 6 - i;
            Rabbits.GiftFirst(s, "Bông");
            runner.Init(s);
            L10n.SetLocale("vi");
            screen.RebuildUi();
            screen.SelectStation(-1);
            screen.Refresh();
            Step(screen, 0.6f);
            screen.SetZoom(GardenScreen.ZoomLevel.Default, false);
            screen.LookAt(screen.StationSpot(0));
            Step(screen, 0.4f);
            Capture(screen, "51-mo-vuon");
            L10n.SetLocale("en");
            screen.RebuildUi();
            Step(screen, 0.6f);
            Capture(screen, "51-mo-vuon-en");
            L10n.SetLocale("vi");
            UiKit.SetLargeText(true);
            screen.RebuildUi();
            Step(screen, 0.6f);
            Capture(screen, "51-mo-vuon-chu-to");
            UiKit.SetLargeText(false);
            screen.RebuildUi();
            Step(screen, 0.6f);

            // The first wave opens the two plots left of the teapot: the rim there steps back while the grass comes up.
            Vector2 side = (screen.StationSpot(4) + screen.StationSpot(5)) / 2f;
            screen.LookAt(side);
            Step(screen, 0.3f);
            Capture(screen, "51-mua-dot-1-truoc");
            s.Plots = Expansion.PlotsForRadius(runner.Defs, Expansion.Rings[0].RadiusAfter);
            screen.Refresh();
            Step(screen, GardenScreen.RimStep * 0.4f);
            Capture(screen, "51-mua-dot-1-giua");
            Step(screen, GardenScreen.RimStep + 0.4f);
            Capture(screen, "51-mua-dot-1-sau");

            screen.SetZoom(GardenScreen.ZoomLevel.Far, false);
            for (int wave = 0; wave <= Expansion.Rings.Length; wave++)
            {
                int plots = wave == 0 ? Plots.Starting : Expansion.PlotsForRadius(runner.Defs, Expansion.Rings[wave - 1].RadiusAfter);
                for (int i = 0; i < s.Levels.Length; i++) s.Levels[i] = i < plots - 2 || i < Plots.Starting ? Mathf.Max(1, 6 - i) : 0;
                s.Plots = plots;
                screen.Refresh();
                Step(screen, GardenScreen.RimStep + 0.4f);
                Capture(screen, "51-dot-" + wave);
            }
            screen.SetZoom(GardenScreen.ZoomLevel.Default, false);
        }

        /// <summary>#56: the rabbit at the teapot holds up its camera bubble, then the camera comes up framed on it.</summary>
        static void RenderPhotoAsk(GameRunner runner, GardenScreen screen)
        {
            GameState s = runner.State;
            if (screen.Shell.State != ShellState.Playing) screen.Shell.Resume();
            screen.SetZoom(GardenScreen.ZoomLevel.Default, false);
            PhotoAsks.Decline(s);
            if (!PhotoAsks.Ask(runner.Defs, s, runner.TodayNumber, () => 0d)) throw new System.Exception("Preview: nobody could ask for a photo");
            screen.LookAt(screen.StationSpot(0));
            Step(screen, 0.6f);
            Capture(screen, "56-xin-chup");
            Press(screen, "Canvas/Garden/Content/PhotoAsk");
            Step(screen, 0.6f);
            Capture(screen, "56-khung-thu");
            screen.ExitPhotoMode();
            Step(screen, 0.3f);
        }

        /// <summary>#60: four rabbits seated at the places to rest, the purse's card with their share, one's own sheet and the Rabbits tab.</summary>
        static void RenderRestShare(GameRunner runner, GardenScreen screen)
        {
            GameState s = GameState.NewGame(runner.Catalog.BuildDefs());
            int[] levels = { 40, 18, 12, 6, 3 };
            for (int i = 0; i < levels.Length; i++) s.Levels[i] = levels[i];
            s.Plots = 7;
            s.RestSpots = 4;
            s.GardenXp = Garden.XpForLevel(8);
            s.HintSeen = true;
            s.TutorialStep = Tutorial.Steps;
            s.Coins = new BigNum(3.1e5);
            Rabbits.Assign(s, Rabbits.Arrive(s, 1, "Mây", 0.1).Id, 0);
            Rabbits.Assign(s, Rabbits.Arrive(s, 0, "Mè", 0.3).Id, 2);
            Rabbits.Arrive(s, 4, "Sao", 0.5);
            Rabbits.Arrive(s, 2, "Sương", 0.2);
            Rabbits.Arrive(s, 1, "Cốm", 0.7);
            Rabbits.Arrive(s, 0, "Bông", 0.4);
            runner.Init(s);
            if (screen.Shell.State != ShellState.Playing) screen.Shell.Resume();
            screen.SelectStation(-1);
            screen.Refresh();
            screen.LookAt(screen.StationSpot(0));
            Step(screen, 0.5f);
            Press(screen, "Canvas/Hud/Purse");
            Step(screen, 0.4f);
            Capture(screen, "60-thu-nhap");
            screen.Income.Close();
            screen.Detail.Open(runner.Defs, s, s.Rabbits[2].Id);
            Step(screen, 0.5f);
            Capture(screen, "60-tho-nghi");
            screen.Detail.Close();
            screen.OpenSheet(3);
            Step(screen, 0.5f);
            Capture(screen, "60-tab-tho");
            screen.CloseSheet();
            Step(screen, 0.3f);
        }

        /// <summary>#59: by day a rabbit at work says a line of its trade; by night a resting rabbit talks of the night.</summary>
        static void RenderChatter(GameRunner runner, GardenScreen screen)
        {
            GameState s = runner.State;
            if (screen.Shell.State != ShellState.Playing) screen.Shell.Resume();
            PhotoAsks.Decline(s); // no camera bubble and no guest: the line has the garden to itself
            s.Order = null;
            screen.SelectStation(-1);
            screen.SetZoom(GardenScreen.ZoomLevel.Default, false);
            screen.ClockHours = () => 13.0;
            screen.Refresh();
            Step(screen, 4f); // the guest walks off
            WakeEveryone(screen, s);
            Step(screen, 2f); // the hearts of the wake float away before the frame
            SayUntil(screen, s, key => Chatter.IsTradeLine(key), false);
            Capture(screen, "59-noi-ngay");
            // A rabbit resting on its seat for the night frame.
            s.RestSpots = Mathf.Max(s.RestSpots, 2);
            Rabbits.Arrive(s, 1, "Mây", 0d);
            screen.ClockHours = () => 22.0;
            screen.Refresh();
            Step(screen, 3f); // the day's line has gone
            WakeEveryone(screen, s);
            Step(screen, 2f);
            SayUntil(screen, s, key => key.StartsWith("chatter.phase.Night."), true);
            Capture(screen, "59-noi-dem");
            Step(screen, 3f);
        }

        /// <summary>Asks for lines until one is the wanted kind from a working (or resting) rabbit, then centres the view on it.</summary>
        static void SayUntil(GardenScreen screen, GameState s, System.Func<string, bool> want, bool resting)
        {
            for (int i = 0; i < 600; i++)
            {
                screen.Advance(0.01f);
                WakeEveryone(screen, s);
                if (!screen.ChatterNow()) continue;
                Rabbit r = Rabbits.Find(s, screen.ChatRabbitId);
                if (r == null || (r.Station < 0) != resting || !want(screen.ChatKey)) continue;
                var line = (RectTransform)screen.transform.Find("Canvas/Garden/Content/ChatLine");
                screen.LookAt(line.anchoredPosition + new Vector2(0f, -120f));
                Step(screen, 0.5f);
                return;
            }
            throw new System.Exception("Preview: no " + (resting ? "resting" : "working") + " rabbit said the wanted line");
        }

        /// <summary>A rabbit that nodded off says nothing: a tap on its station wakes it, and the card it opens is closed again.</summary>
        static void WakeEveryone(GardenScreen screen, GameState s)
        {
            for (int i = 0; i < screen.Runner.Defs.Count; i++)
            {
                Transform z = screen.transform.Find("Canvas/Garden/Content/Station " + screen.Runner.Defs[i].Id + "/Doze");
                if (z == null || !z.gameObject.activeSelf) continue;
                Press(screen, "Canvas/Garden/Content/Station " + screen.Runner.Defs[i].Id);
                screen.SelectStation(-1);
            }
        }

        /// <summary>#61: the album with its set lines and one set complete in gold, then the banner as the next set completes.</summary>
        static void RenderAlbumSets(GameRunner runner, GardenScreen screen)
        {
            GameState s = GameState.NewGame(runner.Catalog.BuildDefs());
            int[] levels = { 30, 12, 8 };
            for (int i = 0; i < levels.Length; i++) s.Levels[i] = levels[i];
            s.Plots = 6;
            s.GardenXp = Garden.XpForLevel(6);
            s.HintSeen = true;
            s.TutorialStep = Tutorial.Steps;
            s.Coins = new BigNum(4.2e4);
            Rabbits.Assign(s, Rabbits.ArriveKind(s, "tho-bong", "Bông").Id, 0);
            foreach (RabbitKind k in AlbumSets.Find(0, RabbitTrade.Brewer).Kinds) Rabbits.See(s, k.Id);
            AlbumSet baker = AlbumSets.Find(0, RabbitTrade.Baker);
            for (int i = 0; i < baker.Kinds.Count - 1; i++) Rabbits.ArriveKind(s, baker.Kinds[i].Id, "Mè");
            Rabbits.See(s, "tho-cat");
            Rabbits.See(s, "tho-matcha");
            s.FreshKinds.Clear();
            runner.Init(s);
            if (screen.Shell.State != ShellState.Playing) screen.Shell.Resume();
            screen.SelectStation(-1);
            screen.OpenSheet(3);
            screen.ShowRabbitAlbum(true);
            screen.Refresh();
            Step(screen, 0.5f);
            screen.DragSheet(GardenScreen.SheetFull); // the sheet at full height: a complete set and the next line both show
            screen.ReleaseSheet();
            Step(screen, 0.6f);
            Capture(screen, "61-so-tho-bo");
            // The last baker of the Silver tier arrives: the set is complete and the banner goes up.
            Rabbits.ArriveKind(s, baker.Kinds[baker.Kinds.Count - 1].Id, "Ca Cao");
            runner.AutoAssignRabbits();
            screen.Refresh();
            Step(screen, 0.45f);
            Capture(screen, "61-du-bo");
            screen.ShowRabbitAlbum(false);
            screen.CloseSheet();
            Step(screen, 1.6f);
        }

        /// <summary>#64: the purse's card with three moon jars and the button that opens one, then the jar on the shop's shelf.</summary>
        static void RenderMoonJars(GameRunner runner, GardenScreen screen)
        {
            GameState s = GameState.NewGame(runner.Catalog.BuildDefs());
            int[] levels = { 60, 30, 18, 12 };
            for (int i = 0; i < levels.Length; i++) s.Levels[i] = levels[i];
            s.Plots = 8;
            s.RestSpots = 3;
            s.GardenXp = Garden.XpForLevel(12);
            s.HintSeen = true;
            s.TutorialStep = Tutorial.Steps;
            s.Coins = new BigNum(3.6e6);
            s.GoldenCarrots = 30;
            s.MoonJars = 3;
            Rabbits.Assign(s, Rabbits.ArriveKind(s, "tho-bong", "Bông").Id, 0);
            Rabbits.Assign(s, Rabbits.Arrive(s, 2, "Lam", 0.2).Id, 1);
            s.FreshKinds.Clear();
            runner.Init(s);
            if (screen.Shell.State != ShellState.Playing) screen.Shell.Resume();
            screen.SelectStation(-1);
            screen.Refresh();
            Step(screen, 0.4f);
            Press(screen, "Canvas/Hud/Purse");
            Step(screen, 0.4f);
            Capture(screen, "64-vi-hu");
            screen.Income.Close();
            // One jar bought today already: two left, and the price on the chip.
            s.MoonJarBuyDay = runner.TodayNumber;
            s.MoonJarsBoughtToday = 1;
            screen.OpenShop();
            screen.Refresh();
            Step(screen, 0.5f);
            Capture(screen, "64-ke-hu");
            screen.Shop.Close();
            Step(screen, 0.4f);
        }

        /// <summary>A brand-new garden for the guidance frames: the first step, then the card step, then the sheet step.</summary>
        static void RenderGuidance(GameRunner runner, GardenScreen screen)
        {
            GameState fresh = GameState.NewGame(runner.Catalog.BuildDefs());
            Rabbits.GiftFirst(fresh, "Bông");
            runner.Init(fresh);
            screen.SelectStation(-1);
            screen.Refresh();
            screen.LookAt(screen.StationSpot(0));
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

            // #1 The new moon: the pause card's row once the garden is far enough, the compare card before and after
            // the hold, the moment, and the garden small again with the season's chip.
            fresh.GardenXp = Garden.XpForLevel(31);
            fresh.Coins = new BigNum(2.4e6);
            fresh.Levels[0] = 180; fresh.Levels[1] = 95; fresh.Levels[2] = 60; fresh.Levels[3] = 25; fresh.Levels[4] = 10;
            fresh.Plots = 8;
            fresh.RestSpots = 4;
            fresh.Clovers = 2;
            fresh.GoldenCarrots = 14;
            Rabbits.Assign(fresh, Rabbits.Arrive(fresh, 2, "Lam", 0.2).Id, 2);
            Rabbits.Assign(fresh, Rabbits.Arrive(fresh, 3, "Tím", 0.4).Id, 3);
            Rabbits.Arrive(fresh, 4, "Vàng", 0.6);
            screen.Refresh();
            Step(screen, 0.3f);
            screen.Shell.OpenPause();
            Step(screen, 0.6f);
            Capture(screen, "1-mua-trang-nut");
            Press(screen, "Canvas/Pause/Card/NewMoon");
            Step(screen, 0.4f);
            Capture(screen, "1-mua-trang-so-sanh");
            screen.NewMoonCard.Hold();
            Step(screen, 0.8f);
            Capture(screen, "1-mua-trang-dang-giu");
            Step(screen, 0.9f);
            screen.NewMoonCard.Release();
            Step(screen, 0.2f);
            Capture(screen, "1-mua-trang-mo-khoa");
            screen.NewMoonCard.Hold();
            screen.NewMoonCard.Release();
            Step(screen, 1.0f);
            Capture(screen, "1-mua-trang-trang-lan");
            Step(screen, 1.6f);
            Capture(screen, "1-mua-trang-trang-moc");
            screen.NewMoonMoment.Tap();
            Step(screen, SummonMomentView.Length + 0.4f); // the moon rabbit fully revealed at the gate
            Capture(screen, "1-mua-trang-tho-moi");
            screen.Arrival.Tap();
            screen.Arrival.Tap();
            Step(screen, 0.6f);
            Capture(screen, "1-mua-trang-vuon-moi");

            // #9 Golden carrots for money: the shop's purse row with its button, and the packs card over it (fake store prices).
            Press(screen, "Canvas/Garden/Content/Shop");
            Step(screen, 0.5f);
            Capture(screen, "9-tiem-mua-them");
            Press(screen, "Canvas/Shop/Card/Have/BuyMore");
            Step(screen, 0.4f);
            Capture(screen, "9-goi-ca-rot-vang");
            Press(screen, "Canvas/GoldenPacks/Card/Pack 1");
            Step(screen, 0.3f);
            Capture(screen, "9-da-mua");
            screen.GoldenPacks.Close();
            screen.Shop.Close();

            // #62 Rain: the garden in a day shower and a night one, what reduced motion keeps, the resting rabbits under a
            // tree, and the Frog Rabbit at the pond in today's first shower.
            {
                GameState wet = runner.State;
                wet.TutorialStep = Tutorial.Steps;
                wet.GardenXp = System.Math.Max(wet.GardenXp, Garden.XpForLevel(FrogRabbit.FromLevel));
                wet.RestSpots = RestSpots.Max;
                for (int i = 0; i < 5; i++) Rabbits.Arrive(wet, i % 3, "Mưa " + i, 0.13 * i);
                screen.Refresh();
                bool reduced = VuonTra.Game.UI.Motion.Reduced;
                VuonTra.Game.UI.Motion.Reduced = false;
                screen.Raining = () => true;
                screen.ClockHours = () => 10.5;
                screen.LookAt(screen.StationSpot(0) + new Vector2(0, -120f));
                Step(screen, 2f);
                Capture(screen, "62-mua-ngay");
                screen.ClockHours = () => 22.0;
                Step(screen, 0.5f);
                Capture(screen, "62-mua-dem");
                VuonTra.Game.UI.Motion.Reduced = true;
                Step(screen, 0.2f);
                Capture(screen, "62-mua-giam-chuyen-dong");
                VuonTra.Game.UI.Motion.Reduced = false;
                screen.LookAt(GardenScreen.PondSpot + new Vector2(-150f, -150f));
                Step(screen, 0.5f);
                Capture(screen, "62-tho-tru-mua");
                // The Frog Rabbit comes in a real shower: today's first, by the clamped day.
                double shower = Weather.ShowerStarts(runner.TodayNumber)[0] + 1d / 60d;
                screen.ClockHours = () => shower;
                screen.Raining = null;
                screen.LookAt(GardenScreen.FrogPondSpot + new Vector2(-200f, 0f));
                Step(screen, 1.2f);
                Capture(screen, "62-tho-ech-den");
                Step(screen, GardenScreen.FrogWalkSeconds);
                Capture(screen, "62-tho-ech");
                wet.FrogLeft = 0d;
                screen.ClockHours = () => 22.0;
                screen.Raining = () => false;
                Step(screen, 3.5f);
                screen.Raining = null;
                VuonTra.Game.UI.Motion.Reduced = reduced;
            }
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

        /// <summary>
        /// #68: the pause card with the mailbox's badge, the mailbox with a make-up letter and a missed season's letter,
        /// the profile's gift-code field with a code typed, and the line a wrong code brings.
        /// </summary>
        static void RenderGiftCodes(GameRunner runner, GardenScreen screen)
        {
            GameState s = GameState.NewGame(runner.Catalog.BuildDefs());
            int[] levels = { 30, 14, 8, 3 };
            for (int i = 0; i < levels.Length; i++) s.Levels[i] = levels[i];
            s.Plots = 6;
            s.GardenXp = Garden.XpForLevel(6);
            s.HintSeen = true;
            s.TutorialStep = Tutorial.Steps;
            s.Coins = new BigNum(2.4e5);
            s.GoldenCarrots = 6;
            s.Profile.GardenName = "Vườn Mây";
            Rabbits.Assign(s, Rabbits.ArriveKind(s, "tho-bong", "Bông").Id, 0);
            s.FreshKinds.Clear();
            runner.Init(s);
            long today = runner.TodayNumber;
            Mailbox.Compensate(s, today);
            s.Letters.Add(new Letter { Id = "season-MidAutumn-2026", Kind = LetterKind.Season, Season = Season.MidAutumn, Day = today - 12 });
            if (screen.Shell.State == ShellState.Title) screen.Shell.StartGame();
            if (screen.Shell.State == ShellState.Paused) screen.Shell.Resume();
            screen.SelectStation(-1);
            screen.Refresh();
            Step(screen, 0.4f);
            screen.Shell.OpenPause();
            Step(screen, 0.5f);
            Capture(screen, "68-tam-nghi-thu");
            Press(screen, "Canvas/Pause/Card/Mail");
            Step(screen, 0.4f);
            Capture(screen, "68-hop-thu");
            Press(screen, "Canvas/Mail/Card/Letter 0/Claim");
            Step(screen, 0.3f);
            Capture(screen, "68-hop-thu-da-nhan");
            screen.Mail.Close();
            Press(screen, "Canvas/Pause/Card/Profile");
            Step(screen, 0.4f);
            screen.Profile.GiftInput.text = "TRATHO-2026";
            Step(screen, 0.1f);
            Capture(screen, "68-ho-so-ma-qua");
            screen.Profile.GiftInput.text = "TRATHO-2025";
            screen.Profile.Redeem();
            Step(screen, 0.1f);
            Capture(screen, "68-ma-sai");
            screen.Profile.Close();
            screen.Shell.Resume();
            Step(screen, 0.4f);
        }

        /// <summary>
        /// #67: the four stations after the star observatory. At R 7 the Stations tab at its foot says what each waits for and
        /// the far garden shows the sign for the eighth ring; the eighth ring opens (the forest steps back, the lotus garden
        /// and the candy kiln get their land at the top of the garden), then the ninth; the four open in the garden up close
        /// and from far away, and the candy kiln's own product sits in the tray.
        /// </summary>
        static void RenderFourStations(GameRunner runner, GardenScreen screen)
        {
            var defs = runner.Catalog.BuildDefs();
            GameState s = GameState.NewGame(defs);
            for (int i = 0; i < 12; i++) s.Levels[i] = 160 - 10 * i;
            s.Plots = 12;
            s.RestSpots = 6;
            s.GardenXp = Garden.XpForLevel(defs[12].NeedGardenLevel) + 40;
            s.HintSeen = true;
            s.TutorialStep = Tutorial.Steps;
            s.Coins = new BigNum(8.4e15);
            Rabbits.Assign(s, Rabbits.ArriveKind(s, "tho-bong", "Bông").Id, 0);
            for (int i = 0; i < 11; i++) Rabbits.Arrive(s, 1, "Thỏ " + (i + 2), (i * 0.37) % 1d);
            foreach (RabbitKind kind in RabbitKinds.All) if (s.SeenKinds.Count < 8) s.SeenKinds.Add(kind.Id);
            s.FreshKinds.Clear();
            GardenGrid.Settle(defs, s);
            runner.Init(s);
            screen.RebuildUi();
            if (screen.Shell.State == ShellState.Title) screen.Shell.StartGame();
            if (screen.Shell.State != ShellState.Playing) screen.Shell.Resume();
            screen.SelectStation(-1);
            screen.Refresh();
            Step(screen, 4f); // past the garden level's banner
            screen.OpenSheet(0);
            Step(screen, 0.5f);
            var list = GameObject.Find("Canvas/Sheet/Panel/Scroll").GetComponent<UnityEngine.UI.ScrollRect>();
            Canvas.ForceUpdateCanvases();
            list.verticalNormalizedPosition = 0f;
            Step(screen, 0.3f);
            Capture(screen, "67-tram-moi-cho");
            screen.CloseSheet();
            Step(screen, 0.5f);
            screen.SetZoom(GardenScreen.ZoomLevel.Far, false);
            screen.LookAt(GardenScreen.CellToWorld(GardenGrid.DefaultStationTile(15)));
            Step(screen, 0.3f);
            Capture(screen, "67-xa-truoc-vong-8");
            screen.SetZoom(GardenScreen.ZoomLevel.Default, false);

            // The eighth ring: the forest steps back and the top of the garden takes the lotus garden and the candy kiln.
            Press(screen, "Canvas/Garden/Canopy/Forest Sign/Board");
            screen.Refresh();
            Step(screen, GardenScreen.RimStep + 1.2f);
            s.Levels[12] = 14; s.Levels[13] = 9;
            // The ninth: the rice paddy and the night tea boat.
            s.GardenXp = Garden.XpForLevel(defs[15].NeedGardenLevel) + 40;
            s.Coins = Expansion.CostOfNext(defs, s) * new BigNum(1.2);
            screen.Refresh();
            Step(screen, 4f);
            Press(screen, "Canvas/Garden/Canopy/Forest Sign/Board");
            screen.Refresh();
            Step(screen, GardenScreen.RimStep + 1.2f);
            s.Levels[14] = 6; s.Levels[15] = 3;
            s.Coins = new BigNum(8.4e17);
            Tray.Add(s, 13, false); Tray.Add(s, 13, false); Tray.Add(s, 15, true); Tray.Add(s, 11, false);
            screen.Refresh();
            Step(screen, 4f);
            screen.LookAt(screen.StationSpot(13) + new Vector2(0f, -120f));
            Step(screen, 0.5f);
            Capture(screen, "67-vong-moi");
            screen.SetZoom(GardenScreen.ZoomLevel.Far, false);
            screen.LookAt(screen.StationSpot(15)); // far, at the top: the band of the four over the shop and the pond
            Step(screen, 0.3f);
            Capture(screen, "67-xa-sau-vong-9");
            screen.SetZoom(GardenScreen.ZoomLevel.Default, false);
            screen.OpenSheet(0);
            Step(screen, 0.5f);
            Canvas.ForceUpdateCanvases();
            list.verticalNormalizedPosition = 0f;
            Step(screen, 0.3f);
            Capture(screen, "67-tram-moi-mo");
            screen.CloseSheet();
            Step(screen, 0.5f);
            Capture(screen, "67-khay-keo");
        }

        /// <summary>
        /// #66: the rewarded-ad seams on the editor's pretend videos. The welcome-back sheet once the day's free ×2 is
        /// spent (golden ×2 stays the main button, "Xem 30 giây" above it), the shop's golden row with its pill, the packs
        /// card with the no-ads pass, and a mooncake result once the day's rounds are spent, first with a video to watch,
        /// then with the pass ("Nhận ngay"). Player builds run on NoAds and show none of this.
        /// </summary>
        static void RenderAdSeams(GameRunner runner, GardenScreen screen)
        {
            GameState s = GameState.NewGame(runner.Catalog.BuildDefs());
            int[] levels = { 60, 30, 18, 12 };
            for (int i = 0; i < levels.Length; i++) s.Levels[i] = levels[i];
            s.Plots = 8;
            s.RestSpots = 3;
            s.GardenXp = Garden.XpForLevel(12);
            s.HintSeen = true;
            s.TutorialStep = Tutorial.Steps;
            s.Coins = new BigNum(3.6e6);
            s.GoldenCarrots = 12;
            Rabbits.Assign(s, Rabbits.ArriveKind(s, "tho-bong", "Bông").Id, 0);
            Rabbits.Assign(s, Rabbits.Arrive(s, 2, "Lam", 0.2).Id, 1);
            s.FreshKinds.Clear();
            runner.Init(s);
            runner.AdProvider = new FakeRewardedAds();
            if (screen.Shell.State != ShellState.Playing) screen.Shell.Resume();
            screen.SelectStation(-1);
            screen.Refresh();
            Step(screen, 0.4f);

            s.AwayDoubleDay = runner.TodayNumber; // the free ×2 was taken this morning
            var away = new AwayReport { Paid = new BigNum(8.4e8), Seconds = 2 * 3600, TrayAdded = 4, GuestsMissed = 2, SleepiestId = Away.Sleepiest(s) };
            runner.ShowAwayForTest(away);
            screen.ShowAway(away);
            Step(screen, 0.5f);
            Capture(screen, "66-di-vang-xem");
            screen.AwayCard.Close();
            Step(screen, 0.4f);

            screen.OpenShop();
            screen.Refresh();
            Step(screen, 0.5f);
            Capture(screen, "66-tiem-xem");
            Press(screen, "Canvas/Shop/Card/Have/BuyMore");
            Step(screen, 0.4f);
            Capture(screen, "66-ve-bo-quang-cao");
            screen.GoldenPacks.Close();
            screen.Shop.Close();
            Step(screen, 0.4f);

            s.PoundingDay = runner.TodayNumber;
            s.PoundingPlays = Pounding.PlaysPerDay - 1; // this is the day's last round
            screen.OpenPounding();
            Step(screen, 30f);
            screen.Refresh();
            Step(screen, 0.3f);
            Capture(screen, "66-them-luot");
            Ads.GrantPass(s, runner.TodayNumber);
            screen.Refresh();
            Step(screen, 0.2f);
            Capture(screen, "66-them-luot-nhan-ngay");
            screen.Pound.Close();
            s.NoAdsUntilDay = -1;
            screen.Refresh();
            Step(screen, 0.4f);
        }

        /// <summary>
        /// #53: the forest steps back ring by ring. For each of the first two rings: the sign at the lawn's edge once the garden
        /// can widen, halfway through the step (the rim fading over, the new grass coming up), and after; the whole garden from
        /// far away before and after each; and the sign in English and in large text.
        /// </summary>
        static void RenderForestRings(GameRunner runner, GardenScreen screen)
        {
            GameState s = GameState.NewGame(runner.Catalog.BuildDefs());
            s.HintSeen = true;
            s.TutorialStep = Tutorial.Steps;
            for (int i = 0; i < Plots.Starting; i++) s.Levels[i] = 6 - i;
            Rabbits.GiftFirst(s, "Bông");
            runner.Init(s);
            L10n.SetLocale("vi");
            screen.RebuildUi();
            screen.SelectStation(-1);
            screen.Refresh();
            Step(screen, 0.6f);
            for (int ring = 0; ring < 2; ring++)
            {
                ForestRing next = Expansion.Next(runner.Defs, s);
                s.GardenXp = System.Math.Max(s.GardenXp, Garden.XpForLevel(next.NeedLevel));
                while (s.Rabbits.Count < next.NeedRabbits) Rabbits.Arrive(s, 0, "Thỏ " + (s.Rabbits.Count + 1), (s.Rabbits.Count * 0.37) % 1d);
                s.Coins = Expansion.CostOfNext(runner.Defs, s) * new BigNum(1.2);
                for (int i = 0; i < s.Plots; i++) if (s.Levels[i] <= 0) s.Levels[i] = 1;
                screen.Refresh();
                Step(screen, 4f); // the new garden level's banner comes and goes first
                screen.SetZoom(GardenScreen.ZoomLevel.Far, false);
                Step(screen, 0.2f);
                Capture(screen, "53-vong-" + (ring + 1) + "-xa-truoc");
                screen.SetZoom(GardenScreen.ZoomLevel.Default, false);
                screen.LookAt(screen.ForestSignSpot + new Vector2(screen.ForestSignSpot.x < 540f ? 380f : -380f, 120f));
                Step(screen, 0.3f);
                Capture(screen, "53-vong-" + (ring + 1) + "-truoc");
                if (ring == 0)
                {
                    L10n.SetLocale("en");
                    screen.RebuildUi();
                    Step(screen, 0.6f);
                    Capture(screen, "53-bien-en");
                    L10n.SetLocale("vi");
                    UiKit.SetLargeText(true);
                    screen.RebuildUi();
                    Step(screen, 0.6f);
                    Capture(screen, "53-bien-chu-to");
                    UiKit.SetLargeText(false);
                    screen.RebuildUi();
                    Step(screen, 0.6f);
                }
                Press(screen, "Canvas/Garden/Canopy/Forest Sign/Board");
                screen.Refresh();
                Step(screen, GardenScreen.RimStep * 0.5f);
                Capture(screen, "53-vong-" + (ring + 1) + "-giua");
                Step(screen, GardenScreen.RimStep + 1.2f);
                Capture(screen, "53-vong-" + (ring + 1) + "-sau");
                screen.SetZoom(GardenScreen.ZoomLevel.Far, false);
                Step(screen, 0.2f);
                Capture(screen, "53-vong-" + (ring + 1) + "-xa-sau");
                screen.SetZoom(GardenScreen.ZoomLevel.Default, false);
            }
        }

        /// <summary>
        /// #54 arranging the garden: the mode on with a dashed ring on every empty tile and the first-time hint, a station
        /// lifted and carried, the garden after a swap (still arranging), and the garden built again after "Xong".
        /// </summary>
        static void RenderLayout(GameRunner runner, GardenScreen screen)
        {
            var defs = runner.Catalog.BuildDefs();
            GameState s = GameState.NewGame(defs);
            int[] levels = { 27, 12, 9, 4 };
            for (int i = 0; i < levels.Length; i++) s.Levels[i] = levels[i];
            s.GardenXp = Garden.XpForLevel(4);
            s.HintSeen = true;
            s.TutorialStep = Tutorial.Steps;
            s.Coins = new BigNum(84000);
            Rabbits.Assign(s, Rabbits.ArriveKind(s, "tho-bong", "Bông").Id, 0);
            Rabbits.Assign(s, Rabbits.Arrive(s, 2, "Lam", 0.2).Id, 1);
            Rabbits.Arrive(s, 1, "Mây", 0.4); // one resting on a stump
            s.FreshKinds.Clear();
            runner.Init(s);
            PlayerPrefs.DeleteKey(GardenScreen.LayoutHintKey);
            screen.RebuildUi();
            if (screen.Shell.State != ShellState.Playing) screen.Shell.Resume();
            screen.SelectStation(-1);
            screen.Refresh();
            screen.LookAt(screen.StationSpot(0) + new Vector2(0f, -80f));
            Step(screen, 0.5f);
            screen.EnterLayoutMode();
            Step(screen, 0.3f);
            Capture(screen, "54-bo-tri-bat");

            // The leaf basket held, then carried halfway towards an empty tile left of the teapot.
            Hex basket = s.Grid.TileOf(GardenGrid.StationId(defs[1])).Value, teapot = s.Grid.TileOf(GardenGrid.StationId(defs[0])).Value;
            Hex empty = basket;
            int best = int.MaxValue;
            foreach (Hex h in screen.LayoutEmptyTiles())
            {
                int d = GardenGrid.Distance(h, new Hex(-1, 1));
                if (d < best) { best = d; empty = h; }
            }
            screen.SimulateLayoutPress(screen.LayoutViewPoint(basket));
            Step(screen, GardenScreen.LayoutHoldSeconds + 0.05f);
            Vector2 half = (screen.LayoutViewPoint(basket) + screen.LayoutViewPoint(empty)) / 2f + new Vector2(60f, 230f); // above the teapot, clear of it
            screen.SimulateLayoutDrag(half);
            Step(screen, 0.1f);
            Capture(screen, "54-dang-nhac");
            screen.SimulateLayoutDrag(screen.LayoutViewPoint(empty));
            screen.SimulateLayoutRelease(screen.LayoutViewPoint(empty));
            Step(screen, 0.4f);

            // The jam pot dropped on the teapot: the two swap tiles.
            Hex jam = s.Grid.TileOf(GardenGrid.StationId(defs[2])).Value;
            screen.SimulateLayoutPress(screen.LayoutViewPoint(jam));
            Step(screen, GardenScreen.LayoutHoldSeconds + 0.05f);
            screen.SimulateLayoutDrag(screen.LayoutViewPoint(teapot));
            screen.SimulateLayoutRelease(screen.LayoutViewPoint(teapot));
            Step(screen, 0.5f);
            Capture(screen, "54-sau-hoan-doi");
            screen.ExitLayoutMode();
            screen.LookAt(screen.StationSpot(0) + new Vector2(0f, -80f));
            Step(screen, 0.5f);
            Capture(screen, "54-sau-xong");
        }

        /// <summary>
        /// QA nits 2 (#79–#82): the welcome-back sheet coming up over the title on a day whose eight guests were all served,
        /// a long kind's plate on a rabbit's own sheet and on its album page (in Vietnamese and English), and mushroom stools
        /// on the decoration spot by a station's level plate.
        /// </summary>
        static void RenderQaNits2()
        {
            EditorSceneManager.OpenScene(ContentSeeder.ScenePath, OpenSceneMode.Single);
            L10n.SetLocale("vi");
            var runner = Object.FindAnyObjectByType<GameRunner>();
            var screen = Object.FindAnyObjectByType<GardenScreen>();
            var defs = runner.Catalog.BuildDefs();
            GameState s = GameState.NewGame(defs);
            for (int i = 0; i < defs.Count; i++) s.Levels[i] = 30 - 2 * i;
            s.Plots = defs.Count;
            s.GardenXp = Garden.XpForLevel(9);
            s.HintSeen = true;
            s.TutorialStep = Tutorial.Steps;
            s.Coins = new BigNum(4.2e7);
            s.GoldenCarrots = 500;
            Rabbits.Assign(s, Rabbits.ArriveKind(s, "tho-den-ong-sao", "Đèn").Id, 0);
            Rabbits.Arrive(s, 1, "Mây", 0.4);
            s.FreshKinds.Clear();
            runner.Init(s);
            screen.Build();
            screen.ClockHours = () => 22.0;
            screen.Raining = () => false;
            screen.Refresh();
            Step(screen, 0.4f);

            // #79 and #80: today's eight guests have all been served, then three hours away; the sheet comes up over the title.
            s.OrderDay = runner.TodayNumber;
            s.OrdersToday = Orders.PerDay;
            long now = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            s.LastSeenUnixSeconds = now - 3 * 3600;
            AwayReport away = Away.Claim(runner.Defs, s, now, runner.Mods);
            runner.ShowAwayForTest(away);
            screen.ShowAway(away);
            Step(screen, 0.5f);
            Capture(screen, "79-di-vang-tren-tieu-de");
            screen.AwayCard.Close();
            Step(screen, 0.4f);
            screen.Shell.StartGame();
            Step(screen, 0.5f);

            // #81: a gold kind with a long name on its rabbit's own sheet, the red kind's album page, then the sheet in English.
            Rabbit lantern = s.Rabbits[0];
            screen.Detail.Open(runner.Defs, s, lantern.Id);
            Step(screen, 0.5f);
            Capture(screen, "81-the-tho-ten-dai");
            screen.Detail.OpenKind(runner.Defs, s, "tho-chom-sao");
            Step(screen, 0.5f);
            Capture(screen, "81-so-tho-ten-dai");
            screen.Detail.Close();
            Step(screen, 0.4f);
            L10n.SetLocale("en");
            screen.RebuildUi();
            screen.Detail.Open(runner.Defs, s, lantern.Id);
            Step(screen, 0.5f);
            Capture(screen, "81-the-tho-ten-dai-en");
            screen.Detail.Close();
            L10n.SetLocale("vi");
            screen.RebuildUi();
            Step(screen, 0.4f);

            // #82: mushroom stools on the first decoration spot whose stools reach a station's level plate.
            for (int i = 0; i < Decor.Spots; i++) { Decor.Buy(s, "ghe-nam"); Decor.Place(s, i, "ghe-nam"); }
            screen.RebuildUi();
            screen.Refresh();
            Step(screen, 0.3f);
            Canvas.ForceUpdateCanvases();
            Transform street = screen.transform.Find("Canvas/Garden/Content");
            // The highest plate on the lawn that a stool reaches, whatever the drawing order.
            int spot = -1;
            Vector2 look = screen.StationSpot(0);
            foreach (RectTransform plate in street.GetComponentsInChildren<RectTransform>(false))
            {
                if (plate.name != "Level" || plate.parent == null || !plate.parent.name.StartsWith("Station ")) continue;
                Rect p = StreetRectOf(plate, street);
                if (spot >= 0 && p.center.y <= look.y) continue;
                for (int i = 0; i < Decor.Spots; i++)
                {
                    Transform item = street.Find("Spot " + i + "/Item");
                    if (item != null && item.gameObject.activeInHierarchy && StreetRectOf((RectTransform)item, street).Overlaps(p)) { spot = i; look = p.center; break; }
                }
            }
            for (int i = 0; i < Decor.Spots; i++) if (i != spot) Decor.Store(s, i);
            screen.RebuildUi();
            screen.Refresh();
            screen.LookAt(look + new Vector2(0f, 60f));
            Step(screen, 0.5f);
            Capture(screen, "82-ghe-nam-canh-tram");
            if (spot < 0) Debug.LogWarning("[VuonTra] Preview #82: no decoration spot reaches a station plate");
        }

        static Rect StreetRectOf(RectTransform rt, Transform street)
        {
            var c = new Vector3[4];
            rt.GetWorldCorners(c);
            Vector3 a = street.InverseTransformPoint(c[0]), b = street.InverseTransformPoint(c[2]);
            return Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
        }

        /// <summary>
        /// #54 follow-up: the arranging mode by the firefly jar, with the dashed rings on the free tiles beside it, a stone
        /// lantern carried over the jar's tile, and the lantern let go there. Before the fix the jar's tile had a ring and the
        /// lantern stood on the "Đom đóm · còn N lượt" plate; now the jar, the leaf basket and the plates keep their tiles.
        /// </summary>
        static void RenderReservedProps()
        {
            EditorSceneManager.OpenScene(ContentSeeder.ScenePath, OpenSceneMode.Single);
            L10n.SetLocale("vi");
            var runner = Object.FindAnyObjectByType<GameRunner>();
            var screen = Object.FindAnyObjectByType<GardenScreen>();
            var defs = runner.Catalog.BuildDefs();
            GameState s = GameState.NewGame(defs);
            int[] levels = { 27, 12, 9, 4, 6, 5, 3, 2 };
            for (int i = 0; i < levels.Length; i++) s.Levels[i] = levels[i];
            s.Plots = levels.Length;
            GardenGrid.Settle(defs, s); // the lawn widens to five rings for the eight plots, out to the jar
            s.GardenXp = Garden.XpForLevel(9); // the mortar, the basket and the jar are all out
            s.HintSeen = true;
            s.TutorialStep = Tutorial.Steps;
            s.Coins = new BigNum(4.2e6);
            s.GoldenCarrots = 100;
            Decor.Buy(s, "den-da");
            Decor.Place(s, 7, "den-da"); // the spot by the tea shop, left of the jar
            s.FreshKinds.Clear();
            runner.Init(s);
            PlayerPrefs.SetInt(GardenScreen.LayoutHintKey, 1);
            screen.Build();
            screen.ClockHours = () => 22.0;
            screen.Raining = () => false;
            screen.Refresh();
            screen.Shell.StartGame();
            Step(screen, 1.4f);
            Hex lantern = s.Grid.TileOf(GardenGrid.DecorId(7)).Value, jar = GardenScreen.WorldToCell(GardenScreen.FireflyJarSpot);
            screen.LookAt((GardenScreen.CellToWorld(lantern) + GardenScreen.CellToWorld(jar)) / 2f + new Vector2(0f, 60f));
            Step(screen, 0.5f);
            screen.EnterLayoutMode();
            Step(screen, 0.3f);
            Capture(screen, "54r-vong-canh-lo");
            screen.SimulateLayoutPress(screen.LayoutViewPoint(lantern));
            Step(screen, GardenScreen.LayoutHoldSeconds + 0.05f);
            screen.SimulateLayoutDrag((screen.LayoutViewPoint(lantern) + screen.LayoutViewPoint(jar)) / 2f);
            screen.SimulateLayoutDrag(screen.LayoutViewPoint(jar));
            Step(screen, 0.1f);
            Capture(screen, "54r-keo-len-lo");
            screen.SimulateLayoutRelease(screen.LayoutViewPoint(jar));
            Step(screen, 0.6f);
            Capture(screen, "54r-tha-len-lo");
            screen.ExitLayoutMode();
            Step(screen, 0.5f);
            Capture(screen, "54r-sau-xong");
            PlayerPrefs.DeleteKey(GardenScreen.LayoutHintKey);
        }
    }
}
