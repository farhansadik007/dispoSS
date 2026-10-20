using System.Runtime.InteropServices;
using DisposableScreenshots.Interop;
using DisposableScreenshots.Models;

namespace DisposableScreenshots.Services;

public static class ScreenService
{
    public static VirtualScreen GetVirtualScreen() =>
        new(
            NativeMethods.GetSystemMetrics(NativeMethods.SM_XVIRTUALSCREEN),
            NativeMethods.GetSystemMetrics(NativeMethods.SM_YVIRTUALSCREEN),
            NativeMethods.GetSystemMetrics(NativeMethods.SM_CXVIRTUALSCREEN),
            NativeMethods.GetSystemMetrics(NativeMethods.SM_CYVIRTUALSCREEN));

    public static IReadOnlyList<MonitorInfo> GetMonitors()
    {
        var monitors = new List<MonitorInfo>();

        NativeMethods.MonitorEnumProc callback = (IntPtr hMonitor, IntPtr hdcMonitor, ref NativeMethods.RECT monitorRect, IntPtr data) =>
        {
            var info = new NativeMethods.MONITORINFOEX
            {
                cbSize = Marshal.SizeOf<NativeMethods.MONITORINFOEX>()
            };

            if (NativeMethods.GetMonitorInfo(hMonitor, ref info))
            {
                double scaleX = 1.0;
                double scaleY = 1.0;

                try
                {
                    if (NativeMethods.GetDpiForMonitor(
                            hMonitor,
                            NativeMethods.MDT_EFFECTIVE_DPI,
                            out var dpiX,
                            out var dpiY) == 0)
                    {
                        scaleX = dpiX / 96.0;
                        scaleY = dpiY / 96.0;
                    }
                }
                catch (DllNotFoundException)
                {
                    // Older systems without Shcore.dll fall back to 100%.
                }

                monitors.Add(new MonitorInfo(
                    info.szDevice,
                    info.rcMonitor.Left,
                    info.rcMonitor.Top,
                    info.rcMonitor.Width,
                    info.rcMonitor.Height,
                    scaleX,
                    scaleY));
            }

            return true;
        };

        NativeMethods.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, callback, IntPtr.Zero);
        GC.KeepAlive(callback);

        if (monitors.Count == 0)
        {
            var virtualScreen = GetVirtualScreen();
            monitors.Add(new MonitorInfo(
                "Primary",
                virtualScreen.Left,
                virtualScreen.Top,
                virtualScreen.Width,
                virtualScreen.Height,
                1.0,
                1.0));
        }

        return monitors
            .OrderBy(monitor => monitor.Left)
            .ThenBy(monitor => monitor.Top)
            .ToArray();
    }

    public static MonitorInfo GetMonitorAtCursor()
    {
        var monitors = GetMonitors();

        if (NativeMethods.GetCursorPos(out var point))
        {
            var match = monitors.FirstOrDefault(monitor => monitor.Contains(point.X, point.Y));
            if (match is not null)
            {
                return match;
            }
        }

        return monitors[0];
    }
}
