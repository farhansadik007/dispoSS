using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using DisposableScreenshots.Models;
using H.NotifyIcon;
using H.NotifyIcon.Core;

namespace DisposableScreenshots.Services;

public sealed class TrayService : IDisposable
{
    private readonly TaskbarIcon _icon;
    private readonly Dictionary<int, MenuItem> _delayItems = new();
    private int _selectedDelay;

    public TrayService(AppSettings settings)
    {
        _selectedDelay = settings.DelaySeconds;
        _icon = new TaskbarIcon
        {
            ToolTipText = "Snipt - double-click to open",
            Icon = LoadIcon(),
            MenuActivation = PopupActivationMode.RightClick,
            ContextMenu = BuildMenu(settings)
        };

        _icon.TrayLeftMouseDoubleClick += (_, _) => OpenRequested?.Invoke(this, EventArgs.Empty);
        _icon.ForceCreate();
    }

    public event EventHandler? OpenRequested;

    public event EventHandler? CaptureRegionRequested;

    public event EventHandler? CaptureFullScreenRequested;

    public event EventHandler? SettingsRequested;

    public event EventHandler? ExitRequested;

    public event EventHandler<int>? DelayChanged;

    public void UpdateDelay(int seconds)
    {
        _selectedDelay = seconds;

        foreach (var pair in _delayItems)
        {
            pair.Value.IsChecked = pair.Key == seconds;
        }
    }

    public void ShowInfo(string title, string message) => ShowNotification(title, message, NotificationIcon.Info);

    public void ShowWarning(string title, string message) => ShowNotification(title, message, NotificationIcon.Warning);

    private void ShowNotification(string title, string message, NotificationIcon icon)
    {
        try
        {
            _icon.ShowNotification(
                title,
                message,
                icon,
                null,
                false,
                false,
                false,
                false,
                TimeSpan.FromSeconds(5));
        }
        catch (Exception exception)
        {
            AppLog.Error("Could not show tray notification", exception);
        }
    }

    private ContextMenu BuildMenu(AppSettings settings)
    {
        _delayItems.Clear();

        var menu = new ContextMenu();
        var hotkey = HotkeyDefinition.Parse(settings.Hotkey);

        var openItem = new MenuItem
        {
            Header = "Open Snipt",
            FontWeight = FontWeights.SemiBold
        };
        openItem.Click += (_, _) => OpenRequested?.Invoke(this, EventArgs.Empty);
        menu.Items.Add(openItem);
        menu.Items.Add(new Separator());

        var captureRegion = new MenuItem
        {
            Header = "Capture region",
            InputGestureText = hotkey.ToString()
        };
        captureRegion.Click += (_, _) => CaptureRegionRequested?.Invoke(this, EventArgs.Empty);
        menu.Items.Add(captureRegion);

        var captureFullScreen = new MenuItem
        {
            Header = "Capture full screen"
        };
        captureFullScreen.Click += (_, _) => CaptureFullScreenRequested?.Invoke(this, EventArgs.Empty);
        menu.Items.Add(captureFullScreen);

        var delayMenu = new MenuItem { Header = "Delay" };

        foreach (var seconds in new[] { 0, 3, 5, 10, 15 })
        {
            var item = new MenuItem
            {
                Header = seconds == 0 ? "Instant" : $"{seconds} seconds",
                IsCheckable = true,
                IsChecked = seconds == _selectedDelay
            };
            item.Click += (_, _) =>
            {
                UpdateDelay(seconds);
                DelayChanged?.Invoke(this, seconds);
            };
            _delayItems[seconds] = item;
            delayMenu.Items.Add(item);
        }

        menu.Items.Add(delayMenu);
        menu.Items.Add(new Separator());

        var settingsItem = new MenuItem { Header = "Settings..." };
        settingsItem.Click += (_, _) => SettingsRequested?.Invoke(this, EventArgs.Empty);
        menu.Items.Add(settingsItem);
        menu.Items.Add(new Separator());

        var exitItem = new MenuItem { Header = "Exit" };
        exitItem.Click += (_, _) => ExitRequested?.Invoke(this, EventArgs.Empty);
        menu.Items.Add(exitItem);

        menu.Style = Application.Current.TryFindResource("RetroTrayMenu") as Style;
        ApplyTrayItemStyles(menu.Items);

        return menu;
    }

    private static void ApplyTrayItemStyles(ItemCollection items)
    {
        var itemStyle = Application.Current.TryFindResource("RetroTrayMenuItem") as Style;
        var separatorStyle = Application.Current.TryFindResource("RetroTraySeparator") as Style;

        foreach (var item in items)
        {
            if (item is MenuItem menuItem)
            {
                if (itemStyle is not null)
                {
                    menuItem.Style = itemStyle;
                }

                ApplyTrayItemStyles(menuItem.Items);
            }
            else if (item is Separator separator && separatorStyle is not null)
            {
                separator.Style = separatorStyle;
            }
        }
    }

    private static Icon LoadIcon()
    {
        try
        {
            var resource = Application.GetResourceStream(new Uri("pack://application:,,,/Assets/app.ico"));
            if (resource is not null)
            {
                return new Icon(resource.Stream);
            }
        }
        catch (Exception exception)
        {
            AppLog.Error("Could not load tray icon", exception);
        }

        return SystemIcons.Application;
    }

    public void Dispose()
    {
        _icon.Dispose();
    }
}
