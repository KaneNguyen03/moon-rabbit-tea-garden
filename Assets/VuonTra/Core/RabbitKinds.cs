using System;
using System.Collections.Generic;

namespace VuonTra.Core
{
    /// <summary>What a rabbit is good at. At a station of its trade it is worth more; anywhere else it still works.</summary>
    public enum RabbitTrade
    {
        None,
        /// <summary>Tea and milk tea.</summary>
        Brewer,
        /// <summary>Jam, the oven, the bakery.</summary>
        Baker,
        /// <summary>Every ingredient station.</summary>
        Gatherer,
        /// <summary>The star jar and the observatory.</summary>
        Stargazer
    }

    /// <summary>One kind of rabbit to collect. Its tier and trade never change.</summary>
    public sealed class RabbitKind
    {
        public string Id;
        public int Tier;
        public RabbitTrade Trade;
        /// <summary>Display names; the string tables are filled from these.</summary>
        public string NameVi, NameEn;

        /// <summary>The picture already has a glow painted in, so the garden adds none of its own.</summary>
        public bool OwnGlow;

        /// <summary>
        /// Where this kind's head is, as a nudge from the usual spot in shares of the drawing's size (right and
        /// up are positive). Hats and charms both follow it. Zero for the kinds drawn on the shared body.
        /// </summary>
        public float HatX, HatY;
        /// <summary>The picture already has a dark ink line, so it is used as drawn.</summary>
        public bool OwnOutline;
        /// <summary>Something grows where a hat would go (antlers, horns, flame): a hat still counts but is not drawn.</summary>
        public bool NoHat;
        /// <summary>The stretch of the year this kind can be met in (#8); None for the kinds of every day.</summary>
        public Season Season;

        public RabbitKind(string id, int tier, RabbitTrade trade, string nameVi, string nameEn, bool ownGlow = false,
            float hatX = 0f, float hatY = 0f, bool ownOutline = false, bool noHat = false, Season season = Season.None)
        {
            Season = season;
            Id = id; Tier = tier; Trade = trade; NameVi = nameVi; NameEn = nameEn; OwnGlow = ownGlow;
            HatX = hatX; HatY = hatY; OwnOutline = ownOutline; NoHat = noHat;
        }
    }

