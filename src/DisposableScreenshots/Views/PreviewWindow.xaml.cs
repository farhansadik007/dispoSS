using System.IO;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using DisposableScreenshots.Interop;
using DisposableScreenshots.Services;
using Microsoft.Win32;

namespace DisposableScreenshots.Views;

public partial class PreviewWindow : Window
{
    private readonly BitmapSource _image;
    private readonly ClipboardService _clipboard;
    private readonly int _clearAfterSeconds;
    private readonly DispatcherTimer _closeTimer;
    private int _remainingSeconds;

    public PreviewWindow(
        BitmapSource image,
        bool copied,
        int previewSeconds,
        ClipboardService clipboard,
        int clearAfterSeconds)
    {
        InitializeComponent();

        _image = image;
        _clipboard = clipboard;
        _clearAfterSeconds = clearAfterSeconds;
        _remainingSeconds = Math.Max(2, previewSeconds);

        PreviewImage.Source = image;
        MetaText.Text = $"{image.PixelWidth} × {image.PixelHeight} px · disposable · not saved";
        SetStatus(copied ? "Copied to clipboard" : "Screenshot ready");
        UpdateAutoCloseText();

        _closeTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _closeTimer.Tick += CloseTimer_Tick;

        Loaded += OnLoaded;
        MouseEnter += (_, _) => _closeTimer.Stop();
        MouseLeave += (_, _) => StartCloseTimer();
        Closed += (_, _) => _closeTimer.Stop();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var monitor = ScreenService.GetMonitorAtCursor();
        var physicalWidth = Math.Max(1, (int)Math.Round(ActualWidth * monitor.ScaleX));
        var physicalHeight = Math.Max(1, (int)Math.Round(ActualHeight * monitor.ScaleY));
        var margin = Math.Max(12, (int)Math.Round(18 * monitor.ScaleX));
        var x = monitor.Right - physicalWidth - margin;
        var y = monitor.Bottom - physicalHeight - margin;

        NativeMethods.SetWindowPos(
            new WindowInteropHelper(this).Handle,
            NativeMethods.HWND_TOPMOST,
            x,
            y,
            physicalWidth,
            physicalHeight,
            NativeMethods.SWP_NOACTIVATE | NativeMethods.SWP_SHOWWINDOW);

        Opacity = 1;
        StartCloseTimer();
    }

    private void StartCloseTimer()
    {
        if (_remainingSeconds <= 0)
        {
            return;
        }

        _closeTimer.Start();
    }

    private void CloseTimer_Tick(object? sender, EventArgs e)
    {
        _remainingSeconds--;

        if (_remainingSeconds <= 0)
        {
            _closeTimer.Stop();
            Close();
            return;
        }

        UpdateAutoCloseText();
    }

    private void UpdateAutoCloseText() =>
        AutoCloseText.Text = $"Closes in {_remainingSeconds}s · hover to pause";

    private void CopyButton_Click(object sender, RoutedEventArgs e)
    {
        if (_clipboard.Copy(_image, _clearAfterSeconds))
        {
            SetStatus("Copied to clipboard");
        }
        else
        {
            SetStatus("Clipboard is busy — try Copy again", true);
        }
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        // The timer would otherwise keep ticking in the dialog's nested message loop.
        _closeTimer.Stop();
        var previousRemainingSeconds = _remainingSeconds;
        _remainingSeconds = 0;
        AutoCloseText.Text = "Save dialog open";

        var dialog = new SaveFileDialog
        {
            Title = "Save screenshot",
            Filter = "PNG image (*.png)|*.png",
            DefaultExt = ".png",
            AddExtension = true,
            FileName = $"dispoSS_{DateTime.Now:yyyy-MM-dd-HH-mm-ss}.png"
        };

        if (dialog.ShowDialog(this) != true)
        {
            _remainingSeconds = Math.Max(2, previousRemainingSeconds);
            UpdateAutoCloseText();
            StartCloseTimer();
            return;
        }

        try
        {
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(_image));

            using var stream = File.Create(dialog.FileName);
            encoder.Save(stream);
            SetStatus("Saved");
            AutoCloseText.Text = "Saved to disk";
            _remainingSeconds = 0;
        }
        catch (Exception exception)
        {
            AppLog.Error("Could not save screenshot", exception);
            SetStatus("Could not save the file", true);
            _remainingSeconds = Math.Max(2, previousRemainingSeconds);
            UpdateAutoCloseText();
            StartCloseTimer();
        }
    }

    private void SetStatus(string text, bool error = false)
    {
        StatusText.Text = text;
        StatusText.Foreground = (Brush)FindResource(error ? "RedBrush" : "NavyBrush");
    }

    private void DiscardButton_Click(object sender, RoutedEventArgs e) => Close();
}
