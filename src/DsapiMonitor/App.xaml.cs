using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DsapiMonitor.Services;
using DsapiMonitor.ViewModels;
using Hardcodet.Wpf.TaskbarNotification;
using Microsoft.Extensions.DependencyInjection;

namespace DsapiMonitor;

public partial class App : Application
{
    internal static bool IsExiting { get; set; }

    private ServiceProvider? _services;
    private TaskbarIcon? _trayIcon;

    protected override void OnStartup(StartupEventArgs e)
    {
        var settings = AppSettings.Load();

        var services = new ServiceCollection();

        services.AddSingleton(settings);

        services.AddHttpClient<IDeepSeekUsageProvider, DeepSeekUsageProvider>(client =>
        {
            client.BaseAddress = new Uri("https://api.deepseek.com/");
            client.Timeout = TimeSpan.FromSeconds(15);
        });

        services.AddSingleton<LatencyProbeService>();
        services.AddSingleton<DeepSeekMonitorService>();
        services.AddSingleton<MonitorViewModel>(sp =>
            new MonitorViewModel(
                sp.GetRequiredService<DeepSeekMonitorService>(),
                sp.GetRequiredService<LatencyProbeService>(),
                sp.GetRequiredService<AppSettings>()));

        _services = services.BuildServiceProvider();

        InitTrayIcon();

        var vm = _services.GetRequiredService<MonitorViewModel>();
        var window = new MainWindow { DataContext = vm };
        window.Show();

        base.OnStartup(e);
    }

    private void InitTrayIcon()
    {
        var showItem = new MenuItem { Header = "显示窗口" };
        showItem.Click += (_, _) => ShowMainWindow();

        var exitItem = new MenuItem { Header = "退出" };
        exitItem.Click += (_, _) => { IsExiting = true; Shutdown(); };

        var menu = new ContextMenu();
        menu.Items.Add(showItem);
        menu.Items.Add(new Separator());
        menu.Items.Add(exitItem);

        _trayIcon = new TaskbarIcon
        {
            IconSource = CreateIconImage(),
            ToolTipText = "DeepSeek 用量监控",
            ContextMenu = menu
        };
        _trayIcon.TrayMouseDoubleClick += (_, _) => ShowMainWindow();
    }

    private void ShowMainWindow()
    {
        if (MainWindow == null) return;
        MainWindow.Show();
        MainWindow.WindowState = WindowState.Normal;
        MainWindow.Activate();
    }

    private static BitmapSource CreateIconImage()
    {
        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen())
        {
            dc.DrawEllipse(
                new SolidColorBrush(Color.FromRgb(0x0E, 0x12, 0x23)),
                null,
                new System.Windows.Point(16, 16), 16, 16);

            var ft = new FormattedText("D",
                System.Globalization.CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                new Typeface("Segoe UI"),
                18,
                new SolidColorBrush(Color.FromRgb(0x22, 0xC5, 0x5E)),
                VisualTreeHelper.GetDpi(visual).PixelsPerDip);
            dc.DrawText(ft, new System.Windows.Point(16 - ft.Width / 2, 16 - ft.Height / 2));
        }

        var bmp = new RenderTargetBitmap(32, 32, 96, 96, PixelFormats.Pbgra32);
        bmp.Render(visual);
        bmp.Freeze();
        return bmp;
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _trayIcon?.Dispose();
        _services?.Dispose();
        base.OnExit(e);
    }
}
