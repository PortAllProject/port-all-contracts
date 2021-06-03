namespace AElf.Contracts.Oracle
{
    public partial class OracleContract
    {
        private const string TokenSymbol = "PORT";
        private const string TokenName = "Port All Project Token";
        private const long TotalSupply = 100_000_000_00000000;

        private const int DefaultExpirationSeconds = 3600;

        private const int DefaultRevealThreshold = 2;

        private const int DefaultAggregateThreshold = 1;

        private const int DefaultMinimumOracleNodesCount = 3;

        private const string NotSetCallbackInfo = "NotSetCallbackInfo";
    }
}