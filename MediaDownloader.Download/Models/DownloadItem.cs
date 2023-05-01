namespace MediaDownloader.Download.Models;

public class DownloadItem
{
    public string Name { get; set; }
    public string Url { get; set; }
    public List<DownloadItem> Entries { get; set; }

    public override string ToString()
    {
        return $"FileName={Name} Url={Url}";
    }
}