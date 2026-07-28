using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;

using MediaDownloader.UI.ViewModels;

using Microsoft.Win32;

using Log = Serilog.Log;

namespace MediaDownloader.UI.Views;

/// <summary>
///     Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow
{
    public MainWindow(MainWindowViewModel viewModel)
    {
        ViewModel = viewModel;

        InitializeComponent();
    }

    public MainWindowViewModel ViewModel { get; }

    private void BrowseButton_OnClick(object sender, RoutedEventArgs e)
    {
        var selectFolderDialog = new OpenFolderDialog
        {
            InitialDirectory = string.IsNullOrEmpty(ViewModel.SelectedDownloadFolder?.Path)
                ? ViewModel.UserVideosFolder
                : ViewModel.SelectedDownloadFolder.Path
        };

        if (selectFolderDialog.ShowDialog(this) == true)
        {
            ViewModel.AddOrUpdateDownloadFolder(selectFolderDialog.FolderName, DateTime.Now);
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

            // The URI comes from the history database, which is user-writable on disk;
            // only http(s) links may be handed to the shell.
            var destination = hyperlink.NavigateUri;
            if (!Utilities.Utilities.IsValidUrl(destination.ToString()))
            {
                Log.Warning("Blocked attempt to open non-http(s) URI {Uri}", destination);
                return;
            }

            Process.Start(new ProcessStartInfo(destination.ToString())
            {
                UseShellExecute = true
            });
        }
        catch (Exception exception)
        {
            Log.Error(exception, "Failed to open hyperlink");
        }
    }

    private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
    {
        _ = ViewModel.UpdateDownloaderAsync();
    }
}