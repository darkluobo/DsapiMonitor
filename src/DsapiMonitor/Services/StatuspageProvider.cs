using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using DsapiMonitor.Models;

namespace DsapiMonitor.Services;

public sealed class DeepSeekUsageProvider : IDeepSeekUsageProvider
{
    private readonly HttpClient _http;

    public DeepSeekUsageProvider(HttpClient http) => _http = http;

    public async Task<AccountBalance> GetBalanceAsync(string apiKey, CancellationToken ct = default)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, "user/balance");
        req.Headers.Authorization = new("Bearer", apiKey);

        using var resp = await _http.SendAsync(req, ct);
        var body = await resp.Content.ReadAsStringAsync(ct);

        if (!resp.IsSuccessStatusCode)
            throw new HttpRequestException(
                $"API 返回 {(int)resp.StatusCode}: {body}", null, resp.StatusCode);

        try
        {
            var data = JsonSerializer.Deserialize<DeepSeekBalanceResponse>(body);
            if (data?.BalanceInfos is null || data.BalanceInfos.Count == 0)
                return new AccountBalance(data?.IsAvailable ?? false, 0, "CNY", DateTimeOffset.Now);

            var info = data.BalanceInfos[0];
            var balance = decimal.TryParse(info.TotalBalance, out var b) ? b : 0m;
            return new AccountBalance(data.IsAvailable, balance, info.Currency, DateTimeOffset.Now);
        }
        catch (JsonException ex)
        {
            throw new JsonException($"JSON 解析失败，原始响应: {body}", ex);
        }
    }

    public async Task<int> ProbeLatencyAsync(string apiKey, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, "user/balance");
            req.Headers.Authorization = new("Bearer", apiKey);
            using var resp = await _http.SendAsync(req, ct);
            sw.Stop();
            return (int)sw.ElapsedMilliseconds;
        }
        catch
        {
            sw.Stop();
            return -1;
        }
    }
}
