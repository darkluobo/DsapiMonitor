using DsapiMonitor.Models;

namespace DsapiMonitor.Services;

public sealed class LatencyProbeService
{
    private readonly LatencyHistory _history = new(maxSize: 60);

    public LatencyHistory History => _history;

    public void RecordLatency(int latencyMs)
    {
        var record = latencyMs >= 0
            ? new LatencyRecord(DateTimeOffset.Now, latencyMs, true)
            : LatencyRecord.Failed(DateTimeOffset.Now);
        _history.Add(record);
    }
}
