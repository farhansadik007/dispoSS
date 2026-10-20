namespace DisposableScreenshots.Models;

public sealed class AppSettings
{
    public string Hotkey { get; set; } = "Ctrl+Alt+S";

    public int DelaySeconds { get; set; } = 3;

    public bool CopyToClipboard { get; set; } = true;

    public bool ShowPreview { get; set; } = true;

    public int PreviewSeconds { get; set; } = 8;

    public int ClipboardClearSeconds { get; set; }

    public bool StartWithWindows { get; set; }

    public bool ShowFirstRunTip { get; set; } = true;

    public AppSettings Clone() => (AppSettings)MemberwiseClone();

    public void Normalize()
    {
        DelaySeconds = Math.Clamp(DelaySeconds, 0, 60);
        PreviewSeconds = Math.Clamp(PreviewSeconds, 2, 60);
        ClipboardClearSeconds = Math.Clamp(ClipboardClearSeconds, 0, 3600);

        if (!HotkeyDefinition.TryParse(Hotkey, out var hotkey))
        {
            Hotkey = "Ctrl+Alt+S";
        }
        else
        {
            Hotkey = hotkey.ToString();
        }
    }
}
