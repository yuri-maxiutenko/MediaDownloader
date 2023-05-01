using System.Text.RegularExpressions;

namespace MediaDownloader.Download.Utilities;

public static class DownloadHelper
{
    public static string SanitizeFileName(string fileName)
    {
        var regexSearch = new string(Path.GetInvalidFileNameChars());
        var regex = new Regex($"[{Regex.Escape(regexSearch)}]");
        return regex.Replace(fileName, "_");
    }
}