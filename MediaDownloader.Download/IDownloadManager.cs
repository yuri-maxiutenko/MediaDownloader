using MediaDownloader.Download.Models;

namespace MediaDownloader.Download;

public interface IDownloadManager
{
    public Task<ICollection<DownloadedItemInfo>> DownloadItemAsync(
        string? downloadUrl,
        string downloadFolderPath,
        DownloadFormatType formatType,
        IProgress<ProgressReportModel> progress,
        CancellationToken cancellationToken);

    public Task<bool> UpdateDownloaderAsync(
        IProgress<ProgressReportModel> progress,
        CancellationToken cancellationToken);
}
