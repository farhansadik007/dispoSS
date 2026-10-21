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
        var folder = isDev ? "dispoSS-dev" : "dispoSS";
        SettingsPath = Path.Combine(root, folder, "settings.json");

        var legacyFolders = isDev
            ? new[] { "Snipt-dev", "DisposableScreenshots-dev" }
            : new[] { "Snipt", "DisposableScreenshots" };
        LegacySettingsPaths = legacyFolders
            .Select(legacyFolder => Path.Combine(root, legacyFolder, "settings.json"))
            .ToArray();
    }

    public string SettingsPath { get; }

    private string[] LegacySettingsPaths { get; }

    public Models.AppSettings Load()
    {
        try
        {
            var path = SettingsPath;
            foreach (var legacyPath in LegacySettingsPaths)
            {
                if (!File.Exists(path) && File.Exists(legacyPath))
                {
                    path = legacyPath;
                }
            }

            if (!File.Exists(path))
            {
                return new Models.AppSettings();
            }

            var json = File.ReadAllText(path, Encoding.UTF8);
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
            File.Copy(temporaryPath, SettingsPath, true);
            File.Delete(temporaryPath);
        }
        catch (Exception exception)
        {
            AppLog.Error("Could not save settings", exception);
            throw;
        }
    }
}
