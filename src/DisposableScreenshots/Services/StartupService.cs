using System.Diagnostics;
using Microsoft.Win32;

namespace DisposableScreenshots.Services;

public static class StartupService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "DisposableScreenshots";

    public static bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false);
        return key?.GetValue(ValueName) is string value && !string.IsNullOrWhiteSpace(value);
    }

    public static void SetEnabled(bool enabled)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true)
                        ?? Registry.CurrentUser.CreateSubKey(RunKeyPath, true);

        if (!enabled)
        {
            key.DeleteValue(ValueName, false);
            return;
        }

        var executable = Process.GetCurrentProcess().MainModule?.FileName;
        if (string.IsNullOrWhiteSpace(executable))
        {
            executable = Environment.GetCommandLineArgs()[0];
        }

        if (string.IsNullOrWhiteSpace(executable))
        {
            throw new InvalidOperationException("The application path could not be determined.");
        }

        key.SetValue(ValueName, $"\"{executable}\" --tray", RegistryValueKind.String);
    }
}
