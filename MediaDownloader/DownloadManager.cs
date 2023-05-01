using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using MediaDownloader.Download;
using MediaDownloader.Download.Models;
using MediaDownloader.Download.Utilities;
using MediaDownloader.Models;
using MediaDownloader.Properties;

using Serilog;

namespace MediaDownloader;

public class DownloadManager : IDownloadManager
{
    private const int DownloadRetriesNumber = 2;
    private const double ProgressValueMax = 100.0;

    private readonly IDownloader _downloader;
    private double _downloadProgressSectionMin;
    private double _downloadProgressSectionStep;

    private IProgress<ProgressReportModel> _progress;

    public DownloadManager(IDownloader downloader)
    {
        _downloader = downloader;
    }

    public async Task<ICollection<DownloadedItemInfo>> DownloadItemAsync(string downloadUrl, string downloadFolderPath,
        DownloadFormatType formatType, IProgress<ProgressReportModel> progress, CancellationToken cancellationToken)
    {
        _progress = progress;

        var retryCounter = 0;

        DownloadItem item = null;
        while (item is null && retryCounter < DownloadRetriesNumber)
        {
            item = await GetItemAsync(downloadUrl, formatType, cancellationToken);
            retryCounter++;
        }

        if (item is null)
        {
            return null;
        }

        _downloadProgressSectionMin = 0;
        _downloadProgressSectionStep = ProgressValueMax / item.Entries.Count;

        if (item.Entries.Count > 1)
        {
            downloadFolderPath = Path.Combine(downloadFolderPath, item.Name);
            Directory.CreateDirectory(downloadFolderPath);
        }

        var result = new List<DownloadedItemInfo>();
        foreach (var entry in item.Entries)
        {
            var downloadPath = Path.Combine(downloadFolderPath, entry.Name);
            var downloadedItemInfo = new DownloadedItemInfo
            {
                Status = DownloadStatus.Unknown, Name = entry.Name, Url = entry.Url, Path = downloadPath
            };
            try
            {
                _progress.Report(new ProgressReportModel
                {
                    Message = $"{Resources.MessageDownloading} {entry.Name}"
                });

                Log.Information("{DownloadingMessage} {Entry}", Resources.MessageDownloading, entry);

                _progress.Report(new ProgressReportModel
                {
                    Message = string.Format(Resources.LogMessageDownloadingFile, downloadPath, entry.Url)
                });

                var success = false;
                retryCounter = 0;
                while (!success && retryCounter < DownloadRetriesNumber)
                {
                    success = await DownloadItemAsync(entry.Url, downloadPath, formatType, cancellationToken);
                    retryCounter++;
                }

                downloadedItemInfo.Status = success ? DownloadStatus.Success : DownloadStatus.Fail;
            }
            catch (OperationCanceledException e)
            {
                Log.Warning(e, "Download cancelled");
                _progress.Report(new ProgressReportModel
                {
                    Message = e.Message
                });
                downloadedItemInfo.Status = DownloadStatus.Cancel;
                return result;
            }
            catch (Exception e)
            {
                Log.Error(e, "Failed to download item");
                _progress.Report(new ProgressReportModel
                {
                    Message = e.Message
                });
                downloadedItemInfo.Status = DownloadStatus.Fail;
            }
            finally
            {
                result.Add(downloadedItemInfo);
                _downloadProgressSectionMin += _downloadProgressSectionStep;
            }
        }

        return result;
    }

    public async Task<bool> UpdateDownloaderAsync(IProgress<ProgressReportModel> progress,
        CancellationToken cancellationToken)
    {
        _progress = progress;
        return await _downloader.UpdateAsync(ProcessDownloaderOutput, ProcessDownloaderError, cancellationToken);
    }

    private async Task<DownloadItem> GetItemAsync(string downloadUrl, DownloadFormatType formatType,
        CancellationToken cancellationToken)
    {
        return await _downloader.GetItemsAsync(downloadUrl, formatType, ProcessDownloaderError, cancellationToken);
    }

    private async Task<bool> DownloadItemAsync(string downloadUrl, string downloadPath, DownloadFormatType formatType,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var success = await _downloader.DownloadItemAsync(downloadPath, downloadUrl, formatType,
            ProcessDownloaderOutput, ProcessDownloaderError, cancellationToken);

        return success;
    }

    private void ProcessDownloaderOutput(object sender, DataReceivedEventArgs e)
    {
        try
        {
            var record = e.Data;
            if (string.IsNullOrEmpty(record))
            {
                return;
            }

            var reportModel = new ProgressReportModel
            {
                Message = record
            };

            if (DownloadOutputParser.TryParseDownloadProgress(record, out var progress))
            {
                var newValue = _downloadProgressSectionMin + progress * _downloadProgressSectionStep / ProgressValueMax;
                reportModel.Value = newValue;
            }

            Log.Information("{Record}", record);
            _progress.Report(reportModel);
        }
        catch (Exception exception)
        {
            Log.Error(exception, "Failed to process download output");
        }
    }

    private void ProcessDownloaderError(object sender, DataReceivedEventArgs e)
    {
        try
        {
            var record = e.Data;
            if (string.IsNullOrEmpty(record))
            {
                return;
            }

            _progress.Report(new ProgressReportModel
            {
                Message = record
            });
            Log.Information("{Record}", record);
        }
        catch (Exception exception)
        {
            Log.Error(exception, "Failed to process download error");
        }
    }
}