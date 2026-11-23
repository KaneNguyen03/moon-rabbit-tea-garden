using System.Collections.Generic;

namespace VuonTra.Core
{
    public static class Photos
    {
        public const int GiftGolden = 1;
        public const int AlbumSize = 12;
        public const int GoldTier = 4;

        public static bool Take(GameState s, long day) => Take(s, day, null);

        public static bool Take(GameState s, long day, string path)
        {
            s.PhotosTaken++;
            if (HasGoldRabbit(s)) s.PhotosWithGoldRabbit++;
            if (Seasons.HasSeasonRabbit(s, Seasons.SeasonOf(day))) s.PhotosWithSeasonRabbit++;
            if (s.RestSpots >= RestSpots.Max) s.PhotosWithFullRest++;
            if (!string.IsNullOrEmpty(path))
            {
                s.Album.Remove(path);
                s.Album.Add(path);
                while (s.Album.Count > AlbumSize) s.Album.RemoveAt(0);
            }
            if (day <= s.PhotoDay) return false;
            s.PhotoDay = day;
            s.GoldenCarrots += GiftGolden;
            return true;
        }

        public static bool GiftWaiting(GameState s, long day) => day > s.PhotoDay;

        public static bool HasGoldRabbit(GameState s)
        {
            foreach (Rabbit r in s.Rabbits)
                if (r.Tier >= GoldTier) return true;
            return false;
        }

        public static int Tidy(GameState s, System.Func<string, bool> exists)
        {
            int dropped = 0;
            for (int i = s.Album.Count - 1; i >= 0; i--)
                if (!exists(s.Album[i])) { s.Album.RemoveAt(i); dropped++; }
            return dropped;
        }
    }
}
