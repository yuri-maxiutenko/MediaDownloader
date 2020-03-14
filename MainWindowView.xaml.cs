using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;

namespace YoutubeDownloader
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly MainWindowViewModel _model;

        public MainWindow()
        {
            InitializeComponent();

            _model = new MainWindowViewModel();
            DataContext = _model;
        }

        private void BrowseButton_OnClick(object sender, RoutedEventArgs e)
        {
            var selectFolderDialog = new FolderBrowserDialog
            {
                ShowNewFolderButton = true,
                SelectedPath = string.IsNullOrEmpty(_model.DownloadFolderPath)
                    ? _model.UserDownloadsFolder
                    : _model.DownloadFolderPath
            };

            var result = selectFolderDialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                _model.DownloadFolderPath = selectFolderDialog.SelectedPath;
            } 
        }

        private void YouTubeLink_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            _model.ValidateDownload();
        }

        private void YouTubeLink_OnPasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                var text = (string) e.DataObject.GetData(typeof(string));
                if (!Utilities.IsValidUrl(text))
                {
                    e.CancelCommand();
                }
            }

            _model.ValidateDownload();
        }

        private void DownloadFolderPath_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            _model.ValidateDownload();
        }
    }
}