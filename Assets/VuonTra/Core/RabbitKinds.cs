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

    /// <summary>
    /// The body a kind is drawn on (owner's review 23 Nov: one master rabbit, so clothes and work animations fit
    /// every kind). A kind on the master is cut into body, head and ears that move on their own; one with a shape
    /// of its own is drawn whole until it is redrawn on the master.
    /// </summary>
    public enum RabbitShape
    {
        /// <summary>A shape of its own: drawn and moved as one picture.</summary>
        Own,
        /// <summary>The master rabbit (Thỏ Bông) with standing ears.</summary>
        Master,
        /// <summary>The master rabbit with lop ears, which hang from the head and move with it.</summary>
        MasterLop
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
        /// Where this kind's head is in its drawing (320 by 343): the middle of the skull across, as a share of the
        /// width from the left; the top of the skull between the ears, as a share of the height from the feet; and
        /// how wide the head is, as a share of the height. Hats sit on this and are sized by it, charms follow it.
        /// </summary>
        public float HeadX, HeadTop, HeadWidth;
        /// <summary>The picture already has a dark ink line, so it is used as drawn.</summary>
        public bool OwnOutline;
        /// <summary>Its head already carries something (antlers, horns, flame, its own hat, a moon on the brow): it cannot wear a hat.</summary>
        public bool NoHat;
        /// <summary>Its neck already carries something (a scarf, a collar, a pendant): it cannot wear a charm.</summary>
        public bool NoCharm;
        /// <summary>Drawn on the master rabbit (and so cut into parts that move) or with a shape of its own.</summary>
        public RabbitShape Shape;
        /// <summary>The stretch of the year this kind can be met in (#8); None for the kinds of every day.</summary>
        public Season Season;
        /// <summary>Comes only with a new moon (#1): never from clovers, the shop or a merge.</summary>
        public bool NewMoonOnly;

        public RabbitKind(string id, int tier, RabbitTrade trade, string nameVi, string nameEn, bool ownGlow = false,
            int headX = 150, int headTop = 86, int headWidth = 140, bool ownOutline = false, bool noHat = false, bool noCharm = false,
            Season season = Season.None, bool newMoonOnly = false, RabbitShape shape = RabbitShape.Own)
        {
            Shape = shape;
            Season = season; NewMoonOnly = newMoonOnly;
            Id = id; Tier = tier; Trade = trade; NameVi = nameVi; NameEn = nameEn; OwnGlow = ownGlow;
            HeadX = headX / DrawingWidth; HeadTop = 1f - headTop / DrawingHeight; HeadWidth = headWidth / DrawingHeight;
            OwnOutline = ownOutline; NoHat = noHat; NoCharm = noCharm;
        }

        /// <summary>The size every kind is drawn at; the head numbers given to the constructor are pixels of it, from the top left.</summary>
        public const float DrawingWidth = 320f, DrawingHeight = 343f;
    }

    /// <summary>
    /// The kinds of rabbit. A rabbit's kind is decided when it is called or merged. Adding a kind is one
    /// line here and one picture, Art/Final/kind_&lt;id&gt;.png: names, odds and the album follow from the table.
    /// </summary>
    public static class RabbitKinds
    {
        static readonly RabbitKind[] Table =
        {
            new RabbitKind("tho-bong", 0, RabbitTrade.Brewer, "Thỏ Bông", "Cotton Rabbit", shape: RabbitShape.Master),
            new RabbitKind("tho-me", 0, RabbitTrade.Baker, "Thỏ Mè", "Sesame Rabbit", shape: RabbitShape.Master),
            new RabbitKind("tho-cup", 1, RabbitTrade.Gatherer, "Thỏ Cụp", "Lop Rabbit", shape: RabbitShape.MasterLop),
            new RabbitKind("tho-matcha", 1, RabbitTrade.Brewer, "Thỏ Matcha", "Matcha Rabbit", shape: RabbitShape.Master),
            new RabbitKind("tho-suong", 2, RabbitTrade.Stargazer, "Thỏ Sương", "Dew Rabbit", shape: RabbitShape.Master),
            new RabbitKind("tho-o-long", 2, RabbitTrade.Gatherer, "Thỏ Ô Long", "Oolong Rabbit", shape: RabbitShape.Master),
            new RabbitKind("tho-oai-huong", 3, RabbitTrade.Gatherer, "Thỏ Oải Hương", "Lavender Rabbit", shape: RabbitShape.Master),
            new RabbitKind("tho-khoai-mon", 3, RabbitTrade.Baker, "Thỏ Khoai Môn", "Taro Rabbit", shape: RabbitShape.MasterLop),
            new RabbitKind("tho-mat-ong", 4, RabbitTrade.Baker, "Thỏ Mật Ong", "Honey Rabbit", shape: RabbitShape.Master),
            new RabbitKind("tho-sao", 4, RabbitTrade.Stargazer, "Thỏ Sao", "Star Rabbit", shape: RabbitShape.Master),
            new RabbitKind("tho-hong-tra", 5, RabbitTrade.Brewer, "Thỏ Hồng Trà", "Ruby Tea Rabbit", shape: RabbitShape.Master),
            new RabbitKind("tho-trang", 5, RabbitTrade.Stargazer, "Thỏ Trăng", "Moon Rabbit", shape: RabbitShape.Master),

            // Thirty more, recoloured from the twelve above. The first kind listed for a tier stays that tier's default.
            new RabbitKind("tho-dau-do", 0, RabbitTrade.Baker, "Thỏ Đậu Đỏ", "Red Bean Rabbit", shape: RabbitShape.Master),
            new RabbitKind("tho-ca-phe", 0, RabbitTrade.Brewer, "Thỏ Cà Phê", "Coffee Rabbit", shape: RabbitShape.Master),
            new RabbitKind("tho-tro", 0, RabbitTrade.Gatherer, "Thỏ Tro", "Ash Rabbit", shape: RabbitShape.Master),
            new RabbitKind("tho-cat", 0, RabbitTrade.Stargazer, "Thỏ Cát", "Sand Rabbit", shape: RabbitShape.Master),
            new RabbitKind("tho-bap", 0, RabbitTrade.Baker, "Thỏ Bắp", "Corn Rabbit", shape: RabbitShape.Master),
            new RabbitKind("tho-khoi", 0, RabbitTrade.Stargazer, "Thỏ Khói", "Smoke Rabbit", shape: RabbitShape.Master),
            new RabbitKind("tho-hong-phan", 0, RabbitTrade.Brewer, "Thỏ Hồng Phấn", "Blush Rabbit", shape: RabbitShape.Master),
            new RabbitKind("tho-ca-cao", 0, RabbitTrade.Baker, "Thỏ Ca Cao", "Cocoa Rabbit", shape: RabbitShape.Master),
            new RabbitKind("tho-cup-kem", 1, RabbitTrade.Gatherer, "Thỏ Cụp Kem", "Cream Lop", shape: RabbitShape.MasterLop),
            new RabbitKind("tho-cup-socola", 1, RabbitTrade.Baker, "Thỏ Cụp Sô Cô La", "Chocolate Lop", shape: RabbitShape.MasterLop),
            new RabbitKind("tho-bac-ha", 1, RabbitTrade.Brewer, "Thỏ Bạc Hà", "Mint Rabbit", shape: RabbitShape.Master),
            new RabbitKind("tho-chanh", 1, RabbitTrade.Brewer, "Thỏ Chanh", "Lemon Rabbit", shape: RabbitShape.Master),
            new RabbitKind("tho-cup-suong", 1, RabbitTrade.Stargazer, "Thỏ Cụp Sương", "Misty Lop", shape: RabbitShape.MasterLop),
            new RabbitKind("tho-la-phong", 1, RabbitTrade.Gatherer, "Thỏ Lá Phong", "Maple Rabbit", shape: RabbitShape.Master),
            new RabbitKind("tho-dao", 1, RabbitTrade.Baker, "Thỏ Đào", "Peach Lop", shape: RabbitShape.MasterLop),
            new RabbitKind("tho-tuyet-hong", 2, RabbitTrade.Brewer, "Thỏ Tuyết Hồng", "Rose Snow Rabbit", shape: RabbitShape.Master),
            new RabbitKind("tho-bao", 2, RabbitTrade.Stargazer, "Thỏ Bão", "Storm Rabbit", shape: RabbitShape.Master),
            new RabbitKind("tho-tra-sua", 2, RabbitTrade.Brewer, "Thỏ Trà Sữa", "Milk Tea Rabbit", shape: RabbitShape.Master),
            new RabbitKind("tho-muc", 2, RabbitTrade.Gatherer, "Thỏ Mực", "Ink Rabbit", shape: RabbitShape.Master),
            new RabbitKind("tho-cam", 2, RabbitTrade.Baker, "Thỏ Cam", "Tangerine Rabbit", shape: RabbitShape.Master),
            new RabbitKind("tho-bang", 2, RabbitTrade.Stargazer, "Thỏ Băng", "Ice Rabbit", shape: RabbitShape.Master),
            new RabbitKind("tho-anh-dao", 3, RabbitTrade.Gatherer, "Thỏ Anh Đào", "Cherry Blossom Rabbit", shape: RabbitShape.Master),
            new RabbitKind("tho-luu-ly", 3, RabbitTrade.Gatherer, "Thỏ Lưu Ly", "Forget-me-not Rabbit", shape: RabbitShape.Master),
            new RabbitKind("tho-viet-quat", 3, RabbitTrade.Baker, "Thỏ Việt Quất", "Blueberry Lop", shape: RabbitShape.MasterLop),
            new RabbitKind("tho-dau-tay", 3, RabbitTrade.Baker, "Thỏ Dâu Tây", "Strawberry Lop", shape: RabbitShape.MasterLop),
            new RabbitKind("tho-hoa-nhai", 3, RabbitTrade.Brewer, "Thỏ Hoa Nhài", "Jasmine Rabbit", shape: RabbitShape.Master),
            new RabbitKind("tho-ho-phach", 4, RabbitTrade.Baker, "Thỏ Hổ Phách", "Amber Rabbit", ownGlow: true, shape: RabbitShape.Master),
            new RabbitKind("tho-ngan-ha", 4, RabbitTrade.Stargazer, "Thỏ Ngân Hà", "Galaxy Rabbit", ownGlow: true, shape: RabbitShape.Master),
            new RabbitKind("tho-bach-kim", 4, RabbitTrade.Brewer, "Thỏ Bạch Kim", "Platinum Rabbit", ownGlow: true, shape: RabbitShape.Master),
            new RabbitKind("tho-nguyet-thuc", 5, RabbitTrade.Stargazer, "Thỏ Nguyệt Thực", "Eclipse Rabbit", ownGlow: true, shape: RabbitShape.Master),

            // Thirty with shapes of their own. Each carries where its head is, since no two are alike.
            new RabbitKind("tho-lun", 0, RabbitTrade.Brewer, "Thỏ Lùn", "Dwarf Rabbit", ownOutline: true, headX: 168, headTop: 98, headWidth: 130),
            new RabbitKind("tho-con", 0, RabbitTrade.Baker, "Thỏ Con", "Baby Rabbit", ownOutline: true, headX: 152, headTop: 72, headWidth: 125),
            new RabbitKind("tho-va", 0, RabbitTrade.Gatherer, "Thỏ Vá", "Patch Rabbit", ownOutline: true, headX: 175, headTop: 64, headWidth: 130),
            new RabbitKind("tho-dong", 0, RabbitTrade.Gatherer, "Thỏ Đồng", "Field Hare", ownOutline: true, headX: 190, headTop: 95, headWidth: 105),
            new RabbitKind("tho-long-xoan", 1, RabbitTrade.Gatherer, "Thỏ Lông Xoăn", "Woolly Rabbit", ownOutline: true, headX: 168, headTop: 78, headWidth: 140),
            new RabbitKind("tho-banh-day", 1, RabbitTrade.Baker, "Thỏ Bánh Dày", "Rice Cake Rabbit", ownOutline: true, headX: 155, headTop: 68, headWidth: 150),
            new RabbitKind("tho-dom", 1, RabbitTrade.Stargazer, "Thỏ Đốm", "Spotted Rabbit", ownOutline: true, headX: 172, headTop: 95, headWidth: 120),
            new RabbitKind("tho-kem-chay", 1, RabbitTrade.Baker, "Thỏ Kem Chảy", "Icing Rabbit", ownOutline: true, headX: 185, headTop: 98, headWidth: 115),
            new RabbitKind("tho-tai-dai", 2, RabbitTrade.Gatherer, "Thỏ Tai Dài", "Longear Rabbit", ownOutline: true, headX: 168, headTop: 100, headWidth: 130),
            new RabbitKind("tho-angora", 2, RabbitTrade.Brewer, "Thỏ Bông Gòn", "Angora Rabbit", ownOutline: true, headX: 160, headTop: 86, headWidth: 130),
            new RabbitKind("tho-latte", 2, RabbitTrade.Brewer, "Thỏ Latte", "Latte Rabbit", ownOutline: true, headX: 165, headTop: 76, headWidth: 135),
            new RabbitKind("tho-dau", 2, RabbitTrade.Baker, "Thỏ Dâu", "Berry Rabbit", ownOutline: true, headX: 170, headTop: 72, headWidth: 140),
            new RabbitKind("tho-flan", 2, RabbitTrade.Baker, "Thỏ Bánh Flan", "Custard Rabbit", ownOutline: true, headX: 170, headTop: 68, headWidth: 140),
            new RabbitKind("tho-su-tu", 3, RabbitTrade.Baker, "Thỏ Sư Tử", "Lionhead Rabbit", ownOutline: true, headX: 172, headTop: 72, headWidth: 150),
            new RabbitKind("tho-hoa-dao", 3, RabbitTrade.Brewer, "Thỏ Hoa Đào", "Peach Blossom Rabbit", ownOutline: true, headX: 168, headTop: 100, headWidth: 135),
            new RabbitKind("tho-sim", 3, RabbitTrade.Gatherer, "Thỏ Sim", "Myrtle Rabbit", ownOutline: true, headX: 165, headTop: 92, headWidth: 140),
            new RabbitKind("tho-may", 3, RabbitTrade.Stargazer, "Thỏ Mây", "Cloud Rabbit", ownOutline: true, headX: 172, headTop: 88, headWidth: 145),
            new RabbitKind("tho-reu", 3, RabbitTrade.Gatherer, "Thỏ Rêu", "Moss Rabbit", ownOutline: true, headX: 183, headTop: 82, headWidth: 125),
            new RabbitKind("tho-gac", 3, RabbitTrade.Gatherer, "Thỏ Gạc", "Antler Rabbit", ownOutline: true, noHat: true, headX: 180, headTop: 95, headWidth: 130),
            new RabbitKind("tho-den-long", 4, RabbitTrade.Stargazer, "Thỏ Đèn Lồng", "Lantern Rabbit", ownOutline: true, ownGlow: true, headX: 200, headTop: 78, headWidth: 135),
            new RabbitKind("tho-tien", 4, RabbitTrade.Gatherer, "Thỏ Tiên", "Fairy Rabbit", ownOutline: true, ownGlow: true, headX: 170, headTop: 98, headWidth: 140),
            new RabbitKind("tho-dom-dom", 4, RabbitTrade.Stargazer, "Thỏ Đom Đóm", "Firefly Rabbit", ownOutline: true, ownGlow: true, headX: 180, headTop: 84, headWidth: 145),
            new RabbitKind("tho-pha-le", 4, RabbitTrade.Stargazer, "Thỏ Pha Lê", "Crystal Rabbit", ownOutline: true, headX: 168, headTop: 112, headWidth: 135),
            new RabbitKind("tho-ba-duoi", 4, RabbitTrade.Brewer, "Thỏ Ba Đuôi", "Three-Tail Rabbit", ownOutline: true, headX: 205, headTop: 98, headWidth: 140),
            new RabbitKind("tho-hang-nga", 5, RabbitTrade.Stargazer, "Thỏ Hằng Nga", "Celestial Rabbit", ownOutline: true, headX: 180, headTop: 102, headWidth: 140, noHat: true, noCharm: true),
            new RabbitKind("tho-mat-troi", 5, RabbitTrade.Baker, "Thỏ Mặt Trời", "Sun Rabbit", ownOutline: true, noHat: true, headX: 180, headTop: 105, headWidth: 135),
            new RabbitKind("tho-cuc-quang", 5, RabbitTrade.Gatherer, "Thỏ Cực Quang", "Aurora Rabbit", ownOutline: true, ownGlow: true, headX: 180, headTop: 95, headWidth: 150),
            new RabbitKind("tho-ngoc", 5, RabbitTrade.Brewer, "Thỏ Ngọc", "Jade Rabbit", ownOutline: true, headX: 172, headTop: 95, headWidth: 140),
            new RabbitKind("tho-rong", 5, RabbitTrade.Baker, "Thỏ Rồng", "Dragon Rabbit", ownOutline: true, noHat: true, headX: 155, headTop: 85, headWidth: 140),
            new RabbitKind("tho-chom-sao", 5, RabbitTrade.Stargazer, "Thỏ Chòm Sao", "Constellation Rabbit", ownOutline: true, headX: 172, headTop: 85, headWidth: 150),
            // The seasons' own kinds (#8): Gold tier, met only while their season runs, kept for good once met.
            new RabbitKind("tho-den-ong-sao", 4, RabbitTrade.Stargazer, "Thỏ Đèn Ông Sao", "Star Lantern Rabbit", season: Season.MidAutumn, headX: 160, headTop: 80, headWidth: 145),
            new RabbitKind("tho-ao-dai", 4, RabbitTrade.Brewer, "Thỏ Áo Dài", "Áo Dài Rabbit", season: Season.Tet, headX: 170, headTop: 80, headWidth: 145, noCharm: true),
            new RabbitKind("tho-noel", 4, RabbitTrade.Baker, "Thỏ Noel", "Noel Rabbit", season: Season.Christmas, headX: 160, headTop: 75, headWidth: 140, noHat: true, noCharm: true),
            // The moon rabbit (#1): purple, a stargazer, and only ever met by beginning a new moon.
            new RabbitKind(NewMoon.MoonKind, 3, RabbitTrade.Stargazer, "Thỏ Trăng Non", "New Moon Rabbit", newMoonOnly: true, headX: 135, headTop: 88, headWidth: 140),
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
                if (Table[i].Tier == tier && !Table[i].NewMoonOnly && (Table[i].Season == Season.None || Table[i].Season == season)) list.Add(Table[i]);
            return list;
        }

        /// <summary>The kinds met only on an occasion, in table order: the seasons' (#8), then the new moon's (#1).</summary>
        public static IReadOnlyList<RabbitKind> Special()
        {
            var list = new List<RabbitKind>(4);
            for (int i = 0; i < Table.Length; i++)
                if (Table[i].Season != Season.None || Table[i].NewMoonOnly) list.Add(Table[i]);
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
