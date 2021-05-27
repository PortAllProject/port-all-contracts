namespace AElf.EventHandler
{
    public class MessageQueueOptions
    {
        public string HostName { get; set; } = "127.0.0.1";
        public int Port { get; set; } = 5672;
        public string ClientName { get; set; } = "AElf";
        public string ExchangeName { get; set; } = "AElfExchange";
        public string UserName { get; set; } = "aelf";
        public string PassWord { get; set; } = "12345678";
        public string Uri { get; set; } = "";
    }
}