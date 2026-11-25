using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using VuonTra.Core;
using VuonTra.Game;
using VuonTra.Game.UI;

namespace VuonTra.EditorTools
{
    public static class MetaSeeder
    {
        public const string UpgradeFolder = "Assets/VuonTra/Content/Upgrades";
        public const string QuestFolder = "Assets/VuonTra/Content/Quests";
        public const string ItemFolder = "Assets/VuonTra/Content/Items";

        struct U { public string Id, Name, Desc, Glyph; public UpgradeEffect Effect; public double Amount, Cost, Growth; public int Max; public Color Accent; }
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

        struct Q { public string Id, Title, Station; public QuestMetric Metric; public double Target, Seconds, Carrots; public int Clovers, Golden; public bool Daily; public Season Season; public int Jars; }

        struct C { public string Id, Title; public QuestMetric Metric; public double[] Targets; public string[] Merged; public int[] MergedAt; public bool JarTiers; }

        static readonly double[] TierSeconds = { 12, 30, 60, 90, 180, 300, 600, 900 };
        static readonly double[] TierCarrots = { 100, 300, 1000, 3000, 1e4, 3e4, 1e5, 3e5 };
        static readonly int[] TierGolden = { 0, 0, 1, 0, 1, 0, 2, 3 };
        const int JarTierFrom = 4;

