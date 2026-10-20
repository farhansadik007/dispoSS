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

    public event EventHandler<AppSettings>? SettingsSaveRequested;

    public event EventHandler? ExitRequested;

    public void ShowSettings(AppSettings settings, bool isDev)
    {
        if (_settingsOpen)
        {
            return;
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

    private void HideButton_Click(object sender, RoutedEventArgs e) => Hide();

    private void ExitButton_Click(object sender, RoutedEventArgs e) =>
        ExitRequested?.Invoke(this, EventArgs.Empty);

    private void SettingsPanel_SaveRequested(object sender, AppSettings settings)
    {
        CloseSettings();
        SettingsSaveRequested?.Invoke(this, settings);
    }

    private void SettingsPanel_CancelRequested(object sender, EventArgs e) => CloseSettings();

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (_settingsOpen && e.Key == Key.Escape)
        {
            e.Handled = true;
            CloseSettings();
        }
    }

    private void CloseSettings()
    {
        if (!_settingsOpen)
        {
            return;
        }

        _settingsOpen = false;
        AnimateViews();
    }

    private void AnimateViews()
    {
        var width = Math.Max(1, ViewHost.ActualWidth);
        var ease = new QuadraticEase { EasingMode = EasingMode.EaseOut };
        var captureTarget = _settingsOpen ? -width : 0;
        var settingsTarget = _settingsOpen ? 0 : width;
        var captureOpacity = _settingsOpen ? 0 : 1;
        var settingsOpacity = _settingsOpen ? 1 : 0;

        var settingsAnimation = new DoubleAnimation(settingsTarget, TransitionDuration)
        {
            EasingFunction = ease
        };
        settingsAnimation.Completed += (_, _) => OnTransitionCompleted();

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
    }

    private void OnTransitionCompleted()
    {
        if (_settingsOpen)
        {
            SettingsView.Visibility = Visibility.Visible;
            SettingsPanel.Focus();
            return;
        }

        SettingsView.Visibility = Visibility.Collapsed;
        SettingsView.BeginAnimation(OpacityProperty, null);
        SettingsView.Opacity = 0;
        SettingsTranslate.BeginAnimation(TranslateTransform.XProperty, null);
        SettingsTranslate.X = 0;

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
