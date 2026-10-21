namespace DisposableScreenshots;

internal static class MathCompat
{
    public static int Clamp(int value, int min, int max) =>
        value < min ? min : value > max ? max : value;

    public static double Clamp(double value, double min, double max) =>
        value < min ? min : value > max ? max : value;
}
