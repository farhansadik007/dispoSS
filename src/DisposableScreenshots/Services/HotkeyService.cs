using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Interop;
using DisposableScreenshots.Interop;
using DisposableScreenshots.Models;

namespace DisposableScreenshots.Services;

public sealed class HotkeyService : IDisposable
{
    private const int HotkeyId = 0x5347;

    private HwndSource? _source;
    private bool _registered;

    public event EventHandler? Pressed;

    public bool Register(HotkeyDefinition hotkey)
    {
        EnsureSource();

        if (_source is null)
        {
            return false;
        }

        Unregister();

        var virtualKey = (uint)KeyInterop.VirtualKeyFromKey(hotkey.Key);
        _registered = NativeMethods.RegisterHotKey(
            _source.Handle,
            HotkeyId,
            hotkey.ToWin32Modifiers(),
            virtualKey);

        if (!_registered)
        {
            var error = Marshal.GetLastWin32Error();
            AppLog.Info($"RegisterHotKey({hotkey}) failed with error {error}.");
        }

        return _registered;
    }

    public void Unregister()
    {
        if (_source is null || !_registered)
        {
            return;
        }

        NativeMethods.UnregisterHotKey(_source.Handle, HotkeyId);
        _registered = false;
    }

    private void EnsureSource()
    {
        if (_source is not null)
        {
            return;
        }

        var parameters = new HwndSourceParameters("DisposableScreenshots.HotkeySink")
        {
            Width = 0,
            Height = 0,
            PositionX = 0,
            PositionY = 0,
            WindowStyle = 0,
            ExtendedWindowStyle = 0
        };

        _source = new HwndSource(parameters);
        _source.AddHook(WndProc);
    }

    private IntPtr WndProc(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (message == NativeMethods.WM_HOTKEY && wParam.ToInt32() == HotkeyId)
        {
            handled = true;
            Pressed?.Invoke(this, EventArgs.Empty);
        }

        return IntPtr.Zero;
    }

    public void Dispose()
    {
        Unregister();

        if (_source is not null)
        {
            _source.RemoveHook(WndProc);
            _source.Dispose();
            _source = null;
        }
    }
}
