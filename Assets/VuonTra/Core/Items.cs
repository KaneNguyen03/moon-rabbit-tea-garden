using System;
using System.Collections.Generic;

namespace VuonTra.Core
{
    public enum ItemSlot { Hat, Charm }

    public enum ItemEffect
    {
        /// <summary>Adds to the wearer's own station multiplier (0.1 = +10%).</summary>
        StationIncome,
        /// <summary>Speeds up the whole garden while the wearer is working.</summary>
        GardenSpeed,
        /// <summary>Raises what the garden earns while the player is away, while the wearer is working.</summary>
        AwayIncome
    }

    /// <summary>A kind of item, as designed.</summary>
    public sealed class ItemDef
    {
        public string Id;
        public string Name;
        public ItemSlot Slot;
        public ItemEffect Effect;
        /// <summary>The bonus at the lowest tier.</summary>
        public double BaseAmount;
    }

    /// <summary>One item the player owns. It carries its own numbers, so it survives content changes.</summary>
    public sealed class Item
    {
        public long Id;
        public string DefId;
        public string Name;
        public ItemSlot Slot;
        public ItemEffect Effect;
        public int Tier;
        public double Amount;
        /// <summary>Id of the rabbit wearing it, or 0.</summary>
        public long WornBy;
    }

    public static class Items
    {
        /// <summary>How much stronger each tier is than the lowest.</summary>
        public static readonly double[] TierScale = { 1, 1.5, 2, 3, 4.5, 7 };

        public static Item Grant(GameState s, ItemDef def, int tier)
        {
            tier = Math.Max(0, Math.Min(Rabbits.TierCount - 1, tier));
            var item = new Item
            {
                Id = ++s.NextItemId, DefId = def.Id, Name = def.Name, Slot = def.Slot, Effect = def.Effect,
                Tier = tier, Amount = def.BaseAmount * TierScale[tier]
            };
            s.Items.Add(item);
            return item;
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

        /// <summary>Puts an item on a rabbit. Whatever was in that slot comes off; the item leaves any previous wearer.</summary>
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

        /// <summary>Takes everything off a rabbit (it is leaving the collection).</summary>
        public static void Strip(GameState s, long rabbitId)
        {
            for (int i = 0; i < s.Items.Count; i++)
                if (s.Items[i].WornBy == rabbitId) s.Items[i].WornBy = 0;
        }

        /// <summary>Extra share of the wearer's station multiplier from its own items.</summary>
        public static double StationBonus(GameState s, long rabbitId)
        {
            double sum = 0d;
            for (int i = 0; i < s.Items.Count; i++)
                if (s.Items[i].WornBy == rabbitId && s.Items[i].Effect == ItemEffect.StationIncome) sum += s.Items[i].Amount;
            return sum;
        }

        /// <summary>Sum of one garden-wide effect over items worn by rabbits that are at work.</summary>
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
