using System.Reflection;
using System.Windows;
using System.Windows.Interop;
using DisposableScreenshots.Interop;
using DisposableScreenshots.Models;

namespace DisposableScreenshots.Views;

public partial class MainWindow : Window
{
    public MainWindow(AppSettings settings, bool isDev)
    {
        InitializeComponent();

        Title = isDev ? "Disposable Screenshots (dev)" : "Disposable Screenshots";
        HotkeyText.Text = HotkeyDefinition.Parse(settings.Hotkey).ToString();

        var version = Assembly.GetExecutingAssembly().GetName().Version;
        VersionText.Text = version is null ? "dev build" : $"v{version.Major}.{version.Minor}.{version.Build}";
    }

    public event EventHandler? CaptureRegionRequested;

    public event EventHandler? CaptureFullScreenRequested;

    public event EventHandler? SettingsRequested;

    public event EventHandler? ExitRequested;

    private void CaptureRegion_Click(object sender, RoutedEventArgs e) =>
        CaptureRegionRequested?.Invoke(this, EventArgs.Empty);

    private void CaptureFullScreen_Click(object sender, RoutedEventArgs e) =>
        CaptureFullScreenRequested?.Invoke(this, EventArgs.Empty);

    private void SettingsButton_Click(object sender, RoutedEventArgs e) =>
        SettingsRequested?.Invoke(this, EventArgs.Empty);

    private void HideButton_Click(object sender, RoutedEventArgs e) => Hide();

    private void ExitButton_Click(object sender, RoutedEventArgs e) =>
        ExitRequested?.Invoke(this, EventArgs.Empty);

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        var handle = new WindowInteropHelper(this).Handle;
        var enabled = 1;
        NativeMethods.DwmSetWindowAttribute(
            handle,
            NativeMethods.DWMWA_USE_IMMERSIVE_DARK_MODE,
            ref enabled,
            sizeof(int));
        NativeMethods.DwmSetWindowAttribute(
            handle,
            NativeMethods.DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1,
            ref enabled,
            sizeof(int));
    }
}
