using System.Reflection;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using DisposableScreenshots.Interop;
using DisposableScreenshots.Models;
using DisposableScreenshots.Services;

namespace DisposableScreenshots.Views;

public partial class MainWindow : Window
{
    private static readonly Duration TransitionDuration = new(TimeSpan.FromMilliseconds(180));

    private bool _settingsOpen;
    private bool _aboutOpen;

    public MainWindow(AppSettings settings, bool isDev)
    {
        InitializeComponent();

        Title = isDev ? "dispoSS (dev)" : "dispoSS";
        HotkeyText.Text = HotkeyDefinition.Parse(settings.Hotkey).ToString();

        var version = Assembly.GetExecutingAssembly().GetName().Version;
        VersionText.Text = version is null ? "dev build" : $"v{version.Major}.{version.Minor}.{version.Build}";
    }

    public event EventHandler? CaptureRegionRequested;

    public event EventHandler? CaptureFullScreenRequested;

    public event EventHandler? SettingsRequested;

    public event EventHandler<AppSettings>? SettingsSaveRequested;

    public event EventHandler? HotkeyCaptureStarted;

    public event EventHandler? HotkeyCaptureFinished;

    public event EventHandler? ExitRequested;

    public void UpdateHotkey(string hotkey) =>
        HotkeyText.Text = HotkeyDefinition.Parse(hotkey).ToString();

    public void ShowSettings(AppSettings settings, bool isDev)
    {
        if (_settingsOpen)
        {
            return;
        }

        if (_aboutOpen)
        {
            _aboutOpen = false;
            AboutView.Visibility = Visibility.Collapsed;
            AboutTranslate.BeginAnimation(TranslateTransform.XProperty, null);
            AboutTranslate.X = 0;
        }

        AppLog.Info("Opening in-window settings.");
        SettingsPanel.Load(settings, isDev);
        _settingsOpen = true;

        SettingsView.Visibility = Visibility.Visible;
        SettingsView.Opacity = 0;
        SettingsTranslate.BeginAnimation(TranslateTransform.XProperty, null);
        SettingsTranslate.X = Math.Max(1, ViewHost.ActualWidth);

        CaptureTranslate.BeginAnimation(TranslateTransform.XProperty, null);
        CaptureTranslate.X = 0;
        CaptureView.Opacity = 1;
        CaptureView.IsHitTestVisible = false;

        AnimateViews();
    }

    private void CaptureRegion_Click(object sender, RoutedEventArgs e) =>
        CaptureRegionRequested?.Invoke(this, EventArgs.Empty);

    private void CaptureFullScreen_Click(object sender, RoutedEventArgs e) =>
        CaptureFullScreenRequested?.Invoke(this, EventArgs.Empty);

    private void SettingsButton_Click(object sender, RoutedEventArgs e) =>
        SettingsRequested?.Invoke(this, EventArgs.Empty);

    private void AboutButton_Click(object sender, RoutedEventArgs e) => ShowAbout();

    private void ShowAbout()
    {
        if (_aboutOpen || _settingsOpen)
        {
            return;
        }

        AppLog.Info("Opening in-window about.");
        _aboutOpen = true;

        AboutView.Visibility = Visibility.Visible;
        AboutView.Opacity = 0;
        AboutTranslate.BeginAnimation(TranslateTransform.XProperty, null);
        AboutTranslate.X = Math.Max(1, ViewHost.ActualWidth);

        CaptureTranslate.BeginAnimation(TranslateTransform.XProperty, null);
        CaptureTranslate.X = 0;
        CaptureView.Opacity = 1;
        CaptureView.IsHitTestVisible = false;

        AnimateViews();
    }

    private void AboutPanel_BackRequested(object sender, EventArgs e) => CloseAbout();

    private void CloseAbout()
    {
        if (!_aboutOpen)
        {
            return;
        }

        _aboutOpen = false;
        AnimateViews();
    }

    private void HideButton_Click(object sender, RoutedEventArgs e) => Hide();

    private void ExitButton_Click(object sender, RoutedEventArgs e) =>
        ExitRequested?.Invoke(this, EventArgs.Empty);

