using System.Collections.Generic;

namespace AElf.Contracts.Lottery
{
    public partial class LotteryContract
    {
        private const long AmountOfElfToGetFirstLotteryCode = 100_0000_0000;
        private const long AmountOfElfToGetMoreLotteryCode = 1000_0000_0000;
        private const int MaximumLotteryCodeAmountForSingleAddress = 21;
        private const string TokenSymbol = "ELF";

        private const int TotalPeriod = 8;

        private List<long> GetDefaultAwardList()
        {
            var awardList = new List<long>
            {
                5000,
                1000, 1000,
                500, 500
            };

            for (var i = 0; i < 5; i++)
            {
                awardList.Add(100);
            }

            for (var i = 0; i < 10; i++)
            {
                awardList.Add(50);
            }

            for (var i = 0; i < 100; i++)
            {
                awardList.Add(10);
            }

            return awardList;
        }
    }
}