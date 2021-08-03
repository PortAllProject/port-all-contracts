using System.Text.Json.Serialization;

namespace AElf.TokenSwap.Dtos
{
    public class ReceiptInfoDto
    {
        [JsonPropertyName("receipt_id")] public long ReceiptId { get; set; }

        [JsonPropertyName("sending_tx_id")] public string SendingTxId { get; set; }

        [JsonPropertyName("sending_time")] public string SendingTime { get; set; }

        [JsonPropertyName("receiving_tx_id")] public string ReceivingTxId { get; set; }

        [JsonPropertyName("receiving_time")] public string ReceivingTime { get; set; }

        [JsonPropertyName("amount")] public long Amount { get; set; }

        [JsonPropertyName("receiving_address")]
        public string ReceivingAddress { get; set; }
    }
}