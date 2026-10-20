namespace DisposableScreenshots.Models;

public sealed record MonitorInfo(
    string DeviceName,
    int Left,
    int Top,
    int Width,
    int Height,
    double ScaleX,
    double ScaleY)
{
    public int Right => Left + Width;

    public int Bottom => Top + Height;

    public bool Contains(int x, int y) => x >= Left && x < Right && y >= Top && y < Bottom;

    public override string ToString() => $"{DeviceName} {Width}x{Height} at {Left},{Top} ({ScaleX:0.##}x)";
}

public sealed record VirtualScreen(int Left, int Top, int Width, int Height)
{
    public int Right => Left + Width;

    public int Bottom => Top + Height;

    public bool Contains(int x, int y) => x >= Left && x < Right && y >= Top && y < Bottom;
}
