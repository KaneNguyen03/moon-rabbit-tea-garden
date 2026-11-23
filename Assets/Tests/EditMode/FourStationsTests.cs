using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using VuonTra.Game;
using VuonTra.Game.UI;
using L10n = VuonTra.Game.L10n;
using Motion = VuonTra.Game.UI.Motion;

namespace VuonTra.Core.Tests
{
    /// <summary>
    /// The four stations after the star observatory (#67a): what they are, the garden level each asks for, the ingredient
    /// links, an old save of twelve stations, the new moon, their product, quests, sound and names, and how the
    /// Stations tab and the garden show them while their land (#53, 67b) is still to come.
    /// </summary>
    public class FourStationsTests
    {
        GameCatalog catalog;
        List<StationDef> defs;

        static readonly string[] NewIds = { "ho-sen", "lo-keo", "ruong-nep", "thuyen-tra" };

        [SetUp]
        public void SetUp()
        {
            catalog = AssetDatabase.LoadAssetAtPath<GameCatalog>("Assets/VuonTra/Content/GameCatalog.asset");
            Assert.IsNotNull(catalog, "Run VuonTra > Seed Content first.");
            defs = catalog.BuildDefs();
        }

        [TearDown]
        public void TearDown()
        {
            L10n.SetLocale("vi");
        }

        int IndexOf(string id) => defs.FindIndex(d => d.Id == id);

        /// <summary>Every station up to the star observatory open, with the land and the purse to open the rest.</summary>
        GameState TwelveOpen(int gardenLevel, int plots = 16)
        {
            GameState s = GameState.NewGame(defs);
            for (int i = 0; i < 12; i++) s.Levels[i] = 50;
            s.Plots = plots;
            s.Coins = new BigNum(1e30);
            s.GardenXp = Garden.XpForLevel(gardenLevel);
            GardenGrid.Settle(defs, s);
            return s;
        }

        // ----- What the four stations are -----

        [Test]
        public void The_Four_New_Stations_Come_After_The_Star_Observatory_In_Order()
        {
            Assert.AreEqual(16, defs.Count);
            Assert.AreEqual("dai-sao", defs[11].Id, "the old twelve keep their places");
            for (int k = 0; k < 4; k++) Assert.AreEqual(NewIds[k], defs[12 + k].Id);

            StationDef lotus = defs[12], candy = defs[13], rice = defs[14], boat = defs[15];
            Assert.AreEqual(StationKind.Ingredient, lotus.Kind);
            Assert.AreEqual(3e14, lotus.BaseCost, 1);
            Assert.AreEqual(StationKind.Brew, candy.Kind);
            Assert.AreEqual(4.5e15, candy.BaseCost, 1);
            Assert.AreEqual(60, candy.CycleSeconds, 1e-9);
            Assert.AreEqual(1.2e10, candy.BaseIncome, 1);
            Assert.AreEqual(RabbitTrade.Baker, candy.Trade, "Làm bánh");
            Assert.AreEqual(StationKind.Ingredient, rice.Kind);
            Assert.AreEqual(6.5e16, rice.BaseCost, 1);
            Assert.AreEqual(StationKind.Brew, boat.Kind);
            Assert.AreEqual(1e18, boat.BaseCost, 1);
            Assert.AreEqual(75, boat.CycleSeconds, 1e-9);
            Assert.AreEqual(1.8e11, boat.BaseIncome, 1);
            Assert.AreEqual(RabbitTrade.Brewer, boat.Trade, "Pha trà");
            foreach (StationDef d in new[] { lotus, rice }) Assert.AreEqual(RabbitTrade.Gatherer, d.Trade, d.Id);
            foreach (StationDef d in new[] { lotus, candy, rice, boat })
            {
                Assert.That(d.CostGrowth, Is.InRange(1.16, 1.17), d.Id);
                Assert.Greater(d.BaseCost, defs[IndexOf(d.Id) - 1].BaseCost, d.Id + " costs more than the one before");
            }
        }

