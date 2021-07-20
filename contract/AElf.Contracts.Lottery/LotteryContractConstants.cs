namespace AElf.Contracts.Lottery
{
    public partial class LotteryContract
    {
        private const long DefaultPrice = 1;
        private const long DefaultDrawingLag = 80;
        private const long MaximumBuyAmount = 100;
        private const int MaximumReturnAmount = 100;

        private const long TotalSharesForProfitRate = 10000;

        private const long LotteryBoughtCountLimitInOnePeriod = 1000;
        private const long DefaultTotalSharesForDividendRate = 10000;
    }
}