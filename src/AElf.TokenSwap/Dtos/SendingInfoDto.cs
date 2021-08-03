using System.Text.Json.Serialization;

namespace AElf.TokenSwap.Dtos
{
    public class SendingInfoDto
    {
        [JsonPropertyName("receipt_id")] public long ReceiptId { get; set; }
        [JsonPropertyName("sending_tx_id")] public string SendingTxId { get; set; }
        [JsonPropertyName("sending_time")] public string SendingTime { get; set; }
    }
}