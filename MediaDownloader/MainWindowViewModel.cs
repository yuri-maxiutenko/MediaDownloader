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
using System.Windows.Media;
using System.Windows.Media.Imaging;

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
        Cancel,
        Unknown
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

    internal class DownloadedItemInfo
    {
        public string Name { get; set; }
        public string Url { get; set; }
        public string Path { get; set; }
    }

    internal class MainWindowViewModel : INotifyPropertyChanged
    {
        private const int DownloadProgressSectionStep = 100;
        private const int DownloadRetriesNumber = 2;
        private const int DownloadFoldersNumber = 10;

        private BitmapImage _downloadButtonIcon;

        private readonly BitmapImage _startDownloadIcon =
            new BitmapImage(new Uri("pack://application:,,,/MediaDownloader;component/Images/icon_download.png"));

        private readonly BitmapImage _stopDownloadIcon =
            new BitmapImage(new Uri("pack://application:,,,/MediaDownloader;component/Images/icon_stop.png"));

        private bool _downloadProgressIsIndeterminate;

        private bool _isDownloadButtonEnabled;
        private bool _isGeneralInterfaceEnabled;
        private bool _isMultipleFiles;
        private bool _showDownloadedItemsButtonIsEnabled;
        private Brush _downloadProgressColor;

        private CancellationTokenSource _cancellation;

        private Downloader _downloader;
        private DownloadFolder _selectedDownloadFolder;

        private DownloadOption _selectedDownloadOption;

        private DownloadStatus _lastDownloadStatus;

        private ICommand _clearButtonClick;
        private ICommand _downloadButtonClick;
        private ICommand _showDownloadedItemsButtonClick;
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

        public MainWindowViewModel()
        {
            Initialize();
        }

        public BitmapImage DownloadButtonIcon
        {
            get => _downloadButtonIcon;
            set
            {
                _downloadButtonIcon = value;
                OnPropertyChanged("DownloadButtonIcon");
            }
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

        public bool ShowDownloadedItemsButtonIsEnabled
        {
            get => _showDownloadedItemsButtonIsEnabled;
            set
            {
                _showDownloadedItemsButtonIsEnabled = value;
                OnPropertyChanged("ShowDownloadedItemsButtonIsEnabled");
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

        public bool DownloadProgressIsIndeterminate
        {
            get => _downloadProgressIsIndeterminate;
            set
            {
                _downloadProgressIsIndeterminate = value;
                OnPropertyChanged("DownloadProgressIsIndeterminate");
            }
        }

        public Brush DownloadProgressColor
        {
            get => _downloadProgressColor;
            set
            {
                _downloadProgressColor = value;
                OnPropertyChanged("DownloadProgressColor");
            }
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

                        DownloadButtonIcon = _stopDownloadIcon;
                        DownloadButtonText = Resources.StopDownloadButtonText;
                        DownloadButtonClick = StopDownloadCommand;
                        IsDownloadButtonEnabled = true;

                        DownloadProgressIsIndeterminate = true;
                        ShowDownloadedItemsButtonIsEnabled = false;
                        DownloadProgressVisibility = Visibility.Visible;
                        DownloadProgressColor = Brushes.LimeGreen;
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

        public ICommand ShowDownloadedItemsButtonClick
        {
            get
            {
                return _showDownloadedItemsButtonClick ?? (_showDownloadedItemsButtonClick = new RelayCommand(
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
                if (File.Exists(LastDownloadedItem.Path) || Directory.Exists(LastDownloadedItem.Path))
                {
                    Process.Start(Resources.ExplorerFileName,
                        $"{Resources.ExplorerOptionSelect}, \"{LastDownloadedItem.Path}\"");
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

            IsDownloadButtonEnabled = false;
            DownloadButtonIcon = _startDownloadIcon;
            DownloadButtonText = Resources.StartDownloadButtonText;
            DownloadButtonClick = StartDownloadCommand;
            IsDownloadButtonEnabled = true;

            ShowDownloadedItemsButtonIsEnabled = true;

            IsGeneralInterfaceEnabled = true;
        }

        public void ValidateDownload()
        {
            try
            {
                var downloadDirectoryExists = Directory.Exists(SelectedDownloadFolder?.Path);
                IsDownloadButtonEnabled = Utilities.IsValidUrl(YouTubeLink) && downloadDirectoryExists;
                ShowDownloadedItemsButtonIsEnabled = downloadDirectoryExists;
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

            LastDownloadedItem = new DownloadedItemInfo();

            IsGeneralInterfaceEnabled = true;

            DownloadButtonIcon = _startDownloadIcon;
            DownloadButtonText = Resources.StartDownloadButtonText;
            DownloadButtonClick = StartDownloadCommand;

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

            DownloadFolders.SortDescriptions.Add(new SortDescription("LastSelectionDate",
                ListSortDirection.Descending));
            var firstItem = (DownloadFolders.View as CollectionView)?.GetItemAt(0);
            DownloadFolders.View.MoveCurrentTo(firstItem);

            DownloadHistory = new CollectionViewSource
            {
                Source = _storage.History
            };

            DownloadHistory.SortDescriptions.Clear();
            DownloadHistory.SortDescriptions.Add(new SortDescription("DownloadDate",
                ListSortDirection.Descending));
            DownloadHistory.SortDescriptions.Add(new SortDescription("FileName",
                ListSortDirection.Ascending));

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

                    _cancellation.Token.ThrowIfCancellationRequested();

                    Logger.Info(Resources.MessageDownloading, entry);

                    var currentItemDownloadPath = Path.Combine(LastDownloadedItem.Path, LastDownloadedItem.Name);

                    DownloadLog = $"{Environment.NewLine}{Environment.NewLine}";
                    DownloadLog = string.Format(Resources.LogMessageDownloadingFile,
                        currentItemDownloadPath, LastDownloadedItem.Url);
                    DownloadLog = $"{Environment.NewLine}{Environment.NewLine}";

                    success = false;
                    retryCounter = 0;
                    while (!success && retryCounter < DownloadRetriesNumber)
                    {
                        success = DownloadItem(LastDownloadedItem.Name, LastDownloadedItem.Url,
                            currentItemDownloadPath);
                        retryCounter++;
                    }

                    _lastDownloadStatus = success ? DownloadStatus.Success : DownloadStatus.Fail;

                    _downloadProgressSectionMin += DownloadProgressSectionStep;

                    LastDownloadedItem.Path = _isMultipleFiles ? LastDownloadedItem.Path : currentItemDownloadPath;
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

        private bool DownloadItem(string fileName, string downloadUrl, string downloadPath)
        {
            var status = DownloadStatus.Fail;

            try
            {
                _cancellation.Token.ThrowIfCancellationRequested();
                var success = _downloader.TryDownloadItem(downloadPath, downloadUrl,
                    SelectedDownloadOption.Option,
                    (o, eventArgs) =>
                    {
                        ThreadPool.QueueUserWorkItem(ProcessDownloaderOutputAsync, eventArgs.Data);
                    }, (o, eventArgs) =>
                    {
                        ThreadPool.QueueUserWorkItem(ProcessDownloaderErrorAsync, eventArgs.Data);
                    }, _cancellation.Token);

                status = success ? DownloadStatus.Success : DownloadStatus.Fail;

                return success;
            }
            catch (OperationCanceledException e)
            {
                status = DownloadStatus.Cancel;
                throw;
            }
            finally
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    _storage.AddHistoryRecord(fileName, downloadPath, downloadUrl, (int) status,
                        (int) SelectedDownloadOption.Format);
                });
            }
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
                    DownloadProgressIsIndeterminate = false;
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