    private void SettingsPanel_SaveRequested(object sender, AppSettings settings)
    {
        CloseSettings();
        SettingsSaveRequested?.Invoke(this, settings);
    }

    private void SettingsPanel_CancelRequested(object sender, EventArgs e) => CloseSettings();

    private void SettingsPanel_HotkeyCaptureStarted(object sender, EventArgs e) =>
        HotkeyCaptureStarted?.Invoke(this, EventArgs.Empty);

    private void SettingsPanel_HotkeyCaptureFinished(object sender, EventArgs e) =>
        HotkeyCaptureFinished?.Invoke(this, EventArgs.Empty);

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape)
        {
            return;
        }

        if (_settingsOpen)
        {
            e.Handled = true;
            CloseSettings();
        }
        else if (_aboutOpen)
        {
            e.Handled = true;
            CloseAbout();
        }
    }

    private void CloseSettings()
    {
        if (!_settingsOpen)
        {
            return;
        }

        SettingsPanel.CancelHotkeyCapture();
        _settingsOpen = false;
        AnimateViews();
    }

    private void AnimateViews()
    {
        var width = Math.Max(1, ViewHost.ActualWidth);
        var ease = new QuadraticEase { EasingMode = EasingMode.EaseOut };
        var overlayOpen = _settingsOpen || _aboutOpen;
        var captureTarget = overlayOpen ? -width : 0;
        var settingsTarget = _settingsOpen ? 0 : width;
        var aboutTarget = _aboutOpen ? 0 : width;
        var captureOpacity = overlayOpen ? 0 : 1;
        var settingsOpacity = _settingsOpen ? 1 : 0;
        var aboutOpacity = _aboutOpen ? 1 : 0;

        var settingsAnimation = new DoubleAnimation(settingsTarget, TransitionDuration)
        {
            EasingFunction = ease
        };
        settingsAnimation.Completed += (_, _) => OnTransitionCompleted();

        var aboutAnimation = new DoubleAnimation(aboutTarget, TransitionDuration)
        {
            EasingFunction = ease
        };
        aboutAnimation.Completed += (_, _) => OnTransitionCompleted();

        CaptureTranslate.BeginAnimation(
            TranslateTransform.XProperty,
            new DoubleAnimation(captureTarget, TransitionDuration) { EasingFunction = ease });
        CaptureView.BeginAnimation(
            OpacityProperty,
            new DoubleAnimation(captureOpacity, TransitionDuration));
        SettingsTranslate.BeginAnimation(TranslateTransform.XProperty, settingsAnimation);
        SettingsView.BeginAnimation(
            OpacityProperty,
            new DoubleAnimation(settingsOpacity, TransitionDuration));
        AboutTranslate.BeginAnimation(TranslateTransform.XProperty, aboutAnimation);
        AboutView.BeginAnimation(
            OpacityProperty,
            new DoubleAnimation(aboutOpacity, TransitionDuration));
    }

    private void OnTransitionCompleted()
    {
        if (_settingsOpen)
        {
            SettingsView.Visibility = Visibility.Visible;
            SettingsPanel.Focus();
            return;
        }

        if (_aboutOpen)
        {
            AboutView.Visibility = Visibility.Visible;
            AboutPanel.Focus();
            return;
        }

        SettingsView.Visibility = Visibility.Collapsed;
        SettingsView.BeginAnimation(OpacityProperty, null);
        SettingsView.Opacity = 0;
        SettingsTranslate.BeginAnimation(TranslateTransform.XProperty, null);
        SettingsTranslate.X = 0;

        AboutView.Visibility = Visibility.Collapsed;
        AboutView.BeginAnimation(OpacityProperty, null);
        AboutView.Opacity = 0;
        AboutTranslate.BeginAnimation(TranslateTransform.XProperty, null);
        AboutTranslate.X = 0;

        CaptureView.BeginAnimation(OpacityProperty, null);
        CaptureView.Opacity = 1;
        CaptureTranslate.BeginAnimation(TranslateTransform.XProperty, null);
        CaptureTranslate.X = 0;
        CaptureView.IsHitTestVisible = true;
    }

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
