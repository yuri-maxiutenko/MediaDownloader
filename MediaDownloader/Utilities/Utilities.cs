using MediaDownloader.Download.Utilities;

namespace MediaDownloader.Utilities;

internal static class Utilities
{
    public static bool IsValidUrl(string? url)
    {
        return UrlValidator.IsValidHttpUrl(url);
    }

    public static int CalculateAbsolutePercent(int value, int maximum)
    {
        return (int)Math.Round(100 * (double)value / maximum);
    }
}