        [Test]
        public void The_Ingredient_Stations_Feed_Exactly_The_Stations_They_Name()
        {
            CollectionAssert.AreEquivalent(new[] { "tiem-banh", "dai-sao" }, defs[12].BoostsIds);
            CollectionAssert.AreEquivalent(new[] { "lo-keo", "tra-sua" }, defs[14].BoostsIds);

            GameState s = TwelveOpen(30);
            s.Levels[13] = 1; // the candy kiln, so the rice paddy has something of its own to feed
            var before = new BigNum[defs.Count];
            for (int i = 0; i < defs.Count; i++) before[i] = Economy.IncomePerCycle(defs, s, i);
            s.Levels[12] = 10;
            s.Levels[14] = 10;
            for (int i = 0; i < defs.Count; i++)
            {
                bool fed = defs[i].Id == "tiem-banh" || defs[i].Id == "dai-sao" || defs[i].Id == "lo-keo" || defs[i].Id == "tra-sua";
                BigNum after = Economy.IncomePerCycle(defs, s, i);
                if (fed) Assert.Greater(after.ToDouble(), before[i].ToDouble(), defs[i].Id + " is fed");
                else Assert.AreEqual(before[i].ToDouble(), after.ToDouble(), System.Math.Abs(before[i].ToDouble()) * 1e-12, defs[i].Id + " is not");
            }
        }

        /// <summary>
        /// The two new ingredient stations give 7 % of the usual rise (#83), the old ones all of it: at full strength they
        /// stacked onto the top brews and made day seven's income over a hundredfold (EconomySimulationTests holds the tenfold cap).
        /// The Stations tab shows the boost they really give.
        /// </summary>
        [Test]
        public void The_Two_New_Ingredient_Stations_Give_Seven_Percent_Of_The_Usual_Boost()
        {
            for (int i = 0; i < 12; i++) Assert.AreEqual(1d, defs[i].BoostScale, 1e-12, defs[i].Id);
            Assert.AreEqual(0.07, defs[12].BoostScale, 1e-12, "the lotus garden");
            Assert.AreEqual(0.07, defs[14].BoostScale, 1e-12, "the rice paddy");
            Assert.AreEqual(1d + 2d * 0.07, Economy.IngredientBoost(defs[12], 10), 1e-12, "level 10: ×3 for an old one, ×1.14 here");
            Assert.AreEqual(Economy.IngredientBoost(10), Economy.IngredientBoost(defs[10], 10), 1e-12, "the old ones as before");

            GameState s = TwelveOpen(30);
            BigNum before = Economy.IncomePerCycle(defs, s, 11);
            s.Levels[12] = 10;
            double rabbit = Rabbits.StationMultiplier(defs, s, 12) * AlbumSets.StationFactor(defs, s, 12);
            Assert.AreEqual(before.ToDouble() * (1d + 2d * 0.07 * rabbit), Economy.IncomePerCycle(defs, s, 11).ToDouble(), before.ToDouble() * 1e-9, "the star observatory, fed by the lotus garden");
            Assert.AreEqual("×" + UiKit.Times(1.14), StationRowView.MainNumber(defs, s, 12, 10, Modifiers.None));
        }

        // ----- The garden level each asks for -----

        [Test]
        public void Each_New_Station_Asks_For_A_Higher_Garden_Level_And_The_Old_Ones_For_None()
        {
            for (int i = 0; i < 12; i++) Assert.AreEqual(0, defs[i].NeedGardenLevel, defs[i].Id);
            for (int i = 13; i < 16; i++) Assert.Greater(defs[i].NeedGardenLevel, defs[i - 1].NeedGardenLevel, defs[i].Id);
            // The fourth segment (the old R 7) asks for 16 too since #83: the lotus garden still waits for its own land, the fifth.
            Assert.GreaterOrEqual(defs[12].NeedGardenLevel, Expansion.Segment(4).NeedLevel, "not before the segment that gives the twelfth station its land");
            Assert.LessOrEqual(defs[15].NeedGardenLevel, NewMoon.FirstLevel, "the last one comes before the first new moon can");
        }

        [Test]
        public void The_Garden_Level_Blocks_Opening_Until_The_Garden_Has_Grown()
        {
            int need = defs[12].NeedGardenLevel;
            GameState s = TwelveOpen(need - 1);
            Assert.IsFalse(Economy.HasGardenLevel(defs, s, 12));
            Assert.IsFalse(Economy.CanUnlock(defs, s, 12), "carrots, land and the station before are not enough");
            Assert.IsFalse(Simulation.TryBuy(defs, s, 12));
            Assert.AreEqual(0, s.Levels[12]);

            s.GardenXp = Garden.XpForLevel(need);
            Assert.IsTrue(Economy.CanUnlock(defs, s, 12));
            Assert.IsTrue(Simulation.TryBuy(defs, s, 12));
            Assert.AreEqual(1, s.Levels[12]);

            // The next one asks for more, and still for the one before.
            s.GardenXp = Garden.XpForLevel(defs[13].NeedGardenLevel - 1);
            Assert.IsFalse(Economy.CanUnlock(defs, s, 13));
            s.GardenXp = Garden.XpForLevel(defs[15].NeedGardenLevel);
            Assert.IsTrue(Economy.CanUnlock(defs, s, 13));
            Assert.IsFalse(Economy.CanUnlock(defs, s, 14), "the station before is still closed");
        }

