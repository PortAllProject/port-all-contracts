using System.Text.Json.Serialization;

namespace AElf.TokenSwap.Dtos
{
    public class ResponseDto
    {
        [JsonPropertyName("code")] public string Code { get; set; }

        [JsonPropertyName("msg")] public string Message { get; set; }
    }
}