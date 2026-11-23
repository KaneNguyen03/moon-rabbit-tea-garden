namespace VuonTra.Core
{
    public sealed class DecorKind
    {
        public string Id;
        public int Price;
        public string Art;
        public DecorKind(string id, int price, string art) { Id = id; Price = price; Art = art; }
    }

    public static class Decor
    {
        public const int Spots = 8;
        public const int LanternColorCost = 10;

        public static readonly DecorKind[] Kinds =
        {
            new DecorKind("den-da", 15, "decor_den-da"),
            new DecorKind("bon-hoa", 20, "decor_bon-hoa"),
            new DecorKind("ghe-nam", 30, "decor_ghe-nam"),
            new DecorKind("tham-picnic", 40, "decor_tham-picnic"),
        };

        public static readonly string[] LanternColorIds = { "giay", "hong", "bac-ha", "oai-huong" };
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

        public static int Placed(GameState s)
        {
            int n = 0;
            foreach (string kind in s.DecorSpots) if (kind != null) n++;
            return n;
        }

        public static int Placed(GameState s, string kindId)
        {
            int n = 0;
            foreach (string kind in s.DecorSpots) if (kind == kindId) n++;
            return n;
        }

        public static int Available(GameState s, string kindId) => Owned(s, kindId) - Placed(s, kindId);

        public static bool ShowEmptySpots(GameState s) => TotalOwned(s) > Placed(s);

        public static bool Buy(GameState s, string kindId)
        {
            int i = IndexOf(kindId);
            if (i < 0 || TotalOwned(s) >= Spots || s.GoldenCarrots < Kinds[i].Price) return false;
            s.GoldenCarrots -= Kinds[i].Price;
            s.DecorOwned[i]++;
            return true;
        }

        static bool SpotOk(GameState s, int spot) => spot >= 0 && spot < s.DecorSpots.Length;

        public static bool Place(GameState s, int spot, string kindId)
        {
            if (!SpotOk(s, spot) || s.DecorSpots[spot] != null || IndexOf(kindId) < 0 || Available(s, kindId) <= 0) return false;
            s.DecorSpots[spot] = kindId;
            GardenGrid.SettleDecor(s);
            return true;
        }

        public static bool Store(GameState s, int spot)
        {
            if (!SpotOk(s, spot) || s.DecorSpots[spot] == null) return false;
            s.DecorSpots[spot] = null;
            GardenGrid.SettleDecor(s);
            return true;
        }

        public static bool Swap(GameState s, int spot, string kindId)
        {
            if (!SpotOk(s, spot) || s.DecorSpots[spot] == kindId || IndexOf(kindId) < 0 || Available(s, kindId) <= 0) return false;
            s.DecorSpots[spot] = kindId;
            GardenGrid.SettleDecor(s);
            return true;
        }

        public static bool SetLanternColor(GameState s, int color)
        {
            if (color < 0 || color >= LanternColors.Length || s.LanternColor == color || s.GoldenCarrots < LanternColorCost) return false;
            s.GoldenCarrots -= LanternColorCost;
            s.LanternColor = color;
            return true;
        }
    }
}