        [Test]
        public void Without_A_Plot_No_Garden_Level_Opens_Them()
        {
            GameState s = TwelveOpen(Garden.MaxLevel, 12);
            for (int i = 12; i < 16; i++) Assert.IsFalse(Economy.CanUnlock(defs, s, i), defs[i].Id);
            Assert.AreEqual(5, Expansion.Next(defs, s).SegmentsAfter, "their land is the next segment of the strip");
        }

        // ----- Their land: the fifth and sixth segments, which were the eighth and ninth rings (#67b) -----

        /// <summary>A garden with four segments open (the old R 7), every old station open, the rabbits and kinds asked for, and a full purse.</summary>
        GameState AtRingSeven(int gardenLevel)
        {
            GameState s = TwelveOpen(gardenLevel, 12);
            s.Grid.Segments = 4;
            for (int i = 0; i < 12; i++) s.Rabbits.Add(new Rabbit { Id = ++s.NextRabbitId, Tier = 0, Name = "R" });
            foreach (RabbitKind kind in RabbitKinds.All) if (s.SeenKinds.Count < 8) s.SeenKinds.Add(kind.Id);
            GardenGrid.Settle(defs, s);
            return s;
        }

        [Test]
        public void The_Fifth_And_Sixth_Segments_Make_Room_For_Two_Stations_Each_At_The_Level_Of_The_First()
        {
            Assert.AreEqual(6, Expansion.LastSegments(defs), "sixteen stations: six segments");
            Assert.AreEqual(defs[12].NeedGardenLevel, Expansion.Segment(5).NeedLevel, "the fifth (R 8) asks what the lotus garden asks");
            Assert.AreEqual(defs[14].NeedGardenLevel, Expansion.Segment(6).NeedLevel, "the sixth (R 9) asks what the rice paddy asks");
            Assert.AreEqual(Expansion.Segment(6).NeedLevel + Expansion.LevelStepBeyond, Expansion.Segment(7).NeedLevel, "past them, five more a segment");

            GameState s = AtRingSeven(Expansion.Segment(5).NeedLevel - 1);
            double price8 = (defs[12].BaseCost + defs[13].BaseCost) * Plots.CostShare;
            Assert.AreEqual(price8, Expansion.CostOfNext(defs, s).ToDouble(), price8 * 1e-9, "a quarter of the two stations' price");
            Assert.IsFalse(Expansion.CanExpand(defs, s), "a level short");
            s.GardenXp = Garden.XpForLevel(Expansion.Segment(5).NeedLevel);
            Assert.IsTrue(Expansion.Expand(defs, s));
            Assert.AreEqual(14, s.Plots);
            Assert.AreEqual(5, s.Grid.Segments);
            for (int i = 12; i < 14; i++) Assert.AreEqual(GardenGrid.DefaultStationTile(i), s.Grid.TileOf(GardenGrid.StationId(defs[i])), defs[i].Id + " on its own tile");
            Assert.IsNull(s.Grid.TileOf(GardenGrid.StationId(defs[14])), "the rice paddy waits for the sixth");
            Assert.IsTrue(Simulation.TryBuy(defs, s, 12), "the lotus garden can be opened");

            double price9 = (defs[14].BaseCost + defs[15].BaseCost) * Plots.CostShare;
            Assert.AreEqual(price9, Expansion.CostOfNext(defs, s).ToDouble(), price9 * 1e-9);
            Assert.IsFalse(Expansion.Expand(defs, s), "the sixth asks for more");
            s.GardenXp = Garden.XpForLevel(Expansion.Segment(6).NeedLevel);
            Assert.IsTrue(Expansion.Expand(defs, s));
            Assert.AreEqual(16, s.Plots);
            Assert.AreEqual(6, s.Grid.Segments);
            for (int i = 14; i < 16; i++) Assert.AreEqual(GardenGrid.DefaultStationTile(i), s.Grid.TileOf(GardenGrid.StationId(defs[i])), defs[i].Id + " on its own tile");
            Assert.IsNull(Expansion.Next(defs, s), "every station has its land");
        }

