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

        public RabbitKind(string id, int tier, RabbitTrade trade, string nameVi, string nameEn, bool ownGlow = false)
        {
            Id = id; Tier = tier; Trade = trade; NameVi = nameVi; NameEn = nameEn; OwnGlow = ownGlow;
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

        public static IReadOnlyList<RabbitKind> OfTier(int tier)
        {
            var list = new List<RabbitKind>(2);
            for (int i = 0; i < Table.Length; i++)
                if (Table[i].Tier == tier) list.Add(Table[i]);
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
