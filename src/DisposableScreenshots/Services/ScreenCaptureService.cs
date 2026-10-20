using System.Drawing;
using System.Drawing.Imaging;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using DisposableScreenshots.Interop;
using DisposableScreenshots.Models;
using DrawingBitmap = System.Drawing.Bitmap;
using DrawingGraphics = System.Drawing.Graphics;
using DrawingSize = System.Drawing.Size;

namespace DisposableScreenshots.Services;

public sealed record PixelRect(int X, int Y, int Width, int Height)
{
    public int Right => X + Width;

    public int Bottom => Y + Height;
}

public sealed class DesktopSnapshot
{
    public DesktopSnapshot(BitmapSource image, VirtualScreen virtualScreen, IReadOnlyList<MonitorInfo> monitors)
    {
        Image = image;
        VirtualScreen = virtualScreen;
        Monitors = monitors;
    }

    public BitmapSource Image { get; }

    public VirtualScreen VirtualScreen { get; }

    public IReadOnlyList<MonitorInfo> Monitors { get; }

    public BitmapSource GetMonitorImage(MonitorInfo monitor) =>
        CropPhysical(new PixelRect(monitor.Left, monitor.Top, monitor.Width, monitor.Height));

    public BitmapSource CropPhysical(PixelRect rect)
    {
        var x = Math.Clamp(rect.X, VirtualScreen.Left, VirtualScreen.Right - 1);
        var y = Math.Clamp(rect.Y, VirtualScreen.Top, VirtualScreen.Bottom - 1);
        var width = Math.Clamp(rect.Width, 1, VirtualScreen.Right - x);
        var height = Math.Clamp(rect.Height, 1, VirtualScreen.Bottom - y);

        var cropped = new CroppedBitmap(
            Image,
            new Int32Rect(x - VirtualScreen.Left, y - VirtualScreen.Top, width, height));
        cropped.Freeze();
        return cropped;
    }
}

public sealed class ScreenCaptureService
{
    public DesktopSnapshot CaptureDesktop()
    {
        var virtualScreen = ScreenService.GetVirtualScreen();
        if (virtualScreen.Width <= 0 || virtualScreen.Height <= 0)
        {
            throw new InvalidOperationException("The desktop bounds could not be determined.");
        }

        using var bitmap = new DrawingBitmap(
            virtualScreen.Width,
            virtualScreen.Height,
            PixelFormat.Format32bppPArgb);

        using (var graphics = DrawingGraphics.FromImage(bitmap))
        {
            graphics.CopyFromScreen(
                virtualScreen.Left,
                virtualScreen.Top,
                0,
                0,
                new DrawingSize(virtualScreen.Width, virtualScreen.Height),
                CopyPixelOperation.SourceCopy);
        }

        return new DesktopSnapshot(ToBitmapSource(bitmap), virtualScreen, ScreenService.GetMonitors());
    }

    private static BitmapSource ToBitmapSource(DrawingBitmap bitmap)
    {
        var handle = bitmap.GetHbitmap();

        try
        {
            var source = Imaging.CreateBitmapSourceFromHBitmap(
                handle,
                IntPtr.Zero,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());
            source.Freeze();
            return source;
        }
        finally
        {
            NativeMethods.DeleteObject(handle);
        }
    }
}
