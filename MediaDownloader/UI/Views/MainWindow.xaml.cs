using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;

using MediaDownloader.UI.ViewModels;

using NLog;

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

    private Logger Logger { get; } = LogManager.GetCurrentClassLogger();

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

    private void YouTubeLink_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        ViewModel.ValidateDownload();
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
        var destination = ((Hyperlink)e.OriginalSource).NavigateUri;
        Process.Start(destination.ToString());
    }

    private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
    {
        try
        {
            ViewModel.UpdateDownloader();
        }
        catch (Exception exception)
        {
            Logger.Error(exception, "Failed to load main window");
        }
    }
}