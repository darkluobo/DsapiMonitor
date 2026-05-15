using System.Text.Json.Serialization;

namespace DsapiMonitor.Models;

public sealed class WebUsageData
{
    [JsonPropertyName("totalCost")]
    public string TotalCost { get; set; } = "";

    [JsonPropertyName("models")]
    public List<WebModelStat> Models { get; set; } = [];

    [JsonPropertyName("raw")]
    public string Raw { get; set; } = "";
}

public sealed class WebModelStat
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("apiCalls")]
    public string ApiCalls { get; set; } = "";

    [JsonPropertyName("tokens")]
    public string Tokens { get; set; } = "";
}
