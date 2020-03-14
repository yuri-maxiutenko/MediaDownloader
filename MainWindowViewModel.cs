using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
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
        public DownloadFormat Format { get; set; }
        public string Name { get; set; }
        public string Option { get; set; }
    }

    internal class MainWindowViewModel : INotifyPropertyChanged
    {
        private ICommand _clearButtonClick;
        private ICommand _downloadButtonClick;
        private ICommand _openDownloadFolderButtonClick;

        private bool _isDownloadButtonEnabled;
        private bool _isOpenDownloadFolderButtonEnabled;

        private string _downloadFolderPath;
        private string _userDownloadsFolder;
        private string _youTubeLink;

        public MainWindowViewModel()
        {
            Initialize();
        }

        public string UserDownloadsFolder =>
            _userDownloadsFolder ??
            (_userDownloadsFolder = new KnownFolder(KnownFolderType.DownloadsLocalized).ExpandedPath);

        public List<DownloadOption> DownloadOptions { get; private set; }

        public ICommand ClearButtonClick
        {
            get
            {
                return _clearButtonClick ?? (_clearButtonClick = new RelayCommand(
                    param => { YouTubeLink = string.Empty; },
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
                    Option = "bestaudio"
                }
            };

            ValidateDownload();
        }
    }
}