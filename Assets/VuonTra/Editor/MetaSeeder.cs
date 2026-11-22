using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using VuonTra.Core;
using VuonTra.Game;
using VuonTra.Game.UI;

namespace VuonTra.EditorTools
{
    /// <summary>
    /// Creates the garden-wide upgrades and the quests as assets and lists them on the catalog.
    /// Numbers are a first pass; they get tuned through playtests.
    /// </summary>
    public static class MetaSeeder
    {
        public const string UpgradeFolder = "Assets/VuonTra/Content/Upgrades";
        public const string QuestFolder = "Assets/VuonTra/Content/Quests";
        public const string ItemFolder = "Assets/VuonTra/Content/Items";

        struct U { public string Id, Name, Desc, Glyph; public UpgradeEffect Effect; public double Amount, Cost, Growth; public int Max; public Color Accent; }
        /// <summary>The picture file for a charm, by its id.</summary>
        static string CharmArt(string id)
        {
            switch (id)
            {
                case "tay-am": return "bua_ban-tay-am";
                case "gio-dem": return "bua_gio-dem";
                case "trang-ram": return "bua_anh-trang-ram";
                case "giac-ngu": return "bua_giac-ngu-dai";
                default: return "bua_" + id;
            }
        }

        struct Q { public string Id, Title, Station; public QuestMetric Metric; public double Target, Seconds, Carrots; public int Clovers, Golden; public bool Daily; public Season Season; }

