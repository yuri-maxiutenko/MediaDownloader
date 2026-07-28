using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Data;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using MediaDownloader.Data.Models;
using MediaDownloader.Download;
using MediaDownloader.Download.Models;
using MediaDownloader.Models;
using MediaDownloader.Properties;
using MediaDownloader.Services;
using MediaDownloader.Utilities;

using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using DateTime = System.DateTime;
using Log = Serilog.Log;

namespace MediaDownloader.UI.ViewModels;

public sealed partial class MainWindowViewModel : ObservableObject
{
    private const double DownloadProgressMax = 100.0;

    private readonly IClipboardService _clipboard;
    private readonly StringBuilder _downloadLog = new();
    private readonly IDownloadManager _downloadManager;
    private readonly IDownloadFolderService _folderService;
    private readonly IHistoryService _historyService;
    private readonly IShellService _shell;

    private CancellationTokenSource? _cancellationTokenSource;
    private bool _isDownloadRunning;
    private string? _userVideosFolder;

    public MainWindowViewModel(
        IDownloadManager downloadManager,
        IHistoryService historyService,
        IDownloadFolderService folderService,
        IShellService shell,
        IClipboardService clipboard)
    {
        _downloadManager = downloadManager;
        _historyService = historyService;
        _folderService = folderService;
        _shell = shell;
        _clipboard = clipboard;

        LastDownloadedItem = new DownloadedItemInfo
        {
            Name = string.Empty,
            Url = string.Empty,
            Path = string.Empty
        };

        DownloadOptions =
        [
            new DownloadOption
            {
                FormatType = DownloadFormatType.Best,
                Name = Resources.DownloaderFormatBestName
            },
            new DownloadOption
            {
                FormatType = DownloadFormatType.BestMp4,
                Name = Resources.DownloaderFormatBestMp4Name
            },
            new DownloadOption
            {
                FormatType = DownloadFormatType.BestDirectLink,
                Name = Resources.DownloaderFormatBestDirectLinkName
            },
            new DownloadOption
            {
                FormatType = DownloadFormatType.AudioOnly,
                Name = Resources.DownloaderFormatAudioOnlyName
            }
        ];

        GeneralInterfaceIsEnabled = true;
        DownloadButtonIcon = IconHelper.GetDownloadIcon(false);
        DownloadButtonText = Resources.StartDownloadButtonText;
        DownloadProgressColor = Brushes.Gainsboro;
        ShowDownloadedItemsButtonIsEnabled = true;
        DownloadHistoryIsEnabled = true;

        ValidateDownload();
    }

    [ObservableProperty]
    public partial string? DownloadButtonIcon { get; set; }

    [ObservableProperty]
    public partial bool DownloadButtonIsEnabled { get; set; }

    [ObservableProperty]
    public partial bool ShowDownloadedItemsButtonIsEnabled { get; set; }

    [ObservableProperty]
    public partial bool GeneralInterfaceIsEnabled { get; set; }

    [ObservableProperty]
    public partial bool DownloadProgressIsIndeterminate { get; set; }

    [ObservableProperty]
    public partial bool DownloadHistoryIsEnabled { get; set; }

    [ObservableProperty]
    public partial Brush? DownloadProgressColor { get; set; }

    [ObservableProperty]
    public partial DownloadFolder? SelectedDownloadFolder { get; set; }

    [ObservableProperty]
    public partial DownloadOption? SelectedDownloadOption { get; set; }

    [ObservableProperty]
    public partial HistoryRecord? DownloadHistorySelectedItem { get; set; }

    [ObservableProperty]
    public partial double DownloadProgressValue { get; set; }

    [ObservableProperty]
    public partial string? MediaUrl { get; set; }

    [ObservableProperty]
    public partial string? DownloadButtonText { get; set; }

    [ObservableProperty]
    public partial string? DownloadMessage { get; set; }

    [ObservableProperty]
    public partial string? DownloadPercentText { get; set; }

    [ObservableProperty]
    public partial Visibility DownloadProgressVisibility { get; set; }

    public CollectionViewSource DownloadFolders => _folderService.FoldersView;

    public CollectionViewSource DownloadHistory => _historyService.HistoryView;

    public DownloadedItemInfo LastDownloadedItem { get; }

    public List<DownloadOption> DownloadOptions { get; }

    public string DownloadLog => _downloadLog.ToString();

