using System.Text.Json.Serialization;

namespace ProxySharp.Models;

public class GeneralResponse
{
    [JsonPropertyName("resCode")]
    public int? ResCode { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}

public class EntityDataResponse : GeneralResponse
{
    [JsonPropertyName("EntityInfo")]
    public string EntityInfo { get; set; }

    [JsonPropertyName("Amount")]
    public decimal Amount { get; set; }
}
