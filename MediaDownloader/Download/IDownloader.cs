using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

using MediaDownloader.Models;

namespace MediaDownloader.Download;

public interface IDownloader
{
    public Task<DownloadItem> GetItemsAsync(string link, string downloadOption,
        DataReceivedEventHandler onErrorDataReceived, CancellationToken cancellationToken);

    public Task<bool> DownloadItemAsync(string downloadFilePath, string link, string downloadOption,
        DataReceivedEventHandler onOutputReceived, DataReceivedEventHandler onErrorReceived,
        CancellationToken cancellationToken);

    public Task<bool> UpdateAsync(DataReceivedEventHandler onOutputReceived,
        DataReceivedEventHandler onErrorReceived, CancellationToken cancellationToken);
}