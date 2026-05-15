using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using DsapiMonitor.ViewModels;

namespace DsapiMonitor;

public partial class MainWindow : Window
{
    private MiniWindow? _mini;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Closing += OnClosing;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var workArea = SystemParameters.WorkArea;
        Left = workArea.Right - Width - 20;
        Top = workArea.Bottom - Height - 20;

        if (DataContext is MonitorViewModel { ApiKeyInput.Length: 0 } vm)
            vm.IsConfiguring = true;
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            if (DataContext is MonitorViewModel vm)
                vm.IsExpanded = !vm.IsExpanded;
            return;
        }

        try { DragMove(); }
        catch { }
        SnapToEdge();
    }

    private void SnapToEdge()
    {
        var workArea = SystemParameters.WorkArea;
        const double threshold = 20;
        var duration = TimeSpan.FromMilliseconds(200);
        var ease = new System.Windows.Media.Animation.CubicEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut };

        double? targetLeft = null;
        double? targetTop = null;

        if (Left - workArea.Left < threshold)
            targetLeft = workArea.Left + 4;
        else if (workArea.Right - (Left + Width) < threshold)
            targetLeft = workArea.Right - Width - 4;

        if (Top - workArea.Top < threshold)
            targetTop = workArea.Top + 4;
        else if (workArea.Bottom - (Top + Height) < threshold)
            targetTop = workArea.Bottom - Height - 4;

        if (targetLeft.HasValue)
            BeginAnimation(LeftProperty, new System.Windows.Media.Animation.DoubleAnimation(targetLeft.Value, new Duration(duration)) { EasingFunction = ease });

        if (targetTop.HasValue)
            BeginAnimation(TopProperty, new System.Windows.Media.Animation.DoubleAnimation(targetTop.Value, new Duration(duration)) { EasingFunction = ease });
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Hide();

    private void MiniButton_Click(object sender, RoutedEventArgs e)
    {
        if (_mini == null)
        {
            _mini = new MiniWindow { DataContext = DataContext };
        }
        else
        {
            _mini.DataContext = DataContext;
        }
        _mini.Show();
        Hide();
    }

    private void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (!App.IsExiting)
        {
            e.Cancel = true;
            Hide();
            _mini?.Hide();
        }
    }

    private void LatencyTab_Click(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is MonitorViewModel vm) vm.CurrentTab = UsageTab.Latency;
    }

    private void TodayTab_Click(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is MonitorViewModel vm) vm.CurrentTab = UsageTab.Today;
    }

    private void MonthTab_Click(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is MonitorViewModel vm) vm.CurrentTab = UsageTab.Month;
    }

    private void DashboardLink_Click(object sender, MouseButtonEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "https://platform.deepseek.com/usage",
            UseShellExecute = true
        });
    }
}
