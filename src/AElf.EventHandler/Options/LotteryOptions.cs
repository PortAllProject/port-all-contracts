namespace AElf.EventHandler
{
    public class LotteryOptions
    {
        public bool IsDrawLottery { get; set; }
        public string AccountAddress { get; set; }
        public string AccountPassword { get; set; }
        public string LotteryContractAddress { get; set; }
        public string StartTimestamp { get; set; }
        public long IntervalMinutes { get; set; }
    }
}