        static QuestTier[] TiersOf(C chain, Q[] mergedRows)
        {
            var tiers = new QuestTier[chain.Targets.Length];
            for (int i = 0; i < tiers.Length; i++)
            {
                int merged = System.Array.IndexOf(chain.MergedAt, i);
                if (merged >= 0)
                {
                    Q old = System.Array.Find(mergedRows, row => row.Id == chain.Merged[merged]);
                    tiers[i] = new QuestTier
                    {
                        Target = chain.Targets[i], RewardSeconds = old.Seconds, RewardCarrots = old.Carrots,
                        RewardGolden = old.Golden, RewardClovers = old.Clovers, Merged = true
                    };
                    continue;
                }
                bool jar = chain.JarTiers && i >= JarTierFrom;
                tiers[i] = new QuestTier
                {
                    Target = chain.Targets[i], RewardSeconds = TierSeconds[i], RewardCarrots = TierCarrots[i],
                    RewardGolden = jar ? 0 : TierGolden[i], RewardJars = jar ? 1 : 0
                };
            }
            return tiers;
        }

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
                new Q { Id = "ngay-cham", Title = "Chạm bé thỏ 50 lần",             Metric = QuestMetric.Taps,          Target = 50, Seconds = 30, Carrots = 30,  Clovers = 1, Daily = true },
                new Q { Id = "ngay-nang", Title = "Nâng cấp trạm 20 lần",           Metric = QuestMetric.LevelsBought,  Target = 20, Seconds = 30, Carrots = 30,  Clovers = 1, Daily = true },
                new Q { Id = "ngay-thu",  Title = "Thu hoạch đủ cà rốt hôm nay",    Metric = QuestMetric.CarrotsEarned, Target = 0,  Seconds = 60, Carrots = 60,  Jars = 1, Daily = true },
                new Q { Id = "ngay-don",  Title = "Giao 2 đơn trà cho khách",      Metric = QuestMetric.OrdersDelivered, Target = 2, Seconds = 30, Carrots = 30, Clovers = 1, Daily = true },
                new Q { Id = "ngay-anh",  Title = "Chụp 3 tấm thỏ xin ảnh",        Metric = QuestMetric.PhotoAsksAnswered, Target = 3, Seconds = 30, Carrots = 30, Clovers = 1, Daily = true },
                new Q { Id = "mo-2",      Title = "Đánh thức bé thỏ thứ hai",       Metric = QuestMetric.StationsOpen,  Target = 2,  Seconds = 12,  Carrots = 60 },
                new Q { Id = "mo-4",      Title = "Có 4 trạm đang hoạt động",       Metric = QuestMetric.StationsOpen,  Target = 4,  Seconds = 90,  Carrots = 500,  Clovers = 2 },
                new Q { Id = "mo-8",      Title = "Cả 8 bé thỏ đều thức",           Metric = QuestMetric.StationsOpen,  Target = 8,  Seconds = 360, Carrots = 5000, Clovers = 5 },
                new Q { Id = "mo-12",     Title = "Cả 12 bé thỏ đều thức",          Metric = QuestMetric.StationsOpen,  Target = 12, Seconds = 600, Carrots = 5e4, Clovers = 5 },
                new Q { Id = "mo-16",     Title = "Có 16 trạm",                     Metric = QuestMetric.StationsOpen,  Target = 16, Seconds = 900, Carrots = 1e5, Clovers = 5 },
                new Q { Id = "tra-10",    Title = "Ấm trà sương đạt cấp 10",        Metric = QuestMetric.StationLevel,  Station = "am-tra", Target = 10, Seconds = 18, Carrots = 100 },
                new Q { Id = "tra-25",    Title = "Ấm trà sương đạt cấp 25",        Metric = QuestMetric.StationLevel,  Station = "am-tra", Target = 25, Seconds = 60, Carrots = 500, Clovers = 1 },
                new Q { Id = "tra-50",    Title = "Ấm trà sương đạt cấp 50",        Metric = QuestMetric.StationLevel,  Station = "am-tra", Target = 50, Seconds = 180, Carrots = 5000, Clovers = 2 },
                new Q { Id = "bua-1",     Title = "Học bùa đầu tiên",               Metric = QuestMetric.UpgradesOwned, Target = 1,  Seconds = 12,  Carrots = 80 },
                new Q { Id = "bua-10",    Title = "Học tổng cộng 10 cấp bùa",       Metric = QuestMetric.UpgradesOwned, Target = 10, Seconds = 180, Carrots = 3000, Clovers = 2 },
                new Q { Id = "gia-3sao",  Title = "Giã bánh 3 sao lần đầu",         Metric = QuestMetric.PoundingThreeStar,   Target = 1,  Seconds = 60,  Carrots = 1000, Clovers = 2 },
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
                new Q { Id = "nghi-6",    Title = "Đủ 6 bé thỏ cùng nghỉ",          Metric = QuestMetric.RabbitsResting,       Target = 6, Seconds = 90, Carrots = 2000, Clovers = 2 },
                new Q { Id = "loai-tim",  Title = "Gặp loài thỏ bậc Tím đầu tiên",   Metric = QuestMetric.RarestKindMet, Target = 4,  Seconds = 60,  Carrots = 1000, Golden = 3 },
                new Q { Id = "loai-vang", Title = "Gặp loài thỏ bậc Vàng đầu tiên",  Metric = QuestMetric.RarestKindMet, Target = 5,  Seconds = 90,  Carrots = 3000, Golden = 3 },
                new Q { Id = "loai-do",   Title = "Gặp loài thỏ bậc Đỏ đầu tiên",    Metric = QuestMetric.RarestKindMet, Target = 6,  Seconds = 180, Carrots = 9000, Golden = 3 },
                new Q { Id = "dat-12",    Title = "Mở đủ 12 ô đất",                  Metric = QuestMetric.PlotsOwned,    Target = 12, Seconds = 180, Carrots = 9000, Golden = 3 },
                new Q { Id = "la-3sao",   Title = "Hứng lá 3 sao lần đầu",           Metric = QuestMetric.LeavesThreeStar, Target = 1,  Seconds = 60,  Carrots = 1000, Clovers = 2 },
                new Q { Id = "dom-3sao",  Title = "Bắt đom đóm 3 sao lần đầu",       Metric = QuestMetric.FirefliesThreeStar, Target = 1,  Seconds = 60,  Carrots = 1000, Clovers = 2 },
                new Q { Id = "ech-5",     Title = "Gặp Thỏ Ếch 5 lần",               Metric = QuestMetric.FrogsMet, Target = 5, Seconds = 60, Carrots = 1000, Clovers = 2 },
            };
            var mergedRows = new[]
            {
                new Q { Id = "cham-100",  Title = "Chạm tổng cộng 100 lần",         Metric = QuestMetric.Taps,          Target = 100,  Seconds = 24,  Carrots = 100 },
                new Q { Id = "cham-1000", Title = "Chạm tổng cộng 1.000 lần",       Metric = QuestMetric.Taps,          Target = 1000, Seconds = 180, Carrots = 2000, Clovers = 2 },
                new Q { Id = "thu-1k",    Title = "Thu tổng cộng 1.000 cà rốt",     Metric = QuestMetric.CarrotsEarned, Target = 1e3, Seconds = 12,  Carrots = 150 },
                new Q { Id = "thu-1m",    Title = "Thu tổng cộng 1 triệu cà rốt",   Metric = QuestMetric.CarrotsEarned, Target = 1e6, Seconds = 90,  Carrots = 1e5, Clovers = 1 },
                new Q { Id = "thu-1b",    Title = "Thu tổng cộng 1 tỉ cà rốt",      Metric = QuestMetric.CarrotsEarned, Target = 1e9, Seconds = 180, Carrots = 1e8, Clovers = 3 },
                new Q { Id = "anh-1",     Title = "Chụp tấm ảnh đầu tiên",          Metric = QuestMetric.PhotosTaken,         Target = 1, Seconds = 12, Carrots = 100 },
                new Q { Id = "gia-30",    Title = "Giã 30 ván",                      Metric = QuestMetric.PoundingGames,       Target = 30, Seconds = 180, Carrots = 3000, Clovers = 3 },
                new Q { Id = "tho-max",   Title = "Một bé thỏ đạt cấp tối đa",       Metric = QuestMetric.RabbitsAtCap,  Target = 1,  Seconds = 90,  Carrots = 3000, Golden = 3 },
                new Q { Id = "la-30",     Title = "Hứng lá 30 ván",                  Metric = QuestMetric.LeavesGames,     Target = 30, Seconds = 180, Carrots = 3000, Clovers = 3 },
                new Q { Id = "dom-30",    Title = "Bắt đom đóm 30 đêm",              Metric = QuestMetric.FirefliesGames,     Target = 30, Seconds = 180, Carrots = 3000, Clovers = 3 },
                new Q { Id = "don-20",    Title = "Giao 20 đơn trà",                 Metric = QuestMetric.OrdersDelivered, Target = 20, Seconds = 90, Carrots = 2000, Clovers = 2 },
                new Q { Id = "loai-10",   Title = "Gặp 10 loài thỏ",                 Metric = QuestMetric.KindsMet, Target = 10, Seconds = 30,  Carrots = 300,   Golden = 1 },
                new Q { Id = "loai-20",   Title = "Gặp 20 loài thỏ",                 Metric = QuestMetric.KindsMet, Target = 20, Seconds = 60,  Carrots = 1000,  Golden = 2, Clovers = 1 },
                new Q { Id = "loai-40",   Title = "Gặp 40 loài thỏ · tặng Mũ sao",   Metric = QuestMetric.KindsMet, Target = 40, Seconds = 120, Carrots = 5000,  Golden = 10 },
                new Q { Id = "loai-72",   Title = "Gặp đủ 72 loài · khung Đủ bộ",    Metric = QuestMetric.KindsMet, Target = 72, Seconds = 300, Carrots = 20000, Golden = 20 },
                new Q { Id = "ca-50",     Title = "Câu 50 con cá",                   Metric = QuestMetric.FishCaught, Target = 50, Seconds = 60, Carrots = 1000, Clovers = 2 },
                new Q { Id = "tim-10",    Title = "Một bé thỏ đạt 10 tim",           Metric = QuestMetric.MostHearts, Target = 10, Seconds = 180, Carrots = 9000, Golden = 3 },
            };
            var chainRows = new[]
            {
                new C { Id = "cham",  Title = "Chạm",            Metric = QuestMetric.Taps,              Targets = new[] { 100d, 1000d, 20000d, 30000d, 45000d, 58000d, 70000d, 100000d }, Merged = new[] { "cham-100", "cham-1000" }, MergedAt = new[] { 0, 1 } },
                new C { Id = "nang",  Title = "Nâng cấp",        Metric = QuestMetric.LevelsBought,      Targets = new[] { 100d, 5000d, 6000d, 8000d, 12000d, 20000d, 35000d, 50000d }, Merged = new string[0], MergedAt = new int[0], JarTiers = true },
                new C { Id = "thu",   Title = "Thu cà rốt",      Metric = QuestMetric.CarrotsEarned,     Targets = new[] { 1e3, 1e6, 1e9, 1e30, 1e31, 1e32, 1e34, 1e36 }, Merged = new[] { "thu-1k", "thu-1m", "thu-1b" }, MergedAt = new[] { 0, 1, 2 } },
                new C { Id = "don",   Title = "Đơn khách",       Metric = QuestMetric.OrdersDelivered,   Targets = new[] { 5d, 20d, 70d, 110d, 150d, 195d, 240d, 2000d }, Merged = new[] { "don-20" }, MergedAt = new[] { 1 }, JarTiers = true },
                new C { Id = "ca",    Title = "Câu cá",          Metric = QuestMetric.FishCaught,        Targets = new[] { 5d, 20d, 50d, 120d, 250d, 500d, 1000d, 2000d }, Merged = new[] { "ca-50" }, MergedAt = new[] { 2 } },
                new C { Id = "anh",   Title = "Ảnh",             Metric = QuestMetric.PhotosTaken,       Targets = new[] { 1d, 20d, 35d, 50d, 65d, 80d, 200d, 500d }, Merged = new[] { "anh-1" }, MergedAt = new[] { 0 }, JarTiers = true },
                new C { Id = "van",   Title = "Ván chơi",        Metric = QuestMetric.MiniGames,         Targets = new[] { 5d, 30d, 60d, 90d, 150d, 250d, 500d, 1000d }, Merged = new[] { "gia-30", "la-30", "dom-30" }, MergedAt = new[] { 1, 2, 3 } },
                new C { Id = "loai",  Title = "Loài đã gặp",     Metric = QuestMetric.KindsMet,          Targets = new[] { 10d, 20d, 40d, 50d, 55d, 60d, 66d, 72d }, Merged = new[] { "loai-10", "loai-20", "loai-40", "loai-72" }, MergedAt = new[] { 0, 1, 2, 7 }, JarTiers = true },
                new C { Id = "tho",   Title = "Thỏ cấp tối đa",  Metric = QuestMetric.RabbitsAtCap,      Targets = new[] { 1d, 20d, 24d, 28d, 32d, 36d, 38d, 40d }, Merged = new[] { "tho-max" }, MergedAt = new[] { 0 } },
                new C { Id = "tim",   Title = "Tim đầy",         Metric = QuestMetric.RabbitsFullHearts, Targets = new[] { 1d, 2d, 4d, 6d, 10d, 15d, 22d, 30d }, Merged = new[] { "tim-10" }, MergedAt = new[] { 0 }, JarTiers = true },
                new C { Id = "cap",   Title = "Cấp vườn",        Metric = QuestMetric.GardenLevelReached, Targets = new[] { 5d, 32d, 34d, 36d, 40d, 60d, 80d, 99d }, Merged = new string[0], MergedAt = new int[0] },
                new C { Id = "trang", Title = "Mùa trăng mới",   Metric = QuestMetric.NewMoons,          Targets = new[] { 1d, 2d, 3d, 4d, 5d, 6d, 7d }, Merged = new string[0], MergedAt = new int[0], JarTiers = true },
            };
            var quests = new List<QuestData>();
            foreach (Q row in questRows)
                if (row.Daily) quests.Add(SeedQuest(row, stations));
            foreach (C row in chainRows)
            {
                QuestData data = LoadOrCreate<QuestData>(QuestFolder + "/" + row.Id + ".asset");
                data.Id = row.Id; data.Title = row.Title; data.Metric = row.Metric; data.Station = null;
                data.Target = row.Targets[row.Targets.Length - 1]; data.Daily = false; data.RewardSeconds = 0; data.RewardCarrots = 0;
                data.RewardClovers = 0; data.RewardGolden = 0; data.Season = Season.None; data.RewardJars = 0;
                data.Tiers = TiersOf(row, mergedRows); data.Merged = row.Merged;
                EditorUtility.SetDirty(data);
                quests.Add(data);
            }
            foreach (Q row in questRows)
                if (!row.Daily) quests.Add(SeedQuest(row, stations));
            foreach (Q gone in mergedRows)
                AssetDatabase.DeleteAsset(QuestFolder + "/" + gone.Id + ".asset");

