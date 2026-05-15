using System.Windows;
using Microsoft.Web.WebView2.Core;

namespace DsapiMonitor;

public partial class LoginWindow : Window
{
    public string? SessionCookie { get; private set; }
    public bool LoginSucceeded { get; private set; }
    public string? UsageDataJson { get; private set; }

    private const string ExtractUsageScript = """
        (() => {
            const r = {};
            const costEl = document.querySelector('.cost-section .section-header .value');
            if (costEl) r.totalCost = costEl.textContent.trim();
            r.models = [];
            document.querySelectorAll('.model-section').forEach(sec => {
                const name = sec.querySelector('.model-name')?.textContent?.trim() || '';
                const cards = sec.querySelectorAll('.stat-card');
                const model = { name, apiCalls: '', tokens: '' };
                cards.forEach(card => {
                    const label = card.querySelector('.label')?.textContent?.trim() || '';
                    const value = card.querySelector('.value')?.textContent?.trim() || '';
                    if (label.includes('请求')) model.apiCalls = value;
                    else if (label.includes('Token')) model.tokens = value;
                });
                r.models.push(model);
            });
            r.raw = document.body.innerText.substring(0, 3000);
            return JSON.stringify(r);
        })()
        """;

    public LoginWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await WebView.EnsureCoreWebView2Async();
        WebView.CoreWebView2.Navigate("https://platform.deepseek.com/sign_in");
    }

    private async void LoginComplete_Click(object sender, RoutedEventArgs e)
    {
        var cookies = await WebView.CoreWebView2.CookieManager.GetCookiesAsync("https://platform.deepseek.com");
        var tokenCookie = cookies.FirstOrDefault(c =>
            c.Name.Contains("token", StringComparison.OrdinalIgnoreCase) ||
            c.Name.Contains("session", StringComparison.OrdinalIgnoreCase) ||
            c.Name.Contains("auth", StringComparison.OrdinalIgnoreCase));

        if (tokenCookie != null)
        {
            SessionCookie = $"{tokenCookie.Name}={tokenCookie.Value}";
            LoginSucceeded = true;
        }
        else
        {
            SessionCookie = string.Join("; ", cookies.Select(c => $"{c.Name}={c.Value}"));
            if (cookies.Count > 0)
            {
                LoginSucceeded = true;
            }
            else
            {
                MessageBox.Show("未检测到登录信息，请确认已在页面中完成登录。", "提示",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
        }

        StatusText.Text = "正在获取用量数据...";

        try
        {
            var navTcs = new TaskCompletionSource<bool>();
            void OnNavCompleted(object? s, CoreWebView2NavigationCompletedEventArgs args)
            {
                navTcs.TrySetResult(args.IsSuccess);
                WebView.CoreWebView2.NavigationCompleted -= OnNavCompleted;
            }
            WebView.CoreWebView2.NavigationCompleted += OnNavCompleted;
            WebView.CoreWebView2.Navigate("https://platform.deepseek.com/usage");

            await navTcs.Task;
            await Task.Delay(2000);

            var json = await WebView.CoreWebView2.ExecuteScriptAsync(ExtractUsageScript);
            if (!string.IsNullOrWhiteSpace(json) && json != "null")
            {
                UsageDataJson = json;
            }
        }
        catch
        {
        }

        DialogResult = true;
    }
}
