using System.Globalization;
using System.Text.RegularExpressions;

namespace MediaDownloader.Download.Utilities;

public static partial class DownloadOutputParser
{
    // yt-dlp prints "[Merger] Merging formats into ..."; youtube-dl used "[ffmpeg]".
    [GeneratedRegex(@"\[(?:ffmpeg|Merger)\]\s*Merging formats into\s*""(.*)""", RegexOptions.IgnoreCase)]
    private static partial Regex ResultFilePathRegex();

    // Modern yt-dlp omits the trailing "and merged".
    [GeneratedRegex(@"\[download\]\s* (.*?)\s*has already been downloaded(?: and merged)?",
        RegexOptions.IgnoreCase | RegexOptions.RightToLeft)]
    private static partial Regex AlreadyDownloadedFilePathRegex();

    [GeneratedRegex(@"(\[download\]\s*)(\S*)(\s*)% of", RegexOptions.IgnoreCase)]
    private static partial Regex DownloadProgressRegex();

    public static bool TryParseDownloadProgress(string record, out double percent)
    {
        percent = 0;

        var matches = DownloadProgressRegex().Match(record);

        if (matches is { Success: true, Groups.Count: >= 3 })
        {
            return double.TryParse(matches.Groups[2].ToString(), NumberStyles.Number, CultureInfo.InvariantCulture,
                out percent);
        }

        return false;
    }

    public static bool TryParseResultFilePath(string record, out string? path)
    {
        path = null;

        var matches = ResultFilePathRegex().Match(record);

        if (!matches.Success || matches.Groups.Count < 2)
        {
            matches = AlreadyDownloadedFilePathRegex().Match(record);
            if (!matches.Success || matches.Groups.Count < 2)
            {
                return false;
            }
        }

        path = matches.Groups[1].ToString();
        return true;
    }
}
