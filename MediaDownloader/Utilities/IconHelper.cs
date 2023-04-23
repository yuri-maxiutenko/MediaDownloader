namespace MediaDownloader.Utilities;

public static class IconHelper
{
    private const int DownloadIconCode = 0xE896;
    private const int StopDownloadIconCode = 0xE71A;

    public static string GetDownloadIcon(bool isDownloading)
    {
        var iconCode = isDownloading ? StopDownloadIconCode : DownloadIconCode;
        return char.ConvertFromUtf32(iconCode);
    }
}