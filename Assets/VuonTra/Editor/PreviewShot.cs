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
    public static class PreviewShot
    {
        public const string Folder = "Logs/preview";
        const int Width = 1080, Height = 1920;

        static Vector2 At(float x, float y) => GardenScreen.CellToWorld(new Hex(0, 0)) + new Vector2(x, y);

        [MenuItem("VuonTra/Render Preview")]
        public static void Render()
        {
            EditorSceneManager.OpenScene(ContentSeeder.ScenePath, OpenSceneMode.Single);
            L10n.SetLocale("vi");
            var runner = Object.FindAnyObjectByType<GameRunner>();
            var screen = Object.FindAnyObjectByType<GardenScreen>();

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
            state.HintSeen = true;
            state.TutorialStep = Tutorial.Steps;
            state.Coins = new BigNum(5.2e6);
            state.Energy = 73;
            Tray.Add(state, 0, false); Tray.Add(state, 0, false); Tray.Add(state, 2, false); Tray.Add(state, 4, true);
            runner.Init(state);

            screen.Build();
            screen.ClockHours = () => 22.0;
            screen.Refresh();
            if (Directory.Exists(Folder)) Directory.Delete(Folder, true);
            Directory.CreateDirectory(Folder);

            Step(screen, 0.4f);
            Capture(screen, "1-title");

            screen.Shell.StartGame();
            for (int f = 1; f <= 5; f++)
            {
                Step(screen, 0.2f);
                Capture(screen, "28-glide-" + f);
            }
            Step(screen, 0.4f);
            Capture(screen, "2-garden");
            screen.LookAt(screen.StationSpot(0) + new Vector2(0, -120f));
            Step(screen, 0.3f);
            Capture(screen, "34-nen-tam");
            screen.LookAt(At(1500f, 0f));
            Step(screen, 0.3f);
            Capture(screen, "34-nen-tren");
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
            screen.PlayPayout(0, new BigNum(1200));
            for (int f = 1; f <= 3; f++)
            {
                Step(screen, 0.15f);
                Capture(screen, "28-fly-" + f);
            }
            Step(screen, 0.6f);
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
            Press(screen, "Canvas/Tray/Slot 0");
            for (int f = 1; f <= 2; f++)
            {
                Step(screen, 0.18f);
                Capture(screen, "37-ban-" + f);
            }
            Step(screen, 1f);
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
            screen.LookAt(At(0f, 0f));
            Step(screen, 0.2f);
            Capture(screen, "z2-default");
            screen.LookAt(At(1600f, 0f));
            Step(screen, 0.2f);
            Capture(screen, "2c-top");
            screen.LookAt(At(-1600f, 0f));
            Step(screen, 0.2f);
            Capture(screen, "2d-bottom");

            screen.FocusStation(5);
            Step(screen, 0.4f);
            Capture(screen, "2b-card");
            Press(screen, "Canvas/Card/Buy");
            Step(screen, 0.45f);
            Capture(screen, "3-moment");
            Step(screen, 2.5f);

            string[] names = { "Bông", "Mây", "Sữa", "Mochi", "Đậu", "Kem", "Nếp", "Mật", "Bánh Trôi", "Su Su", "Khoai", "Tuyết" };
            int[] tiers = { 5, 4, 3, 2, 1, 0, 0, 0, 2, 1, 3, 0 };
            for (int i = 0; i < tiers.Length; i++)
                state.Rabbits.Add(new Rabbit { Id = ++state.NextRabbitId, Tier = tiers[i], Kind = RabbitKinds.OfTier(tiers[i])[RabbitKinds.OfTier(tiers[i]).Count - 1 - i % 3].Id, Name = names[i], Level = 1 + i * 2 });
            foreach (Rabbit seen in state.Rabbits) Rabbits.See(state, Rabbits.KindOf(seen));
            state.Clovers = 4;
            Rabbits.AutoAssign(runner.Defs, state);
            var itemDefs = runner.Catalog.BuildItems();
            for (int i = 0; i < itemDefs.Count; i++) if (itemDefs[i].Price <= 0) Items.Grant(state, itemDefs[i], (i * 2) % Rabbits.TierCount);
            Items.Equip(state, state.Items[0].Id, state.Rabbits[0].Id);
            Items.Equip(state, state.Items[3].Id, state.Rabbits[0].Id);
            state.TotalTaps = 37;
            runner.BuyUpgrade(0);

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
            Press(screen, "Canvas/Hud/Purse");
            Step(screen, 0.4f);
            Capture(screen, "32-income");
            screen.Income.Close();
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
                    break;
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

            Capture(screen, "22-open-0");
            screen.OpenSheet(0);
            for (int f = 1; f <= 5; f++)
            {
                Step(screen, 0.06f);
                Capture(screen, "22-open-" + f);
            }
            Step(screen, 0.5f);
            Capture(screen, "5-stations");
            screen.UpgradeAll.Open(runner.Defs, state);
            Step(screen, 0.4f);
            Capture(screen, "36-nang-tat-ca");
            screen.UpgradeAll.Close();
            Step(screen, 0.1f);
            Press(screen, "Canvas/Sheet/Panel/Scroll/Content/Row " + runner.Defs[3].Id);
            Step(screen, 0.5f);
            Capture(screen, "22-row-to-station");
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
            Press(screen, "Canvas/Sheet/Panel/Rabbits/Content/Summon/Button");
            Step(screen, 0.85f);
            Capture(screen, "18a-gate");
            Step(screen, 0.75f);
            Capture(screen, "18b-shadow");
            Step(screen, 1.6f);
            Capture(screen, "18c-arrived");
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

            screen.ReviewCard.Open(state.Profile.Code);
            Step(screen, 0.4f);
            Capture(screen, "109-the-danh-gia");
            screen.ReviewCard.ShowFeedback();
            screen.ReviewCard.Input.text = "Muốn thêm nhạc đêm cho các bé thỏ";
            Step(screen, 0.4f);
            Capture(screen, "109-gop-y");
            screen.ReviewCard.Close();
            screen.Shell.OpenPause();
            Step(screen, 0.6f);
            Capture(screen, "10-pause");
            {
                Sfx.Tune tuneBefore = Sfx.CurrentTune;
                for (int i = 0; i < 4 && Sfx.CurrentTune != Sfx.Tune.Dawn; i++) Press(screen, "Canvas/Pause/Card/Tune");
                Step(screen, 0.1f);
                Capture(screen, "63-tieng-tram");
                for (int i = 0; i < 4 && Sfx.CurrentTune != tuneBefore; i++) Press(screen, "Canvas/Pause/Card/Tune");
            }
            Press(screen, "Canvas/Pause/Card/Profile");
            Step(screen, 0.4f);
            Capture(screen, "17-profile");
            screen.Profile.Close();

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
                runner.Sync.Resolve(false, "{}", _ => false, null);
                runner.PushToCloud();
                screen.Profile.Open(runner.Defs, state, runner.Account, runner.CloudStatus);
                Step(screen, 0.4f);
                Capture(screen, "38-ho-so-may");
                screen.Profile.Close();
                runner.Cloud = new LocalOnlyCloudSave();
            }

            screen.Shell.Resume();
            Step(screen, 0.6f);
            UiKit.SetLargeText(true);
            screen.RebuildUi();
            Step(screen, 0.8f);
            screen.OpenSheet(0);
            Step(screen, 0.5f);
            Capture(screen, "33-chu-to");
            screen.CloseSheet();
            screen.ReviewCard.Open(state.Profile.Code);
            Step(screen, 0.4f);
            Capture(screen, "109-the-danh-gia-chu-to");
            screen.ReviewCard.ShowFeedback();
            screen.ReviewCard.Input.text = "Muốn thêm nhạc đêm cho các bé thỏ";
            Step(screen, 0.4f);
            Capture(screen, "109-gop-y-chu-to");
            screen.ReviewCard.Close();
            UiKit.SetLargeText(false);
            screen.RebuildUi();
            Step(screen, 0.8f);

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
            screen.ReviewCard.Open(state.Profile.Code);
            Step(screen, 0.4f);
            Capture(screen, "109-the-danh-gia-en");
            screen.ReviewCard.ShowFeedback();
            screen.ReviewCard.Input.text = "More night music for the rabbits";
            Step(screen, 0.4f);
            Capture(screen, "109-gop-y-en");
            screen.ReviewCard.Close();
            screen.Shell.OpenPause();
            Step(screen, 0.6f);
            Capture(screen, "14-pause-en");
            Press(screen, "Canvas/Pause/Card/Profile");
            Step(screen, 0.4f);
            Capture(screen, "17-profile-en");
            screen.Profile.Close();
            L10n.SetLocale("vi");
            screen.RebuildUi();

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
            screen.LookAt(At(1600f, 0f));
            Step(screen, 0.3f);
            Capture(screen, "15-all-top");
            screen.FocusStation(9);
            Step(screen, 0.4f);
            Capture(screen, "15-card-top");
            screen.FocusStation(11);
            Step(screen, 0.4f);
            Capture(screen, "15-card-bottom");
            screen.SelectStation(-1);
            screen.LookAt(At(-1600f, 0f));
            Step(screen, 0.3f);
            Capture(screen, "16-all-bottom");
            var purseWas = state.Coins;
            state.Coins = new BigNum(1e30);
            screen.Refresh();
            Step(screen, 0.2f);
            Capture(screen, "31-edges");
            screen.SetZoom(GardenScreen.ZoomLevel.Near, false);
            screen.LookAt(At(1700f, -200f));
            Step(screen, 0.2f);
            Capture(screen, "31-home");
            screen.SetZoom(GardenScreen.ZoomLevel.Default, false);
            screen.LookAt(At(0f, 0f));
            state.Coins = purseWas;
            screen.Refresh();
            Step(screen, 0.2f);
            state.RestSpots = RestSpots.Max;
            for (int i = 0; i < state.Rabbits.Count && i < 7; i++) state.Rabbits[i].Station = -1;
            screen.Refresh();
            screen.LookAt(At(-500f, -150f));
            Step(screen, 0.4f);
            Capture(screen, "17-rest-bottom");
            screen.LookAt(At(900f, -200f));
            Step(screen, 0.3f);
            Capture(screen, "17-rest-top");
            {
                screen.ClockHours = () => 12.0;
                screen.Refresh();
                Step(screen, screen.StageStroll(0, StrollActivity.Tea));
                int spot = screen.StrollSpotOf(0);
                if (spot >= 0) screen.LookAt(screen.LawnSpots[spot]);
                Step(screen, 0.4f);
                Capture(screen, "55-uong-tra");
                screen.ClockHours = () => 22.0;
                screen.Refresh();
                Step(screen, screen.StageStroll(1, StrollActivity.Firefly));
                spot = screen.StrollSpotOf(1);
                if (spot >= 0) screen.LookAt(screen.LawnSpots[spot]);
                Step(screen, 0.4f);
                Capture(screen, "55-duoi-dom-dom");
            }
            {
                Rabbit mover = null;
                foreach (Rabbit r in state.Rabbits) if (r.Station < 0) { mover = r; break; }
                if (mover != null && runner.AssignRabbit(mover.Id, 0))
                {
                    screen.Refresh();
                    screen.LookAt(At(-200f, -100f));
                    Step(screen, 0.7f);
                    Capture(screen, "28-tho-di-tram");
                    Step(screen, 3f);
                    mover.Affection = Rabbits.PointsFor(Rabbits.LineHearts) - Rabbits.PetPoints;
                    mover.PetDay = -1;
                    screen.FocusStation(0);
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

            state.GoldenCarrots = 12;
            screen.OpenShop();
            Step(screen, 0.4f);
            Capture(screen, "44-tiem");
            state.GoldenCarrots = 75;
            Item crown = Shop.BuyItem(state, itemDefs, "hat-vuong-mien");
            screen.Refresh();
            Step(screen, 0.3f);
            Capture(screen, "44-do-cho-tho");
            screen.Shop.Close();
            Step(screen, 0.3f);
            if (crown != null) Items.Equip(state, crown.Id, state.Rabbits[0].Id);
            state.GoldenCarrots = 20;
            Shop.DyeRabbit(state, state.Rabbits[0].Id, 0);
            screen.Refresh();
            screen.Detail.Open(runner.Defs, state, state.Rabbits[0].Id);
            Step(screen, 0.5f);
            Capture(screen, "44-nhuom");
            screen.Detail.Close();
            Step(screen, 0.3f);
            state.GoldenCarrots = 200;
            Decor.Buy(state, "den-da"); Decor.Buy(state, "bon-hoa"); Decor.Buy(state, "ghe-nam"); Decor.Buy(state, "tham-picnic"); Decor.Buy(state, "den-da");
            Decor.Place(state, 2, "den-da"); Decor.Place(state, 3, "bon-hoa"); Decor.Place(state, 4, "ghe-nam"); Decor.Place(state, 5, "tham-picnic");
            Decor.SetLanternColor(state, 2);
            screen.Refresh();
            screen.LookAt(At(300f, 0f));
            Step(screen, 0.5f);
            Capture(screen, "11-trang-tri");
            screen.OpenShop();
            Step(screen, 0.4f);
            Capture(screen, "11-ke-trang-tri");
            screen.Shop.Close();
            Step(screen, 0.3f);
            for (int i = 0; i < Decor.Spots; i++) Decor.Store(state, i);
            for (int i = 0; i < state.DecorOwned.Length; i++) state.DecorOwned[i] = 0;
            state.LanternColor = 0;
            screen.Refresh();
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
            var away = new AwayReport { Paid = new BigNum(1.2e9), Seconds = 3 * 3600, Capped = true, TrayAdded = 7, TrayFull = true, GuestsMissed = 3, SleepiestId = Away.Sleepiest(state) };
            runner.ShowAwayForTest(away);
            screen.ShowAway(away);
            Step(screen, 0.5f);
            Capture(screen, "69-di-vang");
            screen.AwayCard.Close();
            Step(screen, 0.4f);
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
            RenderForestRings(runner, screen);
            RenderMapEdges(runner, screen);
            RenderLayout(runner, screen);
            RenderPond(runner, screen);
            RenderRecipes(runner, screen);
            RenderAquarium(runner, screen);
            RenderNewcomer(runner, screen);
            RenderOffers(runner, screen);
            RenderQuestChests(runner, screen);
            RenderAchievementTiers(runner, screen);
            RenderMonthPass(runner, screen);
            RenderWelcomeBack(runner, screen);
            RenderTrunk(runner, screen);
            RenderIntros(runner, screen);
            RenderQaNits2();
            RenderReservedProps();
            RenderGuestByPond();
            RenderNgangNits();
            RenderStationSpacing();

            Debug.Log("[VuonTra] Previews written to " + Folder);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

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

            Vector2 side = (screen.StationSpot(4) + screen.StationSpot(5)) / 2f;
            screen.LookAt(side);
            Step(screen, 0.3f);
            Capture(screen, "51-mua-dot-1-truoc");
            s.Plots = Expansion.PlotsForSegments(runner.Defs, 1);
            screen.Refresh();
            Step(screen, GardenScreen.RimStep * 0.4f);
            Capture(screen, "51-mua-dot-1-giua");
            Step(screen, GardenScreen.RimStep + 0.4f);
            Capture(screen, "51-mua-dot-1-sau");

            screen.SetZoom(GardenScreen.ZoomLevel.Far, false);
            for (int wave = 0; wave <= Expansion.Table.Length; wave++)
            {
                int plots = Expansion.PlotsForSegments(runner.Defs, wave);
                for (int i = 0; i < s.Levels.Length; i++) s.Levels[i] = i < plots - 2 || i < Plots.Starting ? Mathf.Max(1, 6 - i) : 0;
                s.Plots = plots;
                screen.Refresh();
                Step(screen, GardenScreen.RimStep + 0.4f);
                Capture(screen, "51-dot-" + wave);
            }
            screen.SetZoom(GardenScreen.ZoomLevel.Default, false);
        }

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

        static void RenderChatter(GameRunner runner, GardenScreen screen)
        {
            GameState s = runner.State;
            if (screen.Shell.State != ShellState.Playing) screen.Shell.Resume();
            PhotoAsks.Decline(s);
            s.Order = null;
            screen.SelectStation(-1);
            screen.SetZoom(GardenScreen.ZoomLevel.Default, false);
            screen.ClockHours = () => 13.0;
            screen.Refresh();
            Step(screen, 4f);
            WakeEveryone(screen, s);
            Step(screen, 2f);
            SayUntil(screen, s, key => Chatter.IsTradeLine(key), false);
            Capture(screen, "59-noi-ngay");
            s.RestSpots = Mathf.Max(s.RestSpots, 2);
            Rabbits.Arrive(s, 1, "Mây", 0d);
            screen.ClockHours = () => 22.0;
            screen.Refresh();
            Step(screen, 3f);
            WakeEveryone(screen, s);
            Step(screen, 2f);
            SayUntil(screen, s, key => key.StartsWith("chatter.phase.Night."), true);
            Capture(screen, "59-noi-dem");
            Step(screen, 3f);
        }

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
            screen.DragSheet(GardenScreen.SheetFull);
            screen.ReleaseSheet();
            Step(screen, 0.6f);
            Capture(screen, "61-so-tho-bo");
            Rabbits.ArriveKind(s, baker.Kinds[baker.Kinds.Count - 1].Id, "Ca Cao");
            runner.AutoAssignRabbits();
            screen.Refresh();
            Step(screen, 0.45f);
            Capture(screen, "61-du-bo");
            screen.ShowRabbitAlbum(false);
            screen.CloseSheet();
            Step(screen, 1.6f);
        }

        static void RenderAchievementTiers(GameRunner runner, GardenScreen screen)
        {
            GameState s = GameState.NewGame(runner.Catalog.BuildDefs());
            int[] levels = { 120, 80, 60, 40, 30, 24, 12, 8 };
            for (int i = 0; i < levels.Length; i++) s.Levels[i] = levels[i];
            s.Plots = 10;
            s.GardenXp = Garden.XpForLevel(24) + 120;
            s.HintSeen = true;
            s.TutorialStep = Tutorial.Steps;
            s.Coins = new BigNum(4.2e14);
            s.TotalEarned = new BigNum(3.1e26);
            s.TotalTaps = 23480;
            s.LevelsBought = 4120;
            s.OrdersDelivered = 62;
            s.FishCaught = 64;
            s.PhotosTaken = 500;
            s.PoundingGames = 31; s.Leaves.Games = 17; s.Fireflies.Games = 9;
            Rabbits.Assign(s, Rabbits.ArriveKind(s, "tho-bong", "Bông").Id, 0);
            Rabbits.Assign(s, Rabbits.Arrive(s, 2, "Lam", 0.2).Id, 1);
            s.FreshKinds.Clear();
            foreach (string id in new[] { "cham-100", "cham-1000", "thu-1k", "thu-1m", "thu-1b", "ca-50", "don-20", "mo-2", "mo-4", "mo-8", "tra-10", "tra-25", "tra-50", "bua-1" }) s.Claimed.Add(id);
            foreach (string id in new[] { "ca.1", "ca.2", "don.1", "nang.1", "van.1", "van.2", "cap.1" }) s.Claimed.Add(id);
            for (int tier = 1; tier <= 8; tier++) s.Claimed.Add("anh." + tier);
            runner.Init(s);
            if (screen.Shell.State != ShellState.Playing) screen.Shell.Resume();
            screen.SelectStation(-1);
            screen.Refresh();
            Step(screen, 4f);
            foreach (string language in new[] { "vi", "en" })
            {
                L10n.SetLocale(language);
                screen.RebuildUi();
                if (screen.Shell.State != ShellState.Playing) screen.Shell.Resume();
                ShowAchievements(screen);
                Capture(screen, "111-" + language + "-thanh-tuu");
                screen.CloseSheet();
                Step(screen, 0.5f);
            }
            L10n.SetLocale("vi");
            screen.RebuildUi();
            if (screen.Shell.State != ShellState.Playing) screen.Shell.Resume();
            screen.OpenSheet(2);
            screen.DragSheet(500f);
            screen.ReleaseSheet();
            Step(screen, 0.5f);
            Capture(screen, "111-vi-san-nhan");
            Press(screen, "Canvas/Sheet/Panel/Quests/Content/Quest cham/Claim");
            Step(screen, 1.2f);
            ShowAchievements(screen);
            Capture(screen, "111-vi-sau-nhan");
            screen.CloseSheet();
            Step(screen, 0.5f);
            UiKit.SetLargeText(true);
            screen.RebuildUi();
            if (screen.Shell.State != ShellState.Playing) screen.Shell.Resume();
            ShowAchievements(screen);
            Capture(screen, "111-vi-chu-to");
            screen.CloseSheet();
            Step(screen, 0.5f);
            UiKit.SetLargeText(false);
            screen.RebuildUi();
        }

        static void ShowAchievements(GardenScreen screen)
        {
            screen.OpenSheet(2);
            screen.DragSheet(500f);
            screen.ReleaseSheet();
            Step(screen, 0.5f);
            var scroll = screen.transform.Find("Canvas/Sheet/Panel/Quests").GetComponent<ScrollRect>();
            Canvas.ForceUpdateCanvases();
            RectTransform heading = null;
            int headings = 0;
            foreach (RectTransform child in scroll.content)
                if (child.name == "Heading" && ++headings == 2) heading = child;
            float top = -heading.anchoredPosition.y - heading.rect.height * (1f - heading.pivot.y);
            scroll.StopMovement();
            scroll.content.anchoredPosition = new Vector2(scroll.content.anchoredPosition.x, top - 12f);
            Step(screen, 0.3f);
        }

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
            s.MoonJarBuyDay = runner.TodayNumber;
            s.MoonJarsBoughtToday = 1;
            screen.OpenShop();
            screen.Refresh();
            Step(screen, 0.5f);
            Capture(screen, "64-ke-hu");
            screen.Shop.Close();
            Step(screen, 0.4f);
        }

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
            Step(screen, SummonMomentView.Length + 0.4f);
            Capture(screen, "1-mua-trang-tho-moi");
            screen.Arrival.Tap();
            screen.Arrival.Tap();
            Step(screen, 0.6f);
            Capture(screen, "1-mua-trang-vuon-moi");

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
            cam.Render();
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
            Step(screen, 4f);
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

            Press(screen, "Canvas/Garden/Content/Forest Sign/Board");
            screen.Refresh();
            Step(screen, GardenScreen.RimStep + 1.2f);
            s.Levels[12] = 14; s.Levels[13] = 9;
            s.GardenXp = Garden.XpForLevel(defs[15].NeedGardenLevel) + 40;
            s.Coins = Expansion.CostOfNext(defs, s) * new BigNum(1.2);
            screen.Refresh();
            Step(screen, 4f);
            Press(screen, "Canvas/Garden/Content/Forest Sign/Board");
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
            screen.LookAt(At(0f, 0f));
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

            s.AwayDoubleDay = runner.TodayNumber;
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
            s.PoundingPlays = Pounding.PlaysPerDay - 1;
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
                MistSegment next = Expansion.Next(runner.Defs, s);
                s.GardenXp = System.Math.Max(s.GardenXp, Garden.XpForLevel(next.NeedLevel));
                while (s.Rabbits.Count < next.NeedRabbits) Rabbits.Arrive(s, 0, "Thỏ " + (s.Rabbits.Count + 1), (s.Rabbits.Count * 0.37) % 1d);
                s.Coins = Expansion.CostOfNext(runner.Defs, s) * new BigNum(1.2);
                for (int i = 0; i < s.Plots; i++) if (s.Levels[i] <= 0) s.Levels[i] = 1;
                screen.Refresh();
                Step(screen, 4f);
                screen.SetZoom(GardenScreen.ZoomLevel.Far, false);
                Step(screen, 0.2f);
                Capture(screen, "53-vong-" + (ring + 1) + "-xa-truoc");
                screen.SetZoom(GardenScreen.ZoomLevel.Default, false);
                screen.LookAt(screen.ForestSignSpot + new Vector2(0f, 120f));
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
                Press(screen, "Canvas/Garden/Content/Forest Sign/Board");
                screen.Refresh();
                Step(screen, GardenScreen.RimStep / 3f + GardenScreen.MistClear * 0.4f);
                Capture(screen, "53-vong-" + (ring + 1) + "-giua");
                Step(screen, GardenScreen.MistClear + 0.6f);
                Capture(screen, "53-vong-" + (ring + 1) + "-sau");
                screen.SetZoom(GardenScreen.ZoomLevel.Far, false);
                Step(screen, 0.2f);
                Capture(screen, "53-vong-" + (ring + 1) + "-xa-sau");
                screen.SetZoom(GardenScreen.ZoomLevel.Default, false);
            }
        }

        [MenuItem("VuonTra/Render Map Edges")]
        public static void RenderMapEdgesOnly()
        {
            EditorSceneManager.OpenScene(ContentSeeder.ScenePath, OpenSceneMode.Single);
            L10n.SetLocale("vi");
            var runner = Object.FindAnyObjectByType<GameRunner>();
            var screen = Object.FindAnyObjectByType<GardenScreen>();
            runner.Init(GameState.NewGame(runner.Catalog.BuildDefs()));
            screen.Build();
            screen.ClockHours = () => 22.0;
            Directory.CreateDirectory(Folder);
            RenderMapEdges(runner, screen);
        }

        [MenuItem("VuonTra/Render Quest Chests")]
        public static void RenderQuestChestsOnly()
        {
            EditorSceneManager.OpenScene(ContentSeeder.ScenePath, OpenSceneMode.Single);
            L10n.SetLocale("vi");
            var runner = Object.FindAnyObjectByType<GameRunner>();
            var screen = Object.FindAnyObjectByType<GardenScreen>();
            runner.Init(GameState.NewGame(runner.Catalog.BuildDefs()));
            screen.Build();
            screen.ClockHours = () => 22.0;
            Directory.CreateDirectory(Folder);
            RenderQuestChests(runner, screen);
        }

        static void RenderQuestChests(GameRunner runner, GardenScreen screen)
        {
            foreach (string locale in new[] { "vi", "en" })
            {
                GameState s = GameState.NewGame(runner.Catalog.BuildDefs());
                int[] levels = { 40, 22, 14, 8 };
                for (int i = 0; i < levels.Length; i++) s.Levels[i] = levels[i];
                s.Plots = 8;
                s.GardenXp = Garden.XpForLevel(12);
                s.HintSeen = true;
                s.TutorialStep = Tutorial.Steps;
                s.Coins = new BigNum(2.4e6);
                s.Profile.Code = "VT-103PREV";
                Rabbits.Assign(s, Rabbits.ArriveKind(s, "tho-bong", "Bông").Id, 0);
                Rabbits.Assign(s, Rabbits.Arrive(s, 1, "Mây", 0.4).Id, 1);
                s.FreshKinds.Clear();
                runner.Init(s);
                long today = runner.TodayNumber;
                var dailies = new System.Collections.Generic.List<QuestDef>();
                foreach (QuestDef q in runner.QuestDefs) if (q.Daily) dailies.Add(q);
                for (int i = 0; i < 3; i++) s.Claimed.Add(dailies[i].Id);
                s.WeekIndex = QuestChests.WeekOf(today);
                s.WeekPoints = 130;
                s.WeekChestsClaimed = 0;
                L10n.SetLocale(locale);
                screen.RebuildUi();
                if (screen.Shell.State != ShellState.Playing) screen.Shell.Resume();
                screen.SelectStation(-1);
                screen.OpenSheet(2);
                screen.Refresh();
                Step(screen, 6f);
                Capture(screen, "103-" + locale + "-hom-nay-3-5");

                foreach (QuestDef q in dailies) s.Claimed.Add(q.Id);
                s.WeekPoints = 150;
                screen.Refresh();
                Step(screen, 1.75f);
                Capture(screen, "103-" + locale + "-hop-rung");
                Press(screen, "Canvas/Sheet/Panel/Quests/Content/QuestBoxRow/Box");
                Step(screen, 0.75f);
                Capture(screen, "103-" + locale + "-hop-lat");
                Step(screen, 1.4f);
                Capture(screen, "103-" + locale + "-hop-mo");
                screen.Prizes.Close();
                screen.Refresh();
                Step(screen, 0.4f);
                Capture(screen, "103-" + locale + "-tuan-ruong-san");
                Press(screen, "Canvas/Sheet/Panel/Quests/Content/QuestWeekRow/Track/Chest 0");
                Step(screen, 2.2f);
                Capture(screen, "103-" + locale + "-ruong-mo");
                screen.Prizes.Close();
                screen.CloseSheet();
            }
            L10n.SetLocale("vi");
            screen.RebuildUi();
            Step(screen, 0.3f);
        }

        [MenuItem("VuonTra/Render Month Pass")]
        public static void RenderMonthPassOnly()
        {
            EditorSceneManager.OpenScene(ContentSeeder.ScenePath, OpenSceneMode.Single);
            L10n.SetLocale("vi");
            var runner = Object.FindAnyObjectByType<GameRunner>();
            var screen = Object.FindAnyObjectByType<GardenScreen>();
            runner.Init(GameState.NewGame(runner.Catalog.BuildDefs()));
            screen.Build();
            screen.ClockHours = () => 22.0;
            Directory.CreateDirectory(Folder);
            RenderMonthPass(runner, screen);
        }

        static void RenderMonthPass(GameRunner runner, GardenScreen screen)
        {
            System.Func<long> clock = runner.DayClock;
            long day = GiftCodes.Day(2026, 12, 12);
            runner.DayClock = () => day;
            foreach (string locale in new[] { "vi", "en", "vi-large" })
            {
                bool large = locale == "vi-large";
                GameState s = GameState.NewGame(runner.Catalog.BuildDefs());
                int[] levels = { 40, 22, 14, 8 };
                for (int i = 0; i < levels.Length; i++) s.Levels[i] = levels[i];
                s.Plots = 8;
                s.GardenXp = Garden.XpForLevel(12);
                s.HintSeen = true;
                s.TutorialStep = Tutorial.Steps;
                s.Coins = new BigNum(2.4e6);
                s.Profile.Code = "VT-107PREV";
                Rabbits.Assign(s, Rabbits.ArriveKind(s, "tho-bong", "Bông").Id, 0);
                Rabbits.Assign(s, Rabbits.Arrive(s, 1, "Mây", 0.4).Id, 1);
                s.FreshKinds.Clear();
                runner.Init(s);
                s.WeekIndex = QuestChests.WeekOf(day);
                s.WeekPoints = 160;
                s.PassMonth = MonthPass.MonthOf(day);
                s.PassPoints = 0;
                L10n.SetLocale(large ? "vi" : locale);
                if (large) UiKit.SetLargeText(true);
                string name = "107-" + locale + "-";
                if (!large)
                {
                    ShowMonthPass(screen);
                    Capture(screen, name + "nac-0");
                }

                s.PassPoints = 12 * MonthPass.PointsPerTier + 15;
                s.PassFreeClaimed = (1 << 10) - 1;
                if (!large)
                {
                    ShowMonthPass(screen);
                    Capture(screen, name + "nac-12-chua-mua");
                }

                MonthPass.Buy(s, day);
                ShowMonthPass(screen);
                Capture(screen, name + "nac-12-da-mua");
                screen.CloseSheet();
                if (large) UiKit.SetLargeText(false);
            }
            runner.DayClock = clock;
            L10n.SetLocale("vi");
            screen.RebuildUi();
            Step(screen, 0.3f);
        }

        static void ShowMonthPass(GardenScreen screen)
        {
            screen.RebuildUi();
            if (screen.Shell.State != ShellState.Playing) screen.Shell.Resume();
            screen.SelectStation(-1);
            screen.OpenSheet(2);
            screen.Refresh();
            Step(screen, 1.5f);
            RectTransform row = screen.MonthPassRows.Root;
            var content = (RectTransform)row.parent;
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
            float top = -row.anchoredPosition.y - row.rect.height * (1f - row.pivot.y);
            content.anchoredPosition = new Vector2(content.anchoredPosition.x, top - 24f);
            screen.Refresh();
            Step(screen, 0.2f);
        }

        [MenuItem("VuonTra/Render Welcome Back")]
        public static void RenderWelcomeBackOnly()
        {
            EditorSceneManager.OpenScene(ContentSeeder.ScenePath, OpenSceneMode.Single);
            L10n.SetLocale("vi");
            var runner = Object.FindAnyObjectByType<GameRunner>();
            var screen = Object.FindAnyObjectByType<GardenScreen>();
            runner.Init(GameState.NewGame(runner.Catalog.BuildDefs()));
            screen.Build();
            screen.ClockHours = () => 22.0;
            Directory.CreateDirectory(Folder);
            RenderWelcomeBack(runner, screen);
        }

        static GameState WelcomeBackGarden(GameRunner runner, long today, int awayDays)
        {
            GameState s = GameState.NewGame(runner.Catalog.BuildDefs());
            int[] levels = { 40, 22, 14, 8, 3 };
            for (int i = 0; i < levels.Length; i++) s.Levels[i] = levels[i];
            s.Plots = 8;
            s.GardenXp = Garden.XpForLevel(12) + 40;
            s.HintSeen = true;
            s.TutorialStep = Tutorial.Steps;
            s.Coins = new BigNum(1.8e6);
            s.Newcomer.Closed = true;
            Rabbits.Assign(s, Rabbits.ArriveKind(s, "tho-bong", "Bông").Id, 0);
            Rabbit loved = Rabbits.ArriveKind(s, "tho-me", "Mè");
            Rabbits.Assign(s, loved.Id, 1);
            Rabbits.AddBond(s, loved, Rabbits.PointsFor(6));
            Rabbits.Assign(s, Rabbits.Arrive(s, 1, "Mây", 0.4).Id, 2);
            s.FreshKinds.Clear();
            s.FirstDaySeen = today - awayDays - 20;
            s.MaxDaySeen = today - awayDays;
            s.WelcomeBack.SeenDay = s.MaxDaySeen;
            s.MailDay = s.MaxDaySeen;
            s.GiftDay = today;
            return s;
        }

        static void ShowWelcomeBack(GameRunner runner, GardenScreen screen, string locale)
        {
            L10n.SetLocale(locale);
            screen.RebuildUi();
            if (screen.Shell.State == ShellState.Title) screen.Shell.StartGame();
            if (screen.Shell.State != ShellState.Playing) screen.Shell.Resume();
            screen.CloseSheet();
            screen.SelectStation(-1);
            screen.LookAt(screen.StationSpot(0));
            screen.Refresh();
            Step(screen, 0.6f);
        }

        static void RenderWelcomeBack(GameRunner runner, GardenScreen screen)
        {
            const long today = 20781;
            System.Func<long> clockBefore = runner.DayClock;
            bool cardsBefore = runner.OfferCardsOn;
            runner.OfferCardsOn = false;
            runner.DayClock = () => today;
            foreach (string locale in new[] { "vi", "en" })
            {
                string tail = locale == "vi" ? "" : "-en";
                GameState s = WelcomeBackGarden(runner, today, 5);
                runner.Init(s);
                ShowWelcomeBack(runner, screen, locale);
                if (!screen.WelcomeCard.IsOpen) throw new System.Exception("Preview: the welcome-back card did not open");
                Capture(screen, "108-the-vuon-nho-ban" + tail);
                if (locale == "vi")
                {
                    UiKit.SetLargeText(true);
                    s.WelcomeBack.CardSeen = false;
                    ShowWelcomeBack(runner, screen, locale);
                    Capture(screen, "108-the-vuon-nho-ban-chu-to");
                    UiKit.SetLargeText(false);
                }

                s.WelcomeBack.StartDay = today - 2;
                s.WelcomeBack.CheckIns = 2;
                s.WelcomeBack.CheckInDay = today - 1;
                s.WelcomeBack.CardSeen = true;
                ShowWelcomeBack(runner, screen, locale);
                Capture(screen, "108-vien-ngay-3-7" + tail);
                Press(screen, "Canvas/Hud/WelcomeBack");
                Step(screen, 0.5f);
                Capture(screen, "108-diem-danh-ngay-3" + tail);
                Press(screen, "Canvas/WelcomeBackCard/Card/Claim");
                Step(screen, 0.5f);
                Capture(screen, "108-diem-danh-da-nhan" + tail);
                screen.WelcomeCard.Close();
            }
            runner.OfferCardsOn = cardsBefore;
            runner.DayClock = clockBefore;
            L10n.SetLocale("vi");
            screen.RebuildUi();
            Step(screen, 0.3f);
        }

        [MenuItem("VuonTra/Render Trunk")]
        public static void RenderTrunkOnly()
        {
            EditorSceneManager.OpenScene(ContentSeeder.ScenePath, OpenSceneMode.Single);
            L10n.SetLocale("vi");
            var runner = Object.FindAnyObjectByType<GameRunner>();
            var screen = Object.FindAnyObjectByType<GardenScreen>();
            runner.Init(GameState.NewGame(runner.Catalog.BuildDefs()));
            screen.Build();
            screen.ClockHours = () => 22.0;
            Directory.CreateDirectory(Folder);
            RenderTrunk(runner, screen);
        }

        static void RenderTrunk(GameRunner runner, GardenScreen screen)
        {
            const long today = 20781;
            System.Func<long> clockBefore = runner.DayClock, localBefore = runner.LocalClock;
            bool cardsBefore = runner.OfferCardsOn;
            runner.OfferCardsOn = false;
            runner.DayClock = () => today;
            long clock = today * 86400L + 14 * 3600L;
            runner.LocalClock = () => clock;
            foreach (string locale in new[] { "vi", "en" })
            {
                string tail = locale == "vi" ? "" : "-en";
                clock = today * 86400L + 14 * 3600L;
                GameState s = WelcomeBackGarden(runner, today, 0);
                s.WelcomeBack.SeenDay = today;
                s.GoldenCarrots = 64;
                runner.Init(s);
                ShowWelcomeBack(runner, screen, locale);
                screen.LookAt(screen.ShopSpot);
                screen.Refresh();
                Step(screen, 0.4f);
                Capture(screen, "113-ruong-canh-tiem" + tail);
                screen.OpenTrunk();
                Step(screen, 0.5f);
                if (!screen.TrunkCard.IsOpen || screen.TrunkCard.ShowsGoods) throw new System.Exception("Preview: the closed trunk card did not open");
                Capture(screen, "113-ruong-dong" + tail);
                screen.TrunkCard.Close();

                clock = today * 86400L + 19 * 3600L + 1200L;
                screen.Refresh();
                Step(screen, 0.3f);
                Capture(screen, "113-ruong-mo-canh-tiem" + tail);
                screen.OpenTrunk();
                Step(screen, 0.5f);
                if (!screen.TrunkCard.ShowsGoods) throw new System.Exception("Preview: the open trunk shows no goods");
                Capture(screen, "113-ruong-mo" + tail);
                if (locale == "vi")
                {
                    UiKit.SetLargeText(true);
                    screen.TrunkCard.Close();
                    ShowWelcomeBack(runner, screen, locale);
                    screen.OpenTrunk();
                    Step(screen, 0.5f);
                    Capture(screen, "113-ruong-mo-chu-to");
                    UiKit.SetLargeText(false);
                    screen.TrunkCard.Close();
                    ShowWelcomeBack(runner, screen, locale);
                    screen.OpenTrunk();
                    Step(screen, 0.3f);
                }
                int item = 1;
                foreach (TrunkOffer offer in screen.TrunkCard.Shelf.Offers)
                    if (offer.Kind != TrunkKind.SeasonRabbit && offer.Kind != TrunkKind.GoldenCall) { item = offer.Slot + 1; break; }
                Press(screen, "Canvas/TrunkCard/Card/Goods/Item " + item + "/Buy");
                Press(screen, "Canvas/TrunkCard/Card/Goods/Item " + item + "/Buy");
                Step(screen, 0.5f);
                if (screen.TrunkCard.IsOpen) Capture(screen, "113-ruong-da-mua" + tail);
                screen.TrunkCard.Close();
                if (screen.Arrival.IsOpen) screen.Arrival.Close();
            }
            runner.OfferCardsOn = cardsBefore;
            runner.DayClock = clockBefore;
            runner.LocalClock = localBefore;
            L10n.SetLocale("vi");
            screen.RebuildUi();
            Step(screen, 0.3f);
        }

        static void RenderMapEdges(GameRunner runner, GardenScreen screen)
        {
            var defs = runner.Catalog.BuildDefs();
            foreach (int segments in new[] { 0, 4, 5, 6 })
            {
                GameState s = GameState.NewGame(defs);
                s.HintSeen = true;
                s.TutorialStep = Tutorial.Steps;
                s.Plots = Expansion.PlotsForSegments(defs, segments);
                s.Grid.Segments = segments;
                for (int i = 0; i < s.Plots; i++) s.Levels[i] = 8 - i / 3;
                Rabbits.GiftFirst(s, "Bông");
                MistSegment next = Expansion.Next(defs, s);
                if (next != null)
                {
                    s.GardenXp = Garden.XpForLevel(next.NeedLevel);
                    while (s.Rabbits.Count < next.NeedRabbits) Rabbits.Arrive(s, 0, "Thỏ " + (s.Rabbits.Count + 1), (s.Rabbits.Count * 0.37) % 1d);
                    foreach (RabbitKind kind in RabbitKinds.All) if (s.SeenKinds.Count < next.NeedKinds) s.SeenKinds.Add(kind.Id);
                    s.Coins = Expansion.CostOfNext(defs, s) * new BigNum(1.2);
                }
                runner.Init(s);
                screen.RebuildUi();
                screen.SelectStation(-1);
                screen.Refresh();
                screen.Shell.StartGame();
                Step(screen, 2f);
                string name = "91c-" + segments + "-doan";
                Debug.Log("91c preview " + segments + " segments: can widen " + Expansion.CanExpand(defs, s) + ", sign at " + screen.ForestSignSpot);
                if (next != null)
                {
                    screen.LookAt(screen.ForestSignSpot + new Vector2(0f, 120f));
                    Step(screen, 0.5f);
                    Capture(screen, name + "-bien");
                }
                screen.SetZoom(GardenScreen.ZoomLevel.Default, false);
                screen.LookAt(new Vector2(screen.StationSpot(0).x, 99999f));
                Step(screen, 0.5f);
                Capture(screen, name + "-tren");
                screen.LookAt(new Vector2(screen.StationSpot(0).x, -99999f));
                Step(screen, 0.5f);
                Capture(screen, name + "-duoi");
                screen.LookAt(new Vector2(-99999f, screen.StationSpot(0).y));
                Step(screen, 0.5f);
                Capture(screen, name + "-trai");
                screen.LookAt(new Vector2(99999f, screen.StationSpot(0).y));
                Step(screen, 0.5f);
                Capture(screen, name + "-phai");
                screen.SetZoom(GardenScreen.ZoomLevel.Far, false);
                screen.LookAt(screen.StationSpot(0));
                Step(screen, 0.5f);
                Capture(screen, name + "-xa");
                screen.SetZoom(GardenScreen.ZoomLevel.Default, false);
            }
        }

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
            Rabbits.Arrive(s, 1, "Mây", 0.4);
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
            Vector2 half = (screen.LayoutViewPoint(basket) + screen.LayoutViewPoint(empty)) / 2f + new Vector2(60f, 230f);
            screen.SimulateLayoutDrag(half);
            Step(screen, 0.1f);
            Capture(screen, "54-dang-nhac");
            screen.SimulateLayoutDrag(screen.LayoutViewPoint(empty));
            screen.SimulateLayoutRelease(screen.LayoutViewPoint(empty));
            Step(screen, 0.4f);

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

            for (int i = 0; i < Decor.Spots; i++) { Decor.Buy(s, "ghe-nam"); Decor.Place(s, i, "ghe-nam"); }
            screen.RebuildUi();
            screen.Refresh();
            Step(screen, 0.3f);
            Canvas.ForceUpdateCanvases();
            Transform street = screen.transform.Find("Canvas/Garden/Content");
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

        static void RenderAquarium(GameRunner runner, GardenScreen screen)
        {
            var defs = runner.Catalog.BuildDefs();
            GameState s = GameState.NewGame(defs);
            int[] levels = { 27, 12, 9, 4 };
            for (int i = 0; i < levels.Length; i++) s.Levels[i] = levels[i];
            s.GardenXp = Garden.XpForLevel(6);
            s.HintSeen = true;
            s.TutorialStep = Tutorial.Steps;
            s.Newcomer.Closed = true;
            s.Coins = new BigNum(84000);
            Rabbits.Assign(s, Rabbits.ArriveKind(s, "tho-bong", "Bông").Id, 0);
            s.FreshKinds.Clear();
            s.Fish[0] = 24; s.Fish[1] = 6; s.Fish[3] = 1;
            for (int n = 0; n < 22; n++) Aquarium.Release(s, 0);
            for (int n = 0; n < 5; n++) Aquarium.Release(s, 1);
            Aquarium.Release(s, 3);
            Aquarium.SeeFresh(s);
            s.Fish[2] = 3;
            s.IntrosSeen.UnionWith(Intros.All);
            s.IntrosTried.UnionWith(Intros.All);
            const long now = 1_800_000_000L;
            s.Baits = 4;
            s.BaitAt = now - 300;
            runner.Init(s);
            runner.FishingClock = () => now;
            screen.Raining = () => false;
            screen.RebuildUi();
            if (screen.Shell.State != ShellState.Playing) screen.Shell.Resume();
            screen.SelectStation(-1);
            screen.Refresh();
            screen.LookAt(GardenScreen.TankSpot + new Vector2(-60f, -120f));
            Step(screen, 3f);
            Capture(screen, "114-be-ca-vuon-truoc");
            runner.ReleaseFish(2);
            screen.Refresh();
            Step(screen, 0.4f);
            Capture(screen, "114-be-ca-vuon");
            foreach (string locale in new[] { "vi", "en" })
            {
                L10n.SetLocale(locale);
                screen.RebuildUi();
                screen.SelectStation(-1);
                screen.Refresh();
                Step(screen, 0.4f);
                screen.OpenFishing();
                Step(screen, 0.6f);
                Press(screen, "Canvas/Pond/Card/Basket/Fish 0");
                Press(screen, "Canvas/Pond/Card/Release");
                Step(screen, 0.4f);
                Capture(screen, "114-gio-ca-tha-" + locale);
                screen.FishingCard.Close();
                Step(screen, 0.5f);
                screen.OpenTank();
                Step(screen, 0.6f);
                Capture(screen, "114-be-ca-3-loai-" + locale);
                screen.TankSheet.Close();
                Step(screen, 0.5f);
                UiKit.SetLargeText(true);
                screen.RebuildUi();
                screen.SelectStation(-1);
                screen.Refresh();
                Step(screen, 0.3f);
                screen.OpenTank();
                Step(screen, 0.6f);
                Capture(screen, "114-be-ca-chu-to-" + locale);
                screen.TankSheet.Close();
                Step(screen, 0.5f);
                UiKit.SetLargeText(false);
            }

            GameState full = GameState.NewGame(defs);
            for (int i = 0; i < levels.Length; i++) full.Levels[i] = levels[i];
            full.GardenXp = Garden.XpForLevel(6);
            full.HintSeen = true;
            full.TutorialStep = Tutorial.Steps;
            full.Newcomer.Closed = true;
            full.Coins = new BigNum(84000);
            full.MoonJars = 2;
            Rabbits.Assign(full, Rabbits.ArriveKind(full, "tho-bong", "Bông").Id, 0);
            full.FreshKinds.Clear();
            int[] tank = { 31, 12, 7, 5, 2, 0 };
            for (int k = 0; k < tank.Length; k++)
            {
                full.Fish[k] = tank[k];
                for (int n = 0; n < tank[k]; n++) Aquarium.Release(full, k);
            }
            full.Fish[5] = 1;
            full.IntrosSeen.UnionWith(Intros.All);
            full.IntrosTried.UnionWith(Intros.All);
            runner.Init(full);
            runner.FishingClock = () => now;
            foreach (string locale in new[] { "vi", "en" })
            {
                L10n.SetLocale(locale);
                screen.RebuildUi();
                if (screen.Shell.State != ShellState.Playing) screen.Shell.Resume();
                screen.SelectStation(-1);
                screen.LookAt(GardenScreen.TankSpot + new Vector2(-60f, -120f));
                screen.Refresh();
                Step(screen, 0.4f);
                if (full.Fish[5] > 0)
                {
                    screen.OpenFishing();
                    Step(screen, 0.6f);
                    Press(screen, "Canvas/Pond/Card/Basket/Fish 5");
                    Press(screen, "Canvas/Pond/Card/Release");
                    Step(screen, 0.4f);
                    Capture(screen, "114-tha-loai-thu-6-" + locale);
                    screen.FishingCard.Close();
                    Step(screen, 0.5f);
                    screen.Refresh();
                }
                else screen.TankCard.Open(full);
                Step(screen, 0.6f);
                Capture(screen, "114-the-du-bo-" + locale);
                screen.TankCard.Close();
                Step(screen, 0.3f);
            }
            L10n.SetLocale("vi");
            screen.Raining = null;
            screen.RebuildUi();
            screen.SelectStation(-1);
            Step(screen, 0.4f);
        }

        static void RenderRecipes(GameRunner runner, GardenScreen screen)
        {
            var defs = runner.Catalog.BuildDefs();
            GameState s = GameState.NewGame(defs);
            int[] levels = { 41, 18, 12, 6 };
            for (int i = 0; i < levels.Length; i++) s.Levels[i] = levels[i];
            s.Plots = 6;
            s.GardenXp = Garden.XpForLevel(7);
            s.HintSeen = true;
            s.TutorialStep = Tutorial.Steps;
            s.Coins = new BigNum(2.4e5);
            s.GoldenCarrots = 4;
            Rabbits.Assign(s, Rabbits.ArriveKind(s, "tho-bong", "Bông").Id, 0);
            Rabbits.Arrive(s, 1, "Mây", 0.4);
            Rabbits.Arrive(s, 0, "Sữa", 0.7);
            s.FreshKinds.Clear();
            runner.Init(s);
            if (screen.Shell.State != ShellState.Playing) screen.Shell.Resume();
            screen.SelectStation(-1);
            screen.Refresh();
            Step(screen, 0.3f);
            screen.FocusStation(0);
            Step(screen, 0.5f);
            Capture(screen, "110-tam-tram-khoa-vi");
            s.Season = 1;
            Recipes.SetLevel(s, 0, 0, 3);
            Recipes.SetLevel(s, 2, 0, 6);
            s.RecipeLeaves = 57;
            runner.Init(s);
            if (screen.Shell.State != ShellState.Playing) screen.Shell.Resume();
            foreach (string locale in new[] { "vi", "en" })
            {
                L10n.SetLocale(locale);
                screen.RebuildUi();
                screen.SelectStation(-1);
                screen.Refresh();
                Step(screen, 0.4f);
                screen.FocusStation(0);
                Step(screen, 0.5f);
                Capture(screen, "110-tam-tram-" + locale);
                screen.FocusStation(2);
                Step(screen, 0.5f);
                Capture(screen, "110-tam-tram-thieu-la-" + locale);
                screen.FocusStation(4);
                Step(screen, 0.5f);
                Capture(screen, "110-tam-tram-chua-mo-" + locale);
                UiKit.SetLargeText(true);
                screen.RebuildUi();
                screen.SelectStation(-1);
                screen.Refresh();
                Step(screen, 0.3f);
                screen.FocusStation(0);
                Step(screen, 0.5f);
                Capture(screen, "110-tam-tram-chu-to-" + locale);
                UiKit.SetLargeText(false);
                screen.RebuildUi();
                screen.SelectStation(-1);
                screen.Refresh();
                Step(screen, 0.3f);
            }
            screen.FocusStation(0);
            Step(screen, 0.4f);
            Press(screen, "Canvas/Card/Dish/Raise");
            Step(screen, 0.35f);
            Capture(screen, "110-nang-mon-en");
            Step(screen, 2f);
            L10n.SetLocale("vi");
            screen.RebuildUi();
            screen.SelectStation(-1);
            Step(screen, 0.4f);
        }

        static void RenderPond(GameRunner runner, GardenScreen screen)
        {
            var defs = runner.Catalog.BuildDefs();
            GameState s = GameState.NewGame(defs);
            int[] levels = { 27, 12, 9, 4 };
            for (int i = 0; i < levels.Length; i++) s.Levels[i] = levels[i];
            s.GardenXp = Garden.XpForLevel(5);
            s.HintSeen = true;
            s.TutorialStep = Tutorial.Steps;
            s.Coins = new BigNum(84000);
            Rabbits.Assign(s, Rabbits.ArriveKind(s, "tho-bong", "Bông").Id, 0);
            Rabbit may = Rabbits.Arrive(s, 1, "Mây", 0.4);
            s.FreshKinds.Clear();
            s.Fish[0] = 3; s.Fish[1] = 2; s.Fish[2] = 1; s.Fish[3] = 1;
            const long now = 1_800_000_000L;
            s.Baits = 3;
            s.BaitAt = now - 300;
            runner.Init(s);
            runner.FishingClock = () => now;
            screen.Raining = () => false;
            screen.RebuildUi();
            if (screen.Shell.State != ShellState.Playing) screen.Shell.Resume();
            screen.SelectStation(-1);
            screen.Refresh();
            screen.LookAt(GardenScreen.PondSpot + new Vector2(-120f, -300f));
            Step(screen, 3f);
            Capture(screen, "58-ao-vuon");

            screen.OpenFishing();
            Step(screen, 0.6f);
            Capture(screen, "58-ao-cho");
            screen.FishingCard.Cast();
            Step(screen, 0.4f);
            Capture(screen, "58-phao-noi");
            for (int k = 0; k < 400 && runner.Cast != null && runner.Cast.Phase != CastPhase.Sinking; k++) screen.Advance(1f / 30f);
            Step(screen, 0.2f);
            Capture(screen, "58-phao-chim");
            screen.FishingCard.TapWater();
            Step(screen, FishingView.ShowSeconds + FishingView.FlySeconds * 0.45f);
            Capture(screen, "58-ca-bay");
            Step(screen, 1f);
            Capture(screen, "58-ca-trong-gio");
            screen.FishingCard.Close();
            Step(screen, 0.4f);

            may.Affection = 25;
            screen.Detail.Open(defs, s, may.Id);
            screen.Refresh();
            Step(screen, 0.5f);
            Press(screen, "Canvas/RabbitSheet/Card/Body/Feed");
            Step(screen, 0.2f);
            Capture(screen, "58-cho-an");
            Step(screen, 1.2f);
            Capture(screen, "58-cho-an-sau");

            may.Affection = Rabbits.AffectionMax - 10;
            s.Fish[5] = 1;
            screen.Refresh();
            Step(screen, 0.3f);
            Press(screen, "Canvas/RabbitSheet/Card/Body/Feed");
            Step(screen, 0.4f);
            Capture(screen, "58-tim-10-bang");
            Step(screen, 2.5f);
            screen.Detail.Close();
            Step(screen, 0.4f);
            screen.OpenSheet(3);
            screen.Refresh();
            Step(screen, 0.6f);
            Capture(screen, "58-tim-10-o");
            screen.CloseSheet();
            screen.Raining = null;
            Step(screen, 0.4f);
        }

        static GameState NewcomerGarden(GameRunner runner, int day, int gardenLevel, int[] levels, long today)
        {
            var defs = runner.Catalog.BuildDefs();
            GameState s = GameState.NewGame(defs);
            for (int i = 0; i < levels.Length; i++) s.Levels[i] = levels[i];
            s.Plots = System.Math.Max(s.Plots, levels.Length);
            s.GardenXp = Garden.XpForLevel(gardenLevel);
            s.HintSeen = true;
            s.TutorialStep = Tutorial.Steps;
            s.Coins = new BigNum(3.4e5);
            Rabbits.Assign(s, Rabbits.ArriveKind(s, "tho-bong", "Bông").Id, 0);
            Rabbits.ArriveKind(s, "tho-me", "Mè");
            s.FreshKinds.Clear();
            s.FirstDaySeen = today - (day - 1);
            s.MaxDaySeen = today;
            s.MailDay = today;
            s.DailyDay = today;
            s.GiftDay = today;
            for (int d = 1; d < day; d++)
            {
                for (int slot = 0; slot < Newcomer.TasksPerDay; slot++) s.Newcomer.Claimed |= Newcomer.At(d, slot).Bit;
                s.Newcomer.DaysPaid |= 1 << (d - 1);
            }
            return s;
        }

        static void ShowNewcomer(GameRunner runner, GardenScreen screen, GameState s, long today, string locale, int select)
        {
            runner.DayClock = () => today;
            runner.Init(s);
            L10n.SetLocale(locale);
            screen.RebuildUi();
            if (screen.Shell.State != ShellState.Playing) screen.Shell.Resume();
            screen.SelectStation(-1);
            screen.LookAt(screen.StationSpot(0));
            screen.Refresh();
            screen.FirstWeek.Open();
            if (select > 0) screen.FirstWeek.Select(select);
            Step(screen, 0.5f);
        }

        static void RenderNewcomer(GameRunner runner, GardenScreen screen)
        {
            const long today = 20781;
            System.Func<long> clockBefore = runner.DayClock;
            foreach (string locale in new[] { "vi", "en" })
            {
                string tail = locale == "vi" ? "" : "-en";
                GameState one = NewcomerGarden(runner, 1, 2, new[] { 27, 6, 3, 1 }, today);
                one.CloverCalls = 1;
                ShowNewcomer(runner, screen, one, today, locale, 0);
                screen.FirstWeek.Close();
                screen.Refresh();
                Step(screen, 0.3f);
                Capture(screen, "102-vien" + tail);
                screen.FirstWeek.Open();
                Step(screen, 0.5f);
                Capture(screen, "102-ngay-1" + tail);
                screen.FirstWeek.Select(2);
                Step(screen, 0.2f);
                Capture(screen, "102-ngay-2-khoa" + tail);

                GameState four = NewcomerGarden(runner, 4, 7, new[] { 60, 24, 18, 12, 6, 2 }, today);
                four.OrdersDelivered = 4;
                four.Pets = 1;
                ShowNewcomer(runner, screen, four, today, locale, 4);
                Capture(screen, "102-ngay-4" + tail);
                if (locale == "vi")
                {
                    UiKit.SetLargeText(true);
                    ShowNewcomer(runner, screen, four, today, locale, 4);
                    Capture(screen, "102-ngay-4-chu-to");
                    UiKit.SetLargeText(false);
                }

                GameState seven = NewcomerGarden(runner, 7, 14, new[] { 120, 60, 40, 30, 20, 14, 8, 3 }, today);
                for (int slot = 0; slot < 3; slot++) seven.Newcomer.Claimed |= Newcomer.At(7, slot).Bit;
                seven.DailyQuestsToday = 4;
                ShowNewcomer(runner, screen, seven, today, locale, 7);
                Capture(screen, "102-ngay-7" + tail);
                Press(screen, "Canvas/NewcomerSheet/Card/Task 3/Claim");
                Step(screen, 2.2f);
                Capture(screen, "102-ngay-7-tho-tim" + tail);
                Step(screen, 3f);
            }
            runner.DayClock = clockBefore;
            L10n.SetLocale("vi");
            screen.RebuildUi();
            Step(screen, 0.3f);
        }

        static void RenderOffers(GameRunner runner, GardenScreen screen)
        {
            var defs = runner.Catalog.BuildDefs();
            GameState s = GameState.NewGame(defs);
            int[] levels = { 18, 9, 5, 2 };
            for (int i = 0; i < levels.Length; i++) s.Levels[i] = levels[i];
            s.Plots = 6;
            s.GardenXp = Garden.XpForLevel(Offers.StarterLevel) + 40;
            s.HintSeen = true;
            s.TutorialStep = Tutorial.Steps;
            s.Coins = new BigNum(64000);
            s.GoldenCarrots = 7;
            Rabbits.Assign(s, Rabbits.ArriveKind(s, "tho-bong", "Bông").Id, 0);
            s.FreshKinds.Clear();
            long[] wall = { (runner.TodayNumber + 1) * Offers.DaySeconds + 9 * 3600L };
            System.Func<long> realClock = runner.OfferWallClock;
            runner.OfferWallClock = () => wall[0];
            runner.Store = new FakeStorefront();
            runner.Init(s);
            runner.OfferCardsOn = true;
            screen.Raining = () => false;
            screen.RebuildUi();
            if (screen.Shell.State != ShellState.Playing) screen.Shell.Resume();
            screen.SelectStation(-1);
            screen.Refresh();
            Step(screen, 0.6f);
            Capture(screen, "105-the-giua");
            UiKit.SetLargeText(true);
            screen.RebuildUi();
            screen.OpenOffer(Offers.Find(Offers.Starter));
            Step(screen, 0.6f);
            Capture(screen, "105-the-giua-chu-to");
            UiKit.SetLargeText(false);
            L10n.SetLocale("en");
            screen.RebuildUi();
            screen.OpenOffer(Offers.Find(Offers.Starter));
            Step(screen, 0.6f);
            Capture(screen, "105-the-giua-en");
            screen.OfferCard.Close();

            wall[0] += 5 * 3600L + 12 * 60L + 30L;
            screen.OpenShop();
            screen.Refresh();
            Step(screen, 0.6f);
            Capture(screen, "105-tiem-goi-en");
            screen.Shop.Close();
            L10n.SetLocale("vi");
            screen.RebuildUi();
            screen.OpenShop();
            screen.Refresh();
            Step(screen, 0.6f);
            Capture(screen, "105-tiem-goi");
            screen.Shop.Close();
            UiKit.SetLargeText(true);
            screen.RebuildUi();
            screen.OpenShop();
            screen.Refresh();
            Step(screen, 0.6f);
            Capture(screen, "105-tiem-goi-chu-to");
            screen.Shop.Close();
            UiKit.SetLargeText(false);
            screen.RebuildUi();

            s.GardenXp = Garden.XpForLevel(10) + 30;
            typeof(GameRunner).GetMethod("NoteOffers", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(runner, null);
            screen.Refresh();
            Step(screen, 0.6f);
            Capture(screen, "105-the-moc");
            Press(screen, "Canvas/Offer/Card/Buy");
            Step(screen, SummonMomentView.Length + 0.4f);
            Capture(screen, "105-moc-tho-tim");
            screen.Arrival.Tap();
            screen.Arrival.Tap();
            Step(screen, 0.4f);
            screen.OpenOffer(Offers.Find(Offers.Starter));
            Press(screen, "Canvas/Offer/Card/Buy");
            Step(screen, 0.5f);
            Capture(screen, "105-da-mua");
            screen.OfferCard.Close();
            runner.OfferCardsOn = false;
            runner.OfferWallClock = realClock;
            screen.Raining = null;
            Step(screen, 0.4f);
        }

        static GameState IntroGarden(GameRunner runner)
        {
            GameState s = GameState.NewGame(runner.Catalog.BuildDefs());
            int[] levels = { 27, 12, 9, 4, 2, 0 };
            for (int i = 0; i < levels.Length; i++) s.Levels[i] = levels[i];
            s.Plots = 6;
            GardenGrid.Settle(runner.Catalog.BuildDefs(), s);
            s.GardenXp = Garden.XpForLevel(5) + 90;
            s.HintSeen = true;
            s.TutorialStep = Tutorial.Steps;
            s.Coins = new BigNum(84000);
            Rabbits.Assign(s, Rabbits.ArriveKind(s, "tho-bong", "Bông").Id, 0);
            Rabbits.Assign(s, Rabbits.Arrive(s, 2, "Lam", 0.2).Id, 1);
            s.FreshKinds.Clear();
            runner.Init(s);
            s.IntrosSeen.UnionWith(Intros.All);
            s.IntrosTried.Clear();
            s.IntrosLater.Clear();
            return s;
        }

        static void ShowIntroFrame(GameRunner runner, GardenScreen screen, GameState s, string id, string name)
        {
            if (runner.State != s) Debug.LogWarning("[VuonTra] intro preview: the runner's state was replaced before " + name);
            s = runner.State;
            s.IntrosSeen.UnionWith(Intros.All);
            s.IntrosSeen.Remove(id);
            screen.IntroQueue.Reset();
            Step(screen, 0.8f);
            Capture(screen, name);
        }

        static void StartIntroScreen(GardenScreen screen)
        {
            screen.RebuildUi();
            if (screen.Shell.State != ShellState.Playing) screen.Shell.Resume();
            screen.SelectStation(-1);
            screen.Refresh();
            screen.LookAt(screen.StationSpot(0) + new Vector2(0f, -120f));
            Step(screen, 0.4f);
        }

        static void RenderIntros(GameRunner runner, GardenScreen screen)
        {
            foreach (string lang in new[] { "vi", "en" })
            {
                L10n.SetLocale(lang);
                GameState s = IntroGarden(runner);
                StartIntroScreen(screen);
                ShowIntroFrame(runner, screen, s, Intros.Fishing, "104-the-cau-ca-" + lang);
                Press(screen, "Canvas/Intro/Later");
                Step(screen, 0.4f);
                if (lang == "vi")
                {
                    screen.Refresh();
                    screen.LookAt(GardenScreen.PondSpot + new Vector2(-60f, -260f));
                    Step(screen, 0.5f);
                    Capture(screen, "104-cham-vang-de-sau");
                    screen.IntroDot(Intros.Fishing).GetComponent<Button>().onClick.Invoke();
                    Step(screen, 1.1f);
                    Capture(screen, "104-thu-ngay-sang-ao");
                    Step(screen, 2.5f);
                }
                ShowIntroFrame(runner, screen, s, Intros.Layout, "104-the-bo-tri-" + lang);
                Press(screen, "Canvas/Intro/Later");
                Step(screen, 0.4f);
                if (lang == "vi")
                {
                    screen.Profile.Open(runner.Defs, s, runner.Account, runner.CloudStatus);
                    Step(screen, 0.4f);
                    Press(screen, "Canvas/Profile/Card/Intros");
                    Step(screen, 0.5f);
                    Capture(screen, "104-da-mo-ho-so");
                    screen.IntroList.Close();
                    screen.Profile.Close();
                }
            }
            UiKit.SetLargeText(true);
            L10n.SetLocale("vi");
            GameState big = IntroGarden(runner);
            big.GardenXp = Garden.XpForLevel(FrogRabbit.FromLevel);
            big.NextOrderId = 1;
            StartIntroScreen(screen);
            ShowIntroFrame(runner, screen, big, Intros.Rain, "104-the-thu-ech-chu-to");
            Press(screen, "Canvas/Intro/Later");
            L10n.SetLocale("en");
            screen.RebuildUi();
            ShowIntroFrame(runner, screen, big, Intros.Orders, "104-the-khach-chu-to-en");
            Press(screen, "Canvas/Intro/Later");
            UiKit.SetLargeText(false);
            L10n.SetLocale("vi");
            screen.RebuildUi();
            screen.IntroQueue.Reset();
            Step(screen, 0.4f);
        }

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
            GardenGrid.Settle(defs, s);
            s.GardenXp = Garden.XpForLevel(9);
            s.HintSeen = true;
            s.TutorialStep = Tutorial.Steps;
            s.Coins = new BigNum(4.2e6);
            s.GoldenCarrots = 100;
            Decor.Buy(s, "den-da");
            Decor.Place(s, 7, "den-da");
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

        static void RenderGuestByPond()
        {
            EditorSceneManager.OpenScene(ContentSeeder.ScenePath, OpenSceneMode.Single);
            L10n.SetLocale("vi");
            var runner = Object.FindAnyObjectByType<GameRunner>();
            var screen = Object.FindAnyObjectByType<GardenScreen>();
            var defs = runner.Catalog.BuildDefs();
            GameState s = GameState.NewGame(defs);
            int[] levels = { 27, 12, 9, 4, 6, 5 };
            for (int i = 0; i < levels.Length; i++) s.Levels[i] = levels[i];
            s.Plots = levels.Length;
            GardenGrid.Settle(defs, s);
            s.GardenXp = Garden.XpForLevel(5);
            s.HintSeen = true;
            s.TutorialStep = Tutorial.Steps;
            s.Coins = new BigNum(4.2e6);
            s.Baits = 3;
            s.FreshKinds.Clear();
            s.Order = new Order { Id = 1, Guest = RabbitKinds.All[0].Id, Left = 150, RewardSeconds = Orders.RewardSeconds };
            s.Order.Lines.Add(new OrderLine { Station = 0, Count = 2 });
            s.Order.Lines.Add(new OrderLine { Station = 1, Count = 3 });
            runner.Init(s);
            screen.Build();
            screen.ClockHours = () => 22.0;
            screen.Raining = () => false;
            screen.Refresh();
            screen.Shell.StartGame();
            screen.SelectStation(-1);
            screen.Refresh();
            screen.SetZoom(GardenScreen.ZoomLevel.Default, false);
            screen.LookAt(screen.GuestSpot + new Vector2(0f, 120f));
            Step(screen, 1.4f);
            Capture(screen, "84-khach-cho-ao");
            screen.SetZoom(GardenScreen.ZoomLevel.Near, false);
            screen.LookAt(screen.GuestSpot + new Vector2(0f, 120f));
            Step(screen, 0.5f);
            Capture(screen, "84-khach-cho-ao-gan");
            screen.SetZoom(GardenScreen.ZoomLevel.Default, false);
        }

        static void RenderNgangNits()
        {
            EditorSceneManager.OpenScene(ContentSeeder.ScenePath, OpenSceneMode.Single);
            L10n.SetLocale("vi");
            var runner = Object.FindAnyObjectByType<GameRunner>();
            var screen = Object.FindAnyObjectByType<GardenScreen>();
            var defs = runner.Catalog.BuildDefs();
            GameState s = GameState.NewGame(defs);
            for (int i = 0; i < Plots.Starting; i++) s.Levels[i] = 6 - i;
            s.HintSeen = true;
            s.TutorialStep = Tutorial.Steps;
            Rabbit bong = Rabbits.GiftFirst(s, "Bông");
            s.FreshKinds.Clear();
            runner.Init(s);
            screen.Build();
            screen.ClockHours = () => 22.0;
            screen.Raining = () => false;
            screen.Refresh();
            screen.Shell.StartGame();
            Step(screen, 1.4f);
            Capture(screen, "nits-6-mo-dau");
            screen.SetZoom(GardenScreen.ZoomLevel.Far, false);
            Step(screen, 0.3f);
            Capture(screen, "nits-6-xa");
            screen.SetZoom(GardenScreen.ZoomLevel.Default, false);

            MistSegment next = Expansion.Next(runner.Defs, s);
            s.GardenXp = System.Math.Max(s.GardenXp, Garden.XpForLevel(next.NeedLevel));
            s.Coins = Expansion.CostOfNext(runner.Defs, s) * new BigNum(1.2);
            screen.Refresh();
            Step(screen, 4f);
            screen.LookAt(screen.ForestSignSpot + new Vector2(0f, 120f));
            Step(screen, 0.3f);
            Press(screen, "Canvas/Garden/Content/Forest Sign/Board");
            screen.Refresh();
            Step(screen, GardenScreen.RimStep + GardenScreen.MistClear + 1.6f);
            Capture(screen, "nits-5-sau-mo-rong");
            screen.ChatterBy(bong.Id);
            Step(screen, 0.5f);
            Capture(screen, "nits-4-noi");
            Step(screen, 3f);

            s.Levels[4] = 3; s.Levels[5] = 2;
            screen.Refresh();
            screen.GlideHome();
            Step(screen, 1.4f);
            Capture(screen, "nits-2-mo-dau");

            GameState fresh = GameState.NewGame(defs);
            for (int i = 0; i < Plots.Starting; i++) fresh.Levels[i] = 6 - i;
            fresh.HintSeen = true;
            fresh.TutorialStep = Tutorial.Steps;
            Rabbits.GiftFirst(fresh, "Bông");
            fresh.FreshKinds.Clear();
            fresh.Order = new Order { Id = 1, Guest = RabbitKinds.All[0].Id, Left = Orders.WaitSeconds, RewardSeconds = Orders.RewardSeconds };
            fresh.Order.Lines.Add(new OrderLine { Station = 0, Count = 2 });
            runner.Init(fresh);
            screen.RebuildUi();
            screen.SelectStation(-1);
            screen.Refresh();
            screen.LookAt(screen.GuestSpot + new Vector2(380f, -200f));
            Step(screen, 0.5f);
            Capture(screen, "nits-1-khach");
            Step(screen, 2.4f);
            Capture(screen, "nits-1-khach-den");
        }

        static void RenderStationSpacing()
        {
            EditorSceneManager.OpenScene(ContentSeeder.ScenePath, OpenSceneMode.Single);
            L10n.SetLocale("vi");
            var runner = Object.FindAnyObjectByType<GameRunner>();
            var screen = Object.FindAnyObjectByType<GardenScreen>();
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
            Rabbits.Assign(s, Rabbits.Arrive(s, 1, "Mây", 0.4).Id, 2);
            s.FreshKinds.Clear();
            runner.Init(s);
            PlayerPrefs.SetInt(GardenScreen.LayoutHintKey, 1);
            screen.Build();
            screen.ClockHours = () => 13.0;
            screen.Raining = () => false;
            screen.Refresh();
            screen.Shell.StartGame();
            screen.SelectStation(-1);
            Step(screen, 1.4f);
            Hex basket = s.Grid.TileOf(GardenGrid.StationId(defs[1])).Value, jam = s.Grid.TileOf(GardenGrid.StationId(defs[2])).Value;
            Hex below = jam;
            foreach (Hex h in screen.LayoutEmptyTiles())
                if (GardenGrid.Distance(h, jam) == 1 && h.R > jam.R && GardenGrid.Distance(h, basket) > 1) { below = h; break; }
            screen.LookAt((GardenScreen.CellToWorld(jam) + GardenScreen.CellToWorld(basket)) / 2f + new Vector2(0f, -60f));
            Step(screen, 0.5f);
            screen.EnterLayoutMode();
            Step(screen, 0.3f);
            screen.SimulateLayoutPress(screen.LayoutViewPoint(basket));
            Step(screen, GardenScreen.LayoutHoldSeconds + 0.05f);
            screen.SimulateLayoutDrag((screen.LayoutViewPoint(basket) + screen.LayoutViewPoint(below)) / 2f);
            screen.SimulateLayoutDrag(screen.LayoutViewPoint(below));
            Step(screen, 0.1f);
            Capture(screen, "88-keo-sat-tram");
            screen.SimulateLayoutRelease(screen.LayoutViewPoint(below));
            Step(screen, 0.6f);
            Capture(screen, "88-tha-sat-tram");
            screen.ExitLayoutMode();
            Step(screen, 0.3f);
            PlayerPrefs.DeleteKey(GardenScreen.LayoutHintKey);
        }
    }
}
