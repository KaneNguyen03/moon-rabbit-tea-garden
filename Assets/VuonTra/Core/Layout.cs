using System.Collections.Generic;

namespace VuonTra.Core
{
    public enum LayoutDrop
    {
        Back,
        Placed,
        Swapped
    }

    public static class Layout
    {
        public static bool IsStored(IReadOnlyList<StationDef> defs, GameState s, int index)
        {
            GardenGrid g = s.Grid;
            return g != null && g.StoredCount > 0 && defs != null && index >= 0 && index < defs.Count && g.IsStored(defs[index].GridId);
        }

        public static LayoutDrop Drop(IReadOnlyList<StationDef> defs, GameState s, Hex from, Hex to)
        {
            GardenGrid g = s.Grid;
            if (g == null || from == to || GardenGrid.IsReserved(to)) return LayoutDrop.Back;
            if (g.OccupantAt(to.Q, to.R) == null) return g.Move(from, to) ? LayoutDrop.Placed : LayoutDrop.Back;
            return g.Swap(from, to) ? LayoutDrop.Swapped : LayoutDrop.Back;
        }

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

        public static bool TakeOut(IReadOnlyList<StationDef> defs, GameState s, int index)
        {
            if (s.Grid == null || defs == null || index < 0 || index >= defs.Count) return false;
            return s.Grid.TakeOut(GardenGrid.StationId(defs[index]));
        }

        public static int IndexOf(IReadOnlyList<StationDef> defs, string id)
        {
            if (defs == null || id == null || !id.StartsWith("station:")) return -1;
            string stationId = id.Substring(8);
            for (int i = 0; i < defs.Count; i++) if (defs[i].Id == stationId) return i;
            return -1;
        }
    }
}
