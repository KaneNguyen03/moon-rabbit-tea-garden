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
        AwayIncome,
        /// <summary>Nothing at all: the item is for looks (#44: the hats the shop sells).</summary>
        None
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
        /// <summary>What the shop asks in golden carrots; 0 when it is not for sale (it comes from quests).</summary>
        public int Price;
        /// <summary>The tier a bought copy has. Quest items roll their own.</summary>
        public int Tier;
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

        /// <summary>True when the player has at least one item of this kind, worn or not.</summary>
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

        /// <summary>
        /// False when the rabbit's kind already carries something in that place: antlers, horns, a flame or its own
        /// hat on the head (<see cref="RabbitKind.NoHat"/>), a scarf, collar or pendant at the neck (<see cref="RabbitKind.NoCharm"/>).
        /// </summary>
        public static bool CanWear(Rabbit r, ItemSlot slot)
        {
            if (r == null) return false;
            RabbitKind kind = RabbitKinds.Find(Rabbits.KindOf(r));
            if (kind == null) return true;
            return slot == ItemSlot.Hat ? !kind.NoHat : !kind.NoCharm;
        }

        /// <summary>Puts an item on a rabbit. Whatever was in that slot comes off; the item leaves any previous wearer. False when the rabbit cannot wear it (<see cref="CanWear"/>).</summary>
        public static bool Equip(GameState s, long itemId, long rabbitId)
        {
            Item item = Find(s, itemId);
            Rabbit rabbit = Rabbits.Find(s, rabbitId);
            if (item == null || !CanWear(rabbit, item.Slot)) return false;
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
