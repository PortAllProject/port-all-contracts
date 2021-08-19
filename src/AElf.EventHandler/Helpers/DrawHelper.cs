using System;
using AElf.Contracts.Lottery;
using AElf.Kernel;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.EventHandler
{
    public class DrawHelper
    {
        private static int _drewPeriod;

        public static void TryToDrawLottery(string url, LotteryOptions lotteryOptions)
        {
            var nodeManager = new NodeManager(url, lotteryOptions.AccountAddress, lotteryOptions.AccountPassword);
            CalculateDrawInfo(Timestamp.FromDateTime(DateTime.Parse(lotteryOptions.StartTimestamp)),
                lotteryOptions.IntervalMinutes, out var isTimeToDraw, out var period);
            if (isTimeToDraw && lotteryOptions.IsDrawLottery)
            {
                var periodAwardBytes = nodeManager.QueryView(lotteryOptions.AccountAddress,
                    lotteryOptions.LotteryContractAddress, "GetPeriodAward", new Int64Value
                    {
                        Value = period
                    });
                var periodAward = new PeriodAward();
                periodAward.MergeFrom(periodAwardBytes);
                nodeManager.SendTransaction(lotteryOptions.AccountAddress, lotteryOptions.LotteryContractAddress,
                    "Draw", new DrawInput
                    {
                        PeriodId = period,
                        ToAwardId = periodAward.StartAwardId + 60
                    });
                nodeManager.SendTransaction(lotteryOptions.AccountAddress, lotteryOptions.LotteryContractAddress,
                    "Draw", new DrawInput
                    {
                        PeriodId = period
                    });
            }
        }

        private static void CalculateDrawInfo(Timestamp startTimestamp, long intervalMinutes, out bool isDraw, out int period)
        {
            var duration = TimestampHelper.GetUtcNow() - startTimestamp;
            period = (int)(duration.Seconds / 60 / intervalMinutes);
            if (_drewPeriod >= period)
            {
                period = 0;
                isDraw = false;
                return;
            }

            _drewPeriod = period;
            isDraw = true;
        }
    }
}