    /// <summary>
    /// The kinds of rabbit. A rabbit's kind is decided when it is called or merged. Adding a kind is one
    /// line here and one picture, Art/Final/kind_&lt;id&gt;.png: names, odds and the album follow from the table.
    /// </summary>
    public static class RabbitKinds
    {
        static readonly RabbitKind[] Table =
        {
            new RabbitKind("tho-bong", 0, RabbitTrade.Brewer, "Thỏ Bông", "Cotton Rabbit"),
            new RabbitKind("tho-me", 0, RabbitTrade.Baker, "Thỏ Mè", "Sesame Rabbit"),
            new RabbitKind("tho-cup", 1, RabbitTrade.Gatherer, "Thỏ Cụp", "Lop Rabbit"),
            new RabbitKind("tho-matcha", 1, RabbitTrade.Brewer, "Thỏ Matcha", "Matcha Rabbit"),
            new RabbitKind("tho-suong", 2, RabbitTrade.Stargazer, "Thỏ Sương", "Dew Rabbit"),
            new RabbitKind("tho-o-long", 2, RabbitTrade.Gatherer, "Thỏ Ô Long", "Oolong Rabbit"),
            new RabbitKind("tho-oai-huong", 3, RabbitTrade.Gatherer, "Thỏ Oải Hương", "Lavender Rabbit"),
            new RabbitKind("tho-khoai-mon", 3, RabbitTrade.Baker, "Thỏ Khoai Môn", "Taro Rabbit"),
            new RabbitKind("tho-mat-ong", 4, RabbitTrade.Baker, "Thỏ Mật Ong", "Honey Rabbit"),
            new RabbitKind("tho-sao", 4, RabbitTrade.Stargazer, "Thỏ Sao", "Star Rabbit"),
            new RabbitKind("tho-hong-tra", 5, RabbitTrade.Brewer, "Thỏ Hồng Trà", "Ruby Tea Rabbit"),
            new RabbitKind("tho-trang", 5, RabbitTrade.Stargazer, "Thỏ Trăng", "Moon Rabbit"),

            // Thirty more, recoloured from the twelve above. The first kind listed for a tier stays that tier's default.
            new RabbitKind("tho-dau-do", 0, RabbitTrade.Baker, "Thỏ Đậu Đỏ", "Red Bean Rabbit"),
            new RabbitKind("tho-ca-phe", 0, RabbitTrade.Brewer, "Thỏ Cà Phê", "Coffee Rabbit"),
            new RabbitKind("tho-tro", 0, RabbitTrade.Gatherer, "Thỏ Tro", "Ash Rabbit"),
            new RabbitKind("tho-cat", 0, RabbitTrade.Stargazer, "Thỏ Cát", "Sand Rabbit"),
            new RabbitKind("tho-bap", 0, RabbitTrade.Baker, "Thỏ Bắp", "Corn Rabbit"),
            new RabbitKind("tho-khoi", 0, RabbitTrade.Stargazer, "Thỏ Khói", "Smoke Rabbit"),
            new RabbitKind("tho-hong-phan", 0, RabbitTrade.Brewer, "Thỏ Hồng Phấn", "Blush Rabbit"),
            new RabbitKind("tho-ca-cao", 0, RabbitTrade.Baker, "Thỏ Ca Cao", "Cocoa Rabbit"),
            new RabbitKind("tho-cup-kem", 1, RabbitTrade.Gatherer, "Thỏ Cụp Kem", "Cream Lop"),
            new RabbitKind("tho-cup-socola", 1, RabbitTrade.Baker, "Thỏ Cụp Sô Cô La", "Chocolate Lop"),
            new RabbitKind("tho-bac-ha", 1, RabbitTrade.Brewer, "Thỏ Bạc Hà", "Mint Rabbit"),
            new RabbitKind("tho-chanh", 1, RabbitTrade.Brewer, "Thỏ Chanh", "Lemon Rabbit"),
            new RabbitKind("tho-cup-suong", 1, RabbitTrade.Stargazer, "Thỏ Cụp Sương", "Misty Lop"),
            new RabbitKind("tho-la-phong", 1, RabbitTrade.Gatherer, "Thỏ Lá Phong", "Maple Rabbit"),
            new RabbitKind("tho-dao", 1, RabbitTrade.Baker, "Thỏ Đào", "Peach Lop"),
            new RabbitKind("tho-tuyet-hong", 2, RabbitTrade.Brewer, "Thỏ Tuyết Hồng", "Rose Snow Rabbit"),
            new RabbitKind("tho-bao", 2, RabbitTrade.Stargazer, "Thỏ Bão", "Storm Rabbit"),
            new RabbitKind("tho-tra-sua", 2, RabbitTrade.Brewer, "Thỏ Trà Sữa", "Milk Tea Rabbit"),
            new RabbitKind("tho-muc", 2, RabbitTrade.Gatherer, "Thỏ Mực", "Ink Rabbit"),
            new RabbitKind("tho-cam", 2, RabbitTrade.Baker, "Thỏ Cam", "Tangerine Rabbit"),
            new RabbitKind("tho-bang", 2, RabbitTrade.Stargazer, "Thỏ Băng", "Ice Rabbit"),
            new RabbitKind("tho-anh-dao", 3, RabbitTrade.Gatherer, "Thỏ Anh Đào", "Cherry Blossom Rabbit"),
            new RabbitKind("tho-luu-ly", 3, RabbitTrade.Gatherer, "Thỏ Lưu Ly", "Forget-me-not Rabbit"),
            new RabbitKind("tho-viet-quat", 3, RabbitTrade.Baker, "Thỏ Việt Quất", "Blueberry Lop"),
            new RabbitKind("tho-dau-tay", 3, RabbitTrade.Baker, "Thỏ Dâu Tây", "Strawberry Lop"),
            new RabbitKind("tho-hoa-nhai", 3, RabbitTrade.Brewer, "Thỏ Hoa Nhài", "Jasmine Rabbit"),
            new RabbitKind("tho-ho-phach", 4, RabbitTrade.Baker, "Thỏ Hổ Phách", "Amber Rabbit", ownGlow: true),
            new RabbitKind("tho-ngan-ha", 4, RabbitTrade.Stargazer, "Thỏ Ngân Hà", "Galaxy Rabbit", ownGlow: true),
            new RabbitKind("tho-bach-kim", 4, RabbitTrade.Brewer, "Thỏ Bạch Kim", "Platinum Rabbit", ownGlow: true),
            new RabbitKind("tho-nguyet-thuc", 5, RabbitTrade.Stargazer, "Thỏ Nguyệt Thực", "Eclipse Rabbit", ownGlow: true),

            // Thirty with shapes of their own. Each carries where its head is, since no two are alike.
            new RabbitKind("tho-lun", 0, RabbitTrade.Brewer, "Thỏ Lùn", "Dwarf Rabbit", ownOutline: true, hatX: 0.048f, hatY: -0.041f),
            new RabbitKind("tho-con", 0, RabbitTrade.Baker, "Thỏ Con", "Baby Rabbit", ownOutline: true, hatX: 0.030f, hatY: -0.023f),
            new RabbitKind("tho-va", 0, RabbitTrade.Gatherer, "Thỏ Vá", "Patch Rabbit", ownOutline: true, hatX: 0.025f, hatY: -0.049f),
            new RabbitKind("tho-dong", 0, RabbitTrade.Gatherer, "Thỏ Đồng", "Field Hare", ownOutline: true, hatX: 0.011f, hatY: -0.047f),
            new RabbitKind("tho-long-xoan", 1, RabbitTrade.Gatherer, "Thỏ Lông Xoăn", "Woolly Rabbit", ownOutline: true, hatX: 0.052f, hatY: -0.040f),
            new RabbitKind("tho-banh-day", 1, RabbitTrade.Baker, "Thỏ Bánh Dày", "Rice Cake Rabbit", ownOutline: true, hatX: -0.002f, hatY: -0.084f),
            new RabbitKind("tho-dom", 1, RabbitTrade.Stargazer, "Thỏ Đốm", "Spotted Rabbit", ownOutline: true, hatX: 0.050f, hatY: -0.076f),
            new RabbitKind("tho-kem-chay", 1, RabbitTrade.Baker, "Thỏ Kem Chảy", "Icing Rabbit", ownOutline: true, hatX: 0.008f, hatY: -0.047f),
            new RabbitKind("tho-tai-dai", 2, RabbitTrade.Gatherer, "Thỏ Tai Dài", "Longear Rabbit", ownOutline: true, hatX: 0.036f, hatY: -0.119f),
            new RabbitKind("tho-angora", 2, RabbitTrade.Brewer, "Thỏ Bông Gòn", "Angora Rabbit", ownOutline: true, hatX: 0.028f, hatY: -0.111f),
            new RabbitKind("tho-latte", 2, RabbitTrade.Brewer, "Thỏ Latte", "Latte Rabbit", ownOutline: true, hatX: 0.042f, hatY: -0.020f),
            new RabbitKind("tho-dau", 2, RabbitTrade.Baker, "Thỏ Dâu", "Berry Rabbit", ownOutline: true, hatX: 0.048f, hatY: -0.012f),
            new RabbitKind("tho-flan", 2, RabbitTrade.Baker, "Thỏ Bánh Flan", "Custard Rabbit", ownOutline: true, hatX: 0.023f, hatY: -0.049f),
            new RabbitKind("tho-su-tu", 3, RabbitTrade.Baker, "Thỏ Sư Tử", "Lionhead Rabbit", ownOutline: true, hatX: 0.039f, hatY: -0.020f),
            new RabbitKind("tho-hoa-dao", 3, RabbitTrade.Brewer, "Thỏ Hoa Đào", "Peach Blossom Rabbit", ownOutline: true, hatX: 0.072f, hatY: -0.076f),
            new RabbitKind("tho-sim", 3, RabbitTrade.Gatherer, "Thỏ Sim", "Myrtle Rabbit", ownOutline: true, hatX: 0.045f, hatY: -0.047f),
            new RabbitKind("tho-may", 3, RabbitTrade.Stargazer, "Thỏ Mây", "Cloud Rabbit", ownOutline: true, hatX: 0.080f, hatY: -0.050f),
            new RabbitKind("tho-reu", 3, RabbitTrade.Gatherer, "Thỏ Rêu", "Moss Rabbit", ownOutline: true, hatX: 0.053f, hatY: -0.040f),
            new RabbitKind("tho-gac", 3, RabbitTrade.Gatherer, "Thỏ Gạc", "Antler Rabbit", ownOutline: true, hatX: 0.080f, hatY: -0.012f, noHat: true),
            new RabbitKind("tho-den-long", 4, RabbitTrade.Stargazer, "Thỏ Đèn Lồng", "Lantern Rabbit", ownOutline: true, ownGlow: true, hatX: 0.128f, hatY: -0.009f),
            new RabbitKind("tho-tien", 4, RabbitTrade.Gatherer, "Thỏ Tiên", "Fairy Rabbit", ownOutline: true, ownGlow: true, hatX: -0.014f, hatY: -0.125f),
            new RabbitKind("tho-dom-dom", 4, RabbitTrade.Stargazer, "Thỏ Đom Đóm", "Firefly Rabbit", ownOutline: true, ownGlow: true, hatX: 0.105f, hatY: -0.017f),
            new RabbitKind("tho-pha-le", 4, RabbitTrade.Stargazer, "Thỏ Pha Lê", "Crystal Rabbit", ownOutline: true, hatX: 0.062f, hatY: -0.073f),
            new RabbitKind("tho-ba-duoi", 4, RabbitTrade.Brewer, "Thỏ Ba Đuôi", "Three-Tail Rabbit", ownOutline: true, hatX: 0.200f, hatY: -0.047f),
            new RabbitKind("tho-hang-nga", 5, RabbitTrade.Stargazer, "Thỏ Hằng Nga", "Celestial Rabbit", ownOutline: true, hatX: 0.020f, hatY: -0.030f),
            new RabbitKind("tho-mat-troi", 5, RabbitTrade.Baker, "Thỏ Mặt Trời", "Sun Rabbit", ownOutline: true, hatX: 0.103f, hatY: 0.000f, noHat: true),
            new RabbitKind("tho-cuc-quang", 5, RabbitTrade.Gatherer, "Thỏ Cực Quang", "Aurora Rabbit", ownOutline: true, ownGlow: true, hatX: 0.108f, hatY: -0.017f),
            new RabbitKind("tho-ngoc", 5, RabbitTrade.Brewer, "Thỏ Ngọc", "Jade Rabbit", ownOutline: true, hatX: 0.059f, hatY: -0.070f),
            new RabbitKind("tho-rong", 5, RabbitTrade.Baker, "Thỏ Rồng", "Dragon Rabbit", ownOutline: true, hatX: -0.027f, hatY: 0.038f, noHat: true),
            new RabbitKind("tho-chom-sao", 5, RabbitTrade.Stargazer, "Thỏ Chòm Sao", "Constellation Rabbit", ownOutline: true, hatX: 0.123f, hatY: -0.047f),
            // The seasons' own kinds (#8): Gold tier, met only while their season runs, kept for good once met.
            new RabbitKind("tho-den-ong-sao", 4, RabbitTrade.Stargazer, "Thỏ Đèn Ông Sao", "Star Lantern Rabbit", season: Season.MidAutumn),
            new RabbitKind("tho-ao-dai", 4, RabbitTrade.Brewer, "Thỏ Áo Dài", "Áo Dài Rabbit", season: Season.Tet),
            new RabbitKind("tho-noel", 4, RabbitTrade.Baker, "Thỏ Noel", "Noel Rabbit", season: Season.Christmas),
        };

