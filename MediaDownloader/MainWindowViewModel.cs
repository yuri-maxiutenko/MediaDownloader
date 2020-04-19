using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

using MediaDownloader.Data;
using MediaDownloader.Data.Models;
using MediaDownloader.Models;
using MediaDownloader.Properties;

using NLog;

namespace MediaDownloader
{
    internal enum DownloadFormat
    {
        Best,
        BestDirectLink,
        AudioOnly
    }

    internal enum DownloadStatus
    {
        Success,
        Fail,
        Cancel
    }

    internal class DownloadOption
    {
        public DownloadFormat Format { get; set; }

        public string Name { get; set; }

        public string Option { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }

    internal class MainWindowViewModel : INotifyPropertyChanged
    {
        private const int DownloadProgressSectionStep = 100;
        private const int DownloadRetriesNumber = 2;
        private const int DownloadFoldersNumber = 10;

        private bool _isDownloadButtonEnabled;
        private bool _isDownloadProgressIndeterminate;
        private bool _isGeneralInterfaceEnabled;
        private bool _isMultipleFiles;
        private bool _isOpenDownloadFolderButtonEnabled;

        private CancellationTokenSource _cancellation;

        private Downloader _downloader;

        private DownloadOption _selectedDownloadOption;

        private DownloadStatus _lastDownloadStatus;

        private ICommand _clearButtonClick;
        private ICommand _downloadButtonClick;
        private ICommand _openDownloadFolderButtonClick;
        private ICommand _startDownloadCommand;
        private ICommand _stopDownloadCommand;

        private int _downloadProgressMax;
        private int _downloadProgressMin;
        private int _downloadProgressSectionMin;
        private int _downloadProgressValue;
        private int _downloadProgressWidth;

        private readonly object _logWritingLock = new object();

        private Storage _storage;

        private string _downloadButtonText;
        private string _downloadMessage;
        private string _downloadPercentText;
        private string _userVideosFolder;
        private string _youTubeLink;

        private readonly StringBuilder _downloadLog = new StringBuilder();

        private Visibility _downloadProgressVisibility;
        private Visibility _showDownloadedItemsButtonVisibility;

        public MainWindowViewModel()
        {
            Initialize();
        }

        public bool IsDownloadButtonEnabled
        {
            get => _isDownloadButtonEnabled;
            set
            {
                _isDownloadButtonEnabled = value;
                OnPropertyChanged("IsDownloadButtonEnabled");
            }
        }

        public bool IsOpenDownloadFolderButtonEnabled
        {
            get => _isOpenDownloadFolderButtonEnabled;
            set
            {
                _isOpenDownloadFolderButtonEnabled = value;
                OnPropertyChanged("IsOpenDownloadFolderButtonEnabled");
            }
        }

        public bool IsGeneralInterfaceEnabled
        {
            get => _isGeneralInterfaceEnabled;
            set
            {
                _isGeneralInterfaceEnabled = value;
                OnPropertyChanged("IsGeneralInterfaceEnabled");
            }
        }

        public bool IsDownloadProgressIndeterminate
        {
            get => _isDownloadProgressIndeterminate;
            set
            {
                _isDownloadProgressIndeterminate = value;
                OnPropertyChanged("IsDownloadProgressIndeterminate");
            }
        }

        public CollectionViewSource DownloadFolders { get; private set; }

        public DownloadFolder SelectedDownloadFolder { get; set; }

        public DownloadOption SelectedDownloadOption
        {
            get => _selectedDownloadOption;
            set
            {
                _selectedDownloadOption = value;
                OnPropertyChanged("SelectedDownloadOption");
            }
        }

        public ICommand ClearButtonClick
        {
            get
            {
                return _clearButtonClick ?? (_clearButtonClick = new RelayCommand(
                    param =>
                    {
                        YouTubeLink = string.Empty;
                    },
                    param => true));
            }
        }

        public ICommand DownloadButtonClick
        {
            get => _downloadButtonClick;
            set
            {
                _downloadButtonClick = value;
                OnPropertyChanged("DownloadButtonClick");
            }
        }

