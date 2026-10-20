using System.Windows.Media.Imaging;
using DisposableScreenshots.Models;
using DisposableScreenshots.Views;

namespace DisposableScreenshots.Services;

public sealed class CaptureCoordinator : IDisposable
{
    private readonly AppSettings _settings;
    private readonly ClipboardService _clipboard;
    private readonly ScreenCaptureService _screenshots = new();

    private readonly List<CaptureOverlayWindow> _overlays = new();
    private CancellationTokenSource? _cancellation;
    private CountdownWindow? _countdown;
    private PreviewWindow? _preview;
    private bool _busy;

    public CaptureCoordinator(AppSettings settings, ClipboardService clipboard)
    {
        _settings = settings;
        _clipboard = clipboard;
    }

    public bool IsBusy => _busy;

    public async Task StartAsync(CaptureMode mode)
    {
        if (_busy)
        {
            return;
        }

        _busy = true;
        _cancellation = new CancellationTokenSource();
        var token = _cancellation.Token;

        try
        {
            AppLog.Info($"Starting {mode} capture (delay {_settings.DelaySeconds}s).");

            if (!await ShowCountdownAsync(_settings.DelaySeconds, token))
            {
                AppLog.Info("Capture cancelled during countdown.");
                return;
            }

            var snapshot = _screenshots.CaptureDesktop();
            AppLog.Info($"Captured desktop {snapshot.VirtualScreen.Width}x{snapshot.VirtualScreen.Height} with {snapshot.Monitors.Count} monitor(s).");

            if (mode == CaptureMode.FullScreen)
            {
                var fullImage = snapshot.CropPhysical(new PixelRect(
                    snapshot.VirtualScreen.Left,
                    snapshot.VirtualScreen.Top,
                    snapshot.VirtualScreen.Width,
                    snapshot.VirtualScreen.Height));

                ShowResult(fullImage);
                return;
            }

            var selection = await ShowRegionSelectionAsync(snapshot, token);
            if (selection is null)
            {
                return;
            }

            ShowResult(snapshot.CropPhysical(selection));
        }
        finally
        {
            CloseTransientWindows();
            _cancellation?.Dispose();
            _cancellation = null;
            _busy = false;
        }
    }

    public void Cancel()
    {
        _cancellation?.Cancel();

        if (_countdown is { IsVisible: true })
        {
            _countdown.Close();
        }

        foreach (var overlay in _overlays.ToArray())
        {
            if (overlay.IsVisible)
            {
                overlay.Close();
            }
        }

        _overlays.Clear();
    }

    private async Task<bool> ShowCountdownAsync(int seconds, CancellationToken token)
    {
        if (seconds <= 0)
        {
            return true;
        }

        var window = new CountdownWindow(seconds);
        _countdown = window;
        window.Show();

        try
        {
            for (var remaining = seconds; remaining > 0; remaining--)
            {
                token.ThrowIfCancellationRequested();
                window.SetRemaining(remaining);
                await Task.Delay(TimeSpan.FromSeconds(1), token);
            }

            window.SetRemaining(0);
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
        finally
        {
            if (window.IsVisible)
            {
                window.Close();
            }

            _countdown = null;
            await Task.Delay(120);
        }
    }

    private async Task<PixelRect?> ShowRegionSelectionAsync(DesktopSnapshot snapshot, CancellationToken token)
    {
        var completion = new TaskCompletionSource<PixelRect?>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var registration = token.Register(() => completion.TrySetResult(null));

        foreach (var monitor in snapshot.Monitors)
        {
            var window = new CaptureOverlayWindow(monitor, snapshot.GetMonitorImage(monitor));
            window.SelectionCompleted += (_, rect) => completion.TrySetResult(rect);
            window.Cancelled += (_, _) => completion.TrySetResult(null);
            _overlays.Add(window);
        }

        foreach (var overlay in _overlays)
        {
            overlay.Show();
        }

        var cursorMonitor = ScreenService.GetMonitorAtCursor();
        var activeOverlay = _overlays.FirstOrDefault(
            overlay => overlay.Monitor.DeviceName == cursorMonitor.DeviceName);

        activeOverlay ??= _overlays.FirstOrDefault();
        if (activeOverlay is not null)
        {
            activeOverlay.Activate();
            activeOverlay.Focus();
        }

        var selection = await completion.Task;
        AppLog.Info(selection is null
            ? "Region selection cancelled."
            : $"Region selected: {selection.X},{selection.Y} {selection.Width}x{selection.Height}.");

        CloseOverlays();
        await Task.Delay(60);
        return selection;
    }

    private void ShowResult(BitmapSource image)
    {
        var copied = _settings.CopyToClipboard &&
                     _clipboard.Copy(image, _settings.ClipboardClearSeconds);

        AppLog.Info($"Capture result {image.PixelWidth}x{image.PixelHeight}; copied={copied}; preview={_settings.ShowPreview}.");

        if (!_settings.ShowPreview && copied)
        {
            return;
        }

        if (_preview is { IsVisible: true })
        {
            _preview.Close();
        }

        _preview = new PreviewWindow(
            image,
            copied,
            _settings.PreviewSeconds,
            _clipboard,
            _settings.ClipboardClearSeconds);
        _preview.Closed += (_, _) => _preview = null;
        _preview.Show();
    }

    private void CloseOverlays()
    {
        foreach (var overlay in _overlays)
        {
            if (overlay.IsVisible)
            {
                overlay.Close();
            }
        }

        _overlays.Clear();
    }

    private void CloseTransientWindows()
    {
        if (_countdown is { IsVisible: true })
        {
            _countdown.Close();
        }

        _countdown = null;
        CloseOverlays();
    }

    public void Dispose()
    {
        Cancel();
        CloseTransientWindows();

        if (_preview is { IsVisible: true })
        {
            _preview.Close();
        }

        _preview = null;
        _cancellation?.Dispose();
        _cancellation = null;
    }
}
