using DsapiMonitor.Models;

namespace DsapiMonitor.Tests;

public class LatencyHistoryTests
{
    [Fact]
    public void Add_ShouldAddRecord()
    {
        var history = new LatencyHistory(maxSize: 10);
        var record = new LatencyRecord(DateTimeOffset.Now, 100, true);

        history.Add(record);

        Assert.Single(history.Records);
        Assert.Equal(100, history.AverageMs);
    }

    [Fact]
    public void Add_ShouldRespectMaxSize()
    {
        var history = new LatencyHistory(maxSize: 3);

        history.Add(new LatencyRecord(DateTimeOffset.Now, 100, true));
        history.Add(new LatencyRecord(DateTimeOffset.Now, 200, true));
        history.Add(new LatencyRecord(DateTimeOffset.Now, 300, true));
        history.Add(new LatencyRecord(DateTimeOffset.Now, 400, true));

        Assert.Equal(3, history.Records.Count);
        Assert.Equal(200, history.Records[0].Milliseconds);
    }

    [Fact]
    public void AverageMs_ShouldExcludeFailedRecords()
    {
        var history = new LatencyHistory(maxSize: 10);

        history.Add(new LatencyRecord(DateTimeOffset.Now, 100, true));
        history.Add(LatencyRecord.Failed(DateTimeOffset.Now));
        history.Add(new LatencyRecord(DateTimeOffset.Now, 300, true));

        Assert.Equal(200, history.AverageMs);
    }

    [Fact]
    public void AverageMs_ShouldReturnZero_WhenEmpty()
    {
        var history = new LatencyHistory(maxSize: 10);
        Assert.Equal(0, history.AverageMs);
    }

    [Fact]
    public void LastMs_ShouldReturnNull_WhenEmpty()
    {
        var history = new LatencyHistory(maxSize: 10);
        Assert.Null(history.LastMs);
    }

    [Fact]
    public void LastSucceeded_ShouldReturnFalse_WhenLastFailed()
    {
        var history = new LatencyHistory(maxSize: 10);
        history.Add(new LatencyRecord(DateTimeOffset.Now, 100, true));
        history.Add(LatencyRecord.Failed(DateTimeOffset.Now));

        Assert.False(history.LastSucceeded);
    }
}

public class AccountBalanceTests
{
    [Fact]
    public void BalanceDisplay_ShouldFormatCorrectly()
    {
        var balance = new AccountBalance(true, 123.45m, "CNY", DateTimeOffset.Now);
        Assert.Equal("CNY 123.45", balance.BalanceDisplay);
    }

    [Fact]
    public void BalanceDisplay_ShouldHandleZero()
    {
        var balance = new AccountBalance(false, 0m, "USD", DateTimeOffset.Now);
        Assert.Equal("USD 0.00", balance.BalanceDisplay);
    }
}
