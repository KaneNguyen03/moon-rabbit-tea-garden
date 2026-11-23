using System.Collections.Generic;

namespace VuonTra.Core
{
    /// <summary>What a drop in the arranging mode did.</summary>
    public enum LayoutDrop
    {
        /// <summary>Nothing changed: the thing goes back to its tile.</summary>
        Back,
        /// <summary>Stood on an empty tile.</summary>
        Placed,
        /// <summary>Exchanged tiles with what stood there.</summary>
        Swapped
    }

    /// <summary>
    /// Arranging the garden (#54): a station, a place to rest or a decoration is picked up and stood on an empty tile, or
    /// swapped with what stands there. An open station can be put away in storage, where it keeps its level but earns nothing
    /// and its rabbit goes to rest; it comes back out on the empty tile nearest the centre. A decoration put away goes back to
    /// the decorations' own storage (#11). Where things stand changes nothing else.
    /// </summary>
    public static class Layout
    {
        /// <summary>True when station <paramref name="index"/> is in storage. Cheap when nothing is, and allocates nothing either way (#97).</summary>
        public static bool IsStored(IReadOnlyList<StationDef> defs, GameState s, int index)
        {
            GardenGrid g = s.Grid;
            return g != null && g.StoredCount > 0 && defs != null && index >= 0 && index < defs.Count && g.IsStored(defs[index].GridId);
        }

        /// <summary>A drop from one tile on another: a move onto an empty lawn tile, a swap with something movable, otherwise back (a tile kept for a prop included).</summary>
        public static LayoutDrop Drop(IReadOnlyList<StationDef> defs, GameState s, Hex from, Hex to)
        {
            GardenGrid g = s.Grid;
            if (g == null || from == to || GardenGrid.IsReserved(to)) return LayoutDrop.Back;
            if (g.OccupantAt(to.Q, to.R) == null) return g.Move(from, to) ? LayoutDrop.Placed : LayoutDrop.Back;
            return g.Swap(from, to) ? LayoutDrop.Swapped : LayoutDrop.Back;
        }

        /// <summary>
        /// Puts something away: an open station (not the teapot, the heart of the garden) into storage, its rabbit to rest;
        /// a decoration back into the decorations' storage. False for anything else.
        /// </summary>
        public static bool Store(IReadOnlyList<StationDef> defs, GameState s, string id)
        {
            if (s.Grid == null || id == null) return false;
            if (id.StartsWith("decor:"))
                return int.TryParse(id.Substring(6), out int spot) && Decor.Store(s, spot);
            int index = IndexOf(defs, id);
            if (index <= 0 || s.Levels[index] <= 0 || !s.Grid.Store(id)) return false;
            foreach (Rabbit r in s.Rabbits) if (r.Station == index) r.Station = -1;
            return true;
        }

        /// <summary>Takes station <paramref name="index"/> out of storage onto the empty tile nearest the centre. False when it is not stored or the lawn is full.</summary>
        public static bool TakeOut(IReadOnlyList<StationDef> defs, GameState s, int index)
        {
            if (s.Grid == null || defs == null || index < 0 || index >= defs.Count) return false;
            return s.Grid.TakeOut(GardenGrid.StationId(defs[index]));
        }

        /// <summary>The station an id names, or -1.</summary>
        public static int IndexOf(IReadOnlyList<StationDef> defs, string id)
        {
            if (defs == null || id == null || !id.StartsWith("station:")) return -1;
            string stationId = id.Substring(8);
            for (int i = 0; i < defs.Count; i++) if (defs[i].Id == stationId) return i;
            return -1;
        }
    }
}
