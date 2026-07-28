using System.ComponentModel;
using System.Windows.Data;

using MediaDownloader.Data;
using MediaDownloader.Data.Models;

namespace MediaDownloader.Services;

public sealed class DownloadFolderService : IDownloadFolderService
{
    private readonly Storage _storage;

    public DownloadFolderService(Storage storage)
    {
        _storage = storage;

        if (!storage.DownloadFolders.Any())
        {
            storage.AddDownloadFolder(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), DateTime.Now);
        }

        FoldersView = new CollectionViewSource
        {
            Source = storage.DownloadFolders
        };
        FoldersView.SortDescriptions.Add(
            new SortDescription(nameof(DownloadFolder.LastSelectionDate), ListSortDirection.Descending));
        MoveCurrentToFirst();
    }

    public CollectionViewSource FoldersView { get; }

    public void AddOrUpdate(string path, DateTime lastSelectionDate)
    {
        _storage.AddOrUpdateDownloadFolder(path, lastSelectionDate);
        RefreshAndMoveCurrentToFirst();
    }

    public void Touch(DownloadFolder folder, DateTime lastSelectionDate)
    {
        _storage.UpdateDownloadFolder(folder.DownloadFolderId, folder.Path, lastSelectionDate);
        RefreshAndMoveCurrentToFirst();
    }

    private void RefreshAndMoveCurrentToFirst()
    {
        FoldersView.View.Refresh();
        MoveCurrentToFirst();
    }

    private void MoveCurrentToFirst()
    {
        var firstItem = (FoldersView.View as CollectionView)?.GetItemAt(0);
        FoldersView.View.MoveCurrentTo(firstItem);
    }
}
