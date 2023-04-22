using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;

using TextBox = System.Windows.Controls.TextBox;

namespace MediaDownloader;

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
            SelectedPath = string.IsNullOrEmpty(_model.SelectedDownloadFolder?.Path)
                ? _model.UserVideosFolder
                : _model.SelectedDownloadFolder.Path
        };

        var result = selectFolderDialog.ShowDialog();
        if (result == System.Windows.Forms.DialogResult.OK)
        {
            _model.AddOrUpdateDownloadFolder(selectFolderDialog.SelectedPath, DateTime.Now);
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
            var text = (string)e.DataObject.GetData(typeof(string));
            if (!Utilities.IsValidUrl(text))
            {
                e.CancelCommand();
            }
        }

        _model.ValidateDownload();
    }

    private void DownloadLog_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is TextBox downloadLog)
        {
            downloadLog.CaretIndex = downloadLog.Text.Length;
            downloadLog.ScrollToEnd();
        }
    }

    private void YouTubeLink_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        SelectText(sender);
    }

    private void YouTubeLink_OnGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        SelectText(sender);
    }

    private void YouTubeLink_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            if (!textBox.IsKeyboardFocusWithin)
            {
                e.Handled = true;
                textBox.Focus();
            }
        }
    }

    private void SelectText(object sender)
    {
        var textBox = sender as TextBox;
        textBox?.SelectAll();
    }

    private void HistoryGridHyperlink_OnClick(object sender, RoutedEventArgs e)
    {
        var destination = ((Hyperlink)e.OriginalSource).NavigateUri;
        Process.Start(destination.ToString());
    }
}