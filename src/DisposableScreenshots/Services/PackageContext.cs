using System.Runtime.InteropServices;
using System.Text;

namespace DisposableScreenshots.Services;

/// <summary>
/// Detects whether the process is running with MSIX package identity.
/// </summary>
public static class PackageContext
{
    private const int AppModelErrorNoPackage = 15700;

    public static bool IsPackaged { get; } = DetectIsPackaged();

    private static bool DetectIsPackaged()
    {
        uint length = 0;
        var result = GetCurrentPackageFullName(ref length, null);
        return result != AppModelErrorNoPackage;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetCurrentPackageFullName(ref uint packageFullNameLength, StringBuilder? packageFullName);
}
