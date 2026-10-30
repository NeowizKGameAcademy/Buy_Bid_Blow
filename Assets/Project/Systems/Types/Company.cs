namespace BuyBidBlow.Systems.Types
{
    public enum Company
    {
        Straw,
        Timber,
        Brick
    }

    public static class CompanyNames
    {
        // Keep the existing serialized company identity; only its player-facing name changes.
        public static string DisplayName(this Company company) => company == Company.Timber ? "Wood" : company.ToString();
    }
}
