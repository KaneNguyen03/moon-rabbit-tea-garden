namespace VuonTra.Core
{
    /// <summary>One thing the shop sells for the garden: its id (for its name and picture), what it costs in golden carrots, and the art it is drawn with.</summary>
    public sealed class DecorKind
    {
        public string Id;
        public int Price;
        /// <summary>The name of the drawn sprite in the game's art.</summary>
        public string Art;
        public DecorKind(string id, int price, string art) { Id = id; Price = price; Art = art; }
    }

    /// <summary>
    /// Decorating the garden (#11): four kinds of decoration bought at the tea shop for golden carrots, each as many
    /// times as there is room for, and eight fixed spots around the garden to stand them on. A piece that is bought
    /// waits in storage until it is placed; placing, swapping and putting away cost nothing. One colour hangs on every
    /// paper lantern at once. None of it changes what the garden earns.
    /// </summary>
    public static class Decor
    {
        /// <summary>How many places there are to stand a decoration, and so the most pieces that can be owned in all.</summary>
        public const int Spots = 8;
        /// <summary>What hanging a new lantern colour costs, every time, in golden carrots.</summary>
        public const int LanternColorCost = 10;

        /// <summary>The four kinds, in the order the shop shows them: stone lantern, flower planter, lounge chair, picnic mat.</summary>
        public static readonly DecorKind[] Kinds =
        {
            new DecorKind("den-da", 15, "decor_den-da"),
            new DecorKind("bon-hoa", 20, "decor_bon-hoa"),
            new DecorKind("ghe-nam", 30, "decor_ghe-nam"),
            new DecorKind("tham-picnic", 40, "decor_tham-picnic"),
        };

        /// <summary>The ids of the lantern colours, for their names: paper, blush, mint, lavender.</summary>
        public static readonly string[] LanternColorIds = { "giay", "hong", "bac-ha", "oai-huong" };
        /// <summary>The colours as six hex digits; index 0 is the paper the lanterns come in.</summary>
        public static readonly string[] LanternColors = { "FFF4E0", "F6A5B8", "7FD9B0", "B49BE0" };

        public static int IndexOf(string kindId)
        {
            for (int i = 0; i < Kinds.Length; i++) if (Kinds[i].Id == kindId) return i;
            return -1;
        }

        public static DecorKind Find(string kindId)
        {
            int i = IndexOf(kindId);
            return i < 0 ? null : Kinds[i];
        }

        /// <summary>How many of a kind the player has bought, placed or not.</summary>
        public static int Owned(GameState s, string kindId)
        {
            int i = IndexOf(kindId);
            return i < 0 || i >= s.DecorOwned.Length ? 0 : s.DecorOwned[i];
        }

        public static int TotalOwned(GameState s)
        {
            int n = 0;
            foreach (int count in s.DecorOwned) n += count;
            return n;
        }

        /// <summary>How many spots hold something.</summary>
        public static int Placed(GameState s)
        {
            int n = 0;
            foreach (string kind in s.DecorSpots) if (kind != null) n++;
            return n;
        }

        /// <summary>How many of a kind stand in the garden.</summary>
        public static int Placed(GameState s, string kindId)
        {
            int n = 0;
            foreach (string kind in s.DecorSpots) if (kind == kindId) n++;
            return n;
        }

        /// <summary>How many of a kind wait in storage: owned and not standing anywhere.</summary>
        public static int Available(GameState s, string kindId) => Owned(s, kindId) - Placed(s, kindId);

        /// <summary>True while something waits in storage: then the empty spots show their "+". A newcomer sees none.</summary>
        public static bool ShowEmptySpots(GameState s) => TotalOwned(s) > Placed(s);

        /// <summary>Buys one piece of a kind for golden carrots, into storage. False when the kind is unknown, the purse is short, or every spot's worth is owned already.</summary>
        public static bool Buy(GameState s, string kindId)
        {
            int i = IndexOf(kindId);
            if (i < 0 || TotalOwned(s) >= Spots || s.GoldenCarrots < Kinds[i].Price) return false;
            s.GoldenCarrots -= Kinds[i].Price;
            s.DecorOwned[i]++;
            return true;
        }

        static bool SpotOk(GameState s, int spot) => spot >= 0 && spot < s.DecorSpots.Length;

        /// <summary>Stands a piece from storage on an empty spot. False when the spot is taken or out of range, or no piece of that kind waits in storage.</summary>
        public static bool Place(GameState s, int spot, string kindId)
        {
            if (!SpotOk(s, spot) || s.DecorSpots[spot] != null || IndexOf(kindId) < 0 || Available(s, kindId) <= 0) return false;
            s.DecorSpots[spot] = kindId;
            GardenGrid.SettleDecor(s);
            return true;
        }

        /// <summary>Takes what stands on a spot back into storage. False when the spot is empty or out of range.</summary>
        public static bool Store(GameState s, int spot)
        {
            if (!SpotOk(s, spot) || s.DecorSpots[spot] == null) return false;
            s.DecorSpots[spot] = null;
            GardenGrid.SettleDecor(s);
            return true;
        }

        /// <summary>Puts away what stands on a spot and stands another kind there instead; on an empty spot it is a placing. False when nothing changes: the same kind, none of the other in storage.</summary>
        public static bool Swap(GameState s, int spot, string kindId)
        {
            if (!SpotOk(s, spot) || s.DecorSpots[spot] == kindId || IndexOf(kindId) < 0 || Available(s, kindId) <= 0) return false;
            s.DecorSpots[spot] = kindId;
            GardenGrid.SettleDecor(s);
            return true;
        }

        /// <summary>Hangs a colour on every paper lantern for <see cref="LanternColorCost"/>. False when the colour is unknown, already hung, or the purse is short.</summary>
        public static bool SetLanternColor(GameState s, int color)
        {
            if (color < 0 || color >= LanternColors.Length || s.LanternColor == color || s.GoldenCarrots < LanternColorCost) return false;
            s.GoldenCarrots -= LanternColorCost;
            s.LanternColor = color;
            return true;
        }
    }
}
