using System.Windows.Input;

namespace YoutubeDownloader
{
    class MainWindowViewModel
    {
        private ICommand _clearButtonClick = null;
        public ICommand ClearButtonClick
        {
            get
            {
                return _clearButtonClick ?? (_clearButtonClick = new RelayCommand(
                    param => { },
                    param => true));
            }
        }

        private ICommand _browseButtonClick = null;
        public ICommand BrowseButtonClick
        {
            get
            {
                return _browseButtonClick ?? (_browseButtonClick = new RelayCommand(
                    param => { },
                    param => true));
            }
        }

        private ICommand _downloadButtonClick = null;
        public ICommand DownloadButtonClick
        {
            get
            {
                return _downloadButtonClick ?? (_downloadButtonClick = new RelayCommand(
                    param => { },
                    param => true));
            }
        }
    }
}
