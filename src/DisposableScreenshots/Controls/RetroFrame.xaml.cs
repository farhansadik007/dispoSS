using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DisposableScreenshots.Controls;

public class RetroFrame : ContentControl
{
    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
        nameof(Title),
        typeof(string),
        typeof(RetroFrame),
        new PropertyMetadata("WINDOW"));

    public static readonly DependencyProperty ShowSettingsButtonProperty = DependencyProperty.Register(
        nameof(ShowSettingsButton),
        typeof(bool),
        typeof(RetroFrame),
        new PropertyMetadata(true, OnShowSettingsButtonChanged));

    private FrameworkElement? _titleBar;
    private Button? _minimizeButton;
    private Button? _settingsButton;
    private Button? _closeButton;

    public event RoutedEventHandler? SettingsRequested;

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public bool ShowSettingsButton
    {
        get => (bool)GetValue(ShowSettingsButtonProperty);
        set => SetValue(ShowSettingsButtonProperty, value);
    }

    public override void OnApplyTemplate()
    {
        DetachHandlers();
        base.OnApplyTemplate();

        _titleBar = GetTemplateChild("PART_TitleBar") as FrameworkElement;
        _minimizeButton = GetTemplateChild("PART_MinimizeButton") as Button;
        _settingsButton = GetTemplateChild("PART_SettingsButton") as Button;
        _closeButton = GetTemplateChild("PART_CloseButton") as Button;

        if (_titleBar is not null)
        {
            _titleBar.MouseLeftButtonDown += TitleBar_MouseLeftButtonDown;
        }

        if (_minimizeButton is not null)
        {
            _minimizeButton.Click += Minimize_Click;
        }

        if (_settingsButton is not null)
        {
            _settingsButton.Visibility = ShowSettingsButton ? Visibility.Visible : Visibility.Collapsed;
            _settingsButton.Click += Settings_Click;
        }

        if (_closeButton is not null)
        {
            _closeButton.Click += Close_Click;
        }
    }

    private static void OnShowSettingsButtonChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is RetroFrame { _settingsButton: not null } frame && e.NewValue is bool show)
        {
            frame._settingsButton.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private void DetachHandlers()
    {
        if (_titleBar is not null)
        {
            _titleBar.MouseLeftButtonDown -= TitleBar_MouseLeftButtonDown;
        }

        if (_minimizeButton is not null)
        {
            _minimizeButton.Click -= Minimize_Click;
        }

        if (_settingsButton is not null)
        {
            _settingsButton.Click -= Settings_Click;
        }

        if (_closeButton is not null)
        {
            _closeButton.Click -= Close_Click;
        }
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        try
        {
            Window.GetWindow(this)?.DragMove();
        }
        catch (InvalidOperationException)
        {
            // The window was closed while the drag was starting.
        }
    }

    private void Minimize_Click(object sender, RoutedEventArgs e)
    {
        if (Window.GetWindow(this) is { } window)
        {
            window.WindowState = WindowState.Minimized;
        }
    }

    private void Settings_Click(object sender, RoutedEventArgs e) =>
        SettingsRequested?.Invoke(this, new RoutedEventArgs());

    private void Close_Click(object sender, RoutedEventArgs e) =>
        Window.GetWindow(this)?.Close();
}
