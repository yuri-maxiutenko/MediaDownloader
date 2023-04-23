using System.Globalization;
using System.Text.RegularExpressions;

using MediaDownloader.Properties;

namespace MediaDownloader.Utilities;

public static class DownloadOutputParser
{
    public static bool TryParseDownloadProgress(string record, out double percent)
    {
        percent = 0;

        var regex = new Regex(Resources.SearchPatternDownloadProgress, RegexOptions.Compiled | RegexOptions.IgnoreCase);
        var matches = regex.Match(record);

        if (matches.Success && matches.Groups.Count >= 3)
        {
            return double.TryParse(matches.Groups[2].ToString(), NumberStyles.Number, CultureInfo.InvariantCulture,
                out percent);
        }

        return false;
    }

    public static bool TryParseResultFilePath(string record, out string path)
    {
        path = null;

        var regex = new Regex(Resources.SearchPatternResultFilePath, RegexOptions.Compiled | RegexOptions.IgnoreCase);
        var matches = regex.Match(record);

        if (!matches.Success || matches.Groups.Count < 2)
        {
            regex = new Regex(Resources.SearchPatternAlreadyDownloadedFilePath,
                RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.RightToLeft);
            matches = regex.Match(record);
            if (!matches.Success || matches.Groups.Count < 2)
            {
                return false;
            }
        }

        path = matches.Groups[1].ToString();
        return true;
    }
}