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

        public static bool TryToDrawLottery(string url, LotteryOptions lotteryOptions, out int period)
        {
            _drewPeriod = lotteryOptions.LatestDrewPeriod == 0 ? _drewPeriod : lotteryOptions.LatestDrewPeriod;

            var nodeManager = new NodeManager(url, lotteryOptions.AccountAddress, lotteryOptions.AccountPassword);
            CalculateDrawInfo(Timestamp.FromDateTime(DateTime.Parse(lotteryOptions.StartTimestamp)),
                lotteryOptions.IntervalMinutes, out var isDraw, out period);
            if (!isDraw || !lotteryOptions.IsDrawLottery) return true;

            nodeManager.SendTransaction(lotteryOptions.AccountAddress, lotteryOptions.LotteryContractAddress,
                "Draw", new DrawInput
                {
                    PeriodId = period
                });

            return true;
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