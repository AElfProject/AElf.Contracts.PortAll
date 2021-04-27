namespace AElf.Contracts.Oracle
{
    public partial class OracleContract
    {
        private const string TokenSymbol = "AEL";
        private const string TokenName = "AelfLinK";
        private const long TotalSupply = 100_000_000_00000000;

        private const int DefaultExpirationSeconds = 3600;

        private const int DefaultRevealThreshold = 2;

        private const int DefaultAggregateThreshold = 1;

        private const int DefaultMinimumOracleNodesCount = 3;
    }
}