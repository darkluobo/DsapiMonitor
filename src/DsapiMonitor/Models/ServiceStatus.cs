namespace DsapiMonitor.Models;

/// <summary>
/// DeepSeek 账户余额信息
/// </summary>
public sealed record AccountBalance(
    bool IsAvailable,
    decimal Balance,
    string Currency,
    DateTimeOffset LastUpdated)
{
    public string BalanceDisplay => $"{Currency} {Balance:N2}";
}

/// <summary>
/// API 调用记录（本地统计）
/// </summary>
public sealed record ApiCallRecord(
    DateTimeOffset Timestamp,
    string Model,
    int PromptTokens,
    int CompletionTokens,
    double CostUsd,
    int LatencyMs);

/// <summary>
/// 本地 API 调用统计汇总
/// </summary>
public sealed class ApiUsageStats
{
    public int TotalCalls { get; set; }
    public int TotalPromptTokens { get; set; }
    public int TotalCompletionTokens { get; set; }
    public decimal TotalCostUsd { get; set; }
    public int TodayCalls { get; set; }
    public decimal TodayCostUsd { get; set; }
}
