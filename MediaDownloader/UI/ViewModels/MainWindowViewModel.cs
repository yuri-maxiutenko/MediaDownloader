using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

using MediaDownloader.Data;
using MediaDownloader.Data.Models;
using MediaDownloader.Download;
using MediaDownloader.Download.Models;
using MediaDownloader.Models;
using MediaDownloader.Properties;
using MediaDownloader.Utilities;

using Microsoft.Extensions.Configuration;
using Microsoft.Toolkit.Mvvm.Input;

using Serilog;

namespace MediaDownloader.UI.ViewModels;

public sealed class MainWindowViewModel : BaseViewModel
{
    private const string AppSettingsFilePath = @".\appsettings.json";
    private const double DownloadProgressMax = 100.0;

    private readonly StringBuilder _downloadLog = new();

    private CancellationTokenSource _cancellationTokenSource;
    private ICommand _clearButtonClick;
    private ICommand _downloadButtonClick;
    private string _downloadButtonIcon;
    private bool _downloadButtonIsEnabled;
    private string _downloadButtonText;
    private IAsyncRelayCommand _downloadCommand;
    private IDownloader _downloader;
    private bool _downloadHistoryIsEnabled;
    private IDownloadManager _downloadManager;
    private string _downloadMessage;
    private string _downloadPercentText;
    private Brush _downloadProgressColor;
    private bool _downloadProgressIsIndeterminate;
    private double _downloadProgressValue;
    private Visibility _downloadProgressVisibility;
    private bool _generalInterfaceIsEnabled;
    private ICommand _historyMenuItemClearHistory;
    private ICommand _historyMenuItemCopyLink;
    private ICommand _historyMenuItemOpenFolder;
    private IAsyncRelayCommand _historyMenuItemReDownload;
    private ICommand _historyMenuItemRemoveFromHistory;
    private string _mediaUrl;
    private DownloadFolder _selectedDownloadFolder;
    private DownloadOption _selectedDownloadOption;
    private ICommand _showDownloadedItemsButtonClick;
    private bool _showDownloadedItemsButtonIsEnabled;
    private ICommand _stopDownloadCommand;
    private Storage _storage;
    private string _userVideosFolder;

    public MainWindowViewModel()
    {
        var userDataFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            Resources.ManufacturerFolderName, Resources.AppFolderName);
        Directory.CreateDirectory(userDataFolderPath);

