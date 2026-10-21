using System.Windows;
using System.Windows.Interop;
using DisposableScreenshots.Interop;
using DisposableScreenshots.Services;

namespace DisposableScreenshots.Views;

public partial class CountdownWindow : Window
{
    private readonly int _totalSeconds;

    public CountdownWindow(int totalSeconds)
    {
        InitializeComponent();
        _totalSeconds = Math.Max(1, totalSeconds);
        CountdownText.Text = _totalSeconds.ToString();
        CountdownProgress.Value = 0;
        SourceInitialized += OnSourceInitialized;
    }

    public void SetRemaining(int remaining)
    {
        remaining = Math.Max(0, remaining);
        CountdownText.Text = remaining.ToString();
        CountdownProgress.Value = MathCompat.Clamp(1.0 - (double)remaining / _totalSeconds, 0, 1);
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        var monitor = ScreenService.GetMonitorAtCursor();
        var width = Math.Max(1, (int)Math.Round(Width * monitor.ScaleX));
        var height = Math.Max(1, (int)Math.Round(Height * monitor.ScaleY));
        var x = monitor.Left + (monitor.Width - width) / 2;
        var y = monitor.Top + (monitor.Height - height) / 2;

        NativeMethods.SetWindowPos(
            new WindowInteropHelper(this).Handle,
            NativeMethods.HWND_TOPMOST,
            x,
            y,
            width,
            height,
            NativeMethods.SWP_NOACTIVATE | NativeMethods.SWP_SHOWWINDOW);
    }
}
