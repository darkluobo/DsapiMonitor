using DsapiMonitor.Models;

namespace DsapiMonitor.Services;

public interface IDeepSeekUsageProvider
{
    /// <summary>
    /// 查询账户余额
    /// </summary>
    Task<AccountBalance> GetBalanceAsync(string apiKey, CancellationToken ct = default);

    /// <summary>
    /// 探测 API 延迟（发送一个轻量请求）
    /// </summary>
    Task<int> ProbeLatencyAsync(string apiKey, CancellationToken ct = default);
}
