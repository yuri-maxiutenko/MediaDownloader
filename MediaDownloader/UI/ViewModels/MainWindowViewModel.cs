﻿using System;
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
using MediaDownloader.Models;
using MediaDownloader.Properties;
using MediaDownloader.Utilities;

using Microsoft.Extensions.Configuration;
using Microsoft.Toolkit.Mvvm.Input;

using NLog;
using NLog.Common;
using NLog.Config;

namespace MediaDownloader.UI.ViewModels;

public sealed class MainWindowViewModel : BaseViewModel
{
    private const string AppSettingsFilePath = @".\appsettings.json";
    private const string NlogSettingsFilePath = @".\nlog.config";

    private const int DownloadProgressSectionStep = 100;
    private const int DownloadRetriesNumber = 2;

    private readonly StringBuilder _downloadLog = new();
    private readonly object _logWritingLock = new();

    private CancellationTokenSource _cancellationTokenSource;
    private ICommand _clearButtonClick;
    private DownloadedItemInfo _currentDownloadedItem;
    private ICommand _downloadButtonClick;
    private string _downloadButtonIcon;
    private bool _downloadButtonIsEnabled;
    private string _downloadButtonText;
    private IAsyncRelayCommand _downloadCommand;
    private IDownloader _downloader;
    private bool _downloadHistoryIsEnabled;
    private string _downloadMessage;
    private string _downloadPercentText;
    private Brush _downloadProgressColor;
    private bool _downloadProgressIsIndeterminate;
    private int _downloadProgressMax;
    private int _downloadProgressMin;
    private int _downloadProgressSectionMin;
    private int _downloadProgressValue;
    private Visibility _downloadProgressVisibility;
    private bool _generalInterfaceIsEnabled;
    private ICommand _historyMenuItemClearHistory;
    private ICommand _historyMenuItemCopyLink;
    private ICommand _historyMenuItemOpenFolder;
    private IAsyncRelayCommand _historyMenuItemReDownload;
    private ICommand _historyMenuItemRemoveFromHistory;
    private bool _isMultipleFiles;
    private DownloadStatus _lastDownloadStatus;
    private DownloadFolder _selectedDownloadFolder;
    private DownloadOption _selectedDownloadOption;
    private ICommand _showDownloadedItemsButtonClick;
    private bool _showDownloadedItemsButtonIsEnabled;
    private ICommand _stopDownloadCommand;
    private Storage _storage;
    private string _userVideosFolder;
    private string _youTubeLink;

    public MainWindowViewModel()
    {
        Initialize();
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
        get { return _clearButtonClick ??= new RelayCommand(() => { YouTubeLink = string.Empty; }, () => true); }
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

                YouTubeLink = DownloadHistorySelectedItem?.Url;
                await DownloadAsync();
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

    public int DownloadProgressValue
    {
        get => _downloadProgressValue;
        set => SetField(ref _downloadProgressValue, value);
    }

    public int DownloadProgressMin
    {
        get => _downloadProgressMin;
        set => SetField(ref _downloadProgressMin, value);
    }

    public int DownloadProgressMax
    {
        get => _downloadProgressMax;
        set => SetField(ref _downloadProgressMax, value);
    }

    public List<DownloadOption> DownloadOptions { get; private set; }

    private Logger Logger { get; } = LogManager.GetCurrentClassLogger();

    public string UserVideosFolder =>
        _userVideosFolder ??= Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);

    public string YouTubeLink
    {
        get => _youTubeLink;
        set => SetField(ref _youTubeLink, value);
    }

    public string DownloadLog
    {
        get
        {
            lock (_logWritingLock)
            {
                return _downloadLog.ToString();
            }
        }
        set
        {
            lock (_logWritingLock)
            {
                _downloadLog.Append(value);
            }

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

            await UpdateDownloaderAsync(_downloader, _cancellationTokenSource.Token);

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
            Logger.Error(e, "Failed to update the downloader");
        }
    }

