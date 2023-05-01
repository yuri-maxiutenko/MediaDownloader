namespace MediaDownloader.Download.Models;

public class DownloadedItemInfo
{
    public DownloadStatus Status { get; set; }
    public string Name { get; set; }
    public string Url { get; set; }
    public string Path { get; set; }
}