        public static void Seed(GameCatalog catalog)
        {
            Directory.CreateDirectory(UpgradeFolder);
            Directory.CreateDirectory(QuestFolder);
            Directory.CreateDirectory(ItemFolder);
            StreetArt.Generate();
            AssetDatabase.Refresh();

            var upgradeRows = new[]
            {
                new U { Id = "tay-am",    Name = "Bàn tay ấm",    Desc = "Mỗi lần chạm làm nhanh thêm 0,2 giây", Glyph = "+",  Effect = UpgradeEffect.TapSeconds,       Amount = 0.2, Cost = 80,   Growth = 3,  Max = 10, Accent = Palette.Blush },
                new U { Id = "gio-dem",   Name = "Gió đêm",       Desc = "Mọi trạm làm nhanh hơn 10%",            Glyph = "»",  Effect = UpgradeEffect.Speed,            Amount = 0.1, Cost = 600,  Growth = 5,  Max = 10, Accent = Palette.Moss },
                new U { Id = "trang-ram", Name = "Ánh trăng rằm", Desc = "Mọi trạm thu gấp đôi cà rốt",           Glyph = "×2", Effect = UpgradeEffect.IncomeMultiplier, Amount = 2,   Cost = 3000, Growth = 15, Max = 10, Accent = Palette.Lantern },
                new U { Id = "giac-ngu",  Name = "Giấc ngủ dài",  Desc = "Vắng mặt vẫn thu thêm 2 giờ",           Glyph = "Zz", Effect = UpgradeEffect.AwayHours,        Amount = 2,   Cost = 5000, Growth = 10, Max = 5,  Accent = Palette.Mix(Palette.Night, Palette.Paper, 0.55f) },
            };
            var upgrades = new List<UpgradeData>();
            foreach (U row in upgradeRows)
            {
                UpgradeData data = LoadOrCreate<UpgradeData>(UpgradeFolder + "/" + row.Id + ".asset");
                data.Id = row.Id; data.DisplayName = row.Name; data.Description = row.Desc; data.Glyph = row.Glyph;
                data.Icon = FinalArt.Find(CharmArt(row.Id));
                data.Effect = row.Effect; data.Amount = row.Amount; data.BaseCost = row.Cost; data.CostGrowth = row.Growth;
                data.MaxLevel = row.Max; data.Accent = row.Accent;
                EditorUtility.SetDirty(data);
                upgrades.Add(data);
            }

            var stations = new Dictionary<string, StationData>();
            foreach (StationData s in catalog.Stations) stations[s.Id] = s;

            var questRows = new[]
            {
                // Daily: come back tomorrow.
                new Q { Id = "ngay-cham", Title = "Chạm bé thỏ 50 lần",             Metric = QuestMetric.Taps,          Target = 50, Seconds = 30, Carrots = 30,  Clovers = 1, Daily = true },
                new Q { Id = "ngay-nang", Title = "Nâng cấp trạm 20 lần",           Metric = QuestMetric.LevelsBought,  Target = 20, Seconds = 30, Carrots = 30,  Clovers = 1, Daily = true },
                new Q { Id = "ngay-thu",  Title = "Thu hoạch đủ cà rốt hôm nay",    Metric = QuestMetric.CarrotsEarned, Target = 0,  Seconds = 60, Carrots = 60,  Clovers = 1, Daily = true },
                new Q { Id = "ngay-don",  Title = "Giao 2 đơn trà cho khách",      Metric = QuestMetric.OrdersDelivered, Target = 2, Seconds = 30, Carrots = 30, Clovers = 1, Daily = true },
                new Q { Id = "ngay-anh",  Title = "Chụp 3 tấm thỏ xin ảnh",        Metric = QuestMetric.PhotoAsksAnswered, Target = 3, Seconds = 30, Carrots = 30, Clovers = 1, Daily = true },
                // Achievements: once.
                new Q { Id = "mo-2",      Title = "Đánh thức bé thỏ thứ hai",       Metric = QuestMetric.StationsOpen,  Target = 2,  Seconds = 12,  Carrots = 60 },
                new Q { Id = "mo-4",      Title = "Có 4 trạm đang hoạt động",       Metric = QuestMetric.StationsOpen,  Target = 4,  Seconds = 90,  Carrots = 500,  Clovers = 2 },
                new Q { Id = "mo-8",      Title = "Cả 8 bé thỏ đều thức",           Metric = QuestMetric.StationsOpen,  Target = 8,  Seconds = 360, Carrots = 5000, Clovers = 5 },
                new Q { Id = "tra-10",    Title = "Ấm trà sương đạt cấp 10",        Metric = QuestMetric.StationLevel,  Station = "am-tra", Target = 10, Seconds = 18, Carrots = 100 },
                new Q { Id = "tra-25",    Title = "Ấm trà sương đạt cấp 25",        Metric = QuestMetric.StationLevel,  Station = "am-tra", Target = 25, Seconds = 60, Carrots = 500, Clovers = 1 },
                new Q { Id = "tra-50",    Title = "Ấm trà sương đạt cấp 50",        Metric = QuestMetric.StationLevel,  Station = "am-tra", Target = 50, Seconds = 180, Carrots = 5000, Clovers = 2 },
                new Q { Id = "cham-100",  Title = "Chạm tổng cộng 100 lần",         Metric = QuestMetric.Taps,          Target = 100,  Seconds = 24,  Carrots = 100 },
                new Q { Id = "cham-1000", Title = "Chạm tổng cộng 1.000 lần",       Metric = QuestMetric.Taps,          Target = 1000, Seconds = 180, Carrots = 2000, Clovers = 2 },
                new Q { Id = "bua-1",     Title = "Học bùa đầu tiên",               Metric = QuestMetric.UpgradesOwned, Target = 1,  Seconds = 12,  Carrots = 80 },
                new Q { Id = "bua-10",    Title = "Học tổng cộng 10 cấp bùa",       Metric = QuestMetric.UpgradesOwned, Target = 10, Seconds = 180, Carrots = 3000, Clovers = 2 },
                new Q { Id = "thu-1k",    Title = "Thu tổng cộng 1.000 cà rốt",     Metric = QuestMetric.CarrotsEarned, Target = 1e3, Seconds = 12,  Carrots = 150 },
                new Q { Id = "thu-1m",    Title = "Thu tổng cộng 1 triệu cà rốt",   Metric = QuestMetric.CarrotsEarned, Target = 1e6, Seconds = 90,  Carrots = 1e5, Clovers = 1 },
                new Q { Id = "thu-1b",    Title = "Thu tổng cộng 1 tỉ cà rốt",      Metric = QuestMetric.CarrotsEarned, Target = 1e9, Seconds = 180, Carrots = 1e8, Clovers = 3 },
                // Pictures of the garden (#18).
                new Q { Id = "anh-1",     Title = "Chụp tấm ảnh đầu tiên",          Metric = QuestMetric.PhotosTaken,         Target = 1, Seconds = 12, Carrots = 100 },
                new Q { Id = "gia-3sao",  Title = "Giã bánh 3 sao lần đầu",         Metric = QuestMetric.PoundingThreeStar,   Target = 1,  Seconds = 60,  Carrots = 1000, Clovers = 2 },
                new Q { Id = "gia-30",    Title = "Giã 30 ván",                      Metric = QuestMetric.PoundingGames,       Target = 30, Seconds = 180, Carrots = 3000, Clovers = 3 },
                // The seasons' quests (#8): one set each, shown only in season, paid in golden carrots.
                new Q { Id = "mua-gap-trung-thu",  Title = "Gặp Thỏ Đèn Ông Sao",             Metric = QuestMetric.SeasonKindMet,          Target = 1, Seconds = 6,  Carrots = 100, Golden = 3,  Season = Season.MidAutumn },
                new Q { Id = "mua-nghe-trung-thu", Title = "Xếp Thỏ Đèn Ông Sao vào đúng nghề", Metric = QuestMetric.SeasonKindInTrade,    Target = 1, Seconds = 6,  Carrots = 100, Golden = 5,  Season = Season.MidAutumn },
                new Q { Id = "mua-anh-trung-thu",  Title = "Chụp ảnh có Thỏ Đèn Ông Sao",       Metric = QuestMetric.PhotosWithSeasonRabbit, Target = 1, Seconds = 6,  Carrots = 100, Golden = 10, Season = Season.MidAutumn },
                new Q { Id = "mua-gap-tet",        Title = "Gặp Thỏ Áo Dài",                  Metric = QuestMetric.SeasonKindMet,          Target = 1, Seconds = 6,  Carrots = 100, Golden = 3,  Season = Season.Tet },
                new Q { Id = "mua-nghe-tet",       Title = "Xếp Thỏ Áo Dài vào đúng nghề",      Metric = QuestMetric.SeasonKindInTrade,    Target = 1, Seconds = 6,  Carrots = 100, Golden = 5,  Season = Season.Tet },
                new Q { Id = "mua-anh-tet",        Title = "Chụp ảnh có Thỏ Áo Dài",            Metric = QuestMetric.PhotosWithSeasonRabbit, Target = 1, Seconds = 6,  Carrots = 100, Golden = 10, Season = Season.Tet },
                new Q { Id = "mua-gap-noel",       Title = "Gặp Thỏ Noel",                    Metric = QuestMetric.SeasonKindMet,          Target = 1, Seconds = 6,  Carrots = 100, Golden = 3,  Season = Season.Christmas },
                new Q { Id = "mua-nghe-noel",      Title = "Xếp Thỏ Noel vào đúng nghề",        Metric = QuestMetric.SeasonKindInTrade,    Target = 1, Seconds = 6,  Carrots = 100, Golden = 5,  Season = Season.Christmas },
                new Q { Id = "mua-anh-noel",       Title = "Chụp ảnh có Thỏ Noel",              Metric = QuestMetric.PhotosWithSeasonRabbit, Target = 1, Seconds = 6,  Carrots = 100, Golden = 10, Season = Season.Christmas },
                new Q { Id = "anh-vang",  Title = "Chụp ảnh có một thỏ bậc Vàng",   Metric = QuestMetric.PhotosWithGoldRabbit, Target = 1, Seconds = 60, Carrots = 1000, Clovers = 2 },
                new Q { Id = "anh-nghi",  Title = "Chụp ảnh khi đủ 6 chỗ nghỉ",     Metric = QuestMetric.PhotosWithFullRest,   Target = 1, Seconds = 90, Carrots = 2000, Clovers = 2 },
                // Golden carrots, once each (#44).
                new Q { Id = "loai-tim",  Title = "Gặp loài thỏ bậc Tím đầu tiên",   Metric = QuestMetric.RarestKindMet, Target = 4,  Seconds = 60,  Carrots = 1000, Golden = 3 },
                new Q { Id = "loai-vang", Title = "Gặp loài thỏ bậc Vàng đầu tiên",  Metric = QuestMetric.RarestKindMet, Target = 5,  Seconds = 90,  Carrots = 3000, Golden = 3 },
                new Q { Id = "loai-do",   Title = "Gặp loài thỏ bậc Đỏ đầu tiên",    Metric = QuestMetric.RarestKindMet, Target = 6,  Seconds = 180, Carrots = 9000, Golden = 3 },
                new Q { Id = "dat-12",    Title = "Mở đủ 12 ô đất",                  Metric = QuestMetric.PlotsOwned,    Target = 12, Seconds = 180, Carrots = 9000, Golden = 3 },
                new Q { Id = "tho-max",   Title = "Một bé thỏ đạt cấp tối đa",       Metric = QuestMetric.RabbitsAtCap,  Target = 1,  Seconds = 90,  Carrots = 3000, Golden = 3 },
                // The leaf-catching game (#10).
                new Q { Id = "la-3sao",   Title = "Hứng lá 3 sao lần đầu",           Metric = QuestMetric.LeavesThreeStar, Target = 1,  Seconds = 60,  Carrots = 1000, Clovers = 2 },
                new Q { Id = "la-30",     Title = "Hứng lá 30 ván",                  Metric = QuestMetric.LeavesGames,     Target = 30, Seconds = 180, Carrots = 3000, Clovers = 3 },
                // The firefly night (#10).
                new Q { Id = "dom-3sao",  Title = "Bắt đom đóm 3 sao lần đầu",       Metric = QuestMetric.FirefliesThreeStar, Target = 1,  Seconds = 60,  Carrots = 1000, Clovers = 2 },
                new Q { Id = "dom-30",    Title = "Bắt đom đóm 30 đêm",              Metric = QuestMetric.FirefliesGames,     Target = 30, Seconds = 180, Carrots = 3000, Clovers = 3 },
                // Guests' tea orders (#20).
                new Q { Id = "don-20",    Title = "Giao 20 đơn trà",                 Metric = QuestMetric.OrdersDelivered, Target = 20, Seconds = 90, Carrots = 2000, Clovers = 2 },
            };
            var quests = new List<QuestData>();
            foreach (Q row in questRows)
            {
                QuestData data = LoadOrCreate<QuestData>(QuestFolder + "/" + row.Id + ".asset");
                data.Id = row.Id; data.Title = row.Title; data.Metric = row.Metric;
                data.Station = row.Station != null ? stations[row.Station] : null;
                data.Target = row.Target; data.Daily = row.Daily; data.RewardSeconds = row.Seconds;
                data.RewardCarrots = row.Carrots; data.RewardClovers = row.Clovers; data.RewardGolden = row.Golden; data.Season = row.Season;
                EditorUtility.SetDirty(data);
                quests.Add(data);
            }

            catalog.RabbitNames = new[]
            {
                "Bông", "Mây", "Sữa", "Mochi", "Đậu", "Bánh Trôi", "Kem", "Nếp", "Mật", "Gạo", "Sương", "Trăng",
                "Mít", "Na", "Bơ", "Cốm", "Chè", "Bưởi", "Sen", "Mận", "Tuyết", "Nắng", "Su Su", "Khoai"
            };
            // Tier drawings are optional: Art/Final/rabbit_tier_0.png … rabbit_tier_5.png when they exist.
            var tierSprites = new Sprite[Rabbits.TierCount];
            for (int t = 0; t < tierSprites.Length; t++)
                tierSprites[t] = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/VuonTra/Art/Final/rabbit_tier_" + t + ".png");
            catalog.TierRabbits = tierSprites;
            // One drawing per kind: Art/Final/kind_<id>.png. A missing one falls back at run time.
            var kindSprites = new Sprite[RabbitKinds.All.Count];
            for (int k = 0; k < kindSprites.Length; k++)
                kindSprites[k] = FinalArt.Kind(RabbitKinds.All[k].Id);
            catalog.KindArt = kindSprites;

            // Quest items carry a bonus; the hats the shop sells (#44, "hat-…", Art/Final/hat_<name>.png) are for looks,
            // priced in golden carrots, sold at a fixed tier, with a lift of their own so each sits right on the head.
            var itemRows = new (string Id, string Name, ItemSlot Slot, ItemEffect Effect, double Amount, int Price, int Tier, float Lift)[]
            {
                ("mu-rom",    "Mũ rơm",        ItemSlot.Hat,   ItemEffect.StationIncome, 0.10, 0, 0, 0f),
                ("mu-bep",    "Mũ đầu bếp",    ItemSlot.Hat,   ItemEffect.GardenSpeed,   0.03, 0, 0, 0f),
                ("vong-hoa",  "Vòng hoa",      ItemSlot.Hat,   ItemEffect.AwayIncome,    0.10, 0, 0, 0f),
                ("chuong",    "Chuông bạc",    ItemSlot.Charm, ItemEffect.StationIncome, 0.10, 0, 0, 0f),
                ("co-may",    "Cỏ may mắn",    ItemSlot.Charm, ItemEffect.GardenSpeed,   0.03, 0, 0, 0f),
                ("long-den",  "Lồng đèn nhỏ",  ItemSlot.Charm, ItemEffect.AwayIncome,    0.10, 0, 0, 0f),
                ("hat-non-la",     "Nón lá",        ItemSlot.Hat, ItemEffect.None, 0, 20, 1, 0.00f),
                ("hat-vong-hoa",   "Vòng hoa tươi", ItemSlot.Hat, ItemEffect.None, 0, 25, 1, 0.06f),
                ("hat-mu-len",     "Mũ len",        ItemSlot.Hat, ItemEffect.None, 0, 30, 2, 0.00f),
                ("hat-no-trang",   "Nơ trăng",      ItemSlot.Hat, ItemEffect.None, 0, 40, 2, 0.05f),
                ("hat-mu-sao",     "Mũ sao",        ItemSlot.Hat, ItemEffect.None, 0, 50, 3, 0.02f),
                ("hat-vuong-mien", "Vương miện",    ItemSlot.Hat, ItemEffect.None, 0, 60, 4, 0.04f),
            };
            var itemAssets = new List<ItemData>();
            foreach (var row in itemRows)
            {
                ItemData data = LoadOrCreate<ItemData>(ItemFolder + "/" + row.Id + ".asset");
                data.Id = row.Id; data.DisplayName = row.Name; data.Slot = row.Slot; data.Effect = row.Effect; data.BaseAmount = row.Amount;
                data.Price = row.Price; data.Tier = row.Tier; data.HatLift = row.Lift;
                string artName = row.Id.StartsWith("hat-") ? "hat_" + row.Id.Substring("hat-".Length) : "item_" + row.Id;
                var drawnIcon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/VuonTra/Art/Final/" + artName + ".png");
                data.Icon = drawnIcon != null ? drawnIcon : PlaceholderArt.Load("item_" + row.Id);
                EditorUtility.SetDirty(data);
                itemAssets.Add(data);
            }
            catalog.Items = itemAssets.ToArray();

            // Work-loop frames: Art/Final/rabbit_<station>_f0.png, _f1.png, … when they exist.
            foreach (StationData station in catalog.Stations)
            {
                var frames = new List<Sprite>();
                for (int f = 0; f < 12; f++)
                {
                    var frame = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/VuonTra/Art/Final/rabbit_" + station.Id + "_f" + f + ".png");
                    if (frame == null) break;
                    frames.Add(frame);
                }
                station.WorkFrames = frames.ToArray();
                EditorUtility.SetDirty(station);
            }

            var decor = new List<Sprite>();
            foreach (string name in new[] { "decor_tree", "decor_bush", "decor_lantern", "decor_fence", "decor_mushrooms", "decor_stones", "decor_pond", "decor_teatable", "decor_stump" })
            {
                var drawn = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/VuonTra/Art/Final/" + name + ".png");
                decor.Add(drawn != null ? drawn : PlaceholderArt.Load(name));
            }
            catalog.Decor = decor.ToArray();

            catalog.Shadow = PlaceholderArt.Load("shadow");
            catalog.IconUp = PlaceholderArt.Load("icon_up");
            var tradeIcons = new Sprite[System.Enum.GetValues(typeof(RabbitTrade)).Length];
            foreach (RabbitTrade trade in System.Enum.GetValues(typeof(RabbitTrade)))
                tradeIcons[(int)trade] = trade == RabbitTrade.None ? null : PlaceholderArt.Load("icon_trade_" + trade);
            catalog.TradeIcons = tradeIcons;
            catalog.MoonGate = FinalArt.Find("prop_cong-trang");
            catalog.GateMidAutumn = FinalArt.Find("decor_cong-trung-thu");
            catalog.GateTet = FinalArt.Find("decor_cong-tet");
            catalog.GateChristmas = FinalArt.Find("decor_cong-giang-sinh");
            catalog.LevelBadge = FinalArt.Find("icon_cap-vuon");
            catalog.Mist = FinalArt.Find("decor_suong");
            catalog.PlotSign = FinalArt.Load("plot_sign");
            catalog.Clover = FinalArt.Load("icon_clover");
            catalog.Hand = FinalArt.Load("icon_hand");
            catalog.Mortar = FinalArt.Find("prop_coi-gia-banh");
            catalog.MortarEmpty = FinalArt.Find("prop_coi-trong");
            catalog.Pestle = FinalArt.Find("prop_chay");
            catalog.Dough = FinalArt.Find("banh_bot");
            catalog.DoughPounded = FinalArt.Find("banh_dang-gia");
            catalog.Cake = FinalArt.Find("banh_xong");
            catalog.CakeDented = FinalArt.Find("banh_meo");
            catalog.TrayWood = FinalArt.Find("khay_go");
            catalog.StoneLantern = FinalArt.Find("decor_den-da");
            catalog.Planter = FinalArt.Find("decor_bon-hoa");
            catalog.GoldenCarrot = FinalArt.Find("icon_ca-rot-vang");
            catalog.GiftBox = FinalArt.Find("icon_hop-qua");
            catalog.ShopProp = FinalArt.Find("prop_tiem");
            catalog.Pad = FinalArt.Load("pad");
            catalog.StationPads = System.Array.FindAll(new[] { FinalArt.Find("pad_dat"), FinalArt.Find("pad_san-go"), FinalArt.Find("pad_vong-da"), FinalArt.Find("pad_hoa") }, s => s != null);
            // Seats 0 and 1 are the stumps; the rest bring a rug, stools, a hammock and a swing.
            catalog.RestProps = new[] { null, null, FinalArt.Find("decor_tham-picnic"), FinalArt.Find("decor_ghe-nam"), FinalArt.Find("decor_vong"), FinalArt.Find("decor_xich-du") };
            catalog.IconCharm = PlaceholderArt.Load("icon_charm");
            catalog.IconQuest = PlaceholderArt.Load("icon_quest");

            catalog.BgSky = StreetArt.Load("bg_sky");
            catalog.BgHills = StreetArt.Load("bg_hills");
            catalog.BgTrees = StreetArt.Load("bg_trees");
            catalog.BgGround = StreetArt.Load("bg_ground");
            catalog.BgVignette = StreetArt.Load("bg_vignette");
            catalog.BgGrass = StreetArt.Load("bg_grass");
            catalog.BgMoonbeam = StreetArt.Load("bg_moonbeam");

            catalog.Upgrades = upgrades.ToArray();
            catalog.Quests = quests.ToArray();
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            Debug.Log("[VuonTra] Seeded " + upgrades.Count + " upgrades and " + quests.Count + " quests.");
        }

        static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }
    }
}
