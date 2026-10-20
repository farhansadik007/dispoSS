using System.IO;
using System.Text;
using System.Text.Json;

namespace DisposableScreenshots.Services;

public sealed class SettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public SettingsService(bool isDev)
    {
        var root = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var folder = isDev ? "DisposableScreenshots-dev" : "DisposableScreenshots";
        SettingsPath = Path.Combine(root, folder, "settings.json");
    }

    public string SettingsPath { get; }

    public Models.AppSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsPath))
            {
                return new Models.AppSettings();
            }

            var json = File.ReadAllText(SettingsPath, Encoding.UTF8);
            var settings = JsonSerializer.Deserialize<Models.AppSettings>(json, JsonOptions) ?? new Models.AppSettings();
            settings.Normalize();
            return settings;
        }
        catch (Exception exception)
        {
            AppLog.Error("Could not load settings", exception);
            return new Models.AppSettings();
        }
    }

    public void Save(Models.AppSettings settings)
    {
        try
        {
            settings.Normalize();
            var directory = Path.GetDirectoryName(SettingsPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(settings, JsonOptions);
            var temporaryPath = SettingsPath + ".tmp";
            File.WriteAllText(temporaryPath, json, new UTF8Encoding(false));
            File.Move(temporaryPath, SettingsPath, true);
        }
        catch (Exception exception)
        {
            AppLog.Error("Could not save settings", exception);
            throw;
        }
    }
}
