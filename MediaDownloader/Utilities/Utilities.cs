using System;

namespace MediaDownloader.Utilities;

internal static class Utilities
{
    public static bool IsValidUrl(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uriResult) &&
               (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
    }

    public static int CalculateAbsolutePercent(int value, int maximum)
    {
        return (int)Math.Round(100 * (double)value / maximum);
    }
}