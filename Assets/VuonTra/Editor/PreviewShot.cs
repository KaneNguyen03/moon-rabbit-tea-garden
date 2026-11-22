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
            screen.LookAt(GardenScreen.StationSpot(0) + new Vector2(0, -120f));
            Step(screen, 0.3f);
            Capture(screen, "34-nen-tam");
            screen.LookAt(new Vector2(540f, 2800f));
            Step(screen, 0.3f);
            Capture(screen, "34-nen-tren");
            // Station ranks (#49): the basket at rank 2 and the jam pot at rank 3 beside the rank-1 teapot.
            state.Levels[1] = 100;
            state.Levels[2] = 300;
            screen.Refresh();
            screen.LookAt(GardenScreen.StationSpot(0) + new Vector2(0, -120f));
            Step(screen, 0.3f);
            Capture(screen, "49-bac-tram");
            state.Levels[1] = 12;
            state.Levels[2] = 9;
            screen.Refresh();
            screen.LookAt(GardenScreen.StationSpot(0));
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
            screen.LookAt(GardenScreen.StationSpot(0) + new Vector2(-60f, 0f));
            Step(screen, 0.6f);
            Capture(screen, "2-dung-cu-nghe");
            screen.SetZoom(GardenScreen.ZoomLevel.Default, false);
            screen.LookAt(GardenScreen.StationSpot(0));
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
                    mover.Affection = Rabbits.AffectionLine - 1;
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
                screen.LookAt(GardenScreen.GuestSpot);
                Step(screen, GardenScreen.GuestWalkSeconds + 0.6f);
                Capture(screen, "20-khach");
                screen.OpenOrder();
                Step(screen, 0.4f);
                Capture(screen, "20-don");
                screen.OrderCard.Close();
                Step(screen, 0.3f);
            }
            // The same garden at noon and at dusk; night is every other frame.
            screen.SelectStation(-1);
            screen.LookAt(GardenScreen.StationSpot(0));
            screen.ClockHours = () => 13.0;
            Step(screen, 0.3f);
            Capture(screen, "21-ban-ngay");
            screen.ClockHours = () => 18.0;
            Step(screen, 0.3f);
            Capture(screen, "21-hoang-hon");
            screen.ClockHours = () => 22.0;
            Step(screen, 0.2f);

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
