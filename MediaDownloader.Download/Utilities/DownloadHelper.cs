using System.Text.RegularExpressions;

namespace MediaDownloader.Download.Utilities;

public static class DownloadHelper
{
    // Path.GetInvalidFileNameChars() is runtime data, so the pattern cannot be a
    // [GeneratedRegex] compile-time constant; build it once instead of per call.
    private static readonly Regex InvalidFileNameChars =
        new($"[{Regex.Escape(new string(Path.GetInvalidFileNameChars()))}]", RegexOptions.Compiled);

    public static string SanitizeFileName(string fileName)
    {
        return InvalidFileNameChars.Replace(fileName, "_");
    }
}
