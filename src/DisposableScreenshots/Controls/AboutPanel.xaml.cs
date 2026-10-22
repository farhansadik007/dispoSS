using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using DisposableScreenshots.Services;

namespace DisposableScreenshots.Controls;

public partial class AboutPanel : UserControl
{
    private const string SupportUrl = "https://www.supportkori.com/farhansadik007";
    private const string GithubUrl = "https://github.com/farhansadik007";
    private const string XUrl = "https://x.com/SadikZisan";
    private const string FacebookUrl = "https://www.facebook.com/FarhanSadikZisan/";
    private const string InstagramUrl = "https://www.instagram.com/_sadik.farhan_/";

    public AboutPanel()
    {
        InitializeComponent();

        var version = Assembly.GetExecutingAssembly().GetName().Version;
        VersionText.Text = version is null ? "dev build" : $"v{version.Major}.{version.Minor}.{version.Build}";
    }

    public event EventHandler? BackRequested;

    private void BackButton_Click(object sender, RoutedEventArgs e) =>
        BackRequested?.Invoke(this, EventArgs.Empty);

    private void SupportButton_Click(object sender, RoutedEventArgs e) => OpenUrl(SupportUrl);

    private void GithubButton_Click(object sender, RoutedEventArgs e) => OpenUrl(GithubUrl);

    private void XButton_Click(object sender, RoutedEventArgs e) => OpenUrl(XUrl);

    private void FacebookButton_Click(object sender, RoutedEventArgs e) => OpenUrl(FacebookUrl);

    private void InstagramButton_Click(object sender, RoutedEventArgs e) => OpenUrl(InstagramUrl);

    private static void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Exception exception)
        {
            AppLog.Error($"Could not open link {url}", exception);
        }
    }
}
