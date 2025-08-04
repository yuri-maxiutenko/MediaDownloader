using MediaDownloader.Download.Models;

namespace MediaDownloader.Models;

public class DownloadOption
{
    public DownloadFormatType FormatType { get; init; }
    public required string Name { get; init; }

    public override string ToString()
    {
        return Name;
    }
}