using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace DisposableScreenshots.Services;

public sealed class ClipboardService : IDisposable
{
    private const string MarkerFormat = "dispoSS.Image";

    private DispatcherTimer? _clearTimer;

    public bool Copy(BitmapSource image, int clearAfterSeconds)
    {
        var data = new DataObject();
        data.SetImage(image);
        data.SetData(MarkerFormat, "1");

        for (var attempt = 0; attempt < 6; attempt++)
        {
            try
            {
                Clipboard.SetDataObject(data, true);
                ScheduleClear(clearAfterSeconds);
                return true;
            }
            catch (ExternalException)
            {
                Thread.Sleep(40);
            }
        }

        AppLog.Info("Could not copy screenshot to the clipboard.");
        return false;
    }

    public bool ClearIfOwned()
    {
        try
        {
            if (!Clipboard.ContainsData(MarkerFormat))
            {
                return false;
            }

            Clipboard.Clear();
            return true;
        }
        catch (ExternalException)
        {
            return false;
        }
    }

    private void ScheduleClear(int clearAfterSeconds)
    {
        _clearTimer?.Stop();
        _clearTimer = null;

        if (clearAfterSeconds <= 0)
        {
            return;
        }

        _clearTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(clearAfterSeconds)
        };
        _clearTimer.Tick += (_, _) =>
        {
            _clearTimer?.Stop();
            ClearIfOwned();
        };
        _clearTimer.Start();
    }

    public void Dispose()
    {
        _clearTimer?.Stop();
        _clearTimer = null;
    }
}
