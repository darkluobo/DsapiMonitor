using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DsapiMonitor;

public partial class MiniWindow : Window
{
    private bool _isDragging;

    public MiniWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;

        var menu = new ContextMenu();
        var showItem = new MenuItem { Header = "显示主窗口" };
        showItem.Click += (_, _) => Restore();
        var exitItem = new MenuItem { Header = "退出" };
        exitItem.Click += (_, _) => { App.IsExiting = true; Application.Current.Shutdown(); };
        menu.Items.Add(showItem);
        menu.Items.Add(new Separator());
        menu.Items.Add(exitItem);
        ContextMenu = menu;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var workArea = SystemParameters.WorkArea;
        Left = workArea.Right - Width - 20;
        Top = workArea.Bottom - Height - 20;
    }

    private void WindowDrag(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2) return;
        _isDragging = true;
        DragMove();
        _isDragging = false;
    }

    private void WindowRestore(object sender, MouseButtonEventArgs e)
    {
        if (!_isDragging && e.ClickCount == 2)
            Restore();
    }

    private void Restore()
    {
        Hide();
        if (Application.Current.MainWindow is MainWindow main)
        {
            main.Show();
            main.Activate();
        }
    }
}
