namespace VuonTra.Core
{
    /// <summary>Taking a picture of the garden: the first one each day brings a small gift.</summary>
    public static class Photos
    {
        public const int GiftClovers = 1;

        /// <summary>Records a photo taken on <paramref name="day"/>. True, and the gift given, when it is the day's first.</summary>
        public static bool Take(GameState s, long day)
        {
            s.PhotosTaken++;
            if (s.PhotoDay == day) return false;
            s.PhotoDay = day;
            s.Clovers += GiftClovers;
            return true;
        }

        /// <summary>True while today's gift is still to be had.</summary>
        public static bool GiftWaiting(GameState s, long day) => s.PhotoDay != day;
    }
}