        public ICommand StartDownloadCommand
        {
            get
            {
                return _startDownloadCommand ?? (_startDownloadCommand = new RelayCommand(
                    param =>
                    {
                        IsGeneralInterfaceEnabled = false;
                        IsDownloadButtonEnabled = false;
                        _cancellation = new CancellationTokenSource();

                        UpdateDownloadFolder(DownloadFolders.View.CurrentItem as DownloadFolder, DateTime.Now);

                        var worker = new BackgroundWorker();
                        worker.DoWork += DownloadItemAsync;
                        worker.RunWorkerCompleted += OnDownloadItemCompleted;
                        worker.RunWorkerAsync();

                        DownloadButtonText = Resources.StopDownloadButtonText;
                        DownloadButtonClick = StopDownloadCommand;
                        IsDownloadButtonEnabled = true;

                        IsDownloadProgressIndeterminate = true;
                        ShowDownloadedItemsButtonVisibility = Visibility.Hidden;
                        DownloadProgressVisibility = Visibility.Visible;
                    },
                    param => true));
            }
        }

        public ICommand StopDownloadCommand
        {
            get
            {
                return _stopDownloadCommand ?? (_stopDownloadCommand = new RelayCommand(
                    param =>
                    {
                        IsDownloadButtonEnabled = false;
                        _cancellation.Cancel();
                    },
                    param => true));
            }
        }

        public ICommand OpenDownloadFolderButtonClick
        {
            get
            {
                return _openDownloadFolderButtonClick ?? (_openDownloadFolderButtonClick = new RelayCommand(
                    param =>
                    {
                        OpenDownloadFolder();
                    },
                    param => true));
            }
        }

        public int DownloadProgressValue
        {
            get => _downloadProgressValue;
            set
            {
                _downloadProgressValue = value;
                OnPropertyChanged("DownloadProgressValue");
            }
        }

        public int DownloadProgressMin
        {
            get => _downloadProgressMin;
            set
            {
                _downloadProgressMin = value;
                OnPropertyChanged("DownloadProgressMin");
            }
        }

        public int DownloadProgressMax
        {
            get => _downloadProgressMax;
            set
            {
                _downloadProgressMax = value;
                OnPropertyChanged("DownloadProgressMax");
            }
        }

        public int DownloadProgressWidth
        {
            get => _downloadProgressWidth;
            set
            {
                _downloadProgressWidth = value;
                OnPropertyChanged("DownloadProgressWidth");
            }
        }

        public List<DownloadOption> DownloadOptions { get; private set; }

        public Logger Logger { get; } = LogManager.GetCurrentClassLogger();

        public string LastItemDownloadPath { get; set; }

        public string UserVideosFolder =>
            _userVideosFolder ??
            (_userVideosFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos));

        public string YouTubeLink
        {
            get => _youTubeLink;
            set
            {
                _youTubeLink = value;
                OnPropertyChanged("YouTubeLink");
            }
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

                OnPropertyChanged("DownloadLog");
            }
        }

        public string DownloadButtonText
        {
            get => _downloadButtonText;
            set
            {
                _downloadButtonText = value;
                OnPropertyChanged("DownloadButtonText");
            }
        }

        public string DownloadMessage
        {
            get => _downloadMessage;
            set
            {
                _downloadMessage = value;
                OnPropertyChanged("DownloadMessage");
            }
        }

        public string DownloadPercentText
        {
            get => _downloadPercentText;
            set
            {
                _downloadPercentText = value;
                OnPropertyChanged("DownloadPercentText");
            }
        }

        public Visibility ShowDownloadedItemsButtonVisibility
        {
            get => _showDownloadedItemsButtonVisibility;
            set
            {
                _showDownloadedItemsButtonVisibility = value;
                OnPropertyChanged("ShowDownloadedItemsButtonVisibility");
            }
        }

