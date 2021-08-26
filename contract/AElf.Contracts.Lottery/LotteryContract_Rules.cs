using System.Collections.Generic;

namespace AElf.Contracts.Lottery
{
    public partial class LotteryContract
    {
        private const long AmountOfElfToGetFirstLotteryCode = 100_0000_0000;
        private const long AmountOfElfToGetMoreLotteryCode = 1000_0000_0000;
        private const int MaximumLotteryCodeAmountForSingleAddress = 21;
        private const string TokenSymbol = "ELF";

        private const int TotalPeriod = 7;

        private List<long> GetDefaultAwardList()
        {
            var awardList = new List<long>
            {
                10000_00000000,
            };

            for (var i = 0; i < 5; i++)
            {
                awardList.Add(1000_00000000);
            }

            for (var i = 0; i < 20; i++)
            {
                awardList.Add(100_00000000);
            }

            return awardList;
        }
    }
}