        [Test]
        public void The_Four_Default_Tiles_Stand_Apart_From_Everything_On_The_Lawn_Of_Their_Segment()
        {
            var others = new List<Hex>();
            for (int i = 0; i < 12; i++) others.Add(GardenGrid.DefaultStationTile(i));
            var things = new List<Hex>();
            for (int n = 0; n < RestSpots.Max; n++) things.Add(GardenGrid.DefaultRestTile(n));
            for (int n = 0; n < Decor.Spots; n++) things.Add(GardenGrid.DefaultDecorTile(n));
            foreach (string id in new[] { GardenGrid.MortarId, GardenGrid.ShopId, GardenGrid.CounterId }) things.Add(GardenGrid.DefaultTile(id, defs));
            for (int i = 12; i < 16; i++)
            {
                Hex h = GardenGrid.DefaultStationTile(i);
                Assert.That(GardenGrid.SegmentOf(h), Is.InRange(0, GardenGrid.SegmentsForPlots(i + 1)), defs[i].Id + " stands on land open once its segment is");
                foreach (Hex o in others) Assert.GreaterOrEqual(GardenGrid.Distance(h, o), Expansion.StationSpacing, defs[i].Id + " keeps two tiles from the station at " + o);
                foreach (Hex t in things) Assert.AreNotEqual(t, h, defs[i].Id + " takes no seat, decoration or prop tile");
                others.Add(h);
            }
        }

        // ----- An old save, and the new moon -----

        [Test]
        public void An_Old_Save_Of_Twelve_Stations_Gains_The_Four_At_Level_Zero_With_Nothing_Shifted()
        {
            List<StationDef> old = defs.GetRange(0, 12);
            GameState was = GameState.NewGame(old);
            was.Plots = 12;
            for (int i = 0; i < 12; i++)
            {
                was.Levels[i] = 10 + i;
                was.Progress[i] = 0.05 * i;
                was.TrayWork[i] = i;
            }
            Rabbit worker = Rabbits.Arrive(was, 1, "Lam", 0d);
            Assert.IsTrue(Rabbits.Assign(was, worker.Id, 11));
            Tray.Add(was, 9, false);
            string json = SaveCodec.Encode(old, was);
            StringAssert.Contains("\"version\":14", json);

            LoadResult loaded = SaveCodec.Decode(defs, json);
            Assert.AreEqual(LoadStatus.Ok, loaded.Status);
            GameState s = loaded.State;
            Assert.AreEqual(16, s.Levels.Length);
            Assert.AreEqual(16, s.Progress.Length);
            Assert.AreEqual(16, s.TrayWork.Length);
            for (int i = 0; i < 12; i++)
            {
                Assert.AreEqual(10 + i, s.Levels[i], defs[i].Id);
                Assert.AreEqual(0.05 * i, s.Progress[i], 1e-9, defs[i].Id);
                Assert.AreEqual(i, s.TrayWork[i], 1e-9, defs[i].Id);
            }
            for (int i = 12; i < 16; i++)
            {
                Assert.AreEqual(0, s.Levels[i], defs[i].Id);
                Assert.AreEqual(0d, s.Progress[i], defs[i].Id);
                Assert.AreEqual(0d, s.TrayWork[i], defs[i].Id);
                Assert.IsNull(s.Grid.TileOf(GardenGrid.StationId(defs[i])), defs[i].Id + " has no plot, so no tile");
            }
            Assert.AreEqual(12, s.Plots);
            Assert.AreEqual(11, s.Rabbits.Find(r => r.Id == worker.Id).Station, "the rabbit stays at the observatory");
            Assert.AreEqual(9, s.Tray[0].Station, "the tray keeps the bakery's batch");

            // Written again, the four are there at level 0, and read back the same.
            GameState again = SaveCodec.Decode(defs, SaveCodec.Encode(defs, s)).State;
            CollectionAssert.AreEqual(s.Levels, again.Levels);
        }

        [Test]
        public void A_New_Moon_Puts_The_Four_New_Stations_Back_To_Zero_Like_The_Rest()
        {
            GameState s = TwelveOpen(NewMoon.FirstLevel);
            for (int i = 12; i < 16; i++) { s.Levels[i] = 30; s.Progress[i] = 0.5; s.TrayWork[i] = 3; }
            Assert.IsTrue(NewMoon.Begin(defs, s, "Trăng", out _));
            Assert.AreEqual(1, s.Levels[0]);
            for (int i = 1; i < 16; i++) Assert.AreEqual(0, s.Levels[i], defs[i].Id);
            for (int i = 12; i < 16; i++)
            {
                Assert.AreEqual(0d, s.Progress[i], defs[i].Id);
                Assert.AreEqual(0d, s.TrayWork[i], defs[i].Id);
            }
            Assert.IsFalse(Economy.CanUnlock(defs, s, 12), "and they ask for their garden level again");
        }

