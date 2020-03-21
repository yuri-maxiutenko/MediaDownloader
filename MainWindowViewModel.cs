using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using Syroot.Windows.IO;

using YoutubeDownloader.Properties;

namespace YoutubeDownloader
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
        public DownloadFormat Format
        {
            get;
            set;
        }

        public string Name
        {
            get;
            set;
        }

        public string Option
        {
            get;
            set;
        }

        public override string ToString()
        {
            return Name;
        }
    }

    internal class MainWindowViewModel : INotifyPropertyChanged
    {
        private const int ProcessWaitTimeoutMs = 500;

        private CancellationTokenSource _cancellation;

        private ICommand _clearButtonClick;
        private ICommand _downloadButtonClick;

        private string _downloadButtonText;

        private string _downloaderPath;

        private string _downloadFolderPath;
        private readonly StringBuilder _downloadLog = new StringBuilder();

        private Visibility _downloadProgressVisibility;
        private string _converterPath;

        private bool _isDownloadButtonEnabled;

        private bool _isGeneralInterfaceEnabled;
        private bool _isOpenDownloadFolderButtonEnabled;

        private DownloadStatus _lastDownloadStatus;
        private readonly object _logWritingLock = new object();
        private ICommand _openDownloadFolderButtonClick;

        private DownloadOption _selectedDownloadOption;

        private Visibility _showDownloadedItemsButtonVisibility;
        private ICommand _startDownloadCommand;
        private ICommand _stopDownloadCommand;
        private string _userDownloadsFolder;
        private string _youTubeLink;

        public MainWindowViewModel()
        {
            Initialize();
        }

        public string LastDownloadedFilePath
        {
            get;
            set;
        }

        public string UserDownloadsFolder =>
            _userDownloadsFolder ??
            (_userDownloadsFolder = new KnownFolder(KnownFolderType.DownloadsLocalized).ExpandedPath);

        public List<DownloadOption> DownloadOptions
        {
            get;
            private set;
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

        public string DownloadFolderPath
        {
            get => _downloadFolderPath;
            set
            {
                _downloadFolderPath = value;
                OnPropertyChanged("DownloadFolderPath");
            }
        }

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

        public string DownloadButtonText
        {
            get => _downloadButtonText;
            set
            {
                _downloadButtonText = value;
                OnPropertyChanged("DownloadButtonText");
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

                        var worker = new BackgroundWorker();
                        worker.DoWork += DownloadItemAsync;
                        worker.RunWorkerCompleted += OnDownloadItemCompleted;
                        worker.RunWorkerAsync();

                        DownloadButtonText = Resources.StopDownloadButtonText;
                        DownloadButtonClick = StopDownloadCommand;
                        IsDownloadButtonEnabled = true;

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
                        if (File.Exists(LastDownloadedFilePath))
                        {
                            Process.Start(Resources.ExplorerFileName,
                                $"{Resources.ExplorerSelectOption}, \"{LastDownloadedFilePath}\"");
                        }
                        else if (Directory.Exists(DownloadFolderPath))
                        {
                            Process.Start(DownloadFolderPath);
                        }
                    },
                    param => true));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnDownloadItemCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            DownloadLog = Environment.NewLine;
            DownloadLog = Environment.NewLine;

            switch (_lastDownloadStatus)
            {
                case DownloadStatus.Success:
                    DownloadLog = string.Format(Resources.LogMessageDownloadSuccess, LastDownloadedFilePath);
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
            var downloadDirectoryExists = Directory.Exists(DownloadFolderPath);
            IsDownloadButtonEnabled = Utilities.IsValidUrl(YouTubeLink) && downloadDirectoryExists;
            IsOpenDownloadFolderButtonEnabled = downloadDirectoryExists;
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void Initialize()
        {
            IsGeneralInterfaceEnabled = true;

            DownloadButtonText = Resources.StartDownloadButtonText;
            DownloadButtonClick = StartDownloadCommand;

            ShowDownloadedItemsButtonVisibility = Visibility.Visible;
            DownloadProgressVisibility = Visibility.Hidden;

            _downloaderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Resources.DownloaderFileName);
            _converterPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Resources.ConverterDirectoryName,
                Resources.BinDirectoryName);

            DownloadFolderPath = UserDownloadsFolder;
            DownloadOptions = new List<DownloadOption>
            {
                new DownloadOption
                {
                    Format = DownloadFormat.Best,
                    Name = Resources.DownloaderFormatBestName,
                    Option = Resources.DownloaderFormatBestOption
                },
                new DownloadOption
                {
                    Format = DownloadFormat.BestDirectLink,
                    Name = Resources.DownloaderFormatBestDirectLinkName,
                    Option = Resources.DownloaderFormatBestDirectLinkOption
                },
                new DownloadOption
                {
                    Format = DownloadFormat.AudioOnly,
                    Name = Resources.DownloaderFormatAudioOnlyName,
                    Option = Resources.DownloaderFormatAudioOnlyOption
                }
            };

            ValidateDownload();
        }

        private int RetrieveItemFileName()
        {
            LastDownloadedFilePath = string.Empty;

            var arguments = new StringBuilder();
            arguments.Append(Resources.DownloaderEncodingUtf8Option);
            arguments.Append(" ");
            arguments.Append(Resources.DownloaderGetFilenameOption);
            arguments.Append(" ");
            arguments.Append($"-o \"{DownloadFolderPath}\\{Resources.DownloaderItemTitleTemplate}\"");
            arguments.Append(" ");
            arguments.Append(YouTubeLink);
            var downloaderProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = _downloaderPath,
                    Arguments = arguments.ToString(),
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                }
            };
            downloaderProcess.OutputDataReceived += (sender, e) =>
            {
                DownloadLog = e.Data;
                DownloadLog = Environment.NewLine;
            };

            downloaderProcess.ErrorDataReceived += (sender, e) =>
            {
                DownloadLog = e.Data;
                DownloadLog = Environment.NewLine;
            };

            downloaderProcess.Start();

            downloaderProcess.BeginErrorReadLine();
            downloaderProcess.BeginOutputReadLine();

            while (!downloaderProcess.HasExited)
            {
                downloaderProcess.WaitForExit(ProcessWaitTimeoutMs);
                if (_cancellation.IsCancellationRequested)
                {
                    downloaderProcess.Kill();
                    downloaderProcess.WaitForExit();
                    _cancellation.Token.ThrowIfCancellationRequested();
                }
            }

            LastDownloadedFilePath = downloaderProcess.ExitCode == 0 ? DownloadLog.Trim() : string.Empty;

            return downloaderProcess.ExitCode;
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

                DownloadLog = string.Empty;

                if (RetrieveItemFileName() != 0)
                {
                    _lastDownloadStatus = DownloadStatus.Fail;
                    return;
                }

                DownloadLog =
                    $"{Environment.NewLine}{Resources.LogMessageDownloadStart}{Environment.NewLine}{Environment.NewLine}";

                _cancellation.Token.ThrowIfCancellationRequested();

                var arguments = new StringBuilder();
                arguments.Append(Resources.DownloaderEncodingUtf8Option);
                arguments.Append(" ");
                arguments.Append($"-f \"{SelectedDownloadOption.Option}\"");
                arguments.Append(" ");
                arguments.Append($"-o \"{Path.Combine(DownloadFolderPath, LastDownloadedFilePath)}\"");
                arguments.Append(" ");
                arguments.Append($"{Resources.DownloaderConverterLocationOption} \"{_converterPath}\"");
                arguments.Append(" ");
                arguments.Append(YouTubeLink);
                var downloaderProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = _downloaderPath,
                        Arguments = arguments.ToString(),
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        StandardOutputEncoding = Encoding.UTF8,
                        StandardErrorEncoding = Encoding.UTF8
                    }
                };
                downloaderProcess.OutputDataReceived += (o, e) =>
                {
                    DownloadLog = e.Data;
                    DownloadLog = Environment.NewLine;
                };

                downloaderProcess.ErrorDataReceived += (o, e) =>
                {
                    DownloadLog = e.Data;
                    DownloadLog = Environment.NewLine;
                };

                downloaderProcess.Start();

                downloaderProcess.BeginErrorReadLine();
                downloaderProcess.BeginOutputReadLine();

                while (!downloaderProcess.HasExited)
                {
                    downloaderProcess.WaitForExit(ProcessWaitTimeoutMs);
                    if (_cancellation.IsCancellationRequested)
                    {
                        downloaderProcess.Kill();
                        downloaderProcess.WaitForExit();
                        _cancellation.Token.ThrowIfCancellationRequested();
                    }
                }

                _lastDownloadStatus = downloaderProcess.ExitCode == 0
                    ? DownloadStatus.Success
                    : DownloadStatus.Fail;
            }
            catch (OperationCanceledException e)
            {
                _lastDownloadStatus = DownloadStatus.Cancel;
                DownloadLog = Environment.NewLine;
                DownloadLog = e.Message;
            }
            catch (Exception e)
            {
                _lastDownloadStatus = DownloadStatus.Fail;
                DownloadLog = Environment.NewLine;
                DownloadLog = e.Message;
            }
        }
    }
}