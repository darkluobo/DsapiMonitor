using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DsapiMonitor.Services;

/// <summary>
/// 通过 platform.deepseek.com 的 session cookie 查询用量数据
/// </summary>
public sealed class PlatformUsageProvider
{
    private readonly HttpClient _http;
    private string _sessionCookie = "";

    public PlatformUsageProvider(HttpClient http) => _http = http;

    public void SetSessionCookie(string cookie)
    {
        _sessionCookie = cookie;
        _http.DefaultRequestHeaders.Remove("Cookie");
        _http.DefaultRequestHeaders.Add("Cookie", cookie);
    }

    public bool HasSession => !string.IsNullOrWhiteSpace(_sessionCookie);

    /// <summary>
    /// 获取用量数据（会依次尝试多个可能的 API 端点）
    /// </summary>
    public async Task<PlatformUsageResult?> GetUsageAsync(
        DateTimeOffset start, DateTimeOffset end, CancellationToken ct = default)
    {
        var startStr = start.ToString("yyyy-MM-dd");
        var endStr = end.ToString("yyyy-MM-dd");

        string[] endpoints =
        [
            $"/api/usage?start_date={startStr}&end_date={endStr}",
            $"/api/billing/usage?start_date={startStr}&end_date={endStr}",
            $"/api/user/usage?start_date={startStr}&end_date={endStr}",
        ];

        foreach (var path in endpoints)
        {
            try
            {
                using var resp = await _http.GetAsync($"https://platform.deepseek.com{path}", ct);
                if (!resp.IsSuccessStatusCode) continue;

                var body = await resp.Content.ReadAsStringAsync(ct);
                if (string.IsNullOrWhiteSpace(body) || body.StartsWith("<!")) continue;

                var result = TryParseUsage(body);
                if (result != null) return result;
            }
            catch { continue; }
        }

        return null;
    }

    private static PlatformUsageResult? TryParseUsage(string json)
    {
        try
        {
            // 尝试解析通用格式
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var result = new PlatformUsageResult();

            // 尝试提取各种可能的字段
            if (root.TryGetProperty("data", out var data))
            {
                if (data.TryGetProperty("total_tokens", out var tt))
                    result.TotalTokens = tt.GetInt64();
                if (data.TryGetProperty("total_cost", out var tc))
                    result.TotalCost = tc.GetDecimal();
                if (data.TryGetProperty("call_count", out var cc))
                    result.CallCount = cc.GetInt32();
            }

            // 尝试提取日明细
            if (root.TryGetProperty("daily_details", out var daily) ||
                root.TryGetProperty("details", out daily) ||
                root.TryGetProperty("records", out daily))
            {
                if (daily.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in daily.EnumerateArray())
                    {
                        var detail = new DailyUsageDetail();
                        if (item.TryGetProperty("date", out var d))
                            detail.Date = d.GetString() ?? "";
                        if (item.TryGetProperty("tokens", out var t))
                            detail.Tokens = t.GetInt64();
                        if (item.TryGetProperty("cost", out var c))
                            detail.Cost = c.GetDecimal();
                        if (item.TryGetProperty("call_count", out var cnt))
                            detail.CallCount = cnt.GetInt32();
                        if (item.TryGetProperty("model", out var m))
                            detail.Model = m.GetString() ?? "";
                        result.DailyDetails.Add(detail);
                    }
                }
            }

            // 如果没解析到任何数据，记录原始 JSON 用于调试
            if (result.TotalTokens == 0 && result.DailyDetails.Count == 0)
            {
                result.RawJson = json;
                return null;
            }

            return result;
        }
        catch
        {
            return null;
        }
    }
}

public sealed class PlatformUsageResult
{
    public long TotalTokens { get; set; }
    public decimal TotalCost { get; set; }
    public int CallCount { get; set; }
    public List<DailyUsageDetail> DailyDetails { get; set; } = [];
    public string? RawJson { get; set; }
}

public sealed class DailyUsageDetail
{
    public string Date { get; set; } = "";
    public long Tokens { get; set; }
    public decimal Cost { get; set; }
    public int CallCount { get; set; }
    public string Model { get; set; } = "";
}
