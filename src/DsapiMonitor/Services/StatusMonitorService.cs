using DsapiMonitor.Models;

namespace DsapiMonitor.Services;

public sealed class DeepSeekMonitorService : IDisposable
{
    private readonly IDeepSeekUsageProvider _usageProvider;
    private readonly LatencyProbeService _latencyTracker;
    private readonly CancellationTokenSource _cts = new();

    private Task? _monitorLoop;
    private string _apiKey = "";

    public event Action<AccountBalance>? BalanceUpdated;
    public event Action<int>? LatencyUpdated;
    public event Action<Exception>? ErrorOccurred;

    public DeepSeekMonitorService(IDeepSeekUsageProvider usageProvider, LatencyProbeService latencyTracker)
    {
        _usageProvider = usageProvider;
        _latencyTracker = latencyTracker;
    }

    public void SetApiKey(string apiKey) => _apiKey = apiKey;

    public void Start()
    {
        _monitorLoop = RunMonitorLoop(_cts.Token);
    }

    public void Restart()
    {
        _cts.Cancel();
        // 重新启动（简单实现，不影响现有逻辑）
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
    }

    private async Task RunMonitorLoop(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            if (!string.IsNullOrWhiteSpace(_apiKey))
            {
                await FetchAndUpdate(ct);
            }

            try { await Task.Delay(TimeSpan.FromSeconds(30), ct); }
            catch (OperationCanceledException) { break; }
        }
    }

    private async Task FetchAndUpdate(CancellationToken ct)
    {
        try
        {
            // 查询余额
            var balance = await _usageProvider.GetBalanceAsync(_apiKey, ct);
            BalanceUpdated?.Invoke(balance);

            // 探测延迟
            var latency = await _usageProvider.ProbeLatencyAsync(_apiKey, ct);
            _latencyTracker.RecordLatency(latency);
            LatencyUpdated?.Invoke(latency);
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(ex);
        }
    }
}