    private async Task<bool> UpdateDownloaderAsync(IDownloader downloader, CancellationToken cancellationToken)
    {
        return await downloader.UpdateAsync(ProcessDownloaderOutput, ProcessDownloaderError, cancellationToken);
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

        _cancellationTokenSource = new CancellationTokenSource();
        await DownloadItemAsync(_downloader, _cancellationTokenSource.Token);

        ProcessDownloadResult();
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
            Logger.Error(e);
        }
    }

    private void ProcessDownloadResult()
    {
        DownloadLog = Environment.NewLine;
        DownloadLog = Environment.NewLine;

        DownloadProgressIsIndeterminate = false;
        DownloadProgressValue = DownloadProgressMax;

        switch (_lastDownloadStatus)
        {
            case DownloadStatus.Success:
                DownloadProgressColor = Brushes.DeepSkyBlue;
                DownloadMessage = string.Format(Resources.MessageDownloadComplete, LastDownloadedItem.Name);

                DownloadLog = Resources.LogMessageDownloadSuccess;
                DownloadLog = Environment.NewLine;
                DownloadLog = _isMultipleFiles
                    ? string.Format(Resources.LogMessageLocationOfFiles, LastDownloadedItem.Path)
                    : string.Format(Resources.LogMessageLocationOfFile, LastDownloadedItem.Path);
                break;
            case DownloadStatus.Fail:
                DownloadProgressColor = Brushes.OrangeRed;
                DownloadMessage = string.Format(Resources.MessageDownloadFailed, LastDownloadedItem.Name);

                DownloadLog = Resources.LogMessageDownloadFail;
                break;
            case DownloadStatus.Cancel:
                DownloadProgressColor = Brushes.Gainsboro;
                DownloadMessage = string.Format(Resources.MessageDownloadCancelled, LastDownloadedItem.Name);

                DownloadLog = Resources.LogMessageDownloadCancel;
                break;
        }

        DownloadButtonIsEnabled = false;
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
            DownloadButtonIsEnabled = Utilities.Utilities.IsValidUrl(YouTubeLink) && downloadDirectoryExists;
            ShowDownloadedItemsButtonIsEnabled = downloadDirectoryExists;
        }
        catch (Exception e)
        {
            Logger.Error(e);
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

    private void Initialize()
    {
        InternalLogger.LogLevel = LogLevel.Debug;
        InternalLogger.LogToConsole = true;
        Configuration = new ConfigurationBuilder().AddJsonFile(AppSettingsFilePath, true, true).Build();
        LogManager.Configuration = new XmlLoggingConfiguration(NlogSettingsFilePath);

        _downloader = new Downloader(Configuration["DownloaderPath"], Configuration["ConverterPath"]);

        _storage = new Storage();

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
                Format = DownloadFormat.Best,
                Name = Resources.DownloaderFormatBestName,
                Option = Resources.DownloaderOptionFormatBest
            },
            new()
            {
                Format = DownloadFormat.BestMp4,
                Name = Resources.DownloaderFormatBestMp4Name,
                Option = Resources.DownloaderOptionFormatBestMp4
            },
            new()
            {
                Format = DownloadFormat.BestDirectLink,
                Name = Resources.DownloaderFormatBestDirectLinkName,
                Option = Resources.DownloaderOptionFormatBestDirectLink
            },
            new()
            {
                Format = DownloadFormat.AudioOnly,
                Name = Resources.DownloaderFormatAudioOnlyName,
                Option = Resources.DownloaderOptionFormatAudioOnly
            }
        };

        ValidateDownload();
    }

    private async Task DownloadItemAsync(IDownloader downloader, CancellationToken cancellationToken)
    {
        try
        {
            _lastDownloadStatus = DownloadStatus.Fail;

            lock (_logWritingLock)
            {
                _downloadLog.Clear();
            }

            DownloadPercentText = string.Empty;

            DownloadLog = string.Empty;

            DownloadMessage = Resources.MessageDownloadPreparing;

            var retryCounter = 0;

            DownloadItem item = null;
            while (item is null && retryCounter < DownloadRetriesNumber)
            {
                item = await GetItemAsync(downloader, cancellationToken);
                retryCounter++;
            }

            if (item is null)
            {
                _lastDownloadStatus = DownloadStatus.Fail;
                return;
            }

            DownloadProgressMin = 0;
            DownloadProgressMax = DownloadProgressSectionStep * item.Entries.Count;
            DownloadProgressValue = DownloadProgressMin;

            _downloadProgressSectionMin = 0;

            LastDownloadedItem.Path = SelectedDownloadFolder.Path;

            _isMultipleFiles = item.Entries.Count > 1;

            if (_isMultipleFiles)
            {
                LastDownloadedItem.Path = Path.Combine(LastDownloadedItem.Path, item.Name);
                Directory.CreateDirectory(LastDownloadedItem.Path);
            }

            foreach (var entry in item.Entries)
            {
                LastDownloadedItem.Name = entry.Name;
                LastDownloadedItem.Url = entry.Url;

                DownloadMessage = string.Format(Resources.MessageDownloading, LastDownloadedItem.Name);

                Logger.Info("{DownloadingMessage} {Entry}", Resources.MessageDownloading, entry);

                _currentDownloadedItem = new DownloadedItemInfo
                {
                    Url = LastDownloadedItem.Url,
                    Name = LastDownloadedItem.Name,
                    Path = Path.Combine(LastDownloadedItem.Path, LastDownloadedItem.Name)
                };

                DownloadLog = $"{Environment.NewLine}{Environment.NewLine}";
                DownloadLog = string.Format(Resources.LogMessageDownloadingFile, _currentDownloadedItem,
                    LastDownloadedItem.Url);
                DownloadLog = $"{Environment.NewLine}{Environment.NewLine}";

                var success = false;
                retryCounter = 0;
                while (!success && retryCounter < DownloadRetriesNumber)
                {
                    success = await DownloadItemAsync(downloader, LastDownloadedItem.Name, LastDownloadedItem.Url,
                        _currentDownloadedItem.Path, cancellationToken);
                    retryCounter++;
                }

                _lastDownloadStatus = success ? DownloadStatus.Success : DownloadStatus.Fail;

                _downloadProgressSectionMin += DownloadProgressSectionStep;

                LastDownloadedItem.Name = _currentDownloadedItem.Name;
                LastDownloadedItem.Path = _isMultipleFiles ? LastDownloadedItem.Path : _currentDownloadedItem.Path;
            }
        }
        catch (OperationCanceledException e)
        {
            Logger.Info(e);
            _lastDownloadStatus = DownloadStatus.Cancel;
            DownloadLog = Environment.NewLine;
            DownloadLog = e.Message;
        }
        catch (Exception e)
        {
            Logger.Error(e);
            _lastDownloadStatus = DownloadStatus.Fail;
            DownloadLog = Environment.NewLine;
            DownloadLog = e.Message;
        }
    }

    private async Task<DownloadItem> GetItemAsync(IDownloader downloader, CancellationToken cancellationToken)
    {
        return await downloader.GetItemsAsync(YouTubeLink, SelectedDownloadOption.Option, ProcessDownloaderError,
            cancellationToken);
    }

    private async Task<bool> DownloadItemAsync(IDownloader downloader, string fileName, string downloadUrl,
        string downloadPath, CancellationToken cancellationToken)
    {
        var status = DownloadStatus.Fail;

        try
        {
            _cancellationTokenSource.Token.ThrowIfCancellationRequested();
            var success = await downloader.DownloadItemAsync(downloadPath, downloadUrl, SelectedDownloadOption.Option,
                ProcessDownloaderOutput, ProcessDownloaderError, cancellationToken);

            status = success ? DownloadStatus.Success : DownloadStatus.Fail;

            return success;
        }
        catch (OperationCanceledException)
        {
            status = DownloadStatus.Cancel;
            throw;
        }
        finally
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                _storage.AddOrUpdateHistoryRecord(fileName, downloadPath, downloadUrl, (int)status,
                    (int)SelectedDownloadOption.Format);
                DownloadHistory.View.Refresh();
            });
        }
    }

    private void ProcessDownloaderOutput(object sender, DataReceivedEventArgs e)
    {
        try
        {
            var record = e.Data;
            DownloadLog = record;
            DownloadLog = Environment.NewLine;

            if (DownloadOutputParser.TryParseDownloadProgress(record, out var progress))
            {
                DownloadProgressIsIndeterminate = false;
                var newValue = _downloadProgressSectionMin + (int)Math.Round(progress);
                DownloadProgressValue = newValue > DownloadProgressValue ? newValue : DownloadProgressValue;

                DownloadPercentText =
                    $"{Utilities.Utilities.CalculateAbsolutePercent(DownloadProgressValue, DownloadProgressMax)}%";
            }
            else if (DownloadOutputParser.TryParseResultFilePath(record, out var path))
            {
                _currentDownloadedItem.Name = Path.GetFileName(path);
                _currentDownloadedItem.Path = path;
            }

            Logger.Info(record);
        }
        catch (Exception exception)
        {
            Logger.Error(exception);
        }
    }

    private void ProcessDownloaderError(object sender, DataReceivedEventArgs e)
    {
        try
        {
            var record = e.Data;
            DownloadLog = record;
            DownloadLog = Environment.NewLine;
            Logger.Info(record);
        }
        catch (Exception exception)
        {
            Logger.Error(exception);
        }
    }
}