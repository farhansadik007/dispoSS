using System.Threading;
using System.Windows;
using System.Windows.Threading;
using DisposableScreenshots.Models;
using DisposableScreenshots.Services;
using DisposableScreenshots.Views;

namespace DisposableScreenshots;

public partial class App : Application
{
    private const string MutexName = @"Local\Snipt.SingleInstance";
    private const string ShowWindowEventName = @"Local\Snipt.ShowWindow";

    private Mutex? _singleInstance;
    private bool _ownsMutex;
    private EventWaitHandle? _showWindowEvent;
    private CancellationTokenSource? _showWindowListener;
    private TrayService? _tray;
    private HotkeyService? _hotkey;
    private ClipboardService? _clipboard;
    private SettingsService? _settingsService;
    private CaptureCoordinator? _capture;
    private AppSettings? _settings;
    private MainWindow? _mainWindow;
    private bool _exiting;

    public static bool IsDev { get; } =
        string.Equals(Environment.GetEnvironmentVariable("SNIPT_DEV"), "1", StringComparison.Ordinal);

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            if (args.ExceptionObject is Exception exception)
            {
                AppLog.Error("Unhandled exception", exception);
            }
        };

        _singleInstance = new Mutex(true, MutexName, out var createdNew);
        _ownsMutex = createdNew;

        if (!createdNew)
        {
            SignalExistingInstance();
            Shutdown();
            return;
        }

        StartShowWindowListener();

        try
        {
            _settingsService = new SettingsService(IsDev);
            _settings = _settingsService.Load();

            if (!IsDev)
            {
                try
                {
                    StartupService.MigrateLegacyEntry();
                }
                catch (Exception exception)
                {
                    AppLog.Error("Could not migrate the startup entry", exception);
                }
            }

            _clipboard = new ClipboardService();
            _capture = new CaptureCoordinator(_settings, _clipboard);

            BuildTray();
            BuildHotkey();

            var autoCapture = e.Args.Any(argument =>
                string.Equals(argument, "--capture", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(argument, "--capture-full", StringComparison.OrdinalIgnoreCase));
            var startInTray = e.Args.Any(argument =>
                string.Equals(argument, "--tray", StringComparison.OrdinalIgnoreCase));
            var showMainWindow = !autoCapture && !startInTray;
            AppLog.Info($"Startup args: {string.Join(" ", e.Args)}; showMainWindow={showMainWindow}.");

            if (showMainWindow)
            {
                ShowMainWindow();
            }

            if (_settings is not null && _settings.ShowFirstRunTip)
            {
                _settings.ShowFirstRunTip = false;

                try
                {
                    _settingsService.Save(_settings);
                }
                catch (Exception exception)
                {
                    AppLog.Error("Could not save first-run state", exception);
                }

                if (!showMainWindow)
                {
                    _tray?.ShowInfo(
                        "Snipt",
                        $"Running in the tray. Press {HotkeyDefinition.Parse(_settings.Hotkey)} to capture.");
                }
            }

            if (e.Args.Any(argument => string.Equals(argument, "--capture-full", StringComparison.OrdinalIgnoreCase)))
            {
                Dispatcher.BeginInvoke(() => BeginCapture(CaptureMode.FullScreen));
            }
            else if (e.Args.Any(argument => string.Equals(argument, "--capture", StringComparison.OrdinalIgnoreCase)))
            {
                Dispatcher.BeginInvoke(() => BeginCapture(CaptureMode.Region));
            }
        }
        catch (Exception exception)
        {
            AppLog.Error("Startup failed", exception);
            MessageBox.Show(
                $"Snipt could not start.\n\n{exception.Message}",
                "Startup error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown();
        }
    }

    private static void SignalExistingInstance()
    {
        for (var attempt = 0; attempt < 10; attempt++)
        {
            try
            {
                if (EventWaitHandle.TryOpenExisting(ShowWindowEventName, out var handle))
                {
                    using (handle)
                    {
                        handle.Set();
                    }

                    return;
                }
            }
            catch (Exception exception)
            {
                AppLog.Error("Could not signal the running instance", exception);
                break;
            }

            Thread.Sleep(100);
        }

        MessageBox.Show(
            "Snipt is already running. Open it from the system tray.",
            "Snipt",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void StartShowWindowListener()
    {
        _showWindowEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ShowWindowEventName);
        _showWindowListener = new CancellationTokenSource();
        var token = _showWindowListener.Token;
        var showWindowEvent = _showWindowEvent;

        _ = Task.Run(
            () =>
            {
                try
                {
                    var handles = new WaitHandle[] { showWindowEvent, token.WaitHandle };

                    while (!token.IsCancellationRequested)
                    {
                        var index = WaitHandle.WaitAny(handles);
                        if (index != 0)
                        {
                            break;
                        }

                        try
                        {
                            Dispatcher.BeginInvoke(() =>
                            {
                                AppLog.Info("Another launch requested the main window.");
                                ShowMainWindow();
                            });
                        }
                        catch (InvalidOperationException)
                        {
                            break;
                        }
                    }
                }
                catch (ObjectDisposedException)
                {
                    // The app is shutting down.
                }
                catch (Exception exception)
                {
                    AppLog.Error("Main window listener failed", exception);
                }
            },
            token);
    }

    private void BuildTray()
    {
        if (_settings is null || _capture is null)
        {
            return;
        }

        _tray = new TrayService(_settings);
        _tray.OpenRequested += (_, _) => ShowMainWindow();
        _tray.CaptureRegionRequested += (_, _) => BeginCapture(CaptureMode.Region);
        _tray.CaptureFullScreenRequested += (_, _) => BeginCapture(CaptureMode.FullScreen);
        _tray.SettingsRequested += (_, _) => ShowSettings();
        _tray.ExitRequested += (_, _) => Shutdown();
        _tray.DelayChanged += (_, seconds) =>
        {
            if (_settings is null || _settingsService is null)
            {
                return;
            }

            var previous = _settings.DelaySeconds;
            _settings.DelaySeconds = seconds;
            _settings.Normalize();

            try
            {
                _settingsService.Save(_settings);
                _tray?.UpdateDelay(_settings.DelaySeconds);
            }
            catch (Exception exception)
            {
                AppLog.Error("Could not save delay setting", exception);
                _settings.DelaySeconds = previous;
                _tray?.UpdateDelay(previous);
                _tray?.ShowWarning("Settings", "Could not save the delay setting.");
            }
        };
    }

    private void BuildHotkey()
    {
        if (_settings is null)
        {
            return;
        }

        _hotkey = new HotkeyService();
        _hotkey.Pressed += (_, _) =>
        {
            AppLog.Info("Global hotkey pressed.");

            if (_capture?.IsBusy == true)
            {
                _capture.Cancel();
                return;
            }

            BeginCapture(CaptureMode.Region);
        };

        var hotkey = HotkeyDefinition.Parse(_settings.Hotkey);
        if (!_hotkey.Register(hotkey))
        {
            AppLog.Info($"Could not register global hotkey {hotkey}.");
            _tray?.ShowWarning(
                "Hotkey unavailable",
                $"{hotkey} is already used by another app. Use the tray menu to capture.");
        }
        else
        {
            AppLog.Info($"Registered global hotkey {hotkey}.");
        }
    }

    private async void BeginCapture(CaptureMode mode)
    {
        if (_capture is null)
        {
            return;
        }

        var restoreMainWindow = _mainWindow is { IsVisible: true };
        if (restoreMainWindow)
        {
            _mainWindow?.Hide();
        }

        try
        {
            await _capture.StartAsync(mode);
        }
        catch (Exception exception)
        {
            AppLog.Error("Capture failed", exception);
            MessageBox.Show(
                $"The capture could not be completed.\n\n{exception.Message}",
                "Capture error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            if (restoreMainWindow && _mainWindow is not null)
            {
                _mainWindow.Show();
                _mainWindow.Activate();
            }
        }
    }

    private void ShowMainWindow()
    {
        if (_settings is null)
        {
            AppLog.Info("ShowMainWindow skipped: settings are not loaded.");
            return;
        }

        if (_mainWindow is { IsVisible: true })
        {
            _mainWindow.Activate();
            return;
        }

        if (_mainWindow is null)
        {
            AppLog.Info("Creating main window.");
            _mainWindow = new MainWindow(_settings, IsDev);
            _mainWindow.CaptureRegionRequested += (_, _) => BeginCapture(CaptureMode.Region);
            _mainWindow.CaptureFullScreenRequested += (_, _) => BeginCapture(CaptureMode.FullScreen);
            _mainWindow.SettingsRequested += (_, _) => ShowSettings();
            _mainWindow.SettingsSaveRequested += (_, draft) => ApplySettings(draft);
            _mainWindow.ExitRequested += (_, _) => Shutdown();
            _mainWindow.Closed += (_, _) => _mainWindow = null;
        }

        _mainWindow.Show();
        _mainWindow.Activate();
        AppLog.Info($"Main window shown: visible={_mainWindow.IsVisible}, handle={new System.Windows.Interop.WindowInteropHelper(_mainWindow).Handle}.");
    }

    private void ShowSettings()
    {
        if (_settings is null || _settingsService is null)
        {
            return;
        }

        ShowMainWindow();
        _mainWindow?.ShowSettings(_settings, IsDev);
    }

    private void ApplySettings(AppSettings draft)
    {
        if (_settings is null || _settingsService is null)
        {
            return;
        }

        var previous = _settings;
        _settings = draft;
        _settings.Normalize();
        AppLog.Info($"Applying settings: path={_settingsService.SettingsPath}, delay={_settings.DelaySeconds}, preview={_settings.PreviewSeconds}, clipboard={_settings.ClipboardClearSeconds}.");

        try
        {
            _settingsService.Save(_settings);
            _tray?.UpdateDelay(_settings.DelaySeconds);
        }
        catch (Exception exception)
        {
            AppLog.Error("Could not apply settings", exception);
            _settings = previous;
            _tray?.UpdateDelay(_settings.DelaySeconds);
            _tray?.ShowWarning("Settings", "Could not save the new settings.");
        }

        if (!IsDev && _settings is not null)
        {
            try
            {
                StartupService.SetEnabled(_settings.StartWithWindows);
            }
            catch (Exception exception)
            {
                AppLog.Error("Could not update startup setting", exception);
                _tray?.ShowWarning("Startup setting", "Could not update the Windows startup entry.");
            }
        }
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        AppLog.Error("Dispatcher exception", e.Exception);
        e.Handled = true;
        MessageBox.Show(
            $"Something went wrong.\n\n{e.Exception.Message}",
            "Snipt",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_exiting)
        {
            base.OnExit(e);
            return;
        }

        _exiting = true;
        _tray?.Dispose();
        _hotkey?.Dispose();
        _clipboard?.Dispose();
        _capture?.Dispose();
        _showWindowListener?.Cancel();
        _showWindowEvent?.Dispose();
        _showWindowListener?.Dispose();

        if (_ownsMutex)
        {
            try
            {
                _singleInstance?.ReleaseMutex();
            }
            catch (ApplicationException)
            {
                // The mutex was already released.
            }
        }

        _singleInstance?.Dispose();
        base.OnExit(e);
    }
}