        public Visibility DownloadProgressVisibility
        {
            get => _downloadProgressVisibility;
            set
            {
                _downloadProgressVisibility = value;
                OnPropertyChanged("DownloadProgressVisibility");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;


        private void OpenDownloadFolder()
        {
            try
            {
                if (File.Exists(LastItemDownloadPath) || Directory.Exists(LastItemDownloadPath))
                {
                    Process.Start(Resources.ExplorerFileName,
                        $"{Resources.ExplorerOptionSelect}, \"{LastItemDownloadPath}\"");
                }
                else if (Directory.Exists(SelectedDownloadFolder.Path))
                {
                    Process.Start(SelectedDownloadFolder.Path);
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        private void OnDownloadItemCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            DownloadLog = Environment.NewLine;
            DownloadLog = Environment.NewLine;

            switch (_lastDownloadStatus)
            {
                case DownloadStatus.Success:
                    DownloadLog = Resources.LogMessageDownloadSuccess;
                    DownloadLog = Environment.NewLine;
                    DownloadLog = _isMultipleFiles
                        ? string.Format(Resources.LogMessageLocationOfFiles, LastItemDownloadPath)
                        : string.Format(Resources.LogMessageLocationOfFile, LastItemDownloadPath);
                    break;
                case DownloadStatus.Fail:
                    DownloadLog = Resources.LogMessageDownloadFail;
                    break;
                case DownloadStatus.Cancel:
                    DownloadLog = Resources.LogMessageDownloadCancel;
                    break;
            }

            IsDownloadButtonEnabled = false;
            DownloadButtonText = Resources.StartDownloadButtonText;
            DownloadButtonClick = StartDownloadCommand;
            IsDownloadButtonEnabled = true;

            DownloadProgressVisibility = Visibility.Hidden;
            ShowDownloadedItemsButtonVisibility = Visibility.Visible;

            IsGeneralInterfaceEnabled = true;
        }

        public void ValidateDownload()
        {
            try
            {
                var downloadDirectoryExists = Directory.Exists(SelectedDownloadFolder?.Path);
                IsDownloadButtonEnabled = Utilities.IsValidUrl(YouTubeLink) && downloadDirectoryExists;
                IsOpenDownloadFolderButtonEnabled = downloadDirectoryExists;
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void AddOrUpdateDownloadFolder(string path, DateTime lastSelectionDate)
        {
            _storage.AddOrUpdateDownloadFolder(path, lastSelectionDate);
            DownloadFolders.View.Refresh();
            var firstItem = (DownloadFolders.View as CollectionView)?.GetItemAt(0);
            DownloadFolders.View.MoveCurrentTo(firstItem);
        }

        public void UpdateDownloadFolder(DownloadFolder folder, DateTime lastSelectionDate)
        {
            _storage.UpdateDownloadFolder(folder.DownloadFolderId, folder.Path, lastSelectionDate);
            DownloadFolders.View.Refresh();
            var firstItem = (DownloadFolders.View as CollectionView)?.GetItemAt(0);
            DownloadFolders.View.MoveCurrentTo(firstItem);
        }

        private void Initialize()
        {
            _downloader = new Downloader();

            _storage = new Storage();

            IsGeneralInterfaceEnabled = true;

            DownloadButtonText = Resources.StartDownloadButtonText;
            DownloadButtonClick = StartDownloadCommand;

            ShowDownloadedItemsButtonVisibility = Visibility.Visible;
            DownloadProgressVisibility = Visibility.Hidden;

            if (!_storage.DownloadFolders.Any())
            {
                _storage.AddDownloadFolder(UserVideosFolder, DateTime.Now);
            }

            DownloadFolders = new CollectionViewSource
            {
                Source = _storage.DownloadFolders
            };

            DownloadFolders.SortDescriptions.Add(new SortDescription("LastSelectionDate",
                ListSortDirection.Descending));
            var firstItem = (DownloadFolders.View as CollectionView)?.GetItemAt(0);
            DownloadFolders.View.MoveCurrentTo(firstItem);

            DownloadOptions = new List<DownloadOption>
            {
                new DownloadOption
                {
                    Format = DownloadFormat.Best,
                    Name = Resources.DownloaderFormatBestName,
                    Option = Resources.DownloaderOptionFormatBest
                },
                new DownloadOption
                {
                    Format = DownloadFormat.BestDirectLink,
                    Name = Resources.DownloaderFormatBestDirectLinkName,
                    Option = Resources.DownloaderOptionFormatBestDirectLink
                },
                new DownloadOption
                {
                    Format = DownloadFormat.AudioOnly,
                    Name = Resources.DownloaderFormatAudioOnlyName,
                    Option = Resources.DownloaderOptionFormatAudioOnly
                }
            };

            ValidateDownload();
        }

        private void DownloadItemAsync(object sender, DoWorkEventArgs args)
        {
            try
            {
                _lastDownloadStatus = DownloadStatus.Fail;

                _cancellation.Token.ThrowIfCancellationRequested();

                lock (_logWritingLock)
                {
                    _downloadLog.Clear();
                }

                DownloadPercentText = string.Empty;

                DownloadLog = string.Empty;

                DownloadMessage = Resources.MessageDownloadPreparing;

                var success = false;
                var retryCounter = 0;

                DownloadItem item = null;
                while (!success && retryCounter < DownloadRetriesNumber)
                {
                    success = GetItem(out item);
                    retryCounter++;
                }

                if (!success || item == null)
                {
                    _lastDownloadStatus = DownloadStatus.Fail;
                    return;
                }

                DownloadProgressMin = 0;
                DownloadProgressMax = DownloadProgressSectionStep * item.Entries.Count;
                DownloadProgressValue = DownloadProgressMin;

                _downloadProgressSectionMin = 0;

                LastItemDownloadPath = SelectedDownloadFolder.Path;

                _isMultipleFiles = item.Entries.Count > 1;

                if (_isMultipleFiles)
                {
                    LastItemDownloadPath = Path.Combine(LastItemDownloadPath, item.Name);
                    Directory.CreateDirectory(LastItemDownloadPath);
                }

                foreach (var entry in item.Entries)
                {
                    DownloadMessage = string.Format(Resources.MessageDownloading, entry.Name);

                    _cancellation.Token.ThrowIfCancellationRequested();

                    Logger.Info(Resources.MessageDownloading, entry);

                    var currentItemDownloadPath = Path.Combine(LastItemDownloadPath, entry.Name);

                    DownloadLog = $"{Environment.NewLine}{Environment.NewLine}";
                    DownloadLog = string.Format(Resources.LogMessageDownloadingFile,
                        currentItemDownloadPath, entry.Link);
                    DownloadLog = $"{Environment.NewLine}{Environment.NewLine}";

                    success = false;
                    retryCounter = 0;
                    while (!success && retryCounter < DownloadRetriesNumber)
                    {
                        success = DownloadItem(entry.Link, currentItemDownloadPath);
                        retryCounter++;
                    }

                    _lastDownloadStatus = success ? DownloadStatus.Success : DownloadStatus.Fail;

                    _downloadProgressSectionMin += DownloadProgressSectionStep;

                    LastItemDownloadPath = _isMultipleFiles ? LastItemDownloadPath : currentItemDownloadPath;
                }

                DownloadProgressValue = DownloadProgressMax;
                DownloadMessage = Resources.MessageDownloadComplete;
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
            finally
            {
                _storage.AddHistoryRecord(LastItemDownloadPath, YouTubeLink, (int) _lastDownloadStatus,
                    (int) SelectedDownloadOption.Format);
            }
        }

        private bool GetItem(out DownloadItem item)
        {
            _cancellation.Token.ThrowIfCancellationRequested();
            return _downloader.TryGetItems(YouTubeLink, SelectedDownloadOption.Option,
                (o, eventArgs) =>
                {
                    ThreadPool.QueueUserWorkItem(ProcessDownloaderErrorAsync, eventArgs.Data);
                },
                _cancellation.Token, out item);
        }

        private bool DownloadItem(string downloadLink, string downloadPath)
        {
            _cancellation.Token.ThrowIfCancellationRequested();
            var success = _downloader.TryDownloadItem(downloadPath, downloadLink,
                SelectedDownloadOption.Option,
                (o, eventArgs) =>
                {
                    ThreadPool.QueueUserWorkItem(ProcessDownloaderOutputAsync, eventArgs.Data);
                }, (o, eventArgs) =>
                {
                    ThreadPool.QueueUserWorkItem(ProcessDownloaderErrorAsync, eventArgs.Data);
                }, _cancellation.Token);

            return success;
        }

        private void ProcessDownloaderOutputAsync(object state)
        {
            try
            {
                if (!(state is string record))
                {
                    return;
                }

                DownloadLog = record;
                DownloadLog = Environment.NewLine;

                if (Utilities.TryParseDownloadProgress(record, out var progress))
                {
                    IsDownloadProgressIndeterminate = false;
                    var newValue = _downloadProgressSectionMin + (int) Math.Round(progress);
                    DownloadProgressValue = newValue > DownloadProgressValue ? newValue : DownloadProgressValue;

                    DownloadPercentText =
                        $"{Utilities.CalculateAbsolutePercent(DownloadProgressValue, DownloadProgressMax)}%";
                }

                Logger.Info(record);
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        private void ProcessDownloaderErrorAsync(object state)
        {
            try
            {
                if (state is string record)
                {
                    DownloadLog = record;
                    DownloadLog = Environment.NewLine;
                    Logger.Info(record);
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }
    }
}