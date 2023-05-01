using System.Text.RegularExpressions;

namespace MediaDownloader.Download;

public static class Utilities
{
    public static string SanitizeFileName(string fileName)
    {
        var regexSearch = new string(Path.GetInvalidFileNameChars());
        var regex = new Regex($"[{Regex.Escape(regexSearch)}]");
        return regex.Replace(fileName, "_");
    }
}