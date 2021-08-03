using System.Text.Json.Serialization;

namespace AElf.TokenSwap.Dtos
{
    public class ResponseDto
    {
        [JsonPropertyName("msg")] public string Message { get; set; }
    }
}