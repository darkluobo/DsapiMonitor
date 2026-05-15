using System.Text.Json.Serialization;

namespace DsapiMonitor.Models;

/// <summary>
/// DeepSeek /user/balance 响应
/// </summary>
internal sealed class DeepSeekBalanceResponse
{
    [JsonPropertyName("is_available")]
    public bool IsAvailable { get; set; }

    [JsonPropertyName("balance_infos")]
    public List<BalanceInfo>? BalanceInfos { get; set; }
}

internal sealed class BalanceInfo
{
    [JsonPropertyName("currency")]
    public string Currency { get; set; } = "CNY";

    [JsonPropertyName("total_balance")]
    public string TotalBalance { get; set; } = "0";

    [JsonPropertyName("granted_balance")]
    public string GrantedBalance { get; set; } = "0";

    [JsonPropertyName("topped_up_balance")]
    public string ToppedUpBalance { get; set; } = "0";
}
