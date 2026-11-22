using System.Collections.Generic;

namespace VuonTra.Core
{
    /// <summary>Taking a picture of the garden: the first one each day brings a small gift, and the album keeps the latest.</summary>
    public static class Photos
    {
        /// <summary>The first photo of the day brings this many golden carrots.</summary>
        public const int GiftGolden = 1;
        /// <summary>How many pictures the album remembers; older ones fall off, the files stay where they were saved.</summary>
        public const int AlbumSize = 12;
        /// <summary>A rabbit of this tier or above in the garden makes a photo count as "with a gold rabbit".</summary>
        public const int GoldTier = 4;

        /// <summary>Records a photo taken on <paramref name="day"/>. True, and the gift given, when it is the day's first.</summary>
        public static bool Take(GameState s, long day) => Take(s, day, null);

        /// <summary>
        /// Records a photo: the day's gift, the counters the quests read, and the picture's place in the album
        /// when it was written somewhere (<paramref name="path"/> null when it was not).
        /// </summary>
        public static bool Take(GameState s, long day, string path)
        {
            s.PhotosTaken++;
            if (HasGoldRabbit(s)) s.PhotosWithGoldRabbit++;
            if (s.RestSpots >= RestSpots.Max) s.PhotosWithFullRest++;
            if (!string.IsNullOrEmpty(path))
            {
                s.Album.Remove(path);
                s.Album.Add(path);
                while (s.Album.Count > AlbumSize) s.Album.RemoveAt(0);
            }
            if (s.PhotoDay == day) return false;
            s.PhotoDay = day;
            s.GoldenCarrots += GiftGolden;
            return true;
        }

        /// <summary>True while today's gift is still to be had.</summary>
        public static bool GiftWaiting(GameState s, long day) => s.PhotoDay != day;

        public static bool HasGoldRabbit(GameState s)
        {
            foreach (Rabbit r in s.Rabbits)
                if (r.Tier >= GoldTier) return true;
            return false;
        }

        /// <summary>Drops album entries that <paramref name="exists"/> says are gone. How many were dropped.</summary>
        public static int Tidy(GameState s, System.Func<string, bool> exists)
        {
            int dropped = 0;
            for (int i = s.Album.Count - 1; i >= 0; i--)
                if (!exists(s.Album[i])) { s.Album.RemoveAt(i); dropped++; }
            return dropped;
        }
    }
}
