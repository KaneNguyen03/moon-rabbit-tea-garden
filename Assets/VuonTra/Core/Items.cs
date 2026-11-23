using System;
using System.Collections.Generic;

namespace VuonTra.Core
{
    public enum ItemSlot { Hat, Charm }

    public enum ItemEffect
    {
        StationIncome,
        GardenSpeed,
        AwayIncome,
        None
    }

    public sealed class ItemDef
    {
        public string Id;
        public string Name;
        public ItemSlot Slot;
        public ItemEffect Effect;
        public double BaseAmount;
        public int Price;
        public int Tier;
        public float HatLift;
    }

    public sealed class Item
    {
        public long Id;
        public string DefId;
        public string Name;
        public ItemSlot Slot;
        public ItemEffect Effect;
        public int Tier;
        public double Amount;
        public long WornBy;
    }

    public static class Items
    {
        public static readonly double[] TierScale = { 1, 1.5, 2, 3, 4.5, 7 };

        public static Item Grant(GameState s, ItemDef def, int tier)
        {
            tier = Math.Max(0, Math.Min(Rabbits.TierCount - 1, tier));
            var item = new Item
            {
                Id = ++s.NextItemId, DefId = def.Id, Name = def.Name, Slot = def.Slot, Effect = def.Effect,
                Tier = tier, Amount = def.Effect == ItemEffect.None ? 0d : def.BaseAmount * TierScale[tier]
            };
            s.Items.Add(item);
            return item;
        }

        public static ItemDef DefOf(IReadOnlyList<ItemDef> defs, string defId)
        {
            if (defs == null || defId == null) return null;
            for (int i = 0; i < defs.Count; i++)
                if (defs[i].Id == defId) return defs[i];
            return null;
        }

        public static bool Owns(GameState s, string defId)
        {
            for (int i = 0; i < s.Items.Count; i++)
                if (s.Items[i].DefId == defId) return true;
            return false;
        }

        public static Item Find(GameState s, long id)
        {
            for (int i = 0; i < s.Items.Count; i++)
                if (s.Items[i].Id == id) return s.Items[i];
            return null;
        }

        public static Item Worn(GameState s, long rabbitId, ItemSlot slot)
        {
            if (rabbitId <= 0) return null;
            for (int i = 0; i < s.Items.Count; i++)
                if (s.Items[i].WornBy == rabbitId && s.Items[i].Slot == slot) return s.Items[i];
            return null;
        }

        public static bool Equip(GameState s, long itemId, long rabbitId)
        {
            Item item = Find(s, itemId);
            if (item == null || Rabbits.Find(s, rabbitId) == null) return false;
            Item current = Worn(s, rabbitId, item.Slot);
            if (current != null && current != item) current.WornBy = 0;
            item.WornBy = rabbitId;
            return true;
        }

        public static bool Unequip(GameState s, long itemId)
        {
            Item item = Find(s, itemId);
            if (item == null) return false;
            item.WornBy = 0;
            return true;
        }

        public static void Strip(GameState s, long rabbitId)
        {
            for (int i = 0; i < s.Items.Count; i++)
                if (s.Items[i].WornBy == rabbitId) s.Items[i].WornBy = 0;
        }

        public static double StationBonus(GameState s, long rabbitId)
        {
            double sum = 0d;
            for (int i = 0; i < s.Items.Count; i++)
                if (s.Items[i].WornBy == rabbitId && s.Items[i].Effect == ItemEffect.StationIncome) sum += s.Items[i].Amount;
            return sum;
        }

        public static double GardenBonus(GameState s, ItemEffect effect)
        {
            double sum = 0d;
            for (int i = 0; i < s.Items.Count; i++)
            {
                Item item = s.Items[i];
                if (item.WornBy <= 0 || item.Effect != effect) continue;
                Rabbit wearer = Rabbits.Find(s, item.WornBy);
                if (wearer != null && wearer.Station >= 0) sum += item.Amount;
            }
            return sum;
        }
    }
}
