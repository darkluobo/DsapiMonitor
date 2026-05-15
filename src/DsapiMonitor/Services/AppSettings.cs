using System.IO;
using System.Text.Json;

namespace DsapiMonitor.Services;

public sealed class AppSettings
{
    private static readonly string SettingsDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DsapiMonitor");

    private static readonly string SettingsPath = Path.Combine(SettingsDir, "settings.json");

    public string ApiKey { get; set; } = "";
    public string SessionCookie { get; set; } = "";
    public double WindowLeft { get; set; } = -1;
    public double WindowTop { get; set; } = -1;
    public decimal BalanceWarningThreshold { get; set; } = 1.0m;
    public decimal DailyLimit { get; set; }

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
        }
        catch { }
        return new AppSettings();
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(SettingsDir);
            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsPath, json);
        }
        catch { }
    }
}
