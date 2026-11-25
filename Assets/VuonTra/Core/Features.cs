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
    }
}
