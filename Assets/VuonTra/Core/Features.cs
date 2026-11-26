namespace VuonTra.Core
{
    public static class Features
    {
#if VUONTRA_MONETIZATION
        public const bool MonetizationByDefault = true;
#else
        public const bool MonetizationByDefault = false;
#endif

        public static bool Monetization { get; set; } = MonetizationByDefault;

        public const bool Garden25DByDefault = false;

        public static bool Garden25D { get; set; } = Garden25DByDefault;
    }
}
