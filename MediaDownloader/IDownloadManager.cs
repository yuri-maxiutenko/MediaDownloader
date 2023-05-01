using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using MediaDownloader.Download.Models;
using MediaDownloader.Models;

namespace MediaDownloader;

public interface IDownloadManager
{
    public Task<ICollection<DownloadedItemInfo>> DownloadItemAsync(string downloadUrl, string downloadFolderPath,
        DownloadFormatType formatType, IProgress<ProgressReportModel> progress, CancellationToken cancellationToken);

    public Task<bool> UpdateDownloaderAsync(IProgress<ProgressReportModel> progress,
        CancellationToken cancellationToken);
}