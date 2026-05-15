namespace DsapiMonitor.Models;

public sealed record LatencyRecord(
    DateTimeOffset Timestamp,
    double Milliseconds,
    bool IsSuccessful)
{
    public static LatencyRecord Failed(DateTimeOffset timestamp) =>
        new(timestamp, 0, false);
}

public sealed class LatencyHistory
{
    private readonly Queue<LatencyRecord> _records = new();
    private readonly int _maxSize;

    public LatencyHistory(int maxSize = 60) => _maxSize = maxSize;

    public IReadOnlyList<LatencyRecord> Records => _records.ToArray();

    public void Add(LatencyRecord record)
    {
        _records.Enqueue(record);
        while (_records.Count > _maxSize)
            _records.Dequeue();
    }

    public double AverageMs => _records.Count == 0 ? 0
        : _records.Where(r => r.IsSuccessful).Average(r => r.Milliseconds);

    public double? LastMs => _records.Count == 0 ? null
        : _records.Last().IsSuccessful ? _records.Last().Milliseconds : null;

    public bool LastSucceeded => _records.Count > 0 && _records.Last().IsSuccessful;
}
