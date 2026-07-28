using System.Text.Json;

using MediaDownloader.Download.Models;

namespace MediaDownloader.Download.Utilities;

internal static class DownloadItemMapper
{
    private const string UnknownItemName = "unknown";

    public static DownloadItem? Map(string json, string? link)
    {
        var info = JsonSerializer.Deserialize<DownloadItemJson>(json);
        if (info is null)
        {
            return null;
        }

        var name = info.Title ?? info.Id ?? UnknownItemName;
        var result = new DownloadItem
        {
            Name = DownloadHelper.SanitizeFileName(name),
            Entries = [],
            Url = string.Empty
        };

        if (info.Entries is { Length: > 0 })
        {
            result.Entries.AddRange(info.Entries
                .Where(item => !string.IsNullOrEmpty(item.WebpageUrl))
                .Select(item => new DownloadItem
                {
                    Name = Path.ChangeExtension(
                        DownloadHelper.SanitizeFileName(item.Title ?? item.Id ?? UnknownItemName), item.Ext),
                    Url = item.WebpageUrl!
                }));
        }
        else
        {
            result.Entries.Add(new DownloadItem
            {
                Name = Path.ChangeExtension(DownloadHelper.SanitizeFileName(name), info.Ext),
                Url = info.WebpageUrl ?? link ?? string.Empty
            });
        }

        return result;
    }
}
