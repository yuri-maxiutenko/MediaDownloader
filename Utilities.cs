using System;
using System.IO;
using System.Text.RegularExpressions;

using YoutubeDownloader.Properties;

namespace YoutubeDownloader
{
    internal static class Utilities
    {
        public static bool IsValidUrl(string url)
        {
            return Uri.TryCreate(url, UriKind.Absolute, out var uriResult)
                   && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
        }

        public static bool TryParseDownloadProgress(string record, out double percent)
        {
            percent = 0;

            var regex = new Regex(Resources.SearchPatternDownloadProgress,
                RegexOptions.Compiled | RegexOptions.IgnoreCase);
            var matches = regex.Match(record);

            if (matches.Success && matches.Groups.Count >= 3)
            {
                double.TryParse(matches.Groups[2].ToString(), out percent);
                return true;
            }

            return false;
        }

        public static string SanitizeFileName(string fileName)
        {
            var regexSearch = new string(Path.GetInvalidFileNameChars());
            var regex = new Regex($"[{Regex.Escape(regexSearch)}]");
            return regex.Replace(fileName, "_");
        }

        public static int CalculateAbsolutePercent(int value, int maximum)
        {
            return (int)Math.Round(100 * (double)value / maximum);
        }
    }
}