using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;

using MediaDownloader.UI.ViewModels;

using Serilog;

using TextBox = System.Windows.Controls.TextBox;

namespace MediaDownloader.UI.Views;

/// <summary>
///     Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow
{
    public MainWindow()
    {
        ViewModel = new MainWindowViewModel();

        InitializeComponent();
    }

    public MainWindowViewModel ViewModel { get; }

    private void BrowseButton_OnClick(object sender, RoutedEventArgs e)
    {
        var selectFolderDialog = new FolderBrowserDialog
        {
            ShowNewFolderButton = true,
            SelectedPath = string.IsNullOrEmpty(ViewModel.SelectedDownloadFolder?.Path)
                ? ViewModel.UserVideosFolder
                : ViewModel.SelectedDownloadFolder.Path
        };

        var result = selectFolderDialog.ShowDialog();
        if (result == System.Windows.Forms.DialogResult.OK)
        {
            ViewModel.AddOrUpdateDownloadFolder(selectFolderDialog.SelectedPath, DateTime.Now);
        }
    }

    private void MediaUrl_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        ViewModel.ValidateDownload();
    }

    private void MediaUrl_OnPasting(object sender, DataObjectPastingEventArgs e)
    {
        if (e.DataObject.GetDataPresent(typeof(string)))
        {
            var text = (string)e.DataObject.GetData(typeof(string));
            if (!Utilities.Utilities.IsValidUrl(text))
            {
                e.CancelCommand();
            }
        }

        ViewModel.ValidateDownload();
    }

    private void DownloadLog_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is not TextBox downloadLog)
        {
            return;
        }

        downloadLog.CaretIndex = downloadLog.Text.Length;
        downloadLog.ScrollToEnd();
    }

    private void MediaUrl_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        SelectText(sender);
    }

    private void MediaUrl_OnGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        SelectText(sender);
    }

    private void MediaUrl_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not TextBox textBox)
        {
            return;
        }

        if (textBox.IsKeyboardFocusWithin)
        {
            return;
        }

        e.Handled = true;
        textBox.Focus();
    }

    private void SelectText(object sender)
    {
        var textBox = sender as TextBox;
        textBox?.SelectAll();
    }

    private void HistoryGridHyperlink_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            if (e.OriginalSource is not Hyperlink hyperlink)
            {
                return;
            }

            var destination = hyperlink.NavigateUri;
            Process.Start(new ProcessStartInfo(destination.ToString()) { UseShellExecute = true });
        }
        catch (Exception exception)
        {
            Log.Error(exception, "Failed to open hyperlink");
        }
    }

    private void OnDownloadCompleted(object sender, EventArgs e)
    {
        Dispatcher.Invoke(() => ViewModel.DownloadHistory.View.Refresh());
    }

    private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
    {
        ViewModel.DownloadCompleted += OnDownloadCompleted;
        _ = ViewModel.UpdateDownloaderAsync();
    }

    private void MainWindow_OnUnloaded(object sender, RoutedEventArgs e)
    {
        ViewModel.DownloadCompleted -= OnDownloadCompleted;
    }
}