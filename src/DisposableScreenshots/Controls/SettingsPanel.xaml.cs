using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using DisposableScreenshots.Models;

namespace DisposableScreenshots.Controls;

public partial class SettingsPanel : UserControl
{
    private static readonly int[] DefaultDelayOptions = { 0, 3, 5, 10, 15 };
    private static readonly int[] DefaultPreviewOptions = { 3, 5, 8, 10, 15, 30 };
    private static readonly int[] DefaultClipboardOptions = { 0, 30, 60, 120, 300 };

    private AppSettings? _draft;
    private List<int> _delayOptions = new();
    private List<int> _previewOptions = new();
    private List<int> _clipboardOptions = new();
    private int _delayIndex;
    private int _previewIndex;
    private int _clipboardIndex;

    public SettingsPanel()
    {
        InitializeComponent();
    }

    public event EventHandler<AppSettings>? SaveRequested;

    public event EventHandler? CancelRequested;

    public void Load(AppSettings settings, bool isDev)
    {
        _draft = settings.Clone();
        HotkeyText.Text = HotkeyDefinition.Parse(settings.Hotkey).ToString().ToUpperInvariant();

        _delayOptions = BuildOptions(settings.DelaySeconds, DefaultDelayOptions);
        _previewOptions = BuildOptions(settings.PreviewSeconds, DefaultPreviewOptions);
        _clipboardOptions = BuildOptions(settings.ClipboardClearSeconds, DefaultClipboardOptions);
        _delayIndex = _delayOptions.IndexOf(settings.DelaySeconds);
        _previewIndex = _previewOptions.IndexOf(settings.PreviewSeconds);
        _clipboardIndex = _clipboardOptions.IndexOf(settings.ClipboardClearSeconds);

        UpdateDelayText();
        UpdatePreviewText();
        UpdateClipboardText();

        CopyCheckBox.IsChecked = settings.CopyToClipboard;
        ShowPreviewCheckBox.IsChecked = settings.ShowPreview;
        PreviewStepper.IsEnabled = settings.ShowPreview;

        StartupCheckBox.IsChecked = settings.StartWithWindows && !isDev;
        StartupCheckBox.IsEnabled = !isDev;
        DevNote.Visibility = isDev ? Visibility.Visible : Visibility.Collapsed;

        var version = Assembly.GetExecutingAssembly().GetName().Version;
        VersionText.Text = version is null ? "dev build" : $"v{version.Major}.{version.Minor}.{version.Build}";
    }

    private static List<int> BuildOptions(int current, IEnumerable<int> defaults)
    {
        var options = defaults.ToList();
        if (!options.Contains(current))
        {
            options.Add(current);
            options.Sort();
        }

        return options;
    }

    private static string FormatSeconds(int value) =>
        value == 0 ? "OFF" : $"{value}S";

    private static string FormatClipboard(int value)
    {
        if (value == 0)
        {
            return "OFF";
        }

        if (value < 60)
        {
            return $"{value}S";
        }

        return value % 60 == 0 ? $"{value / 60}M" : $"{value}S";
    }

    private void DelayPrevious_Click(object sender, RoutedEventArgs e) => MoveDelay(-1);

    private void DelayNext_Click(object sender, RoutedEventArgs e) => MoveDelay(1);

    private void PreviewPrevious_Click(object sender, RoutedEventArgs e) => MovePreview(-1);

    private void PreviewNext_Click(object sender, RoutedEventArgs e) => MovePreview(1);

    private void ClipboardPrevious_Click(object sender, RoutedEventArgs e) => MoveClipboard(-1);

    private void ClipboardNext_Click(object sender, RoutedEventArgs e) => MoveClipboard(1);

    private void MoveDelay(int delta)
    {
        _delayIndex = Wrap(_delayIndex + delta, _delayOptions.Count);
        UpdateDelayText();
    }

    private void MovePreview(int delta)
    {
        _previewIndex = Wrap(_previewIndex + delta, _previewOptions.Count);
        UpdatePreviewText();
    }

    private void MoveClipboard(int delta)
    {
        _clipboardIndex = Wrap(_clipboardIndex + delta, _clipboardOptions.Count);
        UpdateClipboardText();
    }

    private static int Wrap(int index, int count) =>
        count == 0 ? 0 : (index % count + count) % count;

    private void UpdateDelayText() =>
        DelayValueText.Text = FormatSeconds(_delayOptions[_delayIndex]);

    private void UpdatePreviewText() =>
        PreviewValueText.Text = FormatSeconds(_previewOptions[_previewIndex]);

    private void UpdateClipboardText() =>
        ClipboardValueText.Text = FormatClipboard(_clipboardOptions[_clipboardIndex]);

    private void ShowPreviewCheckBox_Checked(object sender, RoutedEventArgs e)
    {
        if (PreviewStepper is not null)
        {
            PreviewStepper.IsEnabled = true;
        }
    }

    private void ShowPreviewCheckBox_Unchecked(object sender, RoutedEventArgs e)
    {
        if (PreviewStepper is not null)
        {
            PreviewStepper.IsEnabled = false;
        }
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (_draft is null)
        {
            return;
        }

        _draft.DelaySeconds = _delayOptions[_delayIndex];
        _draft.PreviewSeconds = _previewOptions[_previewIndex];
        _draft.ClipboardClearSeconds = _clipboardOptions[_clipboardIndex];
        _draft.CopyToClipboard = CopyCheckBox.IsChecked == true;
        _draft.ShowPreview = ShowPreviewCheckBox.IsChecked == true;
        _draft.StartWithWindows = StartupCheckBox.IsChecked == true;
        _draft.Normalize();

        SaveRequested?.Invoke(this, _draft);
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e) =>
        CancelRequested?.Invoke(this, EventArgs.Empty);
}