        public static IReadOnlyList<RabbitKind> All => Table;

        public static RabbitKind Find(string id)
        {
            if (id == null) return null;
            for (int i = 0; i < Table.Length; i++)
                if (Table[i].Id == id) return Table[i];
            return null;
        }

        /// <summary>Position in <see cref="All"/>, or -1.</summary>
        public static int IndexOf(string id)
        {
            for (int i = 0; i < Table.Length; i++)
                if (Table[i].Id == id) return i;
            return -1;
        }

        /// <summary>The tier's kinds of every day; with a <paramref name="season"/>, that season's kind as well.</summary>
        public static IReadOnlyList<RabbitKind> OfTier(int tier, Season season = Season.None)
        {
            var list = new List<RabbitKind>(2);
            for (int i = 0; i < Table.Length; i++)
                if (Table[i].Tier == tier && (Table[i].Season == Season.None || Table[i].Season == season)) list.Add(Table[i]);
            return list;
        }

        /// <summary>The kinds that belong to a season, in table order.</summary>
        public static IReadOnlyList<RabbitKind> Seasonal()
        {
            var list = new List<RabbitKind>(3);
            for (int i = 0; i < Table.Length; i++)
                if (Table[i].Season != Season.None) list.Add(Table[i]);
            return list;
        }

        /// <summary>The kind a rabbit of this tier is when nothing says otherwise (old saves, for one).</summary>
        public static string DefaultFor(int tier)
        {
            tier = Math.Max(0, Math.Min(Rabbits.TierCount - 1, tier));
            for (int i = 0; i < Table.Length; i++)
                if (Table[i].Tier == tier) return Table[i].Id;
            return Table[0].Id;
        }

        /// <summary>Turns a number in [0, 1) into one of the tier's kinds, each equally likely.</summary>
        public static string Pick(int tier, double roll)
        {
            IReadOnlyList<RabbitKind> kinds = OfTier(Math.Max(0, Math.Min(Rabbits.TierCount - 1, tier)));
            if (kinds.Count == 0) return DefaultFor(tier);
            if (double.IsNaN(roll) || roll < 0d) roll = 0d;
            int index = (int)Math.Floor(roll * kinds.Count);
            return kinds[Math.Min(kinds.Count - 1, index)].Id;
        }
    }
}
