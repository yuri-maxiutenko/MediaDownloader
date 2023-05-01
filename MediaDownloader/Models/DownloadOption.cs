using MediaDownloader.Download.Models;

namespace MediaDownloader.Models;

public class DownloadOption
{
    public DownloadFormatType FormatType { get; set; }
    public string Name { get; set; }

    public override string ToString()
    {
        return Name;
    }
}