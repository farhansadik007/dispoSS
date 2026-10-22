using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Win32;
using Windows.ApplicationModel;

namespace DisposableScreenshots.Services;

public static class StartupService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "dispoSS";
    private const string StartupTaskId = "dispoSSStartup";
    private static readonly string[] LegacyValueNames = { "Snipt", "DisposableScreenshots" };

    public static bool IsEnabled()
    {
        if (PackageContext.IsPackaged)
        {
            var task = GetStartupTask();
            return task.State == StartupTaskState.Enabled;
        }

        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false);
        return key?.GetValue(ValueName) is string value && !string.IsNullOrWhiteSpace(value);
    }

    public static async Task SetEnabledAsync(bool enabled)
    {
        if (PackageContext.IsPackaged)
        {
            var task = await StartupTask.GetAsync(StartupTaskId);
            if (!enabled)
            {
                task.Disable();
                AppLog.Info($"Packaged startup task disabled; state={task.State}.");
                return;
            }

            var state = await task.RequestEnableAsync();
            AppLog.Info($"Packaged startup task enable requested; state={state}.");
            if (state != StartupTaskState.Enabled)
            {
                throw new InvalidOperationException(
                    $"Windows did not enable the startup task (state: {state}). " +
                    "Enable dispoSS in Settings > Apps > Startup if it was turned off there.");
            }

            return;
        }

        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true)
                        ?? Registry.CurrentUser.CreateSubKey(RunKeyPath, true);

        foreach (var legacyName in LegacyValueNames)
        {
            key.DeleteValue(legacyName, false);
        }

        if (!enabled)
        {
            key.DeleteValue(ValueName, false);
            return;
        }

        key.SetValue(ValueName, $"\"{GetExecutablePath()}\" --tray", RegistryValueKind.String);
    }

    public static async Task EnsureEnabledAsync()
    {
        if (!PackageContext.IsPackaged)
        {
            return;
        }

        try
        {
            var task = await StartupTask.GetAsync(StartupTaskId);
            if (task.State == StartupTaskState.Enabled)
            {
                return;
            }

            var state = await task.RequestEnableAsync();
            AppLog.Info($"Packaged startup task reconciled; state={state}.");
            if (state != StartupTaskState.Enabled)
            {
                AppLog.Info($"The packaged startup task could not be enabled (state: {state}).");
            }
        }
        catch (Exception exception)
        {
            AppLog.Error("Could not reconcile the packaged startup task", exception);
        }
    }

    public static void MigrateLegacyEntry()
    {
        if (PackageContext.IsPackaged)
        {
            RemovePackagedRunEntry();
            return;
        }

        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true);
        if (key is null)
        {
            return;
        }

        var hadLegacyEntry = false;
        foreach (var legacyName in LegacyValueNames)
        {
            if (key.GetValue(legacyName) is string value && !string.IsNullOrWhiteSpace(value))
            {
                hadLegacyEntry = true;
                key.DeleteValue(legacyName, false);
            }
        }

        if (hadLegacyEntry)
        {
            key.SetValue(ValueName, $"\"{GetExecutablePath()}\" --tray", RegistryValueKind.String);
        }
    }

    private static StartupTask GetStartupTask()
    {
        try
        {
            return StartupTask.GetAsync(StartupTaskId).AsTask().GetAwaiter().GetResult();
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException("The packaged startup task could not be read.", exception);
        }
    }

    private static void RemovePackagedRunEntry()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true);
        if (key?.GetValue(ValueName) is not string value || string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        // An earlier packaged build wrote the WindowsApps executable path to the Run key.
        // That path cannot be launched directly, so remove only that stale entry.
        if (value.IndexOf(@"\WindowsApps\", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            key.DeleteValue(ValueName, false);
        }
    }

    private static string GetExecutablePath()
    {
        var executable = Process.GetCurrentProcess().MainModule?.FileName;
        if (string.IsNullOrWhiteSpace(executable))
        {
            executable = Environment.GetCommandLineArgs()[0];
        }

        if (string.IsNullOrWhiteSpace(executable))
        {
            throw new InvalidOperationException("The application path could not be determined.");
        }

        return executable!;
    }
}
