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
        struct Q { public string Id, Title, Station; public QuestMetric Metric; public double Target, Seconds, Carrots; public int Clovers; public bool Daily; }

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
                new Q { Id = "ngay-cham", Title = "Chạm bé thỏ 50 lần",             Metric = QuestMetric.Taps,          Target = 50, Seconds = 300, Carrots = 30,  Clovers = 1, Daily = true },
                new Q { Id = "ngay-nang", Title = "Nâng cấp trạm 20 lần",           Metric = QuestMetric.LevelsBought,  Target = 20, Seconds = 300, Carrots = 30,  Clovers = 1, Daily = true },
                new Q { Id = "ngay-thu",  Title = "Thu hoạch đủ cà rốt hôm nay",    Metric = QuestMetric.CarrotsEarned, Target = 0,  Seconds = 600, Carrots = 60,  Clovers = 1, Daily = true },
                // Achievements: once.
                new Q { Id = "mo-2",      Title = "Đánh thức bé thỏ thứ hai",       Metric = QuestMetric.StationsOpen,  Target = 2,  Seconds = 120,  Carrots = 60 },
                new Q { Id = "mo-4",      Title = "Có 4 trạm đang hoạt động",       Metric = QuestMetric.StationsOpen,  Target = 4,  Seconds = 900,  Carrots = 500,  Clovers = 2 },
                new Q { Id = "mo-8",      Title = "Cả 8 bé thỏ đều thức",           Metric = QuestMetric.StationsOpen,  Target = 8,  Seconds = 3600, Carrots = 5000, Clovers = 5 },
                new Q { Id = "tra-10",    Title = "Ấm trà sương đạt cấp 10",        Metric = QuestMetric.StationLevel,  Station = "am-tra", Target = 10, Seconds = 180, Carrots = 100 },
                new Q { Id = "tra-25",    Title = "Ấm trà sương đạt cấp 25",        Metric = QuestMetric.StationLevel,  Station = "am-tra", Target = 25, Seconds = 600, Carrots = 500, Clovers = 1 },
                new Q { Id = "tra-50",    Title = "Ấm trà sương đạt cấp 50",        Metric = QuestMetric.StationLevel,  Station = "am-tra", Target = 50, Seconds = 1800, Carrots = 5000, Clovers = 2 },
                new Q { Id = "cham-100",  Title = "Chạm tổng cộng 100 lần",         Metric = QuestMetric.Taps,          Target = 100,  Seconds = 240,  Carrots = 100 },
                new Q { Id = "cham-1000", Title = "Chạm tổng cộng 1.000 lần",       Metric = QuestMetric.Taps,          Target = 1000, Seconds = 1800, Carrots = 2000, Clovers = 2 },
                new Q { Id = "bua-1",     Title = "Học bùa đầu tiên",               Metric = QuestMetric.UpgradesOwned, Target = 1,  Seconds = 120,  Carrots = 80 },
                new Q { Id = "bua-10",    Title = "Học tổng cộng 10 cấp bùa",       Metric = QuestMetric.UpgradesOwned, Target = 10, Seconds = 1800, Carrots = 3000, Clovers = 2 },
                new Q { Id = "thu-1k",    Title = "Thu tổng cộng 1.000 cà rốt",     Metric = QuestMetric.CarrotsEarned, Target = 1e3, Seconds = 120,  Carrots = 150 },
                new Q { Id = "thu-1m",    Title = "Thu tổng cộng 1 triệu cà rốt",   Metric = QuestMetric.CarrotsEarned, Target = 1e6, Seconds = 900,  Carrots = 1e5, Clovers = 1 },
                new Q { Id = "thu-1b",    Title = "Thu tổng cộng 1 tỉ cà rốt",      Metric = QuestMetric.CarrotsEarned, Target = 1e9, Seconds = 1800, Carrots = 1e8, Clovers = 3 },
            };
            var quests = new List<QuestData>();
            foreach (Q row in questRows)
            {
                QuestData data = LoadOrCreate<QuestData>(QuestFolder + "/" + row.Id + ".asset");
                data.Id = row.Id; data.Title = row.Title; data.Metric = row.Metric;
                data.Station = row.Station != null ? stations[row.Station] : null;
                data.Target = row.Target; data.Daily = row.Daily; data.RewardSeconds = row.Seconds;
                data.RewardCarrots = row.Carrots; data.RewardClovers = row.Clovers;
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

            var itemRows = new (string Id, string Name, ItemSlot Slot, ItemEffect Effect, double Amount)[]
            {
                ("mu-rom",    "Mũ rơm",        ItemSlot.Hat,   ItemEffect.StationIncome, 0.10),
                ("mu-bep",    "Mũ đầu bếp",    ItemSlot.Hat,   ItemEffect.GardenSpeed,   0.03),
                ("vong-hoa",  "Vòng hoa",      ItemSlot.Hat,   ItemEffect.AwayIncome,    0.10),
                ("chuong",    "Chuông bạc",    ItemSlot.Charm, ItemEffect.StationIncome, 0.10),
                ("co-may",    "Cỏ may mắn",    ItemSlot.Charm, ItemEffect.GardenSpeed,   0.03),
                ("long-den",  "Lồng đèn nhỏ",  ItemSlot.Charm, ItemEffect.AwayIncome,    0.10),
            };
            var itemAssets = new List<ItemData>();
            foreach (var row in itemRows)
            {
                ItemData data = LoadOrCreate<ItemData>(ItemFolder + "/" + row.Id + ".asset");
                data.Id = row.Id; data.DisplayName = row.Name; data.Slot = row.Slot; data.Effect = row.Effect; data.BaseAmount = row.Amount;
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
            foreach (string name in new[] { "decor_tree", "decor_bush", "decor_lantern", "decor_fence", "decor_mushrooms", "decor_stones" })
            {
                var drawn = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/VuonTra/Art/Final/" + name + ".png");
                decor.Add(drawn != null ? drawn : PlaceholderArt.Load(name));
            }
            catalog.Decor = decor.ToArray();

            catalog.BgSky = StreetArt.Load("bg_sky");
            catalog.BgHills = StreetArt.Load("bg_hills");
            catalog.BgTrees = StreetArt.Load("bg_trees");
            catalog.BgGround = StreetArt.Load("bg_ground");

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
