using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using DisposableScreenshots.Interop;
using DisposableScreenshots.Models;
using DisposableScreenshots.Services;

namespace DisposableScreenshots.Views;

public partial class CaptureOverlayWindow : Window
{
    private readonly MonitorInfo _monitor;

    public CaptureOverlayWindow(MonitorInfo monitor, BitmapSource monitorImage)
    {
        InitializeComponent();
        _monitor = monitor;
        Width = monitor.Width / Math.Max(0.1, monitor.ScaleX);
        Height = monitor.Height / Math.Max(0.1, monitor.ScaleY);

        SelectionSurface.Snapshot = monitorImage;
        SelectionSurface.SelectionCompleted += OnSelectionCompleted;
        SelectionSurface.CancelRequested += (_, _) => Cancelled?.Invoke(this, EventArgs.Empty);
        SourceInitialized += OnSourceInitialized;
        Loaded += OnLoaded;
        Activated += (_, _) => SelectionSurface.Focus();
        PreviewKeyDown += OnPreviewKeyDown;
    }

    public MonitorInfo Monitor => _monitor;

    public event EventHandler<PixelRect>? SelectionCompleted;

    public event EventHandler? Cancelled;

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        var handle = new WindowInteropHelper(this).Handle;
        NativeMethods.SetWindowPos(
            handle,
            NativeMethods.HWND_TOPMOST,
            _monitor.Left,
            _monitor.Top,
            _monitor.Width,
            _monitor.Height,
            NativeMethods.SWP_NOACTIVATE | NativeMethods.SWP_SHOWWINDOW);
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        UpdateScale();
        AppLog.Info(
            $"Overlay loaded for {_monitor.DeviceName}: window={ActualWidth:0}x{ActualHeight:0}, " +
            $"surface={SelectionSurface.ActualWidth:0}x{SelectionSurface.ActualHeight:0}.");
        SelectionSurface.Focus();
    }

    protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
    {
        base.OnRenderSizeChanged(sizeInfo);
        UpdateScale();
    }

    private void UpdateScale()
    {
        SelectionSurface.PixelScaleX = _monitor.Width / Math.Max(1, SelectionSurface.ActualWidth);
        SelectionSurface.PixelScaleY = _monitor.Height / Math.Max(1, SelectionSurface.ActualHeight);
    }

    private void OnSelectionCompleted(object? sender, Rect selection)
    {
        var scaleX = _monitor.Width / Math.Max(1, SelectionSurface.ActualWidth);
        var scaleY = _monitor.Height / Math.Max(1, SelectionSurface.ActualHeight);

        var x = _monitor.Left + (int)Math.Round(selection.Left * scaleX);
        var y = _monitor.Top + (int)Math.Round(selection.Top * scaleY);
        var width = Math.Max(1, (int)Math.Round(selection.Width * scaleX));
        var height = Math.Max(1, (int)Math.Round(selection.Height * scaleY));

        width = Math.Min(width, _monitor.Right - x);
        height = Math.Min(height, _monitor.Bottom - y);

        SelectionCompleted?.Invoke(this, new PixelRect(x, y, width, height));
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            e.Handled = true;
            Cancelled?.Invoke(this, EventArgs.Empty);
        }
    }

    public void Cancel()
    {
        Cancelled?.Invoke(this, EventArgs.Empty);
    }
}