        // ----- Product, quests, sound, names -----

        [Test]
        public void The_Two_Makers_Have_Their_Own_Product_And_Every_New_Station_Its_Prop()
        {
            foreach (StationData data in catalog.Stations)
            {
                if (System.Array.IndexOf(NewIds, data.Id) < 0) continue;
                Assert.IsNotNull(data.Prop, data.Id);
                Assert.AreEqual("prop_" + data.Id, data.Prop.name, data.Id + " is drawn");
                if (data.Kind == StationKind.Brew)
                {
                    Assert.IsNotNull(data.Product, data.Id + " has something for the tray");
                    Assert.AreEqual("product_" + data.Id, data.Product.name);
                }
            }
        }

        [Test]
        public void Twelve_And_Sixteen_Stations_Open_Are_Quests_Paying_Five_Clovers()
        {
            List<QuestDef> quests = catalog.BuildQuests();
            foreach ((string id, int target) in new[] { ("mo-12", 12), ("mo-16", 16) })
            {
                QuestDef q = quests.Find(x => x.Id == id);
                Assert.IsNotNull(q, id);
                Assert.AreEqual(QuestMetric.StationsOpen, q.Metric, id);
                Assert.AreEqual(target, q.Target, 1e-9, id);
                Assert.AreEqual(5, q.RewardClovers, id);
                Assert.AreEqual(0, q.RewardGolden, id + ": golden carrots would push the casual week past six golden calls");
                Assert.IsFalse(q.Daily, id);
            }
            GameState s = TwelveOpen(30);
            QuestDef sixteen = quests.Find(x => x.Id == "mo-16");
            Assert.AreEqual(12, Quests.Progress(sixteen, defs, s), 1e-9);
            for (int i = 12; i < 16; i++) s.Levels[i] = 1;
            Assert.AreEqual(16, Quests.Progress(sixteen, defs, s), 1e-9);
        }

        [Test]
        public void Each_New_Station_Plays_A_Loop_And_Has_A_Name_In_Both_Languages()
        {
            foreach (string id in NewIds) Assert.IsNotNull(Sfx.StationLoopOf(id), id);
            var vi = new[] { "Vườn hoa sen", "Lò kẹo mạch nha", "Ruộng lúa nếp", "Thuyền trà đêm" };
            L10n.SetLocale("vi");
            for (int k = 0; k < 4; k++) Assert.AreEqual(vi[k], L10n.Station(NewIds[k]));
            Assert.AreEqual("Có 12 trạm đang hoạt động", L10n.T("quest.mo-12"));
            L10n.SetLocale("en");
            foreach (string id in NewIds)
            {
                string name = L10n.Station(id);
                Assert.IsFalse(string.IsNullOrEmpty(name), id);
                Assert.AreNotEqual("station." + id, name, id + " has an English name");
            }
            Assert.AreEqual("Needs garden level 16", L10n.T("row.needlevel", 16));
        }

        // ----- How the screen shows them -----

        sealed class LiveScreen : System.IDisposable
        {
            public readonly GameObject Root;
            public readonly GameRunner Runner;
            public readonly GardenScreen View;

            public LiveScreen(GameCatalog catalog, GameState state)
            {
                Motion.SetReduced(true);
                Root = new GameObject("Game");
                var cam = new GameObject("Cam", typeof(Camera));
                cam.transform.SetParent(Root.transform);
                Runner = Root.AddComponent<GameRunner>();
                Runner.Catalog = catalog;
                Runner.Init(state);
                View = Root.AddComponent<GardenScreen>();
                View.Runner = Runner;
                View.Cam = cam.GetComponent<Camera>();
                View.Build();
                View.Shell.StartGame();
                View.Refresh();
            }

            public Text Text(string path)
            {
                Transform t = Root.transform.Find(path);
                Assert.IsNotNull(t, "Missing UI object: " + path);
                return t.GetComponent<Text>();
            }

            public void Dispose()
            {
                Object.DestroyImmediate(Root);
                Motion.SetReduced(false);
            }
        }