        Initialize(userDataFolderPath);
    }

    public IConfiguration Configuration { get; set; }

    public string DownloadButtonIcon
    {
        get => _downloadButtonIcon;
        set => SetField(ref _downloadButtonIcon, value);
    }

    public bool DownloadButtonIsEnabled
    {
        get => _downloadButtonIsEnabled;
        set => SetField(ref _downloadButtonIsEnabled, value);
    }

    public bool ShowDownloadedItemsButtonIsEnabled
    {
        get => _showDownloadedItemsButtonIsEnabled;
        set => SetField(ref _showDownloadedItemsButtonIsEnabled, value);
    }

    public bool GeneralInterfaceIsEnabled
    {
        get => _generalInterfaceIsEnabled;
        set => SetField(ref _generalInterfaceIsEnabled, value);
    }

    public bool DownloadProgressIsIndeterminate
    {
        get => _downloadProgressIsIndeterminate;
        set => SetField(ref _downloadProgressIsIndeterminate, value);
    }

    public bool DownloadHistoryIsEnabled
    {
        get => _downloadHistoryIsEnabled;
        set => SetField(ref _downloadHistoryIsEnabled, value);
    }

    public Brush DownloadProgressColor
    {
        get => _downloadProgressColor;
        set => SetField(ref _downloadProgressColor, value);
    }

    public CollectionViewSource DownloadFolders { get; private set; }

    public CollectionViewSource DownloadHistory { get; private set; }

    public DownloadedItemInfo LastDownloadedItem { get; set; }

    public DownloadFolder SelectedDownloadFolder
    {
        get => _selectedDownloadFolder;
        set
        {
            _selectedDownloadFolder = value;
            ValidateDownload();
        }
    }

    public DownloadOption SelectedDownloadOption
    {
        get => _selectedDownloadOption;
        set => SetField(ref _selectedDownloadOption, value);
    }

    public HistoryRecord DownloadHistorySelectedItem { get; set; }

    public ICommand ClearButtonClick
    {
        get { return _clearButtonClick ??= new RelayCommand(() => { MediaUrl = string.Empty; }, () => true); }
    }

    public ICommand DownloadButtonClick
    {
        get => _downloadButtonClick;
        set => SetField(ref _downloadButtonClick, value);
    }

    public IAsyncRelayCommand DownloadCommand
    {
        get { return _downloadCommand ??= new AsyncRelayCommand(DownloadAsync); }
    }

    public ICommand StopDownloadCommand
    {
        get
        {
            return _stopDownloadCommand ??= new RelayCommand(() =>
            {
                DownloadButtonIsEnabled = false;
                _cancellationTokenSource.Cancel();
            }, () => true);
        }
    }

    public ICommand ShowDownloadedItemsButtonClick
    {
        get { return _showDownloadedItemsButtonClick ??= new RelayCommand(OpenDownloadFolder, () => true); }
    }

    public ICommand HistoryMenuItemOpenFolder
    {
        get
        {
            return _historyMenuItemOpenFolder ??= new RelayCommand(() =>
            {
                var path = DownloadHistorySelectedItem?.Path;
                if (string.IsNullOrEmpty(path))
                {
                    return;
                }

                if (File.Exists(path) || Directory.Exists(path))
                {
                    Process.Start(Resources.ExplorerFileName, $"{Resources.ExplorerOptionSelect}, \"{path}\"");
                }
                else if (Directory.Exists(SelectedDownloadFolder.Path))
                {
                    Process.Start(SelectedDownloadFolder.Path);
                }
            }, () => true);
        }
    }

    public IAsyncRelayCommand HistoryMenuItemReDownload
    {
        get
        {
            return _historyMenuItemReDownload ??= new AsyncRelayCommand(async () =>
            {
                if (string.IsNullOrEmpty(DownloadHistorySelectedItem?.Url))
                {
                    return;
                }

                MediaUrl = DownloadHistorySelectedItem?.Url;
                await DownloadAsync().ConfigureAwait(false);
            });
        }
    }

    public ICommand HistoryMenuItemCopyLink
    {
        get
        {
            return _historyMenuItemCopyLink ??= new RelayCommand(() =>
            {
                if (!string.IsNullOrEmpty(DownloadHistorySelectedItem?.Url))
                {
                    Clipboard.SetText(DownloadHistorySelectedItem.Url);
                }
            }, () => true);
        }
    }

    public ICommand HistoryMenuItemRemoveFromHistory
    {
        get
        {
            return _historyMenuItemRemoveFromHistory ??= new RelayCommand(() =>
            {
                if (DownloadHistorySelectedItem != null)
                {
                    _storage.RemoveHistoryRecord(DownloadHistorySelectedItem);
                    DownloadHistory.View.Refresh();
                }
            }, () => true);
        }
    }

    public ICommand HistoryMenuItemClearHistory
    {
        get
        {
            return _historyMenuItemClearHistory ??= new RelayCommand(() =>
            {
                _storage.ClearHistory();
                DownloadHistory.View.Refresh();
            }, () => true);
        }
    }

    public double DownloadProgressValue
    {
        get => _downloadProgressValue;
        set => SetField(ref _downloadProgressValue, value);
    }

    public List<DownloadOption> DownloadOptions { get; private set; }

    public string UserVideosFolder =>
        _userVideosFolder ??= Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);

    public string MediaUrl
    {
        get => _mediaUrl;
        set => SetField(ref _mediaUrl, value);
    }

    public string DownloadLog
    {
        get => _downloadLog.ToString();
        set
        {
            _downloadLog.Append(value);
            OnPropertyChanged();
        }
    }

    public string DownloadButtonText
    {
        get => _downloadButtonText;
        set => SetField(ref _downloadButtonText, value);
    }

    public string DownloadMessage
    {
        get => _downloadMessage;
        set => SetField(ref _downloadMessage, value);
    }

    public string DownloadPercentText
    {
        get => _downloadPercentText;
        set => SetField(ref _downloadPercentText, value);
    }

    public Visibility DownloadProgressVisibility
    {
        get => _downloadProgressVisibility;
        set => SetField(ref _downloadProgressVisibility, value);
    }

    public event EventHandler DownloadCompleted;

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
            await _downloadManager.UpdateDownloaderAsync(progress, _cancellationTokenSource.Token)
                .ConfigureAwait(false);

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

    private async Task DownloadAsync()
    {
        GeneralInterfaceIsEnabled = false;
        DownloadButtonIsEnabled = false;
        DownloadHistoryIsEnabled = false;

        UpdateDownloadFolder(DownloadFolders.View.CurrentItem as DownloadFolder, DateTime.Now);

        DownloadButtonIcon = IconHelper.GetDownloadIcon(true);
        DownloadButtonText = Resources.StopDownloadButtonText;
        DownloadButtonClick = StopDownloadCommand;
        DownloadButtonIsEnabled = true;

        DownloadProgressIsIndeterminate = true;
        ShowDownloadedItemsButtonIsEnabled = false;
        DownloadProgressVisibility = Visibility.Visible;
        DownloadProgressColor = Brushes.LimeGreen;

        await DownloadItemsAsync().ConfigureAwait(false);
    }

    private async Task DownloadItemsAsync()
    {
        _cancellationTokenSource = new CancellationTokenSource();

        var progress = new Progress<ProgressReportModel>(HandleProgress);

        var downloadedItemsInfo = await _downloadManager.DownloadItemAsync(MediaUrl, SelectedDownloadFolder.Path,
            SelectedDownloadOption.FormatType, progress, _cancellationTokenSource.Token).ConfigureAwait(false);

        ProcessDownloadResult(downloadedItemsInfo);
    }

    private void HandleProgress(ProgressReportModel reportModel)
    {
        Task.Run(() =>
        {
            if (!string.IsNullOrEmpty(reportModel.Message))
            {
                DownloadLog = reportModel.Message;
                DownloadLog = Environment.NewLine;
            }

            if (reportModel.Value is not { } progressValue)
            {
                return;
            }

            DownloadProgressIsIndeterminate = false;
            DownloadProgressValue = progressValue;
            DownloadPercentText = $"{progressValue}%";
        });
    }

    private void OpenDownloadFolder()
    {
        try
        {
            if (File.Exists(LastDownloadedItem.Path) || Directory.Exists(LastDownloadedItem.Path))
            {
                Process.Start(Resources.ExplorerFileName,
                    $"{Resources.ExplorerOptionSelect}, \"{LastDownloadedItem.Path}\"");
            }
            else if (Directory.Exists(SelectedDownloadFolder.Path))
            {
                Process.Start(Resources.ExplorerFileName, SelectedDownloadFolder.Path);
            }
        }
        catch (Exception e)
        {
            Log.Error(e, "Failed to open download folder");
        }
    }

    private void ProcessDownloadResult(ICollection<DownloadedItemInfo> downloadedItemsInfo)
    {
        DownloadLog = Environment.NewLine;
        DownloadLog = Environment.NewLine;

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
                    DownloadLog = $"{Resources.MessageItemDownloadComplete} {info.Name}";
                    DownloadLog = Environment.NewLine;
                    break;
                case DownloadStatus.Fail:
                    DownloadLog = $"{Resources.MessageItemDownloadFailed} {info.Name}";
                    DownloadLog = Environment.NewLine;
                    lastDownloadStatus = info.Status;
                    break;
                case DownloadStatus.Cancel:
                    DownloadLog = $"{Resources.MessageItemDownloadCanceled} {info.Name}";
                    DownloadLog = Environment.NewLine;
                    lastDownloadStatus = info.Status;
                    break;
            }

            _storage.AddOrUpdateHistoryRecord(info.Name, info.Path, info.Url, (int)info.Status,
                (int)SelectedDownloadOption.FormatType);
        }

        OnDownloadCompleted();

        switch (lastDownloadStatus)
        {
            case DownloadStatus.Success:
                DownloadProgressColor = Brushes.DeepSkyBlue;
                DownloadMessage = Resources.MessageDownloadComplete;
                DownloadLog = Resources.LogMessageDownloadSuccess;
                break;
            case DownloadStatus.Fail:
                DownloadProgressColor = Brushes.DarkOrange;
                DownloadMessage = Resources.MessageDownloadFailed;
                break;
            case DownloadStatus.Cancel:
                DownloadProgressColor = Brushes.Gainsboro;
                DownloadMessage = Resources.MessageDownloadCancelled;
                DownloadLog = Resources.LogMessageDownloadCancel;
                break;
        }

        if (hasSuccessfulDownloads)
        {
            var downloadPath = downloadedItemsInfo.FirstOrDefault(x => x.Status == DownloadStatus.Success)?.Path;
            if (!string.IsNullOrEmpty(downloadPath))
            {
                DownloadLog = Environment.NewLine;
                DownloadLog = downloadedItemsInfo.Count > 1
                    ? $"{Resources.LogMessageLocationOfFiles} {Path.GetDirectoryName(downloadPath)}"
                    : $"{Resources.LogMessageLocationOfFile} {downloadPath}";
            }
        }

        DownloadButtonIcon = IconHelper.GetDownloadIcon(false);
        DownloadButtonText = Resources.StartDownloadButtonText;
        DownloadButtonClick = DownloadCommand;
        DownloadButtonIsEnabled = true;
        ShowDownloadedItemsButtonIsEnabled = true;
        GeneralInterfaceIsEnabled = true;
        DownloadHistoryIsEnabled = true;
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
        _storage.AddOrUpdateDownloadFolder(path, lastSelectionDate);
        DownloadFolders.View.Refresh();
        var firstItem = (DownloadFolders.View as CollectionView)?.GetItemAt(0);
        DownloadFolders.View.MoveCurrentTo(firstItem);
    }

    private void UpdateDownloadFolder(DownloadFolder folder, DateTime lastSelectionDate)
    {
        _storage.UpdateDownloadFolder(folder.DownloadFolderId, folder.Path, lastSelectionDate);
        DownloadFolders.View.Refresh();
        var firstItem = (DownloadFolders.View as CollectionView)?.GetItemAt(0);
        DownloadFolders.View.MoveCurrentTo(firstItem);
    }

    private void Initialize(string userDataFolderPath)
    {
        LogConfigurator.SetupLogs(userDataFolderPath);
        Configuration = new ConfigurationBuilder().AddJsonFile(AppSettingsFilePath, true, true).Build();

        _downloader = new Downloader(Configuration["DownloaderPath"], Configuration["ConverterPath"]);
        _downloadManager = new DownloadManager(_downloader);

        var dataFolderPath = Path.Combine(userDataFolderPath, Resources.DataFolderName);
        Directory.CreateDirectory(dataFolderPath);
        _storage = new Storage($"Data Source={Path.Combine(dataFolderPath, Resources.DatabaseName)}");

        LastDownloadedItem = new DownloadedItemInfo();

        GeneralInterfaceIsEnabled = true;

        DownloadButtonIcon = IconHelper.GetDownloadIcon(false);
        DownloadButtonText = Resources.StartDownloadButtonText;
        DownloadButtonClick = DownloadCommand;

        DownloadProgressColor = Brushes.Gainsboro;

        ShowDownloadedItemsButtonIsEnabled = true;

        if (!_storage.DownloadFolders.Any())
        {
            _storage.AddDownloadFolder(UserVideosFolder, DateTime.Now);
        }

        DownloadFolders = new CollectionViewSource
        {
            Source = _storage.DownloadFolders
        };

        DownloadFolders.SortDescriptions.Add(new SortDescription("LastSelectionDate", ListSortDirection.Descending));
        var firstItem = (DownloadFolders.View as CollectionView)?.GetItemAt(0);
        DownloadFolders.View.MoveCurrentTo(firstItem);

        DownloadHistory = new CollectionViewSource
        {
            Source = _storage.History
        };

        DownloadHistory.SortDescriptions.Clear();
        DownloadHistory.SortDescriptions.Add(new SortDescription("DownloadDate", ListSortDirection.Descending));
        DownloadHistory.SortDescriptions.Add(new SortDescription("FileName", ListSortDirection.Ascending));

        DownloadHistoryIsEnabled = true;

        DownloadOptions = new List<DownloadOption>
        {
            new()
            {
                FormatType = DownloadFormatType.Best,
                Name = Resources.DownloaderFormatBestName
            },
            new()
            {
                FormatType = DownloadFormatType.BestMp4,
                Name = Resources.DownloaderFormatBestMp4Name
            },
            new()
            {
                FormatType = DownloadFormatType.BestDirectLink,
                Name = Resources.DownloaderFormatBestDirectLinkName
            },
            new()
            {
                FormatType = DownloadFormatType.AudioOnly,
                Name = Resources.DownloaderFormatAudioOnlyName
            }
        };

        ValidateDownload();
    }

    private void OnDownloadCompleted()
    {
        DownloadCompleted?.Invoke(this, EventArgs.Empty);
    }
}