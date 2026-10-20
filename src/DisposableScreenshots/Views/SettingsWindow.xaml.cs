using System.Reflection;
using System.Windows;
using System.Windows.Interop;
using DisposableScreenshots.Interop;
using DisposableScreenshots.Models;

namespace DisposableScreenshots.Views;

public partial class SettingsWindow : Window
{
    private readonly AppSettings _settings;

    public SettingsWindow(AppSettings settings, bool isDev)
    {
        InitializeComponent();

        _settings = settings;
        Title = isDev ? "Disposable Screenshots (dev)" : "Disposable Screenshots";
        HotkeyText.Text = HotkeyDefinition.Parse(settings.Hotkey).ToString();

        DelayComboBox.ItemsSource = BuildOptions(
            settings.DelaySeconds,
            (0, "Instant"),
            (3, "3 seconds"),
            (5, "5 seconds"),
            (10, "10 seconds"),
            (15, "15 seconds"));
        DelayComboBox.SelectedValue = settings.DelaySeconds;

        PreviewComboBox.ItemsSource = BuildOptions(
            settings.PreviewSeconds,
            (3, "3 seconds"),
            (5, "5 seconds"),
            (8, "8 seconds"),
            (10, "10 seconds"),
            (15, "15 seconds"),
            (30, "30 seconds"));
        PreviewComboBox.SelectedValue = settings.PreviewSeconds;

        ClipboardClearComboBox.ItemsSource = BuildOptions(
            settings.ClipboardClearSeconds,
            (0, "Never"),
            (30, "After 30 seconds"),
            (60, "After 1 minute"),
            (120, "After 2 minutes"),
            (300, "After 5 minutes"));
        ClipboardClearComboBox.SelectedValue = settings.ClipboardClearSeconds;

        CopyCheckBox.IsChecked = settings.CopyToClipboard;
        ShowPreviewCheckBox.IsChecked = settings.ShowPreview;
        PreviewComboBox.IsEnabled = settings.ShowPreview;
        ShowPreviewCheckBox.Checked += (_, _) => PreviewComboBox.IsEnabled = true;
        ShowPreviewCheckBox.Unchecked += (_, _) => PreviewComboBox.IsEnabled = false;

        StartupCheckBox.IsChecked = settings.StartWithWindows && !isDev;
        if (isDev)
        {
            StartupCheckBox.IsEnabled = false;
            DevNote.Visibility = Visibility.Visible;
        }

        var version = Assembly.GetExecutingAssembly().GetName().Version;
        VersionText.Text = version is null ? "dev build" : $"v{version.Major}.{version.Minor}.{version.Build}";
    }

    public bool? Result { get; private set; }

    private static List<KeyValuePair<int, string>> BuildOptions(int current, params (int Value, string Label)[] options)
    {
        var list = options
            .Select(option => new KeyValuePair<int, string>(option.Value, option.Label))
            .ToList();

        if (list.All(option => option.Key != current))
        {
            list.Add(new KeyValuePair<int, string>(current, $"{current} seconds"));
            list.Sort((left, right) => left.Key.CompareTo(right.Key));
        }

        return list;
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

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        _settings.DelaySeconds = DelayComboBox.SelectedValue is int delay ? delay : 3;
        _settings.PreviewSeconds = PreviewComboBox.SelectedValue is int preview ? preview : 8;
        _settings.ClipboardClearSeconds = ClipboardClearComboBox.SelectedValue is int clear ? clear : 0;
        _settings.CopyToClipboard = CopyCheckBox.IsChecked == true;
        _settings.ShowPreview = ShowPreviewCheckBox.IsChecked == true;
        _settings.StartWithWindows = StartupCheckBox.IsChecked == true;
        _settings.Normalize();

        Result = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        Result = false;
        Close();
    }
}
