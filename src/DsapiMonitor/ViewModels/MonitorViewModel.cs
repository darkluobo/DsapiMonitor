using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Media;
using DsapiMonitor.Models;
using DsapiMonitor.Services;

namespace DsapiMonitor.ViewModels;

public sealed class MonitorViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly DeepSeekMonitorService _monitor;
    private readonly LatencyProbeService _latencyTracker;
    private readonly AppSettings _settings;

    private string _balanceText = "---";
    private string _statusText = "未配置";
    private string _latencyText = "--";
    private string _lastUpdateText = "--";
    private string _apiKey = "";
    private string? _errorText;
    private string? _diagnosticText;
    private bool _isExpanded = true;
    private bool _isConfiguring;
    private bool _isRunning;
    private OverallState _state = OverallState.Unknown;

    // 用量追踪
    private decimal _prevBalance;
    private decimal _todayUsage;
    private decimal _monthUsage;
    private DateTime _lastBalanceDate;
    private DateTime _lastBalanceMonth;
    private string _todayUsageText = "--";
    private string _monthUsageText = "--";
    private UsageTab _currentTab = UsageTab.Latency;

    // 余额预警 & 变化提醒
    private string? _balanceWarningText;
    private string? _balanceChangeText;
    private decimal _balanceWarningThreshold;
    private CancellationTokenSource? _changeCts;

    // 每日限额
    private string _dailyLimitInput = "";
    private bool _isDailyLimitExceeded;

    public MonitorViewModel(DeepSeekMonitorService monitor, LatencyProbeService latencyTracker, AppSettings settings)
    {
        _monitor = monitor;
        _latencyTracker = latencyTracker;
        _settings = settings;

        _apiKey = settings.ApiKey;
        _balanceWarningThreshold = settings.BalanceWarningThreshold;
        _dailyLimitInput = settings.DailyLimit > 0 ? settings.DailyLimit.ToString("G") : "";

        _monitor.BalanceUpdated += OnBalanceUpdated;
        _monitor.LatencyUpdated += OnLatencyUpdated;
        _monitor.ErrorOccurred += OnError;

        OpenDashboardCommand = new RelayCommand(() =>
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "https://platform.deepseek.com/usage",
                UseShellExecute = true
            }));

        ToggleExpandCommand = new RelayCommand(() => IsExpanded = !IsExpanded);
        ShowConfigCommand = new RelayCommand(() => IsConfiguring = true);
        SaveApiKeyCommand = new RelayCommand(SaveApiKey);
        CancelConfigCommand = new RelayCommand(() => IsConfiguring = false);
        LatencyTabCommand = new RelayCommand(() => SwitchTab(UsageTab.Latency));
        TodayTabCommand = new RelayCommand(() => SwitchTab(UsageTab.Today));
        MonthTabCommand = new RelayCommand(() => SwitchTab(UsageTab.Month));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    // --- 主要显示属性 ---

    public string BalanceText { get => _balanceText; set { _balanceText = value; OnPropertyChanged(); } }
    public string StatusText { get => _statusText; set { _statusText = value; OnPropertyChanged(); } }
    public string LatencyText { get => _latencyText; set { _latencyText = value; OnPropertyChanged(); } }
    public string LastUpdateText { get => _lastUpdateText; set { _lastUpdateText = value; OnPropertyChanged(); } }

    public string? ErrorText { get => _errorText; set { _errorText = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasError)); } }
    public bool HasError => _errorText != null;
    public string? DiagnosticText { get => _diagnosticText; set { _diagnosticText = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasDiagnostic)); } }
    public bool HasDiagnostic => _diagnosticText != null;

    public bool IsExpanded { get => _isExpanded; set { _isExpanded = value; OnPropertyChanged(); } }
    public bool IsConfiguring { get => _isConfiguring; set { _isConfiguring = value; OnPropertyChanged(); } }

    public string ApiKeyInput { get => _apiKey; set { _apiKey = value; OnPropertyChanged(); } }

    public OverallState State { get => _state; set { _state = value; OnPropertyChanged(); OnPropertyChanged(nameof(StateBrush)); } }

    // 用量数据
    public string TodayUsageText { get => _todayUsageText; set { _todayUsageText = value; OnPropertyChanged(); } }
    public string MonthUsageText { get => _monthUsageText; set { _monthUsageText = value; OnPropertyChanged(); } }

    // 余额预警 & 变化提醒
    public string? BalanceWarningText { get => _balanceWarningText; set { _balanceWarningText = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasBalanceWarning)); } }
    public bool HasBalanceWarning => _balanceWarningText != null;
    public string? BalanceChangeText { get => _balanceChangeText; set { _balanceChangeText = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasBalanceChange)); } }
    public bool HasBalanceChange => _balanceChangeText != null;

    // 每日限额
    public string DailyLimitInput
    {
        get => _dailyLimitInput;
        set
        {
            _dailyLimitInput = value;
            OnPropertyChanged();
            if (decimal.TryParse(value, out var limit) && limit > 0)
            {
                _settings.DailyLimit = limit;
                _settings.Save();
            }
            else if (string.IsNullOrWhiteSpace(value))
            {
                _settings.DailyLimit = 0;
                _settings.Save();
            }
            CheckDailyLimit();
        }
    }
    public bool IsDailyLimitExceeded
    {
        get => _isDailyLimitExceeded;
        set { _isDailyLimitExceeded = value; OnPropertyChanged(); OnPropertyChanged(nameof(DailyLimitBrush)); }
    }
    public Brush DailyLimitBrush => IsDailyLimitExceeded
        ? new SolidColorBrush(Color.FromRgb(0xEF, 0x44, 0x44))
        : new SolidColorBrush(Color.FromRgb(0x0E, 0x12, 0x23));

    public UsageTab CurrentTab { get => _currentTab; set { _currentTab = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsLatencyTab)); OnPropertyChanged(nameof(IsTodayTab)); OnPropertyChanged(nameof(IsMonthTab)); } }
    public bool IsLatencyTab => CurrentTab == UsageTab.Latency;
    public bool IsTodayTab => CurrentTab == UsageTab.Today;
    public bool IsMonthTab => CurrentTab == UsageTab.Month;

    public ObservableCollection<double> LatencyPoints => _latencyTracker.History.Records
        .Where(r => r.IsSuccessful)
        .Select(r => r.Milliseconds)
        .ToObservableCollection();

    public Brush StateBrush => State switch
    {
        OverallState.Operational => new SolidColorBrush(Color.FromRgb(0x00, 0xE6, 0x76)),
        OverallState.DegradedPerformance => new SolidColorBrush(Color.FromRgb(0xFF, 0xC1, 0x07)),
        OverallState.OutOfService => new SolidColorBrush(Color.FromRgb(0xFF, 0x52, 0x52)),
        _ => new SolidColorBrush(Color.FromRgb(0x9E, 0x9E, 0x9E))
    };

    // --- 命令 ---

    public ICommand OpenDashboardCommand { get; }
    public ICommand ToggleExpandCommand { get; }
    public ICommand ShowConfigCommand { get; }
    public ICommand SaveApiKeyCommand { get; }
    public ICommand CancelConfigCommand { get; }
    public ICommand LatencyTabCommand { get; }
    public ICommand TodayTabCommand { get; }
    public ICommand MonthTabCommand { get; }

    // --- 方法 ---

    public void Start()
    {
        if (!string.IsNullOrWhiteSpace(_apiKey))
        {
            _monitor.SetApiKey(_apiKey);
            _monitor.Start();
        }
    }

    private void SaveApiKey()
    {
        IsConfiguring = false;
        if (!string.IsNullOrWhiteSpace(_apiKey))
        {
            _settings.ApiKey = _apiKey;
            _settings.Save();

            _monitor.SetApiKey(_apiKey);
            StatusText = "查询中...";
            DiagnosticText = null;
            if (!_isRunning)
            {
                _isRunning = true;
                _monitor.Start();
            }
        }
    }

    private void SwitchTab(UsageTab tab)
    {
        CurrentTab = tab;
    }

    private void TrackUsage(decimal balance)
    {
        var now = DateTime.Now;

        // 首次记录
        if (_prevBalance == 0)
        {
            _prevBalance = balance;
            _lastBalanceDate = now.Date;
            _lastBalanceMonth = new DateTime(now.Year, now.Month, 1);
            return;
        }

        // 跨天重置
        if (now.Date != _lastBalanceDate)
        {
            _todayUsage = 0;
            _lastBalanceDate = now.Date;
        }

        // 跨月重置
        var currentMonth = new DateTime(now.Year, now.Month, 1);
        if (currentMonth != _lastBalanceMonth)
        {
            _monthUsage = 0;
            _lastBalanceMonth = currentMonth;
        }

        // 余额减少 = 消耗
        if (balance < _prevBalance)
        {
            var delta = _prevBalance - balance;
            _todayUsage += delta;
            _monthUsage += delta;
            ShowBalanceChange($"-¥{delta:N4}");
        }

        _prevBalance = balance;

        TodayUsageText = $"¥{_todayUsage:N4}";
        MonthUsageText = $"¥{_monthUsage:N4}";

        CheckDailyLimit();
    }

    private void CheckDailyLimit()
    {
        IsDailyLimitExceeded = _settings.DailyLimit > 0 && _todayUsage >= _settings.DailyLimit;
    }

    private void ShowBalanceChange(string text)
    {
        BalanceChangeText = text;
        _changeCts?.Cancel();
        _changeCts = new CancellationTokenSource();
        var ct = _changeCts.Token;
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(4000, ct);
                System.Windows.Application.Current.Dispatcher.Invoke(() => BalanceChangeText = null);
            }
            catch (OperationCanceledException) { }
        });
    }

    private void OnBalanceUpdated(AccountBalance balance)
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            TrackUsage(balance.Balance);

            BalanceText = balance.BalanceDisplay;
            StatusText = balance.IsAvailable ? "账户正常" : "余额不足";
            State = balance.IsAvailable ? OverallState.Operational : OverallState.OutOfService;
            LastUpdateText = balance.LastUpdated.ToLocalTime().ToString("HH:mm:ss");
            ErrorText = null;

            // 余额预警
            if (balance.Balance <= _balanceWarningThreshold && balance.IsAvailable)
                BalanceWarningText = $"余额不足 ¥{_balanceWarningThreshold:N2}，请及时充值";
            else
                BalanceWarningText = null;

            OnPropertyChanged(nameof(LatencyPoints));
        });
    }

    private void OnLatencyUpdated(int latencyMs)
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            LatencyText = latencyMs >= 0 ? $"{latencyMs}ms" : "超时";
            OnPropertyChanged(nameof(LatencyPoints));
        });
    }

    private void OnError(Exception ex)
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            ErrorText = ex.Message;
            DiagnosticText = $"[{DateTime.Now:HH:mm:ss}] {ex.GetType().Name}: {ex.Message}";
            State = OverallState.OutOfService;
        });
    }

    public void Dispose()
    {
        _monitor.Dispose();
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public enum UsageTab { Latency, Today, Month }

internal sealed class RelayCommand : ICommand
{
    private readonly Action _execute;
    public RelayCommand(Action execute) => _execute = execute;
    public event EventHandler? CanExecuteChanged;
    public bool CanExecute(object? parameter) => true;
    public void Execute(object? parameter) => _execute();
}

public enum OverallState { Unknown, Operational, DegradedPerformance, OutOfService }

internal static class CollectionExtensions
{
    public static ObservableCollection<T> ToObservableCollection<T>(this IEnumerable<T> source)
        => new(source);
}