        [Test]
        public void The_Stations_Tab_Lists_Sixteen_Rows_And_Says_Which_Garden_Level_A_New_One_Asks_For()
        {
            L10n.SetLocale("vi");
            GameState s = TwelveOpen(10, 12);
            using (var screen = new LiveScreen(catalog, s))
            {
                screen.View.OpenSheet(0);
                screen.View.Refresh();
                Transform content = screen.Root.transform.Find("Canvas/Sheet/Panel/Scroll/Content");
                foreach (StationDef d in defs) Assert.IsNotNull(content.Find("Row " + d.Id), d.Id + " has a row");
                Assert.AreEqual(L10n.T("row.needlevel", defs[12].NeedGardenLevel), screen.Text("Canvas/Sheet/Panel/Scroll/Content/Row ho-sen/Sub").text);
                Assert.AreEqual(L10n.T("row.needlevel", defs[15].NeedGardenLevel), screen.Text("Canvas/Sheet/Panel/Scroll/Content/Row thuyen-tra/Sub").text);

                // Grown enough, the lotus garden waits on its land, which #53 brings.
                s.GardenXp = Garden.XpForLevel(defs[12].NeedGardenLevel);
                screen.View.Refresh();
                Assert.AreEqual(L10n.T("row.needplot"), screen.Text("Canvas/Sheet/Panel/Scroll/Content/Row ho-sen/Sub").text);
            }
        }

        [Test]
        public void With_Their_Segments_Open_The_Four_Stand_On_Their_Tiles_Inside_The_Garden()
        {
            GameState s = TwelveOpen(30);
            for (int i = 12; i < 16; i++) s.Levels[i] = 1;
            using (var screen = new LiveScreen(catalog, s))
            {
                var street = (RectTransform)screen.Root.transform.Find("Canvas/Garden/Content");
                for (int i = 12; i < 16; i++)
                {
                    Transform actor = street.Find("Station " + defs[i].Id);
                    Assert.IsTrue(actor.gameObject.activeSelf, defs[i].Id + " stands in the garden");
                    Vector2 spot = screen.View.StationSpot(i);
                    Assert.AreEqual(GardenScreen.CellToWorld(GardenGrid.DefaultStationTile(i)), spot, defs[i].Id + " on its tile");
                    Assert.IsTrue(spot.x > 200f && spot.x < street.sizeDelta.x - 200f && spot.y > 200f && spot.y < street.sizeDelta.y - 300f,
                        defs[i].Id + " at " + spot + " stands well inside the garden of " + street.sizeDelta);
                }
            }
        }

        [Test]
        public void Touching_A_New_Stations_Row_Slides_The_Garden_To_Its_Tile()
        {
            GameState s = TwelveOpen(30);
            for (int i = 12; i < 16; i++) s.Levels[i] = 1;
            using (var screen = new LiveScreen(catalog, s))
            {
                var world = (RectTransform)screen.Root.transform.Find("Canvas/Garden/Content");
                screen.View.OpenSheet(0);
                for (int f = 0; f < 10; f++) screen.View.Advance(1f / 30f);
                for (int i = 12; i < 16; i++)
                {
                    screen.Root.transform.Find("Canvas/Sheet/Panel/Scroll/Content/Row " + defs[i].Id).GetComponent<Button>().onClick.Invoke();
                    for (int f = 0; f < 12; f++) screen.View.Advance(1f / 30f);
                    Vector2 at = screen.View.StationSpot(i) * world.localScale.x + world.anchoredPosition;
                    Assert.Greater(at.y, screen.View.SheetHeight + 60f, defs[i].Id + " stands above the sheet");
                    Assert.Less(at.y, 1920f - 160f, defs[i].Id + " stands under the HUD");
                    Assert.That(at.x, Is.InRange(0f, 1080f), defs[i].Id + " is on the screen");
                }
            }
        }

        [Test]
        public void A_Batch_From_The_Candy_Kiln_Shows_Its_Own_Product_In_The_Tray()
        {
            GameState s = TwelveOpen(30);
            s.Levels[12] = 1; s.Levels[13] = 1;
            using (var screen = new LiveScreen(catalog, s))
            {
                Tray.Add(s, 13, false);
                screen.View.Refresh();
                Transform tray = screen.Root.transform.Find("Canvas/Tray");
                Assert.IsTrue(tray.gameObject.activeSelf);
                Assert.AreEqual("product_lo-keo", tray.Find("Slot 0/Icon").GetComponent<Image>().sprite.name);
            }
        }
    }
}