    public string UserVideosFolder =>
        _userVideosFolder ??= Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);

    public async Task UpdateDownloaderAsync()
    {
        try
        {
            GeneralInterfaceIsEnabled = false;
            DownloadButtonIsEnabled = false;
            DownloadHistoryIsEnabled = false;
            DownloadProgressIsIndeterminate = true;
            ShowDownloadedItemsButtonIsEnabled = false;
            DownloadProgressVisibility = Visibility.Visible;
            DownloadProgressColor = Brushes.LimeGreen;
            DownloadMessage = Resources.MessageUpdatingDownloader;

            _cancellationTokenSource = new CancellationTokenSource();

            var progress = new Progress<ProgressReportModel>(HandleProgress);
            await _downloadManager.UpdateDownloaderAsync(progress, _cancellationTokenSource.Token);

            GeneralInterfaceIsEnabled = true;
            DownloadButtonIsEnabled = true;
            DownloadHistoryIsEnabled = true;
            ShowDownloadedItemsButtonIsEnabled = true;
            DownloadProgressIsIndeterminate = false;
            DownloadProgressColor = Brushes.Gainsboro;
            DownloadMessage = string.Empty;
        }
        catch (Exception e)
        {
            Log.Error(e, "Failed to update the downloader");
        }
    }

    public void ValidateDownload()
    {
        try
        {
            var downloadDirectoryExists = Directory.Exists(SelectedDownloadFolder?.Path);
            DownloadButtonIsEnabled = Utilities.Utilities.IsValidUrl(MediaUrl) && downloadDirectoryExists;
            ShowDownloadedItemsButtonIsEnabled = downloadDirectoryExists;
        }
        catch (Exception e)
        {
            Log.Error(e, "Failed to validate download");
        }
    }

    public void AddOrUpdateDownloadFolder(string path, DateTime lastSelectionDate)
    {
        _folderService.AddOrUpdate(path, lastSelectionDate);
    }

    partial void OnMediaUrlChanged(string? value)
    {
        ValidateDownload();
    }

    partial void OnSelectedDownloadFolderChanged(DownloadFolder? value)
    {
        ValidateDownload();
    }

    [RelayCommand]
    private void Clear()
    {
        MediaUrl = string.Empty;
    }

    [RelayCommand(AllowConcurrentExecutions = true)]
    private async Task DownloadOrStopAsync()
    {
        if (_isDownloadRunning)
        {
            DownloadButtonIsEnabled = false;
            _cancellationTokenSource?.Cancel();
            return;
        }

        await DownloadAsync();
    }

    [RelayCommand]
    private void ShowDownloadedItems()
    {
        if (File.Exists(LastDownloadedItem.Path) || Directory.Exists(LastDownloadedItem.Path))
        {
            _shell.OpenFileLocation(LastDownloadedItem.Path);
        }
        else if (SelectedDownloadFolder is { } folder && Directory.Exists(folder.Path))
        {
            _shell.OpenFolder(folder.Path);
        }
    }

    [RelayCommand]
    private void OpenHistoryItemFolder()
    {
        var path = DownloadHistorySelectedItem?.Path;
        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        if (File.Exists(path) || Directory.Exists(path))
        {
            _shell.OpenFileLocation(path);
        }
        else if (SelectedDownloadFolder is { } folder && Directory.Exists(folder.Path))
        {
            _shell.OpenFolder(folder.Path);
        }
    }

    [RelayCommand]
    private async Task ReDownloadAsync()
    {
        if (string.IsNullOrEmpty(DownloadHistorySelectedItem?.Url))
        {
            return;
        }

        MediaUrl = DownloadHistorySelectedItem.Url;
        await DownloadAsync();
    }

    [RelayCommand]
    private void CopyHistoryItemLink()
    {
        if (!string.IsNullOrEmpty(DownloadHistorySelectedItem?.Url))
        {
            _clipboard.SetText(DownloadHistorySelectedItem.Url);
        }
    }

    [RelayCommand]
    private void RemoveFromHistory()
    {
        if (DownloadHistorySelectedItem is { } record)
        {
            _historyService.Remove(record);
        }
    }

    [RelayCommand]
    private void ClearHistory()
    {
        _historyService.Clear();
    }

    private async Task DownloadAsync()
    {
        if (SelectedDownloadFolder is not { } downloadFolder || SelectedDownloadOption is not { } downloadOption)
        {
            return;
        }

        _isDownloadRunning = true;
        try
        {
            GeneralInterfaceIsEnabled = false;
            DownloadHistoryIsEnabled = false;

            if (DownloadFolders.View.CurrentItem is DownloadFolder currentFolder)
            {
                _folderService.Touch(currentFolder, DateTime.Now);
            }

            DownloadButtonIcon = IconHelper.GetDownloadIcon(true);
            DownloadButtonText = Resources.StopDownloadButtonText;
            DownloadButtonIsEnabled = true;

            DownloadProgressIsIndeterminate = true;
            ShowDownloadedItemsButtonIsEnabled = false;
            DownloadProgressVisibility = Visibility.Visible;
            DownloadProgressColor = Brushes.LimeGreen;

            _cancellationTokenSource = new CancellationTokenSource();
            var progress = new Progress<ProgressReportModel>(HandleProgress);

            var downloadedItemsInfo = await _downloadManager.DownloadItemAsync(MediaUrl, downloadFolder.Path,
                downloadOption.FormatType, progress, _cancellationTokenSource.Token);

            ProcessDownloadResult(downloadedItemsInfo, downloadOption);
        }
        catch (OperationCanceledException)
        {
            AppendLog(Environment.NewLine);
            CompleteDownloadUi(DownloadStatus.Cancel);
        }
        catch (Exception e)
        {
            Log.Error(e, "Download failed");
            AppendLog($"{e.Message}{Environment.NewLine}");
            CompleteDownloadUi(DownloadStatus.Fail);
        }
        finally
        {
            _isDownloadRunning = false;
        }
    }

    private void HandleProgress(ProgressReportModel reportModel)
    {
        // Progress<T> marshals this callback onto the UI thread; no locking needed.
        if (!string.IsNullOrEmpty(reportModel.Message))
        {
            AppendLog($"{reportModel.Message}{Environment.NewLine}");
        }

        if (reportModel.Value is not { } progressValue)
        {
            return;
        }

        DownloadProgressIsIndeterminate = false;
        DownloadProgressValue = progressValue;
        DownloadPercentText = $"{progressValue}%";
    }

    private void ProcessDownloadResult(ICollection<DownloadedItemInfo> downloadedItemsInfo, DownloadOption option)
    {
        AppendLog(Environment.NewLine);
        AppendLog(Environment.NewLine);

        DownloadProgressIsIndeterminate = false;
        DownloadProgressValue = DownloadProgressMax;

        var lastDownloadStatus = DownloadStatus.Success;
        var hasSuccessfulDownloads = false;
        foreach (var info in downloadedItemsInfo)
        {
            switch (info.Status)
            {
                case DownloadStatus.Success:
                    hasSuccessfulDownloads = true;
                    AppendLog($"{Resources.MessageItemDownloadComplete} {info.Name}{Environment.NewLine}");
                    break;
                case DownloadStatus.Fail:
                    AppendLog($"{Resources.MessageItemDownloadFailed} {info.Name}{Environment.NewLine}");
                    lastDownloadStatus = info.Status;
                    break;
                case DownloadStatus.Cancel:
                    AppendLog($"{Resources.MessageItemDownloadCanceled} {info.Name}{Environment.NewLine}");
                    lastDownloadStatus = info.Status;
                    break;
            }

            _historyService.AddOrUpdate(info.Name, info.Path, info.Url, (int)info.Status, (int)option.FormatType);
        }

        CompleteDownloadUi(lastDownloadStatus);

        if (hasSuccessfulDownloads)
        {
            var downloadPath = downloadedItemsInfo.FirstOrDefault(x => x.Status == DownloadStatus.Success)?.Path;
            if (!string.IsNullOrEmpty(downloadPath))
            {
                AppendLog(Environment.NewLine);
                AppendLog(downloadedItemsInfo.Count > 1
                    ? $"{Resources.LogMessageLocationOfFiles} {Path.GetDirectoryName(downloadPath)}"
                    : $"{Resources.LogMessageLocationOfFile} {downloadPath}");
            }
        }
    }

    private void CompleteDownloadUi(DownloadStatus status)
    {
        switch (status)
        {
            case DownloadStatus.Success:
                DownloadProgressColor = Brushes.DeepSkyBlue;
                DownloadMessage = Resources.MessageDownloadComplete;
                AppendLog(Resources.LogMessageDownloadSuccess);
                break;
            case DownloadStatus.Fail:
                DownloadProgressColor = Brushes.DarkOrange;
                DownloadMessage = Resources.MessageDownloadFailed;
                break;
            case DownloadStatus.Cancel:
                DownloadProgressColor = Brushes.Gainsboro;
                DownloadMessage = Resources.MessageDownloadCancelled;
                AppendLog(Resources.LogMessageDownloadCancel);
                break;
        }

        DownloadButtonIcon = IconHelper.GetDownloadIcon(false);
        DownloadButtonText = Resources.StartDownloadButtonText;
        DownloadButtonIsEnabled = true;
        ShowDownloadedItemsButtonIsEnabled = true;
        GeneralInterfaceIsEnabled = true;
        DownloadHistoryIsEnabled = true;
    }

    private void AppendLog(string text)
    {
        _downloadLog.Append(text);
        OnPropertyChanged(nameof(DownloadLog));
    }
}
