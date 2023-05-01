using System.Diagnostics;

using MediaDownloader.Download.Models;

namespace MediaDownloader.Download;

public interface IDownloader
{
    public Task<DownloadItem> GetItemsAsync(string link, DownloadFormatType downloadFormatType,
        DataReceivedEventHandler onErrorDataReceived, CancellationToken cancellationToken);

    public Task<bool> DownloadItemAsync(string downloadFilePath, string link, DownloadFormatType downloadFormatType,
        DataReceivedEventHandler onOutputReceived, DataReceivedEventHandler onErrorReceived,
        CancellationToken cancellationToken);

    public Task<bool> UpdateAsync(DataReceivedEventHandler onOutputReceived, DataReceivedEventHandler onErrorReceived,
        CancellationToken cancellationToken);
}