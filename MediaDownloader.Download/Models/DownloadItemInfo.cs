namespace MediaDownloader.Download.Models;

public class DownloadedItemInfo
{
    public DownloadStatus Status { get; set; }
    public required string Name { get; init; }
    public required string Url { get; init; }
    public required string Path { get; init; }
}