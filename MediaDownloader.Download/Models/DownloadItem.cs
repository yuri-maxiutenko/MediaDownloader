namespace MediaDownloader.Download.Models;

public class DownloadItem
{
    public required string Name { get; init; }
    public required string Url { get; init; }
    public List<DownloadItem> Entries { get; init; } = [];

    public override string ToString()
    {
        return $"FileName={Name} Url={Url}";
    }
}