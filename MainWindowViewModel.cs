using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Input;
using Syroot.Windows.IO;

namespace YoutubeDownloader
{
    internal enum DownloadFormat
    {
        Best,
        BestDirectLink,
        AudioOnly
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
        readonly object _logWritingLock = new object();

        private const string DownloaderFileName = "youtube-dl.exe";
        private const string FfmpegDirectory = @"ffmpeg\bin";

        private ICommand _clearButtonClick;
        private ICommand _downloadButtonClick;

        private string _downloaderPath;
        private string _downloadFolderPath;
        private readonly StringBuilder _downloadLog = new StringBuilder();
        private string _ffmpegPath;

        private bool _isDownloadButtonEnabled;
        private bool _isOpenDownloadFolderButtonEnabled;
        private ICommand _openDownloadFolderButtonClick;

        private DownloadOption _selectedDownloadOption;
        private string _userDownloadsFolder;
        private string _youTubeLink;

        public MainWindowViewModel()
        {
            Initialize();
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

        public ICommand DownloadButtonClick
        {
            get
            {
                return _downloadButtonClick ?? (_downloadButtonClick = new RelayCommand(
                    param =>
                    {
                        ThreadPool.QueueUserWorkItem(DownloadItemAsync);
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
                        if (Directory.Exists(DownloadFolderPath))
                        {
                            Process.Start(DownloadFolderPath);
                        }
                    },
                    param => true));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

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
            _downloaderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DownloaderFileName);
            _ffmpegPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, FfmpegDirectory);

            DownloadFolderPath = UserDownloadsFolder;
            DownloadOptions = new List<DownloadOption>
            {
                new DownloadOption
                {
                    Format = DownloadFormat.Best,
                    Name = "Best quality video",
                    Option = "bestvideo[ext=mp4]+bestaudio[ext=m4a]/best[ext=mp4]/best"
                },
                new DownloadOption
                {
                    Format = DownloadFormat.BestDirectLink,
                    Name = "Best quality video available by direct link",
                    Option = "(bestvideo+bestaudio/best)[protocol^=http]"
                },
                new DownloadOption
                {
                    Format = DownloadFormat.AudioOnly,
                    Name = "Audio only",
                    Option = "mp3/ogg/m4a/aac"
                }
            };

            ValidateDownload();
        }

        private void DownloadItemAsync(object o)
        {
            lock (_logWritingLock)
            {
                _downloadLog.Clear();
            }

            DownloadLog = string.Empty;

            var arguments = new StringBuilder();
            arguments.Append($"-f \"{SelectedDownloadOption.Option}\"");
            arguments.Append(" ");
            arguments.Append($"-o \"{DownloadFolderPath}\\%(title)s.%(ext)s\"");
            arguments.Append(" ");
            arguments.Append($"--ffmpeg-location \"{_ffmpegPath}\"");
            arguments.Append(" ");
            arguments.Append(YouTubeLink);
            var downloadProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = _downloaderPath,
                    Arguments = arguments.ToString(),
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };
            downloadProcess.OutputDataReceived += (sender, e) =>
            {
                DownloadLog = e.Data;
                DownloadLog = Environment.NewLine;
            };

            downloadProcess.ErrorDataReceived += (sender, e) =>
            {
                DownloadLog = e.Data;
                DownloadLog = Environment.NewLine;
            };

            downloadProcess.Start();

            downloadProcess.BeginErrorReadLine();
            downloadProcess.BeginOutputReadLine();

            downloadProcess.WaitForExit();
        }
    }
}