            catalog.RabbitNames = new[]
            {
                "Bông", "Mây", "Sữa", "Mochi", "Đậu", "Bánh Trôi", "Kem", "Nếp", "Mật", "Gạo", "Sương", "Trăng",
                "Mít", "Na", "Bơ", "Cốm", "Chè", "Bưởi", "Sen", "Mận", "Tuyết", "Nắng", "Su Su", "Khoai"
            };
            var tierSprites = new Sprite[Rabbits.TierCount];
            for (int t = 0; t < tierSprites.Length; t++)
                tierSprites[t] = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/VuonTra/Art/Final/rabbit_tier_" + t + ".png");
            catalog.TierRabbits = tierSprites;
            var kindSprites = new Sprite[RabbitKinds.All.Count];
            for (int k = 0; k < kindSprites.Length; k++)
                kindSprites[k] = FinalArt.Kind(RabbitKinds.All[k].Id);
            catalog.KindArt = kindSprites;

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
            catalog.MoonJar = FinalArt.Find("icon_hu-trang");
            catalog.Fish = new[] { FinalArt.Find("fish_1"), FinalArt.Find("fish_2"), FinalArt.Find("fish_3"), FinalArt.Find("fish_4"), FinalArt.Find("fish_5"), FinalArt.Find("fish_6") };
            catalog.FishBasket = FinalArt.Find("item_gio-ca");
            catalog.FloatUp = FinalArt.Find("float_up");
            catalog.FloatDown = FinalArt.Find("float_down");
            catalog.PondWater = FinalArt.Find("pond_water");
            catalog.Ripples = System.Array.FindAll(new[] { FinalArt.Find("ripple_1"), FinalArt.Find("ripple_2"), FinalArt.Find("ripple_3") }, s => s != null);
            catalog.ShopProp = FinalArt.Find("prop_tiem");
            catalog.Pad = FinalArt.Load("pad");
            catalog.StationPads = System.Array.FindAll(new[] { FinalArt.Find("pad_dat"), FinalArt.Find("pad_san-go"), FinalArt.Find("pad_vong-da"), FinalArt.Find("pad_hoa") }, s => s != null);
            Sprite stumpSeat = null;
            catalog.RestProps = new[] { stumpSeat, stumpSeat, FinalArt.Find("decor_tham-picnic"), FinalArt.Find("decor_ghe-nam"), FinalArt.Find("decor_vong"), FinalArt.Find("decor_xich-du") };
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

        static QuestData SeedQuest(Q row, Dictionary<string, StationData> stations)
        {
            QuestData data = LoadOrCreate<QuestData>(QuestFolder + "/" + row.Id + ".asset");
            data.Id = row.Id; data.Title = row.Title; data.Metric = row.Metric;
            data.Station = row.Station != null ? stations[row.Station] : null;
            data.Target = row.Target; data.Daily = row.Daily; data.RewardSeconds = row.Seconds;
            data.RewardCarrots = row.Carrots; data.RewardClovers = row.Clovers; data.RewardGolden = row.Golden; data.Season = row.Season; data.RewardJars = row.Jars;
            data.Tiers = new QuestTier[0]; data.Merged = new string[0];
            EditorUtility.SetDirty(data);
            return data;
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
